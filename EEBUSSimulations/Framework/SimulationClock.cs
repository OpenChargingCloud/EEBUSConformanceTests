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

using Microsoft.Extensions.Time.Testing;

#endregion

namespace cloud.charging.open.protocols.EEBUS.Simulations
{

    /// <summary>
    /// The time axis of a simulation.
    ///
    /// Every simulated device runs on this one clock, which is a
    /// <see cref="FakeTimeProvider"/> rather than the wall clock - so a
    /// two-hour failsafe duration takes as long to simulate as anything else,
    /// and running the same script twice gives the same answer twice. That is
    /// the whole reason the stack was built on a TimeProvider from WP01
    /// onwards (WORKPLAN § 8).
    ///
    /// Time moves in <see cref="Resolution"/> steps rather than in one jump,
    /// because things happen between the steps of a script: heartbeats are sent,
    /// limits expire, an EV notices that nobody has spoken to it for four
    /// seconds. A simulation which jumped straight from minute 5 to minute 35
    /// would simulate none of that.
    ///
    /// <see cref="Speed"/> is the only place the real clock appears at all, and
    /// only to slow things down: a person watching a simulation wants to see it
    /// happen.
    /// </summary>
    public sealed class SimulationClock
    {

        #region Data

        private readonly FakeTimeProvider  time;

        #endregion

        #region Properties

        /// <summary>
        /// The time provider every device of the simulation shares.
        /// </summary>
        public TimeProvider     TimeProvider    => time;

        /// <summary>
        /// When the simulation began.
        /// </summary>
        public DateTimeOffset   Start           { get; }

        /// <summary>
        /// How far the simulation has got.
        /// </summary>
        public TimeSpan         Elapsed         => time.GetUtcNow() - Start;

        /// <summary>
        /// How large a step the clock takes at a time.
        ///
        /// One second by default, which is fine for everything in the
        /// e-mobility family: the shortest reaction the specifications ask for
        /// is the four second heartbeat timeout of the overload protection.
        /// </summary>
        public TimeSpan         Resolution      { get; }

        /// <summary>
        /// How many simulated seconds pass per real second, or zero for "as
        /// fast as the machine can".
        ///
        /// Zero in the tests and in the CI; sixty on somebody's screen, which
        /// turns a two-hour scenario into two minutes of watching.
        /// </summary>
        public Double           Speed           { get; }

        #endregion

        #region Events

        /// <summary>
        /// The clock moved one step. Whoever needs to look at the time
        /// regularly - a state machine which falls back when nobody talks to
        /// it - hangs off this.
        /// </summary>
        public event Func<TimeSpan, CancellationToken, Task>? OnTick;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create the time axis of a simulation.
        /// </summary>
        /// <param name="Start">When the simulation begins. A fixed default, so that two runs of one script produce identical logs.</param>
        /// <param name="Resolution">How large a step the clock takes at a time.</param>
        /// <param name="Speed">How many simulated seconds pass per real second. Zero runs as fast as possible.</param>
        public SimulationClock(DateTimeOffset?  Start        = null,
                               TimeSpan?        Resolution   = null,
                               Double           Speed        = 0)
        {

            this.Start       = Start ?? new DateTimeOffset(2026, 7, 26, 6, 0, 0, TimeSpan.Zero);
            this.Resolution  = Resolution ?? TimeSpan.FromSeconds(1);
            this.Speed       = Speed;
            this.time        = new FakeTimeProvider(this.Start);

        }

        #endregion


        #region AdvanceTo(Offset, CancellationToken = default)

        /// <summary>
        /// Move the clock forward to the given offset from the start, one
        /// resolution step at a time.
        /// </summary>
        /// <param name="Offset">How far into the simulation to go.</param>
        /// <param name="CancellationToken">An optional cancellation token.</param>
        public async Task AdvanceTo(TimeSpan           Offset,
                                    CancellationToken  CancellationToken   = default)
        {

            while (Elapsed < Offset && !CancellationToken.IsCancellationRequested)
            {

                var step = Offset - Elapsed < Resolution
                               ? Offset - Elapsed
                               : Resolution;

                time.Advance(step);

                if (OnTick is not null)
                    foreach (var handler in OnTick.GetInvocationList().Cast<Func<TimeSpan, CancellationToken, Task>>())
                        await handler(Elapsed, CancellationToken);

                if (Speed > 0)
                    await Task.Delay(TimeSpan.FromSeconds(step.TotalSeconds / Speed),
                                     CancellationToken);

            }

        }

        #endregion

        #region Advance(By, CancellationToken = default)

        /// <summary>
        /// Move the clock forward by the given amount.
        /// </summary>
        /// <param name="By">How much further.</param>
        /// <param name="CancellationToken">An optional cancellation token.</param>
        public Task Advance(TimeSpan           By,
                            CancellationToken  CancellationToken   = default)

            => AdvanceTo(Elapsed + By, CancellationToken);

        #endregion


        /// <summary>Return a text representation of this clock.</summary>
        public override String ToString()

            => $"{Elapsed:hh\\:mm\\:ss}" +
               $"{(Speed > 0 ? $" at {Speed}x" : " as fast as possible")}";

    }

}
