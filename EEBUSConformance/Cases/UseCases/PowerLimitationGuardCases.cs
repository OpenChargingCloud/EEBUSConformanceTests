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

    #region (class) AGuardAnnouncementCase

    /// <summary>
    /// The three cases which ask an energy guard to introduce itself: after a
    /// reboot, after the connection came back, and after everything lost power
    /// ([*-TS-030]).
    ///
    /// They differ only in how the silence started, and the answer they want is
    /// the same one: a heartbeat and a following limit within 60 seconds. That
    /// window is not arbitrary. A controllable system which has just come up sits
    /// in "init" holding itself to its failsafe value and gives the world 120
    /// seconds to take charge of it; an energy guard which takes longer than that
    /// has left a house limited for no reason other than that nobody said hello.
    /// </summary>
    /// <param name="UseCase">"LPC" or "LPP".</param>
    /// <param name="Suffix">The rest of the official identifier.</param>
    /// <param name="BlackStart">Whether both devices lost power rather than only the connection.</param>
    public abstract class AGuardAnnouncementCase(String   UseCase,
                                                 String   Suffix,
                                                 Boolean  BlackStart) : APowerLimitationCase(UseCase, Suffix)
    {

        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            var scenario = await PowerLimitationScenario.Create(Context.Parameters,
                                                                Limitation,
                                                                DUTIsSystem,
                                                                CancellationToken);

            await Context.Precondition(BlackStart ? "CF_EG_ConnectionEstablished, CF_CS_UnlCntrl"
                                                  : "CF_EG_Reboot, CF_CS_FS",
                                       async () => {

                // The connection has to have existed before it can be lost.
                await scenario.Connect(CancellationToken);
                await scenario.Advance(TimeSpan.FromSeconds(5), CancellationToken);

            });

            await Context.Step("1",
                               BlackStart
                                   ? "Switch off the power supply to both the tester and the DUT."
                                   : "Disconnect the EG from the CS and wait for the reboot to be completed.",
                               BlackStart
                                   ? "Both devices turn off."
                                   : "Reboot completed within StartUpDur_EG.",
                               async _ => {

                                   scenario.Disconnect();
                                   await scenario.Advance(TimeSpan.FromSeconds(120), CancellationToken);

                                   if (BlackStart)
                                       await scenario.Restart(CancellationToken: CancellationToken);

                               });

            // The mark is set before the communication comes back, so that the
            // announcement which follows it is the one being measured.
            var mark = scenario.Observed;

            await Context.Step("2",
                               BlackStart
                                   ? "Switch on the power supply to both the tester and the DUT."
                                   : "Restore the communication between EG and CS.",
                               "The devices find each other again.",
                               async _ => {

                                   scenario.Reconnect();

                                   // Rediscovery is what tells the energy guard
                                   // that communication is possible again -
                                   // which is the trigger rule 913 names.
                                   await scenario.Connect(CancellationToken);

                               });

            await Context.Step("3",
                               "Wait for the EG to send at least one heartbeat and a following APCL write command in time.",
                               "The CS receives at least one EG heartbeat and a following APCL write command within 60 seconds.",
                               async step => {

                                   var (heartbeat, limit, waited) = await scenario.WaitForAnnouncement(TimeSpan.FromSeconds(60),
                                                                                                       mark,
                                                                                                       CancellationToken);

                                   step.Observe(limit
                                                    ? $"the heartbeat and the following limit arrived within {waited.TotalSeconds:F0} s"
                                                    : heartbeat
                                                          ? "a heartbeat arrived, but no limit followed it within 60 s"
                                                          : "no heartbeat arrived within 60 s");

                                   step.Require(heartbeat,
                                                "the energy guard sent no heartbeat within 60 seconds of the communication being restored");

                                   step.Require(limit,
                                                "the energy guard sent a heartbeat but no limit followed it within 60 seconds");

                               });

        }

    }

    #endregion


    #region ATC_*_COM_PT_EGConnection_001, _002 and _003

    /// <summary>The energy guard announces itself after it has rebooted.</summary>
    public abstract class EGConnection_001(String UseCase)
        : AGuardAnnouncementCase(UseCase, "COM_PT_EGConnection_001", BlackStart: false);

    public sealed class ATC_LPC_COM_PT_EGConnection_001() : EGConnection_001("LPC");
    public sealed class ATC_LPP_COM_PT_EGConnection_001() : EGConnection_001("LPP");


    /// <summary>The energy guard announces itself after the connection came back.</summary>
    public abstract class EGConnection_002(String UseCase)
        : AGuardAnnouncementCase(UseCase, "COM_PT_EGConnection_002", BlackStart: false);

    public sealed class ATC_LPC_COM_PT_EGConnection_002() : EGConnection_002("LPC");
    public sealed class ATC_LPP_COM_PT_EGConnection_002() : EGConnection_002("LPP");


    /// <summary>The energy guard reconnects by itself after a black start.</summary>
    public abstract class EGConnection_003(String UseCase)
        : AGuardAnnouncementCase(UseCase, "COM_PT_EGConnection_003", BlackStart: true);

    public sealed class ATC_LPC_COM_PT_EGConnection_003() : EGConnection_003("LPC");
    public sealed class ATC_LPP_COM_PT_EGConnection_003() : EGConnection_003("LPP");

    #endregion


    #region ATC_*_COM_PT_EGMessages_001

    /// <summary>
    /// An external stimulus makes the energy guard limit the controllable system
    /// ([*-TS-001]).
    ///
    /// The stimulus is deliberately outside the protocol - a tariff signal, a
    /// grid operator's message, somebody pressing a button - because the use case
    /// says nothing about why an energy guard decides to limit. What it says is
    /// what has to come out of the decision.
    /// </summary>
    public abstract class EGMessages_001(String UseCase) : APowerLimitationCase(UseCase, "COM_PT_EGMessages_001")
    {

        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            var scenario = await Reach(Context, "CF_CS_UnlCntrl", CancellationToken);

            // Three specific test cases, one per value of the data set - the low
            // end of the declared range, a value inside it and the high end.
            foreach (var number in new[] { 2, 3, 4 })
                await Context.Step($"1.{number - 1}",
                                   "Set an external stimulus signalling the EG to send an activated APCL write command to the CS.",
                                   "The EG is able to send an activated APCL write command. The CS receives and accepts it.",
                                   async step => {

                                       var value    = scenario.Sheet.Limit(number);
                                       var partner  = scenario.SystemAsSeenByGuard
                                                          ?? throw new ConformanceInconclusive("the energy guard does not know the controllable system");

                                       await scenario.Heartbeat(CancellationToken);

                                       var written  = await scenario.Guard.WriteConsumptionLimit(partner, value, true,
                                                                                                 CancellationToken: CancellationToken);

                                       var refused  = written.Result is not null && written.Result.ErrorNumber != 0;

                                       step.Observe($"{value} W was {(refused ? $"refused: {written.Result!.Description}" : "accepted")}, " +
                                                    $"and the controllable system is in {scenario.State}");

                                       step.Require(!refused,
                                                    $"the controllable system refused the limit: {written.Result?.Description}");

                                       step.Require(scenario.State == PowerLimitationState.Limited,
                                                    $"the controllable system is in {scenario.State} rather than Limited");

                                   });

        }

    }

    public sealed class ATC_LPC_COM_PT_EGMessages_001() : EGMessages_001("LPC");
    public sealed class ATC_LPP_COM_PT_EGMessages_001() : EGMessages_001("LPP");

    #endregion

    #region ATC_*_COM_PT_EGMessages_002

    /// <summary>
    /// The energy guard tries again after the controllable system said no
    /// ([*-TS-046]).
    ///
    /// A NACK is not the end of the conversation. The value may have been
    /// momentarily inapplicable, the timing may have been unlucky, the device may
    /// have been busy - and an energy guard which writes once, gets refused and
    /// forgets about it has quietly stopped limiting a device it believes it is
    /// limiting.
    /// </summary>
    public abstract class EGMessages_002(String UseCase) : APowerLimitationCase(UseCase, "COM_PT_EGMessages_002")
    {

        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            var scenario = await Reach(Context, "CF_CS_UnlAuto", CancellationToken);
            var partner  = scenario.SystemAsSeenByGuard
                               ?? throw new ConformanceInconclusive("the energy guard does not know the controllable system");
            var mark     = scenario.Observed;

            await Context.Step("2",
                               "Wait for the EG to send at least one heartbeat.",
                               "The CS receives at least one heartbeat and the connection is maintained.",
                               async step => {

                                   await scenario.Heartbeat(CancellationToken);
                                   step.Observe($"the controllable system is in {scenario.State}");

                               });

            await Context.Step("3",
                               "The CS intentionally rejects the next APCL write command. Wait for the EG to send one.",
                               "The EG sends the activated write command in time and receives a corresponding NACK from the CS.",
                               async step => {

                                   scenario.System.CanApplyLimit = _ => false;
                                   mark = scenario.Observed;

                                   var written = await scenario.Guard.WriteConsumptionLimit(partner,
                                                                                            scenario.Sheet.Limit(3),
                                                                                            true,
                                                                                            CancellationToken: CancellationToken);

                                   step.Require(written.Result is not null && written.Result.ErrorNumber != 0,
                                                "the controllable system was made unable to apply the limit but accepted it anyway");

                               });

            await Context.Step("4",
                               "Wait for the EG to send at least one heartbeat and a following APCL write command in time.",
                               "The CS receives at least one EG heartbeat and a following APCL write command within 60 seconds.",
                               async step => {

                                   scenario.System.CanApplyLimit = null;

                                   var (heartbeat, limit, waited) = await scenario.WaitForAnnouncement(TimeSpan.FromSeconds(120),
                                                                                                       mark,
                                                                                                       CancellationToken);

                                   step.Observe(limit
                                                    ? $"the energy guard tried again after {waited.TotalSeconds:F0} s"
                                                    : "the energy guard did not try again within 120 s");

                                   step.Require(heartbeat && limit,
                                                "the energy guard did not resend its limit after the controllable system refused it");

                               });

        }

    }

    public sealed class ATC_LPC_COM_PT_EGMessages_002() : EGMessages_002("LPC");
    public sealed class ATC_LPP_COM_PT_EGMessages_002() : EGMessages_002("LPP");

    #endregion

    #region ATC_*_COM_PT_EGMessages_003 and _004

    /// <summary>
    /// The energy guard keeps sending valid messages over an extended period
    /// ([*-TS-001], [*-TS-001/2], [*-TS-002]).
    ///
    /// Twenty-five rounds of activating and deactivating a limit. The length is
    /// the test: an off-by-one in a message counter, an identifier which is
    /// reused once it wraps, a duration which accumulates instead of resetting -
    /// none of them shows up in the first exchange, and all of them show up in the
    /// twenty-fifth.
    /// </summary>
    public abstract class EGMessages_003(String UseCase) : APowerLimitationCase(UseCase, "COM_PT_EGMessages_003")
    {

        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            var scenario = await Reach(Context, "CF_CS_UnlCntrl", CancellationToken);
            var partner  = scenario.SystemAsSeenByGuard
                               ?? throw new ConformanceInconclusive("the energy guard does not know the controllable system");

            await Context.Step("1",
                               "25 iterations of: an activated APCL write command with a duration, then a deactivated one.",
                               "The CS receives and accepts each write command.",
                               async step => {

                                   var refusals = 0;

                                   for (var round = 1; round <= 25; round++)
                                   {

                                       await scenario.Heartbeat(CancellationToken);

                                       var activated = await scenario.Guard.WriteConsumptionLimit(partner,
                                                                                                  scenario.Sheet.Limit(3),
                                                                                                  true,
                                                                                                  scenario.Sheet.LimitDuration(1),
                                                                                                  CancellationToken);

                                       if (activated.Result is not null && activated.Result.ErrorNumber != 0)
                                           refusals++;

                                       var deactivated = await scenario.Guard.WriteConsumptionLimit(partner,
                                                                                                    scenario.Sheet.Limit(3),
                                                                                                    false,
                                                                                                    CancellationToken: CancellationToken);

                                       if (deactivated.Result is not null && deactivated.Result.ErrorNumber != 0)
                                           refusals++;

                                   }

                                   step.Observe($"50 write commands, {refusals} of them refused; the controllable system " +
                                                $"ended in {scenario.State}");

                                   step.Require(refusals == 0,
                                                $"{refusals} of the 50 write commands were refused");

                               });

        }

    }

    public sealed class ATC_LPC_COM_PT_EGMessages_003() : EGMessages_003("LPC");
    public sealed class ATC_LPP_COM_PT_EGMessages_003() : EGMessages_003("LPP");


    /// <summary>
    /// The same for the failsafe values ([*-TS-003], [*-TS-011/1], [*-TS-013/1]).
    /// </summary>
    public abstract class EGMessages_004(String UseCase) : APowerLimitationCase(UseCase, "COM_PT_EGMessages_004")
    {

        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            var scenario = await Reach(Context, "CF_CS_UnlCntrl", CancellationToken);
            var partner  = scenario.SystemAsSeenByGuard
                               ?? throw new ConformanceInconclusive("the energy guard does not know the controllable system");

            await Context.Step("1",
                               "5 iterations of: an FCAPL write command, then a Failsafe Duration Minimum write command.",
                               "The CS receives and accepts each write command.",
                               async step => {

                                   var refusals = 0;

                                   for (var round = 1; round <= 5; round++)
                                   {

                                       var failsafe = await scenario.Guard.WriteFailsafeValues(partner,
                                                                                               Limit:              scenario.Sheet.Failsafe(3),
                                                                                               CancellationToken:  CancellationToken);

                                       if (failsafe.Result is not null && failsafe.Result.ErrorNumber != 0)
                                           refusals++;

                                       var duration = await scenario.Guard.WriteFailsafeValues(partner,
                                                                                               DurationMinimum:    scenario.Sheet.FailsafeDuration(2),
                                                                                               CancellationToken:  CancellationToken);

                                       if (duration.Result is not null && duration.Result.ErrorNumber != 0)
                                           refusals++;

                                   }

                                   step.Observe($"10 write commands, {refusals} of them refused");

                                   step.Require(refusals == 0,
                                                $"{refusals} of the 10 write commands were refused");

                               });

        }

    }

    public sealed class ATC_LPC_COM_PT_EGMessages_004() : EGMessages_004("LPC");
    public sealed class ATC_LPP_COM_PT_EGMessages_004() : EGMessages_004("LPP");

    #endregion

}
