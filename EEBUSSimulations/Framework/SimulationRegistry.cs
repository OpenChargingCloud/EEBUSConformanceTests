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

namespace cloud.charging.open.protocols.EEBUS.Simulations
{

    /// <summary>
    /// Which simulations there are.
    ///
    /// One place which knows the names, so that the command line, the help text
    /// and the smoke tests cannot disagree about what exists - a test which
    /// enumerates this and runs everything in it cannot be forgotten when a
    /// simulation is added.
    /// </summary>
    public static class SimulationRegistry
    {

        #region Data

        private static readonly Dictionary<String, Func<SimulationOptions, ASimulation>> factories

            = new (StringComparer.OrdinalIgnoreCase) {

                  ["lpc-chain"]      = options => new LPCChainSimulation    (options),
                  ["mpc-meter"]      = options => new MPCMeterSimulation    (options),
                  ["opev-curtail"]   = options => new OPEVCurtailSimulation (options),
                  ["emobility-day"]  = options => new EMobilityDaySimulation(options),
                  ["device-replay"]  = options => new DeviceReplaySimulation(options)

              };

        #endregion

        #region Names / Create(Name, Options) / Describe()

        /// <summary>
        /// The names of every simulation there is.
        /// </summary>
        public static IEnumerable<String> Names

            => factories.Keys.Order(StringComparer.Ordinal);


        /// <summary>
        /// Create a simulation by name, or null when there is no such thing.
        /// </summary>
        /// <param name="Name">The name of a simulation.</param>
        /// <param name="Options">How it is to be run.</param>
        public static ASimulation? Create(String              Name,
                                          SimulationOptions?  Options   = null)

            => factories.TryGetValue(Name, out var factory)
                   ? factory(Options ?? new SimulationOptions())
                   : null;


        /// <summary>
        /// Every simulation, built with default options, so that a caller can
        /// ask each of them what it is.
        /// </summary>
        public static IEnumerable<ASimulation> Describe()

            => Names.Select(name => factories[name](new SimulationOptions()));

        #endregion

    }

}
