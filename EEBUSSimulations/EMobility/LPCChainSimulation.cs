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

using cloud.charging.open.protocols.EEBUS.SPINE;
using cloud.charging.open.protocols.EEBUS.SPINE.Model;
using cloud.charging.open.protocols.EEBUS.UseCases;
using cloud.charging.open.protocols.EEBUS.UseCases.LimitationOfPower;
using cloud.charging.open.protocols.EEBUS.UseCases.LPC;

#endregion

namespace cloud.charging.open.protocols.EEBUS.Simulations
{

    /// <summary>
    /// The §14a EnWG chain: a control box limits a wallbox, and then stops
    /// talking to it.
    ///
    /// This is the scenario the German grid regulation actually produces. A grid
    /// operator's control box is the energy guard; a wallbox is the controllable
    /// system; the limit is 4.2 kW, which is what §14a EnWG guarantees a
    /// controllable consumer at minimum. Everything up to that point is
    /// commissioning.
    ///
    /// The half worth watching is the end. The control box stops sending
    /// heartbeats - a cable, a firmware update, a router - and the wallbox has
    /// to notice and fall back to its **failsafe** value on its own, without
    /// anybody telling it to. That is the entire reason the use case has a state
    /// machine, and it is the one thing a device can pass every scenario test
    /// and still get wrong in the field.
    ///
    /// Run it with `--fault heartbeat` to see that; without the fault the
    /// control box keeps beating and the limit simply expires.
    /// </summary>
    public class LPCChainSimulation : ASimulation
    {

        #region Data

        private SPINELocalDevice       controlBox  = null!;
        private SPINELocalDevice       wallbox     = null!;
        private SPINELoopback          wire        = null!;

        private LPCEnergyGuard         guard       = null!;
        private LPCControllableSystem  system      = null!;

        private TimeSpan?              limitEndsAt;

        #endregion

        #region Properties

        /// <summary>What this simulation is called on the command line.</summary>
        public override String             Name         => "lpc-chain";

        /// <summary>What it shows.</summary>
        public override String             Description  => "§14a EnWG: a control box limits a wallbox to 4.2 kW, then goes quiet";

        /// <summary>What can be made to go wrong.</summary>
        public override IEnumerable<String> Faults      => [ "heartbeat" ];

        /// <summary>The wallbox, as the control box sees it.</summary>
        private SPINERemoteEntity          CS           => wire.BAsSeenByA.Entity([ 1 ])!;

        /// <summary>The control box, as the wallbox sees it.</summary>
        private SPINERemoteEntity          EG           => wire.AAsSeenByB.Entity([ 1 ])!;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create the §14a chain simulation.
        /// </summary>
        /// <param name="Options">How it is to be run.</param>
        public LPCChainSimulation(SimulationOptions? Options = null)

            : base(Options)

        { }

        #endregion


        #region (override) Build(CancellationToken)

        /// <summary>
        /// A control box and a wallbox, connected.
        /// </summary>
        protected override async Task Build(CancellationToken CancellationToken)
        {

            controlBox = new SPINELocalDevice("d:_i:19667_ControlBox",
                                              DeviceTypeType.ElectricitySupplySystem,
                                              TimeProvider: Clock.TimeProvider);

            wallbox    = new SPINELocalDevice("d:_i:19667_Wallbox",
                                              DeviceTypeType.ChargingStation,
                                              TimeProvider: Clock.TimeProvider);

            guard      = new LPCEnergyGuard       (controlBox.AddEntity(EntityTypeType.GridGuard));
            system     = new LPCControllableSystem(wallbox.   AddEntity(EntityTypeType.EVSE));

            // What the wallbox is: 11 kW at most, and it will fall back to
            // 4.2 kW for at least two hours if left alone.
            system.ConsumptionNominalMax    = 11000;
            system.FailsafeLimit            = 4200;
            system.FailsafeDurationMinimum  = TimeSpan.FromHours(2);

            wire = new SPINELoopback(controlBox, wallbox);

            await guard. Register();
            await system.Register();

            await wire.A.NodeManagement.RequestDetailedDiscovery(wire.BAsSeenByA);
            await wire.B.NodeManagement.RequestDetailedDiscovery(wire.AAsSeenByB);
            await wire.A.NodeManagement.RequestUseCaseData      (wire.BAsSeenByA);
            await wire.B.NodeManagement.RequestUseCaseData      (wire.AAsSeenByB);

            Note("control box", $"found a controllable system at {CS.Address}");

            // The wallbox looks at the clock on every tick: the whole point of
            // the use case is what it does when nobody is talking to it, and
            // silence is not an event anybody can raise.
            system.StateMachine.OnTransition += (_, transition) =>
                Log.Log(Clock.Elapsed,
                        "wallbox",
                        $"{transition.From} -> {transition.To}: {transition.Reason}",
                        $"transition {transition.Transition}");

            // A limit arrives with a *relative* end time - "thirty minutes" -
            // which is a duration from the moment it was written and does not
            // count itself down. So the device starts its own timer when the
            // limit arrives, which is the only way it can know when to stop.
            system.OnLimitWritten += (_, _, isActive, accepted) => {

                if (!accepted)
                    return;

                limitEndsAt = isActive && system.ConsumptionLimit.Duration is TimeSpan duration
                                  ? Clock.Elapsed + duration
                                  : null;

            };

            Clock.OnTick += async (elapsed, cancellationToken) => {

                await system.Check(cancellationToken);

                // The state machine notices a stopped heartbeat by itself and
                // deliberately does not notice a limit running out: rule 908 is
                // not one of its three time-driven transitions, because the
                // duration is in the data rather than in the state. So the
                // device's application has to look - and a controllable system
                // which forgets to would hold a limit for ever.
                if (limitEndsAt is TimeSpan ends && elapsed >= ends &&
                    system.StateMachine.State == PowerLimitationState.Limited)
                {
                    limitEndsAt = null;
                    await system.LimitExpired(cancellationToken);
                }

                Log.Sample(elapsed, "charging [W]", Charging());

            };

        }

        #endregion

        #region (override) Script()

        /// <summary>
        /// What happens when.
        /// </summary>
        protected override IEnumerable<SimulationStep> Script()
        {

            var minute = TimeSpan.FromMinutes(1);

            yield return At(TimeSpan.Zero, "commission", async cancellationToken => {

                // The wallbox subscribes to the control box's heartbeat; the
                // control box subscribes to the limit and binds so that it may
                // write one.
                await new UseCaseFeature(FeatureTypeType.DeviceDiagnosis, system.Entity, EG).
                          Subscribe(cancellationToken);

                var loadControl = guard.LoadControlOf(CS);

                await loadControl.Subscribe(cancellationToken);
                await loadControl.Bind     (cancellationToken);

                await guard.ConfigurationOf(CS).Bind(cancellationToken);

                Note("both", "subscriptions and bindings in place");

            });

            yield return At(1 * minute, "read the failsafe values", async cancellationToken => {

                var (limit, duration) = await guard.ReadFailsafeValues(CS, cancellationToken);
                var nominal           = await guard.ReadConsumptionNominalMax(CS, cancellationToken);

                Note("control box",
                     $"the wallbox falls back to {limit} W for at least {duration}, and draws at most {nominal} W",
                     "LPC-940");

            });

            yield return At(2 * minute, "start the heartbeat", async cancellationToken => {

                await guard.StartHeartbeat(CancellationToken: cancellationToken);

                Note("control box", "heartbeat started", "LPC-930");

            });

            yield return At(5 * minute, "limit to 4.2 kW for 30 minutes", async cancellationToken => {

                await guard.WriteConsumptionLimit(CS,
                                                  4200,
                                                  IsActive:           true,
                                                  Duration:           TimeSpan.FromMinutes(30),
                                                  CancellationToken:  cancellationToken);

                Note("control box", "wrote a limit of 4200 W for 30 minutes", "LPC-901");

            });

            yield return At(20 * minute, "prolong the limit", async cancellationToken => {

                await guard.WriteConsumptionLimit(CS,
                                                  4200,
                                                  IsActive:           true,
                                                  Duration:           TimeSpan.FromMinutes(30),
                                                  CancellationToken:  cancellationToken);

                Note("control box", "prolonged the limit by another 30 minutes");

            });

            if (Options.Has("heartbeat"))
            {

                yield return At(30 * minute, "the control box goes quiet", cancellationToken => {

                    guard.StopHeartbeat();

                    Note("control box",
                         "stopped sending heartbeats - a cable, a firmware update, a router",
                         "LPC-912");

                    return Task.CompletedTask;

                });

                // The heartbeat timeout of the limitation of power consumption
                // is two minutes, so the wallbox should have fallen back by
                // minute 32 or so - on its own, from its own clock.
                yield return Say(35 * minute,
                                 "observer",
                                 "the wallbox has been alone for five minutes; whatever it is doing now, it decided by itself");

            }

            else
            {

                yield return Say(50 * minute,
                                 "observer",
                                 "the limit runs out while the control box is still there and healthy");

                yield return Say(55 * minute,
                                 "observer",
                                 "no limit is in force and the wallbox may draw its full nominal power");

            }

        }

        #endregion

        #region (override) Settle

        /// <summary>
        /// Long enough after the last step for the two minute heartbeat timeout
        /// to have elapsed and for the transition it causes to be in the log.
        /// </summary>
        protected override TimeSpan Settle => TimeSpan.FromMinutes(5);

        #endregion


        #region (private) Charging()

        /// <summary>
        /// What the wallbox is actually drawing.
        ///
        /// A car which would take 11 kW if it were allowed to, against whichever
        /// limit the state machine says is in force - the one the control box
        /// wrote, the failsafe one, or none at all (Table 1 of the
        /// specification). This is the number the whole chain exists to control,
        /// and it is the one a person looks at.
        /// </summary>
        private Decimal Charging()
        {

            var wanted = 11000m;

            var limit  = system.StateMachine.Limitation switch {

                             PowerLimitationApplied.ActivePowerLimit  => system.ConsumptionLimit.Value,
                             PowerLimitationApplied.FailsafeLimit     => system.FailsafeLimit,
                             _                                        => null

                         };

            return limit is Decimal value && value < wanted
                       ? value
                       : wanted;

        }

        #endregion

    }

}
