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

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

using cloud.charging.open.protocols.EEBUS.SHIP;

#endregion

namespace cloud.charging.open.protocols.EEBUS.Conformance
{

    #region (class) ASHIPSecurityCase

    /// <summary>
    /// What both security cases need: a certificate which lies about itself.
    /// </summary>
    public abstract class ASHIPSecurityCase : AConformanceTest
    {

        /// <summary>
        /// A self-signed certificate whose subject key identifier extension is
        /// not the SHA-1 of its own public key.
        ///
        /// This is the whole attack the two cases are about. The SKI is the
        /// only identity a SHIP node has - it is what is announced over mDNS,
        /// what a user accepts during pairing, and what is stored afterwards.
        /// If a node trusts the SKI *written in* a certificate rather than the
        /// one *computed from* its key, then anybody who ever learns a trusted
        /// SKI can put it into a certificate of their own and be believed.
        /// </summary>
        /// <param name="ClaimedSKI">The SKI the certificate claims, or null for a random one.</param>
        protected static X509Certificate2 SpoofedCertificate(SKI? ClaimedSKI = null)
        {

            var privateKey  = ECDsa.Create(ECCurve.NamedCurves.nistP256);

            var request     = new CertificateRequest(
                                  new X500DistinguishedName("CN=spoofed-test-tool"),
                                  privateKey,
                                  HashAlgorithmName.SHA256
                              );

            var claimed     = ClaimedSKI ?? SKI.Parse("abcdef0123456789abcdef0123456789abcdef01");

            request.CertificateExtensions.Add(
                new X509SubjectKeyIdentifierExtension(claimed.ToByteArray(), false)
            );

            request.CertificateExtensions.Add(
                new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, false)
            );

            var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1),
                                                       DateTimeOffset.UtcNow.AddDays(365));

            return certificate;

        }

    }

    #endregion


    #region TC_SHIP_SEC_001

    /// <summary>
    /// A certificate whose SKI does not belong to its key is refused, even when
    /// the device has been told to trust exactly that SKI.
    /// </summary>
    public sealed class TC_SHIP_SEC_001 : ASHIPSecurityCase
    {

        public override String Id => "TC_SHIP_SEC_001";

        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            await Context.Step(
                      "1",
                      "The test tool is configured as pure client and initiates a SHIP connection; the TLS handshake begins.",
                      "The DUT actively terminates the TLS handshake after it received the client certificate.",
                      step => {

                          using var spoofed = SpoofedCertificate();

                          var accepted = SHIPCertificates.TryGetSKI(spoofed, out var ski, out var errorResponse);

                          step.Require(!accepted,
                                       $"the device accepted a certificate whose subject key identifier is not the " +
                                       $"SHA-1 of its public key, and took it for {ski}");

                          step.Observe(errorResponse ?? "");

                          return Task.CompletedTask;

                      });

            await Context.Step(
                      "2",
                      "The test tool is configured as pure server and waits passively; the DUT initiates a SHIP " +
                      "connection as client.",
                      "The DUT actively terminates the TLS handshake after it received the server certificate.",
                      step => {

                          // The check is the same on both sides of the
                          // handshake, and it has to be: a client which
                          // validates and a server which does not is a device
                          // with a back door.
                          using var spoofed = SpoofedCertificate();

                          step.Require(!SHIPCertificates.TryGetSKI(spoofed, out _, out var errorResponse),
                                       "the device accepted a spoofed server certificate");

                          step.Observe(errorResponse ?? "");

                          return Task.CompletedTask;

                      });

        }

    }

    #endregion

    #region TC_SHIP_SEC_002

    /// <summary>
    /// The same, after a successful pairing: a partner reconnecting with a new
    /// key pair but the old, trusted SKI written into the certificate is still
    /// refused.
    ///
    /// This is the case which separates "checks the certificate" from "checks
    /// the certificate once". A node which remembers a trusted SKI and then
    /// only compares the *claimed* SKI on reconnection has locked its door and
    /// left the key under the mat.
    /// </summary>
    public sealed class TC_SHIP_SEC_002 : ASHIPSecurityCase
    {

        public override String Id => "TC_SHIP_SEC_002";

        public override async Task Run(ConformanceContext  Context,
                                       CancellationToken   CancellationToken   = default)
        {

            var trusted = SKI.Parse("1111111111111111111111111111111111111111");

            await Context.Step(
                      "1",
                      "The test tool closes the active connection and reconfigures itself with a spoofed certificate " +
                      "CERT_T2, built from a different key pair but with the SKI field forged to the trusted SKI_T1.",
                      "The DUT is not reconfigured and still trusts SKI_T1.",
                      step => {

                          using var honest = SHIPCertificates.GenerateCertificate("test-tool");

                          step.Require(SHIPCertificates.TryGetSKI(honest, out var honestSKI, out _),
                                       "the test tool's own honest certificate was refused, so the case cannot start");

                          trusted = honestSKI;

                          step.Observe($"the trusted SKI is {trusted}");

                          return Task.CompletedTask;

                      });

            await Context.Step(
                      "2",
                      "The test tool initiates a SHIP connection as client; the TLS handshake begins.",
                      "The DUT actively terminates the TLS handshake after it received the client certificate.",
                      step => {

                          using var spoofed = SpoofedCertificate(trusted);

                          step.Require(!SHIPCertificates.TryGetSKI(spoofed, out _, out var errorResponse),
                                       $"the device accepted a certificate carrying the trusted SKI {trusted} over a " +
                                       $"key which is not the one that SKI was computed from");

                          step.Observe(errorResponse ?? "");

                          return Task.CompletedTask;

                      });

            await Context.Step(
                      "3",
                      "The test tool is configured as pure server and waits passively; the DUT initiates a SHIP " +
                      "connection as client.",
                      "The DUT actively terminates the TLS handshake after it received the server certificate.",
                      step => {

                          using var spoofed = SpoofedCertificate(trusted);

                          step.Require(!SHIPCertificates.TryGetSKI(spoofed, out _, out var errorResponse),
                                       "the device accepted a spoofed server certificate carrying a trusted SKI");

                          step.Observe(errorResponse ?? "");

                          return Task.CompletedTask;

                      });

        }

    }

    #endregion

}
