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
    /// The MGCP and MPC halves of the use case catalog:
    /// EEBus_UC_HighLevel_TestSpecification_MGCP_V1.0.2 (47 abstract test cases,
    /// 29 requirements) and its power consumption counterpart (54 and 35).
    ///
    /// These two are shaped very differently from the limitation use cases and
    /// very regularly among themselves. Nothing is written, nothing has a state,
    /// nothing falls back: a monitored device publishes measurements and a
    /// monitoring appliance reads them. So almost every abstract test case is one
    /// cell of a matrix - which data point, on which phase, in which energy
    /// direction, with which value state - and the numbering follows the matrix
    /// rather than any narrative. That is why the phase and voltage families
    /// below are generated: writing out twelve nearly identical voltage cases by
    /// hand is how the eleventh one ends up pointing at the wrong requirement.
    ///
    /// The one asymmetry worth naming is where the negative cases live. A value
    /// marked "out of range" or "error" SHALL be ignored by the monitoring
    /// appliance, and only the appliance can be seen doing that - so every
    /// <c>NT</c> case has the appliance as the device under test, and the
    /// monitored side has none at all (MPC test specification, section 6.11.2).
    /// </summary>
    public static class MonitoringCatalog
    {

        #region (class) Flavour

        /// <summary>
        /// Which of the two monitoring use cases a generated entry belongs to.
        /// </summary>
        /// <param name="Abbreviation">"MGCP" or "MPC".</param>
        /// <param name="Source">The use case document, for the report.</param>
        /// <param name="Measured">The actor which is measured: "GCP" or "MU".</param>
        /// <param name="MeasuredName">What that actor is called in prose.</param>
        /// <param name="ValueState">The requirement about the state of a measured value.</param>
        /// <param name="SignRule">The requirement about the sign of a directed value.</param>
        /// <param name="VoltageRule">The requirement about voltage being direction independent.</param>
        /// <param name="Polling">The supplementary requirement about polling.</param>
        /// <param name="Notification">The supplementary requirement about notification.</param>
        private sealed record Flavour(String  Abbreviation,
                                      String  Source,
                                      String  Measured,
                                      String  MeasuredName,
                                      String  ValueState,
                                      String  SignRule,
                                      String  VoltageRule,
                                      String  Polling,
                                      String  Notification);

        private static readonly Flavour grid
            = new ("MGCP", UseCaseSources.MGCP, "GCP", "grid connection point",
                   "008", "010", "011", "012", "013");

        private static readonly Flavour power
            = new ("MPC",  UseCaseSources.MPC,  "MU",  "monitored unit",
                   "008", "009", "010", "013", "014");

        #endregion

        #region Data

        private static readonly Lazy<IReadOnlyList<ConformanceTestCase>> testCases = new (
            () => [ .. GridCases(),
                    .. PowerCases() ]
        );

        private static readonly Lazy<IReadOnlyList<ConformanceRequirement>> requirements = new (
            () => [ .. GridRules(),
                    .. PowerRules() ]
        );

        #endregion

        #region Properties

        /// <summary>The 101 abstract test cases of the two monitoring use cases.</summary>
        public static IReadOnlyList<ConformanceTestCase>     TestCases
            => testCases.Value;

        /// <summary>The 64 requirements they map to.</summary>
        public static IReadOnlyList<ConformanceRequirement>  Requirements
            => requirements.Value;

        #endregion


        #region (private) The six voltage pairs and the three phases

        /// <summary>
        /// The six phase pairs a voltage is measured between, in the order the
        /// abstract test cases number them: the three against neutral first, then
        /// the three between phases.
        /// </summary>
        private static readonly (String Key, String Name)[] voltagePairs = [
            ("an", "phase A and neutral"),
            ("bn", "phase B and neutral"),
            ("cn", "phase C and neutral"),
            ("ab", "phase A and phase B"),
            ("bc", "phase B and phase C"),
            ("ca", "phase C and phase A")
        ];

        /// <summary>
        /// The three phases a current or a phase-specific power is measured on.
        /// </summary>
        private static readonly (String Key, String Name)[] phases = [
            ("a", "phase A"),
            ("b", "phase B"),
            ("c", "phase C")
        ];

        #endregion

        #region (private) Builders

        /// <summary>
        /// One entry of a monitoring catalog.
        /// </summary>
        private static ConformanceTestCase Case(Flavour                         Flavour,
                                                String                          Suffix,
                                                String                          Group,
                                                String                          Title,
                                                String                          Actor,
                                                IReadOnlyList<String>           Requirements,
                                                UInt32                          SpecificTestCases     = 1,
                                                String?                         Variation             = null,
                                                Func<ParameterSheet, String?>?  NotApplicableBecause  = null,
                                                String?                         Applicability         = null)

            => new ($"ATC_{Flavour.Abbreviation}_{Suffix}",
                    ConformanceLayers.UseCase,
                    $"{Flavour.Abbreviation} {Group}",
                    Title,
                    // The measured side answers and notifies, so it is the SPINE
                    // server; the appliance asks and subscribes.
                    Actor == "MA" ? DUTRoles.Client : DUTRoles.Server,
                    Actor,
                    Mandatory:      true,
                    Requirements:   [.. Requirements.Select(number => $"[{Flavour.Abbreviation}-TS-{number}]")],
                    Preconditions:  [ $"CF_{Flavour.Measured}_ConnectionEstablished", "CF_MA_ConnectionEstablished" ]) {
                   SpecificTestCases     = SpecificTestCases,
                   Variation             = Variation,
                   NotApplicableBecause  = NotApplicableBecause,
                   Applicability         = Applicability
               };


        /// <summary>
        /// Whether the device publishes a data point at all - and, where the data
        /// point is phase-specific, whether it is connected to that phase.
        ///
        /// A meter which measures only its total active power implements the use
        /// case completely (MPC 1.0.0, section 2.1), and only values related to
        /// the connected phases are delivered at all (rule 006/7). Both make a
        /// case not applicable rather than failed.
        /// </summary>
        private static Func<ParameterSheet, String?> Publishes(Flavour  Flavour,
                                                               String   DataPoint,
                                                               String?  Phase   = null)

            => parameters => {

                   var sheet = parameters.UseCases.Monitoring(Flavour.Abbreviation);

                   if (!sheet.Supports(DataPoint))
                       return $"the device does not publish {DataPoint} (parameter sheet, \"Supported data points\")";

                   if (Phase is not null && !sheet.SupportsPhase(Phase))
                       return Phase.Length == 2 && Phase[1] != 'n'
                                  ? $"the device does not measure the voltage between two phases (parameter sheet, \"Phase-to-phase AC Voltage\")"
                                  : $"the device is not connected to phase {Phase.ToUpperInvariant()[0]} (parameter sheet, \"Connected phases\")";

                   return null;

               };


        /// <summary>
        /// Whether the device can be driven in the energy direction a case needs.
        /// </summary>
        private static Func<ParameterSheet, String?> CanGo(Flavour  Flavour,
                                                           String   DataPoint,
                                                           Boolean  Producing)

            => parameters => {

                   var sheet = parameters.UseCases.Monitoring(Flavour.Abbreviation);

                   if (!sheet.Supports(DataPoint))
                       return $"the device does not publish {DataPoint} (parameter sheet, \"Supported data points\")";

                   if (Producing && !sheet.CanProduce)
                       return "the device cannot be made to produce energy (parameter sheet, \"Energy direction\")";

                   if (!Producing && !sheet.CanConsume)
                       return "the device cannot be made to consume energy (parameter sheet, \"Energy direction\")";

                   return null;

               };


        /// <summary>
        /// Whether the device is polled at an interval, respectively whether it
        /// notifies on change.
        ///
        /// The two supplementary requirements of section 5.3 say it plainly: for
        /// appliances which use a mechanism, the matching abstract test cases are
        /// "no longer considered 'recommended' but 'mandatory'". A device which
        /// only subscribes is not failing the polling case.
        /// </summary>
        private static Func<ParameterSheet, String?> Uses(Flavour  Flavour,
                                                          Boolean  Polling)

            => parameters => Polling
                                 ? parameters.UseCases.Monitoring(Flavour.Abbreviation).Polling
                                       ? null
                                       : "the device does not request or send data at an interval (parameter sheet, \"Data transmission\")"
                                 : parameters.UseCases.Monitoring(Flavour.Abbreviation).Notification
                                       ? null
                                       : "the device does not send data on a value change (parameter sheet, \"Data transmission\")";

        #endregion


        #region (private) GridRules() / PowerRules()

        /// <summary>
        /// The 29 requirements of chapter 5 of the MGCP test specification.
        /// </summary>
        private static IEnumerable<ConformanceRequirement> GridRules()
        {

            ConformanceRequirement Rule(String Number, String Section, String Text, String? OutOfScope = null)
                => new ($"[MGCP-TS-{Number}]", $"{UseCaseSources.MGCP}, {Section}", Text) { OutOfScope = OutOfScope };

            yield return Rule("001",    "2.4, 2.4.1.1",   "Scenario 1: the grid connection point provides the PV feed-in power limitation factor.");
            yield return Rule("001/1",  "2.4.1.2",        "The limitation factor and the nominal peak power of each PV system have been configured by the installer according to local regulations.",
                                                          UseCaseSources.LocalRegulations);
            yield return Rule("002",    "2.4, 2.4.2.1",   "Scenario 2: the grid connection point provides its total active power.");
            yield return Rule("003",    "2.4, 2.4.3.1",   "Scenario 3: the grid connection point provides the total energy fed into the public grid.");
            yield return Rule("004",    "2.4, 2.4.4.1",   "Scenario 4: the grid connection point provides the total energy consumed from the public grid.");
            yield return Rule("005",    "2.4",            "Scenario 5: the grid connection point provides its momentary current per phase.");
            yield return Rule("005/1",  "2.4.5.1",        "The phase-specific active AC current on phase A.");
            yield return Rule("005/2",  "2.4.5.1",        "The phase-specific active AC current on phase B.");
            yield return Rule("005/3",  "2.4.5.1",        "The phase-specific active AC current on phase C.");
            yield return Rule("005/4",  "2.4.5.1",        "Only the active current is considered in this use case.");
            yield return Rule("005/5",  "2.4.5.1",        "Only rms values are considered.",
                                                          UseCaseSources.DataQuality);
            yield return Rule("006",    "2.4, 2.4.6.1",   "Scenario 6: the grid connection point provides its phase-specific AC voltages.");
            yield return Rule("006/1",  "2.4.6.1",        "The voltage between phase A and neutral.");
            yield return Rule("006/2",  "2.4.6.1",        "The voltage between phase B and neutral.");
            yield return Rule("006/3",  "2.4.6.1",        "The voltage between phase C and neutral.");
            yield return Rule("006/4",  "2.4.6.1",        "The voltage between phase A and phase B.");
            yield return Rule("006/5",  "2.4.6.1",        "The voltage between phase B and phase C.");
            yield return Rule("006/6",  "2.4.6.1",        "The voltage between phase C and phase A.");
            yield return Rule("006/7",  "2.4.6.1",        "Only values related to the connected phases are delivered.");
            yield return Rule("006/8",  "2.4.6.1",        "Where the scenario is supported, at least one of the voltages is supported.",
                                                          UseCaseSources.DataQuality);
            yield return Rule("007",    "2.4, 2.4.7.1",   "Scenario 7: the grid connection point provides the frequency it measures.");
            yield return Rule("008",    "2.6.2",          "Every measured value carries a state saying whether it is correct or is to be ignored.");
            yield return Rule("008/1",  "2.6.2",          "A value marked \"out of range\" is ignored by the monitoring appliance.");
            yield return Rule("008/2",  "2.6.2",          "A value marked \"error\" is ignored by the monitoring appliance.");
            yield return Rule("009",    "2.4",            "The monitoring appliance supports at least one of the scenarios 2, 3 and 4.",
                                                          UseCaseSources.DataQuality);
            yield return Rule("010",    "2.6.1",          "Current, active power and energy are positive while consuming and negative while producing.");
            yield return Rule("011",    "2.6.1",          "Voltages are measured independently of the energy direction.");
            yield return Rule("012",    "5.3",            "Where an appliance requests or sends data at an interval, the polling test cases are mandatory rather than recommended.");
            yield return Rule("013",    "5.3",            "Where an appliance requests or sends data on a value change, the notification test cases are mandatory rather than recommended.");

        }


        /// <summary>
        /// The 35 requirements of chapter 5 of the MPC test specification.
        /// </summary>
        private static IEnumerable<ConformanceRequirement> PowerRules()
        {

            ConformanceRequirement Rule(String Number, String Section, String Text, String? OutOfScope = null)
                => new ($"[MPC-TS-{Number}]", $"{UseCaseSources.MPC}, {Section}", Text) { OutOfScope = OutOfScope };

            yield return Rule("001",    "2.3, 2.3.1.1",   "Scenario 1: the monitored unit provides its total active power.");
            yield return Rule("002",    "2.3.1.1",        "Scenario 1: a monitored unit on more than one phase may additionally provide the active power per phase.");
            yield return Rule("002/1",  "2.3.1.1",        "The phase-specific active power on phase A.");
            yield return Rule("002/2",  "2.3.1.1",        "The phase-specific active power on phase B.");
            yield return Rule("002/3",  "2.3.1.1",        "The phase-specific active power on phase C.");
            yield return Rule("002/4",  "2.3.1.1",        "The phase-specific values are provided only where the monitored unit knows which phases it is connected to.");
            yield return Rule("003",    "2.3, 2.3.2.1",   "Scenario 2: the monitored unit provides the total energy it has consumed since installation or reset.");
            yield return Rule("003/1",  "2.3.2.1",        "The total consumed energy is provided only where the monitored unit is able to consume energy.");
            yield return Rule("004",    "2.3, 2.3.2.1",   "Scenario 2: the monitored unit provides the total energy it has produced since installation or reset.");
            yield return Rule("004/1",  "2.3.2.1",        "The total produced energy is provided only where the monitored unit is able to produce energy.");
            yield return Rule("005",    "2.3.3",          "Scenario 3: the monitored unit provides its momentary current per phase.");
            yield return Rule("005/1",  "2.3.3.1",        "The phase-specific active AC current on phase A.");
            yield return Rule("005/2",  "2.3.3.1",        "The phase-specific active AC current on phase B.");
            yield return Rule("005/3",  "2.3.3.1",        "The phase-specific active AC current on phase C.");
            yield return Rule("005/4",  "2.3",            "The phase-specific current should be provided where the monitored unit knows its connected phases.");
            yield return Rule("005/5",  "2.3.3.1",        "Only the active current is considered in this use case.",
                                                          UseCaseSources.DataQuality);
            yield return Rule("005/6",  "2.3.3.1",        "Only rms values are considered.",
                                                          UseCaseSources.DataQuality);
            yield return Rule("006",    "2.3, 2.3.4.1",   "Scenario 4: the monitored unit provides its phase-specific AC voltages.");
            yield return Rule("006/1",  "2.3.4.1",        "The voltage between phase A and neutral.");
            yield return Rule("006/2",  "2.3.4.1",        "The voltage between phase B and neutral.");
            yield return Rule("006/3",  "2.3.4.1",        "The voltage between phase C and neutral.");
            yield return Rule("006/4",  "2.3.4.1",        "The voltage between phase A and phase B.");
            yield return Rule("006/5",  "2.3.4.1",        "The voltage between phase B and phase C.");
            yield return Rule("006/6",  "2.3.4.1",        "The voltage between phase C and phase A.");
            yield return Rule("006/7",  "2.3.4.1",        "Only values related to the connected phases are delivered.");
            yield return Rule("007",    "2.3, 2.3.5.1",   "Scenario 5: the monitored unit provides the frequency it measures at its AC connection.");
            yield return Rule("008",    "2.5.2",          "Every measured value carries a state saying whether it is correct or is to be ignored.");
            yield return Rule("008/1",  "2.5.2",          "A value marked \"out of range\" is ignored by the monitoring appliance.");
            yield return Rule("008/2",  "2.5.2",          "A value marked \"error\" is ignored by the monitoring appliance.");
            yield return Rule("009",    "2.5.1",          "Current, active power and energy are positive while consuming and negative while producing.");
            yield return Rule("010",    "2.5.1",          "Voltages are measured independently of the energy direction.");
            yield return Rule("011",    "2.1",            "A monitored unit on several phases may provide per-phase values; the total is always provided regardless.",
                                                          UseCaseSources.DataQuality);
            yield return Rule("012",    "2.1",            "A customer energy manager does not act as monitored unit within this use case.",
                                                          UseCaseSources.DataQuality);
            yield return Rule("013",    "5.3",            "Where an appliance requests or sends data at an interval, the polling test cases are mandatory rather than recommended.");
            yield return Rule("014",    "5.3",            "Where an appliance requests or sends data on a value change, the notification test cases are mandatory rather than recommended.");

        }

        #endregion

        #region (private) GridCases()

        /// <summary>
        /// The 47 abstract test cases of the MGCP test specification: 18 with the
        /// grid connection point as the device under test, 29 with the monitoring
        /// appliance.
        /// </summary>
        private static IEnumerable<ConformanceTestCase> GridCases()
        {

            var uc = grid;

            #region Chapter 7 - the grid connection point as the device under test

            yield return Case(uc, "COM_PT_GCPPolling_001",       "GCP",  "The grid connection point answers a poll within 120 seconds",
                              "GCP", [ "012" ], NotApplicableBecause: Uses(uc, Polling: true));

            yield return Case(uc, "COM_PT_GCPNotification_001",  "GCP",  "The grid connection point sends changed data within 120 seconds",
                              "GCP", [ "013" ], NotApplicableBecause: Uses(uc, Polling: false));

            yield return Case(uc, "SCE1_PT_GCPPowerLimitFactor_001", "GCP", "The grid connection point sends the PV feed-in power limitation factor",
                              "GCP", [ "001" ], NotApplicableBecause: Publishes(uc, "PowerLimitFactor"));

            yield return Case(uc, "SCE2_PT_GCPTotalActivePower_001", "GCP", "The grid connection point sends its momentary power in both directions",
                              "GCP", [ "002", "010" ],
                              SpecificTestCases:     4,
                              Variation:             "Monitoring value states (all): MonitoringState_01; Energy directions (all): consume, produce",
                              NotApplicableBecause:  Publishes(uc, "TotalActivePower"));

            yield return Case(uc, "SCE3_PT_GCPTotalFeedInEnergy_001", "GCP", "The total feed-in energy does not move while the grid connection point consumes",
                              "GCP", [ "003", "010", "012" ],
                              Variation:             "Energy directions (all): consume",
                              NotApplicableBecause:  CanGo(uc, "TotalFeedInEnergy", Producing: false));

            yield return Case(uc, "SCE3_PT_GCPTotalFeedInEnergy_002", "GCP", "The total feed-in energy rises while the grid connection point produces",
                              "GCP", [ "003", "010" ],
                              SpecificTestCases:     2,
                              Variation:             "Energy directions (all): produce",
                              NotApplicableBecause:  CanGo(uc, "TotalFeedInEnergy", Producing: true));

            yield return Case(uc, "SCE4_PT_GCPTotalConsumedEnergy_001", "GCP", "The total consumed energy rises while the grid connection point consumes",
                              "GCP", [ "004", "010" ],
                              SpecificTestCases:     2,
                              Variation:             "Energy directions (all): consume",
                              NotApplicableBecause:  CanGo(uc, "TotalConsumedEnergy", Producing: false));

            yield return Case(uc, "SCE4_PT_GCPTotalConsumedEnergy_002", "GCP", "The total consumed energy does not move while the grid connection point produces",
                              "GCP", [ "004", "010", "012" ],
                              Variation:             "Energy directions (all): produce",
                              NotApplicableBecause:  CanGo(uc, "TotalConsumedEnergy", Producing: true));

            for (var index = 0; index < phases.Length; index++)
                yield return Case(uc, $"SCE5_PT_GCPActiveACCurrent_{index + 1:D3}", "GCP",
                                  $"The grid connection point sends the active AC current on {phases[index].Name}",
                                  "GCP", [ "005", $"005/{index + 1}", "005/4", "010" ],
                                  SpecificTestCases:     4,
                                  Variation:             "Energy directions (all): consume, produce",
                                  NotApplicableBecause:  Publishes(uc, "ActiveACCurrent", phases[index].Key));

            for (var index = 0; index < voltagePairs.Length; index++)
                yield return Case(uc, $"SCE6_PT_GCPACVoltage_{index + 1:D3}", "GCP",
                                  $"The grid connection point sends the AC voltage between {voltagePairs[index].Name}",
                                  "GCP", [ "006", $"006/{index + 1}", "006/7", "011" ],
                                  SpecificTestCases:     2,
                                  NotApplicableBecause:  Publishes(uc, "ACVoltage", voltagePairs[index].Key));

            yield return Case(uc, "SCE7_PT_GCPFrequency_001", "GCP", "The grid connection point sends the frequency",
                              "GCP", [ "007" ],
                              SpecificTestCases:     2,
                              NotApplicableBecause:  Publishes(uc, "Frequency"));

            #endregion

            #region Chapter 8 - the monitoring appliance as the device under test

            yield return Case(uc, "COM_PT_MAPolling_001",       "MA",  "The monitoring appliance polls at the interval it declared",
                              "MA", [ "012" ], NotApplicableBecause: Uses(uc, Polling: true));

            yield return Case(uc, "COM_PT_MANotification_001",  "MA",  "The monitoring appliance receives changed data within 120 seconds",
                              "MA", [ "013" ], NotApplicableBecause: Uses(uc, Polling: false));

            yield return Case(uc, "SCE1_PT_MAPowerLimitFactor_001", "MA", "The monitoring appliance receives the PV feed-in power limitation factor",
                              "MA", [ "001" ], NotApplicableBecause: Publishes(uc, "PowerLimitFactor"));

            yield return Case(uc, "SCE2_PT_MATotalActivePower_001", "MA", "The monitoring appliance receives the momentary power with state \"normal\"",
                              "MA", [ "002", "010" ],
                              SpecificTestCases:     4,
                              Variation:             "Monitoring value states (all): MonitoringState_01; Energy directions (all): consume, produce",
                              NotApplicableBecause:  Publishes(uc, "TotalActivePower"));

            yield return Case(uc, "SCE2_NT_MATotalActivePower_002", "MA", "The monitoring appliance discards a momentary power which is out of range or erroneous",
                              "MA", [ "008", "008/1", "008/2" ],
                              SpecificTestCases:     4,
                              Variation:             "Monitoring value states (all): MonitoringState_02, MonitoringState_03; Energy directions (all): consume, produce",
                              NotApplicableBecause:  Publishes(uc, "TotalActivePower"));

            yield return Case(uc, "SCE3_PT_MATotalFeedInEnergy_001", "MA", "The monitoring appliance receives the total feed-in energy with state \"normal\"",
                              "MA", [ "003", "010" ],
                              SpecificTestCases:     2,
                              NotApplicableBecause:  Publishes(uc, "TotalFeedInEnergy"));

            yield return Case(uc, "SCE3_NT_MATotalFeedInEnergy_002", "MA", "The monitoring appliance discards a total feed-in energy which is out of range or erroneous",
                              "MA", [ "008", "008/1", "008/2" ],
                              SpecificTestCases:     2,
                              NotApplicableBecause:  Publishes(uc, "TotalFeedInEnergy"));

            yield return Case(uc, "SCE4_PT_MATotalConsumedEnergy_001", "MA", "The monitoring appliance receives the total consumed energy with state \"normal\"",
                              "MA", [ "004", "010" ],
                              SpecificTestCases:     2,
                              NotApplicableBecause:  Publishes(uc, "TotalConsumedEnergy"));

            yield return Case(uc, "SCE4_NT_MATotalConsumedEnergy_002", "MA", "The monitoring appliance discards a total consumed energy which is out of range or erroneous",
                              "MA", [ "008", "008/1", "008/2" ],
                              SpecificTestCases:     2,
                              NotApplicableBecause:  Publishes(uc, "TotalConsumedEnergy"));

            for (var index = 0; index < phases.Length; index++)
            {

                yield return Case(uc, $"SCE5_PT_MAActiveACCurrent_{2 * index + 1:D3}", "MA",
                                  $"The monitoring appliance receives the AC current on {phases[index].Name} with state \"normal\"",
                                  "MA", [ "005", $"005/{index + 1}", "005/4", "010" ],
                                  SpecificTestCases:     4,
                                  NotApplicableBecause:  Publishes(uc, "ActiveACCurrent", phases[index].Key));

                yield return Case(uc, $"SCE5_NT_MAActiveACCurrent_{2 * index + 2:D3}", "MA",
                                  $"The monitoring appliance discards an AC current on {phases[index].Name} which is out of range or erroneous",
                                  "MA", [ "008", "008/1", "008/2" ],
                                  SpecificTestCases:     4,
                                  NotApplicableBecause:  Publishes(uc, "ActiveACCurrent", phases[index].Key));

            }

            for (var index = 0; index < voltagePairs.Length; index++)
            {

                yield return Case(uc, $"SCE6_PT_MAACVoltage_{2 * index + 1:D3}", "MA",
                                  $"The monitoring appliance receives the AC voltage between {voltagePairs[index].Name} with state \"normal\"",
                                  "MA", [ "006", $"006/{index + 1}", "006/7", "011" ],
                                  SpecificTestCases:     2,
                                  NotApplicableBecause:  Publishes(uc, "ACVoltage", voltagePairs[index].Key));

                yield return Case(uc, $"SCE6_NT_MAACVoltage_{2 * index + 2:D3}", "MA",
                                  $"The monitoring appliance discards an AC voltage between {voltagePairs[index].Name} which is out of range or erroneous",
                                  "MA", [ "008", "008/1", "008/2" ],
                                  SpecificTestCases:     2,
                                  NotApplicableBecause:  Publishes(uc, "ACVoltage", voltagePairs[index].Key));

            }

            yield return Case(uc, "SCE7_PT_MAFrequency_001", "MA", "The monitoring appliance receives the frequency with state \"normal\"",
                              "MA", [ "007" ],
                              SpecificTestCases:     2,
                              NotApplicableBecause:  Publishes(uc, "Frequency"));

            yield return Case(uc, "SCE7_NT_MAFrequency_002", "MA", "The monitoring appliance discards a frequency which is out of range or erroneous",
                              "MA", [ "008", "008/1", "008/2" ],
                              SpecificTestCases:     2,
                              NotApplicableBecause:  Publishes(uc, "Frequency"));

            #endregion

        }

        #endregion

        #region (private) PowerCases()

        /// <summary>
        /// The 54 abstract test cases of the MPC test specification: 20 with the
        /// monitored unit as the device under test, 34 with the monitoring
        /// appliance.
        /// </summary>
        private static IEnumerable<ConformanceTestCase> PowerCases()
        {

            var uc = power;

            #region Chapter 7 - the monitored unit as the device under test

            yield return Case(uc, "COM_PT_MUPolling_001",       "MU",  "The monitored unit answers a poll within 120 seconds",
                              "MU", [ "013" ], NotApplicableBecause: Uses(uc, Polling: true));

            yield return Case(uc, "COM_PT_MUNotification_001",  "MU",  "The monitored unit sends changed data within 120 seconds",
                              "MU", [ "014" ], NotApplicableBecause: Uses(uc, Polling: false));

            yield return Case(uc, "SCE1_PT_MUTotalActivePower_001", "MU", "The monitored unit sends its momentary power in both directions",
                              "MU", [ "001", "009" ],
                              SpecificTestCases:     4,
                              Variation:             "Monitoring value states (all): MonitoringState_01; Energy directions (all): consume, produce",
                              NotApplicableBecause:  Publishes(uc, "TotalActivePower"));

            for (var index = 0; index < phases.Length; index++)
                yield return Case(uc, $"SCE1_PT_MUPhaseActivePower_{index + 1:D3}", "MU",
                                  $"The monitored unit sends the active power on {phases[index].Name}",
                                  "MU", [ "002", $"002/{index + 1}", "002/4", "009" ],
                                  SpecificTestCases:     4,
                                  Variation:             "Energy directions (all): consume, produce",
                                  NotApplicableBecause:  Publishes(uc, "PhaseActivePower", phases[index].Key));

            yield return Case(uc, "SCE2_PT_MUTotalConsumedEnergy_001", "MU", "The total consumed energy rises while the monitored unit consumes",
                              "MU", [ "003", "003/1", "009" ],
                              SpecificTestCases:     2,
                              NotApplicableBecause:  CanGo(uc, "TotalConsumedEnergy", Producing: false));

            yield return Case(uc, "SCE2_PT_MUTotalConsumedEnergy_002", "MU", "The total consumed energy does not move while the monitored unit produces",
                              "MU", [ "003", "003/1", "009", "013" ],
                              NotApplicableBecause:  CanGo(uc, "TotalConsumedEnergy", Producing: true));

            yield return Case(uc, "SCE2_PT_MUTotalProducedEnergy_001", "MU", "The total produced energy does not move while the monitored unit consumes",
                              "MU", [ "004", "004/1", "009", "013" ],
                              NotApplicableBecause:  CanGo(uc, "TotalProducedEnergy", Producing: false));

            yield return Case(uc, "SCE2_PT_MUTotalProducedEnergy_002", "MU", "The total produced energy rises while the monitored unit produces",
                              "MU", [ "004", "004/1", "009" ],
                              SpecificTestCases:     2,
                              NotApplicableBecause:  CanGo(uc, "TotalProducedEnergy", Producing: true));

            for (var index = 0; index < phases.Length; index++)
                yield return Case(uc, $"SCE3_PT_MUActiveACCurrent_{index + 1:D3}", "MU",
                                  $"The monitored unit sends the active AC current on {phases[index].Name}",
                                  "MU", [ "005", $"005/{index + 1}", "005/4", "009" ],
                                  SpecificTestCases:     4,
                                  Variation:             "Energy directions (all): consume, produce",
                                  NotApplicableBecause:  Publishes(uc, "ActiveACCurrent", phases[index].Key));

            for (var index = 0; index < voltagePairs.Length; index++)
                yield return Case(uc, $"SCE4_PT_MUACVoltage_{index + 1:D3}", "MU",
                                  $"The monitored unit sends the AC voltage between {voltagePairs[index].Name}",
                                  "MU", [ "006", $"006/{index + 1}", "006/7", "010" ],
                                  SpecificTestCases:     2,
                                  NotApplicableBecause:  Publishes(uc, "ACVoltage", voltagePairs[index].Key));

            yield return Case(uc, "SCE5_PT_MUFrequency_001", "MU", "The monitored unit sends the frequency",
                              "MU", [ "007" ],
                              SpecificTestCases:     2,
                              NotApplicableBecause:  Publishes(uc, "Frequency"));

            #endregion

            #region Chapter 8 - the monitoring appliance as the device under test

            yield return Case(uc, "COM_PT_MAPolling_001",       "MA",  "The monitoring appliance polls at the interval it declared",
                              "MA", [ "013" ], NotApplicableBecause: Uses(uc, Polling: true));

            yield return Case(uc, "COM_PT_MANotification_001",  "MA",  "The monitoring appliance receives changed data within 120 seconds",
                              "MA", [ "014" ], NotApplicableBecause: Uses(uc, Polling: false));

            yield return Case(uc, "SCE1_PT_MATotalActivePower_001", "MA", "The monitoring appliance receives the momentary power with state \"normal\"",
                              "MA", [ "001", "009" ],
                              SpecificTestCases:     4,
                              Variation:             "Monitoring value states (all): MonitoringState_01; Energy directions (all): consume, produce",
                              NotApplicableBecause:  Publishes(uc, "TotalActivePower"));

            yield return Case(uc, "SCE1_NT_MATotalActivePower_002", "MA", "The monitoring appliance discards a momentary power which is out of range or erroneous",
                              "MA", [ "008", "008/1", "008/2" ],
                              SpecificTestCases:     4,
                              Variation:             "Monitoring value states (all): MonitoringState_02, MonitoringState_03; Energy directions (all): consume, produce",
                              NotApplicableBecause:  Publishes(uc, "TotalActivePower"));

            for (var index = 0; index < phases.Length; index++)
            {

                yield return Case(uc, $"SCE1_PT_MAPhaseActivePower_{2 * index + 1:D3}", "MA",
                                  $"The monitoring appliance receives the active power on {phases[index].Name} with state \"normal\"",
                                  "MA", [ "002", $"002/{index + 1}", "002/4", "009" ],
                                  SpecificTestCases:     4,
                                  NotApplicableBecause:  Publishes(uc, "PhaseActivePower", phases[index].Key));

                yield return Case(uc, $"SCE1_NT_MAPhaseActivePower_{2 * index + 2:D3}", "MA",
                                  $"The monitoring appliance discards an active power on {phases[index].Name} which is out of range or erroneous",
                                  "MA", [ "008", "008/1", "008/2" ],
                                  SpecificTestCases:     4,
                                  NotApplicableBecause:  Publishes(uc, "PhaseActivePower", phases[index].Key));

            }

            yield return Case(uc, "SCE2_PT_MATotalConsumedEnergy_001", "MA", "The monitoring appliance receives the total consumed energy with state \"normal\"",
                              "MA", [ "003", "003/1", "009" ],
                              SpecificTestCases:     2,
                              NotApplicableBecause:  Publishes(uc, "TotalConsumedEnergy"));

            yield return Case(uc, "SCE2_NT_MATotalConsumedEnergy_002", "MA", "The monitoring appliance discards a total consumed energy which is out of range or erroneous",
                              "MA", [ "008", "008/1", "008/2" ],
                              SpecificTestCases:     2,
                              NotApplicableBecause:  Publishes(uc, "TotalConsumedEnergy"));

            yield return Case(uc, "SCE2_PT_MATotalProducedEnergy_001", "MA", "The monitoring appliance receives the total produced energy with state \"normal\"",
                              "MA", [ "004", "004/1", "009" ],
                              SpecificTestCases:     2,
                              NotApplicableBecause:  Publishes(uc, "TotalProducedEnergy"));

            yield return Case(uc, "SCE2_NT_MATotalProducedEnergy_002", "MA", "The monitoring appliance discards a total produced energy which is out of range or erroneous",
                              "MA", [ "008", "008/1", "008/2" ],
                              SpecificTestCases:     2,
                              NotApplicableBecause:  Publishes(uc, "TotalProducedEnergy"));

            for (var index = 0; index < phases.Length; index++)
            {

                yield return Case(uc, $"SCE3_PT_MAActiveACCurrent_{2 * index + 1:D3}", "MA",
                                  $"The monitoring appliance receives the AC current on {phases[index].Name} with state \"normal\"",
                                  "MA", [ "005", $"005/{index + 1}", "005/4", "009" ],
                                  SpecificTestCases:     4,
                                  NotApplicableBecause:  Publishes(uc, "ActiveACCurrent", phases[index].Key));

                yield return Case(uc, $"SCE3_NT_MAActiveACCurrent_{2 * index + 2:D3}", "MA",
                                  $"The monitoring appliance discards an AC current on {phases[index].Name} which is out of range or erroneous",
                                  "MA", [ "008", "008/1", "008/2" ],
                                  SpecificTestCases:     4,
                                  NotApplicableBecause:  Publishes(uc, "ActiveACCurrent", phases[index].Key));

            }

            for (var index = 0; index < voltagePairs.Length; index++)
            {

                yield return Case(uc, $"SCE4_PT_MAACVoltage_{2 * index + 1:D3}", "MA",
                                  $"The monitoring appliance receives the AC voltage between {voltagePairs[index].Name} with state \"normal\"",
                                  "MA", [ "006", $"006/{index + 1}", "006/7", "010" ],
                                  SpecificTestCases:     2,
                                  NotApplicableBecause:  Publishes(uc, "ACVoltage", voltagePairs[index].Key));

                yield return Case(uc, $"SCE4_NT_MAACVoltage_{2 * index + 2:D3}", "MA",
                                  $"The monitoring appliance discards an AC voltage between {voltagePairs[index].Name} which is out of range or erroneous",
                                  "MA", [ "008", "008/1", "008/2" ],
                                  SpecificTestCases:     2,
                                  NotApplicableBecause:  Publishes(uc, "ACVoltage", voltagePairs[index].Key));

            }

            yield return Case(uc, "SCE5_PT_MAFrequency_001", "MA", "The monitoring appliance receives the frequency with state \"normal\"",
                              "MA", [ "007" ],
                              SpecificTestCases:     2,
                              NotApplicableBecause:  Publishes(uc, "Frequency"));

            yield return Case(uc, "SCE5_NT_MAFrequency_002", "MA", "The monitoring appliance discards a frequency which is out of range or erroneous",
                              "MA", [ "008", "008/1", "008/2" ],
                              SpecificTestCases:     2,
                              NotApplicableBecause:  Publishes(uc, "Frequency"));

            #endregion

        }

        #endregion

    }

}
