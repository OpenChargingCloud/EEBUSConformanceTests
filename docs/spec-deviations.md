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

## Wire format bugs found in our own earlier code

Not deviations of anybody else's — kept here because the tests which found them are the reason
this repository exists (WORKPLAN.md § 0):

1. `messageProtocolHandshake.formats` was serialised as a flat array instead of the complex type
   `{"formats":{"format":[…]}}` (XSD `MessageProtocolFormatsType`).
2. The access methods **response** used the element `accessMethodsRequest` instead of
   `accessMethods` — which made the handshake loop forever.
3. `connectionClose.reason` was read from the JSON property `"dns"` (copy and paste).
