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

using NUnit.Framework;

#endregion

namespace cloud.charging.open.protocols.EEBUS.Conformance.tests
{

    /// <summary>
    /// The catalog itself, checked against itself.
    ///
    /// A conformance suite whose catalog has drifted reports nonsense with full
    /// confidence, so the mapping tables of the two test specifications are
    /// verified in both directions before anything is executed.
    /// </summary>
    [TestFixture]
    public class ConformanceCatalogTests
    {

        #region TheCatalogIsComplete()

        /// <summary>
        /// The official catalogs hold thirty-three SHIP and thirty-one SPINE
        /// test cases, and 203 abstract test cases across the four use case
        /// specifications. If any number changes, a specification was updated
        /// and this suite has not caught up.
        ///
        /// The use case numbers are checked against the count each official
        /// parameter sheet states about itself on its "Report" worksheet, which
        /// is the one place the specifications total themselves up. The number of
        /// *specific* test cases is checked with them, because that is the
        /// number a certification body counts: an abstract case whose data set
        /// says "(all)" is executed once per value.
        /// </summary>
        [Test]
        public void TheCatalogIsComplete()
        {

            Assert.Multiple(() => {

                Assert.That(ConformanceCatalog.Of(ConformanceLayers.SHIP). Count(), Is.EqualTo(33),
                            "EEBus_SHIP_TestSpecification_V1.0.0 defines 33 test cases.");

                Assert.That(ConformanceCatalog.Of(ConformanceLayers.SPINE).Count(), Is.EqualTo(31),
                            "EEBus_SPINE_TestSpecification_V1.0.0 defines 31 test cases.");

                foreach (var (useCase, cases, specific) in new[] {
                             ("LPC",  51, 99),
                             ("LPP",  51, 99),
                             ("MGCP", 47, 110),
                             ("MPC",  54, 144)
                         })
                {

                    var entries = ConformanceCatalog.TestCases.
                                      Where(testCase => testCase.Id.StartsWith($"ATC_{useCase}_", StringComparison.Ordinal)).
                                      ToList();

                    Assert.That(entries.Count, Is.EqualTo(cases),
                                $"EEBus_UC_HighLevel_TestSpecification_{useCase}_V1.0.2 defines {cases} abstract test cases.");

                    Assert.That(entries.Sum(testCase => testCase.SpecificTestCases), Is.EqualTo(specific),
                                $"EEBus_UC_ParameterSheet_{useCase}_V1.0.2 counts {specific} specific test cases.");

                }

                Assert.That(ConformanceCatalog.Of(ConformanceLayers.UseCase).Count(), Is.EqualTo(203),
                            "The four use case test specifications define 203 abstract test cases together.");

            });

        }

        #endregion

        #region TheRequirementMappingHoldsInBothDirections()

        /// <summary>
        /// Every requirement a case names exists, every requirement is verified
        /// by at least one case, and no identifier appears twice.
        /// </summary>
        [Test]
        public void TheRequirementMappingHoldsInBothDirections()
        {

            var complaints = ConformanceCatalog.Verify().ToList();

            Assert.That(complaints, Is.Empty,
                        String.Join(Environment.NewLine, complaints));

        }

        #endregion

        #region EveryTestCaseIsExecutable()

        /// <summary>
        /// Every entry of the catalog has an executable test carrying its
        /// identifier.
        ///
        /// This is allowed to fail one day - a new specification version brings
        /// new cases - but it has to fail *loudly*, because the alternative is
        /// a report which counts a case nobody wrote as one which passed.
        /// </summary>
        [Test]
        public void EveryTestCaseIsExecutable()
        {

            var missing = ConformanceSuite.Missing.ToList();

            Assert.That(missing, Is.Empty,
                        $"No executable test carries the identifier(s): {String.Join(", ", missing)}");

        }

        #endregion

        #region EveryExecutableTestIsInTheCatalog()

        /// <summary>
        /// And the other way round: nothing executable carries an identifier
        /// the catalog does not know.
        /// </summary>
        [Test]
        public void EveryExecutableTestIsInTheCatalog()
        {

            Assert.Multiple(() => {

                foreach (var id in ConformanceSuite.Implemented)
                    Assert.That(ConformanceCatalog.TestCase(id), Is.Not.Null,
                                $"'{id}' is executable, but not a test case of the catalog.");

            });

        }

        #endregion

        #region TheParameterSheetSurvivesJSON()

        /// <summary>
        /// A parameter sheet written out and read back in says the same thing,
        /// with the official PAR_ names and the "yes"/"no" wording of the
        /// official sheets.
        /// </summary>
        [Test]
        public void TheParameterSheetSurvivesJSON()
        {

            var written = ParameterSheet.OurOwnStack.ToJSON();
            var read    = ParameterSheet.Parse(written);

            Assert.Multiple(() => {

                Assert.That(written["PAR_shipSvc"]?.Value<String>(),             Is.EqualTo("yes"));
                Assert.That(written["PAR_initialTimeoutSupported"]?.Value<String>(), Is.EqualTo("no"));
                Assert.That(written["PAR_addressChangeRecovery"]?.Value<String>(),   Is.EqualTo("session-only"));

                Assert.That(read.ShipSvc,                          Is.EqualTo(ParameterSheet.OurOwnStack.ShipSvc));
                Assert.That(read.QueryAccessMethods,               Is.EqualTo(ParameterSheet.OurOwnStack.QueryAccessMethods));
                Assert.That(read.InitialTimeoutSupported,          Is.EqualTo(ParameterSheet.OurOwnStack.InitialTimeoutSupported));
                Assert.That(read.LpcLppTestValue1,                 Is.EqualTo(ParameterSheet.OurOwnStack.LpcLppTestValue1));
                Assert.That(read.SpineVersion,                     Is.EqualTo(ParameterSheet.OurOwnStack.SpineVersion));
                Assert.That(read.Actors,                           Is.EquivalentTo(ParameterSheet.OurOwnStack.Actors));

            });

        }

        #endregion

        #region OurOwnParameterSheetIsUsable()

        /// <summary>
        /// The self test profile does not contradict itself.
        /// </summary>
        [Test]
        public void OurOwnParameterSheetIsUsable()
        {

            var complaints = ParameterSheet.OurOwnStack.Validate().ToList();

            Assert.That(complaints, Is.Empty,
                        String.Join(Environment.NewLine, complaints));

        }

        #endregion

        #region ADeclarationDecidesWhetherACaseApplies()

        /// <summary>
        /// The parameter sheet is not decoration: changing a declaration
        /// changes which cases are asked at all.
        /// </summary>
        [Test]
        public void ADeclarationDecidesWhetherACaseApplies()
        {

            var silent = new ParameterSheet {
                             ShipSvc             = false,
                             ShipId              = "control-box-0001",
                             QueryAccessMethods  = false
                         };

            Assert.Multiple(() => {

                Assert.That(ConformanceCatalog.TestCase("TC_SHIP_MDNS_001")!.Applies(silent, out var mdns), Is.False,
                            "A control box which announces no SHIP service cannot be asked about its TXT record.");

                Assert.That(mdns, Does.Contain("PAR_shipSvc"));

                Assert.That(ConformanceCatalog.TestCase("TC_SHIP_AMDATA_004")!.Applies(silent, out _), Is.True,
                            "A device declaring PAR_queryAccessMethods = \"no\" is the only one this case applies to.");

                Assert.That(ConformanceCatalog.TestCase("TC_SHIP_AMDATA_004")!.Applies(ParameterSheet.OurOwnStack, out _), Is.False,
                            "A device which does ask is out of scope of that case.");

            });

        }

        #endregion

        #region AnActorDecidesWhetherAUseCaseFlavouredCaseApplies()

        /// <summary>
        /// The actor, never the role, decides whether one of the LPC/LPP
        /// flavoured SPINE cases applies (SPINE test specification, chapter 3.2).
        /// </summary>
        [Test]
        public void AnActorDecidesWhetherAUseCaseFlavouredCaseApplies()
        {

            var guardOnly = new ParameterSheet {
                                Actors = new HashSet<String> { "EG" }
                            };

            Assert.Multiple(() => {

                Assert.That(ConformanceCatalog.TestCase("TC_SPINE_RTC_001")!.Applies(guardOnly, out _), Is.True,
                            "An energy guard is asked the energy guard cases.");

                Assert.That(ConformanceCatalog.TestCase("TC_SPINE_RTS_001")!.Applies(guardOnly, out var reason), Is.False,
                            "A device which is no controllable system is not asked the controllable system cases.");

                Assert.That(reason, Does.Contain("CS"));

                Assert.That(ConformanceCatalog.TestCase("TC_SPINE_COMP_001")!.Applies(guardOnly, out _), Is.True,
                            "Everybody is asked the protocol cases.");

            });

        }

        #endregion

        #region TheReportCarriesTheCatalogIdentifiers()

        /// <summary>
        /// A report which does not carry the official identifiers cannot be
        /// compared with anybody else's, which is the whole point of adopting
        /// them.
        /// </summary>
        [Test]
        public async Task TheReportCarriesTheCatalogIdentifiers()
        {

            var run       = await ConformanceRunner.Run(ParameterSheet.OurOwnStack, "TC_SHIP_CMI");
            var markdown  = ConformanceReport.ToMarkdown(run);
            var csv       = ConformanceReport.ToCSV(run);

            Assert.Multiple(() => {

                Assert.That(run.Outcomes,           Has.Count.EqualTo(6));
                Assert.That(markdown,               Does.Contain("TC_SHIP_CMI_003"));
                Assert.That(markdown,               Does.Contain("SHIP-TS-CMI-04"));
                Assert.That(csv,                    Does.Contain("TC_SHIP_CMI_003;SHIP;CMI"));

            });

        }

        #endregion

    }

}
