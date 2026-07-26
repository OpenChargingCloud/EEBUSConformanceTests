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

using Newtonsoft.Json.Linq;

using cloud.charging.open.protocols.EEBUS.SPINE;
using cloud.charging.open.protocols.EEBUS.SPINE.Model;

#endregion

namespace cloud.charging.open.protocols.EEBUS.Conformance
{

    /// <summary>
    /// The SPINE half of the test tool: the communication partner a device
    /// under test exchanges datagrams with while a TC_SPINE_* case runs.
    ///
    /// It has two halves of its own. Most of the catalog is spoken by *hand* -
    /// a datagram built to be wrong in one specific way, sent, and whatever
    /// comes back is inspected. The rest needs a working communication partner,
    /// because a third of the SPINE catalog is really a use case case: it
    /// establishes LPC or LPP between the two sides and then checks one
    /// protocol rule inside that conversation.
    ///
    /// For those, the test tool has to be a correct partner which is wrong in
    /// exactly one place - announcing a client feature under an arbitrary type,
    /// appending an element nobody has defined, claiming a use case is not
    /// available. That is what <see cref="Mutate"/> is for: the tool talks
    /// normally, and one hook rewrites its outgoing datagrams on the way out.
    /// The specification describes those preconditions in exactly those words -
    /// "internally pre-configured to autonomously append" - so the hook is not
    /// a shortcut, it is the shape of the requirement.
    /// </summary>
    public sealed class SPINETestTool
    {

        #region (class) Wire

        /// <summary>
        /// One direction between the two devices: everything is recorded, and
        /// what the test tool sends may be rewritten on the way out.
        /// </summary>
        private sealed class Wire(String Name) : ISPINEWriter
        {

            private readonly List<JObject>       raw        = [];
            private readonly List<DatagramType>  datagrams  = [];

            /// <summary>What went through, as JSON.</summary>
            public IReadOnlyList<JObject>       Raw        => raw;

            /// <summary>What went through, read by the model.</summary>
            public IReadOnlyList<DatagramType>  Datagrams  => datagrams;

            /// <summary>Where it goes, when it goes anywhere.</summary>
            public SPINELocalDevice?            Target     { get; set; }

            /// <summary>How the other side knows the sender.</summary>
            public SPINERemoteDevice?           Sender     { get; set; }

            /// <summary>Whether to deliver at all.</summary>
            public Boolean                      Deliver    { get; set; } = true;

            /// <summary>What to do to a datagram before it leaves.</summary>
            public Func<JObject, JObject>?      Mutate     { get; set; }

            /// <summary>What this direction is called, for error messages.</summary>
            public String                       Name       { get; } = Name;


            public async Task SendSPINEDatagram(JObject            Datagram,
                                                CancellationToken  CancellationToken   = default)
            {

                var outgoing = Mutate is not null ? Mutate(Datagram) : Datagram;

                raw.Add(outgoing);

                var datagram = SPINEJSON.Read<DatagramType>(outgoing["datagram"] ?? outgoing);

                if (datagram is not null)
                    datagrams.Add(datagram);

                if (Deliver && Target is not null && Sender is not null)
                    await Target.ProcessDatagram(outgoing, Sender, CancellationToken);

            }

        }

        #endregion


        #region Data

        private readonly FakeTimeProvider  time;
        private readonly Wire              dutToTool;
        private readonly Wire              toolToDUT;
        private readonly DateTimeOffset    startedAt;

        private UInt64                     msgCounter;

        #endregion

        #region Properties

        /// <summary>The device under test.</summary>
        public SPINELocalDevice    DUT               { get; }

        /// <summary>The device the test tool itself presents.</summary>
        public SPINELocalDevice    Tool              { get; }

        /// <summary>How the device under test knows the test tool.</summary>
        public SPINERemoteDevice   ToolAsSeenByDUT   { get; }

        /// <summary>How the test tool knows the device under test.</summary>
        public SPINERemoteDevice   DUTAsSeenByTool   { get; }

        /// <summary>The clock both devices run on.</summary>
        public FakeTimeProvider    Time              => time;

        /// <summary>How much protocol time has passed.</summary>
        public TimeSpan            Elapsed           => time.GetUtcNow() - startedAt;

        /// <summary>Everything the device under test sent.</summary>
        public IReadOnlyList<DatagramType>  FromDUT      => dutToTool.Datagrams;

        /// <summary>The same, as it went on the wire.</summary>
        public IReadOnlyList<JObject>       FromDUTRaw   => dutToTool.Raw;

        /// <summary>Everything the test tool sent through its own device.</summary>
        public IReadOnlyList<DatagramType>  FromTool     => toolToDUT.Datagrams;

        /// <summary>
        /// Whether the test tool's own device answers what the device under
        /// test asks it. Off for the hand spoken cases, on for the ones which
        /// need a working communication partner.
        /// </summary>
        public Boolean AutoAnswer
        {
            get => dutToTool.Deliver;
            set => dutToTool.Deliver = value;
        }

        /// <summary>
        /// What to do to every datagram the test tool's own device sends,
        /// before the device under test sees it - the PRE_TestTool_*_Configured
        /// preconditions.
        /// </summary>
        public Func<JObject, JObject>? Mutate
        {
            get => toolToDUT.Mutate;
            set => toolToDUT.Mutate = value;
        }

        /// <summary>The primary node management feature of the device under test.</summary>
        public FeatureAddressType DUTNodeManagement
            => new () { Device = DUT.DeviceAddress, Entity = [ 0 ], Feature = 0 };

        /// <summary>The primary node management feature of the test tool.</summary>
        public FeatureAddressType ToolNodeManagement
            => new () { Device = Tool.DeviceAddress, Entity = [ 0 ], Feature = 0 };

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Wire a device under test to a test tool.
        /// </summary>
        /// <param name="DUT">The device under test.</param>
        /// <param name="Tool">The device the test tool presents.</param>
        /// <param name="Time">The clock both of them run on.</param>
        public SPINETestTool(SPINELocalDevice  DUT,
                             SPINELocalDevice  Tool,
                             FakeTimeProvider  Time)
        {

            this.DUT              = DUT;
            this.Tool             = Tool;
            this.time             = Time;
            this.startedAt        = Time.GetUtcNow();

            this.dutToTool        = new Wire("DUT -> test tool") { Deliver = false };
            this.toolToDUT        = new Wire("test tool -> DUT");

            this.ToolAsSeenByDUT  = DUT. AddRemoteDevice($"ski-of-{Tool.DeviceAddress}", dutToTool, () => Interlocked.Increment(ref dutCounter));
            this.DUTAsSeenByTool  = Tool.AddRemoteDevice($"ski-of-{DUT.DeviceAddress}",  toolToDUT, NextMsgCounter);

            this.ToolAsSeenByDUT.DeviceAddress  = Tool.DeviceAddress;
            this.ToolAsSeenByDUT.DeviceType     = Tool.DeviceType;

            this.DUTAsSeenByTool.DeviceAddress  = DUT.DeviceAddress;
            this.DUTAsSeenByTool.DeviceType     = DUT.DeviceType;

            this.dutToTool.Target  = Tool;
            this.dutToTool.Sender  = DUTAsSeenByTool;

            this.toolToDUT.Target  = DUT;
            this.toolToDUT.Sender  = ToolAsSeenByDUT;

        }

        private UInt64 dutCounter;

        #endregion


        #region NextMsgCounter()

        /// <summary>
        /// The next message counter of the test tool. Ascending, as
        /// PAR_default demands - the cases which need something else say so.
        /// </summary>
        public UInt64 NextMsgCounter()

            => Interlocked.Increment(ref msgCounter);

        #endregion

        #region Send(Datagram / JSON, CancellationToken = default)

        /// <summary>
        /// Send a datagram the test tool built by hand.
        /// </summary>
        /// <param name="Datagram">A SPINE datagram.</param>
        /// <param name="CancellationToken">An optional cancellation token.</param>
        public Task Send(DatagramType       Datagram,
                         CancellationToken  CancellationToken   = default)

            => Send(new JObject(new JProperty("datagram", SPINEJSON.ToJObject(Datagram))),
                    CancellationToken);


        /// <summary>
        /// Send whatever this JSON is.
        ///
        /// The catalog needs this more often than one would think: an unknown
        /// function, an element nobody defined, a string where an unsigned
        /// integer belongs. None of them can be built from the model, which is
        /// exactly why they are worth sending.
        /// </summary>
        /// <param name="JSON">The JSON representation of a datagram.</param>
        /// <param name="CancellationToken">An optional cancellation token.</param>
        public async Task Send(JObject            JSON,
                               CancellationToken  CancellationToken   = default)
        {

            await DUT.ProcessDatagram(JSON, ToolAsSeenByDUT, CancellationToken);

            await Task.Yield();

        }

        #endregion

        #region Advance(By, CancellationToken = default)

        /// <summary>
        /// Let protocol time pass.
        /// </summary>
        /// <param name="By">How much.</param>
        /// <param name="CancellationToken">An optional cancellation token.</param>
        public async Task Advance(TimeSpan           By,
                                  CancellationToken  CancellationToken   = default)
        {

            var remaining   = By;
            var resolution  = TimeSpan.FromSeconds(1);

            while (remaining > TimeSpan.Zero)
            {

                var step = remaining < resolution ? remaining : resolution;

                time.Advance(step);
                remaining -= step;

                await Task.Yield();

            }

        }

        #endregion


        #region AnswersTo(MsgCounter) / ResultFor(MsgCounter) / ReplyFor(MsgCounter)

        /// <summary>
        /// Everything the device sent in answer to the given message - which
        /// the specification defines as everything whose msgCounterReference is
        /// that message's counter, and nothing else.
        /// </summary>
        /// <param name="MsgCounter">The message counter of a request.</param>
        public IEnumerable<DatagramType> AnswersTo(UInt64 MsgCounter)

            => FromDUT.Where(datagram => datagram.Header?.MsgCounterReference == MsgCounter);


        /// <summary>
        /// The result the device answered with, or null when it sent none.
        /// </summary>
        /// <param name="MsgCounter">The message counter of a request.</param>
        public ResultDataType? ResultFor(UInt64 MsgCounter)

            => AnswersTo(MsgCounter).
                   Where (datagram => datagram.Header?.CmdClassifier == CmdClassifierType.Result).
                   Select(datagram => datagram.Payload?.Cmd?.FirstOrDefault()?.GetData("resultData") as ResultDataType).
                   FirstOrDefault(result => result is not null);


        /// <summary>
        /// The reply the device answered with, or null when it sent none.
        /// </summary>
        /// <param name="MsgCounter">The message counter of a request.</param>
        public DatagramType? ReplyFor(UInt64 MsgCounter)

            => AnswersTo(MsgCounter).
                   FirstOrDefault(datagram => datagram.Header?.CmdClassifier == CmdClassifierType.Reply);


        /// <summary>
        /// The data of the reply the device answered with, or null.
        /// </summary>
        /// <param name="MsgCounter">The message counter of a request.</param>
        /// <param name="Function">The name of a SPINE function.</param>
        public Object? ReplyDataFor(UInt64  MsgCounter,
                                    String  Function)

            => ReplyFor(MsgCounter)?.Payload?.Cmd?.FirstOrDefault()?.GetData(Function);

        #endregion

        #region Sent(Classifier, Function)

        /// <summary>
        /// Whether the device sent a message of the given kind about the given
        /// function - the way a test step asks "did it ask us for the detailed
        /// discovery yet".
        /// </summary>
        /// <param name="Classifier">What kind of message.</param>
        /// <param name="Function">The name of a SPINE function.</param>
        public Boolean Sent(CmdClassifierType  Classifier,
                            String             Function)

            => FromDUT.Any(datagram => datagram.Header?.CmdClassifier == Classifier &&
                                       datagram.Payload?.Cmd?.Any(cmd => cmd.DataFunction == Function) == true);


        /// <summary>
        /// Everything of that kind the device sent.
        /// </summary>
        /// <param name="Classifier">What kind of message.</param>
        /// <param name="Function">The name of a SPINE function.</param>
        public IEnumerable<DatagramType> All(CmdClassifierType  Classifier,
                                             String             Function)

            => FromDUT.Where(datagram => datagram.Header?.CmdClassifier == Classifier &&
                                         datagram.Payload?.Cmd?.Any(cmd => cmd.DataFunction == Function) == true);

        #endregion

    }

}
