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

using System.Text.RegularExpressions;

using Microsoft.Extensions.Time.Testing;

using Newtonsoft.Json.Linq;

using cloud.charging.open.protocols.EEBUS.SPINE;
using cloud.charging.open.protocols.EEBUS.SPINE.Model;

#endregion

namespace cloud.charging.open.protocols.EEBUS.Conformance
{

    #region (class) ASPINECase

    /// <summary>
    /// The shared start of the hand spoken SPINE cases: a device under test, a
    /// test tool which says only what a step tells it to, and a connection
    /// between them.
    /// </summary>
    public abstract class ASPINECase : AConformanceTest
    {

        /// <summary>
        /// The regular expression every version string of SPINE has to match.
        /// </summary>
        protected static readonly Regex VersionFormat = new (@"^[1-9][0-9]*\.[0-9]+\.[0-9]+$");


        /// <summary>
        /// PRE_SPINE_ConnectionEstablished / PRE_SPINE_NewConnectionEstablished:
        /// two devices which can exchange datagrams and have said nothing to
        /// each other yet.
        /// </summary>
        /// <param name="Parameters">What the device declared about itself.</param>
        protected static SPINETestTool Connected(ParameterSheet Parameters)
        {

            var time  = new FakeTimeProvider(new DateTimeOffset(2026, 7, 26, 12, 0, 0, TimeSpan.Zero));

            var dut   = new SPINELocalDevice("d:_i:19667_DUT",
                                             DeviceTypeType.EnergyManagementSystem,
                                             TimeProvider: time);

            dut.AddEntity(EntityTypeType.CEM);

            var tool  = new SPINELocalDevice("d:_i:19667_TestTool",
                                             DeviceTypeType.ChargingStation,
                                             TimeProvider: time);

            tool.AddEntity(EntityTypeType.EVSE);

            return new SPINETestTool(dut, tool, time);

        }

    }

    #endregion


    #region TC_SPINE_COMP_001

    /// <summary>
    /// A function nobody has ever heard of earns an application error, not
    /// silence - the sender has to be able to tell "you do not support this"
    /// from "you did not hear me".
    /// </summary>
    public sealed class TC_SPINE_COMP_001 : ASPINECase
    {

        public override String Id => "TC_SPINE_COMP_001";

        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            var tool = Connected(Context.Parameters);

            await Context.Step(
                      "1",
                      "The test tool sends a read request using PAR_readUnknownFunction.",
                      "The DUT responds with a resultData message indicating an errorNumber > 0 " +
                      "(best practice: errorNumber = 6).",
                      async step => {

                          var counter  = tool.NextMsgCounter();
                          var unknown  = SPINEParameters.RandomName(20);

                          // The function does not exist, so no model type can
                          // build it - which is exactly the point. The datagram
                          // is written as JSON, the way it would arrive.
                          await tool.Send(
                                    new JObject(
                                        new JProperty("datagram", new JObject(
                                            new JProperty("header", new JObject(
                                                new JProperty("specificationVersion",  "1.999.999"),
                                                new JProperty("addressSource",         SPINEJSON.ToJObject(tool.ToolNodeManagement)),
                                                new JProperty("addressDestination",    SPINEJSON.ToJObject(tool.DUTNodeManagement)),
                                                new JProperty("msgCounter",            counter),
                                                new JProperty("cmdClassifier",         "read")
                                            )),
                                            new JProperty("payload", new JObject(
                                                new JProperty("cmd", new JArray(
                                                    new JObject(new JProperty(unknown, new JObject()))
                                                ))
                                            ))
                                        ))
                                    ),
                                    CancellationToken
                                );

                          var result = tool.ResultFor(counter);

                          step.Require(result is not null,
                                       "the device said nothing about a function it cannot possibly support");

                          step.Require(result!.ErrorNumber > 0,
                                       $"the device answered with errorNumber {result.ErrorNumber}, which means success");

                          if (result.ErrorNumber != (UInt64) SPINEErrorNumbers.CommandNotSupported)
                              step.Observe($"errorNumber {result.ErrorNumber} rather than the recommended 6");

                          step.Observe($"errorNumber {result.ErrorNumber}: {result.Description}");

                      });

        }

    }

    #endregion

    #region TC_SPINE_COMP_002

    /// <summary>
    /// A datagram from the future - same major version, higher minor - is
    /// processed rather than refused. Forward compatibility is what lets a
    /// device shipped today keep working next to one shipped in five years.
    /// </summary>
    public sealed class TC_SPINE_COMP_002 : ASPINECase
    {

        public override String Id => "TC_SPINE_COMP_002";

        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            var tool = Connected(Context.Parameters);

            await Context.Step(
                      "1",
                      "The test tool sends a nodeManagementDetailedDiscoveryData read using PAR_nmDiscoveryFuture.",
                      "The DUT responds with a nodeManagementDetailedDiscoveryData reply.",
                      async step => {

                          var counter = tool.NextMsgCounter();

                          await tool.Send(SPINEParameters.NmDiscoveryFuture(tool.ToolNodeManagement,
                                                                            tool.DUTNodeManagement,
                                                                            counter),
                                          CancellationToken);

                          step.Require(tool.ReplyDataFor(counter, SPINENodeManagement.DetailedDiscoveryData) is not null,
                                       tool.ResultFor(counter) is ResultDataType refused
                                           ? $"the device refused a datagram announcing SPINE 1.999.999 with errorNumber {refused.ErrorNumber}"
                                           : "the device did not answer a datagram announcing a higher minor version");

                      });

        }

    }

    #endregion

    #region TC_SPINE_COMP_003

    /// <summary>
    /// Elements nobody has defined yet, inside an otherwise valid reply, are
    /// ignored and the rest is used.
    /// </summary>
    public sealed class TC_SPINE_COMP_003 : ASPINECase
    {

        public override String Id => "TC_SPINE_COMP_003";

        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            var tool = Connected(Context.Parameters);

            var read = 0UL;

            await Context.Step(
                      "1",
                      "The test tool waits for a nodeManagementDetailedDiscoveryData read request for up to 30 s.",
                      "The DUT sends a nodeManagementDetailedDiscoveryData read request.",
                      async step => {

                          _ = tool.DUT.NodeManagement.RequestDetailedDiscovery(tool.ToolAsSeenByDUT, CancellationToken);

                          await tool.Advance(TimeSpan.FromSeconds(30), CancellationToken);

                          var request = tool.All(CmdClassifierType.Read, SPINENodeManagement.DetailedDiscoveryData).FirstOrDefault();

                          step.Require(request is not null,
                                       "the device did not ask a new partner what it is");

                          read = request!.Header?.MsgCounter ?? 0;

                      });

            await Context.Step(
                      "2",
                      "The test tool responds with a nodeManagementDetailedDiscoveryData reply using " +
                      "PAR_replyDiscoveryUnknownElements: an unsupported entity type, an unsupported feature type " +
                      "and an element which is not defined anywhere.",
                      "The DUT does not respond to the reply with a resultData message.",
                      async step => {

                          var before  = tool.FromDUT.Count;
                          var unknown = SPINEParameters.RandomName(15);

                          await tool.Send(
                                    new JObject(
                                        new JProperty("datagram", new JObject(
                                            new JProperty("header", new JObject(
                                                new JProperty("specificationVersion",  "1.999.999"),
                                                new JProperty("addressSource",         SPINEJSON.ToJObject(tool.ToolNodeManagement)),
                                                new JProperty("addressDestination",    SPINEJSON.ToJObject(tool.DUTNodeManagement)),
                                                new JProperty("msgCounter",            tool.NextMsgCounter()),
                                                new JProperty("msgCounterReference",   read),
                                                new JProperty("cmdClassifier",         "reply")
                                            )),
                                            new JProperty("payload", new JObject(
                                                new JProperty("cmd", new JArray(
                                                    new JObject(
                                                        new JProperty("nodeManagementDetailedDiscoveryData", new JObject(
                                                            new JProperty("entityInformation", new JArray(
                                                                new JObject(new JProperty("description", new JObject(
                                                                    new JProperty("entityAddress", new JObject(
                                                                        new JProperty("device", tool.Tool.DeviceAddress),
                                                                        new JProperty("entity", new JArray(99))
                                                                    )),
                                                                    new JProperty("entityType", "unsupportedEntityType")
                                                                )))
                                                            )),
                                                            new JProperty("featureInformation", new JArray(
                                                                new JObject(new JProperty("description", new JObject(
                                                                    new JProperty("featureAddress", new JObject(
                                                                        new JProperty("device",  tool.Tool.DeviceAddress),
                                                                        new JProperty("entity",  new JArray(99)),
                                                                        new JProperty("feature", 99)
                                                                    )),
                                                                    new JProperty("featureType", "unsupportedFeatureType"),
                                                                    new JProperty(unknown,       "testData")
                                                                )))
                                                            ))
                                                        ))
                                                    )
                                                ))
                                            ))
                                        ))
                                    ),
                                    CancellationToken
                                );

                          var answers = tool.FromDUT.Skip(before).
                                            Where(datagram => datagram.Header?.CmdClassifier == CmdClassifierType.Result).
                                            ToList();

                          step.Require(answers.Count == 0,
                                       "the device judged a reply it was not asked to judge: " +
                                       String.Join(", ", answers.Select(answer => answer.Payload?.Cmd?.FirstOrDefault()?.GetData("resultData") is ResultDataType result
                                                                                      ? $"errorNumber {result.ErrorNumber}"
                                                                                      : "a result")));

                      });

        }

    }

    #endregion

    #region TC_SPINE_COMP_004

    /// <summary>
    /// A reply whose payload is malformed is not answered with an application
    /// error either.
    ///
    /// The rule behind it is subtle and worth stating: a reply is acknowledged
    /// as a *transmission*, not judged as an *application*. Answering "your
    /// payload is nonsense" with errorNumber 7 looks helpful and is wrong -
    /// the sender asked nothing, so there is nothing to answer.
    /// </summary>
    public sealed class TC_SPINE_COMP_004 : ASPINECase
    {

        public override String Id => "TC_SPINE_COMP_004";

        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            var tool = Connected(Context.Parameters);

            var read = 0UL;

            await Context.Step(
                      "1",
                      "The test tool waits for a nodeManagementDetailedDiscoveryData read request for up to 30 s.",
                      "The DUT sends a nodeManagementDetailedDiscoveryData read request.",
                      async step => {

                          _ = tool.DUT.NodeManagement.RequestDetailedDiscovery(tool.ToolAsSeenByDUT, CancellationToken);

                          await tool.Advance(TimeSpan.FromSeconds(30), CancellationToken);

                          var request = tool.All(CmdClassifierType.Read, SPINENodeManagement.DetailedDiscoveryData).FirstOrDefault();

                          step.Require(request is not null,
                                       "the device did not ask a new partner what it is");

                          read = request!.Header?.MsgCounter ?? 0;

                      });

            await Context.Step(
                      "2",
                      "The test tool responds with a nodeManagementDetailedDiscoveryData reply using " +
                      "PAR_replyInvalidWithAck: a string where an unsigned integer belongs, and ackRequest = true.",
                      "The DUT remains silent or responds with a resultData message indicating errorNumber = 0, " +
                      "but no application error.",
                      async step => {

                          var before = tool.FromDUT.Count;

                          await tool.Send(
                                    new JObject(
                                        new JProperty("datagram", new JObject(
                                            new JProperty("header", new JObject(
                                                new JProperty("specificationVersion",  Context.Parameters.SpineVersion),
                                                new JProperty("addressSource",         SPINEJSON.ToJObject(tool.ToolNodeManagement)),
                                                new JProperty("addressDestination",    SPINEJSON.ToJObject(tool.DUTNodeManagement)),
                                                new JProperty("msgCounter",            tool.NextMsgCounter()),
                                                new JProperty("msgCounterReference",   read),
                                                new JProperty("cmdClassifier",         "reply"),
                                                new JProperty("ackRequest",            true)
                                            )),
                                            new JProperty("payload", new JObject(
                                                new JProperty("cmd", new JArray(
                                                    new JObject(
                                                        new JProperty("nodeManagementDetailedDiscoveryData", new JObject(
                                                            new JProperty("entityInformation", new JArray(
                                                                new JObject(new JProperty("description", new JObject(
                                                                    new JProperty("entityAddress", new JObject(
                                                                        new JProperty("entity", new JArray("InvalidStringValue"))
                                                                    ))
                                                                )))
                                                            ))
                                                        ))
                                                    )
                                                ))
                                            ))
                                        ))
                                    ),
                                    CancellationToken
                                );

                          foreach (var datagram in tool.FromDUT.Skip(before))
                          {

                              if (datagram.Header?.CmdClassifier != CmdClassifierType.Result)
                                  continue;

                              if (datagram.Payload?.Cmd?.FirstOrDefault()?.GetData("resultData") is not ResultDataType result)
                                  continue;

                              step.Require(result.ErrorNumber == 0,
                                           $"the device judged a malformed reply as an application error " +
                                           $"({result.ErrorNumber}: {result.Description})");

                              step.Observe("acknowledged with errorNumber 0");

                          }

                      });

        }

    }

    #endregion

    #region TC_SPINE_COMP_005

    /// <summary>
    /// Every version string a device sends has to match the format, and the use
    /// case document sub revision has to be there.
    ///
    /// A pedantic looking rule with a practical reason: version strings are
    /// compared, and a comparison between "1.3.0" and "v1.3" has no defined
    /// answer.
    /// </summary>
    public sealed class TC_SPINE_COMP_005 : ASPINECase
    {

        public override String Id => "TC_SPINE_COMP_005";

        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            var tool = Connected(Context.Parameters);

            await Context.Step(
                      "1",
                      "The test tool sends a nodeManagementDetailedDiscoveryData read request.",
                      "The DUT responds with a reply; datagram.header.specificationVersion and every entry of " +
                      "specificationVersionList.specificationVersion strictly match \"[1-9][0-9]*\\.[0-9]+\\.[0-9]+\".",
                      async step => {

                          var counter = tool.NextMsgCounter();

                          await tool.Send(SPINEParameters.ReadDetailedDiscovery(tool.ToolNodeManagement,
                                                                                tool.DUTNodeManagement,
                                                                                counter,
                                                                                SpecificationVersion: Context.Parameters.SpineVersion),
                                          CancellationToken);

                          var reply = tool.ReplyFor(counter);

                          step.Require(reply is not null,
                                       "the device did not answer the detailed discovery read");

                          var announced = reply!.Header?.SpecificationVersion ?? "";

                          step.Require(VersionFormat.IsMatch(announced),
                                       $"the header announces the version \"{announced}\", which does not match the format");

                          var discovery = reply.Payload?.Cmd?.FirstOrDefault()?.GetData(SPINENodeManagement.DetailedDiscoveryData)
                                              as NodeManagementDetailedDiscoveryDataType;

                          foreach (var version in discovery?.DeviceInformation?.Description?.NetworkFeatureSet is not null
                                                      ? discovery.SpecificationVersionList?.SpecificationVersion ?? []
                                                      : discovery?.SpecificationVersionList?.SpecificationVersion ?? [])
                              step.Require(VersionFormat.IsMatch(version.ToString() ?? ""),
                                           $"the specification version list contains \"{version}\", which does not match the format");

                          step.Observe($"header {announced}");

                      });

            await Context.Step(
                      "2",
                      "The test tool sends a nodeManagementUseCaseData read request.",
                      "The DUT responds with a reply; every useCaseVersion matches the format and " +
                      "useCaseDocumentSubRevision is present and populated.",
                      async step => {

                          var counter = tool.NextMsgCounter();

                          await tool.Send(SPINEParameters.ReadUseCaseData(tool.ToolNodeManagement,
                                                                          tool.DUTNodeManagement,
                                                                          counter),
                                          CancellationToken);

                          var useCases = tool.ReplyDataFor(counter, SPINENodeManagement.UseCaseData)
                                             as NodeManagementUseCaseDataType;

                          step.Require(useCases is not null,
                                       "the device did not answer the use case discovery read");

                          var announced = 0;

                          foreach (var information in useCases!.UseCaseInformation ?? [])
                              foreach (var support in information.UseCaseSupport ?? [])
                              {

                                  announced++;

                                  step.Require(VersionFormat.IsMatch(support.UseCaseVersion?.ToString() ?? ""),
                                               $"{support.UseCaseName} announces the version \"{support.UseCaseVersion}\", " +
                                               $"which does not match the format");

                                  step.Require(!String.IsNullOrEmpty(support.UseCaseDocumentSubRevision),
                                               $"{support.UseCaseName} announces no useCaseDocumentSubRevision");

                              }

                          step.Observe(announced > 0
                                           ? $"{announced} use case(s) announced"
                                           : "the device announces no use cases at all");

                      });

        }

    }

    #endregion

    #region TC_SPINE_COMP_006

    /// <summary>
    /// Seven version strings which break the format in seven different ways.
    ///
    /// The interesting part of this case is its verdict rule: rejecting is
    /// recommended, accepting is tolerated *for now* and earns a warning rather
    /// than a failure. The specification says outright that a future version
    /// may tighten this - which is a fair description of a field full of
    /// devices announcing "v1.3.0".
    /// </summary>
    public sealed class TC_SPINE_COMP_006 : ASPINECase
    {

        public override String Id => "TC_SPINE_COMP_006";

        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            String[] versions = [ "TS1.3.0", "V0.3.0", "v0.3.0", "0.3.0", "2.0.0", "V1.3.0", "v1.3.0" ];

            foreach (var version in versions)
            {

                var announced = version;

                await Context.Step(
                          $"1 ({announced})",
                          $"The test tool sends a nodeManagementDetailedDiscoveryData read request using " +
                          $"PAR_readInvalidVersionFormat with the header version set to \"{announced}\".",
                          "The DUT either rejects the request with an application error or by terminating the " +
                          "connection (recommended), or accepts it - which is tolerated with a warning.",
                          async step => {

                              var tool     = Connected(Context.Parameters);
                              var counter  = tool.NextMsgCounter();

                              await tool.Send(SPINEParameters.ReadDetailedDiscovery(tool.ToolNodeManagement,
                                                                                    tool.DUTNodeManagement,
                                                                                    counter,
                                                                                    SpecificationVersion: announced),
                                              CancellationToken);

                              var result = tool.ResultFor(counter);
                              var reply  = tool.ReplyFor(counter);

                              if (result is not null && result.ErrorNumber > 0)
                              {
                                  step.Observe($"rejected with errorNumber {result.ErrorNumber}");
                                  return;
                              }

                              step.Require(reply is not null || result is null,
                                           $"the device answered \"{announced}\" with something which is neither a " +
                                           $"rejection nor a reply");

                              step.Tolerate(reply is not null
                                                ? $"accepted \"{announced}\" and replied"
                                                : $"neither answered nor rejected \"{announced}\"");

                          });

            }

        }

    }

    #endregion

}
