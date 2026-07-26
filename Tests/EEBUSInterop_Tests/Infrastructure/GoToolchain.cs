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

using System.Diagnostics;

#endregion

namespace cloud.charging.open.protocols.EEBUS.Interop.tests
{

    /// <summary>
    /// Interoperability tests run the Go reference implementations as external
    /// peers. This helper reports whether a Go toolchain is available at all.
    /// </summary>
    public static class GoToolchain
    {

        #region Data

        private static readonly Lazy<String?> version    = new(DetectVersion);
        private static readonly Lazy<String?> wslVersion = new(DetectWSLVersion);

        #endregion

        #region Properties

        /// <summary>
        /// The version reported by "go version", or null when Go is not installed.
        /// </summary>
        public static String?  Version
            => version.Value;

        /// <summary>
        /// Whether a Go toolchain is available.
        /// </summary>
        public static Boolean  IsAvailable
            => version.Value is not null;

        #endregion


        #region Require()

        /// <summary>
        /// Mark the current test as inconclusive when no Go toolchain is available.
        /// </summary>
        public static void Require()
        {

            if (IsAvailable)
                return;

            // A Go toolchain inside WSL cannot be used from a test run on Windows:
            // the Go peer would live in the WSL network namespace, which breaks the
            // mDNS discovery and the "Go connects to us" direction. Run the whole
            // suite within WSL instead - the way the CI does it.
            NUnit.Framework.Assert.Inconclusive(
                wslVersion.Value is not null
                    ? $"No Go toolchain on this PATH, but WSL provides one ({wslVersion.Value}). " +
                       "Run the interoperability tests inside WSL, so that both peers share one " +
                       "network stack: wsl -e bash -lc \"cd <repository> && dotnet test --filter TestCategory=Interop\""
                    : "No Go toolchain found. The interoperability tests run ship-go/spine-go/eebus-go " +
                      "as external peers and therefore need Go (>= 1.24) on the PATH."
            );

        }

        #endregion

        #region (private) DetectVersion()

        private static String? DetectVersion()
            => Run("go", "version");

        #endregion

        #region (private) DetectWSLVersion()

        private static String? DetectWSLVersion()

            => OperatingSystem.IsWindows()
                   ? Run("wsl.exe", "-e", "bash", "-lc", "go version")
                   : null;

        #endregion

        #region (private) Run(FileName, Arguments)

        private static String? Run(String FileName, params String[] Arguments)
        {
            try
            {

                var startInfo = new ProcessStartInfo(FileName) {
                                    RedirectStandardOutput  = true,
                                    RedirectStandardError   = true,
                                    UseShellExecute         = false,
                                    CreateNoWindow          = true
                                };

                foreach (var argument in Arguments)
                    startInfo.ArgumentList.Add(argument);

                using var process = Process.Start(startInfo);

                if (process is null)
                    return null;

                var output = process.StandardOutput.ReadToEnd();

                if (!process.WaitForExit(TimeSpan.FromSeconds(30)) || process.ExitCode != 0)
                    return null;

                // wsl.exe may answer in UTF-16, which arrives here as NUL bytes.
                output = output.Replace("\0", "").Trim();

                return output.StartsWith("go version", StringComparison.Ordinal)
                           ? output
                           : null;

            }
            catch (Exception)
            {
                return null;
            }
        }

        #endregion

    }

}
