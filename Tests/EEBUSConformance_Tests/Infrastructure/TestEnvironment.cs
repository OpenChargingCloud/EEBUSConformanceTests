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

namespace cloud.charging.open.protocols.EEBUS.Conformance.tests
{

    /// <summary>
    /// Locates the reference material a conformance test run may want to use:
    /// the EEBUS specifications, the recorded responses of real devices and the
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
        /// The root directory of the EEBUSConformanceTests repository.
        /// </summary>
        public static DirectoryInfo  RepositoryRoot
            => repositoryRoot.Value;

        /// <summary>
        /// The EEBUS specifications (SHIP, SPINE, use cases, test specifications).
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
        /// The EEBUS use case implementation in Go (git submodule "enbility/eebus-go").
        /// </summary>
        public static DirectoryInfo  EEBUSGo
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
                    $"The EEBUS specifications are not available at '{directory.FullName}'. " +
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

            if (!Submodule.Exists || !IsCheckedOut(Submodule))
                NUnit.Framework.Assert.Inconclusive(
                    $"The git submodule '{Submodule.FullName}' is not available. " +
                     "Run: git submodule update --init --recursive"
                );

            return Submodule;

        }

        /// <summary>
        /// Whether a submodule directory holds anything besides its own ".git".
        /// </summary>
        /// <remarks>
        /// The obvious test - "the directory is empty" - is not enough, because a
        /// submodule which git registered but could not populate is not empty: it
        /// holds the ".git" file pointing at .git/modules, and nothing else.
        ///
        /// That is not a hypothetical. "git submodule update --init libs/devices"
        /// on Windows creates exactly that and then stops, because the recordings
        /// contain "vaillant/arotherm-vwl-75:6a" and NTFS reserves the colon for
        /// alternate data streams - the same reason ci.yml checks that submodule
        /// out on Linux only. Counting ".git" as content made RealDeviceData_IsAvailable
        /// a permanent failure on every Windows working copy, while the Windows CI
        /// leg reported it inconclusive merely because it never creates the
        /// directory at all. Two answers to one question, and the harsher one was
        /// wrong.
        /// </remarks>
        private static Boolean IsCheckedOut(DirectoryInfo Submodule)

            => Submodule.EnumerateFileSystemInfos().
                         Any(entry => !entry.Name.Equals(".git", StringComparison.Ordinal));

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
