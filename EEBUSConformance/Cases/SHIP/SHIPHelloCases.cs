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

using cloud.charging.open.protocols.EEBUS.SHIP;

#endregion

namespace cloud.charging.open.protocols.EEBUS.Conformance
{

    #region TC_SHIP_HELLO_001

    /// <summary>
    /// Both sides announce "ready" and the device moves on to the protocol
    /// handshake.
    /// </summary>
    public sealed class TC_SHIP_HELLO_001 : AConformanceTest
    {

        public override String Id => "TC_SHIP_HELLO_001";

        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            var tool = await SHIPTestTool.Open(new SHIPTestToolOptions(SHIPRoles.Server), CancellationToken);

            await Context.Step(
                      "1",
                      "The test tool sends a CMI message using PAR_cmiValidInit and waits for the DUT's initial SME \"hello\" message.",
                      "The DUT replies with a CMI message and subsequently sends its initial SME \"hello\" message with phase = \"ready\".",
                      async step => {

                          await tool.SendRaw(SHIPParameters.CmiValidInitBytes, CancellationToken);

                          step.Require(await tool.WaitFor<SHIPInitMessage>(TimeSpan.FromSeconds(10)) is not null,
                                       "the device did not answer the connection mode initialisation");

                          var hello = await tool.WaitFor<SHIPHelloMessage>(TimeSpan.FromSeconds(10));

                          step.Require(hello is not null,
                                       "the device did not send a connection hello");

                          step.Require(hello!.ConnectionHello.Phase == ConnectionHelloPhase.Ready,
                                       $"the device announced the phase \"{hello.ConnectionHello.Phase}\" although it trusts the test tool");

                      });

            await Context.Step(
                      "2",
                      "The test tool sends an SME \"hello\" message using PAR_helloStateReady.",
                      "The DUT sends an SME \"protocol handshake\" message.",
                      async step => {

                          await tool.Send(SHIPParameters.HelloStateReady, CancellationToken);

                          step.Require(await EnteredProtocolHandshake(tool, Context, CancellationToken),
                                       "the device did not enter the protocol handshake");

                      });

        }


        /// <summary>
        /// Whether the device moved on to the protocol handshake.
        ///
        /// The step says "the DUT sends an SME protocol handshake message", and
        /// for a device in the *server* role that cannot be the first message
        /// of the phase: the three way handshake starts with the client's
        /// announceMax, and a server which announced by itself would be the one
        /// breaking chapter 13.4.4.2 (the Go reference waits here too). The
        /// test tool is the SHIP client throughout these cases, and sending its
        /// own announceMax is its own next step rather than an automatic reply,
        /// so PRE_SHIP_Manual_Message_Handling does not forbid it. What is
        /// verified is what the step means: the device left the hello phase and
        /// answers within the protocol handshake.
        /// </summary>
        internal static async Task<Boolean> EnteredProtocolHandshake(SHIPTestTool        Tool,
                                                                     ConformanceContext  Context,
                                                                     CancellationToken   CancellationToken)
        {

            if (await Tool.WaitFor<SHIPHandshakeMessage>(TimeSpan.FromSeconds(1)) is not null)
                return true;

            await Tool.Send(SHIPParameters.ProtocolAnnounceMax(Context.Parameters), CancellationToken);

            return await Tool.WaitFor<SHIPHandshakeMessage>(TimeSpan.FromSeconds(10)) is not null;

        }

    }

    #endregion

    #region TC_SHIP_HELLO_002

    /// <summary>
    /// A device waiting for its user to accept a partner has to be kept waiting
    /// by that partner, arbitrarily often. Somebody has to walk to the wallbox
    /// and press a button, and the connection must not die while they do.
    /// </summary>
    public sealed class TC_SHIP_HELLO_002 : AConformanceTest
    {

        public override String Id => "TC_SHIP_HELLO_002";

        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            var tool = await SHIPTestTool.Open(new SHIPTestToolOptions(SHIPRoles.Server), CancellationToken);

            await Context.Step(
                      "1",
                      "The test tool sends a CMI message using PAR_cmiValidInit and waits for the DUT's initial SME \"hello\" message.",
                      "The DUT replies with a CMI message and subsequently sends its initial SME \"hello\" message with phase = \"ready\".",
                      async step => {

                          await tool.SendRaw(SHIPParameters.CmiValidInitBytes, CancellationToken);

                          step.Require(await tool.WaitFor<SHIPInitMessage>(TimeSpan.FromSeconds(10)) is not null,
                                       "the device did not answer the connection mode initialisation");

                          var hello = await tool.WaitFor<SHIPHelloMessage>(TimeSpan.FromSeconds(10));

                          step.Require(hello is not null && hello.ConnectionHello.Phase == ConnectionHelloPhase.Ready,
                                       "the device did not announce itself as ready");

                      });

            await Context.Step(
                      "2",
                      "The test tool waits for 45 seconds.",
                      "The underlying TCP/TLS connection is still active.",
                      async step => {

                          await tool.Advance(TimeSpan.FromSeconds(45), CancellationToken);

                          step.Require(!tool.DUTClosed,
                                       $"the device gave up after {tool.Elapsed.TotalSeconds:0.#} s, before its own Wait-For-Ready timer could have expired");

                      });

            for (var iteration = 1U; iteration <= Context.Parameters.HelloProlongationCount; iteration++)
            {

                var round = iteration;

                await Context.Step(
                          $"3.1 ({round})",
                          "The test tool sends an SME \"hello\" message using PAR_helloProlongationRequest.",
                          "The DUT sends an SME \"hello\" update message with phase = \"ready\" and maintains the connection.",
                          async step => {

                              await tool.Send(SHIPParameters.HelloProlongationRequest, CancellationToken);

                              var update = await tool.WaitFor<SHIPHelloMessage>(TimeSpan.FromSeconds(10),
                                                                                message => message.ConnectionHello.Phase == ConnectionHelloPhase.Ready);

                              step.Require(update is not null,
                                           $"the device did not answer prolongation request {round} with a \"ready\" update");

                              step.Require(!tool.DUTClosed,
                                           "the device closed the connection although it was asked to keep waiting");

                          });

                await Context.Step(
                          $"3.2 ({round})",
                          "The test tool waits for 60 seconds.",
                          "The underlying TCP/TLS connection is still active.",
                          async step => {

                              // One millisecond short of the full minute, on
                              // purpose. A prolongation restarts a sixty second
                              // timer, so waiting exactly sixty seconds and then
                              // asking again lands on the very instant the timer
                              // fires - on a real wire the two are separated by
                              // the network, on a simulated clock they are not.
                              // The intent of the step is "prolong before the
                              // timer expires", and a device which gave up
                              // earlier still fails here.
                              await tool.Advance(TimeSpan.FromSeconds(60) - TimeSpan.FromMilliseconds(1), CancellationToken);

                              step.Require(!tool.DUTClosed,
                                           $"the device gave up {tool.Elapsed.TotalSeconds:0.#} s in, although it had been asked to keep waiting");

                          });

            }

            await Context.Step(
                      "4",
                      "The test tool sends an SME \"hello\" message using PAR_helloStateReady.",
                      "The DUT sends an SME \"protocol handshake\" message.",
                      async step => {

                          await tool.Send(SHIPParameters.HelloStateReady, CancellationToken);

                          step.Require(await TC_SHIP_HELLO_001.EnteredProtocolHandshake(tool, Context, CancellationToken),
                                       "the device did not enter the protocol handshake after the partner became ready");

                      });

        }

    }

    #endregion

    #region TC_SHIP_HELLO_003

    /// <summary>
    /// The other end of the same mechanism: a partner which neither becomes
    /// ready nor asks for more time is given up on - loudly, with an "aborted"
    /// message, not by silently dropping the socket.
    /// </summary>
    public sealed class TC_SHIP_HELLO_003 : AConformanceTest
    {

        public override String Id => "TC_SHIP_HELLO_003";

        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            var tool     = await SHIPTestTool.Open(new SHIPTestToolOptions(SHIPRoles.Server), CancellationToken);
            var waiting  = TimeSpan.Zero;

            await Context.Step(
                      "1",
                      "The test tool sends a CMI message using PAR_cmiValidInit and waits for the DUT's initial SME \"hello\" message.",
                      "The DUT sends its initial SME \"hello\" message with phase = \"ready\"; " +
                      "\"connectionHello.waiting\" is present and between 58000 and 240000 milliseconds.",
                      async step => {

                          await tool.SendRaw(SHIPParameters.CmiValidInitBytes, CancellationToken);

                          var hello = await tool.WaitFor<SHIPHelloMessage>(TimeSpan.FromSeconds(10));

                          step.Require(hello is not null,
                                       "the device did not send a connection hello");

                          step.Require(hello!.ConnectionHello.Waiting.HasValue,
                                       "the device did not announce how long it waits, so a partner cannot prolong in time");

                          var announced = hello.ConnectionHello.Waiting!.Value;

                          step.Require(announced is >= 58000 and <= 240000,
                                       $"the device announced a waiting time of {announced} ms, outside the accepted 58000 to 240000 ms");

                          waiting = TimeSpan.FromMilliseconds(announced);

                          step.Observe($"waiting {announced} ms");

                      });

            await Context.Step(
                      "2",
                      "The test tool waits exactly the received \"connectionHello.waiting\" value plus 2000 milliseconds " +
                      "and does NOT send any SME message.",
                      "The DUT sends an SME \"hello\" message with phase = \"aborted\" and actively closes the TCP/TLS connection.",
                      async step => {

                          await tool.Advance(waiting + TimeSpan.FromSeconds(2), CancellationToken);

                          var aborted = tool.Received.OfType<SHIPHelloMessage>().
                                            LastOrDefault(message => message.ConnectionHello.Phase == ConnectionHelloPhase.Aborted);

                          step.Require(aborted is not null,
                                       "the device did not announce the abort, so the partner cannot tell a refusal from a broken network");

                          step.Require(tool.DUTClosed,
                                       "the device announced the abort but kept the connection open");

                      });

        }

    }

    #endregion

    #region TC_SHIP_HELLO_004

    /// <summary>
    /// A "pending" without a prolongation request, arriving at a device which
    /// is already ready, means nothing and has to be ignored in silence.
    ///
    /// It is the one state machine rule that is easy to get wrong by being
    /// helpful: answering it, or treating it as a reason to start waiting
    /// again, both break the handshake.
    /// </summary>
    public sealed class TC_SHIP_HELLO_004 : AConformanceTest
    {

        public override String Id => "TC_SHIP_HELLO_004";

        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            var tool = await SHIPTestTool.Open(new SHIPTestToolOptions(SHIPRoles.Server), CancellationToken);

            await Context.Step(
                      "1",
                      "The test tool sends a CMI message using PAR_cmiValidInit and waits for the DUT's initial SME \"hello\" message.",
                      "The DUT replies with a CMI message and subsequently sends its initial SME \"hello\" message with phase = \"ready\".",
                      async step => {

                          await tool.SendRaw(SHIPParameters.CmiValidInitBytes, CancellationToken);

                          var hello = await tool.WaitFor<SHIPHelloMessage>(TimeSpan.FromSeconds(10));

                          step.Require(hello is not null && hello.ConnectionHello.Phase == ConnectionHelloPhase.Ready,
                                       "the device did not announce itself as ready");

                      });

            await Context.Step(
                      "2",
                      "The test tool sends an SME \"hello\" message using PAR_helloStatePending.",
                      "The DUT maintains the connection.",
                      async step => {

                          await tool.Send(SHIPParameters.HelloStatePending, CancellationToken);

                          step.Require(!tool.DUTClosed,
                                       $"the device closed the connection over a pending update it was supposed to ignore ({tool.CloseReason})");

                      });

            await Context.Step(
                      "3",
                      "The test tool waits for 30 seconds and subsequently sends an SME \"hello\" message using PAR_helloStateReady.",
                      "The DUT sends an SME \"protocol handshake\" message.",
                      async step => {

                          await tool.Advance(TimeSpan.FromSeconds(30), CancellationToken);

                          step.Require(!tool.DUTClosed,
                                       $"the device gave up while waiting, after {tool.Elapsed.TotalSeconds:0.#} s");

                          await tool.Send(SHIPParameters.HelloStateReady, CancellationToken);

                          step.Require(await TC_SHIP_HELLO_001.EnteredProtocolHandshake(tool, Context, CancellationToken),
                                       "the device did not enter the protocol handshake");

                      });

        }

    }

    #endregion

}
