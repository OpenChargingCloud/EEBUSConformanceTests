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

#endregion

namespace cloud.charging.open.protocols.EEBUS.Conformance
{

    /// <summary>
    /// What a conformance run looks like when somebody has to read it.
    ///
    /// Three shapes, because three different people ask: a summary for whoever
    /// wants a verdict, the failing steps for whoever has to fix something, and
    /// the coverage per requirement for whoever has to argue with a
    /// certification body about which clause was actually broken.
    /// </summary>
    public static class ConformanceReport
    {

        #region ToText(Run)

        /// <summary>
        /// The run as plain text, for a terminal.
        /// </summary>
        /// <param name="Run">A conformance run.</param>
        public static String ToText(ConformanceRun Run)
        {

            var text = new StringBuilder();

            text.AppendLine($"EEBUS conformance run against {Run.Parameters.DeviceName} ({Run.Parameters.Manufacturer})");
            text.AppendLine($"{Run.StartedAt:yyyy-MM-dd HH:mm:ss} UTC, SHIP {Run.Parameters.ShipVersion}, SPINE {Run.Parameters.SpineVersion}");
            text.AppendLine();

            foreach (var group in Run.Outcomes.GroupBy(outcome => (outcome.TestCase.Layer, outcome.TestCase.Group)))
            {

                text.AppendLine($"{group.Key.Layer} {group.Key.Group}");

                foreach (var outcome in group)
                {

                    text.AppendLine($"  {Symbol(outcome.Verdict)} {outcome.TestCase.Id,-22} {outcome.TestCase.Title}");

                    if (outcome.Summary is not null)
                        text.AppendLine($"      {outcome.Summary}");

                    if (outcome.Verdict is ConformanceVerdicts.Failed or ConformanceVerdicts.Inconclusive)
                        foreach (var step in outcome.Steps.Where(step => step.Verdict != ConformanceVerdicts.Passed))
                            text.AppendLine($"      step {step.Number}: {step.Expected}");

                }

                text.AppendLine();

            }

            text.AppendLine(Summary(Run));

            return text.ToString();

        }

        #endregion

        #region ToMarkdown(Run)

        /// <summary>
        /// The run as Markdown, for docs/reports/.
        /// </summary>
        /// <param name="Run">A conformance run.</param>
        public static String ToMarkdown(ConformanceRun Run)
        {

            var text = new StringBuilder();

            #region Header and summary

            text.AppendLine($"# EEBUS conformance report — {Run.Parameters.DeviceName}");
            text.AppendLine();
            text.AppendLine($"*{Run.StartedAt:yyyy-MM-dd HH:mm:ss} UTC · {Run.Parameters.Manufacturer} · " +
                            $"SHIP {Run.Parameters.ShipVersion} · SPINE {Run.Parameters.SpineVersion} · " +
                            $"actors {String.Join(", ", Run.Parameters.Actors.Order())}*");
            text.AppendLine();
            text.AppendLine("Catalog: `EEBus_SHIP_TestSpecification_V1.0.0`, `EEBus_SPINE_TestSpecification_V1.0.0`. " +
                            "Identifiers are the official ones.");
            text.AppendLine();

            text.AppendLine("| Verdict | Cases |");
            text.AppendLine("|---|---|");

            foreach (var verdict in Enum.GetValues<ConformanceVerdicts>())
                text.AppendLine($"| {Symbol(verdict)} {Name(verdict)} | {Run.Count(verdict)} |");

            text.AppendLine();
            text.AppendLine(Run.Compliant
                                ? "**Every mandatory test case which applied to this device passed.**"
                                : $"**{Run.Blocking.Count()} mandatory test case(s) which applied to this device did not pass.**");
            text.AppendLine();

            #endregion

            #region The cases

            text.AppendLine("## Test cases");
            text.AppendLine();

            foreach (var group in Run.Outcomes.GroupBy(outcome => (outcome.TestCase.Layer, outcome.TestCase.Group)))
            {

                text.AppendLine($"### {group.Key.Layer} — {group.Key.Group}");
                text.AppendLine();
                text.AppendLine("| Test case | Title | DUT | M/O | Verdict | Note |");
                text.AppendLine("|---|---|---|---|---|---|");

                foreach (var outcome in group)
                    text.AppendLine($"| `{outcome.TestCase.Id}` " +
                                    $"| {Escape(outcome.TestCase.Title)} " +
                                    $"| {outcome.TestCase.DUTRole}{(outcome.TestCase.Actor != "Any" ? $" ({outcome.TestCase.Actor})" : "")} " +
                                    $"| {(outcome.TestCase.Mandatory ? "M" : "O")} " +
                                    $"| {Symbol(outcome.Verdict)} {Name(outcome.Verdict)} " +
                                    $"| {Escape(outcome.Summary ?? "")} |");

                text.AppendLine();

            }

            #endregion

            #region The steps of everything which did not simply pass

            var interesting = Run.Outcomes.
                                  Where(outcome => outcome.Verdict is ConformanceVerdicts.Failed
                                                                   or ConformanceVerdicts.Warning
                                                                   or ConformanceVerdicts.Inconclusive).
                                  ToList();

            if (interesting.Count > 0)
            {

                text.AppendLine("## What happened");
                text.AppendLine();

                foreach (var outcome in interesting)
                {

                    text.AppendLine($"### `{outcome.TestCase.Id}` — {outcome.TestCase.Title}");
                    text.AppendLine();
                    text.AppendLine($"{Symbol(outcome.Verdict)} **{Name(outcome.Verdict)}**" +
                                    $"{(outcome.Summary is not null ? $" — {outcome.Summary}" : "")}");
                    text.AppendLine();

                    if (outcome.TestCase.Requirements.Count > 0)
                        text.AppendLine($"Verifies {String.Join(", ", outcome.TestCase.Requirements.Select(requirement => $"`{requirement}`"))}.");

                    // A failure this repository has already decided about says
                    // so here. The verdict above stays "failed" - a decision is
                    // not a pass - but a reader deserves to know it was one.
                    if (outcome.Verdict == ConformanceVerdicts.Failed &&
                        outcome.TestCase.KnownDeviation is String deviation)
                    {
                        text.AppendLine();
                        text.AppendLine($"> **Known deviation.** {deviation}");
                    }

                    text.AppendLine();
                    text.AppendLine("| Step | Action | Expected | Result |");
                    text.AppendLine("|---|---|---|---|");

                    foreach (var step in outcome.Steps)
                        text.AppendLine($"| {step.Number} " +
                                        $"| {Escape(step.Action)} " +
                                        $"| {Escape(step.Expected)} " +
                                        $"| {Symbol(step.Verdict)} {Escape(step.Note ?? Name(step.Verdict))} |");

                    text.AppendLine();

                }

            }

            #endregion

            #region The coverage per requirement

            text.AppendLine("## Requirement coverage");
            text.AppendLine();
            text.AppendLine("| Requirement | Source | Verified by | Result |");
            text.AppendLine("|---|---|---|---|");

            foreach (var (requirement, outcomes) in Run.PerRequirement())
            {

                if (outcomes.Count == 0)
                    continue;

                var worst = outcomes.Any(outcome => outcome.Verdict == ConformanceVerdicts.Failed)
                                ? ConformanceVerdicts.Failed
                                : outcomes.Any(outcome => outcome.Verdict == ConformanceVerdicts.Inconclusive)
                                      ? ConformanceVerdicts.Inconclusive
                                      : outcomes.Any(outcome => outcome.Verdict == ConformanceVerdicts.NotImplemented)
                                            ? ConformanceVerdicts.NotImplemented
                                            : outcomes.Any(outcome => outcome.Verdict == ConformanceVerdicts.Warning)
                                                  ? ConformanceVerdicts.Warning
                                                  : outcomes.All(outcome => outcome.Verdict == ConformanceVerdicts.NotApplicable)
                                                        ? ConformanceVerdicts.NotApplicable
                                                        : ConformanceVerdicts.Passed;

                text.AppendLine($"| `{requirement.Id}` " +
                                $"| {Escape(requirement.Source)} " +
                                $"| {String.Join(", ", outcomes.Select(outcome => $"`{outcome.TestCase.Id}`"))} " +
                                $"| {Symbol(worst)} {Name(worst)} |");

            }

            text.AppendLine();

            #endregion

            text.AppendLine("---");
            text.AppendLine();
            text.AppendLine("Generated by `eebus conform`. The EEBUS specifications are licensed material and are not " +
                            "part of this repository; this report reproduces identifiers and section references only.");

            return text.ToString();

        }

        #endregion

        #region ToCSV(Run)

        /// <summary>
        /// The run as one row per test case, for whoever wants a spreadsheet.
        /// </summary>
        /// <param name="Run">A conformance run.</param>
        public static String ToCSV(ConformanceRun Run)
        {

            var text = new StringBuilder();

            text.AppendLine("testCase;layer;group;title;dutRole;actor;mandatory;verdict;requirements;note");

            foreach (var outcome in Run.Outcomes)
                text.AppendLine($"{outcome.TestCase.Id};" +
                                $"{outcome.TestCase.Layer};" +
                                $"{outcome.TestCase.Group};" +
                                $"{outcome.TestCase.Title.Replace(';', ',')};" +
                                $"{outcome.TestCase.DUTRole};" +
                                $"{outcome.TestCase.Actor};" +
                                $"{(outcome.TestCase.Mandatory ? "M" : "O")};" +
                                $"{Name(outcome.Verdict)};" +
                                $"{String.Join(" ", outcome.TestCase.Requirements)};" +
                                $"{(outcome.Summary ?? "").Replace(';', ',')}");

            return text.ToString();

        }

        #endregion

        #region Summary(Run)

        /// <summary>
        /// The one line a build log wants.
        /// </summary>
        /// <param name="Run">A conformance run.</param>
        public static String Summary(ConformanceRun Run)

            => $"{Run.Count(ConformanceVerdicts.Passed)} passed, " +
               $"{Run.Count(ConformanceVerdicts.Failed)} failed, " +
               $"{Run.Count(ConformanceVerdicts.Warning)} with warnings, " +
               $"{Run.Count(ConformanceVerdicts.Inconclusive)} inconclusive, " +
               $"{Run.Count(ConformanceVerdicts.NotApplicable)} not applicable, " +
               $"{Run.Count(ConformanceVerdicts.NotImplemented)} not implemented " +
               $"of {Run.Outcomes.Count} catalog entries.";

        #endregion


        #region (private) Symbol(Verdict) / Name(Verdict) / Escape(Text)

        private static String Symbol(ConformanceVerdicts Verdict)

            => Verdict switch {
                   ConformanceVerdicts.Passed          => "✅",
                   ConformanceVerdicts.Warning         => "⚠️",
                   ConformanceVerdicts.Failed          => "❌",
                   ConformanceVerdicts.NotApplicable   => "➖",
                   ConformanceVerdicts.NotImplemented  => "🚧",
                   _                                   => "❔"
               };

        private static String Name(ConformanceVerdicts Verdict)

            => Verdict switch {
                   ConformanceVerdicts.Passed          => "passed",
                   ConformanceVerdicts.Warning         => "warning",
                   ConformanceVerdicts.Failed          => "failed",
                   ConformanceVerdicts.NotApplicable   => "not applicable",
                   ConformanceVerdicts.NotImplemented  => "not implemented",
                   _                                   => "inconclusive"
               };

        private static String Escape(String Text)

            => Text.Replace("|", "\\|").Replace("\r", "").Replace("\n", " ");

        #endregion

    }

}
