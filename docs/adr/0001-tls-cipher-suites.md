# ADR 0001: TLS cipher suites and certificate keys across platforms

**Status:** accepted (2026-07-26, WP02)

## Context

SHIP TS 1.0.1, chapter 9.1 defines two cipher suites:

| Cipher suite | SHIP |
|---|---|
| `TLS_ECDHE_ECDSA_WITH_AES_128_CBC_SHA256` | required |
| `TLS_ECDHE_ECDSA_WITH_AES_128_GCM_SHA256` | optional |

Both are considered weak today; the CBC variant in particular is disabled by default in
several modern TLS stacks. The reference implementation pins exactly these two suites.

.NET exposes `CipherSuitesPolicy` for this, but **only on Linux and macOS**. On Windows,
TLS goes through SChannel, where the enabled cipher suites are a system-wide setting;
constructing a `CipherSuitesPolicy` throws `PlatformNotSupportedException`.

A second platform difference surfaced during the TLS tests: SChannel refuses to use a
certificate whose private key is *ephemeral* — as produced by
`CertificateRequest.CreateSelfSigned(...)` — and aborts the handshake with
"Received an unexpected EOF or 0 bytes from the transport stream".

A third one only shows up on Linux: TLS 1.3 moved its cipher suites into a namespace of
their own, so a policy holding just the two suites of chapter 9.1 allows *no* TLS 1.3
suite at all. .NET does not react to this by simply not negotiating TLS 1.3 — it refuses
the connection with `The 'RequireEncryption' encryption policy is not supported by this
installation of OpenSSL` as soon as `EnabledSslProtocols` names TLS 1.3 explicitly. On
Windows the policy is `null`, so the combination went unnoticed until the Linux CI job ran.

## Decision

1. `SHIPTLS.CipherSuites` lists both suites of the specification. The policy is built
   lazily and, where the platform refuses it, `SHIPTLS.SHIPCipherSuitesPolicy` is `null`
   and the connection falls back to the system defaults.
   `SHIPTLS.SupportsCipherSuitesPolicy` makes this observable, and the conformance tests
   assert the expected behaviour per platform instead of hiding the difference.
2. TLS 1.2 and 1.3 are both enabled. SHIP 1.0.1 mandates 1.2; allowing 1.3 in addition
   matches the reference implementation and avoids rejecting newer partners.
   The policy therefore has to be the *union* of chapter 9.1 and `SHIPTLS.TLS13CipherSuites`
   — pinning the SHIP suites restricts TLS 1.2, it must not disable TLS 1.3. The Go
   reference implementation arrives at the same result by construction, because Go applies
   `tls.Config.CipherSuites` to TLS 1.2 and below only. `SHIPTLS.CipherSuites` keeps
   quoting the specification alone; the union exists only inside the policy.
3. `SHIPCertificates.GenerateCertificate(...)` round-trips the certificate through
   PKCS#12 on Windows, so that the resulting private key is usable by SChannel.
4. Certificate validation never rejects on PKI grounds (expired, unknown issuer, no
   chain): SHIP TS 1.0.1, chapter 12.1.1 requires that such checks must not prevent
   communication when the public key is authenticated. What is rejected is a certificate
   without a usable SHIP identity — no ECDSA key, no subject key identifier, or a subject
   key identifier that does not match the public key.

## Consequences

* On Windows the GCM suite is normally available, so interoperability with the Go stack
  works. A partner that insists on the CBC suite can still fail if that suite is disabled
  system wide. Should this occur in the field, the fallback is a BouncyCastle-based TLS
  layer; Hermod already carries BouncyCastle.
* Conformance test runs have to record the platform: a cipher suite result from Windows
  says something different than one from Linux. The same asymmetry hides bugs — the CI
  matrix has to keep running both, a green Windows job proves nothing about the policy.
* Every TLS version added to `SHIPTLS.Protocols` has to bring its suites into the policy.
  `CipherSuitesPolicy_CoversEveryEnabledProtocol` checks that pairing.
* The interoperability suite runs on Linux (or WSL), where the cipher suites can be
  pinned exactly as the specification demands.
