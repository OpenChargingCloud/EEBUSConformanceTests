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
using cloud.charging.open.protocols.EEBUS.UseCases.MPC;

#endregion

namespace cloud.charging.open.protocols.EEBUS.Simulations
{

    /// <summary>
    /// A meter publishing a load profile, and an energy manager watching it.
    ///
    /// The plainest simulation and the one which shows what "subscribe rather
    /// than poll" actually buys: the meter publishes a new value every minute
    /// and the energy manager never asks for anything after the first read. The
    /// general implementation guideline § 3.2.2 makes that the rule and § 3.2.3
    /// calls polling next to a working subscription an anti-pattern; this counts
    /// the datagrams and shows the difference.
    ///
    /// The load profile is a day in a house: a quiet night, a morning peak,
    /// a photovoltaic dip around noon where the meter reading goes **negative**
    /// because the house is exporting, and an evening peak. Which is also the
    /// case a careless client gets wrong, because a house which exports 2 kW is
    /// not a house which imports 2 kW.
    /// </summary>
    public class MPCMeterSimulation : ASimulation
    {

        #region Data

        private SPINELocalDevice        hems       = null!;
        private SPINELocalDevice        meter      = null!;
        private SPINELoopback           wire       = null!;

        private MPCMonitoringAppliance  appliance  = null!;
        private MPCMonitoredUnit        unit       = null!;

        private Int32                   asked;

        #endregion

        #region Properties

        /// <summary>What this simulation is called on the command line.</summary>
        public override String  Name         => "mpc-meter";

        /// <summary>What it shows.</summary>
        public override String  Description  => "a meter publishes a day's load profile; the energy manager never asks twice";

        /// <summary>The meter, as the energy manager sees it.</summary>
        private SPINERemoteEntity MU        => wire.BAsSeenByA.Entity([ 1 ])!;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create the meter simulation.
        /// </summary>
        /// <param name="Options">How it is to be run.</param>
        public MPCMeterSimulation(SimulationOptions? Options = null)

            : base(Options,
                   Resolution: TimeSpan.FromMinutes(1))

        { }

        #endregion


        #region (override) Build(CancellationToken)

        /// <summary>
        /// A meter and an energy manager, connected.
        /// </summary>
        protected override async Task Build(CancellationToken CancellationToken)
        {

            hems      = new SPINELocalDevice("d:_i:19667_HEMS",
                                             DeviceTypeType.EnergyManagementSystem,
                                             TimeProvider: Clock.TimeProvider);

            meter     = new SPINELocalDevice("d:_i:19667_Meter",
                                             DeviceTypeType.SubMeter,
                                             TimeProvider: Clock.TimeProvider);

            appliance = new MPCMonitoringAppliance(hems. AddEntity(EntityTypeType.CEM));

            unit      = new MPCMonitoredUnit      (meter.AddEntity(EntityTypeType.SubMeterElectricity),
                                                   PowerPerPhase:  true,
                                                   Energy:         true);

            wire = new SPINELoopback(hems, meter);

            await appliance.Register();
            await unit.     Register();

            await wire.A.NodeManagement.RequestDetailedDiscovery(wire.BAsSeenByA);
            await wire.B.NodeManagement.RequestDetailedDiscovery(wire.AAsSeenByB);
            await wire.A.NodeManagement.RequestUseCaseData      (wire.BAsSeenByA);
            await wire.B.NodeManagement.RequestUseCaseData      (wire.AAsSeenByB);

            var scenarios = appliance.PartnerFor(MU)?.Scenarios.Order().ToList() ?? [];

            Note("energy manager",
                 $"found a monitored unit which supports scenarios {String.Join(", ", scenarios)}");

        }

        #endregion

        #region (override) Script()

        /// <summary>
        /// Subscribe once, then publish a day.
        /// </summary>
        protected override IEnumerable<SimulationStep> Script()
        {

            var hour = TimeSpan.FromHours(1);

            yield return At(TimeSpan.Zero, "subscribe", async cancellationToken => {

                await appliance.Subscribe(MU, cancellationToken);

                asked = wire.AToB.Datagrams.Count;

                Note("energy manager",
                     $"read the descriptions and subscribed, in {asked} datagrams",
                     "IG § 3.2.2");

            });

            // Six in the morning to ten at night, a value every fifteen minutes.
            for (var minutes = 0; minutes <= 16 * 60; minutes += 15)
            {

                var at    = TimeSpan.FromMinutes(minutes);
                var power = HouseholdPower(TimeSpan.FromHours(6) + at);

                yield return At(at, $"publish {power} W", async cancellationToken => {

                    await unit.Set(MonitoringOfPowerConsumption.PowerTotal,
                                   power,
                                   cancellationToken);

                    // What the energy manager knows, read from its own side of
                    // the wire rather than from the meter's.
                    var reading = appliance.Read(MU, MonitoringOfPowerConsumption.PowerTotal);

                    Log.Sample(Clock.Elapsed, "published [W]", power);
                    Log.Sample(Clock.Elapsed, "received [W]",  reading?.Value ?? 0);

                });

            }

            // Six hours in is midday, when the photovoltaic system is producing
            // more than the house uses and the reading goes below zero.
            yield return At(6 * hour, "note the exporting hour", cancellationToken => {

                var value = appliance.Read(MU, MonitoringOfPowerConsumption.PowerTotal)?.Value;

                Note("energy manager",
                     value < 0
                         ? $"the meter reads {value} W - negative, so the house is exporting, " +
                           $"and a client which took the absolute value would see an import of {Math.Abs(value.Value)} W"
                         : $"the meter reads {value} W");

                return Task.CompletedTask;

            });

            yield return At(16 * hour, "count the datagrams", cancellationToken => {

                Note("energy manager",
                     $"asked for nothing after subscribing: {wire.AToB.Datagrams.Count - asked} datagram(s) sent " +
                     $"while {wire.BToA.Datagrams.Count} arrived",
                     "IG § 3.2.3");

                return Task.CompletedTask;

            });

        }

        #endregion

        #region (override) Settle

        /// <summary>Nothing has to settle: the last publish is the last event.</summary>
        protected override TimeSpan Settle => TimeSpan.Zero;

        #endregion


        #region (private) HouseholdPower(TimeOfDay)

        /// <summary>
        /// What a household draws at a given time of day, in watts.
        ///
        /// A quiet night, a morning peak, a photovoltaic dip in the middle of
        /// the day where the number goes negative because the house is
        /// exporting, and an evening peak. Rough on purpose - the point is the
        /// shape, and in particular that it crosses zero.
        /// </summary>
        private static Decimal HouseholdPower(TimeSpan TimeOfDay)
        {

            var hour = (Decimal) TimeOfDay.TotalHours % 24;

            return hour switch {

                       < 6   =>   300,                        // the fridge and the router
                       < 9   =>  2200,                        // showers and kettles
                       < 11  =>   900,
                       < 15  => -2400,                        // the sun is out and the house exports
                       < 17  =>  -600,
                       < 19  =>  1400,
                       < 22  =>  3100,                        // cooking, washing, television
                       _     =>   400

                   };

        }

        #endregion

    }

}
