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

    #region TC_SHIP_ROLE_001

    /// <summary>
    /// The device accepts a connection, evaluates the connection mode
    /// initialisation of the test tool and reaches the connection data
    /// preparation state - which its "hello" message proves.
    /// </summary>
    public sealed class TC_SHIP_ROLE_001 : AConformanceTest
    {

        public override String Id => "TC_SHIP_ROLE_001";

        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            var tool = await SHIPTestTool.Open(new SHIPTestToolOptions(SHIPRoles.Server), CancellationToken);

            await Context.Step(
                      "1",
                      "The test tool waits for 7 seconds after the completion of the TLS handshake.",
                      "The underlying TCP/TLS connection is still actively established.",
                      async step => {

                          await tool.Advance(TimeSpan.FromSeconds(7), CancellationToken);

                          step.Require(!tool.DUTClosed,
                                       $"the device closed the connection after {tool.Elapsed.TotalSeconds:0.#} s, " +
                                       $"before the CmiTimeout could possibly have expired ({tool.CloseReason})");

                      });

            await Context.Step(
                      "2",
                      "The test tool sends a CMI message using PAR_cmiValidInit.",
                      "The DUT sends a CMI message with MessageType = 0 and MessageValue = 0 (CmiHead = 0).",
                      async step => {

                          await tool.SendRaw(SHIPParameters.CmiValidInitBytes, CancellationToken);

                          var init = await tool.WaitFor<SHIPInitMessage>(TimeSpan.FromSeconds(10));

                          step.Require(init is not null,
                                       "the device did not answer the connection mode initialisation");

                      });

            await Context.Step(
                      "3",
                      "The test tool waits for an incoming SHIP message.",
                      "The DUT sends an SME \"hello\" message.",
                      async step => {

                          var hello = await tool.WaitFor<SHIPHelloMessage>(TimeSpan.FromSeconds(10));

                          step.Require(hello is not null,
                                       "the device did not enter the connection data preparation state");

                          step.Observe($"phase \"{hello!.ConnectionHello.Phase}\"");

                      });

        }

    }

    #endregion

    #region TC_SHIP_ROLE_002

    /// <summary>
    /// The same the other way round: the device opens the connection, sends its
    /// own connection mode initialisation first and evaluates the test tool's
    /// answer.
    /// </summary>
    public sealed class TC_SHIP_ROLE_002 : AConformanceTest
    {

        public override String Id => "TC_SHIP_ROLE_002";

        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            var tool = await SHIPTestTool.Open(new SHIPTestToolOptions(SHIPRoles.Client), CancellationToken);

            await Context.Step(
                      "1",
                      "The test tool waits for the CMI message.",
                      "The DUT sends its CMI message with MessageType = 0 and MessageValue = 0 (CmiHead = 0).",
                      async step => {

                          var init = await tool.WaitFor<SHIPInitMessage>(TimeSpan.FromSeconds(10));

                          step.Require(init is not null,
                                       "the device did not start the connection mode initialisation");

                      });

            await Context.Step(
                      "2",
                      "The test tool waits for 7 seconds.",
                      "The underlying TCP/TLS connection is still actively established.",
                      async step => {

                          await tool.Advance(TimeSpan.FromSeconds(7), CancellationToken);

                          step.Require(!tool.DUTClosed,
                                       $"the device closed the connection after {tool.Elapsed.TotalSeconds:0.#} s ({tool.CloseReason})");

                      });

            await Context.Step(
                      "3",
                      "The test tool sends a CMI message using PAR_cmiValidInit.",
                      "The DUT sends an SME \"hello\" message.",
                      async step => {

                          await tool.SendRaw(SHIPParameters.CmiValidInitBytes, CancellationToken);

                          var hello = await tool.WaitFor<SHIPHelloMessage>(TimeSpan.FromSeconds(10));

                          step.Require(hello is not null,
                                       "the device did not enter the connection data preparation state");

                          step.Observe($"phase \"{hello!.ConnectionHello.Phase}\"");

                      });

        }

    }

    #endregion

    #region TC_SHIP_ROLE_003

    /// <summary>
    /// Role polymorphism: the device is a server towards one partner and a
    /// client towards another, at the same time.
    ///
    /// This is not a curiosity. Every energy manager is exactly this - a server
    /// to the wallbox which connects to it and a client to the grid box it
    /// connects to - so a stack which keeps one role per device rather than one
    /// per connection cannot be an energy manager at all.
    /// </summary>
    public sealed class TC_SHIP_ROLE_003 : AConformanceTest
    {

        public override String Id => "TC_SHIP_ROLE_003";

        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            // Instance A of the test tool is a pure client, so the device is
            // the server on that connection; instance B is a pure server, so
            // the device is the client on the other one.
            var towardsA = await SHIPTestTool.Open(new SHIPTestToolOptions(SHIPRoles.Server) { DUTShipId = "dut-0001" }, CancellationToken);
            var towardsB = await SHIPTestTool.Open(new SHIPTestToolOptions(SHIPRoles.Client) { DUTShipId = "dut-0001" }, CancellationToken);

            await Context.Step(
                      "1",
                      "The test tool's SHIP instance B waits passively for the DUT to actively initiate a SHIP connection.",
                      "The DUT initiates a TCP connection to the test tool's SHIP instance B within 5 minutes.",
                      async step => {

                          var init = await towardsB.WaitFor<SHIPInitMessage>(TimeSpan.FromMinutes(5));

                          step.Require(init is not null,
                                       "the device never opened a connection of its own");

                      });

            await Context.Step(
                      "2",
                      "The test tool's SHIP instance A immediately establishes a SHIP connection to the DUT. " +
                      "Both test tool instances proceed with their respective TLS and SME handshakes in parallel.",
                      "The DUT successfully establishes both connections simultaneously (acting as SME server for " +
                      "instance A, and as SME client for instance B), and both instances receive an SME \"hello\" " +
                      "message with phase = \"ready\" within 30 seconds.",
                      async step => {

                          await towardsA.SendRaw(SHIPParameters.CmiValidInitBytes, CancellationToken);
                          await towardsB.SendRaw(SHIPParameters.CmiValidInitBytes, CancellationToken);

                          var helloA = await towardsA.WaitFor<SHIPHelloMessage>(TimeSpan.FromSeconds(30),
                                                                                message => message.ConnectionHello.Phase == ConnectionHelloPhase.Ready);

                          var helloB = await towardsB.WaitFor<SHIPHelloMessage>(TimeSpan.FromSeconds(30),
                                                                                message => message.ConnectionHello.Phase == ConnectionHelloPhase.Ready);

                          step.Require(helloA is not null,
                                       "the connection on which the device is the server did not become ready");

                          step.Require(helloB is not null,
                                       "the connection on which the device is the client did not become ready");

                          step.Require(towardsA.DUT.Role == SHIPRoles.Server &&
                                       towardsB.DUT.Role == SHIPRoles.Client,
                                       "the device did not hold both roles at once");

                      });

        }

    }

    #endregion

}
