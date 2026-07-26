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

namespace cloud.charging.open.protocols.EEBUS.Conformance
{

    // The 101 abstract test cases of the two monitoring specifications, each
    // of them one cell of the matrix their chapters 7 and 8 are laid out as:
    // which data point, on which phase, in which direction of energy, with
    // which value state. The bodies live in MonitoringCases.cs, because a case
    // which differs from its neighbour only in a phase letter should differ
    // from it only in a phase letter - twelve hand-written voltage cases is how
    // the eleventh one ends up checking phase B twice.

    #region MGCP - the grid connection point as the device under test

    public sealed class ATC_MGCP_COM_PT_GCPPolling_001()      : ARhythmCase("MGCP", "COM_PT_GCPPolling_001",      Polling: true);
    public sealed class ATC_MGCP_COM_PT_GCPNotification_001() : ARhythmCase("MGCP", "COM_PT_GCPNotification_001", Polling: false);

    public sealed class ATC_MGCP_SCE1_PT_GCPPowerLimitFactor_001() : APowerLimitFactorCase("SCE1_PT_GCPPowerLimitFactor_001");

    public sealed class ATC_MGCP_SCE2_PT_GCPTotalActivePower_001()
        : AMeasuredValueCase("MGCP", "SCE2_PT_GCPTotalActivePower_001", "TotalActivePower");

    /// <summary>The feed-in meter stands still while the house is drawing from the grid.</summary>
    public sealed class ATC_MGCP_SCE3_PT_GCPTotalFeedInEnergy_001()
        : AMeasuredValueCase("MGCP", "SCE3_PT_GCPTotalFeedInEnergy_001", "TotalFeedInEnergy", Producing: false)
    { protected override Boolean Unchanging => true; }

    public sealed class ATC_MGCP_SCE3_PT_GCPTotalFeedInEnergy_002()
        : AMeasuredValueCase("MGCP", "SCE3_PT_GCPTotalFeedInEnergy_002", "TotalFeedInEnergy", Producing: true);

    public sealed class ATC_MGCP_SCE4_PT_GCPTotalConsumedEnergy_001()
        : AMeasuredValueCase("MGCP", "SCE4_PT_GCPTotalConsumedEnergy_001", "TotalConsumedEnergy", Producing: false);

    /// <summary>The consumption meter stands still while the house is feeding in.</summary>
    public sealed class ATC_MGCP_SCE4_PT_GCPTotalConsumedEnergy_002()
        : AMeasuredValueCase("MGCP", "SCE4_PT_GCPTotalConsumedEnergy_002", "TotalConsumedEnergy", Producing: true)
    { protected override Boolean Unchanging => true; }

    public sealed class ATC_MGCP_SCE5_PT_GCPActiveACCurrent_001()
        : AMeasuredValueCase("MGCP", "SCE5_PT_GCPActiveACCurrent_001", "ActiveACCurrent", "a");
    public sealed class ATC_MGCP_SCE5_PT_GCPActiveACCurrent_002()
        : AMeasuredValueCase("MGCP", "SCE5_PT_GCPActiveACCurrent_002", "ActiveACCurrent", "b");
    public sealed class ATC_MGCP_SCE5_PT_GCPActiveACCurrent_003()
        : AMeasuredValueCase("MGCP", "SCE5_PT_GCPActiveACCurrent_003", "ActiveACCurrent", "c");

    public sealed class ATC_MGCP_SCE6_PT_GCPACVoltage_001()
        : AMeasuredValueCase("MGCP", "SCE6_PT_GCPACVoltage_001", "ACVoltage", "an", Directed: false);
    public sealed class ATC_MGCP_SCE6_PT_GCPACVoltage_002()
        : AMeasuredValueCase("MGCP", "SCE6_PT_GCPACVoltage_002", "ACVoltage", "bn", Directed: false);
    public sealed class ATC_MGCP_SCE6_PT_GCPACVoltage_003()
        : AMeasuredValueCase("MGCP", "SCE6_PT_GCPACVoltage_003", "ACVoltage", "cn", Directed: false);
    public sealed class ATC_MGCP_SCE6_PT_GCPACVoltage_004()
        : AMeasuredValueCase("MGCP", "SCE6_PT_GCPACVoltage_004", "ACVoltage", "ab", Directed: false);
    public sealed class ATC_MGCP_SCE6_PT_GCPACVoltage_005()
        : AMeasuredValueCase("MGCP", "SCE6_PT_GCPACVoltage_005", "ACVoltage", "bc", Directed: false);
    public sealed class ATC_MGCP_SCE6_PT_GCPACVoltage_006()
        : AMeasuredValueCase("MGCP", "SCE6_PT_GCPACVoltage_006", "ACVoltage", "ca", Directed: false);

    public sealed class ATC_MGCP_SCE7_PT_GCPFrequency_001()
        : AMeasuredValueCase("MGCP", "SCE7_PT_GCPFrequency_001", "Frequency", Directed: false);

    #endregion

    #region MGCP - the monitoring appliance as the device under test

    public sealed class ATC_MGCP_COM_PT_MAPolling_001()      : ARhythmCase("MGCP", "COM_PT_MAPolling_001",      Polling: true);
    public sealed class ATC_MGCP_COM_PT_MANotification_001() : ARhythmCase("MGCP", "COM_PT_MANotification_001", Polling: false);

    public sealed class ATC_MGCP_SCE1_PT_MAPowerLimitFactor_001() : APowerLimitFactorCase("SCE1_PT_MAPowerLimitFactor_001");

    public sealed class ATC_MGCP_SCE2_PT_MATotalActivePower_001()
        : AMeasuredValueCase("MGCP", "SCE2_PT_MATotalActivePower_001", "TotalActivePower");
    public sealed class ATC_MGCP_SCE2_NT_MATotalActivePower_002()
        : ADiscardCase("MGCP", "SCE2_NT_MATotalActivePower_002", "TotalActivePower");

    public sealed class ATC_MGCP_SCE3_PT_MATotalFeedInEnergy_001()
        : AMeasuredValueCase("MGCP", "SCE3_PT_MATotalFeedInEnergy_001", "TotalFeedInEnergy", Producing: true);
    public sealed class ATC_MGCP_SCE3_NT_MATotalFeedInEnergy_002()
        : ADiscardCase("MGCP", "SCE3_NT_MATotalFeedInEnergy_002", "TotalFeedInEnergy");

    public sealed class ATC_MGCP_SCE4_PT_MATotalConsumedEnergy_001()
        : AMeasuredValueCase("MGCP", "SCE4_PT_MATotalConsumedEnergy_001", "TotalConsumedEnergy");
    public sealed class ATC_MGCP_SCE4_NT_MATotalConsumedEnergy_002()
        : ADiscardCase("MGCP", "SCE4_NT_MATotalConsumedEnergy_002", "TotalConsumedEnergy");

    public sealed class ATC_MGCP_SCE5_PT_MAActiveACCurrent_001()
        : AMeasuredValueCase("MGCP", "SCE5_PT_MAActiveACCurrent_001", "ActiveACCurrent", "a");
    public sealed class ATC_MGCP_SCE5_NT_MAActiveACCurrent_002()
        : ADiscardCase("MGCP", "SCE5_NT_MAActiveACCurrent_002", "ActiveACCurrent", "a");
    public sealed class ATC_MGCP_SCE5_PT_MAActiveACCurrent_003()
        : AMeasuredValueCase("MGCP", "SCE5_PT_MAActiveACCurrent_003", "ActiveACCurrent", "b");
    public sealed class ATC_MGCP_SCE5_NT_MAActiveACCurrent_004()
        : ADiscardCase("MGCP", "SCE5_NT_MAActiveACCurrent_004", "ActiveACCurrent", "b");
    public sealed class ATC_MGCP_SCE5_PT_MAActiveACCurrent_005()
        : AMeasuredValueCase("MGCP", "SCE5_PT_MAActiveACCurrent_005", "ActiveACCurrent", "c");
    public sealed class ATC_MGCP_SCE5_NT_MAActiveACCurrent_006()
        : ADiscardCase("MGCP", "SCE5_NT_MAActiveACCurrent_006", "ActiveACCurrent", "c");

    public sealed class ATC_MGCP_SCE6_PT_MAACVoltage_001()
        : AMeasuredValueCase("MGCP", "SCE6_PT_MAACVoltage_001", "ACVoltage", "an", Directed: false);
    public sealed class ATC_MGCP_SCE6_NT_MAACVoltage_002()
        : ADiscardCase("MGCP", "SCE6_NT_MAACVoltage_002", "ACVoltage", "an");
    public sealed class ATC_MGCP_SCE6_PT_MAACVoltage_003()
        : AMeasuredValueCase("MGCP", "SCE6_PT_MAACVoltage_003", "ACVoltage", "bn", Directed: false);
    public sealed class ATC_MGCP_SCE6_NT_MAACVoltage_004()
        : ADiscardCase("MGCP", "SCE6_NT_MAACVoltage_004", "ACVoltage", "bn");
    public sealed class ATC_MGCP_SCE6_PT_MAACVoltage_005()
        : AMeasuredValueCase("MGCP", "SCE6_PT_MAACVoltage_005", "ACVoltage", "cn", Directed: false);
    public sealed class ATC_MGCP_SCE6_NT_MAACVoltage_006()
        : ADiscardCase("MGCP", "SCE6_NT_MAACVoltage_006", "ACVoltage", "cn");
    public sealed class ATC_MGCP_SCE6_PT_MAACVoltage_007()
        : AMeasuredValueCase("MGCP", "SCE6_PT_MAACVoltage_007", "ACVoltage", "ab", Directed: false);
    public sealed class ATC_MGCP_SCE6_NT_MAACVoltage_008()
        : ADiscardCase("MGCP", "SCE6_NT_MAACVoltage_008", "ACVoltage", "ab");
    public sealed class ATC_MGCP_SCE6_PT_MAACVoltage_009()
        : AMeasuredValueCase("MGCP", "SCE6_PT_MAACVoltage_009", "ACVoltage", "bc", Directed: false);
    public sealed class ATC_MGCP_SCE6_NT_MAACVoltage_010()
        : ADiscardCase("MGCP", "SCE6_NT_MAACVoltage_010", "ACVoltage", "bc");
    public sealed class ATC_MGCP_SCE6_PT_MAACVoltage_011()
        : AMeasuredValueCase("MGCP", "SCE6_PT_MAACVoltage_011", "ACVoltage", "ca", Directed: false);
    public sealed class ATC_MGCP_SCE6_NT_MAACVoltage_012()
        : ADiscardCase("MGCP", "SCE6_NT_MAACVoltage_012", "ACVoltage", "ca");

    public sealed class ATC_MGCP_SCE7_PT_MAFrequency_001()
        : AMeasuredValueCase("MGCP", "SCE7_PT_MAFrequency_001", "Frequency", Directed: false);
    public sealed class ATC_MGCP_SCE7_NT_MAFrequency_002()
        : ADiscardCase("MGCP", "SCE7_NT_MAFrequency_002", "Frequency");

    #endregion

    #region MPC - the monitored unit as the device under test

    public sealed class ATC_MPC_COM_PT_MUPolling_001()      : ARhythmCase("MPC", "COM_PT_MUPolling_001",      Polling: true);
    public sealed class ATC_MPC_COM_PT_MUNotification_001() : ARhythmCase("MPC", "COM_PT_MUNotification_001", Polling: false);

    public sealed class ATC_MPC_SCE1_PT_MUTotalActivePower_001()
        : AMeasuredValueCase("MPC", "SCE1_PT_MUTotalActivePower_001", "TotalActivePower");

    public sealed class ATC_MPC_SCE1_PT_MUPhaseActivePower_001()
        : AMeasuredValueCase("MPC", "SCE1_PT_MUPhaseActivePower_001", "PhaseActivePower", "a");
    public sealed class ATC_MPC_SCE1_PT_MUPhaseActivePower_002()
        : AMeasuredValueCase("MPC", "SCE1_PT_MUPhaseActivePower_002", "PhaseActivePower", "b");
    public sealed class ATC_MPC_SCE1_PT_MUPhaseActivePower_003()
        : AMeasuredValueCase("MPC", "SCE1_PT_MUPhaseActivePower_003", "PhaseActivePower", "c");

    public sealed class ATC_MPC_SCE2_PT_MUTotalConsumedEnergy_001()
        : AMeasuredValueCase("MPC", "SCE2_PT_MUTotalConsumedEnergy_001", "TotalConsumedEnergy", Producing: false);

    /// <summary>The consumption meter stands still while the unit is producing.</summary>
    public sealed class ATC_MPC_SCE2_PT_MUTotalConsumedEnergy_002()
        : AMeasuredValueCase("MPC", "SCE2_PT_MUTotalConsumedEnergy_002", "TotalConsumedEnergy", Producing: true)
    { protected override Boolean Unchanging => true; }

    /// <summary>The production meter stands still while the unit is consuming.</summary>
    public sealed class ATC_MPC_SCE2_PT_MUTotalProducedEnergy_001()
        : AMeasuredValueCase("MPC", "SCE2_PT_MUTotalProducedEnergy_001", "TotalProducedEnergy", Producing: false)
    { protected override Boolean Unchanging => true; }

    public sealed class ATC_MPC_SCE2_PT_MUTotalProducedEnergy_002()
        : AMeasuredValueCase("MPC", "SCE2_PT_MUTotalProducedEnergy_002", "TotalProducedEnergy", Producing: true);

    public sealed class ATC_MPC_SCE3_PT_MUActiveACCurrent_001()
        : AMeasuredValueCase("MPC", "SCE3_PT_MUActiveACCurrent_001", "ActiveACCurrent", "a");
    public sealed class ATC_MPC_SCE3_PT_MUActiveACCurrent_002()
        : AMeasuredValueCase("MPC", "SCE3_PT_MUActiveACCurrent_002", "ActiveACCurrent", "b");
    public sealed class ATC_MPC_SCE3_PT_MUActiveACCurrent_003()
        : AMeasuredValueCase("MPC", "SCE3_PT_MUActiveACCurrent_003", "ActiveACCurrent", "c");

    public sealed class ATC_MPC_SCE4_PT_MUACVoltage_001()
        : AMeasuredValueCase("MPC", "SCE4_PT_MUACVoltage_001", "ACVoltage", "an", Directed: false);
    public sealed class ATC_MPC_SCE4_PT_MUACVoltage_002()
        : AMeasuredValueCase("MPC", "SCE4_PT_MUACVoltage_002", "ACVoltage", "bn", Directed: false);
    public sealed class ATC_MPC_SCE4_PT_MUACVoltage_003()
        : AMeasuredValueCase("MPC", "SCE4_PT_MUACVoltage_003", "ACVoltage", "cn", Directed: false);
    public sealed class ATC_MPC_SCE4_PT_MUACVoltage_004()
        : AMeasuredValueCase("MPC", "SCE4_PT_MUACVoltage_004", "ACVoltage", "ab", Directed: false);
    public sealed class ATC_MPC_SCE4_PT_MUACVoltage_005()
        : AMeasuredValueCase("MPC", "SCE4_PT_MUACVoltage_005", "ACVoltage", "bc", Directed: false);
    public sealed class ATC_MPC_SCE4_PT_MUACVoltage_006()
        : AMeasuredValueCase("MPC", "SCE4_PT_MUACVoltage_006", "ACVoltage", "ca", Directed: false);

    public sealed class ATC_MPC_SCE5_PT_MUFrequency_001()
        : AMeasuredValueCase("MPC", "SCE5_PT_MUFrequency_001", "Frequency", Directed: false);

    #endregion

    #region MPC - the monitoring appliance as the device under test

    public sealed class ATC_MPC_COM_PT_MAPolling_001()      : ARhythmCase("MPC", "COM_PT_MAPolling_001",      Polling: true);
    public sealed class ATC_MPC_COM_PT_MANotification_001() : ARhythmCase("MPC", "COM_PT_MANotification_001", Polling: false);

    public sealed class ATC_MPC_SCE1_PT_MATotalActivePower_001()
        : AMeasuredValueCase("MPC", "SCE1_PT_MATotalActivePower_001", "TotalActivePower");
    public sealed class ATC_MPC_SCE1_NT_MATotalActivePower_002()
        : ADiscardCase("MPC", "SCE1_NT_MATotalActivePower_002", "TotalActivePower");

    public sealed class ATC_MPC_SCE1_PT_MAPhaseActivePower_001()
        : AMeasuredValueCase("MPC", "SCE1_PT_MAPhaseActivePower_001", "PhaseActivePower", "a");
    public sealed class ATC_MPC_SCE1_NT_MAPhaseActivePower_002()
        : ADiscardCase("MPC", "SCE1_NT_MAPhaseActivePower_002", "PhaseActivePower", "a");
    public sealed class ATC_MPC_SCE1_PT_MAPhaseActivePower_003()
        : AMeasuredValueCase("MPC", "SCE1_PT_MAPhaseActivePower_003", "PhaseActivePower", "b");
    public sealed class ATC_MPC_SCE1_NT_MAPhaseActivePower_004()
        : ADiscardCase("MPC", "SCE1_NT_MAPhaseActivePower_004", "PhaseActivePower", "b");
    public sealed class ATC_MPC_SCE1_PT_MAPhaseActivePower_005()
        : AMeasuredValueCase("MPC", "SCE1_PT_MAPhaseActivePower_005", "PhaseActivePower", "c");
    public sealed class ATC_MPC_SCE1_NT_MAPhaseActivePower_006()
        : ADiscardCase("MPC", "SCE1_NT_MAPhaseActivePower_006", "PhaseActivePower", "c");

    public sealed class ATC_MPC_SCE2_PT_MATotalConsumedEnergy_001()
        : AMeasuredValueCase("MPC", "SCE2_PT_MATotalConsumedEnergy_001", "TotalConsumedEnergy");
    public sealed class ATC_MPC_SCE2_NT_MATotalConsumedEnergy_002()
        : ADiscardCase("MPC", "SCE2_NT_MATotalConsumedEnergy_002", "TotalConsumedEnergy");

    public sealed class ATC_MPC_SCE2_PT_MATotalProducedEnergy_001()
        : AMeasuredValueCase("MPC", "SCE2_PT_MATotalProducedEnergy_001", "TotalProducedEnergy", Producing: true);
    public sealed class ATC_MPC_SCE2_NT_MATotalProducedEnergy_002()
        : ADiscardCase("MPC", "SCE2_NT_MATotalProducedEnergy_002", "TotalProducedEnergy");

    public sealed class ATC_MPC_SCE3_PT_MAActiveACCurrent_001()
        : AMeasuredValueCase("MPC", "SCE3_PT_MAActiveACCurrent_001", "ActiveACCurrent", "a");
    public sealed class ATC_MPC_SCE3_NT_MAActiveACCurrent_002()
        : ADiscardCase("MPC", "SCE3_NT_MAActiveACCurrent_002", "ActiveACCurrent", "a");
    public sealed class ATC_MPC_SCE3_PT_MAActiveACCurrent_003()
        : AMeasuredValueCase("MPC", "SCE3_PT_MAActiveACCurrent_003", "ActiveACCurrent", "b");
    public sealed class ATC_MPC_SCE3_NT_MAActiveACCurrent_004()
        : ADiscardCase("MPC", "SCE3_NT_MAActiveACCurrent_004", "ActiveACCurrent", "b");
    public sealed class ATC_MPC_SCE3_PT_MAActiveACCurrent_005()
        : AMeasuredValueCase("MPC", "SCE3_PT_MAActiveACCurrent_005", "ActiveACCurrent", "c");
    public sealed class ATC_MPC_SCE3_NT_MAActiveACCurrent_006()
        : ADiscardCase("MPC", "SCE3_NT_MAActiveACCurrent_006", "ActiveACCurrent", "c");

    public sealed class ATC_MPC_SCE4_PT_MAACVoltage_001()
        : AMeasuredValueCase("MPC", "SCE4_PT_MAACVoltage_001", "ACVoltage", "an", Directed: false);
    public sealed class ATC_MPC_SCE4_NT_MAACVoltage_002()
        : ADiscardCase("MPC", "SCE4_NT_MAACVoltage_002", "ACVoltage", "an");
    public sealed class ATC_MPC_SCE4_PT_MAACVoltage_003()
        : AMeasuredValueCase("MPC", "SCE4_PT_MAACVoltage_003", "ACVoltage", "bn", Directed: false);
    public sealed class ATC_MPC_SCE4_NT_MAACVoltage_004()
        : ADiscardCase("MPC", "SCE4_NT_MAACVoltage_004", "ACVoltage", "bn");
    public sealed class ATC_MPC_SCE4_PT_MAACVoltage_005()
        : AMeasuredValueCase("MPC", "SCE4_PT_MAACVoltage_005", "ACVoltage", "cn", Directed: false);
    public sealed class ATC_MPC_SCE4_NT_MAACVoltage_006()
        : ADiscardCase("MPC", "SCE4_NT_MAACVoltage_006", "ACVoltage", "cn");
    public sealed class ATC_MPC_SCE4_PT_MAACVoltage_007()
        : AMeasuredValueCase("MPC", "SCE4_PT_MAACVoltage_007", "ACVoltage", "ab", Directed: false);
    public sealed class ATC_MPC_SCE4_NT_MAACVoltage_008()
        : ADiscardCase("MPC", "SCE4_NT_MAACVoltage_008", "ACVoltage", "ab");
    public sealed class ATC_MPC_SCE4_PT_MAACVoltage_009()
        : AMeasuredValueCase("MPC", "SCE4_PT_MAACVoltage_009", "ACVoltage", "bc", Directed: false);
    public sealed class ATC_MPC_SCE4_NT_MAACVoltage_010()
        : ADiscardCase("MPC", "SCE4_NT_MAACVoltage_010", "ACVoltage", "bc");
    public sealed class ATC_MPC_SCE4_PT_MAACVoltage_011()
        : AMeasuredValueCase("MPC", "SCE4_PT_MAACVoltage_011", "ACVoltage", "ca", Directed: false);
    public sealed class ATC_MPC_SCE4_NT_MAACVoltage_012()
        : ADiscardCase("MPC", "SCE4_NT_MAACVoltage_012", "ACVoltage", "ca");

    public sealed class ATC_MPC_SCE5_PT_MAFrequency_001()
        : AMeasuredValueCase("MPC", "SCE5_PT_MAFrequency_001", "Frequency", Directed: false);
    public sealed class ATC_MPC_SCE5_NT_MAFrequency_002()
        : ADiscardCase("MPC", "SCE5_NT_MAFrequency_002", "Frequency");

    #endregion

}
