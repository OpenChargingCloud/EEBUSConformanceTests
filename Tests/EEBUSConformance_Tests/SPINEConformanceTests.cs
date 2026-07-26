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

using NUnit.Framework;

#endregion

namespace cloud.charging.open.protocols.EEBUS.Conformance.tests
{

    /// <summary>
    /// The thirty-one test cases of EEBus_SPINE_TestSpecification_V1.0.0.
    /// </summary>
    [TestFixture]
    public class SPINEConformanceTests : AConformanceFixture
    {

        [Test] [Property("TC", "TC_SPINE_COMP_001")]    public Task TC_SPINE_COMP_001_RejectUnknownFunction()               => Conform("TC_SPINE_COMP_001");
        [Test] [Property("TC", "TC_SPINE_COMP_002")]    public Task TC_SPINE_COMP_002_ForwardVersionCompatibility()         => Conform("TC_SPINE_COMP_002");
        [Test] [Property("TC", "TC_SPINE_COMP_003")]    public Task TC_SPINE_COMP_003_IgnoreUnknownElements()               => Conform("TC_SPINE_COMP_003");
        [Test] [Property("TC", "TC_SPINE_COMP_004")]    public Task TC_SPINE_COMP_004_IgnoreInvalidReplies()                => Conform("TC_SPINE_COMP_004");
        [Test] [Property("TC", "TC_SPINE_COMP_005")]    public Task TC_SPINE_COMP_005_StrictVersionFormatting()             => Conform("TC_SPINE_COMP_005");
        [Test] [Property("TC", "TC_SPINE_COMP_006")]    public Task TC_SPINE_COMP_006_VersionFormatVariations()             => Conform("TC_SPINE_COMP_006");

        [Test] [Property("TC", "TC_SPINE_DATA_001")]    public Task TC_SPINE_DATA_001_AscendingMessageCounters()            => Conform("TC_SPINE_DATA_001");
        [Test] [Property("TC", "TC_SPINE_DATA_002")]    public Task TC_SPINE_DATA_002_AllowSkippedCounters()                => Conform("TC_SPINE_DATA_002");
        [Test] [Property("TC", "TC_SPINE_DATA_003")]    public Task TC_SPINE_DATA_003_HandleCounterResetOrOverflow()        => Conform("TC_SPINE_DATA_003");
        [Test] [Property("TC", "TC_SPINE_DATA_004")]    public Task TC_SPINE_DATA_004_MatchReferenceCounter()               => Conform("TC_SPINE_DATA_004");
        [Test] [Property("TC", "TC_SPINE_DATA_005")]    public Task TC_SPINE_DATA_005_AcknowledgeNotifyDatagrams()          => Conform("TC_SPINE_DATA_005");
        [Test] [Property("TC", "TC_SPINE_DATA_006")]    public Task TC_SPINE_DATA_006_NoResponseToResults()                 => Conform("TC_SPINE_DATA_006");
        [Test] [Property("TC", "TC_SPINE_DATA_007")]    public Task TC_SPINE_DATA_007_IgnoreAckRequestInRead()              => Conform("TC_SPINE_DATA_007");
        [Test] [Property("TC", "TC_SPINE_DATA_008")]    public Task TC_SPINE_DATA_008_NoResponseToResultsDUTInitiated()     => Conform("TC_SPINE_DATA_008");

        [Test] [Property("TC", "TC_SPINE_FC_001")]      public Task TC_SPINE_FC_001_RejectNonPrimaryDestinations()          => Conform("TC_SPINE_FC_001");

        [Test] [Property("TC", "TC_SPINE_DDISC_001")]   public Task TC_SPINE_DDISC_001_InitialDiscoveryOfUnknownPartner()   => Conform("TC_SPINE_DDISC_001");
        [Test] [Property("TC", "TC_SPINE_DDISC_002")]   public Task TC_SPINE_DDISC_002_DisconnectUncommunicativePartner()   => Conform("TC_SPINE_DDISC_002");

        [Test] [Property("TC", "TC_SPINE_BIND_001")]    public Task TC_SPINE_BIND_001_DenyNodeManagementBinding()           => Conform("TC_SPINE_BIND_001");
        [Test] [Property("TC", "TC_SPINE_BIND_002")]    public Task TC_SPINE_BIND_002_RejectUnboundWrite()                  => Conform("TC_SPINE_BIND_002");

        [Test] [Property("TC", "TC_SPINE_SUBS_001")]    public Task TC_SPINE_SUBS_001_AcceptNodeManagementSubscription()    => Conform("TC_SPINE_SUBS_001");
        [Test] [Property("TC", "TC_SPINE_SUBS_002")]    public Task TC_SPINE_SUBS_002_IdempotentSubscriptionDeletion()      => Conform("TC_SPINE_SUBS_002");

        [Test] [Property("TC", "TC_SPINE_ENTITY_001")]  public Task TC_SPINE_ENTITY_001_DynamicServerDiscovery()            => Conform("TC_SPINE_ENTITY_001");
        [Test] [Property("TC", "TC_SPINE_ENTITY_002")]  public Task TC_SPINE_ENTITY_002_DynamicServerSubscription()         => Conform("TC_SPINE_ENTITY_002");

        [Test] [Property("TC", "TC_SPINE_RTS_001")]     public Task TC_SPINE_RTS_001_TolerateArbitraryClientFeatureTypes()  => Conform("TC_SPINE_RTS_001");
        [Test] [Property("TC", "TC_SPINE_RTS_002")]     public Task TC_SPINE_RTS_002_TolerateUnknownFeatureTypes()          => Conform("TC_SPINE_RTS_002");
        [Test] [Property("TC", "TC_SPINE_RTS_003")]     public Task TC_SPINE_RTS_003_DeduceClientFromBindingRequest()       => Conform("TC_SPINE_RTS_003");
        [Test] [Property("TC", "TC_SPINE_RTS_004")]     public Task TC_SPINE_RTS_004_IgnoreUnknownPayloadElements()         => Conform("TC_SPINE_RTS_004");
        [Test] [Property("TC", "TC_SPINE_RTS_005")]     public Task TC_SPINE_RTS_005_ApplyRFEMergeLogicScaledNumberType()   => Conform("TC_SPINE_RTS_005");

        [Test] [Property("TC", "TC_SPINE_RTC_001")]     public Task TC_SPINE_RTC_001_IgnoreExtraServerElements()            => Conform("TC_SPINE_RTC_001");
        [Test] [Property("TC", "TC_SPINE_RTC_002")]     public Task TC_SPINE_RTC_002_IgnoreUnknownServerElements()          => Conform("TC_SPINE_RTC_002");
        [Test] [Property("TC", "TC_SPINE_RTC_003")]     public Task TC_SPINE_RTC_003_IgnoreUseCaseAvailableFlag()           => Conform("TC_SPINE_RTC_003");

    }

}
