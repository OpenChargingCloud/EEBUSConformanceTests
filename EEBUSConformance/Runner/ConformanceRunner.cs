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

using System.Diagnostics;

#endregion

namespace cloud.charging.open.protocols.EEBUS.Conformance
{

    /// <summary>
    /// Runs the catalog against a device.
    ///
    /// The runner does three things, and the third is the one which matters:
    /// it asks the parameter sheet whether a case applies at all, it runs the
    /// ones which do, and it reports the ones which have no executable test as
    /// <see cref="ConformanceVerdicts.NotImplemented"/> rather than quietly
    /// leaving them out. A conformance report whose denominator is "the cases
    /// we wrote" says nothing; one whose denominator is the official catalog
    /// says everything.
    /// </summary>
    public static class ConformanceRunner
    {

        #region Run(Parameters, Filter = null, CancellationToken = default)

        /// <summary>
        /// Run every applicable test case of the catalog.
        /// </summary>
        /// <param name="Parameters">What the device declared about itself.</param>
        /// <param name="Filter">An optional filter over the catalog identifiers, e.g. "TC_SHIP_".</param>
        /// <param name="CancellationToken">An optional cancellation token.</param>
        public static async Task<ConformanceRun> Run(ParameterSheet     Parameters,
                                                     String?            Filter              = null,
                                                     CancellationToken  CancellationToken   = default)
        {

            var startedAt  = DateTimeOffset.UtcNow;
            var outcomes   = new List<ConformanceOutcome>();

            foreach (var testCase in ConformanceCatalog.TestCases)
            {

                if (Filter is not null && !testCase.Id.Contains(Filter, StringComparison.OrdinalIgnoreCase))
                    continue;

                outcomes.Add(await RunOne(testCase, Parameters, CancellationToken));

            }

            return new ConformanceRun(Parameters, outcomes, startedAt);

        }

        #endregion

        #region RunOne(TestCase, Parameters, CancellationToken = default)

        /// <summary>
        /// Run one test case of the catalog.
        /// </summary>
        /// <param name="TestCase">A catalog entry.</param>
        /// <param name="Parameters">What the device declared about itself.</param>
        /// <param name="CancellationToken">An optional cancellation token.</param>
        public static async Task<ConformanceOutcome> RunOne(ConformanceTestCase  TestCase,
                                                            ParameterSheet       Parameters,
                                                            CancellationToken    CancellationToken   = default)
        {

            if (!TestCase.Applies(Parameters, out var reason))
                return new ConformanceOutcome(TestCase,
                                              ConformanceVerdicts.NotApplicable,
                                              [],
                                              reason);

            var test = ConformanceSuite.TestFor(TestCase.Id);

            if (test is null)
                return new ConformanceOutcome(TestCase,
                                              ConformanceVerdicts.NotImplemented,
                                              [],
                                              "no executable test carries this identifier yet");

            var context   = new ConformanceContext(Parameters);
            var stopwatch = Stopwatch.StartNew();

            try
            {

                await test.Run(context, CancellationToken);

                stopwatch.Stop();

                return new ConformanceOutcome(TestCase,
                                              context.Warned
                                                  ? ConformanceVerdicts.Warning
                                                  : ConformanceVerdicts.Passed,
                                              context.Steps,
                                              context.Warned
                                                  ? "passed, with a behaviour the specification tolerates only for now"
                                                  : null,
                                              stopwatch.Elapsed);

            }
            catch (ConformanceStepFailed failed)
            {

                stopwatch.Stop();

                return new ConformanceOutcome(TestCase,
                                              ConformanceVerdicts.Failed,
                                              context.Steps,
                                              failed.Message,
                                              stopwatch.Elapsed);

            }
            catch (ConformanceInconclusive inconclusive)
            {

                stopwatch.Stop();

                return new ConformanceOutcome(TestCase,
                                              ConformanceVerdicts.Inconclusive,
                                              context.Steps,
                                              inconclusive.Message,
                                              stopwatch.Elapsed);

            }
            catch (Exception e)
            {

                stopwatch.Stop();

                // An exception out of a test case is a fault of the test tool
                // until proven otherwise, so it is reported as inconclusive
                // rather than as a failure of the device.
                return new ConformanceOutcome(TestCase,
                                              ConformanceVerdicts.Inconclusive,
                                              context.Steps,
                                              $"the test tool broke: {e.GetType().Name}: {e.Message}",
                                              stopwatch.Elapsed);

            }

        }

        #endregion

    }

}
