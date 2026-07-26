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

namespace cloud.charging.open.protocols.EEBUS.Conformance
{

    #region (class) ConformanceStepFailed / ConformanceInconclusive

    /// <summary>
    /// A step did not meet its expected result. Thrown rather than returned,
    /// because a test case which has already gone wrong must not carry on
    /// poking at the device.
    /// </summary>
    /// <param name="Message">What was expected and what happened instead.</param>
    public sealed class ConformanceStepFailed(String Message) : Exception(Message);

    /// <summary>
    /// The case could not be brought to a verdict - usually a precondition
    /// which could not be established.
    /// </summary>
    /// <param name="Message">What was missing.</param>
    public sealed class ConformanceInconclusive(String Message) : Exception(Message);

    #endregion


    #region (class) ConformanceStep

    /// <summary>
    /// What a running step can say about what it saw.
    /// </summary>
    public sealed class ConformanceStep
    {

        private readonly List<String> notes = [];

        /// <summary>Whether something tolerated but not clean happened.</summary>
        public Boolean  Warned    { get; private set; }

        /// <summary>What was observed.</summary>
        public String?  Note
            => notes.Count > 0 ? String.Join("; ", notes) : null;


        /// <summary>
        /// Demand something, and end the test case when it is not so.
        /// </summary>
        /// <param name="Condition">What has to hold.</param>
        /// <param name="Otherwise">What to say when it does not.</param>
        public void Require(Boolean Condition, String Otherwise)
        {

            if (!Condition)
                throw new ConformanceStepFailed(Otherwise);

        }

        /// <summary>
        /// Record what was seen, without judging it.
        /// </summary>
        /// <param name="Text">An observation.</param>
        public void Observe(String Text)
        {
            notes.Add(Text);
        }

        /// <summary>
        /// Accept something the specification tolerates for now, and say so.
        /// The step still passes; the case ends with a warning.
        /// </summary>
        /// <param name="Text">What was tolerated.</param>
        public void Tolerate(String Text)
        {

            Warned = true;

            notes.Add(Text);

        }

    }

    #endregion


    #region (class) ConformanceContext

    /// <summary>
    /// What a running test case is given: the declarations of the device, and
    /// somewhere to write down what each step did.
    ///
    /// The step shape is not decoration either. A conformance result which says
    /// "failed" is worth very little; one which says "step 2 expected an SME
    /// protocol handshake and the connection was closed instead" is a bug
    /// report. Writing the cases step by step, in the specification's own
    /// words, is what makes the second kind fall out for free.
    /// </summary>
    /// <param name="Parameters">What the device declared about itself.</param>
    public sealed class ConformanceContext(ParameterSheet Parameters)
    {

        #region Data

        private readonly List<ConformanceStepResult> steps = [];

        #endregion

        #region Properties

        /// <summary>
        /// What the device declared about itself.
        /// </summary>
        public ParameterSheet                         Parameters    { get; } = Parameters;

        /// <summary>
        /// What each step did, in order.
        /// </summary>
        public IReadOnlyList<ConformanceStepResult>   Steps
            => steps;

        /// <summary>
        /// Whether any step tolerated something.
        /// </summary>
        public Boolean                                Warned
            => steps.Any(step => step.Verdict == ConformanceVerdicts.Warning);

        #endregion


        #region Step(Number, Action, Expected, Body)

        /// <summary>
        /// Run one step of the test case and record what it did.
        /// </summary>
        /// <param name="Number">The step number of the specification's table.</param>
        /// <param name="Action">What the test tool does.</param>
        /// <param name="Expected">What the device is supposed to do.</param>
        /// <param name="Body">Doing it.</param>
        public async Task Step(String                          Number,
                               String                          Action,
                               String                          Expected,
                               Func<ConformanceStep, Task>     Body)
        {

            var step = new ConformanceStep();

            try
            {

                await Body(step);

                steps.Add(new ConformanceStepResult(
                              Number,
                              Action,
                              Expected,
                              step.Warned ? ConformanceVerdicts.Warning : ConformanceVerdicts.Passed,
                              step.Note
                          ));

            }
            catch (ConformanceStepFailed failed)
            {

                steps.Add(new ConformanceStepResult(
                              Number,
                              Action,
                              Expected,
                              ConformanceVerdicts.Failed,
                              step.Note is not null
                                  ? $"{step.Note}; {failed.Message}"
                                  : failed.Message
                          ));

                throw;

            }
            catch (ConformanceInconclusive)
            {

                steps.Add(new ConformanceStepResult(
                              Number,
                              Action,
                              Expected,
                              ConformanceVerdicts.Inconclusive,
                              step.Note
                          ));

                throw;

            }

        }

        #endregion

        #region Precondition(Name, Body)

        /// <summary>
        /// Establish a precondition. Failing to reach it is never a failure of
        /// the device under test within this case - it means the case never
        /// started, which is a different thing and gets a different verdict.
        /// </summary>
        /// <param name="Name">The PRE_ identifier.</param>
        /// <param name="Body">Reaching it.</param>
        public async Task Precondition(String  Name,
                                       Func<Task>  Body)
        {

            try
            {
                await Body();
            }
            catch (Exception e) when (e is not ConformanceInconclusive)
            {
                throw new ConformanceInconclusive($"{Name} could not be established: {e.Message}");
            }

        }

        #endregion

    }

    #endregion


    #region (class) AConformanceTest

    /// <summary>
    /// An executable test case, carrying the official catalog identifier.
    /// </summary>
    public abstract class AConformanceTest
    {

        /// <summary>
        /// The official test case identifier, e.g. "TC_SHIP_CMI_003".
        /// </summary>
        public abstract String Id { get; }

        /// <summary>
        /// The catalog entry this test executes.
        /// </summary>
        public ConformanceTestCase TestCase

            => ConformanceCatalog.TestCase(Id)
                   ?? throw new InvalidOperationException($"'{Id}' is not a test case of the catalog!");


        /// <summary>
        /// Run the case against the device under test.
        /// </summary>
        /// <param name="Context">Where the steps are written down.</param>
        /// <param name="CancellationToken">An optional cancellation token.</param>
        public abstract Task Run(ConformanceContext  Context,
                                 CancellationToken   CancellationToken   = default);

    }

    #endregion

}
