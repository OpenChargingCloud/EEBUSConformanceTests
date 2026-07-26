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

#endregion

namespace cloud.charging.open.protocols.EEBUS.ModelGen
{

    /// <summary>
    /// The naming rules of the generated model.
    ///
    /// The type names of the specification are kept verbatim - "LoadControlEventDataType"
    /// stays "LoadControlEventDataType". Debugging a wire problem means reading the XSD,
    /// the specification PDF and the Go reference implementation next to our code, and
    /// every renaming makes that harder for no gain.
    /// </summary>
    public static class Naming
    {

        #region PascalCase(Name)

        /// <summary>
        /// Turn an XSD element name into a C# property name: the element names
        /// of SPINE are camel case already, so only the first letter changes.
        /// </summary>
        /// <param name="Name">An XSD element name.</param>
        public static String PascalCase(String Name)

            => Name.Length == 0
                   ? Name
                   : Char.ToUpperInvariant(Name[0]) + Name[1..];

        #endregion

        #region ResourceOf(SchemaFile)

        /// <summary>
        /// The resource name of an XSD file: "EEBus_SPINE_TS_LoadControl.xsd"
        /// becomes "LoadControl".
        /// </summary>
        /// <param name="SchemaFile">The path or URI of an XSD file.</param>
        public static String ResourceOf(String? SchemaFile)
        {

            if (SchemaFile is null || SchemaFile.Length == 0)
                return "Unknown";

            var name = Path.GetFileNameWithoutExtension(
                           SchemaFile.Replace('\\', '/')
                       );

            const String prefix = "EEBus_SPINE_TS_";

            if (name.StartsWith(prefix, StringComparison.Ordinal))
                name = name[prefix.Length..];

            if (name.EndsWith("_overview", StringComparison.Ordinal))
                name = name[..^"_overview".Length];

            return name.Length == 0
                       ? "Unknown"
                       : name;

        }

        #endregion

        #region Identifier(Value)

        /// <summary>
        /// Turn the value of an XSD enumeration into something that can be a C#
        /// identifier. Most values already are one; the units of measurement and
        /// a few others are not ("l/s", "m^3/h", "1").
        /// </summary>
        /// <param name="Value">The value of an XSD enumeration.</param>
        public static String Identifier(String Value)
        {

            var builder = new StringBuilder(Value.Length + 1);

            foreach (var character in Value)
                builder.Append(Char.IsLetterOrDigit(character) || character == '_'
                                   ? character
                                   : '_');

            var identifier = builder.ToString();

            if (identifier.Length == 0)
                return "_";

            // A C# identifier may not start with a digit.
            if (Char.IsDigit(identifier[0]))
                identifier = "_" + identifier;

            return identifier;

        }

        #endregion

        #region ValueNames(Values)

        /// <summary>
        /// The C# names of the values of one string type.
        ///
        /// The values are capitalised, because that is what a C# property looks
        /// like - unless capitalising two values would collide. The units of
        /// measurement contain both "s" (second) and "S" (siemens), and losing
        /// that difference would be losing the specification.
        /// </summary>
        /// <param name="Values">The values of an XSD enumeration.</param>
        public static Dictionary<String, String> ValueNames(IEnumerable<String> Values)
        {

            var values      = Values.ToList();
            var identifiers = values.ToDictionary(value => value,
                                                  Identifier);

            var capitalised = values.ToDictionary(value => value,
                                                  value => PascalCase(identifiers[value]));

            var collides    = capitalised.Values.
                                  GroupBy(name => name, StringComparer.Ordinal).
                                  Where  (group => group.Count() > 1).
                                  Select (group => group.Key).
                                  ToHashSet(StringComparer.Ordinal);

            var result      = new Dictionary<String, String>();
            var used        = new HashSet<String>(StringComparer.Ordinal);

            foreach (var value in values)
            {

                var name = collides.Contains(capitalised[value])
                               ? identifiers[value]
                               : capitalised[value];

                // Two different values may still sanitise to the same identifier
                // ("m^3" and "m/3" would); make that visible rather than silently
                // dropping one of them.
                var candidate = name;
                var counter   = 2;

                while (!used.Add(candidate))
                    candidate = $"{name}_{counter++}";

                result.Add(value, candidate);

            }

            return result;

        }

        #endregion

        #region XmlEscape(Text)

        /// <summary>
        /// Escape a text for use within an XML documentation comment.
        /// </summary>
        /// <param name="Text">A text.</param>
        public static String XmlEscape(String Text)

            => Text.Replace("&", "&amp;").
                    Replace("<", "&lt;").
                    Replace(">", "&gt;");

        #endregion

    }

}
