# Findings: where the specification, the XSDs and the reference implementation disagree

This is the running list of contradictions found while building the stack — between the EEBUS
specification documents, the official XSDs, and the Go reference implementation
([enbility](https://github.com/enbility)), which is the stack proven in certification.

The rule of this repository (WORKPLAN.md § 1.2): where a document and the Go behaviour
contradict each other, **the Go behaviour decides for wire compatibility** and **the
specification decides for the conformance tests**. Every such case is written down here, with
what we do and why.

It is modelled after `libs/ship-go/docs/SPEC_COMPLIANCE.md`.

---

## SPINE

### S1 — `SupplyConditionThresholdRelationListDataType` serialises its entries under the type name

*Found: 2026-07-26 (WP06a), spine-go `model/supplyconditions.go`.*

The XSD is unambiguous:

```xml
<xs:complexType name="SupplyConditionThresholdRelationListDataType">
    <xs:sequence>
        <xs:element maxOccurs="unbounded" minOccurs="0" ref="ns_p:supplyConditionThresholdRelationData"/>
    </xs:sequence>
</xs:complexType>
```

so the JSON property is `supplyConditionThresholdRelationData`. spine-go writes

```go
SupplyConditionThresholdRelationData []SupplyConditionThresholdRelationDataType `json:"SupplyConditionThresholdRelationDataType,omitempty"`
```

— the name of the *type*, capitalised, instead of the name of the element. Every other list type
of the same file follows the XSD, so this is a copy and paste slip rather than a decision.

**What we do:** follow the XSD. The generated model uses `supplyConditionThresholdRelationData`.

**Consequence:** a partner built on spine-go cannot read our `supplyConditionThresholdRelationListData`
and we cannot read theirs. No use case we implement uses this function, so nothing is blocked;
should that change, this is where to look first. The deviation is pinned in
`SPINEModelTests.knownDeviations`, which fails once spine-go agrees with the XSD again, so the
entry cannot rot.

### S2 — spine-go accepts `electricalConnectionCharacteristicData` as a payload

*Found: 2026-07-26 (WP06a), spine-go `model/commandframe.go`.*

`CmdType` of spine-go carries a field `electricalConnectionCharacteristicData`. The XSD does not
list that element in `DataChoiceGroup`, and `FunctionEnumType` knows only
`electricalConnectionCharacteristicListData`: the element is the *entry* of that list, not a
payload of its own.

**What we do:** follow the XSD — `CmdType` carries the list only. Reading is unaffected (nobody
sends a payload which is not a function); only a partner deliberately sending the single entry
would be refused, and doing so is not allowed by the specification.

Pinned in `SPINEModelTests.knownGoOnlyProperties`.

### S3 — an empty list arrives as `{}`

*Found: 2026-07-26 (WP06a), spine-go `spine/testdata/nm_detaileddiscovery_emptyarray.json`.*

Not a contradiction but a property of the format, noted here because it looks like a bug in a
datagram dump: SHIP TS 1.0.1, chapter 11 turns every JSON object into an array of objects with
one property each, and the transformation back cannot tell an object which had no properties
from an array which had no entries. An empty list therefore arrives as `{}`.

**What we do:** `SPINEJSON` reads `{}` where a list is expected as the empty list, and writes
the ordinary `[]`. A non-empty object where a list is expected stays an error.

### S4 — a partial filter with selectors reaches one entry and one incoming item in spine-go

*Found: 2026-07-26 (WP06c), spine-go `model/update.go`, `copyToSelectedData`.*

SPINE 1.3.0, Table 6 describes the combination "partial filter with `<SELECTORS>`" as:

> `<SELECTORS>` specify **locations** (specific identifiable list items) […] where `<FUNCTION>`
> data is added or modified. `<FUNCTION>` SHALL NOT use identifiers.

Locations, in the plural — the whole point of the combination is that a selector may name
something which is not an identifier (5.3.4.7.1: "some `<SELECTORS>` also provide other search
criteria") and then selects every entry which matches. spine-go applies the update to the first
matching entry only (`break` after the first `SelectorMatch`) and reads only the first entry of
the incoming list (`&newData[0]`).

**What we do:** apply every stated item to every selected entry, and never append — the
specification adds "`<SELECTORS>` SHALL match with already existing locations. Therefore, it is
not possible to add new list entries to a list with identifiable list items with this
combination."

**Consequence:** where a partner selects several entries at once, spine-go changes one of them
and we change all of them. None of the 29 official examples uses the combination, and no use
case we implement sends it, so this is a difference to know about rather than a blocker. Held by
`SPINEUpdateTests.SelectorsOfAPartialFilter_ChangeEveryEntryTheySelect`.

### S5 — spine-go never adds a list entry on a write from a remote device

*Found: 2026-07-26 (WP06c), spine-go `model/collection_operations.go`, `Merge`.*

The merge of spine-go appends an entry which was not there before only when the update is a
local one:

```go
if !exist && !remoteWrite {
    // only local updates can append data
    result = append(result, s2Item)
}
```

SPINE 1.3.0 devotes an example to exactly the opposite: `EEBus_SPINE_Spec_Example_RFE_W-A-Y_1-1-01.xml`,
listed in Table 21 under *"Classifier: write — List, list entry affected — Adding content"*, with
the rules "identifier must not be present before" and "identifier must be declared in
`<FUNCTION>`". A client adding a load control limit or a setpoint is the ordinary case of the
LPC and OPEV use cases.

**What we do:** follow the specification and add the entry. Whether a client may add anything at
all is a question of the feature's `possibleOperations` and of the use case, one layer above the
data model — this layer answers only what the data itself says (see S6).

Held by `SPINERestrictedFunctionExchangeTests.Write_AddsANewListEntry`.

### S6 — a refused partial write is applied in part by spine-go

*Found: 2026-07-26 (WP06c), spine-go `model/update.go`.*

SPINE 1.3.0, 5.3.4.2 closes the section on write combinations with:

> In general, a write operation with restricted function exchange SHALL ONLY be executed by a
> server if it can execute the received operation completely.

spine-go changes every entry it is allowed to change, skips the ones it is not, and returns
`false` afterwards; the caller in `spine/function_data.go` turns that into an error result, but
the data has already been changed. A client which receives the error and reads the function back
therefore finds a state which is neither the old one nor the one it asked for.

**What we do:** work on a copy and keep it only when every part of the command was allowed. A
refused write answers with the data exactly as it was.

Held by `SPINEUpdateTests.ARefusedWrite_ChangesNothingAtAll`.

### S7 — which property permits a remote write is not in the XSDs

*Found: 2026-07-26 (WP06c).*

Three data types of SPINE carry a boolean which decides whether another device may change
them — `LoadControlLimitDataType.isLimitChangeable`, `DeviceConfigurationKeyValueDataType.isValueChangeable`
and `SetpointDataType.isSetpointChangeable`. In the XSD they look like any other optional
boolean; that they gate writes is stated in the text of the resource specification.

**What we do:** the same as for the identifiers (ADR 0002, decision 9) — take them from the
`eebus:"writecheck"` struct tags of spine-go, where they are curated and proven in
certification, and emit them as `[EEBUSWriteCheck]`. The fixture carries them, so the model
tests hold them in place without spine-go being present
(`SPINEUpdateTests.TheWriteMarks_AreThoseOfTheGoReferenceImplementation`).

### S8 — spine-go drops the selectors of a read when the partner cannot do partial *writes*

*Found: 2026-07-26 (WP08), eebus-go `features/client/feature.go`, `requestData`.*

Before sending a restricted read, the helper of eebus-go removes the selectors and elements when
the partner cannot answer them:

```go
// remove the selectors if the remote does not allow partial reads
// or partial writes, because in that case we need to have all data
if selectors != nil && (!op.ReadPartial() || !op.WritePartial()) {
    selectors = nil
    elements = nil
}
```

The comment says "partial reads or partial writes", and so does the code — but whether a partner
can answer a *read* partially is `possibleOperations.read.partial` alone. A device which offers a
partial read and no partial write is asked for everything, every time.

**What we do:** `UseCaseFeature.RequestData` looks at the read flag only. The reason for dropping
them at all is right and worth keeping: SPINE 1.3.0, 5.3.4.5 lets a server ignore a restriction it
does not support and answer in full, so sending a filter it will ignore is noise.

**Consequence:** where a partner offers a partial read but no partial write, we ask for the part
and spine-go asks for everything. Both get a valid answer; ours is smaller. Held by
`UseCaseFrameworkTests.APartialReadIsNotAskedOfAPartnerWhichCannotAnswerIt`.

---

## Use cases

### U1 — the EVCC specification spells `communicationsStandard` two different ways

*Found: 2026-07-26 (WP09/5),
`EEBus_UC_TS_EVCommissioningAndConfiguration_V1.0.1.pdf`.*

The document contradicts itself about the name of the configuration key which carries the
standard a car speaks to a charging station — the key which decides whether half the e-mobility
family is usable with that car at all:

| Where | Spelling |
|---|---|
| Table 6, "Content of Function `deviceConfigurationKeyValueDescriptionListData` at Actor EV" | `communicationStandard` |
| Table 13, the same content as a specialization at Actor CEM | `communicationStandard` |
| Section 3.4.2.2, the selector a client is told to read the description with | `communicationsStandard` |

The SPINE resource specification has `communicationsStandard`, and so do spine-go
(`model/deviceconfiguration.go`) and eebus-go — so the field, which was built against the
certified stack, has the plural.

**What we do:** we **send** `communicationsStandard`, per the conflict rule. A client of ours
**accepts either** (`EVCommissioningAndConfiguration.CommunicationStandardKeys`), because a car
built literally from the content tables is a car which exists and a manager which did not
recognise its key would conclude that it does not know what the car speaks — and therefore that
no further use case is possible with it.

**Consequence:** a conformance test for this key has to accept both spellings from a device under
test and cannot treat either as a defect. Held by
`EVCCTests.Scenario2_TheKeyIsSpelledTheWayTheResourceSpecificationSpellsIt` and
`EVCCTests.Scenario2_ACarSpellingItTheOtherWayIsStillUnderstood`.

---

### U2 — EVSOC names its client actor `MonitoringAppliance`, eebus-go announces `CEM`

*Found: 2026-07-26 (WP09/5), `EEBus_UC_TS_EVStateOfCharge_V1.0.0_RC1_public.pdf` section 3.2.2
vs. `eebus-go/usecases/cem/evsoc/usecase.go`.*

The specification says the watching side "SHALL be denoted as `MonitoringAppliance`" in the use
case discovery. eebus-go registers it as `model.UseCaseActorTypeCEM`.

This is the same shape as the OPEV actor question (specification `EnergyGuard`, eebus-go `CEM`),
and note that it is *not* general: EVCEM's chapter 2 calls its client the "Energy Guard" but its
section 3.2.2 says the wire says `CEM`, so there the two agree and there is nothing to reconcile.

**What we do:** our EV accepts both actors, and our appliance announces `MonitoringAppliance` by
default or `CEM` on request (`EVSOCMonitoringAppliance(AnnounceAsCEM: true)`). Carried by
`MonitoringProfile.AlsoKnownAsClientActor`.

**Consequence:** the conformance catalog checks the specification's name; the interoperability
suite has to expect the other one. Held by `EVSOCTests.TheWatchingActorGoesByTwoNames`.

---

### U3 — the EVSECC actor of the Porsche PMCC is `EV`

*Found: 2026-07-26 (WP09/5), noted in `eebus-go/usecases/cem/evsecc/usecase.go`
("The Porsche PMCC devices use this actor for this use case incorrectly").*

Not a contradiction between documents but between a document and a shipped product: the
specification's actor for the EVSE commissioning use case is `EVSE`, and the PMCC announces `EV`.

**What we do:** the same as eebus-go — accept it, and say so. `EVSECCEnergyManager` takes
`StrictActor` for the case where a test wants the letter of the specification. Held by
`EVSECCTests.AStationWhichAnnouncesTheWrongActorIsStillAccepted`.

---

### U4 — `incentiveTable` has no primary key, so it cannot be written partially

*Found: 2026-07-26 (WP09/6), `EEBus_UC_TS_CoordinatedEVCharging_V1.0.1.pdf` Table 11 against
SPINE 1.3.0, 5.3.4.1.*

Not a contradiction between documents but a consequence worth writing down, because it changes what
a client may send.

The CEVC content tables mark every element of `incentiveTableData` with `\W` — a client writes all
of it — and the tables are written in the same style as every other list function in the family,
where a partial write addressed by the primary identifier is the norm and the general implementation
guideline § 3.1 actively asks for one ("in cases where not all Elements are writeable by a client
[…] the client SHALL use RFE for the write command").

But `incentiveTable` is not that kind of list. Its identity sits *inside* the entry, in
`tariff.tariffId`, and the entry itself carries no key — so SPINE 1.3.0, 5.3.4.1 allows "only the
exchange of the complete list". Our update engine refuses a partial write of it, correctly, with
exactly that sentence.

**What we do:** `CEVCEnergyBroker.WriteIncentives` writes the incentive table **in full**, and says
why in a comment. Everything else in the use case is written partially as usual.

**Consequence:** a conformance test must not treat a full write of `incentiveTableData` as a
violation of the guideline's "use RFE" rule, and must not expect a device to accept a partial one.
Held by `CEVCTests.Scenario3_TheBrokerWritesPricesIntoTheTariffTheCarDescribed`.

---

### U5 — the Porsche PMCC announces every use case under the actor `EV`

*Found: 2026-07-26 (WP10), by replaying
`libs/devices/porsche/mobile-charger-connect/usecase-data.json` through
`sim device-replay`.*

Finding U3 recorded that the PMCC announces the **EVSE commissioning** use case
as the actor `EV`. Replaying the recording rather than reading about it shows
that this is not one mistake but a policy: the device puts **all eight** of its
use cases under a single `useCaseInformation` entry with `actor: "EV"` —
including `evseCommissioningAndConfiguration` and `evChargingSummary`, both of
which the specifications place at the EVSE.

| Use case | Actor per specification | Actor announced |
|---|---|---|
| `evCommissioningAndConfiguration` | EV | EV |
| `measurementOfElectricityDuringEvCharging` | EV | EV |
| `optimizationOfSelfConsumptionDuringEvCharging` | EV | EV |
| `overloadProtectionByEvChargingCurrentCurtailment` | EV | EV |
| `coordinatedEvCharging` | EV | EV |
| `evStateOfCharge` | EV | EV |
| **`evseCommissioningAndConfiguration`** | **EVSE** | EV |
| **`evChargingSummary`** | **EVSE** | EV |

**What we do:** we tolerate it where there is a precedent to follow and not
otherwise. `EVSECCEnergyManager` accepts `EV` as well as `EVSE`, because
eebus-go does and documents why (U3). `EVCSEnergyBroker` accepts only `EVSE`,
because the EV charging summary has no reference implementation and inventing a
tolerance from one device's recording would be guessing at what the field does.

**Consequence:** an energy manager built strictly to the specification can name
a PMCC and read its charging summary from neither. The replay names that
situation for what it is — "implements *X* but found no partner for it, the
device announced it under an actor this side does not accept" — rather than as
a gap in the implementation, because the two have different owners. Held by
`SimulationTests.DeviceReplay_TheReportNamesAnActorMismatchAsSuch`.

---

## SHIP

### H1 — the cipher suites of chapter 9.1 do not cover TLS 1.3

*Found: 2026-07-25 (WP02), see `docs/adr/0001-tls-cipher-suites.md`.*

SHIP TS 1.0.1 chapter 9.1 predates TLS 1.3 and names two TLS 1.2 cipher suites. TLS 1.3 moved
its suites into a namespace of their own, so pinning exactly those two leaves TLS 1.3 without
any suite at all, and .NET/OpenSSL then refuses every connection rather than simply not
negotiating TLS 1.3.

**What we do:** the cipher suites policy is the union of chapter 9.1 and the TLS 1.3 suites; the
Go reference implementation arrives at the same result because Go applies
`tls.Config.CipherSuites` to TLS 1.2 and below only. `SHIPTLS.CipherSuites` keeps quoting the
specification alone. Details and the platform differences are in the ADR.

---

## Conformance catalog

Findings of WP11, where running the official test specifications against this stack turned up a
disagreement rather than a bug of ours. The bugs it turned up are in the section below.

### C1 — the double connection rule of SHIP 12.2.2 and the one everybody implements are not the same

*Found: 2026-07-26 (WP11), `TC_SHIP_CONN_001`.*

SHIP TS 1.0.1 chapter 12.2.2 resolves two simultaneous connections to the same partner by SKI:
the node with the larger value **keeps the most recent connection** and closes the others. The
official test case checks exactly that, with the device under test holding the larger SKI.

ship-go does something else, and says so in a comment: *"This is hard to implement without any
flaws. Therefore I chose a different approach: The connection initiated by the higher SKI will
be kept"* (`hub/hub_connections_registry.go`). The two rules disagree precisely in the scenario
of the test case.

They cannot both be followed, and following the specification alone is worse than useless:
against a ship-go peer — which is EVCC and most of the installed base — the two sides would keep
*different* connections and neither would work. Our stack therefore follows ship-go, and
`TC_SHIP_CONN_001` **fails**, on purpose.

**What we do:** `SHIPNode.KeepThisConnection` implements the ship-go rule. The conformance
report says "failed" for `TC_SHIP_CONN_001`, because that is what a certification body has to
see; the catalog entry carries the reason as a `KnownDeviation`, so the self test reports it as
inconclusive-with-explanation rather than turning the build red over a decision which has
already been taken. If the ecosystem ever converges on the specification's rule, this is a
one-line change and a deleted paragraph.

### C2 — TC_SHIP_HELLO_001/002/004 expect a server to start the protocol handshake

*Found: 2026-07-26 (WP11).*

The three hello cases run with the device as SHIP **server** and end with "the DUT sends an SME
protocol handshake message". A server cannot: the three way handshake of chapter 13.4.4.2 begins
with the *client's* `announceMax`, and `TC_SHIP_PROT_001` — same role, same phase — says so
outright ("the DUT SHALL wait for the client's announceMax message"). ship-go waits here as
well.

**What we do:** the test tool, which is the SHIP client in these cases, sends its own
`announceMax` and then verifies what the step means — that the device left the hello phase and
answers within the protocol handshake. Sending it is the tool's own next step rather than an
automatic reply, so `PRE_SHIP_Manual_Message_Handling` does not forbid it. The reading is
written into `TC_SHIP_HELLO_001.EnteredProtocolHandshake`.

---

## Wire format bugs found in our own earlier code

Not deviations of anybody else's — kept here because the tests which found them are the reason
this repository exists (WORKPLAN.md § 0):

1. `messageProtocolHandshake.formats` was serialised as a flat array instead of the complex type
   `{"formats":{"format":[…]}}` (XSD `MessageProtocolFormatsType`).
2. The access methods **response** used the element `accessMethodsRequest` instead of
   `accessMethods` — which made the handshake loop forever.
3. `connectionClose.reason` was read from the JSON property `"dns"` (copy and paste).

Found by the official test specifications in WP11, all four in the stack (`libs/WWCP_EEBUS`):

4. **A server dropped an invalid CMI message without answering it** (`TC_SHIP_CMI_001`,
   `TC_SHIP_CMI_005`). Chapter 13.4.3 has the server send its own CMI message — "message type 0,
   CmiHead 0 is all I speak" — *and then* close. Closing silently leaves the client unable to
   tell an incompatible partner from a broken network. Fixed in
   `SHIPConnection.RefuseCmiAsync`.
5. **The data exchange state refused every SME control message** (`TC_SHIP_AM_001`,
   `TC_SHIP_MSG_003`, `TC_SHIP_AMDATA_001`/`003`). An access methods request may arrive at any
   time while the connection lives (13.4.6), and answering it is mandatory (`SHIP-TS-ACC-01`) —
   the connection instead treated it as an unexpected message and closed. Five test cases failed
   on the same line.
6. **SPINE data arriving during the access methods exchange was refused as well**
   (`TC_SHIP_AMDATA_002`), against the implementation guideline § 2.1, which says outright that
   incoming SPINE messages are passed to the application immediately even while an access
   methods query is pending. Now they are, and what the application answers is queued until the
   one state in which SHIP data may be sent.
7. **A binding to the primary node management feature was accepted** (`TC_SPINE_BIND_001`).
   SPINE 7.3.1 gives that feature the role "special", which cannot be bound — and a binding to
   it is a licence to write into the very place where the device keeps its topology, its
   bindings and its subscriptions. Fixed in `SPINENodeManagement.HandleRelationCall`.

Found by the four use case high level test specifications in WP11b, all six in the stack:

8. **A limit which was refused did not move the state machine** (`ATC_LPC_COM_PT_CSTransition1_001`,
   `…_CSTransition8_001`, `…_CSTransition11_001`, and the same three under `ATC_LPP_*`). A value
   below zero was rejected *before* `PowerLimitationStateMachine.LimitWritten` ever saw it, so
   the controllable system stayed where it was. Rules 902, 918 and the transitions 1, 8 and 11
   say the opposite, and the reason is worth stating: what ends "init", the failsafe state and
   "unlimited/autonomous" is not a *usable* limit, it is the proof that an energy guard is
   there at all. A device which refuses the value and stays in its failsafe state has taken the
   one message which shows it is not alone and concluded from it that it is. Fixed in
   `APowerLimitationControllableSystem.ApproveLimit`.
9. **…and in the two controlled states it moved the state machine when it should not have**
   (`ATC_*_COM_NT_CSLimited_001`). The mirror image of finding 8, and a separate rule: in
   "limited" and "unlimited/controlled" the energy guard is already known to be there, so
   rejecting a limit changes nothing (rules 907/1 and 907/2). The state machine had folded
   "the guard deactivated the limit" and "the limit could not be applied" into one condition, so
   an inapplicable value silently unlimited a limited device. Fixed in
   `PowerLimitationStateMachine.LimitWritten`.
10. **Rule 037 was not implemented at all** (`ATC_*_COM_PT_CSConnection_002`/`_004`,
    `…_CSFS_001`/`_003`, `…_NT_CSUnlAuto_001` — ten cases across the two use cases). In "init",
    the failsafe state and "unlimited/autonomous", commands on any data point *other* than the
    limit are evaluated only after a heartbeat and a following limit. The failsafe values are
    what a device falls back on when everything else has failed, so letting an unproven partner
    rewrite them hands over the one number which was supposed to be safe. Fixed in
    `APowerLimitationControllableSystem.ApproveConfiguration`.
11. **A rejected failsafe duration left the old value in place** (`ATC_*_COM_PT_CSConnection_005`
    and `…_008`). Rule 022/5: having refused a duration longer than it accepts, the controllable
    system SHALL move to its own maximum. Refusing and changing nothing leaves the energy guard
    believing a number the device never confirmed, and the two of them then disagree about how
    long a failsafe state would last.
12. **The controllable system never sent the heartbeat it declares** (`ATC_*_COM_PT_CSHeartbeat_001`).
    Table 21 lists `deviceDiagnosisHeartbeatData` among its server data and rule 006/032 asks for
    one at least every 60 seconds; the feature and the function existed and nothing ever wrote
    them. An energy guard watching it would have concluded the appliance had died. Fixed by
    giving `APowerLimitationControllableSystem` a `SPINEHeartbeat` of its own.
13. **A value marked "out of range" or "error" was read as if it were good**
    (`ATC_MGCP_SCE*_NT_MA*` and `ATC_MPC_SCE*_NT_MA*` — 26 cases). MPC 2.5.2 and MGCP 2.6.2 both
    say such a value SHALL be ignored by the monitoring appliance; `AMonitoringAppliance.Readings`
    returned a list of numbers without looking at their state. A meter which knows its reading is
    wrong and says so has done its job — an energy manager which takes the number anyway has
    turned a detected fault into an undetected one. Fixed there.

Two more things the use case specifications asked for and this stack did not do at all, now
implemented rather than reported: the energy guard introduces itself with a heartbeat and a
following limit whenever it (re)discovers a controllable system (rule 913, `AnnounceTo`), and a
monitored device can publish a measurement together with the state of that measurement
(`AMonitoredDevice.Set`, `MeasurementValueStateType`) — without which finding 13 could not have
been detected from the outside at all.
