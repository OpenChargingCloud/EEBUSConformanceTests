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

using cloud.charging.open.protocols.EEBUS.UseCases.LimitationOfPower;

#endregion

namespace cloud.charging.open.protocols.EEBUS.Conformance
{

    #region (class) APowerLimitationCase

    /// <summary>
    /// The shared start of every abstract test case of the two power limitation
    /// use cases.
    ///
    /// Every case below is written once and instantiated twice, for the same
    /// reason the catalog is: LPC and LPP are one specification pointed in
    /// opposite directions, their abstract test cases match one for one after the
    /// prefix, and the stack shares an implementation between them. A test which
    /// exists twice is a test which will one day disagree with itself.
    /// </summary>
    /// <param name="UseCase">"LPC" or "LPP".</param>
    /// <param name="Suffix">The rest of the official identifier.</param>
    public abstract class APowerLimitationCase(String  UseCase,
                                               String  Suffix) : AConformanceTest
    {

        #region Properties

        /// <summary>The official abstract test case identifier.</summary>
        public override String Id
            => $"ATC_{UseCase}_{Suffix}";

        /// <summary>Which of the two use cases this run is about.</summary>
        protected String  Limitation
            => UseCase;

        /// <summary>Whether the device under test is the controllable system.</summary>
        protected Boolean DUTIsSystem
            => TestCase.Actor == "CS";

        #endregion


        #region Reach(Context, Configuration, CancellationToken)

        /// <summary>
        /// Bring both sides into one of the test configurations of section 6.5,
        /// and hand back the scenario sitting in it.
        ///
        /// The configurations are states of the use case, not of the protocol,
        /// and reaching them is the pre-condition rather than the test: "it does
        /// not matter whether this is achieved via debug interface, exact logging
        /// or triggering a message by the tester" (section 6.5). So everything
        /// here goes through the same messages a real energy guard would send,
        /// and anything which goes wrong on the way is inconclusive rather than a
        /// failure of the device.
        /// </summary>
        /// <param name="Context">Where the steps are written down.</param>
        /// <param name="Configuration">A CF_CS_ identifier.</param>
        /// <param name="CancellationToken">An optional cancellation token.</param>
        protected async Task<PowerLimitationScenario> Reach(ConformanceContext  Context,
                                                            String              Configuration,
                                                            CancellationToken   CancellationToken)
        {

            var scenario = await PowerLimitationScenario.Create(Context.Parameters,
                                                                Limitation,
                                                                DUTIsSystem,
                                                                CancellationToken);

            var sheet    = scenario.Sheet;

            await Context.Precondition(Configuration, async () => {

                await scenario.Connect(CancellationToken);

                switch (Configuration)
                {

                    // The controllable system has (re)started and nobody has
                    // said anything to it yet.
                    case "CF_CS_Init":
                    case "CF_CS_Reset_Init":
                        break;

                    // A heartbeat and a following deactivated limit, which is
                    // transition 1.
                    case "CF_CS_UnlCntrl":
                        await scenario.Heartbeat(CancellationToken);
                        await scenario.WriteLimit(LimitMessages.ValueDeactivatedDeleteDuration, sheet.Limit(3), CancellationToken: CancellationToken);
                        break;

                    // ... and a following activated one, which is transition 2.
                    case "CF_CS_Limited_wo_dur":
                        await scenario.Heartbeat(CancellationToken);
                        await scenario.WriteLimit(LimitMessages.ValueActivatedDeleteDuration, sheet.Limit(3), CancellationToken: CancellationToken);
                        break;

                    case "CF_CS_Limited_w_dur":
                        await scenario.Heartbeat(CancellationToken);
                        await scenario.WriteLimit(LimitMessages.ValueActivatedDuration, sheet.Limit(3), sheet.LimitDuration(1), CancellationToken);
                        break;

                    // Controlled, and then the heartbeat stops: transition 5.
                    case "CF_CS_FS":
                        await scenario.Heartbeat(CancellationToken);
                        await scenario.WriteLimit(LimitMessages.ValueDeactivatedDeleteDuration, sheet.Limit(3), CancellationToken: CancellationToken);
                        scenario.Disconnect();
                        await scenario.Advance(TimeSpan.FromSeconds(130), CancellationToken);
                        scenario.Reconnect();
                        break;

                    // Nobody ever took control: transition 3.
                    case "CF_CS_UnlAuto":
                        await scenario.Advance(TimeSpan.FromSeconds(130), CancellationToken);
                        break;

                    default:
                        throw new ConformanceInconclusive($"{Configuration} is not a test configuration of this specification.");

                }

                var expected = Configuration switch {
                                   "CF_CS_Init"           => PowerLimitationState.Init,
                                   "CF_CS_Reset_Init"     => PowerLimitationState.Init,
                                   "CF_CS_UnlCntrl"       => PowerLimitationState.UnlimitedControlled,
                                   "CF_CS_Limited_wo_dur" => PowerLimitationState.Limited,
                                   "CF_CS_Limited_w_dur"  => PowerLimitationState.Limited,
                                   "CF_CS_FS"             => PowerLimitationState.FailsafeState,
                                   _                      => PowerLimitationState.UnlimitedAutonomous
                               };

                if (scenario.State != expected)
                    throw new ConformanceInconclusive($"the controllable system is in {scenario.State} rather than {expected}");

            });

            return scenario;

        }

        #endregion

        #region Accepted(Result) / Rejected(Result)

        /// <summary>
        /// What "the CS receives and accepts the write command" means on the
        /// wire: an ACK rather than a NACK.
        /// </summary>
        protected static String Outcome(cloud.charging.open.protocols.EEBUS.SPINE.Model.ResultDataType? Result)

            => Result is null
                   ? "accepted"
                   : $"refused with errorNumber {Result.ErrorNumber}: {Result.Description}";

        #endregion

    }

    #endregion


    #region ATC_*_COM_PT_EGHeartbeat_001

    /// <summary>
    /// The energy guard sends its heartbeat at least every 60 seconds
    /// ([*-TS-006]).
    ///
    /// It reads like a formality and it is the most load bearing rule in the use
    /// case: the controllable system falls into its failsafe state after 120
    /// seconds without one, and in Germany that fallback is law rather than
    /// convention. An energy guard whose heartbeat is a little late does not
    /// inconvenience anybody - it curtails a house.
    /// </summary>
    public abstract class EGHeartbeat_001(String UseCase) : APowerLimitationCase(UseCase, "COM_PT_EGHeartbeat_001")
    {

        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            var scenario = await Reach(Context, "CF_CS_UnlCntrl", CancellationToken);

            await scenario.Guard.Heartbeat.Start(PowerLimitation.HeartbeatInterval, CancellationToken);

            await Context.Step("1",
                               "Count 5 heartbeats sent by the EG.",
                               "The longest period between 2 consecutive heartbeats does not exceed 60 seconds.",
                               async step => {

                                   await scenario.Advance(TimeSpan.FromMinutes(6), CancellationToken);

                                   var (count, widest) = scenario.HeartbeatsFromDUT();

                                   step.Observe($"{count} heartbeat(s), the widest gap {widest.TotalSeconds:F0} s");

                                   step.Require(count >= 5,
                                                $"only {count} heartbeat(s) arrived in six minutes");

                                   step.Require(widest <= TimeSpan.FromSeconds(60),
                                                $"{widest.TotalSeconds:F0} seconds passed between two heartbeats");

                               });

        }

    }

    public sealed class ATC_LPC_COM_PT_EGHeartbeat_001() : EGHeartbeat_001("LPC");
    public sealed class ATC_LPP_COM_PT_EGHeartbeat_001() : EGHeartbeat_001("LPP");

    #endregion

    #region ATC_*_COM_PT_CSHeartbeat_001

    /// <summary>
    /// The controllable system sends its heartbeat at least every 60 seconds
    /// ([*-TS-007]).
    /// </summary>
    public abstract class CSHeartbeat_001(String UseCase) : APowerLimitationCase(UseCase, "COM_PT_CSHeartbeat_001")
    {

        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            var scenario = await Reach(Context, "CF_CS_UnlCntrl", CancellationToken);

            await scenario.System.Heartbeat.Start(PowerLimitation.HeartbeatInterval, CancellationToken);

            await Context.Step("1",
                               "Count 5 heartbeats sent by the CS.",
                               "The longest period between 2 consecutive heartbeats does not exceed 60 seconds.",
                               async step => {

                                   await scenario.Advance(TimeSpan.FromMinutes(6), CancellationToken);

                                   var (count, widest) = scenario.HeartbeatsFromDUT();

                                   step.Observe($"{count} heartbeat(s), the widest gap {widest.TotalSeconds:F0} s");

                                   step.Require(count >= 5,
                                                $"only {count} heartbeat(s) arrived in six minutes");

                                   step.Require(widest <= TimeSpan.FromSeconds(60),
                                                $"{widest.TotalSeconds:F0} seconds passed between two heartbeats");

                               });

        }

    }

    public sealed class ATC_LPC_COM_PT_CSHeartbeat_001() : CSHeartbeat_001("LPC");
    public sealed class ATC_LPP_COM_PT_CSHeartbeat_001() : CSHeartbeat_001("LPP");

    #endregion

}
