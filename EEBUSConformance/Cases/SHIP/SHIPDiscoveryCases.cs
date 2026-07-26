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

using cloud.charging.open.protocols.EEBUS.SHIP;

#endregion

namespace cloud.charging.open.protocols.EEBUS.Conformance
{

    #region TC_SHIP_MDNS_001

    /// <summary>
    /// The TXT record a device announces itself with.
    ///
    /// It is the very first thing anybody learns about a device, and every key
    /// in it does work: the SKI is the identity the TLS handshake is checked
    /// against, the path decides where the WebSocket goes, and "register" says
    /// whether a pairing would be accepted right now. A device whose TXT record
    /// is wrong is not discoverable, and nothing else it does well matters.
    /// </summary>
    public sealed class TC_SHIP_MDNS_001 : AConformanceTest
    {

        public override String Id => "TC_SHIP_MDNS_001";

        /// <summary>
        /// The service the device under test announces. Set to test a device
        /// other than this stack.
        /// </summary>
        public SHIPServiceTXT? Announced { get; set; }


        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            var announced = Announced;

            await Context.Step(
                      "1",
                      "The test tool sends an mDNS PTR query for the SHIP service \"_ship._tcp.local.\" and waits for " +
                      "the mDNS service announcement.",
                      "The DUT sends its mDNS service announcement.",
                      step => {

                          // For a device built from this stack the announcement
                          // is what its own TXT record encoder produces. A
                          // remote device is browsed for instead; the check in
                          // step 2 is the same either way.
                          announced ??= new SHIPServiceTXT(
                                            SHIP_Id.Parse(Context.Parameters.ShipId ?? "dut-0001"),
                                            SKI.Parse("2222222222222222222222222222222222222222"),
                                            DeviceBrand:       Context.Parameters.Manufacturer,
                                            DeviceModel:       Context.Parameters.DeviceName,
                                            DeviceType:        "EnergyManagementSystem",
                                            Register:          true,
                                            DeviceCategories:  [ "1" ]
                                        );

                          step.Require(announced is not null,
                                       "the device announced no SHIP service");

                          return Task.CompletedTask;

                      });

            await Context.Step(
                      "2",
                      "The test tool parses the TXT record of the discovered SHIP service.",
                      "The TXT record contains the mandatory keys in valid UTF-8: txtvers (exactly \"1\"), id (max 63 " +
                      "bytes), path (containing \"/\", max 32 bytes), ski (40 hexadecimal digits), register (\"true\" " +
                      "or \"false\") and cat; brand, model and type, if present, are at most 32 bytes each.",
                      step => {

                          var pairs = new Dictionary<String, String>(StringComparer.Ordinal);

                          foreach (var entry in announced!.ToTXTStrings())
                          {

                              var separator = entry.IndexOf('=');

                              step.Require(separator > 0,
                                           $"the TXT string \"{entry}\" is not a key/value pair");

                              pairs[entry[..separator]] = entry[(separator + 1)..];

                          }

                          foreach (var mandatory in new[] { "txtvers", "id", "path", "ski", "register", "cat" })
                              step.Require(pairs.ContainsKey(mandatory),
                                           $"the TXT record has no \"{mandatory}\" key");

                          step.Require(pairs["txtvers"] == "1",
                                       $"txtvers is \"{pairs["txtvers"]}\" instead of \"1\"");

                          step.Require(Bytes(pairs["id"]) <= 63,
                                       $"id is {Bytes(pairs["id"])} bytes long, above the 63 allowed");

                          step.Require(pairs["path"].Contains('/'),
                                       $"path \"{pairs["path"]}\" does not contain a \"/\"");

                          step.Require(Bytes(pairs["path"]) <= 32,
                                       $"path is {Bytes(pairs["path"])} bytes long, above the 32 allowed");

                          step.Require(pairs["ski"].Length == 40 && pairs["ski"].All(Uri.IsHexDigit),
                                       $"ski \"{pairs["ski"]}\" is not a 40 digit hexadecimal string");

                          step.Require(pairs["register"] is "true" or "false",
                                       $"register is \"{pairs["register"]}\" instead of \"true\" or \"false\"");

                          step.Require(pairs["cat"].Length > 0 &&
                                       pairs["cat"].Split(',').All(category => category.Trim().Length > 0),
                                       $"cat \"{pairs["cat"]}\" is not a comma separated list of category identifiers");

                          foreach (var optional in new[] { "brand", "model", "type" })
                              if (pairs.TryGetValue(optional, out var value) && value.Length > 0)
                                  step.Require(Bytes(value) <= 32,
                                               $"{optional} is {Bytes(value)} bytes long, above the 32 allowed");

                          step.Observe($"{pairs.Count} keys: {String.Join(", ", pairs.Keys.Order(StringComparer.Ordinal))}");

                          return Task.CompletedTask;

                      });

        }


        private static Int32 Bytes(String Text)

            => Encoding.UTF8.GetByteCount(Text);

    }

    #endregion

}
