# EEBUS conformance report — WWCP_EEBUS

*2026-07-26 17:45:02 UTC · GraphDefined GmbH · SHIP 1.0 · SPINE 1.3.0 · actors CS, EG*

Catalog: `EEBus_SHIP_TestSpecification_V1.0.0`, `EEBus_SPINE_TestSpecification_V1.0.0`. Identifiers are the official ones.

| Verdict | Cases |
|---|---|
| ✅ passed | 60 |
| ⚠️ warning | 1 |
| ❌ failed | 1 |
| ➖ not applicable | 2 |
| 🚧 not implemented | 0 |
| ❔ inconclusive | 0 |

**1 mandatory test case(s) which applied to this device did not pass.**

## Test cases

### SHIP — MDNS

| Test case | Title | DUT | M/O | Verdict | Note |
|---|---|---|---|---|---|
| `TC_SHIP_MDNS_001` | Validate mDNS TXT record | Server | M | ✅ passed |  |

### SHIP — CONN

| Test case | Title | DUT | M/O | Verdict | Note |
|---|---|---|---|---|---|
| `TC_SHIP_CONN_001` | Resolve simultaneous connections by SKI (DUT has larger SKI) | ServerAndClient | M | ❌ failed | the device kept the older connection A instead of the most recent one - it resolved the double connection by who initiated rather than by which is newer (the rule of ship-go, which the specification does not share) |

### SHIP — ROLE

| Test case | Title | DUT | M/O | Verdict | Note |
|---|---|---|---|---|---|
| `TC_SHIP_ROLE_001` | DUT as SME server | Server | M | ✅ passed |  |
| `TC_SHIP_ROLE_002` | DUT as SME client | Client | M | ✅ passed |  |
| `TC_SHIP_ROLE_003` | Simultaneous role polymorphism | ServerAndClient | M | ✅ passed |  |

### SHIP — SEC

| Test case | Title | DUT | M/O | Verdict | Note |
|---|---|---|---|---|---|
| `TC_SHIP_SEC_001` | Reject spoofed certificate (no prior pairing) | ServerOrClient | M | ✅ passed |  |
| `TC_SHIP_SEC_002` | Reject spoofed certificate (with prior pairing) | ServerOrClient | M | ✅ passed |  |

### SHIP — MSG

| Test case | Title | DUT | M/O | Verdict | Note |
|---|---|---|---|---|---|
| `TC_SHIP_MSG_001` | Reject message without MessageValue | ServerOrClient | M | ✅ passed |  |
| `TC_SHIP_MSG_002` | Reject unknown MessageType | ServerOrClient | M | ✅ passed |  |
| `TC_SHIP_MSG_003` | Support JSON whitespace formatting | ServerOrClient | M | ✅ passed |  |

### SHIP — CMI

| Test case | Title | DUT | M/O | Verdict | Note |
|---|---|---|---|---|---|
| `TC_SHIP_CMI_001` | Reject invalid MessageType (DUT as server) | Server | M | ✅ passed |  |
| `TC_SHIP_CMI_002` | Reject invalid MessageType (DUT as client) | Client | M | ✅ passed |  |
| `TC_SHIP_CMI_003` | Apply CmiTimeout (DUT as server) | Server | M | ✅ passed |  |
| `TC_SHIP_CMI_004` | Apply CmiTimeout (DUT as client) | Client | M | ✅ passed |  |
| `TC_SHIP_CMI_005` | Reject invalid CmiHead (DUT as server) | Server | M | ✅ passed |  |
| `TC_SHIP_CMI_006` | Reject invalid CmiHead (DUT as client) | Client | M | ✅ passed |  |

### SHIP — HELLO

| Test case | Title | DUT | M/O | Verdict | Note |
|---|---|---|---|---|---|
| `TC_SHIP_HELLO_001` | Process valid Hello & enter protocol handshake | Server | M | ✅ passed |  |
| `TC_SHIP_HELLO_002` | Accept prolongation requests | Server | M | ✅ passed |  |
| `TC_SHIP_HELLO_003` | Apply Wait-For-Ready-Timer (timeout) | Server | M | ✅ passed |  |
| `TC_SHIP_HELLO_004` | Ignore "pending" updates without prolongationRequest in "ready" state | Server | M | ✅ passed |  |

### SHIP — PROT

| Test case | Title | DUT | M/O | Verdict | Note |
|---|---|---|---|---|---|
| `TC_SHIP_PROT_001` | Support JSON-UTF8 format (DUT as server) | Server | M | ✅ passed |  |
| `TC_SHIP_PROT_002` | Support JSON-UTF8 format (DUT as client) | Client | M | ✅ passed |  |
| `TC_SHIP_PROT_003` | Apply Wait-Timer (DUT as server) | Server | M | ✅ passed |  |
| `TC_SHIP_PROT_004` | Apply Wait-Timer (DUT as client) | Client | M | ✅ passed |  |
| `TC_SHIP_PROT_005` | Reject unexpected message (DUT as server) | Server | M | ✅ passed |  |
| `TC_SHIP_PROT_006` | Reject unexpected message (DUT as client) | Client | M | ✅ passed |  |

### SHIP — PIN

| Test case | Title | DUT | M/O | Verdict | Note |
|---|---|---|---|---|---|
| `TC_SHIP_PIN_001` | PIN state none | ServerOrClient | M | ✅ passed |  |

### SHIP — TERM

| Test case | Title | DUT | M/O | Verdict | Note |
|---|---|---|---|---|---|
| `TC_SHIP_TERM_001` | Apply maxTime during termination (DUT initiates) | ServerOrClient | M | ✅ passed |  |

### SHIP — AM

| Test case | Title | DUT | M/O | Verdict | Note |
|---|---|---|---|---|---|
| `TC_SHIP_AM_001` | Verify SHIP ID in access methods response | ServerOrClient | M | ✅ passed |  |

### SHIP — AMDATA

| Test case | Title | DUT | M/O | Verdict | Note |
|---|---|---|---|---|---|
| `TC_SHIP_AMDATA_001` | Parallel processing of SME "data" while answering an "access methods" request | ServerOrClient | M | ✅ passed |  |
| `TC_SHIP_AMDATA_002` | Parallel processing of SME "data" while awaiting a delayed "access methods" response | ServerOrClient | M | ✅ passed |  |
| `TC_SHIP_AMDATA_003` | Parallel processing of SME "access methods" while awaiting a delayed "data" response | ServerOrClient | M | ✅ passed |  |
| `TC_SHIP_AMDATA_004` | Verify that a DUT declaring PAR_queryAccessMethods = "no" sends no "access methods request" | ServerOrClient | M | ➖ not applicable | the device declares PAR_queryAccessMethods = "yes" and is expected to ask |

### SPINE — COMP

| Test case | Title | DUT | M/O | Verdict | Note |
|---|---|---|---|---|---|
| `TC_SPINE_COMP_001` | Reject unknown Function | Special | M | ✅ passed |  |
| `TC_SPINE_COMP_002` | Forward version compatibility | Special | M | ✅ passed |  |
| `TC_SPINE_COMP_003` | Ignore unknown Elements | Special | M | ✅ passed |  |
| `TC_SPINE_COMP_004` | Ignore invalid replies | Special | M | ✅ passed |  |
| `TC_SPINE_COMP_005` | Strict version formatting | Special | M | ✅ passed |  |
| `TC_SPINE_COMP_006` | Version format variations | Special | M | ⚠️ warning | passed, with a behaviour the specification tolerates only for now |

### SPINE — DATA

| Test case | Title | DUT | M/O | Verdict | Note |
|---|---|---|---|---|---|
| `TC_SPINE_DATA_001` | Ascending message counters | Special | M | ✅ passed |  |
| `TC_SPINE_DATA_002` | Allow skipped counters | Special | M | ✅ passed |  |
| `TC_SPINE_DATA_003` | Handle message counter reset or overflow | Special | M | ✅ passed |  |
| `TC_SPINE_DATA_004` | Match reference counter | Special | M | ✅ passed |  |
| `TC_SPINE_DATA_005` | Acknowledge notify datagrams | Special | M | ✅ passed |  |
| `TC_SPINE_DATA_006` | No response to results | Special | M | ✅ passed |  |
| `TC_SPINE_DATA_007` | Ignore ackRequest in read | Special | M | ✅ passed |  |
| `TC_SPINE_DATA_008` | No response to results (DUT initiated) | Special | M | ✅ passed |  |

### SPINE — FC

| Test case | Title | DUT | M/O | Verdict | Note |
|---|---|---|---|---|---|
| `TC_SPINE_FC_001` | Reject non-primary destinations | Special | M | ✅ passed |  |

### SPINE — DDISC

| Test case | Title | DUT | M/O | Verdict | Note |
|---|---|---|---|---|---|
| `TC_SPINE_DDISC_001` | Initial discovery of unknown partner | Special | M | ✅ passed |  |
| `TC_SPINE_DDISC_002` | Disconnect uncommunicative partner | Special | M | ➖ not applicable | the device declares PAR_initialTimeoutSupported = "no"; Annex D.3 leaves this optional |

### SPINE — BIND

| Test case | Title | DUT | M/O | Verdict | Note |
|---|---|---|---|---|---|
| `TC_SPINE_BIND_001` | Deny NodeManagement binding | Special | M | ✅ passed |  |
| `TC_SPINE_BIND_002` | Reject unbound write | Server (CS) | M | ✅ passed |  |

### SPINE — SUBS

| Test case | Title | DUT | M/O | Verdict | Note |
|---|---|---|---|---|---|
| `TC_SPINE_SUBS_001` | Accept NodeManagement subscription | Special | M | ✅ passed |  |
| `TC_SPINE_SUBS_002` | Idempotent subscription deletion | Special | M | ✅ passed |  |

### SPINE — ENTITY

| Test case | Title | DUT | M/O | Verdict | Note |
|---|---|---|---|---|---|
| `TC_SPINE_ENTITY_001` | Dynamic server discovery | Client (EG) | M | ✅ passed |  |
| `TC_SPINE_ENTITY_002` | Dynamic server subscription | Client (CS) | M | ✅ passed |  |

### SPINE — RTS

| Test case | Title | DUT | M/O | Verdict | Note |
|---|---|---|---|---|---|
| `TC_SPINE_RTS_001` | Tolerate arbitrary client Feature types | Server (CS) | M | ✅ passed |  |
| `TC_SPINE_RTS_002` | Tolerate unknown Feature types | Server (CS) | M | ✅ passed |  |
| `TC_SPINE_RTS_003` | Deduce client address from client's binding request | Server (CS) | M | ✅ passed |  |
| `TC_SPINE_RTS_004` | Ignore unknown payload Elements | Server (CS) | M | ✅ passed |  |
| `TC_SPINE_RTS_005` | Apply RFE merge logic ScaledNumberType | Server (CS) | M | ✅ passed |  |

### SPINE — RTC

| Test case | Title | DUT | M/O | Verdict | Note |
|---|---|---|---|---|---|
| `TC_SPINE_RTC_001` | Ignore extra server Elements | Client (EG) | M | ✅ passed |  |
| `TC_SPINE_RTC_002` | Ignore unknown server Elements | Client (EG) | M | ✅ passed |  |
| `TC_SPINE_RTC_003` | Ignore useCaseAvailable flag | Client (EG) | M | ✅ passed |  |

## What happened

### `TC_SHIP_CONN_001` — Resolve simultaneous connections by SKI (DUT has larger SKI)

❌ **failed** — the device kept the older connection A instead of the most recent one - it resolved the double connection by who initiated rather than by which is newer (the rule of ship-go, which the specification does not share)

Verifies `SHIP-TS-CONN-01`.

> **Known deviation.** This stack resolves a double connection the way ship-go does - keeping the connection initiated by the larger SKI rather than the most recent one - because doing otherwise would leave the two sides keeping different connections against the whole installed base. See docs/spec-deviations.md, C1.

| Step | Action | Expected | Result |
|---|---|---|---|
| 1 | The test tool waits passively for the DUT to actively initiate a SHIP connection (Connection A) to the test tool. | The DUT initiates a TCP connection (Connection A) to the test tool. | ✅ passed |
| 2 | As soon as the test tool receives the incoming TCP stream for connection A, it immediately establishes a second, simultaneous SHIP connection (Connection B) to the DUT using the exact same certificate. | The DUT keeps the most recent connection (Connection B) open, continues with the SME phases on it, and actively closes the older connection (Connection A) within 30 s. | ❌ the device kept the older connection A instead of the most recent one - it resolved the double connection by who initiated rather than by which is newer (the rule of ship-go, which the specification does not share) |

### `TC_SPINE_COMP_006` — Version format variations

⚠️ **warning** — passed, with a behaviour the specification tolerates only for now

Verifies `SPINE-TS-COMP-06`.

| Step | Action | Expected | Result |
|---|---|---|---|
| 1 (TS1.3.0) | The test tool sends a nodeManagementDetailedDiscoveryData read request using PAR_readInvalidVersionFormat with the header version set to "TS1.3.0". | The DUT either rejects the request with an application error or by terminating the connection (recommended), or accepts it - which is tolerated with a warning. | ⚠️ accepted "TS1.3.0" and replied |
| 1 (V0.3.0) | The test tool sends a nodeManagementDetailedDiscoveryData read request using PAR_readInvalidVersionFormat with the header version set to "V0.3.0". | The DUT either rejects the request with an application error or by terminating the connection (recommended), or accepts it - which is tolerated with a warning. | ⚠️ accepted "V0.3.0" and replied |
| 1 (v0.3.0) | The test tool sends a nodeManagementDetailedDiscoveryData read request using PAR_readInvalidVersionFormat with the header version set to "v0.3.0". | The DUT either rejects the request with an application error or by terminating the connection (recommended), or accepts it - which is tolerated with a warning. | ⚠️ accepted "v0.3.0" and replied |
| 1 (0.3.0) | The test tool sends a nodeManagementDetailedDiscoveryData read request using PAR_readInvalidVersionFormat with the header version set to "0.3.0". | The DUT either rejects the request with an application error or by terminating the connection (recommended), or accepts it - which is tolerated with a warning. | ⚠️ accepted "0.3.0" and replied |
| 1 (2.0.0) | The test tool sends a nodeManagementDetailedDiscoveryData read request using PAR_readInvalidVersionFormat with the header version set to "2.0.0". | The DUT either rejects the request with an application error or by terminating the connection (recommended), or accepts it - which is tolerated with a warning. | ⚠️ accepted "2.0.0" and replied |
| 1 (V1.3.0) | The test tool sends a nodeManagementDetailedDiscoveryData read request using PAR_readInvalidVersionFormat with the header version set to "V1.3.0". | The DUT either rejects the request with an application error or by terminating the connection (recommended), or accepts it - which is tolerated with a warning. | ⚠️ accepted "V1.3.0" and replied |
| 1 (v1.3.0) | The test tool sends a nodeManagementDetailedDiscoveryData read request using PAR_readInvalidVersionFormat with the header version set to "v1.3.0". | The DUT either rejects the request with an application error or by terminating the connection (recommended), or accepts it - which is tolerated with a warning. | ⚠️ accepted "v1.3.0" and replied |

## Requirement coverage

| Requirement | Source | Verified by | Result |
|---|---|---|---|
| `SHIP-TS-MDNS-01` | SHIP 7.3.2 | `TC_SHIP_MDNS_001` | ✅ passed |
| `SRIP-TS-TXT-01` | SRIP 2.2 | `TC_SHIP_MDNS_001` | ✅ passed |
| `SHIP-TS-CONN-01` | SHIP 12.2.2 | `TC_SHIP_CONN_001` | ❌ failed |
| `SHIP-TS-ROLE-01` | SHIP 13.4.1 | `TC_SHIP_ROLE_001`, `TC_SHIP_ROLE_002`, `TC_SHIP_ROLE_003` | ✅ passed |
| `SHIP-TS-SEC-01` | SHIP 12.2 | `TC_SHIP_SEC_001` | ✅ passed |
| `SHIP-TS-SEC-02` | SHIP 12.2 | `TC_SHIP_SEC_002` | ✅ passed |
| `SHIP-TS-MSG-01` | SHIP 13.4.2 | `TC_SHIP_MSG_001` | ✅ passed |
| `SHIP-TS-MSG-02` | SHIP 13.4.2 | `TC_SHIP_MSG_002` | ✅ passed |
| `SHIP-TS-CMI-01` | SHIP 13.4.3 | `TC_SHIP_ROLE_001`, `TC_SHIP_ROLE_002`, `TC_SHIP_MSG_001` | ✅ passed |
| `SHIP-TS-CMI-02` | SHIP 13.4.3 | `TC_SHIP_CMI_001`, `TC_SHIP_CMI_002` | ✅ passed |
| `SHIP-TS-CMI-03` | SHIP 13.4.3 | `TC_SHIP_CMI_005`, `TC_SHIP_CMI_006` | ✅ passed |
| `SHIP-TS-CMI-04` | SHIP 13.4.3 | `TC_SHIP_CMI_003`, `TC_SHIP_CMI_004` | ✅ passed |
| `SHIP-TS-HELLO-01` | SHIP 13.4.4.1.3 | `TC_SHIP_HELLO_001` | ✅ passed |
| `SHIP-TS-HELLO-02` | SHIP 13.4.4.1.3 | `TC_SHIP_HELLO_002` | ✅ passed |
| `SHIP-TS-HELLO-03` | SHIP 13.4.4.1.3 | `TC_SHIP_HELLO_003` | ✅ passed |
| `SHIP-TS-HELLO-04` | SHIP 13.4.4.1.3 | `TC_SHIP_HELLO_004` | ✅ passed |
| `SHIP-TS-PROT-01` | SHIP 13.4.4.2.2 | `TC_SHIP_PROT_001`, `TC_SHIP_PROT_002` | ✅ passed |
| `SHIP-TS-PROT-02` | SHIP 13.4.4.2.3 | `TC_SHIP_PROT_003`, `TC_SHIP_PROT_004` | ✅ passed |
| `SHIP-TS-PROT-03` | SHIP 13.4.4.2.3 | `TC_SHIP_PROT_005`, `TC_SHIP_PROT_006` | ✅ passed |
| `SHIP-TS-PIN-01` | SHIP 13.4.4.3.5.1 | `TC_SHIP_PIN_001` | ✅ passed |
| `SHIP-TS-TERM-01` | SHIP 13.4.8.1.2 | `TC_SHIP_TERM_001` | ✅ passed |
| `SHIP-TS-DATA-01` | SHIP 13.4.6 | `TC_SHIP_AM_001` | ✅ passed |
| `SHIP-TS-ACC-01` | SHIP 13.4.6 | `TC_SHIP_MSG_003` | ✅ passed |
| `SHIP-IG-01` | SHIP-IG 2.2 | `TC_SHIP_MSG_003` | ✅ passed |
| `SHIP-IG-02` | SHIP-IG 2.1 | `TC_SHIP_AMDATA_001`, `TC_SHIP_AMDATA_002` | ✅ passed |
| `SHIP-IG-03` | SHIP-IG 2.1 | `TC_SHIP_AMDATA_003` | ✅ passed |
| `SPINE-TS-BIND-01` | SPINE_PS 7.3.1, 7.3.2 | `TC_SPINE_BIND_001` | ✅ passed |
| `SPINE-TS-BIND-02` | SPINE_PS 7.3.5 | `TC_SPINE_BIND_002` | ✅ passed |
| `SPINE-TS-SUBS-01` | SPINE_PS 7.4.4 | `TC_SPINE_SUBS_001` | ✅ passed |
| `SPINE-TS-SUBS-02` | SPINE_PS 7.4.1, 7.5.1 | `TC_SPINE_SUBS_002` | ✅ passed |
| `SPINE-TS-COMP-01` | SPINE_RS Table 19 | `TC_SPINE_COMP_001` | ✅ passed |
| `SPINE-TS-COMP-02` | SPINE_PS 4.3.4.3, 4.3.4.7 | `TC_SPINE_COMP_002`, `TC_SPINE_RTS_002`, `TC_SPINE_RTS_004`, `TC_SPINE_RTC_002` | ✅ passed |
| `SPINE-TS-COMP-03` | SPINE_PS 4.3.4 | `TC_SPINE_COMP_003`, `TC_SPINE_RTS_004`, `TC_SPINE_RTC_001`, `TC_SPINE_RTC_002` | ✅ passed |
| `SPINE-TS-COMP-04` | SPINE_PS 5.2.4 Table 2, 5.2.5.1 | `TC_SPINE_COMP_004` | ✅ passed |
| `SPINE-TS-COMP-05` | IG-SPINE 2.5 | `TC_SPINE_COMP_005` | ✅ passed |
| `SPINE-TS-COMP-06` | SPINE_RS 3.10.1.4 | `TC_SPINE_COMP_006` | ⚠️ warning |
| `SPINE-TS-DATA-01` | SPINE_PS 5.2.3.1 | `TC_SPINE_DATA_001` | ✅ passed |
| `SPINE-TS-DATA-02` | SPINE_PS 5.2.3.1 | `TC_SPINE_DATA_002` | ✅ passed |
| `SPINE-TS-DATA-03` | SPINE_PS 5.2.3.1 | `TC_SPINE_DATA_003` | ✅ passed |
| `SPINE-TS-DATA-04` | SPINE_PS 5.2.3.2 | `TC_SPINE_DATA_004` | ✅ passed |
| `SPINE-TS-DATA-05` | SPINE_PS 5.2.5.1 | `TC_SPINE_DATA_005`, `TC_SPINE_RTS_001` | ✅ passed |
| `SPINE-TS-DATA-06` | SPINE_PS 5.2.4 | `TC_SPINE_DATA_006`, `TC_SPINE_DATA_008` | ✅ passed |
| `SPINE-TS-DATA-07` | SPINE_PS 5.2.4 | `TC_SPINE_DATA_007` | ✅ passed |
| `SPINE-TS-FC-01` | SPINE_PS 7.1, IG-SPINE 2.3 | `TC_SPINE_FC_001` | ✅ passed |
| `SPINE-TS-DD-01` | SPINE_PS Annex D.3 | `TC_SPINE_DDISC_001` | ✅ passed |
| `SPINE-TS-DD-02` | SPINE_PS Annex D.3 | `TC_SPINE_DDISC_002` | ➖ not applicable |
| `SPINE-TS-UCD-01` | IG-SPINE 2.2 | `TC_SPINE_RTC_003` | ✅ passed |
| `SPINE-TS-ES-01` | UC_LPC 3.4.1, 3.4.3 | `TC_SPINE_ENTITY_001` | ✅ passed |
| `SPINE-TS-ES-02` | UC_LPC 3.4.3 | `TC_SPINE_ENTITY_002` | ✅ passed |
| `SPINE-TS-RT-01` | SPINE_PS 3.1, 7.1.1.4 | `TC_SPINE_RTS_001` | ✅ passed |
| `SPINE-TS-RT-02` | UC_LPC 3.4.1.1, 3.4.3.1 | `TC_SPINE_RTS_003` | ✅ passed |
| `SPINE-TS-RT-03` | SPINE_PS 5.3.4.8 | `TC_SPINE_RTS_005` | ✅ passed |

---

Generated by `eebus conform`. The EEBUS specifications are licensed material and are not part of this repository; this report reproduces identifiers and section references only.
