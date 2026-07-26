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

using System.Xml;
using System.Xml.Schema;

#endregion

namespace cloud.charging.open.protocols.EEBUS.ModelGen
{

    /// <summary>
    /// Reads the SPINE XSDs and normalises them into a <see cref="SpineModel"/>.
    ///
    /// The XSDs are a faithful description of the SPINE data model, but they are
    /// an XML description of a data model which is transmitted as JSON, so a few
    /// things have to be decided here rather than read:
    ///
    /// * Complex types are flattened. Where the XSD derives "EntityAddressType"
    ///   from "DeviceAddressType", the generated class carries both the inherited
    ///   and the own properties, in that order. The wire format has no notion of
    ///   a base type, and the order is what matters: EEBUS JSON is an ordered
    ///   format. The Go reference implementation flattens as well.
    /// * The named simple types which are plain numbers or plain strings become
    ///   the C# primitive rather than a type of their own. Only the types which
    ///   carry the values of the specification become types.
    /// * A complex type with simple content ("SpecificationVersionDataType" is a
    ///   "SpecificationVersionType") is the type it extends. On the wire it is a
    ///   string, not an object.
    /// </summary>
    public sealed class SchemaReader
    {

        #region Data

        /// <summary>
        /// The target namespace of the SPINE XSDs.
        /// </summary>
        public const String SpineNamespace = "http://docs.eebus.org/spine/xsd/v1";

        private const String XsdNamespace  = "http://www.w3.org/2001/XMLSchema";

        /// <summary>
        /// The XSD type which turns an enumeration into an extensible one
        /// (SPINE 1.3.0, CommonDataTypes).
        /// </summary>
        private const String EnumExtendType = "EnumExtendType";

        /// <summary>
        /// Types which are not generated, because their C# counterpart is written
        /// by hand: the XSD says "a duration or a timestamp" and nothing about
        /// what that means for a data model which has to reproduce a datagram.
        /// </summary>
        private static readonly HashSet<String> handWritten = new (StringComparer.Ordinal) {
            "AbsoluteOrRelativeTimeType"
        };

        /// <summary>
        /// The XSD built-in types and what they are in C#.
        /// The date and time types become the hand-written types which keep the
        /// received text, see <see cref="handWritten"/>.
        /// </summary>
        private static readonly Dictionary<String, (String Type, Boolean IsValueType)> builtins = new (StringComparer.Ordinal) {
            [ "boolean"       ] = ( "Boolean",      true  ),
            [ "string"        ] = ( "String",       false ),
            [ "normalizedString" ] = ( "String",    false ),
            [ "token"         ] = ( "String",       false ),
            [ "anyURI"        ] = ( "String",       false ),
            [ "hexBinary"     ] = ( "String",       false ),
            [ "base64Binary"  ] = ( "String",       false ),
            [ "byte"          ] = ( "SByte",        true  ),
            [ "unsignedByte"  ] = ( "Byte",         true  ),
            [ "short"         ] = ( "Int16",        true  ),
            [ "unsignedShort" ] = ( "UInt16",       true  ),
            [ "int"           ] = ( "Int32",        true  ),
            [ "unsignedInt"   ] = ( "UInt32",       true  ),
            [ "long"          ] = ( "Int64",        true  ),
            [ "unsignedLong"  ] = ( "UInt64",       true  ),
            [ "integer"       ] = ( "Int64",        true  ),
            [ "decimal"       ] = ( "Decimal",      true  ),
            [ "float"         ] = ( "Single",       true  ),
            [ "double"        ] = ( "Double",       true  ),
            [ "dateTime"      ] = ( "DateTimeType", true  ),
            [ "time"          ] = ( "TimeType",     true  ),
            [ "duration"      ] = ( "DurationType", true  )
        };

        private readonly XmlSchemaSet                                       schemaSet;

        /// <summary>XSD type name -&gt; the C# type it is an alias of.</summary>
        private readonly Dictionary<String, (String Type, Boolean IsValueType)>  aliases      = new (StringComparer.Ordinal);

        /// <summary>XSD type name -&gt; the string type generated for it.</summary>
        private readonly Dictionary<String, SpineStringType>                stringTypes  = new (StringComparer.Ordinal);

        /// <summary>The XSD complex types which become classes.</summary>
        private readonly HashSet<String>                                    classNames   = new (StringComparer.Ordinal);

        /// <summary>The problems found while reading, reported at the end.</summary>
        private readonly List<String>                                       warnings     = [];

        /// <summary>The name of a selectors element -&gt; the function it belongs to.</summary>
        private readonly Dictionary<String, String>                         selectorsElementOfFunction = new (StringComparer.Ordinal);

        /// <summary>The name of an elements element -&gt; the function it belongs to.</summary>
        private readonly Dictionary<String, String>                         elementsElementOfFunction  = new (StringComparer.Ordinal);

        #endregion

        #region Properties

        /// <summary>
        /// The problems found while reading the XSDs.
        /// </summary>
        public IEnumerable<String> Warnings
            => warnings;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new reader for the XSDs within the given directory.
        /// </summary>
        /// <param name="XSDDirectory">The directory holding the SPINE XSDs.</param>
        public SchemaReader(String XSDDirectory)
        {

            schemaSet = new XmlSchemaSet {
                            XmlResolver = new XmlUrlResolver()
                        };

            // The XSDs include each other, and XmlSchemaSet resolves every
            // include to the file it already knows, so adding all of them
            // is not the same as adding them several times.
            foreach (var file in Directory.GetFiles(XSDDirectory, "*.xsd").Order(StringComparer.Ordinal))
                schemaSet.Add(SpineNamespace, file);

            schemaSet.Compile();

        }

        #endregion


        #region Read()

        /// <summary>
        /// Read the XSDs and return the normalised model.
        /// </summary>
        public SpineModel Read()
        {

            var version = schemaSet.Schemas().
                              OfType<XmlSchema>().
                              Select(schema => schema.Version).
                              FirstOrDefault(v => !String.IsNullOrEmpty(v)) ?? "unknown";

            // 1) Classify every global type, so that a property can be resolved
            //    no matter in which order the types were declared.
            ClassifyTypes();

            // 2) The function registry, read from the three choice groups of the
            //    command frame. It is also needed while building the properties
            //    of "CmdType" and "FilterType".
            var functions = ReadFunctions();

            var functionNames = functions.ToDictionary(function => function.Name,
                                                       StringComparer.Ordinal);

            // 3) The classes.
            var classes = new List<SpineClass>();

            foreach (var name in classNames.Order(StringComparer.Ordinal))
            {

                var complexType = GlobalType(name) as XmlSchemaComplexType;

                if (complexType is null)
                    continue;

                classes.Add(
                    new SpineClass(
                        name,
                        Naming.ResourceOf(complexType.SourceUri),
                        Properties(name, complexType, functionNames)
                    )
                );

            }

            return new SpineModel(
                       version,
                       classes,
                       [.. stringTypes.Values.OrderBy(stringType => stringType.Name, StringComparer.Ordinal)],
                       functions
                   );

        }

        #endregion

        #region (private) ClassifyTypes()

        /// <summary>
        /// Decide for every global type of the SPINE namespace whether it becomes
        /// a class, a string type or is only an alias of something else.
        /// </summary>
        private void ClassifyTypes()
        {

            var simpleTypes = new List<XmlSchemaSimpleType>();

            foreach (var entry in schemaSet.GlobalTypes.Values)
            {

                if (entry is not XmlSchemaType type ||
                    type.QualifiedName.Namespace != SpineNamespace)
                {
                    continue;
                }

                var name = type.QualifiedName.Name;

                if (handWritten.Contains(name))
                {
                    aliases[name] = (name, true);
                    continue;
                }

                if (type is XmlSchemaSimpleType simpleType)
                    simpleTypes.Add(simpleType);

                else if (type is XmlSchemaComplexType complexType)
                {

                    // A complex type with simple content is a string (or a number)
                    // on the wire, not an object.
                    if (complexType.ContentModel is XmlSchemaSimpleContent { Content: XmlSchemaSimpleContentExtension extension })
                        aliases[name] = ResolveLater(extension.BaseTypeName);

                    else
                        classNames.Add(name);

                }

            }

            // The simple types may build on each other, so they are classified
            // until nothing changes any more instead of in one pass.
            var pending = simpleTypes;

            while (pending.Count > 0)
            {

                var unresolved = new List<XmlSchemaSimpleType>();

                foreach (var simpleType in pending)
                    if (!ClassifySimpleType(simpleType))
                        unresolved.Add(simpleType);

                if (unresolved.Count == pending.Count)
                {

                    foreach (var simpleType in unresolved)
                        warnings.Add($"The simple type '{simpleType.QualifiedName.Name}' could not be resolved and was mapped to String.");

                    foreach (var simpleType in unresolved)
                        aliases[simpleType.QualifiedName.Name] = ("String", false);

                    break;

                }

                pending = unresolved;

            }

            // An extensible enumeration is the union of its "...EnumType" and
            // "EnumExtendType". The "...EnumType" itself never appears on the
            // wire, so it becomes an alias of the union.
            foreach (var stringType in stringTypes.Values.ToList())
            {

                var enumName = stringType.Name.EndsWith("Type", StringComparison.Ordinal)
                                   ? stringType.Name[..^"Type".Length] + "EnumType"
                                   : null;

                if (stringType.IsExtensible &&
                    enumName is not null    &&
                    stringTypes.ContainsKey(enumName))
                {
                    stringTypes.Remove(enumName);
                    aliases[enumName] = (stringType.Name, true);
                }

            }

        }

        #endregion

        #region (private) ClassifySimpleType(SimpleType)

        /// <summary>
        /// Classify one simple type. Returns false when it builds on another
        /// SPINE type which has not been classified yet.
        /// </summary>
        /// <param name="SimpleType">A global simple type.</param>
        private Boolean ClassifySimpleType(XmlSchemaSimpleType SimpleType)
        {

            var name = SimpleType.QualifiedName.Name;

            if (aliases.ContainsKey(name) || stringTypes.ContainsKey(name))
                return true;

            switch (SimpleType.Content)
            {

                #region A union: an extensible enumeration, or a union of enumerations

                case XmlSchemaSimpleTypeUnion union:
                {

                    var members      = union.MemberTypes ?? [];
                    var isExtensible = members.Any(member => member.Name == EnumExtendType);
                    var others       = members.Where(member => member.Name != EnumExtendType).ToList();

                    var values       = new List<String>();

                    foreach (var member in others)
                    {

                        if (member.Namespace != SpineNamespace)
                            // A union of built-in types, e.g. "xs:duration xs:dateTime".
                            // Those are hand-written and never get here.
                            return Alias(name, ("String", false));

                        if (!TryCollectValues(member.Name, values, out var known))
                            return false;

                        if (!known)
                            return false;

                    }

                    if (values.Count > 0)
                    {
                        stringTypes.Add(
                            name,
                            new SpineStringType(
                                name,
                                Naming.ResourceOf(SimpleType.SourceUri),
                                values,
                                isExtensible
                            )
                        );
                        return true;
                    }

                    return Alias(name, ("String", false));

                }

                #endregion

                #region A restriction: values of the specification, or a plain primitive

                case XmlSchemaSimpleTypeRestriction restriction:
                {

                    var baseName = restriction.BaseTypeName;

                    var values   = restriction.Facets.
                                       OfType<XmlSchemaEnumerationFacet>().
                                       Select(facet => facet.Value ?? "").
                                       ToList();

                    if (baseName.Namespace == XsdNamespace)
                    {

                        if (values.Count > 0)
                        {
                            stringTypes.Add(
                                name,
                                new SpineStringType(
                                    name,
                                    Naming.ResourceOf(SimpleType.SourceUri),
                                    values,
                                    false
                                )
                            );
                            return true;
                        }

                        return Alias(name,
                                     builtins.TryGetValue(baseName.Name, out var builtin)
                                         ? builtin
                                         : ("String", false));

                    }

                    if (baseName.Namespace == SpineNamespace)
                    {

                        // A restriction of a SPINE type without own values is
                        // that type; with own values it is an enumeration of
                        // its own.
                        if (values.Count > 0)
                        {
                            stringTypes.Add(
                                name,
                                new SpineStringType(
                                    name,
                                    Naming.ResourceOf(SimpleType.SourceUri),
                                    values,
                                    false
                                )
                            );
                            return true;
                        }

                        if (aliases.TryGetValue(baseName.Name, out var baseAlias))
                            return Alias(name, baseAlias);

                        if (stringTypes.TryGetValue(baseName.Name, out var baseStringType))
                        {
                            // "FeatureSetpointSpecificUsageEnumType" restricts
                            // "FeatureMeasurementSpecificUsageEnumType" without
                            // narrowing it: same values, own name.
                            stringTypes.Add(
                                name,
                                new SpineStringType(
                                    name,
                                    Naming.ResourceOf(SimpleType.SourceUri),
                                    [.. baseStringType.Values],
                                    baseStringType.IsExtensible
                                )
                            );
                            return true;
                        }

                        return false;

                    }

                    return Alias(name, ("String", false));

                }

                #endregion

                default:
                    return Alias(name, ("String", false));

            }

        }

        #endregion

        #region (private) TryCollectValues(TypeName, Values, out Known)

        /// <summary>
        /// Collect the values of an enumeration, following unions and
        /// restrictions of other enumerations.
        /// </summary>
        /// <param name="TypeName">The name of a SPINE simple type.</param>
        /// <param name="Values">The values collected so far.</param>
        /// <param name="Known">Whether the type could be resolved at all.</param>
        private Boolean TryCollectValues(String TypeName, List<String> Values, out Boolean Known)
        {

            Known = false;

            if (GlobalType(TypeName) is not XmlSchemaSimpleType simpleType)
                return false;

            switch (simpleType.Content)
            {

                case XmlSchemaSimpleTypeRestriction restriction:
                {

                    var values = restriction.Facets.
                                     OfType<XmlSchemaEnumerationFacet>().
                                     Select(facet => facet.Value ?? "").
                                     ToList();

                    if (values.Count > 0)
                    {
                        foreach (var value in values)
                            if (!Values.Contains(value, StringComparer.Ordinal))
                                Values.Add(value);
                        Known = true;
                        return true;
                    }

                    if (restriction.BaseTypeName.Namespace == SpineNamespace)
                        return TryCollectValues(restriction.BaseTypeName.Name, Values, out Known);

                    // A plain string or number without values: nothing to collect,
                    // but the type is understood.
                    Known = true;
                    return true;

                }

                case XmlSchemaSimpleTypeUnion union:
                {

                    foreach (var member in union.MemberTypes ?? [])
                    {

                        if (member.Name == EnumExtendType)
                            continue;

                        if (member.Namespace != SpineNamespace)
                            continue;

                        if (!TryCollectValues(member.Name, Values, out _))
                            return false;

                    }

                    Known = true;
                    return true;

                }

                default:
                    Known = true;
                    return true;

            }

        }

        #endregion

        #region (private) Alias(Name, Target) / ResolveLater(QualifiedName) / GlobalType(Name)

        private Boolean Alias(String Name, (String Type, Boolean IsValueType) Target)
        {
            aliases[Name] = Target;
            return true;
        }

        /// <summary>
        /// The C# type of a base type which may itself not be classified yet;
        /// resolved lazily by <see cref="Resolve"/>.
        /// </summary>
        private (String Type, Boolean IsValueType) ResolveLater(XmlQualifiedName BaseTypeName)

            => BaseTypeName.Namespace == XsdNamespace && builtins.TryGetValue(BaseTypeName.Name, out var builtin)
                   ? builtin
                   : (BaseTypeName.Name, true);

        private XmlSchemaType? GlobalType(String Name)

            => schemaSet.GlobalTypes[new XmlQualifiedName(Name, SpineNamespace)] as XmlSchemaType;

        #endregion

        #region (private) Resolve(TypeName)

        /// <summary>
        /// The C# type of an XSD type reference.
        /// </summary>
        /// <param name="TypeName">The name of an XSD type.</param>
        private (String Type, Boolean IsValueType) Resolve(XmlQualifiedName TypeName)
        {

            if (TypeName.IsEmpty)
            {
                warnings.Add("An element without a type was mapped to String.");
                return ("String", false);
            }

            if (TypeName.Namespace == XsdNamespace)
                return builtins.TryGetValue(TypeName.Name, out var builtin)
                           ? builtin
                           : ("String", false);

            var name = TypeName.Name;

            // Aliases may point at other aliases; three steps are more than the
            // XSDs ever need, and a broken chain is reported rather than looped.
            for (var step = 0; step < 8; step++)
            {

                if (stringTypes.ContainsKey(name))
                    return (name, true);

                if (classNames.Contains(name))
                    return (name, false);

                if (aliases.TryGetValue(name, out var alias))
                {

                    if (alias.Type == name)
                        return alias;

                    name = alias.Type;

                    // The alias may already be a C# type rather than an XSD one.
                    if (!aliases.ContainsKey(name)  &&
                        !stringTypes.ContainsKey(name) &&
                        !classNames.Contains(name))
                    {
                        return alias;
                    }

                    continue;

                }

                break;

            }

            warnings.Add($"The type '{TypeName.Name}' could not be resolved and was mapped to String.");

            return ("String", false);

        }

        #endregion

        #region (private) Properties(ClassName, ComplexType, Functions)

        /// <summary>
        /// The properties of one complex type, in the order of the XSD.
        /// </summary>
        private List<SpineProperty> Properties(String                             ClassName,
                                               XmlSchemaComplexType               ComplexType,
                                               Dictionary<String, SpineFunction>  Functions)
        {

            var properties = new List<SpineProperty>();
            var used       = new HashSet<String>(StringComparer.Ordinal);

            foreach (var element in Elements(ComplexType.ContentTypeParticle))
            {

                var xmlName = element.QualifiedName.IsEmpty
                                  ? element.Name ?? ""
                                  : element.QualifiedName.Name;

                if (xmlName.Length == 0)
                    continue;

                var (type, isValueType) = ResolveElementType(ClassName, xmlName, element);

                var name = Naming.PascalCase(xmlName);

                // A C# member may not be called like the type it lives in.
                if (String.Equals(name, ClassName, StringComparison.Ordinal))
                    name += "Value";

                if (!used.Add(name))
                {
                    warnings.Add($"'{ClassName}' declares '{xmlName}' more than once; the repetition was dropped.");
                    continue;
                }

                var function = (String?) null;
                var part     = FunctionPart.None;

                if (Functions.ContainsKey(xmlName))
                {
                    function = xmlName;
                    part     = FunctionPart.Data;
                }

                else if (selectorsElementOfFunction.TryGetValue(xmlName, out var selectorsFunction))
                {
                    function = selectorsFunction;
                    part     = FunctionPart.Selectors;
                }

                else if (elementsElementOfFunction.TryGetValue(xmlName, out var elementsFunction))
                {
                    function = elementsFunction;
                    part     = FunctionPart.Elements;
                }

                properties.Add(
                    new SpineProperty(
                        xmlName,
                        name,
                        type,
                        element.MaxOccurs > 1,
                        isValueType,
                        function,
                        part
                    )
                );

            }

            return properties;

        }

        #endregion

        #region (private) ResolveElementType(ClassName, XmlName, Element)

        /// <summary>
        /// The C# type of one element declaration.
        /// </summary>
        /// <param name="ClassName">The complex type the element belongs to, for the warnings.</param>
        /// <param name="XmlName">The name of the element, for the warnings.</param>
        /// <param name="Element">An element declaration.</param>
        private (String Type, Boolean IsValueType) ResolveElementType(String            ClassName,
                                                                      String            XmlName,
                                                                      XmlSchemaElement  Element)
        {

            var named = Element.ElementSchemaType?.QualifiedName ?? XmlQualifiedName.Empty;

            if (named.IsEmpty)
                named = Element.SchemaTypeName;

            if (!named.IsEmpty)
                return Resolve(named);

            // An anonymous type, declared inline. SPINE uses those to narrow a
            // named type to a few of its fields - "nodeManagementDestinationData"
            // carries a "NetworkManagementDeviceDescriptionDataType" with five of
            // its elements - so the named base is what belongs into the model.
            // On the wire the two are the same object, and the Go reference
            // implementation uses the base type as well.
            var baseName = (Element.SchemaType ?? Element.ElementSchemaType) switch {

                XmlSchemaComplexType { ContentModel: XmlSchemaComplexContent { Content: XmlSchemaComplexContentRestriction restriction } }
                    => restriction.BaseTypeName,

                XmlSchemaComplexType { ContentModel: XmlSchemaComplexContent { Content: XmlSchemaComplexContentExtension  extension   } }
                    => extension.BaseTypeName,

                XmlSchemaSimpleType  { Content:      XmlSchemaSimpleTypeRestriction simpleRestriction }
                    => simpleRestriction.BaseTypeName,

                _   => XmlQualifiedName.Empty

            };

            if (!baseName.IsEmpty)
                return Resolve(baseName);

            warnings.Add($"'{ClassName}.{XmlName}' has an anonymous type without a named base and was mapped to String.");

            return ("String", false);

        }

        #endregion

        #region (private) Elements(Particle)

        /// <summary>
        /// All element declarations of a particle, in document order.
        /// The particle is the compiled one, so the groups of the XSD are
        /// already resolved.
        /// </summary>
        private static IEnumerable<XmlSchemaElement> Elements(XmlSchemaParticle? Particle)
        {

            switch (Particle)
            {

                case XmlSchemaElement element:
                    yield return element;
                    break;

                case XmlSchemaGroupBase groupBase:
                    foreach (var item in groupBase.Items)
                        if (item is XmlSchemaParticle particle)
                            foreach (var element in Elements(particle))
                                yield return element;
                    break;

            }

        }

        #endregion

        #region (private) ReadFunctions()

        /// <summary>
        /// The function registry, read from the three choice groups of the
        /// command frame: which functions there are, and which data, selectors
        /// and elements type belongs to each of them.
        /// </summary>
        private List<SpineFunction> ReadFunctions()
        {

            var data      = GroupElements("DataChoiceGroup");
            var selectors = GroupElements("DataSelectorsChoiceGroup");
            var elements  = GroupElements("DataElementsChoiceGroup");

            var functions = new List<SpineFunction>();

            foreach (var (name, element) in data.OrderBy(entry => entry.Key, StringComparer.Ordinal))
            {

                var (dataType, _) = Resolve(element.ElementSchemaType?.QualifiedName ?? element.SchemaTypeName);

                var hasSelectors  = selectors.TryGetValue(name + "Selectors", out var selectorsElement);

                var selectorsType = hasSelectors
                                        ? Resolve(selectorsElement!.ElementSchemaType?.QualifiedName ?? selectorsElement.SchemaTypeName).Type
                                        : null;

                if (hasSelectors)
                    selectorsElementOfFunction[name + "Selectors"] = name;

                // The elements of a function are the elements of one entry of it,
                // not of the whole list: "loadControlLimitListData" is answered
                // by "loadControlLimitDataElements". Which entry that is, is in
                // the XSD - it is the repeated element of the list type - so the
                // name does not have to be guessed.
                var elementsName  = ItemElementOf(dataType) ?? name;

                if (!elements.TryGetValue(elementsName + "Elements", out var elementsElement))
                    elements.TryGetValue(name + "Elements", out elementsElement);

                var elementsType  = elementsElement is not null
                                        ? Resolve(elementsElement.ElementSchemaType?.QualifiedName ?? elementsElement.SchemaTypeName).Type
                                        : null;

                if (elementsElement is not null)
                {

                    var elementsElementName = elementsElement.QualifiedName.IsEmpty
                                                  ? elementsElement.Name ?? ""
                                                  : elementsElement.QualifiedName.Name;

                    // One elements type may serve several functions - the entries
                    // of a list and the list itself share their fields - and the
                    // command frame carries the element only once. The first
                    // function wins, which is the one the entries belong to.
                    if (elementsElementName.Length > 0 && !elementsElementOfFunction.ContainsKey(elementsElementName))
                        elementsElementOfFunction[elementsElementName] = name;

                }

                var resource      = Naming.ResourceOf(
                                        (GlobalType(dataType) as XmlSchemaComplexType)?.SourceUri
                                    );

                functions.Add(
                    new SpineFunction(
                        name,
                        resource,
                        dataType,
                        selectorsType,
                        elementsType
                    )
                );

            }

            return functions;

        }

        #endregion

        #region (private) ItemElementOf(TypeName)

        /// <summary>
        /// The name of the repeated element of a list type, or null when the
        /// given type is not one.
        ///
        /// A "...ListDataType" of SPINE holds nothing but its entries, so a
        /// complex type with exactly one element, which may occur more than
        /// once, is a list of that element.
        /// </summary>
        /// <param name="TypeName">The name of an XSD complex type.</param>
        private String? ItemElementOf(String TypeName)
        {

            if (GlobalType(TypeName) is not XmlSchemaComplexType complexType)
                return null;

            var elements = Elements(complexType.ContentTypeParticle).ToList();

            return elements.Count == 1 && elements[0].MaxOccurs > 1
                       ? elements[0].QualifiedName.IsEmpty
                             ? elements[0].Name
                             : elements[0].QualifiedName.Name
                       : null;

        }

        #endregion

        #region (private) GroupElements(GroupName)

        /// <summary>
        /// The elements referenced by a global group, by their name.
        /// </summary>
        /// <param name="GroupName">The name of a global XSD group.</param>
        private Dictionary<String, XmlSchemaElement> GroupElements(String GroupName)
        {

            var result = new Dictionary<String, XmlSchemaElement>(StringComparer.Ordinal);

            // XmlSchemaSet publishes the global elements, types and attributes,
            // but not the groups; those stay with the schema which declares them.
            var group = schemaSet.Schemas().
                            OfType<XmlSchema>().
                            Select(schema => schema.Groups[new XmlQualifiedName(GroupName, SpineNamespace)]).
                            OfType<XmlSchemaGroup>().
                            FirstOrDefault();

            if (group is null)
            {
                warnings.Add($"The group '{GroupName}' was not found; the function registry is incomplete.");
                return result;
            }

            foreach (var element in Elements(group.Particle))
            {

                var name = element.RefName.IsEmpty
                               ? element.Name ?? ""
                               : element.RefName.Name;

                if (name.Length == 0)
                    continue;

                // The group references global elements; those carry the type.
                if (schemaSet.GlobalElements[new XmlQualifiedName(name, SpineNamespace)] is XmlSchemaElement globalElement)
                    result[name] = globalElement;

            }

            return result;

        }

        #endregion

    }

}
