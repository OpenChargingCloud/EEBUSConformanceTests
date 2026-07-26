/*
 * Copyright (c) 2014-2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of EEBusConformanceTests <https://github.com/OpenChargingCloud/EEBusConformanceTests>
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

using NUnit.Framework;

#endregion

namespace cloud.charging.open.protocols.EEBus.Conformance.tests
{

    /// <summary>
    /// Verifies that a test run finds its reference material.
    /// </summary>
    [TestFixture]
    public class TestEnvironmentTests
    {

        #region RepositoryRoot_IsFound()

        [Test]
        public void RepositoryRoot_IsFound()
        {

            var root = TestEnvironment.RepositoryRoot;

            Assert.Multiple(() => {
                Assert.That(root.Exists,                                                    Is.True);
                Assert.That(File.Exists(Path.Combine(root.FullName, "WORKPLAN.md")),     Is.True);
            });

        }

        #endregion

        #region GoReferenceImplementations_AreCheckedOut()

        [Test]
        public void GoReferenceImplementations_AreCheckedOut()
        {

            Assert.Multiple(() => {
                Assert.That(TestEnvironment.RequireSubmodule(TestEnvironment.ShipGo). Exists,  Is.True);
                Assert.That(TestEnvironment.RequireSubmodule(TestEnvironment.SpineGo).Exists,  Is.True);
                Assert.That(TestEnvironment.RequireSubmodule(TestEnvironment.EEBusGo).Exists,  Is.True);
            });

        }

        #endregion

        #region SpineGoTestData_ProvidesGoldenDatagrams()

        /// <summary>
        /// The golden SPINE datagrams of the Go reference implementation are the
        /// fixtures of our serialisation tests (WP06/WP07).
        /// </summary>
        [Test]
        public void SpineGoTestData_ProvidesGoldenDatagrams()
        {

            TestEnvironment.RequireSubmodule(TestEnvironment.SpineGo);

            var testData = TestEnvironment.SpineGoTestData;

            Assert.That(testData.Exists,                        Is.True, testData.FullName);
            Assert.That(testData.GetFiles("*.json").Length,     Is.GreaterThan(0));

        }

        #endregion

        #region Specifications_AreAvailableOrInconclusive()

        /// <summary>
        /// The specifications are licensed material and not part of the repository,
        /// so this test is inconclusive instead of failing when they are missing.
        /// </summary>
        [Test]
        public void Specifications_AreAvailableOrInconclusive()
        {

            var spine = TestEnvironment.RequireSpecifications(
                            "SHIP SPINE",
                            "Technical Specifications"
                        );

            Assert.That(spine.Exists, Is.True);

        }

        #endregion

        #region RealDeviceData_IsAvailable()

        [Test]
        public void RealDeviceData_IsAvailable()
        {

            var devices = TestEnvironment.RequireSubmodule(TestEnvironment.RealDevices);

            Assert.That(devices.GetFiles("discovery-data.json", SearchOption.AllDirectories).Length,
                        Is.GreaterThan(0),
                        "No recorded device discovery data found.");

        }

        #endregion

    }

}
