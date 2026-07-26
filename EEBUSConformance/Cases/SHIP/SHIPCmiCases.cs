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

    #region TC_SHIP_CMI_001

    /// <summary>
    /// A server receiving something other than an init message during the
    /// connection mode initialisation answers with its own CMI message and only
    /// then closes.
    ///
    /// The order matters: the point of the CMI phase is to agree on a format
    /// before anything else is exchanged, so a server which just drops the
    /// connection leaves the client unable to tell an incompatible partner from
    /// a broken network.
    /// </summary>
    public sealed class TC_SHIP_CMI_001 : AConformanceTest
    {

        public override String Id => "TC_SHIP_CMI_001";

        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            var tool = await SHIPTestTool.Open(new SHIPTestToolOptions(SHIPRoles.Server), CancellationToken);

            await Context.Step(
                      "1",
                      "The test tool sends a CMI message using PAR_cmiInvalidMessageType.",
                      "The DUT sends a CMI message with MessageType = 0 and MessageValue = 0 (CmiHead = 0).",
                      async step => {

                          await tool.SendRaw(SHIPParameters.CmiInvalidMessageType, CancellationToken);

                          var init = await tool.WaitFor<SHIPInitMessage>(TimeSpan.FromSeconds(10));

                          step.Require(init is not null,
                                       tool.DUTClosed
                                           ? $"the device closed the connection without answering the CMI phase first ({tool.CloseReason})"
                                           : "the device did not send its own CMI message");

                      });

            await Context.Step(
                      "2",
                      "The test tool waits for a SHIP connection termination.",
                      "The underlying TCP/TLS connection is actively closed by the DUT.",
                      async step => {

                          step.Require(await tool.WaitForClose(TimeSpan.FromSeconds(10)),
                                       "the device kept the connection open after an invalid CMI message");

                      });

        }

    }

    #endregion

    #region TC_SHIP_CMI_002

    /// <summary>
    /// A client receiving something other than an init message in answer to its
    /// own closes at once, and says nothing further - it has no format to say
    /// it in.
    /// </summary>
    public sealed class TC_SHIP_CMI_002 : AConformanceTest
    {

        public override String Id => "TC_SHIP_CMI_002";

        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            var tool  = await SHIPTestTool.Open(new SHIPTestToolOptions(SHIPRoles.Client), CancellationToken);
            var sent  = 0;

            await Context.Step(
                      "1",
                      "The test tool waits for an incoming SHIP connection and the initial CMI message.",
                      "The DUT establishes the connection and sends its initial CMI message with MessageType = 0 and MessageValue = 0.",
                      async step => {

                          var init = await tool.WaitFor<SHIPInitMessage>(TimeSpan.FromSeconds(10));

                          step.Require(init is not null,
                                       "the device did not start the connection mode initialisation");

                          sent = tool.Received.Count;

                      });

            await Context.Step(
                      "2",
                      "The test tool sends a CMI message using PAR_cmiInvalidMessageType.",
                      "The DUT does NOT send any further SHIP messages and actively closes the connection immediately.",
                      async step => {

                          await tool.SendRaw(SHIPParameters.CmiInvalidMessageType, CancellationToken);

                          step.Require(tool.Received.Count == sent,
                                       $"the device answered with {tool.Received.Count - sent} further message(s) instead of closing silently");

                          step.Require(await tool.WaitForClose(TimeSpan.FromSeconds(10)),
                                       "the device kept the connection open after an invalid CMI message");

                      });

        }

    }

    #endregion

    #region TC_SHIP_CMI_003

    /// <summary>
    /// The CmiTimeout of a server, from both sides: it may not give up before
    /// ten seconds, and it has to give up before thirty.
    /// </summary>
    public sealed class TC_SHIP_CMI_003 : AConformanceTest
    {

        public override String Id => "TC_SHIP_CMI_003";

        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            var tool = await SHIPTestTool.Open(new SHIPTestToolOptions(SHIPRoles.Server), CancellationToken);

            await Context.Step(
                      "1",
                      "The test tool waits for 7 seconds.",
                      "The underlying TCP/TLS connection is still active.",
                      async step => {

                          await tool.Advance(TimeSpan.FromSeconds(7), CancellationToken);

                          step.Require(!tool.DUTClosed,
                                       $"the device gave up after {tool.Elapsed.TotalSeconds:0.#} s, below the lower bound of the CmiTimeout");

                      });

            await Context.Step(
                      "2",
                      "The test tool waits for a SHIP connection termination for up to 25 seconds " +
                      "(total elapsed time: 32 seconds since TLS handshake completion).",
                      "The underlying TCP/TLS connection is actively closed by the DUT within the timeframe.",
                      async step => {

                          step.Require(await tool.WaitForClose(TimeSpan.FromSeconds(25)),
                                       "the device waited longer than the CmiTimeout allows for a connection mode initialisation");

                          step.Observe($"closed after {tool.ClosedAt?.TotalSeconds:0.#} s");

                      });

        }

    }

    #endregion

    #region TC_SHIP_CMI_004

    /// <summary>
    /// The CmiTimeout of a client, which starts counting when it sent its own
    /// initialisation.
    /// </summary>
    public sealed class TC_SHIP_CMI_004 : AConformanceTest
    {

        public override String Id => "TC_SHIP_CMI_004";

        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            var tool = await SHIPTestTool.Open(new SHIPTestToolOptions(SHIPRoles.Client), CancellationToken);

            await Context.Step(
                      "1",
                      "The test tool waits for an incoming SHIP connection and the initial CMI message.",
                      "The DUT establishes the connection and sends its initial CMI message.",
                      async step => {

                          var init = await tool.WaitFor<SHIPInitMessage>(TimeSpan.FromSeconds(10));

                          step.Require(init is not null,
                                       "the device did not start the connection mode initialisation");

                      });

            await Context.Step(
                      "2",
                      "The test tool waits for 7 seconds and does NOT send any SME message.",
                      "The underlying TCP/TLS connection is still active.",
                      async step => {

                          await tool.Advance(TimeSpan.FromSeconds(7), CancellationToken);

                          step.Require(!tool.DUTClosed,
                                       $"the device gave up after {tool.Elapsed.TotalSeconds:0.#} s, below the lower bound of the CmiTimeout");

                      });

            await Context.Step(
                      "3",
                      "The test tool waits for a SHIP connection termination for up to 25 seconds.",
                      "The underlying TCP/TLS connection is actively closed by the DUT within the timeframe.",
                      async step => {

                          step.Require(await tool.WaitForClose(TimeSpan.FromSeconds(25)),
                                       "the device waited longer than the CmiTimeout allows for an answer");

                          step.Observe($"closed after {tool.ClosedAt?.TotalSeconds:0.#} s");

                      });

        }

    }

    #endregion

    #region TC_SHIP_CMI_005

    /// <summary>
    /// A CmiHead greater than zero is the way a future version of the protocol
    /// would announce itself. A server which does not understand it answers
    /// with its own CMI message - saying "zero is all I have" - and closes.
    /// </summary>
    public sealed class TC_SHIP_CMI_005 : AConformanceTest
    {

        public override String Id => "TC_SHIP_CMI_005";

        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            var tool = await SHIPTestTool.Open(new SHIPTestToolOptions(SHIPRoles.Server), CancellationToken);

            await Context.Step(
                      "1",
                      "The test tool sends a CMI message using PAR_cmiInvalidCmiHead.",
                      "The DUT sends a CMI message with MessageType = 0 and MessageValue = 0 (CmiHead = 0).",
                      async step => {

                          await tool.SendRaw(SHIPParameters.CmiInvalidCmiHead, CancellationToken);

                          var init = await tool.WaitFor<SHIPInitMessage>(TimeSpan.FromSeconds(10));

                          step.Require(init is not null,
                                       tool.DUTClosed
                                           ? $"the device closed the connection without answering the CMI phase first ({tool.CloseReason})"
                                           : "the device did not send its own CMI message");

                      });

            await Context.Step(
                      "2",
                      "The test tool waits for a SHIP connection termination for up to 10 seconds.",
                      "The underlying TCP/TLS connection is actively closed by the DUT.",
                      async step => {

                          step.Require(await tool.WaitForClose(TimeSpan.FromSeconds(10)),
                                       "the device kept the connection open after an invalid CmiHead");

                      });

        }

    }

    #endregion

    #region TC_SHIP_CMI_006

    /// <summary>
    /// The client side of the same: a CmiHead greater than zero from the server
    /// ends the connection at once.
    /// </summary>
    public sealed class TC_SHIP_CMI_006 : AConformanceTest
    {

        public override String Id => "TC_SHIP_CMI_006";

        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            var tool = await SHIPTestTool.Open(new SHIPTestToolOptions(SHIPRoles.Client), CancellationToken);

            await Context.Step(
                      "1",
                      "The test tool waits for an incoming SHIP connection and the initial CMI message.",
                      "The DUT establishes the connection and sends its initial CMI message.",
                      async step => {

                          var init = await tool.WaitFor<SHIPInitMessage>(TimeSpan.FromSeconds(10));

                          step.Require(init is not null,
                                       "the device did not start the connection mode initialisation");

                      });

            await Context.Step(
                      "2",
                      "The test tool sends a CMI message using PAR_cmiInvalidCmiHead.",
                      "The DUT actively closes the TCP/TLS connection immediately.",
                      async step => {

                          await tool.SendRaw(SHIPParameters.CmiInvalidCmiHead, CancellationToken);

                          step.Require(await tool.WaitForClose(TimeSpan.FromSeconds(10)),
                                       "the device kept the connection open after an invalid CmiHead");

                      });

        }

    }

    #endregion

}
