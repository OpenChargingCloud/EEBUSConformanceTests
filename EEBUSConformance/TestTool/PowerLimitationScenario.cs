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
using cloud.charging.open.protocols.EEBUS.UseCases;
using cloud.charging.open.protocols.EEBUS.UseCases.LimitationOfPower;
using cloud.charging.open.protocols.EEBUS.UseCases.LPC;
using cloud.charging.open.protocols.EEBUS.UseCases.LPP;

#endregion

namespace cloud.charging.open.protocols.EEBUS.Conformance
{

    #region (enum) LimitMessages

    /// <summary>
    /// The seventeen message combinations of section 6.11.4 - which facets of
    /// the limit a single write carries.
    ///
    /// The list looks pedantic until one reads what it is for. A limit has three
    /// separable facets - a value, an activation flag and a duration - and a
    /// write may carry any subset of them. Whether a device treats "the value
    /// alone" the same as "the value together with the activation it already
    /// has" is exactly the sort of thing which works between two implementations
    /// by accident and breaks against the third. So the specification enumerates
    /// all seventeen combinations which mean anything, and each abstract test
    /// case names the ones it may be run with.
    /// </summary>
    public enum LimitMessages
    {

        /// <summary>MSG_01: the value alone.</summary>
        Value,

        /// <summary>MSG_02: the activation alone.</summary>
        Activated,

        /// <summary>MSG_03: the deactivation alone.</summary>
        Deactivated,

        /// <summary>MSG_04: delete the duration.</summary>
        DeleteDuration,

        /// <summary>MSG_05: a duration alone.</summary>
        Duration,

        /// <summary>MSG_06: value and activation.</summary>
        ValueActivated,

        /// <summary>MSG_07: value and deactivation.</summary>
        ValueDeactivated,

        /// <summary>MSG_08: value, and delete the duration.</summary>
        ValueDeleteDuration,

        /// <summary>MSG_09: value and duration.</summary>
        ValueDuration,

        /// <summary>MSG_10: activation, and delete the duration.</summary>
        ActivatedDeleteDuration,

        /// <summary>MSG_11: activation and duration.</summary>
        ActivatedDuration,

        /// <summary>MSG_12: deactivation, and delete the duration.</summary>
        DeactivatedDeleteDuration,

        /// <summary>MSG_13: deactivation and duration.</summary>
        DeactivatedDuration,

        /// <summary>MSG_14: value, activation, and delete the duration.</summary>
        ValueActivatedDeleteDuration,

        /// <summary>MSG_15: value, activation and duration.</summary>
        ValueActivatedDuration,

        /// <summary>MSG_16: value, deactivation, and delete the duration.</summary>
        ValueDeactivatedDeleteDuration,

        /// <summary>MSG_17: value, deactivation and duration.</summary>
        ValueDeactivatedDuration

    }

    #endregion


    /// <summary>
    /// The test tool of the two power limitation use cases: an energy guard and
    /// a controllable system wired together, one of which is the device under
    /// test.
    ///
    /// This is a different thing from <see cref="LpcLppScenario"/>, which exists
    /// so that the SPINE catalog has a working conversation to check protocol
    /// rules inside. Here the conversation *is* the subject: the abstract test
    /// cases ask what state the controllable system is in after two minutes of
    /// silence, what it does with a limit which arrives 61 seconds after a
    /// heartbeat, and whether it still remembers its failsafe values after a
    /// reboot. So this tool keeps the heartbeat manual, can cut the wire, can
    /// restart either side, and can write a limit which carries only some of its
    /// facets.
    ///
    /// Every timeout in these specifications is measured in minutes - 120 seconds
    /// without a heartbeat, two hours in the failsafe state - and there are 102
    /// abstract test cases. On a real clock that is a working week; on the
    /// <see cref="FakeTimeProvider"/> it is a few seconds, and the timings are
    /// exact rather than approximately exact, which matters when a case turns on
    /// the difference between 59 and 61 seconds.
    /// </summary>
    public sealed class PowerLimitationScenario
    {

        #region Data

        private readonly FakeTimeProvider  time;

        #endregion

        #region Properties

        /// <summary>The two wired devices.</summary>
        public SPINETestTool                       Tool         { get; }

        /// <summary>The energy guard, wherever it lives.</summary>
        public APowerLimitationEnergyGuard         Guard        { get; private set; }

        /// <summary>The controllable system, wherever it lives.</summary>
        public APowerLimitationControllableSystem  System       { get; private set; }

        /// <summary>Whether the device under test is the controllable system.</summary>
        public Boolean                             DUTIsSystem  { get; }

        /// <summary>Which of the two use cases this is: "LPC" or "LPP".</summary>
        public String                              UseCase      { get; }

        /// <summary>What the device declared about itself.</summary>
        public PowerLimitationParameters           Sheet        { get; }


        /// <summary>The state the controllable system is in.</summary>
        public PowerLimitationState                State
            => System.StateMachine.State;

        /// <summary>
        /// How many datagrams the device under test has sent so far - the mark a
        /// test step sets before it starts waiting for the next one.
        /// </summary>
        public Int32                               Observed
            => (DUTIsSystem ? Tool.FromTool : Tool.FromDUT).Count;

        /// <summary>The controllable system, as the energy guard sees it.</summary>
        public SPINERemoteEntity? SystemAsSeenByGuard
            => DUTIsSystem
                   ? Tool.DUTAsSeenByTool.Entities.FirstOrDefault(entity => entity.EntityId.SequenceEqual(System.Entity.EntityId))
                   : Tool.ToolAsSeenByDUT.Entities.FirstOrDefault(entity => entity.EntityId.SequenceEqual(System.Entity.EntityId));

        /// <summary>The energy guard, as the controllable system sees it.</summary>
        public SPINERemoteEntity? GuardAsSeenBySystem
            => DUTIsSystem
                   ? Tool.ToolAsSeenByDUT.Entities.FirstOrDefault(entity => entity.EntityId.SequenceEqual(Guard.Entity.EntityId))
                   : Tool.DUTAsSeenByTool.Entities.FirstOrDefault(entity => entity.EntityId.SequenceEqual(Guard.Entity.EntityId));

        #endregion

        #region Constructor(s)

        private PowerLimitationScenario(SPINETestTool                       Tool,
                                        FakeTimeProvider                    Time,
                                        APowerLimitationEnergyGuard         Guard,
                                        APowerLimitationControllableSystem  System,
                                        Boolean                             DUTIsSystem,
                                        String                              UseCase,
                                        PowerLimitationParameters           Sheet)
        {

            this.Tool         = Tool;
            this.time         = Time;
            this.Guard        = Guard;
            this.System       = System;
            this.DUTIsSystem  = DUTIsSystem;
            this.UseCase      = UseCase;
            this.Sheet        = Sheet;

        }

        #endregion


        #region (static) Create(Parameters, UseCase, DUTIsSystem, CancellationToken = default)

        /// <summary>
        /// Build both sides and register both actors, without exchanging
        /// anything yet.
        /// </summary>
        /// <param name="Parameters">What the device declared about itself.</param>
        /// <param name="UseCase">"LPC" or "LPP".</param>
        /// <param name="DUTIsSystem">Whether the device under test is the controllable system.</param>
        /// <param name="CancellationToken">An optional cancellation token.</param>
        public static async Task<PowerLimitationScenario> Create(ParameterSheet     Parameters,
                                                                 String             UseCase,
                                                                 Boolean            DUTIsSystem,
                                                                 CancellationToken  CancellationToken   = default)
        {

            var sheet       = Parameters.UseCases.Limitation(UseCase);
            var production  = UseCase == "LPP";

            var time        = new FakeTimeProvider(new DateTimeOffset(2026, 7, 26, 12, 0, 0, TimeSpan.Zero));

            var dut         = new SPINELocalDevice("d:_i:19667_DUT",
                                                   DUTIsSystem ? DeviceTypeType.ChargingStation : DeviceTypeType.EnergyManagementSystem,
                                                   TimeProvider: time);

            var tool        = new SPINELocalDevice("d:_i:19667_TestTool",
                                                   DUTIsSystem ? DeviceTypeType.EnergyManagementSystem : DeviceTypeType.ChargingStation,
                                                   TimeProvider: time);

            var wire        = new SPINETestTool(dut, tool, time) { AutoAnswer = true };

            var systemEntity  = (DUTIsSystem ? dut  : tool).AddEntity(production ? EntityTypeType.Inverter : EntityTypeType.EVSE);
            var guardEntity   = (DUTIsSystem ? tool : dut ).AddEntity(EntityTypeType.CEM);

            APowerLimitationControllableSystem system = production
                                                            ? new LPPControllableSystem(systemEntity, sheet.SystemIsEnergyManager)
                                                            : new LPCControllableSystem(systemEntity, sheet.SystemIsEnergyManager);

            APowerLimitationEnergyGuard        guard  = production
                                                            ? new LPPEnergyGuard(guardEntity)
                                                            : new LPCEnergyGuard (guardEntity);

            // CF_EG_ManualExecution, and only where the energy guard is the
            // tester rather than the device under test: "If an EG action is
            // required, it is described within the test steps" (Table 8). An
            // energy guard which introduces itself the moment it sees a
            // controllable system - which rule 913 requires of a real one - would
            // walk the state machine out of "init" before the first test step
            // ran. Where the energy guard *is* the device under test, it stays
            // autonomous, because that autonomy is what three of its cases check.
            if (DUTIsSystem)
                guard.AnnounceOnDiscovery = false;

            // What the manufacturer declares as pre-configured, which several
            // abstract test cases then read back after a factory reset.
            system.FailsafeLimit            = sheet.PreConfiguredFailsafe;
            system.FailsafeDurationMinimum  = sheet.PreConfiguredFailsafeDuration;
            system.ConsumptionNominalMax    = sheet.LimitMax;

            // A limit outside the declared range is one this device cannot apply
            // - which is what makes APCL_05 interesting and, where the device
            // says it alters such values instead, what makes it uninteresting.
            if (!sheet.SystemAltersTooLargeLimit)
                system.CanApplyLimit = value => value >= sheet.LimitMin && value <= sheet.LimitMax;

            await guard. Register(CancellationToken);
            await system.Register(CancellationToken);

            return new PowerLimitationScenario(wire, time, guard, system, DUTIsSystem, UseCase, sheet);

        }

        #endregion

        #region Connect(CancellationToken = default) / Disconnect() / Reconnect()

        /// <summary>
        /// Discover each other and put the bindings and subscriptions of the use
        /// case in place - but send nothing yet.
        ///
        /// This is where every test configuration begins. No heartbeat is
        /// started here on purpose: CF_EG_ManualExecution means "if an energy
        /// guard action is required, it is described within the test steps", and
        /// a timer quietly beating in the background would make three quarters of
        /// the state machine cases pass for the wrong reason.
        /// </summary>
        /// <param name="CancellationToken">An optional cancellation token.</param>
        public async Task Connect(CancellationToken CancellationToken = default)
        {

            await Tool.DUT. NodeManagement.RequestDetailedDiscovery(Tool.ToolAsSeenByDUT, CancellationToken);
            await Tool.Tool.NodeManagement.RequestDetailedDiscovery(Tool.DUTAsSeenByTool, CancellationToken);
            await Tool.DUT. NodeManagement.RequestUseCaseData      (Tool.ToolAsSeenByDUT, CancellationToken);
            await Tool.Tool.NodeManagement.RequestUseCaseData      (Tool.DUTAsSeenByTool, CancellationToken);

            var systemSide  = SystemAsSeenByGuard
                                  ?? throw new ConformanceInconclusive("The energy guard did not discover the controllable system.");

            var guardSide   = GuardAsSeenBySystem
                                  ?? throw new ConformanceInconclusive("The controllable system did not discover the energy guard.");

            // The controllable system watches the heartbeat of the energy guard;
            // losing it is what sends it into its failsafe state.
            await new UseCaseFeature(FeatureTypeType.DeviceDiagnosis, System.Entity, guardSide).Subscribe(CancellationToken);

            // ... and the energy guard watches the controllable system's, which
            // is what ATC_*_CSHeartbeat_001 is about.
            await new UseCaseFeature(FeatureTypeType.DeviceDiagnosis, Guard.Entity, systemSide).Subscribe(CancellationToken);

            var loadControl = Guard.LoadControlOf(systemSide);

            await loadControl.Subscribe(CancellationToken);
            await loadControl.Bind     (CancellationToken);

            await Guard.ConfigurationOf(systemSide).Bind(CancellationToken);

        }


        /// <summary>
        /// CF_EG_ConnectionLoss: the wire goes quiet in both directions.
        /// </summary>
        public void Disconnect()
        {
            Tool.AutoAnswer = false;
        }


        /// <summary>
        /// The wire comes back.
        /// </summary>
        public void Reconnect()
        {
            Tool.AutoAnswer = true;
        }

        #endregion

        #region Advance(By, CancellationToken = default)

        /// <summary>
        /// Let protocol time pass, and let the controllable system notice.
        ///
        /// The state machine has no timer of its own - a device which has gone
        /// quiet gives nothing to react to, so somebody has to look - and here
        /// that somebody is whoever drives the clock. Looking once per simulated
        /// second is what makes "changes its configuration within 120 seconds"
        /// something the test can measure rather than assume.
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

                await System.Check(CancellationToken);
                await Task.Yield();

            }

        }


        /// <summary>
        /// How long the controllable system takes to leave the state it is in,
        /// or null when it is still there when the time is up.
        /// </summary>
        /// <param name="Within">How long to watch.</param>
        /// <param name="CancellationToken">An optional cancellation token.</param>
        public async Task<TimeSpan?> WaitForStateChange(TimeSpan           Within,
                                                        CancellationToken  CancellationToken   = default)
        {

            var was      = State;
            var waited   = TimeSpan.Zero;
            var second   = TimeSpan.FromSeconds(1);

            while (waited < Within)
            {

                await Advance(second, CancellationToken);
                waited += second;

                if (State != was)
                    return waited;

            }

            return null;

        }

        #endregion

        #region Heartbeat(CancellationToken = default)

        /// <summary>
        /// One heartbeat of the energy guard, now.
        /// </summary>
        /// <param name="CancellationToken">An optional cancellation token.</param>
        public async Task Heartbeat(CancellationToken CancellationToken = default)
        {
            await Guard.Heartbeat.SendOnce(PowerLimitation.HeartbeatInterval, CancellationToken);
        }


        /// <summary>
        /// How many heartbeats the device under test has sent, and how far apart
        /// the widest gap between two of them was.
        /// </summary>
        public (Int32 Count, TimeSpan Widest) HeartbeatsFromDUT()
        {

            var beats   = Tool.FromDUT.
                              Where (datagram => datagram.Payload?.Cmd?.Any(cmd => cmd.DataFunction == PowerLimitation.HeartbeatData) == true).
                              ToList();

            var stamps  = beats.
                              Select(datagram => datagram.Payload!.Cmd!.
                                                     Select(cmd => cmd.GetData(PowerLimitation.HeartbeatData) as DeviceDiagnosisHeartbeatDataType).
                                                     FirstOrDefault(data => data is not null)?.
                                                     Timestamp?.AsDateTimeOffset).
                              Where (stamp => stamp is not null).
                              Select(stamp => stamp!.Value).
                              OrderBy(stamp => stamp).
                              ToList();

            var widest  = TimeSpan.Zero;

            for (var index = 1; index < stamps.Count; index++)
                if (stamps[index] - stamps[index - 1] > widest)
                    widest = stamps[index] - stamps[index - 1];

            return (stamps.Count, widest);

        }

        /// <summary>
        /// Watch for what rule 913 asks an energy guard to do when it finds a
        /// controllable system: one heartbeat, and then a limit.
        ///
        /// The order is checked rather than assumed, because the order is the
        /// rule. A limit which arrives before the heartbeat is a limit the
        /// controllable system is required to ignore, so an energy guard which
        /// sends both in the wrong order has sent one useful message and one
        /// wasted one.
        /// </summary>
        /// <param name="Within">How long to wait.</param>
        /// <param name="Since">How many datagrams the device under test had already sent when the waiting began - see <see cref="Observed"/>.</param>
        /// <param name="CancellationToken">An optional cancellation token.</param>
        public async Task<(Boolean Heartbeat, Boolean LimitAfterIt, TimeSpan Waited)> WaitForAnnouncement(TimeSpan           Within,
                                                                                                          Int32              Since,
                                                                                                          CancellationToken  CancellationToken   = default)
        {

            var from    = DUTIsSystem ? Tool.FromTool : Tool.FromDUT;
            var already = Since;
            var waited  = TimeSpan.Zero;
            var second  = TimeSpan.FromSeconds(1);

            while (waited <= Within)
            {

                var beat  = -1;
                var limit = -1;

                for (var index = already; index < from.Count; index++)
                {

                    var functions = from[index].Payload?.Cmd?.Select(cmd => cmd.DataFunction).ToList() ?? [];

                    if (beat < 0 && functions.Contains(PowerLimitation.HeartbeatData))
                        beat = index;

                    if (beat >= 0 && limit < 0 && index > beat && functions.Contains(PowerLimitation.LimitListData))
                        limit = index;

                }

                if (beat >= 0 && limit >= 0)
                    return (true, true, waited);

                if (waited == Within)
                    return (beat >= 0, false, waited);

                await Advance(second, CancellationToken);
                waited += second;

            }

            return (false, false, waited);

        }

        #endregion

        #region WriteLimit(...) / WriteFailsafe(...)

        /// <summary>
        /// Write the limit as one of the seventeen message combinations.
        ///
        /// Our own energy guard refuses to send a magnitude below zero, which is
        /// correct of an energy guard and useless here: half the negative cases
        /// exist to watch what the *controllable system* does with exactly that.
        /// So the write goes out through the same feature but without the check,
        /// which is what a test tool is for.
        /// </summary>
        /// <param name="Message">Which facets of the limit the write carries.</param>
        /// <param name="Value">The limit, as a magnitude in watts.</param>
        /// <param name="Duration">How long it is valid for, where the combination carries one.</param>
        /// <param name="CancellationToken">An optional cancellation token.</param>
        /// <returns>Null when the write was accepted, the error otherwise.</returns>
        public async Task<ResultDataType?> WriteLimit(LimitMessages      Message,
                                                      Decimal?           Value               = null,
                                                      TimeSpan?          Duration            = null,
                                                      CancellationToken  CancellationToken   = default)
        {

            var systemSide  = SystemAsSeenByGuard
                                  ?? throw new ConformanceInconclusive("The energy guard does not know the controllable system.");

            var loadControl = Guard.LoadControlOf(systemSide);

            // Which of the controllable system's limits is the one of this use
            // case is a question only the description answers, and an energy
            // guard which has not read it has no business writing anything.
            if (loadControl.Data<LoadControlLimitDescriptionListDataType>(PowerLimitation.LimitDescriptionListData) is null)
                await loadControl.RequestData(PowerLimitation.LimitDescriptionListData, CancellationToken: CancellationToken);

            var limitId     = loadControl.
                                  Data<LoadControlLimitDescriptionListDataType>(PowerLimitation.LimitDescriptionListData)?.
                                  LoadControlLimitDescriptionData?.
                                  FirstOrDefault(description => System.Profile.IsTheLimit(description))?.LimitId
                                      ?? throw new ConformanceInconclusive("The controllable system offers no limit of this use case.");

            var entry       = new LoadControlLimitDataType { LimitId = limitId };

            if (Carries(Message, "value"))
                entry.Value = ScaledNumberType.FromValue(Value ?? 0);

            if (Carries(Message, "activated"))
                entry.IsLimitActive = true;

            if (Carries(Message, "deactivated"))
                entry.IsLimitActive = false;

            if (Carries(Message, "duration"))
                entry.TimePeriod = TimePeriodType.FromDuration(Duration ?? TimeSpan.FromSeconds(60));

            // "Delete the duration" is a partial write with the element present
            // and empty, which is how SPINE says "remove this" as opposed to
            // "leave it alone" (SPINE 1.3.0, 5.3.3).
            if (Carries(Message, "delete"))
                entry.TimePeriod = new TimePeriodType();

            var response = await loadControl.WriteData(
                                     PowerLimitation.LimitListData,
                                     new LoadControlLimitListDataType {
                                         LoadControlLimitData = [ entry ]
                                     },
                                     Partial: true,
                                     CancellationToken: CancellationToken
                                 );

            return response.Result is not null && response.Result.ErrorNumber != 0
                       ? response.Result
                       : null;

        }


        /// <summary>
        /// Write the failsafe limit, the failsafe duration minimum, or both.
        ///
        /// Again without the energy guard's own bounds check: ATC_*_CSConnection_005
        /// and _008 exist to send a duration outside two to 24 hours and watch
        /// what comes back.
        /// </summary>
        /// <param name="Limit">The failsafe limit in watts, if any.</param>
        /// <param name="DurationMinimum">The failsafe duration minimum, if any.</param>
        /// <param name="CancellationToken">An optional cancellation token.</param>
        /// <returns>Null when the write was accepted, the error otherwise.</returns>
        public async Task<ResultDataType?> WriteFailsafe(Decimal?           Limit               = null,
                                                         TimeSpan?          DurationMinimum     = null,
                                                         CancellationToken  CancellationToken   = default)
        {

            var systemSide     = SystemAsSeenByGuard
                                     ?? throw new ConformanceInconclusive("The energy guard does not know the controllable system.");

            var configuration  = Guard.ConfigurationOf(systemSide);

            await configuration.RequestData(PowerLimitation.KeyValueDescriptionListData, CancellationToken: CancellationToken);

            var descriptions   = configuration.
                                     Data<DeviceConfigurationKeyValueDescriptionListDataType>(PowerLimitation.KeyValueDescriptionListData)?.
                                     DeviceConfigurationKeyValueDescriptionData ?? [];

            var entries        = new List<DeviceConfigurationKeyValueDataType>();

            if (Limit is not null &&
                descriptions.FirstOrDefault(description => description.KeyName == System.Profile.FailsafeLimitKey)?.KeyId is UInt32 limitKey)
                entries.Add(new DeviceConfigurationKeyValueDataType {
                                KeyId  = limitKey,
                                Value  = new DeviceConfigurationKeyValueValueType {
                                             ScaledNumber = ScaledNumberType.FromValue(Limit.Value)
                                         }
                            });

            if (DurationMinimum is not null &&
                descriptions.FirstOrDefault(description => description.KeyName == PowerLimitation.FailsafeDurationKey)?.KeyId is UInt32 durationKey)
                entries.Add(new DeviceConfigurationKeyValueDataType {
                                KeyId  = durationKey,
                                Value  = new DeviceConfigurationKeyValueValueType {
                                             Duration = DurationType.Parse(DurationMinimum.Value)
                                         }
                            });

            if (entries.Count == 0)
                throw new ConformanceInconclusive("The controllable system offers neither failsafe value.");

            var response = await configuration.WriteData(
                                     PowerLimitation.KeyValueListData,
                                     new DeviceConfigurationKeyValueListDataType {
                                         DeviceConfigurationKeyValueData = entries
                                     },
                                     Partial: true,
                                     CancellationToken: CancellationToken
                                 );

            return response.Result is not null && response.Result.ErrorNumber != 0
                       ? response.Result
                       : null;

        }

        #endregion

        #region ReadLimit(...) / ReadFailsafe(...) / ReadNominalMax(...)

        /// <summary>
        /// The limit as it currently stands at the controllable system.
        /// </summary>
        /// <param name="CancellationToken">An optional cancellation token.</param>
        public async Task<(Decimal? Value, Boolean IsActive)> ReadLimit(CancellationToken CancellationToken = default)
        {

            var systemSide = SystemAsSeenByGuard
                                 ?? throw new ConformanceInconclusive("The energy guard does not know the controllable system.");

            return await Guard.ReadConsumptionLimit(systemSide, CancellationToken);

        }


        /// <summary>
        /// The failsafe values as they currently stand there.
        /// </summary>
        /// <param name="CancellationToken">An optional cancellation token.</param>
        public async Task<(Decimal? Limit, TimeSpan? DurationMinimum)> ReadFailsafe(CancellationToken CancellationToken = default)
        {

            var systemSide = SystemAsSeenByGuard
                                 ?? throw new ConformanceInconclusive("The energy guard does not know the controllable system.");

            return await Guard.ReadFailsafeValues(systemSide, CancellationToken);

        }


        /// <summary>
        /// Which nominal maximum the controllable system reports, and its value.
        ///
        /// Two of them exist and exactly one may be there: a device on an energy
        /// manager reports what it is contractually allowed to draw, one which is
        /// not reports what it is physically able to draw, and reporting the
        /// wrong one is a failure rather than a detail (rules 039 and 040).
        /// </summary>
        /// <param name="CancellationToken">An optional cancellation token.</param>
        public async Task<(Boolean Contractual, Boolean Physical, Decimal? Value)> ReadNominalMax(CancellationToken CancellationToken = default)
        {

            var systemSide  = SystemAsSeenByGuard
                                  ?? throw new ConformanceInconclusive("The energy guard does not know the controllable system.");

            var electrical  = Guard.ElectricalOf(systemSide);

            await electrical.RequestData(PowerLimitation.CharacteristicListData, CancellationToken: CancellationToken);

            var characteristics = electrical.
                                      Data<ElectricalConnectionCharacteristicListDataType>(PowerLimitation.CharacteristicListData)?.
                                      ElectricalConnectionCharacteristicData ?? [];

            var contractual  = characteristics.FirstOrDefault(characteristic => characteristic.CharacteristicType == System.Profile.ContractualNominalMax);
            var physical     = characteristics.FirstOrDefault(characteristic => characteristic.CharacteristicType == System.Profile.NominalMax);

            return (contractual is not null,
                    physical    is not null,
                    (contractual ?? physical)?.Value?.Value);

        }

        #endregion

        #region Restart(FactoryReset = false, CancellationToken = default)

        /// <summary>
        /// CF_CS_Reset_Init respectively a plain reboot of the controllable
        /// system.
        ///
        /// A reboot puts the state machine back into "init" and leaves the stored
        /// values alone; a factory reset throws them away as well, which is what
        /// tells ATC_*_CSInit_002 (defaults after a reset) from ATC_*_CSInit_003
        /// (the written values survive a reboot).
        /// </summary>
        /// <param name="FactoryReset">Whether the stored values are thrown away too.</param>
        /// <param name="CancellationToken">An optional cancellation token.</param>
        public async Task Restart(Boolean            FactoryReset        = false,
                                  CancellationToken  CancellationToken   = default)
        {

            System.StateMachine.Restart();

            if (FactoryReset)
            {
                System.FailsafeLimit            = Sheet.PreConfiguredFailsafe;
                System.FailsafeDurationMinimum  = Sheet.PreConfiguredFailsafeDuration;
            }

            await System.Check(CancellationToken);

        }

        #endregion


        #region (private static) Carries(Message, Facet)

        /// <summary>
        /// Whether one of the seventeen message combinations carries a facet.
        /// </summary>
        private static Boolean Carries(LimitMessages  Message,
                                       String         Facet)

            => Facet switch {

                   "value"        => Message is LimitMessages.Value
                                             or LimitMessages.ValueActivated
                                             or LimitMessages.ValueDeactivated
                                             or LimitMessages.ValueDeleteDuration
                                             or LimitMessages.ValueDuration
                                             or LimitMessages.ValueActivatedDeleteDuration
                                             or LimitMessages.ValueActivatedDuration
                                             or LimitMessages.ValueDeactivatedDeleteDuration
                                             or LimitMessages.ValueDeactivatedDuration,

                   "activated"    => Message is LimitMessages.Activated
                                             or LimitMessages.ValueActivated
                                             or LimitMessages.ActivatedDeleteDuration
                                             or LimitMessages.ActivatedDuration
                                             or LimitMessages.ValueActivatedDeleteDuration
                                             or LimitMessages.ValueActivatedDuration,

                   "deactivated"  => Message is LimitMessages.Deactivated
                                             or LimitMessages.ValueDeactivated
                                             or LimitMessages.DeactivatedDeleteDuration
                                             or LimitMessages.DeactivatedDuration
                                             or LimitMessages.ValueDeactivatedDeleteDuration
                                             or LimitMessages.ValueDeactivatedDuration,

                   "duration"     => Message is LimitMessages.Duration
                                             or LimitMessages.ValueDuration
                                             or LimitMessages.ActivatedDuration
                                             or LimitMessages.DeactivatedDuration
                                             or LimitMessages.ValueActivatedDuration
                                             or LimitMessages.ValueDeactivatedDuration,

                   "delete"       => Message is LimitMessages.DeleteDuration
                                             or LimitMessages.ValueDeleteDuration
                                             or LimitMessages.ActivatedDeleteDuration
                                             or LimitMessages.DeactivatedDeleteDuration
                                             or LimitMessages.ValueActivatedDeleteDuration
                                             or LimitMessages.ValueDeactivatedDeleteDuration,

                   _              => false

               };

        #endregion

    }

}
