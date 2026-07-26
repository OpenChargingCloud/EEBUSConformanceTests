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
    /// The catalog of EEBus_SPINE_TestSpecification_V1.0.0: thirty-one test
    /// cases in nine groups.
    ///
    /// A third of them are not really protocol cases at all but *use case*
    /// cases wearing protocol clothes: they establish LPC or LPP between the
    /// device and the test tool and then check one protocol rule inside that
    /// conversation. That is why the catalog carries an actor per case - the
    /// role never decides whether a case applies, the actor does (chapter 3.2).
    /// </summary>
    public static class SPINECatalog
    {

        #region Requirements

        /// <summary>
        /// The requirements of chapter 3.1, each with the section of [SPINE_PS],
        /// [SPINE_RS], [IG-SPINE] or the LPC/LPP use case specification it is
        /// taken from.
        /// </summary>
        public static IReadOnlyList<ConformanceRequirement> Requirements { get; } = [

            new ("SPINE-TS-BIND-01",  "SPINE_PS 7.3.1, 7.3.2",  "A binding request to the primary node management feature is denied; the role \"special\" cannot be bound."),
            new ("SPINE-TS-BIND-02",  "SPINE_PS 7.3.5",         "A write to a feature which needs a binding is rejected with an application error when the sender holds none; error 9 is recommended."),
            new ("SPINE-TS-SUBS-01",  "SPINE_PS 7.4.4",         "A subscription request to the primary node management feature is accepted - the one exception the role \"special\" has."),
            new ("SPINE-TS-SUBS-02",  "SPINE_PS 7.4.1, 7.5.1",  "An existing subscription can be deleted, and deleting it twice does not break anything."),
            new ("SPINE-TS-COMP-01",  "SPINE_RS Table 19",      "An unknown function is answered with an application error; error 6, command not supported, is the best practice."),
            new ("SPINE-TS-COMP-02",  "SPINE_PS 4.3.4.3, 4.3.4.7", "A datagram announcing a higher minor version is accepted and processed."),
            new ("SPINE-TS-COMP-03",  "SPINE_PS 4.3.4",         "Unknown elements within an otherwise valid datagram are silently ignored."),
            new ("SPINE-TS-COMP-04",  "SPINE_PS 5.2.4 Table 2, 5.2.5.1", "A reply is acknowledged as a transmission, not judged as an application: an invalid payload does not earn an application error."),
            new ("SPINE-TS-COMP-05",  "IG-SPINE 2.5",           "Every version string sent matches [1-9][0-9]*\\.[0-9]+\\.[0-9]+, and the use case document sub revision is populated."),
            new ("SPINE-TS-COMP-06",  "SPINE_RS 3.10.1.4",      "A version string which breaks the format may end the communication; a leading v or V may optionally be tolerated."),
            new ("SPINE-TS-DATA-01",  "SPINE_PS 5.2.3.1",       "Outgoing message counters ascend strictly."),
            new ("SPINE-TS-DATA-02",  "SPINE_PS 5.2.3.1",       "Incoming message counters may skip values as long as they ascend."),
            new ("SPINE-TS-DATA-03",  "SPINE_PS 5.2.3.1",       "A message counter lower than the last one - a reboot, an overflow - resets the baseline instead of causing an error."),
            new ("SPINE-TS-DATA-04",  "SPINE_PS 5.2.3.2",       "A reply carries the message counter of its request as msgCounterReference."),
            new ("SPINE-TS-DATA-05",  "SPINE_PS 5.2.5.1",       "A notify with ackRequest is answered with a result."),
            new ("SPINE-TS-DATA-06",  "SPINE_PS 5.2.4",         "A result is never answered with a result, whatever its ackRequest says."),
            new ("SPINE-TS-DATA-07",  "SPINE_PS 5.2.4",         "The ackRequest of a read is not evaluated: the obligation to reply does not depend on it."),
            new ("SPINE-TS-FC-01",    "SPINE_PS 7.1, IG-SPINE 2.3", "Node management reaches entity 0, feature 0 or it is rejected."),
            new ("SPINE-TS-DD-01",    "SPINE_PS Annex D.3",     "A device with client features asks a new, unknown partner for its detailed discovery."),
            new ("SPINE-TS-DD-02",    "SPINE_PS Annex D.3",     "A partner which stays silent for thirty seconds may be disconnected."),
            new ("SPINE-TS-UCD-01",   "IG-SPINE 2.2",           "The useCaseAvailable flag of a server actor is ignored entirely."),
            new ("SPINE-TS-ES-01",    "UC_LPC 3.4.1, 3.4.3",    "Server actors are found dynamically, whatever their addresses and identifiers are this time."),
            new ("SPINE-TS-ES-02",    "UC_LPC 3.4.3",           "The energy guard's device diagnosis is subscribed at the address the topology says now, not the one it had before."),
            new ("SPINE-TS-RT-01",    "SPINE_PS 3.1, 7.1.1.4",  "A client feature of any known feature type is tolerated."),
            new ("SPINE-TS-RT-02",    "UC_LPC 3.4.1.1, 3.4.3.1","Which client matters is deduced from the binding request it sent, not from whoever else is around."),
            new ("SPINE-TS-RT-03",    "SPINE_PS 5.3.4.8",       "The merge logic beats any default: a partial write carrying a number and no scale updates the number and keeps the scale.")

        ];

        #endregion

        #region (private) Applicability helpers

        private static String? NeedsClientDiscovery(ParameterSheet Parameters)

            => Parameters.ClientDetailedDiscoverySupported
                   ? null
                   : "the device declares PAR_clientDetailedDiscoverySupported = \"no\"; a pure server never asks";

        private static String? NeedsClientSubscription(ParameterSheet Parameters)

            => Parameters.ClientSubscriptionSupported
                   ? null
                   : "the device declares PAR_clientSubscriptionSupported = \"no\"; a pure server never subscribes";

        private static String? NeedsInitialTimeout(ParameterSheet Parameters)

            => Parameters.InitialTimeoutSupported
                   ? null
                   : "the device declares PAR_initialTimeoutSupported = \"no\"; Annex D.3 leaves this optional";

        #endregion

        #region TestCases

        /// <summary>
        /// The thirty-one test cases of chapter 4, with the roles, actors and
        /// the mandatory/optional status of the mapping table in chapter 3.2.
        /// </summary>
        public static IReadOnlyList<ConformanceTestCase> TestCases { get; } = [

            #region 4.1 Compatibility

            new ("TC_SPINE_COMP_001",
                 ConformanceLayers.SPINE, "COMP",
                 "Reject unknown Function",
                 DUTRoles.Special, "Any", true,
                 [ "SPINE-TS-COMP-01" ],
                 [ "PRE_SPINE_ConnectionEstablished" ]) {
                Applicability = "Applicable to any DUT."
            },

            new ("TC_SPINE_COMP_002",
                 ConformanceLayers.SPINE, "COMP",
                 "Forward version compatibility",
                 DUTRoles.Special, "Any", true,
                 [ "SPINE-TS-COMP-02" ],
                 [ "PRE_SPINE_ConnectionEstablished" ]) {
                Applicability = "Applicable to any DUT."
            },

            new ("TC_SPINE_COMP_003",
                 ConformanceLayers.SPINE, "COMP",
                 "Ignore unknown Elements",
                 DUTRoles.Special, "Any", true,
                 [ "SPINE-TS-COMP-03" ],
                 [ "PRE_SPINE_NewConnectionEstablished" ]) {
                Applicability         = "Applicable to any DUT declaring PAR_clientDetailedDiscoverySupported = \"yes\".",
                NotApplicableBecause  = NeedsClientDiscovery
            },

            new ("TC_SPINE_COMP_004",
                 ConformanceLayers.SPINE, "COMP",
                 "Ignore invalid replies",
                 DUTRoles.Special, "Any", true,
                 [ "SPINE-TS-COMP-04" ],
                 [ "PRE_SPINE_NewConnectionEstablished" ]) {
                Applicability         = "Applicable to any DUT declaring PAR_clientDetailedDiscoverySupported = \"yes\".",
                NotApplicableBecause  = NeedsClientDiscovery
            },

            new ("TC_SPINE_COMP_005",
                 ConformanceLayers.SPINE, "COMP",
                 "Strict version formatting",
                 DUTRoles.Special, "Any", true,
                 [ "SPINE-TS-COMP-05" ],
                 [ "PRE_SPINE_ConnectionEstablished" ]) {
                Applicability = "Applicable to any DUT."
            },

            new ("TC_SPINE_COMP_006",
                 ConformanceLayers.SPINE, "COMP",
                 "Version format variations",
                 DUTRoles.Special, "Any", true,
                 [ "SPINE-TS-COMP-06" ],
                 [ "PRE_SPINE_NewConnectionEstablished" ]) {
                Applicability = "Applicable to any DUT."
            },

            #endregion

            #region 4.2 Datagram and message counters

            new ("TC_SPINE_DATA_001",
                 ConformanceLayers.SPINE, "DATA",
                 "Ascending message counters",
                 DUTRoles.Special, "Any", true,
                 [ "SPINE-TS-DATA-01" ],
                 [ "PRE_SPINE_ConnectionEstablished" ]) {
                Applicability = "Applicable to any DUT."
            },

            new ("TC_SPINE_DATA_002",
                 ConformanceLayers.SPINE, "DATA",
                 "Allow skipped counters",
                 DUTRoles.Special, "Any", true,
                 [ "SPINE-TS-DATA-02" ],
                 [ "PRE_SPINE_NewConnectionEstablished" ]) {
                Applicability = "Applicable to any DUT."
            },

            new ("TC_SPINE_DATA_003",
                 ConformanceLayers.SPINE, "DATA",
                 "Handle message counter reset or overflow",
                 DUTRoles.Special, "Any", true,
                 [ "SPINE-TS-DATA-03" ],
                 [ "PRE_SPINE_NewConnectionEstablished" ]) {
                Applicability = "Applicable to any DUT."
            },

            new ("TC_SPINE_DATA_004",
                 ConformanceLayers.SPINE, "DATA",
                 "Match reference counter",
                 DUTRoles.Special, "Any", true,
                 [ "SPINE-TS-DATA-04" ],
                 [ "PRE_SPINE_NewConnectionEstablished" ]) {
                Applicability = "Applicable to any DUT."
            },

            new ("TC_SPINE_DATA_005",
                 ConformanceLayers.SPINE, "DATA",
                 "Acknowledge notify datagrams",
                 DUTRoles.Special, "Any", true,
                 [ "SPINE-TS-DATA-05" ],
                 [ "PRE_SPINE_ConnectionEstablished", "PRE_SPINE_SubscriptionEstablished" ]) {
                Applicability         = "Applicable to any DUT declaring PAR_clientSubscriptionSupported = \"yes\".",
                NotApplicableBecause  = NeedsClientSubscription
            },

            new ("TC_SPINE_DATA_006",
                 ConformanceLayers.SPINE, "DATA",
                 "No response to results",
                 DUTRoles.Special, "Any", true,
                 [ "SPINE-TS-DATA-06" ],
                 [ "PRE_SPINE_ConnectionEstablished" ]) {
                Applicability = "Applicable to any DUT."
            },

            new ("TC_SPINE_DATA_007",
                 ConformanceLayers.SPINE, "DATA",
                 "Ignore ackRequest in read",
                 DUTRoles.Special, "Any", true,
                 [ "SPINE-TS-DATA-07" ],
                 [ "PRE_SPINE_ConnectionEstablished" ]) {
                Applicability = "Applicable to any DUT."
            },

            new ("TC_SPINE_DATA_008",
                 ConformanceLayers.SPINE, "DATA",
                 "No response to results (DUT initiated)",
                 DUTRoles.Special, "Any", true,
                 [ "SPINE-TS-DATA-06" ],
                 [ "PRE_SPINE_NewConnectionEstablished" ]) {
                Applicability         = "Applicable to any DUT declaring PAR_clientDetailedDiscoverySupported = \"yes\".",
                NotApplicableBecause  = NeedsClientDiscovery
            },

            #endregion

            #region 4.3 Functional commissioning (primary node management)

            new ("TC_SPINE_FC_001",
                 ConformanceLayers.SPINE, "FC",
                 "Reject non-primary destinations",
                 DUTRoles.Special, "Any", true,
                 [ "SPINE-TS-FC-01" ],
                 [ "PRE_SPINE_ConnectionEstablished" ]) {
                Applicability = "Applicable to any DUT."
            },

            #endregion

            #region 4.4 Detailed discovery

            new ("TC_SPINE_DDISC_001",
                 ConformanceLayers.SPINE, "DDISC",
                 "Initial discovery of unknown partner",
                 DUTRoles.Special, "Any", true,
                 [ "SPINE-TS-DD-01" ],
                 [ "PRE_SPINE_NewConnectionEstablished" ]) {
                Applicability         = "Applicable to any DUT declaring PAR_clientDetailedDiscoverySupported = \"yes\".",
                NotApplicableBecause  = NeedsClientDiscovery
            },

            new ("TC_SPINE_DDISC_002",
                 ConformanceLayers.SPINE, "DDISC",
                 "Disconnect uncommunicative partner",
                 DUTRoles.Special, "Any", true,
                 [ "SPINE-TS-DD-02" ],
                 [ "PRE_SPINE_NewConnectionEstablished" ]) {
                Applicability         = "Applicable to any DUT declaring PAR_initialTimeoutSupported = \"yes\".",
                NotApplicableBecause  = NeedsInitialTimeout
            },

            #endregion

            #region 4.5 Binding

            new ("TC_SPINE_BIND_001",
                 ConformanceLayers.SPINE, "BIND",
                 "Deny NodeManagement binding",
                 DUTRoles.Special, "Any", true,
                 [ "SPINE-TS-BIND-01" ],
                 [ "PRE_SPINE_ConnectionEstablished", "PRE_SPINE_DetailedDiscovery" ]) {
                Applicability = "Applicable to any DUT."
            },

            new ("TC_SPINE_BIND_002",
                 ConformanceLayers.SPINE, "BIND",
                 "Reject unbound write",
                 DUTRoles.Server, "CS", true,
                 [ "SPINE-TS-BIND-02" ],
                 [ "PRE_SPINE_ConnectionEstablished", "PRE_SPINE_Scenario_LpcLpp_S1_S3" ]) {
                Applicability = "Applicable to DUTs which act as controllable system in LPC or LPP."
            },

            #endregion

            #region 4.6 Subscription

            new ("TC_SPINE_SUBS_001",
                 ConformanceLayers.SPINE, "SUBS",
                 "Accept NodeManagement subscription",
                 DUTRoles.Special, "Any", true,
                 [ "SPINE-TS-SUBS-01" ],
                 [ "PRE_SPINE_ConnectionEstablished", "PRE_SPINE_DetailedDiscovery" ]) {
                Applicability = "Applicable to any DUT."
            },

            new ("TC_SPINE_SUBS_002",
                 ConformanceLayers.SPINE, "SUBS",
                 "Idempotent subscription deletion",
                 DUTRoles.Special, "Any", true,
                 [ "SPINE-TS-SUBS-02" ],
                 [ "PRE_SPINE_ConnectionEstablished", "PRE_SPINE_DetailedDiscovery",
                   "PRE_SPINE_SubscriptionEstablished" ]) {
                Applicability = "Applicable to any DUT."
            },

            #endregion

            #region 4.7 Entity settings

            new ("TC_SPINE_ENTITY_001",
                 ConformanceLayers.SPINE, "ENTITY",
                 "Dynamic server discovery",
                 DUTRoles.Client, "EG", true,
                 [ "SPINE-TS-ES-01" ],
                 [ "PRE_SPINE_ConnectionEstablished", "PRE_SPINE_Scenario_LpcLpp_S1_S3" ]) {
                Applicability = "Applicable to DUTs which act as energy guard in LPC or LPP."
            },

            new ("TC_SPINE_ENTITY_002",
                 ConformanceLayers.SPINE, "ENTITY",
                 "Dynamic server subscription",
                 DUTRoles.Client, "CS", true,
                 [ "SPINE-TS-ES-02" ],
                 [ "PRE_SPINE_ConnectionEstablished", "PRE_SPINE_Scenario_LpcLpp_S1_S3",
                   "PRE_SPINE_AddressChangeRecoveryResolved" ]) {
                Applicability = "Applicable to DUTs which act as controllable system in LPC or LPP."
            },

            #endregion

            #region 4.8 Runtime behaviour of a server

            new ("TC_SPINE_RTS_001",
                 ConformanceLayers.SPINE, "RTS",
                 "Tolerate arbitrary client Feature types",
                 DUTRoles.Server, "CS", true,
                 [ "SPINE-TS-RT-01", "SPINE-TS-DATA-05" ],
                 [ "PRE_TestTool_ReplyDiscoveryKnownArbitraryType_Configured",
                   "PRE_SPINE_NewConnectionEstablished" ]) {
                Applicability = "Applicable to DUTs which act as controllable system in LPC or LPP."
            },

            new ("TC_SPINE_RTS_002",
                 ConformanceLayers.SPINE, "RTS",
                 "Tolerate unknown Feature types",
                 DUTRoles.Server, "CS", true,
                 [ "SPINE-TS-COMP-02" ],
                 [ "PRE_TestTool_ReplyDiscoveryUnknownFutureType_Configured",
                   "PRE_TestTool_AsFutureDevice", "PRE_SPINE_NewConnectionEstablished" ]) {
                Applicability = "Applicable to DUTs which act as controllable system in LPC or LPP."
            },

            new ("TC_SPINE_RTS_003",
                 ConformanceLayers.SPINE, "RTS",
                 "Deduce client address from client's binding request",
                 DUTRoles.Server, "CS", true,
                 [ "SPINE-TS-RT-02" ],
                 [ "PRE_TestTool_Multi_EG_Configured", "PRE_SPINE_NewConnectionEstablished",
                   "PRE_SPINE_DetailedDiscovery" ]) {
                Applicability = "Applicable to DUTs which act as controllable system in LPC or LPP."
            },

            new ("TC_SPINE_RTS_004",
                 ConformanceLayers.SPINE, "RTS",
                 "Ignore unknown payload Elements",
                 DUTRoles.Server, "CS", true,
                 [ "SPINE-TS-COMP-02", "SPINE-TS-COMP-03" ],
                 [ "PRE_TestTool_AsFutureDevice", "PRE_SPINE_ConnectionEstablished",
                   "PRE_SPINE_Scenario_LpcLpp_S1_S3" ]) {
                Applicability = "Applicable to DUTs which act as controllable system in LPC or LPP."
            },

            new ("TC_SPINE_RTS_005",
                 ConformanceLayers.SPINE, "RTS",
                 "Apply RFE merge logic ScaledNumberType",
                 DUTRoles.Server, "CS", true,
                 [ "SPINE-TS-RT-03" ],
                 [ "PRE_SPINE_ConnectionEstablished", "PRE_SPINE_Scenario_LpcLpp_S1_S3" ]) {
                Applicability = "Applicable to DUTs which act as controllable system in LPC or LPP."
            },

            #endregion

            #region 4.9 Runtime behaviour of a client

            new ("TC_SPINE_RTC_001",
                 ConformanceLayers.SPINE, "RTC",
                 "Ignore extra server Elements",
                 DUTRoles.Client, "EG", true,
                 [ "SPINE-TS-COMP-03" ],
                 [ "PRE_TestTool_ReplyLpcLppExtraElement_Configured",
                   "PRE_SPINE_NewConnectionEstablished" ]) {
                Applicability = "Applicable to DUTs which act as energy guard in LPC or LPP."
            },

            new ("TC_SPINE_RTC_002",
                 ConformanceLayers.SPINE, "RTC",
                 "Ignore unknown server Elements",
                 DUTRoles.Client, "EG", true,
                 [ "SPINE-TS-COMP-02", "SPINE-TS-COMP-03" ],
                 [ "PRE_TestTool_ReplyLpcLppUnknownElement_Configured",
                   "PRE_TestTool_AsFutureDevice", "PRE_SPINE_NewConnectionEstablished" ]) {
                Applicability = "Applicable to DUTs which act as energy guard in LPC or LPP."
            },

            new ("TC_SPINE_RTC_003",
                 ConformanceLayers.SPINE, "RTC",
                 "Ignore useCaseAvailable flag",
                 DUTRoles.Client, "EG", true,
                 [ "SPINE-TS-UCD-01" ],
                 [ "PRE_TestTool_ReplyUcdAvailableFalse_Configured",
                   "PRE_SPINE_NewConnectionEstablished" ]) {
                Applicability = "Applicable to DUTs which act as energy guard in LPC or LPP."
            }

            #endregion

        ];

        #endregion

    }

}
