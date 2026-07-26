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

using cloud.charging.open.protocols.EEBUS.SHIP;
using cloud.charging.open.protocols.EEBUS.SPINE;
using cloud.charging.open.protocols.EEBUS.SPINE.Model;

#endregion

namespace cloud.charging.open.protocols.EEBUS.Conformance
{

    #region (class) ASHIPSessionCase

    /// <summary>
    /// The shared start of every case which begins from an established
    /// connection: open one, run the whole handshake, and hand over a device
    /// which is exchanging data.
    /// </summary>
    public abstract class ASHIPSessionCase : AConformanceTest
    {

        /// <summary>
        /// Whether a SPINE device has to sit behind the connection.
        /// </summary>
        protected virtual Boolean NeedsSPINE => false;


        /// <summary>
        /// PRE_SHIP_ConnectionEstablished, in whichever role the case wants.
        /// </summary>
        /// <param name="Context">Where the steps are written down.</param>
        /// <param name="Role">Which side of the connection the device is on.</param>
        /// <param name="CancellationToken">An optional cancellation token.</param>
        protected async Task<SHIPTestTool> Established(ConformanceContext  Context,
                                                       SHIPRoles           Role,
                                                       CancellationToken   CancellationToken)
        {

            var tool = await SHIPTestTool.Open(
                                 new SHIPTestToolOptions(Role) {
                                     AttachSPINE = NeedsSPINE
                                 },
                                 CancellationToken
                             );

            await Context.Precondition("PRE_SHIP_ConnectionEstablished",
                                       () => tool.ReachConnectionEstablished(Context.Parameters, CancellationToken));

            return tool;

        }


        /// <summary>
        /// An SME "data" message carrying a SPINE read of a node management
        /// function on entity 0, feature 0 - the payload the access methods
        /// cases use to prove that data and control messages are handled at the
        /// same time.
        /// </summary>
        /// <param name="Tool">The test tool.</param>
        /// <param name="Function">The name of a node management function.</param>
        /// <param name="MsgCounter">The message counter of the read.</param>
        protected static SHIPDataMessage SpineRead(SHIPTestTool  Tool,
                                                   String        Function,
                                                   UInt64        MsgCounter)
        {

            var source       = new FeatureAddressType { Device = "d:_i:19667_testtool",                          Entity = [ 0 ], Feature = 0 };
            var destination  = new FeatureAddressType { Device = Tool.SPINE?.DeviceAddress ?? "d:_i:19667_dut",  Entity = [ 0 ], Feature = 0 };

            var datagram     = SPINEParameters.Datagram(source,
                                                        destination,
                                                        CmdClassifierType.Read,
                                                        MsgCounter,
                                                        SPINEParameters.ReadCmd(Function));

            return new SHIPDataMessage(
                       new DataType(
                           new SHIP.HeaderType(SHIP.Version.ProtocolId),
                           new JObject(new JProperty("datagram", SPINEJSON.ToJObject(datagram)))
                       )
                   );

        }

    }

    #endregion


    #region TC_SHIP_MSG_001

    /// <summary>
    /// A message type byte and nothing else is not a SHIP message. The device
    /// may drop it or close the connection, but it may not act on it - and if
    /// it keeps the connection, it has to keep working.
    /// </summary>
    public sealed class TC_SHIP_MSG_001 : ASHIPSessionCase
    {

        public override String Id => "TC_SHIP_MSG_001";

        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            var tool    = await Established(Context, SHIPRoles.Server, CancellationToken);
            var closed  = false;

            await Context.Step(
                      "1",
                      "The test tool sends a SHIP message using PAR_msgEmptyMessageValue and waits for up to 3 seconds.",
                      "The DUT either actively closes the TCP/TLS connection, OR it silently ignores the message and " +
                      "keeps the connection open.",
                      async step => {

                          await tool.SendRaw(SHIPParameters.MsgEmptyMessageValue, CancellationToken);
                          await tool.Advance(TimeSpan.FromSeconds(3), CancellationToken);

                          closed = tool.DUTClosed;

                          step.Observe(closed
                                           ? "the device closed the connection"
                                           : "the device ignored the message and kept the connection");

                      });

            if (closed)
                return;

            await Context.Step(
                      "2",
                      "The test tool sends an SME \"access methods request\" message.",
                      "The DUT replies with an SME \"access methods\" message.",
                      async step => {

                          await tool.Send(new SHIPAccessMethodsRequestMessage(), CancellationToken);

                          step.Require(await tool.WaitFor<SHIPAccessMethodsMessage>(TimeSpan.FromSeconds(10)) is not null,
                                       tool.DUTClosed
                                           ? $"the device closed the connection instead of answering ({tool.CloseReason})"
                                           : "the device did not answer the access methods request");

                      });

        }

    }

    #endregion

    #region TC_SHIP_MSG_002

    /// <summary>
    /// The same for a message type nobody has defined - which is how a future
    /// version of SHIP will look to a device shipped today.
    /// </summary>
    public sealed class TC_SHIP_MSG_002 : ASHIPSessionCase
    {

        public override String Id => "TC_SHIP_MSG_002";

        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            var tool    = await Established(Context, SHIPRoles.Server, CancellationToken);
            var closed  = false;

            await Context.Step(
                      "1",
                      "The test tool sends a SHIP message using PAR_msgUnknownMessageType and waits for up to 3 seconds.",
                      "The DUT either actively closes the TCP/TLS connection, OR silently ignores the message and " +
                      "keeps the connection open.",
                      async step => {

                          await tool.SendRaw(SHIPParameters.MsgUnknownMessageType, CancellationToken);
                          await tool.Advance(TimeSpan.FromSeconds(3), CancellationToken);

                          closed = tool.DUTClosed;

                          step.Observe(closed
                                           ? "the device closed the connection"
                                           : "the device ignored the message and kept the connection");

                      });

            if (closed)
                return;

            await Context.Step(
                      "2",
                      "The test tool sends an SME \"access methods request\" message.",
                      "The DUT replies with an SME \"access methods\" message.",
                      async step => {

                          await tool.Send(new SHIPAccessMethodsRequestMessage(), CancellationToken);

                          step.Require(await tool.WaitFor<SHIPAccessMethodsMessage>(TimeSpan.FromSeconds(10)) is not null,
                                       tool.DUTClosed
                                           ? $"the device closed the connection instead of answering ({tool.CloseReason})"
                                           : "the device did not answer the access methods request");

                      });

        }

    }

    #endregion

    #region TC_SHIP_MSG_003

    /// <summary>
    /// JSON is JSON: a parser which only accepts minified payloads is not a
    /// JSON parser. The test sends a request padded with every structural
    /// whitespace character the JSON specification allows.
    /// </summary>
    public sealed class TC_SHIP_MSG_003 : ASHIPSessionCase
    {

        public override String Id => "TC_SHIP_MSG_003";

        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            var tool = await Established(Context, SHIPRoles.Server, CancellationToken);

            await Context.Step(
                      "1",
                      "The test tool sends an SME \"access methods request\" message whose JSON structure is " +
                      "artificially formatted with spaces, horizontal tabs, LFs and CRs before and after the " +
                      "structural characters.",
                      "The DUT replies with an SME \"access methods\" message.",
                      async step => {

                          var padded = SHIPTestTool.WithWhitespace(new SHIPAccessMethodsRequestMessage());

                          step.Observe($"{padded.Length} bytes instead of the minified {new SHIPAccessMethodsRequestMessage().ToByteArray().Length - 1}");

                          await tool.SendText(SHIPMessageTypes.CONTROL, padded, CancellationToken);

                          step.Require(await tool.WaitFor<SHIPAccessMethodsMessage>(TimeSpan.FromSeconds(10)) is not null,
                                       tool.DUTClosed
                                           ? $"the device closed the connection over the formatting ({tool.CloseReason})"
                                           : "the device did not answer a request which was only formatted differently");

                      });

        }

    }

    #endregion

    #region TC_SHIP_PIN_001

    /// <summary>
    /// A device which does not want a PIN says so and moves on.
    /// </summary>
    public sealed class TC_SHIP_PIN_001 : AConformanceTest
    {

        public override String Id => "TC_SHIP_PIN_001";

        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            var tool = await SHIPTestTool.Open(new SHIPTestToolOptions(SHIPRoles.Server), CancellationToken);

            await Context.Precondition("PRE_SHIP_Protocol_Handshake_Completed",
                                       () => tool.ReachProtocolHandshakeCompleted(Context.Parameters, CancellationToken));

            await Context.Step(
                      "1",
                      "In parallel, the test tool sends an SME \"PIN state\" message using PAR_pinStateNone and " +
                      "waits for an incoming SME \"PIN state\" message from the DUT.",
                      "The DUT sends an SME \"PIN state\" message with pinState = \"none\".",
                      async step => {

                          var pinState = await tool.WaitFor<SHIPPinStateMessage>(TimeSpan.FromSeconds(10));

                          step.Require(pinState is not null,
                                       "the device did not announce its PIN state");

                          step.Require(pinState!.ConnectionPinState.PinState == PinState.None,
                                       $"the device announced pinState \"{pinState.ConnectionPinState.PinState}\" " +
                                       $"although it was configured without a PIN");

                          await tool.Send(SHIPParameters.PinStateNone, CancellationToken);

                      });

            await Context.Step(
                      "2",
                      "The test tool sends an SME \"access methods request\" message.",
                      "The DUT replies with an SME \"access methods\" message.",
                      async step => {

                          // The device asks as well; answering it is what the
                          // specification expects of any correct partner and is
                          // not part of what is being verified here.
                          if (await tool.WaitFor<SHIPAccessMethodsRequestMessage>(TimeSpan.FromSeconds(10)) is not null)
                              await tool.Send(SHIPParameters.AccessMethods(tool.Options.ToolShipId), CancellationToken);

                          await tool.Send(new SHIPAccessMethodsRequestMessage(), CancellationToken);

                          step.Require(await tool.WaitFor<SHIPAccessMethodsMessage>(TimeSpan.FromSeconds(10)) is not null,
                                       "the device did not reach the data exchange state");

                      });

        }

    }

    #endregion

    #region TC_SHIP_TERM_001

    /// <summary>
    /// A device announcing a termination has to keep its own word: it names a
    /// maxTime, and it closes no later than that even if nobody confirms.
    /// </summary>
    public sealed class TC_SHIP_TERM_001 : ASHIPSessionCase
    {

        public override String Id => "TC_SHIP_TERM_001";

        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            var tool     = await Established(Context, SHIPRoles.Server, CancellationToken);
            var maxTime  = TimeSpan.Zero;

            await Context.Step(
                      "1",
                      "The tester configures the DUT to revoke the trust for the test tool.",
                      "The DUT sends an SME \"close\" message with phase = \"announce\", a valid \"reason\", and " +
                      "announces a \"maxTime\" value.",
                      async step => {

                          // Revoking the trust is what a tester does through the
                          // user interface; on this stack it is the application
                          // asking the connection to close.
                          await tool.DUT.CloseAsync(ConnectionCloseReasons.RemovedConnection, CancellationToken);

                          var close = await tool.WaitFor<SHIPCloseMessage>(TimeSpan.FromSeconds(10));

                          step.Require(close is not null,
                                       "the device dropped the connection without announcing the termination");

                          step.Require(close!.ConnectionClose.Phase == ConnectionClosePhases.Announce,
                                       $"the device announced the phase \"{close.ConnectionClose.Phase}\" instead of \"announce\"");

                          step.Require(close.ConnectionClose.Reason is not null,
                                       "the device announced no reason for the termination");

                          step.Require(close.ConnectionClose.MaxTime.HasValue,
                                       "the device announced no maxTime, so the partner cannot tell how long it may still answer");

                          maxTime = TimeSpan.FromMilliseconds(close.ConnectionClose.MaxTime!.Value);

                          step.Observe($"reason \"{close.ConnectionClose.Reason}\", maxTime {maxTime.TotalSeconds:0.#} s");

                      });

            await Context.Step(
                      "2",
                      "The test tool silently ignores the message (does not send a confirm message) and waits.",
                      "The DUT actively closes the TCP/TLS connection no later than the announced \"maxTime\" + 2 s.",
                      async step => {

                          step.Require(await tool.WaitForClose(maxTime + TimeSpan.FromSeconds(2)),
                                       $"the device kept the connection open longer than the {maxTime.TotalSeconds:0.#} s it announced");

                          step.Observe($"closed {tool.ClosedAt?.TotalSeconds:0.#} s after the connection opened");

                      });

        }

    }

    #endregion

    #region TC_SHIP_AM_001

    /// <summary>
    /// The access methods message carries the identifier the device announces
    /// over mDNS, and it has to be the same one - it is how two nodes agree
    /// that the thing they discovered and the thing they are talking to are one
    /// device.
    /// </summary>
    public sealed class TC_SHIP_AM_001 : ASHIPSessionCase
    {

        public override String Id => "TC_SHIP_AM_001";

        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            var tool = await Established(Context, SHIPRoles.Server, CancellationToken);

            await Context.Step(
                      "1",
                      "The test tool sends an SME \"access methods request\" message to the DUT.",
                      "The DUT replies with an SME \"access methods\" message whose accessMethods.id is present and " +
                      "equals the DUT's SHIP ID.",
                      async step => {

                          await tool.Send(new SHIPAccessMethodsRequestMessage(), CancellationToken);

                          var methods = await tool.WaitFor<SHIPAccessMethodsMessage>(TimeSpan.FromSeconds(10));

                          step.Require(methods is not null,
                                       tool.DUTClosed
                                           ? $"the device closed the connection instead of answering ({tool.CloseReason})"
                                           : "the device did not answer the access methods request");

                          var expected = Context.Parameters.ShipId ?? tool.Options.DUTShipId;

                          step.Require(methods!.AccessMethods.Id.ToString() == expected,
                                       $"the device announced the SHIP identifier \"{methods.AccessMethods.Id}\", " +
                                       $"but is known as \"{expected}\"");

                      });

        }

    }

    #endregion

    #region TC_SHIP_AMDATA_001

    /// <summary>
    /// Three messages in a row, without waiting: a SPINE read, an access
    /// methods request, another SPINE read. All three have to be answered.
    ///
    /// This is the case which catches the tempting design: one queue, one
    /// message at a time, the next one only after the previous is answered.
    /// It works perfectly until a partner asks two things at once, which is
    /// exactly what an energy manager does on every new connection.
    /// </summary>
    public sealed class TC_SHIP_AMDATA_001 : ASHIPSessionCase
    {

        public override String Id => "TC_SHIP_AMDATA_001";

        protected override Boolean NeedsSPINE => true;

        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            var tool = await Established(Context, SHIPRoles.Server, CancellationToken);

            await Context.Step(
                      "1",
                      "The test tool sends, in immediate succession and without waiting for responses: an SME \"data\" " +
                      "message with a SPINE detailed discovery read, an SME \"access methods request\" message, and an " +
                      "SME \"data\" message with a SPINE use case discovery read.",
                      "Within 10 s, the DUT sends the SME \"access methods\" reply AND both corresponding SPINE reply " +
                      "messages, in any order.",
                      async step => {

                          await tool.Send(SpineRead(tool, SPINENodeManagement.DetailedDiscoveryData, 4711), CancellationToken);
                          await tool.Send(new SHIPAccessMethodsRequestMessage(),                            CancellationToken);
                          await tool.Send(SpineRead(tool, SPINENodeManagement.UseCaseData,           4712), CancellationToken);

                          await tool.Advance(TimeSpan.FromSeconds(10), CancellationToken);

                          var methods = tool.Received.OfType<SHIPAccessMethodsMessage>().Any();
                          var replies = tool.SPINEDatagrams.
                                            Where(datagram => datagram.Header?.CmdClassifier == CmdClassifierType.Reply).
                                            ToList();

                          step.Require(methods,
                                       tool.DUTClosed
                                           ? $"the device closed the connection over the three messages ({tool.CloseReason})"
                                           : "the device did not answer the access methods request");

                          step.Require(replies.Any(datagram => datagram.Header?.MsgCounterReference == 4711),
                                       "the device did not answer the detailed discovery read");

                          step.Require(replies.Any(datagram => datagram.Header?.MsgCounterReference == 4712),
                                       "the device did not answer the use case discovery read");

                      });

        }

    }

    #endregion

    #region TC_SHIP_AMDATA_002

    /// <summary>
    /// The mirror image: the device is waiting for an answer of its own, and
    /// still has to keep working while it waits.
    /// </summary>
    public sealed class TC_SHIP_AMDATA_002 : ASHIPSessionCase
    {

        public override String Id => "TC_SHIP_AMDATA_002";

        protected override Boolean NeedsSPINE => true;

        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            var tool = await SHIPTestTool.Open(
                                 new SHIPTestToolOptions(SHIPRoles.Server) { AttachSPINE = true },
                                 CancellationToken
                             );

            // The connection is brought up without answering the device's own
            // access methods request, because leaving it open for eight seconds
            // is the whole point of this case.
            await Context.Precondition("PRE_SHIP_ConnectionEstablished",
                                       () => tool.ReachProtocolHandshakeCompleted(Context.Parameters, CancellationToken));

            await Context.Step(
                      "1",
                      "The test tool waits for an SME \"access methods request\" message of the DUT.",
                      "Within 10 s, the DUT sends the SME \"access methods request\" message.",
                      async step => {

                          if (await tool.WaitFor<SHIPPinStateMessage>(TimeSpan.FromSeconds(10)) is not null)
                              await tool.Send(SHIPParameters.PinStateNone, CancellationToken);

                          step.Require(await tool.WaitFor<SHIPAccessMethodsRequestMessage>(TimeSpan.FromSeconds(10)) is not null,
                                       "the device did not ask for the access methods, although it declared PAR_queryAccessMethods = \"yes\"");

                      });

            await Context.Step(
                      "2",
                      "The test tool sends a SPINE detailed discovery read, an access methods request and a SPINE use " +
                      "case discovery read in immediate succession, and answers the DUT's own access methods request " +
                      "only 8 s later.",
                      "The DUT sends the SME \"access methods\" reply and both SPINE replies within 10 s of the start " +
                      "of this step.",
                      async step => {

                          await tool.Send(SpineRead(tool, SPINENodeManagement.DetailedDiscoveryData, 4711), CancellationToken);
                          await tool.Send(new SHIPAccessMethodsRequestMessage(),                            CancellationToken);
                          await tool.Send(SpineRead(tool, SPINENodeManagement.UseCaseData,           4712), CancellationToken);

                          await tool.Advance(TimeSpan.FromSeconds(8), CancellationToken);

                          await tool.Send(SHIPParameters.AccessMethods(tool.Options.ToolShipId), CancellationToken);

                          await tool.Advance(TimeSpan.FromSeconds(2), CancellationToken);

                          var replies = tool.SPINEDatagrams.
                                            Where(datagram => datagram.Header?.CmdClassifier == CmdClassifierType.Reply).
                                            ToList();

                          step.Require(tool.Received.OfType<SHIPAccessMethodsMessage>().Any(),
                                       tool.DUTClosed
                                           ? $"the device closed the connection ({tool.CloseReason})"
                                           : "the device did not answer the access methods request while its own was still open");

                          step.Require(replies.Any(datagram => datagram.Header?.MsgCounterReference == 4711),
                                       "the device did not answer the detailed discovery read while its own access methods request was open");

                          step.Require(replies.Any(datagram => datagram.Header?.MsgCounterReference == 4712),
                                       "the device did not answer the use case discovery read while its own access methods request was open");

                      });

        }

    }

    #endregion

    #region TC_SHIP_AMDATA_003

    /// <summary>
    /// And the third direction: an access methods request arriving while the
    /// device is waiting for a SPINE answer of its own.
    /// </summary>
    public sealed class TC_SHIP_AMDATA_003 : ASHIPSessionCase
    {

        public override String Id => "TC_SHIP_AMDATA_003";

        protected override Boolean NeedsSPINE => true;

        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            var tool = await Established(Context, SHIPRoles.Server, CancellationToken);

            var read = (DatagramType?) null;

            await Context.Step(
                      "1",
                      "The test tool waits for an SME \"data\" message containing a SPINE detailed discovery read of the DUT.",
                      "Within 10 s, the DUT sends the SPINE detailed discovery read command.",
                      async step => {

                          // A client asks a new partner what it is; on this
                          // stack the application starts that, so the tool does.
                          if (tool.SPINE is not null)
                          {

                              var remote = tool.SPINE.RemoteDeviceForSKI("ski-of-testtool");

                              if (remote is not null)
                                  _ = tool.SPINE.NodeManagement.RequestDetailedDiscovery(remote, CancellationToken);

                          }

                          await tool.Advance(TimeSpan.FromSeconds(10), CancellationToken);

                          read = tool.SPINEDatagrams.
                                     FirstOrDefault(datagram => datagram.Header?.CmdClassifier == CmdClassifierType.Read &&
                                                                datagram.Payload?.Cmd?.Any(cmd => cmd.DataFunction == SPINENodeManagement.DetailedDiscoveryData) == true);

                          step.Require(read is not null,
                                       "the device did not ask the new partner for its detailed discovery");

                      });

            await Context.Step(
                      "2",
                      "The test tool sends an SME \"access methods request\" message and then waits for up to 8 s.",
                      "The DUT sends the SME \"access methods\" reply.",
                      async step => {

                          var before = tool.Received.OfType<SHIPAccessMethodsMessage>().Count();

                          await tool.Send(new SHIPAccessMethodsRequestMessage(), CancellationToken);
                          await tool.Advance(TimeSpan.FromSeconds(8), CancellationToken);

                          step.Require(tool.Received.OfType<SHIPAccessMethodsMessage>().Count() > before,
                                       tool.DUTClosed
                                           ? $"the device closed the connection ({tool.CloseReason})"
                                           : "the device postponed the access methods request while its own SPINE read was open");

                      });

            await Context.Step(
                      "3",
                      "The test tool sends an SME \"data\" message containing a SPINE detailed discovery reply.",
                      "The underlying TCP/TLS connection is still actively established.",
                      async step => {

                          step.Require(!tool.DUTClosed,
                                       $"the device closed the connection ({tool.CloseReason})");

                      });

        }

    }

    #endregion

    #region TC_SHIP_AMDATA_004

    /// <summary>
    /// A methodology safeguard rather than a protocol rule: a device declaring
    /// that it never asks for the access methods must not ask.
    /// </summary>
    public sealed class TC_SHIP_AMDATA_004 : ASHIPSessionCase
    {

        public override String Id => "TC_SHIP_AMDATA_004";

        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            var tool = await Established(Context, SHIPRoles.Server, CancellationToken);

            await Context.Step(
                      "1",
                      "The test tool waits for an SME \"access methods request\" message of the DUT for 120 s.",
                      "Within 120 s, the DUT sends no SME \"access methods request\" message and the connection is " +
                      "still actively established.",
                      async step => {

                          var before = tool.Received.OfType<SHIPAccessMethodsRequestMessage>().Count();

                          await tool.Advance(TimeSpan.FromSeconds(120), CancellationToken);

                          step.Require(tool.Received.OfType<SHIPAccessMethodsRequestMessage>().Count() == before,
                                       "the device asked for the access methods although it declared PAR_queryAccessMethods = \"no\"");

                          step.Require(!tool.DUTClosed,
                                       "the device closed the connection while it was being watched");

                      });

        }

    }

    #endregion

}
