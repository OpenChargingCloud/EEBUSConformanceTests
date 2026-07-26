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

    #region TC_SPINE_DATA_001

    /// <summary>
    /// Message counters ascend. Twenty reads in a row, twenty counters going up.
    /// </summary>
    public sealed class TC_SPINE_DATA_001 : ASPINECase
    {

        public override String Id => "TC_SPINE_DATA_001";

        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            var tool = Connected(Context.Parameters);

            await Context.Step(
                      "1",
                      "The test tool sends a nodeManagementDetailedDiscoveryData read request 20 times consecutively.",
                      "The DUT responds with 20 replies whose msgCounter values ascend strictly.",
                      async step => {

                          var asked = new List<UInt64>();

                          for (var round = 0; round < 20; round++)
                          {

                              var counter = tool.NextMsgCounter();

                              asked.Add(counter);

                              await tool.Send(SPINEParameters.ReadDetailedDiscovery(tool.ToolNodeManagement,
                                                                                    tool.DUTNodeManagement,
                                                                                    counter),
                                              CancellationToken);

                          }

                          var replies = asked.Select(counter => tool.ReplyFor(counter)).ToList();

                          step.Require(replies.All(reply => reply is not null),
                                       $"the device answered only {replies.Count(reply => reply is not null)} of 20 reads");

                          var counters = replies.Select(reply => reply!.Header?.MsgCounter ?? 0).ToList();

                          for (var index = 1; index < counters.Count; index++)
                              step.Require(counters[index] > counters[index - 1],
                                           $"the message counters {counters[index - 1]} and {counters[index]} do not ascend");

                          step.Observe($"{counters.First()} … {counters.Last()}");

                      });

        }

    }

    #endregion

    #region TC_SPINE_DATA_002

    /// <summary>
    /// Incoming counters may skip. Nothing says they have to be consecutive -
    /// a partner talking to three devices at once numbers all of them from one
    /// sequence.
    /// </summary>
    public sealed class TC_SPINE_DATA_002 : ASPINECase
    {

        public override String Id => "TC_SPINE_DATA_002";

        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            var tool = Connected(Context.Parameters);

            foreach (var counter in new UInt64[] { 21012, 123321 })
            {

                var msgCounter = counter;

                await Context.Step(
                          msgCounter == 21012 ? "1" : "2",
                          $"The test tool sends a nodeManagementDetailedDiscoveryData read request with msgCounter set to {msgCounter}.",
                          "The DUT responds with a nodeManagementDetailedDiscoveryData reply.",
                          async step => {

                              await tool.Send(SPINEParameters.ReadDetailedDiscovery(tool.ToolNodeManagement,
                                                                                    tool.DUTNodeManagement,
                                                                                    msgCounter),
                                              CancellationToken);

                              step.Require(tool.ReplyDataFor(msgCounter, SPINENodeManagement.DetailedDiscoveryData) is not null,
                                           $"the device did not answer a read numbered {msgCounter}");

                          });

            }

        }

    }

    #endregion

    #region TC_SPINE_DATA_003

    /// <summary>
    /// A counter which goes backwards - a partner rebooted, or wrapped around -
    /// resets the baseline instead of being treated as an attack.
    ///
    /// A device which refuses anything below what it has already seen becomes
    /// permanently unreachable to a partner which restarts, which is the one
    /// thing partners reliably do.
    /// </summary>
    public sealed class TC_SPINE_DATA_003 : ASPINECase
    {

        public override String Id => "TC_SPINE_DATA_003";

        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            var tool = Connected(Context.Parameters);

            var step = 0;

            foreach (var counter in new UInt64[] { 123456, 1000, 2000 })
            {

                var msgCounter = counter;

                step++;

                await Context.Step(
                          step.ToString(),
                          $"The test tool sends a nodeManagementDetailedDiscoveryData read request with msgCounter set to {msgCounter}.",
                          "The DUT responds with a nodeManagementDetailedDiscoveryData reply.",
                          async stepResult => {

                              await tool.Send(SPINEParameters.ReadDetailedDiscovery(tool.ToolNodeManagement,
                                                                                    tool.DUTNodeManagement,
                                                                                    msgCounter),
                                              CancellationToken);

                              stepResult.Require(tool.ReplyDataFor(msgCounter, SPINENodeManagement.DetailedDiscoveryData) is not null,
                                                 $"the device did not answer a read numbered {msgCounter}");

                          });

            }

        }

    }

    #endregion

    #region TC_SPINE_DATA_004

    /// <summary>
    /// Every reply carries the counter of its request. Twenty times, with the
    /// counters far apart, so that a device which simply echoes the last one it
    /// saw is caught.
    /// </summary>
    public sealed class TC_SPINE_DATA_004 : ASPINECase
    {

        public override String Id => "TC_SPINE_DATA_004";

        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            var tool = Connected(Context.Parameters);

            await Context.Step(
                      "1",
                      "The test tool sends 20 nodeManagementDetailedDiscoveryData read requests with the message " +
                      "counters 88777 + 200*i, waiting for the reply of each.",
                      "Every reply's msgCounterReference exactly matches the msgCounter of its request.",
                      async step => {

                          for (var i = 0UL; i < 20; i++)
                          {

                              var counter = 88777 + 200 * i;

                              await tool.Send(SPINEParameters.ReadDetailedDiscovery(tool.ToolNodeManagement,
                                                                                    tool.DUTNodeManagement,
                                                                                    counter),
                                              CancellationToken);

                              var reply = tool.ReplyFor(counter);

                              step.Require(reply is not null,
                                           $"the device did not answer the read numbered {counter}");

                              step.Require(reply!.Header?.MsgCounterReference == counter,
                                           $"the reply refers to {reply.Header?.MsgCounterReference} instead of {counter}");

                          }

                          step.Observe("20 of 20 replies referred to their own request");

                      });

        }

    }

    #endregion

    #region TC_SPINE_DATA_005

    /// <summary>
    /// A notify which asks to be acknowledged is acknowledged.
    /// </summary>
    public sealed class TC_SPINE_DATA_005 : ASPINECase
    {

        public override String Id => "TC_SPINE_DATA_005";

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
                      "The test tool sends a nodeManagementDetailedDiscoveryData notify datagram using " +
                      "PAR_notifyDiscoveryModifiedAck.",
                      "The DUT responds with a resultData message (ACK) indicating errorNumber = 0.",
                      async step => {

                          var counter = tool.NextMsgCounter();

                          await tool.Send(SPINEParameters.NotifyDiscoveryModified(tool.ToolNodeManagement,
                                                                                  tool.DUTNodeManagement,
                                                                                  counter,
                                                                                  tool.Tool.DeviceAddress),
                                          CancellationToken);

                          var result = tool.ResultFor(counter);

                          step.Require(result is not null,
                                       "the device did not acknowledge a notify which asked to be acknowledged");

                          step.Require(result!.ErrorNumber == 0,
                                       $"the device acknowledged with errorNumber {result.ErrorNumber}: {result.Description}");

                      });

        }

    }

    #endregion

    #region TC_SPINE_DATA_006

    /// <summary>
    /// A result is never answered with a result - not even one which asks for
    /// it. Two devices which acknowledge each other's acknowledgements have
    /// found a way to talk forever without saying anything.
    /// </summary>
    public sealed class TC_SPINE_DATA_006 : ASPINECase
    {

        public override String Id => "TC_SPINE_DATA_006";

        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            var tool  = Connected(Context.Parameters);
            var read  = 0UL;

            await Context.Step(
                      "1",
                      "The test tool sends a nodeManagementDetailedDiscoveryData read request.",
                      "The DUT responds with a nodeManagementDetailedDiscoveryData reply.",
                      async step => {

                          read = tool.NextMsgCounter();

                          await tool.Send(SPINEParameters.ReadDetailedDiscovery(tool.ToolNodeManagement,
                                                                                tool.DUTNodeManagement,
                                                                                read),
                                          CancellationToken);

                          step.Require(tool.ReplyFor(read) is not null,
                                       "the device did not answer the detailed discovery read");

                      });

            await Context.Step(
                      "2",
                      "The test tool sends a resultData message using PAR_resultAck.",
                      "The DUT remains silent and does not send any response.",
                      async step => {

                          var counter  = tool.NextMsgCounter();
                          var before   = tool.FromDUT.Count;
                          var replyTo  = tool.ReplyFor(read)?.Header?.MsgCounter;

                          await tool.Send(SPINEParameters.ResultAck(tool.ToolNodeManagement,
                                                                    tool.DUTNodeManagement,
                                                                    counter,
                                                                    replyTo),
                                          CancellationToken);

                          var answers = tool.FromDUT.Skip(before).
                                            Where(datagram => datagram.Header?.MsgCounterReference == counter).
                                            ToList();

                          step.Require(answers.Count == 0,
                                       $"the device answered a result with {answers.Count} datagram(s) - " +
                                       $"two devices doing that never stop");

                      });

        }

    }

    #endregion

    #region TC_SPINE_DATA_007

    /// <summary>
    /// The ackRequest of a read is not evaluated: a read is answered with a
    /// reply, and the reply *is* the acknowledgement. Sending both would mean
    /// answering one question twice.
    /// </summary>
    public sealed class TC_SPINE_DATA_007 : ASPINECase
    {

        public override String Id => "TC_SPINE_DATA_007";

        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            var tool = Connected(Context.Parameters);

            await Context.Step(
                      "1",
                      "The test tool sends a nodeManagementDetailedDiscoveryData read request using PAR_readAckRequestFalse.",
                      "The DUT responds with a reply datagram.",
                      async step => {

                          var counter = tool.NextMsgCounter();

                          await tool.Send(SPINEParameters.ReadDetailedDiscovery(tool.ToolNodeManagement,
                                                                                tool.DUTNodeManagement,
                                                                                counter,
                                                                                AckRequest: false),
                                          CancellationToken);

                          step.Require(tool.ReplyFor(counter) is not null,
                                       "the device did not answer a read whose ackRequest was false");

                      });

            await Context.Step(
                      "2",
                      "The test tool sends a nodeManagementDetailedDiscoveryData read request using PAR_readAckRequestTrue.",
                      "The DUT responds with a reply datagram AND does NOT send an additional resultData message.",
                      async step => {

                          var counter = tool.NextMsgCounter();

                          await tool.Send(SPINEParameters.ReadDetailedDiscovery(tool.ToolNodeManagement,
                                                                                tool.DUTNodeManagement,
                                                                                counter,
                                                                                AckRequest: true),
                                          CancellationToken);

                          step.Require(tool.ReplyFor(counter) is not null,
                                       "the device did not answer a read whose ackRequest was true");

                          step.Require(tool.ResultFor(counter) is null,
                                       "the device answered the read twice, with a reply and a result");

                      });

        }

    }

    #endregion

    #region TC_SPINE_DATA_008

    /// <summary>
    /// The same rule seen from the other side: a result answering the device's
    /// own request is not answered either.
    /// </summary>
    public sealed class TC_SPINE_DATA_008 : ASPINECase
    {

        public override String Id => "TC_SPINE_DATA_008";

        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            var tool  = Connected(Context.Parameters);
            var read  = 0UL;

            await Context.Step(
                      "1",
                      "The test tool waits for a nodeManagementDetailedDiscoveryData read request for up to 30 s.",
                      "The DUT sends a nodeManagementDetailedDiscoveryData read request.",
                      async step => {

                          _ = tool.DUT.NodeManagement.RequestDetailedDiscovery(tool.ToolAsSeenByDUT, CancellationToken);

                          await tool.Advance(TimeSpan.FromSeconds(30), CancellationToken);

                          var request = tool.All(CmdClassifierType.Read, SPINENodeManagement.DetailedDiscoveryData).FirstOrDefault();

                          step.Require(request is not null,
                                       "the device did not ask a new partner what it is");

                          read = request!.Header?.MsgCounter ?? 0;

                      });

            await Context.Step(
                      "2",
                      "The test tool responds with a resultData message using PAR_resultAck.",
                      "The DUT remains silent or terminates the connection; it does NOT send a resultData message.",
                      async step => {

                          var counter  = tool.NextMsgCounter();
                          var before   = tool.FromDUT.Count;

                          await tool.Send(SPINEParameters.ResultAck(tool.ToolNodeManagement,
                                                                    tool.DUTNodeManagement,
                                                                    counter,
                                                                    read),
                                          CancellationToken);

                          var results = tool.FromDUT.Skip(before).
                                            Where(datagram => datagram.Header?.CmdClassifier == CmdClassifierType.Result).
                                            ToList();

                          step.Require(results.Count == 0,
                                       $"the device answered a result with {results.Count} result(s) of its own");

                      });

        }

    }

    #endregion

}
