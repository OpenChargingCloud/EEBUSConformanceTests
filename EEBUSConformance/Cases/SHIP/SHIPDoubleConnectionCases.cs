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

using cloud.charging.open.protocols.EEBUS.SHIP;

#endregion

namespace cloud.charging.open.protocols.EEBUS.Conformance
{

    #region TC_SHIP_CONN_001

    /// <summary>
    /// Two nodes which discover each other at the same time open two
    /// connections, and one of them has to go.
    ///
    /// The specification resolves it by SKI: the node with the larger value
    /// decides, and its decision is to keep the *most recent* connection. That
    /// is what this case checks, and it is worth checking, because the Go
    /// reference implementation - and therefore most of the installed base -
    /// resolves it differently on purpose: it keeps the connection *initiated
    /// by* the larger SKI, with the comment that the specification's rule "is
    /// hard to implement without any flaws". The two rules disagree in exactly
    /// this scenario.
    /// </summary>
    public sealed class TC_SHIP_CONN_001 : AConformanceTest
    {

        #region (class) RecordingTransport

        /// <summary>
        /// A transport which only remembers whether it was closed - enough to
        /// see which of two connections survived.
        /// </summary>
        private sealed class RecordingTransport : ISHIPTransport
        {

            public Boolean  IsClosed     { get; private set; }
            public String?  CloseReason  { get; private set; }

            public Task SendAsync(Byte[] Frame, CancellationToken CancellationToken = default)
                => Task.CompletedTask;

            public Task CloseAsync(String? Reason = null, CancellationToken CancellationToken = default)
            {

                if (!IsClosed)
                {
                    IsClosed     = true;
                    CloseReason  = Reason;
                }

                return Task.CompletedTask;

            }

        }

        #endregion


        public override String Id => "TC_SHIP_CONN_001";

        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            // PRE_SHIP_TestTool_Smaller_SKI: the device has the larger value,
            // so the device is the one which has to decide.
            var toolSKI      = SKI.Parse("1111111111111111111111111111111111111111");
            var dutSKI       = SKI.Parse("2222222222222222222222222222222222222222");

            var time         = new FakeTimeProvider(new DateTimeOffset(2026, 7, 26, 12, 0, 0, TimeSpan.Zero));

            var dut          = new SHIPNode(dutSKI,
                                            SHIP_Id.Parse(Context.Parameters.ShipId ?? "dut-0001"),
                                            TimeProvider: time) {
                                   AutoAccept = true
                               };

            var connectionA  = new RecordingTransport();
            var connectionB  = new RecordingTransport();

            await Context.Step(
                      "1",
                      "The test tool waits passively for the DUT to actively initiate a SHIP connection (Connection A) " +
                      "to the test tool.",
                      "The DUT initiates a TCP connection (Connection A) to the test tool.",
                      async step => {

                          var opened = await dut.ConnectAsync(toolSKI, connectionA, CancellationToken);

                          step.Require(opened is not null,
                                       "the device did not open a connection of its own");

                          step.Require(!connectionA.IsClosed,
                                       "the device closed its own connection right away");

                      });

            await Context.Step(
                      "2",
                      "As soon as the test tool receives the incoming TCP stream for connection A, it immediately " +
                      "establishes a second, simultaneous SHIP connection (Connection B) to the DUT using the exact " +
                      "same certificate.",
                      "The DUT keeps the most recent connection (Connection B) open, continues with the SME phases on " +
                      "it, and actively closes the older connection (Connection A) within 30 s.",
                      async step => {

                          await dut.AcceptAsync(toolSKI, connectionB, CancellationToken);

                          await Task.Yield();

                          step.Require(connectionA.IsClosed,
                                       "the device kept the older connection A instead of the most recent one - " +
                                       "it resolved the double connection by who initiated rather than by which is newer " +
                                       "(the rule of ship-go, which the specification does not share)");

                          step.Require(!connectionB.IsClosed,
                                       $"the device closed the most recent connection B ({connectionB.CloseReason})");

                      });

        }

    }

    #endregion

}
