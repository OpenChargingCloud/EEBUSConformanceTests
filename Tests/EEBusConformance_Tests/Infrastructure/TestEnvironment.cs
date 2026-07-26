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

namespace cloud.charging.open.protocols.EEBus.Conformance.tests
{

    /// <summary>
    /// Locates the reference material a conformance test run may want to use:
    /// the EEBus specifications, the recorded responses of real devices and the
    /// Go reference implementations.
    ///
    /// Only the git submodules are guaranteed to be present. The specifications
    /// are licensed material and therefore not part of the repository, so tests
    /// depending on them must degrade gracefully (see <see cref="RequireSpecifications"/>).
    /// </summary>
    public static class TestEnvironment
    {

        #region Data

        private static readonly Lazy<DirectoryInfo> repositoryRoot = new(FindRepositoryRoot);

        #endregion

        #region Properties

        /// <summary>
        /// The root directory of the EEBusConformanceTests repository.
        /// </summary>
        public static DirectoryInfo  RepositoryRoot
            => repositoryRoot.Value;

        /// <summary>
        /// The EEBus specifications (SHIP, SPINE, use cases, test specifications).
        /// Not part of the repository: licensed material, see WORKPLAN.md chapter 1.2.
        /// </summary>
        public static DirectoryInfo  Specifications
            => Subdirectory("docs", "specs");

        /// <summary>
        /// Recorded "NodeManagementDetailedDiscoveryData"/"NodeManagementUseCaseData"
        /// responses of real devices (git submodule "enbility/devices").
        /// </summary>
        public static DirectoryInfo  RealDevices
            => Subdirectory("libs", "devices");

        /// <summary>
        /// The SHIP reference implementation in Go (git submodule "enbility/ship-go").
        /// </summary>
        public static DirectoryInfo  ShipGo
            => Subdirectory("libs", "ship-go");

        /// <summary>
        /// The SPINE reference implementation in Go (git submodule "enbility/spine-go").
        /// </summary>
        public static DirectoryInfo  SpineGo
            => Subdirectory("libs", "spine-go");

        /// <summary>
        /// The EEBus use case implementation in Go (git submodule "enbility/eebus-go").
        /// </summary>
        public static DirectoryInfo  EEBusGo
            => Subdirectory("libs", "eebus-go");

        /// <summary>
        /// Golden SPINE datagrams of the Go reference implementation, used as
        /// serialisation fixtures.
        /// </summary>
        public static DirectoryInfo  SpineGoTestData
            => Subdirectory("libs", "spine-go", "spine", "testdata");

        #endregion


        #region RequireSpecifications(Path = null)

        /// <summary>
        /// Return the given path within the specifications directory, or mark the
        /// current test as inconclusive when the specifications are not available
        /// (e.g. on a continuous integration machine).
        /// </summary>
        /// <param name="Path">An optional path relative to the specifications directory.</param>
        public static DirectoryInfo RequireSpecifications(params String[] Path)
        {

            var directory = Path.Length == 0
                                ? Specifications
                                : Subdirectory(["docs", "specs", .. Path]);

            if (!directory.Exists)
                NUnit.Framework.Assert.Inconclusive(
                    $"The EEBus specifications are not available at '{directory.FullName}'. " +
                     "They are licensed material and therefore not part of this repository, " +
                     "see WORKPLAN.md chapter 1.2."
                );

            return directory;

        }

        #endregion

        #region RequireSubmodule(Submodule)

        /// <summary>
        /// Return the given submodule directory, or mark the current test as
        /// inconclusive when the submodule has not been checked out.
        /// </summary>
        /// <param name="Submodule">A submodule directory below "libs".</param>
        public static DirectoryInfo RequireSubmodule(DirectoryInfo Submodule)
        {

            if (!Submodule.Exists || Submodule.GetFileSystemInfos().Length == 0)
                NUnit.Framework.Assert.Inconclusive(
                    $"The git submodule '{Submodule.FullName}' is not available. " +
                     "Run: git submodule update --init --recursive"
                );

            return Submodule;

        }

        #endregion


        #region (private) FindRepositoryRoot()

        private static DirectoryInfo FindRepositoryRoot()
        {

            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory is not null)
            {

                if (File.Exists(Path.Combine(directory.FullName, ".gitmodules")) &&
                    Directory.Exists(Path.Combine(directory.FullName, "libs")))
                {
                    return directory;
                }

                directory = directory.Parent;

            }

            throw new DirectoryNotFoundException(
                      $"Could not locate the repository root above '{AppContext.BaseDirectory}'!"
                  );

        }

        #endregion

        #region (private) Subdirectory(PathElements)

        private static DirectoryInfo Subdirectory(params String[] PathElements)

            => new (Path.Combine([RepositoryRoot.FullName, .. PathElements]));

        #endregion

    }

}
