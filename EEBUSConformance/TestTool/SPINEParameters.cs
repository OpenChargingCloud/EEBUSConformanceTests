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

using cloud.charging.open.protocols.EEBUS.SPINE;
using cloud.charging.open.protocols.EEBUS.SPINE.Model;

#endregion

namespace cloud.charging.open.protocols.EEBUS.Conformance
{

    /// <summary>
    /// The PAR_ blocks of the SPINE test specification, chapter 2.5.7: the
    /// datagrams the test tool sends during the test steps.
    ///
    /// All of them inherit the header defaults of PAR_default - the
    /// specification version, an ascending message counter, and the implicit
    /// address routing of chapter 2.5.5 - which is what <see cref="Datagram"/>
    /// fills in, so each block below states only what it deviates in.
    /// </summary>
    public static class SPINEParameters
    {

        #region Datagram(...)

        /// <summary>
        /// A datagram with the header defaults of PAR_default filled in.
        /// </summary>
        /// <param name="Source">Where it comes from.</param>
        /// <param name="Destination">Where it goes.</param>
        /// <param name="Classifier">What kind of message it is.</param>
        /// <param name="MsgCounter">Its message counter.</param>
        /// <param name="Cmd">What it carries.</param>
        /// <param name="SpecificationVersion">Which SPINE version it announces.</param>
        /// <param name="AckRequest">Whether an acknowledgement is asked for.</param>
        /// <param name="MsgCounterReference">Which message it answers.</param>
        public static DatagramType Datagram(FeatureAddressType  Source,
                                            FeatureAddressType  Destination,
                                            CmdClassifierType   Classifier,
                                            UInt64              MsgCounter,
                                            CmdType             Cmd,
                                            String              SpecificationVersion   = "1.3.0",
                                            Boolean?            AckRequest             = null,
                                            UInt64?             MsgCounterReference    = null)

            => new () {
                   Header   = new HeaderType {
                                  SpecificationVersion  = SpecificationVersion,
                                  AddressSource         = Source,
                                  AddressDestination    = Destination,
                                  MsgCounter            = MsgCounter,
                                  MsgCounterReference   = MsgCounterReference,
                                  CmdClassifier         = Classifier,
                                  AckRequest            = AckRequest
                              },
                   Payload  = new PayloadType {
                                  Cmd = [ Cmd ]
                              }
               };

        #endregion

        #region ReadCmd(Function) / EmptyCmd(Function)

        /// <summary>
        /// The command of a read: the function with an empty payload
        /// (SPINE 1.3.0, 5.3.4.4).
        /// </summary>
        /// <param name="Function">The name of a SPINE function.</param>
        public static CmdType ReadCmd(String Function)
        {

            var info  = SPINEFunctions.Get(Function)
                            ?? throw new ArgumentException($"'{Function}' is not a function of SPINE {SPINE.Version.String}.",
                                                           nameof(Function));

            var cmd   = new CmdType();

            cmd.SetData(Function, Activator.CreateInstance(info.DataType));

            return cmd;

        }

        #endregion


        #region PAR_readUnknownFunction

        /// <summary>
        /// PAR_readUnknownFunction: a read of a function nobody has ever heard
        /// of, aimed at the one feature which certainly exists.
        ///
        /// The generated command carries the random function name directly,
        /// because no model type can be created for a function which does not
        /// exist - which is the whole point of the case.
        /// </summary>
        /// <param name="Source">Where it comes from.</param>
        /// <param name="Destination">The primary node management feature of the device.</param>
        /// <param name="MsgCounter">Its message counter.</param>
        public static DatagramType ReadUnknownFunction(FeatureAddressType  Source,
                                                       FeatureAddressType  Destination,
                                                       UInt64              MsgCounter)
        {

            var cmd = new CmdType();

            cmd.SetData(RandomName(20), new Object());

            return Datagram(Source, Destination, CmdClassifierType.Read, MsgCounter, cmd,
                            SpecificationVersion: "1.999.999");

        }

        #endregion

        #region PAR_nmDiscoveryFuture / PAR_readAckRequestFalse / PAR_readAckRequestTrue / PAR_readInvalidVersionFormat

        /// <summary>
        /// PAR_nmDiscoveryFuture: a perfectly ordinary detailed discovery read
        /// announcing a version from the future.
        /// </summary>
        public static DatagramType NmDiscoveryFuture(FeatureAddressType  Source,
                                                     FeatureAddressType  Destination,
                                                     UInt64              MsgCounter)

            => Datagram(Source, Destination, CmdClassifierType.Read, MsgCounter,
                        ReadCmd(SPINENodeManagement.DetailedDiscoveryData),
                        SpecificationVersion: "1.999.999");


        /// <summary>
        /// PAR_readAckRequestFalse / PAR_readAckRequestTrue: the same read, once
        /// with and once without an acknowledgement request, which a read has to
        /// ignore either way.
        /// </summary>
        public static DatagramType ReadDetailedDiscovery(FeatureAddressType  Source,
                                                         FeatureAddressType  Destination,
                                                         UInt64              MsgCounter,
                                                         Boolean?            AckRequest             = null,
                                                         String              SpecificationVersion   = "1.3.0")

            => Datagram(Source, Destination, CmdClassifierType.Read, MsgCounter,
                        ReadCmd(SPINENodeManagement.DetailedDiscoveryData),
                        SpecificationVersion:  SpecificationVersion,
                        AckRequest:            AckRequest);


        /// <summary>
        /// A use case discovery read.
        /// </summary>
        public static DatagramType ReadUseCaseData(FeatureAddressType  Source,
                                                   FeatureAddressType  Destination,
                                                   UInt64              MsgCounter)

            => Datagram(Source, Destination, CmdClassifierType.Read, MsgCounter,
                        ReadCmd(SPINENodeManagement.UseCaseData));

        #endregion

        #region PAR_resultAck

        /// <summary>
        /// PAR_resultAck: a result which asks to be acknowledged - which no
        /// result may ever be, because two devices acknowledging each other's
        /// acknowledgements never stop.
        /// </summary>
        public static DatagramType ResultAck(FeatureAddressType  Source,
                                             FeatureAddressType  Destination,
                                             UInt64              MsgCounter,
                                             UInt64?             MsgCounterReference)
        {

            var cmd = new CmdType();

            cmd.SetData("resultData", new ResultDataType { ErrorNumber = 0 });

            return Datagram(Source, Destination, CmdClassifierType.Result, MsgCounter, cmd,
                            AckRequest:           true,
                            MsgCounterReference:  MsgCounterReference);

        }

        #endregion

        #region PAR_fcDestE1SourceE0

        /// <summary>
        /// PAR_fcDestE1SourceE0: a node management call aimed at entity 1,
        /// feature 0 - which is not the primary node management feature and
        /// therefore not somewhere node management may be spoken.
        /// </summary>
        public static DatagramType SubscriptionToWrongDestination(FeatureAddressType  Source,
                                                                  String              DUTDeviceAddress,
                                                                  UInt64              MsgCounter,
                                                                  String              ToolDeviceAddress)
        {

            var cmd = new CmdType();

            cmd.SetData(SPINENodeManagement.SubscriptionRequestCall,
                        new NodeManagementSubscriptionRequestCallType {
                            SubscriptionRequest = new SubscriptionManagementRequestCallType {
                                                      ClientAddress  = new FeatureAddressType { Device = ToolDeviceAddress, Entity = [ 0 ], Feature = 0 },
                                                      ServerAddress  = new FeatureAddressType { Device = DUTDeviceAddress,  Entity = [ 0 ], Feature = 0 }
                                                  }
                        });

            return Datagram(Source,
                            new FeatureAddressType { Device = DUTDeviceAddress, Entity = [ 1 ], Feature = 0 },
                            CmdClassifierType.Call,
                            MsgCounter,
                            cmd);

        }

        #endregion

        #region PAR_bindingNm / PAR_subscriptionNm / PAR_deleteSubscriptionNm

        /// <summary>
        /// PAR_bindingNm: a binding request to the primary node management
        /// feature, which is the one binding nobody may have.
        /// </summary>
        public static DatagramType BindingToNodeManagement(FeatureAddressType  Source,
                                                           FeatureAddressType  Destination,
                                                           UInt64              MsgCounter)
        {

            var cmd = new CmdType();

            cmd.SetData(SPINENodeManagement.BindingRequestCall,
                        new NodeManagementBindingRequestCallType {
                            BindingRequest = new BindingManagementRequestCallType {
                                                 ClientAddress  = Source,
                                                 ServerAddress  = Destination
                                             }
                        });

            return Datagram(Source, Destination, CmdClassifierType.Call, MsgCounter, cmd, AckRequest: true);

        }


        /// <summary>
        /// PAR_subscriptionNm: a subscription to the primary node management
        /// feature, which - unlike the binding - is exactly what everybody does.
        /// </summary>
        public static DatagramType SubscriptionToNodeManagement(FeatureAddressType  Source,
                                                                FeatureAddressType  Destination,
                                                                UInt64              MsgCounter)
        {

            var cmd = new CmdType();

            cmd.SetData(SPINENodeManagement.SubscriptionRequestCall,
                        new NodeManagementSubscriptionRequestCallType {
                            SubscriptionRequest = new SubscriptionManagementRequestCallType {
                                                      ClientAddress  = Source,
                                                      ServerAddress  = Destination
                                                  }
                        });

            return Datagram(Source, Destination, CmdClassifierType.Call, MsgCounter, cmd, AckRequest: true);

        }


        /// <summary>
        /// PAR_deleteSubscriptionNm: giving it up again.
        /// </summary>
        public static DatagramType DeleteSubscriptionToNodeManagement(FeatureAddressType  Source,
                                                                      FeatureAddressType  Destination,
                                                                      UInt64              MsgCounter)
        {

            var cmd = new CmdType();

            cmd.SetData(SPINENodeManagement.SubscriptionDeleteCall,
                        new NodeManagementSubscriptionDeleteCallType {
                            SubscriptionDelete = new SubscriptionManagementDeleteCallType {
                                                     ClientAddress  = Source,
                                                     ServerAddress  = Destination
                                                 }
                                             });

            return Datagram(Source, Destination, CmdClassifierType.Call, MsgCounter, cmd, AckRequest: true);

        }


        /// <summary>
        /// A read of the binding respectively subscription table, to check what
        /// the device actually stored.
        /// </summary>
        public static DatagramType ReadRelations(FeatureAddressType  Source,
                                                 FeatureAddressType  Destination,
                                                 UInt64              MsgCounter,
                                                 Boolean             Bindings)

            => Datagram(Source, Destination, CmdClassifierType.Read, MsgCounter,
                        ReadCmd(Bindings
                                    ? SPINENodeManagement.BindingData
                                    : SPINENodeManagement.SubscriptionData));

        #endregion

        #region PAR_notifyAck

        /// <summary>
        /// PAR_notifyAck: a heartbeat notification which asks to be
        /// acknowledged.
        /// </summary>
        public static DatagramType NotifyHeartbeat(FeatureAddressType  Source,
                                                   FeatureAddressType  Destination,
                                                   UInt64              MsgCounter,
                                                   DateTimeOffset      Timestamp,
                                                   UInt64              HeartbeatCounter   = 1,
                                                   String              SpecificationVersion = "1.3.0")
        {

            var cmd = new CmdType();

            cmd.SetData("deviceDiagnosisHeartbeatData",
                        new DeviceDiagnosisHeartbeatDataType {
                            Timestamp          = AbsoluteOrRelativeTimeType.Parse(Timestamp.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ss.fZ")),
                            HeartbeatCounter   = HeartbeatCounter,
                            HeartbeatTimeout   = DurationType.Parse("PT120S")
                        });

            return Datagram(Source, Destination, CmdClassifierType.Notify, MsgCounter, cmd,
                            SpecificationVersion:  SpecificationVersion,
                            AckRequest:            true);

        }

        #endregion

        #region PAR_notifyDiscoveryModifiedAck

        /// <summary>
        /// PAR_notifyDiscoveryModifiedAck: a partial detailed discovery notify
        /// which asks to be acknowledged.
        /// </summary>
        public static DatagramType NotifyDiscoveryModified(FeatureAddressType  Source,
                                                           FeatureAddressType  Destination,
                                                           UInt64              MsgCounter,
                                                           String              ToolDeviceAddress)
        {

            var cmd = new CmdType();

            cmd.SetData(SPINENodeManagement.DetailedDiscoveryData,
                        new NodeManagementDetailedDiscoveryDataType {
                            EntityInformation = [
                                new NodeManagementDetailedDiscoveryEntityInformationType {
                                    Description = new NetworkManagementEntityDescriptionDataType {
                                                      EntityAddress    = new EntityAddressType { Device = ToolDeviceAddress, Entity = [ 0 ] },
                                                      LastStateChange  = NetworkManagementStateChangeType.Modified
                                                  }
                                }
                            ]
                        });

            cmd.Function  = FunctionType.Parse(SPINENodeManagement.DetailedDiscoveryData);
            cmd.Filter    = [ new FilterType { CmdControl = CmdControlType.ForPartial } ];

            return Datagram(Source, Destination, CmdClassifierType.Notify, MsgCounter, cmd, AckRequest: true);

        }

        #endregion


        #region RandomName(Length)

        /// <summary>
        /// ${SYS.Unknown_Element_Tag} and the random function name of
        /// PAR_readUnknownFunction: letters only, so that the result cannot
        /// collide with anything the specification might introduce later.
        /// </summary>
        /// <param name="Length">How many characters.</param>
        public static String RandomName(Int32 Length)
        {

            const String alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

            return new String(Enumerable.Range(0, Length).
                                  Select(_ => alphabet[Random.Shared.Next(alphabet.Length)]).
                                  ToArray());

        }

        #endregion

    }

}
