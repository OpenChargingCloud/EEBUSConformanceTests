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

    #region (enum) ConformanceVerdicts

    /// <summary>
    /// What a test case run says about a device.
    ///
    /// The distinction which matters most is between <see cref="Failed"/> and
    /// <see cref="NotApplicable"/>, and between both of them and
    /// <see cref="NotImplemented"/>. A device which was never asked has not
    /// passed anything; a case we have not written yet says nothing about the
    /// device at all. Collapsing any of these into "not passed" is how a
    /// conformance report starts lying.
    /// </summary>
    public enum ConformanceVerdicts
    {

        /// <summary>Every step met its expected result.</summary>
        Passed,

        /// <summary>
        /// Every step passed, but something the specification tolerates only
        /// for now happened - the version format cases are the reason this
        /// value exists (SPINE test specification 4.1.6).
        /// </summary>
        Warning,

        /// <summary>A step did not meet its expected result.</summary>
        Failed,

        /// <summary>
        /// The device does not fulfil the prerequisites of the case. Not a
        /// result about the device: a statement that the question was not
        /// asked, "regardless of its M or O status".
        /// </summary>
        NotApplicable,

        /// <summary>
        /// The catalog knows this case, but no executable test carries its
        /// identifier yet. A statement about us, not about the device.
        /// </summary>
        NotImplemented,

        /// <summary>
        /// The case could not be run to a verdict - a missing precondition, a
        /// test tool error, an exception.
        /// </summary>
        Inconclusive

    }

    #endregion

    #region (class) ConformanceStepResult

    /// <summary>
    /// One step of a test case, in the shape the specification writes it:
    /// what the test tool did, what the device was supposed to do, and what
    /// actually happened.
    /// </summary>
    /// <param name="Number">The step number, as in the specification's table.</param>
    /// <param name="Action">What the test tool did.</param>
    /// <param name="Expected">What the device was supposed to do.</param>
    /// <param name="Verdict">What happened.</param>
    /// <param name="Note">What was observed, when that is worth saying.</param>
    public sealed record ConformanceStepResult(String               Number,
                                               String               Action,
                                               String               Expected,
                                               ConformanceVerdicts  Verdict,
                                               String?              Note   = null)
    {

        public override String ToString()

            => $"{Number}. {Verdict}: {Expected}{(Note is not null ? $" - {Note}" : "")}";

    }

    #endregion

    #region (class) ConformanceOutcome

    /// <summary>
    /// The result of one test case against one device.
    /// </summary>
    /// <param name="TestCase">The catalog entry.</param>
    /// <param name="Verdict">The overall verdict.</param>
    /// <param name="Steps">What each step did.</param>
    /// <param name="Summary">One line saying why, for the report.</param>
    /// <param name="Duration">How long the case took in device time.</param>
    public sealed record ConformanceOutcome(ConformanceTestCase                  TestCase,
                                            ConformanceVerdicts                  Verdict,
                                            IReadOnlyList<ConformanceStepResult> Steps,
                                            String?                              Summary    = null,
                                            TimeSpan                             Duration   = default)
    {

        /// <summary>
        /// Whether this outcome stands in the way of compliance: a mandatory
        /// case which was asked and did not pass.
        /// </summary>
        public Boolean Blocking

            => TestCase.Mandatory &&
               Verdict is ConformanceVerdicts.Failed or ConformanceVerdicts.Inconclusive;


        public override String ToString()

            => $"{TestCase.Id}: {Verdict}{(Summary is not null ? $" - {Summary}" : "")}";

    }

    #endregion

    #region (class) ConformanceRun

    /// <summary>
    /// A whole run of the catalog against one device.
    /// </summary>
    /// <param name="Parameters">What the device declared about itself.</param>
    /// <param name="Outcomes">What each case said.</param>
    /// <param name="StartedAt">When the run started.</param>
    public sealed record ConformanceRun(ParameterSheet                    Parameters,
                                        IReadOnlyList<ConformanceOutcome> Outcomes,
                                        DateTimeOffset                    StartedAt)
    {

        /// <summary>
        /// How many cases ended with the given verdict.
        /// </summary>
        /// <param name="Verdict">A verdict.</param>
        public Int32 Count(ConformanceVerdicts Verdict)

            => Outcomes.Count(outcome => outcome.Verdict == Verdict);


        /// <summary>
        /// Every mandatory case which was asked and did not pass. An empty list
        /// is the only thing which means anything.
        /// </summary>
        public IEnumerable<ConformanceOutcome> Blocking

            => Outcomes.Where(outcome => outcome.Blocking);


        /// <summary>
        /// Whether the device passed every mandatory case which applied to it.
        ///
        /// Strictly, and including the cases this repository knowingly fails:
        /// "conformant" is not a thing one gets to define locally.
        /// </summary>
        public Boolean Compliant

            => !Blocking.Any();


        /// <summary>
        /// The blocking outcomes nobody has decided about yet.
        ///
        /// This is the list a build should watch. A case which fails because of
        /// a decision written into the catalog is still reported as failed -
        /// see <see cref="Compliant"/> - but it is not news, and a build which
        /// is red forever is a build nobody reads.
        /// </summary>
        public IEnumerable<ConformanceOutcome> Unexpected

            => Blocking.Where(outcome => outcome.TestCase.KnownDeviation is null);


        /// <summary>
        /// The coverage per requirement: which cases verify it, and what they
        /// said. This is the table chapter 3.1 of both test specifications
        /// exists for - a failure points at the specification clause it broke.
        /// </summary>
        public IEnumerable<(ConformanceRequirement Requirement, IReadOnlyList<ConformanceOutcome> Outcomes)> PerRequirement()
        {

            foreach (var requirement in ConformanceCatalog.Requirements)
                yield return (
                    requirement,
                    Outcomes.Where(outcome => outcome.TestCase.Requirements.Contains(requirement.Id)).ToList()
                );

        }


        public override String ToString()

            => $"{Parameters.DeviceName}: {Count(ConformanceVerdicts.Passed)} passed, " +
               $"{Count(ConformanceVerdicts.Failed)} failed, " +
               $"{Count(ConformanceVerdicts.Warning)} with warnings, " +
               $"{Count(ConformanceVerdicts.NotApplicable)} not applicable, " +
               $"{Count(ConformanceVerdicts.NotImplemented)} not implemented";

    }

    #endregion

}
