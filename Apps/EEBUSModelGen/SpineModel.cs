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

namespace cloud.charging.open.protocols.EEBUS.ModelGen
{

    /// <summary>
    /// Which of the three faces of a SPINE function a property carries.
    /// </summary>
    public enum FunctionPart
    {

        /// <summary>No function metadata.</summary>
        None,

        /// <summary>The function data.</summary>
        Data,

        /// <summary>The selectors of the function.</summary>
        Selectors,

        /// <summary>The elements of the function.</summary>
        Elements

    }


    /// <summary>
    /// One property of a generated class.
    /// </summary>
    /// <param name="XmlName">The name of the XSD element, which is also the JSON property name.</param>
    /// <param name="CSharpName">The name of the C# property.</param>
    /// <param name="CSharpType">The C# type, without the nullable marker and without the list.</param>
    /// <param name="IsList">Whether the element may occur more than once.</param>
    /// <param name="IsValueType">Whether the C# type is a value type, which decides how the property is made nullable.</param>
    /// <param name="Function">The SPINE function this property belongs to, for the properties of a command frame.</param>
    /// <param name="Part">Which face of that function it carries.</param>
    /// <param name="IsKey">Whether this property is an identifier of its data type.</param>
    /// <param name="IsPrimaryKey">Whether this property is the primary identifier of its data type.</param>
    /// <param name="IsWriteCheck">Whether this property states that its data type may be changed by a remote peer.</param>
    public sealed record SpineProperty(String        XmlName,
                                       String        CSharpName,
                                       String        CSharpType,
                                       Boolean       IsList,
                                       Boolean       IsValueType,
                                       String?       Function       = null,
                                       FunctionPart  Part           = FunctionPart.None,
                                       Boolean       IsKey          = false,
                                       Boolean       IsPrimaryKey   = false,
                                       Boolean       IsWriteCheck   = false);


    /// <summary>
    /// A complex type of the SPINE data model, which becomes a C# class.
    /// </summary>
    /// <param name="Name">The name of the XSD complex type, kept verbatim.</param>
    /// <param name="Resource">The resource (XSD file) it was declared in.</param>
    /// <param name="Properties">Its properties, in the order of the XSD sequence.</param>
    public sealed record SpineClass(String                Name,
                                    String                Resource,
                                    List<SpineProperty>   Properties);


    /// <summary>
    /// A simple type of the SPINE data model which is a string on the wire and
    /// has a set of values defined by the specification.
    /// </summary>
    /// <param name="Name">The name of the XSD simple type, kept verbatim.</param>
    /// <param name="Resource">The resource (XSD file) it was declared in.</param>
    /// <param name="Values">The values of the specification, in the order of the XSD.</param>
    /// <param name="IsExtensible">Whether the XSD unions the values with "EnumExtendType".</param>
    public sealed record SpineStringType(String        Name,
                                         String        Resource,
                                         List<String>  Values,
                                         Boolean       IsExtensible);


    /// <summary>
    /// One entry of the function registry: a SPINE function together with the
    /// three types which represent it.
    /// </summary>
    /// <param name="Name">The function name, as within "FunctionEnumType".</param>
    /// <param name="Resource">The resource (XSD file) the function data was declared in.</param>
    /// <param name="DataType">The C# type of the function data.</param>
    /// <param name="SelectorsType">The C# type of its selectors, where the function has any.</param>
    /// <param name="ElementsType">The C# type of its elements, where the function has any.</param>
    public sealed record SpineFunction(String   Name,
                                       String   Resource,
                                       String   DataType,
                                       String?  SelectorsType,
                                       String?  ElementsType);


    /// <summary>
    /// The whole normalised SPINE data model, as read from the XSDs.
    /// </summary>
    /// <param name="Version">The version of the specification, as stated by the XSDs.</param>
    /// <param name="Classes">All complex types.</param>
    /// <param name="StringTypes">All string types with values.</param>
    /// <param name="Functions">The function registry.</param>
    public sealed record SpineModel(String                 Version,
                                    List<SpineClass>       Classes,
                                    List<SpineStringType>  StringTypes,
                                    List<SpineFunction>    Functions);

}
