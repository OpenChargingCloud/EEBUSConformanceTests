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

#region Usings

using cloud.charging.open.protocols.EEBUS.SPINE.Model;

#endregion

namespace cloud.charging.open.protocols.EEBUS.Conformance
{

    #region (class) AMonitoringCase

    /// <summary>
    /// The shared start of every abstract test case of the two monitoring use
    /// cases.
    /// </summary>
    /// <param name="UseCase">"MGCP" or "MPC".</param>
    /// <param name="Suffix">The rest of the official identifier.</param>
    public abstract class AMonitoringCase(String  UseCase,
                                          String  Suffix) : AConformanceTest
    {

        /// <summary>The official abstract test case identifier.</summary>
        public override String Id
            => $"ATC_{UseCase}_{Suffix}";

        /// <summary>Which of the two use cases this run is about.</summary>
        protected String  Monitoring
            => UseCase;

        /// <summary>Whether the device under test is the watching side.</summary>
        protected Boolean DUTIsAppliance
            => TestCase.Actor == "MA";


        /// <summary>
        /// CF_*_ConnectionEstablished on both sides, which is the pre-condition
        /// of every case in both specifications.
        /// </summary>
        /// <param name="Context">Where the steps are written down.</param>
        /// <param name="CancellationToken">An optional cancellation token.</param>
        protected async Task<MonitoringScenario> Connected(ConformanceContext  Context,
                                                           CancellationToken   CancellationToken)
        {

            var scenario = await MonitoringScenario.Create(Context.Parameters,
                                                            Monitoring,
                                                            DUTIsAppliance,
                                                            CancellationToken);

            await Context.Precondition($"CF_{scenario.Measured.Profile.ServerActor}_ConnectionEstablished, CF_MA_ConnectionEstablished",
                                       async () => await scenario.Connect(CancellationToken));

            return scenario;

        }


        /// <summary>
        /// The quantity a case is about, or an inconclusive verdict when this
        /// device does not carry it at all.
        /// </summary>
        /// <param name="Scenario">The two wired devices.</param>
        /// <param name="DataPoint">The data point name of the abstract test case.</param>
        /// <param name="Phase">The phase or phase pair, where it has one.</param>
        protected static UseCases.Monitoring.MonitoringQuantity Quantity(MonitoringScenario  Scenario,
                                                                         String              DataPoint,
                                                                         String?             Phase)

            => Scenario.Quantity(DataPoint, Phase)
                   ?? throw new ConformanceInconclusive($"{DataPoint}{(Phase is not null ? $" on {Phase}" : "")} " +
                                                        $"is not a data point of this use case");

    }

    #endregion


    #region (class) ARhythmCase

    /// <summary>
    /// The four cases per specification which count messages rather than look at
    /// them: is the data asked for, or sent, often enough.
    ///
    /// The two supplementary requirements of section 5.3 are unusual and worth
    /// reading twice: an appliance which polls makes the polling cases mandatory,
    /// an appliance which subscribes makes the notification cases mandatory, and
    /// neither is required to do both. So a device is measured against the
    /// mechanism it declared rather than against a mechanism the test bench
    /// assumed.
    /// </summary>
    /// <param name="UseCase">"MGCP" or "MPC".</param>
    /// <param name="Suffix">The rest of the official identifier.</param>
    /// <param name="Polling">Whether this is the polling case rather than the notification one.</param>
    public abstract class ARhythmCase(String   UseCase,
                                      String   Suffix,
                                      Boolean  Polling) : AMonitoringCase(UseCase, Suffix)
    {

        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            var scenario  = await Connected(Context, CancellationToken);
            var quantity  = scenario.Quantity("TotalActivePower")
                                ?? throw new ConformanceInconclusive("this use case has no total active power to watch");

            var window    = TimeSpan.FromSeconds(120);
            var mark      = scenario.Measurements().Count;

            await Context.Step("1",
                               Polling
                                   ? "Polling: request and count 5 data messages."
                                   : "Notification: initiate 5 value changes within 10 minutes.",
                               Polling
                                   ? "The data of the chosen data point arrives after each request within 120 seconds."
                                   : "A new value is sent for each change, each within 120 seconds of the change.",
                               async step => {

                                   for (var round = 1; round <= 5; round++)
                                   {

                                       await scenario.Publish(quantity, 1000 + 100 * round, CancellationToken: CancellationToken);

                                       if (Polling)
                                           await scenario.PollMeasurements(CancellationToken);

                                       await scenario.Advance(TimeSpan.FromSeconds(30), CancellationToken);

                                   }

                                   var (count, widest) = scenario.Measurements();

                                   step.Observe($"{count - mark} measurement message(s), the widest gap {widest.TotalSeconds:F0} s");

                                   step.Require(count - mark >= 5,
                                                $"only {count - mark} of the 5 expected measurement messages arrived");

                                   step.Require(widest <= window,
                                                $"{widest.TotalSeconds:F0} seconds passed between two measurement messages, " +
                                                $"which is more than the 120 seconds the use case asks for");

                               });

        }

    }

    #endregion

    #region (class) AMeasuredValueCase

    /// <summary>
    /// The measuring side publishes a value and the watching side gets it.
    ///
    /// The plainest thing in the whole catalog and the one everything else rests
    /// on: a number left one device and arrived at the other with its meaning
    /// intact. The sign is part of the meaning - positive while consuming,
    /// negative while producing - and a device which gets it backwards has told
    /// an energy manager to do the opposite of what it should.
    /// </summary>
    /// <param name="UseCase">"MGCP" or "MPC".</param>
    /// <param name="Suffix">The rest of the official identifier.</param>
    /// <param name="DataPoint">Which data point the case is about.</param>
    /// <param name="Phase">The phase or phase pair, where it has one.</param>
    /// <param name="Producing">Whether the device is made to produce rather than to consume.</param>
    /// <param name="Directed">Whether the sign of the value follows the direction of energy at all.</param>
    public abstract class AMeasuredValueCase(String   UseCase,
                                             String   Suffix,
                                             String   DataPoint,
                                             String?  Phase       = null,
                                             Boolean  Producing   = false,
                                             Boolean  Directed    = true) : AMonitoringCase(UseCase, Suffix)
    {

        /// <summary>Whether the value is expected to stay where it is rather than to move.</summary>
        protected virtual Boolean Unchanging
            => false;


        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            var scenario  = await Connected(Context, CancellationToken);
            var quantity  = Quantity(scenario, DataPoint, Phase);

            // The sign is what the direction means. Voltage and frequency are
            // measured whatever the energy is doing, so they are always positive.
            var sign      = Directed && Producing ? -1 : 1;
            var first     = sign * Baseline(DataPoint);
            var second    = Unchanging ? first : sign * (Baseline(DataPoint) + Step(DataPoint));

            await Context.Step("1",
                               $"Determine the current data for {Describe()}.",
                               "A value can be determined by the MA.",
                               async step => {

                                   await scenario.Publish(quantity, first, CancellationToken: CancellationToken);
                                   await scenario.Advance(TimeSpan.FromSeconds(5), CancellationToken);

                                   var reading = scenario.Read(quantity);

                                   step.Observe($"the watching side reads {reading?.Value.ToString() ?? "nothing"}");

                                   step.Require(reading is not null,
                                                $"the watching side has no value for {Describe()} at all");

                                   step.Require(reading!.Value == first,
                                                $"the watching side reads {reading.Value} rather than the {first} which was published");

                               });

            await Context.Step("2",
                               Unchanging
                                   ? $"Keep the device in the {(Producing ? "producing" : "consuming")} direction."
                                   : $"Initiate a value change on the measuring side large enough to send data for {Describe()}.",
                               Unchanging
                                   ? $"The value for {Describe()} does not change."
                                   : $"A new value for {Describe()} is sent within 120 seconds since the value changed.",
                               async step => {

                                   await scenario.Publish(quantity, second, CancellationToken: CancellationToken);
                                   await scenario.Advance(TimeSpan.FromSeconds(120), CancellationToken);

                                   var reading = scenario.Read(quantity);

                                   step.Observe($"the watching side reads {reading?.Value.ToString() ?? "nothing"}");

                                   step.Require(reading is not null,
                                                $"the watching side lost its value for {Describe()}");

                                   step.Require(reading!.Value == second,
                                                $"the watching side reads {reading.Value} rather than the {second} which was published");

                                   if (Directed)
                                       step.Require(Producing ? reading.Value <= 0 : reading.Value >= 0,
                                                    $"the value is {reading.Value} while the device is " +
                                                    $"{(Producing ? "producing" : "consuming")}, which has the wrong sign " +
                                                    $"for the direction of energy");

                               });

        }


        /// <summary>What the case is about, in words, for the report.</summary>
        protected String Describe()

            => DataPoint switch {
                   "TotalActivePower"     => "the momentary power",
                   "PhaseActivePower"     => $"the active power on phase {Phase?.ToUpperInvariant()}",
                   "TotalConsumedEnergy"  => "the total consumed energy",
                   "TotalProducedEnergy"  => "the total produced energy",
                   "TotalFeedInEnergy"    => "the total feed-in energy",
                   "ActiveACCurrent"      => $"the AC current on phase {Phase?.ToUpperInvariant()}",
                   "ACVoltage"            => $"the AC voltage on {Phase?.ToUpperInvariant()}",
                   "Frequency"            => "the frequency",
                   _                      => DataPoint
               };


        /// <summary>A plausible first value for a data point, in its own unit.</summary>
        internal static Decimal Baseline(String DataPoint)

            => DataPoint switch {
                   "TotalActivePower"     => 4200,
                   "PhaseActivePower"     => 1400,
                   "TotalConsumedEnergy"  => 123456,
                   "TotalProducedEnergy"  => 65432,
                   "TotalFeedInEnergy"    => 65432,
                   "ActiveACCurrent"      => 6,
                   "ACVoltage"            => 230,
                   "Frequency"            => 50,
                   _                      => 1
               };


        /// <summary>How far it moves when something happens.</summary>
        internal static Decimal Step(String DataPoint)

            => DataPoint switch {
                   "TotalActivePower"     => 800,
                   "PhaseActivePower"     => 300,
                   "TotalConsumedEnergy"  => 250,
                   "TotalProducedEnergy"  => 250,
                   "TotalFeedInEnergy"    => 250,
                   "ActiveACCurrent"      => 4,
                   "ACVoltage"            => 3,
                   "Frequency"            => 1,
                   _                      => 1
               };

    }

    #endregion

    #region (class) ADiscardCase

    /// <summary>
    /// A value the sensor has disowned is thrown away rather than used
    /// ([*-TS-008], [*-TS-008/1], [*-TS-008/2]).
    ///
    /// This is the only requirement in either monitoring use case which is about
    /// something *not* happening, and it is the one worth having a test bench
    /// for. A meter which knows its reading is wrong and says so has done its
    /// job; an energy manager which takes the number anyway has turned a detected
    /// fault into an undetected one, and will curtail a house on a measurement
    /// its own supplier already disclaimed.
    ///
    /// The check is done against a good value published first: "discarded" has to
    /// mean the appliance still holds what it last legitimately knew, not that it
    /// holds nothing.
    /// </summary>
    /// <param name="UseCase">"MGCP" or "MPC".</param>
    /// <param name="Suffix">The rest of the official identifier.</param>
    /// <param name="DataPoint">Which data point the case is about.</param>
    /// <param name="Phase">The phase or phase pair, where it has one.</param>
    public abstract class ADiscardCase(String   UseCase,
                                       String   Suffix,
                                       String   DataPoint,
                                       String?  Phase   = null) : AMonitoringCase(UseCase, Suffix)
    {

        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            var scenario  = await Connected(Context, CancellationToken);
            var quantity  = Quantity(scenario, DataPoint, Phase);
            var good      = AMeasuredValueCase.Baseline(DataPoint);
            var bad       = good + 9999;

            await Context.Precondition("a value the monitoring appliance may legitimately hold",
                                       async () => {

                await scenario.Publish(quantity, good, CancellationToken: CancellationToken);
                await scenario.Advance(TimeSpan.FromSeconds(5), CancellationToken);

                if (scenario.Read(quantity)?.Value != good)
                    throw new ConformanceInconclusive("the monitoring appliance did not take the good value to begin with");

            });

            foreach (var state in new[] { MeasurementValueStateType.OutOfRange,
                                          MeasurementValueStateType.Error })

                await Context.Step(state == MeasurementValueStateType.OutOfRange ? "1.1" : "1.2",
                                   $"Send the data for {Describe()} with the value state \"{state}\".",
                                   $"The MA receives the data with a value state \"{state}\" and discards it.",
                                   async step => {

                                       await scenario.Publish(quantity, bad, state, CancellationToken);
                                       await scenario.Advance(TimeSpan.FromSeconds(5), CancellationToken);

                                       var reading = scenario.Read(quantity);

                                       step.Observe($"after publishing {bad} as \"{state}\", the watching side reads " +
                                                    $"{reading?.Value.ToString() ?? "nothing"}");

                                       step.Require(reading?.Value != bad,
                                                    $"the monitoring appliance took {bad} although the measuring side had " +
                                                    $"marked it \"{state}\" and required it to be ignored");

                                   });

        }


        /// <summary>What the case is about, in words.</summary>
        private String Describe()

            => DataPoint switch {
                   "TotalActivePower"     => "the momentary power",
                   "PhaseActivePower"     => $"the active power on phase {Phase?.ToUpperInvariant()}",
                   "TotalConsumedEnergy"  => "the total consumed energy",
                   "TotalProducedEnergy"  => "the total produced energy",
                   "TotalFeedInEnergy"    => "the total feed-in energy",
                   "ActiveACCurrent"      => $"the AC current on phase {Phase?.ToUpperInvariant()}",
                   "ACVoltage"            => $"the AC voltage on {Phase?.ToUpperInvariant()}",
                   "Frequency"            => "the frequency",
                   _                      => DataPoint
               };

    }

    #endregion

    #region (class) APowerLimitFactorCase

    /// <summary>
    /// The grid connection point publishes how much of what the photovoltaic
    /// system could produce it is allowed to feed in ([MGCP-TS-001]).
    ///
    /// The one data point in either monitoring use case which is not a
    /// measurement at all: it is a configuration value, set by an installer
    /// according to local regulations, and it lives on the device configuration
    /// feature rather than on the measurement one.
    /// </summary>
    /// <param name="Suffix">The rest of the official identifier.</param>
    public abstract class APowerLimitFactorCase(String Suffix) : AMonitoringCase("MGCP", Suffix)
    {

        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            var scenario = await Connected(Context, CancellationToken);

            var point    = scenario.GridConnectionPoint
                               ?? throw new ConformanceInconclusive("this scenario has no grid connection point");

            await Context.Step("1",
                               "Determine the current PV feed-in power limitation factor.",
                               "A value between zero and one is provided.",
                               async step => {

                                   await point.SetCurtailmentLimitFactor(0.7m, CancellationToken);
                                   await scenario.Advance(TimeSpan.FromSeconds(5), CancellationToken);

                                   var factor = point.CurtailmentLimitFactor;

                                   step.Observe($"the factor reads {factor}");

                                   step.Require(factor is not null,
                                                "the grid connection point publishes no PV feed-in power limitation factor");

                                   step.Require(factor >= 0 && factor <= 1,
                                                $"the factor is {factor}, which is not a fraction between zero and one");

                               });

        }

    }

    #endregion

}
