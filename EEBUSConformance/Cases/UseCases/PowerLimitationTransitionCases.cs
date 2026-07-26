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

    #region (class) ATransitionCase

    /// <summary>
    /// The shape shared by almost every one of the twelve transition cases of
    /// section 8.2.8 to 8.2.19: put the controllable system into a state, send
    /// it something, and see where it ends up.
    ///
    /// Writing them as one class parameterised by four things is not a shortcut
    /// but a reading of the specification. Section 2.3.3 is a table of twelve
    /// rows, each of which says "from this state, on this stimulus, to that
    /// state"; the abstract test cases are that table transcribed, with one case
    /// per row and a second where a row can be reached two ways. Anything which
    /// made them look more different than they are would be hiding the structure
    /// the specification is built on.
    /// </summary>
    /// <param name="UseCase">"LPC" or "LPP".</param>
    /// <param name="Suffix">The rest of the official identifier.</param>
    /// <param name="Number">The transition number of section 2.3.3, for the report.</param>
    /// <param name="From">The test configuration to start in.</param>
    /// <param name="To">The state the controllable system has to reach.</param>
    /// <param name="Message">Which message combination the write carries.</param>
    /// <param name="Accepted">Whether the write is expected to be accepted.</param>
    public abstract class ATransitionCase(String                UseCase,
                                          String                Suffix,
                                          UInt32                Number,
                                          String                From,
                                          PowerLimitationState  To,
                                          LimitMessages         Message,
                                          Boolean               Accepted) : APowerLimitationCase(UseCase, Suffix)
    {

        /// <summary>Which of the six limit values of section 6.11.2 to write.</summary>
        protected virtual Int32 LimitValue
            => Accepted ? 3 : 6;

        /// <summary>The duration to send, where the message combination carries one.</summary>
        protected virtual Int32 DurationValue
            => 1;


        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            var scenario = await Reach(Context, From, CancellationToken);
            var value    = scenario.Sheet.Limit(LimitValue);
            var was      = scenario.State;

            // Every case whose pre-condition is CF_EG_ManualExecution starts
            // from a controllable system which has heard nothing yet, so the
            // heartbeat which makes the write evaluable at all is a test step of
            // its own - and in three of the states, leaving it out is what makes
            // the write be ignored (rule 036).
            if (From is "CF_CS_Init" or "CF_CS_FS" or "CF_CS_UnlAuto")
                await Context.Step("2",
                                   "Send an EG heartbeat.",
                                   "The CS receives the heartbeat.",
                                   async step => {
                                       await scenario.Heartbeat(CancellationToken);
                                       step.Observe($"the controllable system is in {scenario.State}");
                                   });

            await Context.Step(From is "CF_CS_Init" or "CF_CS_FS" or "CF_CS_UnlAuto" ? "3" : "1",
                               Accepted
                                   ? $"Send an EG {(Carries(Message) ? "activation" : "deactivation")} write command."
                                   : "Send an EG APCL write command with a negative value.",
                               $"The CS receives and {(Accepted ? "accepts" : "rejects")} the write command. " +
                               $"The CS changes its configuration to {Configuration(To)}.",
                               async step => {

                                   var refused = await scenario.WriteLimit(Message,
                                                                          value,
                                                                          scenario.Sheet.LimitDuration(DurationValue),
                                                                          CancellationToken);

                                   step.Observe($"the write was {Outcome(refused)}, and the controllable system went " +
                                                $"{was} -> {scenario.State}");

                                   step.Require(Accepted == (refused is null),
                                                Accepted
                                                    ? $"the write was refused: {refused?.Description}"
                                                    : "the write was accepted, but a limit with this value is not applicable");

                                   step.Require(scenario.State == To,
                                                $"the controllable system is in {scenario.State} rather than {To} " +
                                                $"(transition {Number} of section 2.3.3)");

                               });

        }


        /// <summary>Whether the combination activates rather than deactivates the limit.</summary>
        private static Boolean Carries(LimitMessages Message)

            => Message is LimitMessages.Activated
                       or LimitMessages.ValueActivated
                       or LimitMessages.ActivatedDeleteDuration
                       or LimitMessages.ActivatedDuration
                       or LimitMessages.ValueActivatedDeleteDuration
                       or LimitMessages.ValueActivatedDuration;


        /// <summary>The test configuration name of a state, for the expected result.</summary>
        protected static String Configuration(PowerLimitationState State)

            => State switch {
                   PowerLimitationState.Init                 => "CF_CS_Init",
                   PowerLimitationState.UnlimitedControlled  => "CF_CS_UnlCntrl",
                   PowerLimitationState.Limited              => "CF_CS_Limited",
                   PowerLimitationState.FailsafeState        => "CF_CS_FS",
                   _                                         => "CF_CS_UnlAuto"
               };

    }

    #endregion

    #region (class) ATimedTransitionCase

    /// <summary>
    /// The transitions which happen because nothing happened.
    ///
    /// Three of the twelve are caused by time rather than by a message - the
    /// heartbeat stops, or a heartbeat arrives and no limit follows it - and they
    /// are the ones a device is most likely to get wrong, because getting them
    /// right means running a timer for something which may never come.
    /// </summary>
    /// <param name="UseCase">"LPC" or "LPP".</param>
    /// <param name="Suffix">The rest of the official identifier.</param>
    /// <param name="Number">The transition number of section 2.3.3.</param>
    /// <param name="From">The test configuration to start in.</param>
    /// <param name="To">The state the controllable system has to reach.</param>
    /// <param name="Tolerated">A second state the specification also permits, where it does.</param>
    public abstract class ATimedTransitionCase(String                 UseCase,
                                               String                 Suffix,
                                               UInt32                 Number,
                                               String                 From,
                                               PowerLimitationState   To,
                                               PowerLimitationState?  Tolerated   = null) : APowerLimitationCase(UseCase, Suffix)
    {

        /// <summary>Whether a heartbeat is sent before the waiting begins.</summary>
        protected virtual Boolean HeartbeatFirst
            => false;

        /// <summary>Whether the connection is interrupted before the waiting begins.</summary>
        protected virtual Boolean InterruptFirst
            => false;


        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            var scenario = await Reach(Context, From, CancellationToken);
            var step     = 1;

            if (HeartbeatFirst)
                await Context.Step((++step).ToString(),
                                   "Send an EG heartbeat.",
                                   "The CS receives the heartbeat.",
                                   async _ => await scenario.Heartbeat(CancellationToken));

            if (InterruptFirst)
                await Context.Step((++step).ToString(),
                                   "Simulate an interrupted connection, e.g. by disconnecting the network.",
                                   "The network connection to the EG is interrupted.",
                                   _ => {
                                       scenario.Disconnect();
                                       return Task.CompletedTask;
                                   });

            await Context.Step((++step).ToString(),
                               "Wait for configuration change of the CS for 130 seconds.",
                               $"The CS changes its configuration to {Configuration(To)}" +
                               $"{(Tolerated is not null ? $" or stays in {Configuration(Tolerated.Value)}" : "")}.",
                               async body => {

                                   var was     = scenario.State;
                                   var waited  = await scenario.WaitForStateChange(TimeSpan.FromSeconds(130), CancellationToken);

                                   body.Observe(waited is not null
                                                    ? $"{was} -> {scenario.State} after {waited.Value.TotalSeconds:F0} s"
                                                    : $"still in {was} after 130 s");

                                   body.Require(scenario.State == To ||
                                                (Tolerated is not null && scenario.State == Tolerated.Value),
                                                $"the controllable system is in {scenario.State} rather than {To} " +
                                                $"(transition {Number} of section 2.3.3)");

                               });

        }


        /// <summary>The test configuration name of a state.</summary>
        private static String Configuration(PowerLimitationState State)

            => State switch {
                   PowerLimitationState.Init                 => "CF_CS_Init",
                   PowerLimitationState.UnlimitedControlled  => "CF_CS_UnlCntrl",
                   PowerLimitationState.Limited              => "CF_CS_Limited",
                   PowerLimitationState.FailsafeState        => "CF_CS_FS",
                   _                                         => "CF_CS_UnlAuto"
               };

    }

    #endregion


    #region Transition 1 - init to unlimited/controlled

    /// <summary>A rejected activated limit still takes "init" to "unlimited/controlled" ([*-TS-018], [*-TS-035/1]).</summary>
    public abstract class CSTransition1_001(String UseCase)
        : ATransitionCase(UseCase, "COM_PT_CSTransition1_001", 1, "CF_CS_Init",
                          PowerLimitationState.UnlimitedControlled, LimitMessages.ValueActivatedDeleteDuration, Accepted: false);

    public sealed class ATC_LPC_COM_PT_CSTransition1_001() : CSTransition1_001("LPC");
    public sealed class ATC_LPP_COM_PT_CSTransition1_001() : CSTransition1_001("LPP");


    /// <summary>An accepted deactivated limit takes "init" to "unlimited/controlled" ([*-TS-021]).</summary>
    public abstract class CSTransition1_002(String UseCase)
        : ATransitionCase(UseCase, "COM_PT_CSTransition1_002", 1, "CF_CS_Init",
                          PowerLimitationState.UnlimitedControlled, LimitMessages.ValueDeactivatedDeleteDuration, Accepted: true);

    public sealed class ATC_LPC_COM_PT_CSTransition1_002() : CSTransition1_002("LPC");
    public sealed class ATC_LPP_COM_PT_CSTransition1_002() : CSTransition1_002("LPP");

    #endregion

    #region Transition 2 - init to limited

    /// <summary>An accepted activated limit takes "init" to "limited" ([*-TS-020]).</summary>
    public abstract class CSTransition2_001(String UseCase)
        : ATransitionCase(UseCase, "COM_PT_CSTransition2_001", 2, "CF_CS_Init",
                          PowerLimitationState.Limited, LimitMessages.ValueActivatedDuration, Accepted: true);

    public sealed class ATC_LPC_COM_PT_CSTransition2_001() : CSTransition2_001("LPC");
    public sealed class ATC_LPP_COM_PT_CSTransition2_001() : CSTransition2_001("LPP");

    #endregion

    #region Transition 3 - init to unlimited/autonomous

    /// <summary>
    /// Nobody said anything at all, and after 120 seconds the controllable system
    /// stops waiting ([*-TS-022], [*-TS-022/1]).
    /// </summary>
    public abstract class CSTransition3_001(String UseCase)
        : ATimedTransitionCase(UseCase, "COM_PT_CSTransition3_001", 3, "CF_CS_Init",
                               PowerLimitationState.UnlimitedAutonomous, PowerLimitationState.Init);

    public sealed class ATC_LPC_COM_PT_CSTransition3_001() : CSTransition3_001("LPC");
    public sealed class ATC_LPP_COM_PT_CSTransition3_001() : CSTransition3_001("LPP");


    /// <summary>
    /// A heartbeat arrived but no limit followed it, which counts for nothing
    /// ([*-TS-022], [*-TS-022/1]).
    /// </summary>
    public abstract class CSTransition3_002(String UseCase)
        : ATimedTransitionCase(UseCase, "COM_PT_CSTransition3_002", 3, "CF_CS_Init",
                               PowerLimitationState.UnlimitedAutonomous, PowerLimitationState.Init)
    {
        protected override Boolean HeartbeatFirst => true;
    }

    public sealed class ATC_LPC_COM_PT_CSTransition3_002() : CSTransition3_002("LPC");
    public sealed class ATC_LPP_COM_PT_CSTransition3_002() : CSTransition3_002("LPP");

    #endregion

    #region Transition 4 - unlimited/controlled to limited

    /// <summary>An accepted activated limit takes "unlimited/controlled" to "limited" ([*-TS-027]).</summary>
    public abstract class CSTransition4_001(String UseCase)
        : ATransitionCase(UseCase, "COM_PT_CSTransition4_001", 4, "CF_CS_UnlCntrl",
                          PowerLimitationState.Limited, LimitMessages.ValueActivatedDuration, Accepted: true);

    public sealed class ATC_LPC_COM_PT_CSTransition4_001() : CSTransition4_001("LPC");
    public sealed class ATC_LPP_COM_PT_CSTransition4_001() : CSTransition4_001("LPP");

    #endregion

    #region Transition 5 - unlimited/controlled to the failsafe state

    /// <summary>
    /// The heartbeat stops and 120 seconds later the controllable system limits
    /// itself ([*-TS-028]).
    ///
    /// This is the rule the whole use case exists for, and the only one with
    /// legal weight behind it in Germany: an appliance which has lost its energy
    /// guard falls back to a value the grid can survive rather than to whatever
    /// it was doing.
    /// </summary>
    public abstract class CSTransition5_001(String UseCase)
        : ATimedTransitionCase(UseCase, "COM_PT_CSTransition5_001", 5, "CF_CS_UnlCntrl",
                               PowerLimitationState.FailsafeState)
    {
        protected override Boolean InterruptFirst => true;
    }

    public sealed class ATC_LPC_COM_PT_CSTransition5_001() : CSTransition5_001("LPC");
    public sealed class ATC_LPP_COM_PT_CSTransition5_001() : CSTransition5_001("LPP");

    #endregion

    #region Transition 6 - limited to unlimited/controlled

    /// <summary>
    /// The duration of the limit ran out ([*-TS-001/1], [*-TS-008], [*-TS-008/1],
    /// [*-TS-025]).
    /// </summary>
    public abstract class CSTransition6_001(String UseCase) : APowerLimitationCase(UseCase, "COM_PT_CSTransition6_001")
    {

        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            var scenario = await Reach(Context, "CF_CS_Limited_wo_dur", CancellationToken);
            var duration = scenario.Sheet.LimitDuration(1)!.Value;

            await Context.Step("1",
                               "Send an EG APCL duration write command.",
                               "The CS receives and accepts the write command. The CS changes its configuration to CF_CS_Limited_w_dur.",
                               async step => {

                                   var refused = await scenario.WriteLimit(LimitMessages.ValueActivatedDuration,
                                                                          scenario.Sheet.Limit(3),
                                                                          duration,
                                                                          CancellationToken);

                                   step.Require(refused is null,
                                                $"the write was refused: {refused?.Description}");

                                   step.Require(scenario.State == PowerLimitationState.Limited,
                                                $"the controllable system is in {scenario.State} rather than Limited");

                               });

            await Context.Step("2",
                               "Wait for the set duration to expire.",
                               "The duration is expired. The CS changes its configuration to CF_CS_UnlCntrl.",
                               async step => {

                                   // The heartbeat has to keep coming, or the
                                   // controllable system would fall into its
                                   // failsafe state instead and the case would
                                   // pass for the wrong reason.
                                   await scenario.Advance(duration / 2, CancellationToken);
                                   await scenario.Heartbeat(CancellationToken);
                                   await scenario.Advance(duration, CancellationToken);

                                   await scenario.System.LimitExpired(CancellationToken);

                                   step.Observe($"the controllable system is in {scenario.State}");

                                   step.Require(scenario.State == PowerLimitationState.UnlimitedControlled,
                                                $"the controllable system is in {scenario.State} rather than UnlimitedControlled " +
                                                $"(transition 6 of section 2.3.3)");

                               });

            await Context.Step("3",
                               "Optional test step: check the APCL duration parameter of the CS.",
                               "The APCL duration parameter is deleted or has a value of 0 seconds.",
                               step => {

                                   var (_, _, remaining) = scenario.System.ConsumptionLimit;

                                   step.Observe(remaining is null
                                                    ? "the duration is deleted"
                                                    : $"the duration reads {remaining.Value.TotalSeconds:F0} s");

                                   if (remaining is not null && remaining.Value > TimeSpan.Zero)
                                       step.Tolerate("the duration of an expired limit is still set, which rule 008/1 leaves optional");

                                   return Task.CompletedTask;

                               });

        }

    }

    public sealed class ATC_LPC_COM_PT_CSTransition6_001() : CSTransition6_001("LPC");
    public sealed class ATC_LPP_COM_PT_CSTransition6_001() : CSTransition6_001("LPP");


    /// <summary>A deactivation takes "limited" to "unlimited/controlled" ([*-TS-026]).</summary>
    public abstract class CSTransition6_002(String UseCase)
        : ATransitionCase(UseCase, "COM_PT_CSTransition6_002", 6, "CF_CS_Limited_wo_dur",
                          PowerLimitationState.UnlimitedControlled, LimitMessages.ValueDeactivatedDeleteDuration, Accepted: true);

    public sealed class ATC_LPC_COM_PT_CSTransition6_002() : CSTransition6_002("LPC");
    public sealed class ATC_LPP_COM_PT_CSTransition6_002() : CSTransition6_002("LPP");

    #endregion

    #region Transition 7 - limited to the failsafe state

    /// <summary>
    /// The heartbeat stops while the controllable system is limited, and it falls
    /// back to its failsafe value rather than keeping the last limit it was given
    /// ([*-TS-029]).
    /// </summary>
    public abstract class CSTransition7_001(String UseCase) : APowerLimitationCase(UseCase, "COM_PT_CSTransition7_001")
    {

        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            var scenario = await Reach(Context, "CF_CS_Limited_wo_dur", CancellationToken);

            await Context.Step("1",
                               "Send an EG FCAPL write command.",
                               "The CS receives and accepts the write command.",
                               async step => {

                                   var refused = await scenario.WriteFailsafe(scenario.Sheet.Failsafe(3),
                                                                              CancellationToken: CancellationToken);

                                   step.Require(refused is null,
                                                $"the write was refused: {refused?.Description}");

                               });

            await Context.Step("2",
                               "Simulate an interrupted connection, e.g. by disconnecting the network.",
                               "The network connection to the EG is interrupted.",
                               _ => {
                                   scenario.Disconnect();
                                   return Task.CompletedTask;
                               });

            await Context.Step("3",
                               "Wait for configuration change of the CS for at least 130 seconds.",
                               "After the communication to the EG has been interrupted for 130 seconds, the CS changes its configuration to CF_CS_FS.",
                               async step => {

                                   var waited = await scenario.WaitForStateChange(TimeSpan.FromSeconds(130), CancellationToken);

                                   step.Observe(waited is not null
                                                    ? $"the failsafe state was reached after {waited.Value.TotalSeconds:F0} s"
                                                    : "the state did not change within 130 s");

                                   step.Require(scenario.State == PowerLimitationState.FailsafeState,
                                                $"the controllable system is in {scenario.State} rather than FailsafeState " +
                                                $"(transition 7 of section 2.3.3)");

                               });

        }

    }

    public sealed class ATC_LPC_COM_PT_CSTransition7_001() : CSTransition7_001("LPC");
    public sealed class ATC_LPP_COM_PT_CSTransition7_001() : CSTransition7_001("LPP");

    #endregion

    #region Transition 8 - the failsafe state to unlimited/controlled

    /// <summary>A limit which cannot be applied still ends the failsafe state ([*-TS-031], [*-TS-035/1]).</summary>
    public abstract class CSTransition8_001(String UseCase)
        : ATransitionCase(UseCase, "COM_PT_CSTransition8_001", 8, "CF_CS_FS",
                          PowerLimitationState.UnlimitedControlled, LimitMessages.ValueActivatedDeleteDuration, Accepted: false);

    public sealed class ATC_LPC_COM_PT_CSTransition8_001() : CSTransition8_001("LPC");
    public sealed class ATC_LPP_COM_PT_CSTransition8_001() : CSTransition8_001("LPP");


    /// <summary>A deactivated limit ends the failsafe state ([*-TS-033]).</summary>
    public abstract class CSTransition8_002(String UseCase)
        : ATransitionCase(UseCase, "COM_PT_CSTransition8_002", 8, "CF_CS_FS",
                          PowerLimitationState.UnlimitedControlled, LimitMessages.ValueDeactivatedDeleteDuration, Accepted: true);

    public sealed class ATC_LPC_COM_PT_CSTransition8_002() : CSTransition8_002("LPC");
    public sealed class ATC_LPP_COM_PT_CSTransition8_002() : CSTransition8_002("LPP");

    #endregion

    #region Transition 9 - the failsafe state to limited

    /// <summary>An accepted activated limit ends the failsafe state ([*-TS-032]).</summary>
    public abstract class CSTransition9_001(String UseCase)
        : ATransitionCase(UseCase, "COM_PT_CSTransition9_001", 9, "CF_CS_FS",
                          PowerLimitationState.Limited, LimitMessages.ValueActivatedDeleteDuration, Accepted: true)
    {
        protected override Int32 DurationValue => 2;
    }

    public sealed class ATC_LPC_COM_PT_CSTransition9_001() : CSTransition9_001("LPC");
    public sealed class ATC_LPP_COM_PT_CSTransition9_001() : CSTransition9_001("LPP");

    #endregion

    #region Transition 10 - the failsafe state to unlimited/autonomous

    /// <summary>
    /// The failsafe duration minimum expires and the controllable system may stop
    /// holding itself back ([*-TS-012], [*-TS-022], [*-TS-022/3]).
    /// </summary>
    public abstract class CSTransition10_001(String UseCase) : APowerLimitationCase(UseCase, "COM_PT_CSTransition10_001")
    {

        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            var scenario = await Reach(Context, "CF_CS_UnlCntrl", CancellationToken);
            var minimum  = scenario.Sheet.FailsafeDuration(2);

            await Context.Step("1",
                               "Send an EG Failsafe Duration Minimum write command.",
                               "The CS receives and accepts the write command.",
                               async step => {

                                   var refused = await scenario.WriteFailsafe(DurationMinimum:    minimum,
                                                                              CancellationToken:  CancellationToken);

                                   step.Require(refused is null,
                                                $"the write was refused: {refused?.Description}");

                               });

            await Context.Step("2",
                               "Simulate an interrupted connection, e.g. by disconnecting the network.",
                               "The network connection to the EG is interrupted.",
                               _ => {
                                   scenario.Disconnect();
                                   return Task.CompletedTask;
                               });

            await Context.Step("3",
                               "Wait for configuration change of the CS for 130 seconds.",
                               "The CS changes its configuration to CF_CS_FS within 130 seconds since communication interrupt.",
                               async step => {

                                   await scenario.WaitForStateChange(TimeSpan.FromSeconds(130), CancellationToken);

                                   step.Require(scenario.State == PowerLimitationState.FailsafeState,
                                                $"the controllable system is in {scenario.State} rather than FailsafeState");

                               });

            await Context.Step("4",
                               "Wait for the Failsafe Duration Minimum to expire.",
                               "The Failsafe Duration Minimum of the CS expired.",
                               async step => {

                                   await scenario.Advance(minimum, CancellationToken);
                                   step.Observe($"{minimum.TotalMinutes:F0} minutes of failsafe time passed");

                               });

            await Context.Step("5",
                               "Wait for configuration change of the CS for 130 seconds.",
                               "The CS changes its configuration to CF_CS_UnlAuto or stays in CF_CS_FS.",
                               async step => {

                                   await scenario.Advance(TimeSpan.FromSeconds(130), CancellationToken);

                                   step.Observe($"the controllable system is in {scenario.State}");

                                   step.Require(scenario.State is PowerLimitationState.UnlimitedAutonomous
                                                               or PowerLimitationState.FailsafeState,
                                                $"the controllable system is in {scenario.State}, which is neither " +
                                                $"UnlimitedAutonomous nor FailsafeState (transition 10 of section 2.3.3)");

                               });

        }

    }

    public sealed class ATC_LPC_COM_PT_CSTransition10_001() : CSTransition10_001("LPC");
    public sealed class ATC_LPP_COM_PT_CSTransition10_001() : CSTransition10_001("LPP");


    /// <summary>
    /// A heartbeat arrives in the failsafe state but no limit follows it
    /// ([*-TS-022], [*-TS-022/2]).
    /// </summary>
    public abstract class CSTransition10_002(String UseCase)
        : ATimedTransitionCase(UseCase, "COM_PT_CSTransition10_002", 10, "CF_CS_FS",
                               PowerLimitationState.UnlimitedAutonomous, PowerLimitationState.FailsafeState)
    {
        protected override Boolean HeartbeatFirst => true;
    }

    public sealed class ATC_LPC_COM_PT_CSTransition10_002() : CSTransition10_002("LPC");
    public sealed class ATC_LPP_COM_PT_CSTransition10_002() : CSTransition10_002("LPP");

    #endregion

    #region Transition 11 - unlimited/autonomous to unlimited/controlled

    /// <summary>A rejected limit still ends "unlimited/autonomous" ([*-TS-031], [*-TS-035/1]).</summary>
    public abstract class CSTransition11_001(String UseCase)
        : ATransitionCase(UseCase, "COM_PT_CSTransition11_001", 11, "CF_CS_UnlAuto",
                          PowerLimitationState.UnlimitedControlled, LimitMessages.ValueActivatedDeleteDuration, Accepted: false);

    public sealed class ATC_LPC_COM_PT_CSTransition11_001() : CSTransition11_001("LPC");
    public sealed class ATC_LPP_COM_PT_CSTransition11_001() : CSTransition11_001("LPP");


    /// <summary>A deactivated limit ends "unlimited/autonomous" ([*-TS-033]).</summary>
    public abstract class CSTransition11_002(String UseCase)
        : ATransitionCase(UseCase, "COM_PT_CSTransition11_002", 11, "CF_CS_UnlAuto",
                          PowerLimitationState.UnlimitedControlled, LimitMessages.ValueDeactivated, Accepted: true);

    public sealed class ATC_LPC_COM_PT_CSTransition11_002() : CSTransition11_002("LPC");
    public sealed class ATC_LPP_COM_PT_CSTransition11_002() : CSTransition11_002("LPP");

    #endregion

    #region Transition 12 - unlimited/autonomous to limited

    /// <summary>An accepted activated limit ends "unlimited/autonomous" ([*-TS-032]).</summary>
    public abstract class CSTransition12_001(String UseCase)
        : ATransitionCase(UseCase, "COM_PT_CSTransition12_001", 12, "CF_CS_UnlAuto",
                          PowerLimitationState.Limited, LimitMessages.ValueActivatedDeleteDuration, Accepted: true)
    {
        protected override Int32 DurationValue => 2;
    }

    public sealed class ATC_LPC_COM_PT_CSTransition12_001() : CSTransition12_001("LPC");
    public sealed class ATC_LPP_COM_PT_CSTransition12_001() : CSTransition12_001("LPP");

    #endregion


    #region The two use case instances

    /// <summary>
    /// The controllable system accepts the opening deactivation and then refuses
    /// a real limit for one of the reasons the use case permits.
    ///
    /// The two instances differ by exactly one item on a list. A controllable
    /// system on an energy manager may refuse because uncontrolled loads prevent
    /// it from achieving the limit; one which is a single appliance may not, since
    /// it has no uncontrolled loads to blame. Everything else about the two cases
    /// is identical, which is why they share a body.
    /// </summary>
    /// <param name="UseCase">"LPC" or "LPP".</param>
    /// <param name="Suffix">The rest of the official identifier.</param>
    /// <param name="Reason">Why the controllable system refuses.</param>
    public abstract class AInstanceRejectionCase(String  UseCase,
                                                 String  Suffix,
                                                 String  Reason) : APowerLimitationCase(UseCase, Suffix)
    {

        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            var scenario = await Reach(Context, "CF_CS_Init", CancellationToken);

            await Context.Step("2",
                               "Send an EG heartbeat.",
                               "The CS receives the heartbeat.",
                               async _ => await scenario.Heartbeat(CancellationToken));

            await Context.Step("3",
                               "Send an EG APCL deactivation write command.",
                               "The CS receives and accepts the write command. The CS changes its configuration to CF_CS_UnlCntrl.",
                               async step => {

                                   var refused = await scenario.WriteLimit(LimitMessages.ValueDeactivatedDeleteDuration,
                                                                          scenario.Sheet.Limit(2),
                                                                          CancellationToken: CancellationToken);

                                   step.Require(refused is null,
                                                $"the write was refused: {refused?.Description}");

                                   step.Require(scenario.State == PowerLimitationState.UnlimitedControlled,
                                                $"the controllable system is in {scenario.State} rather than UnlimitedControlled");

                               });

            await Context.Step("4",
                               "Send an EG APCL write command.",
                               $"The CS receives and rejects the write command due to exceptions permitted by the use case ({Reason}).",
                               async step => {

                                   // The permitted reason, made to happen. On a
                                   // real device this is arranged through
                                   // whatever the manufacturer documented in the
                                   // parameter sheet - a debug interface, a load
                                   // switched on, a safety input pulled.
                                   scenario.System.CanApplyLimit = _ => false;

                                   var refused = await scenario.WriteLimit(LimitMessages.ValueActivatedDuration,
                                                                          scenario.Sheet.Limit(3),
                                                                          scenario.Sheet.LimitDuration(1),
                                                                          CancellationToken);

                                   step.Observe($"the write was {Outcome(refused)}");

                                   step.Require(refused is not null,
                                                "the write was accepted although the controllable system was made unable to apply it");

                                   step.Require(scenario.State == PowerLimitationState.UnlimitedControlled,
                                                $"the controllable system is in {scenario.State} rather than staying in " +
                                                $"UnlimitedControlled after refusing the limit (rule 907/1)");

                               });

        }

    }


    public abstract class INS1_CSTransition1_001(String UseCase)
        : AInstanceRejectionCase(UseCase, "INS1_PT_CSTransition1_001",
                                 "self-protection, safety, law, or uncontrolled loads");

    public sealed class ATC_LPC_INS1_PT_CSTransition1_001() : INS1_CSTransition1_001("LPC");
    public sealed class ATC_LPP_INS1_PT_CSTransition1_001() : INS1_CSTransition1_001("LPP");


    public abstract class INS2_CSTransition1_001(String UseCase)
        : AInstanceRejectionCase(UseCase, "INS2_PT_CSTransition1_001",
                                 "self-protection, safety or law");

    public sealed class ATC_LPC_INS2_PT_CSTransition1_001() : INS2_CSTransition1_001("LPC");
    public sealed class ATC_LPP_INS2_PT_CSTransition1_001() : INS2_CSTransition1_001("LPP");

    #endregion

}
