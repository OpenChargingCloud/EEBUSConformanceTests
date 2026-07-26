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

using cloud.charging.open.protocols.EEBus.Conformance.tests;

#endregion

namespace cloud.charging.open.protocols.EEBus.Interop.tests
{

    /// <summary>
    /// Verifies the preconditions of the interoperability test suite.
    /// The suite itself is built in WP12.
    /// </summary>
    [TestFixture]
    [Category("Interop")]
    public class GoToolchainTests
    {

        #region GoToolchain_IsAvailableOrInconclusive()

        [Test]
        public void GoToolchain_IsAvailableOrInconclusive()
        {

            GoToolchain.Require();

            TestContext.Out.WriteLine(GoToolchain.Version);

            Assert.That(GoToolchain.Version, Does.StartWith("go version"));

        }

        #endregion

        #region GoPeerSources_AreBuildable()

        /// <summary>
        /// Every Go peer we start is compiled from a submodule, so each of them
        /// has to provide its own module definition.
        /// </summary>
        [Test]
        public void GoPeerSources_AreBuildable()
        {

            GoToolchain.Require();

            Assert.Multiple(() => {
                Assert.That(File.Exists(Path.Combine(TestEnvironment.ShipGo. FullName, "go.mod")),  Is.True);
                Assert.That(File.Exists(Path.Combine(TestEnvironment.SpineGo.FullName, "go.mod")),  Is.True);
                Assert.That(File.Exists(Path.Combine(TestEnvironment.EEBusGo.FullName, "go.mod")),  Is.True);
            });

        }

        #endregion

    }

}
