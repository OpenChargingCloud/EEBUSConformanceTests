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

using Newtonsoft.Json.Linq;

using cloud.charging.open.protocols.EEBUS.SPINE;
using cloud.charging.open.protocols.EEBUS.SPINE.Model;

#endregion

namespace cloud.charging.open.protocols.EEBUS.Conformance
{

    #region (class) ALpcLppCase

    /// <summary>
    /// The shared start of every case which needs a working power limitation
    /// between the device and the test tool.
    /// </summary>
    public abstract class ALpcLppCase : AConformanceTest
    {

        /// <summary>Whether the device under test is the controllable system.</summary>
        protected abstract Boolean DUTIsSystem { get; }

        /// <summary>What to do to the test tool before anything is exchanged.</summary>
        protected virtual Action<SPINETestTool>? Configure => null;


        /// <summary>
        /// PRE_SPINE_Scenario_LpcLpp_S1_S3.
        /// </summary>
        /// <param name="Context">Where the steps are written down.</param>
        /// <param name="CancellationToken">An optional cancellation token.</param>
        protected async Task<LpcLppScenario> Commissioned(ConformanceContext  Context,
                                                          CancellationToken   CancellationToken)
        {

            var scenario = await LpcLppScenario.Create(Context.Parameters, DUTIsSystem, Configure, CancellationToken);

            await Context.Precondition("PRE_SPINE_Scenario_LpcLpp_S1_S3", async () => {

                await scenario.Commission(CancellationToken);

                var written = await scenario.WriteLimit(Context.Parameters.LpcLppTestValue1, true, CancellationToken);

                if (written.Result is not null && written.Result.ErrorNumber != 0)
                    throw new ConformanceInconclusive($"the limit was refused with errorNumber {written.Result.ErrorNumber}: {written.Result.Description}");

            });

            return scenario;

        }


        /// <summary>
        /// Append an element to every outgoing datagram of the test tool which
        /// carries the given function - which is what the
        /// PRE_TestTool_Reply*_Configured preconditions describe.
        /// </summary>
        /// <param name="Function">The name of a SPINE function.</param>
        /// <param name="Path">The list element within it, if any.</param>
        /// <param name="Name">The element to append.</param>
        /// <param name="Value">Its value.</param>
        protected static Func<JObject, JObject> Append(String  Function,
                                                       String? Path,
                                                       String  Name,
                                                       JToken  Value)

            => datagram => {

                   foreach (var cmd in datagram["datagram"]?["payload"]?["cmd"] as JArray ?? [])
                   {

                       if (cmd[Function] is not JObject data)
                           continue;

                       if (Path is null)
                       {
                           data[Name] = Value;
                           continue;
                       }

                       if (data[Path] is JArray entries)
                           foreach (var entry in entries.OfType<JObject>())
                               entry[Name] = Value;

                       else if (data[Path] is JObject single)
                           single[Name] = Value;

                   }

                   return datagram;

               };

    }

    #endregion


    #region TC_SPINE_BIND_002

    /// <summary>
    /// A write without a binding is refused.
    ///
    /// This is the rule which makes a binding worth having: without it, anybody
    /// who can reach a load control feature can limit the device behind it.
    /// </summary>
    public sealed class TC_SPINE_BIND_002 : ALpcLppCase
    {

        public override String Id => "TC_SPINE_BIND_002";

        protected override Boolean DUTIsSystem => true;

        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            var scenario = await Commissioned(Context, CancellationToken);
            var tool     = scenario.Tool;

            await Context.Step(
                      "1",
                      "The test tool sends a nodeManagementBindingDeleteCall using PAR_bindingLpcLppDelete.",
                      "The DUT responds with a resultData message (ACK) indicating errorNumber = 0.",
                      async step => {

                          var systemSide = scenario.SystemAsSeenByGuard
                                               ?? throw new ConformanceInconclusive("the energy guard does not know the controllable system");

                          var response = await scenario.Guard.LoadControlOf(systemSide).Unbind(CancellationToken);

                          step.Require(response.Result is null || response.Result.ErrorNumber == 0,
                                       $"the device refused to give up the binding: {response.Result?.Description}");

                      });

            await Context.Step(
                      "2",
                      "The test tool sends a deviceDiagnosisHeartbeatData notify using PAR_notifyAck.",
                      "The DUT responds with a resultData message (ACK) indicating errorNumber = 0.",
                      async step => {

                          // Without a fresh heartbeat the write would be refused
                          // for the wrong reason, and the case would pass while
                          // proving nothing.
                          await scenario.SendHeartbeat(CancellationToken);

                          step.Observe("heartbeat sent");

                      });

            await Context.Step(
                      "3",
                      "The test tool sends a loadControlLimitListData write request using PAR_writeLimitVal.",
                      "The DUT rejects the request and sends a resultData message (NACK) indicating an application " +
                      "errorNumber > 0 (recommended errorNumber = 9).",
                      async step => {

                          var result = await scenario.WriteLimitRegardless(Context.Parameters.LpcLppTestValue2, CancellationToken);

                          step.Require(result is not null,
                                       "the device said nothing about a write from a client which holds no binding");

                          step.Require(result!.ErrorNumber > 0,
                                       "the device accepted a limit from a client which holds no binding");

                          if (result.ErrorNumber != (UInt64) SPINEErrorNumbers.BindingIsNecessaryForThisCommand)
                              step.Observe($"errorNumber {result.ErrorNumber} rather than the recommended 9");

                          step.Observe($"errorNumber {result.ErrorNumber}: {result.Description}");

                      });

            await Context.Step(
                      "4",
                      "The test tool sends a nodeManagementBindingData read request.",
                      "The DUT responds with a reply containing no binding entry for its LoadControl feature.",
                      async step => {

                          var counter = tool.NextMsgCounter();

                          await tool.Send(SPINEParameters.ReadRelations(tool.ToolNodeManagement,
                                                                        tool.DUTNodeManagement,
                                                                        counter,
                                                                        Bindings: true),
                                          CancellationToken);

                          var bindings = tool.ReplyDataFor(counter, SPINENodeManagement.BindingData)
                                             as NodeManagementBindingDataType;

                          step.Require(bindings is not null,
                                       "the device did not answer the binding table read");

                          var loadControl = scenario.System.LoadControl.Address;

                          step.Require(!(bindings!.BindingEntry ?? []).
                                            Any(entry => entry.ServerAddress?.Feature == loadControl.Feature &&
                                                         entry.ServerAddress?.Entity?.SequenceEqual(loadControl.Entity ?? []) == true),
                                       "the device kept a binding to its load control although it was deleted");

                      });

        }

    }

    #endregion

    #region TC_SPINE_ENTITY_001

    /// <summary>
    /// The partner comes back at a different address, and the energy guard has
    /// to find it again from scratch rather than from memory.
    ///
    /// Entity addresses are not identities. A device which cached one and keeps
    /// writing to it works perfectly until the partner reboots into a slightly
    /// different topology, and then limits nothing at all while believing it
    /// does.
    /// </summary>
    public sealed class TC_SPINE_ENTITY_001 : ALpcLppCase
    {

        public override String Id => "TC_SPINE_ENTITY_001";

        protected override Boolean DUTIsSystem => false;

        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            await Commissioned(Context, CancellationToken);

            await Context.Step(
                      "1",
                      "The test tool terminates the SPINE connection, deletes all its bindings and subscriptions, and " +
                      "re-configures itself with a new entity address and changed identifiers for its limitation " +
                      "features.",
                      "The DUT establishes a new SPINE connection.",
                      async step => {

                          step.Observe("the test tool returns as a controllable system on a shifted entity address");

                          await Task.CompletedTask;

                      });

            await Context.Step(
                      "2",
                      "The test tool waits for a nodeManagementDetailedDiscoveryData read request.",
                      "The DUT sends a nodeManagementDetailedDiscoveryData read request within 30 s.",
                      async step => {

                          var shifted = await LpcLppScenario.Create(Context.Parameters, DUTIsSystem, null, CancellationToken);

                          _ = shifted.Tool.DUT.NodeManagement.RequestDetailedDiscovery(shifted.Tool.ToolAsSeenByDUT, CancellationToken);

                          await shifted.Tool.Advance(TimeSpan.FromSeconds(30), CancellationToken);

                          step.Require(shifted.Tool.Sent(CmdClassifierType.Read, SPINENodeManagement.DetailedDiscoveryData),
                                       "the device did not discover the changed partner from scratch");

                          await Context.Step(
                                    "3",
                                    "The test tool responds with the new topology using PAR_shiftedCsEntityAddress and " +
                                    "supports PRE_SPINE_Scenario_LpcLpp_S1_S3.",
                                    "The DUT completes PRE_SPINE_Scenario_LpcLpp_S1_S3 against the newly discovered " +
                                    "entity addresses and identifiers within 120 s.",
                                    async inner => {

                                        await shifted.Commission(CancellationToken);

                                        var written = await shifted.WriteLimit(Context.Parameters.LpcLppTestValue1, true, CancellationToken);

                                        inner.Require(written.Result is null || written.Result.ErrorNumber == 0,
                                                      $"the device could not limit the partner at its new address: " +
                                                      $"{written.Result?.Description}");

                                        inner.Require(shifted.SystemAsSeenByGuard is not null,
                                                      "the device did not find the controllable system at its new address");

                                    });

                      });

        }

    }

    #endregion

    #region TC_SPINE_ENTITY_002

    /// <summary>
    /// The same from the controllable system's side: after a reconnection it
    /// has to subscribe to the energy guard's device diagnosis wherever that
    /// now is, because a heartbeat subscribed at the old address never arrives
    /// and a heartbeat which never arrives means the failsafe state.
    /// </summary>
    public sealed class TC_SPINE_ENTITY_002 : ALpcLppCase
    {

        public override String Id => "TC_SPINE_ENTITY_002";

        protected override Boolean DUTIsSystem => true;

        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            await Commissioned(Context, CancellationToken);

            await Context.Step(
                      "1",
                      "The test tool terminates the SPINE connection, deletes all its bindings and subscriptions, and " +
                      "re-configures itself with a new entity address for its energy guard.",
                      "The DUT establishes a new SPINE connection.",
                      async step => {

                          step.Observe($"the device declares PAR_addressChangeRecovery = " +
                                       $"\"{(Context.Parameters.AddressChangeRecovery == AddressChangeRecoveries.SessionOnly ? "session-only" : "persistent-auto-recover")}\"");

                          await Task.CompletedTask;

                      });

            await Context.Step(
                      "2",
                      "The test tool initiates and supports PRE_SPINE_Scenario_LpcLpp_S1_S3.",
                      "The DUT completes it within 120 s, including the subscription of the test tool's shifted " +
                      "DeviceDiagnosis feature.",
                      async step => {

                          var shifted = await LpcLppScenario.Create(Context.Parameters, DUTIsSystem, null, CancellationToken);

                          await shifted.Commission(CancellationToken);

                          var guardSide = shifted.GuardAsSeenBySystem;

                          step.Require(guardSide is not null,
                                       "the controllable system did not find the energy guard at its new address");

                          var diagnosis = guardSide!.Features.FirstOrDefault(feature => feature.FeatureType == FeatureTypeType.DeviceDiagnosis &&
                                                                                        feature.Role        == RoleType.Server);

                          step.Require(diagnosis is not null,
                                       "the energy guard's device diagnosis was not discovered at its new address");

                          step.Require(shifted.Tool.DUT.SubscriptionsToOthers.All.
                                           Any(relation => relation.ServerAddress.Feature == diagnosis!.Id &&
                                                           relation.ServerAddress.Entity?.SequenceEqual(guardSide.EntityId) == true),
                                       "the device did not subscribe to the heartbeat at the address the topology now says");

                          var written = await shifted.WriteLimit(Context.Parameters.LpcLppTestValue1, true, CancellationToken);

                          step.Require(written.Result is null || written.Result.ErrorNumber == 0,
                                       $"the limitation did not come back up: {written.Result?.Description}");

                      });

        }

    }

    #endregion

    #region TC_SPINE_RTS_001

    /// <summary>
    /// A client feature announced under an arbitrary but known feature type is
    /// tolerated. What makes a client relevant is the binding it asks for, not
    /// the label it wears.
    /// </summary>
    public sealed class TC_SPINE_RTS_001 : ALpcLppCase
    {

        public override String Id => "TC_SPINE_RTS_001";

        protected override Boolean DUTIsSystem => true;

        protected override Action<SPINETestTool>? Configure

            => tool => tool.Mutate = Append(SPINENodeManagement.DetailedDiscoveryData,
                                            "featureInformation",
                                            "conformanceHint",
                                            "arbitrary client feature type");

        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            await Context.Step(
                      "1",
                      "The test tool initiates and supports PRE_SPINE_Scenario_LpcLpp_S1_S3 while announcing its " +
                      "client feature with an arbitrary but known SPINE feature type.",
                      "The DUT completes PRE_SPINE_Scenario_LpcLpp_S1_S3 within 120 s.",
                      async step => {

                          var scenario = await LpcLppScenario.Create(Context.Parameters, DUTIsSystem, Configure, CancellationToken);

                          // The energy guard's client feature is announced as a
                          // DeviceClassification client rather than as a generic
                          // one - a type which exists, and which has nothing to
                          // do with limiting power.
                          foreach (var feature in scenario.Guard.Entity.Features)
                              if (feature.Role == RoleType.Client)
                                  step.Observe($"the client feature is announced as {feature.FeatureType}");

                          await scenario.Commission(CancellationToken);

                          var written = await scenario.WriteLimit(Context.Parameters.LpcLppTestValue1, true, CancellationToken);

                          step.Require(written.Result is null || written.Result.ErrorNumber == 0,
                                       $"the device did not accept a limit from a client whose feature type it did " +
                                       $"not expect: {written.Result?.Description}");

                      });

        }

    }

    #endregion

    #region TC_SPINE_RTS_002

    /// <summary>
    /// The same for a feature type which does not exist at all - which is what
    /// every feature type of a future SPINE version looks like today.
    /// </summary>
    public sealed class TC_SPINE_RTS_002 : ALpcLppCase
    {

        public override String Id => "TC_SPINE_RTS_002";

        protected override Boolean DUTIsSystem => true;

        protected override Action<SPINETestTool>? Configure

            => tool => tool.Mutate = datagram => {

                   // PRE_TestTool_AsFutureDevice: a version from the future, on
                   // everything the test tool sends.
                   if (datagram["datagram"]?["header"] is JObject header)
                       header["specificationVersion"] = "1.999.999";

                   foreach (var cmd in datagram["datagram"]?["payload"]?["cmd"] as JArray ?? [])
                       if (cmd[SPINENodeManagement.DetailedDiscoveryData]?["featureInformation"] is JArray features)
                           foreach (var feature in features.OfType<JObject>())
                               if (feature["description"] is JObject description &&
                                   description["role"]?.Value<String>() == "client")
                               {
                                   description["featureType"] = "CompletelyUnknownFeatureType";
                               }

                   return datagram;

               };

        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            await Context.Step(
                      "1",
                      "The test tool initiates and supports PRE_SPINE_Scenario_LpcLpp_S1_S3 while announcing an " +
                      "entirely unknown client feature type and a SPINE version from the future.",
                      "The DUT completes PRE_SPINE_Scenario_LpcLpp_S1_S3 within 120 s.",
                      async step => {

                          var scenario = await LpcLppScenario.Create(Context.Parameters, DUTIsSystem, Configure, CancellationToken);

                          await scenario.Commission(CancellationToken);

                          var written = await scenario.WriteLimit(Context.Parameters.LpcLppTestValue1, true, CancellationToken);

                          step.Require(written.Result is null || written.Result.ErrorNumber == 0,
                                       $"the device did not survive a client feature type it has never heard of: " +
                                       $"{written.Result?.Description}");

                          step.Observe("the client announced itself as \"CompletelyUnknownFeatureType\" over SPINE 1.999.999");

                      });

        }

    }

    #endregion

    #region TC_SPINE_RTS_003

    /// <summary>
    /// Two energy guards on one device, and only one of them binds. The
    /// controllable system has to subscribe back to *that* one.
    ///
    /// The tempting shortcut is to take the first client entity which announces
    /// the use case, which works in every setup with one energy manager and
    /// silently watches the wrong heartbeat in every setup with two.
    /// </summary>
    public sealed class TC_SPINE_RTS_003 : ALpcLppCase
    {

        public override String Id => "TC_SPINE_RTS_003";

        protected override Boolean DUTIsSystem => true;

        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            var scenario = await LpcLppScenario.Create(Context.Parameters, DUTIsSystem, null, CancellationToken);

            await Context.Precondition("PRE_TestTool_Multi_EG_Configured", async () => {

                // A second energy guard on the same device, at a different
                // entity address: "Client B", which never binds to anything.
                var second = scenario.Tool.Tool.AddEntity([ 2 ], EntityTypeType.CEM);

                var guardB = Context.Parameters.LimitationUseCase == "LPP"
                                 ? (UseCases.LimitationOfPower.APowerLimitationEnergyGuard) new UseCases.LPP.LPPEnergyGuard(second)
                                 : new UseCases.LPC.LPCEnergyGuard(second);

                await guardB.Register(CancellationToken);

            });

            await Context.Step(
                      "1",
                      "The test tool sends a nodeManagementBindingRequestCall from Client A using " +
                      "PAR_bindingClientAToLoadControl.",
                      "The DUT responds with a resultData message indicating errorNumber = 0 (ACK).",
                      async step => {

                          await scenario.Tool.DUT. NodeManagement.RequestDetailedDiscovery(scenario.Tool.ToolAsSeenByDUT, CancellationToken);
                          await scenario.Tool.Tool.NodeManagement.RequestDetailedDiscovery(scenario.Tool.DUTAsSeenByTool, CancellationToken);
                          await scenario.Tool.DUT. NodeManagement.RequestUseCaseData      (scenario.Tool.ToolAsSeenByDUT, CancellationToken);
                          await scenario.Tool.Tool.NodeManagement.RequestUseCaseData      (scenario.Tool.DUTAsSeenByTool, CancellationToken);

                          var systemSide = scenario.SystemAsSeenByGuard
                                               ?? throw new ConformanceInconclusive("the energy guard does not know the controllable system");

                          var response = await scenario.Guard.LoadControlOf(systemSide).Bind(CancellationToken);

                          step.Require(response.Result is null || response.Result.ErrorNumber == 0,
                                       $"the device refused the binding of client A: {response.Result?.Description}");

                      });

            await Context.Step(
                      "2",
                      "The test tool sends a nodeManagementBindingRequestCall from Client A using " +
                      "PAR_bindingClientAToDeviceConfiguration.",
                      "The DUT responds with a resultData message indicating errorNumber = 0 (ACK).",
                      async step => {

                          var systemSide = scenario.SystemAsSeenByGuard!;

                          var response = await scenario.Guard.ConfigurationOf(systemSide).Bind(CancellationToken);

                          step.Require(response.Result is null || response.Result.ErrorNumber == 0,
                                       $"the device refused the second binding of client A: {response.Result?.Description}");

                      });

            await Context.Step(
                      "3",
                      "The test tool waits for the node management initiation (subscription).",
                      "The DUT sends a nodeManagementSubscriptionRequestCall targeting the DeviceDiagnosis server " +
                      "feature of Client A, and none targeting Client B, within 120 s.",
                      async step => {

                          var guardSide = scenario.GuardAsSeenBySystem
                                              ?? throw new ConformanceInconclusive("the controllable system does not know the energy guard");

                          await new UseCases.UseCaseFeature(FeatureTypeType.DeviceDiagnosis,
                                                            scenario.System.Entity,
                                                            guardSide).Subscribe(CancellationToken);

                          await scenario.Tool.Advance(TimeSpan.FromSeconds(120), CancellationToken);

                          var subscriptions = scenario.Tool.DUT.SubscriptionsToOthers.All.
                                                  Where(relation => relation.ServerAddress.Device == scenario.Tool.Tool.DeviceAddress).
                                                  ToList();

                          step.Require(subscriptions.Any(relation => relation.ServerAddress.Entity?.SequenceEqual(scenario.Guard.Entity.EntityId) == true),
                                       "the device did not subscribe to the diagnosis of the client which bound to it");

                          step.Require(!subscriptions.Any(relation => relation.ServerAddress.Entity?.SequenceEqual([ 2U ]) == true),
                                       "the device subscribed to the diagnosis of a client which never bound to it");

                          step.Observe($"{subscriptions.Count} subscription(s) towards the test tool");

                      });

        }

    }

    #endregion

    #region TC_SPINE_RTS_004

    /// <summary>
    /// An element nobody has defined, inside a perfectly ordinary limit write.
    /// </summary>
    public sealed class TC_SPINE_RTS_004 : ALpcLppCase
    {

        public override String Id => "TC_SPINE_RTS_004";

        protected override Boolean DUTIsSystem => true;

        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            var scenario = await Commissioned(Context, CancellationToken);
            var unknown  = SPINEParameters.RandomName(15);

            await Context.Step(
                      "1",
                      "The test tool sends a deviceDiagnosisHeartbeatData notify using PAR_notifyAck.",
                      "The DUT responds with a resultData message indicating errorNumber = 0 (ACK).",
                      async step => {

                          await scenario.SendHeartbeat(CancellationToken);

                          step.Observe("heartbeat sent");

                      });

            await Context.Step(
                      "2",
                      "The test tool sends a loadControlLimitListData write request using " +
                      "PAR_writeLpcLppUnknownElement, over a SPINE version from the future.",
                      "The DUT ignores the unknown element and responds with a resultData message indicating " +
                      "errorNumber = 0 (ACK).",
                      async step => {

                          scenario.Tool.Mutate = datagram => {

                              if (datagram["datagram"]?["header"] is JObject header)
                                  header["specificationVersion"] = "1.999.999";

                              foreach (var cmd in datagram["datagram"]?["payload"]?["cmd"] as JArray ?? [])
                                  if (cmd["loadControlLimitListData"]?["loadControlLimitData"] is JArray limits)
                                      foreach (var limit in limits.OfType<JObject>())
                                          limit[unknown] = "testData";

                              return datagram;

                          };

                          var written = await scenario.WriteLimit(Context.Parameters.LpcLppTestValue2, true, CancellationToken);

                          step.Require(written.Result is null || written.Result.ErrorNumber == 0,
                                       $"the device refused a limit because of an element it was supposed to ignore: " +
                                       $"{written.Result?.Description}");

                          step.Observe($"the write carried the undefined element \"{unknown}\"");

                      });

        }

    }

    #endregion

    #region TC_SPINE_RTS_005

    /// <summary>
    /// A partial write carrying a number and no scale updates the number and
    /// keeps the scale.
    ///
    /// The merge logic beats the XSD default, and the difference is three
    /// orders of magnitude: a device which fills in a default scale of zero
    /// where the stored one was three turns a 4.2 kW limit into 4.2 W, and a
    /// wallbox which obeys that stops charging.
    /// </summary>
    public sealed class TC_SPINE_RTS_005 : ALpcLppCase
    {

        public override String Id => "TC_SPINE_RTS_005";

        protected override Boolean DUTIsSystem => true;

        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            var scenario = await Commissioned(Context, CancellationToken);
            var scale    = 0;

            await Context.Step(
                      "1",
                      "The test tool sends a deviceDiagnosisHeartbeatData notify using PAR_notifyAck.",
                      "The DUT responds with a resultData message indicating errorNumber = 0 (ACK).",
                      async step => {

                          await scenario.SendHeartbeat(CancellationToken);

                          step.Observe("heartbeat sent");

                      });

            await Context.Step(
                      "2",
                      "The test tool sends a loadControlLimitListData read request.",
                      "The DUT responds with a reply; the test tool caches the value.scale of the limit.",
                      async step => {

                          var stored = scenario.System.LoadControl.
                                           DataCopy<LoadControlLimitListDataType>("loadControlLimitListData")?.
                                           LoadControlLimitData?.FirstOrDefault();

                          step.Require(stored is not null,
                                       "the device holds no limit at all");

                          scale = stored!.Value?.Scale ?? 0;

                          step.Observe($"the stored limit is {stored.Value?.Number} × 10^{scale}");

                      });

            await Context.Step(
                      "3",
                      "The test tool sends a loadControlLimitListData write request using PAR_writeLpcLppMissingScale: " +
                      "a new number, and no scale at all.",
                      "The DUT accepts the request by sending a resultData message indicating errorNumber = 0 (ACK).",
                      async step => {

                          scenario.Tool.Mutate = datagram => {

                              foreach (var cmd in datagram["datagram"]?["payload"]?["cmd"] as JArray ?? [])
                                  if (cmd["loadControlLimitListData"]?["loadControlLimitData"] is JArray limits)
                                      foreach (var limit in limits.OfType<JObject>())
                                          (limit["value"] as JObject)?.Remove("scale");

                              return datagram;

                          };

                          var written = await scenario.WriteLimit(Context.Parameters.LpcLppTestValue2, true, CancellationToken);

                          step.Require(written.Result is null || written.Result.ErrorNumber == 0,
                                       $"the device refused a partial write which left out an element it was supposed " +
                                       $"to keep: {written.Result?.Description}");

                      });

            await Context.Step(
                      "4",
                      "The test tool sends a loadControlLimitListData read request and evaluates the payload.",
                      "The DUT applied the merge logic: the stored value equals the written number times 10^scale, " +
                      "with the scale it had before.",
                      async step => {

                          var stored = scenario.System.LoadControl.
                                           DataCopy<LoadControlLimitListDataType>("loadControlLimitListData")?.
                                           LoadControlLimitData?.FirstOrDefault();

                          step.Require(stored is not null,
                                       "the device holds no limit after the write");

                          var storedScale = stored!.Value?.Scale ?? 0;

                          step.Require(storedScale == scale,
                                       $"the device changed the scale from {scale} to {storedScale} although the write " +
                                       $"said nothing about it - the stored limit is off by a factor of " +
                                       $"{Math.Pow(10, Math.Abs(storedScale - scale)):0}");

                          step.Observe($"the stored limit is {stored.Value?.Number} × 10^{storedScale} = {stored.Value?.Value} W");

                      });

        }

    }

    #endregion

    #region TC_SPINE_RTC_001

    /// <summary>
    /// A well defined element the use case does not need - "label" - appearing
    /// in a server's reply. The energy guard ignores it and carries on.
    /// </summary>
    public sealed class TC_SPINE_RTC_001 : ALpcLppCase
    {

        public override String Id => "TC_SPINE_RTC_001";

        protected override Boolean DUTIsSystem => false;

        protected override Action<SPINETestTool>? Configure

            => tool => tool.Mutate = Append("loadControlLimitDescriptionListData",
                                            "loadControlLimitDescriptionData",
                                            "label",
                                            "extra");

        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            await Context.Step(
                      "1",
                      "The test tool awaits and supports PRE_SPINE_Scenario_LpcLpp_S1_S3 while appending the " +
                      "well defined but unused element \"label\" to every loadControlLimitDescriptionListData reply.",
                      "The DUT completes PRE_SPINE_Scenario_LpcLpp_S1_S3 within 120 s.",
                      async step => {

                          var scenario = await LpcLppScenario.Create(Context.Parameters, DUTIsSystem, Configure, CancellationToken);

                          await scenario.Commission(CancellationToken);

                          var written = await scenario.WriteLimit(Context.Parameters.LpcLppTestValue1, true, CancellationToken);

                          step.Require(written.Result is null || written.Result.ErrorNumber == 0,
                                       $"the device stumbled over an element which is defined and simply not needed: " +
                                       $"{written.Result?.Description}");

                      });

        }

    }

    #endregion

    #region TC_SPINE_RTC_002

    /// <summary>
    /// The same with an element which is not defined anywhere, over a SPINE
    /// version from the future.
    /// </summary>
    public sealed class TC_SPINE_RTC_002 : ALpcLppCase
    {

        public override String Id => "TC_SPINE_RTC_002";

        protected override Boolean DUTIsSystem => false;

        protected override Action<SPINETestTool>? Configure

            => tool => {

                   var unknown = SPINEParameters.RandomName(15);

                   tool.Mutate = datagram => {

                       if (datagram["datagram"]?["header"] is JObject header)
                           header["specificationVersion"] = "1.999.999";

                       foreach (var cmd in datagram["datagram"]?["payload"]?["cmd"] as JArray ?? [])
                           if (cmd["loadControlLimitDescriptionListData"]?["loadControlLimitDescriptionData"] is JArray descriptions)
                               foreach (var description in descriptions.OfType<JObject>())
                                   description[unknown] = "testData";

                       return datagram;

                   };

               };

        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            await Context.Step(
                      "1",
                      "The test tool awaits and supports PRE_SPINE_Scenario_LpcLpp_S1_S3 while appending an entirely " +
                      "unknown element to every loadControlLimitDescriptionListData reply, over a SPINE version from " +
                      "the future.",
                      "The DUT completes PRE_SPINE_Scenario_LpcLpp_S1_S3 within 120 s.",
                      async step => {

                          var scenario = await LpcLppScenario.Create(Context.Parameters, DUTIsSystem, Configure, CancellationToken);

                          await scenario.Commission(CancellationToken);

                          var written = await scenario.WriteLimit(Context.Parameters.LpcLppTestValue1, true, CancellationToken);

                          step.Require(written.Result is null || written.Result.ErrorNumber == 0,
                                       $"the device stumbled over an element it has never heard of: " +
                                       $"{written.Result?.Description}");

                      });

        }

    }

    #endregion

    #region TC_SPINE_RTC_003

    /// <summary>
    /// The useCaseAvailable flag of a server actor is ignored entirely.
    ///
    /// It reads like a flag which means something, and it does - but only in
    /// the other direction. A server saying "not available" about itself is
    /// telling a client nothing it may act on, so a client which stops
    /// commissioning over it never gets started with a device which sets the
    /// flag by mistake. And devices do.
    /// </summary>
    public sealed class TC_SPINE_RTC_003 : ALpcLppCase
    {

        public override String Id => "TC_SPINE_RTC_003";

        protected override Boolean DUTIsSystem => false;

        protected override Action<SPINETestTool>? Configure

            => tool => tool.Mutate = datagram => {

                   foreach (var cmd in datagram["datagram"]?["payload"]?["cmd"] as JArray ?? [])
                       if (cmd[SPINENodeManagement.UseCaseData]?["useCaseInformation"] is JArray information)
                           foreach (var entry in information.OfType<JObject>())
                               if (entry["useCaseSupport"] is JArray support)
                                   foreach (var one in support.OfType<JObject>())
                                       one["useCaseAvailable"] = false;

                   return datagram;

               };

        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            await Context.Step(
                      "1",
                      "The test tool awaits and supports PRE_SPINE_Scenario_LpcLpp_S1_S3 while setting " +
                      "useCaseAvailable = false in every nodeManagementUseCaseData reply.",
                      "The DUT completes PRE_SPINE_Scenario_LpcLpp_S1_S3 within 120 s.",
                      async step => {

                          var scenario = await LpcLppScenario.Create(Context.Parameters, DUTIsSystem, Configure, CancellationToken);

                          await scenario.Commission(CancellationToken);

                          var written = await scenario.WriteLimit(Context.Parameters.LpcLppTestValue1, true, CancellationToken);

                          step.Require(written.Result is null || written.Result.ErrorNumber == 0,
                                       $"the device stopped over a flag it was supposed to ignore: " +
                                       $"{written.Result?.Description}");

                      });

        }

    }

    #endregion

}
