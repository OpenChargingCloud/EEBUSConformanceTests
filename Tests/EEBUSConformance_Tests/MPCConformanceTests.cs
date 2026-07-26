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
    /// The 54 abstract test cases of the "Monitoring of Power Consumption"
    /// high level test specification, run against this stack.
    ///
    /// The identifiers are the official ones of EEBus_UC_HighLevel_TestSpecification_MPC_V1.0.2,
    /// carried twice - as the method name and as the property "TC" - so that a
    /// test runner, a CI log and a certification report all say the same thing
    /// about the same case.
    /// </summary>
    [TestFixture]
    public class MPCConformanceTests : AConformanceFixture
    {

        [Test] [Property("ATC", "ATC_MPC_COM_PT_MUPolling_001")]
        public Task ATC_MPC_COM_PT_MUPolling_001() => Conform("ATC_MPC_COM_PT_MUPolling_001");

        [Test] [Property("ATC", "ATC_MPC_COM_PT_MUNotification_001")]
        public Task ATC_MPC_COM_PT_MUNotification_001() => Conform("ATC_MPC_COM_PT_MUNotification_001");

        [Test] [Property("ATC", "ATC_MPC_SCE1_PT_MUTotalActivePower_001")]
        public Task ATC_MPC_SCE1_PT_MUTotalActivePower_001() => Conform("ATC_MPC_SCE1_PT_MUTotalActivePower_001");

        [Test] [Property("ATC", "ATC_MPC_SCE1_PT_MUPhaseActivePower_001")]
        public Task ATC_MPC_SCE1_PT_MUPhaseActivePower_001() => Conform("ATC_MPC_SCE1_PT_MUPhaseActivePower_001");

        [Test] [Property("ATC", "ATC_MPC_SCE1_PT_MUPhaseActivePower_002")]
        public Task ATC_MPC_SCE1_PT_MUPhaseActivePower_002() => Conform("ATC_MPC_SCE1_PT_MUPhaseActivePower_002");

        [Test] [Property("ATC", "ATC_MPC_SCE1_PT_MUPhaseActivePower_003")]
        public Task ATC_MPC_SCE1_PT_MUPhaseActivePower_003() => Conform("ATC_MPC_SCE1_PT_MUPhaseActivePower_003");

        [Test] [Property("ATC", "ATC_MPC_SCE2_PT_MUTotalConsumedEnergy_001")]
        public Task ATC_MPC_SCE2_PT_MUTotalConsumedEnergy_001() => Conform("ATC_MPC_SCE2_PT_MUTotalConsumedEnergy_001");

        [Test] [Property("ATC", "ATC_MPC_SCE2_PT_MUTotalConsumedEnergy_002")]
        public Task ATC_MPC_SCE2_PT_MUTotalConsumedEnergy_002() => Conform("ATC_MPC_SCE2_PT_MUTotalConsumedEnergy_002");

        [Test] [Property("ATC", "ATC_MPC_SCE2_PT_MUTotalProducedEnergy_001")]
        public Task ATC_MPC_SCE2_PT_MUTotalProducedEnergy_001() => Conform("ATC_MPC_SCE2_PT_MUTotalProducedEnergy_001");

        [Test] [Property("ATC", "ATC_MPC_SCE2_PT_MUTotalProducedEnergy_002")]
        public Task ATC_MPC_SCE2_PT_MUTotalProducedEnergy_002() => Conform("ATC_MPC_SCE2_PT_MUTotalProducedEnergy_002");

        [Test] [Property("ATC", "ATC_MPC_SCE3_PT_MUActiveACCurrent_001")]
        public Task ATC_MPC_SCE3_PT_MUActiveACCurrent_001() => Conform("ATC_MPC_SCE3_PT_MUActiveACCurrent_001");

        [Test] [Property("ATC", "ATC_MPC_SCE3_PT_MUActiveACCurrent_002")]
        public Task ATC_MPC_SCE3_PT_MUActiveACCurrent_002() => Conform("ATC_MPC_SCE3_PT_MUActiveACCurrent_002");

        [Test] [Property("ATC", "ATC_MPC_SCE3_PT_MUActiveACCurrent_003")]
        public Task ATC_MPC_SCE3_PT_MUActiveACCurrent_003() => Conform("ATC_MPC_SCE3_PT_MUActiveACCurrent_003");

        [Test] [Property("ATC", "ATC_MPC_SCE4_PT_MUACVoltage_001")]
        public Task ATC_MPC_SCE4_PT_MUACVoltage_001() => Conform("ATC_MPC_SCE4_PT_MUACVoltage_001");

        [Test] [Property("ATC", "ATC_MPC_SCE4_PT_MUACVoltage_002")]
        public Task ATC_MPC_SCE4_PT_MUACVoltage_002() => Conform("ATC_MPC_SCE4_PT_MUACVoltage_002");

        [Test] [Property("ATC", "ATC_MPC_SCE4_PT_MUACVoltage_003")]
        public Task ATC_MPC_SCE4_PT_MUACVoltage_003() => Conform("ATC_MPC_SCE4_PT_MUACVoltage_003");

        [Test] [Property("ATC", "ATC_MPC_SCE4_PT_MUACVoltage_004")]
        public Task ATC_MPC_SCE4_PT_MUACVoltage_004() => Conform("ATC_MPC_SCE4_PT_MUACVoltage_004");

        [Test] [Property("ATC", "ATC_MPC_SCE4_PT_MUACVoltage_005")]
        public Task ATC_MPC_SCE4_PT_MUACVoltage_005() => Conform("ATC_MPC_SCE4_PT_MUACVoltage_005");

        [Test] [Property("ATC", "ATC_MPC_SCE4_PT_MUACVoltage_006")]
        public Task ATC_MPC_SCE4_PT_MUACVoltage_006() => Conform("ATC_MPC_SCE4_PT_MUACVoltage_006");

        [Test] [Property("ATC", "ATC_MPC_SCE5_PT_MUFrequency_001")]
        public Task ATC_MPC_SCE5_PT_MUFrequency_001() => Conform("ATC_MPC_SCE5_PT_MUFrequency_001");

        [Test] [Property("ATC", "ATC_MPC_COM_PT_MAPolling_001")]
        public Task ATC_MPC_COM_PT_MAPolling_001() => Conform("ATC_MPC_COM_PT_MAPolling_001");

        [Test] [Property("ATC", "ATC_MPC_COM_PT_MANotification_001")]
        public Task ATC_MPC_COM_PT_MANotification_001() => Conform("ATC_MPC_COM_PT_MANotification_001");

        [Test] [Property("ATC", "ATC_MPC_SCE1_PT_MATotalActivePower_001")]
        public Task ATC_MPC_SCE1_PT_MATotalActivePower_001() => Conform("ATC_MPC_SCE1_PT_MATotalActivePower_001");

        [Test] [Property("ATC", "ATC_MPC_SCE1_NT_MATotalActivePower_002")]
        public Task ATC_MPC_SCE1_NT_MATotalActivePower_002() => Conform("ATC_MPC_SCE1_NT_MATotalActivePower_002");

        [Test] [Property("ATC", "ATC_MPC_SCE1_PT_MAPhaseActivePower_001")]
        public Task ATC_MPC_SCE1_PT_MAPhaseActivePower_001() => Conform("ATC_MPC_SCE1_PT_MAPhaseActivePower_001");

        [Test] [Property("ATC", "ATC_MPC_SCE1_NT_MAPhaseActivePower_002")]
        public Task ATC_MPC_SCE1_NT_MAPhaseActivePower_002() => Conform("ATC_MPC_SCE1_NT_MAPhaseActivePower_002");

        [Test] [Property("ATC", "ATC_MPC_SCE1_PT_MAPhaseActivePower_003")]
        public Task ATC_MPC_SCE1_PT_MAPhaseActivePower_003() => Conform("ATC_MPC_SCE1_PT_MAPhaseActivePower_003");

        [Test] [Property("ATC", "ATC_MPC_SCE1_NT_MAPhaseActivePower_004")]
        public Task ATC_MPC_SCE1_NT_MAPhaseActivePower_004() => Conform("ATC_MPC_SCE1_NT_MAPhaseActivePower_004");

        [Test] [Property("ATC", "ATC_MPC_SCE1_PT_MAPhaseActivePower_005")]
        public Task ATC_MPC_SCE1_PT_MAPhaseActivePower_005() => Conform("ATC_MPC_SCE1_PT_MAPhaseActivePower_005");

        [Test] [Property("ATC", "ATC_MPC_SCE1_NT_MAPhaseActivePower_006")]
        public Task ATC_MPC_SCE1_NT_MAPhaseActivePower_006() => Conform("ATC_MPC_SCE1_NT_MAPhaseActivePower_006");

        [Test] [Property("ATC", "ATC_MPC_SCE2_PT_MATotalConsumedEnergy_001")]
        public Task ATC_MPC_SCE2_PT_MATotalConsumedEnergy_001() => Conform("ATC_MPC_SCE2_PT_MATotalConsumedEnergy_001");

        [Test] [Property("ATC", "ATC_MPC_SCE2_NT_MATotalConsumedEnergy_002")]
        public Task ATC_MPC_SCE2_NT_MATotalConsumedEnergy_002() => Conform("ATC_MPC_SCE2_NT_MATotalConsumedEnergy_002");

        [Test] [Property("ATC", "ATC_MPC_SCE2_PT_MATotalProducedEnergy_001")]
        public Task ATC_MPC_SCE2_PT_MATotalProducedEnergy_001() => Conform("ATC_MPC_SCE2_PT_MATotalProducedEnergy_001");

        [Test] [Property("ATC", "ATC_MPC_SCE2_NT_MATotalProducedEnergy_002")]
        public Task ATC_MPC_SCE2_NT_MATotalProducedEnergy_002() => Conform("ATC_MPC_SCE2_NT_MATotalProducedEnergy_002");

        [Test] [Property("ATC", "ATC_MPC_SCE3_PT_MAActiveACCurrent_001")]
        public Task ATC_MPC_SCE3_PT_MAActiveACCurrent_001() => Conform("ATC_MPC_SCE3_PT_MAActiveACCurrent_001");

        [Test] [Property("ATC", "ATC_MPC_SCE3_NT_MAActiveACCurrent_002")]
        public Task ATC_MPC_SCE3_NT_MAActiveACCurrent_002() => Conform("ATC_MPC_SCE3_NT_MAActiveACCurrent_002");

        [Test] [Property("ATC", "ATC_MPC_SCE3_PT_MAActiveACCurrent_003")]
        public Task ATC_MPC_SCE3_PT_MAActiveACCurrent_003() => Conform("ATC_MPC_SCE3_PT_MAActiveACCurrent_003");

        [Test] [Property("ATC", "ATC_MPC_SCE3_NT_MAActiveACCurrent_004")]
        public Task ATC_MPC_SCE3_NT_MAActiveACCurrent_004() => Conform("ATC_MPC_SCE3_NT_MAActiveACCurrent_004");

        [Test] [Property("ATC", "ATC_MPC_SCE3_PT_MAActiveACCurrent_005")]
        public Task ATC_MPC_SCE3_PT_MAActiveACCurrent_005() => Conform("ATC_MPC_SCE3_PT_MAActiveACCurrent_005");

        [Test] [Property("ATC", "ATC_MPC_SCE3_NT_MAActiveACCurrent_006")]
        public Task ATC_MPC_SCE3_NT_MAActiveACCurrent_006() => Conform("ATC_MPC_SCE3_NT_MAActiveACCurrent_006");

        [Test] [Property("ATC", "ATC_MPC_SCE4_PT_MAACVoltage_001")]
        public Task ATC_MPC_SCE4_PT_MAACVoltage_001() => Conform("ATC_MPC_SCE4_PT_MAACVoltage_001");

        [Test] [Property("ATC", "ATC_MPC_SCE4_NT_MAACVoltage_002")]
        public Task ATC_MPC_SCE4_NT_MAACVoltage_002() => Conform("ATC_MPC_SCE4_NT_MAACVoltage_002");

        [Test] [Property("ATC", "ATC_MPC_SCE4_PT_MAACVoltage_003")]
        public Task ATC_MPC_SCE4_PT_MAACVoltage_003() => Conform("ATC_MPC_SCE4_PT_MAACVoltage_003");

        [Test] [Property("ATC", "ATC_MPC_SCE4_NT_MAACVoltage_004")]
        public Task ATC_MPC_SCE4_NT_MAACVoltage_004() => Conform("ATC_MPC_SCE4_NT_MAACVoltage_004");

        [Test] [Property("ATC", "ATC_MPC_SCE4_PT_MAACVoltage_005")]
        public Task ATC_MPC_SCE4_PT_MAACVoltage_005() => Conform("ATC_MPC_SCE4_PT_MAACVoltage_005");

        [Test] [Property("ATC", "ATC_MPC_SCE4_NT_MAACVoltage_006")]
        public Task ATC_MPC_SCE4_NT_MAACVoltage_006() => Conform("ATC_MPC_SCE4_NT_MAACVoltage_006");

        [Test] [Property("ATC", "ATC_MPC_SCE4_PT_MAACVoltage_007")]
        public Task ATC_MPC_SCE4_PT_MAACVoltage_007() => Conform("ATC_MPC_SCE4_PT_MAACVoltage_007");

        [Test] [Property("ATC", "ATC_MPC_SCE4_NT_MAACVoltage_008")]
        public Task ATC_MPC_SCE4_NT_MAACVoltage_008() => Conform("ATC_MPC_SCE4_NT_MAACVoltage_008");

        [Test] [Property("ATC", "ATC_MPC_SCE4_PT_MAACVoltage_009")]
        public Task ATC_MPC_SCE4_PT_MAACVoltage_009() => Conform("ATC_MPC_SCE4_PT_MAACVoltage_009");

        [Test] [Property("ATC", "ATC_MPC_SCE4_NT_MAACVoltage_010")]
        public Task ATC_MPC_SCE4_NT_MAACVoltage_010() => Conform("ATC_MPC_SCE4_NT_MAACVoltage_010");

        [Test] [Property("ATC", "ATC_MPC_SCE4_PT_MAACVoltage_011")]
        public Task ATC_MPC_SCE4_PT_MAACVoltage_011() => Conform("ATC_MPC_SCE4_PT_MAACVoltage_011");

        [Test] [Property("ATC", "ATC_MPC_SCE4_NT_MAACVoltage_012")]
        public Task ATC_MPC_SCE4_NT_MAACVoltage_012() => Conform("ATC_MPC_SCE4_NT_MAACVoltage_012");

        [Test] [Property("ATC", "ATC_MPC_SCE5_PT_MAFrequency_001")]
        public Task ATC_MPC_SCE5_PT_MAFrequency_001() => Conform("ATC_MPC_SCE5_PT_MAFrequency_001");

        [Test] [Property("ATC", "ATC_MPC_SCE5_NT_MAFrequency_002")]
        public Task ATC_MPC_SCE5_NT_MAFrequency_002() => Conform("ATC_MPC_SCE5_NT_MAFrequency_002");

    }

}
