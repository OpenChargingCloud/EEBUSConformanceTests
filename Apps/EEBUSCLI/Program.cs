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

using SHIP  = cloud.charging.open.protocols.EEBUS.SHIP;
using SPINE = cloud.charging.open.protocols.EEBUS.SPINE;

#endregion

namespace cloud.charging.open.protocols.EEBUS.CLI
{

    /// <summary>
    /// Command line runner for the EEBUS simulations and conformance test runs.
    ///
    /// The subcommands are implemented along with their work packages
    /// (simulations: WP10, conformance runs: WP11); for now the tool reports
    /// which protocol versions this build speaks.
    /// </summary>
    public static class Program
    {

        public static Int32 Main(String[] Arguments)
        {

            var command = Arguments.FirstOrDefault() ?? "version";

            switch (command)
            {

                case "version":
                case "--version":
                case "-v":
                    PrintVersion();
                    return 0;

                case "help":
                case "--help":
                case "-h":
                    PrintHelp();
                    return 0;

                default:
                    Console.Error.WriteLine($"Unknown command '{command}'.");
                    Console.Error.WriteLine();
                    PrintHelp();
                    return 1;

            }

        }


        #region (private) PrintVersion()

        private static void PrintVersion()
        {

            Console.WriteLine("EEBUS conformance test suite");
            Console.WriteLine();
            Console.WriteLine($"  SHIP   {SHIP. Version.String}  (protocolId '{SHIP.Version.ProtocolId}', handshake {SHIP.Version.Major}.{SHIP.Version.Minor})");
            Console.WriteLine($"  SPINE  {SPINE.Version.String}  ({SPINE.Version.XMLNamespace})");
            Console.WriteLine();

        }

        #endregion

        #region (private) PrintHelp()

        private static void PrintHelp()
        {

            Console.WriteLine("Usage: eebus <command>");
            Console.WriteLine();
            Console.WriteLine("Commands:");
            Console.WriteLine("  version    Show the implemented EEBUS protocol versions");
            Console.WriteLine("  help       Show this help");
            Console.WriteLine();
            Console.WriteLine("Planned:");
            Console.WriteLine("  sim <name>          Run an e-mobility simulation      (WP10)");
            Console.WriteLine("  conformance <target> Run the conformance test catalog (WP11)");
            Console.WriteLine();

        }

        #endregion

    }

}
