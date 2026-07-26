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

using System.Reflection;

#endregion

namespace cloud.charging.open.protocols.EEBUS.Conformance
{

    /// <summary>
    /// Every executable test case, found by its own identifier.
    ///
    /// The list is built by reflection over this assembly rather than written
    /// by hand, so that a case which exists is always registered and a case
    /// which is registered always exists. Forgetting to add a line to a list is
    /// the one way a conformance suite can silently shrink.
    /// </summary>
    public static class ConformanceSuite
    {

        #region Data

        private static readonly Lazy<IReadOnlyDictionary<String, Func<AConformanceTest>>> tests = new (Discover);

        #endregion

        #region Properties

        /// <summary>
        /// The identifiers of every executable test case.
        /// </summary>
        public static IEnumerable<String> Implemented
            => tests.Value.Keys.Order(StringComparer.Ordinal);

        /// <summary>
        /// The identifiers of every catalog entry without an executable test.
        /// </summary>
        public static IEnumerable<String> Missing
            => ConformanceCatalog.TestCases.
                   Select(testCase => testCase.Id).
                   Where (id => !tests.Value.ContainsKey(id)).
                   Order (StringComparer.Ordinal);

        #endregion


        #region TestFor(Id)

        /// <summary>
        /// A fresh instance of the executable test with the given identifier,
        /// or null when there is none.
        /// </summary>
        /// <param name="Id">An official test case identifier.</param>
        public static AConformanceTest? TestFor(String Id)

            => tests.Value.TryGetValue(Id, out var factory)
                   ? factory()
                   : null;

        #endregion

        #region (private) Discover()

        private static IReadOnlyDictionary<String, Func<AConformanceTest>> Discover()
        {

            var found = new Dictionary<String, Func<AConformanceTest>>(StringComparer.Ordinal);

            foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
            {

                if (type.IsAbstract ||
                    !type.IsSubclassOf(typeof(AConformanceTest)) ||
                    type.GetConstructor(Type.EmptyTypes) is null)
                {
                    continue;
                }

                var instance = (AConformanceTest) Activator.CreateInstance(type)!;

                found[instance.Id] = () => (AConformanceTest) Activator.CreateInstance(type)!;

            }

            return found;

        }

        #endregion

    }

}
