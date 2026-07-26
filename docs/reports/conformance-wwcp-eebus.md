# EEBUS conformance report — WWCP_EEBUS

*2026-07-26 20:31:36 UTC · GraphDefined GmbH · SHIP 1.0 · SPINE 1.3.0 · actors CS, EG, GCP, MA, MU*

Catalog: `EEBus_SHIP_TestSpecification_V1.0.0`, `EEBus_SPINE_TestSpecification_V1.0.0`. Identifiers are the official ones.

| Verdict | Cases |
|---|---|
| ✅ passed | 231 |
| ⚠️ warning | 3 |
| ❌ failed | 1 |
| ➖ not applicable | 32 |
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

### UseCase — LPC EG

| Test case | Title | DUT | M/O | Verdict | Note |
|---|---|---|---|---|---|
| `ATC_LPC_COM_PT_EGHeartbeat_001` | The energy guard sends its heartbeat at least every 60 seconds | Client (EG) | M | ✅ passed |  |
| `ATC_LPC_COM_PT_EGConnection_001` | The energy guard sends a heartbeat and a following APCL after it has rebooted | Client (EG) | M | ✅ passed |  |
| `ATC_LPC_COM_PT_EGConnection_002` | The energy guard sends a heartbeat and a following APCL after the connection is restored | Client (EG) | M | ✅ passed |  |
| `ATC_LPC_COM_PT_EGConnection_003` | The energy guard reconnects by itself after a black start | Client (EG) | M | ✅ passed |  |
| `ATC_LPC_COM_PT_EGMessages_001` | An external stimulus makes the energy guard write an activated APCL | Client (EG) | M | ✅ passed |  |
| `ATC_LPC_COM_PT_EGMessages_002` | The energy guard resends its limit after the controllable system has rejected it | Client (EG) | M | ➖ not applicable | the device declares no "quick resend write {L} if previous write {L} was rejected" (parameter sheet, "Optional Support" A5) |
| `ATC_LPC_COM_PT_EGMessages_003` | The energy guard writes valid limits over an extended period | Client (EG) | M | ✅ passed |  |
| `ATC_LPC_COM_PT_EGMessages_004` | The energy guard writes valid failsafe values over an extended period | Client (EG) | M | ✅ passed |  |

### UseCase — LPC CS

| Test case | Title | DUT | M/O | Verdict | Note |
|---|---|---|---|---|---|
| `ATC_LPC_COM_PT_CSHeartbeat_001` | The controllable system sends its heartbeat at least every 60 seconds | Server (CS) | M | ✅ passed |  |
| `ATC_LPC_COM_NT_CSConnection_001` | The controllable system evaluates no limit before the first heartbeat | Server (CS) | M | ✅ passed |  |
| `ATC_LPC_COM_PT_CSConnection_002` | The controllable system evaluates no FCAPL write before a heartbeat and a limit | Server (CS) | M | ✅ passed |  |
| `ATC_LPC_COM_PT_CSConnection_003` | The controllable system accepts only APCL and FCAPL values above zero | Server (CS) | M | ✅ passed |  |
| `ATC_LPC_COM_PT_CSConnection_004` | The controllable system evaluates no failsafe duration write before a heartbeat and a limit | Server (CS) | M | ✅ passed |  |
| `ATC_LPC_COM_PT_CSConnection_005` | The controllable system handles a failsafe duration minimum above its own maximum | Server (CS) | M | ✅ passed |  |
| `ATC_LPC_COM_PT_CSConnection_006` | The controllable system alters an APCL larger than it can store | Server (CS) | M | ✅ passed |  |
| `ATC_LPC_COM_PT_CSConnection_007` | The controllable system evaluates APCL writes correctly across the whole range | Server (CS) | M | ✅ passed |  |
| `ATC_LPC_COM_PT_CSConnection_008` | The controllable system evaluates FCAPL and failsafe duration writes correctly | Server (CS) | M | ✅ passed |  |
| `ATC_LPC_COM_PT_CSConnection_009` | The controllable system reconnects by itself after a black start | Server (CS) | M | ✅ passed |  |
| `ATC_LPC_COM_PT_CSInit_001` | The controllable system starts limited to its FCAPL with a deactivated APCL | Server (CS) | M | ✅ passed |  |
| `ATC_LPC_COM_PT_CSInit_002` | The controllable system starts with its default parameters after a factory reset | Server (CS) | M | ✅ passed |  |
| `ATC_LPC_COM_PT_CSInit_003` | The controllable system stores the FCAPL and the failsafe duration minimum persistently | Server (CS) | M | ✅ passed |  |
| `ATC_LPC_COM_NT_CSLimited_001` | A rejected limit leaves the controllable system limited and activated | Server (CS) | M | ✅ passed |  |
| `ATC_LPC_COM_PT_CSLimited_002` | The controllable system keeps accepting limits while the heartbeat is briefly absent | Server (CS) | M | ✅ passed |  |
| `ATC_LPC_COM_NT_CSUnlCntrl_001` | A rejected limit leaves the controllable system in "unlimited/controlled" | Server (CS) | M | ✅ passed |  |
| `ATC_LPC_COM_PT_CSUnlCntrl_002` | An energy manager reports its Contractual consumption Nominal Max and not the other one | Server (CS) | M | ➖ not applicable | the controllable system is not an energy manager, so it reports the Power Nominal Max instead (Table 13, footnote 4) |
| `ATC_LPC_COM_PT_CSUnlCntrl_003` | A device which is not an energy manager reports its Power consumption Nominal Max and not the other one | Server (CS) | M | ✅ passed |  |
| `ATC_LPC_COM_PT_CSFS_001` | In its failsafe state the controllable system evaluates nothing before a heartbeat and a limit | Server (CS) | M | ✅ passed |  |
| `ATC_LPC_COM_PT_CSFS_002` | The controllable system stays in its failsafe state for the failsafe duration minimum | Server (CS) | M | ✅ passed |  |
| `ATC_LPC_COM_PT_CSFS_003` | The controllable system rejects a failsafe duration write while in its failsafe state | Server (CS) | M | ✅ passed |  |
| `ATC_LPC_COM_NT_CSUnlAuto_001` | In "unlimited/autonomous" the controllable system evaluates nothing before a heartbeat and a limit | Server (CS) | M | ✅ passed |  |
| `ATC_LPC_COM_PT_CSUnlAuto_002` | The controllable system stays below its nominal maximum with the limit deactivated | Server (CS) | M | ➖ not applicable | step 1 compares the actual power against the nominal maximum, which is a physical measurement rather than anything on the wire (parameter sheet, "Supplementary optional verifications" M1/N1) |
| `ATC_LPC_COM_PT_CSTransition1_001` | Transition 1: a rejected activated limit takes "init" to "unlimited/controlled" | Server (CS) | M | ✅ passed |  |
| `ATC_LPC_COM_PT_CSTransition1_002` | Transition 1: an accepted deactivated limit takes "init" to "unlimited/controlled" | Server (CS) | M | ✅ passed |  |
| `ATC_LPC_COM_PT_CSTransition2_001` | Transition 2: an accepted activated limit takes "init" to "limited" | Server (CS) | M | ✅ passed |  |
| `ATC_LPC_COM_PT_CSTransition3_001` | Transition 3: no heartbeat at all takes "init" to "unlimited/autonomous" | Server (CS) | M | ✅ passed |  |
| `ATC_LPC_COM_PT_CSTransition3_002` | Transition 3: a heartbeat without a following limit takes "init" to "unlimited/autonomous" | Server (CS) | M | ✅ passed |  |
| `ATC_LPC_COM_PT_CSTransition4_001` | Transition 4: an accepted activated limit takes "unlimited/controlled" to "limited" | Server (CS) | M | ✅ passed |  |
| `ATC_LPC_COM_PT_CSTransition5_001` | Transition 5: a silent heartbeat takes "unlimited/controlled" to the failsafe state | Server (CS) | M | ✅ passed |  |
| `ATC_LPC_COM_PT_CSTransition6_001` | Transition 6: an expired duration takes "limited" to "unlimited/controlled" | Server (CS) | M | ⚠️ warning | passed, with a behaviour the specification tolerates only for now |
| `ATC_LPC_COM_PT_CSTransition6_002` | Transition 6: a deactivation takes "limited" to "unlimited/controlled" | Server (CS) | M | ✅ passed |  |
| `ATC_LPC_COM_PT_CSTransition7_001` | Transition 7: a silent heartbeat takes "limited" to the failsafe state | Server (CS) | M | ✅ passed |  |
| `ATC_LPC_COM_PT_CSTransition8_001` | Transition 8: a limit which cannot be applied takes the failsafe state to "unlimited/controlled" | Server (CS) | M | ✅ passed |  |
| `ATC_LPC_COM_PT_CSTransition8_002` | Transition 8: a deactivated limit takes the failsafe state to "unlimited/controlled" | Server (CS) | M | ✅ passed |  |
| `ATC_LPC_COM_PT_CSTransition9_001` | Transition 9: an accepted activated limit takes the failsafe state to "limited" | Server (CS) | M | ✅ passed |  |
| `ATC_LPC_COM_PT_CSTransition10_001` | Transition 10: the expiring failsafe duration takes the failsafe state to "unlimited/autonomous" | Server (CS) | M | ✅ passed |  |
| `ATC_LPC_COM_PT_CSTransition10_002` | Transition 10: a heartbeat without a following limit takes the failsafe state to "unlimited/autonomous" | Server (CS) | M | ✅ passed |  |
| `ATC_LPC_COM_PT_CSTransition11_001` | Transition 11: a rejected limit takes "unlimited/autonomous" to "unlimited/controlled" | Server (CS) | M | ✅ passed |  |
| `ATC_LPC_COM_PT_CSTransition11_002` | Transition 11: a deactivated limit takes "unlimited/autonomous" to "unlimited/controlled" | Server (CS) | M | ✅ passed |  |
| `ATC_LPC_COM_PT_CSTransition12_001` | Transition 12: an accepted activated limit takes "unlimited/autonomous" to "limited" | Server (CS) | M | ✅ passed |  |

### UseCase — LPC INS1

| Test case | Title | DUT | M/O | Verdict | Note |
|---|---|---|---|---|---|
| `ATC_LPC_INS1_PT_CSTransition1_001` | On an energy manager the controllable system may reject a limit for a permitted reason | Server (CS) | M | ➖ not applicable | the controllable system is not an energy manager, so this is use case instance 2 |

### UseCase — LPC INS2

| Test case | Title | DUT | M/O | Verdict | Note |
|---|---|---|---|---|---|
| `ATC_LPC_INS2_PT_CSTransition1_001` | Off an energy manager the controllable system may reject a limit for a permitted reason | Server (CS) | M | ✅ passed |  |

### UseCase — LPP EG

| Test case | Title | DUT | M/O | Verdict | Note |
|---|---|---|---|---|---|
| `ATC_LPP_COM_PT_EGHeartbeat_001` | The energy guard sends its heartbeat at least every 60 seconds | Client (EG) | M | ✅ passed |  |
| `ATC_LPP_COM_PT_EGConnection_001` | The energy guard sends a heartbeat and a following APPL after it has rebooted | Client (EG) | M | ✅ passed |  |
| `ATC_LPP_COM_PT_EGConnection_002` | The energy guard sends a heartbeat and a following APPL after the connection is restored | Client (EG) | M | ✅ passed |  |
| `ATC_LPP_COM_PT_EGConnection_003` | The energy guard reconnects by itself after a black start | Client (EG) | M | ✅ passed |  |
| `ATC_LPP_COM_PT_EGMessages_001` | An external stimulus makes the energy guard write an activated APPL | Client (EG) | M | ✅ passed |  |
| `ATC_LPP_COM_PT_EGMessages_002` | The energy guard resends its limit after the controllable system has rejected it | Client (EG) | M | ➖ not applicable | the device declares no "quick resend write {L} if previous write {L} was rejected" (parameter sheet, "Optional Support" A5) |
| `ATC_LPP_COM_PT_EGMessages_003` | The energy guard writes valid limits over an extended period | Client (EG) | M | ✅ passed |  |
| `ATC_LPP_COM_PT_EGMessages_004` | The energy guard writes valid failsafe values over an extended period | Client (EG) | M | ✅ passed |  |

### UseCase — LPP CS

| Test case | Title | DUT | M/O | Verdict | Note |
|---|---|---|---|---|---|
| `ATC_LPP_COM_PT_CSHeartbeat_001` | The controllable system sends its heartbeat at least every 60 seconds | Server (CS) | M | ✅ passed |  |
| `ATC_LPP_COM_NT_CSConnection_001` | The controllable system evaluates no limit before the first heartbeat | Server (CS) | M | ✅ passed |  |
| `ATC_LPP_COM_PT_CSConnection_002` | The controllable system evaluates no FPAPL write before a heartbeat and a limit | Server (CS) | M | ✅ passed |  |
| `ATC_LPP_COM_PT_CSConnection_003` | The controllable system accepts only APPL and FPAPL values above zero | Server (CS) | M | ✅ passed |  |
| `ATC_LPP_COM_PT_CSConnection_004` | The controllable system evaluates no failsafe duration write before a heartbeat and a limit | Server (CS) | M | ✅ passed |  |
| `ATC_LPP_COM_PT_CSConnection_005` | The controllable system handles a failsafe duration minimum above its own maximum | Server (CS) | M | ✅ passed |  |
| `ATC_LPP_COM_PT_CSConnection_006` | The controllable system alters an APPL larger than it can store | Server (CS) | M | ✅ passed |  |
| `ATC_LPP_COM_PT_CSConnection_007` | The controllable system evaluates APPL writes correctly across the whole range | Server (CS) | M | ✅ passed |  |
| `ATC_LPP_COM_PT_CSConnection_008` | The controllable system evaluates FPAPL and failsafe duration writes correctly | Server (CS) | M | ✅ passed |  |
| `ATC_LPP_COM_PT_CSConnection_009` | The controllable system reconnects by itself after a black start | Server (CS) | M | ✅ passed |  |
| `ATC_LPP_COM_PT_CSInit_001` | The controllable system starts limited to its FPAPL with a deactivated APPL | Server (CS) | M | ✅ passed |  |
| `ATC_LPP_COM_PT_CSInit_002` | The controllable system starts with its default parameters after a factory reset | Server (CS) | M | ✅ passed |  |
| `ATC_LPP_COM_PT_CSInit_003` | The controllable system stores the FPAPL and the failsafe duration minimum persistently | Server (CS) | M | ✅ passed |  |
| `ATC_LPP_COM_NT_CSLimited_001` | A rejected limit leaves the controllable system limited and activated | Server (CS) | M | ✅ passed |  |
| `ATC_LPP_COM_PT_CSLimited_002` | The controllable system keeps accepting limits while the heartbeat is briefly absent | Server (CS) | M | ✅ passed |  |
| `ATC_LPP_COM_NT_CSUnlCntrl_001` | A rejected limit leaves the controllable system in "unlimited/controlled" | Server (CS) | M | ✅ passed |  |
| `ATC_LPP_COM_PT_CSUnlCntrl_002` | An energy manager reports its Contractual production Nominal Max and not the other one | Server (CS) | M | ➖ not applicable | the controllable system is not an energy manager, so it reports the Power Nominal Max instead (Table 13, footnote 4) |
| `ATC_LPP_COM_PT_CSUnlCntrl_003` | A device which is not an energy manager reports its Power production Nominal Max and not the other one | Server (CS) | M | ✅ passed |  |
| `ATC_LPP_COM_PT_CSFS_001` | In its failsafe state the controllable system evaluates nothing before a heartbeat and a limit | Server (CS) | M | ✅ passed |  |
| `ATC_LPP_COM_PT_CSFS_002` | The controllable system stays in its failsafe state for the failsafe duration minimum | Server (CS) | M | ✅ passed |  |
| `ATC_LPP_COM_PT_CSFS_003` | The controllable system rejects a failsafe duration write while in its failsafe state | Server (CS) | M | ✅ passed |  |
| `ATC_LPP_COM_NT_CSUnlAuto_001` | In "unlimited/autonomous" the controllable system evaluates nothing before a heartbeat and a limit | Server (CS) | M | ✅ passed |  |
| `ATC_LPP_COM_PT_CSUnlAuto_002` | The controllable system stays below its nominal maximum with the limit deactivated | Server (CS) | M | ➖ not applicable | step 1 compares the actual power against the nominal maximum, which is a physical measurement rather than anything on the wire (parameter sheet, "Supplementary optional verifications" M1/N1) |
| `ATC_LPP_COM_PT_CSTransition1_001` | Transition 1: a rejected activated limit takes "init" to "unlimited/controlled" | Server (CS) | M | ✅ passed |  |
| `ATC_LPP_COM_PT_CSTransition1_002` | Transition 1: an accepted deactivated limit takes "init" to "unlimited/controlled" | Server (CS) | M | ✅ passed |  |
| `ATC_LPP_COM_PT_CSTransition2_001` | Transition 2: an accepted activated limit takes "init" to "limited" | Server (CS) | M | ✅ passed |  |
| `ATC_LPP_COM_PT_CSTransition3_001` | Transition 3: no heartbeat at all takes "init" to "unlimited/autonomous" | Server (CS) | M | ✅ passed |  |
| `ATC_LPP_COM_PT_CSTransition3_002` | Transition 3: a heartbeat without a following limit takes "init" to "unlimited/autonomous" | Server (CS) | M | ✅ passed |  |
| `ATC_LPP_COM_PT_CSTransition4_001` | Transition 4: an accepted activated limit takes "unlimited/controlled" to "limited" | Server (CS) | M | ✅ passed |  |
| `ATC_LPP_COM_PT_CSTransition5_001` | Transition 5: a silent heartbeat takes "unlimited/controlled" to the failsafe state | Server (CS) | M | ✅ passed |  |
| `ATC_LPP_COM_PT_CSTransition6_001` | Transition 6: an expired duration takes "limited" to "unlimited/controlled" | Server (CS) | M | ⚠️ warning | passed, with a behaviour the specification tolerates only for now |
| `ATC_LPP_COM_PT_CSTransition6_002` | Transition 6: a deactivation takes "limited" to "unlimited/controlled" | Server (CS) | M | ✅ passed |  |
| `ATC_LPP_COM_PT_CSTransition7_001` | Transition 7: a silent heartbeat takes "limited" to the failsafe state | Server (CS) | M | ✅ passed |  |
| `ATC_LPP_COM_PT_CSTransition8_001` | Transition 8: a limit which cannot be applied takes the failsafe state to "unlimited/controlled" | Server (CS) | M | ✅ passed |  |
| `ATC_LPP_COM_PT_CSTransition8_002` | Transition 8: a deactivated limit takes the failsafe state to "unlimited/controlled" | Server (CS) | M | ✅ passed |  |
| `ATC_LPP_COM_PT_CSTransition9_001` | Transition 9: an accepted activated limit takes the failsafe state to "limited" | Server (CS) | M | ✅ passed |  |
| `ATC_LPP_COM_PT_CSTransition10_001` | Transition 10: the expiring failsafe duration takes the failsafe state to "unlimited/autonomous" | Server (CS) | M | ✅ passed |  |
| `ATC_LPP_COM_PT_CSTransition10_002` | Transition 10: a heartbeat without a following limit takes the failsafe state to "unlimited/autonomous" | Server (CS) | M | ✅ passed |  |
| `ATC_LPP_COM_PT_CSTransition11_001` | Transition 11: a rejected limit takes "unlimited/autonomous" to "unlimited/controlled" | Server (CS) | M | ✅ passed |  |
| `ATC_LPP_COM_PT_CSTransition11_002` | Transition 11: a deactivated limit takes "unlimited/autonomous" to "unlimited/controlled" | Server (CS) | M | ✅ passed |  |
| `ATC_LPP_COM_PT_CSTransition12_001` | Transition 12: an accepted activated limit takes "unlimited/autonomous" to "limited" | Server (CS) | M | ✅ passed |  |

### UseCase — LPP INS1

| Test case | Title | DUT | M/O | Verdict | Note |
|---|---|---|---|---|---|
| `ATC_LPP_INS1_PT_CSTransition1_001` | On an energy manager the controllable system may reject a limit for a permitted reason | Server (CS) | M | ➖ not applicable | the controllable system is not an energy manager, so this is use case instance 2 |

### UseCase — LPP INS2

| Test case | Title | DUT | M/O | Verdict | Note |
|---|---|---|---|---|---|
| `ATC_LPP_INS2_PT_CSTransition1_001` | Off an energy manager the controllable system may reject a limit for a permitted reason | Server (CS) | M | ✅ passed |  |

### UseCase — MGCP GCP

| Test case | Title | DUT | M/O | Verdict | Note |
|---|---|---|---|---|---|
| `ATC_MGCP_COM_PT_GCPPolling_001` | The grid connection point answers a poll within 120 seconds | Server (GCP) | M | ➖ not applicable | the device does not request or send data at an interval (parameter sheet, "Data transmission") |
| `ATC_MGCP_COM_PT_GCPNotification_001` | The grid connection point sends changed data within 120 seconds | Server (GCP) | M | ✅ passed |  |
| `ATC_MGCP_SCE1_PT_GCPPowerLimitFactor_001` | The grid connection point sends the PV feed-in power limitation factor | Server (GCP) | M | ✅ passed |  |
| `ATC_MGCP_SCE2_PT_GCPTotalActivePower_001` | The grid connection point sends its momentary power in both directions | Server (GCP) | M | ✅ passed |  |
| `ATC_MGCP_SCE3_PT_GCPTotalFeedInEnergy_001` | The total feed-in energy does not move while the grid connection point consumes | Server (GCP) | M | ✅ passed |  |
| `ATC_MGCP_SCE3_PT_GCPTotalFeedInEnergy_002` | The total feed-in energy rises while the grid connection point produces | Server (GCP) | M | ✅ passed |  |
| `ATC_MGCP_SCE4_PT_GCPTotalConsumedEnergy_001` | The total consumed energy rises while the grid connection point consumes | Server (GCP) | M | ✅ passed |  |
| `ATC_MGCP_SCE4_PT_GCPTotalConsumedEnergy_002` | The total consumed energy does not move while the grid connection point produces | Server (GCP) | M | ✅ passed |  |
| `ATC_MGCP_SCE5_PT_GCPActiveACCurrent_001` | The grid connection point sends the active AC current on phase A | Server (GCP) | M | ✅ passed |  |
| `ATC_MGCP_SCE5_PT_GCPActiveACCurrent_002` | The grid connection point sends the active AC current on phase B | Server (GCP) | M | ✅ passed |  |
| `ATC_MGCP_SCE5_PT_GCPActiveACCurrent_003` | The grid connection point sends the active AC current on phase C | Server (GCP) | M | ✅ passed |  |
| `ATC_MGCP_SCE6_PT_GCPACVoltage_001` | The grid connection point sends the AC voltage between phase A and neutral | Server (GCP) | M | ✅ passed |  |
| `ATC_MGCP_SCE6_PT_GCPACVoltage_002` | The grid connection point sends the AC voltage between phase B and neutral | Server (GCP) | M | ✅ passed |  |
| `ATC_MGCP_SCE6_PT_GCPACVoltage_003` | The grid connection point sends the AC voltage between phase C and neutral | Server (GCP) | M | ✅ passed |  |
| `ATC_MGCP_SCE6_PT_GCPACVoltage_004` | The grid connection point sends the AC voltage between phase A and phase B | Server (GCP) | M | ➖ not applicable | the device does not measure the voltage between two phases (parameter sheet, "Phase-to-phase AC Voltage") |
| `ATC_MGCP_SCE6_PT_GCPACVoltage_005` | The grid connection point sends the AC voltage between phase B and phase C | Server (GCP) | M | ➖ not applicable | the device does not measure the voltage between two phases (parameter sheet, "Phase-to-phase AC Voltage") |
| `ATC_MGCP_SCE6_PT_GCPACVoltage_006` | The grid connection point sends the AC voltage between phase C and phase A | Server (GCP) | M | ➖ not applicable | the device does not measure the voltage between two phases (parameter sheet, "Phase-to-phase AC Voltage") |
| `ATC_MGCP_SCE7_PT_GCPFrequency_001` | The grid connection point sends the frequency | Server (GCP) | M | ✅ passed |  |

### UseCase — MGCP MA

| Test case | Title | DUT | M/O | Verdict | Note |
|---|---|---|---|---|---|
| `ATC_MGCP_COM_PT_MAPolling_001` | The monitoring appliance polls at the interval it declared | Client (MA) | M | ➖ not applicable | the device does not request or send data at an interval (parameter sheet, "Data transmission") |
| `ATC_MGCP_COM_PT_MANotification_001` | The monitoring appliance receives changed data within 120 seconds | Client (MA) | M | ✅ passed |  |
| `ATC_MGCP_SCE1_PT_MAPowerLimitFactor_001` | The monitoring appliance receives the PV feed-in power limitation factor | Client (MA) | M | ✅ passed |  |
| `ATC_MGCP_SCE2_PT_MATotalActivePower_001` | The monitoring appliance receives the momentary power with state "normal" | Client (MA) | M | ✅ passed |  |
| `ATC_MGCP_SCE2_NT_MATotalActivePower_002` | The monitoring appliance discards a momentary power which is out of range or erroneous | Client (MA) | M | ✅ passed |  |
| `ATC_MGCP_SCE3_PT_MATotalFeedInEnergy_001` | The monitoring appliance receives the total feed-in energy with state "normal" | Client (MA) | M | ✅ passed |  |
| `ATC_MGCP_SCE3_NT_MATotalFeedInEnergy_002` | The monitoring appliance discards a total feed-in energy which is out of range or erroneous | Client (MA) | M | ✅ passed |  |
| `ATC_MGCP_SCE4_PT_MATotalConsumedEnergy_001` | The monitoring appliance receives the total consumed energy with state "normal" | Client (MA) | M | ✅ passed |  |
| `ATC_MGCP_SCE4_NT_MATotalConsumedEnergy_002` | The monitoring appliance discards a total consumed energy which is out of range or erroneous | Client (MA) | M | ✅ passed |  |
| `ATC_MGCP_SCE5_PT_MAActiveACCurrent_001` | The monitoring appliance receives the AC current on phase A with state "normal" | Client (MA) | M | ✅ passed |  |
| `ATC_MGCP_SCE5_NT_MAActiveACCurrent_002` | The monitoring appliance discards an AC current on phase A which is out of range or erroneous | Client (MA) | M | ✅ passed |  |
| `ATC_MGCP_SCE5_PT_MAActiveACCurrent_003` | The monitoring appliance receives the AC current on phase B with state "normal" | Client (MA) | M | ✅ passed |  |
| `ATC_MGCP_SCE5_NT_MAActiveACCurrent_004` | The monitoring appliance discards an AC current on phase B which is out of range or erroneous | Client (MA) | M | ✅ passed |  |
| `ATC_MGCP_SCE5_PT_MAActiveACCurrent_005` | The monitoring appliance receives the AC current on phase C with state "normal" | Client (MA) | M | ✅ passed |  |
| `ATC_MGCP_SCE5_NT_MAActiveACCurrent_006` | The monitoring appliance discards an AC current on phase C which is out of range or erroneous | Client (MA) | M | ✅ passed |  |
| `ATC_MGCP_SCE6_PT_MAACVoltage_001` | The monitoring appliance receives the AC voltage between phase A and neutral with state "normal" | Client (MA) | M | ✅ passed |  |
| `ATC_MGCP_SCE6_NT_MAACVoltage_002` | The monitoring appliance discards an AC voltage between phase A and neutral which is out of range or erroneous | Client (MA) | M | ✅ passed |  |
| `ATC_MGCP_SCE6_PT_MAACVoltage_003` | The monitoring appliance receives the AC voltage between phase B and neutral with state "normal" | Client (MA) | M | ✅ passed |  |
| `ATC_MGCP_SCE6_NT_MAACVoltage_004` | The monitoring appliance discards an AC voltage between phase B and neutral which is out of range or erroneous | Client (MA) | M | ✅ passed |  |
| `ATC_MGCP_SCE6_PT_MAACVoltage_005` | The monitoring appliance receives the AC voltage between phase C and neutral with state "normal" | Client (MA) | M | ✅ passed |  |
| `ATC_MGCP_SCE6_NT_MAACVoltage_006` | The monitoring appliance discards an AC voltage between phase C and neutral which is out of range or erroneous | Client (MA) | M | ✅ passed |  |
| `ATC_MGCP_SCE6_PT_MAACVoltage_007` | The monitoring appliance receives the AC voltage between phase A and phase B with state "normal" | Client (MA) | M | ➖ not applicable | the device does not measure the voltage between two phases (parameter sheet, "Phase-to-phase AC Voltage") |
| `ATC_MGCP_SCE6_NT_MAACVoltage_008` | The monitoring appliance discards an AC voltage between phase A and phase B which is out of range or erroneous | Client (MA) | M | ➖ not applicable | the device does not measure the voltage between two phases (parameter sheet, "Phase-to-phase AC Voltage") |
| `ATC_MGCP_SCE6_PT_MAACVoltage_009` | The monitoring appliance receives the AC voltage between phase B and phase C with state "normal" | Client (MA) | M | ➖ not applicable | the device does not measure the voltage between two phases (parameter sheet, "Phase-to-phase AC Voltage") |
| `ATC_MGCP_SCE6_NT_MAACVoltage_010` | The monitoring appliance discards an AC voltage between phase B and phase C which is out of range or erroneous | Client (MA) | M | ➖ not applicable | the device does not measure the voltage between two phases (parameter sheet, "Phase-to-phase AC Voltage") |
| `ATC_MGCP_SCE6_PT_MAACVoltage_011` | The monitoring appliance receives the AC voltage between phase C and phase A with state "normal" | Client (MA) | M | ➖ not applicable | the device does not measure the voltage between two phases (parameter sheet, "Phase-to-phase AC Voltage") |
| `ATC_MGCP_SCE6_NT_MAACVoltage_012` | The monitoring appliance discards an AC voltage between phase C and phase A which is out of range or erroneous | Client (MA) | M | ➖ not applicable | the device does not measure the voltage between two phases (parameter sheet, "Phase-to-phase AC Voltage") |
| `ATC_MGCP_SCE7_PT_MAFrequency_001` | The monitoring appliance receives the frequency with state "normal" | Client (MA) | M | ✅ passed |  |
| `ATC_MGCP_SCE7_NT_MAFrequency_002` | The monitoring appliance discards a frequency which is out of range or erroneous | Client (MA) | M | ✅ passed |  |

### UseCase — MPC MU

| Test case | Title | DUT | M/O | Verdict | Note |
|---|---|---|---|---|---|
| `ATC_MPC_COM_PT_MUPolling_001` | The monitored unit answers a poll within 120 seconds | Server (MU) | M | ➖ not applicable | the device does not request or send data at an interval (parameter sheet, "Data transmission") |
| `ATC_MPC_COM_PT_MUNotification_001` | The monitored unit sends changed data within 120 seconds | Server (MU) | M | ✅ passed |  |
| `ATC_MPC_SCE1_PT_MUTotalActivePower_001` | The monitored unit sends its momentary power in both directions | Server (MU) | M | ✅ passed |  |
| `ATC_MPC_SCE1_PT_MUPhaseActivePower_001` | The monitored unit sends the active power on phase A | Server (MU) | M | ✅ passed |  |
| `ATC_MPC_SCE1_PT_MUPhaseActivePower_002` | The monitored unit sends the active power on phase B | Server (MU) | M | ✅ passed |  |
| `ATC_MPC_SCE1_PT_MUPhaseActivePower_003` | The monitored unit sends the active power on phase C | Server (MU) | M | ✅ passed |  |
| `ATC_MPC_SCE2_PT_MUTotalConsumedEnergy_001` | The total consumed energy rises while the monitored unit consumes | Server (MU) | M | ✅ passed |  |
| `ATC_MPC_SCE2_PT_MUTotalConsumedEnergy_002` | The total consumed energy does not move while the monitored unit produces | Server (MU) | M | ✅ passed |  |
| `ATC_MPC_SCE2_PT_MUTotalProducedEnergy_001` | The total produced energy does not move while the monitored unit consumes | Server (MU) | M | ✅ passed |  |
| `ATC_MPC_SCE2_PT_MUTotalProducedEnergy_002` | The total produced energy rises while the monitored unit produces | Server (MU) | M | ✅ passed |  |
| `ATC_MPC_SCE3_PT_MUActiveACCurrent_001` | The monitored unit sends the active AC current on phase A | Server (MU) | M | ✅ passed |  |
| `ATC_MPC_SCE3_PT_MUActiveACCurrent_002` | The monitored unit sends the active AC current on phase B | Server (MU) | M | ✅ passed |  |
| `ATC_MPC_SCE3_PT_MUActiveACCurrent_003` | The monitored unit sends the active AC current on phase C | Server (MU) | M | ✅ passed |  |
| `ATC_MPC_SCE4_PT_MUACVoltage_001` | The monitored unit sends the AC voltage between phase A and neutral | Server (MU) | M | ✅ passed |  |
| `ATC_MPC_SCE4_PT_MUACVoltage_002` | The monitored unit sends the AC voltage between phase B and neutral | Server (MU) | M | ✅ passed |  |
| `ATC_MPC_SCE4_PT_MUACVoltage_003` | The monitored unit sends the AC voltage between phase C and neutral | Server (MU) | M | ✅ passed |  |
| `ATC_MPC_SCE4_PT_MUACVoltage_004` | The monitored unit sends the AC voltage between phase A and phase B | Server (MU) | M | ➖ not applicable | the device does not measure the voltage between two phases (parameter sheet, "Phase-to-phase AC Voltage") |
| `ATC_MPC_SCE4_PT_MUACVoltage_005` | The monitored unit sends the AC voltage between phase B and phase C | Server (MU) | M | ➖ not applicable | the device does not measure the voltage between two phases (parameter sheet, "Phase-to-phase AC Voltage") |
| `ATC_MPC_SCE4_PT_MUACVoltage_006` | The monitored unit sends the AC voltage between phase C and phase A | Server (MU) | M | ➖ not applicable | the device does not measure the voltage between two phases (parameter sheet, "Phase-to-phase AC Voltage") |
| `ATC_MPC_SCE5_PT_MUFrequency_001` | The monitored unit sends the frequency | Server (MU) | M | ✅ passed |  |

### UseCase — MPC MA

| Test case | Title | DUT | M/O | Verdict | Note |
|---|---|---|---|---|---|
| `ATC_MPC_COM_PT_MAPolling_001` | The monitoring appliance polls at the interval it declared | Client (MA) | M | ➖ not applicable | the device does not request or send data at an interval (parameter sheet, "Data transmission") |
| `ATC_MPC_COM_PT_MANotification_001` | The monitoring appliance receives changed data within 120 seconds | Client (MA) | M | ✅ passed |  |
| `ATC_MPC_SCE1_PT_MATotalActivePower_001` | The monitoring appliance receives the momentary power with state "normal" | Client (MA) | M | ✅ passed |  |
| `ATC_MPC_SCE1_NT_MATotalActivePower_002` | The monitoring appliance discards a momentary power which is out of range or erroneous | Client (MA) | M | ✅ passed |  |
| `ATC_MPC_SCE1_PT_MAPhaseActivePower_001` | The monitoring appliance receives the active power on phase A with state "normal" | Client (MA) | M | ✅ passed |  |
| `ATC_MPC_SCE1_NT_MAPhaseActivePower_002` | The monitoring appliance discards an active power on phase A which is out of range or erroneous | Client (MA) | M | ✅ passed |  |
| `ATC_MPC_SCE1_PT_MAPhaseActivePower_003` | The monitoring appliance receives the active power on phase B with state "normal" | Client (MA) | M | ✅ passed |  |
| `ATC_MPC_SCE1_NT_MAPhaseActivePower_004` | The monitoring appliance discards an active power on phase B which is out of range or erroneous | Client (MA) | M | ✅ passed |  |
| `ATC_MPC_SCE1_PT_MAPhaseActivePower_005` | The monitoring appliance receives the active power on phase C with state "normal" | Client (MA) | M | ✅ passed |  |
| `ATC_MPC_SCE1_NT_MAPhaseActivePower_006` | The monitoring appliance discards an active power on phase C which is out of range or erroneous | Client (MA) | M | ✅ passed |  |
| `ATC_MPC_SCE2_PT_MATotalConsumedEnergy_001` | The monitoring appliance receives the total consumed energy with state "normal" | Client (MA) | M | ✅ passed |  |
| `ATC_MPC_SCE2_NT_MATotalConsumedEnergy_002` | The monitoring appliance discards a total consumed energy which is out of range or erroneous | Client (MA) | M | ✅ passed |  |
| `ATC_MPC_SCE2_PT_MATotalProducedEnergy_001` | The monitoring appliance receives the total produced energy with state "normal" | Client (MA) | M | ✅ passed |  |
| `ATC_MPC_SCE2_NT_MATotalProducedEnergy_002` | The monitoring appliance discards a total produced energy which is out of range or erroneous | Client (MA) | M | ✅ passed |  |
| `ATC_MPC_SCE3_PT_MAActiveACCurrent_001` | The monitoring appliance receives the AC current on phase A with state "normal" | Client (MA) | M | ✅ passed |  |
| `ATC_MPC_SCE3_NT_MAActiveACCurrent_002` | The monitoring appliance discards an AC current on phase A which is out of range or erroneous | Client (MA) | M | ✅ passed |  |
| `ATC_MPC_SCE3_PT_MAActiveACCurrent_003` | The monitoring appliance receives the AC current on phase B with state "normal" | Client (MA) | M | ✅ passed |  |
| `ATC_MPC_SCE3_NT_MAActiveACCurrent_004` | The monitoring appliance discards an AC current on phase B which is out of range or erroneous | Client (MA) | M | ✅ passed |  |
| `ATC_MPC_SCE3_PT_MAActiveACCurrent_005` | The monitoring appliance receives the AC current on phase C with state "normal" | Client (MA) | M | ✅ passed |  |
| `ATC_MPC_SCE3_NT_MAActiveACCurrent_006` | The monitoring appliance discards an AC current on phase C which is out of range or erroneous | Client (MA) | M | ✅ passed |  |
| `ATC_MPC_SCE4_PT_MAACVoltage_001` | The monitoring appliance receives the AC voltage between phase A and neutral with state "normal" | Client (MA) | M | ✅ passed |  |
| `ATC_MPC_SCE4_NT_MAACVoltage_002` | The monitoring appliance discards an AC voltage between phase A and neutral which is out of range or erroneous | Client (MA) | M | ✅ passed |  |
| `ATC_MPC_SCE4_PT_MAACVoltage_003` | The monitoring appliance receives the AC voltage between phase B and neutral with state "normal" | Client (MA) | M | ✅ passed |  |
| `ATC_MPC_SCE4_NT_MAACVoltage_004` | The monitoring appliance discards an AC voltage between phase B and neutral which is out of range or erroneous | Client (MA) | M | ✅ passed |  |
| `ATC_MPC_SCE4_PT_MAACVoltage_005` | The monitoring appliance receives the AC voltage between phase C and neutral with state "normal" | Client (MA) | M | ✅ passed |  |
| `ATC_MPC_SCE4_NT_MAACVoltage_006` | The monitoring appliance discards an AC voltage between phase C and neutral which is out of range or erroneous | Client (MA) | M | ✅ passed |  |
| `ATC_MPC_SCE4_PT_MAACVoltage_007` | The monitoring appliance receives the AC voltage between phase A and phase B with state "normal" | Client (MA) | M | ➖ not applicable | the device does not measure the voltage between two phases (parameter sheet, "Phase-to-phase AC Voltage") |
| `ATC_MPC_SCE4_NT_MAACVoltage_008` | The monitoring appliance discards an AC voltage between phase A and phase B which is out of range or erroneous | Client (MA) | M | ➖ not applicable | the device does not measure the voltage between two phases (parameter sheet, "Phase-to-phase AC Voltage") |
| `ATC_MPC_SCE4_PT_MAACVoltage_009` | The monitoring appliance receives the AC voltage between phase B and phase C with state "normal" | Client (MA) | M | ➖ not applicable | the device does not measure the voltage between two phases (parameter sheet, "Phase-to-phase AC Voltage") |
| `ATC_MPC_SCE4_NT_MAACVoltage_010` | The monitoring appliance discards an AC voltage between phase B and phase C which is out of range or erroneous | Client (MA) | M | ➖ not applicable | the device does not measure the voltage between two phases (parameter sheet, "Phase-to-phase AC Voltage") |
| `ATC_MPC_SCE4_PT_MAACVoltage_011` | The monitoring appliance receives the AC voltage between phase C and phase A with state "normal" | Client (MA) | M | ➖ not applicable | the device does not measure the voltage between two phases (parameter sheet, "Phase-to-phase AC Voltage") |
| `ATC_MPC_SCE4_NT_MAACVoltage_012` | The monitoring appliance discards an AC voltage between phase C and phase A which is out of range or erroneous | Client (MA) | M | ➖ not applicable | the device does not measure the voltage between two phases (parameter sheet, "Phase-to-phase AC Voltage") |
| `ATC_MPC_SCE5_PT_MAFrequency_001` | The monitoring appliance receives the frequency with state "normal" | Client (MA) | M | ✅ passed |  |
| `ATC_MPC_SCE5_NT_MAFrequency_002` | The monitoring appliance discards a frequency which is out of range or erroneous | Client (MA) | M | ✅ passed |  |

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

### `ATC_LPC_COM_PT_CSTransition6_001` — Transition 6: an expired duration takes "limited" to "unlimited/controlled"

⚠️ **warning** — passed, with a behaviour the specification tolerates only for now

Verifies `[LPC-TS-001/1]`, `[LPC-TS-008]`, `[LPC-TS-008/1]`, `[LPC-TS-025]`.

| Step | Action | Expected | Result |
|---|---|---|---|
| 1 | Send an EG APCL duration write command. | The CS receives and accepts the write command. The CS changes its configuration to CF_CS_Limited_w_dur. | ✅ passed |
| 2 | Wait for the set duration to expire. | The duration is expired. The CS changes its configuration to CF_CS_UnlCntrl. | ✅ the controllable system is in UnlimitedControlled |
| 3 | Optional test step: check the APCL duration parameter of the CS. | The APCL duration parameter is deleted or has a value of 0 seconds. | ⚠️ the duration reads 60 s; the duration of an expired limit is still set, which rule 008/1 leaves optional |

### `ATC_LPP_COM_PT_CSTransition6_001` — Transition 6: an expired duration takes "limited" to "unlimited/controlled"

⚠️ **warning** — passed, with a behaviour the specification tolerates only for now

Verifies `[LPP-TS-001/1]`, `[LPP-TS-008]`, `[LPP-TS-008/1]`, `[LPP-TS-025]`.

| Step | Action | Expected | Result |
|---|---|---|---|
| 1 | Send an EG APCL duration write command. | The CS receives and accepts the write command. The CS changes its configuration to CF_CS_Limited_w_dur. | ✅ passed |
| 2 | Wait for the set duration to expire. | The duration is expired. The CS changes its configuration to CF_CS_UnlCntrl. | ✅ the controllable system is in UnlimitedControlled |
| 3 | Optional test step: check the APCL duration parameter of the CS. | The APCL duration parameter is deleted or has a value of 0 seconds. | ⚠️ the duration reads 60 s; the duration of an expired limit is still set, which rule 008/1 leaves optional |

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
| `[LPC-TS-001]` | LPC 1.0.0, 2.8.1 | `ATC_LPC_COM_PT_EGMessages_001`, `ATC_LPC_COM_PT_EGMessages_003`, `ATC_LPC_COM_PT_CSConnection_007`, `ATC_LPC_COM_PT_CSConnection_008` | ✅ passed |
| `[LPC-TS-001/1]` | LPC 1.0.0, 2.6.1.1 | `ATC_LPC_COM_PT_CSTransition6_001` | ⚠️ warning |
| `[LPC-TS-001/2]` | LPC 1.0.0, 2.6.1.1 | `ATC_LPC_COM_PT_EGMessages_003`, `ATC_LPC_COM_PT_CSLimited_002` | ✅ passed |
| `[LPC-TS-002]` | LPC 1.0.0, 2.2, 2.6.1.1 | `ATC_LPC_COM_PT_EGMessages_003`, `ATC_LPC_COM_PT_CSLimited_002` | ✅ passed |
| `[LPC-TS-003]` | LPC 1.0.0, 2.6.2.1 | `ATC_LPC_COM_PT_EGMessages_004`, `ATC_LPC_COM_PT_CSConnection_002` | ✅ passed |
| `[LPC-TS-004]` | LPC 1.0.0, 2.2, 2.6.1.1 | `ATC_LPC_COM_NT_CSConnection_001` | ✅ passed |
| `[LPC-TS-005]` | LPC 1.0.0, 2.2, 2.6.2.1 | `ATC_LPC_COM_PT_CSConnection_003`, `ATC_LPC_COM_PT_CSConnection_004` | ✅ passed |
| `[LPC-TS-006]` | LPC 1.0.0, 2.1, 2.6.3.1 | `ATC_LPC_COM_PT_EGHeartbeat_001` | ✅ passed |
| `[LPC-TS-007]` | LPC 1.0.0, 2.1, 2.6.3.1 | `ATC_LPC_COM_PT_CSHeartbeat_001` | ✅ passed |
| `[LPC-TS-008]` | LPC 1.0.0, 2.6.1.1 | `ATC_LPC_COM_PT_CSTransition6_001` | ⚠️ warning |
| `[LPC-TS-008/1]` | LPC 1.0.0, 2.6.1.1 | `ATC_LPC_COM_PT_CSTransition6_001` | ⚠️ warning |
| `[LPC-TS-009]` | LPC 1.0.0, 2.6.1.1 | `ATC_LPC_COM_NT_CSUnlCntrl_001`, `ATC_LPC_COM_PT_CSFS_003` | ✅ passed |
| `[LPC-TS-009/1]` | LPC 1.0.0, 2.3.2 | `ATC_LPC_COM_NT_CSLimited_001` | ✅ passed |
| `[LPC-TS-009/2]` | LPC 1.0.0, 2.3.2 | `ATC_LPC_COM_PT_CSInit_002` | ✅ passed |
| `[LPC-TS-009/3]` | LPC 1.0.0, 2.3.2 | `ATC_LPC_COM_PT_CSInit_001`, `ATC_LPC_COM_PT_CSInit_002`, `ATC_LPC_COM_NT_CSUnlCntrl_001`, `ATC_LPC_COM_PT_CSFS_003`, `ATC_LPC_COM_PT_CSUnlAuto_002` | ✅ passed |
| `[LPC-TS-010]` | LPC 1.0.0, 2.2 | `ATC_LPC_COM_PT_CSUnlAuto_002` | ➖ not applicable |
| `[LPC-TS-010/1]` | LPC 1.0.0, 2.2 | `ATC_LPC_COM_PT_CSUnlCntrl_003` | ✅ passed |
| `[LPC-TS-010/2]` | LPC 1.0.0, 2.6.4.1 | `ATC_LPC_COM_PT_CSUnlCntrl_003` | ✅ passed |
| `[LPC-TS-010/3]` | LPC 1.0.0, 2.2 | `ATC_LPC_COM_PT_CSUnlCntrl_002` | ➖ not applicable |
| `[LPC-TS-010/4]` | LPC 1.0.0, 2.6.4.1 | `ATC_LPC_COM_PT_CSUnlCntrl_002` | ➖ not applicable |
| `[LPC-TS-011]` | LPC 1.0.0, 2.2, 2.6.2.1 | `ATC_LPC_COM_PT_CSInit_001`, `ATC_LPC_COM_PT_CSInit_002` | ✅ passed |
| `[LPC-TS-011/1]` | LPC 1.0.0, 2.6.2.1 | `ATC_LPC_COM_PT_EGMessages_004`, `ATC_LPC_COM_PT_CSInit_003` | ✅ passed |
| `[LPC-TS-012]` | LPC 1.0.0, 2.1 | `ATC_LPC_COM_PT_CSFS_002`, `ATC_LPC_COM_PT_CSTransition10_001` | ✅ passed |
| `[LPC-TS-013]` | LPC 1.0.0, 2.6.2.1 | `ATC_LPC_COM_PT_CSInit_002`, `ATC_LPC_COM_PT_CSFS_002` | ✅ passed |
| `[LPC-TS-013/1]` | LPC 1.0.0, 2.6.2.1 | `ATC_LPC_COM_PT_EGMessages_004`, `ATC_LPC_COM_PT_CSInit_003` | ✅ passed |
| `[LPC-TS-014]` | LPC 1.0.0, 2.6.2.1 | `ATC_LPC_COM_PT_CSConnection_005` | ✅ passed |
| `[LPC-TS-015]` | LPC 1.0.0, 2.6.2.1 | `ATC_LPC_COM_PT_CSConnection_005` | ✅ passed |
| `[LPC-TS-015/1]` | LPC 1.0.0, 2.6.2.1 | `ATC_LPC_COM_PT_CSConnection_005`, `ATC_LPC_COM_PT_CSConnection_008` | ✅ passed |
| `[LPC-TS-016]` | LPC 1.0.0, 2.6.2.1 | `ATC_LPC_COM_PT_CSConnection_005`, `ATC_LPC_COM_PT_CSConnection_008` | ✅ passed |
| `[LPC-TS-017]` | LPC 1.0.0, 2.2, 2.3.2 | `ATC_LPC_COM_PT_CSInit_001` | ✅ passed |
| `[LPC-TS-018]` | LPC 1.0.0, 2.2, 2.3.3 | `ATC_LPC_COM_PT_CSConnection_003`, `ATC_LPC_COM_PT_CSTransition1_001` | ✅ passed |
| `[LPC-TS-019]` | LPC 1.0.0, 2.2, 2.3.3 | `ATC_LPC_COM_PT_CSInit_001` | ✅ passed |
| `[LPC-TS-020]` | LPC 1.0.0, 2.2, 2.3.3 | `ATC_LPC_COM_PT_CSTransition2_001` | ✅ passed |
| `[LPC-TS-021]` | LPC 1.0.0, 2.2, 2.3.3 | `ATC_LPC_COM_PT_CSTransition1_002` | ✅ passed |
| `[LPC-TS-022]` | LPC 1.0.0, 2.2, 2.3.3 | `ATC_LPC_COM_PT_CSTransition3_001`, `ATC_LPC_COM_PT_CSTransition3_002`, `ATC_LPC_COM_PT_CSTransition10_001`, `ATC_LPC_COM_PT_CSTransition10_002` | ✅ passed |
| `[LPC-TS-022/1]` | LPC 1.0.0, 2.2, 2.3.3 | `ATC_LPC_COM_PT_CSTransition3_001`, `ATC_LPC_COM_PT_CSTransition3_002` | ✅ passed |
| `[LPC-TS-022/2]` | LPC 1.0.0, 2.2, 2.3.3 | `ATC_LPC_COM_PT_CSTransition10_002` | ✅ passed |
| `[LPC-TS-022/3]` | LPC 1.0.0, 2.2, 2.3.3 | `ATC_LPC_COM_PT_CSTransition10_001` | ✅ passed |
| `[LPC-TS-023]` | LPC 1.0.0, 2.2 | `ATC_LPC_COM_NT_CSUnlCntrl_001` | ✅ passed |
| `[LPC-TS-024]` | LPC 1.0.0, 2.2 | `ATC_LPC_COM_NT_CSLimited_001` | ✅ passed |
| `[LPC-TS-025]` | LPC 1.0.0, 2.2, 2.3.3 | `ATC_LPC_COM_PT_CSTransition6_001` | ⚠️ warning |
| `[LPC-TS-026]` | LPC 1.0.0, 2.2, 2.3.3 | `ATC_LPC_COM_PT_CSTransition6_002` | ✅ passed |
| `[LPC-TS-027]` | LPC 1.0.0, 2.2, 2.3.3 | `ATC_LPC_COM_PT_CSTransition4_001` | ✅ passed |
| `[LPC-TS-028]` | LPC 1.0.0, 2.2, 2.3.3 | `ATC_LPC_COM_PT_CSTransition5_001` | ✅ passed |
| `[LPC-TS-029]` | LPC 1.0.0, 2.2, 2.3.3 | `ATC_LPC_COM_PT_CSTransition7_001` | ✅ passed |
| `[LPC-TS-030]` | LPC 1.0.0, 2.2 | `ATC_LPC_COM_PT_EGConnection_001`, `ATC_LPC_COM_PT_EGConnection_002`, `ATC_LPC_COM_PT_EGConnection_003` | ✅ passed |
| `[LPC-TS-031]` | LPC 1.0.0, 2.2, 2.3.3 | `ATC_LPC_COM_PT_CSTransition8_001`, `ATC_LPC_COM_PT_CSTransition11_001` | ✅ passed |
| `[LPC-TS-032]` | LPC 1.0.0, 2.2, 2.3.3 | `ATC_LPC_COM_PT_CSTransition9_001`, `ATC_LPC_COM_PT_CSTransition12_001` | ✅ passed |
| `[LPC-TS-033]` | LPC 1.0.0, 2.2, 2.3.3 | `ATC_LPC_COM_PT_CSFS_001`, `ATC_LPC_COM_NT_CSUnlAuto_001`, `ATC_LPC_COM_PT_CSTransition8_002`, `ATC_LPC_COM_PT_CSTransition11_002` | ✅ passed |
| `[LPC-TS-035]` | LPC 1.0.0, 2.2 | `ATC_LPC_COM_PT_CSConnection_007`, `ATC_LPC_INS1_PT_CSTransition1_001`, `ATC_LPC_INS2_PT_CSTransition1_001` | ✅ passed |
| `[LPC-TS-035/1]` | LPC 1.0.0, 2.2 | `ATC_LPC_COM_NT_CSLimited_001`, `ATC_LPC_COM_PT_CSTransition1_001`, `ATC_LPC_COM_PT_CSTransition8_001`, `ATC_LPC_COM_PT_CSTransition11_001` | ✅ passed |
| `[LPC-TS-035/2]` | LPC 1.0.0, 2.2 | `ATC_LPC_INS1_PT_CSTransition1_001` | ➖ not applicable |
| `[LPC-TS-035/3]` | LPC 1.0.0, 2.2 | `ATC_LPC_INS2_PT_CSTransition1_001` | ✅ passed |
| `[LPC-TS-035/4]` | LPC 1.0.0, 2.2 | `ATC_LPC_COM_PT_CSConnection_006`, `ATC_LPC_COM_PT_CSConnection_007` | ✅ passed |
| `[LPC-TS-036]` | LPC 1.0.0, 2.2 | `ATC_LPC_COM_NT_CSConnection_001`, `ATC_LPC_COM_PT_CSConnection_002`, `ATC_LPC_COM_PT_CSFS_001`, `ATC_LPC_COM_NT_CSUnlAuto_001` | ✅ passed |
| `[LPC-TS-037]` | LPC 1.0.0, 2.2 | `ATC_LPC_COM_PT_CSConnection_002`, `ATC_LPC_COM_PT_CSConnection_004`, `ATC_LPC_COM_PT_CSFS_001`, `ATC_LPC_COM_NT_CSUnlAuto_001` | ✅ passed |
| `[LPC-TS-038]` | LPC 1.0.0, 2.8.1 | `ATC_LPC_COM_PT_CSConnection_002`, `ATC_LPC_COM_PT_CSConnection_003`, `ATC_LPC_COM_PT_CSConnection_008`, `ATC_LPC_COM_PT_CSUnlCntrl_002`, `ATC_LPC_COM_PT_CSUnlCntrl_003`, `ATC_LPC_COM_PT_CSUnlAuto_002` | ✅ passed |
| `[LPC-TS-039]` | LPC 1.0.0, 2.6.4.1 | `ATC_LPC_COM_PT_CSUnlCntrl_002` | ➖ not applicable |
| `[LPC-TS-040]` | LPC 1.0.0, 2.6.4.1 | `ATC_LPC_COM_PT_CSUnlCntrl_003` | ✅ passed |
| `[LPC-TS-044]` | LPC 1.0.0, 2.6.2.1 | `ATC_LPC_COM_PT_CSInit_003` | ✅ passed |
| `[LPC-TS-046]` | LPC 1.0.0, 2.2 | `ATC_LPC_COM_PT_EGMessages_002`, `ATC_LPC_COM_PT_CSConnection_009` | ✅ passed |
| `[LPP-TS-001]` | LPP 1.0.0, 2.8.1 | `ATC_LPP_COM_PT_EGMessages_001`, `ATC_LPP_COM_PT_EGMessages_003`, `ATC_LPP_COM_PT_CSConnection_007`, `ATC_LPP_COM_PT_CSConnection_008` | ✅ passed |
| `[LPP-TS-001/1]` | LPP 1.0.0, 2.6.1.1 | `ATC_LPP_COM_PT_CSTransition6_001` | ⚠️ warning |
| `[LPP-TS-001/2]` | LPP 1.0.0, 2.6.1.1 | `ATC_LPP_COM_PT_EGMessages_003`, `ATC_LPP_COM_PT_CSLimited_002` | ✅ passed |
| `[LPP-TS-002]` | LPP 1.0.0, 2.2, 2.6.1.1 | `ATC_LPP_COM_PT_EGMessages_003`, `ATC_LPP_COM_PT_CSLimited_002` | ✅ passed |
| `[LPP-TS-003]` | LPP 1.0.0, 2.6.2.1 | `ATC_LPP_COM_PT_EGMessages_004`, `ATC_LPP_COM_PT_CSConnection_002` | ✅ passed |
| `[LPP-TS-004]` | LPP 1.0.0, 2.2, 2.6.1.1 | `ATC_LPP_COM_NT_CSConnection_001` | ✅ passed |
| `[LPP-TS-005]` | LPP 1.0.0, 2.2, 2.6.2.1 | `ATC_LPP_COM_PT_CSConnection_003`, `ATC_LPP_COM_PT_CSConnection_004` | ✅ passed |
| `[LPP-TS-006]` | LPP 1.0.0, 2.1, 2.6.3.1 | `ATC_LPP_COM_PT_EGHeartbeat_001` | ✅ passed |
| `[LPP-TS-007]` | LPP 1.0.0, 2.1, 2.6.3.1 | `ATC_LPP_COM_PT_CSHeartbeat_001` | ✅ passed |
| `[LPP-TS-008]` | LPP 1.0.0, 2.6.1.1 | `ATC_LPP_COM_PT_CSTransition6_001` | ⚠️ warning |
| `[LPP-TS-008/1]` | LPP 1.0.0, 2.6.1.1 | `ATC_LPP_COM_PT_CSTransition6_001` | ⚠️ warning |
| `[LPP-TS-009]` | LPP 1.0.0, 2.6.1.1 | `ATC_LPP_COM_NT_CSUnlCntrl_001`, `ATC_LPP_COM_PT_CSFS_003` | ✅ passed |
| `[LPP-TS-009/1]` | LPP 1.0.0, 2.3.2 | `ATC_LPP_COM_NT_CSLimited_001` | ✅ passed |
| `[LPP-TS-009/2]` | LPP 1.0.0, 2.3.2 | `ATC_LPP_COM_PT_CSInit_002` | ✅ passed |
| `[LPP-TS-009/3]` | LPP 1.0.0, 2.3.2 | `ATC_LPP_COM_PT_CSInit_001`, `ATC_LPP_COM_PT_CSInit_002`, `ATC_LPP_COM_NT_CSUnlCntrl_001`, `ATC_LPP_COM_PT_CSFS_003`, `ATC_LPP_COM_PT_CSUnlAuto_002` | ✅ passed |
| `[LPP-TS-010]` | LPP 1.0.0, 2.2 | `ATC_LPP_COM_PT_CSUnlAuto_002` | ➖ not applicable |
| `[LPP-TS-010/1]` | LPP 1.0.0, 2.2 | `ATC_LPP_COM_PT_CSUnlCntrl_003` | ✅ passed |
| `[LPP-TS-010/2]` | LPP 1.0.0, 2.6.4.1 | `ATC_LPP_COM_PT_CSUnlCntrl_003` | ✅ passed |
| `[LPP-TS-010/3]` | LPP 1.0.0, 2.2 | `ATC_LPP_COM_PT_CSUnlCntrl_002` | ➖ not applicable |
| `[LPP-TS-010/4]` | LPP 1.0.0, 2.6.4.1 | `ATC_LPP_COM_PT_CSUnlCntrl_002` | ➖ not applicable |
| `[LPP-TS-011]` | LPP 1.0.0, 2.2, 2.6.2.1 | `ATC_LPP_COM_PT_CSInit_001`, `ATC_LPP_COM_PT_CSInit_002` | ✅ passed |
| `[LPP-TS-011/1]` | LPP 1.0.0, 2.6.2.1 | `ATC_LPP_COM_PT_EGMessages_004`, `ATC_LPP_COM_PT_CSInit_003` | ✅ passed |
| `[LPP-TS-012]` | LPP 1.0.0, 2.1 | `ATC_LPP_COM_PT_CSFS_002`, `ATC_LPP_COM_PT_CSTransition10_001` | ✅ passed |
| `[LPP-TS-013]` | LPP 1.0.0, 2.6.2.1 | `ATC_LPP_COM_PT_CSInit_002`, `ATC_LPP_COM_PT_CSFS_002` | ✅ passed |
| `[LPP-TS-013/1]` | LPP 1.0.0, 2.6.2.1 | `ATC_LPP_COM_PT_EGMessages_004`, `ATC_LPP_COM_PT_CSInit_003` | ✅ passed |
| `[LPP-TS-014]` | LPP 1.0.0, 2.6.2.1 | `ATC_LPP_COM_PT_CSConnection_005` | ✅ passed |
| `[LPP-TS-015]` | LPP 1.0.0, 2.6.2.1 | `ATC_LPP_COM_PT_CSConnection_005` | ✅ passed |
| `[LPP-TS-015/1]` | LPP 1.0.0, 2.6.2.1 | `ATC_LPP_COM_PT_CSConnection_005`, `ATC_LPP_COM_PT_CSConnection_008` | ✅ passed |
| `[LPP-TS-016]` | LPP 1.0.0, 2.6.2.1 | `ATC_LPP_COM_PT_CSConnection_005`, `ATC_LPP_COM_PT_CSConnection_008` | ✅ passed |
| `[LPP-TS-017]` | LPP 1.0.0, 2.2, 2.3.2 | `ATC_LPP_COM_PT_CSInit_001` | ✅ passed |
| `[LPP-TS-018]` | LPP 1.0.0, 2.2, 2.3.3 | `ATC_LPP_COM_PT_CSConnection_003`, `ATC_LPP_COM_PT_CSTransition1_001` | ✅ passed |
| `[LPP-TS-019]` | LPP 1.0.0, 2.2, 2.3.3 | `ATC_LPP_COM_PT_CSInit_001` | ✅ passed |
| `[LPP-TS-020]` | LPP 1.0.0, 2.2, 2.3.3 | `ATC_LPP_COM_PT_CSTransition2_001` | ✅ passed |
| `[LPP-TS-021]` | LPP 1.0.0, 2.2, 2.3.3 | `ATC_LPP_COM_PT_CSTransition1_002` | ✅ passed |
| `[LPP-TS-022]` | LPP 1.0.0, 2.2, 2.3.3 | `ATC_LPP_COM_PT_CSTransition3_001`, `ATC_LPP_COM_PT_CSTransition3_002`, `ATC_LPP_COM_PT_CSTransition10_001`, `ATC_LPP_COM_PT_CSTransition10_002` | ✅ passed |
| `[LPP-TS-022/1]` | LPP 1.0.0, 2.2, 2.3.3 | `ATC_LPP_COM_PT_CSTransition3_001`, `ATC_LPP_COM_PT_CSTransition3_002` | ✅ passed |
| `[LPP-TS-022/2]` | LPP 1.0.0, 2.2, 2.3.3 | `ATC_LPP_COM_PT_CSTransition10_002` | ✅ passed |
| `[LPP-TS-022/3]` | LPP 1.0.0, 2.2, 2.3.3 | `ATC_LPP_COM_PT_CSTransition10_001` | ✅ passed |
| `[LPP-TS-023]` | LPP 1.0.0, 2.2 | `ATC_LPP_COM_NT_CSUnlCntrl_001` | ✅ passed |
| `[LPP-TS-024]` | LPP 1.0.0, 2.2 | `ATC_LPP_COM_NT_CSLimited_001` | ✅ passed |
| `[LPP-TS-025]` | LPP 1.0.0, 2.2, 2.3.3 | `ATC_LPP_COM_PT_CSTransition6_001` | ⚠️ warning |
| `[LPP-TS-026]` | LPP 1.0.0, 2.2, 2.3.3 | `ATC_LPP_COM_PT_CSTransition6_002` | ✅ passed |
| `[LPP-TS-027]` | LPP 1.0.0, 2.2, 2.3.3 | `ATC_LPP_COM_PT_CSTransition4_001` | ✅ passed |
| `[LPP-TS-028]` | LPP 1.0.0, 2.2, 2.3.3 | `ATC_LPP_COM_PT_CSTransition5_001` | ✅ passed |
| `[LPP-TS-029]` | LPP 1.0.0, 2.2, 2.3.3 | `ATC_LPP_COM_PT_CSTransition7_001` | ✅ passed |
| `[LPP-TS-030]` | LPP 1.0.0, 2.2 | `ATC_LPP_COM_PT_EGConnection_001`, `ATC_LPP_COM_PT_EGConnection_002`, `ATC_LPP_COM_PT_EGConnection_003` | ✅ passed |
| `[LPP-TS-031]` | LPP 1.0.0, 2.2, 2.3.3 | `ATC_LPP_COM_PT_CSTransition8_001`, `ATC_LPP_COM_PT_CSTransition11_001` | ✅ passed |
| `[LPP-TS-032]` | LPP 1.0.0, 2.2, 2.3.3 | `ATC_LPP_COM_PT_CSTransition9_001`, `ATC_LPP_COM_PT_CSTransition12_001` | ✅ passed |
| `[LPP-TS-033]` | LPP 1.0.0, 2.2, 2.3.3 | `ATC_LPP_COM_PT_CSFS_001`, `ATC_LPP_COM_NT_CSUnlAuto_001`, `ATC_LPP_COM_PT_CSTransition8_002`, `ATC_LPP_COM_PT_CSTransition11_002` | ✅ passed |
| `[LPP-TS-035]` | LPP 1.0.0, 2.2 | `ATC_LPP_COM_PT_CSConnection_007`, `ATC_LPP_INS1_PT_CSTransition1_001`, `ATC_LPP_INS2_PT_CSTransition1_001` | ✅ passed |
| `[LPP-TS-035/1]` | LPP 1.0.0, 2.2 | `ATC_LPP_COM_NT_CSLimited_001`, `ATC_LPP_COM_PT_CSTransition1_001`, `ATC_LPP_COM_PT_CSTransition8_001`, `ATC_LPP_COM_PT_CSTransition11_001` | ✅ passed |
| `[LPP-TS-035/2]` | LPP 1.0.0, 2.2 | `ATC_LPP_INS1_PT_CSTransition1_001` | ➖ not applicable |
| `[LPP-TS-035/3]` | LPP 1.0.0, 2.2 | `ATC_LPP_INS2_PT_CSTransition1_001` | ✅ passed |
| `[LPP-TS-035/4]` | LPP 1.0.0, 2.2 | `ATC_LPP_COM_PT_CSConnection_006`, `ATC_LPP_COM_PT_CSConnection_007` | ✅ passed |
| `[LPP-TS-036]` | LPP 1.0.0, 2.2 | `ATC_LPP_COM_NT_CSConnection_001`, `ATC_LPP_COM_PT_CSConnection_002`, `ATC_LPP_COM_PT_CSFS_001`, `ATC_LPP_COM_NT_CSUnlAuto_001` | ✅ passed |
| `[LPP-TS-037]` | LPP 1.0.0, 2.2 | `ATC_LPP_COM_PT_CSConnection_002`, `ATC_LPP_COM_PT_CSConnection_004`, `ATC_LPP_COM_PT_CSFS_001`, `ATC_LPP_COM_NT_CSUnlAuto_001` | ✅ passed |
| `[LPP-TS-038]` | LPP 1.0.0, 2.8.1 | `ATC_LPP_COM_PT_CSConnection_002`, `ATC_LPP_COM_PT_CSConnection_003`, `ATC_LPP_COM_PT_CSConnection_008`, `ATC_LPP_COM_PT_CSUnlCntrl_002`, `ATC_LPP_COM_PT_CSUnlCntrl_003`, `ATC_LPP_COM_PT_CSUnlAuto_002` | ✅ passed |
| `[LPP-TS-039]` | LPP 1.0.0, 2.6.4.1 | `ATC_LPP_COM_PT_CSUnlCntrl_002` | ➖ not applicable |
| `[LPP-TS-040]` | LPP 1.0.0, 2.6.4.1 | `ATC_LPP_COM_PT_CSUnlCntrl_003` | ✅ passed |
| `[LPP-TS-044]` | LPP 1.0.0, 2.6.2.1 | `ATC_LPP_COM_PT_CSInit_003` | ✅ passed |
| `[LPP-TS-046]` | LPP 1.0.0, 2.2 | `ATC_LPP_COM_PT_EGMessages_002`, `ATC_LPP_COM_PT_CSConnection_009` | ✅ passed |
| `[MGCP-TS-001]` | MGCP 1.0.0, 2.4, 2.4.1.1 | `ATC_MGCP_SCE1_PT_GCPPowerLimitFactor_001`, `ATC_MGCP_SCE1_PT_MAPowerLimitFactor_001` | ✅ passed |
| `[MGCP-TS-002]` | MGCP 1.0.0, 2.4, 2.4.2.1 | `ATC_MGCP_SCE2_PT_GCPTotalActivePower_001`, `ATC_MGCP_SCE2_PT_MATotalActivePower_001` | ✅ passed |
| `[MGCP-TS-003]` | MGCP 1.0.0, 2.4, 2.4.3.1 | `ATC_MGCP_SCE3_PT_GCPTotalFeedInEnergy_001`, `ATC_MGCP_SCE3_PT_GCPTotalFeedInEnergy_002`, `ATC_MGCP_SCE3_PT_MATotalFeedInEnergy_001` | ✅ passed |
| `[MGCP-TS-004]` | MGCP 1.0.0, 2.4, 2.4.4.1 | `ATC_MGCP_SCE4_PT_GCPTotalConsumedEnergy_001`, `ATC_MGCP_SCE4_PT_GCPTotalConsumedEnergy_002`, `ATC_MGCP_SCE4_PT_MATotalConsumedEnergy_001` | ✅ passed |
| `[MGCP-TS-005]` | MGCP 1.0.0, 2.4 | `ATC_MGCP_SCE5_PT_GCPActiveACCurrent_001`, `ATC_MGCP_SCE5_PT_GCPActiveACCurrent_002`, `ATC_MGCP_SCE5_PT_GCPActiveACCurrent_003`, `ATC_MGCP_SCE5_PT_MAActiveACCurrent_001`, `ATC_MGCP_SCE5_PT_MAActiveACCurrent_003`, `ATC_MGCP_SCE5_PT_MAActiveACCurrent_005` | ✅ passed |
| `[MGCP-TS-005/1]` | MGCP 1.0.0, 2.4.5.1 | `ATC_MGCP_SCE5_PT_GCPActiveACCurrent_001`, `ATC_MGCP_SCE5_PT_MAActiveACCurrent_001` | ✅ passed |
| `[MGCP-TS-005/2]` | MGCP 1.0.0, 2.4.5.1 | `ATC_MGCP_SCE5_PT_GCPActiveACCurrent_002`, `ATC_MGCP_SCE5_PT_MAActiveACCurrent_003` | ✅ passed |
| `[MGCP-TS-005/3]` | MGCP 1.0.0, 2.4.5.1 | `ATC_MGCP_SCE5_PT_GCPActiveACCurrent_003`, `ATC_MGCP_SCE5_PT_MAActiveACCurrent_005` | ✅ passed |
| `[MGCP-TS-005/4]` | MGCP 1.0.0, 2.4.5.1 | `ATC_MGCP_SCE5_PT_GCPActiveACCurrent_001`, `ATC_MGCP_SCE5_PT_GCPActiveACCurrent_002`, `ATC_MGCP_SCE5_PT_GCPActiveACCurrent_003`, `ATC_MGCP_SCE5_PT_MAActiveACCurrent_001`, `ATC_MGCP_SCE5_PT_MAActiveACCurrent_003`, `ATC_MGCP_SCE5_PT_MAActiveACCurrent_005` | ✅ passed |
| `[MGCP-TS-006]` | MGCP 1.0.0, 2.4, 2.4.6.1 | `ATC_MGCP_SCE6_PT_GCPACVoltage_001`, `ATC_MGCP_SCE6_PT_GCPACVoltage_002`, `ATC_MGCP_SCE6_PT_GCPACVoltage_003`, `ATC_MGCP_SCE6_PT_GCPACVoltage_004`, `ATC_MGCP_SCE6_PT_GCPACVoltage_005`, `ATC_MGCP_SCE6_PT_GCPACVoltage_006`, `ATC_MGCP_SCE6_PT_MAACVoltage_001`, `ATC_MGCP_SCE6_PT_MAACVoltage_003`, `ATC_MGCP_SCE6_PT_MAACVoltage_005`, `ATC_MGCP_SCE6_PT_MAACVoltage_007`, `ATC_MGCP_SCE6_PT_MAACVoltage_009`, `ATC_MGCP_SCE6_PT_MAACVoltage_011` | ✅ passed |
| `[MGCP-TS-006/1]` | MGCP 1.0.0, 2.4.6.1 | `ATC_MGCP_SCE6_PT_GCPACVoltage_001`, `ATC_MGCP_SCE6_PT_MAACVoltage_001` | ✅ passed |
| `[MGCP-TS-006/2]` | MGCP 1.0.0, 2.4.6.1 | `ATC_MGCP_SCE6_PT_GCPACVoltage_002`, `ATC_MGCP_SCE6_PT_MAACVoltage_003` | ✅ passed |
| `[MGCP-TS-006/3]` | MGCP 1.0.0, 2.4.6.1 | `ATC_MGCP_SCE6_PT_GCPACVoltage_003`, `ATC_MGCP_SCE6_PT_MAACVoltage_005` | ✅ passed |
| `[MGCP-TS-006/4]` | MGCP 1.0.0, 2.4.6.1 | `ATC_MGCP_SCE6_PT_GCPACVoltage_004`, `ATC_MGCP_SCE6_PT_MAACVoltage_007` | ➖ not applicable |
| `[MGCP-TS-006/5]` | MGCP 1.0.0, 2.4.6.1 | `ATC_MGCP_SCE6_PT_GCPACVoltage_005`, `ATC_MGCP_SCE6_PT_MAACVoltage_009` | ➖ not applicable |
| `[MGCP-TS-006/6]` | MGCP 1.0.0, 2.4.6.1 | `ATC_MGCP_SCE6_PT_GCPACVoltage_006`, `ATC_MGCP_SCE6_PT_MAACVoltage_011` | ➖ not applicable |
| `[MGCP-TS-006/7]` | MGCP 1.0.0, 2.4.6.1 | `ATC_MGCP_SCE6_PT_GCPACVoltage_001`, `ATC_MGCP_SCE6_PT_GCPACVoltage_002`, `ATC_MGCP_SCE6_PT_GCPACVoltage_003`, `ATC_MGCP_SCE6_PT_GCPACVoltage_004`, `ATC_MGCP_SCE6_PT_GCPACVoltage_005`, `ATC_MGCP_SCE6_PT_GCPACVoltage_006`, `ATC_MGCP_SCE6_PT_MAACVoltage_001`, `ATC_MGCP_SCE6_PT_MAACVoltage_003`, `ATC_MGCP_SCE6_PT_MAACVoltage_005`, `ATC_MGCP_SCE6_PT_MAACVoltage_007`, `ATC_MGCP_SCE6_PT_MAACVoltage_009`, `ATC_MGCP_SCE6_PT_MAACVoltage_011` | ✅ passed |
| `[MGCP-TS-007]` | MGCP 1.0.0, 2.4, 2.4.7.1 | `ATC_MGCP_SCE7_PT_GCPFrequency_001`, `ATC_MGCP_SCE7_PT_MAFrequency_001` | ✅ passed |
| `[MGCP-TS-008]` | MGCP 1.0.0, 2.6.2 | `ATC_MGCP_SCE2_NT_MATotalActivePower_002`, `ATC_MGCP_SCE3_NT_MATotalFeedInEnergy_002`, `ATC_MGCP_SCE4_NT_MATotalConsumedEnergy_002`, `ATC_MGCP_SCE5_NT_MAActiveACCurrent_002`, `ATC_MGCP_SCE5_NT_MAActiveACCurrent_004`, `ATC_MGCP_SCE5_NT_MAActiveACCurrent_006`, `ATC_MGCP_SCE6_NT_MAACVoltage_002`, `ATC_MGCP_SCE6_NT_MAACVoltage_004`, `ATC_MGCP_SCE6_NT_MAACVoltage_006`, `ATC_MGCP_SCE6_NT_MAACVoltage_008`, `ATC_MGCP_SCE6_NT_MAACVoltage_010`, `ATC_MGCP_SCE6_NT_MAACVoltage_012`, `ATC_MGCP_SCE7_NT_MAFrequency_002` | ✅ passed |
| `[MGCP-TS-008/1]` | MGCP 1.0.0, 2.6.2 | `ATC_MGCP_SCE2_NT_MATotalActivePower_002`, `ATC_MGCP_SCE3_NT_MATotalFeedInEnergy_002`, `ATC_MGCP_SCE4_NT_MATotalConsumedEnergy_002`, `ATC_MGCP_SCE5_NT_MAActiveACCurrent_002`, `ATC_MGCP_SCE5_NT_MAActiveACCurrent_004`, `ATC_MGCP_SCE5_NT_MAActiveACCurrent_006`, `ATC_MGCP_SCE6_NT_MAACVoltage_002`, `ATC_MGCP_SCE6_NT_MAACVoltage_004`, `ATC_MGCP_SCE6_NT_MAACVoltage_006`, `ATC_MGCP_SCE6_NT_MAACVoltage_008`, `ATC_MGCP_SCE6_NT_MAACVoltage_010`, `ATC_MGCP_SCE6_NT_MAACVoltage_012`, `ATC_MGCP_SCE7_NT_MAFrequency_002` | ✅ passed |
| `[MGCP-TS-008/2]` | MGCP 1.0.0, 2.6.2 | `ATC_MGCP_SCE2_NT_MATotalActivePower_002`, `ATC_MGCP_SCE3_NT_MATotalFeedInEnergy_002`, `ATC_MGCP_SCE4_NT_MATotalConsumedEnergy_002`, `ATC_MGCP_SCE5_NT_MAActiveACCurrent_002`, `ATC_MGCP_SCE5_NT_MAActiveACCurrent_004`, `ATC_MGCP_SCE5_NT_MAActiveACCurrent_006`, `ATC_MGCP_SCE6_NT_MAACVoltage_002`, `ATC_MGCP_SCE6_NT_MAACVoltage_004`, `ATC_MGCP_SCE6_NT_MAACVoltage_006`, `ATC_MGCP_SCE6_NT_MAACVoltage_008`, `ATC_MGCP_SCE6_NT_MAACVoltage_010`, `ATC_MGCP_SCE6_NT_MAACVoltage_012`, `ATC_MGCP_SCE7_NT_MAFrequency_002` | ✅ passed |
| `[MGCP-TS-010]` | MGCP 1.0.0, 2.6.1 | `ATC_MGCP_SCE2_PT_GCPTotalActivePower_001`, `ATC_MGCP_SCE3_PT_GCPTotalFeedInEnergy_001`, `ATC_MGCP_SCE3_PT_GCPTotalFeedInEnergy_002`, `ATC_MGCP_SCE4_PT_GCPTotalConsumedEnergy_001`, `ATC_MGCP_SCE4_PT_GCPTotalConsumedEnergy_002`, `ATC_MGCP_SCE5_PT_GCPActiveACCurrent_001`, `ATC_MGCP_SCE5_PT_GCPActiveACCurrent_002`, `ATC_MGCP_SCE5_PT_GCPActiveACCurrent_003`, `ATC_MGCP_SCE2_PT_MATotalActivePower_001`, `ATC_MGCP_SCE3_PT_MATotalFeedInEnergy_001`, `ATC_MGCP_SCE4_PT_MATotalConsumedEnergy_001`, `ATC_MGCP_SCE5_PT_MAActiveACCurrent_001`, `ATC_MGCP_SCE5_PT_MAActiveACCurrent_003`, `ATC_MGCP_SCE5_PT_MAActiveACCurrent_005` | ✅ passed |
| `[MGCP-TS-011]` | MGCP 1.0.0, 2.6.1 | `ATC_MGCP_SCE6_PT_GCPACVoltage_001`, `ATC_MGCP_SCE6_PT_GCPACVoltage_002`, `ATC_MGCP_SCE6_PT_GCPACVoltage_003`, `ATC_MGCP_SCE6_PT_GCPACVoltage_004`, `ATC_MGCP_SCE6_PT_GCPACVoltage_005`, `ATC_MGCP_SCE6_PT_GCPACVoltage_006`, `ATC_MGCP_SCE6_PT_MAACVoltage_001`, `ATC_MGCP_SCE6_PT_MAACVoltage_003`, `ATC_MGCP_SCE6_PT_MAACVoltage_005`, `ATC_MGCP_SCE6_PT_MAACVoltage_007`, `ATC_MGCP_SCE6_PT_MAACVoltage_009`, `ATC_MGCP_SCE6_PT_MAACVoltage_011` | ✅ passed |
| `[MGCP-TS-012]` | MGCP 1.0.0, 5.3 | `ATC_MGCP_COM_PT_GCPPolling_001`, `ATC_MGCP_SCE3_PT_GCPTotalFeedInEnergy_001`, `ATC_MGCP_SCE4_PT_GCPTotalConsumedEnergy_002`, `ATC_MGCP_COM_PT_MAPolling_001` | ✅ passed |
| `[MGCP-TS-013]` | MGCP 1.0.0, 5.3 | `ATC_MGCP_COM_PT_GCPNotification_001`, `ATC_MGCP_COM_PT_MANotification_001` | ✅ passed |
| `[MPC-TS-001]` | MPC 1.0.0, 2.3, 2.3.1.1 | `ATC_MPC_SCE1_PT_MUTotalActivePower_001`, `ATC_MPC_SCE1_PT_MATotalActivePower_001` | ✅ passed |
| `[MPC-TS-002]` | MPC 1.0.0, 2.3.1.1 | `ATC_MPC_SCE1_PT_MUPhaseActivePower_001`, `ATC_MPC_SCE1_PT_MUPhaseActivePower_002`, `ATC_MPC_SCE1_PT_MUPhaseActivePower_003`, `ATC_MPC_SCE1_PT_MAPhaseActivePower_001`, `ATC_MPC_SCE1_PT_MAPhaseActivePower_003`, `ATC_MPC_SCE1_PT_MAPhaseActivePower_005` | ✅ passed |
| `[MPC-TS-002/1]` | MPC 1.0.0, 2.3.1.1 | `ATC_MPC_SCE1_PT_MUPhaseActivePower_001`, `ATC_MPC_SCE1_PT_MAPhaseActivePower_001` | ✅ passed |
| `[MPC-TS-002/2]` | MPC 1.0.0, 2.3.1.1 | `ATC_MPC_SCE1_PT_MUPhaseActivePower_002`, `ATC_MPC_SCE1_PT_MAPhaseActivePower_003` | ✅ passed |
| `[MPC-TS-002/3]` | MPC 1.0.0, 2.3.1.1 | `ATC_MPC_SCE1_PT_MUPhaseActivePower_003`, `ATC_MPC_SCE1_PT_MAPhaseActivePower_005` | ✅ passed |
| `[MPC-TS-002/4]` | MPC 1.0.0, 2.3.1.1 | `ATC_MPC_SCE1_PT_MUPhaseActivePower_001`, `ATC_MPC_SCE1_PT_MUPhaseActivePower_002`, `ATC_MPC_SCE1_PT_MUPhaseActivePower_003`, `ATC_MPC_SCE1_PT_MAPhaseActivePower_001`, `ATC_MPC_SCE1_PT_MAPhaseActivePower_003`, `ATC_MPC_SCE1_PT_MAPhaseActivePower_005` | ✅ passed |
| `[MPC-TS-003]` | MPC 1.0.0, 2.3, 2.3.2.1 | `ATC_MPC_SCE2_PT_MUTotalConsumedEnergy_001`, `ATC_MPC_SCE2_PT_MUTotalConsumedEnergy_002`, `ATC_MPC_SCE2_PT_MATotalConsumedEnergy_001` | ✅ passed |
| `[MPC-TS-003/1]` | MPC 1.0.0, 2.3.2.1 | `ATC_MPC_SCE2_PT_MUTotalConsumedEnergy_001`, `ATC_MPC_SCE2_PT_MUTotalConsumedEnergy_002`, `ATC_MPC_SCE2_PT_MATotalConsumedEnergy_001` | ✅ passed |
| `[MPC-TS-004]` | MPC 1.0.0, 2.3, 2.3.2.1 | `ATC_MPC_SCE2_PT_MUTotalProducedEnergy_001`, `ATC_MPC_SCE2_PT_MUTotalProducedEnergy_002`, `ATC_MPC_SCE2_PT_MATotalProducedEnergy_001` | ✅ passed |
| `[MPC-TS-004/1]` | MPC 1.0.0, 2.3.2.1 | `ATC_MPC_SCE2_PT_MUTotalProducedEnergy_001`, `ATC_MPC_SCE2_PT_MUTotalProducedEnergy_002`, `ATC_MPC_SCE2_PT_MATotalProducedEnergy_001` | ✅ passed |
| `[MPC-TS-005]` | MPC 1.0.0, 2.3.3 | `ATC_MPC_SCE3_PT_MUActiveACCurrent_001`, `ATC_MPC_SCE3_PT_MUActiveACCurrent_002`, `ATC_MPC_SCE3_PT_MUActiveACCurrent_003`, `ATC_MPC_SCE3_PT_MAActiveACCurrent_001`, `ATC_MPC_SCE3_PT_MAActiveACCurrent_003`, `ATC_MPC_SCE3_PT_MAActiveACCurrent_005` | ✅ passed |
| `[MPC-TS-005/1]` | MPC 1.0.0, 2.3.3.1 | `ATC_MPC_SCE3_PT_MUActiveACCurrent_001`, `ATC_MPC_SCE3_PT_MAActiveACCurrent_001` | ✅ passed |
| `[MPC-TS-005/2]` | MPC 1.0.0, 2.3.3.1 | `ATC_MPC_SCE3_PT_MUActiveACCurrent_002`, `ATC_MPC_SCE3_PT_MAActiveACCurrent_003` | ✅ passed |
| `[MPC-TS-005/3]` | MPC 1.0.0, 2.3.3.1 | `ATC_MPC_SCE3_PT_MUActiveACCurrent_003`, `ATC_MPC_SCE3_PT_MAActiveACCurrent_005` | ✅ passed |
| `[MPC-TS-005/4]` | MPC 1.0.0, 2.3 | `ATC_MPC_SCE3_PT_MUActiveACCurrent_001`, `ATC_MPC_SCE3_PT_MUActiveACCurrent_002`, `ATC_MPC_SCE3_PT_MUActiveACCurrent_003`, `ATC_MPC_SCE3_PT_MAActiveACCurrent_001`, `ATC_MPC_SCE3_PT_MAActiveACCurrent_003`, `ATC_MPC_SCE3_PT_MAActiveACCurrent_005` | ✅ passed |
| `[MPC-TS-006]` | MPC 1.0.0, 2.3, 2.3.4.1 | `ATC_MPC_SCE4_PT_MUACVoltage_001`, `ATC_MPC_SCE4_PT_MUACVoltage_002`, `ATC_MPC_SCE4_PT_MUACVoltage_003`, `ATC_MPC_SCE4_PT_MUACVoltage_004`, `ATC_MPC_SCE4_PT_MUACVoltage_005`, `ATC_MPC_SCE4_PT_MUACVoltage_006`, `ATC_MPC_SCE4_PT_MAACVoltage_001`, `ATC_MPC_SCE4_PT_MAACVoltage_003`, `ATC_MPC_SCE4_PT_MAACVoltage_005`, `ATC_MPC_SCE4_PT_MAACVoltage_007`, `ATC_MPC_SCE4_PT_MAACVoltage_009`, `ATC_MPC_SCE4_PT_MAACVoltage_011` | ✅ passed |
| `[MPC-TS-006/1]` | MPC 1.0.0, 2.3.4.1 | `ATC_MPC_SCE4_PT_MUACVoltage_001`, `ATC_MPC_SCE4_PT_MAACVoltage_001` | ✅ passed |
| `[MPC-TS-006/2]` | MPC 1.0.0, 2.3.4.1 | `ATC_MPC_SCE4_PT_MUACVoltage_002`, `ATC_MPC_SCE4_PT_MAACVoltage_003` | ✅ passed |
| `[MPC-TS-006/3]` | MPC 1.0.0, 2.3.4.1 | `ATC_MPC_SCE4_PT_MUACVoltage_003`, `ATC_MPC_SCE4_PT_MAACVoltage_005` | ✅ passed |
| `[MPC-TS-006/4]` | MPC 1.0.0, 2.3.4.1 | `ATC_MPC_SCE4_PT_MUACVoltage_004`, `ATC_MPC_SCE4_PT_MAACVoltage_007` | ➖ not applicable |
| `[MPC-TS-006/5]` | MPC 1.0.0, 2.3.4.1 | `ATC_MPC_SCE4_PT_MUACVoltage_005`, `ATC_MPC_SCE4_PT_MAACVoltage_009` | ➖ not applicable |
| `[MPC-TS-006/6]` | MPC 1.0.0, 2.3.4.1 | `ATC_MPC_SCE4_PT_MUACVoltage_006`, `ATC_MPC_SCE4_PT_MAACVoltage_011` | ➖ not applicable |
| `[MPC-TS-006/7]` | MPC 1.0.0, 2.3.4.1 | `ATC_MPC_SCE4_PT_MUACVoltage_001`, `ATC_MPC_SCE4_PT_MUACVoltage_002`, `ATC_MPC_SCE4_PT_MUACVoltage_003`, `ATC_MPC_SCE4_PT_MUACVoltage_004`, `ATC_MPC_SCE4_PT_MUACVoltage_005`, `ATC_MPC_SCE4_PT_MUACVoltage_006`, `ATC_MPC_SCE4_PT_MAACVoltage_001`, `ATC_MPC_SCE4_PT_MAACVoltage_003`, `ATC_MPC_SCE4_PT_MAACVoltage_005`, `ATC_MPC_SCE4_PT_MAACVoltage_007`, `ATC_MPC_SCE4_PT_MAACVoltage_009`, `ATC_MPC_SCE4_PT_MAACVoltage_011` | ✅ passed |
| `[MPC-TS-007]` | MPC 1.0.0, 2.3, 2.3.5.1 | `ATC_MPC_SCE5_PT_MUFrequency_001`, `ATC_MPC_SCE5_PT_MAFrequency_001` | ✅ passed |
| `[MPC-TS-008]` | MPC 1.0.0, 2.5.2 | `ATC_MPC_SCE1_NT_MATotalActivePower_002`, `ATC_MPC_SCE1_NT_MAPhaseActivePower_002`, `ATC_MPC_SCE1_NT_MAPhaseActivePower_004`, `ATC_MPC_SCE1_NT_MAPhaseActivePower_006`, `ATC_MPC_SCE2_NT_MATotalConsumedEnergy_002`, `ATC_MPC_SCE2_NT_MATotalProducedEnergy_002`, `ATC_MPC_SCE3_NT_MAActiveACCurrent_002`, `ATC_MPC_SCE3_NT_MAActiveACCurrent_004`, `ATC_MPC_SCE3_NT_MAActiveACCurrent_006`, `ATC_MPC_SCE4_NT_MAACVoltage_002`, `ATC_MPC_SCE4_NT_MAACVoltage_004`, `ATC_MPC_SCE4_NT_MAACVoltage_006`, `ATC_MPC_SCE4_NT_MAACVoltage_008`, `ATC_MPC_SCE4_NT_MAACVoltage_010`, `ATC_MPC_SCE4_NT_MAACVoltage_012`, `ATC_MPC_SCE5_NT_MAFrequency_002` | ✅ passed |
| `[MPC-TS-008/1]` | MPC 1.0.0, 2.5.2 | `ATC_MPC_SCE1_NT_MATotalActivePower_002`, `ATC_MPC_SCE1_NT_MAPhaseActivePower_002`, `ATC_MPC_SCE1_NT_MAPhaseActivePower_004`, `ATC_MPC_SCE1_NT_MAPhaseActivePower_006`, `ATC_MPC_SCE2_NT_MATotalConsumedEnergy_002`, `ATC_MPC_SCE2_NT_MATotalProducedEnergy_002`, `ATC_MPC_SCE3_NT_MAActiveACCurrent_002`, `ATC_MPC_SCE3_NT_MAActiveACCurrent_004`, `ATC_MPC_SCE3_NT_MAActiveACCurrent_006`, `ATC_MPC_SCE4_NT_MAACVoltage_002`, `ATC_MPC_SCE4_NT_MAACVoltage_004`, `ATC_MPC_SCE4_NT_MAACVoltage_006`, `ATC_MPC_SCE4_NT_MAACVoltage_008`, `ATC_MPC_SCE4_NT_MAACVoltage_010`, `ATC_MPC_SCE4_NT_MAACVoltage_012`, `ATC_MPC_SCE5_NT_MAFrequency_002` | ✅ passed |
| `[MPC-TS-008/2]` | MPC 1.0.0, 2.5.2 | `ATC_MPC_SCE1_NT_MATotalActivePower_002`, `ATC_MPC_SCE1_NT_MAPhaseActivePower_002`, `ATC_MPC_SCE1_NT_MAPhaseActivePower_004`, `ATC_MPC_SCE1_NT_MAPhaseActivePower_006`, `ATC_MPC_SCE2_NT_MATotalConsumedEnergy_002`, `ATC_MPC_SCE2_NT_MATotalProducedEnergy_002`, `ATC_MPC_SCE3_NT_MAActiveACCurrent_002`, `ATC_MPC_SCE3_NT_MAActiveACCurrent_004`, `ATC_MPC_SCE3_NT_MAActiveACCurrent_006`, `ATC_MPC_SCE4_NT_MAACVoltage_002`, `ATC_MPC_SCE4_NT_MAACVoltage_004`, `ATC_MPC_SCE4_NT_MAACVoltage_006`, `ATC_MPC_SCE4_NT_MAACVoltage_008`, `ATC_MPC_SCE4_NT_MAACVoltage_010`, `ATC_MPC_SCE4_NT_MAACVoltage_012`, `ATC_MPC_SCE5_NT_MAFrequency_002` | ✅ passed |
| `[MPC-TS-009]` | MPC 1.0.0, 2.5.1 | `ATC_MPC_SCE1_PT_MUTotalActivePower_001`, `ATC_MPC_SCE1_PT_MUPhaseActivePower_001`, `ATC_MPC_SCE1_PT_MUPhaseActivePower_002`, `ATC_MPC_SCE1_PT_MUPhaseActivePower_003`, `ATC_MPC_SCE2_PT_MUTotalConsumedEnergy_001`, `ATC_MPC_SCE2_PT_MUTotalConsumedEnergy_002`, `ATC_MPC_SCE2_PT_MUTotalProducedEnergy_001`, `ATC_MPC_SCE2_PT_MUTotalProducedEnergy_002`, `ATC_MPC_SCE3_PT_MUActiveACCurrent_001`, `ATC_MPC_SCE3_PT_MUActiveACCurrent_002`, `ATC_MPC_SCE3_PT_MUActiveACCurrent_003`, `ATC_MPC_SCE1_PT_MATotalActivePower_001`, `ATC_MPC_SCE1_PT_MAPhaseActivePower_001`, `ATC_MPC_SCE1_PT_MAPhaseActivePower_003`, `ATC_MPC_SCE1_PT_MAPhaseActivePower_005`, `ATC_MPC_SCE2_PT_MATotalConsumedEnergy_001`, `ATC_MPC_SCE2_PT_MATotalProducedEnergy_001`, `ATC_MPC_SCE3_PT_MAActiveACCurrent_001`, `ATC_MPC_SCE3_PT_MAActiveACCurrent_003`, `ATC_MPC_SCE3_PT_MAActiveACCurrent_005` | ✅ passed |
| `[MPC-TS-010]` | MPC 1.0.0, 2.5.1 | `ATC_MPC_SCE4_PT_MUACVoltage_001`, `ATC_MPC_SCE4_PT_MUACVoltage_002`, `ATC_MPC_SCE4_PT_MUACVoltage_003`, `ATC_MPC_SCE4_PT_MUACVoltage_004`, `ATC_MPC_SCE4_PT_MUACVoltage_005`, `ATC_MPC_SCE4_PT_MUACVoltage_006`, `ATC_MPC_SCE4_PT_MAACVoltage_001`, `ATC_MPC_SCE4_PT_MAACVoltage_003`, `ATC_MPC_SCE4_PT_MAACVoltage_005`, `ATC_MPC_SCE4_PT_MAACVoltage_007`, `ATC_MPC_SCE4_PT_MAACVoltage_009`, `ATC_MPC_SCE4_PT_MAACVoltage_011` | ✅ passed |
| `[MPC-TS-013]` | MPC 1.0.0, 5.3 | `ATC_MPC_COM_PT_MUPolling_001`, `ATC_MPC_SCE2_PT_MUTotalConsumedEnergy_002`, `ATC_MPC_SCE2_PT_MUTotalProducedEnergy_001`, `ATC_MPC_COM_PT_MAPolling_001` | ✅ passed |
| `[MPC-TS-014]` | MPC 1.0.0, 5.3 | `ATC_MPC_COM_PT_MUNotification_001`, `ATC_MPC_COM_PT_MANotification_001` | ✅ passed |

---

Generated by `eebus conform`. The EEBUS specifications are licensed material and are not part of this repository; this report reproduces identifiers and section references only.
