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

using System.Globalization;
using System.Text;

#endregion

namespace cloud.charging.open.protocols.EEBUS.Simulations
{

    /// <summary>
    /// One thing which happened during a simulation.
    /// </summary>
    /// <param name="At">How far into the simulation it happened.</param>
    /// <param name="Actor">Who it happened to or who did it.</param>
    /// <param name="Message">What happened, for a person.</param>
    /// <param name="Rule">The rule of a specification which says it should, where there is one.</param>
    public sealed record SimulationEvent(TimeSpan  At,
                                         String    Actor,
                                         String    Message,
                                         String?   Rule   = null)
    {

        /// <summary>Return a text representation of this event.</summary>
        public override String ToString()

            => $"{At:hh\\:mm\\:ss}  {Actor,-16}  {Message}" +
               $"{(Rule is not null ? $"  [{Rule}]" : "")}";

    }


    /// <summary>
    /// One measured or decided number at one point in a simulation.
    /// </summary>
    /// <param name="At">When.</param>
    /// <param name="Series">Which quantity it belongs to.</param>
    /// <param name="Value">Its value.</param>
    public sealed record SimulationSample(TimeSpan  At,
                                          String    Series,
                                          Decimal   Value);


    /// <summary>
    /// What a simulation produced: a story and a table.
    ///
    /// The story is for a person reading afterwards - what happened, when, and
    /// which rule of which specification says it should have. The table is the
    /// same run as numbers over time, which is what makes a simulation a thing
    /// you can plot rather than only read.
    ///
    /// Both are also what the smoke tests assert against, which is the point of
    /// keeping the two apart: a test which wants to know "did the failsafe
    /// activate" asks the log, and one which wants to know "what was the power
    /// then" asks the samples.
    /// </summary>
    public sealed class SimulationLog
    {

        #region Data

        private readonly List<SimulationEvent>   events   = [];
        private readonly List<SimulationSample>  samples  = [];

        #endregion

        #region Properties

        /// <summary>
        /// Everything which happened, in order.
        /// </summary>
        public IReadOnlyList<SimulationEvent>   Events    => events;

        /// <summary>
        /// Every number which was recorded, in order.
        /// </summary>
        public IReadOnlyList<SimulationSample>  Samples   => samples;

        /// <summary>
        /// The names of the recorded series, in the order they first appeared.
        /// </summary>
        public IEnumerable<String>              Series
            => samples.Select(sample => sample.Series).Distinct();

        #endregion


        #region Log(At, Actor, Message, Rule = null) / Sample(At, Series, Value)

        /// <summary>
        /// Write down that something happened.
        /// </summary>
        /// <param name="At">How far into the simulation.</param>
        /// <param name="Actor">Who.</param>
        /// <param name="Message">What.</param>
        /// <param name="Rule">Which rule of a specification says so, where there is one.</param>
        public SimulationEvent Log(TimeSpan  At,
                                   String    Actor,
                                   String    Message,
                                   String?   Rule   = null)
        {

            var entry = new SimulationEvent(At, Actor, Message, Rule);

            events.Add(entry);

            return entry;

        }


        /// <summary>
        /// Write down a number.
        /// </summary>
        /// <param name="At">How far into the simulation.</param>
        /// <param name="Series">Which quantity.</param>
        /// <param name="Value">Its value.</param>
        public void Sample(TimeSpan  At,
                           String    Series,
                           Decimal   Value)
        {
            samples.Add(new SimulationSample(At, Series, Value));
        }

        #endregion

        #region Happened(Contains) / ValueAt(Series, At)

        /// <summary>
        /// Whether something whose message contains the given text was logged.
        /// </summary>
        /// <param name="Contains">A piece of a message.</param>
        public Boolean Happened(String Contains)

            => events.Any(entry => entry.Message.Contains(Contains, StringComparison.OrdinalIgnoreCase));


        /// <summary>
        /// What a series was showing at a given point - the last value recorded
        /// at or before it, which is what "the power at 10:30" means for a
        /// series that only changes when something happens.
        /// </summary>
        /// <param name="Series">Which quantity.</param>
        /// <param name="At">When.</param>
        public Decimal? ValueAt(String    Series,
                                TimeSpan  At)

            => samples.Where     (sample => sample.Series == Series && sample.At <= At).
                       OrderBy   (sample => sample.At).
                       LastOrDefault()?.Value;

        #endregion


        #region ToText() / ToCSV() / ToMarkdown(Title)

        /// <summary>
        /// The story, as a person reads it on a terminal.
        /// </summary>
        public String ToText()
        {

            var text = new StringBuilder();

            foreach (var entry in events)
                text.AppendLine(entry.ToString());

            return text.ToString();

        }


        /// <summary>
        /// The numbers, as a spreadsheet or a plotting tool reads them: one row
        /// per point in time, one column per series.
        ///
        /// A series which did not change at a given time keeps its last value,
        /// because a gap in a power curve means "no reading" and this is not
        /// that - it is a value which stayed where it was.
        /// </summary>
        public String ToCSV()
        {

            var series  = Series.ToList();
            var times   = samples.Select(sample => sample.At).Distinct().Order().ToList();
            var latest  = new Dictionary<String, Decimal>();

            var csv     = new StringBuilder();

            csv.AppendLine(String.Join(';', [ "seconds", .. series ]));

            foreach (var at in times)
            {

                foreach (var sample in samples.Where(sample => sample.At == at))
                    latest[sample.Series] = sample.Value;

                csv.AppendLine(String.Join(';',
                                   [ at.TotalSeconds.ToString(CultureInfo.InvariantCulture),
                                     .. series.Select(name => latest.TryGetValue(name, out var value)
                                                                  ? value.ToString(CultureInfo.InvariantCulture)
                                                                  : "") ]));

            }

            return csv.ToString();

        }


        /// <summary>
        /// The story as a report, for docs/reports/.
        /// </summary>
        /// <param name="Title">What the simulation is called.</param>
        public String ToMarkdown(String Title)
        {

            var markdown = new StringBuilder();

            markdown.AppendLine($"# {Title}");
            markdown.AppendLine();
            markdown.AppendLine("| Time | Actor | What happened | Rule |");
            markdown.AppendLine("|---|---|---|---|");

            foreach (var entry in events)
                markdown.AppendLine($"| {entry.At:hh\\:mm\\:ss} | {entry.Actor} | {entry.Message} | {entry.Rule ?? ""} |");

            markdown.AppendLine();

            return markdown.ToString();

        }

        #endregion

    }

}
