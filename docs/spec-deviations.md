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
