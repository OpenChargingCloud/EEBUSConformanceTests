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

using Newtonsoft.Json.Linq;

#endregion

namespace cloud.charging.open.protocols.EEBUS.Conformance
{

    #region (enum) AddressChangeRecoveries

    /// <summary>
    /// How a device survives a communication partner whose entity address
    /// changed while it was away (PAR_addressChangeRecovery).
    /// </summary>
    public enum AddressChangeRecoveries
    {

        /// <summary>
        /// Bindings and subscriptions live as long as the session does, and not
        /// one second longer. The recommended answer, and the simple one: what
        /// is never stored can never be stale.
        /// </summary>
        SessionOnly,

        /// <summary>
        /// They are stored, and after a reconnection the device notices by
        /// itself which of them became invalid, cleans them up and builds them
        /// again against the new addresses.
        /// </summary>
        PersistentAutoRecover

    }

    #endregion


    /// <summary>
    /// The manufacturer declarations about a device under test - what the
    /// official test suite calls the "Parameter Sheet".
    ///
    /// Every field carries the official PAR_ name, because these declarations
    /// are not decoration: they decide which test cases apply at all. A device
    /// declaring PAR_shipSvc = "no" is not failing the mDNS case, it is not
    /// being asked. Getting that wrong in either direction is the difference
    /// between a certification report and a nice-looking table.
    ///
    /// The official sheets are XLSX files; this is the same structure as JSON,
    /// with the same field names, so an importer can be added later without
    /// changing anything which reads a sheet.
    /// </summary>
    public class ParameterSheet
    {

        #region Properties (SHIP)

        /// <summary>
        /// PAR_shipSvc: whether the device announces its SHIP service via mDNS.
        /// Only a control box conforming to the FNN specification may say no.
        /// </summary>
        public Boolean                  ShipSvc                           { get; init; } = true;

        /// <summary>
        /// PAR_helloProlongationCount: how many prolongation requests the test
        /// tool sends in TC_SHIP_HELLO_002. At least two.
        /// </summary>
        public UInt32                   HelloProlongationCount            { get; init; } = 2;

        /// <summary>
        /// PAR_testToolShipVersion: the highest SHIP version the test tool
        /// announces. It has to share the major version of the device.
        /// </summary>
        public String                   TestToolShipVersion               { get; init; } = "1.0";

        /// <summary>
        /// PAR_shipVersion: the highest SHIP version the device supports.
        /// </summary>
        public String                   ShipVersion                       { get; init; } = "1.0";

        /// <summary>
        /// PAR_initiateClose: whether the device announces a termination with a
        /// close message before it drops the connection.
        /// </summary>
        public Boolean                  InitiateClose                     { get; init; } = true;

        /// <summary>
        /// PAR_shipId: the SHIP identifier of the device. Mandatory only when
        /// it announces no service, because then there is no TXT record to read
        /// it from.
        /// </summary>
        public String?                  ShipId                            { get; init; }

        /// <summary>
        /// PAR_queryAccessMethods: whether the device sends an access methods
        /// request of its own once the connection is up.
        /// </summary>
        public Boolean                  QueryAccessMethods                { get; init; } = true;

        #endregion

        #region Properties (SPINE)

        /// <summary>
        /// PAR_clientDetailedDiscoverySupported: whether the device asks a new
        /// partner for its detailed discovery. Declared in both sheets, with
        /// the same meaning.
        /// </summary>
        public Boolean                  ClientDetailedDiscoverySupported  { get; init; } = true;

        /// <summary>
        /// PAR_clientSubscriptionSupported: whether the device subscribes to
        /// server features of others.
        /// </summary>
        public Boolean                  ClientSubscriptionSupported       { get; init; } = true;

        /// <summary>
        /// PAR_initialTimeoutSupported: whether the device drops a partner
        /// which says nothing for thirty seconds. Optional in Annex D.3.
        /// </summary>
        public Boolean                  InitialTimeoutSupported           { get; init; }

        /// <summary>
        /// PAR_addressChangeRecovery: how the device recovers from a partner
        /// whose address changed.
        /// </summary>
        public AddressChangeRecoveries  AddressChangeRecovery             { get; init; } = AddressChangeRecoveries.SessionOnly;

        /// <summary>
        /// PAR_spineVersion: the SPINE version the device supports, 1.3.0 or higher.
        /// </summary>
        public String                   SpineVersion                      { get; init; } = "1.3.0";

        /// <summary>
        /// PAR_limitationUseCase: which of the two power limitation use cases
        /// the LPC/LPP flavoured test cases are executed with. "LPC" or "LPP".
        /// </summary>
        public String                   LimitationUseCase                 { get; init; } = "LPC";

        /// <summary>
        /// PAR_lpcLpp_Test_Value1: a valid limit within the physical range of
        /// the device. Its last digit may not be a zero - the sheet asks for
        /// that so that a device silently rounding to hundreds is noticed.
        /// </summary>
        public Int64                    LpcLppTestValue1                  { get; init; } = 4201;

        /// <summary>
        /// PAR_lpcLpp_Test_Value2: a second, different valid limit.
        /// </summary>
        public Int64                    LpcLppTestValue2                  { get; init; } = 3300;

        #endregion

        #region Properties (the device itself)

        /// <summary>
        /// Which use case actors the device implements: "EG" for energy guard,
        /// "CS" for controllable system. The actor, never the role, decides
        /// whether a use case flavoured test case applies.
        /// </summary>
        public IReadOnlySet<String>     Actors                            { get; init; } = new HashSet<String> { "EG", "CS" };

        /// <summary>
        /// What the device is called in the report.
        /// </summary>
        public String                   DeviceName                        { get; init; } = "WWCP_EEBUS";

        /// <summary>
        /// Who made it.
        /// </summary>
        public String                   Manufacturer                      { get; init; } = "GraphDefined GmbH";

        #endregion


        #region (static) OurOwnStack

        /// <summary>
        /// What this stack declares about itself - the self test profile.
        ///
        /// Two declarations are deliberately modest. PAR_initialTimeoutSupported
        /// is "no" because the optional thirty second drop of a silent partner
        /// is not implemented, and declaring it would earn a failed test case
        /// rather than a skipped one. PAR_queryAccessMethods is "yes" because
        /// the connection does send its own request, which is what makes
        /// TC_SHIP_AMDATA_004 not apply and TC_SHIP_AMDATA_002 apply.
        /// </summary>
        public static ParameterSheet OurOwnStack { get; } = new () {

            ShipSvc                           = true,
            HelloProlongationCount            = 2,
            TestToolShipVersion               = "1.0",
            ShipVersion                       = "1.0",
            InitiateClose                     = true,
            ShipId                            = null,
            QueryAccessMethods                = true,

            ClientDetailedDiscoverySupported  = true,
            ClientSubscriptionSupported       = true,
            InitialTimeoutSupported           = false,
            AddressChangeRecovery             = AddressChangeRecoveries.SessionOnly,
            SpineVersion                      = "1.3.0",
            LimitationUseCase                 = "LPC",
            LpcLppTestValue1                  = 4201,
            LpcLppTestValue2                  = 3300,

            Actors                            = new HashSet<String> { "EG", "CS" },
            DeviceName                        = "WWCP_EEBUS",
            Manufacturer                      = "GraphDefined GmbH"

        };

        #endregion

        #region (static) Parse(JSON) / TryLoad(File)

        /// <summary>
        /// Read a parameter sheet from its JSON representation, which uses the
        /// official PAR_ names.
        /// </summary>
        /// <param name="JSON">The JSON representation of a parameter sheet.</param>
        public static ParameterSheet Parse(JObject JSON)
        {

            var fallback = OurOwnStack;

            return new ParameterSheet {

                ShipSvc                           = Yes(JSON, "PAR_shipSvc",                          fallback.ShipSvc),
                HelloProlongationCount            = (UInt32?) JSON["PAR_helloProlongationCount"]                    ?? fallback.HelloProlongationCount,
                TestToolShipVersion               =           JSON["PAR_testToolShipVersion"]?.Value<String>()      ?? fallback.TestToolShipVersion,
                ShipVersion                       =           JSON["PAR_shipVersion"]?.        Value<String>()      ?? fallback.ShipVersion,
                InitiateClose                     = Yes(JSON, "PAR_initiateClose",                    fallback.InitiateClose),
                ShipId                            =           JSON["PAR_shipId"]?.             Value<String>(),
                QueryAccessMethods                = Yes(JSON, "PAR_queryAccessMethods",               fallback.QueryAccessMethods),

                ClientDetailedDiscoverySupported  = Yes(JSON, "PAR_clientDetailedDiscoverySupported", fallback.ClientDetailedDiscoverySupported),
                ClientSubscriptionSupported       = Yes(JSON, "PAR_clientSubscriptionSupported",      fallback.ClientSubscriptionSupported),
                InitialTimeoutSupported           = Yes(JSON, "PAR_initialTimeoutSupported",          fallback.InitialTimeoutSupported),

                AddressChangeRecovery             = String.Equals(JSON["PAR_addressChangeRecovery"]?.Value<String>(),
                                                                  "persistent-auto-recover",
                                                                  StringComparison.OrdinalIgnoreCase)
                                                        ? AddressChangeRecoveries.PersistentAutoRecover
                                                        : AddressChangeRecoveries.SessionOnly,

                SpineVersion                      =           JSON["PAR_spineVersion"]?.       Value<String>()      ?? fallback.SpineVersion,
                LimitationUseCase                 =           JSON["PAR_limitationUseCase"]?.  Value<String>()      ?? fallback.LimitationUseCase,
                LpcLppTestValue1                  = (Int64?)  JSON["PAR_lpcLpp_Test_Value1"]                        ?? fallback.LpcLppTestValue1,
                LpcLppTestValue2                  = (Int64?)  JSON["PAR_lpcLpp_Test_Value2"]                        ?? fallback.LpcLppTestValue2,

                Actors                            = JSON["actors"] is JArray actors
                                                        ? new HashSet<String>(actors.Select(actor => actor.Value<String>() ?? "").Where(actor => actor.Length > 0))
                                                        : fallback.Actors,

                DeviceName                        =           JSON["deviceName"]?.  Value<String>() ?? fallback.DeviceName,
                Manufacturer                      =           JSON["manufacturer"]?.Value<String>() ?? fallback.Manufacturer

            };

        }


        /// <summary>
        /// Read a parameter sheet from a file, or fall back to the self test
        /// profile when there is none.
        /// </summary>
        /// <param name="File">A JSON parameter sheet, or null.</param>
        public static ParameterSheet TryLoad(FileInfo? File)

            => File is not null && File.Exists
                   ? Parse(JObject.Parse(System.IO.File.ReadAllText(File.FullName)))
                   : OurOwnStack;

        #endregion

        #region ToJSON()

        /// <summary>
        /// The JSON representation of this parameter sheet, using the official
        /// PAR_ names and the "yes"/"no" wording of the sheets.
        /// </summary>
        public JObject ToJSON()

            => new (

                   new JProperty("deviceName",                            DeviceName),
                   new JProperty("manufacturer",                          Manufacturer),
                   new JProperty("actors",                                new JArray(Actors.Order())),

                   new JProperty("PAR_shipSvc",                           ShipSvc                          ? "yes" : "no"),
                   new JProperty("PAR_helloProlongationCount",            HelloProlongationCount),
                   new JProperty("PAR_testToolShipVersion",               TestToolShipVersion),
                   new JProperty("PAR_shipVersion",                       ShipVersion),
                   new JProperty("PAR_initiateClose",                     InitiateClose                    ? "yes" : "no"),
                   new JProperty("PAR_shipId",                            ShipId ?? ""),
                   new JProperty("PAR_queryAccessMethods",                QueryAccessMethods               ? "yes" : "no"),

                   new JProperty("PAR_clientDetailedDiscoverySupported",  ClientDetailedDiscoverySupported ? "yes" : "no"),
                   new JProperty("PAR_clientSubscriptionSupported",       ClientSubscriptionSupported      ? "yes" : "no"),
                   new JProperty("PAR_initialTimeoutSupported",           InitialTimeoutSupported          ? "yes" : "no"),
                   new JProperty("PAR_addressChangeRecovery",             AddressChangeRecovery == AddressChangeRecoveries.PersistentAutoRecover
                                                                              ? "persistent-auto-recover"
                                                                              : "session-only"),
                   new JProperty("PAR_spineVersion",                      SpineVersion),
                   new JProperty("PAR_limitationUseCase",                 LimitationUseCase),
                   new JProperty("PAR_lpcLpp_Test_Value1",                LpcLppTestValue1),
                   new JProperty("PAR_lpcLpp_Test_Value2",                LpcLppTestValue2)

               );

        #endregion

        #region Validate()

        /// <summary>
        /// What a sheet says which cannot be true, or which would make a test
        /// case meaningless.
        /// </summary>
        public IEnumerable<String> Validate()
        {

            if (HelloProlongationCount < 2)
                yield return "PAR_helloProlongationCount has to be at least 2 (SHIP test specification, chapter 2.5).";

            if (!ShipSvc && String.IsNullOrEmpty(ShipId))
                yield return "PAR_shipId is mandatory when PAR_shipSvc is \"no\": without a TXT record there is nowhere else to read it from.";

            if (LpcLppTestValue1 % 10 == 0)
                yield return "The last digit of PAR_lpcLpp_Test_Value1 may not be a zero - a device rounding to tens would pass unnoticed.";

            if (LpcLppTestValue1 == LpcLppTestValue2)
                yield return "PAR_lpcLpp_Test_Value2 has to differ from PAR_lpcLpp_Test_Value1.";

            if (LimitationUseCase is not ("LPC" or "LPP"))
                yield return $"PAR_limitationUseCase is \"{LimitationUseCase}\", but only \"LPC\" and \"LPP\" exist.";

            if (MajorOf(TestToolShipVersion) != MajorOf(ShipVersion))
                yield return $"PAR_testToolShipVersion ({TestToolShipVersion}) and PAR_shipVersion ({ShipVersion}) have to share their major version.";

        }


        private static String MajorOf(String Version)

            => Version.Split('.')[0];

        #endregion

        #region (private) Yes(JSON, Name, Default)

        /// <summary>
        /// The sheets answer with "yes" and "no" rather than with booleans.
        /// </summary>
        private static Boolean Yes(JObject  JSON,
                                   String   Name,
                                   Boolean  Default)
        {

            var value = JSON[Name];

            if (value is null)
                return Default;

            if (value.Type == JTokenType.Boolean)
                return value.Value<Boolean>();

            return String.Equals(value.Value<String>(), "yes", StringComparison.OrdinalIgnoreCase);

        }

        #endregion


        public override String ToString()

            => $"{DeviceName} ({Manufacturer}): SHIP {ShipVersion}, SPINE {SpineVersion}, actors {String.Join(", ", Actors.Order())}";

    }

}
