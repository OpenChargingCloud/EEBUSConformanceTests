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
using cloud.charging.open.protocols.EEBUS.UseCases.Commissioning;
using cloud.charging.open.protocols.EEBUS.UseCases.EVCC;
using cloud.charging.open.protocols.EEBUS.UseCases.EVCEM;
using cloud.charging.open.protocols.EEBUS.UseCases.EVSECC;
using cloud.charging.open.protocols.EEBUS.UseCases.OPEV;

#endregion

namespace cloud.charging.open.protocols.EEBUS.Simulations
{

    /// <summary>
    /// A car arrives, says what it is, charges, and is curtailed.
    ///
    /// The whole e-mobility commissioning chain in one story, in the order it
    /// happens on a real forecourt: the charging station is already there and
    /// says who made it (EVSECC); a car is plugged in and its **EV entity
    /// appears** (EVCC scenario 1); the car says what it speaks, whether it
    /// charges asymmetrically and how hard it may be charged; it starts drawing
    /// current and reporting it (EVCEM); the energy guard curtails it phase by
    /// phase (OPEV); and at the end the energy guard announces a failure and the
    /// car falls back to a current it chose itself.
    ///
    /// Five use cases on two devices, with one EV entity playing four server
    /// actors at once - which is the situation ADR 0006 and ADR 0007 are about,
    /// here as a story rather than as a unit test.
    ///
    /// Run it with `--fault guard` to see the failure at the end; without it the
    /// curtailment is simply released.
    /// </summary>
    public class OPEVCurtailSimulation : ASimulation
    {

        #region Data

        private SPINELocalDevice       hems      = null!;
        private SPINELocalDevice       evse      = null!;
        private SPINELoopback          wire      = null!;

        private SPINELocalEntity?      evEntity;

        private EVSECCChargingStation  station   = null!;
        private EVCCElectricVehicle    car       = null!;
        private EVCEMElectricVehicle   measured  = null!;
        private OPEVElectricVehicle    curtailed = null!;

        private EVSECCEnergyManager    manager   = null!;
        private EVCCEnergyManager      commis    = null!;
        private EVCEMEnergyManager     meter     = null!;
        private OPEVEnergyGuard        guard     = null!;

        #endregion

        #region Properties

        /// <summary>What this simulation is called on the command line.</summary>
        public override String               Name         => "opev-curtail";

        /// <summary>What it shows.</summary>
        public override String               Description  => "a car arrives, is commissioned, charges, and is curtailed phase by phase";

        /// <summary>What can be made to go wrong.</summary>
        public override IEnumerable<String>  Faults       => [ "guard" ];

        /// <summary>The charging station, as the energy manager sees it.</summary>
        private SPINERemoteEntity            EVSE         => wire.BAsSeenByA.Entity([ 1 ])!;

        /// <summary>The car, as the energy manager sees it.</summary>
        private SPINERemoteEntity            EV           => wire.BAsSeenByA.Entity([ 2 ])!;

        /// <summary>The energy manager, as the car sees it.</summary>
        private SPINERemoteEntity            CEM          => wire.AAsSeenByB.Entity([ 1 ])!;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create the curtailment simulation.
        /// </summary>
        /// <param name="Options">How it is to be run.</param>
        public OPEVCurtailSimulation(SimulationOptions? Options = null)

            : base(Options)

        { }

        #endregion


        #region (override) Build(CancellationToken)

        /// <summary>
        /// An energy manager and a charging station. The car comes later,
        /// because that is the whole of EVCC scenario 1.
        /// </summary>
        protected override async Task Build(CancellationToken CancellationToken)
        {

            hems     = new SPINELocalDevice("d:_i:19667_HEMS",
                                            DeviceTypeType.EnergyManagementSystem,
                                            TimeProvider: Clock.TimeProvider);

            evse     = new SPINELocalDevice("d:_i:19667_EVSE",
                                            DeviceTypeType.ChargingStation,
                                            TimeProvider: Clock.TimeProvider);

            var cem  = hems.AddEntity(EntityTypeType.CEM);

            station  = new EVSECCChargingStation(evse.AddEntity(EntityTypeType.EVSE),
                                                 new ManufacturerData(DeviceName:  "Wallbox 22",
                                                                       VendorName:  "GraphDefined",
                                                                       BrandName:   "OpenCharging"));

            manager  = new EVSECCEnergyManager(cem);
            commis   = new EVCCEnergyManager  (cem);
            meter    = new EVCEMEnergyManager (cem);
            guard    = new OPEVEnergyGuard    (cem);

            wire = new SPINELoopback(hems, evse);

            await station.Register();
            await manager.Register();
            await commis. Register();
            await meter.  Register();
            await guard.  Register();

            await Discover(CancellationToken);

            Clock.OnTick += (elapsed, _) => {

                if (evEntity is not null)
                {

                    curtailed.Check();

                    var currents = curtailed.ChargingCurrents;

                    Log.Sample(elapsed, "phase A [A]", currents[0]);
                    Log.Sample(elapsed, "phase B [A]", currents[1]);
                    Log.Sample(elapsed, "phase C [A]", currents[2]);

                }

                return Task.CompletedTask;

            };

        }


        /// <summary>
        /// What both sides do whenever the shape of the other one changes -
        /// which here happens when the car is plugged in and its entity appears.
        /// </summary>
        private async Task Discover(CancellationToken CancellationToken)
        {
            await wire.A.NodeManagement.RequestDetailedDiscovery(wire.BAsSeenByA, CancellationToken);
            await wire.B.NodeManagement.RequestDetailedDiscovery(wire.AAsSeenByB, CancellationToken);
            await wire.A.NodeManagement.RequestUseCaseData      (wire.BAsSeenByA, CancellationToken);
            await wire.B.NodeManagement.RequestUseCaseData      (wire.AAsSeenByB, CancellationToken);
        }

        #endregion

        #region (override) Script()

        /// <summary>
        /// What happens when.
        /// </summary>
        protected override IEnumerable<SimulationStep> Script()
        {

            var minute = TimeSpan.FromMinutes(1);

            yield return At(TimeSpan.Zero, "commission the charging station", async cancellationToken => {

                await manager.Subscribe(EVSE, cancellationToken);

                Note("energy manager",
                     $"a charging station is here: {manager.Manufacturer(EVSE)?.DeviceName}, " +
                     $"{(manager.HasFailed(EVSE) ? "in failure" : "working")}",
                     "EVSECC-020");

            });

            yield return At(2 * minute, "a car is plugged in", async cancellationToken => {

                // The EV entity appears below the charging station. This *is*
                // EVCC scenario 1: there is no message which says "a car
                // arrived", there is an entity which was not there before.
                evEntity  = evse.AddEntity(EntityTypeType.EV);

                car       = new EVCCElectricVehicle(evEntity,
                                                    CommunicationStandard:  EVCommissioningAndConfiguration.ISO15118_2_ed2,
                                                    AsymmetricCharging:     true,
                                                    Identifier:             "01-23-45-67-89-AB",
                                                    Manufacturer:           new ManufacturerData(DeviceName: "e-Golf", VendorName: "Volkswagen"),
                                                    MinimumChargingPower:   1400,
                                                    MaximumChargingPower:   11000,
                                                    SleepMode:              true);

                measured  = new EVCEMElectricVehicle(evEntity, Current: true, Power: true, Energy: true);
                curtailed = new OPEVElectricVehicle (evEntity);

                curtailed.SetPermittedCurrents(6, 16);
                curtailed.SafeCurrent = 6;

                await car.      Register();
                await measured. Register();
                await curtailed.Register();

                await Discover(cancellationToken);

                Note("charging station", "an EV entity appeared below the EVSE", "EVCC-001");

            });

            yield return At(3 * minute, "read what the car is", async cancellationToken => {

                await commis.Subscribe(EV, cancellationToken);
                await meter. Subscribe(EV, cancellationToken);

                var limits = commis.ChargingPowerLimits(EV);

                Note("energy manager",
                     $"{commis.Manufacturer(EV)?.DeviceName}, speaks {commis.CommunicationStandard(EV)}, " +
                     $"asymmetric charging {(commis.AsymmetricCharging(EV) == true ? "supported" : "not supported")}, " +
                     $"identifier {commis.Identifier(EV)}",
                     "EVCC-002, EVCC-006, EVCC-007");

                Note("energy manager",
                     $"it charges between {limits?.Minimum} W and {limits?.Maximum} W - " +
                     $"and the minimum is not zero, so throttling below it stops the session rather than slowing it",
                     "EVCC-017");

            });

            yield return At(4 * minute, "commission the curtailment", async cancellationToken => {

                await new UseCaseFeature(FeatureTypeType.DeviceDiagnosis, evEntity!, CEM).
                          Subscribe(cancellationToken);

                var loadControl = guard.LoadControlOf(EV);

                await loadControl.Subscribe(cancellationToken);
                await loadControl.Bind     (cancellationToken);

                await guard.ElectricalOf(EV).Subscribe(cancellationToken);
                await guard.StartHeartbeat(CancellationToken: cancellationToken);

                curtailed.Check();

                var phases = await guard.ReadPhases(EV, cancellationToken);

                Note("energy guard",
                     $"the car charges on {phases.Count} phase(s), " +
                     $"between {phases[0].MinimumCurrent} A and {phases[0].MaximumCurrent} A each",
                     "OPEV-001");

            });

            yield return At(5 * minute, "charge at 16 A", async cancellationToken => {

                await guard.WriteCurrentLimit(EV, 16, CancellationToken: cancellationToken);

                await measured.Set([
                    (MeasurementOfElectricityDuringEVCharging.Current(ElectricalConnectionPhaseNameType.A), 16),
                    (MeasurementOfElectricityDuringEVCharging.Current(ElectricalConnectionPhaseNameType.B), 16),
                    (MeasurementOfElectricityDuringEVCharging.Current(ElectricalConnectionPhaseNameType.C), 16),
                    (MeasurementOfElectricityDuringEVCharging.PowerTotal,                                   11000)
                ], CancellationToken: cancellationToken);

                Note("energy guard", "16 A per phase - the car charges at full power");

            });

            yield return At(15 * minute, "the circuit gets busy", async cancellationToken => {

                await guard.WriteCurrentLimits(EV, [ 10, 6, 6 ], CancellationToken: cancellationToken);

                await measured.Set([
                    (MeasurementOfElectricityDuringEVCharging.Current(ElectricalConnectionPhaseNameType.A), 10),
                    (MeasurementOfElectricityDuringEVCharging.Current(ElectricalConnectionPhaseNameType.B),  6),
                    (MeasurementOfElectricityDuringEVCharging.Current(ElectricalConnectionPhaseNameType.C),  6)
                ], CancellationToken: cancellationToken);

                Note("energy guard",
                     "curtailed to 10/6/6 A - asymmetrically, because the car said it can, " +
                     "and phase A has room the other two do not",
                     "OPEV-002");

            });

            yield return At(25 * minute, "pause the charging", async cancellationToken => {

                await guard.WriteCurrentLimit(EV, 0, CancellationToken: cancellationToken);

                Note("energy guard", "0 A - the circuit needs everything it has for something else");

            });

            if (Options.Has("guard"))
            {

                yield return At(35 * minute, "the energy guard announces a failure", async cancellationToken => {

                    await guard.SetOperatingState(DeviceDiagnosisOperatingStateType.Failure,
                                                  "E-4711",
                                                  cancellationToken);

                    Note("energy guard",
                         "announced a failure - and it is still beating, so an availability check alone would never notice",
                         "OPEV-007");

                });

            }

            else
            {

                yield return At(35 * minute, "release the curtailment", async cancellationToken => {

                    await guard.WriteCurrentLimit(EV, 16, IsActive: false, CancellationToken: cancellationToken);

                    Note("energy guard", "no curtailment needed any more", "OPEV-004");

                });

            }

        }

        #endregion

        #region (override) Finish(CancellationToken)

        /// <summary>
        /// Say what the car ended up doing, which is the answer the whole
        /// simulation is about.
        /// </summary>
        protected override Task Finish(CancellationToken CancellationToken)
        {

            curtailed.Check();

            Note("car",
                 $"trust: {curtailed.Trust}; charging with {String.Join("/", curtailed.ChargingCurrents)} A");

            return Task.CompletedTask;

        }

        #endregion

    }

}
