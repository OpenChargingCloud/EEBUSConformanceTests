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

using cloud.charging.open.protocols.EEBUS.ModelGen;

#endregion

#region Command line

var xsdDirectory   = (String?) null;
var outDirectory   = (String?) null;
var goDirectory    = (String?) null;
var fixtureFile    = (String?) null;
var listOnly       = false;

for (var i = 0; i < args.Length; i++)
{
    switch (args[i])
    {

        case "--xsd":
            xsdDirectory = i + 1 < args.Length ? args[++i] : null;
            break;

        case "--out":
            outDirectory = i + 1 < args.Length ? args[++i] : null;
            break;

        case "--go-model":
            goDirectory  = i + 1 < args.Length ? args[++i] : null;
            break;

        case "--fixture":
            fixtureFile  = i + 1 < args.Length ? args[++i] : null;
            break;

        case "--list":
            listOnly     = true;
            break;

        case "--help":
        case "-h":
            Console.WriteLine("""
                eebus-modelgen - generates the SPINE data model from the official XSDs

                  --xsd       <dir>  the directory holding the SPINE XSDs
                                     (default: docs/specs/.../EEBus_SPINE_V1.3.0_Final_hp/XSDs)
                  --out       <dir>  where the C# files are written
                                     (default: libs/WWCP_EEBUS/WWCP_EEBUS_SPINE/Model)
                  --go-model  <dir>  the Go model, read for the identifiers of the data types
                                     (default: libs/spine-go/model)
                  --fixture   <file> where the Go model fixture of the tests is written
                                     (default: WWCP_EEBUS_SPINE_Tests/TestData/spine-go-model.json)
                  --list             read the XSDs and report what was found, write nothing

                The generated code is checked in. The generator runs on demand, not
                as part of the build: the XSDs are licensed material and are not
                part of this repository.
                """);
            return 0;

        default:
            Console.Error.WriteLine($"Unknown argument: {args[i]}");
            return 2;

    }
}

#endregion

#region The repository root and the default directories

var root = new DirectoryInfo(AppContext.BaseDirectory);

while (root is not null && !File.Exists(Path.Combine(root.FullName, "WORKPLAN.md")))
    root = root.Parent;

if (root is null)
{
    Console.Error.WriteLine("The repository root could not be found (no WORKPLAN.md in any parent directory).");
    return 2;
}

xsdDirectory ??= Path.Combine(root.FullName,
                              "docs", "specs", "SHIP SPINE", "Technical Specifications",
                              "EEBus_SPINE_V1.3.0", "EEBus_SPINE_V1.3.0_Final_hp", "XSDs");

outDirectory ??= Path.Combine(root.FullName, "libs", "WWCP_EEBUS", "WWCP_EEBUS_SPINE", "Model");
goDirectory  ??= Path.Combine(root.FullName, "libs", "spine-go", "model");

fixtureFile  ??= Path.Combine(root.FullName, "libs", "WWCP_EEBUS", "WWCP_EEBUS_SPINE_Tests",
                              "TestData", "spine-go-model.json");

if (!Directory.Exists(xsdDirectory))
{
    Console.Error.WriteLine($"""
        The SPINE XSDs were not found at
            {xsdDirectory}

        They are part of the EEBUS specifications, which are licensed material and
        therefore not part of this repository. Put them below docs/specs/ (which is
        ignored by git) or point --xsd somewhere else.
        """);
    return 3;
}

#endregion

#region Read the XSDs

Console.WriteLine($"Reading the SPINE XSDs from {xsdDirectory} ...");

var reader = new SchemaReader(xsdDirectory);
var model  = reader.Read();

#endregion

#region The identifiers of the data types, from the Go reference implementation

var goTypes       = GoTags.Read(goDirectory);
var unmatchedKeys = new List<String>();

if (goTypes.Count > 0)
{

    var keysByType = goTypes.
                         Where(type => type.Fields.Any(field => field.IsKey)).
                         ToDictionary(type => type.Name,
                                      type => type.Fields.
                                                  Where(field => field.IsKey).
                                                  ToDictionary(field => field.JSONName,
                                                               field => field.IsPrimary,
                                                               StringComparer.Ordinal),
                                      StringComparer.Ordinal);

    var matched = new HashSet<String>(StringComparer.Ordinal);

    foreach (var complexType in model.Classes)
    {

        if (!keysByType.TryGetValue(complexType.Name, out var keys))
            continue;

        for (var i = 0; i < complexType.Properties.Count; i++)
        {

            var property = complexType.Properties[i];

            if (!keys.TryGetValue(property.XmlName, out var isPrimary))
                continue;

            complexType.Properties[i] = property with {
                                            IsKey         = true,
                                            IsPrimaryKey  = isPrimary
                                        };

            matched.Add($"{complexType.Name}.{property.XmlName}");

        }

    }

    unmatchedKeys.AddRange(
        keysByType.SelectMany(type => type.Value.Keys.Select(key => $"{type.Key}.{key}")).
                   Where     (key  => !matched.Contains(key))
    );

}

else
    Console.WriteLine($"The Go model was not found at {goDirectory}; the identifiers of the data types are missing.");

#endregion

#region Report

Console.WriteLine();
Console.WriteLine($"SPINE {model.Version}");
Console.WriteLine($"  {model.Classes.Count,4} complex types in {model.Classes.Select(c => c.Resource).Distinct().Count()} resources");
Console.WriteLine($"  {model.StringTypes.Count,4} string types ({model.StringTypes.Count(s => s.IsExtensible)} of them extensible)");
Console.WriteLine($"  {model.Functions.Count,4} functions");
Console.WriteLine($"  {model.Classes.Sum(c => c.Properties.Count),4} properties, {model.Classes.Sum(c => c.Properties.Count(p => p.IsKey))} of them identifiers");

foreach (var warning in reader.Warnings)
    Console.WriteLine($"  warning: {warning}");

foreach (var key in unmatchedKeys)
    Console.WriteLine($"  warning: the Go model marks '{key}' as an identifier, but the XSDs do not know it.");

#endregion

#region Write

if (listOnly)
    return 0;

Console.WriteLine();
Console.WriteLine($"Writing to {outDirectory} ...");

var emitter = new Emitter(model, outDirectory);
emitter.Emit();

Console.WriteLine($"  {emitter.WrittenFiles.Count()} files written.");

if (goTypes.Count > 0)
{

    Directory.CreateDirectory(Path.GetDirectoryName(fixtureFile)!);

    GoTags.WriteFixture(goTypes, fixtureFile, model.Version);

    Console.WriteLine($"  the Go model fixture was written to {fixtureFile}.");

}

return 0;

#endregion
