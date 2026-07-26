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
    /// The LPC and LPP halves of the use case catalog:
    /// EEBus_UC_HighLevel_TestSpecification_LPC_V1.0.2 and its production twin -
    /// 51 abstract test cases and 76 requirements each.
    ///
    /// Both are written here once and instantiated twice, because the two
    /// specifications are one specification pointed in opposite directions. That
    /// is not a guess: their abstract test case identifiers match one for one
    /// after the prefix, so do their requirement numbers - [LPC-TS-919] and
    /// [LPP-TS-919] are the same sentence about a different direction of energy -
    /// and the stack already shares one implementation between them (ADR 0006).
    /// Writing the table twice would be an invitation for the two copies to drift
    /// apart, which is exactly the failure a conformance catalog cannot afford.
    ///
    /// What does differ is the wording, and the wording is what a report shows,
    /// so every text below is a template over five tokens: the limit's
    /// abbreviation, the failsafe limit's, the direction, the noun and the
    /// nominal maximum's name.
    /// </summary>
    public static class PowerLimitationCatalog
    {

        #region (class) Flavour

        /// <summary>
        /// Which of the two use cases a generated entry belongs to, and how it
        /// words itself.
        /// </summary>
        /// <param name="Abbreviation">"LPC" or "LPP".</param>
        /// <param name="Source">The use case document, for the report.</param>
        /// <param name="Limit">What the active power limit is called: "APCL" or "APPL".</param>
        /// <param name="Failsafe">What the failsafe limit is called: "FCAPL" or "FPAPL".</param>
        /// <param name="Noun">"consumption" or "production".</param>
        /// <param name="Verb">"consume" or "produce".</param>
        private sealed record Flavour(String  Abbreviation,
                                      String  Source,
                                      String  Limit,
                                      String  Failsafe,
                                      String  Noun,
                                      String  Verb)
        {

            /// <summary>
            /// A text with the five tokens filled in.
            /// </summary>
            /// <param name="Template">A text using {L}, {F}, {noun} and {verb}.</param>
            public String Say(String Template)

                => Template.Replace("{L}",     Limit).
                            Replace("{F}",     Failsafe).
                            Replace("{noun}",  Noun).
                            Replace("{verb}",  Verb);

        }

        private static readonly Flavour consumption
            = new ("LPC", UseCaseSources.LPC, "APCL", "FCAPL", "consumption", "consume");

        private static readonly Flavour production
            = new ("LPP", UseCaseSources.LPP, "APPL", "FPAPL", "production",  "produce");

        #endregion

        #region Data

        private static readonly Lazy<IReadOnlyList<ConformanceTestCase>> testCases = new (
            () => [ .. Cases(consumption),
                    .. Cases(production) ]
        );

        private static readonly Lazy<IReadOnlyList<ConformanceRequirement>> requirements = new (
            () => [ .. Rules(consumption),
                    .. Rules(production) ]
        );

        #endregion

        #region Properties

        /// <summary>The 102 abstract test cases of the two power limitation use cases.</summary>
        public static IReadOnlyList<ConformanceTestCase>     TestCases
            => testCases.Value;

        /// <summary>The 152 requirements they map to.</summary>
        public static IReadOnlyList<ConformanceRequirement>  Requirements
            => requirements.Value;

        #endregion


        #region (private) Rules(Flavour)

        /// <summary>
        /// The requirements of chapter 5.2 of one of the two specifications.
        ///
        /// Fifteen of the 76 are marked out of scope by the specification itself,
        /// and they are kept rather than dropped: a requirement which the test
        /// specification declines to test is a fact about the test specification,
        /// and a coverage table which quietly omits them would read as if the
        /// document tested everything it wrote down.
        /// </summary>
        private static IEnumerable<ConformanceRequirement> Rules(Flavour Flavour)
        {

            ConformanceRequirement Rule(String   Number,
                                        String   Section,
                                        String   Text,
                                        String?  OutOfScope   = null)

                => new ($"[{Flavour.Abbreviation}-TS-{Number}]",
                        $"{Flavour.Source}, {Section}",
                        Flavour.Say(Text)) {
                       OutOfScope = OutOfScope
                   };


            #region Scenario 1: the limit itself

            yield return Rule("001",    "2.8.1",          "The {L} is always greater than or equal to zero.");
            yield return Rule("001/1",  "2.6.1.1",        "A limit may carry a duration saying how long it is valid for.");
            yield return Rule("001/2",  "2.6.1.1",        "The energy guard may activate or deactivate the limit.");
            yield return Rule("002",    "2.2, 2.6.1.1",   "The controllable system confirms an accepted {L} with an ACK.");
            yield return Rule("003",    "2.6.2.1",        "The controllable system confirms an accepted {F} with an ACK.");
            yield return Rule("004",    "2.2, 2.6.1.1",   "A {L} which cannot be applied is answered with a NACK.");
            yield return Rule("005",    "2.2, 2.6.2.1",   "A write of the {F} or of the failsafe duration minimum which is not accepted is declined with a NACK.");

            #endregion

            #region Scenario 3: the heartbeats

            yield return Rule("006",    "2.1, 2.6.3.1",   "The energy guard sends its heartbeat at least every 60 seconds.");
            yield return Rule("007",    "2.1, 2.6.3.1",   "The controllable system sends its heartbeat at least every 60 seconds.");

            #endregion

            #region The limit's duration and activation

            yield return Rule("008",    "2.6.1.1",        "A limit with a duration is deactivated as soon as the duration reaches zero.");
            yield return Rule("008/1",  "2.6.1.1",        "The controllable system may remove the duration once it has expired.");
            yield return Rule("009",    "2.6.1.1",        "The controllable system sets the {L} to activated or deactivated according to its own state.");
            yield return Rule("009/1",  "2.3.2",          "In state \"limited\" the {L} is activated.");
            yield return Rule("009/2",  "2.3.2",          "After a restart the {L} is deactivated.");
            yield return Rule("009/3",  "2.3.2",          "In \"init\", \"unlimited/controlled\", \"failsafe state\" and \"unlimited/autonomous\" the {L} is deactivated.");

            #endregion

            #region Scenario 4: the nominal maximum

            yield return Rule("010",    "2.2",            "The controllable system never {verb}s more than its nominal maximum.");
            yield return Rule("010/1",  "2.2",            "A controllable system which is not an energy manager should report its Power {noun} Nominal Max.");
            yield return Rule("010/2",  "2.6.4.1",        "Power {noun} Nominal Max should be supported where the controllable system is not an energy manager.");
            yield return Rule("010/3",  "2.2",            "A controllable system on an energy manager should report its Contractual {noun} Nominal Max.");
            yield return Rule("010/4",  "2.6.4.1",        "Contractual {noun} Nominal Max should be supported where the controllable system is an energy manager.");

            #endregion

            #region Scenario 2: the failsafe values

            yield return Rule("011",    "2.2, 2.6.2.1",   "A default value for the {F} is configured.");
            yield return Rule("011/1",  "2.6.2.1",        "The {F} may be changed by the energy guard.");
            yield return Rule("011/2",  "2.6.2.1",        "Once the energy guard has changed the {F}, it should no longer be configurable at the device itself.",
                                                          UseCaseSources.LocalRegulations);
            yield return Rule("012",    "2.1",            "The controllable system stays in its failsafe state for at least the failsafe duration minimum.");
            yield return Rule("013",    "2.6.2.1",        "The failsafe duration minimum is pre-configured by the vendor, between two and 24 hours.");
            yield return Rule("013/1",  "2.6.2.1",        "The failsafe duration minimum may be changed by the energy guard.");
            yield return Rule("013/2",  "2.6.2.1",        "Once the energy guard has changed it, it should no longer be configurable at the device itself.",
                                                          UseCaseSources.LocalRegulations);
            yield return Rule("014",    "2.6.2.1",        "The largest failsafe duration minimum the controllable system accepts lies between its pre-configured value and 24 hours.");
            yield return Rule("015",    "2.6.2.1",        "The energy guard chooses a failsafe duration minimum between two and 24 hours.");
            yield return Rule("015/1",  "2.6.2.1",        "The controllable system may reject a failsafe duration minimum above its own maximum.");
            yield return Rule("016",    "2.6.2.1",        "Having rejected such a write, the controllable system changes its failsafe duration minimum to its own maximum.");

            #endregion

            #region The state machine of section 2.3

            yield return Rule("017",    "2.2, 2.3.2",     "After a restart the controllable system begins limited to its {F}.");
            yield return Rule("017/1",  "2.2",            "A controllable system on an energy manager may exceed the {F} while listed conditions prevent keeping it.",
                                                          UseCaseSources.DataQuality);
            yield return Rule("018",    "2.2, 2.3.3",     "A heartbeat and a following activated limit which is not accepted takes the controllable system from \"init\" to \"unlimited/controlled\".");
            yield return Rule("019",    "2.2, 2.3.3",     "Without an earlier change, or after losing it in a restart, the controllable system uses its pre-configured {F}.");
            yield return Rule("020",    "2.2, 2.3.3",     "A heartbeat and a following accepted activated limit takes the controllable system from \"init\" to \"limited\".");
            yield return Rule("021",    "2.2, 2.3.3",     "A heartbeat and a following deactivated limit takes the controllable system from \"init\" to \"unlimited/controlled\".");
            yield return Rule("022",    "2.2, 2.3.3",     "From \"init\" and from the failsafe state the controllable system may switch to \"unlimited/autonomous\".");
            yield return Rule("022/1",  "2.2, 2.3.3",     "No heartbeat, or a heartbeat without a following limit, within 120 seconds of entering \"init\" may take the controllable system to \"unlimited/autonomous\".");
            yield return Rule("022/2",  "2.2, 2.3.3",     "A heartbeat in the failsafe state without a following limit within 120 seconds may take the controllable system to \"unlimited/autonomous\".");
            yield return Rule("022/3",  "2.2, 2.3.3",     "After the failsafe duration minimum has expired the controllable system may switch to \"unlimited/autonomous\".");
            yield return Rule("022/4",  "2.2",            "A controllable system on an energy manager may exceed the {F} only while one of the listed conditions holds.",
                                                          UseCaseSources.DataQuality);
            yield return Rule("022/5",  "2.2",            "A controllable system which is not an energy manager may exceed the {F} only while one of the listed conditions holds.",
                                                          UseCaseSources.DataQuality);
            yield return Rule("023",    "2.2",            "Rejecting a limit leaves the controllable system in \"unlimited/controlled\" if it was there.");
            yield return Rule("024",    "2.2",            "Rejecting a limit leaves the controllable system in \"limited\" if it was there.");
            yield return Rule("025",    "2.2, 2.3.3",     "An expired limit duration takes the controllable system from \"limited\" to \"unlimited/controlled\".");
            yield return Rule("026",    "2.2, 2.3.3",     "A deactivation of the limit takes the controllable system from \"limited\" to \"unlimited/controlled\".");
            yield return Rule("027",    "2.2, 2.3.3",     "An accepted activated limit takes the controllable system from \"unlimited/controlled\" to \"limited\".");
            yield return Rule("028",    "2.2, 2.3.3",     "No energy guard heartbeat within 120 seconds takes the controllable system from \"unlimited/controlled\" to its failsafe state.");
            yield return Rule("029",    "2.2, 2.3.3",     "No energy guard heartbeat within 120 seconds takes the controllable system from \"limited\" to its failsafe state.");
            yield return Rule("030",    "2.2",            "Having established or restored communication, the energy guard sends a heartbeat and a following {L} within 60 seconds.");
            yield return Rule("031",    "2.2, 2.3.3",     "A heartbeat and a following limit which cannot be applied takes the controllable system from the failsafe state or \"unlimited/autonomous\" to \"unlimited/controlled\".");
            yield return Rule("032",    "2.2, 2.3.3",     "A heartbeat and a following accepted activated limit takes the controllable system from the failsafe state or \"unlimited/autonomous\" to \"limited\".");
            yield return Rule("033",    "2.2, 2.3.3",     "A heartbeat and a following deactivated limit takes the controllable system from the failsafe state or \"unlimited/autonomous\" to \"unlimited/controlled\".");
            yield return Rule("034",    "2.2",            "A controllable system on an energy manager manages its connected devices to keep the limit, rather than limiting its own {noun}.",
                                                          UseCaseSources.DataQuality);

            #endregion

            #region Evaluating a limit

            yield return Rule("035",    "2.2",            "On receiving the {L} the controllable system evaluates whether it is able to apply it.");
            yield return Rule("035/1",  "2.2",            "An {L} below zero is rejected.");
            yield return Rule("035/2",  "2.2",            "A controllable system on an energy manager applies the {L} unless one of the listed conditions requires rejecting it.");
            yield return Rule("035/3",  "2.2",            "A controllable system which is not an energy manager applies the {L} unless one of the listed conditions requires rejecting it.");
            yield return Rule("035/4",  "2.2",            "An {L} may exceed what the device can {verb}; a limit too large to store may be altered to the largest storable value.");
            yield return Rule("036",    "2.2",            "In \"init\", the failsafe state and \"unlimited/autonomous\", only a limit arriving within 60 seconds of a heartbeat is evaluated.");
            yield return Rule("037",    "2.2",            "In those states, commands on any other data point are evaluated only after a heartbeat and a following limit.");

            #endregion

            #region The data points themselves

            yield return Rule("038",    "2.8.1",          "The {F} and both nominal maxima are always greater than or equal to zero.");
            yield return Rule("039",    "2.6.4.1",        "Power {noun} Nominal Max is not supported where the controllable system is an energy manager.");
            yield return Rule("040",    "2.6.4.1",        "Contractual {noun} Nominal Max is not supported where the controllable system is not an energy manager.");
            yield return Rule("041",    "2.7.1",          "The failsafe duration minimum is the same data point as in the opposite limitation use case.",
                                                          UseCaseSources.LocalRegulations);
            yield return Rule("042",    "2.7.4",          "On an inverter the use case \"Monitoring of Inverter\" is taken into account.",
                                                          UseCaseSources.LocalRegulations);
            yield return Rule("042/1",  "2.7.4",          "The rules on the resource hierarchy of an inverter are followed.",
                                                          UseCaseSources.LocalRegulations);
            yield return Rule("043",    "2.2",            "The energy guard should support the monitoring use cases as monitoring appliance.",
                                                          UseCaseSources.LocalRegulations);
            yield return Rule("043/1",  "2.2",            "The energy guard should monitor the actual power {noun} of the controllable system.",
                                                          UseCaseSources.LocalRegulations);
            yield return Rule("043/2",  "2.2",            "The controllable system should provide its actual power {noun}.",
                                                          UseCaseSources.LocalRegulations);
            yield return Rule("043/3",  "2.2",            "On an energy manager, \"Monitoring of Grid Connection Point\" is used to provide it.",
                                                          UseCaseSources.LocalRegulations);
            yield return Rule("043/4",  "2.2",            "Otherwise the matching monitoring use case is used to provide it.",
                                                          UseCaseSources.LocalRegulations);
            yield return Rule("044",    "2.6.2.1",        "The controllable system should store a changed {F} and failsafe duration minimum persistently.");
            yield return Rule("045",    "2.2",            "In \"limited\" the controllable system may deactivate the limit only under the listed conditions.",
                                                          UseCaseSources.DataQuality);
            yield return Rule("046",    "2.2",            "An energy guard expects its writes to be rejected and reacts to a NACK appropriately.");

            #endregion

        }

        #endregion

        #region (private) Cases(Flavour)

        /// <summary>
        /// The 51 abstract test cases of chapters 7 and 8 of one of the two
        /// specifications: eight with the energy guard as the device under test,
        /// 41 with the controllable system, and one each for the two use case
        /// instances.
        /// </summary>
        private static IEnumerable<ConformanceTestCase> Cases(Flavour Flavour)
        {

            var uc     = Flavour.Abbreviation;
            var sheet  = uc;

            ConformanceTestCase Case(String                          Suffix,
                                     String                          Group,
                                     String                          Title,
                                     String                          Actor,
                                     IReadOnlyList<String>           Requirements,
                                     IReadOnlyList<String>           Preconditions,
                                     UInt32                          SpecificTestCases     = 1,
                                     String?                         Variation             = null,
                                     Func<ParameterSheet, String?>?  NotApplicableBecause  = null,
                                     String?                         Applicability         = null)

                => new ($"ATC_{uc}_{Suffix}",
                        ConformanceLayers.UseCase,
                        $"{uc} {Group}",
                        Flavour.Say(Title),
                        Actor == "EG" ? DUTRoles.Client : DUTRoles.Server,
                        Actor,
                        Mandatory:      true,
                        Requirements:   [.. Requirements.Select(number => $"[{uc}-TS-{number}]")],
                        Preconditions:  Preconditions) {
                       SpecificTestCases     = SpecificTestCases,
                       Variation             = Variation is not null ? Flavour.Say(Variation) : null,
                       NotApplicableBecause  = NotApplicableBecause,
                       Applicability         = Applicability
                   };


            #region The "Optional Support" answers which decide applicability

            // Every one of these is a row of the parameter sheet's "Optional
            // Support" worksheet. The rule the specification states is the same
            // for all of them: the case is recommended until the manufacturer
            // declares the functionality, and mandatory once they do. So a "no"
            // here is not a failure and not a pass - it is a question which was
            // never asked.
            PowerLimitationParameters Sheet(ParameterSheet Parameters)
                => Parameters.UseCases.Limitation(sheet);

            Func<ParameterSheet, String?> Declared(Func<PowerLimitationParameters, Boolean> Answer,
                                                   String                                  Row,
                                                   String                                  What)

                => parameters => Answer(Sheet(parameters))
                                     ? null
                                     : $"the device declares no \"{What}\" (parameter sheet, \"Optional Support\" {Row})";

            #endregion


            #region Chapter 7 - the energy guard as the device under test

            yield return Case("COM_PT_EGHeartbeat_001",
                              "EG",
                              "The energy guard sends its heartbeat at least every 60 seconds",
                              "EG",
                              [ "006" ],
                              [ "CF_EG_ConnectionEstablished", "CF_CS_UnlCntrl" ]);

            yield return Case("COM_PT_EGConnection_001",
                              "EG",
                              "The energy guard sends a heartbeat and a following {L} after it has rebooted",
                              "EG",
                              [ "030" ],
                              [ "CF_EG_Reboot", "CF_CS_FS" ]);

            yield return Case("COM_PT_EGConnection_002",
                              "EG",
                              "The energy guard sends a heartbeat and a following {L} after the connection is restored",
                              "EG",
                              [ "030" ],
                              [ "CF_EG_ConnectionLoss", "CF_CS_FS" ]);

            yield return Case("COM_PT_EGConnection_003",
                              "EG",
                              "The energy guard reconnects by itself after a black start",
                              "EG",
                              [ "030" ],
                              [ "CF_EG_ConnectionEstablished", "CF_CS_UnlCntrl" ],
                              NotApplicableBecause:  Declared(sheetOf => sheetOf.GuardBlackStart, "E1", "black start"),
                              Applicability:         "Mandatory where the energy guard is black start capable (Table 13, footnote 3).");

            yield return Case("COM_PT_EGMessages_001",
                              "EG",
                              "An external stimulus makes the energy guard write an activated {L}",
                              "EG",
                              [ "001" ],
                              [ "CF_EG_ConnectionEstablished", "CF_CS_UnlCntrl" ],
                              SpecificTestCases:  3,
                              Variation:          "{L} values (all): {L}_02, {L}_03, {L}_04");

            yield return Case("COM_PT_EGMessages_002",
                              "EG",
                              "The energy guard resends its limit after the controllable system has rejected it",
                              "EG",
                              [ "046" ],
                              [ "CF_EG_Reboot", "CF_CS_UnlAuto" ],
                              SpecificTestCases:  6,
                              Variation:             "{L} values (all): {L}_02, {L}_03, {L}_04",
                              NotApplicableBecause:  Declared(sheetOf => sheetOf.GuardResendsAfterReject, "A5",
                                                              "quick resend write {L} if previous write {L} was rejected"));

            yield return Case("COM_PT_EGMessages_003",
                              "EG",
                              "The energy guard writes valid limits over an extended period",
                              "EG",
                              [ "001", "001/2", "002" ],
                              [ "CF_EG_ConnectionEstablished", "CF_CS_UnlCntrl" ],
                              NotApplicableBecause:  Declared(sheetOf => sheetOf.GuardWritesLimit, "A1",
                                                              "read/write {L} (value, activation flag, duration)"));

            yield return Case("COM_PT_EGMessages_004",
                              "EG",
                              "The energy guard writes valid failsafe values over an extended period",
                              "EG",
                              [ "003", "011/1", "013/1" ],
                              [ "CF_EG_ConnectionEstablished", "CF_CS_UnlCntrl" ],
                              NotApplicableBecause:  Declared(sheetOf => sheetOf.GuardWritesFailsafe, "A2",
                                                              "read/write {F} and Failsafe Duration Minimum"));

            #endregion

            #region Chapter 8.2.1 to 8.2.7 - the controllable system, state by state

            yield return Case("COM_PT_CSHeartbeat_001",
                              "CS",
                              "The controllable system sends its heartbeat at least every 60 seconds",
                              "CS",
                              [ "007" ],
                              [ "CF_EG_ConnectionEstablished", "CF_CS_UnlCntrl" ]);

            yield return Case("COM_NT_CSConnection_001",
                              "CS",
                              "The controllable system evaluates no limit before the first heartbeat",
                              "CS",
                              [ "004", "036" ],
                              [ "CF_EG_ManualExecution", "CF_CS_Init" ],
                              SpecificTestCases:     2,
                              Variation:  "Message combinations (any): MSG_16; {L} values (all): {L}_03");

            yield return Case("COM_PT_CSConnection_002",
                              "CS",
                              "The controllable system evaluates no {F} write before a heartbeat and a limit",
                              "CS",
                              [ "003", "036", "037", "038" ],
                              [ "CF_EG_ManualExecution", "CF_CS_Init" ],
                              SpecificTestCases:     2,
                              Variation:  "{F} values (all): {F}_03");

            yield return Case("COM_PT_CSConnection_003",
                              "CS",
                              "The controllable system accepts only {L} and {F} values above zero",
                              "CS",
                              [ "005", "018", "038" ],
                              [ "CF_EG_ManualExecution", "CF_CS_Init" ],
                              SpecificTestCases:     2,
                              Variation:  "{L} values (all): {L}_06; {F} values (all): {F}_06, {F}_03");

            yield return Case("COM_PT_CSConnection_004",
                              "CS",
                              "The controllable system evaluates no failsafe duration write before a heartbeat and a limit",
                              "CS",
                              [ "005", "037" ],
                              [ "CF_EG_ManualExecution", "CF_CS_Init" ],
                              Variation:  "Failsafe Duration Minimum values (all): {F}_DUR_02");

            yield return Case("COM_PT_CSConnection_005",
                              "CS",
                              "The controllable system handles a failsafe duration minimum above its own maximum",
                              "CS",
                              [ "014", "015", "015/1", "016" ],
                              [ "CF_EG_ManualExecution", "CF_CS_Init" ],
                              Variation:  "Failsafe Duration Minimum values (all): {F}_DUR_03");

            yield return Case("COM_PT_CSConnection_006",
                              "CS",
                              "The controllable system alters an {L} larger than it can store",
                              "CS",
                              [ "035/4" ],
                              [ "CF_EG_ManualExecution", "CF_CS_Init" ],
                              Variation:             "{L} values (all): {L}_05; {L} duration values (all): {L}_DUR_02",
                              NotApplicableBecause:  Declared(sheetOf => sheetOf.SystemAltersTooLargeLimit, "D1",
                                                              "auto-adjust too large received {L}"));

            yield return Case("COM_PT_CSConnection_007",
                              "CS",
                              "The controllable system evaluates {L} writes correctly across the whole range",
                              "CS",
                              [ "001", "035", "035/4" ],
                              [ "CF_EG_ManualExecution", "CF_CS_Init" ],
                              SpecificTestCases:  10,
                              Variation:          "{L} values (all): {L}_02, {L}_03, {L}_04, {L}_05 and {L}_01");

            yield return Case("COM_PT_CSConnection_008",
                              "CS",
                              "The controllable system evaluates {F} and failsafe duration writes correctly",
                              "CS",
                              [ "001", "015/1", "016", "038" ],
                              [ "CF_EG_ConnectionEstablished", "CF_CS_UnlCntrl" ],
                              SpecificTestCases:  7,
                              Variation:          "{F} values (all): {F}_01..{F}_05; Failsafe Duration Minimum values (all): {F}_DUR_01, {F}_DUR_02, {F}_DUR_03");

            yield return Case("COM_PT_CSConnection_009",
                              "CS",
                              "The controllable system reconnects by itself after a black start",
                              "CS",
                              [ "046" ],
                              [ "CF_EG_ConnectionEstablished", "CF_CS_UnlCntrl" ],
                              SpecificTestCases:     2,
                              Variation:             "Message combinations (any): MSG_16; {L} values (all): {L}_03",
                              NotApplicableBecause:  Declared(sheetOf => sheetOf.SystemBlackStart, "E2", "black start"),
                              Applicability:         "Mandatory where the controllable system is black start capable (Table 13, footnote 3).");

            yield return Case("COM_PT_CSInit_001",
                              "CS",
                              "The controllable system starts limited to its {F} with a deactivated {L}",
                              "CS",
                              [ "009/3", "011", "017", "019" ],
                              [ "CF_EG_ManualExecution", "CF_CS_Reset_Init" ]);

            yield return Case("COM_PT_CSInit_002",
                              "CS",
                              "The controllable system starts with its default parameters after a factory reset",
                              "CS",
                              [ "009/2", "009/3", "011", "013" ],
                              [ "CF_EG_ManualExecution", "CF_CS_Reset_Init" ]);

            yield return Case("COM_PT_CSInit_003",
                              "CS",
                              "The controllable system stores the {F} and the failsafe duration minimum persistently",
                              "CS",
                              [ "011/1", "013/1", "044" ],
                              [ "CF_EG_ManualExecution", "CF_CS_Init" ],
                              SpecificTestCases:     2,
                              Variation:             "{F} values (all): {F}_03; Failsafe Duration Minimum values (all): {F}_DUR_02",
                              NotApplicableBecause:  Declared(sheetOf => sheetOf.SystemStoresPersistently, "D3",
                                                              "store {F} and Failsafe Duration Minimum values persistently"));

            yield return Case("COM_NT_CSLimited_001",
                              "CS",
                              "A rejected limit leaves the controllable system limited and activated",
                              "CS",
                              [ "009/1", "024", "035/1" ],
                              [ "CF_EG_ConnectionEstablished", "CF_CS_Limited_wo_dur" ],
                              Variation:  "{L} values (all): {L}_06");

            yield return Case("COM_PT_CSLimited_002",
                              "CS",
                              "The controllable system keeps accepting limits while the heartbeat is briefly absent",
                              "CS",
                              [ "001/2", "002" ],
                              [ "CF_EG_ManualExecution", "CF_CS_Init" ],
                              SpecificTestCases:     2,
                              Variation:  "{L} values (all): {L}_03; {L} duration values (all): {L}_DUR_02");

            yield return Case("COM_NT_CSUnlCntrl_001",
                              "CS",
                              "A rejected limit leaves the controllable system in \"unlimited/controlled\"",
                              "CS",
                              [ "009", "009/3", "023" ],
                              [ "CF_EG_ConnectionEstablished", "CF_CS_UnlCntrl" ],
                              Variation:  "{L} values (all): {L}_06");

            yield return Case("COM_PT_CSUnlCntrl_002",
                              "CS",
                              "An energy manager reports its Contractual {noun} Nominal Max and not the other one",
                              "CS",
                              [ "010/3", "010/4", "038", "039" ],
                              [ "CF_EG_ConnectionEstablished", "CF_CS_UnlCntrl" ],
                              NotApplicableBecause:  parameters => Sheet(parameters).SystemIsEnergyManager
                                                                       ? null
                                                                       : "the controllable system is not an energy manager, so it reports the Power Nominal Max instead (Table 13, footnote 4)",
                              Applicability:         "Exactly one of this case and CSUnlCntrl_003 is executed, decided by \"CS type CEM?\".");

            yield return Case("COM_PT_CSUnlCntrl_003",
                              "CS",
                              "A device which is not an energy manager reports its Power {noun} Nominal Max and not the other one",
                              "CS",
                              [ "010/1", "010/2", "038", "040" ],
                              [ "CF_EG_ConnectionEstablished", "CF_CS_UnlCntrl" ],
                              NotApplicableBecause:  parameters => Sheet(parameters).SystemIsEnergyManager
                                                                       ? "the controllable system is an energy manager, so it reports the Contractual Nominal Max instead (Table 13, footnote 4)"
                                                                       : null,
                              Applicability:         "Exactly one of this case and CSUnlCntrl_002 is executed, decided by \"CS type CEM?\".");

            yield return Case("COM_PT_CSFS_001",
                              "CS",
                              "In its failsafe state the controllable system evaluates nothing before a heartbeat and a limit",
                              "CS",
                              [ "033", "036", "037" ],
                              [ "CF_EG_ManualExecution", "CF_CS_FS" ],
                              SpecificTestCases:     2,
                              Variation:  "{L} values (all): {L}_03; {F} values (all): {F}_03; Failsafe Duration Minimum values (any): {F}_DUR_02");

            yield return Case("COM_PT_CSFS_002",
                              "CS",
                              "The controllable system stays in its failsafe state for the failsafe duration minimum",
                              "CS",
                              [ "012", "013" ],
                              [ "CF_EG_ConnectionLoss", "CF_CS_FS" ]);

            yield return Case("COM_PT_CSFS_003",
                              "CS",
                              "The controllable system rejects a failsafe duration write while in its failsafe state",
                              "CS",
                              [ "009", "009/3" ],
                              [ "CF_EG_ConnectionLoss", "CF_CS_FS" ],
                              Variation:  "Failsafe Duration Minimum values (any): {F}_DUR_02");

            yield return Case("COM_NT_CSUnlAuto_001",
                              "CS",
                              "In \"unlimited/autonomous\" the controllable system evaluates nothing before a heartbeat and a limit",
                              "CS",
                              [ "033", "036", "037" ],
                              [ "CF_EG_ManualExecution", "CF_CS_UnlAuto" ],
                              SpecificTestCases:     2,
                              Variation:             "{L} values (all): {L}_03; {F} values (all): {F}_03",
                              NotApplicableBecause:  Declared(sheetOf => sheetOf.SystemHasAutonomousState, "D2",
                                                              "state \"unlimited/autonomous\" implemented"));

            yield return Case("COM_PT_CSUnlAuto_002",
                              "CS",
                              "The controllable system stays below its nominal maximum with the limit deactivated",
                              "CS",
                              [ "009/3", "010", "038" ],
                              [ "CF_EG_ConnectionLoss", "CF_CS_UnlAuto" ],
                              NotApplicableBecause:  parameters => Sheet(parameters).PhysicalMeasurement
                                                                       ? null
                                                                       : "step 1 compares the actual power against the nominal maximum, which is a physical measurement " +
                                                                         "rather than anything on the wire (parameter sheet, \"Supplementary optional verifications\" M1/N1)",
                              Applicability:         "Needs a tester which can measure what the device actually {verb}s.");

            #endregion

            #region Chapter 8.2.8 to 8.2.19 - one case per transition of section 2.3.3

            // The twelve transitions, in the specification's own numbering. Two
            // of them are reached in two ways and therefore have two cases; the
            // eight which end in or leave "unlimited/autonomous" hang on the one
            // declaration D2, because a controllable system is allowed never to
            // enter that state at all.
            var autonomous = Declared(sheetOf => sheetOf.SystemHasAutonomousState, "D2",
                                      "state \"unlimited/autonomous\" implemented");

            yield return Case("COM_PT_CSTransition1_001",
                              "CS",
                              "Transition 1: a rejected activated limit takes \"init\" to \"unlimited/controlled\"",
                              "CS",
                              [ "018", "035/1" ],
                              [ "CF_EG_ManualExecution", "CF_CS_Init" ],
                              Variation:  "{L} values (all): {L}_06");

            yield return Case("COM_PT_CSTransition1_002",
                              "CS",
                              "Transition 1: an accepted deactivated limit takes \"init\" to \"unlimited/controlled\"",
                              "CS",
                              [ "021" ],
                              [ "CF_EG_ManualExecution", "CF_CS_Init" ],
                              Variation:  "{L} values (all): {L}_03; {L} duration values (all): {L}_DUR_01");

            yield return Case("COM_PT_CSTransition2_001",
                              "CS",
                              "Transition 2: an accepted activated limit takes \"init\" to \"limited\"",
                              "CS",
                              [ "020" ],
                              [ "CF_EG_ManualExecution", "CF_CS_Init" ],
                              SpecificTestCases:  3,
                              Variation:          "{L} values (all): {L}_02, {L}_03, {L}_04");

            yield return Case("COM_PT_CSTransition3_001",
                              "CS",
                              "Transition 3: no heartbeat at all takes \"init\" to \"unlimited/autonomous\"",
                              "CS",
                              [ "022", "022/1" ],
                              [ "CF_EG_ManualExecution", "CF_CS_Init" ],
                              NotApplicableBecause:  autonomous);

            yield return Case("COM_PT_CSTransition3_002",
                              "CS",
                              "Transition 3: a heartbeat without a following limit takes \"init\" to \"unlimited/autonomous\"",
                              "CS",
                              [ "022", "022/1" ],
                              [ "CF_EG_ManualExecution", "CF_CS_Init" ],
                              NotApplicableBecause:  autonomous);

            yield return Case("COM_PT_CSTransition4_001",
                              "CS",
                              "Transition 4: an accepted activated limit takes \"unlimited/controlled\" to \"limited\"",
                              "CS",
                              [ "027" ],
                              [ "CF_EG_ConnectionEstablished", "CF_CS_UnlCntrl" ],
                              SpecificTestCases:  3,
                              Variation:          "{L} values (all): {L}_02, {L}_03, {L}_04; {L} duration values (any): {L}_DUR_01");

            yield return Case("COM_PT_CSTransition5_001",
                              "CS",
                              "Transition 5: a silent heartbeat takes \"unlimited/controlled\" to the failsafe state",
                              "CS",
                              [ "028" ],
                              [ "CF_EG_ConnectionEstablished", "CF_CS_UnlCntrl" ]);

            yield return Case("COM_PT_CSTransition6_001",
                              "CS",
                              "Transition 6: an expired duration takes \"limited\" to \"unlimited/controlled\"",
                              "CS",
                              [ "001/1", "008", "008/1", "025" ],
                              [ "CF_EG_ConnectionEstablished", "CF_CS_Limited_wo_dur" ],
                              Variation:  "{L} values (all): {L}_03; {L} duration values (all): {L}_DUR_01");

            yield return Case("COM_PT_CSTransition6_002",
                              "CS",
                              "Transition 6: a deactivation takes \"limited\" to \"unlimited/controlled\"",
                              "CS",
                              [ "026" ],
                              [ "CF_EG_ConnectionEstablished", "CF_CS_Limited_wo_dur" ],
                              Variation:  "{L} values (all): {L}_03; {L} duration values (any): {L}_DUR_01");

            yield return Case("COM_PT_CSTransition7_001",
                              "CS",
                              "Transition 7: a silent heartbeat takes \"limited\" to the failsafe state",
                              "CS",
                              [ "029" ],
                              [ "CF_EG_ConnectionEstablished", "CF_CS_Limited_wo_dur" ],
                              SpecificTestCases:  3,
                              Variation:          "{F} values (all): {F}_02, {F}_03, {F}_04");

            yield return Case("COM_PT_CSTransition8_001",
                              "CS",
                              "Transition 8: a limit which cannot be applied takes the failsafe state to \"unlimited/controlled\"",
                              "CS",
                              [ "031", "035/1" ],
                              [ "CF_EG_ConnectionLoss", "CF_CS_FS" ],
                              Variation:  "{L} values (all): {L}_06");

            yield return Case("COM_PT_CSTransition8_002",
                              "CS",
                              "Transition 8: a deactivated limit takes the failsafe state to \"unlimited/controlled\"",
                              "CS",
                              [ "033" ],
                              [ "CF_EG_ConnectionLoss", "CF_CS_FS" ],
                              SpecificTestCases:  3,
                              Variation:          "{L} values (all): {L}_02, {L}_03, {L}_04");

            yield return Case("COM_PT_CSTransition9_001",
                              "CS",
                              "Transition 9: an accepted activated limit takes the failsafe state to \"limited\"",
                              "CS",
                              [ "032" ],
                              [ "CF_EG_ConnectionLoss", "CF_CS_FS" ],
                              SpecificTestCases:  3,
                              Variation:          "{L} values (all): {L}_02, {L}_03, {L}_04; {L} duration values (all): {L}_DUR_02");

            yield return Case("COM_PT_CSTransition10_001",
                              "CS",
                              "Transition 10: the expiring failsafe duration takes the failsafe state to \"unlimited/autonomous\"",
                              "CS",
                              [ "012", "022", "022/3" ],
                              [ "CF_EG_ConnectionEstablished", "CF_CS_UnlCntrl" ],
                              Variation:             "Failsafe Duration Minimum values (any): {F}_DUR_02",
                              NotApplicableBecause:  autonomous);

            yield return Case("COM_PT_CSTransition10_002",
                              "CS",
                              "Transition 10: a heartbeat without a following limit takes the failsafe state to \"unlimited/autonomous\"",
                              "CS",
                              [ "022", "022/2" ],
                              [ "CF_EG_ConnectionLoss", "CF_CS_FS" ],
                              NotApplicableBecause:  autonomous);

            yield return Case("COM_PT_CSTransition11_001",
                              "CS",
                              "Transition 11: a rejected limit takes \"unlimited/autonomous\" to \"unlimited/controlled\"",
                              "CS",
                              [ "031", "035/1" ],
                              [ "CF_EG_ManualExecution", "CF_CS_UnlAuto" ],
                              Variation:             "{L} values (all): {L}_06",
                              NotApplicableBecause:  autonomous);

            yield return Case("COM_PT_CSTransition11_002",
                              "CS",
                              "Transition 11: a deactivated limit takes \"unlimited/autonomous\" to \"unlimited/controlled\"",
                              "CS",
                              [ "033" ],
                              [ "CF_EG_ManualExecution", "CF_CS_UnlAuto" ],
                              Variation:             "{L} values (all): {L}_03",
                              NotApplicableBecause:  autonomous);

            yield return Case("COM_PT_CSTransition12_001",
                              "CS",
                              "Transition 12: an accepted activated limit takes \"unlimited/autonomous\" to \"limited\"",
                              "CS",
                              [ "032" ],
                              [ "CF_EG_ManualExecution", "CF_CS_UnlAuto" ],
                              SpecificTestCases:     3,
                              Variation:             "{L} values (all): {L}_02, {L}_03, {L}_04; {L} duration values (all): {L}_DUR_02",
                              NotApplicableBecause:  autonomous);

            #endregion

            #region Chapters 8.3 and 8.4 - the two use case instances

            // The same case twice, and the difference is one list. A controllable
            // system on an energy manager may refuse a limit for four reasons;
            // one which is not may refuse it for three, because "uncontrolled
            // loads prevent achieving the limit" is not something a single
            // appliance gets to say about itself.
            yield return Case("INS1_PT_CSTransition1_001",
                              "INS1",
                              "On an energy manager the controllable system may reject a limit for a permitted reason",
                              "CS",
                              [ "035", "035/2" ],
                              [ "CF_EG_ManualExecution", "CF_CS_Init" ],
                              SpecificTestCases:  4,
                              Variation:             "{L} values (all): {L}_02, {L}_03",
                              NotApplicableBecause:  parameters => !Sheet(parameters).SystemIsEnergyManager
                                                                       ? "the controllable system is not an energy manager, so this is use case instance 2"
                                                                       : Sheet(parameters).SystemCanRejectPermitted
                                                                             ? null
                                                                             : "the device declares no \"permitted rejection of received {L} write command\" " +
                                                                               "(parameter sheet, \"Optional Support\" B3)",
                              Applicability:         "LPC instance 1: the controllable system is located on a customer energy manager.");

            yield return Case("INS2_PT_CSTransition1_001",
                              "INS2",
                              "Off an energy manager the controllable system may reject a limit for a permitted reason",
                              "CS",
                              [ "035", "035/3" ],
                              [ "CF_EG_ManualExecution", "CF_CS_Init" ],
                              SpecificTestCases:  4,
                              Variation:             "{L} values (all): {L}_02, {L}_03",
                              NotApplicableBecause:  parameters => Sheet(parameters).SystemIsEnergyManager
                                                                       ? "the controllable system is an energy manager, so this is use case instance 1"
                                                                       : Sheet(parameters).SystemCanRejectPermitted
                                                                             ? null
                                                                             : "the device declares no \"permitted rejection of received {L} write command\" " +
                                                                               "(parameter sheet, \"Optional Support\" C3)",
                              Applicability:         "LPC instance 2: the controllable system is not located on a customer energy manager.");

            #endregion

        }

        #endregion

    }

}
