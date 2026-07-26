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

using Microsoft.Extensions.Time.Testing;

using cloud.charging.open.protocols.EEBUS.SPINE;
using cloud.charging.open.protocols.EEBUS.SPINE.Model;
using cloud.charging.open.protocols.EEBUS.UseCases.MGCP;
using cloud.charging.open.protocols.EEBUS.UseCases.MPC;
using cloud.charging.open.protocols.EEBUS.UseCases.Monitoring;

#endregion

namespace cloud.charging.open.protocols.EEBUS.Conformance
{

    /// <summary>
    /// The test tool of the two monitoring use cases: a device which measures
    /// and a device which watches, one of which is the device under test.
    ///
    /// These use cases are small, which makes the test bench's job unusually
    /// clear. There is no state machine, nothing is written and nothing falls
    /// back, so an abstract test case reduces to three questions: does the value
    /// arrive, does it carry the right sign for the direction of energy, and is a
    /// value the sensor has disowned thrown away rather than used.
    ///
    /// The third is the one worth building a tool for. A measurement carries a
    /// state - normal, out of range, error - and "SHALL be ignored by the MA" is
    /// a requirement about something *not* happening, which can only be seen from
    /// the outside if the tool can publish a value the real device never would.
    /// So the monitored side here can lie on purpose (<see cref="Publish"/> with
    /// a value state), and the 26 negative cases across the two specifications
    /// are exactly that lie told 26 ways.
    /// </summary>
    public sealed class MonitoringScenario
    {

        #region Data

        private readonly FakeTimeProvider  time;

        #endregion

        #region Properties

        /// <summary>The two wired devices.</summary>
        public SPINETestTool          Tool          { get; }

        /// <summary>The device which measures - a monitored unit or a grid connection point.</summary>
        public AMonitoredDevice       Measured      { get; }

        /// <summary>The device which watches.</summary>
        public AMonitoringAppliance   Appliance     { get; }

        /// <summary>Whether the device under test is the watching side.</summary>
        public Boolean                DUTIsAppliance  { get; }

        /// <summary>Which of the two use cases this is: "MGCP" or "MPC".</summary>
        public String                 UseCase       { get; }

        /// <summary>What the device declared about itself.</summary>
        public MonitoringParameters   Sheet         { get; }

        /// <summary>The grid connection point, where this is the MGCP scenario.</summary>
        public MGCPGridConnectionPoint? GridConnectionPoint
            => Measured as MGCPGridConnectionPoint;


        /// <summary>The measuring side, as the watching side sees it.</summary>
        public SPINERemoteEntity? MeasuredAsSeenByAppliance
            => DUTIsAppliance
                   ? Tool.ToolAsSeenByDUT.Entities.FirstOrDefault(entity => entity.EntityId.SequenceEqual(Measured.Entity.EntityId))
                   : Tool.DUTAsSeenByTool.Entities.FirstOrDefault(entity => entity.EntityId.SequenceEqual(Measured.Entity.EntityId));

        #endregion

        #region Constructor(s)

        private MonitoringScenario(SPINETestTool         Tool,
                                   FakeTimeProvider      Time,
                                   AMonitoredDevice      Measured,
                                   AMonitoringAppliance  Appliance,
                                   Boolean               DUTIsAppliance,
                                   String                UseCase,
                                   MonitoringParameters  Sheet)
        {

            this.Tool            = Tool;
            this.time            = Time;
            this.Measured        = Measured;
            this.Appliance       = Appliance;
            this.DUTIsAppliance  = DUTIsAppliance;
            this.UseCase         = UseCase;
            this.Sheet           = Sheet;

        }

        #endregion


        #region (static) Create(Parameters, UseCase, DUTIsAppliance, CancellationToken = default)

        /// <summary>
        /// Build both sides and register both actors.
        /// </summary>
        /// <param name="Parameters">What the device declared about itself.</param>
        /// <param name="UseCase">"MGCP" or "MPC".</param>
        /// <param name="DUTIsAppliance">Whether the device under test is the watching side.</param>
        /// <param name="CancellationToken">An optional cancellation token.</param>
        public static async Task<MonitoringScenario> Create(ParameterSheet     Parameters,
                                                            String             UseCase,
                                                            Boolean            DUTIsAppliance,
                                                            CancellationToken  CancellationToken   = default)
        {

            var sheet   = Parameters.UseCases.Monitoring(UseCase);
            var grid    = UseCase == "MGCP";

            var time    = new FakeTimeProvider(new DateTimeOffset(2026, 7, 26, 12, 0, 0, TimeSpan.Zero));

            var dut     = new SPINELocalDevice("d:_i:19667_DUT",
                                               DUTIsAppliance ? DeviceTypeType.EnergyManagementSystem : DeviceTypeType.ElectricitySupplySystem,
                                               TimeProvider: time);

            var tool    = new SPINELocalDevice("d:_i:19667_TestTool",
                                               DUTIsAppliance ? DeviceTypeType.ElectricitySupplySystem : DeviceTypeType.EnergyManagementSystem,
                                               TimeProvider: time);

            var wire    = new SPINETestTool(dut, tool, time) { AutoAnswer = true };

            var phases  = sheet.Phases.
                              Order().
                              Select(phase => ElectricalConnectionPhaseNameType.Parse(phase)).
                              ToList();

            var measuredEntity   = (DUTIsAppliance ? tool : dut ).AddEntity(grid ? EntityTypeType.GridConnectionPointOfPremises : EntityTypeType.ElectricityStorageSystem);
            var applianceEntity  = (DUTIsAppliance ? dut  : tool).AddEntity(EntityTypeType.CEM);

            AMonitoredDevice      measured   = grid
                                                   ? new MGCPGridConnectionPoint(measuredEntity,
                                                                                 phases,
                                                                                 Curtailment: sheet.PowerLimitFactor,
                                                                                 Current:     sheet.ActiveACCurrent,
                                                                                 Voltage:     sheet.ACVoltage,
                                                                                 Frequency:   sheet.Frequency)
                                                   : new MPCMonitoredUnit(measuredEntity,
                                                                          phases,
                                                                          PowerPerPhase: sheet.PhaseActivePower,
                                                                          Energy:        sheet.TotalConsumedEnergy || sheet.TotalProducedEnergy,
                                                                          Current:       sheet.ActiveACCurrent,
                                                                          Voltage:       sheet.ACVoltage,
                                                                          Frequency:     sheet.Frequency);

            AMonitoringAppliance  appliance  = grid
                                                   ? new MGCPMonitoringAppliance(applianceEntity)
                                                   : new MPCMonitoringAppliance (applianceEntity);

            await measured. Register(CancellationToken);
            await appliance.Register(CancellationToken);

            return new MonitoringScenario(wire, time, measured, appliance, DUTIsAppliance, UseCase, sheet);

        }

        #endregion

        #region Connect(CancellationToken = default)

        /// <summary>
        /// CF_*_ConnectionEstablished on both sides: discovered, described and
        /// subscribed.
        /// </summary>
        /// <param name="CancellationToken">An optional cancellation token.</param>
        public async Task Connect(CancellationToken CancellationToken = default)
        {

            await Tool.DUT. NodeManagement.RequestDetailedDiscovery(Tool.ToolAsSeenByDUT, CancellationToken);
            await Tool.Tool.NodeManagement.RequestDetailedDiscovery(Tool.DUTAsSeenByTool, CancellationToken);
            await Tool.DUT. NodeManagement.RequestUseCaseData      (Tool.ToolAsSeenByDUT, CancellationToken);
            await Tool.Tool.NodeManagement.RequestUseCaseData      (Tool.DUTAsSeenByTool, CancellationToken);

            var measuredSide = MeasuredAsSeenByAppliance
                                   ?? throw new ConformanceInconclusive($"The monitoring appliance did not discover the {Measured.Profile.ServerActor}.");

            await Appliance.Subscribe(measuredSide, CancellationToken);

        }

        #endregion

        #region Advance(By, CancellationToken = default)

        /// <summary>
        /// Let protocol time pass. The monitoring use cases have no timers of
        /// their own; what passes here is the 120 seconds within which a changed
        /// value has to have arrived.
        /// </summary>
        /// <param name="By">How much time passes.</param>
        /// <param name="CancellationToken">An optional cancellation token.</param>
        public async Task Advance(TimeSpan           By,
                                  CancellationToken  CancellationToken   = default)
        {

            var remaining   = By;
            var resolution  = TimeSpan.FromSeconds(1);

            while (remaining > TimeSpan.Zero)
            {

                var step = remaining < resolution ? remaining : resolution;

                time.Advance(step);
                remaining -= step;

                await Task.Yield();

            }

        }

        #endregion


        #region Quantity(DataPoint, Phase = null)

        /// <summary>
        /// The quantity an abstract test case names, in the vocabulary of
        /// whichever of the two use cases this is.
        /// </summary>
        /// <param name="DataPoint">The data point name used by the abstract test cases.</param>
        /// <param name="Phase">The phase or phase pair, where the data point has one.</param>
        public MonitoringQuantity? Quantity(String   DataPoint,
                                            String?  Phase   = null)
        {

            // The abstract test cases name a voltage by the two points it is
            // measured between - "phase A and neutral", "phase C and phase A" -
            // while SPINE names the phase itself and treats neutral as the
            // implied reference (EEBus_SPINE_TS_ElectricalConnection.xsd). So
            // "an" is the phase named "a", and "ca" is the pair SPINE spells the
            // other way round.
            var phase = Phase?.ToLowerInvariant() switch {
                            null   => (ElectricalConnectionPhaseNameType?) null,
                            "a"    => ElectricalConnectionPhaseNameType.A,
                            "b"    => ElectricalConnectionPhaseNameType.B,
                            "c"    => ElectricalConnectionPhaseNameType.C,
                            "an"   => ElectricalConnectionPhaseNameType.A,
                            "bn"   => ElectricalConnectionPhaseNameType.B,
                            "cn"   => ElectricalConnectionPhaseNameType.C,
                            "ab"   => ElectricalConnectionPhaseNameType.Ab,
                            "bc"   => ElectricalConnectionPhaseNameType.Bc,
                            "ca"   => ElectricalConnectionPhaseNameType.Ac,
                            var it => ElectricalConnectionPhaseNameType.Parse(it)
                        };

            return UseCase == "MGCP"

                       ? DataPoint switch {
                             "TotalActivePower"     => MonitoringOfGridConnectionPoint.Power,
                             "TotalFeedInEnergy"    => MonitoringOfGridConnectionPoint.EnergyFeedIn,
                             "TotalConsumedEnergy"  => MonitoringOfGridConnectionPoint.EnergyConsumed,
                             "ActiveACCurrent"      => phase is not null ? MonitoringOfGridConnectionPoint.Current(phase.Value) : null,
                             "ACVoltage"            => phase is not null ? MonitoringOfGridConnectionPoint.Voltage(phase.Value) : null,
                             "Frequency"            => MonitoringOfGridConnectionPoint.Frequency,
                             _                      => null
                         }

                       : DataPoint switch {
                             "TotalActivePower"     => MonitoringOfPowerConsumption.PowerTotal,
                             "PhaseActivePower"     => phase is not null ? MonitoringOfPowerConsumption.Power  (phase.Value) : null,
                             "TotalConsumedEnergy"  => MonitoringOfPowerConsumption.EnergyConsumed,
                             "TotalProducedEnergy"  => MonitoringOfPowerConsumption.EnergyProduced,
                             "ActiveACCurrent"      => phase is not null ? MonitoringOfPowerConsumption.Current(phase.Value) : null,
                             "ACVoltage"            => phase is not null ? MonitoringOfPowerConsumption.Voltage(phase.Value) : null,
                             "Frequency"            => MonitoringOfPowerConsumption.Frequency,
                             _                      => null
                         };

        }

        #endregion

        #region Publish(Quantity, Value, ValueState = null, CancellationToken = default)

        /// <summary>
        /// The measuring side publishes a value, optionally one it has marked as
        /// unusable.
        /// </summary>
        /// <param name="Quantity">What was measured.</param>
        /// <param name="Value">Its value.</param>
        /// <param name="ValueState">Normal by omission; "outOfRange" or "error" for the negative cases.</param>
        /// <param name="CancellationToken">An optional cancellation token.</param>
        public async Task Publish(MonitoringQuantity          Quantity,
                                  Decimal                     Value,
                                  MeasurementValueStateType?  ValueState          = null,
                                  CancellationToken           CancellationToken   = default)
        {
            await Measured.Set(Quantity, Value, ValueState, CancellationToken);
        }

        #endregion

        #region Read(Quantity)

        /// <summary>
        /// What the watching side currently believes about a quantity, or null
        /// when it has nothing - which is the whole point of the negative cases.
        /// </summary>
        /// <param name="Quantity">A quantity.</param>
        public MonitoringReading? Read(MonitoringQuantity Quantity)
        {

            var measuredSide = MeasuredAsSeenByAppliance;

            return measuredSide is null
                       ? null
                       : Appliance.Read(measuredSide, Quantity);

        }

        #endregion

        #region Notifications(Quantity)

        /// <summary>
        /// How many notifications of a quantity crossed the wire from the
        /// measuring side, and how far apart the widest gap between two of them
        /// was.
        ///
        /// The polling and notification cases count messages rather than look at
        /// values, because what they verify is the rhythm: "the interval between
        /// 2 consecutive requests shall not exceed 120 seconds".
        /// </summary>
        public (Int32 Count, TimeSpan Widest) Measurements()
        {

            // The time comes out of the measurement itself rather than out of the
            // datagram header: SPINE leaves the header timestamp optional, and a
            // measurement which does not say when it was taken is of very little
            // use to anybody, so every one of them carries its own.
            var stamps = (DUTIsAppliance ? Tool.FromTool : Tool.FromDUT).
                             SelectMany(datagram => datagram.Payload?.Cmd ?? []).
                             Select    (cmd      => cmd.GetData(MonitoringFunctions.MeasurementListData) as MeasurementListDataType).
                             Where     (data     => data is not null).
                             SelectMany(data     => data!.MeasurementData ?? []).
                             Select    (entry    => entry.Timestamp?.AsDateTimeOffset).
                             Where     (stamp    => stamp is not null).
                             Select    (stamp    => stamp!.Value).
                             OrderBy   (stamp    => stamp).
                             ToList();

            var widest = TimeSpan.Zero;

            for (var index = 1; index < stamps.Count; index++)
                if (stamps[index] - stamps[index - 1] > widest)
                    widest = stamps[index] - stamps[index - 1];

            return (stamps.Count, widest);

        }

        #endregion

        #region Poll(Quantity, CancellationToken = default)

        /// <summary>
        /// The watching side asks for the values rather than waiting to be told.
        /// </summary>
        /// <param name="CancellationToken">An optional cancellation token.</param>
        public async Task PollMeasurements(CancellationToken CancellationToken = default)
        {

            var measuredSide = MeasuredAsSeenByAppliance
                                   ?? throw new ConformanceInconclusive($"The monitoring appliance does not know the {Measured.Profile.ServerActor}.");

            await Appliance.MeasurementOf(measuredSide).
                      RequestData(MonitoringFunctions.MeasurementListData, CancellationToken: CancellationToken);

        }

        #endregion

    }

}
