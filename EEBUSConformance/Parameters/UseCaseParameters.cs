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

#endregion

namespace cloud.charging.open.protocols.EEBUS.Conformance
{

    #region (class) PowerLimitationParameters

    /// <summary>
    /// The parameter sheet of one of the two power limitation use cases -
    /// EEBus_UC_ParameterSheet_LPC_V1.0.2.xlsx and its production twin.
    ///
    /// Two worksheets of it matter here. "Specific Test Cases" holds the ranges
    /// the manufacturer declares, out of which the test bench derives the data
    /// sets APCL_01..06, FCAPL_01..06 and FCAPL_DUR_01..03 exactly as section
    /// 6.11 prescribes - which is why those are computed here rather than
    /// declared. "Optional Support" holds the yes/no answers which turn a
    /// recommended abstract test case into a mandatory one, and every field
    /// below carrying an official row number (A1, D2, E2, ...) is one of them.
    ///
    /// The production sheet reads the same with the words swapped, and one
    /// difference which looks larger than it is: its APPL values are negative,
    /// because a physicist counts production as negative power. On the wire
    /// nothing is negative - the limit is a
    /// <c>signDependentAbsValueLimit</c> whose <c>limitDirection</c> already says
    /// "produce" - so both use cases reduce to the same rule, "a magnitude below
    /// zero is refused", and this class holds magnitudes for both.
    /// </summary>
    public class PowerLimitationParameters
    {

        #region Properties (the ranges)

        /// <summary>
        /// Whether the controllable system runs on a customer energy manager -
        /// the sheet's "CS type CEM?", which decides between LPC instance 1 and
        /// instance 2 and therefore which nominal maximum has to exist.
        /// </summary>
        public Boolean   SystemIsEnergyManager   { get; init; }

        /// <summary>StartUpDur_EG: how long the energy guard needs to boot.</summary>
        public TimeSpan  StartUpDurationGuard    { get; init; } = TimeSpan.FromSeconds(30);

        /// <summary>StartUpDur_CS: how long the controllable system needs to boot.</summary>
        public TimeSpan  StartUpDurationSystem   { get; init; } = TimeSpan.FromSeconds(30);

        /// <summary>APCLmin / APPLmin: the lower end of the limit range, as a magnitude in watts.</summary>
        public Decimal   LimitMin                { get; init; }

        /// <summary>APCLmax / APPLmax: the upper end of it.</summary>
        public Decimal   LimitMax                { get; init; } = 11000;

        /// <summary>FCAPLmin / FPAPLmin: the lower end of the failsafe limit range.</summary>
        public Decimal   FailsafeMin             { get; init; }

        /// <summary>FCAPLmax / FPAPLmax: the upper end of it.</summary>
        public Decimal   FailsafeMax             { get; init; } = 11000;

        /// <summary>PFCAPL / PFPAPL: the failsafe limit the device is pre-configured with.</summary>
        public Decimal   PreConfiguredFailsafe   { get; init; } = 4200;

        /// <summary>PFSDM: the failsafe duration minimum the device is pre-configured with.</summary>
        public TimeSpan  PreConfiguredFailsafeDuration  { get; init; } = TimeSpan.FromHours(2);

        /// <summary>MFSDM: the largest failsafe duration minimum the device accepts, two to 24 hours.</summary>
        public TimeSpan  MaximumFailsafeDuration        { get; init; } = TimeSpan.FromHours(24);

        #endregion

        #region Properties (the "Optional Support" worksheet)

        /// <summary>A1: the energy guard reads and writes the limit (scenario 1).</summary>
        public Boolean  GuardWritesLimit           { get; init; } = true;

        /// <summary>A2: the energy guard reads and writes the failsafe values (scenario 2).</summary>
        public Boolean  GuardWritesFailsafe        { get; init; } = true;

        /// <summary>A5: the energy guard resends a limit quickly after a rejection.</summary>
        public Boolean  GuardResendsAfterReject    { get; init; } = true;

        /// <summary>D1: the controllable system alters a limit which is too large to store, instead of refusing it.</summary>
        public Boolean  SystemAltersTooLargeLimit  { get; init; } = true;

        /// <summary>
        /// D2: the state "unlimited/autonomous" is implemented at all.
        ///
        /// The use case allows a controllable system to delay entering it, and
        /// the delay may in theory be infinite - so a device may never enter it,
        /// and eight abstract test cases hang on this one answer (LPC test
        /// specification, Table 13 footnote 1).
        /// </summary>
        public Boolean  SystemHasAutonomousState   { get; init; } = true;

        /// <summary>D3: the controllable system stores the failsafe values persistently.</summary>
        public Boolean  SystemStoresPersistently   { get; init; } = true;

        /// <summary>B3 / C3: the controllable system can be made to reject a limit for a permitted reason.</summary>
        public Boolean  SystemCanRejectPermitted   { get; init; } = true;

        /// <summary>E1: the energy guard is black start capable.</summary>
        public Boolean  GuardBlackStart            { get; init; } = true;

        /// <summary>E2: the controllable system is black start capable.</summary>
        public Boolean  SystemBlackStart           { get; init; } = true;

        /// <summary>
        /// M1 / N1: whether the tester can measure what the device actually
        /// consumes or produces, and so verify that it keeps below its nominal
        /// maximum.
        ///
        /// The default is no, and that is not modesty: this is the one thing in
        /// the whole use case catalog which is not on the wire at all. A software
        /// test bench talking to a software stack has no wattmeter, and pretending
        /// otherwise would turn a question nobody asked into a case which passed.
        /// </summary>
        public Boolean  PhysicalMeasurement        { get; init; }

        #endregion

        #region The data sets of section 6.11

        /// <summary>
        /// delta = 0.05 * (max - min), the auxiliary value out of which the
        /// limit data sets are built.
        /// </summary>
        public Decimal LimitDelta
            => 0.05m * (LimitMax - LimitMin);

        /// <summary>The same for the failsafe limit range.</summary>
        public Decimal FailsafeDelta
            => 0.05m * (FailsafeMax - FailsafeMin);


        /// <summary>
        /// APCL_01..APCL_06 respectively APPL_01..APPL_06, as magnitudes.
        /// </summary>
        /// <param name="Number">Which of the six, from 1 to 6.</param>
        public Decimal Limit(Int32 Number)

            => Number switch {
                   1 => Math.Max(0, LimitMin - LimitDelta),
                   2 => LimitMin + LimitDelta,
                   3 => (LimitMin + LimitMax) / 2,
                   4 => LimitMax - LimitDelta,
                   5 => LimitMax + LimitDelta,
                   6 => -1000,
                   _ => throw new ArgumentOutOfRangeException(nameof(Number), "There are six limit values, APCL_01 to APCL_06.")
               };


        /// <summary>
        /// FCAPL_01..FCAPL_06 respectively FPAPL_01..FPAPL_06.
        /// </summary>
        /// <param name="Number">Which of the six, from 1 to 6.</param>
        public Decimal Failsafe(Int32 Number)

            => Number switch {
                   1 => Math.Max(0, FailsafeMin - FailsafeDelta),
                   2 => FailsafeMin + FailsafeDelta,
                   3 => (FailsafeMin + FailsafeMax) / 2,
                   4 => FailsafeMax - FailsafeDelta,
                   5 => FailsafeMax + FailsafeDelta,
                   6 => -1000,
                   _ => throw new ArgumentOutOfRangeException(nameof(Number), "There are six failsafe values, FCAPL_01 to FCAPL_06.")
               };


        /// <summary>
        /// FCAPL_DUR_01..03: one hour 54 minutes (below the two hour floor, and
        /// therefore refusable), one the device should accept, and 1.05 times its
        /// own maximum (which it may accept or refuse - both are conformant, and
        /// ATC_*_CSConnection_005 exists to watch which).
        /// </summary>
        /// <param name="Number">Which of the three, from 1 to 3.</param>
        public TimeSpan FailsafeDuration(Int32 Number)

            => Number switch {
                   1 => TimeSpan.FromMinutes(114),
                   2 => PreConfiguredFailsafeDuration < MaximumFailsafeDuration
                            ? PreConfiguredFailsafeDuration + (MaximumFailsafeDuration - PreConfiguredFailsafeDuration) / 2
                            : MaximumFailsafeDuration,
                   3 => MaximumFailsafeDuration * 1.05,
                   _ => throw new ArgumentOutOfRangeException(nameof(Number), "There are three duration values, FCAPL_DUR_01 to FCAPL_DUR_03.")
               };


        /// <summary>
        /// APCL_DUR_01 and APCL_DUR_02: sixty seconds, and no duration at all.
        /// </summary>
        /// <param name="Number">Which of the two, 1 or 2.</param>
        public TimeSpan? LimitDuration(Int32 Number)

            => Number switch {
                   1 => TimeSpan.FromSeconds(60),
                   2 => null,
                   _ => throw new ArgumentOutOfRangeException(nameof(Number), "There are two duration values, APCL_DUR_01 and APCL_DUR_02.")
               };

        #endregion


        #region (static) Parse(JSON) / ToJSON()

        /// <summary>
        /// Read a power limitation parameter block from its JSON representation.
        /// </summary>
        /// <param name="JSON">The JSON representation, or null for the defaults.</param>
        public static PowerLimitationParameters Parse(JObject? JSON)
        {

            if (JSON is null)
                return new PowerLimitationParameters();

            var fallback = new PowerLimitationParameters();

            return new PowerLimitationParameters {

                SystemIsEnergyManager          = Yes(JSON, "CS type CEM?",                    fallback.SystemIsEnergyManager),
                StartUpDurationGuard           = Seconds(JSON, "StartUpDur_EG",               fallback.StartUpDurationGuard),
                StartUpDurationSystem          = Seconds(JSON, "StartUpDur_CS",               fallback.StartUpDurationSystem),

                LimitMin                       = (Decimal?) JSON["APCLmin"]  ?? (Decimal?) JSON["APPLmin"]  ?? fallback.LimitMin,
                LimitMax                       = (Decimal?) JSON["APCLmax"]  ?? (Decimal?) JSON["APPLmax"]  ?? fallback.LimitMax,
                FailsafeMin                    = (Decimal?) JSON["FCAPLmin"] ?? (Decimal?) JSON["FPAPLmin"] ?? fallback.FailsafeMin,
                FailsafeMax                    = (Decimal?) JSON["FCAPLmax"] ?? (Decimal?) JSON["FPAPLmax"] ?? fallback.FailsafeMax,
                PreConfiguredFailsafe          = (Decimal?) JSON["PFCAPL"]   ?? (Decimal?) JSON["PFPAPL"]   ?? fallback.PreConfiguredFailsafe,

                PreConfiguredFailsafeDuration  = Seconds(JSON, "PFSDM", fallback.PreConfiguredFailsafeDuration),
                MaximumFailsafeDuration        = Seconds(JSON, "MFSDM", fallback.MaximumFailsafeDuration),

                GuardWritesLimit               = Yes(JSON, "A1", fallback.GuardWritesLimit),
                GuardWritesFailsafe            = Yes(JSON, "A2", fallback.GuardWritesFailsafe),
                GuardResendsAfterReject        = Yes(JSON, "A5", fallback.GuardResendsAfterReject),
                SystemCanRejectPermitted       = Yes(JSON, "B3", Yes(JSON, "C3", fallback.SystemCanRejectPermitted)),
                SystemAltersTooLargeLimit      = Yes(JSON, "D1", fallback.SystemAltersTooLargeLimit),
                SystemHasAutonomousState       = Yes(JSON, "D2", fallback.SystemHasAutonomousState),
                SystemStoresPersistently       = Yes(JSON, "D3", fallback.SystemStoresPersistently),
                GuardBlackStart                = Yes(JSON, "E1", fallback.GuardBlackStart),
                SystemBlackStart               = Yes(JSON, "E2", fallback.SystemBlackStart),
                PhysicalMeasurement            = Yes(JSON, "M1", Yes(JSON, "N1", fallback.PhysicalMeasurement))

            };

        }


        /// <summary>
        /// The JSON representation of this parameter block, using the names of
        /// the official worksheets.
        /// </summary>
        /// <param name="Production">Whether to write the production wording.</param>
        public JObject ToJSON(Boolean Production = false)
        {

            var limit     = Production ? "APPL"  : "APCL";
            var failsafe  = Production ? "FPAPL" : "FCAPL";

            return new JObject(

                       new JProperty("CS type CEM?",     SystemIsEnergyManager ? "yes" : "no"),
                       new JProperty("StartUpDur_EG",    StartUpDurationGuard. TotalSeconds),
                       new JProperty("StartUpDur_CS",    StartUpDurationSystem.TotalSeconds),

                       new JProperty($"{limit}min",      LimitMin),
                       new JProperty($"{limit}max",      LimitMax),
                       new JProperty($"{failsafe}min",   FailsafeMin),
                       new JProperty($"{failsafe}max",   FailsafeMax),
                       new JProperty($"P{failsafe}",     PreConfiguredFailsafe),
                       new JProperty("PFSDM",            PreConfiguredFailsafeDuration.TotalSeconds),
                       new JProperty("MFSDM",            MaximumFailsafeDuration.      TotalSeconds),

                       new JProperty("A1",               GuardWritesLimit          ? "yes" : "no"),
                       new JProperty("A2",               GuardWritesFailsafe       ? "yes" : "no"),
                       new JProperty("A5",               GuardResendsAfterReject   ? "yes" : "no"),
                       new JProperty("B3",               SystemCanRejectPermitted  ? "yes" : "no"),
                       new JProperty("C3",               SystemCanRejectPermitted  ? "yes" : "no"),
                       new JProperty("D1",               SystemAltersTooLargeLimit ? "yes" : "no"),
                       new JProperty("D2",               SystemHasAutonomousState  ? "yes" : "no"),
                       new JProperty("D3",               SystemStoresPersistently  ? "yes" : "no"),
                       new JProperty("E1",               GuardBlackStart           ? "yes" : "no"),
                       new JProperty("E2",               SystemBlackStart          ? "yes" : "no"),
                       new JProperty("M1",               PhysicalMeasurement       ? "yes" : "no"),
                       new JProperty("N1",               PhysicalMeasurement       ? "yes" : "no")

                   );

        }

        #endregion

        #region Validate()

        /// <summary>
        /// What a sheet says which cannot be true.
        /// </summary>
        public IEnumerable<String> Validate()
        {

            if (LimitMin >= LimitMax)
                yield return "The limit range is empty: min has to be smaller than max.";

            if (FailsafeMin >= FailsafeMax)
                yield return "The failsafe limit range is empty: min has to be smaller than max.";

            if (PreConfiguredFailsafe < 0)
                yield return "The pre-configured failsafe limit is never below zero (section 2.8.1).";

            if (MaximumFailsafeDuration < TimeSpan.FromHours(2) ||
                MaximumFailsafeDuration > TimeSpan.FromHours(24))
                yield return "MFSDM has to be between two and 24 hours (rule 022/1).";

            if (PreConfiguredFailsafeDuration < TimeSpan.FromHours(2) ||
                PreConfiguredFailsafeDuration > MaximumFailsafeDuration)
                yield return "PFSDM has to be between two hours and MFSDM (rule 022/1).";

        }

        #endregion

        #region (private) Yes(JSON, Name, Default) / Seconds(JSON, Name, Default)

        private static Boolean Yes(JObject JSON, String Name, Boolean Default)
        {

            var value = JSON[Name];

            if (value is null)
                return Default;

            if (value.Type == JTokenType.Boolean)
                return value.Value<Boolean>();

            return String.Equals(value.Value<String>(), "yes", StringComparison.OrdinalIgnoreCase);

        }

        private static TimeSpan Seconds(JObject JSON, String Name, TimeSpan Default)

            => (Double?) JSON[Name] is Double seconds
                   ? TimeSpan.FromSeconds(seconds)
                   : Default;

        #endregion

    }

    #endregion

    #region (class) MonitoringParameters

    /// <summary>
    /// The parameter sheet of one of the two monitoring use cases -
    /// EEBus_UC_ParameterSheet_MPC_V1.0.2.xlsx and the grid connection point
    /// twin.
    ///
    /// Almost all of it is "which of these data points do you have": a meter
    /// which knows nothing but its total active power implements the use case
    /// completely, and every abstract test case about a data point it does not
    /// publish is not applicable rather than failed.
    /// </summary>
    public class MonitoringParameters
    {

        #region Properties

        /// <summary>Whether the device is asked for its data at an interval.</summary>
        public Boolean   Polling         { get; init; }

        /// <summary>How long that interval is at most.</summary>
        public TimeSpan  PollingInterval { get; init; } = TimeSpan.FromSeconds(120);

        /// <summary>Whether the device sends its data when a value changed.</summary>
        public Boolean   Notification    { get; init; } = true;

        /// <summary>Which phases the device is connected to.</summary>
        public IReadOnlySet<String>  Phases  { get; init; } = new HashSet<String> { "a", "b", "c" };

        /// <summary>Whether it publishes the total active power (MPC scenario 1, MGCP scenario 2).</summary>
        public Boolean  TotalActivePower       { get; init; } = true;

        /// <summary>Whether it publishes the active power of each phase (MPC scenario 1 only).</summary>
        public Boolean  PhaseActivePower       { get; init; } = true;

        /// <summary>Whether it publishes the energy it consumed.</summary>
        public Boolean  TotalConsumedEnergy    { get; init; } = true;

        /// <summary>Whether it publishes the energy it produced, respectively fed in.</summary>
        public Boolean  TotalProducedEnergy    { get; init; } = true;

        /// <summary>Whether it publishes the current of each phase.</summary>
        public Boolean  ActiveACCurrent        { get; init; } = true;

        /// <summary>Whether it publishes the voltage of each phase pair.</summary>
        public Boolean  ACVoltage              { get; init; } = true;

        /// <summary>
        /// Whether it publishes the voltage *between* two phases as well as
        /// between a phase and neutral.
        ///
        /// Six voltages exist and a device is required to publish none of them
        /// in particular: "if this Scenario is supported, at least one of the
        /// values stated above SHALL be supported" (MGCP-TS-006/8). A meter which
        /// measures three phase-to-neutral voltages implements scenario 6
        /// completely, so the three phase-to-phase cases are not applicable to it
        /// rather than failed.
        /// </summary>
        public Boolean  PhaseToPhaseVoltage    { get; init; } = true;

        /// <summary>Whether it publishes the grid frequency.</summary>
        public Boolean  Frequency              { get; init; } = true;

        /// <summary>Whether it publishes the PV feed-in power limitation factor (MGCP scenario 1).</summary>
        public Boolean  PowerLimitFactor       { get; init; } = true;

        /// <summary>Whether the device can be made to consume energy, for the direction dependent cases.</summary>
        public Boolean  CanConsume             { get; init; } = true;

        /// <summary>Whether it can be made to produce energy.</summary>
        public Boolean  CanProduce             { get; init; } = true;

        #endregion


        #region Supports(DataPoint) / SupportsPhase(Phase)

        /// <summary>
        /// Whether the device publishes the given data point.
        /// </summary>
        /// <param name="DataPoint">One of the names used by the abstract test cases.</param>
        public Boolean Supports(String DataPoint)

            => DataPoint switch {
                   "TotalActivePower"     => TotalActivePower,
                   "PhaseActivePower"     => PhaseActivePower,
                   "TotalConsumedEnergy"  => TotalConsumedEnergy,
                   "TotalProducedEnergy"  => TotalProducedEnergy,
                   "TotalFeedInEnergy"    => TotalProducedEnergy,
                   "ActiveACCurrent"      => ActiveACCurrent,
                   "ACVoltage"            => ACVoltage,
                   "Frequency"            => Frequency,
                   "PowerLimitFactor"     => PowerLimitFactor,
                   _                      => true
               };


        /// <summary>
        /// Whether the device measures on the given phase - "a", "b" or "c" - or
        /// between the given pair: "an" for phase A against neutral, "ab" for
        /// phase A against phase B.
        ///
        /// Neutral is not a phase and is never declared: it is the reference
        /// everything else is measured against, and a device connected to phase C
        /// is by construction connected to the C-to-neutral voltage.
        /// </summary>
        /// <param name="Phase">A phase or a pair of them.</param>
        public Boolean SupportsPhase(String Phase)
        {

            var letters = Phase.ToLowerInvariant();

            if (letters.Length == 2 &&
                letters[1] != 'n'   &&
                !PhaseToPhaseVoltage)
                return false;

            return letters.All(letter => letter == 'n' || Phases.Contains(letter.ToString()));

        }

        #endregion

        #region (static) Parse(JSON) / ToJSON() / Validate()

        /// <summary>
        /// Read a monitoring parameter block from its JSON representation.
        /// </summary>
        /// <param name="JSON">The JSON representation, or null for the defaults.</param>
        public static MonitoringParameters Parse(JObject? JSON)
        {

            if (JSON is null)
                return new MonitoringParameters();

            var fallback = new MonitoringParameters();

            return new MonitoringParameters {

                Polling              = Yes(JSON, "Polling",              fallback.Polling),
                Notification         = Yes(JSON, "Notification",         fallback.Notification),

                PollingInterval      = (Double?) JSON["Interval"] is Double seconds
                                           ? TimeSpan.FromSeconds(seconds)
                                           : fallback.PollingInterval,

                Phases               = JSON["Phases"] is JArray phases
                                           ? new HashSet<String>(phases.Select(phase => (phase.Value<String>() ?? "").ToLowerInvariant()))
                                           : fallback.Phases,

                TotalActivePower     = Yes(JSON, "Total Active Power",           fallback.TotalActivePower),
                PhaseActivePower     = Yes(JSON, "Phase-Specific Active Power",  fallback.PhaseActivePower),
                TotalConsumedEnergy  = Yes(JSON, "Total Consumed Energy",        fallback.TotalConsumedEnergy),
                TotalProducedEnergy  = Yes(JSON, "Total Produced Energy",        fallback.TotalProducedEnergy),
                ActiveACCurrent      = Yes(JSON, "Phase-Specific AC Current",    fallback.ActiveACCurrent),
                ACVoltage            = Yes(JSON, "Phase-Specific AC Voltage",    fallback.ACVoltage),
                PhaseToPhaseVoltage  = Yes(JSON, "Phase-to-phase AC Voltage",    fallback.PhaseToPhaseVoltage),
                Frequency            = Yes(JSON, "AC Frequency",                 fallback.Frequency),
                PowerLimitFactor     = Yes(JSON, "PV Feed-In Power Limitation Factor", fallback.PowerLimitFactor),

                CanConsume           = Yes(JSON, "consume", fallback.CanConsume),
                CanProduce           = Yes(JSON, "produce", fallback.CanProduce)

            };

        }


        /// <summary>
        /// The JSON representation of this parameter block.
        /// </summary>
        public JObject ToJSON()

            => new (

                   new JProperty("Polling",                              Polling             ? "yes" : "no"),
                   new JProperty("Notification",                         Notification        ? "yes" : "no"),
                   new JProperty("Interval",                             PollingInterval.TotalSeconds),
                   new JProperty("Phases",                               new JArray(Phases.Order())),

                   new JProperty("Total Active Power",                   TotalActivePower    ? "yes" : "no"),
                   new JProperty("Phase-Specific Active Power",          PhaseActivePower    ? "yes" : "no"),
                   new JProperty("Total Consumed Energy",                TotalConsumedEnergy ? "yes" : "no"),
                   new JProperty("Total Produced Energy",                TotalProducedEnergy ? "yes" : "no"),
                   new JProperty("Phase-Specific AC Current",            ActiveACCurrent     ? "yes" : "no"),
                   new JProperty("Phase-Specific AC Voltage",            ACVoltage           ? "yes" : "no"),
                   new JProperty("Phase-to-phase AC Voltage",             PhaseToPhaseVoltage ? "yes" : "no"),
                   new JProperty("AC Frequency",                         Frequency           ? "yes" : "no"),
                   new JProperty("PV Feed-In Power Limitation Factor",   PowerLimitFactor    ? "yes" : "no"),

                   new JProperty("consume",                              CanConsume          ? "yes" : "no"),
                   new JProperty("produce",                              CanProduce          ? "yes" : "no")

               );


        /// <summary>
        /// What a sheet says which cannot be true.
        /// </summary>
        public IEnumerable<String> Validate()
        {

            if (!Polling && !Notification)
                yield return "A monitoring device which neither answers polls nor notifies publishes nothing at all.";

            if (!CanConsume && !CanProduce)
                yield return "A monitored unit which can neither consume nor produce has nothing to measure.";

            if (Phases.Count == 0 && (PhaseActivePower || ActiveACCurrent || ACVoltage))
                yield return "Phase-specific data points were declared, but no connected phase.";

        }

        #endregion

        #region (private) Yes(JSON, Name, Default)

        private static Boolean Yes(JObject JSON, String Name, Boolean Default)
        {

            var value = JSON[Name];

            if (value is null)
                return Default;

            if (value.Type == JTokenType.Boolean)
                return value.Value<Boolean>();

            return String.Equals(value.Value<String>(), "yes", StringComparison.OrdinalIgnoreCase);

        }

        #endregion

    }

    #endregion

    #region (class) UseCaseParameters

    /// <summary>
    /// The four use case parameter sheets together, as one device declares them.
    ///
    /// The official sheets are four separate workbooks, because a certification
    /// run is per use case. A device is one device though, so here they are four
    /// blocks of one sheet, under the abbreviations the specifications use.
    /// </summary>
    public class UseCaseParameters
    {

        /// <summary>EEBus_UC_ParameterSheet_LPC_V1.0.2.</summary>
        public PowerLimitationParameters  LPC   { get; init; } = new ();

        /// <summary>EEBus_UC_ParameterSheet_LPP_V1.0.2.</summary>
        public PowerLimitationParameters  LPP   { get; init; } = new ();

        /// <summary>EEBus_UC_ParameterSheet_MGCP_V1.0.2.</summary>
        public MonitoringParameters       MGCP  { get; init; } = new ();

        /// <summary>EEBus_UC_ParameterSheet_MPC_V1.0.2.</summary>
        public MonitoringParameters       MPC   { get; init; } = new ();


        /// <summary>
        /// The power limitation block of one of the two use cases.
        /// </summary>
        /// <param name="UseCase">"LPC" or "LPP".</param>
        public PowerLimitationParameters Limitation(String UseCase)

            => UseCase == "LPP" ? LPP : LPC;


        /// <summary>
        /// The monitoring block of one of the two use cases.
        /// </summary>
        /// <param name="UseCase">"MGCP" or "MPC".</param>
        public MonitoringParameters Monitoring(String UseCase)

            => UseCase == "MGCP" ? MGCP : MPC;


        #region (static) Parse(JSON) / ToJSON() / Validate()

        /// <summary>
        /// Read all four blocks from their JSON representation.
        /// </summary>
        /// <param name="JSON">The JSON representation, or null for the defaults.</param>
        public static UseCaseParameters Parse(JObject? JSON)

            => new () {
                   LPC   = PowerLimitationParameters.Parse(JSON?["LPC"]  as JObject),
                   LPP   = PowerLimitationParameters.Parse(JSON?["LPP"]  as JObject),
                   MGCP  = MonitoringParameters.     Parse(JSON?["MGCP"] as JObject),
                   MPC   = MonitoringParameters.     Parse(JSON?["MPC"]  as JObject)
               };


        /// <summary>
        /// The JSON representation of all four blocks.
        /// </summary>
        public JObject ToJSON()

            => new (
                   new JProperty("LPC",   LPC. ToJSON()),
                   new JProperty("LPP",   LPP. ToJSON(Production: true)),
                   new JProperty("MGCP",  MGCP.ToJSON()),
                   new JProperty("MPC",   MPC. ToJSON())
               );


        /// <summary>
        /// What the four sheets say which cannot be true.
        /// </summary>
        public IEnumerable<String> Validate()
        {

            foreach (var complaint in LPC. Validate()) yield return $"LPC: {complaint}";
            foreach (var complaint in LPP. Validate()) yield return $"LPP: {complaint}";
            foreach (var complaint in MGCP.Validate()) yield return $"MGCP: {complaint}";
            foreach (var complaint in MPC. Validate()) yield return $"MPC: {complaint}";

        }

        #endregion

    }

    #endregion

}
