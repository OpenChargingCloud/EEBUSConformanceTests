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
    /// The use case half of the catalog: the abstract test cases of the four
    /// high level test specifications V1.0.2.
    ///
    /// These specifications are shaped differently from the SHIP and SPINE ones,
    /// and the difference matters for everything below.
    ///
    /// * Their identifiers begin with <c>ATC</c>, not <c>TC</c> - an *abstract*
    ///   test case, from which one or more *specific* test cases are derived by
    ///   filling in the data sets of section 6.11 (LPC section 6.8: "(all)" means
    ///   every value gets its own specific test case, "(any)" means pick one).
    ///   51 + 51 + 47 + 54 = 203 abstract cases become 99 + 99 + 110 + 144 = 452
    ///   specific ones, and the official parameter sheets count exactly that.
    ///   The catalog is the abstract layer, because that is where the identifiers
    ///   and the requirement mapping live; the variation is a property of a case,
    ///   carried in <see cref="ConformanceTestCase.SpecificTestCases"/>.
    ///
    /// * They are about a *use case*, not about a protocol, so the actor decides
    ///   everything: an energy guard is never asked a controllable system's
    ///   questions. And unlike SHIP and SPINE, where every certifiable device has
    ///   to master both roles, here a device usually implements exactly one side.
    ///
    /// * Their preconditions are named configurations (<c>CF_CS_UnlCntrl</c>,
    ///   <c>CF_EG_ConnectionLoss</c>) rather than protocol phases - a state of the
    ///   use case's own state machine, reached however the test bench can reach
    ///   it.
    ///
    /// * Much of what they mark optional is optional *conditionally*: the
    ///   parameter sheets carry an "Optional support" worksheet, and a "yes"
    ///   there turns a recommended case into a mandatory one. That is why
    ///   <see cref="UseCaseParameters"/> exists and why so many entries here
    ///   carry a <see cref="ConformanceTestCase.NotApplicableBecause"/>.
    /// </summary>
    public static class UseCaseCatalog
    {

        #region Data

        private static readonly Lazy<IReadOnlyList<ConformanceTestCase>> testCases = new (
            () => [ .. PowerLimitationCatalog.TestCases,
                    .. MonitoringCatalog.     TestCases ]
        );

        private static readonly Lazy<IReadOnlyList<ConformanceRequirement>> requirements = new (
            () => [ .. PowerLimitationCatalog.Requirements,
                    .. MonitoringCatalog.     Requirements ]
        );

        #endregion

        #region Properties

        /// <summary>
        /// Every abstract test case of the four use case test specifications.
        /// </summary>
        public static IReadOnlyList<ConformanceTestCase>     TestCases
            => testCases.Value;

        /// <summary>
        /// Every requirement they map to.
        /// </summary>
        public static IReadOnlyList<ConformanceRequirement>  Requirements
            => requirements.Value;

        #endregion

    }


    #region (class) UseCaseSources

    /// <summary>
    /// Where the use case requirements come from, for the report.
    /// </summary>
    public static class UseCaseSources
    {

        /// <summary>Limitation of Power Consumption 1.0.0.</summary>
        public const String LPC   = "LPC 1.0.0";

        /// <summary>Limitation of Power Production 1.0.0.</summary>
        public const String LPP   = "LPP 1.0.0";

        /// <summary>Monitoring of Grid Connection Point 1.0.0.</summary>
        public const String MGCP  = "MGCP 1.0.0";

        /// <summary>Monitoring of Power Consumption 1.0.0.</summary>
        public const String MPC   = "MPC 1.0.0";


        /// <summary>
        /// The reason the LPC and LPP test specifications give for the
        /// requirements they leave untested because they are not about the
        /// protocol at all.
        /// </summary>
        public const String LocalRegulations
            = "Out of scope: the requirement refers to local regulations, external standards or internal conditions.";

        /// <summary>
        /// The reason they give for the requirements about what a value *means*
        /// rather than about what is on the wire.
        /// </summary>
        public const String DataQuality
            = "Out of scope: the test specification does not check the quality of the data.";

    }

    #endregion

}
