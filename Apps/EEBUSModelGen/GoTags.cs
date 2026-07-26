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

using System.Text;
using System.Text.RegularExpressions;

#endregion

namespace cloud.charging.open.protocols.EEBUS.ModelGen
{

    /// <summary>
    /// Reads the struct definitions of the Go reference implementation.
    ///
    /// spine-go is the second reading of the same specification, and it is the
    /// one which is proven in certification. Two things are taken from it:
    ///
    /// * which properties of a data type are its identifiers. That cannot be read
    ///   from the XSD - it is stated in the text of the specification, data type
    ///   by data type ("SHALL be set as PRIMARY IDENTIFIER") - and spine-go keeps
    ///   it in its struct tags, curated against the specification
    ///   (model/PRIMARYKEY_TAG_GUIDELINES.md).
    /// * which property of a data type says whether a remote peer may change it.
    ///   There are only three of them in all of SPINE ("isLimitChangeable",
    ///   "isValueChangeable", "isSetpointChangeable"), the XSD makes them look
    ///   like any other boolean, and the update system needs to know them.
    /// * a fixture of all JSON property names, checked in with the tests. That
    ///   turns "our model says the same as the reference implementation" into
    ///   something a test can assert without spine-go being present at all,
    ///   which matters because WWCP_EEBUS is also built on its own.
    /// </summary>
    public static partial class GoTags
    {

        #region (class) GoField / GoType

        /// <summary>
        /// One field of a Go data type.
        /// </summary>
        /// <param name="JSONName">The JSON property name.</param>
        /// <param name="IsKey">Whether it is an identifier of its data type.</param>
        /// <param name="IsPrimary">Whether it is the primary identifier.</param>
        /// <param name="IsWriteCheck">Whether it states that its data type may be written by a remote peer.</param>
        public sealed record GoField(String   JSONName,
                                     Boolean  IsKey,
                                     Boolean  IsPrimary,
                                     Boolean  IsWriteCheck);

        /// <summary>
        /// One data type of the Go reference implementation.
        /// </summary>
        /// <param name="Name">The name of the data type, which is the one of the XSD.</param>
        /// <param name="Fields">Its fields, in declaration order.</param>
        public sealed record GoType(String        Name,
                                    List<GoField> Fields);

        #endregion

        [GeneratedRegex(@"^type\s+(\w+)\s+struct\s*\{")]
        private static partial Regex StructStart();

        [GeneratedRegex("json:\"([^\",]+)")]
        private static partial Regex JSONTag();

        [GeneratedRegex("eebus:\"([^\"]*)\"")]
        private static partial Regex EEBUSTag();


        #region Read(GoModelDirectory)

        /// <summary>
        /// Read all data types declared by the Go model.
        /// </summary>
        /// <param name="GoModelDirectory">The directory holding the Go model, i.e. "libs/spine-go/model".</param>
        public static List<GoType> Read(String GoModelDirectory)
        {

            var types = new List<GoType>();

            if (!Directory.Exists(GoModelDirectory))
                return types;

            foreach (var file in Directory.GetFiles(GoModelDirectory, "*.go").Order(StringComparer.Ordinal))
            {

                if (file.EndsWith("_test.go", StringComparison.Ordinal))
                    continue;

                var current = (GoType?) null;

                foreach (var rawLine in File.ReadLines(file))
                {

                    var line  = rawLine.Trim();

                    var start = StructStart().Match(line);

                    if (start.Success)
                    {
                        current = new GoType(start.Groups[1].Value, []);
                        types.Add(current);
                        continue;
                    }

                    if (current is null)
                        continue;

                    if (line == "}")
                    {
                        current = null;
                        continue;
                    }

                    var json = JSONTag().Match(line);

                    if (!json.Success)
                        continue;

                    var eebus = EEBUSTag().Match(line);

                    var tags  = eebus.Success
                                    ? eebus.Groups[1].Value.Split(',', StringSplitOptions.TrimEntries)
                                    : [];

                    current.Fields.Add(
                        new GoField(
                            json.Groups[1].Value,
                            tags.Contains("key",        StringComparer.Ordinal),
                            tags.Contains("primarykey", StringComparer.Ordinal),
                            tags.Contains("writecheck", StringComparer.Ordinal)
                        )
                    );

                }

            }

            return types;

        }

        #endregion

        #region WriteFixture(Types, Path, SpineVersion)

        /// <summary>
        /// Write the fixture the generator tests compare the model against.
        ///
        /// Written by hand rather than with a JSON library, so that the file is
        /// stable line by line: it is checked in, and a diff of it has to be
        /// readable.
        /// </summary>
        /// <param name="Types">The data types read from the Go model.</param>
        /// <param name="Path">Where to write the fixture.</param>
        /// <param name="SpineVersion">The version of the specification.</param>
        public static void WriteFixture(List<GoType>  Types,
                                        String        Path,
                                        String        SpineVersion)
        {

            var builder = new StringBuilder();

            builder.AppendLine("{");
            builder.AppendLine("");
            builder.AppendLine("  \"_comment\":       [");
            builder.AppendLine("                        \"The data types of the Go reference implementation (enbility/spine-go), read from its\",");
            builder.AppendLine("                        \"struct tags by Apps/EEBUSModelGen and checked in as the oracle of the model tests.\",");
            builder.AppendLine("                        \"It is not hand-written: regenerate it with 'eebus-modelgen' after updating spine-go.\"");
            builder.AppendLine("                     ],");
            builder.AppendLine("  \"source\":         \"libs/spine-go/model\",");
            builder.AppendLine($"  \"spineVersion\":   \"{SpineVersion}\",");
            builder.AppendLine("  \"types\":          {");

            var types = Types.Where  (type => type.Fields.Count > 0).
                              OrderBy(type => type.Name, StringComparer.Ordinal).
                              ToList();

            for (var i = 0; i < types.Count; i++)
            {

                var type = types[i];

                builder.AppendLine($"    \"{type.Name}\": {{");
                builder.AppendLine($"      \"fields\": [ {String.Join(", ", type.Fields.Select(field => $"\"{field.JSONName}\""))} ]");

                var keys       = type.Fields.Where(field => field.IsKey).       ToList();
                var writeCheck = type.Fields.Where(field => field.IsWriteCheck).ToList();

                if (keys.Count > 0)
                {
                    builder.Length -= Environment.NewLine.Length;
                    builder.AppendLine(",");
                    builder.AppendLine($"      \"keys\":   {{ {String.Join(", ", keys.Select(key => $"\"{key.JSONName}\": \"{(key.IsPrimary ? "primary" : "key")}\""))} }}");
                }

                if (writeCheck.Count > 0)
                {
                    builder.Length -= Environment.NewLine.Length;
                    builder.AppendLine(",");
                    builder.AppendLine($"      \"writeCheck\": [ {String.Join(", ", writeCheck.Select(field => $"\"{field.JSONName}\""))} ]");
                }

                builder.AppendLine($"    }}{(i < types.Count - 1 ? "," : "")}");

            }

            builder.AppendLine("  }");
            builder.AppendLine("");
            builder.AppendLine("}");

            File.WriteAllText(Path, builder.ToString().ReplaceLineEndings("\r\n"));

        }

        #endregion

    }

}
