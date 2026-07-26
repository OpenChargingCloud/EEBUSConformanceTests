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

    #region ATC_*_COM_NT_CSConnection_001

    /// <summary>
    /// Nothing counts before the first heartbeat ([*-TS-004], [*-TS-036]).
    ///
    /// The rule exists so that a controllable system cannot be limited by a
    /// device which has not established that it is there. A write which arrives
    /// on a fresh connection with no heartbeat behind it might be a stale packet,
    /// a replay, or a device which crashed halfway through starting up.
    /// </summary>
    public abstract class CSConnection_001(String UseCase) : APowerLimitationCase(UseCase, "COM_NT_CSConnection_001")
    {

        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            var scenario = await Reach(Context, "CF_CS_Init", CancellationToken);
            var value    = scenario.Sheet.Limit(3);

            await Context.Step("2",
                               "Send an EG APCL deactivation write command.",
                               "The CS receives and rejects the write command.",
                               async step => {

                                   var refused = await scenario.WriteLimit(LimitMessages.ValueDeactivatedDeleteDuration,
                                                                          value, CancellationToken: CancellationToken);

                                   step.Observe($"the write was {Outcome(refused)}");

                                   step.Require(refused is not null,
                                                "the write was accepted although no heartbeat had arrived yet");

                                   step.Require(scenario.State == PowerLimitationState.Init,
                                                $"the controllable system left \"init\" for {scenario.State} on a write it should not have evaluated");

                               });

            await Context.Step("3",
                               "Send an EG heartbeat.",
                               "The CS receives the heartbeat.",
                               async _ => await scenario.Heartbeat(CancellationToken));

            await Context.Step("4",
                               "Send an EG APCL deactivation write command.",
                               "The CS receives and accepts the write command. The CS changes its configuration to CF_CS_UnlCntrl.",
                               async step => {

                                   var refused = await scenario.WriteLimit(LimitMessages.ValueDeactivatedDeleteDuration,
                                                                          value, CancellationToken: CancellationToken);

                                   step.Require(refused is null,
                                                $"the same write after a heartbeat was refused: {refused?.Description}");

                                   step.Require(scenario.State == PowerLimitationState.UnlimitedControlled,
                                                $"the controllable system is in {scenario.State} rather than UnlimitedControlled");

                               });

        }

    }

    public sealed class ATC_LPC_COM_NT_CSConnection_001() : CSConnection_001("LPC");
    public sealed class ATC_LPP_COM_NT_CSConnection_001() : CSConnection_001("LPP");

    #endregion

    #region ATC_*_COM_PT_CSConnection_002 and _004

    /// <summary>
    /// Nothing else counts before the first heartbeat *and* the first limit
    /// ([*-TS-036], [*-TS-037]).
    ///
    /// A second gate behind the first, and a stricter one: the failsafe values
    /// are what the device falls back on when everything else fails, so a device
    /// which has not yet been given a limit has no business letting anybody
    /// change them.
    /// </summary>
    /// <param name="UseCase">"LPC" or "LPP".</param>
    /// <param name="Suffix">The rest of the official identifier.</param>
    /// <param name="FailsafeLimit">Whether the guarded write is of the failsafe limit rather than of its duration.</param>
    public abstract class ACSConnectionGateCase(String   UseCase,
                                                String   Suffix,
                                                Boolean  FailsafeLimit) : APowerLimitationCase(UseCase, Suffix)
    {

        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            var scenario  = await Reach(Context, "CF_CS_Init", CancellationToken);
            var what      = FailsafeLimit ? "FCAPL" : "Failsafe Duration Minimum";

            Task<cloud.charging.open.protocols.EEBUS.SPINE.Model.ResultDataType?> Write()

                => FailsafeLimit
                       ? scenario.WriteFailsafe(scenario.Sheet.Failsafe(3), CancellationToken: CancellationToken)
                       : scenario.WriteFailsafe(DurationMinimum: scenario.Sheet.FailsafeDuration(2), CancellationToken: CancellationToken);

            await Context.Step("2",
                               "Send an EG heartbeat.",
                               "The CS receives the heartbeat.",
                               async _ => await scenario.Heartbeat(CancellationToken));

            await Context.Step("3",
                               $"Send an EG {what} write command.",
                               "The CS receives and rejects the write command. The EG receives a NACK from the CS.",
                               async step => {

                                   var refused = await Write();

                                   step.Observe($"the write was {Outcome(refused)}");

                                   step.Require(refused is not null,
                                                $"the {what} was written although no limit had been accepted yet (rule 037)");

                               });

            await Context.Step("4",
                               "Send an EG APCL deactivation write command.",
                               "The CS receives and accepts the write command. The CS changes its configuration to CF_CS_UnlCntrl.",
                               async step => {

                                   var refused = await scenario.WriteLimit(LimitMessages.ValueDeactivatedDeleteDuration,
                                                                          scenario.Sheet.Limit(3), CancellationToken: CancellationToken);

                                   step.Require(refused is null,
                                                $"the write was refused: {refused?.Description}");

                                   step.Require(scenario.State == PowerLimitationState.UnlimitedControlled,
                                                $"the controllable system is in {scenario.State} rather than UnlimitedControlled");

                               });

            await Context.Step("5",
                               $"Send an EG {what} write command.",
                               "The CS receives and accepts the write command.",
                               async step => {

                                   var refused = await Write();

                                   step.Require(refused is null,
                                                $"the {what} was still refused after a heartbeat and a limit: {refused?.Description}");

                               });

        }

    }


    public abstract class CSConnection_002(String UseCase) : ACSConnectionGateCase(UseCase, "COM_PT_CSConnection_002", FailsafeLimit: true);

    public sealed class ATC_LPC_COM_PT_CSConnection_002() : CSConnection_002("LPC");
    public sealed class ATC_LPP_COM_PT_CSConnection_002() : CSConnection_002("LPP");


    public abstract class CSConnection_004(String UseCase) : ACSConnectionGateCase(UseCase, "COM_PT_CSConnection_004", FailsafeLimit: false);

    public sealed class ATC_LPC_COM_PT_CSConnection_004() : CSConnection_004("LPC");
    public sealed class ATC_LPP_COM_PT_CSConnection_004() : CSConnection_004("LPP");

    #endregion

    #region ATC_*_COM_PT_CSConnection_003

    /// <summary>
    /// Neither the limit nor the failsafe limit is ever below zero
    /// ([*-TS-005], [*-TS-018], [*-TS-038]).
    /// </summary>
    public abstract class CSConnection_003(String UseCase) : APowerLimitationCase(UseCase, "COM_PT_CSConnection_003")
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
                               "Send an EG APCL write command with a negative value.",
                               "The CS receives and rejects the write command. The CS changes its configuration to CF_CS_UnlCntrl.",
                               async step => {

                                   var refused = await scenario.WriteLimit(LimitMessages.ValueActivatedDeleteDuration,
                                                                          scenario.Sheet.Limit(6), CancellationToken: CancellationToken);

                                   step.Require(refused is not null,
                                                "a limit below zero was accepted");

                                   step.Require(scenario.State == PowerLimitationState.UnlimitedControlled,
                                                $"the controllable system is in {scenario.State} rather than UnlimitedControlled");

                               });

            await Context.Step("4",
                               "Send an EG FCAPL write command with a negative value.",
                               "The CS receives and rejects the write command. The EG receives a NACK from the CS.",
                               async step => {

                                   var refused = await scenario.WriteFailsafe(scenario.Sheet.Failsafe(6),
                                                                              CancellationToken: CancellationToken);

                                   step.Require(refused is not null,
                                                "a failsafe limit below zero was accepted");

                               });

            await Context.Step("5",
                               "Send an EG FCAPL write command.",
                               "The CS receives and accepts the write command.",
                               async step => {

                                   var refused = await scenario.WriteFailsafe(scenario.Sheet.Failsafe(3),
                                                                              CancellationToken: CancellationToken);

                                   step.Require(refused is null,
                                                $"a valid failsafe limit was refused: {refused?.Description}");

                               });

        }

    }

    public sealed class ATC_LPC_COM_PT_CSConnection_003() : CSConnection_003("LPC");
    public sealed class ATC_LPP_COM_PT_CSConnection_003() : CSConnection_003("LPP");

    #endregion

    #region ATC_*_COM_PT_CSConnection_005

    /// <summary>
    /// A failsafe duration minimum above what the device accepts
    /// ([*-TS-014], [*-TS-015], [*-TS-015/1], [*-TS-016]).
    ///
    /// Both answers are conformant and the case exists to see which: the device
    /// may accept the value, or it may refuse it - and then it SHALL move to its
    /// own maximum rather than leaving the old value in place. The second half is
    /// the one which is easy to miss, and it is the one which matters: a device
    /// which refuses and changes nothing has left the energy guard believing a
    /// number which is not true.
    /// </summary>
    public abstract class CSConnection_005(String UseCase) : APowerLimitationCase(UseCase, "COM_PT_CSConnection_005")
    {

        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            var scenario  = await Reach(Context, "CF_CS_Init", CancellationToken);
            var tooLong   = scenario.Sheet.FailsafeDuration(3);
            var maximum   = scenario.Sheet.MaximumFailsafeDuration;

            var accepted  = false;

            await Context.Step("2",
                               "Send an EG heartbeat.",
                               "The CS receives the heartbeat.",
                               async _ => await scenario.Heartbeat(CancellationToken));

            await Context.Step("3",
                               "Send an EG APCL deactivation write command.",
                               "The CS receives and accepts the write command. The CS changes its configuration to CF_CS_UnlCntrl.",
                               async step => {

                                   var refused = await scenario.WriteLimit(LimitMessages.ValueDeactivatedDeleteDuration,
                                                                          scenario.Sheet.Limit(3), CancellationToken: CancellationToken);

                                   step.Require(refused is null,
                                                $"the write was refused: {refused?.Description}");

                               });

            await Context.Step("4",
                               "Send an EG Failsafe Duration Minimum write command.",
                               "The CS receives and accepts or rejects the write command.",
                               async step => {

                                   var refused = await scenario.WriteFailsafe(DurationMinimum:    tooLong,
                                                                              CancellationToken:  CancellationToken);

                                   accepted = refused is null;

                                   step.Observe($"{tooLong.TotalHours:F2} h against a maximum of {maximum.TotalHours:F2} h was {Outcome(refused)}");

                               });

            await Context.Step("5",
                               "Check the Failsafe Duration Minimum value of the CS.",
                               "The CS changed the Failsafe Duration Minimum - either to the value sent, or to its own maximum.",
                               async step => {

                                   var (_, now) = await scenario.ReadFailsafe(CancellationToken);

                                   step.Observe($"it now reads {now?.TotalHours.ToString("F2") ?? "nothing"} h");

                                   step.Require(now is not null,
                                                "the controllable system reports no failsafe duration minimum at all");

                                   step.Require(accepted
                                                    ? now == tooLong
                                                    : now == maximum,
                                                accepted
                                                    ? $"the write was accepted but the value is {now!.Value.TotalHours:F2} h rather than the {tooLong.TotalHours:F2} h sent"
                                                    : $"the write was rejected and the value is {now!.Value.TotalHours:F2} h rather than the device's own " +
                                                      $"maximum of {maximum.TotalHours:F2} h (rule 022/5)");

                               });

        }

    }

    public sealed class ATC_LPC_COM_PT_CSConnection_005() : CSConnection_005("LPC");
    public sealed class ATC_LPP_COM_PT_CSConnection_005() : CSConnection_005("LPP");

    #endregion

    #region ATC_*_COM_PT_CSConnection_006

    /// <summary>
    /// A limit larger than the device could ever draw is accepted and clamped
    /// rather than refused ([*-TS-035/4]).
    /// </summary>
    public abstract class CSConnection_006(String UseCase) : APowerLimitationCase(UseCase, "COM_PT_CSConnection_006")
    {

        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            var scenario = await Reach(Context, "CF_CS_Init", CancellationToken);
            var tooLarge = scenario.Sheet.Limit(5);

            await Context.Step("2",
                               "Send an EG heartbeat.",
                               "The CS receives the heartbeat.",
                               async _ => await scenario.Heartbeat(CancellationToken));

            await Context.Step("3",
                               "Send an EG APCL activation write command.",
                               "The CS receives and accepts the write command. The CS changes its configuration to CF_CS_Limited_wo_dur.",
                               async step => {

                                   var refused = await scenario.WriteLimit(LimitMessages.ValueActivatedDeleteDuration,
                                                                          tooLarge, CancellationToken: CancellationToken);

                                   step.Require(refused is null,
                                                $"a limit above the device's maximum was refused rather than altered: {refused?.Description}");

                                   step.Require(scenario.State == PowerLimitationState.Limited,
                                                $"the controllable system is in {scenario.State} rather than Limited");

                               });

            await Context.Step("4",
                               "Check the APCL value of the CS.",
                               "The CS changes its APCL value according to the value sent.",
                               async step => {

                                   var (value, active) = await scenario.ReadLimit(CancellationToken);

                                   step.Observe($"the limit reads {value} W, {(active ? "activated" : "deactivated")}");

                                   step.Require(value is not null,
                                                "the controllable system reports no limit at all");

                                   step.Require(value == tooLarge || value == scenario.Sheet.LimitMax,
                                                $"the limit reads {value} W, which is neither the {tooLarge} W sent nor the " +
                                                $"{scenario.Sheet.LimitMax} W the device declared as its maximum");

                               });

        }

    }

    public sealed class ATC_LPC_COM_PT_CSConnection_006() : CSConnection_006("LPC");
    public sealed class ATC_LPP_COM_PT_CSConnection_006() : CSConnection_006("LPP");

    #endregion

    #region ATC_*_COM_PT_CSConnection_007

    /// <summary>
    /// The whole declared range of limits, one after another
    /// ([*-TS-001], [*-TS-035], [*-TS-035/4]).
    ///
    /// Five specific test cases out of one abstract one, and the reason to run
    /// all five is arithmetic rather than protocol: a device which stores its
    /// limit in the wrong unit, or rounds it to hundreds, or clamps it at a value
    /// nobody asked about, passes a single mid-range value and fails at the ends.
    /// </summary>
    public abstract class CSConnection_007(String UseCase) : APowerLimitationCase(UseCase, "COM_PT_CSConnection_007")
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
                                                                          scenario.Sheet.Limit(3), CancellationToken: CancellationToken);

                                   step.Require(refused is null,
                                                $"the write was refused: {refused?.Description}");

                               });

            // One specific test case per value, run in one go because the
            // abstract case is one conversation and splitting it would need five
            // commissionings to prove the same thing.
            foreach (var number in new[] { 1, 2, 3, 4, 5 })
                await Context.Step($"4.{number}",
                                   $"Send an EG APCL activation write command with {Values(Limitation)}_{number:D2}.",
                                   "The CS receives and accepts the write command, changes to CF_CS_Limited_wo_dur, " +
                                   "and changes its APCL value according to the value sent.",
                                   async step => {

                                       var value    = scenario.Sheet.Limit(number);

                                       await scenario.Heartbeat(CancellationToken);

                                       var refused  = await scenario.WriteLimit(LimitMessages.ValueActivatedDeleteDuration,
                                                                                value, CancellationToken: CancellationToken);

                                       var (stored, _) = await scenario.ReadLimit(CancellationToken);

                                       step.Observe($"{value} W was {Outcome(refused)} and reads back as {stored} W");

                                       step.Require(refused is null,
                                                    $"the limit {value} W within the declared range was refused: {refused?.Description}");

                                       step.Require(scenario.State == PowerLimitationState.Limited,
                                                    $"the controllable system is in {scenario.State} rather than Limited");

                                       // The fifth value is above the declared
                                       // maximum, which rule 035/4 lets the
                                       // device alter to the largest it can hold.
                                       step.Require(stored == value || (number == 5 && stored == scenario.Sheet.LimitMax),
                                                    $"the limit reads back as {stored} W rather than the {value} W sent");

                                   });

        }

        private static String Values(String UseCase)
            => UseCase == "LPP" ? "APPL" : "APCL";

    }

    public sealed class ATC_LPC_COM_PT_CSConnection_007() : CSConnection_007("LPC");
    public sealed class ATC_LPP_COM_PT_CSConnection_007() : CSConnection_007("LPP");

    #endregion

    #region ATC_*_COM_PT_CSConnection_008

    /// <summary>
    /// The whole declared range of failsafe values, and the three durations
    /// ([*-TS-001], [*-TS-015/1], [*-TS-016], [*-TS-038]).
    /// </summary>
    public abstract class CSConnection_008(String UseCase) : APowerLimitationCase(UseCase, "COM_PT_CSConnection_008")
    {

        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            var scenario = await Reach(Context, "CF_CS_UnlCntrl", CancellationToken);

            foreach (var number in new[] { 1, 2, 3, 4, 5 })
                await Context.Step($"1.{number}",
                                   $"Send an EG FCAPL write command with value {number}.",
                                   "The CS receives and accepts the write command and changes its FCAPL value accordingly.",
                                   async step => {

                                       var value    = scenario.Sheet.Failsafe(number);
                                       var refused  = await scenario.WriteFailsafe(value, CancellationToken: CancellationToken);
                                       var (now, _) = await scenario.ReadFailsafe(CancellationToken);

                                       step.Observe($"{value} W was {Outcome(refused)} and reads back as {now} W");

                                       step.Require(refused is null,
                                                    $"the failsafe limit {value} W was refused: {refused?.Description}");

                                       step.Require(now == value,
                                                    $"the failsafe limit reads back as {now} W rather than the {value} W sent");

                                   });

            // The three durations mean three different things: one below the two
            // hour floor and therefore refusable, one the device should take, and
            // one above its own maximum where either answer is conformant.
            await Context.Step("2.1",
                               "Send an EG Failsafe Duration Minimum write command of 1 h 54 min.",
                               "The CS receives and rejects the write command.",
                               async step => {

                                   var refused = await scenario.WriteFailsafe(DurationMinimum:    scenario.Sheet.FailsafeDuration(1),
                                                                              CancellationToken:  CancellationToken);

                                   step.Require(refused is not null,
                                                "a failsafe duration minimum below the two hour floor was accepted (rule 022/1)");

                               });

            await Context.Step("2.2",
                               "Send an EG Failsafe Duration Minimum write command within the device's range.",
                               "The CS receives and accepts the write command and changes its value accordingly.",
                               async step => {

                                   var value    = scenario.Sheet.FailsafeDuration(2);
                                   var refused  = await scenario.WriteFailsafe(DurationMinimum:    value,
                                                                               CancellationToken:  CancellationToken);

                                   var (_, now) = await scenario.ReadFailsafe(CancellationToken);

                                   step.Require(refused is null,
                                                $"a failsafe duration minimum within the device's own range was refused: {refused?.Description}");

                                   step.Require(now == value,
                                                $"the failsafe duration minimum reads back as {now} rather than the {value} sent");

                               });

            await Context.Step("2.3",
                               "Send an EG Failsafe Duration Minimum write command above the device's maximum.",
                               "The CS accepts it, or rejects it and changes its value to its own maximum.",
                               async step => {

                                   var value     = scenario.Sheet.FailsafeDuration(3);
                                   var refused   = await scenario.WriteFailsafe(DurationMinimum:    value,
                                                                                CancellationToken:  CancellationToken);

                                   var (_, now)  = await scenario.ReadFailsafe(CancellationToken);

                                   step.Observe($"{value.TotalHours:F2} h was {Outcome(refused)} and reads back as {now?.TotalHours:F2} h");

                                   step.Require(refused is null
                                                    ? now == value
                                                    : now == scenario.Sheet.MaximumFailsafeDuration,
                                                refused is null
                                                    ? $"the write was accepted but the value reads back as {now}"
                                                    : $"the write was rejected and the value reads back as {now} rather than the " +
                                                      $"device's own maximum of {scenario.Sheet.MaximumFailsafeDuration} (rule 022/5)");

                               });

        }

    }

    public sealed class ATC_LPC_COM_PT_CSConnection_008() : CSConnection_008("LPC");
    public sealed class ATC_LPP_COM_PT_CSConnection_008() : CSConnection_008("LPP");

    #endregion

    #region ATC_*_COM_PT_CSConnection_009

    /// <summary>
    /// The controllable system comes back by itself after everything lost power
    /// ([*-TS-046]).
    /// </summary>
    public abstract class CSConnection_009(String UseCase) : APowerLimitationCase(UseCase, "COM_PT_CSConnection_009")
    {

        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            var scenario = await Reach(Context, "CF_CS_UnlCntrl", CancellationToken);

            await Context.Step("1",
                               "Switch off the power supply to both the tester and the DUT.",
                               "Both devices turn off.",
                               _ => {
                                   scenario.Disconnect();
                                   return Task.CompletedTask;
                               });

            await Context.Step("2",
                               "Wait for a 90 seconds interval.",
                               "",
                               async _ => await scenario.Advance(TimeSpan.FromSeconds(90), CancellationToken));

            await Context.Step("3",
                               "Switch on the power supply to both the tester and the DUT.",
                               "Both devices turn on.",
                               async _ => {
                                   await scenario.Restart(CancellationToken: CancellationToken);
                                   scenario.Reconnect();
                               });

            await Context.Step("4",
                               "Wait for the tester to be in CF_EG_ConnectionEstablished and for the CS to be at least in CF_CS_Init.",
                               "If the start up takes longer than 120 seconds the CS may already have changed to CF_CS_UnlAuto.",
                               async step => {

                                   await scenario.Advance(scenario.Sheet.StartUpDurationSystem, CancellationToken);

                                   step.Observe($"the controllable system came back in {scenario.State}");

                                   step.Require(scenario.State is PowerLimitationState.Init
                                                              or PowerLimitationState.UnlimitedAutonomous,
                                                $"the controllable system came back in {scenario.State} rather than in \"init\"");

                               });

            await Context.Step("5",
                               "Send an EG heartbeat.",
                               "The CS receives the heartbeat.",
                               async _ => await scenario.Heartbeat(CancellationToken));

            await Context.Step("6",
                               "Send an EG APCL deactivation write command.",
                               "The CS receives and accepts the write command and changes its configuration to CF_CS_UnlCntrl.",
                               async step => {

                                   var refused = await scenario.WriteLimit(LimitMessages.ValueDeactivatedDeleteDuration,
                                                                          scenario.Sheet.Limit(3), CancellationToken: CancellationToken);

                                   step.Observe($"the write was {Outcome(refused)}");

                                   step.Require(refused is null,
                                                $"the controllable system did not accept a limit after the black start: {refused?.Description}");

                                   step.Require(scenario.State == PowerLimitationState.UnlimitedControlled,
                                                $"the controllable system is in {scenario.State} rather than UnlimitedControlled");

                               });

        }

    }

    public sealed class ATC_LPC_COM_PT_CSConnection_009() : CSConnection_009("LPC");
    public sealed class ATC_LPP_COM_PT_CSConnection_009() : CSConnection_009("LPP");

    #endregion


    #region ATC_*_COM_PT_CSInit_001 and _002

    /// <summary>
    /// A controllable system starts limited and deactivated
    /// ([*-TS-009/3], [*-TS-011], [*-TS-017], [*-TS-019]).
    ///
    /// The most important two seconds in the use case. A device which comes up
    /// unlimited because nobody has told it otherwise yet has inverted the whole
    /// point: the failsafe value is what applies until an energy guard says
    /// something, not after it stops.
    /// </summary>
    public abstract class CSInit_001(String UseCase) : APowerLimitationCase(UseCase, "COM_PT_CSInit_001")
    {

        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            var scenario = await Reach(Context, "CF_CS_Reset_Init", CancellationToken);

            await Context.Step("2",
                               "Check the FCAPL parameter of the CS.",
                               "The CS limits its power consumption with its pre-configured failsafe value.",
                               async step => {

                                   var (limit, _) = await scenario.ReadFailsafe(CancellationToken);

                                   step.Observe($"the failsafe limit reads {limit} W, and the applied limitation is " +
                                                $"{scenario.System.StateMachine.Limitation}");

                                   step.Require(limit == scenario.Sheet.PreConfiguredFailsafe,
                                                $"the failsafe limit reads {limit} W rather than the pre-configured " +
                                                $"{scenario.Sheet.PreConfiguredFailsafe} W");

                                   step.Require(scenario.System.StateMachine.Limitation == PowerLimitationApplied.FailsafeLimit,
                                                $"the controllable system is holding itself to {scenario.System.StateMachine.Limitation} " +
                                                $"rather than to its failsafe limit (rule 901/1)");

                               });

            await Context.Step("3",
                               "Check if the APCL of the CS is activated or deactivated.",
                               "The APCL of the CS is deactivated.",
                               async step => {

                                   var (_, active) = await scenario.ReadLimit(CancellationToken);

                                   step.Require(!active,
                                                "the limit is activated although the controllable system has just started (rule 009/2)");

                               });

        }

    }

    public sealed class ATC_LPC_COM_PT_CSInit_001() : CSInit_001("LPC");
    public sealed class ATC_LPP_COM_PT_CSInit_001() : CSInit_001("LPP");


    /// <summary>
    /// After a factory reset the device is back to what the manufacturer
    /// declared ([*-TS-009/2], [*-TS-009/3], [*-TS-011], [*-TS-013]).
    /// </summary>
    public abstract class CSInit_002(String UseCase) : APowerLimitationCase(UseCase, "COM_PT_CSInit_002")
    {

        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            var scenario = await Reach(Context, "CF_CS_Reset_Init", CancellationToken);

            await Context.Step("1",
                               "Reset the CS.",
                               "The CS reboots.",
                               async _ => await scenario.Restart(FactoryReset: true, CancellationToken: CancellationToken));

            await Context.Step("3",
                               "Send an EG heartbeat.",
                               "The CS receives the heartbeat.",
                               async _ => await scenario.Heartbeat(CancellationToken));

            await Context.Step("4",
                               "Check if the APCL of the CS is activated or deactivated.",
                               "The APCL of the CS is deactivated.",
                               async step => {

                                   var (_, active) = await scenario.ReadLimit(CancellationToken);

                                   step.Require(!active,
                                                "the limit is activated after a factory reset (rule 009/2)");

                               });

            await Context.Step("5",
                               "Check the PFCAPL value of the CS.",
                               "The value is equal to the one specified in the parameter sheet.",
                               async step => {

                                   var (limit, _) = await scenario.ReadFailsafe(CancellationToken);

                                   step.Require(limit == scenario.Sheet.PreConfiguredFailsafe,
                                                $"the failsafe limit reads {limit} W rather than the declared " +
                                                $"{scenario.Sheet.PreConfiguredFailsafe} W");

                               });

            await Context.Step("6",
                               "Check the PFSDM value of the CS.",
                               "The value is equal to the one specified in the parameter sheet.",
                               async step => {

                                   var (_, duration) = await scenario.ReadFailsafe(CancellationToken);

                                   step.Require(duration == scenario.Sheet.PreConfiguredFailsafeDuration,
                                                $"the failsafe duration minimum reads {duration} rather than the declared " +
                                                $"{scenario.Sheet.PreConfiguredFailsafeDuration}");

                               });

        }

    }

    public sealed class ATC_LPC_COM_PT_CSInit_002() : CSInit_002("LPC");
    public sealed class ATC_LPP_COM_PT_CSInit_002() : CSInit_002("LPP");

    #endregion

    #region ATC_*_COM_PT_CSInit_003

    /// <summary>
    /// What the energy guard wrote survives a reboot
    /// ([*-TS-011/1], [*-TS-013/1], [*-TS-044]).
    /// </summary>
    public abstract class CSInit_003(String UseCase) : APowerLimitationCase(UseCase, "COM_PT_CSInit_003")
    {

        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            var scenario  = await Reach(Context, "CF_CS_Init", CancellationToken);
            var failsafe  = scenario.Sheet.Failsafe(3);
            var duration  = scenario.Sheet.FailsafeDuration(2);

            await Context.Step("2",
                               "Send an EG heartbeat.",
                               "The CS receives the heartbeat.",
                               async _ => await scenario.Heartbeat(CancellationToken));

            await Context.Step("3",
                               "Send an EG APCL deactivation write command.",
                               "The CS receives and accepts the write command and changes to CF_CS_UnlCntrl.",
                               async step => {

                                   var refused = await scenario.WriteLimit(LimitMessages.ValueDeactivatedDeleteDuration,
                                                                          scenario.Sheet.Limit(3), CancellationToken: CancellationToken);

                                   step.Require(refused is null, $"the write was refused: {refused?.Description}");

                               });

            await Context.Step("4",
                               "Send an EG FCAPL write command.",
                               "The CS receives and accepts the write command.",
                               async step => {

                                   var refused = await scenario.WriteFailsafe(failsafe, CancellationToken: CancellationToken);

                                   step.Require(refused is null, $"the write was refused: {refused?.Description}");

                               });

            await Context.Step("6",
                               "Send an EG Failsafe Duration Minimum write command.",
                               "The CS receives and accepts the write command.",
                               async step => {

                                   var refused = await scenario.WriteFailsafe(DurationMinimum:    duration,
                                                                              CancellationToken:  CancellationToken);

                                   step.Require(refused is null, $"the write was refused: {refused?.Description}");

                               });

            await Context.Step("8",
                               "Reboot the CS and wait until it's able to exchange messages.",
                               "The CS restarts in configuration CF_CS_Init.",
                               async step => {

                                   await scenario.Restart(CancellationToken: CancellationToken);

                                   step.Require(scenario.State == PowerLimitationState.Init,
                                                $"the controllable system came back in {scenario.State} rather than in \"init\"");

                               });

            await Context.Step("11",
                               "Check the FCAPL parameter of the CS.",
                               "The CS changed the FCAPL to the value sent in test step 4.",
                               async step => {

                                   var (limit, _) = await scenario.ReadFailsafe(CancellationToken);

                                   step.Require(limit == failsafe,
                                                $"the failsafe limit reads {limit} W rather than the {failsafe} W written before the reboot");

                               });

            await Context.Step("12",
                               "Check the Failsafe Duration Minimum parameter of the CS.",
                               "The CS changed the Failsafe Duration Minimum to the value sent in test step 6.",
                               async step => {

                                   var (_, now) = await scenario.ReadFailsafe(CancellationToken);

                                   step.Require(now == duration,
                                                $"the failsafe duration minimum reads {now} rather than the {duration} written before the reboot");

                               });

        }

    }

    public sealed class ATC_LPC_COM_PT_CSInit_003() : CSInit_003("LPC");
    public sealed class ATC_LPP_COM_PT_CSInit_003() : CSInit_003("LPP");

    #endregion


    #region The three "a rejection changes nothing" cases

    /// <summary>
    /// A limit which cannot be applied leaves a controlled controllable system
    /// exactly where it was.
    ///
    /// The counterpart to transitions 8 and 11: from the failsafe state or from
    /// "unlimited/autonomous" a rejected limit *does* change the state, because
    /// what it proves is that an energy guard is there. From "limited" or
    /// "unlimited/controlled" it changes nothing, because that was already known.
    /// </summary>
    /// <param name="UseCase">"LPC" or "LPP".</param>
    /// <param name="Suffix">The rest of the official identifier.</param>
    /// <param name="From">The test configuration to start in.</param>
    /// <param name="Stays">The state the controllable system has to stay in.</param>
    /// <param name="Activated">Whether the limit is activated in that state.</param>
    public abstract class ARejectionKeepsStateCase(String                UseCase,
                                                   String                Suffix,
                                                   String                From,
                                                   PowerLimitationState  Stays,
                                                   Boolean               Activated) : APowerLimitationCase(UseCase, Suffix)
    {

        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            var scenario = await Reach(Context, From, CancellationToken);

            await Context.Step("1",
                               "Check if the APCL of the CS is activated or deactivated.",
                               $"The APCL of the CS is {(Activated ? "activated" : "deactivated")}.",
                               async step => {

                                   var (_, active) = await scenario.ReadLimit(CancellationToken);

                                   step.Require(active == Activated,
                                                $"the limit is {(active ? "activated" : "deactivated")} in {Stays}, " +
                                                $"which rule 009 says it should not be");

                               });

            await Context.Step("2",
                               "Send an EG APCL write command with a negative value.",
                               $"The CS receives and rejects the write command and stays in {From}.",
                               async step => {

                                   var refused = await scenario.WriteLimit(LimitMessages.ValueActivatedDeleteDuration,
                                                                          scenario.Sheet.Limit(6), CancellationToken: CancellationToken);

                                   step.Observe($"the write was {Outcome(refused)}, and the controllable system is in {scenario.State}");

                                   step.Require(refused is not null,
                                                "a limit below zero was accepted");

                                   step.Require(scenario.State == Stays,
                                                $"the controllable system left {Stays} for {scenario.State} on a limit it rejected");

                               });

        }

    }


    public abstract class CSLimited_001(String UseCase)
        : ARejectionKeepsStateCase(UseCase, "COM_NT_CSLimited_001", "CF_CS_Limited_wo_dur",
                                   PowerLimitationState.Limited, Activated: true);

    public sealed class ATC_LPC_COM_NT_CSLimited_001() : CSLimited_001("LPC");
    public sealed class ATC_LPP_COM_NT_CSLimited_001() : CSLimited_001("LPP");


    public abstract class CSUnlCntrl_001(String UseCase)
        : ARejectionKeepsStateCase(UseCase, "COM_NT_CSUnlCntrl_001", "CF_CS_UnlCntrl",
                                   PowerLimitationState.UnlimitedControlled, Activated: false);

    public sealed class ATC_LPC_COM_NT_CSUnlCntrl_001() : CSUnlCntrl_001("LPC");
    public sealed class ATC_LPP_COM_NT_CSUnlCntrl_001() : CSUnlCntrl_001("LPP");

    #endregion

    #region ATC_*_COM_PT_CSLimited_002

    /// <summary>
    /// A limited controllable system keeps accepting limits while the heartbeat
    /// is briefly absent ([*-TS-001/2], [*-TS-002]).
    ///
    /// Ninety seconds of silence is not two minutes of silence, and the
    /// difference is the whole tolerance the use case allows. A device which
    /// panics early throws away a working control loop over a delayed packet.
    /// </summary>
    public abstract class CSLimited_002(String UseCase) : APowerLimitationCase(UseCase, "COM_PT_CSLimited_002")
    {

        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            var scenario = await Reach(Context, "CF_CS_Init", CancellationToken);
            var value    = scenario.Sheet.Limit(3);

            await Context.Step("2",
                               "Send an EG heartbeat.",
                               "The CS receives the heartbeat.",
                               async _ => await scenario.Heartbeat(CancellationToken));

            await Context.Step("3",
                               "Send an EG APCL activation write command.",
                               "The CS receives and accepts the write command and changes to CF_CS_Limited_wo_dur.",
                               async step => {

                                   var refused = await scenario.WriteLimit(LimitMessages.ValueActivatedDeleteDuration,
                                                                          value, CancellationToken: CancellationToken);

                                   step.Require(refused is null, $"the write was refused: {refused?.Description}");
                                   step.Require(scenario.State == PowerLimitationState.Limited,
                                                $"the controllable system is in {scenario.State} rather than Limited");

                               });

            await Context.Step("4",
                               "Send an EG heartbeat.",
                               "The CS receives the heartbeat.",
                               async _ => await scenario.Heartbeat(CancellationToken));

            await Context.Step("5",
                               "Wait for a 90 second interval.",
                               "",
                               async _ => await scenario.Advance(TimeSpan.FromSeconds(90), CancellationToken));

            await Context.Step("6",
                               "Send an EG APCL activation write command.",
                               "The CS receives and accepts the write command.",
                               async step => {

                                   var refused = await scenario.WriteLimit(LimitMessages.ValueActivatedDeleteDuration,
                                                                          value, CancellationToken: CancellationToken);

                                   step.Observe($"after 90 s without a heartbeat the write was {Outcome(refused)}");

                                   step.Require(refused is null,
                                                $"the write was refused after only 90 seconds of silence: {refused?.Description}");

                               });

            await Context.Step("7",
                               "Send an EG heartbeat.",
                               "The CS receives the heartbeat and stays in CF_CS_Limited_wo_dur.",
                               async step => {

                                   await scenario.Heartbeat(CancellationToken);

                                   step.Require(scenario.State == PowerLimitationState.Limited,
                                                $"the controllable system is in {scenario.State} rather than staying in Limited");

                               });

        }

    }

    public sealed class ATC_LPC_COM_PT_CSLimited_002() : CSLimited_002("LPC");
    public sealed class ATC_LPP_COM_PT_CSLimited_002() : CSLimited_002("LPP");

    #endregion

    #region ATC_*_COM_PT_CSUnlCntrl_002 and _003

    /// <summary>
    /// Exactly one of the two nominal maxima exists, and which one says what kind
    /// of device this is.
    ///
    /// An energy manager reports what it is contractually allowed to draw at the
    /// grid connection point; a single appliance reports what it is physically
    /// able to draw. Reporting both would be a device claiming to be both, and
    /// reporting the wrong one tells an energy guard to plan against a number
    /// which does not constrain anything.
    /// </summary>
    /// <param name="UseCase">"LPC" or "LPP".</param>
    /// <param name="Suffix">The rest of the official identifier.</param>
    /// <param name="Contractual">Whether the contractual maximum is the one which has to be there.</param>
    public abstract class ANominalMaxCase(String   UseCase,
                                          String   Suffix,
                                          Boolean  Contractual) : APowerLimitationCase(UseCase, Suffix)
    {

        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            var scenario  = await Reach(Context, "CF_CS_UnlCntrl", CancellationToken);
            var expected  = Contractual ? "Contractual" : "Power";
            var forbidden = Contractual ? "Power" : "Contractual";

            var (hasContractual, hasPhysical, value) = await scenario.ReadNominalMax(CancellationToken);

            var present   = Contractual ? hasContractual : hasPhysical;
            var absent    = Contractual ? hasPhysical    : hasContractual;

            await Context.Step("1",
                               $"Check the {forbidden} Nominal Max value of the CS.",
                               $"The {forbidden} Nominal Max value is not supported.",
                               step => {

                                   step.Require(!absent,
                                                $"the controllable system reports a {forbidden} Nominal Max, which " +
                                                $"{(Contractual ? "rule 039" : "rule 040")} forbids for this kind of device");

                                   return Task.CompletedTask;

                               });

            await Context.Step("2",
                               $"Check the {expected} Nominal Max value of the CS.",
                               $"The {expected} Nominal Max value is supported and provided, and greater than or equal to zero.",
                               step => {

                                   step.Observe($"it reads {value} W");

                                   step.Require(present,
                                                $"the controllable system does not report a {expected} Nominal Max at all");

                                   step.Require(value is not null,
                                                $"the {expected} Nominal Max is declared but carries no value");

                                   step.Require(value >= 0,
                                                $"the {expected} Nominal Max is {value} W, which is below zero (rule 010)");

                                   if (value == 0)
                                       step.Tolerate($"the {expected} Nominal Max is zero, which is legal and almost certainly not intended");

                                   return Task.CompletedTask;

                               });

        }

    }


    public abstract class CSUnlCntrl_002(String UseCase) : ANominalMaxCase(UseCase, "COM_PT_CSUnlCntrl_002", Contractual: true);

    public sealed class ATC_LPC_COM_PT_CSUnlCntrl_002() : CSUnlCntrl_002("LPC");
    public sealed class ATC_LPP_COM_PT_CSUnlCntrl_002() : CSUnlCntrl_002("LPP");


    public abstract class CSUnlCntrl_003(String UseCase) : ANominalMaxCase(UseCase, "COM_PT_CSUnlCntrl_003", Contractual: false);

    public sealed class ATC_LPC_COM_PT_CSUnlCntrl_003() : CSUnlCntrl_003("LPC");
    public sealed class ATC_LPP_COM_PT_CSUnlCntrl_003() : CSUnlCntrl_003("LPP");

    #endregion


    #region ATC_*_COM_PT_CSFS_001 and ATC_*_COM_NT_CSUnlAuto_001

    /// <summary>
    /// In the two uncontrolled states nothing is evaluated before a heartbeat and
    /// a limit ([*-TS-033], [*-TS-036], [*-TS-037]).
    ///
    /// The same case twice, from the two states which have lost their energy
    /// guard. Both have to be asked, because a device which implements the gate
    /// for one and not the other has a hole exactly where it hurts: a controllable
    /// system in its failsafe state is one which somebody may be trying to talk
    /// out of a limitation it should not leave lightly.
    /// </summary>
    /// <param name="UseCase">"LPC" or "LPP".</param>
    /// <param name="Suffix">The rest of the official identifier.</param>
    /// <param name="From">Which of the two states to start in.</param>
    public abstract class AUncontrolledGateCase(String  UseCase,
                                                String  Suffix,
                                                String  From) : APowerLimitationCase(UseCase, Suffix)
    {

        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            var scenario = await Reach(Context, From, CancellationToken);
            var was      = scenario.State;

            await Context.Step("2",
                               "Send an EG heartbeat.",
                               "The CS receives the heartbeat.",
                               async _ => await scenario.Heartbeat(CancellationToken));

            await Context.Step("3",
                               "Wait for a 90 second interval.",
                               "The 60 second window after the heartbeat has passed.",
                               async _ => await scenario.Advance(TimeSpan.FromSeconds(90), CancellationToken));

            await Context.Step("4",
                               "Send an EG APCL deactivation write command.",
                               $"The CS receives and rejects the write command and stays in {From}.",
                               async step => {

                                   var refused = await scenario.WriteLimit(LimitMessages.ValueDeactivatedDeleteDuration,
                                                                          scenario.Sheet.Limit(3), CancellationToken: CancellationToken);

                                   step.Observe($"the write was {Outcome(refused)}");

                                   step.Require(refused is not null,
                                                "the limit was evaluated although the heartbeat before it was 90 seconds old (rule 036)");

                                   step.Require(scenario.State == was,
                                                $"the controllable system left {was} for {scenario.State} on a write it should not have evaluated");

                               });

            await Context.Step("5",
                               "Send an EG FCAPL write command.",
                               $"The CS receives and rejects the write command and stays in {From}.",
                               async step => {

                                   var refused = await scenario.WriteFailsafe(scenario.Sheet.Failsafe(3),
                                                                              CancellationToken: CancellationToken);

                                   step.Require(refused is not null,
                                                "the failsafe limit was written although no heartbeat and limit had preceded it (rule 037)");

                               });

            await Context.Step("7",
                               "Send an EG heartbeat.",
                               "The CS receives the heartbeat.",
                               async _ => await scenario.Heartbeat(CancellationToken));

            await Context.Step("8",
                               "Send an EG APCL deactivation write command.",
                               "The CS receives and accepts the write command and changes to CF_CS_UnlCntrl.",
                               async step => {

                                   var refused = await scenario.WriteLimit(LimitMessages.ValueDeactivatedDeleteDuration,
                                                                          scenario.Sheet.Limit(3), CancellationToken: CancellationToken);

                                   step.Require(refused is null,
                                                $"the write was refused even though a fresh heartbeat preceded it: {refused?.Description}");

                                   step.Require(scenario.State == PowerLimitationState.UnlimitedControlled,
                                                $"the controllable system is in {scenario.State} rather than UnlimitedControlled");

                               });

            await Context.Step("10",
                               "Send an EG FCAPL write command.",
                               "The CS receives and accepts the write command.",
                               async step => {

                                   var refused = await scenario.WriteFailsafe(scenario.Sheet.Failsafe(3),
                                                                              CancellationToken: CancellationToken);

                                   step.Require(refused is null,
                                                $"the failsafe limit was still refused after a heartbeat and a limit: {refused?.Description}");

                               });

        }

    }


    public abstract class CSFS_001(String UseCase) : AUncontrolledGateCase(UseCase, "COM_PT_CSFS_001", "CF_CS_FS");

    public sealed class ATC_LPC_COM_PT_CSFS_001() : CSFS_001("LPC");
    public sealed class ATC_LPP_COM_PT_CSFS_001() : CSFS_001("LPP");


    public abstract class CSUnlAuto_001(String UseCase) : AUncontrolledGateCase(UseCase, "COM_NT_CSUnlAuto_001", "CF_CS_UnlAuto");

    public sealed class ATC_LPC_COM_NT_CSUnlAuto_001() : CSUnlAuto_001("LPC");
    public sealed class ATC_LPP_COM_NT_CSUnlAuto_001() : CSUnlAuto_001("LPP");

    #endregion

    #region ATC_*_COM_PT_CSFS_002

    /// <summary>
    /// The failsafe state lasts at least as long as the failsafe duration
    /// minimum ([*-TS-012], [*-TS-013]).
    /// </summary>
    public abstract class CSFS_002(String UseCase) : APowerLimitationCase(UseCase, "COM_PT_CSFS_002")
    {

        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            var scenario = await Reach(Context, "CF_CS_FS", CancellationToken);
            var minimum  = scenario.System.StateMachine.FailsafeDurationMinimum;

            scenario.Disconnect();

            await Context.Step("1",
                               "Wait until the Failsafe Duration Minimum expires.",
                               "The CS changes its configuration to CF_CS_UnlAuto or stays in CF_CS_FS after the duration expires.",
                               async step => {

                                   // Just short of the minimum: whatever else
                                   // happens, the failsafe state may not have
                                   // been left yet.
                                   await scenario.Advance(minimum - TimeSpan.FromMinutes(1), CancellationToken);

                                   step.Require(scenario.State == PowerLimitationState.FailsafeState,
                                                $"the controllable system left its failsafe state for {scenario.State} after " +
                                                $"{(minimum - TimeSpan.FromMinutes(1)).TotalMinutes:F0} of {minimum.TotalMinutes:F0} minutes");

                                   await scenario.Advance(TimeSpan.FromMinutes(3), CancellationToken);

                                   step.Observe($"after the full {minimum.TotalMinutes:F0} minutes it is in {scenario.State}");

                                   step.Require(scenario.State is PowerLimitationState.FailsafeState
                                                              or PowerLimitationState.UnlimitedAutonomous,
                                                $"the controllable system is in {scenario.State}, which is neither the failsafe " +
                                                $"state nor \"unlimited/autonomous\"");

                               });

        }

    }

    public sealed class ATC_LPC_COM_PT_CSFS_002() : CSFS_002("LPC");
    public sealed class ATC_LPP_COM_PT_CSFS_002() : CSFS_002("LPP");

    #endregion

    #region ATC_*_COM_PT_CSFS_003

    /// <summary>
    /// A failsafe duration write is refused while the device is in its failsafe
    /// state ([*-TS-009], [*-TS-009/3]).
    /// </summary>
    public abstract class CSFS_003(String UseCase) : APowerLimitationCase(UseCase, "COM_PT_CSFS_003")
    {

        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            var scenario = await Reach(Context, "CF_CS_FS", CancellationToken);

            await Context.Step("2",
                               "Check if the APCL of the CS is activated or deactivated.",
                               "The APCL of the CS is deactivated.",
                               async step => {

                                   var (_, active) = await scenario.ReadLimit(CancellationToken);

                                   step.Require(!active,
                                                "the limit is activated in the failsafe state (rule 009/2)");

                               });

            await Context.Step("3",
                               "Send an EG heartbeat.",
                               "The CS receives the heartbeat.",
                               async _ => await scenario.Heartbeat(CancellationToken));

            await Context.Step("4",
                               "Send an EG Failsafe Duration Minimum write command.",
                               "The CS receives and rejects the write command and stays in CF_CS_FS.",
                               async step => {

                                   var refused = await scenario.WriteFailsafe(DurationMinimum:    scenario.Sheet.FailsafeDuration(2),
                                                                              CancellationToken:  CancellationToken);

                                   step.Observe($"the write was {Outcome(refused)}, and the controllable system is in {scenario.State}");

                                   step.Require(refused is not null,
                                                "the failsafe duration minimum was written in the failsafe state, before any limit had been accepted (rule 037)");

                                   step.Require(scenario.State == PowerLimitationState.FailsafeState,
                                                $"the controllable system left its failsafe state for {scenario.State}");

                               });

        }

    }

    public sealed class ATC_LPC_COM_PT_CSFS_003() : CSFS_003("LPC");
    public sealed class ATC_LPP_COM_PT_CSFS_003() : CSFS_003("LPP");

    #endregion

    #region ATC_*_COM_PT_CSUnlAuto_002

    /// <summary>
    /// An uncontrolled controllable system still stays below its nominal maximum
    /// ([*-TS-009/3], [*-TS-010], [*-TS-038]).
    ///
    /// The only case in the whole use case catalog whose first step is not on the
    /// wire at all: it compares what the device actually draws against the number
    /// it published. Without a wattmeter it is not applicable rather than passed,
    /// which is why the parameter sheet asks about it.
    /// </summary>
    public abstract class CSUnlAuto_002(String UseCase) : APowerLimitationCase(UseCase, "COM_PT_CSUnlAuto_002")
    {

        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            var scenario = await Reach(Context, "CF_CS_UnlAuto", CancellationToken);

            await Context.Step("1",
                               "Compare the actual power consumption with the pre-configured nominal max parameter.",
                               "The actual power consumption is less than or equal to the nominal max.",
                               async step => {

                                   var (contractual, physical, value) = await scenario.ReadNominalMax(CancellationToken);

                                   step.Require(contractual || physical,
                                                "the controllable system publishes neither nominal maximum, so there is nothing to compare against");

                                   step.Observe($"the {(contractual ? "contractual" : "physical")} nominal maximum reads {value} W; " +
                                                $"the actual power has to be measured by the tester");

                               });

            await Context.Step("2",
                               "Check if the APCL of the CS is activated or deactivated.",
                               "The APCL of the CS is deactivated.",
                               async step => {

                                   var (_, active) = await scenario.ReadLimit(CancellationToken);

                                   step.Require(!active,
                                                "the limit is activated in \"unlimited/autonomous\" (rule 009/2)");

                               });

        }

    }

    public sealed class ATC_LPC_COM_PT_CSUnlAuto_002() : CSUnlAuto_002("LPC");
    public sealed class ATC_LPP_COM_PT_CSUnlAuto_002() : CSUnlAuto_002("LPP");

    #endregion

}
