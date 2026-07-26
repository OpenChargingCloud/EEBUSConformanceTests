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

    /// <summary>
    /// The PAR_ blocks of the SHIP test specification, chapter 2.5: the exact
    /// messages the test tool sends during the test steps.
    ///
    /// They live in one place and carry their official names, so that a step
    /// reads the way the specification writes it - "the test tool sends a CMI
    /// message using PAR_cmiInvalidCmiHead" - and so that changing what a
    /// parameter block means changes it for every case at once.
    ///
    /// Five of them cannot be built from a SHIP message class at all, because
    /// the message classes only build valid messages. Those are byte arrays.
    /// </summary>
    public static class SHIPParameters
    {

        #region Message type and message value blocks (byte arrays)

        /// <summary>
        /// PAR_cmiValidInit: message type 0, CmiHead 0. The one CMI message
        /// which is correct.
        /// </summary>
        public static Byte[] CmiValidInitBytes { get; }
            = [ (Byte) SHIPMessageTypes.INIT, SHIPMessageValue.CMI_HEAD ];

        /// <summary>
        /// PAR_msgEmptyMessageValue: a data message with no message value at
        /// all - the message type byte and nothing else.
        /// </summary>
        public static Byte[] MsgEmptyMessageValue { get; }
            = [ (Byte) SHIPMessageTypes.DATA ];

        /// <summary>
        /// PAR_msgUnknownMessageType: message type 0xFF, with a valid one byte
        /// payload so that the message value cannot be the reason it is
        /// refused.
        /// </summary>
        public static Byte[] MsgUnknownMessageType { get; }
            = [ 0xFF, 0x00 ];

        /// <summary>
        /// PAR_cmiInvalidMessageType: a control message where an init belongs.
        /// </summary>
        public static Byte[] CmiInvalidMessageType { get; }
            = [ (Byte) SHIPMessageTypes.CONTROL, 0x00 ];

        /// <summary>
        /// PAR_cmiInvalidCmiHead: an init message whose CmiHead is 1.
        /// </summary>
        public static Byte[] CmiInvalidCmiHead { get; }
            = [ (Byte) SHIPMessageTypes.INIT, 0x01 ];

        #endregion

        #region Hello blocks

        /// <summary>
        /// PAR_helloStateReady: connectionHello.phase = "ready".
        /// </summary>
        public static SHIPHelloMessage HelloStateReady { get; }
            = new (new ConnectionHello(ConnectionHelloPhase.Ready));

        /// <summary>
        /// PAR_helloProlongationRequest: connectionHello.phase = "pending"
        /// together with prolongationRequest = true.
        /// </summary>
        public static SHIPHelloMessage HelloProlongationRequest { get; }
            = new (new ConnectionHello(ConnectionHelloPhase.Pending, null, true));

        /// <summary>
        /// PAR_helloStatePending: connectionHello.phase = "pending" with a
        /// waiting time of two minutes and no prolongation request - the one a
        /// device in state "ready" has to ignore in silence.
        /// </summary>
        public static SHIPHelloMessage HelloStatePending { get; }
            = new (new ConnectionHello(ConnectionHelloPhase.Pending, 120000));

        #endregion

        #region Protocol handshake blocks

        /// <summary>
        /// PAR_protAnnounceMax: the version of PAR_testToolShipVersion and the
        /// format JSON-UTF8.
        /// </summary>
        /// <param name="Parameters">What the device declared about itself.</param>
        public static SHIPHandshakeMessage ProtocolAnnounceMax(ParameterSheet Parameters)

            => new (
                   new MessageProtocolHandshake(
                       ProtocolHandshakeTypeTypes.announceMax,
                       Version(Parameters.TestToolShipVersion),
                       [ MessageProtocolFormat.JSON_UTF8 ]
                   )
               );


        /// <summary>
        /// The answer of a test tool acting as SHIP server: the format
        /// JSON-UTF8 and the greatest version both sides support, which is the
        /// smaller of the two announced maxima.
        /// </summary>
        /// <param name="AnnounceMax">What the device announced.</param>
        /// <param name="Parameters">What the device declared about itself.</param>
        public static SHIPHandshakeMessage ProtocolSelect(MessageProtocolHandshake  AnnounceMax,
                                                          ParameterSheet            Parameters)
        {

            var tool     = Version(Parameters.TestToolShipVersion);
            var chosen   = AnnounceMax.Version.Major < tool.Major ||
                          (AnnounceMax.Version.Major == tool.Major && AnnounceMax.Version.Minor < tool.Minor)
                               ? AnnounceMax.Version
                               : tool;

            return new SHIPHandshakeMessage(
                       new MessageProtocolHandshake(
                           ProtocolHandshakeTypeTypes.select,
                           chosen,
                           [ MessageProtocolFormat.JSON_UTF8 ]
                       )
                   );

        }

        #endregion

        #region PIN and access methods blocks

        /// <summary>
        /// PAR_pinStateNone: connectionPinState.pinState = "none".
        /// </summary>
        public static SHIPPinStateMessage PinStateNone { get; }
            = new (new ConnectionPinState(PinState.None));


        /// <summary>
        /// An access methods message announcing the given SHIP identifier.
        /// </summary>
        /// <param name="ShipId">A SHIP identifier.</param>
        public static SHIPAccessMethodsMessage AccessMethods(String ShipId)

            => new (new AccessMethodsType(SHIP_Id.Parse(ShipId), null, null));

        #endregion


        #region (private) Version(Text)

        /// <summary>
        /// A SHIP version as the handshake carries it: a major and a minor.
        /// </summary>
        /// <param name="Text">A version, e.g. "1.0".</param>
        private static MessageProtocolHandshakeVersion Version(String Text)
        {

            var parts = Text.Split('.');

            return new MessageProtocolHandshakeVersion(
                       parts.Length > 0 && UInt16.TryParse(parts[0], out var major) ? major : (UInt16) 1,
                       parts.Length > 1 && UInt16.TryParse(parts[1], out var minor) ? minor : (UInt16) 0
                   );

        }

        #endregion

    }

}
