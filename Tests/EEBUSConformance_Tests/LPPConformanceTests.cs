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
    /// The 51 abstract test cases of the "Limitation of Power Production"
    /// high level test specification, run against this stack.
    ///
    /// The identifiers are the official ones of EEBus_UC_HighLevel_TestSpecification_LPP_V1.0.2,
    /// carried twice - as the method name and as the property "TC" - so that a
    /// test runner, a CI log and a certification report all say the same thing
    /// about the same case.
    /// </summary>
    [TestFixture]
    public class LPPConformanceTests : AConformanceFixture
    {

        [Test] [Property("ATC", "ATC_LPP_COM_PT_EGHeartbeat_001")]
        public Task ATC_LPP_COM_PT_EGHeartbeat_001() => Conform("ATC_LPP_COM_PT_EGHeartbeat_001");

        [Test] [Property("ATC", "ATC_LPP_COM_PT_EGConnection_001")]
        public Task ATC_LPP_COM_PT_EGConnection_001() => Conform("ATC_LPP_COM_PT_EGConnection_001");

        [Test] [Property("ATC", "ATC_LPP_COM_PT_EGConnection_002")]
        public Task ATC_LPP_COM_PT_EGConnection_002() => Conform("ATC_LPP_COM_PT_EGConnection_002");

        [Test] [Property("ATC", "ATC_LPP_COM_PT_EGConnection_003")]
        public Task ATC_LPP_COM_PT_EGConnection_003() => Conform("ATC_LPP_COM_PT_EGConnection_003");

        [Test] [Property("ATC", "ATC_LPP_COM_PT_EGMessages_001")]
        public Task ATC_LPP_COM_PT_EGMessages_001() => Conform("ATC_LPP_COM_PT_EGMessages_001");

        [Test] [Property("ATC", "ATC_LPP_COM_PT_EGMessages_002")]
        public Task ATC_LPP_COM_PT_EGMessages_002() => Conform("ATC_LPP_COM_PT_EGMessages_002");

        [Test] [Property("ATC", "ATC_LPP_COM_PT_EGMessages_003")]
        public Task ATC_LPP_COM_PT_EGMessages_003() => Conform("ATC_LPP_COM_PT_EGMessages_003");

        [Test] [Property("ATC", "ATC_LPP_COM_PT_EGMessages_004")]
        public Task ATC_LPP_COM_PT_EGMessages_004() => Conform("ATC_LPP_COM_PT_EGMessages_004");

        [Test] [Property("ATC", "ATC_LPP_COM_PT_CSHeartbeat_001")]
        public Task ATC_LPP_COM_PT_CSHeartbeat_001() => Conform("ATC_LPP_COM_PT_CSHeartbeat_001");

        [Test] [Property("ATC", "ATC_LPP_COM_NT_CSConnection_001")]
        public Task ATC_LPP_COM_NT_CSConnection_001() => Conform("ATC_LPP_COM_NT_CSConnection_001");

        [Test] [Property("ATC", "ATC_LPP_COM_PT_CSConnection_002")]
        public Task ATC_LPP_COM_PT_CSConnection_002() => Conform("ATC_LPP_COM_PT_CSConnection_002");

        [Test] [Property("ATC", "ATC_LPP_COM_PT_CSConnection_003")]
        public Task ATC_LPP_COM_PT_CSConnection_003() => Conform("ATC_LPP_COM_PT_CSConnection_003");

        [Test] [Property("ATC", "ATC_LPP_COM_PT_CSConnection_004")]
        public Task ATC_LPP_COM_PT_CSConnection_004() => Conform("ATC_LPP_COM_PT_CSConnection_004");

        [Test] [Property("ATC", "ATC_LPP_COM_PT_CSConnection_005")]
        public Task ATC_LPP_COM_PT_CSConnection_005() => Conform("ATC_LPP_COM_PT_CSConnection_005");

        [Test] [Property("ATC", "ATC_LPP_COM_PT_CSConnection_006")]
        public Task ATC_LPP_COM_PT_CSConnection_006() => Conform("ATC_LPP_COM_PT_CSConnection_006");

        [Test] [Property("ATC", "ATC_LPP_COM_PT_CSConnection_007")]
        public Task ATC_LPP_COM_PT_CSConnection_007() => Conform("ATC_LPP_COM_PT_CSConnection_007");

        [Test] [Property("ATC", "ATC_LPP_COM_PT_CSConnection_008")]
        public Task ATC_LPP_COM_PT_CSConnection_008() => Conform("ATC_LPP_COM_PT_CSConnection_008");

        [Test] [Property("ATC", "ATC_LPP_COM_PT_CSConnection_009")]
        public Task ATC_LPP_COM_PT_CSConnection_009() => Conform("ATC_LPP_COM_PT_CSConnection_009");

        [Test] [Property("ATC", "ATC_LPP_COM_PT_CSInit_001")]
        public Task ATC_LPP_COM_PT_CSInit_001() => Conform("ATC_LPP_COM_PT_CSInit_001");

        [Test] [Property("ATC", "ATC_LPP_COM_PT_CSInit_002")]
        public Task ATC_LPP_COM_PT_CSInit_002() => Conform("ATC_LPP_COM_PT_CSInit_002");

        [Test] [Property("ATC", "ATC_LPP_COM_PT_CSInit_003")]
        public Task ATC_LPP_COM_PT_CSInit_003() => Conform("ATC_LPP_COM_PT_CSInit_003");

        [Test] [Property("ATC", "ATC_LPP_COM_NT_CSLimited_001")]
        public Task ATC_LPP_COM_NT_CSLimited_001() => Conform("ATC_LPP_COM_NT_CSLimited_001");

        [Test] [Property("ATC", "ATC_LPP_COM_PT_CSLimited_002")]
        public Task ATC_LPP_COM_PT_CSLimited_002() => Conform("ATC_LPP_COM_PT_CSLimited_002");

        [Test] [Property("ATC", "ATC_LPP_COM_NT_CSUnlCntrl_001")]
        public Task ATC_LPP_COM_NT_CSUnlCntrl_001() => Conform("ATC_LPP_COM_NT_CSUnlCntrl_001");

        [Test] [Property("ATC", "ATC_LPP_COM_PT_CSUnlCntrl_002")]
        public Task ATC_LPP_COM_PT_CSUnlCntrl_002() => Conform("ATC_LPP_COM_PT_CSUnlCntrl_002");

        [Test] [Property("ATC", "ATC_LPP_COM_PT_CSUnlCntrl_003")]
        public Task ATC_LPP_COM_PT_CSUnlCntrl_003() => Conform("ATC_LPP_COM_PT_CSUnlCntrl_003");

        [Test] [Property("ATC", "ATC_LPP_COM_PT_CSFS_001")]
        public Task ATC_LPP_COM_PT_CSFS_001() => Conform("ATC_LPP_COM_PT_CSFS_001");

        [Test] [Property("ATC", "ATC_LPP_COM_PT_CSFS_002")]
        public Task ATC_LPP_COM_PT_CSFS_002() => Conform("ATC_LPP_COM_PT_CSFS_002");

        [Test] [Property("ATC", "ATC_LPP_COM_PT_CSFS_003")]
        public Task ATC_LPP_COM_PT_CSFS_003() => Conform("ATC_LPP_COM_PT_CSFS_003");

        [Test] [Property("ATC", "ATC_LPP_COM_NT_CSUnlAuto_001")]
        public Task ATC_LPP_COM_NT_CSUnlAuto_001() => Conform("ATC_LPP_COM_NT_CSUnlAuto_001");

        [Test] [Property("ATC", "ATC_LPP_COM_PT_CSUnlAuto_002")]
        public Task ATC_LPP_COM_PT_CSUnlAuto_002() => Conform("ATC_LPP_COM_PT_CSUnlAuto_002");

        [Test] [Property("ATC", "ATC_LPP_COM_PT_CSTransition1_001")]
        public Task ATC_LPP_COM_PT_CSTransition1_001() => Conform("ATC_LPP_COM_PT_CSTransition1_001");

        [Test] [Property("ATC", "ATC_LPP_COM_PT_CSTransition1_002")]
        public Task ATC_LPP_COM_PT_CSTransition1_002() => Conform("ATC_LPP_COM_PT_CSTransition1_002");

        [Test] [Property("ATC", "ATC_LPP_COM_PT_CSTransition2_001")]
        public Task ATC_LPP_COM_PT_CSTransition2_001() => Conform("ATC_LPP_COM_PT_CSTransition2_001");

        [Test] [Property("ATC", "ATC_LPP_COM_PT_CSTransition3_001")]
        public Task ATC_LPP_COM_PT_CSTransition3_001() => Conform("ATC_LPP_COM_PT_CSTransition3_001");

        [Test] [Property("ATC", "ATC_LPP_COM_PT_CSTransition3_002")]
        public Task ATC_LPP_COM_PT_CSTransition3_002() => Conform("ATC_LPP_COM_PT_CSTransition3_002");

        [Test] [Property("ATC", "ATC_LPP_COM_PT_CSTransition4_001")]
        public Task ATC_LPP_COM_PT_CSTransition4_001() => Conform("ATC_LPP_COM_PT_CSTransition4_001");

        [Test] [Property("ATC", "ATC_LPP_COM_PT_CSTransition5_001")]
        public Task ATC_LPP_COM_PT_CSTransition5_001() => Conform("ATC_LPP_COM_PT_CSTransition5_001");

        [Test] [Property("ATC", "ATC_LPP_COM_PT_CSTransition6_001")]
        public Task ATC_LPP_COM_PT_CSTransition6_001() => Conform("ATC_LPP_COM_PT_CSTransition6_001");

        [Test] [Property("ATC", "ATC_LPP_COM_PT_CSTransition6_002")]
        public Task ATC_LPP_COM_PT_CSTransition6_002() => Conform("ATC_LPP_COM_PT_CSTransition6_002");

        [Test] [Property("ATC", "ATC_LPP_COM_PT_CSTransition7_001")]
        public Task ATC_LPP_COM_PT_CSTransition7_001() => Conform("ATC_LPP_COM_PT_CSTransition7_001");

        [Test] [Property("ATC", "ATC_LPP_COM_PT_CSTransition8_001")]
        public Task ATC_LPP_COM_PT_CSTransition8_001() => Conform("ATC_LPP_COM_PT_CSTransition8_001");

        [Test] [Property("ATC", "ATC_LPP_COM_PT_CSTransition8_002")]
        public Task ATC_LPP_COM_PT_CSTransition8_002() => Conform("ATC_LPP_COM_PT_CSTransition8_002");

        [Test] [Property("ATC", "ATC_LPP_COM_PT_CSTransition9_001")]
        public Task ATC_LPP_COM_PT_CSTransition9_001() => Conform("ATC_LPP_COM_PT_CSTransition9_001");

        [Test] [Property("ATC", "ATC_LPP_COM_PT_CSTransition10_001")]
        public Task ATC_LPP_COM_PT_CSTransition10_001() => Conform("ATC_LPP_COM_PT_CSTransition10_001");

        [Test] [Property("ATC", "ATC_LPP_COM_PT_CSTransition10_002")]
        public Task ATC_LPP_COM_PT_CSTransition10_002() => Conform("ATC_LPP_COM_PT_CSTransition10_002");

        [Test] [Property("ATC", "ATC_LPP_COM_PT_CSTransition11_001")]
        public Task ATC_LPP_COM_PT_CSTransition11_001() => Conform("ATC_LPP_COM_PT_CSTransition11_001");

        [Test] [Property("ATC", "ATC_LPP_COM_PT_CSTransition11_002")]
        public Task ATC_LPP_COM_PT_CSTransition11_002() => Conform("ATC_LPP_COM_PT_CSTransition11_002");

        [Test] [Property("ATC", "ATC_LPP_COM_PT_CSTransition12_001")]
        public Task ATC_LPP_COM_PT_CSTransition12_001() => Conform("ATC_LPP_COM_PT_CSTransition12_001");

        [Test] [Property("ATC", "ATC_LPP_INS1_PT_CSTransition1_001")]
        public Task ATC_LPP_INS1_PT_CSTransition1_001() => Conform("ATC_LPP_INS1_PT_CSTransition1_001");

        [Test] [Property("ATC", "ATC_LPP_INS2_PT_CSTransition1_001")]
        public Task ATC_LPP_INS2_PT_CSTransition1_001() => Conform("ATC_LPP_INS2_PT_CSTransition1_001");

    }

}
