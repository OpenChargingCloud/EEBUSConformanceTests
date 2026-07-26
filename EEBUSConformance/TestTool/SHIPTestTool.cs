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

using System.Text;

using Microsoft.Extensions.Time.Testing;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using cloud.charging.open.protocols.EEBUS.SHIP;
using cloud.charging.open.protocols.EEBUS.SPINE;
using cloud.charging.open.protocols.EEBUS.SPINE.Model;

#endregion

namespace cloud.charging.open.protocols.EEBUS.Conformance
{

    #region (class) SHIPTestToolOptions

    /// <summary>
    /// How a SHIP conversation with the device under test is set up.
    /// </summary>
    /// <param name="DUTRole">Which side of the connection the device is on.</param>
    public sealed class SHIPTestToolOptions(SHIPRoles DUTRole)
    {

        /// <summary>Which side of the connection the device is on.</summary>
        public SHIPRoles  DUTRole              { get; }      = DUTRole;

        /// <summary>
        /// PRE_SHIP_Mutual_Trust: whether the device already trusts the test
        /// tool, and therefore may announce "ready" right away.
        /// </summary>
        public Boolean    MutualTrust          { get; init; } = true;

        /// <summary>
        /// Whether the device is willing to wait for a trust decision at all.
        /// </summary>
        public Boolean    AllowWaitingForTrust { get; init; } = true;

        /// <summary>The SHIP identifier the device announces.</summary>
        public String     DUTShipId            { get; init; } = "dut-0001";

        /// <summary>The SHIP identifier the test tool announces.</summary>
        public String     ToolShipId           { get; init; } = "testtool-0001";

        /// <summary>
        /// Whether a SPINE device sits behind the SHIP connection.
        ///
        /// The access methods and data cases need it: they check that the
        /// device answers SPINE reads and SME control messages at the same
        /// time, which cannot be asked of a SHIP connection with nothing behind
        /// it.
        /// </summary>
        public Boolean    AttachSPINE          { get; init; }

    }

    #endregion


    /// <summary>
    /// The SHIP half of the test tool: the communication partner a device under
    /// test talks to while a TC_SHIP_* case runs.
    ///
    /// It is deliberately *not* a SHIP node. A node is well behaved by
    /// construction, and more than half of the official catalog consists of
    /// things a well behaved node would never do - sending a control message
    /// where an init belongs, announcing a CmiHead of 1, going quiet for four
    /// minutes in the middle of the hello phase. So the tool builds frames by
    /// hand and speaks only when a test step tells it to. That is the whole
    /// difference between an implementation and a test tool: this one has to be
    /// able to misbehave on purpose, precisely and repeatably.
    ///
    /// Time is the second reason it exists. Every SHIP timer runs on a
    /// TimeProvider (WORKPLAN § 8), so the tool can sit out a thirty second
    /// CmiTimeout or a four minute Wait-For-Ready timer in no time at all and
    /// get the same answer every run. The whole SHIP suite takes a few hundred
    /// milliseconds of wall clock and covers about twenty minutes of protocol.
    /// </summary>
    public sealed class SHIPTestTool
    {

        #region (class) ToolTransport

        /// <summary>
        /// What the device under test sends into: everything is kept, nothing
        /// is answered by itself.
        /// </summary>
        private sealed class ToolTransport : ISHIPTransport
        {

            private readonly List<Byte[]> frames = [];

            public IReadOnlyList<Byte[]>  Frames       => frames;
            public Boolean                IsClosed     { get; private set; }
            public String?                CloseReason  { get; private set; }
            public TimeSpan?              ClosedAt     { get; private set; }

            public TimeProvider?          Clock        { get; set; }
            public DateTimeOffset         StartedAt    { get; set; }

            public Task SendAsync(Byte[] Frame, CancellationToken CancellationToken = default)
            {
                frames.Add(Frame);
                return Task.CompletedTask;
            }

            public Task CloseAsync(String? Reason = null, CancellationToken CancellationToken = default)
            {

                if (!IsClosed)
                {
                    IsClosed     = true;
                    CloseReason  = Reason;
                    ClosedAt     = Clock is not null ? Clock.GetUtcNow() - StartedAt : null;
                }

                return Task.CompletedTask;

            }

        }

        #endregion

        #region (class) StaticTrust

        private sealed class StaticTrust(Boolean Trusted, Boolean Waiting) : ISHIPTrustProvider
        {
            public Boolean IsTrusted           (SKI RemoteSKI) => Trusted;
            public Boolean AllowWaitingForTrust(SKI RemoteSKI) => Waiting;
        }

        #endregion


        #region Data

        /// <summary>
        /// How finely the clock is advanced while waiting. Small enough that a
        /// timer never fires "late" by more than this, large enough that a four
        /// minute wait is a few thousand steps rather than a few hundred
        /// thousand.
        /// </summary>
        private static readonly TimeSpan   resolution = TimeSpan.FromMilliseconds(100);

        private readonly FakeTimeProvider     time;
        private readonly ToolTransport        transport;
        private readonly DateTimeOffset       startedAt;
        private readonly List<ASHIPMessage>   received     = [];
        private readonly Queue<JObject>       spineInbox   = new ();

        private Int32                         readFrames;

        #endregion

        #region Properties

        /// <summary>
        /// The connection of the device under test. Only the test tool touches
        /// it; a test case talks to the device through this tool.
        /// </summary>
        public SHIPConnection        DUT           { get; }

        /// <summary>Which side of the connection the device is on.</summary>
        public SHIPRoles             DUTRole       { get; }

        /// <summary>How it was set up.</summary>
        public SHIPTestToolOptions   Options       { get; }

        /// <summary>How much protocol time has passed since the tool opened.</summary>
        public TimeSpan              Elapsed
            => time.GetUtcNow() - startedAt;

        /// <summary>Whether the device closed the underlying connection.</summary>
        public Boolean               DUTClosed
            => transport.IsClosed;

        /// <summary>Why it closed, when it said.</summary>
        public String?               CloseReason
            => transport.CloseReason;

        /// <summary>When it closed.</summary>
        public TimeSpan?             ClosedAt
            => transport.ClosedAt;

        /// <summary>Every message the device sent, in order.</summary>
        public IReadOnlyList<ASHIPMessage> Received
        {
            get
            {
                Drain();
                return received;
            }
        }

        /// <summary>
        /// The SPINE device behind the SHIP connection, when the case asked for
        /// one.
        /// </summary>
        public SPINELocalDevice?     SPINE         { get; private set; }

        /// <summary>
        /// The SPINE datagrams the device sent, read by the model.
        /// </summary>
        public IReadOnlyList<DatagramType> SPINEDatagrams
            => Received.OfType<SHIPDataMessage>().
                   Select(message => message.Data.Payload is JObject payload
                                         ? SPINEJSON.Read<DatagramType>(payload["datagram"] ?? payload)
                                         : null).
                   Where (datagram => datagram is not null).
                   Select(datagram => datagram!).
                   ToList();

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Open a SHIP conversation with a device under test built from this
        /// stack - the self test.
        /// </summary>
        /// <param name="Options">How the conversation is set up.</param>
        private SHIPTestTool(SHIPTestToolOptions Options)
        {

            this.Options    = Options;
            this.DUTRole    = Options.DUTRole;

            this.time       = new FakeTimeProvider(new DateTimeOffset(2026, 7, 26, 12, 0, 0, TimeSpan.Zero));
            this.startedAt  = time.GetUtcNow();

            this.transport  = new ToolTransport {
                                  Clock      = time,
                                  StartedAt  = startedAt
                              };

            this.DUT        = new SHIPConnection(
                                  Options.DUTRole,
                                  SKI.Parse("1111111111111111111111111111111111111111"),
                                  SHIP_Id.Parse(Options.DUTShipId),
                                  transport,
                                  new StaticTrust(Options.MutualTrust, Options.AllowWaitingForTrust),
                                  TimeProvider: time
                              );

            if (Options.AttachSPINE)
            {

                SPINE = new SPINELocalDevice($"d:_i:19667_{Options.DUTShipId}",
                                             DeviceTypeType.EnergyManagementSystem,
                                             TimeProvider: time);

                SPINE.AddEntity(EntityTypeType.CEM);

                // What the device sends back goes out as SHIP data - but only
                // once the handshake is done, which is the only state in which
                // SPINE exists at all.
                SPINE.AddRemoteDevice("ski-of-testtool",
                                      new SPINEOverSHIP(this));

                // A datagram is not processed while the connection is still
                // inside ReceiveAsync: it is queued and handled afterwards,
                // exactly as a real device would hand it to another thread.
                DUT.OnSPINEDataReceived += (_, datagram) => spineInbox.Enqueue(datagram);

            }

        }

        #endregion

        #region (class) SPINEOverSHIP

        /// <summary>
        /// Where the SPINE device behind the connection writes to.
        /// </summary>
        private sealed class SPINEOverSHIP(SHIPTestTool Tool) : ISPINEWriter
        {

            public Task SendSPINEDatagram(JObject            Datagram,
                                          CancellationToken  CancellationToken   = default)

                => Tool.DUT.IsCompleted ||
                   Tool.DUT.State == SHIPMessageExchangeStates.SmeAccessMethodsRequest
                       ? Tool.DUT.SendSPINEDataAsync(Datagram, CancellationToken)
                       : Task.CompletedTask;

        }

        #endregion

        #region (static) Open(Options, CancellationToken = default)

        /// <summary>
        /// Open a conversation and let the device take its first step, which
        /// for a client is its own CMI message and for a server is waiting.
        /// </summary>
        /// <param name="Options">How the conversation is set up.</param>
        /// <param name="CancellationToken">An optional cancellation token.</param>
        public static async Task<SHIPTestTool> Open(SHIPTestToolOptions  Options,
                                                    CancellationToken    CancellationToken   = default)
        {

            var tool = new SHIPTestTool(Options);

            await tool.DUT.StartAsync(CancellationToken);

            return tool;

        }

        #endregion


        #region Send(Message / Frame / Bytes, CancellationToken = default)

        /// <summary>
        /// Send a well formed SHIP message to the device.
        /// </summary>
        /// <param name="Message">A SHIP message.</param>
        /// <param name="CancellationToken">An optional cancellation token.</param>
        public Task Send(ASHIPMessage       Message,
                         CancellationToken  CancellationToken   = default)

            => SendRaw(Message.ToByteArray(), CancellationToken);


        /// <summary>
        /// Send whatever these bytes are. The only way to send the things a
        /// SHIP message class refuses to build: an empty message value, an
        /// unknown message type, a CmiHead greater than zero.
        /// </summary>
        /// <param name="Bytes">The payload of a binary WebSocket frame.</param>
        /// <param name="CancellationToken">An optional cancellation token.</param>
        public async Task SendRaw(Byte[]             Bytes,
                                  CancellationToken  CancellationToken   = default)
        {

            if (!transport.IsClosed)
                await DUT.ReceiveAsync(Bytes, CancellationToken);

            await Settle();

        }


        /// <summary>
        /// Send a message whose JSON is given as text, so that its exact bytes
        /// can be chosen - which is what the whitespace case is about.
        /// </summary>
        /// <param name="MessageType">The SHIP message type.</param>
        /// <param name="JSON">The JSON text, exactly as it should go on the wire.</param>
        /// <param name="CancellationToken">An optional cancellation token.</param>
        public Task SendText(SHIPMessageTypes   MessageType,
                             String             JSON,
                             CancellationToken  CancellationToken   = default)
        {

            var body   = Encoding.UTF8.GetBytes(JSON);
            var bytes  = new Byte[1 + body.Length];

            bytes[0] = (Byte) MessageType;
            Array.Copy(body, 0, bytes, 1, body.Length);

            return SendRaw(bytes, CancellationToken);

        }

        #endregion

        #region Advance(By, CancellationToken = default) / Settle()

        /// <summary>
        /// Let the given amount of protocol time pass, so that the timers of
        /// the device fire.
        /// </summary>
        /// <param name="By">How much time.</param>
        /// <param name="CancellationToken">An optional cancellation token.</param>
        public async Task Advance(TimeSpan           By,
                                  CancellationToken  CancellationToken   = default)
        {

            var remaining = By;

            while (remaining > TimeSpan.Zero)
            {

                var step = remaining < resolution ? remaining : resolution;

                time.Advance(step);
                remaining -= step;

                await Settle();

            }

        }


        /// <summary>
        /// Let whatever the device started finish before looking at the result:
        /// hand the SPINE datagrams which arrived to the device's SPINE layer,
        /// and let its answers go back out.
        /// </summary>
        private async Task Settle()
        {

            await Task.Yield();

            while (spineInbox.Count > 0 && SPINE is not null)
            {

                var datagram = spineInbox.Dequeue();
                var remote   = SPINE.RemoteDeviceForSKI("ski-of-testtool");

                if (remote is not null)
                    await SPINE.ProcessDatagram(datagram, remote);

            }

        }

        #endregion

        #region WaitFor<T>(Within) / WaitForAny(Within) / WaitForClose(Within)

        /// <summary>
        /// Wait for the device to send a message of the given kind, letting
        /// protocol time pass while waiting.
        /// </summary>
        /// <param name="Within">How long to wait at most.</param>
        /// <param name="Match">An optional additional condition.</param>
        /// <returns>The message, or null when it never came.</returns>
        public async Task<T?> WaitFor<T>(TimeSpan     Within,
                                         Func<T, Boolean>?  Match   = null)

            where T : ASHIPMessage

        {

            var found = Look(Match);

            if (found is not null)
                return found;

            var waited = TimeSpan.Zero;

            while (waited < Within)
            {

                var step = Within - waited < resolution ? Within - waited : resolution;

                time.Advance(step);
                waited += step;

                await Settle();

                found = Look(Match);

                if (found is not null)
                    return found;

            }

            return null;

        }


        /// <summary>
        /// Wait for the device to say anything at all.
        /// </summary>
        /// <param name="Within">How long to wait at most.</param>
        public Task<ASHIPMessage?> WaitForAny(TimeSpan Within)

            => WaitFor<ASHIPMessage>(Within);


        /// <summary>
        /// Wait for the device to close the connection.
        /// </summary>
        /// <param name="Within">How long to wait at most.</param>
        /// <returns>Whether it closed.</returns>
        public async Task<Boolean> WaitForClose(TimeSpan Within)
        {

            if (transport.IsClosed)
                return true;

            var waited = TimeSpan.Zero;

            while (waited < Within)
            {

                var step = Within - waited < resolution ? Within - waited : resolution;

                time.Advance(step);
                waited += step;

                await Settle();

                if (transport.IsClosed)
                    return true;

            }

            return false;

        }

        #endregion

        #region Drain() / Look<T>(Match) / Take<T>()

        /// <summary>
        /// Parse whatever the device sent since the last look.
        /// </summary>
        private void Drain()
        {

            while (readFrames < transport.Frames.Count)
            {

                var frame = transport.Frames[readFrames++];

                if (ASHIPMessage.TryParse(frame, out var message, out _))
                    received.Add(message);

                else
                    // A frame this stack cannot read is still a frame the
                    // device sent; keeping it out of the list would hide it.
                    received.Add(new UnparsableMessage(frame));

            }

        }


        private T? Look<T>(Func<T, Boolean>? Match)

            where T : ASHIPMessage

        {

            Drain();

            for (var i = taken; i < received.Count; i++)
                if (received[i] is T candidate && (Match is null || Match(candidate)))
                {
                    taken = i + 1;
                    return candidate;
                }

            return null;

        }

        private Int32 taken;

        #endregion

        #region (class) UnparsableMessage

        /// <summary>
        /// A frame the device sent which this stack could not read. It has a
        /// place in the list on purpose: "the device answered with something we
        /// do not understand" and "the device said nothing" are different
        /// findings.
        /// </summary>
        /// <param name="Frame">The bytes as they arrived.</param>
        public sealed class UnparsableMessage(Byte[] Frame) : ASHIPMessage(SHIPMessageTypes.CONTROL)
        {

            /// <summary>The bytes as they arrived.</summary>
            public Byte[] Frame { get; } = Frame;

            public override JObject? ToMessageJSON()
                => null;

        }

        #endregion


        #region Preconditions

        #region ReachCMI(CancellationToken = default)

        /// <summary>
        /// Get the connection mode initialisation out of the way, in whichever
        /// order the roles demand.
        /// </summary>
        /// <param name="CancellationToken">An optional cancellation token.</param>
        public async Task ReachCMI(CancellationToken CancellationToken = default)
        {

            if (DUTRole == SHIPRoles.Client)
            {

                var init = await WaitFor<SHIPInitMessage>(TimeSpan.FromSeconds(10));

                if (init is null)
                    throw new ConformanceInconclusive("The device did not send its CMI message.");

            }

            await Send(new SHIPInitMessage(), CancellationToken);

        }

        #endregion

        #region ReachHelloCompleted(CancellationToken = default)

        /// <summary>
        /// PRE_SHIP_Hello_Completed: both sides reached "ready" and the
        /// protocol handshake may begin. From here the tool answers nothing by
        /// itself any more.
        /// </summary>
        /// <param name="CancellationToken">An optional cancellation token.</param>
        public async Task ReachHelloCompleted(CancellationToken CancellationToken = default)
        {

            await ReachCMI(CancellationToken);

            var hello = await WaitFor<SHIPHelloMessage>(TimeSpan.FromSeconds(10),
                                                        message => message.ConnectionHello.Phase == ConnectionHelloPhase.Ready);

            if (hello is null)
                throw new ConformanceInconclusive("The device did not announce itself as ready.");

            await Send(SHIPParameters.HelloStateReady, CancellationToken);

        }

        #endregion

        #region ReachProtocolHandshakeCompleted(Parameters, CancellationToken = default)

        /// <summary>
        /// PRE_SHIP_Protocol_Handshake_Completed: the three way handshake is
        /// done and the PIN verification is about to start.
        /// </summary>
        /// <param name="Parameters">What the device declared about itself.</param>
        /// <param name="CancellationToken">An optional cancellation token.</param>
        public async Task ReachProtocolHandshakeCompleted(ParameterSheet     Parameters,
                                                          CancellationToken  CancellationToken   = default)
        {

            await ReachHelloCompleted(CancellationToken);

            if (DUTRole == SHIPRoles.Server)
            {

                await Send(SHIPParameters.ProtocolAnnounceMax(Parameters), CancellationToken);

                var select = await WaitFor<SHIPHandshakeMessage>(TimeSpan.FromSeconds(10),
                                                                 message => message.MessageProtocolHandshake.HandshakeType == ProtocolHandshakeTypeTypes.select);

                if (select is null)
                    throw new ConformanceInconclusive("The device did not select a message format.");

                await Send(new SHIPHandshakeMessage(select.MessageProtocolHandshake), CancellationToken);

            }

            else
            {

                var announce = await WaitFor<SHIPHandshakeMessage>(TimeSpan.FromSeconds(10),
                                                                   message => message.MessageProtocolHandshake.HandshakeType == ProtocolHandshakeTypeTypes.announceMax);

                if (announce is null)
                    throw new ConformanceInconclusive("The device did not announce its maximum message format.");

                await Send(SHIPParameters.ProtocolSelect(announce.MessageProtocolHandshake, Parameters), CancellationToken);

                var confirm = await WaitFor<SHIPHandshakeMessage>(TimeSpan.FromSeconds(10),
                                                                  message => message.MessageProtocolHandshake.HandshakeType == ProtocolHandshakeTypeTypes.select);

                if (confirm is null)
                    throw new ConformanceInconclusive("The device did not confirm the selected message format.");

            }

        }

        #endregion

        #region ReachConnectionEstablished(Parameters, CancellationToken = default)

        /// <summary>
        /// PRE_SHIP_ConnectionEstablished: everything is done and SPINE data
        /// may flow - the starting point of a third of the SHIP catalog.
        /// </summary>
        /// <param name="Parameters">What the device declared about itself.</param>
        /// <param name="CancellationToken">An optional cancellation token.</param>
        public async Task ReachConnectionEstablished(ParameterSheet     Parameters,
                                                     CancellationToken  CancellationToken   = default)
        {

            await ReachProtocolHandshakeCompleted(Parameters, CancellationToken);

            var pinState = await WaitFor<SHIPPinStateMessage>(TimeSpan.FromSeconds(10));

            if (pinState is null)
                throw new ConformanceInconclusive("The device did not announce its PIN state.");

            await Send(SHIPParameters.PinStateNone, CancellationToken);

            // Whoever asks first is answered; both sides ask.
            var request = await WaitFor<SHIPAccessMethodsRequestMessage>(TimeSpan.FromSeconds(10));

            if (request is not null)
                await Send(SHIPParameters.AccessMethods(Options.ToolShipId), CancellationToken);

            await Send(new SHIPAccessMethodsRequestMessage(), CancellationToken);

            var methods = await WaitFor<SHIPAccessMethodsMessage>(TimeSpan.FromSeconds(10));

            if (methods is null)
                throw new ConformanceInconclusive("The device did not answer the access methods request.");

            if (!DUT.IsCompleted)
                throw new ConformanceInconclusive($"The device did not reach the data exchange state, but is in '{DUT.State}'.");

        }

        #endregion

        #endregion

        #region (static) WithWhitespace(Message)

        /// <summary>
        /// The JSON of a message, blown up with every structural whitespace
        /// character the JSON specification allows - space, horizontal tab, line
        /// feed and carriage return, before and after every structural
        /// character.
        /// </summary>
        /// <param name="Message">A SHIP message.</param>
        public static String WithWhitespace(ASHIPMessage Message)
        {

            var json     = EEBUSJSON.ToEEBUSJSON(Message.ToMessageJSON()
                                                     ?? throw new ArgumentException("This message carries no JSON!", nameof(Message))).
                               ToString(Formatting.None);

            var builder  = new StringBuilder();

            foreach (var character in json)
            {

                if (character is '{' or '}' or '[' or ']' or ':' or ',')
                {
                    builder.Append(" \t\r\n");
                    builder.Append(character);
                    builder.Append("\n\r\t ");
                }

                else
                    builder.Append(character);

            }

            return builder.ToString();

        }

        #endregion

    }

}
