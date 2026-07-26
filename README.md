# EEBUS Conformance & Interoperability Tests

Conformance and interoperability test suite for the **EEBUS** protocol family
(SHIP and SPINE), defined by the [EEBUS Initiative e.V.](https://www.eebus.org).

The suite is built around the official EEBUS test specifications and primarily tests
[WWCP_EEBUS](https://github.com/OpenChargingCloud/WWCP_EEBUS) — the EEBUS protocol stack
developed alongside it — but it can be pointed at any EEBUS device or stack.

## What is in here

| Directory | Content |
|-----------|---------|
| `Tests/EEBUSConformance_Tests` | The conformance test catalog, following the official test case identifiers one to one: `TC_SHIP_*`, `TC_SPINE_*` from the two protocol test specifications, and `ATC_LPC_*`, `ATC_LPP_*`, `ATC_MGCP_*`, `ATC_MPC_*` from the four use case high level test specifications — 267 cases in all. Own additional test cases use the prefix `TC_OCC_*`. |
| `Tests/EEBUSInterop_Tests` | Interoperability runs against other EEBUS implementations, started as external peers. |
| `EEBUSSimulations` | Simulations of the common e-mobility use cases (LPC, MPC, OPEV, ...). |
| `Apps/EEBUSCLI` | `eebus` command line runner for simulations and conformance runs. |
| `libs/` | Git submodules: the stack under test, its foundations, and the reference implementations. |
| `docs/` | Working notes, architecture decisions, test reports — and `docs/specs/` (not in the repository, see below). |
| `WORKPLAN.md` | The detailed work plan: architecture, work packages, protocol digest. |

## Submodules

| Submodule | Purpose |
|-----------|---------|
| [`libs/WWCP_EEBUS`](https://github.com/OpenChargingCloud/WWCP_EEBUS) | **The stack under test**: SHIP, SPINE and use case implementation in C# |
| [`libs/Styx`](https://github.com/Vanaheimr/Styx), [`libs/Hermod`](https://github.com/Vanaheimr/Hermod) | Foundations of the stack: helpers, networking (WebSockets, DNS, TLS) |
| [`libs/ship-go`](https://github.com/enbility/ship-go), [`libs/spine-go`](https://github.com/enbility/spine-go), [`libs/eebus-go`](https://github.com/enbility/eebus-go) | Go reference implementations — protocol reference and interoperability peers |
| [`libs/devices`](https://github.com/enbility/devices) | Recorded discovery/use case responses of real devices (Elli, Kostal, Porsche, SMA, Vaillant, Viessmann, ...) |
| [`libs/devices-app`](https://github.com/enbility/devices-app) | GUI tool for manual pairing against our server |

```bash
git clone --recurse-submodules https://github.com/OpenChargingCloud/EEBUSConformanceTests.git
```

On **Windows**, check out the submodules individually and leave out `libs/devices`: it
contains a path with a colon (`vaillant/arotherm-vwl-75:6a`), which Windows cannot represent.
Its contents remain readable via `git -C libs/devices show HEAD:<path>`.

## Specifications

The EEBUS specifications are free of charge after registration at
[eebus.org](https://www.eebus.org/specifications-media/), but they are licensed material and
therefore **not part of this repository**. Put them into `docs/specs/` (git-ignored) to run the
tests that build on them; tests report *inconclusive* rather than failing when they are absent.

## Building and testing

```bash
dotnet build EEBUSConformanceTests.sln
```

```bash
dotnet test EEBUSConformanceTests.sln --filter "TestCategory!=Interop"
```

### Interoperability tests

These start the Go reference implementations as external peers and therefore need a Go
toolchain (>= 1.24 — ship-go, spine-go and eebus-go require it); without one they report
*inconclusive* instead of failing.

Run them on **Linux, or inside WSL on Windows**, so that both peers share one network stack:

```bash
wsl -e bash -lc "cd /mnt/c/path/to/EEBUSConformanceTests && dotnet test --filter TestCategory=Interop"
```

A Go toolchain inside WSL cannot be driven from a test run on Windows: the Go peer would
live in the WSL network namespace, which breaks mDNS discovery and the direction where the
Go peer connects to us.

## Time is a protocol feature

SHIP handshake timers, SPINE heartbeats and use case failsafe durations are protocol behaviour
that has to be tested — not waited for. Everything time dependent is therefore driven by a
`System.TimeProvider`; tests advance a `FakeTimeProvider` instead of sleeping.

## License

Apache License 2.0, see [WWCP_EEBUS](https://github.com/OpenChargingCloud/WWCP_EEBUS).
