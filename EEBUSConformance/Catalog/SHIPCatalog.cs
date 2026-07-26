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

namespace cloud.charging.open.protocols.EEBUS.Conformance
{

    /// <summary>
    /// The catalog of EEBus_SHIP_TestSpecification_V1.0.0: thirty-three test
    /// cases in twelve groups, and the twenty-six requirements they map to.
    ///
    /// Every identifier here is the official one, one to one, because that is
    /// the whole point - a result which does not carry the catalog identifier
    /// cannot be compared with anybody else's.
    /// </summary>
    public static class SHIPCatalog
    {

        #region Requirements

        /// <summary>
        /// The requirements of chapter 3.1, each with the section of [SHIP],
        /// [SHIP_IG] or [SRIP] it is taken from.
        /// </summary>
        public static IReadOnlyList<ConformanceRequirement> Requirements { get; } = [

            new ("SHIP-TS-MDNS-01",  "SHIP 7.3.2",        "The TXT record carries every mandatory key, in UTF-8."),
            new ("SRIP-TS-TXT-01",   "SRIP 2.2",          "The TXT record carries the installation process keys as well, brand, model and cat among them."),
            new ("SHIP-TS-CONN-01",  "SHIP 12.2.2",       "Of two connections to the same partner, the node with the larger SKI keeps the newest and closes the rest."),
            new ("SHIP-TS-ROLE-01",  "SHIP 13.4.1",       "A node may hold a different role per connection - server on one, client on another, at the same time."),
            new ("SHIP-TS-SEC-01",   "SHIP 12.2",         "A certificate whose SKI is not the SHA-1 of its own public key is rejected and the TLS handshake aborted."),
            new ("SHIP-TS-SEC-02",   "SHIP 12.2",         "That check is repeated on every reconnection, so a known SKI does not buy a forged certificate anything."),
            new ("SHIP-TS-MSG-01",   "SHIP 13.4.2",       "A SHIP message needs a message value of at least one octet."),
            new ("SHIP-TS-MSG-02",   "SHIP 13.4.2",       "An unknown message type is silently discarded or the channel is closed."),
            new ("SHIP-TS-CMI-01",   "SHIP 13.4.3",       "The connection mode initialisation accepts message type 0 with a CmiHead of 0."),
            new ("SHIP-TS-CMI-02",   "SHIP 13.4.3",       "A CMI message of any other message type closes the connection."),
            new ("SHIP-TS-CMI-03",   "SHIP 13.4.3",       "A CmiHead greater than zero closes the connection."),
            new ("SHIP-TS-CMI-04",   "SHIP 13.4.3",       "No CMI message before the CmiTimeout expires closes the connection."),
            new ("SHIP-TS-HELLO-01", "SHIP 13.4.4.1.3",   "Entering the hello state starts the Wait-For-Ready timer."),
            new ("SHIP-TS-HELLO-02", "SHIP 13.4.4.1.3",   "An incoming prolongation request is accepted and the Wait-For-Ready timer increased."),
            new ("SHIP-TS-HELLO-03", "SHIP 13.4.4.1.3",   "An expired Wait-For-Ready timer runs the common abort procedure."),
            new ("SHIP-TS-HELLO-04", "SHIP 13.4.4.1.3",   "A \"pending\" without a prolongation request, received while ready, is silently ignored."),
            new ("SHIP-TS-PROT-01",  "SHIP 13.4.4.2.2",   "JSON-UTF8 is supported for the protocol handshake."),
            new ("SHIP-TS-PROT-02",  "SHIP 13.4.4.2.3",   "An expired Wait timer aborts with error type 1, timeout."),
            new ("SHIP-TS-PROT-03",  "SHIP 13.4.4.2.3",   "A valid but unexpected message aborts with error type 2, unexpected message."),
            new ("SHIP-TS-PIN-01",   "SHIP 13.4.4.3.5.1", "A node not requiring a PIN announces pinState \"none\" and proceeds to the data exchange."),
            new ("SHIP-TS-TERM-01",  "SHIP 13.4.8.1.2",   "Whoever announces a termination closes at the latest after the maxTime it announced."),
            new ("SHIP-TS-DATA-01",  "SHIP 13.4.6",       "The accessMethods.id of an access methods message is the node's own SHIP identifier."),
            new ("SHIP-TS-ACC-01",   "SHIP 13.4.6",       "An access methods request is answered with an access methods message."),
            new ("SHIP-IG-01",       "SHIP-IG 2.2",       "The JSON parser accepts structural whitespace; a minified payload may not be expected."),
            new ("SHIP-IG-02",       "SHIP-IG 2.1",       "SPINE data is passed to the application immediately, even while an access methods query is open."),
            new ("SHIP-IG-03",       "SHIP-IG 2.1",       "An incoming access methods message is not postponed while a SPINE response is outstanding.")

        ];

        #endregion

        #region TestCases

        /// <summary>
        /// The thirty-three test cases of chapter 4, with the roles and the
        /// mandatory/optional status of the mapping table in chapter 3.2.
        /// </summary>
        public static IReadOnlyList<ConformanceTestCase> TestCases { get; } = [

            #region 4.1 Discovery (mDNS/DNS-SD)

            new ("TC_SHIP_MDNS_001",
                 ConformanceLayers.SHIP, "MDNS",
                 "Validate mDNS TXT record",
                 DUTRoles.Server, "Any", true,
                 [ "SHIP-TS-MDNS-01", "SRIP-TS-TXT-01" ],
                 [ "PRE_SHIP_ServerPort" ]) {
                Applicability         = "Applicable to any DUT where PAR_shipSvc is \"yes\".",
                NotApplicableBecause  = parameters => parameters.ShipSvc ? null : "the device declares PAR_shipSvc = \"no\" and announces no SHIP service"
            },

            #endregion

            #region 4.2 Prevent double connections

            new ("TC_SHIP_CONN_001",
                 ConformanceLayers.SHIP, "CONN",
                 "Resolve simultaneous connections by SKI (DUT has larger SKI)",
                 DUTRoles.ServerAndClient, "Any", true,
                 [ "SHIP-TS-CONN-01" ],
                 [ "PRE_SHIP_ServerPort", "PRE_SHIP_TestTool_Smaller_SKI",
                   "PRE_SHIP_Mutual_Trust", "PRE_SHIP_TestTool_ServerAndClient" ]) {
                Applicability         = "Applicable to any DUT where PAR_shipSvc is \"yes\".",
                NotApplicableBecause  = parameters => parameters.ShipSvc ? null : "the device declares PAR_shipSvc = \"no\"",
                KnownDeviation        = "This stack resolves a double connection the way ship-go does - keeping the " +
                                        "connection initiated by the larger SKI rather than the most recent one - " +
                                        "because doing otherwise would leave the two sides keeping different " +
                                        "connections against the whole installed base. See docs/spec-deviations.md, C1."
            },

            #endregion

            #region 4.3 SME connection and roles

            new ("TC_SHIP_ROLE_001",
                 ConformanceLayers.SHIP, "ROLE",
                 "DUT as SME server",
                 DUTRoles.Server, "Any", true,
                 [ "SHIP-TS-ROLE-01", "SHIP-TS-CMI-01" ],
                 [ "PRE_SHIP_ServerPort", "PRE_SHIP_TestTool_ClientOnly",
                   "PRE_SHIP_Mutual_Trust", "PRE_SHIP_TLS_Established" ]) {
                Applicability         = "Applicable to any DUT where PAR_shipSvc is \"yes\".",
                NotApplicableBecause  = parameters => parameters.ShipSvc ? null : "the device declares PAR_shipSvc = \"no\""
            },

            new ("TC_SHIP_ROLE_002",
                 ConformanceLayers.SHIP, "ROLE",
                 "DUT as SME client",
                 DUTRoles.Client, "Any", true,
                 [ "SHIP-TS-ROLE-01", "SHIP-TS-CMI-01" ],
                 [ "PRE_SHIP_TestTool_ServerOnly", "PRE_SHIP_Mutual_Trust",
                   "PRE_SHIP_Ready_For_Handshake" ]) {
                Applicability = "Applicable to any DUT."
            },

            new ("TC_SHIP_ROLE_003",
                 ConformanceLayers.SHIP, "ROLE",
                 "Simultaneous role polymorphism",
                 DUTRoles.ServerAndClient, "Any", true,
                 [ "SHIP-TS-ROLE-01" ],
                 [ "PRE_SHIP_ServerPort", "PRE_SHIP_TestTool_Multiple_Instances",
                   "PRE_SHIP_TestTool_ClientOnly", "PRE_SHIP_TestTool_ServerOnly",
                   "PRE_SHIP_Mutual_Trust", "PRE_SHIP_Ready_For_Handshake" ]) {
                Applicability         = "Applicable to any DUT where PAR_shipSvc is \"yes\".",
                NotApplicableBecause  = parameters => parameters.ShipSvc ? null : "the device declares PAR_shipSvc = \"no\""
            },

            #endregion

            #region 4.4 Security

            new ("TC_SHIP_SEC_001",
                 ConformanceLayers.SHIP, "SEC",
                 "Reject spoofed certificate (no prior pairing)",
                 DUTRoles.ServerOrClient, "Any", true,
                 [ "SHIP-TS-SEC-01" ],
                 [ "PRE_SHIP_No_Prior_Pairing", "PRE_SHIP_TestTool_SpoofedCertificate",
                   "PRE_SHIP_Mutual_Trust" ]) {
                Applicability = "Applicable to any DUT."
            },

            new ("TC_SHIP_SEC_002",
                 ConformanceLayers.SHIP, "SEC",
                 "Reject spoofed certificate (with prior pairing)",
                 DUTRoles.ServerOrClient, "Any", true,
                 [ "SHIP-TS-SEC-02" ],
                 [ "PRE_SHIP_ConnectionEstablished" ]) {
                Applicability = "Applicable to any DUT."
            },

            #endregion

            #region 4.5 Basic message structure

            new ("TC_SHIP_MSG_001",
                 ConformanceLayers.SHIP, "MSG",
                 "Reject message without MessageValue",
                 DUTRoles.ServerOrClient, "Any", true,
                 [ "SHIP-TS-MSG-01", "SHIP-TS-CMI-01" ],
                 [ "PRE_SHIP_ConnectionEstablished", "PRE_SHIP_Manual_Message_Handling" ]) {
                Applicability = "Applicable to any DUT."
            },

            new ("TC_SHIP_MSG_002",
                 ConformanceLayers.SHIP, "MSG",
                 "Reject unknown MessageType",
                 DUTRoles.ServerOrClient, "Any", true,
                 [ "SHIP-TS-MSG-02" ],
                 [ "PRE_SHIP_ConnectionEstablished", "PRE_SHIP_Manual_Message_Handling" ]) {
                Applicability = "Applicable to any DUT."
            },

            new ("TC_SHIP_MSG_003",
                 ConformanceLayers.SHIP, "MSG",
                 "Support JSON whitespace formatting",
                 DUTRoles.ServerOrClient, "Any", true,
                 [ "SHIP-IG-01", "SHIP-TS-ACC-01" ],
                 [ "PRE_SHIP_ConnectionEstablished" ]) {
                Applicability = "Applicable to any DUT."
            },

            #endregion

            #region 4.6 SME connection mode initialization (CMI)

            new ("TC_SHIP_CMI_001",
                 ConformanceLayers.SHIP, "CMI",
                 "Reject invalid MessageType (DUT as server)",
                 DUTRoles.Server, "Any", true,
                 [ "SHIP-TS-CMI-02" ],
                 [ "PRE_SHIP_ServerPort", "PRE_SHIP_TestTool_ClientOnly", "PRE_SHIP_Mutual_Trust",
                   "PRE_SHIP_TLS_Established", "PRE_SHIP_Manual_Message_Handling" ]) {
                Applicability         = "Applicable to any DUT where PAR_shipSvc is \"yes\".",
                NotApplicableBecause  = parameters => parameters.ShipSvc ? null : "the device declares PAR_shipSvc = \"no\""
            },

            new ("TC_SHIP_CMI_002",
                 ConformanceLayers.SHIP, "CMI",
                 "Reject invalid MessageType (DUT as client)",
                 DUTRoles.Client, "Any", true,
                 [ "SHIP-TS-CMI-02" ],
                 [ "PRE_SHIP_TestTool_ServerOnly", "PRE_SHIP_Mutual_Trust",
                   "PRE_SHIP_Manual_Message_Handling" ]) {
                Applicability = "Applicable to any DUT."
            },

            new ("TC_SHIP_CMI_003",
                 ConformanceLayers.SHIP, "CMI",
                 "Apply CmiTimeout (DUT as server)",
                 DUTRoles.Server, "Any", true,
                 [ "SHIP-TS-CMI-04" ],
                 [ "PRE_SHIP_ServerPort", "PRE_SHIP_TestTool_ClientOnly", "PRE_SHIP_Mutual_Trust",
                   "PRE_SHIP_TLS_Established", "PRE_SHIP_Manual_Message_Handling" ]) {
                Applicability         = "Applicable to any DUT where PAR_shipSvc is \"yes\".",
                NotApplicableBecause  = parameters => parameters.ShipSvc ? null : "the device declares PAR_shipSvc = \"no\""
            },

            new ("TC_SHIP_CMI_004",
                 ConformanceLayers.SHIP, "CMI",
                 "Apply CmiTimeout (DUT as client)",
                 DUTRoles.Client, "Any", true,
                 [ "SHIP-TS-CMI-04" ],
                 [ "PRE_SHIP_TestTool_ServerOnly", "PRE_SHIP_Mutual_Trust",
                   "PRE_SHIP_Manual_Message_Handling" ]) {
                Applicability = "Applicable to any DUT."
            },

            new ("TC_SHIP_CMI_005",
                 ConformanceLayers.SHIP, "CMI",
                 "Reject invalid CmiHead (DUT as server)",
                 DUTRoles.Server, "Any", true,
                 [ "SHIP-TS-CMI-03" ],
                 [ "PRE_SHIP_ServerPort", "PRE_SHIP_TestTool_ClientOnly", "PRE_SHIP_Mutual_Trust",
                   "PRE_SHIP_TLS_Established", "PRE_SHIP_Manual_Message_Handling" ]) {
                Applicability         = "Applicable to any DUT where PAR_shipSvc is \"yes\".",
                NotApplicableBecause  = parameters => parameters.ShipSvc ? null : "the device declares PAR_shipSvc = \"no\""
            },

            new ("TC_SHIP_CMI_006",
                 ConformanceLayers.SHIP, "CMI",
                 "Reject invalid CmiHead (DUT as client)",
                 DUTRoles.Client, "Any", true,
                 [ "SHIP-TS-CMI-03" ],
                 [ "PRE_SHIP_TestTool_ServerOnly", "PRE_SHIP_Mutual_Trust",
                   "PRE_SHIP_Manual_Message_Handling" ]) {
                Applicability = "Applicable to any DUT."
            },

            #endregion

            #region 4.7 SME connection state "Hello"

            new ("TC_SHIP_HELLO_001",
                 ConformanceLayers.SHIP, "HELLO",
                 "Process valid Hello & enter protocol handshake",
                 DUTRoles.Server, "Any", true,
                 [ "SHIP-TS-HELLO-01" ],
                 [ "PRE_SHIP_ServerPort", "PRE_SHIP_TestTool_ClientOnly", "PRE_SHIP_Mutual_Trust",
                   "PRE_SHIP_TLS_Established", "PRE_SHIP_Manual_Message_Handling" ]) {
                Applicability         = "Applicable to any DUT where PAR_shipSvc is \"yes\".",
                NotApplicableBecause  = parameters => parameters.ShipSvc ? null : "the device declares PAR_shipSvc = \"no\""
            },

            new ("TC_SHIP_HELLO_002",
                 ConformanceLayers.SHIP, "HELLO",
                 "Accept prolongation requests",
                 DUTRoles.Server, "Any", true,
                 [ "SHIP-TS-HELLO-02" ],
                 [ "PRE_SHIP_ServerPort", "PRE_SHIP_TestTool_ClientOnly", "PRE_SHIP_Mutual_Trust",
                   "PRE_SHIP_TLS_Established", "PRE_SHIP_Manual_Message_Handling" ]) {
                Applicability         = "Applicable to any DUT where PAR_shipSvc is \"yes\".",
                NotApplicableBecause  = parameters => parameters.ShipSvc ? null : "the device declares PAR_shipSvc = \"no\""
            },

            new ("TC_SHIP_HELLO_003",
                 ConformanceLayers.SHIP, "HELLO",
                 "Apply Wait-For-Ready-Timer (timeout)",
                 DUTRoles.Server, "Any", true,
                 [ "SHIP-TS-HELLO-03" ],
                 [ "PRE_SHIP_ServerPort", "PRE_SHIP_TestTool_ClientOnly", "PRE_SHIP_Mutual_Trust",
                   "PRE_SHIP_TLS_Established", "PRE_SHIP_Manual_Message_Handling" ]) {
                Applicability         = "Applicable to any DUT where PAR_shipSvc is \"yes\".",
                NotApplicableBecause  = parameters => parameters.ShipSvc ? null : "the device declares PAR_shipSvc = \"no\""
            },

            new ("TC_SHIP_HELLO_004",
                 ConformanceLayers.SHIP, "HELLO",
                 "Ignore \"pending\" updates without prolongationRequest in \"ready\" state",
                 DUTRoles.Server, "Any", true,
                 [ "SHIP-TS-HELLO-04" ],
                 [ "PRE_SHIP_ServerPort", "PRE_SHIP_TestTool_ClientOnly", "PRE_SHIP_Mutual_Trust",
                   "PRE_SHIP_TLS_Established", "PRE_SHIP_Manual_Message_Handling" ]) {
                Applicability         = "Applicable to any DUT where PAR_shipSvc is \"yes\".",
                NotApplicableBecause  = parameters => parameters.ShipSvc ? null : "the device declares PAR_shipSvc = \"no\""
            },

            #endregion

            #region 4.8 SME protocol handshake

            new ("TC_SHIP_PROT_001",
                 ConformanceLayers.SHIP, "PROT",
                 "Support JSON-UTF8 format (DUT as server)",
                 DUTRoles.Server, "Any", true,
                 [ "SHIP-TS-PROT-01" ],
                 [ "PRE_SHIP_ServerPort", "PRE_SHIP_Hello_Completed" ]) {
                Applicability         = "Applicable to any DUT where PAR_shipSvc is \"yes\".",
                NotApplicableBecause  = parameters => parameters.ShipSvc ? null : "the device declares PAR_shipSvc = \"no\""
            },

            new ("TC_SHIP_PROT_002",
                 ConformanceLayers.SHIP, "PROT",
                 "Support JSON-UTF8 format (DUT as client)",
                 DUTRoles.Client, "Any", true,
                 [ "SHIP-TS-PROT-01" ],
                 [ "PRE_SHIP_Hello_Completed" ]) {
                Applicability = "Applicable to any DUT."
            },

            new ("TC_SHIP_PROT_003",
                 ConformanceLayers.SHIP, "PROT",
                 "Apply Wait-Timer (DUT as server)",
                 DUTRoles.Server, "Any", true,
                 [ "SHIP-TS-PROT-02" ],
                 [ "PRE_SHIP_ServerPort", "PRE_SHIP_Hello_Completed" ]) {
                Applicability         = "Applicable to any DUT where PAR_shipSvc is \"yes\".",
                NotApplicableBecause  = parameters => parameters.ShipSvc ? null : "the device declares PAR_shipSvc = \"no\""
            },

            new ("TC_SHIP_PROT_004",
                 ConformanceLayers.SHIP, "PROT",
                 "Apply Wait-Timer (DUT as client)",
                 DUTRoles.Client, "Any", true,
                 [ "SHIP-TS-PROT-02" ],
                 [ "PRE_SHIP_Hello_Completed" ]) {
                Applicability = "Applicable to any DUT."
            },

            new ("TC_SHIP_PROT_005",
                 ConformanceLayers.SHIP, "PROT",
                 "Reject unexpected message (DUT as server)",
                 DUTRoles.Server, "Any", true,
                 [ "SHIP-TS-PROT-03" ],
                 [ "PRE_SHIP_ServerPort", "PRE_SHIP_Hello_Completed" ]) {
                Applicability         = "Applicable to any DUT where PAR_shipSvc is \"yes\".",
                NotApplicableBecause  = parameters => parameters.ShipSvc ? null : "the device declares PAR_shipSvc = \"no\""
            },

            new ("TC_SHIP_PROT_006",
                 ConformanceLayers.SHIP, "PROT",
                 "Reject unexpected message (DUT as client)",
                 DUTRoles.Client, "Any", true,
                 [ "SHIP-TS-PROT-03" ],
                 [ "PRE_SHIP_Hello_Completed" ]) {
                Applicability = "Applicable to any DUT."
            },

            #endregion

            #region 4.9 PIN verification

            new ("TC_SHIP_PIN_001",
                 ConformanceLayers.SHIP, "PIN",
                 "PIN state none",
                 DUTRoles.ServerOrClient, "Any", true,
                 [ "SHIP-TS-PIN-01" ],
                 [ "PRE_SHIP_PIN_Disabled", "PRE_SHIP_Protocol_Handshake_Completed" ]) {
                Applicability = "Applicable to any DUT."
            },

            #endregion

            #region 4.10 Connection termination

            new ("TC_SHIP_TERM_001",
                 ConformanceLayers.SHIP, "TERM",
                 "Apply maxTime during termination (DUT initiates)",
                 DUTRoles.ServerOrClient, "Any", true,
                 [ "SHIP-TS-TERM-01" ],
                 [ "PRE_SHIP_ConnectionEstablished", "PRE_SHIP_Manual_Message_Handling" ]) {
                Applicability         = "Applicable to any DUT where PAR_initiateClose is \"yes\".",
                NotApplicableBecause  = parameters => parameters.InitiateClose ? null : "the device declares PAR_initiateClose = \"no\" and closes without announcing"
            },

            #endregion

            #region 4.11 Access methods and data exchange

            new ("TC_SHIP_AM_001",
                 ConformanceLayers.SHIP, "AM",
                 "Verify SHIP ID in access methods response",
                 DUTRoles.ServerOrClient, "Any", true,
                 [ "SHIP-TS-DATA-01" ],
                 [ "PRE_SHIP_ConnectionEstablished" ]) {
                Applicability = "Applicable to any DUT."
            },

            new ("TC_SHIP_AMDATA_001",
                 ConformanceLayers.SHIP, "AMDATA",
                 "Parallel processing of SME \"data\" while answering an \"access methods\" request",
                 DUTRoles.ServerOrClient, "Any", true,
                 [ "SHIP-IG-02" ],
                 [ "PRE_SHIP_ConnectionEstablished" ]) {
                Applicability = "Applicable to any DUT."
            },

            new ("TC_SHIP_AMDATA_002",
                 ConformanceLayers.SHIP, "AMDATA",
                 "Parallel processing of SME \"data\" while awaiting a delayed \"access methods\" response",
                 DUTRoles.ServerOrClient, "Any", true,
                 [ "SHIP-IG-02" ],
                 [ "PRE_SHIP_ConnectionEstablished" ]) {
                Applicability         = "Applicable to any DUT where PAR_queryAccessMethods is \"yes\".",
                NotApplicableBecause  = parameters => parameters.QueryAccessMethods ? null : "the device declares PAR_queryAccessMethods = \"no\" and never asks"
            },

            new ("TC_SHIP_AMDATA_003",
                 ConformanceLayers.SHIP, "AMDATA",
                 "Parallel processing of SME \"access methods\" while awaiting a delayed \"data\" response",
                 DUTRoles.ServerOrClient, "Any", true,
                 [ "SHIP-IG-03" ],
                 [ "PRE_SHIP_ConnectionEstablished" ]) {
                Applicability         = "Applicable to any DUT where PAR_clientDetailedDiscoverySupported is \"yes\".",
                NotApplicableBecause  = parameters => parameters.ClientDetailedDiscoverySupported ? null : "the device declares PAR_clientDetailedDiscoverySupported = \"no\""
            },

            new ("TC_SHIP_AMDATA_004",
                 ConformanceLayers.SHIP, "AMDATA",
                 "Verify that a DUT declaring PAR_queryAccessMethods = \"no\" sends no \"access methods request\"",
                 DUTRoles.ServerOrClient, "Any", true,
                 // The only case of the catalog which verifies no requirement:
                 // it is a methodology safeguard, checking that a device which
                 // declared itself out of TC_SHIP_AMDATA_002 is telling the truth.
                 [ ],
                 [ "PRE_SHIP_ConnectionEstablished" ]) {
                Applicability         = "Applicable to any DUT where PAR_queryAccessMethods is \"no\".",
                NotApplicableBecause  = parameters => parameters.QueryAccessMethods ? "the device declares PAR_queryAccessMethods = \"yes\" and is expected to ask" : null
            }

            #endregion

        ];

        #endregion

    }

}
