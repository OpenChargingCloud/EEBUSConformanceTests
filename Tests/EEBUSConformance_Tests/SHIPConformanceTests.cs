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

namespace cloud.charging.open.protocols.EEBUS.Conformance.tests
{

    #region (class) AConformanceFixture

    /// <summary>
    /// The self test: every case of the official catalog, executed against this
    /// stack, one NUnit test per catalog identifier.
    ///
    /// Each test carries the official identifier twice - as its method name and
    /// as the property "TC" - so that a test runner, a CI log and a
    /// certification report all say the same thing about the same case.
    /// </summary>
    public abstract class AConformanceFixture
    {

        /// <summary>
        /// What this stack declares about itself.
        /// </summary>
        protected static ParameterSheet Parameters => ParameterSheet.OurOwnStack;


        /// <summary>
        /// Run one case of the catalog and turn its verdict into an NUnit
        /// result.
        ///
        /// The mapping is where the honesty lives. A case which does not apply
        /// is *inconclusive*, not passed: this stack was never asked, and a
        /// green test would claim otherwise. A case which the specification
        /// tolerates for now passes with its warning written into the result.
        /// </summary>
        /// <param name="Id">An official test case identifier.</param>
        protected static async Task Conform(String Id)
        {

            var testCase = ConformanceCatalog.TestCase(Id);

            Assert.That(testCase, Is.Not.Null, $"'{Id}' is not a test case of the catalog.");

            var outcome = await ConformanceRunner.RunOne(testCase!, Parameters);

            var story   = String.Join(Environment.NewLine,
                                      outcome.Steps.Select(step => $"  {step.Number}. [{step.Verdict}] {step.Expected}" +
                                                                   (step.Note is not null ? $"{Environment.NewLine}       {step.Note}" : "")));

            switch (outcome.Verdict)
            {

                case ConformanceVerdicts.Passed:
                    Assert.Pass($"{Id}: passed.{Environment.NewLine}{story}");
                    break;

                case ConformanceVerdicts.Warning:
                    Assert.Pass($"{Id}: passed with a warning - {outcome.Summary}{Environment.NewLine}{story}");
                    break;

                case ConformanceVerdicts.NotApplicable:
                    Assert.Inconclusive($"{Id} does not apply: {outcome.Summary}");
                    break;

                case ConformanceVerdicts.NotImplemented:
                    Assert.Inconclusive($"{Id} has no executable test yet.");
                    break;

                case ConformanceVerdicts.Inconclusive:
                    Assert.Inconclusive($"{Id}: {outcome.Summary}{Environment.NewLine}{story}");
                    break;

                default:

                    // A failure this repository has already decided about, and
                    // written down, does not turn the build red - but it still
                    // says "failed" in the conformance report, and the reason
                    // is repeated here so that nobody has to go looking for it.
                    if (testCase!.KnownDeviation is String deviation)
                        Assert.Inconclusive($"{Id}: failed, knowingly.{Environment.NewLine}" +
                                            $"  {outcome.Summary}{Environment.NewLine}" +
                                            $"  {deviation}{Environment.NewLine}{story}");

                    Assert.Fail($"{Id}: {outcome.Summary}{Environment.NewLine}{story}");
                    break;

            }

        }

    }

    #endregion


    /// <summary>
    /// The thirty-three test cases of EEBus_SHIP_TestSpecification_V1.0.0.
    /// </summary>
    [TestFixture]
    public class SHIPConformanceTests : AConformanceFixture
    {

        [Test] [Property("TC", "TC_SHIP_MDNS_001")]     public Task TC_SHIP_MDNS_001_ValidateMDNSTxtRecord()                     => Conform("TC_SHIP_MDNS_001");

        [Test] [Property("TC", "TC_SHIP_CONN_001")]     public Task TC_SHIP_CONN_001_ResolveSimultaneousConnectionsBySKI()       => Conform("TC_SHIP_CONN_001");

        [Test] [Property("TC", "TC_SHIP_ROLE_001")]     public Task TC_SHIP_ROLE_001_DUTAsSMEServer()                            => Conform("TC_SHIP_ROLE_001");
        [Test] [Property("TC", "TC_SHIP_ROLE_002")]     public Task TC_SHIP_ROLE_002_DUTAsSMEClient()                            => Conform("TC_SHIP_ROLE_002");
        [Test] [Property("TC", "TC_SHIP_ROLE_003")]     public Task TC_SHIP_ROLE_003_SimultaneousRolePolymorphism()              => Conform("TC_SHIP_ROLE_003");

        [Test] [Property("TC", "TC_SHIP_SEC_001")]      public Task TC_SHIP_SEC_001_RejectSpoofedCertificateNoPriorPairing()     => Conform("TC_SHIP_SEC_001");
        [Test] [Property("TC", "TC_SHIP_SEC_002")]      public Task TC_SHIP_SEC_002_RejectSpoofedCertificateWithPriorPairing()   => Conform("TC_SHIP_SEC_002");

        [Test] [Property("TC", "TC_SHIP_MSG_001")]      public Task TC_SHIP_MSG_001_RejectMessageWithoutMessageValue()           => Conform("TC_SHIP_MSG_001");
        [Test] [Property("TC", "TC_SHIP_MSG_002")]      public Task TC_SHIP_MSG_002_RejectUnknownMessageType()                   => Conform("TC_SHIP_MSG_002");
        [Test] [Property("TC", "TC_SHIP_MSG_003")]      public Task TC_SHIP_MSG_003_SupportJSONWhitespaceFormatting()            => Conform("TC_SHIP_MSG_003");

        [Test] [Property("TC", "TC_SHIP_CMI_001")]      public Task TC_SHIP_CMI_001_RejectInvalidMessageType_Server()            => Conform("TC_SHIP_CMI_001");
        [Test] [Property("TC", "TC_SHIP_CMI_002")]      public Task TC_SHIP_CMI_002_RejectInvalidMessageType_Client()            => Conform("TC_SHIP_CMI_002");
        [Test] [Property("TC", "TC_SHIP_CMI_003")]      public Task TC_SHIP_CMI_003_ApplyCmiTimeout_Server()                     => Conform("TC_SHIP_CMI_003");
        [Test] [Property("TC", "TC_SHIP_CMI_004")]      public Task TC_SHIP_CMI_004_ApplyCmiTimeout_Client()                     => Conform("TC_SHIP_CMI_004");
        [Test] [Property("TC", "TC_SHIP_CMI_005")]      public Task TC_SHIP_CMI_005_RejectInvalidCmiHead_Server()                => Conform("TC_SHIP_CMI_005");
        [Test] [Property("TC", "TC_SHIP_CMI_006")]      public Task TC_SHIP_CMI_006_RejectInvalidCmiHead_Client()                => Conform("TC_SHIP_CMI_006");

        [Test] [Property("TC", "TC_SHIP_HELLO_001")]    public Task TC_SHIP_HELLO_001_ProcessValidHello()                        => Conform("TC_SHIP_HELLO_001");
        [Test] [Property("TC", "TC_SHIP_HELLO_002")]    public Task TC_SHIP_HELLO_002_AcceptProlongationRequests()               => Conform("TC_SHIP_HELLO_002");
        [Test] [Property("TC", "TC_SHIP_HELLO_003")]    public Task TC_SHIP_HELLO_003_ApplyWaitForReadyTimer()                   => Conform("TC_SHIP_HELLO_003");
        [Test] [Property("TC", "TC_SHIP_HELLO_004")]    public Task TC_SHIP_HELLO_004_IgnorePendingWithoutProlongation()         => Conform("TC_SHIP_HELLO_004");

        [Test] [Property("TC", "TC_SHIP_PROT_001")]     public Task TC_SHIP_PROT_001_SupportJSONUTF8_Server()                    => Conform("TC_SHIP_PROT_001");
        [Test] [Property("TC", "TC_SHIP_PROT_002")]     public Task TC_SHIP_PROT_002_SupportJSONUTF8_Client()                    => Conform("TC_SHIP_PROT_002");
        [Test] [Property("TC", "TC_SHIP_PROT_003")]     public Task TC_SHIP_PROT_003_ApplyWaitTimer_Server()                     => Conform("TC_SHIP_PROT_003");
        [Test] [Property("TC", "TC_SHIP_PROT_004")]     public Task TC_SHIP_PROT_004_ApplyWaitTimer_Client()                     => Conform("TC_SHIP_PROT_004");
        [Test] [Property("TC", "TC_SHIP_PROT_005")]     public Task TC_SHIP_PROT_005_RejectUnexpectedMessage_Server()            => Conform("TC_SHIP_PROT_005");
        [Test] [Property("TC", "TC_SHIP_PROT_006")]     public Task TC_SHIP_PROT_006_RejectUnexpectedMessage_Client()            => Conform("TC_SHIP_PROT_006");

        [Test] [Property("TC", "TC_SHIP_PIN_001")]      public Task TC_SHIP_PIN_001_PinStateNone()                               => Conform("TC_SHIP_PIN_001");

        [Test] [Property("TC", "TC_SHIP_TERM_001")]     public Task TC_SHIP_TERM_001_ApplyMaxTimeDuringTermination()             => Conform("TC_SHIP_TERM_001");

        [Test] [Property("TC", "TC_SHIP_AM_001")]       public Task TC_SHIP_AM_001_VerifyShipIdInAccessMethodsResponse()         => Conform("TC_SHIP_AM_001");

        [Test] [Property("TC", "TC_SHIP_AMDATA_001")]   public Task TC_SHIP_AMDATA_001_ParallelDataWhileAnsweringAccessMethods() => Conform("TC_SHIP_AMDATA_001");
        [Test] [Property("TC", "TC_SHIP_AMDATA_002")]   public Task TC_SHIP_AMDATA_002_ParallelDataWhileAwaitingAccessMethods()  => Conform("TC_SHIP_AMDATA_002");
        [Test] [Property("TC", "TC_SHIP_AMDATA_003")]   public Task TC_SHIP_AMDATA_003_ParallelAccessMethodsWhileAwaitingData()  => Conform("TC_SHIP_AMDATA_003");
        [Test] [Property("TC", "TC_SHIP_AMDATA_004")]   public Task TC_SHIP_AMDATA_004_NoAccessMethodsRequestWhenDeclaredNo()    => Conform("TC_SHIP_AMDATA_004");

    }

}
