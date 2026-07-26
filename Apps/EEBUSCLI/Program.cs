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

using System.Globalization;

using cloud.charging.open.protocols.EEBUS.Conformance;
using cloud.charging.open.protocols.EEBUS.Simulations;

using SHIP  = cloud.charging.open.protocols.EEBUS.SHIP;
using SPINE = cloud.charging.open.protocols.EEBUS.SPINE;

#endregion

namespace cloud.charging.open.protocols.EEBUS.CLI
{

    /// <summary>
    /// Command line runner for the EEBUS simulations and conformance test runs.
    /// </summary>
    public static class Program
    {

        public static async Task<Int32> Main(String[] Arguments)
        {

            var command = Arguments.FirstOrDefault() ?? "version";

            switch (command)
            {

                case "version":
                case "--version":
                case "-v":
                    PrintVersion();
                    return 0;

                case "sim":
                    return await RunSimulation(Arguments.Skip(1).ToArray());

                case "conform":
                    return await RunConformance(Arguments.Skip(1).ToArray());

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


        #region (private) RunSimulation(Arguments)

        private static async Task<Int32> RunSimulation(String[] Arguments)
        {

            var name = Arguments.FirstOrDefault();

            if (name is null || name is "list" or "--list")
            {
                PrintSimulations();
                return name is null ? 1 : 0;
            }

            var speed   = 0.0;
            var faults  = new List<String>();
            String? device  = null;
            String? output  = null;

            for (var index = 1; index < Arguments.Length; index++)
            {

                var value = index + 1 < Arguments.Length ? Arguments[index + 1] : null;

                switch (Arguments[index])
                {

                    case "--speed" when value is not null:
                        if (!Double.TryParse(value, CultureInfo.InvariantCulture, out speed))
                        {
                            Console.Error.WriteLine($"'{value}' is not a speed factor.");
                            return 1;
                        }
                        index++;
                        break;

                    case "--fault" when value is not null:
                        faults.Add(value);
                        index++;
                        break;

                    case "--device" when value is not null:
                        device = value;
                        index++;
                        break;

                    case "--out" when value is not null:
                        output = value;
                        index++;
                        break;

                    default:
                        Console.Error.WriteLine($"Unknown option '{Arguments[index]}'.");
                        return 1;

                }

            }

            var simulation = SimulationRegistry.Create(name,
                                                       new SimulationOptions(speed, faults, device));

            if (simulation is null)
            {
                Console.Error.WriteLine($"There is no simulation called '{name}'.");
                Console.Error.WriteLine();
                PrintSimulations();
                return 1;
            }

            // Ctrl+C stops the scenario rather than the process, so that
            // whatever has happened so far is still reported.
            using var stopping = new CancellationTokenSource();

            Console.CancelKeyPress += (_, eventArgs) => {
                eventArgs.Cancel = true;
                stopping.Cancel();
            };

            Console.WriteLine($"{simulation.Name}: {simulation.Description}");
            Console.WriteLine();

            var result = await simulation.Run(stopping.Token);

            Console.Write(result.Log.ToText());
            Console.WriteLine();
            Console.WriteLine($"{result.Duration:hh\\:mm\\:ss} simulated, " +
                              $"{result.Log.Events.Count} event(s), " +
                              $"{result.Log.Samples.Count} sample(s)");

            if (output is not null)
            {

                var folder = Path.GetDirectoryName(Path.GetFullPath(output));

                if (folder is not null)
                    Directory.CreateDirectory(folder);

                await File.WriteAllTextAsync($"{output}.csv", result.Log.ToCSV(), stopping.Token);
                await File.WriteAllTextAsync($"{output}.md",  result.Log.ToMarkdown(simulation.Description), stopping.Token);

                Console.WriteLine($"written to {output}.csv and {output}.md");

            }

            return 0;

        }

        #endregion

        #region (private) RunConformance(Arguments)

        private static async Task<Int32> RunConformance(String[] Arguments)
        {

            var verb = Arguments.FirstOrDefault() ?? "run";

            if (verb is "list" or "--list")
            {
                PrintCatalog();
                return 0;
            }

            String?  filter      = null;
            String?  output      = null;
            String?  sheet       = null;

            for (var index = verb == "run" ? 1 : 0; index < Arguments.Length; index++)
            {

                var value = index + 1 < Arguments.Length ? Arguments[index + 1] : null;

                switch (Arguments[index])
                {

                    case "--only" when value is not null:
                        filter = value;
                        index++;
                        break;

                    case "--sheet" when value is not null:
                        sheet = value;
                        index++;
                        break;

                    case "--out" when value is not null:
                        output = value;
                        index++;
                        break;

                    default:
                        Console.Error.WriteLine($"Unknown option '{Arguments[index]}'.");
                        return 1;

                }

            }

            var parameters = ParameterSheet.TryLoad(sheet is not null ? new FileInfo(sheet) : null);
            var complaints = parameters.Validate().ToList();

            if (complaints.Count > 0)
            {

                Console.Error.WriteLine("The parameter sheet is not usable:");

                foreach (var complaint in complaints)
                    Console.Error.WriteLine($"  {complaint}");

                return 1;

            }

            using var stopping = new CancellationTokenSource();

            Console.CancelKeyPress += (_, eventArgs) => {
                eventArgs.Cancel = true;
                stopping.Cancel();
            };

            var run = await ConformanceRunner.Run(parameters, filter, stopping.Token);

            Console.Write(ConformanceReport.ToText(run));

            if (output is not null)
            {

                var folder = Path.GetDirectoryName(Path.GetFullPath(output));

                if (folder is not null)
                    Directory.CreateDirectory(folder);

                await File.WriteAllTextAsync($"{output}.md",  ConformanceReport.ToMarkdown(run), stopping.Token);
                await File.WriteAllTextAsync($"{output}.csv", ConformanceReport.ToCSV(run),      stopping.Token);

                Console.WriteLine($"written to {output}.md and {output}.csv");

            }

            // A mandatory case which applied, did not pass, and which nobody has
            // decided about is the only thing which makes this command fail.
            // Everything else - a case which does not apply, a case nobody has
            // written yet, a failure this repository has already weighed up and
            // written down - is information rather than a verdict.
            foreach (var known in run.Blocking.Where(outcome => outcome.TestCase.KnownDeviation is not null))
                Console.WriteLine($"{known.TestCase.Id} fails knowingly: {known.TestCase.KnownDeviation}");

            return run.Unexpected.Any() ? 1 : 0;

        }

        #endregion

        #region (private) PrintVersion() / PrintSimulations() / PrintCatalog() / PrintHelp()

        private static void PrintVersion()
        {

            Console.WriteLine("EEBUS conformance test suite");
            Console.WriteLine();
            Console.WriteLine($"  SHIP   {SHIP. Version.String}  (protocolId '{SHIP.Version.ProtocolId}', handshake {SHIP.Version.Major}.{SHIP.Version.Minor})");
            Console.WriteLine($"  SPINE  {SPINE.Version.String}  ({SPINE.Version.XMLNamespace})");
            Console.WriteLine();

        }


        private static void PrintSimulations()
        {

            Console.WriteLine("Simulations:");
            Console.WriteLine();

            foreach (var simulation in SimulationRegistry.Describe())
            {

                Console.WriteLine($"  {simulation.Name,-16} {simulation.Description}");

                var faults = simulation.Faults.ToList();

                if (faults.Count > 0)
                    Console.WriteLine($"  {"",-16} --fault {String.Join(" | --fault ", faults)}");

            }

            Console.WriteLine();

        }


        private static void PrintCatalog()
        {

            Console.WriteLine("Conformance catalog:");
            Console.WriteLine();

            foreach (var group in ConformanceCatalog.TestCases.GroupBy(testCase => (testCase.Layer, testCase.Group)))
            {

                Console.WriteLine($"  {group.Key.Layer} {group.Key.Group}");

                foreach (var testCase in group)
                    Console.WriteLine($"    {(ConformanceSuite.TestFor(testCase.Id) is not null ? " " : "?")} " +
                                      $"{testCase.Id,-22} {testCase.Title}");

            }

            Console.WriteLine();
            Console.WriteLine($"  {ConformanceCatalog.TestCases.Count} test cases, " +
                              $"{ConformanceSuite.Implemented.Count()} of them executable " +
                              $"(a '?' marks one which is not).");
            Console.WriteLine($"  {ConformanceCatalog.Requirements.Count} requirements.");
            Console.WriteLine();

        }


        private static void PrintHelp()
        {

            Console.WriteLine("Usage: eebus <command>");
            Console.WriteLine();
            Console.WriteLine("Commands:");
            Console.WriteLine("  version                Show the implemented EEBUS protocol versions");
            Console.WriteLine("  sim list               List the simulations");
            Console.WriteLine("  sim <name> [options]   Run one");
            Console.WriteLine("  conform list           List the conformance test catalog");
            Console.WriteLine("  conform [options]      Run the catalog against a device");
            Console.WriteLine("  help                   Show this help");
            Console.WriteLine();
            Console.WriteLine("Options of 'sim':");
            Console.WriteLine("  --speed <factor>   Simulated seconds per real second; omitted runs as fast as possible");
            Console.WriteLine("  --fault <name>     Make something go wrong; may be given more than once");
            Console.WriteLine("  --device <path>    Which recorded device to replay, e.g. porsche/mobile-charger-connect");
            Console.WriteLine("  --out <path>       Write <path>.csv and <path>.md");
            Console.WriteLine();
            Console.WriteLine("Options of 'conform':");
            Console.WriteLine("  --only <text>      Run only the test cases whose identifier contains this, e.g. TC_SHIP_CMI");
            Console.WriteLine("  --sheet <path>     The parameter sheet of the device (JSON); omitted runs the self test");
            Console.WriteLine("  --out <path>       Write <path>.md and <path>.csv");
            Console.WriteLine();

        }

        #endregion

    }

}
