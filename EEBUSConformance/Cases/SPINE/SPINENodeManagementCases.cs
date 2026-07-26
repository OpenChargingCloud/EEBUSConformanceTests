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

#endregion

namespace cloud.charging.open.protocols.EEBUS.Conformance
{

    #region TC_SPINE_FC_001

    /// <summary>
    /// Node management lives on entity 0, feature 0 and nowhere else.
    ///
    /// A device which answers node management wherever it is addressed looks
    /// forgiving and is not: the addresses in a binding or subscription request
    /// are then decided by whoever asked, and two partners can end up with two
    /// different ideas of the same relation.
    /// </summary>
    public sealed class TC_SPINE_FC_001 : ASPINECase
    {

        public override String Id => "TC_SPINE_FC_001";

        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            var tool = Connected(Context.Parameters);

            await Context.Step(
                      "1",
                      "The test tool sends a nodeManagementSubscriptionRequestCall using PAR_fcDestE1SourceE0, " +
                      "addressed to entity 1, feature 0.",
                      "The DUT rejects the request and sends a resultData message (NACK) indicating errorNumber > 0.",
                      async step => {

                          var counter = tool.NextMsgCounter();

                          await tool.Send(SPINEParameters.SubscriptionToWrongDestination(tool.ToolNodeManagement,
                                                                                          tool.DUT.DeviceAddress,
                                                                                          counter,
                                                                                          tool.Tool.DeviceAddress),
                                          CancellationToken);

                          var result = tool.ResultFor(counter);

                          step.Require(result is not null,
                                       "the device said nothing about node management sent to the wrong feature");

                          step.Require(result!.ErrorNumber > 0,
                                       "the device accepted node management on a feature which is not its primary one");

                          step.Observe($"errorNumber {result.ErrorNumber}: {result.Description}");

                      });

        }

    }

    #endregion

    #region TC_SPINE_DDISC_001

    /// <summary>
    /// A device with client features asks a partner it has never met what it
    /// is. Nothing else can happen before it does.
    /// </summary>
    public sealed class TC_SPINE_DDISC_001 : ASPINECase
    {

        public override String Id => "TC_SPINE_DDISC_001";

        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            var tool = Connected(Context.Parameters);

            await Context.Step(
                      "1",
                      "The test tool waits for a nodeManagementDetailedDiscoveryData read request for up to 30 s, " +
                      "presenting a SPINE device address which has never been connected to the DUT before.",
                      "The DUT sends a nodeManagementDetailedDiscoveryData read request.",
                      async step => {

                          // On this stack the discovery of a new partner is
                          // started by whoever added it, which is the layer
                          // above SPINE.
                          _ = tool.DUT.NodeManagement.RequestDetailedDiscovery(tool.ToolAsSeenByDUT, CancellationToken);

                          await tool.Advance(TimeSpan.FromSeconds(30), CancellationToken);

                          step.Require(tool.Sent(CmdClassifierType.Read, SPINENodeManagement.DetailedDiscoveryData),
                                       "the device did not ask a new, unknown partner for its detailed discovery");

                      });

        }

    }

    #endregion

    #region TC_SPINE_DDISC_002

    /// <summary>
    /// A newly paired partner which says nothing for thirty seconds may be
    /// dropped. Optional, and declared through PAR_initialTimeoutSupported.
    /// </summary>
    public sealed class TC_SPINE_DDISC_002 : ASPINECase
    {

        public override String Id => "TC_SPINE_DDISC_002";

        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            var tool = Connected(Context.Parameters);

            await Context.Step(
                      "1",
                      "The test tool waits for a SPINE connection termination for up to 30 s plus the tolerance " +
                      "window, sending nothing and silently dropping everything it receives.",
                      "The DUT either terminates the SPINE connection actively or initiates a communication retry.",
                      async step => {

                          var before = tool.FromDUT.Count;

                          await tool.Advance(TimeSpan.FromSeconds(32), CancellationToken);

                          step.Require(tool.FromDUT.Count > before,
                                       "the device neither dropped the silent partner nor retried, although it " +
                                       "declared PAR_initialTimeoutSupported = \"yes\"");

                      });

        }

    }

    #endregion

    #region TC_SPINE_BIND_001

    /// <summary>
    /// Nobody may bind to the primary node management feature.
    ///
    /// A binding is what lets a client write; node management is where a device
    /// keeps its own topology, its bindings and its subscriptions. A binding to
    /// it would be a licence to rewrite exactly those.
    /// </summary>
    public sealed class TC_SPINE_BIND_001 : ASPINECase
    {

        public override String Id => "TC_SPINE_BIND_001";

        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            var tool = Connected(Context.Parameters);

            await Context.Precondition("PRE_SPINE_DetailedDiscovery", async () => {

                var counter = tool.NextMsgCounter();

                await tool.Send(SPINEParameters.ReadDetailedDiscovery(tool.ToolNodeManagement,
                                                                      tool.DUTNodeManagement,
                                                                      counter),
                                CancellationToken);

                if (tool.ReplyFor(counter) is null)
                    throw new ConformanceInconclusive("the device did not answer the detailed discovery read");

            });

            await Context.Step(
                      "1",
                      "The test tool sends a binding request call using PAR_bindingNm.",
                      "The DUT rejects the request and sends a resultData message (NACK) indicating errorNumber > 0.",
                      async step => {

                          var counter = tool.NextMsgCounter();

                          await tool.Send(SPINEParameters.BindingToNodeManagement(tool.ToolNodeManagement,
                                                                                  tool.DUTNodeManagement,
                                                                                  counter),
                                          CancellationToken);

                          var result = tool.ResultFor(counter);

                          step.Require(result is not null,
                                       "the device said nothing about a binding request to its node management");

                          step.Require(result!.ErrorNumber > 0,
                                       "the device accepted a binding to its primary node management feature");

                          step.Observe($"errorNumber {result.ErrorNumber}: {result.Description}");

                      });

            await Context.Step(
                      "2",
                      "The test tool sends a nodeManagementBindingData read request.",
                      "The DUT responds with a reply containing no binding entry for its primary node management feature.",
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

                          var offending = (bindings!.BindingEntry ?? []).
                                              Where(entry => entry.ServerAddress?.Entity?.SequenceEqual([ 0U ]) == true &&
                                                             entry.ServerAddress?.Feature == 0).
                                              ToList();

                          step.Require(offending.Count == 0,
                                       $"the device stored {offending.Count} binding(s) to its primary node management feature");

                          step.Observe($"{(bindings.BindingEntry ?? []).Count()} binding(s) stored");

                      });

        }

    }

    #endregion

    #region TC_SPINE_SUBS_001

    /// <summary>
    /// Subscribing to the primary node management feature, on the other hand,
    /// is exactly what everybody does: it is how a device learns that its
    /// partner grew an entity or lost one.
    /// </summary>
    public sealed class TC_SPINE_SUBS_001 : ASPINECase
    {

        public override String Id => "TC_SPINE_SUBS_001";

        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            var tool = Connected(Context.Parameters);

            await Context.Step(
                      "1",
                      "The test tool sends a nodeManagementSubscriptionRequestCall using PAR_subscriptionNm.",
                      "The DUT responds with a resultData message (ACK) indicating errorNumber = 0.",
                      async step => {

                          var counter = tool.NextMsgCounter();

                          await tool.Send(SPINEParameters.SubscriptionToNodeManagement(tool.ToolNodeManagement,
                                                                                        tool.DUTNodeManagement,
                                                                                        counter),
                                          CancellationToken);

                          var result = tool.ResultFor(counter);

                          step.Require(result is not null,
                                       "the device did not acknowledge the subscription request");

                          step.Require(result!.ErrorNumber == 0,
                                       $"the device refused a subscription to its node management with errorNumber " +
                                       $"{result.ErrorNumber}: {result.Description}");

                      });

            await Context.Step(
                      "2",
                      "The test tool sends a nodeManagementSubscriptionData read request.",
                      "The DUT responds with a reply containing a subscription entry for its primary node management feature.",
                      async step => {

                          var counter = tool.NextMsgCounter();

                          await tool.Send(SPINEParameters.ReadRelations(tool.ToolNodeManagement,
                                                                        tool.DUTNodeManagement,
                                                                        counter,
                                                                        Bindings: false),
                                          CancellationToken);

                          var subscriptions = tool.ReplyDataFor(counter, SPINENodeManagement.SubscriptionData)
                                                  as NodeManagementSubscriptionDataType;

                          step.Require(subscriptions is not null,
                                       "the device did not answer the subscription table read");

                          step.Require((subscriptions!.SubscriptionEntry ?? []).
                                           Any(entry => entry.ClientAddress?.Entity?.SequenceEqual([ 0U ]) == true &&
                                                        entry.ClientAddress?.Feature == 0 &&
                                                        entry.ServerAddress?.Entity?.SequenceEqual([ 0U ]) == true &&
                                                        entry.ServerAddress?.Feature == 0),
                                       "the device acknowledged the subscription but did not store it");

                      });

        }

    }

    #endregion

    #region TC_SPINE_SUBS_002

    /// <summary>
    /// Deleting a subscription works, and deleting it twice does not break
    /// anything - a partner which reconnects and cleans up is allowed to be
    /// thorough about it.
    /// </summary>
    public sealed class TC_SPINE_SUBS_002 : ASPINECase
    {

        public override String Id => "TC_SPINE_SUBS_002";

        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            var tool = Connected(Context.Parameters);

            await Context.Precondition("PRE_SPINE_SubscriptionEstablished", async () => {

                var counter = tool.NextMsgCounter();

                await tool.Send(SPINEParameters.SubscriptionToNodeManagement(tool.ToolNodeManagement,
                                                                             tool.DUTNodeManagement,
                                                                             counter),
                                CancellationToken);

                if (tool.ResultFor(counter) is not ResultDataType result || result.ErrorNumber != 0)
                    throw new ConformanceInconclusive("the device refused a subscription to its primary node management feature");

            });

            await Context.Step(
                      "1",
                      "The test tool sends a nodeManagementSubscriptionDeleteCall using PAR_deleteSubscriptionNm.",
                      "The DUT responds with a resultData message (ACK) indicating errorNumber = 0.",
                      async step => {

                          var counter = tool.NextMsgCounter();

                          await tool.Send(SPINEParameters.DeleteSubscriptionToNodeManagement(tool.ToolNodeManagement,
                                                                                              tool.DUTNodeManagement,
                                                                                              counter),
                                          CancellationToken);

                          var result = tool.ResultFor(counter);

                          step.Require(result is not null,
                                       "the device did not acknowledge the deletion");

                          step.Require(result!.ErrorNumber == 0,
                                       $"the device refused to delete an existing subscription with errorNumber " +
                                       $"{result.ErrorNumber}: {result.Description}");

                      });

            await Context.Step(
                      "2",
                      "The test tool sends the same nodeManagementSubscriptionDeleteCall again.",
                      "The DUT responds with a resultData message indicating errorNumber = 0 or an application error.",
                      async step => {

                          var counter = tool.NextMsgCounter();

                          await tool.Send(SPINEParameters.DeleteSubscriptionToNodeManagement(tool.ToolNodeManagement,
                                                                                              tool.DUTNodeManagement,
                                                                                              counter),
                                          CancellationToken);

                          var result = tool.ResultFor(counter);

                          step.Require(result is not null,
                                       "the device said nothing about the repeated deletion, so a partner cleaning up " +
                                       "cannot tell whether it worked");

                          step.Observe(result!.ErrorNumber == 0
                                           ? "acknowledged the repeated deletion"
                                           : $"refused the repeated deletion with errorNumber {result.ErrorNumber}, which is allowed");

                      });

            await Context.Step(
                      "3",
                      "The test tool sends a nodeManagementSubscriptionData read request.",
                      "The DUT responds with a reply containing no entry for its primary node management feature.",
                      async step => {

                          var counter = tool.NextMsgCounter();

                          await tool.Send(SPINEParameters.ReadRelations(tool.ToolNodeManagement,
                                                                        tool.DUTNodeManagement,
                                                                        counter,
                                                                        Bindings: false),
                                          CancellationToken);

                          var subscriptions = tool.ReplyDataFor(counter, SPINENodeManagement.SubscriptionData)
                                                  as NodeManagementSubscriptionDataType;

                          step.Require(subscriptions is not null,
                                       "the device did not answer the subscription table read");

                          step.Require(!(subscriptions!.SubscriptionEntry ?? []).
                                            Any(entry => entry.ClientAddress?.Entity?.SequenceEqual([ 0U ]) == true &&
                                                         entry.ClientAddress?.Feature == 0 &&
                                                         entry.ServerAddress?.Entity?.SequenceEqual([ 0U ]) == true &&
                                                         entry.ServerAddress?.Feature == 0),
                                       "the device acknowledged the deletion but kept the subscription");

                      });

        }

    }

    #endregion

}
