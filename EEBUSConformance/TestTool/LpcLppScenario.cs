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

    /// <summary>
    /// PRE_SPINE_Scenario_LpcLpp_S1_S3: a device and a test tool which have
    /// actually got a power limitation going between them.
    ///
    /// A third of the SPINE catalog needs this, and that is not an accident.
    /// Most of what the protocol specification demands only becomes observable
    /// inside a working conversation: which client a server subscribes back to,
    /// whether an unknown element is survivable, what a partial write with a
    /// missing scale does to a stored value. None of it can be seen on an empty
    /// connection, so the test specification borrows the simplest real use case
    /// there is and checks the protocol rules inside it.
    ///
    /// Which of the two - LPC or LPP - is chosen by PAR_limitationUseCase; they
    /// are the same conversation pointed in opposite directions, and this stack
    /// shares one implementation between them (ADR 0006), so both are reached
    /// by the same code here.
    /// </summary>
    public sealed class LpcLppScenario
    {

        #region Properties

        /// <summary>The two wired devices.</summary>
        public SPINETestTool                       Tool         { get; }

        /// <summary>The energy guard, wherever it lives.</summary>
        public APowerLimitationEnergyGuard         Guard        { get; }

        /// <summary>The controllable system, wherever it lives.</summary>
        public APowerLimitationControllableSystem  System       { get; }

        /// <summary>Whether the device under test is the controllable system.</summary>
        public Boolean                             DUTIsSystem  { get; }

        /// <summary>Which of the two use cases this is.</summary>
        public String                              UseCase      { get; }


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

        private LpcLppScenario(SPINETestTool                       Tool,
                               APowerLimitationEnergyGuard         Guard,
                               APowerLimitationControllableSystem  System,
                               Boolean                             DUTIsSystem,
                               String                              UseCase)
        {

            this.Tool         = Tool;
            this.Guard        = Guard;
            this.System       = System;
            this.DUTIsSystem  = DUTIsSystem;
            this.UseCase      = UseCase;

        }

        #endregion


        #region (static) Create(Parameters, DUTIsSystem, Configure = null, CancellationToken = default)

        /// <summary>
        /// Build the two sides and register both actors.
        /// </summary>
        /// <param name="Parameters">What the device declared about itself.</param>
        /// <param name="DUTIsSystem">Whether the device under test is the controllable system.</param>
        /// <param name="Configure">What to do to the test tool before anything is exchanged - the PRE_TestTool_*_Configured preconditions.</param>
        /// <param name="CancellationToken">An optional cancellation token.</param>
        public static async Task<LpcLppScenario> Create(ParameterSheet         Parameters,
                                                        Boolean                DUTIsSystem,
                                                        Action<SPINETestTool>? Configure           = null,
                                                        CancellationToken      CancellationToken   = default)
        {

            var time    = new FakeTimeProvider(new DateTimeOffset(2026, 7, 26, 12, 0, 0, TimeSpan.Zero));

            var dut     = new SPINELocalDevice("d:_i:19667_DUT",
                                               DUTIsSystem ? DeviceTypeType.ChargingStation : DeviceTypeType.EnergyManagementSystem,
                                               TimeProvider: time);

            var tool    = new SPINELocalDevice("d:_i:19667_TestTool",
                                               DUTIsSystem ? DeviceTypeType.EnergyManagementSystem : DeviceTypeType.ChargingStation,
                                               TimeProvider: time);

            var wire    = new SPINETestTool(dut, tool, time) { AutoAnswer = true };

            var lpp     = Parameters.LimitationUseCase == "LPP";

            var systemEntity  = (DUTIsSystem ? dut  : tool).AddEntity(lpp ? EntityTypeType.Inverter : EntityTypeType.EVSE);
            var guardEntity   = (DUTIsSystem ? tool : dut ).AddEntity(EntityTypeType.CEM);

            APowerLimitationControllableSystem system = lpp
                                                            ? new LPPControllableSystem(systemEntity)
                                                            : new LPCControllableSystem(systemEntity);

            APowerLimitationEnergyGuard        guard  = lpp
                                                            ? new LPPEnergyGuard(guardEntity)
                                                            : new LPCEnergyGuard (guardEntity);

            // The mandatory data of the use case; without it the controllable
            // system cannot play scenario 2 and the commissioning is not what
            // the precondition describes.
            system.FailsafeLimit            = 4200;
            system.FailsafeDurationMinimum  = TimeSpan.FromHours(2);

            // One property for both directions: which way it points is the
            // profile's business, not the caller's (ADR 0006).
            system.ConsumptionNominalMax = 11000;

            Configure?.Invoke(wire);

            await guard. Register(CancellationToken);
            await system.Register(CancellationToken);

            return new LpcLppScenario(wire, guard, system, DUTIsSystem,
                                      lpp ? "limitationOfPowerProduction" : "limitationOfPowerConsumption");

        }

        #endregion

        #region Commission(CancellationToken = default)

        /// <summary>
        /// Reach the state the precondition describes: both sides discovered,
        /// the bindings and subscriptions of the use case in place, and at least
        /// one heartbeat and one limit written and acknowledged.
        /// </summary>
        /// <param name="CancellationToken">An optional cancellation token.</param>
        public async Task Commission(CancellationToken CancellationToken = default)
        {

            await Tool.DUT. NodeManagement.RequestDetailedDiscovery(Tool.ToolAsSeenByDUT, CancellationToken);
            await Tool.Tool.NodeManagement.RequestDetailedDiscovery(Tool.DUTAsSeenByTool, CancellationToken);
            await Tool.DUT. NodeManagement.RequestUseCaseData      (Tool.ToolAsSeenByDUT, CancellationToken);
            await Tool.Tool.NodeManagement.RequestUseCaseData      (Tool.DUTAsSeenByTool, CancellationToken);

            var systemSide  = SystemAsSeenByGuard
                                  ?? throw new ConformanceInconclusive("The energy guard did not discover the controllable system.");

            var guardSide   = GuardAsSeenBySystem
                                  ?? throw new ConformanceInconclusive("The controllable system did not discover the energy guard.");

            // The controllable system watches the heartbeat of the energy
            // guard; losing it is what sends it into its failsafe state.
            await new UseCaseFeature(FeatureTypeType.DeviceDiagnosis, System.Entity, guardSide).Subscribe(CancellationToken);

            var loadControl = Guard.LoadControlOf(systemSide);

            await loadControl.Subscribe(CancellationToken);
            await loadControl.Bind     (CancellationToken);

            await Guard.ConfigurationOf(systemSide).Bind(CancellationToken);

            await Guard.StartHeartbeat(CancellationToken: CancellationToken);

        }

        #endregion

        #region WriteLimitRegardless(Value, CancellationToken = default)

        /// <summary>
        /// PAR_writeLimitVal, sent whatever this side thinks of it.
        ///
        /// The energy guard of this stack refuses to write where it holds no
        /// binding, which is correct of a client and useless in a test tool: the
        /// whole point of TC_SPINE_BIND_002 is to watch what the *server* does
        /// with a write it should not have received. So the datagram is built by
        /// hand and handed to the device directly.
        /// </summary>
        /// <param name="Value">The limit in watts.</param>
        /// <param name="CancellationToken">An optional cancellation token.</param>
        public async Task<ResultDataType?> WriteLimitRegardless(Decimal            Value,
                                                                CancellationToken  CancellationToken   = default)
        {

            var limitId  = System.LoadControl.
                               DataCopy<LoadControlLimitListDataType>("loadControlLimitListData")?.
                               LoadControlLimitData?.FirstOrDefault()?.LimitId
                                   ?? throw new ConformanceInconclusive("the controllable system holds no limit to write to");

            var client   = Guard.Entity.Features.FirstOrDefault(feature => feature.Role == RoleType.Client)
                               ?? throw new ConformanceInconclusive("the energy guard has no client feature");

            var cmd      = new CmdType();

            cmd.SetData("loadControlLimitListData",
                        new LoadControlLimitListDataType {
                            LoadControlLimitData = [
                                new LoadControlLimitDataType {
                                    LimitId        = limitId,
                                    IsLimitActive  = true,
                                    Value          = ScaledNumberType.FromValue(Value)
                                }
                            ]
                        });

            cmd.Function  = FunctionType.Parse("loadControlLimitListData");
            cmd.Filter    = [ new FilterType { CmdControl = CmdControlType.ForPartial } ];

            var counter   = Tool.NextMsgCounter();

            await Tool.Send(
                      SPINEParameters.Datagram(
                          new FeatureAddressType { Device = Tool.Tool.DeviceAddress, Entity = Guard. Entity.EntityId.ToList(), Feature = client.Id },
                          new FeatureAddressType { Device = Tool.DUT. DeviceAddress, Entity = System.Entity.EntityId.ToList(), Feature = System.LoadControl.Id },
                          CmdClassifierType.Write,
                          counter,
                          cmd,
                          AckRequest: true
                      ),
                      CancellationToken
                  );

            return Tool.ResultFor(counter);

        }

        #endregion

        #region SendHeartbeat(CancellationToken = default)

        /// <summary>
        /// What PAR_notifyAck is: one heartbeat from the energy guard, arriving
        /// at the controllable system.
        ///
        /// The heartbeat is a timer rather than a method, so this lets the
        /// interval pass and the timer do it. A limit write which arrives long
        /// after the last heartbeat is refused for that reason alone, so the
        /// cases which write have to keep the heartbeat fresh or they would
        /// pass while proving nothing.
        /// </summary>
        /// <param name="CancellationToken">An optional cancellation token.</param>
        public async Task SendHeartbeat(CancellationToken CancellationToken = default)
        {
            await Tool.Advance(Guard.Heartbeat.Interval ?? TimeSpan.FromSeconds(60), CancellationToken);
        }

        #endregion

        #region WriteLimit(Value, IsActive = true, CancellationToken = default)

        /// <summary>
        /// What scenario 1 is: the energy guard writes a limit.
        /// </summary>
        /// <param name="Value">The limit in watts.</param>
        /// <param name="IsActive">Whether it is activated.</param>
        /// <param name="CancellationToken">An optional cancellation token.</param>
        public async Task<SPINEResponse> WriteLimit(Decimal            Value,
                                                    Boolean            IsActive            = true,
                                                    CancellationToken  CancellationToken   = default)
        {

            var systemSide = SystemAsSeenByGuard
                                 ?? throw new ConformanceInconclusive("The energy guard does not know the controllable system.");

            return await Guard.WriteConsumptionLimit(systemSide,
                                                     Value,
                                                     IsActive,
                                                     CancellationToken: CancellationToken);

        }

        #endregion

    }

}
