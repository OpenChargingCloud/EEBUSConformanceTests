/*
 * Copyright (c) 2014-2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of EEBUSConformanceTests <https://github.com/OpenChargingCloud/EEBUSConformanceTests>
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

namespace cloud.charging.open.protocols.EEBUS.Simulations
{

    /// <summary>
    /// One thing a simulation script does at one point on its time axis.
    /// </summary>
    /// <param name="At">How far into the simulation it happens.</param>
    /// <param name="What">What it is, for the log.</param>
    /// <param name="Action">Doing it.</param>
    public sealed record SimulationStep(TimeSpan                            At,
                                        String                              What,
                                        Func<CancellationToken, Task>       Action)
    {

        /// <summary>Return a text representation of this step.</summary>
        public override String ToString()

            => $"{At:hh\\:mm\\:ss}  {What}";

    }


    /// <summary>
    /// How a simulation is to be run.
    /// </summary>
    /// <param name="Speed">How many simulated seconds pass per real second. Zero runs as fast as possible.</param>
    /// <param name="Faults">Which things are to go wrong, by name.</param>
    /// <param name="Device">Which recorded device to replay, where the simulation replays one.</param>
    public sealed record SimulationOptions(Double                Speed     = 0,
                                           IEnumerable<String>?  Faults    = null,
                                           String?               Device    = null)
    {

        private readonly HashSet<String> faults = (Faults ?? []).ToHashSet(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Whether the given thing is to go wrong in this run.
        ///
        /// The interesting half of every one of these use cases is what happens
        /// when something stops working, and a simulation which can only show
        /// the happy path shows the half nobody needed to see.
        /// </summary>
        /// <param name="Fault">The name of a fault.</param>
        public Boolean Has(String Fault)

            => faults.Contains(Fault);

    }


    /// <summary>
    /// What a simulation produced.
    /// </summary>
    /// <param name="Name">Which simulation it was.</param>
    /// <param name="Duration">How long it ran, in simulated time.</param>
    /// <param name="Log">What happened and what the numbers did.</param>
    public sealed record SimulationResult(String         Name,
                                          TimeSpan       Duration,
                                          SimulationLog  Log);


    /// <summary>
    /// A scripted scenario over a set of simulated EEBUS devices.
    ///
    /// Every simulation is the same three things: some devices with use cases on
    /// them, a script of what happens when, and a log of what came out. The
    /// devices talk to each other over an in-memory loopback rather than over
    /// SHIP, because what is being simulated here is the **use case layer** -
    /// a real socket would add nothing to a story about failsafe values and
    /// charging currents, and would take away the determinism which makes these
    /// double as integration tests.
    ///
    /// Everything runs on one <see cref="SimulationClock"/>, so a two-hour
    /// failsafe duration and a four-second heartbeat timeout cost the same
    /// nothing to simulate, and running the same script twice gives the same
    /// answer twice.
    /// </summary>
    public abstract class ASimulation
    {

        #region Properties

        /// <summary>
        /// What this simulation is called on the command line.
        /// </summary>
        public abstract String            Name          { get; }

        /// <summary>
        /// What it shows, in one line.
        /// </summary>
        public abstract String            Description   { get; }

        /// <summary>
        /// The faults this simulation knows how to inject, for the help text.
        /// </summary>
        public virtual IEnumerable<String> Faults       => [];

        /// <summary>
        /// How it is being run.
        /// </summary>
        public SimulationOptions          Options       { get; }

        /// <summary>
        /// Its time axis.
        /// </summary>
        public SimulationClock            Clock         { get; }

        /// <summary>
        /// What happened.
        /// </summary>
        public SimulationLog              Log           { get; } = new ();

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a simulation.
        /// </summary>
        /// <param name="Options">How it is to be run.</param>
        /// <param name="Resolution">How large a step its clock takes at a time.</param>
        protected ASimulation(SimulationOptions?  Options      = null,
                              TimeSpan?           Resolution   = null)
        {

            this.Options  = Options ?? new SimulationOptions();
            this.Clock    = new SimulationClock(Resolution:  Resolution,
                                                Speed:       this.Options.Speed);

        }

        #endregion


        #region Run(CancellationToken = default)

        /// <summary>
        /// Build the devices, run the script, and return what happened.
        /// </summary>
        /// <param name="CancellationToken">An optional cancellation token.</param>
        public async Task<SimulationResult> Run(CancellationToken CancellationToken = default)
        {

            await Build(CancellationToken);

            var script = Script().OrderBy(step => step.At).ToList();

            foreach (var step in script)
            {

                await Clock.AdvanceTo(step.At, CancellationToken);

                if (CancellationToken.IsCancellationRequested)
                    break;

                await step.Action(CancellationToken);

            }

            // Let the last thing which was set take effect and be seen: a script
            // whose final step writes a limit and then stops has not shown
            // whether the limit was applied.
            await Clock.AdvanceTo((script.LastOrDefault()?.At ?? TimeSpan.Zero) + Settle,
                                  CancellationToken);

            await Finish(CancellationToken);

            return new SimulationResult(Name, Clock.Elapsed, Log);

        }

        #endregion


        #region (protected) Build(...) / Script() / Finish(...) / Settle

        /// <summary>
        /// Create the devices, put the use cases on them and connect them.
        /// </summary>
        /// <param name="CancellationToken">An optional cancellation token.</param>
        protected abstract Task Build(CancellationToken CancellationToken);


        /// <summary>
        /// What happens when.
        /// </summary>
        protected abstract IEnumerable<SimulationStep> Script();


        /// <summary>
        /// Anything to do once the script has run out. Nothing, usually.
        /// </summary>
        /// <param name="CancellationToken">An optional cancellation token.</param>
        protected virtual Task Finish(CancellationToken CancellationToken)

            => Task.CompletedTask;


        /// <summary>
        /// How long the clock keeps running after the last step of the script.
        /// </summary>
        protected virtual TimeSpan Settle => TimeSpan.FromMinutes(1);

        #endregion

        #region (protected) At(...) / Say(...) / Note(...)

        /// <summary>
        /// A step of the script.
        /// </summary>
        /// <param name="At">How far into the simulation.</param>
        /// <param name="What">What it is.</param>
        /// <param name="Action">Doing it.</param>
        protected SimulationStep At(TimeSpan                       At,
                                    String                         What,
                                    Func<CancellationToken, Task>  Action)

            => new (At, What, Action);


        /// <summary>
        /// A step which only writes something down - a marker in the story.
        /// </summary>
        /// <param name="At">How far into the simulation.</param>
        /// <param name="Actor">Who.</param>
        /// <param name="What">What.</param>
        /// <param name="Rule">Which rule of a specification says so.</param>
        protected SimulationStep Say(TimeSpan  At,
                                     String    Actor,
                                     String    What,
                                     String?   Rule   = null)

            => new (At,
                    What,
                    _ => {
                        Log.Log(At, Actor, What, Rule);
                        return Task.CompletedTask;
                    });


        /// <summary>
        /// Write something down at the current point of the simulation.
        /// </summary>
        /// <param name="Actor">Who.</param>
        /// <param name="What">What.</param>
        /// <param name="Rule">Which rule of a specification says so.</param>
        protected void Note(String   Actor,
                            String   What,
                            String?  Rule   = null)
        {
            Log.Log(Clock.Elapsed, Actor, What, Rule);
        }

        #endregion

    }

}
