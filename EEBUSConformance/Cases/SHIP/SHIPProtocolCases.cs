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

    #region TC_SHIP_PROT_001

    /// <summary>
    /// The three way protocol handshake with the device as server: it hears the
    /// client's maximum, selects JSON-UTF8 and a version no higher than the one
    /// it was offered, and accepts the confirmation.
    /// </summary>
    public sealed class TC_SHIP_PROT_001 : AConformanceTest
    {

        public override String Id => "TC_SHIP_PROT_001";

        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            var tool = await SHIPTestTool.Open(new SHIPTestToolOptions(SHIPRoles.Server), CancellationToken);

            await Context.Precondition("PRE_SHIP_Hello_Completed",
                                       () => tool.ReachHelloCompleted(CancellationToken));

            await Context.Step(
                      "1",
                      "The test tool sends an SME \"protocol handshake\" message using PAR_protAnnounceMax.",
                      "The DUT responds with handshakeType = \"select\", containing the format \"JSON-UTF8\" and a " +
                      "SHIP specification version which does not exceed the version announced by the test tool.",
                      async step => {

                          await tool.Send(SHIPParameters.ProtocolAnnounceMax(Context.Parameters), CancellationToken);

                          var select = await tool.WaitFor<SHIPHandshakeMessage>(TimeSpan.FromSeconds(10));

                          step.Require(select is not null,
                                       "the device did not answer the protocol handshake proposal");

                          var handshake = select!.MessageProtocolHandshake;

                          step.Require(handshake.HandshakeType == ProtocolHandshakeTypeTypes.select,
                                       $"the device answered with handshakeType \"{handshake.HandshakeType}\" instead of a selection");

                          step.Require(handshake.Formats.Contains(MessageProtocolFormat.JSON_UTF8),
                                       "the device did not select JSON-UTF8, the one format every SHIP node has to support");

                          var offered = Context.Parameters.TestToolShipVersion.Split('.');
                          var major   = UInt16.Parse(offered[0]);
                          var minor   = offered.Length > 1 ? UInt16.Parse(offered[1]) : (UInt16) 0;

                          step.Require(handshake.Version.Major < major ||
                                      (handshake.Version.Major == major && handshake.Version.Minor <= minor),
                                       $"the device selected SHIP {handshake.Version.Major}.{handshake.Version.Minor}, " +
                                       $"which is higher than the {major}.{minor} it was offered");

                          step.Observe($"selected SHIP {handshake.Version.Major}.{handshake.Version.Minor}, JSON-UTF8");

                      });

            await Context.Step(
                      "2",
                      "The test tool sends the identical \"select\" message to confirm the selection.",
                      "The DUT sends an SME \"PIN state\" message.",
                      async step => {

                          var select = tool.Received.OfType<SHIPHandshakeMessage>().Last();

                          await tool.Send(new SHIPHandshakeMessage(select.MessageProtocolHandshake), CancellationToken);

                          step.Require(await tool.WaitFor<SHIPPinStateMessage>(TimeSpan.FromSeconds(10)) is not null,
                                       "the device did not proceed to the PIN verification");

                      });

        }

    }

    #endregion

    #region TC_SHIP_PROT_002

    /// <summary>
    /// The same handshake with the device as client: it announces its maximum
    /// by itself and echoes the server's selection back.
    /// </summary>
    public sealed class TC_SHIP_PROT_002 : AConformanceTest
    {

        public override String Id => "TC_SHIP_PROT_002";

        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            var tool     = await SHIPTestTool.Open(new SHIPTestToolOptions(SHIPRoles.Client), CancellationToken);
            var announce = (SHIPHandshakeMessage?) null;

            await Context.Precondition("PRE_SHIP_Hello_Completed",
                                       () => tool.ReachHelloCompleted(CancellationToken));

            await Context.Step(
                      "1",
                      "The test tool waits for an incoming SME \"protocol handshake\" message from the DUT.",
                      "The DUT sends handshakeType = \"announceMax\" offering the format \"JSON-UTF8\" and the " +
                      "SHIP specification version defined in PAR_shipVersion.",
                      async step => {

                          announce = await tool.WaitFor<SHIPHandshakeMessage>(TimeSpan.FromSeconds(10));

                          step.Require(announce is not null,
                                       "the device did not start the protocol handshake");

                          var handshake = announce!.MessageProtocolHandshake;

                          step.Require(handshake.HandshakeType == ProtocolHandshakeTypeTypes.announceMax,
                                       $"the device started with handshakeType \"{handshake.HandshakeType}\"");

                          step.Require(handshake.Formats.Contains(MessageProtocolFormat.JSON_UTF8),
                                       "the device did not offer JSON-UTF8");

                          step.Require($"{handshake.Version.Major}.{handshake.Version.Minor}" == Context.Parameters.ShipVersion,
                                       $"the device announced SHIP {handshake.Version.Major}.{handshake.Version.Minor}, " +
                                       $"but declared PAR_shipVersion = {Context.Parameters.ShipVersion}");

                      });

            await Context.Step(
                      "2",
                      "The test tool responds with handshakeType = \"select\", choosing \"JSON-UTF8\" and the greatest " +
                      "SHIP specification version supported by both sides.",
                      "The DUT sends the identical \"select\" message back and subsequently sends an SME \"PIN state\" message.",
                      async step => {

                          await tool.Send(SHIPParameters.ProtocolSelect(announce!.MessageProtocolHandshake, Context.Parameters),
                                          CancellationToken);

                          var confirm = await tool.WaitFor<SHIPHandshakeMessage>(TimeSpan.FromSeconds(10));

                          step.Require(confirm is not null,
                                       "the device did not confirm the selection");

                          step.Require(confirm!.MessageProtocolHandshake.HandshakeType == ProtocolHandshakeTypeTypes.select,
                                       $"the device confirmed with handshakeType \"{confirm.MessageProtocolHandshake.HandshakeType}\"");

                          step.Require(await tool.WaitFor<SHIPPinStateMessage>(TimeSpan.FromSeconds(10)) is not null,
                                       "the device did not proceed to the PIN verification");

                      });

        }

    }

    #endregion

    #region TC_SHIP_PROT_003

    /// <summary>
    /// A server whose Wait timer expires during the protocol handshake says so
    /// with error type 1 and closes.
    /// </summary>
    public sealed class TC_SHIP_PROT_003 : AConformanceTest
    {

        public override String Id => "TC_SHIP_PROT_003";

        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            var tool = await SHIPTestTool.Open(new SHIPTestToolOptions(SHIPRoles.Server), CancellationToken);

            await Context.Step(
                      "1",
                      "The test tool completes the CMI and SME \"hello\" phases.",
                      "The DUT sends its final SME \"hello\" message with phase = \"ready\" and waits passively.",
                      async step => {

                          await tool.ReachHelloCompleted(CancellationToken);

                          step.Require(!tool.DUTClosed,
                                       "the device closed the connection during the hello phase");

                      });

            await Context.Step(
                      "2",
                      "The test tool waits for up to 12 seconds and does NOT send any SME message.",
                      "The DUT sends an SME \"protocol handshake error\" message with error = 1 (timeout) and " +
                      "actively closes the TCP/TLS connection.",
                      async step => {

                          var error = await tool.WaitFor<SHIPHandshakeErrorMessage>(TimeSpan.FromSeconds(12));

                          step.Require(error is not null,
                                       "the device did not report the expired Wait timer");

                          step.Require(error!.MessageProtocolHandshakeError.Error == (Byte) MessageProtocolHandshakeErrors.Timeout,
                                       $"the device reported error {error.MessageProtocolHandshakeError.Error} instead of 1 (timeout)");

                          step.Require(await tool.WaitForClose(TimeSpan.FromSeconds(2)),
                                       "the device reported the timeout but kept the connection open");

                      });

        }

    }

    #endregion

    #region TC_SHIP_PROT_004

    /// <summary>
    /// The same for a client waiting for the server's selection.
    /// </summary>
    public sealed class TC_SHIP_PROT_004 : AConformanceTest
    {

        public override String Id => "TC_SHIP_PROT_004";

        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            var tool = await SHIPTestTool.Open(new SHIPTestToolOptions(SHIPRoles.Client), CancellationToken);

            await Context.Precondition("PRE_SHIP_Hello_Completed",
                                       () => tool.ReachHelloCompleted(CancellationToken));

            await Context.Step(
                      "1",
                      "The test tool waits for an incoming SME \"protocol handshake\" message from the DUT.",
                      "The DUT sends handshakeType = \"announceMax\" offering \"JSON-UTF8\".",
                      async step => {

                          var announce = await tool.WaitFor<SHIPHandshakeMessage>(TimeSpan.FromSeconds(10));

                          step.Require(announce is not null,
                                       "the device did not start the protocol handshake");

                      });

            await Context.Step(
                      "2",
                      "The test tool completely stops sending any SHIP messages and waits for up to 12 seconds.",
                      "The DUT sends an SME \"protocol handshake error\" message with error = 1 (timeout) and " +
                      "actively closes the TCP/TLS connection.",
                      async step => {

                          var error = await tool.WaitFor<SHIPHandshakeErrorMessage>(TimeSpan.FromSeconds(12));

                          step.Require(error is not null,
                                       "the device did not report the expired Wait timer");

                          step.Require(error!.MessageProtocolHandshakeError.Error == (Byte) MessageProtocolHandshakeErrors.Timeout,
                                       $"the device reported error {error.MessageProtocolHandshakeError.Error} instead of 1 (timeout)");

                          step.Require(await tool.WaitForClose(TimeSpan.FromSeconds(2)),
                                       "the device reported the timeout but kept the connection open");

                      });

        }

    }

    #endregion

    #region TC_SHIP_PROT_005

    /// <summary>
    /// A hello message where a protocol handshake belongs: error type 2.
    ///
    /// The hello is the interesting choice for this test. It is a perfectly
    /// valid SHIP message which was perfectly valid one state earlier, so a
    /// state machine which checks "can I parse this" rather than "may this
    /// arrive now" sails straight through it.
    /// </summary>
    public sealed class TC_SHIP_PROT_005 : AConformanceTest
    {

        public override String Id => "TC_SHIP_PROT_005";

        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            var tool = await SHIPTestTool.Open(new SHIPTestToolOptions(SHIPRoles.Server), CancellationToken);

            await Context.Precondition("PRE_SHIP_Hello_Completed",
                                       () => tool.ReachHelloCompleted(CancellationToken));

            await Context.Step(
                      "1",
                      "The test tool sends an SME \"hello\" message using PAR_helloStateReady.",
                      "The DUT sends an SME \"protocol handshake error\" message with error = 2 (unexpected message) " +
                      "and actively closes the TCP/TLS connection.",
                      async step => {

                          await tool.Send(SHIPParameters.HelloStateReady, CancellationToken);

                          var error = await tool.WaitFor<SHIPHandshakeErrorMessage>(TimeSpan.FromSeconds(10));

                          step.Require(error is not null,
                                       "the device did not refuse a message which may not arrive in this state");

                          step.Require(error!.MessageProtocolHandshakeError.Error == (Byte) MessageProtocolHandshakeErrors.UnexpectedMessage,
                                       $"the device reported error {error.MessageProtocolHandshakeError.Error} instead of 2 (unexpected message)");

                          step.Require(await tool.WaitForClose(TimeSpan.FromSeconds(2)),
                                       "the device reported the unexpected message but kept the connection open");

                      });

        }

    }

    #endregion

    #region TC_SHIP_PROT_006

    /// <summary>
    /// The client side of the same rule.
    /// </summary>
    public sealed class TC_SHIP_PROT_006 : AConformanceTest
    {

        public override String Id => "TC_SHIP_PROT_006";

        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            var tool = await SHIPTestTool.Open(new SHIPTestToolOptions(SHIPRoles.Client), CancellationToken);

            await Context.Precondition("PRE_SHIP_Hello_Completed",
                                       () => tool.ReachHelloCompleted(CancellationToken));

            await Context.Step(
                      "1",
                      "The test tool waits for an incoming SME \"protocol handshake\" message from the DUT.",
                      "The DUT sends handshakeType = \"announceMax\" offering \"JSON-UTF8\".",
                      async step => {

                          var announce = await tool.WaitFor<SHIPHandshakeMessage>(TimeSpan.FromSeconds(10));

                          step.Require(announce is not null,
                                       "the device did not start the protocol handshake");

                      });

            await Context.Step(
                      "2",
                      "The test tool sends an SME \"hello\" message using PAR_helloStateReady.",
                      "The DUT sends an SME \"protocol handshake error\" message with error = 2 (unexpected message) " +
                      "and actively closes the TCP/TLS connection.",
                      async step => {

                          await tool.Send(SHIPParameters.HelloStateReady, CancellationToken);

                          var error = await tool.WaitFor<SHIPHandshakeErrorMessage>(TimeSpan.FromSeconds(10));

                          step.Require(error is not null,
                                       "the device did not refuse a message which may not arrive in this state");

                          step.Require(error!.MessageProtocolHandshakeError.Error == (Byte) MessageProtocolHandshakeErrors.UnexpectedMessage,
                                       $"the device reported error {error.MessageProtocolHandshakeError.Error} instead of 2 (unexpected message)");

                          step.Require(await tool.WaitForClose(TimeSpan.FromSeconds(2)),
                                       "the device reported the unexpected message but kept the connection open");

                      });

        }

    }

    #endregion

}
