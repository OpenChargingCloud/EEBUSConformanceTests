# EEBUS in C# / .NET 10 — Work Plan

**Repository:** `OpenChargingCloud/EEBUSConformanceTests`
**Status:** 2026-07-26 (after analysing ship-go, spine-go, eebus-go, Hermod, Styx, WWCP_EEBUS
and the official specifications in `docs/specs/`)
**Audience:** Autonomous coding agents (Opus 5 / Sonnet 5 et al.) working through the work packages.

---

## 0. Mission and scope

Build a complete **EEBUS protocol stack in C# (.NET 10)** — SHIP client/server and SPINE
client/server — plus a **use case layer**, **example simulations for e-mobility**
(LPC, MPC, OPEV, …) and a **conformance and interoperability test suite** (NUnit) which tests
both our own stack and other implementations (ship-go, spine-go, eebus-go, EVCC, EEBUS.Net, …).

**Two repository model (decision by Achim, 2026-07-26):**

| Repository | Role |
|---|---|
| [`OpenChargingCloud/WWCP_EEBUS`](https://github.com/OpenChargingCloud/WWCP_EEBUS) (submodule `libs/WWCP_EEBUS`) | **The stack.** `WWCP_EEBUS_SHIP`, `WWCP_EEBUS_SPINE`, `WWCP_EEBUS_UseCases` (and their unit tests) are built here. The existing code is refactored (→ WP-W0). All "production" work packages (WP01–WP09) deliver into this repository. |
| `OpenChargingCloud/EEBUSConformanceTests` (this repository) | **The test bench.** Conformance catalog (WP11), interoperability harness (WP12), simulations (WP10), device replay, reference submodules, specifications. It primarily tests WWCP_EEBUS, but can test any EEBUS device. |

Changes to `libs/WWCP_EEBUS` are committed and pushed within the submodule (after Achim's
approval); afterwards the submodule pointer is updated here.

### Status (2026-07-26)

| WP | Status |
|---|---|
| **WP-W0** | ✅ done — `WWCP_EEBUS` builds standalone (Styx + Hermod only), `dotnet test` green (13 tests) |
| **WP00** | ✅ done — `EEBUSConformanceTests.sln`, CLI, conformance and interoperability test projects, GitHub Actions CI; `dotnet test` green (5 tests + 2 interoperability tests reporting "inconclusive" without Go) |
| **WP01** | ✅ done — complete SHIP message set (including init/CMI, the PIN family, `accessMethodsRequest`), **EEBUSJSON** verified against the golden vectors of ship-go, `SHIPFrame` framing, `SHIPMessageExchangeStates` (0–39) |
| **WP02** | ✅ done — `SKI` value type, ECDSA P-256 certificates with subject key identifier, SHA-256 fingerprint, SHIP TLS profile; a real TLS handshake with mutual authentication in the tests; ADR `docs/adr/0001-tls-cipher-suites.md` |
| **WP03** | ✅ done — `SHIPServiceTXT` (SHIP 7.3.2 including `serial`/`cat`, manufacturer keys survive parsing), `SHIPServiceInstance`, `ISHIPDiscovery` with two implementations: **`SHIPMDNSDiscovery`** (real multicast DNS: responder, browser, goodbye with TTL 0) and `InMemorySHIPDiscovery` for environments without multicast. **Own DNS-SD wire encoding** (`SHIPMDNSMessage`), because Hermod's `TXT` carries only *one* string while DNS-SD requires one character string per key/value pair (RFC 6763 §6.1) — including name compression when reading. Live test over real multicast green. |
| **WP04** | ✅ done — `SHIPConnection` with all phases (CMI, hello including prolongation, protocol handshake, PIN, access methods, data, close); **every timer runs on a TimeProvider**, tests use `FakeTimeProvider` without a single real wait |
| **WP05** | ✅ done — `SHIPNode` (connection registry, **double connection rule** SHIP 12.2.2 in both directions, pairing with approve/reject, `ISHIPTrustStore` as in-memory and JSON file variant, protection against connecting to itself) plus **`SHIPWebSocketEndpoint`**: Hermod WebSocket server and client with sub protocol `ship`, the TLS profile of WP02, SKI extraction from the TLS handshake. **End-to-end test green:** two nodes over real TLS WebSockets, from the TLS handshake to the SPINE datagram. |
| **WP06a** | ✅ done — `Apps/EEBUSModelGen` generates the SPINE 1.3.0 model from the 76 official XSDs: **562 complex types, 81 enumerations, 142 functions, 2133 properties** in 121 checked-in files. Extensible string types in the `PredefinedStrings` style, the function registry `SPINEFunctions`, `[EEBUSFunction]`/`[EEBUSKey]` metadata, the ISO 8601 types keeping their text, `SPINEJSON` for the two JSON defaults which are wrong for this protocol. Checked against spine-go through a generated fixture **and** against its 23 recorded datagrams; ADR `docs/adr/0002-spine-model-generation.md`, three findings in `docs/spec-deviations.md` |
| **WP06b** | ✅ done — the generated types are `partial` and serialise **opt-in**, so the hand-written semantics live under `WWCP_EEBUS_SPINE/Additions/` and cannot leak into a datagram: `ScaledNumberType.Value`/`FromValue` (decimal, exact), the address `ToString`/`Matches`/`Clone`, `TimePeriodType.Duration(TimeProvider)`, `PossibleOperationsType`, `CmdType`/`FilterType`/`CmdControlType` reaching every function through the generated `[EEBUSFunction]` metadata, the one-line datagram overview, the SPINE error numbers, `UseCaseInformationDataType` set/find/supports/remove |
| **WP06c** | ✅ done — the **restricted function exchange** (SPINE 1.3.0, § 5.3.4) as one generic engine over the model metadata, `WWCP_EEBUS_SPINE/Update/`: selector matching (including the exact-match rule for entity addresses), element filters which reach into nested elements, merging by identifiers, the "list item without identifier applies to all entries" rule, the write marks, and `SPINERead` for the answering half of a partial read. **All 29 official example datagrams of Annex A** are tests: read by the model, written back unchanged, put through the EEBUS JSON transformation of SHIP and back, and applied to a defined state. ADR `docs/adr/0003-spine-update-system.md`, four further findings (S4–S7) in `docs/spec-deviations.md` |
| WP07–WP14 | open (order see § 10) |

**Test inventory:** 117 SHIP + 78 SPINE + 1 use case tests within the stack (4 of them marked
`[Category("LocalNetwork")]`: real sockets or multicast), 5 conformance tests within the test
bench, 2 interoperability tests (green in WSL). The CI excludes `LocalNetwork`, because runners
provide neither multicast nor a free port 5353 reliably.

**Wire format bugs found in the existing code and fixed** (all of them uncovered by the new
tests — exactly what this repository is for):
1. `messageProtocolHandshake.formats` was serialised as a flat array instead of the complex
   type `{"formats":{"format":[…]}}` (XSD `MessageProtocolFormatsType`).
2. The access methods **response** used the element `accessMethodsRequest` instead of
   `accessMethods` — which made the handshake loop forever.
3. `connectionClose.reason` was read from the JSON property `"dns"` (copy and paste).

Decisions taken in WP-W0 (details in the work package): **variant A** (core without
`WWCP_Core`, adapter parked), `customData` removed from all SHIP wire types, one dedicated
`Version` class per project stating the implemented specification versions.

Open points for Achim: both repositories have `commit.gpgsign=true`, so commits have to be
signed by Achim — the WP-W0 changes are **staged** on the submodule branch `wp-w0-refactoring`.

**Go environment (set up 2026-07-26):** Go **1.26.5** lives in WSL/Debian 13 below
`~/.local/go` (tarball, SHA-256 verified, installed without sudo; `~/.local/bin/go` is on the
login PATH via Debian's `~/.profile`). ship-go, spine-go and eebus-go build there without
errors, and WSL already has the **.NET 10 SDK** (10.0.302) — so the interoperability tests run
locally, entirely within WSL (verified: 2/2 green).
**Important:** a Go toolchain inside WSL cannot sensibly be driven from a test run on Windows —
the Go peer would live in the WSL network namespace, which breaks mDNS discovery and the
direction where the Go peer connects to us. Therefore, for WP12: **always run the
interoperability suite on Linux or within WSL**
(`wsl -e bash -lc "cd … && dotnet test --filter TestCategory=Interop"`), just like the CI does.
On Windows, `GoToolchain.Require()` reports "inconclusive" together with that hint. The
reference repositories require Go >= 1.24 (spine-go and eebus-go even pin
`toolchain go1.24.4`) — the CI was corrected from 1.23 to 1.26 accordingly.

Guiding principles:

1. **Hermod and Styx as the foundation** (git submodules below `libs/`): WebSocket client and
   server, TLS, DNS, logging, Illias helpers. Adopt the coding conventions of Hermod and Styx.
2. **NUnit 4.x** for all tests (like `HermodTests`).
3. **`System.TimeProvider` everywhere** time is involved (timers, timeouts, heartbeats,
   failsafe durations, timestamps). Tests use `FakeTimeProvider`. No `DateTime.Now`, no
   `Task.Delay(...)` without a TimeProvider, no `Thread.Sleep` in tests.
4. **Reference implementations as submodules** below `libs/` (ship-go, spine-go, eebus-go):
   the source for protocol details, golden files, and as interoperability peers.
5. The stack has to master **both roles** (client *and* server) on the SHIP and SPINE level —
   only then can conformance tests be run against arbitrary devices.

---

## 1. Reference material

### 1.1 Submodules (already cloned)

| Path | Content | Most important files/folders |
|---|---|---|
| `libs/Styx` | Base library (Illias, CLI, …), Apache-2.0 | `Styx/Illias/` (helpers, `Time/Timestamp.cs`), `Styx/CLI/` |
| `libs/Hermod` | Networking stack, Apache-2.0, net10.0 | `Hermod/HTTP1/WebSocket/` (client + server), `Hermod/DNS/` (client + server, multicast capable, SRV/PTR/TXT), `Hermod/TLS/`, `Hermod/PKI/`, `Hermod/Logging/` |
| `libs/ship-go` | SHIP 1.0.1 reference (Go, enbility) | `model/model.go` (SHIP messages), `ship/` (state machines `hs_init.go`, `hs_hello.go`, `hs_prot.go`, `hs_pin.go`, `hs_access.go`, timers in `ship/types.go`), `mdns/mdns.go` (TXT records), `cert/cert.go` (TLS/SKI), `hub/` (double connections, pairing), `ws/` (framing), `docs/SPEC_COMPLIANCE.md`, `ARCHITECTURE.md`, `examples/` |
| `libs/spine-go` | SPINE 1.3.0 reference (Go, enbility) | `model/` (~26k lines of data model including `datagram.go`, `commandframe.go`, `commondatatypes.go`, `PRIMARYKEY_TAG_GUIDELINES.md`, `UPDATE_SYSTEM_GUIDE.md`), `spine/` (DeviceLocal/Remote, NodeManagement, subscription/binding/heartbeat managers, `send.go`), `spine/testdata/` + `integration_tests/testdata/` (golden JSON!) |
| `libs/eebus-go` | Use case layer + service (Go, enbility) | `service/` (assembly of SHIP + SPINE), `api/configuration.go`, `usecases/` (`cs/lpc`, `cs/lpp`, `eg/lpc`, `eg/lpp`, `cem/opev`, `cem/oscev`, `cem/evcc`, `cem/evcem`, `cem/evsecc`, `cem/evsoc`, `cem/cevc`, `cem/ohpcf`, `cem/vabd`, `cem/vapd`, `ma/mpc`, `ma/mgcp`, `ma/mdt`, `mu/mpc`, `gcp/mgcp`, `usecase/usecase.go` = UseCaseBase), `features/` (client and server feature helpers), `examples/` (`hems`, `evse`, `controlbox`, `ced`, `heatpump`, `remote`) |
| `libs/devices` | **Recorded answers of real devices** (MIT): `NodeManagementDetailedDiscoveryData` + `NodeManagementUseCaseData` of series devices (Elli, EVCC, Kostal, Porsche PMCC, SMA, Spelsberg, Vaillant, Viessmann) | per device `device.json`, `discovery-data.json`, `usecase-data.json`; `schema/` (JSON schemas), `devices.json`/`usecases.json` (aggregates). **Windows note:** `vaillant/arotherm-vwl-75:6a/` contains a colon → cannot be checked out; the content is readable via `git -C libs/devices show "HEAD:vaillant/arotherm-vwl-75:6a/discovery-data.json"` (which is why the submodule is set to `ignore = dirty`) |
| `libs/devices-app` | GUI test tool (Go + Vue 3, MIT): pairing through a web UI, shows the SPINE data, use cases and features of the communication partner | `main.go` (HTTP :7050, EEBUS :4815, automatic certificates); an ideal **manual peer** for our server |
| `libs/WWCP_EEBUS` | **Target repository of the stack** (C#, Apache-2.0, net10.0), namespace `cloud.charging.open.protocols.EEBUS.SHIP/.SPINE` | `WWCP_EEBUS_SHIP/` (SHIP messages as TryParse/ToJSON types, `PredefinedStrings/`, OCPP-like `EEBUSAdapter/`, `WebSocket/SHIPWebSocketClient/-Server.cs`, `AEEBUSNode.cs`), `WWCP_EEBUS_SPINE/` (only `Version.cs` — empty), both `*_Tests/` are placeholders. **Note:** the csproj expects `WWCP_Core` siblings (→ WP-W0) |

### 1.2 Specifications — **available locally below `docs/specs/`!**

> `docs/specs/` is working material (EEBUS download license) and is **not committed**
> (`.gitignore`). Every agent working here can and should look into it directly.

Structure: five category folders (`E-Mobility/`, `Grid/`, `HVAC/`, `Inverter/`, `SHIP SPINE/`),
each containing `Technical Specifications/`, partly `Implementation Guides/` and
`Test Specifications/`. All ZIP archives are **extracted locally** (folder of the same name
next to the archive; extract again after a fresh checkout).

| Path (below `docs/specs/`) | Content |
|---|---|
| `SHIP SPINE/Technical Specifications/EEBus_SHIP_TS_Specification_v1.0.1-1/EEBus_SHIP_TS_Specification_v1.0.1/` | **SHIP TS 1.0.1** (96 pages, PDF + `EEBus_SHIP_TS_TransferProtocol.xsd`). Chapters: 5 registration, 6 reconnection, 7 discovery (7.3 mDNS/TXT), 8 TCP, 9 TLS (9.1 cipher suites, **9.2 maximum fragment length**), 10 WebSocket (10.2 sub protocol), **11 JSON format (11.4/11.5 = the normative EEBUS JSON rules)**, 12 key management (12.2 SKI, 12.5 PIN, 12.6 QR code), 13 data exchange (13.4 the SME state machines), 14 well-known `protocolId`. **The interoperability target version.** |
| `SHIP SPINE/Technical Specifications/EEBus_SHIP_TS_Specification_v1.1.0_public/EEBus_SHIP_TS_Specification_v1.1.0_public/` | **SHIP TS 1.1.0** (122 pages, PDF + XSD). Same chapter structure, new among others: 7.4 re-discovery recommendations, 9.7 TLS ECC extension, **9.8 TLS probing**, 12.6 SHIP commissioning tool, 6.1 key changes. Delta review task in WP01/WP04; the implementation target remains 1.0.1 |
| `SHIP SPINE/Technical Specifications/EEBus_SPINE_V1.3.0/EEBus_SPINE_V1.3.0_Final_hp/` | **SPINE 1.3.0 FINAL**: `Documentation/` (introduction, protocol specification, resource specification), `XSDs/` (**76 XSDs — the primary source for the model generation in WP06**, namespace `http://docs.eebus.org/spine/xsd/v1`), `ExampleXMLs/RestrictedFunctionExchange/` (30 official partial update datagrams → fixtures for WP06/07) |
| `SHIP SPINE/Technical Specifications/EEBUS_TS_ShipRequirementsForInstallationProcess_V1.1.0.pdf` + `EEBus_SHIP_Pairing_Service_TS_Specification_V1.0.0.pdf` | Installation process **v1.1.0** + **SHIP pairing service** — the basis of WP14 |
| `SHIP SPINE/Test Specifications/` | **The official protocol test specifications** (each a PDF + a parameter sheet XLSX): `EEBus_SHIP_TestSpecification_V1.0.0` (test cases `TC_SHIP_*`: MDNS, CONN, ROLE, SEC, MSG, CMI, HELLO, PROT, PIN, TERM, AM, AMDATA — with a requirement ↔ test case mapping), `EEBus_SPINE_TestSpecification_V1.0.0` (`TC_SPINE_*`: COMP, DATA, FC, DDISC, BIND, SUBS, ENTITY, RTS, RTC), `EEBus_SHIP_Pairing_Service_TestSpec_V1.0.0`. **The foundation of WP11!** |
| `SHIP SPINE/Implementation Guides/EEBus_UC_IG_GeneralGuidelines_V1.0.0.pdf` | **Cross-cutting rules (13 pages) — required reading for WP07/WP08!** Defines "client actor"/"server actor", the handling of secondary functions (**heartbeats**), several energy management devices in one setup, the obligation to provide use case specific mandatory data, **subscriptions take precedence over polling + redundant polling is prohibited**, the definition of "relevant" features, binding partner clarifications. Partly replaces the missing `UseCaseBaseSpecification` in content (but is *not* identical to it) (+ copies of the LPC/LPP implementation guides) |
| `Grid/Technical Specifications/` | **LPC V1.0.0**, **LPP V1.0.0**, **MGCP V1.0.0**, **MPC V1.0.0** (public) as well as **new grid use cases: PowerDemandForecast V1.0.0, PowerEnvelope V1.0.0, TimeOfUseTariff V1.0.0** |
| `Grid/Implementation Guides/` | **IG LPC V1.1.0**, IG LPP V1.0.0 — practical guides (§14a!) for WP09/1 and `sim lpc-chain` |
| `Grid/Test Specifications/` | **The use case high level test specifications V1.0.2** for LPC, LPP, MGCP, MPC (each a PDF + a parameter sheet XLSX) — the use case part of the WP11 catalog |
| `E-Mobility/Technical Specifications/` | The eight EV use case specifications: OPEV V1.0.1b, OSCEV V1.0.1b, CEVC V1.0.1, EVCC V1.0.1, EVSECC V1.0.1, EVCEM V1.0.1, EVSOC V1.0.0 RC1, **EVCS V1.0.1** (not in eebus-go, see § 2.3) |
| `HVAC/Technical Specifications/` | OHPCF V1.0.0, IncentiveTableBasedPowerConsumptionManagement V1.0.0, **NodeIdentification V1.0.0**, VisualizationOfHeatingAreaName V1.0.0 + the archives `EEBUS_HVAC_SystemFunction_UseCases` / `…_Temperature_UseCases` (each a complete use case family) |
| `Inverter/Technical Specifications/` | ControlOfBattery V1.0.0, MonitoringOfBattery V1.0.0, MonitoringOfInverter V1.0.0, MonitoringOfPVString V1.0.0 RC4, VABD V1.0.0 RC1, VAPD V1.0.0 RC1 |

Still missing (nice to have): `EEBus_UC_TS_UseCaseBaseSpecification.pdf` — referenced in
chapter 1.1.1 ("EEBUS documents", tag `[UseCaseBaseSpecification]`) of exactly **9** of the
local PDFs: the seven e-mobility specifications OPEV, OSCEV, CEVC, EVCC, EVSECC, EVCEM, EVCS
as well as VABD/VAPD (inverter, RC1). It is **not** referenced by the grid family (LPC/MPC:
only the SPINE protocol and resource specification + SHIP 1.0.1), by EVSOC RC1 (which still
references SHIP 1.0.0!) and by HVAC — there the generic use case rules sit directly in
chapter 2.4. Also still missing — if they exist at all — the high level test specifications of
the e-mobility family.

Conflict rule: where a PDF or XSD contradicts the Go reference, the Go behaviour decides for
**wire compatibility** (it is proven in certification), the specification decides for the
**conformance tests** — such cases are documented as findings in `docs/spec-deviations.md`
(modelled after `libs/ship-go/docs/SPEC_COMPLIANCE.md`).

### 1.3 The EEBUS ecosystem (research result, as of July 2026)

| Project | Language | Scope | Value for us |
|---|---|---|---|
| [enbility/ship-go](https://github.com/enbility/ship-go) + [spine-go](https://github.com/enbility/spine-go) + [eebus-go](https://github.com/enbility/eebus-go) | Go | complete, in production (EVCC) | reference + primary interoperability peer |
| [evcc-io/evcc](https://github.com/evcc-io/evcc) | Go | HEMS product using eebus-go; §14a EnWG via LPC/MPC, EEBUS wallboxes (among others Porsche PMCC, Elli) | a realistic end-to-end interoperability peer |
| [digitaltwinconsortium/EEBUS.Net](https://github.com/digitaltwinconsortium/EEBUS.Net) | C#/.NET 6 | SHIP complete, SPINE started; MIT | a second independent SHIP peer; a source of ideas for C# idioms (but ASP.NET based, a different architecture) |
| [NIBEGroup/openeebus](https://github.com/NIBEGroup/openeebus) | C | SHIP + SPINE (heat pump manufacturer) | an optional third interoperability peer |
| [LMF-DHBW/go_eebus](https://github.com/LMF-DHBW/go_eebus) | Go | an older framework | historical only |
| [openmuc/jeebus.spine](https://github.com/openmuc/jeebus.spine), [arasgungore/EEBUS-in-Java](https://github.com/arasgungore/EEBUS-in-Java) | Java | SPINE respectively SHIP + SPINE, older | comparison material |
| [OpenChargingCloud/WWCP_EEBUS](https://github.com/OpenChargingCloud/WWCP_EEBUS) | C# | an early skeleton (SHIP messages, an OCPP-like adapter; SPINE empty) | **the target repository of the stack** (§ 0 two repository model, WP-W0 refactoring); included as `libs/WWCP_EEBUS` |
| KEO Connectivity, [EEBUS Tester](https://eebustester.com/) | commercial | certified stack / the official test tool | the certification target; mind the KEO peculiarities (see risks) |

---

## 2. Protocol digest (distilled from the analysis)

> This section summarises what the agents need to know, with pointers to the authoritative
> files within the submodules. **When in doubt: read the Go source.**

### 2.1 SHIP (Smart Home IP) 1.0.1

**Transport:** WebSocket over TLS (`wss://`), **sub protocol `ship`** (SHIP 10.2), the path
taken from the mDNS TXT record (`path`, by default `/ship/`), the usual port 4712 (freely
choosable). **Binary frames**; every SHIP message is **one prefix byte (message type) +
JSON (UTF-8)**:

| Byte | Type | Content |
|---|---|---|
| `0` | `init` | exactly `[0x00, 0x00]` (value 0) |
| `1` | `control` | handshake messages (`connectionHello`, `messageProtocolHandshake[Error]`, `connectionPinState/-Input/-Error`, `accessMethodsRequest`, `accessMethods`) |
| `2` | `data` | `{"data":{"header":{"protocolId":"ee1.0"},"payload":<SPINE datagram>}}` |
| `3` | `end` | `{"connectionClose":{"phase":"announce"\|"confirm","maxTime":…,"reason":…}}` |

ship-go limits incoming messages to 100 KiB (`ws/types.go`) — a sensible hardening, adopted.

**"EEBUS JSON"** (normative: SHIP TS **chapter 11**, "Message Representation Using JSON Text
Format", 11.4/11.5 XML↔JSON transformation): every JSON object is encoded on the wire as an
**ordered array of single property objects** (an inheritance from XML; the order is
significant!):

```json
// ordinary JSON:
{"messageProtocolHandshake":{"handshakeType":"announceMax","version":{"major":1,"minor":0},"formats":{"format":["JSON-UTF8"]}}}
// EEBUS JSON (as it goes on the wire, after the type byte):
[{"messageProtocolHandshake":[{"handshakeType":"announceMax"},{"version":[{"major":1},{"minor":0}]},{"formats":[{"format":["JSON-UTF8"]}]}]}]
```

ship-go implements the reverse transformation naively with string replacements
(`ship/helper.go`) — **we build it structurally** (JObject ↔ EEBUS form, order preserving,
with the special case `[]`→`{}`) and tolerantly (known device quirks: the PMCC appends `0x00`
bytes; see `JsonFromEEBUSJson`).

**Discovery (SHIP 5/7):** mDNS/DNS-SD, service type **`_ship._tcp`**, TXT records (SHIP 7.3.2):
`txtvers=1`, `path=/ship/`, `id=<unique identifier>`, `ski=<hex>`, `brand=`, `model=`, `type=`,
`register=true|false` (the auto accept signal), optionally `serial=`, `cat=` (installation
process specification).

**TLS (SHIP 9):** TLS >= 1.2, self-signed **ECDSA P-256** certificates with a
**subject key identifier** extension; mutual certificates are mandatory (a client certificate
is required). Cipher suites: `TLS_ECDHE_ECDSA_WITH_AES_128_CBC_SHA256` (required),
`TLS_ECDHE_ECDSA_WITH_AES_128_GCM_SHA256` (optional). The identity is the **SKI** (40
hexadecimal characters, extracted from the certificate and normalised, presented in groups of
four). Trust means: the user confirmed the SKI (pairing), or auto accept is on.
Reference: `libs/ship-go/cert/cert.go`, `hub/hub_connections_cert*`.

**Connection setup (SHIP 13.4), states in `libs/ship-go/model/types.go`:**

1. **CMI** (13.4.3): both sides send `init` (`[0,0]`); timeout **10 s** (`cmiTimeout`).
   The client sends immediately, the server waits, validates, and answers.
2. **SME hello** (13.4.4.1): `connectionHello` with `phase` ∈ `ready|pending|aborted`,
   `waiting` (ms), `prolongationRequest`. Trust logic: if the peer is already trusted → `ready`,
   otherwise `pending` and waiting for the user's approval, with the prolongation mechanism.
   Timers (from `ship/types.go`): `tHelloInit` **60 s**, `tHelloInc` **60 s**,
   `tHelloProlongThrInc` **30 s**, `tHelloProlongMin` **1 s**, `tHelloProlongWaitingGap` **15 s**,
   `tAbortDelay` **1 s**. The complete state machine: `ship/hs_hello.go` (with the client and
   server tests next to it).
3. **Protocol handshake** (13.4.4.2): the client sends `announceMax` with
   `version {major:1,minor:0}` and `formats:["JSON-UTF8"]` → the server answers `select`
   (JSON-UTF8) → the client confirms (an echo). Errors → `messageProtocolHandshakeError`
   (`error`: 1 = timeout, 2 = unexpected message, 3 = selection mismatch). UTF-16 is optional
   and — like in ship-go — **not** supported.
4. **PIN** (13.4.5): `connectionPinState` with `pinState` ∈ `required|optional|pinOk|none`.
   Like ship-go: implement **only `none`**; a `required` from the other side aborts the
   connection (document this).
5. **Access methods** (13.4.6): `accessMethodsRequest` → `accessMethods {id, dnsSd_mDns?, dns{uri}?}`.
   The `id` serves among other things to detect duplicates.
6. **Data exchange** (13.4.7): SPINE datagrams within `data` messages.
7. **Close**: `connectionClose` `announce` (optionally with `maxTime`) → `confirm`, then the
   WebSocket close.

**Double connections (SHIP 12.2.2):** if both sides connect at the same time, exactly one
connection wins, decided by comparing the SKI values. What ship-go does (a documented,
deliberate deviation): "the connection **initiated by the higher SKI** is kept" —
`hub/hub_connections_registry.go` (`keepThisConnection`). We implement the same rule (for
interoperability!) and test both directions.

**Reconnection:** not part of the specification; ship-go uses an exponential backoff
(1st / 2nd / 3rd+ attempt: 0–3 s / 3–10 s / 10–20 s). Adopt it (configurable, TimeProvider based).

**SHIP pairing service** (an optional extension, the "installation process"): QR code payload,
HMAC-SHA256 keyring, the `register` flag, the 15 minute AddCu replacement logic —
`libs/ship-go/ARCHITECTURE_SHIPPAIRING.md`, `api/shippairing.go`. A separate, later work package.

### 2.2 SPINE 1.3.0

**Datagram** (`libs/spine-go/model/datagram.go`):

```
datagram
├── header: specificationVersion (e.g. "1.3.0"), addressSource/addressDestination
│           {device: string, entity: uint[], feature: uint}, addressOriginator?,
│           msgCounter (monotonic per device), msgCounterReference (for reply/result/notify
│           on a request), cmdClassifier ∈ read|reply|notify|write|call|result,
│           ackRequest?, timestamp?
└── payload.cmd[]: one function payload per cmd (a choice out of ~100 functions)
                   + optional filter[] (partial reads/writes: cmdControl partial|delete,
                   *ListDataSelectors, *DataElements) + optional function
```

**Device model:** `Device` → `Entity[]` (the address is an index path, entity 0 is
`DeviceInformation`) → `Feature[]` (role `client|server|special`). **NodeManagement** is
entity 0 / feature 0, with the functions `nodeManagementDetailedDiscoveryData`,
`…SubscriptionRequestCall/DeleteCall/Data`, `…BindingRequestCall/DeleteCall/Data`,
`…UseCaseData`, `…DestinationListData`.
The feature types (33) and entity types (including `EV`, `EVSE`, `CEM`, `GridGuard`,
`ControllableSystem`, `GridConnectionPointOfPremises`, …):
`libs/spine-go/model/commondatatypes.go` (around line 620).

**Communication rules:**

* `read` → `reply`; `write` (requires a **binding**) → optionally `result` (with `ackRequest`);
  changes → `notify` to the **subscribers**; `call` for the RPC-like functions
  (subscriptions and bindings).
* `result` datagrams (`resultData` with `errorNumber`, 0 = ok) reference the request via
  `msgCounterReference`.
* **Partial updates** are the trickiest part: selectors and elements per function; the read and
  write logic and the merge semantics are documented in
  `libs/spine-go/model/UPDATE_SYSTEM_GUIDE.md` and `PRIMARYKEY_TAG_GUIDELINES.md` — required
  reading for WP06/WP07.
* **Heartbeat:** `deviceDiagnosisHeartbeatData` (timeout as an ISO 8601 duration, counter,
  timestamp), sent to the subscribers; the default check window of eebus-go is 2 minutes
  (`IsHeartbeatWithinDuration`).
* **Use case announcement:** `nodeManagementUseCaseData` with `useCaseInformation[]`
  (actor + useCaseSupport[]: name, version, available, scenarioSupport[], documentSubRevision).

**Golden files:** `libs/spine-go/spine/testdata/*.json` (node management discovery,
subscriptions, destination list) and `libs/spine-go/integration_tests/testdata/*.json`
(electrical connection, measurement, partial notifies) — in ordinary JSON; the EEBUS JSON
transformation happens on the SHIP level only. These files are used one to one as fixtures of
our serialisation tests.
**In addition `libs/devices`:** real `discovery-data.json`/`usecase-data.json` files of series
devices (Porsche PMCC, Elli, SMA, Kostal, Vaillant, Viessmann, …) — mandatory fixtures for
parser robustness (among other things older `specificationVersion` 1.1.x/1.2.x!) and the basis
of the device replay mode (§ 5).

### 2.3 The use case layer (eebus-go)

`usecases/usecase/usecase.go` (**UseCaseBase**) encapsulates the actor (`UseCaseActorType`),
the use case name, version and sub revision, the scenarios (number, mandatory, the server
features required at the other side), the valid remote actor and entity types, and the event
callback; it registers the use case within `useCaseData`, checks the compatibility when a
remote entity appears, and raises events.

Relevant for e-mobility (actor → package in eebus-go):

| UC | Name (SPINE) | Actors (client ↔ server) | Scenarios (the core) |
|---|---|---|---|
| **LPC** | `limitationOfPowerConsumption` | EnergyGuard (`eg/lpc`) ↔ ControllableSystem (`cs/lpc`) | 1 power limit (LoadControl, obligation, W), 2 failsafe values (DeviceConfiguration: `failsafeConsumptionActivePowerLimit`, `failsafeDurationMinimum`), 3 heartbeat (DeviceDiagnosis), 4 constraints (`ElectricalConnectionCharacteristic` → `ConsumptionNominalMax`) — the basis of §14a EnWG |
| **LPP** | `limitationOfPowerProduction` | EnergyGuard ↔ ControllableSystem | the mirror image for production (PV and storage, §9 EEG) |
| **MPC** | `monitoringOfPowerConsumption` | MonitoringAppliance (`ma/mpc`) ↔ MonitoredUnit (`mu/mpc`) | 1 active power (mandatory), 2 energy, 3 current per phase, 4 voltage, 5 frequency (Measurement + ElectricalConnection) |
| **MGCP** | `monitoringOfGridConnectionPoint` | MonitoringAppliance ↔ GridConnectionPoint (`gcp/mgcp`, `ma/mgcp`) | the grid connection point: consumption and feed-in, energy, current/voltage/frequency + the PV curtailment factor |
| **OPEV** | `overloadProtectionByEvChargingCurrentCurtailment` (v1.0.1b) | per the specification: **Energy Guard** ↔ EV (eebus-go announces the client actor as "CEM" — cover both in the catalog!); only one Energy Guard per EV | 1 "Energy Guard curtails charging current of EV" (LoadControl obligation per phase, A; minimum and maximum from `ElectricalConnectionPermittedValueSet`), 2 "EV checks Energy Guard availability" (heartbeat), 3 "Energy Guard sends error state" (`deviceDiagnosisStateData`). **Latency budgets:** submeter/EG <= 2 s, EG→EV <= 1 s, EV <= 2 s (specification chapter 2.1) — timing assertions for conformance and the simulations |
| **OSCEV** | `optimizationOfSelfConsumptionDuringEvCharging` | CEM (`cem/oscev`) ↔ EV | current **recommendations** (LoadControl category `recommendation`) for charging from PV surplus |
| **EVCC** | `evCommissioningAndConfiguration` | CEM (`cem/evcc`) ↔ EV | identification, communication standard (ISO 15118 / IEC 61851), asymmetric charging, manufacturer data, sleep/standby |
| **EVSECC** | `evseCommissioningAndConfiguration` | CEM (`cem/evsecc`) ↔ EVSE | manufacturer data, operating state and failures (DeviceDiagnosis) |
| **EVCEM** | `measurementOfElectricityDuringEvCharging` | CEM (`cem/evcem`) ↔ EV | current, power and energy per phase while charging |
| **EVSOC** | `evStateOfCharge` | CEM (`cem/evsoc`) ↔ EV | state of charge (Measurement) |
| **CEVC** | `coordinatedEvCharging` | CEM (`cem/cevc`) ↔ EV | charging planning: `TimeSeries` (constraints and plan), `IncentiveTable` (tariffs and incentives) |
| **EVCS** | `evChargingSummary` (v1.0.1) | **Energy Broker** → EVSE (+ EV via EVCC/EVCEM) | scenario 1: "Energy Broker sends Charging Session Summary to EVSE" (billing and session data, the Bill feature). **Not in eebus-go** → implemented purely from the specification (`docs/specs/E-Mobility/Technical Specifications/EEBus_UC_TS_EVChargingSummary_V1.0.1.pdf`); low priority, but a unique selling point |

A memory aid for feature reuse: **LoadControl** (LPC/LPP/OPEV/OSCEV),
**Measurement + ElectricalConnection** (MPC/MGCP/EVCEM/EVSOC/VABD/VAPD),
**DeviceConfiguration** (LPC failsafe, EVCC), **DeviceDiagnosis** (heartbeat and state
everywhere), **TimeSeries + IncentiveTable** (CEVC), **Identification** (EVCC).

---

## 3. Target architecture (C#)

### 3.1 Projects and namespaces (the two repository model)

**Stack → `libs/WWCP_EEBUS`** (commit there; namespaces as they already are,
`cloud.charging.open.protocols.EEBUS.SHIP` / `.SPINE`, newly `.UseCases`):

```
WWCP_EEBUS/
├── WWCP_EEBUS_SHIP/               # SHIP: messages, EEBUSJSON, state machines, mDNS, PKI, SHIPNode
├── WWCP_EEBUS_SHIP_Tests/         # NUnit (placeholders today → WP01/02/03/04/05)
├── WWCP_EEBUS_SPINE/              # SPINE: model (XSD generated), core, node management
├── WWCP_EEBUS_SPINE_Tests/        # NUnit (+ golden files from spine-go and the example XMLs)
├── WWCP_EEBUS_UseCases/           # NEW: UseCaseBase + LPC/LPP/MPC/MGCP/OPEV/… (+ _Tests)
├── WWCP_EEBUS_Adapter/            # NEW (later): the bridge to WWCP_Core/OverlayNetworking
└── WWCP_EEBUS.sln
```

**Test bench → this repository** (references the stack projects through the submodule path):

```
EEBUSConformanceTests/
├── libs/                          # submodules (including WWCP_EEBUS!)
├── EEBUSSimulations/              # the simulation library (§ 5)
├── Apps/EEBUSCLI/                 # the console runner (sim …, conformance …; Styx.CLI)
├── Tests/
│   ├── EEBUSConformance_Tests/    # the conformance catalog (against a configurable target device)
│   └── EEBUSInterop_Tests/        # orchestrates Go and .NET peers as processes
├── docs/                          # specs/ (not committed), notes, ADRs, reports/
├── EEBUSConformanceTests.sln
├── WORKPLAN.md
└── redo.sh
```

* The csproj style follows `libs/Hermod/Hermod/Hermod.csproj` exactly: `net10.0`,
  `ImplicitUsings`, `Nullable`, `RootNamespace`/`AssemblyName` set; references as
  `ProjectReference` to Styx and Hermod. Keep the package references minimal
  (Newtonsoft.Json and BouncyCastle arrive through Hermod).
* Tests: `NUnit` 4.6+, `NUnit3TestAdapter`, `Microsoft.NET.Test.Sdk`,
  `Microsoft.Extensions.TimeProvider.Testing` (FakeTimeProvider).
* **License (decided 2026-07-26):** Apache-2.0, the file header template is
  `libs/WWCP_EEBUS/WWCP_EEBUS_SHIP/Messages/ASHIPMessage.cs`
  ("Copyright (c) 2014-… GraphDefined GmbH … This file is part of WWCP EEBUS").
  New files in this repository use the same header (with the project name adjusted).

### 3.2 Layers and core abstractions

```
┌────────────────────────────────────────────────────────────────┐
│ Simulations / EEBUSCLI / conformance tests                     │
├────────────────────────────────────────────────────────────────┤
│ EEBUS.UseCases   UseCaseBase, LPC/LPP/MPC/MGCP/OPEV/OSCEV/…    │
│                  + reusable feature helpers                    │
├────────────────────────────────────────────────────────────────┤
│ EEBUS.SPINE      SPINEDevice (local/remote), entity, feature,  │
│                  node management, subscription/binding/        │
│                  heartbeat managers, sender, data model        │
├────────────────────────────────────────────────────────────────┤
│ EEBUS.SHIP       SHIPNode (hub), SHIPConnection (state machine),│
│                  EEBUSJSON, mDNS discovery and announcement,    │
│                  certificates/SKI, trust store/pairing         │
├────────────────────────────────────────────────────────────────┤
│ Hermod           WebSocketClient/AWebSocketServer, DNS, TLS    │
│ Styx             Illias helpers, CLI                           │
└────────────────────────────────────────────────────────────────┘
```

Important interfaces (mirroring the ship-go and spine-go APIs, adapted to C# idioms):

* `ISHIPNode` (≈ the ship-go `HubInterface`): `StartAsync/ShutdownAsync`,
  `RegisterRemoteService`, `SetAutoAccept`, `CancelPairing`, `DisconnectService`, events
  (`OnRemoteServiceConnected/-Disconnected`, `OnVisibleServicesUpdated`,
  `OnPairingDetailUpdate`, the `AllowWaitingForTrust` callback). **Important:** remote services
  must be registrable **without mDNS** as well, by an explicit `ws(s)://host:port/path` address
  (for CI and conformance test rigs!).
* `ISHIPConnectionDataReader/-Writer` — the handover point between SHIP and SPINE (raw SPINE
  payloads).
* `ISPINEDeviceLocal/-Remote`, `IEntityLocal/-Remote`, `IFeatureLocal/-Remote`, `ISender`,
  `IEventBus` (≈ the spine-go `api/`).
* `IUseCase`, `UseCaseBase` (≈ the eebus-go `usecases/usecase`).
* **Every** time dependent component receives a `TimeProvider` through its constructor (see § 9).

### 3.3 JSON strategy

* **Newtonsoft.Json** (the house standard of Hermod; `JObject`/`JProperty` preserve the
  insertion order — a precondition for EEBUS JSON).
* SPINE model: C# classes with `JsonProperty(..., NullValueHandling.Ignore)` attributes (field
  names exactly like the Go `json:` tags), plus a few custom converters: ISO 8601 durations
  (`DurationType` ↔ `TimeSpan`), `AbsoluteOrRelativeTimeType`, hex binary, and the numeric
  `ScaledNumberType`.
* The **EEBUSJSON** class within `EEBUS.SHIP`: a structural, lossless conversion between
  ordinary JSON and the EEBUS array form (recursive; the `{}`↔`[]` special case; trimming
  `0x00` bytes when reading). Property based tests: a roundtrip over all golden files.
* The public envelope types (SHIP messages, the SPINE datagram) additionally follow the
  GraphDefined style with `TryParse(JObject, out T, out String? errorResponse)` / `ToJSON()`.

---

## 4. Work packages

> Effort: S (<= half a day for an agent), M (~1 day), L (several days), XL (more than a week,
> or to be split). Every work package ends with: green tests (`dotnet test`), no warnings, a
> short piece of documentation in `docs/`.
> **WP01–WP09 deliver into `libs/WWCP_EEBUS`** (submodule commits, pushed after approval),
> WP00 and WP10–WP13 into this repository.

### WP-W0 — refactoring WWCP_EEBUS *(M; first, together with WP00)* ✅ **done**

> **Result:** the branch `wp-w0-refactoring` within the submodule, everything staged (the
> commit is waiting for Achim's GPG signature). `dotnet build WWCP_EEBUS.sln` is green without
> warnings in our own sources, `dotnet test` is green: 11 SHIP + 1 SPINE + 1 use case test.
> Implemented in addition to the table below: `generated.cs` deleted (it carried a wrong AGPL
> header from WWCP_ChargingStation; the PIN types it contained arrive properly in WP01), the
> `Version` classes decoupled (before, SHIP and SPINE declared the **same** class
> `EEBUS.Version` → a conflict as soon as both are referenced), and two nullability analysis
> errors after removing CustomData fixed.

An inventory of `libs/WWCP_EEBUS` (2026-07-26) and the refactoring tasks:

| Existing code | Assessment → action |
|---|---|
| `WWCP_EEBUS_SHIP/DataStructures/` (Complex, Enums, `PredefinedStrings/` such as `SHIP_Id`, `ConnectionHelloPhase`) | **Keep and extend** — the PredefinedStrings style (extensible string types instead of C# enums) matches the XSD pattern `EnumExtendType` exactly (§ WP06). Add the missing types: `ConnectionPinState/-Input/-Error`, `AccessMethodsRequest`, the waiting/prolongation fields per WP01 |
| `Messages/ASHIPMessage.cs` + the control/data/close messages | **Keep, but:** remove `CustomData` — an OCPP concept; SHIP messages have a fixed schema (only `ShipData` has an `extension`), and wire fidelity is mandatory for conformance. Keep the TryParse/ToJSON pattern |
| `EEBUSAdapter/` (IN/OUT/FORWARD), `AEEBUSNode.cs`, `IEEBUSNetworkingNode.cs` | **Move out** into `WWCP_EEBUS_Adapter/` (a new project, for now not part of the solution): it depends on `WWCP_Core`/`WWCP_OverlayNetworking`, which are not available here. To be revived once the core stands |
| `IOCPPWebSocketAdapterIN/OUT/FORWARD.cs`, `generated.cs` | **Delete** (leftovers copied from OCPP), or inspect and delete |
| `WebSocket/SHIPWebSocketClient/-Server.cs` | **Rebuild** on a plain Hermod foundation (sub protocol `ship`, TLS and client certificates from WP02); the state machine is attached in WP04/05 |
| `WWCP_EEBUS_SHIP.csproj` | **Remove** the `WWCP_Core` and `WWCP_OverlayNetworking` references; Styx and Hermod only (the paths `..\..\Styx\…`, `..\..\Hermod\…` already work within the `libs/WWCP_EEBUS` layout) |
| `WWCP_EEBUS_SPINE/` (empty), `*_Tests/` (the placeholder `Class1.cs`) | Set up the projects per § 3.1 (NUnit packages, first smoke tests); **create** `WWCP_EEBUS_UseCases(+_Tests)`; create `WWCP_EEBUS.sln` |

* **Architectural decision (default A, to be confirmed by Achim):**
  (A) the SHIP/SPINE/UseCases core **standalone** (Styx and Hermod only), with the WWCP
  integration as a separate adapter project later ↔ (B) keep the OverlayNetworking foundation
  and carry `WWCP_Core` along as another submodule. The plan assumes **A**.
* **Acceptance:** `dotnet build WWCP_EEBUS.sln` green **inside this repository** (with only
  `libs/Styx` and `libs/Hermod` as neighbours); both test projects run; the commit within the
  submodule is prepared and pushed after Achim's approval.

### WP00 — the test bench skeleton and CI *(S; in parallel with WP-W0)* ✅ **done**

> **Result:** `EEBUSConformanceTests.sln` with `EEBUSSimulations`, `Apps/EEBUSCLI`,
> `Tests/EEBUSConformance_Tests`, `Tests/EEBUSInterop_Tests`; `dotnet run --project
> Apps/EEBUSCLI -- version` reports SHIP 1.0.1 / SPINE 1.3.0 through the complete reference
> chain. Infrastructure already usable: `TestEnvironment` (finds the repository root, the
> specifications, `libs/devices`, the spine-go golden files; **`Assert.Inconclusive` instead of
> a failure** when licensed specifications or submodules are missing) and `GoToolchain`
> (likewise without Go).
> **An important trick:** `libs/Directory.Build.props` (empty) stops the MSBuild search, so
> that `TreatWarningsAsErrors` from the root `Directory.Build.props` does **not** reach the
> submodules (Hermod/Styx/WWCP_EEBUS) — otherwise the build fails on Hermod's warnings.
> The CI (`.github/workflows/build-and-test.yml`) initialises the submodules **individually**
> instead of using `submodules: recursive`, because `libs/devices` cannot be checked out on
> Windows (§ 9).

* `EEBUSConformanceTests.sln`: the projects from § 3.1 (EEBUSSimulations, EEBUSCLI,
  EEBUSConformance_Tests, EEBUSInterop_Tests) + a `ProjectReference` into the
  `libs/WWCP_EEBUS` projects.
* `Directory.Build.props`: net10.0, LangVersion latest, Nullable, TreatWarningsAsErrors,
  shared versions. `.gitignore`: among others `docs/specs/`.
* GitHub Actions: submodules, `dotnet build` + `dotnet test` (Windows + Linux); a second job
  installs Go for the interoperability tests (`actions/setup-go`).
* An `.editorconfig` modelled after the Hermod style.
* **Acceptance:** the CI is green on both operating systems (the interoperability category
  excluded for now).

### WP01 — the SHIP message model and EEBUSJSON *(M; only WP00 needed)* ✅ **done**

* Port `libs/ship-go/model/model.go` + `model/types.go` into `WWCP_EEBUS_SHIP/Messages/`
  (ConnectionHello, MessageProtocolHandshake(+Error), ConnectionPinState/…,
  AccessMethodsRequest/AccessMethods, ShipData, ConnectionClose; the
  `ShipMessageExchangeState` enumeration with the same values 0–39, so that log data can be
  compared).
* `EEBUSJSON` (see § 3.3) including its tolerance modes.
* Framing: `SHIPFrame.TryParse(ReadOnlyMemory<byte>)` / `.ToBytes()` (type byte + payload).
* **Tests:** golden roundtrips for every message; the EEBUS JSON examples from § 2.1;
  fuzz-like robustness (empty, only the type byte, more than 100 KiB, invalid UTF-8, a `0x00`
  suffix).

### WP02 — certificates, SKI, the TLS profile *(M; in parallel with WP01)* ✅ **done**

* `SHIPCertificates`: creation of self-signed ECDSA P-256 certificates **with a subject key
  identifier** (BouncyCastle through Hermod, or
  `System.Security.Cryptography.X509Certificates.CertificateRequest` — prefer the latter, use
  BouncyCastle only where necessary); SKI extraction and normalisation (reference
  `libs/ship-go/cert/cert.go`, `fingerprint.go`; the presentation format with and without
  spaces).
* The TLS configuration of both roles: the required and optional cipher suites, the TLS 1.2/1.3
  behaviour, `RemoteCertificateValidation` = "accept every certificate, the identity is the
  SKI, trust is decided by the hello phase and the trust store".
* **Note for Windows:** `CipherSuitesPolicy` is **not** supported by SChannel → fall back to
  the operating system defaults on Windows (the GCM suite is available there); on Linux set
  both suites explicitly — **together with the TLS 1.3 suites**, otherwise .NET rejects every
  connection as long as TLS 1.3 is enabled. Record the risk and the fallback (BouncyCastle
  TLS) in `docs/adr/0001-tls-cipher-suites.md`.
* **Tests:** a certificate roundtrip, the SKI of known certificates (fixtures from the ship-go
  tests), a TLS handshake C# ↔ C# with a mandatory client certificate; cipher suite
  verification by inspecting the `SslStream`.

### WP03 — mDNS / DNS-SD for `_ship._tcp` *(L; in parallel)* ✅ **done**

* An inventory of the Hermod DNS: `DNS/Server/DNSServer.cs` (UDP multicast available),
  `DNSServiceInstanceName`, the SRV/PTR/TXT/A/AAAA records. Add what is missing (within
  Hermod, as an extension fit for a pull request, or for now within
  `WWCP_EEBUS_SHIP/Discovery/`):
  * **Responder:** announce and unannounce `<instance>._ship._tcp.local` (PTR, SRV, TXT,
    A/AAAA), including goodbye packets (TTL 0), with minimal probing and conflict handling.
  * **Browser:** a continuous search and resolution, with events on add/update/remove; TXT
    parsing into `SHIPServiceTXT` (ski, id, path, register, brand, model, type, serial, cat).
* Document the Windows peculiarity: port 5353 is shared with the responder of the operating
  system (`SO_REUSEADDR`); the CI has no multicast → exclude the discovery tests with
  `[Category("LocalNetwork")]` and build a loopback test mode (responder and browser
  in-process).
* **Tests:** record encoding against known bytes; an in-process announce → browse roundtrip;
  TXT parser robustness (missing keys, upper and lower case).
* **Interoperability check (manual, local):** make `libs/ship-go/examples/quickstart` visible ↔
  find it.

### WP04 — the SHIP connection: the state machines *(XL → split into 4a–4d; needs WP01 + WP02)* ✅ **done**

The heart of it. One `SHIPConnection` per WebSocket, role `Client|Server`, the complete SME
state machine:

* **4a CMI** (`hs_init.go`): the 10 s timeout, byte validation, the role logic.
* **4b hello** (`hs_hello.go` and both of its test files!): ready/pending/aborted, `waiting`,
  prolongation (requested at "waiting − 30 s", the minimum values, the
  `AllowWaitingForTrust` callback, an abort below 1 s), the timers exactly as in § 2.1.
* **4c** the protocol handshake (`hs_prot.go`), PIN `none` (`hs_pin.go`), access methods
  (`hs_access.go`).
* **4d** the data phase and the close handshake (`connection_messaging.go`,
  `connection_lifecycle.go`): forwarding the SPINE payload through
  `ISHIPConnectionDataReader`, `connectionClose` announce/confirm with `maxTime`, the WebSocket
  ping/pong keepalive, a clean teardown.
* Every timer runs through `TimeProvider.CreateTimer`; the state machine is an explicit
  `switch` machine using the same state values as ship-go (so that logs can be compared!).
* **Tests (with FakeTimeProvider!):** every ship-go state machine test (`hs_*_client_test.go`,
  `hs_*_server_test.go`, `handshake_timer_*`) gets a C# counterpart: the timeout paths, series
  of prolongations, an abort in every phase, unexpected messages, a double close. An in-memory
  transport stub instead of a real socket.

### WP05 — SHIPNode (hub), pairing and the trust store *(L; needs WP03 + WP04)* ✅ **done**

* `SHIPNode`: a WebSocket server (Hermod `AWebSocketServer`; enforce the sub protocol `ship`,
  path routing, TLS with a mandatory client certificate) plus outgoing connections (Hermod
  `WebSocketClient` with `SecWebSocketProtocols=["ship"]` and a client certificate).
* Management: the known remote services (by SKI; from mDNS and/or manual registration), the
  connection registry, the **double connection rule** (§ 2.1), the reconnect backoff, the
  pairing states (≈ the ship-go `api/connectionstate.go`:
  `None/Queued/Initiated/ReceivedPairingRequest/Pin?/Trusted/…`), a persistent trust store
  (a JSON file; an interface for other backends).
* Events towards the application (≈ the `HubReaderInterface`).
* **Tests:** a full handshake C# client ↔ C# server, in memory and over real sockets
  (localhost, throwaway certificates); a double connection in both directions; a rejected
  trust decision; a reconnect after a kill.
* **First interoperability milestones:** `dotnet EEBUSCLI ship-listen` ↔
  `go run ./examples/quickstart` (from `libs/ship-go`) in both directions, up to
  `SmeStateComplete`.

### WP06 — the SPINE data model *(XL → parallelisable; needs WP-W0)*

The goal: the complete SPINE 1.3.0 model within `WWCP_EEBUS_SPINE/Model/`. **The primary
strategy is code generation from the 76 official XSDs** (for the path see appendix B, "SPINE
XSDs"); spine-go serves as the oracle for the JSON field names and behavioural details.

* **6a the generator (first, M):** ✅ **done** — see `docs/adr/0002-spine-model-generation.md`
  for what was decided and why. Regenerate with `dotnet run --project Apps/EEBUSModelGen`
  (`--list` reports without writing). Two rules turned out to matter more than expected:
  an anonymous type declared inline is the named type it restricts (119 properties were
  silently typed as `String` before that was handled — the fixture comparison could not see it,
  the roundtrip over the recorded datagrams did), and `SPINEJSON` has to be used for reading and
  writing, because `DateParseHandling` mangles every timestamp and an empty list arrives as
  `{}`. WP06c added one thing to the generator: the `eebus:"writecheck"` marks, which say
  whether a remote peer may change a data type at all (S7 in `docs/spec-deviations.md`).
  <br>The original plan for 6a, for reference: a small tool `Apps/EEBUSModelGen` (in this repository,
  because the XSDs live here): it parses the XSDs (`System.Xml.Schema`/`XmlSchemaSet`) and
  generates one C# file per resource, following fixed rules:
  * `xs:simpleType` enumerations with an `EnumExtendType` union → **extensible string types**
    in the existing `PredefinedStrings` style of WWCP_EEBUS (no C# `enum`! Unknown values have
    to remain transportable);
  * complex types → classes with nullable properties + `JsonProperty` (camel case like the XSD
    element names; the comparison against the spine-go `json:` tags is a generator test);
  * generate the `…ElementsType`/`…SelectorsType`/`…ListDataType` mechanically as well;
  * the function metadata (function name ↔ type ↔ selectors ↔ elements, the primary keys from
    `PRIMARYKEY_TAG_GUIDELINES.md`) as a generated registry
    (`[EEBUSFunction("loadControlLimitListData", …)]`).
  * The generated code is **checked in** (the generator runs on demand, not during the build).
* **6b the core review (M):** ✅ **done** — the generated types are `partial`, so the manual
  sharpening happens **next to** them under `WWCP_EEBUS_SPINE/Additions/` and never by editing
  generated code. Covered: CommonDataTypes (ScaledNumber, the three address types,
  TimePeriod, PossibleOperations, ElementTag), CommandFrame (Cmd/Filter/CmdControl reaching
  every function through the generated metadata), Datagram (the one-line overview), Result (the
  error numbers, which the XSD declares as a bare `xs:unsignedInt`) and UseCaseInformation.
  <br>The decision which came out of it: generated types serialise **opt-in**
  (`[JsonObject(MemberSerialization.OptIn)]`). Additions are ordinary public properties, and
  without that every one of them would have gone into the next datagram — `ScaledNumberType`
  was serialising `"Value"` next to `"number"` and `"scale"` before it was noticed.
  <br>Left for their own work packages, because they are logic rather than data:
  NodeManagement/NetworkManagement detailed discovery belongs to **WP07b**, subscription and
  binding management to **WP07c**. The names of the actors and use cases stay plain strings
  here — which use cases exist is decided by the use case specifications, so the constants
  belong to **WP08**.
* **6c the update system (M):** ✅ **done** — see `docs/adr/0003-spine-update-system.md`.
  `WWCP_EEBUS_SPINE/Update/` holds one generic engine (`SPINETypeInfo`, `SPINESelectors`,
  `SPINEElements`, `SPINEUpdate`, `SPINERead`) driven by the `[EEBUSKey]`, `[EEBUSWriteCheck]`
  and `[EEBUSFunction]` marks of the generated model — no code per function, and none per list
  type either (spine-go needs about forty hand-written `*_additions.go` files for that).
  <br>What turned out to matter: the **specification decides here, not spine-go** (three
  differences, S4–S6 in `docs/spec-deviations.md`, each held by a test) — the update semantics
  are a statement about what a message *means*, and agreeing with somebody else's misreading is
  not compatibility. A stated element **replaces** rather than merges, which the specification's
  own `W-M-Y_1-1-02.xml` decides: it modifies a value of 105·10⁻¹ by sending nothing but
  `<number>14</number>`, and § 5.3.4.7.1 lets the omitted scale fall back to its default, so the
  answer is 14. And nothing is changed in place, which is what makes § 5.3.4.2 ("a server shall
  only execute a restricted write if it can execute it completely") implementable at all.
  <br>All 29 official `ExampleXMLs/RestrictedFunctionExchange/` datagrams are fixtures: read by
  the model, written back unchanged, put through the EEBUS JSON transformation of SHIP TS
  chapter 11 and back (which also gave WP01's `EEBUSJSON` 29 specification documents to prove
  itself against), and applied to a defined state. `SPINEExampleXML` converts the XML **along
  the model**, because an element which occurs once cannot be told from a list of one entry
  without the schema. Two of the examples close a loop the specification opened itself: the
  answer of `SPINERead` to the read `RD-P-Y_1-2-01` is compared with the reply `RY-P-Y_1-1-01`
  which the specification ships for it.
* **Tests:** a serialisation roundtrip of all golden files from `spine-go/spine/testdata` and
  `integration_tests/testdata`; a completeness check of the field names by reflection against a
  name list generated from the Go tags (a one-off helper script, its result checked in as a
  JSON fixture). In addition: parse **all** `discovery-data.json`/`usecase-data.json` files
  from `libs/devices` without errors (the fixture import copies them into
  `Tests/…/TestData/RealDevices/<brand>_<model>/` with Windows-safe names; extract the two
  Vaillant files via `git show`, see § 1.1).

### WP07 — the SPINE core *(XL → 7a devices/features, 7b node management, 7c managers; needs WP06a)*

A port of the logic from `libs/spine-go/spine/`:

* **7a:** `DeviceLocal/Remote`, `EntityLocal/Remote`, `FeatureLocal/Remote`, the function data
  store with partial updates, the `Sender` (msgCounter, request tracking, deduplication like
  `send.go`), the routing in `ProcessCmd` including the error `result`.
* **7b:** node management completely (detailed discovery including the entity add/remove
  notifies, subscriptions, bindings, use case data, the destination list).
* **7c:** the SubscriptionManager, the BindingManager (write permissions only with a binding!),
  the HeartbeatManager (sending and monitoring, on a TimeProvider), the event bus (core versus
  application level).
* **Tests:** counterparts of `spine-go/spine/*_test.go` (very extensive there); an in-memory
  pair LocalDevice ↔ LocalDevice over a loopback `ISHIPConnectionDataWriter`; golden datagrams
  byte by byte (with a deterministic msgCounter through an injectable counter).

### WP08 — the use case framework *(M; needs WP07)*

* `UseCaseBase` (≈ `eebus-go/usecases/usecase/usecase.go`): scenario declaration,
  `AddFeatures()`, `IsCompatibleEntityType`, recognition of remote use cases (comparing
  `useCaseData`), events (`UseCaseSupportUpdate`, the data update enumerations per use case),
  registering with and unregistering from the event bus.
* **The use case discovery version rules** (chapter 2.4 of every use case specification, e.g.
  the EVCS PDF): the SHALL rules for evaluating `useCaseVersion` (choose the highest compatible
  version within the same major version, the behaviour on a major version mismatch, the
  semantics of `useCaseAvailable`) and the tolerance rules (ignore unknown elements and
  scenarios instead of aborting) — these belong into the UseCaseBase generically, not into the
  individual use cases.
* Port the reusable feature helpers (`eebus-go/features/client` + `/server`): LoadControl,
  Measurement, ElectricalConnection, DeviceConfiguration, DeviceDiagnosis,
  DeviceClassification, Identification, TimeSeries, IncentiveTable.
* **Tests:** use case registration → a correct `nodeManagementUseCaseData`; the compatibility
  matrix.

### WP09 — the use cases *(L in total; S–M each, highly parallelisable; needs WP08)*

In order of priority:

1. **LPC** on both sides (`cs/lpc` + `eg/lpc`) — including the pending approval mechanism
   (`PendingConsumptionLimits`/`ApproveOrDeny…`), the failsafe values, the heartbeat fallback
   behaviour, the nominal maximum characteristic. **References:** § 2.3 and the eebus-go
   `public.go`, and now normatively
   `Grid/Technical Specifications/EEBus_UC_TS_LimitationOfPowerConsumption_V1.0.0_public.pdf`
   as well as the practical guide
   `Grid/Implementation Guides/EEBus_UC_IG_LimitationOfPowerConsumption_V1.1.0.pdf`
   (the §14a rollout details); the test cases of the high level test specification LPC V1.0.2
   flow into WP11.
2. **OPEV** (`cem/opev` and the specification `EEBus_UC_TS_OverloadProtection…V1.0.1b.pdf`)
   plus the counterpart on the "EV side" (not present as a server use case in eebus-go — we
   build **both** sides, the EV server side following chapter 3 of the specification, and
   validate the behaviour against `examples/evse` and `hems`). Mind the actor nuance
   (specification: Energy Guard, eebus-go: CEM — § 2.3).
3. **MPC** (`mu/mpc` + `ma/mpc`).
4. **LPP**, **MGCP**.
5. **EVSECC**, **EVCC**, **EVCEM**, **EVSOC** (the specifications V1.0.1 respectively EVSOC
   V1.0.0 RC1 are available in `docs/specs/` — take the scenario and feature tables from there).
6. **OSCEV** (specification V1.0.1b), **CEVC** (specification V1.0.1; TimeSeries and
   IncentiveTable — the most demanding data models).
7. **EVCS** (EV charging summary, specification V1.0.1; optional): the only use case without a
   Go reference — purely specification driven (the Bill feature), and therefore a good stress
   test of our specification-to-code pipeline.
* **Tests per use case:** scenario walkthroughs over an in-memory device pair; data flow
  assertions (e.g. LPC: write a limit → a notify at the energy guard; a heartbeat failure → a
  failsafe event).

### WP10 — the e-mobility simulations *(L; needs WP09/1–3; details in § 5)*

### WP11 — the conformance test suite *(L; grows along from WP01 on)*

* **The catalog is the official test specifications** (§ 1.2, available locally!). Our NUnit
  tests adopt the **official test case identifiers** one to one (e.g.
  `[Property("TC", "TC_SHIP_CMI_003")]`, with the method name
  `TC_SHIP_CMI_003_ApplyCmiTimeout_Server`):
  * `TC_SHIP_*` from `EEBus_SHIP_TestSpecification_V1.0.0` — the groups: MDNS (the TXT record),
    CONN (double connections and the SKI), ROLE (server/client/polymorphism), SEC (spoofed
    certificates), MSG (the message structure, whitespace tolerance), CMI, HELLO (including the
    prolongation cases), PROT, PIN, TERM (close), AM/AMDATA (access methods).
  * `TC_SPINE_*` from `EEBus_SPINE_TestSpecification_V1.0.0` — the groups: COMP (tolerate
    unknown functions and elements, version formats), DATA (msgCounter monotonicity and
    overflow, acknowledgements, `msgCounterReference`), FC (primary node management), DDISC
    (detailed discovery, disconnecting a silent partner), BIND (deny a node management binding,
    reject a write without a binding), SUBS (idempotent deletion), ENTITY (dynamic entities),
    RTS/RTC (the server and client tolerance rules, the RFE merge with `ScaledNumberType`).
  * The use case level: the **high level test specifications V1.0.2** for LPC/LPP/MGCP/MPC
    (`Grid/Test Specifications/`), and for pairing
    `EEBus_SHIP_Pairing_Service_TestSpec_V1.0.0` (WP14).
  * The requirement ↔ test case mapping (chapter 3 of every test specification) is adopted as a
    fixture table — the report then shows the coverage per requirement.
* **The parameter sheets:** the official `*_ParameterSheet_*.xlsx` files define the parameters
  of the device under test (the PAR_ blocks, the variable registry, the address conventions —
  SPINE test specification chapter 2.5). Our conformance runner uses the same parameter
  structure (a JSON configuration with the field names of the sheet; an XLSX import can follow
  later).
* Our own additional cases beyond the official specifications (e.g. the device quirks from § 9,
  ship-go behavioural details, EEBUS JSON robustness) carry the prefix `TC_OCC_*`
  (OpenChargingCloud) — cleanly separated from the official catalog.
* Runnable against: (a) our own stack (a self test), (b) **any external device** (a
  configuration file: the target by mDNS filter or host and port, our own SKI and certificate,
  auto accept, the role client or server) — which is the very purpose of this repository!
* Negative tests need a **scriptable "misbehaving mode"** of our own SHIP state machine (e.g.
  "send a hello pending without waiting", "do not answer the CMI") → provide test hooks in
  `SHIPConnection` (internal + `InternalsVisibleTo` for the test projects).
* Report: the NUnit result plus generated Markdown/HTML (`docs/reports/`) with the catalog
  identifier ↔ result.

### WP12 — the interoperability harness *(L; needs WP05 for the SHIP part, WP09 for the use case part)*

* `EEBUSInterop_Tests` and the helper class `GoPeer`: it starts the Go examples as processes
  (`go run ./examples/…` with pre-generated certificates and SKIs, fixed ports, `-autoaccept`
  flags; the working directory is the respective submodule), waits for log markers, and cleans
  up reliably (killing the process tree, waiting for the port). All of them with
  `[Category("Interop")]`.
* The matrix (both directions client/server wherever possible):

  | Peer | Level | Scenario |
  |---|---|---|
  | `ship-go/examples/quickstart` respectively `client` | SHIP | the handshake up to complete, close, a double connection |
  | `eebus-go/examples/evse` ↔ our CEM | SPINE + use cases | discovery, EVSECC/EVCC, the OPEV limits |
  | `eebus-go/examples/hems` ↔ our EVSE/EV simulation | SPINE + use cases | the same from the other direction |
  | `eebus-go/examples/controlbox` ↔ our controllable system | use case LPC | limit/failsafe/heartbeat (the §14a chain) |
  | `eebus-go/examples/ced` ↔ our energy guard | use case LPC | the same from the other direction |
  | EVCC (a binary via Docker, with an EEBUS configuration) | end to end | our CS-LPC as a "controllable device" attached to the EVCC HEMS |
  | `EEBUS.Net` (dotnet) | SHIP | a foreign C# peer, the SHIP handshake |
  | `libs/devices-app` (`go run .` → web UI :7050, EEBUS :4815) | SHIP + SPINE | GUI pairing against our server; a manual visual check of our discovery and use case data (semi-automatic: start the app headless, pair through REST?) |
  | `openeebus` (C, optional) | SHIP/SPINE | a stretch goal |

* CI: a Linux job with Go; prefer direct connections without mDNS (controlbox supports
  `pairingTargets`/`remoteSKIs`; otherwise use our side as the server and the Go side as the
  client).

### WP13 — documentation, examples, packaging *(M; ongoing)*

* `README.md` (the purpose of the repository, a quickstart, an architecture picture), `docs/`
  (maintain this digest, the ADRs, the test catalog), XML documentation comments in the Hermod
  style, NuGet packaging of the three core projects.

### WP14 — the SHIP pairing service / installation process *(L; optional, after WP05)*

* The QR payload format, the HMAC keyring, the semantics of the `register` TXT key, the
  15 minute AddCu logic. **The normative sources are available locally**
  (`docs/specs/SHIP SPINE/Technical Specifications/`):
  `EEBus_SHIP_Pairing_Service_TS_Specification_V1.0.0.pdf` and
  `EEBUS_TS_ShipRequirementsForInstallationProcess_V1.1.0.pdf` (v1.1.0 — first review the
  deltas against the 1.0.0 implemented by ship-go!), together with the official
  **pairing test specification**
  `docs/specs/SHIP SPINE/Test Specifications/EEBus_SHIP_Pairing_Service_TestSpec_V1.0.0/`
  (PDF + parameter sheet) for the connection to WP11; the implementation reference is
  `libs/ship-go/ARCHITECTURE_SHIPPAIRING.md`, `api/shippairing*.go`, `pairing/`.
  Increasingly relevant for §14a rollouts (a smart meter gateway control box ↔ a device).

---

## 5. The e-mobility simulations (WP10 in detail)

Every simulation is a **library plus a CLI verb**
(`EEBUSCLI sim <name> [--script … --speed …]`), runs either interactively (Styx.CLI) or driven
by a script (a JSON scenario with a time axis), and **entirely on a TimeProvider** (which
enables the time-lapse mode `--speed 60` and deterministic tests).

1. **`sim lpc-chain` (the §14a EnWG chain):** an energy guard / control box (EG-LPC) ↔ a
   wallbox (CS-LPC). The script: pairing → discovery → subscriptions and bindings → configure
   the failsafe values (4.2 kW / 2 h) → heartbeats → a limit of 4.2 kW active for 30 minutes →
   a prolongation → its release → simulate a heartbeat failure (`--fault heartbeat`) → observe
   the failsafe activation. The output: an event log and a power time series (CSV).
2. **`sim mpc-meter`:** a wallbox or meter as a monitored unit (MU-MPC, scenarios 1–5) with a
   configurable load profile (sine, step, CSV replay); a CEM as the monitoring appliance which
   checks the notify cadence and the values.
3. **`sim opev-curtail`:** a CEM ↔ an EVSE and EV (the EV entity appears when "plugging in"):
   EVSECC/EVCC commissioning → EVCEM measurement values → OPEV current limits per phase
   (16 A → 6 A → a pause at 0 A → release), including the limits from the permitted value sets
   and the failure state (`SetOperatingState`).
4. **`sim emobility-day` (integration):** a HEMS/CEM coordinates an EVSE (OPEV/OSCEV) and the
   PV surplus (the OSCEV recommendations follow the PV curve) while at the same time receiving
   a §14a limit as a CS-LPC from a simulated control box — this shows the double actor role of
   the CEM (breaking the guard limit down to the EV charging current). This is exactly the
   EVCC scenario found in practice.
5. **`sim device-replay --device porsche/mobile-charger-connect`:** emulates a real device from
   `libs/devices` — the discovery and use case data are answered one to one from
   `discovery-data.json`/`usecase-data.json`. This allows testing CEM implementations (ours and
   others) against "real" wallboxes and heat pumps without any hardware.
6. **Later:** `sim cevc-plan` (charging planning with TimeSeries and IncentiveTable),
   `sim evsoc`.

Every simulation gets an **NUnit smoke test** (FakeTimeProvider, time lapse, assertions on the
key events) — which makes the simulations integration tests at the same time.

---

## 6. The remaining use cases: their influence on the architecture

| Use case family | Building blocks involved | Architectural consequence (to be considered now) |
|---|---|---|
| CEVC (charging planning) | TimeSeries, IncentiveTable, partial writes | keep the update and selector system generic (WP06/07), the time series semantics centrally on a TimeProvider; model `AbsoluteOrRelativeTime` properly |
| OHPCF, the HVAC/DHW family (`ma/mdt`, the configuration use cases) | HVAC, SmartEnergyManagementPs, Setpoint | keep the feature and function registry open (no hard-coded use case list); the model (6a) and the update system (6c) already cover these classes, because neither of them knows a function by name |
| ControlOfBattery / FlexibleLoad / FlexibleStartForWhiteGoods | PowerSequences, TaskManagement, ActuatorLevel | the most complex choreographies — not for version 1, but the sender and the state machine must not contain any use case assumptions |
| MonitoringOfBattery/Inverter/PVString, VABD/VAPD | Measurement, ElectricalConnection | the same infrastructure as MPC → keep the feature helpers reusable (no use case specific special paths within Measurement) |
| IncentiveTableBasedPowerConsumptionManagement | IncentiveTable at the grid connection point | like CEVC; the tariff data types early in the model (6b) |
| MonitoringAndControlOfSmartGridReadyConditions | SupplyCondition, Threshold | model coverage only |
| **The new grid use cases: PowerDemandForecast, PowerEnvelope, TimeOfUseTariff (each V1.0.0, `Grid/Technical Specifications/`)** | TimeSeries (forecasts), LoadControl/Setpoint envelopes, IncentiveTable (time of use tariffs) | successors and complements to the §14a world — not in eebus-go yet! An architectural check in WP09: the same feature helpers have to be able to carry envelope and forecast time series; candidates for later unique-selling-point work packages, like EVCS |
| NodeIdentification V1.0.0 (`HVAC/Technical Specifications/`) | Identification | a small use case mechanism for node identification; model coverage only |

The specifications of **all** of the families named above are available locally by now (§ 1.2) —
which makes the architectural statements of this table verifiable against the original PDFs.

**The cross-cutting requirements derived from this** (already planned into WP04–WP08): a generic
function data store with partial updates; reusable server helpers for
LoadControl/Measurement/ElectricalConnection/DeviceConfiguration; a shared heartbeat
infrastructure; an event bus with use case neutral events; persistence interfaces (the trust
store, optionally a ring buffer log like the ship-go `RingBufferPersistence`); and clean
handling of multiple entities (an EV appears dynamically below an EVSE and disappears again!).

---

## 7. Test strategy

1. **Unit tests** (per project): the state machine transitions, serialisation, the manager
   logic. NUnit 4 (`Assert.That`), async tests, passing a `CancellationToken` through, no
   sleeping — time exclusively through `FakeTimeProvider.Advance(...)`.
2. **Golden files:** the SHIP messages (from § 2.1 and the ship-go tests) and the SPINE
   datagrams (from the spine-go `testdata/`) byte by byte respectively structurally; our own
   fixtures below `Tests/**/TestData/`.
3. **In-memory integration:** the complete stack against itself (a loopback transport, one
   FakeTimeProvider for both sides) — fast full scenarios including the use cases.
4. **Socket integration:** localhost with real TLS certificates (freshly created per test run).
5. **Conformance** (WP11): the catalog, against our own stack in the CI; against foreign
   devices through a configuration file.
6. **Interoperability** (WP12): `[Category("Interop")]`, a Linux CI job with Go; locally the
   mDNS cases in addition.
7. **Conventions:** test names `Method_State_Expectation`; one fixture per class; `[Category]`
   for `LocalNetwork`, `Interop`, `Slow`; test data builders instead of copy and paste.

---

## 8. TimeProvider strategy (an inventory)

A constructor parameter `TimeProvider? TimeProvider = null` (defaulting to
`TimeProvider.System`) for every component with a relation to time; **never** access the clock
statically. Points in time as `DateTimeOffset` through `GetUtcNow()`, timers through
`CreateTimer` (→ `ITimer`), backoff through a timer.

| Component | Use of time |
|---|---|
| SHIP CMI/hello/protocol/PIN/access | all handshake timers (10 s / 60 s / prolongation …) |
| SHIP close | the `maxTime` waiting window |
| WebSocket keepalive | the ping interval, the pong timeout |
| SHIPNode | the reconnect backoff, the pairing timeouts, the mDNS TTL and re-announcement |
| SPINE sender | response timeouts, the age of a request |
| HeartbeatManager | the send interval, `IsHeartbeatWithinDuration(2 min)` |
| LPC/LPP | the limit `Duration`, the failsafe `DurationMinimum`, the failsafe activation after losing the heartbeat |
| CEVC/TimeSeries | the slot time axes, relative times |
| Simulations | the entire scenario time axis, the `--speed` factor |
| Conformance tests | provoking timeouts (a FakeTimeProvider in memory, real short timeouts remotely) |

A note: do **not** use the static `Timestamp.Now` of Styx (with its time travel) in new code;
where Hermod APIs deliver timestamps, we convert at the boundary.

---

## 9. Risks and pitfalls (to be addressed early)

1. **TLS cipher suites on Windows:** `CipherSuitesPolicy` is unsupported under SChannel, and
   the CBC suite may be disabled → GCM is enough against ship-go and eebus-go; devices
   insisting on CBC could fail on Windows (the fallback would be BouncyCastle TLS; see the ADR
   from WP02).
2. **mDNS on Windows and in the CI:** the coexistence on port 5353, no multicast in the CI →
   direct addressing as a first class feature (which is practical for conformance tests in a
   lab as well).
3. **The EEBUS JSON order:** the Newtonsoft `JObject` preserves the order — assert it in a
   test; never use a `Dictionary<string,…>` on the wire path.
4. **Device quirks:** the PMCC `0x00` suffix; the KEO stack uses several identical entities
   (there is an eebus-go workaround in `cs/lpc/usecase.go` — choosing the heartbeat source);
   some devices send a `specificationVersion` of 1.2.x — parse tolerantly.
5. **The specifications:** completely available locally (§ 1.2, do not commit them!) including
   the official test specifications. For the conflict rule see § 1.2 (the wire: the Go
   behaviour; the conformance catalog: the specification; document the deviations).
   **SHIP 1.1.0** is available as well: the implementation target remains 1.0.1 (the
   interoperability reality), but read 9.8 "TLS probing" and its siblings once while designing
   the TLS and reconnect behaviour (WP02/WP05), so that 1.1 capability is not blocked.
6. **The size of the model:** port WP06 in a disciplined way, one file at a time plus golden
   tests, otherwise field name errors creep in which only surface during interoperability runs.
7. **Concurrency:** ship-go documents deadlock and race traps (`CONCURRENCY_GUIDE.md` in
   `ship/` and `hub/`) — in C# use channels and `await` instead of locks, and serialise sending
   per connection through a writer queue.
8. **The WWCP_EEBUS refactoring:** have Achim confirm the A/B decision about the `WWCP_Core`
   dependency (WP-W0) before WP01; push the submodule commits only after approval. Removing
   `CustomData` from the SHIP wire types is a breaking change against the previous skeleton —
   intentionally so.
9. **`libs/devices` on Windows:** one Vaillant path contains a `:` and cannot be checked out
   (the submodule stays permanently "dirty", hence `ignore = dirty` in `.gitmodules`); the
   fixture import uses `git show` instead of the file system (see WP06). Linux CI is not
   affected.

---

## 10. Order and parallelisation

```
WP-W0 ─┬─ WP01 ─┬─ WP04 ─── WP05 ──┬─ WP12(SHIP part)
WP00 ──┤        │                  │
       ├─ WP02 ─┘                  │
       ├─ WP03 ────────────────────┤
       ├─ WP06a ─ WP06b/c ─ WP07 ─ WP08 ─ WP09(LPC→OPEV→MPC→…) ─┬─ WP10 ─ WP12(use case part)
       └─ WP11(the SHIP/SPINE catalog, growing from WP01 on)     └─ WP11(the use case catalog)
WP13 ongoing; WP14 optional, after WP05
```

Recommended batches for parallel agents:

* **Batch 1:** WP-W0 + WP00 (one agent each), then WP01 + WP02 + WP03 + WP06a (four agents in
  parallel).
* **Batch 2:** WP04 (one strong agent, 4a–4d sequentially) ‖ WP06b ‖ the preparation of WP07a.
* **Batch 3:** WP05 ‖ WP07 ‖ the WP11 skeleton.
* **Batch 4:** WP08, then the WP09 use cases fanned out (one agent per use case), the SHIP part
  of WP12.
* **Batch 5:** the WP10 simulations, the use case part of WP12, completing WP11, WP13.

**Definition of done (global):** the build has no warnings on Windows and Linux; all tests are
green; new public APIs carry XML documentation; where applicable, the work package has an
interoperability proof in the `docs/reports/` folder; no raw use of `DateTime.Now` or
`Task.Delay` (an analyzer rule from WP00 on).

---

## Appendix A — SHIP timer values (a quick reference)

| Timer | Value | Source |
|---|---|---|
| `cmiTimeout` | 10 s | SHIP 4.2/13.4.3; `ship/types.go` |
| `cmiCloseTimeout` | 100 ms | ship-go |
| `tHelloInit` / `tHelloInc` | 60 s / 60 s | SHIP 13.4.4.1.3 |
| `tHelloProlongThrInc` | 30 s | SHIP 13.4.4.1.3 |
| `tHelloProlongMin` | 1 s | SHIP 13.4.4.1.3 |
| `tHelloProlongWaitingGap` | 15 s | SHIP 13.4.4.1.3 |
| `tAbortDelay` | 1 s | ship-go |
| WebSocket ping | ~30 s (the usual implementation choice) | ship-go `ws/` |
| Reconnect backoff | 0–3 s / 3–10 s / 10–20 s | ship-go `hub/hub_connections_retry.go` |
| Heartbeat check window | 2 min | eebus-go `cs/lpc/public.go` |

## Appendix B — Useful starting points per topic

| Topic | File |
|---|---|
| The SHIP messages, complete | `libs/ship-go/model/model.go` |
| The SHIP state machine states (0–39) | `libs/ship-go/model/types.go` |
| The hello state machine including prolongation | `libs/ship-go/ship/hs_hello.go` |
| The EEBUS JSON transformation | `libs/ship-go/ship/helper.go` |
| The double connection rule | `libs/ship-go/hub/hub_connections_registry.go` |
| The structure of the mDNS TXT record | `libs/ship-go/mdns/mdns.go` (around line 700) |
| Certificate/SKI/cipher suites | `libs/ship-go/cert/cert.go` |
| The SPINE datagram | `libs/spine-go/model/datagram.go` |
| The cmd choice + filters | `libs/spine-go/model/commandframe.go` |
| The partial update system | `libs/spine-go/model/UPDATE_SYSTEM_GUIDE.md` |
| Node management discovery | `libs/spine-go/spine/nodemanagement_detaileddiscovery.go` |
| Heartbeat | `libs/spine-go/spine/heartbeat_manager.go` |
| UseCaseBase | `libs/eebus-go/usecases/usecase/usecase.go` |
| LPC server/client | `libs/eebus-go/usecases/cs/lpc/`, `eg/lpc/` |
| OPEV | `libs/eebus-go/usecases/cem/opev/` |
| MPC server | `libs/eebus-go/usecases/mu/mpc/usecase.go` |
| The service assembly | `libs/eebus-go/service/service.go`, `api/configuration.go` |
| The interoperability examples | `libs/eebus-go/examples/{hems,evse,controlbox,ced}` |
| The SHIP specification 1.0.1 (PDF + XSD) | `docs/specs/SHIP SPINE/Technical Specifications/EEBus_SHIP_TS_Specification_v1.0.1-1/EEBus_SHIP_TS_Specification_v1.0.1/` (chapter 11 = EEBUS JSON, chapter 13.4 = the state machines) |
| The SHIP specification 1.1.0 (delta review) | `docs/specs/SHIP SPINE/Technical Specifications/EEBus_SHIP_TS_Specification_v1.1.0_public/EEBus_SHIP_TS_Specification_v1.1.0_public/` |
| The SPINE XSDs (the code generation source) | `docs/specs/SHIP SPINE/Technical Specifications/EEBus_SPINE_V1.3.0/EEBus_SPINE_V1.3.0_Final_hp/XSDs/` (76 files) |
| The SPINE PDFs | `docs/specs/SHIP SPINE/Technical Specifications/EEBus_SPINE_V1.3.0/EEBus_SPINE_V1.3.0_Final_hp/Documentation/` |
| The official partial update examples | `docs/specs/SHIP SPINE/Technical Specifications/EEBus_SPINE_V1.3.0/EEBus_SPINE_V1.3.0_Final_hp/ExampleXMLs/RestrictedFunctionExchange/` |
| The official test specifications (SHIP/SPINE/pairing) | `docs/specs/SHIP SPINE/Test Specifications/` (each a PDF + a parameter sheet XLSX) |
| The use case high level test specifications LPC/LPP/MGCP/MPC | `docs/specs/Grid/Test Specifications/` |
| The use case specifications grid / e-mobility / HVAC / inverter | `docs/specs/{Grid,E-Mobility,HVAC,Inverter}/Technical Specifications/` |
| The existing C# SHIP code | `libs/WWCP_EEBUS/WWCP_EEBUS_SHIP/` (Messages, DataStructures, PredefinedStrings) |
| The C# style template for TryParse/ToJSON + the license header | `libs/WWCP_EEBUS/WWCP_EEBUS_SHIP/Messages/ASHIPMessage.cs`, `…/DataStructures/Complex/ConnectionHello.cs` |
