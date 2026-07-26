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
using cloud.charging.open.protocols.EEBUS.UseCases.OPEV;
using cloud.charging.open.protocols.EEBUS.UseCases.OSCEV;

#endregion

namespace cloud.charging.open.protocols.EEBUS.Simulations
{

    /// <summary>
    /// A day in a house with a photovoltaic system, a car and a grid operator.
    ///
    /// This is the one which is actually hard, and it is hard for a reason worth
    /// stating: the energy manager in the middle is a **controllable system**
    /// looking upwards and an **energy guard** looking downwards. The grid
    /// operator's control box limits the house; the house limits the car. One
    /// entity, two roles, opposite directions - and the number which comes down
    /// from the control box in watts has to come out at the car as a current per
    /// phase.
    ///
    /// On top of that the same energy manager is advising the car about the sun
    /// (OSCEV) while curtailing it for the grid (OPEV), and those two write to
    /// the same load control feature of the same car. The obligation wins where
    /// they disagree - that is what obligation means - and the simulation shows
    /// both numbers next to each other so it can be seen.
    ///
    /// The shape of the day: the sun comes up and the recommendation rises with
    /// it; the grid operator limits the house at the evening peak; the sun goes
    /// down; the limit is released.
    /// </summary>
    public class EMobilityDaySimulation : ASimulation
    {

        #region Data

        private SPINELocalDevice       controlBox   = null!;
        private SPINELocalDevice       hems         = null!;
        private SPINELocalDevice       evse         = null!;

        private SPINELoopback          gridWire     = null!;
        private SPINELoopback          carWire      = null!;

        private LPCEnergyGuard         gridGuard    = null!;
        private LPCControllableSystem  house        = null!;

        private OPEVEnergyGuard        houseGuard   = null!;
        private OSCEVEnergyManager     sunshine     = null!;

        private OPEVElectricVehicle    curtailed    = null!;
        private OSCEVElectricVehicle   optimised    = null!;

        private TimeSpan?              limitEndsAt;

        #endregion

        #region Properties

        /// <summary>What this simulation is called on the command line.</summary>
        public override String               Name         => "emobility-day";

        /// <summary>What it shows.</summary>
        public override String               Description  => "a day: the grid limits the house, the house limits the car, and the sun advises it";

        /// <summary>What can be made to go wrong.</summary>
        public override IEnumerable<String>  Faults       => [ "heartbeat" ];

        /// <summary>The house, as the control box sees it.</summary>
        private SPINERemoteEntity            CS           => gridWire.BAsSeenByA.Entity([ 1 ])!;

        /// <summary>The control box, as the house sees it.</summary>
        private SPINERemoteEntity            EG           => gridWire.AAsSeenByB.Entity([ 1 ])!;

        /// <summary>The car, as the house sees it.</summary>
        private SPINERemoteEntity            EV           => carWire.BAsSeenByA.Entity([ 1 ])!;

        /// <summary>The house, as the car sees it.</summary>
        private SPINERemoteEntity            CEM          => carWire.AAsSeenByB.Entity([ 1 ])!;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create the day simulation.
        /// </summary>
        /// <param name="Options">How it is to be run.</param>
        public EMobilityDaySimulation(SimulationOptions? Options = null)

            : base(Options,
                   Resolution: TimeSpan.FromMinutes(1))

        { }

        #endregion


        #region (override) Build(CancellationToken)

        /// <summary>
        /// Three devices and two wires: the grid operator to the house, and the
        /// house to the car.
        /// </summary>
        protected override async Task Build(CancellationToken CancellationToken)
        {

            controlBox = new SPINELocalDevice("d:_i:19667_ControlBox",
                                              DeviceTypeType.ElectricitySupplySystem,
                                              TimeProvider: Clock.TimeProvider);

            hems       = new SPINELocalDevice("d:_i:19667_HEMS",
                                              DeviceTypeType.EnergyManagementSystem,
                                              TimeProvider: Clock.TimeProvider);

            evse       = new SPINELocalDevice("d:_i:19667_EVSE",
                                              DeviceTypeType.ChargingStation,
                                              TimeProvider: Clock.TimeProvider);

            gridGuard  = new LPCEnergyGuard(controlBox.AddEntity(EntityTypeType.GridGuard));

            // The energy manager: one entity which is the controllable system of
            // the grid operator and the energy guard of the car at the same
            // time. IsEnergyManager changes which nominal maximum it reports -
            // the house's, not a single appliance's.
            var cemEntity = hems.AddEntity(EntityTypeType.CEM);

            house      = new LPCControllableSystem(cemEntity, IsEnergyManager: true);
            houseGuard = new OPEVEnergyGuard      (cemEntity);
            sunshine   = new OSCEVEnergyManager   (cemEntity);

            house.ConsumptionNominalMax    = 30000;   // the house connection
            house.FailsafeLimit            = 4200;    // §14a
            house.FailsafeDurationMinimum  = TimeSpan.FromHours(2);

            var evEntity  = evse.AddEntity(EntityTypeType.EV);

            curtailed  = new OPEVElectricVehicle (evEntity);
            optimised  = new OSCEVElectricVehicle(evEntity);

            curtailed.SetPermittedCurrents(6, 16);
            optimised.SetPermittedCurrents(6, 16);
            curtailed.SafeCurrent = 6;

            gridWire = new SPINELoopback(controlBox, hems);
            carWire  = new SPINELoopback(hems,       evse);

            await gridGuard. Register();
            await house.     Register();
            await houseGuard.Register();
            await sunshine.  Register();
            await curtailed. Register();
            await optimised. Register();

            foreach (var link in new[] { gridWire, carWire })
            {
                await link.A.NodeManagement.RequestDetailedDiscovery(link.BAsSeenByA, CancellationToken);
                await link.B.NodeManagement.RequestDetailedDiscovery(link.AAsSeenByB, CancellationToken);
                await link.A.NodeManagement.RequestUseCaseData      (link.BAsSeenByA, CancellationToken);
                await link.B.NodeManagement.RequestUseCaseData      (link.AAsSeenByB, CancellationToken);
            }

            house.StateMachine.OnTransition += (_, transition) =>
                Log.Log(Clock.Elapsed,
                        "energy manager",
                        $"{transition.From} -> {transition.To}: {transition.Reason}",
                        $"transition {transition.Transition}");

            // A limit's end time is relative to when it was written, so the
            // device starts its own timer - see LPCChainSimulation for why.
            house.OnLimitWritten += (_, _, isActive, accepted) => {

                if (accepted)
                    limitEndsAt = isActive && house.ConsumptionLimit.Duration is TimeSpan duration
                                      ? Clock.Elapsed + duration
                                      : null;

            };

            Clock.OnTick += async (elapsed, cancellationToken) => {

                await house.Check(cancellationToken);

                if (limitEndsAt is TimeSpan ends && elapsed >= ends &&
                    house.StateMachine.State == PowerLimitationState.Limited)
                {
                    limitEndsAt = null;
                    await house.LimitExpired(cancellationToken);
                }

                curtailed.Check();
                optimised.Check();

                Log.Sample(elapsed, "house limit [W]",   HouseLimit());
                Log.Sample(elapsed, "PV surplus [W]",    Sunshine(elapsed));
                Log.Sample(elapsed, "obligation [A]",    curtailed.ChargingCurrents[0]);
                Log.Sample(elapsed, "recommendation [A]", optimised.RecommendedCurrents[0] ?? 0);

            };

        }

        #endregion

        #region (override) Script()

        /// <summary>
        /// A day, in hours from six in the morning.
        /// </summary>
        protected override IEnumerable<SimulationStep> Script()
        {

            var hour = TimeSpan.FromHours(1);

            yield return At(TimeSpan.Zero, "commission both directions", async cancellationToken => {

                // Upwards: the house watches the control box's heartbeat, the
                // control box may write the house's limit.
                await new UseCaseFeature(FeatureTypeType.DeviceDiagnosis, house.Entity, EG).
                          Subscribe(cancellationToken);

                var houseLoad = gridGuard.LoadControlOf(CS);
                await houseLoad.Subscribe(cancellationToken);
                await houseLoad.Bind     (cancellationToken);
                await gridGuard.ConfigurationOf(CS).Bind(cancellationToken);
                await gridGuard.StartHeartbeat(CancellationToken: cancellationToken);

                // Downwards: the car watches the energy manager's heartbeat, the
                // energy manager may write the car's currents.
                await new UseCaseFeature(FeatureTypeType.DeviceDiagnosis, curtailed.Entity, CEM).
                          Subscribe(cancellationToken);

                var carLoad = houseGuard.LoadControlOf(EV);
                await carLoad.Subscribe(cancellationToken);
                await carLoad.Bind     (cancellationToken);
                await houseGuard.ElectricalOf(EV).Subscribe(cancellationToken);
                await houseGuard.StartHeartbeat(CancellationToken: cancellationToken);

                await house.Check(cancellationToken);
                curtailed.Check();
                optimised.Check();

                Note("energy manager",
                     "commissioned in both directions: a controllable system upwards, an energy guard downwards");

            });

            // The sun: a recommendation which follows the surplus, every half
            // hour from eight to eighteen.
            for (var minutes = 2 * 60; minutes <= 12 * 60; minutes += 30)
            {

                var at      = TimeSpan.FromMinutes(minutes);
                var surplus = Sunshine(at);
                var current = Amperes(surplus);

                yield return At(at, $"advise {current} A", async cancellationToken => {

                    if (current > 0)
                        await sunshine.WriteSelfProducedCurrent(EV, current, CancellationToken: cancellationToken);
                    else
                        await sunshine.WriteSelfProducedCurrent(EV, 0, IsActive: false, CancellationToken: cancellationToken);

                });

            }

            yield return At(2 * hour, "the car starts charging", async cancellationToken => {

                await houseGuard.WriteCurrentLimit(EV, 16, CancellationToken: cancellationToken);

                Note("energy manager", "the car may draw its full 16 A per phase; the sun decides what it should");

            });

            yield return At(11 * hour, "the grid operator limits the house", async cancellationToken => {

                await gridGuard.WriteConsumptionLimit(CS,
                                                      4200,
                                                      IsActive:           true,
                                                      Duration:           TimeSpan.FromHours(2),
                                                      CancellationToken:  cancellationToken);

                Note("control box", "limited the house to 4200 W - the §14a minimum", "LPC-901");

            });

            yield return At(11 * hour + TimeSpan.FromMinutes(1), "break the house limit down to the car", async cancellationToken => {

                // The interesting line of the whole simulation. What arrives is
                // a power for the whole house; what has to leave is a current
                // per phase for one car, with the rest of the house taken off
                // first.
                var forTheHouse = HouseLimit();
                var forTheRest  = 1500m;
                var forTheCar   = Math.Max(0, forTheHouse - forTheRest);
                var current     = Amperes(forTheCar);

                await houseGuard.WriteCurrentLimit(EV, current, CancellationToken: cancellationToken);

                Note("energy manager",
                     $"{forTheHouse} W for the house, less {forTheRest} W for everything which is not the car, " +
                     $"leaves {forTheCar} W - which is {current} A per phase across three phases");

            });

            if (Options.Has("heartbeat"))
            {

                yield return At(12 * hour, "the control box goes quiet", cancellationToken => {

                    gridGuard.StopHeartbeat();

                    Note("control box", "stopped sending heartbeats", "LPC-912");

                    return Task.CompletedTask;

                });

                yield return Say(13 * hour,
                                 "observer",
                                 "the house has been alone for an hour; it is holding its own failsafe value now, " +
                                 "and the car is being held to whatever that leaves");

            }

            else
            {

                yield return At(13 * hour, "the limit runs out", cancellationToken => {

                    Note("observer", "the house limit expires while the control box is still there");

                    return Task.CompletedTask;

                });

                yield return At(13 * hour + TimeSpan.FromMinutes(1), "let the car charge again", async cancellationToken => {

                    await houseGuard.WriteCurrentLimit(EV, 16, CancellationToken: cancellationToken);

                    Note("energy manager", "the house is unlimited again, so the car may be too");

                });

            }

            yield return At(14 * hour, "the sun is gone", async cancellationToken => {

                await sunshine.WriteSelfProducedCurrent(EV, 0, IsActive: false, CancellationToken: cancellationToken);

                Note("energy manager",
                     "no self-produced current left to advise about; the car charges on whatever the obligation allows");

            });

        }

        #endregion

        #region (override) Finish(CancellationToken)

        /// <summary>
        /// Say what the day came to.
        /// </summary>
        protected override Task Finish(CancellationToken CancellationToken)
        {

            Note("energy manager",
                 $"ended in {house.StateMachine.State}, holding the house to {HouseLimit()} W");

            Note("car",
                 $"obligation {curtailed.ChargingCurrents[0]} A, " +
                 $"recommendation {optimised.RecommendedCurrents[0]?.ToString() ?? "none"}");

            return Task.CompletedTask;

        }

        #endregion


        #region (private) HouseLimit() / Sunshine(At) / Amperes(Watts)

        /// <summary>
        /// What the house is holding itself to, in watts.
        /// </summary>
        private Decimal HouseLimit()

            => house.StateMachine.Limitation switch {

                   PowerLimitationApplied.ActivePowerLimit  => house.ConsumptionLimit.Value ?? house.ConsumptionNominalMax ?? 0,
                   PowerLimitationApplied.FailsafeLimit     => house.FailsafeLimit          ?? 0,
                   _                                        => house.ConsumptionNominalMax  ?? 0

               };


        /// <summary>
        /// What the photovoltaic system has left over at a given point of the
        /// day, in watts. A bell curve with its peak at midday, which here is
        /// six hours in.
        /// </summary>
        private static Decimal Sunshine(TimeSpan At)
        {

            var hours = At.TotalHours;

            if (hours is < 2 or > 12)
                return 0;

            // 0 at eight, 8 kW at noon, 0 again at six in the evening.
            var fraction = 1 - Math.Abs(hours - 7) / 5;

            return (Decimal) Math.Round(8000 * Math.Max(0, fraction), 0);

        }


        /// <summary>
        /// A three-phase power at 230 V, as a current per phase.
        /// </summary>
        private static Decimal Amperes(Decimal Watts)

            => Math.Clamp(Math.Round(Watts / (3 * 230m), 0), 0, 16);

        #endregion

    }

}
