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

    #region (enum) ConformanceLayers

    /// <summary>
    /// Which specification a test case belongs to.
    /// </summary>
    public enum ConformanceLayers
    {

        /// <summary>EEBus_SHIP_TestSpecification_V1.0.0.</summary>
        SHIP,

        /// <summary>EEBus_SPINE_TestSpecification_V1.0.0.</summary>
        SPINE,

        /// <summary>The use case high level test specifications V1.0.2.</summary>
        UseCase,

        /// <summary>Our own cases beyond the official catalog.</summary>
        OpenChargingCloud

    }

    #endregion

    #region (enum) DUTRoles

    /// <summary>
    /// The role a device under test takes within a test case.
    ///
    /// This is never an applicability filter: every certifiable device has to
    /// master both roles (SPINE test specification, chapter 3.2). It says which
    /// half of the conversation the device is on while the case runs.
    /// </summary>
    public enum DUTRoles
    {

        /// <summary>The device accepts the connection, respectively answers.</summary>
        Server,

        /// <summary>The device opens the connection, respectively asks.</summary>
        Client,

        /// <summary>Both at the same time, on two connections.</summary>
        ServerAndClient,

        /// <summary>Either one; the case is executed once, in whichever role.</summary>
        ServerOrClient,

        /// <summary>Neither: node management, where both sides are entity 0, feature 0.</summary>
        Special

    }

    #endregion


    #region (class) ConformanceRequirement

    /// <summary>
    /// A normative requirement of one of the EEBUS specifications, as the test
    /// specifications identify it.
    ///
    /// The text is our own one line paraphrase together with the section it
    /// comes from - the normative wording lives in the specification and
    /// nowhere else. That is not a shortcut but what the test specifications
    /// themselves do and say they do ("The requirement texts in this table are
    /// short summaries of the referenced source sections and are not
    /// necessarily verbatim quotes", SHIP test specification chapter 3.1).
    /// </summary>
    /// <param name="Id">The official requirement identifier, e.g. "SHIP-TS-CMI-01".</param>
    /// <param name="Source">Where it comes from, e.g. "SHIP 13.4.3".</param>
    /// <param name="Text">What it demands, in one line.</param>
    public sealed record ConformanceRequirement(String  Id,
                                                String  Source,
                                                String  Text)
    {

        public override String ToString()

            => $"{Id} ({Source}): {Text}";

    }

    #endregion

    #region (class) ConformanceTestCase

    /// <summary>
    /// One test case of the catalog, as the official test specification defines
    /// it: identifier, roles, applicability, preconditions and the requirements
    /// it verifies.
    ///
    /// This is the *specification* of a test case, not its execution. Whether
    /// the case can be run at all against a given device follows from
    /// <see cref="Applies"/> and the parameter sheet; how it is run is an
    /// <see cref="AConformanceTest"/> carrying the same identifier.
    /// </summary>
    /// <param name="Id">The official test case identifier, e.g. "TC_SHIP_CMI_003".</param>
    /// <param name="Layer">Which specification it belongs to.</param>
    /// <param name="Group">The group within it, e.g. "CMI".</param>
    /// <param name="Title">What it verifies, as the specification names it.</param>
    /// <param name="DUTRole">The role the device under test takes.</param>
    /// <param name="Actor">The use case actor a device has to implement for the case to apply: "Any", "EG" or "CS".</param>
    /// <param name="Mandatory">Whether passing it is required for compliance.</param>
    /// <param name="Requirements">The requirement identifiers it verifies.</param>
    /// <param name="Preconditions">The PRE_ identifiers which have to hold before the first step.</param>
    public sealed record ConformanceTestCase(String                 Id,
                                             ConformanceLayers      Layer,
                                             String                 Group,
                                             String                 Title,
                                             DUTRoles               DUTRole,
                                             String                 Actor,
                                             Boolean                Mandatory,
                                             IReadOnlyList<String>  Requirements,
                                             IReadOnlyList<String>  Preconditions)
    {

        /// <summary>
        /// Why the case does not apply to a device declaring the given
        /// parameters, or null when it does apply.
        ///
        /// A case which does not apply is neither passed nor failed: the
        /// official wording is "Not Applicable", "regardless of its M or O
        /// status" (SPINE test specification, chapter 3.2).
        /// </summary>
        public Func<ParameterSheet, String?>?  NotApplicableBecause    { get; init; }

        /// <summary>
        /// The applicability as the specification words it, for the report.
        /// </summary>
        public String?                         Applicability           { get; init; }

        /// <summary>
        /// Why this stack is knowingly expected to fail this case, and where
        /// that decision is written down.
        ///
        /// A deviation is *not* a reason to soften the verdict: the report
        /// still says "failed", because that is what a certification body has
        /// to see. It is a reason to keep the build green while the decision
        /// stands, and to say out loud which decision that is - the conflict
        /// rule of WORKPLAN § 1.2 (the Go behaviour decides for wire
        /// compatibility, the specification decides for the conformance tests)
        /// only means anything if both halves are visible.
        /// </summary>
        public String?                         KnownDeviation          { get; init; }


        /// <summary>
        /// Whether this case applies to a device declaring the given parameters.
        /// </summary>
        /// <param name="Parameters">The manufacturer declarations of the device under test.</param>
        /// <param name="Reason">Why not, when it does not.</param>
        public Boolean Applies(ParameterSheet Parameters, out String? Reason)
        {

            Reason = NotApplicableBecause?.Invoke(Parameters);

            if (Reason is null &&
                Actor  != "Any" &&
                !Parameters.Actors.Contains(Actor))
            {
                Reason = $"the device does not declare the actor {Actor}";
            }

            return Reason is null;

        }


        public override String ToString()

            => $"{Id}: {Title}";

    }

    #endregion


    #region (class) ConformanceCatalog

    /// <summary>
    /// The conformance catalog: every test case of the official test
    /// specifications, and the requirements they map to.
    ///
    /// The catalog is deliberately *data* and deliberately complete - it holds
    /// cases we cannot execute yet just as much as those we can. That is the
    /// point of a catalog: the coverage report can only be honest if the
    /// denominator is the official one rather than "everything we happen to
    /// have written".
    /// </summary>
    public static class ConformanceCatalog
    {

        #region Data

        private static readonly Lazy<IReadOnlyList<ConformanceTestCase>> testCases = new (
            () => [ .. SHIPCatalog. TestCases,
                    .. SPINECatalog.TestCases ]
        );

        private static readonly Lazy<IReadOnlyList<ConformanceRequirement>> requirements = new (
            () => [ .. SHIPCatalog. Requirements,
                    .. SPINECatalog.Requirements ]
        );

        #endregion

        #region Properties

        /// <summary>
        /// Every test case of the catalog.
        /// </summary>
        public static IReadOnlyList<ConformanceTestCase>     TestCases
            => testCases.Value;

        /// <summary>
        /// Every requirement the test cases map to.
        /// </summary>
        public static IReadOnlyList<ConformanceRequirement>  Requirements
            => requirements.Value;

        #endregion


        #region TestCase(Id) / Requirement(Id)

        /// <summary>
        /// The test case with the given identifier, or null.
        /// </summary>
        /// <param name="Id">An official test case identifier.</param>
        public static ConformanceTestCase? TestCase(String Id)

            => TestCases.FirstOrDefault(testCase => testCase.Id == Id);


        /// <summary>
        /// The requirement with the given identifier, or null.
        /// </summary>
        /// <param name="Id">An official requirement identifier.</param>
        public static ConformanceRequirement? Requirement(String Id)

            => Requirements.FirstOrDefault(requirement => requirement.Id == Id);

        #endregion

        #region Of(Layer) / TestCasesFor(RequirementId)

        /// <summary>
        /// Every test case of one specification.
        /// </summary>
        /// <param name="Layer">A specification.</param>
        public static IEnumerable<ConformanceTestCase> Of(ConformanceLayers Layer)

            => TestCases.Where(testCase => testCase.Layer == Layer);


        /// <summary>
        /// Every test case verifying the given requirement - the requirement to
        /// test case mapping of chapter 3.1, read off the cases themselves so
        /// that the two directions cannot drift apart.
        /// </summary>
        /// <param name="RequirementId">An official requirement identifier.</param>
        public static IEnumerable<ConformanceTestCase> TestCasesFor(String RequirementId)

            => TestCases.Where(testCase => testCase.Requirements.Contains(RequirementId));

        #endregion

        #region Verify()

        /// <summary>
        /// Check the catalog against itself: every requirement a case names has
        /// to exist, and every requirement has to be verified by at least one
        /// case.
        ///
        /// Both directions matter. A case naming a requirement which is not in
        /// the table means the mapping was mistyped; a requirement no case
        /// names means the specification demands something nobody tests, which
        /// is worth knowing about a *test* specification.
        /// </summary>
        public static IEnumerable<String> Verify()
        {

            foreach (var testCase in TestCases)
                foreach (var requirement in testCase.Requirements)
                    if (Requirement(requirement) is null)
                        yield return $"{testCase.Id} refers to the unknown requirement {requirement}.";

            foreach (var requirement in Requirements)
                if (!TestCasesFor(requirement.Id).Any())
                    yield return $"No test case verifies {requirement.Id}.";

            foreach (var duplicate in TestCases.GroupBy(testCase => testCase.Id).Where(group => group.Count() > 1))
                yield return $"The test case {duplicate.Key} exists {duplicate.Count()} times.";

        }

        #endregion

    }

    #endregion

}
