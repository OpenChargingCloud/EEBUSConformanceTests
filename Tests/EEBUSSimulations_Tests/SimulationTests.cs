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

namespace cloud.charging.open.protocols.EEBUS.Simulations.tests
{

    /// <summary>
    /// The simulations, run as tests.
    ///
    /// Every simulation is an integration test which happens to print a story:
    /// it drives the real use case implementations over the real SPINE core on
    /// a controlled clock, so asserting on its log asserts on the stack. What
    /// makes that worth doing rather than duplicating the unit tests is the
    /// **shape** of the scenarios - a whole day, three devices, five use cases
    /// on one entity - which is where things go wrong that nothing smaller
    /// catches.
    ///
    /// They run in milliseconds, because nothing here waits for a real second.
    /// </summary>
    [TestFixture]
    public class SimulationTests
    {

        #region EverySimulationRunsAndSaysSomething()

        /// <summary>
        /// The registry is the one place which knows what exists, so a
        /// simulation cannot be added without this test running it.
        /// </summary>
        [Test]
        public async Task EverySimulationRunsAndSaysSomething()
        {

            foreach (var name in SimulationRegistry.Names)
            {

                var simulation = SimulationRegistry.Create(name)!;
                var result     = await simulation.Run();

                Assert.Multiple(() => {

                    Assert.That(result.Name,           Is.EqualTo(name));
                    Assert.That(result.Log.Events,     Is.Not.Empty, $"'{name}' logged nothing at all.");
                    Assert.That(simulation.Description, Is.Not.Empty);

                });

            }

        }

        #endregion

        #region ASimulationRunsTheSameWayTwice()

        /// <summary>
        /// The reason everything is on a TimeProvider: two runs of one script
        /// produce the same story. A simulation which cannot do that is not a
        /// test.
        /// </summary>
        [Test]
        public async Task ASimulationRunsTheSameWayTwice()
        {

            var first  = await SimulationRegistry.Create("lpc-chain", new SimulationOptions(Faults: [ "heartbeat" ]))!.Run();
            var second = await SimulationRegistry.Create("lpc-chain", new SimulationOptions(Faults: [ "heartbeat" ]))!.Run();

            Assert.That(second.Log.ToText(), Is.EqualTo(first.Log.ToText()));

        }

        #endregion


        #region LPCChain_TheWallboxFallsBackWhenTheControlBoxGoesQuiet()

        /// <summary>
        /// The §14a case the whole use case exists for: nobody tells the wallbox
        /// to fall back, it works it out from its own clock.
        /// </summary>
        [Test]
        public async Task LPCChain_TheWallboxFallsBackWhenTheControlBoxGoesQuiet()
        {

            var result = await SimulationRegistry.Create("lpc-chain",
                                                          new SimulationOptions(Faults: [ "heartbeat" ]))!.Run();

            Assert.Multiple(() => {

                Assert.That(result.Log.Happened("Limited"),        Is.True,
                            "the wallbox never accepted the limit");

                Assert.That(result.Log.Happened("FailsafeState"),  Is.True,
                            "the wallbox did not fall back after the control box went quiet");

                Assert.That(result.Log.Happened("LPC-912"),        Is.True,
                            "the fallback did not quote the rule which caused it");

                // 4200 W is the failsafe value, and the wallbox is holding
                // itself to it at the end without anybody having said so.
                Assert.That(result.Log.ValueAt("charging [W]", result.Duration), Is.EqualTo(4200));

            });

        }

        #endregion

        #region LPCChain_WithoutTheFaultTheLimitSimplyExpires()

        /// <summary>
        /// The other half: a limit's end time is relative to when it was
        /// written, so the wallbox has to run its own timer. One which does not
        /// would hold the limit for ever.
        /// </summary>
        [Test]
        public async Task LPCChain_WithoutTheFaultTheLimitSimplyExpires()
        {

            var result = await SimulationRegistry.Create("lpc-chain")!.Run();

            Assert.Multiple(() => {

                Assert.That(result.Log.Happened("LPC-908"),       Is.True,
                            "the limit never expired");

                Assert.That(result.Log.Happened("FailsafeState"), Is.False,
                            "the wallbox fell into the failsafe state although the control box kept beating");

                Assert.That(result.Log.ValueAt("charging [W]", result.Duration), Is.EqualTo(11000),
                            "the wallbox did not go back to its full power after the limit ran out");

            });

        }

        #endregion


        #region MPCMeter_TheApplianceSubscribesOnceAndNeverAsksAgain()

        /// <summary>
        /// The general implementation guideline § 3.2.2 and § 3.2.3: subscribe,
        /// then stop asking. Sixty-odd notifies arrive and the appliance sends
        /// nothing.
        /// </summary>
        [Test]
        public async Task MPCMeter_TheApplianceSubscribesOnceAndNeverAsksAgain()
        {

            var result = await SimulationRegistry.Create("mpc-meter")!.Run();

            Assert.Multiple(() => {

                Assert.That(result.Log.Happened("asked for nothing after subscribing: 0 datagram(s)"),
                            Is.True,
                            "the appliance polled next to a working subscription");

                Assert.That(result.Log.Happened("exporting"), Is.True,
                            "the day never went below zero, so the sign was never exercised");

            });

        }

        #endregion

        #region MPCMeter_WhatIsPublishedIsWhatIsReceived()

        /// <summary>
        /// The two series are the same run seen from the two ends of the wire.
        /// </summary>
        [Test]
        public async Task MPCMeter_WhatIsPublishedIsWhatIsReceived()
        {

            var result = await SimulationRegistry.Create("mpc-meter")!.Run();

            var published = result.Log.Samples.Where(sample => sample.Series == "published [W]").ToList();
            var received  = result.Log.Samples.Where(sample => sample.Series == "received [W]"). ToList();

            Assert.Multiple(() => {

                Assert.That(published, Is.Not.Empty);
                Assert.That(received,  Has.Count.EqualTo(published.Count));

                for (var index = 0; index < published.Count; index++)
                    Assert.That(received[index].Value, Is.EqualTo(published[index].Value),
                                $"the value published at {published[index].At} did not arrive");

            });

        }

        #endregion


        #region OPEVCurtail_TheCarIsCommissionedBeforeItIsCurtailed()

        /// <summary>
        /// Five use cases in the order they happen on a forecourt, with four
        /// server actors on the one EV entity.
        /// </summary>
        [Test]
        public async Task OPEVCurtail_TheCarIsCommissionedBeforeItIsCurtailed()
        {

            var result = await SimulationRegistry.Create("opev-curtail")!.Run();

            Assert.Multiple(() => {

                Assert.That(result.Log.Happened("Wallbox 22"),          Is.True, "EVSECC: the station never named itself");
                Assert.That(result.Log.Happened("an EV entity appeared"), Is.True, "EVCC scenario 1 never happened");
                Assert.That(result.Log.Happened("e-Golf"),              Is.True, "EVCC scenario 5 never arrived");
                Assert.That(result.Log.Happened("iso15118-2ed2"),       Is.True, "EVCC scenario 2 never arrived");
                Assert.That(result.Log.Happened("01-23-45-67-89-AB"),   Is.True, "EVCC scenario 4 never arrived");
                Assert.That(result.Log.Happened("3 phase(s)"),          Is.True, "OPEV never read the phases");

                // The curtailment itself, phase by phase and asymmetrically.
                Assert.That(result.Log.ValueAt("phase A [A]", TimeSpan.FromMinutes(20)), Is.EqualTo(10));
                Assert.That(result.Log.ValueAt("phase B [A]", TimeSpan.FromMinutes(20)), Is.EqualTo(6));

            });

        }

        #endregion

        #region OPEVCurtail_AnAnnouncedFailureMakesTheCarStopTrusting()

        /// <summary>
        /// [OPEV-007]: the guard is still beating, so nothing but the announced
        /// failure tells the car anything is wrong - and it falls back to the
        /// safe current it chose itself.
        /// </summary>
        [Test]
        public async Task OPEVCurtail_AnAnnouncedFailureMakesTheCarStopTrusting()
        {

            var result = await SimulationRegistry.Create("opev-curtail",
                                                          new SimulationOptions(Faults: [ "guard" ]))!.Run();

            Assert.Multiple(() => {

                Assert.That(result.Log.Happened("trust: PartnerFailed"), Is.True,
                            "the car went on trusting an energy guard which said it had failed");

                Assert.That(result.Log.ValueAt("phase A [A]", result.Duration), Is.EqualTo(6),
                            "the car did not fall back to its safe current");

            });

        }

        #endregion


        #region EMobilityDay_TheEnergyManagerPlaysBothRolesAtOnce()

        /// <summary>
        /// The one which is actually hard: a limit in watts arrives from above
        /// and has to leave as a current per phase below, with the rest of the
        /// house taken off first.
        /// </summary>
        [Test]
        public async Task EMobilityDay_TheEnergyManagerPlaysBothRolesAtOnce()
        {

            var result = await SimulationRegistry.Create("emobility-day")!.Run();

            Assert.Multiple(() => {

                Assert.That(result.Log.Happened("commissioned in both directions"), Is.True);

                Assert.That(result.Log.Happened("limited the house to 4200 W"),     Is.True,
                            "the grid operator's limit never arrived");

                Assert.That(result.Log.Happened("leaves 2700 W"),                   Is.True,
                            "the house limit was not broken down for the car");

                // 2700 W over three phases at 230 V is 3.9 A, rounded to 4.
                Assert.That(result.Log.ValueAt("obligation [A]", TimeSpan.FromHours(11.5)), Is.EqualTo(4));

            });

        }

        #endregion

        #region EMobilityDay_TheSunAdvisesWhileTheGridObliges()

        /// <summary>
        /// Two use cases writing to one car's load control feature at once, and
        /// the difference between them: the obligation is always a number, the
        /// recommendation is only a number while there is sun.
        /// </summary>
        [Test]
        public async Task EMobilityDay_TheSunAdvisesWhileTheGridObliges()
        {

            var result = await SimulationRegistry.Create("emobility-day")!.Run();

            var midday = result.Log.ValueAt("recommendation [A]", TimeSpan.FromHours(7));
            var night  = result.Log.ValueAt("recommendation [A]", result.Duration);

            Assert.Multiple(() => {

                Assert.That(midday, Is.GreaterThan(0),
                            "the sun was at its peak and the car was advised nothing");

                Assert.That(night,  Is.EqualTo(0),
                            "the sun was gone and the car was still being advised");

                Assert.That(result.Log.ValueAt("obligation [A]", result.Duration), Is.GreaterThan(0),
                            "the obligation should still be a number when the recommendation is not");

            });

        }

        #endregion

        #region EMobilityDay_LosingTheGridOperatorPutsTheHouseIntoItsFailsafe()

        [Test]
        public async Task EMobilityDay_LosingTheGridOperatorPutsTheHouseIntoItsFailsafe()
        {

            var result = await SimulationRegistry.Create("emobility-day",
                                                          new SimulationOptions(Faults: [ "heartbeat" ]))!.Run();

            Assert.Multiple(() => {

                Assert.That(result.Log.Happened("FailsafeState"), Is.True,
                            "the house did not fall back when the control box went quiet");

                Assert.That(result.Log.ValueAt("house limit [W]", result.Duration), Is.EqualTo(4200),
                            "the house is not holding itself to its failsafe value");

            });

        }

        #endregion


        #region DeviceReplay_ARealDeviceIsReadOrSaidToBeMissing()

        /// <summary>
        /// The replay depends on the `libs/devices` submodule, which cannot be
        /// checked out on Windows in full (one folder name contains a colon).
        /// So this asserts the two honest outcomes and no third: either the
        /// recordings were found and read, or the simulation said they were not
        /// there.
        /// </summary>
        [Test]
        public async Task DeviceReplay_ARealDeviceIsReadOrSaidToBeMissing()
        {

            var result = await SimulationRegistry.Create("device-replay")!.Run();

            if (result.Log.Happened("no recordings"))
                Assert.Inconclusive("libs/devices is not checked out; nothing to replay.");

            Assert.Multiple(() => {

                Assert.That(result.Log.Happened("recording(s) of"), Is.True);

                Assert.That(result.Log.Events.Any(entry => entry.Message.Contains("accepted")),
                            Is.True,
                            "no recorded datagram of a real device was accepted");

                Assert.That(result.Log.Events.Any(entry => entry.Actor == "energy manager"),
                            Is.True,
                            "the energy manager learned nothing from the replay");

            });

        }

        #endregion

        #region DeviceReplay_OurStackCanWorkWithARealWallbox()

        /// <summary>
        /// The answer this repository exists to give, against a recording of a
        /// real Elli wallbox: of the use cases it announces, which can our stack
        /// actually play?
        ///
        /// All of them, as of WP09 - including the EV charging summary, which
        /// has no Go reference implementation and was written from the
        /// specification alone.
        /// </summary>
        [Test]
        public async Task DeviceReplay_OurStackCanWorkWithARealWallbox()
        {

            var result = await SimulationRegistry.Create("device-replay",
                                                          new SimulationOptions(Device: "elli/charger-connect-pro"))!.Run();

            if (result.Log.Happened("no recordings"))
                Assert.Inconclusive("libs/devices is not checked out; nothing to replay.");

            Assert.Multiple(() => {

                foreach (var useCase in new[] {
                             "evseCommissioningAndConfiguration",
                             "evCommissioningAndConfiguration",
                             "measurementOfElectricityDuringEvCharging",
                             "overloadProtectionByEvChargingCurrentCurtailment",
                             "optimizationOfSelfConsumptionDuringEvCharging",
                             "coordinatedEvCharging",
                             "evChargingSummary"
                         })
                    Assert.That(result.Log.Happened($"can play {useCase}"), Is.True,
                                $"the wallbox announces {useCase} and our stack could not match it");

                Assert.That(result.Log.Happened("does not implement"), Is.False,
                            "the wallbox announced a use case whose client side we do not have");

            });

        }

        #endregion

        #region DeviceReplay_TheReportNamesAnActorMismatchAsSuch()

        /// <summary>
        /// The Porsche PMCC announces **every** use case under the actor "EV",
        /// including the EV charging summary, which the specification puts at
        /// the EVSE. Our energy manager tolerates that for the EVSE
        /// commissioning - eebus-go documents the quirk and we followed it - and
        /// does not for the charging summary, where there is no precedent to
        /// follow.
        ///
        /// The report has to say which of the two it is: "we do not implement
        /// this" and "we implement it and the actor did not match" are different
        /// problems with different owners, and only the second is a finding
        /// about the device.
        /// </summary>
        [Test]
        public async Task DeviceReplay_TheReportNamesAnActorMismatchAsSuch()
        {

            var result = await SimulationRegistry.Create("device-replay",
                                                          new SimulationOptions(Device: "porsche/mobile-charger-connect"))!.Run();

            if (result.Log.Happened("no recordings"))
                Assert.Inconclusive("libs/devices is not checked out; nothing to replay.");

            Assert.Multiple(() => {

                // The documented quirk, tolerated: the PMCC says "EV" where the
                // specification says "EVSE", and we name it anyway.
                Assert.That(result.Log.Happened("can play evseCommissioningAndConfiguration"), Is.True,
                            "the PMCC actor quirk of finding U3 was not tolerated");

                // The same quirk one use case further, not tolerated - and
                // reported as an actor mismatch rather than as a gap in ours.
                Assert.That(result.Log.Happened("implements evChargingSummary as EnergyBroker but found no partner"),
                            Is.True,
                            "an actor mismatch was not reported as one");

                Assert.That(result.Log.Happened("does not implement the client side of evChargingSummary"),
                            Is.False,
                            "an actor mismatch was reported as a missing implementation");

            });

        }

        #endregion

        #region TheLogTurnsIntoCSVAndMarkdown()

        /// <summary>
        /// What the `--out` option writes.
        /// </summary>
        [Test]
        public async Task TheLogTurnsIntoCSVAndMarkdown()
        {

            var result = await SimulationRegistry.Create("lpc-chain")!.Run();

            var csv      = result.Log.ToCSV();
            var markdown = result.Log.ToMarkdown("the chain");

            Assert.Multiple(() => {

                Assert.That(csv,      Does.StartWith("seconds;"));
                Assert.That(csv,      Does.Contain("charging [W]"));
                Assert.That(csv.Split('\n'), Has.Length.GreaterThan(10));

                Assert.That(markdown, Does.StartWith("# the chain"));
                Assert.That(markdown, Does.Contain("LPC-901"));

            });

        }

        #endregion

    }

}
