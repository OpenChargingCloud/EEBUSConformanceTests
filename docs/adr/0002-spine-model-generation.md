# ADR 0002: the SPINE data model is generated from the official XSDs

**Status:** accepted (2026-07-26, WP06a)

## Context

SPINE 1.3.0 is a large data model: 562 complex types, 81 enumerations and 142 functions, spread
over 40 XSDs. Writing that by hand is a week of typing and a permanent source of the one kind of
bug this repository exists to find — a property name which does not match the specification.

Three sources describe the same model, and they are not equally good at the same things:

| Source | Good for | Silent about |
|---|---|---|
| The 76 official XSDs (`docs/specs/.../EEBus_SPINE_V1.3.0_Final_hp/XSDs/`) | every type, every field, the order of the fields, which values an enumeration has, which functions exist | that it describes a format transmitted as JSON |
| The specification PDFs | which field is the PRIMARY IDENTIFIER of a data type, what the fields mean | — (but not machine readable) |
| [spine-go](https://github.com/enbility/spine-go) | the JSON names as they really go over the wire, the identifiers (curated in `model/PRIMARYKEY_TAG_GUIDELINES.md`), proven in certification | — (but it is one reading of the specification, with its own slips) |

The XSDs are the only machine-readable complete source, so they are what the model is generated
from. spine-go is the oracle the result is checked against.

The XSDs are licensed material and are **not** part of this repository (`docs/specs/` is
gitignored), so the generator cannot run during a build.

## Decision

1. **A generator, `Apps/EEBUSModelGen`**, reads the XSDs with `XmlSchemaSet` and writes
   `libs/WWCP_EEBUS/WWCP_EEBUS_SPINE/Model/`. It **runs on demand, and its output is checked
   in**: everybody has to be able to build the stack, not only those who hold the
   specifications. The tool lives in this repository because the XSDs do.

2. **The names of the specification are kept verbatim.** `LoadControlEventDataType` stays
   `LoadControlEventDataType`; the enumeration behind
   `LoadControlEventActionType = LoadControlEventActionEnumType | EnumExtendType` is called
   `LoadControlEventActionType`. Debugging a wire problem means reading the XSD, the
   specification PDF and the Go implementation next to our code, and every renaming makes that
   harder for no gain. This differs from the hand-written SHIP layer, which uses shorter names —
   deliberately, and it stops at the layer boundary.

3. **Complex types are flattened.** Where the XSD derives `EntityAddressType` from
   `DeviceAddressType`, the generated class carries the inherited and its own properties, in
   that order. The wire format has no base types, the order is what matters, and spine-go
   flattens as well. An anonymous type declared inline is the named type it restricts — SPINE
   uses those to narrow a type to a few of its fields, which is the same object on the wire.

4. **The model is a mutable DTO with `JsonProperty` attributes**, not the
   `TryParse`/`ToJSON` style of the rest of GraphDefined. 562 hand-shaped types would be 100.000
   lines of generated ceremony; attribute-driven serialisation is what WORKPLAN § 3.3 asks for.
   The public envelopes (the SHIP messages) keep the house style. Every property is nullable and
   carries `NullValueHandling.Ignore` — every element of SPINE is optional, because a partial
   notify sends exactly the fields which changed. Every property also carries an explicit
   `Order`: EEBUS JSON is an ordered format, and a reordering while refactoring must not change
   a datagram.

5. **Enumerations become extensible string types** in the `PredefinedStrings` style of the SHIP
   layer, never a C# `enum`: SPINE unions its enumerations with `EnumExtendType`, so a value of
   a manufacturer is a legal value which has to survive being received and sent again. The
   closed enumerations (`MonthType`, `RoleType`, `CmdClassifierType`, …) are generated the same
   way and parse just as tolerantly — whether a value is allowed is a question for the
   conformance tests, not for the parser, and `IsDefined` answers it. Comparison is **ordinal**:
   the units of measurement contain both `s` (second) and `S` (siemens).

6. **The named simple types which are plain numbers or plain strings become the C# primitive.**
   `LoadControlLimitIdType` is a `UInt32`. Wrapping 44 numeric identifiers would mean 44 JSON
   converters for no safety that matters at this layer; identity lives one layer up, where
   entity and feature addresses are.

7. **The ISO 8601 types keep their text.** `DurationType`, `DateTimeType`, `TimeType` and
   `AbsoluteOrRelativeTimeType` are hand-written string-backed structs with typed accessors
   (`AsTimeSpan`, `AsDateTimeOffset`). `PT2M` and `PT120S` are the same duration and not the same
   datagram, and a test bench which silently re-formats what it forwards cannot tell anybody
   what actually went over the wire. spine-go keeps them as plain strings; we keep the text and
   add the typed reading.

8. **The function registry is generated** from the three choice groups of the command frame
   (`SPINEFunctions`), and `CmdType`/`FilterType` carry `[EEBUSFunction(name, part)]` on every
   property. Which elements type belongs to a function is read from the XSD rather than guessed
   from the name — `loadControlLimitListData` is answered by `loadControlLimitDataElements`,
   because the elements describe one *entry* of the list.

9. **The identifiers of the data types are imported from spine-go** and emitted as
   `[EEBUSKey]` / `[EEBUSKey(IsPrimary: true)]`. Which property is the PRIMARY IDENTIFIER is
   stated in the text of the specification, data type by data type, and cannot be read from the
   XSD. spine-go has it curated against the specification and proven in certification; taking
   the 97 entries from there is more trustworthy than typing them.

10. **spine-go is the oracle, through a checked-in fixture.** The generator also writes
    `WWCP_EEBUS_SPINE_Tests/TestData/spine-go-model.json` — every data type of spine-go with its
    JSON property names and identifiers. The model tests compare against that file, so they run
    without spine-go being present, which matters because WWCP_EEBUS is also built on its own.
    Differences are pinned in `SPINEModelTests.knownDeviations` with a reason, and a separate
    test fails when a pinned difference disappears, so the list cannot become a hiding place.

11. **The generated types are `partial` and serialise opt-in** (WP06b). Everything the XSD
    describes but cannot express — what a scaled number is worth, how an address reads, which
    function a command carries — is written as a partial class under
    `WWCP_EEBUS_SPINE/Additions/`, never by editing a generated file. Those additions are
    ordinary public properties, and the JSON library would put every one of them into the next
    datagram; `[JsonObject(MemberSerialization.OptIn)]` on every generated class is what keeps
    a convenience property from becoming a field of the protocol. Two tests hold that in place:
    one asserts the attribute on all 562 types, the other serialises a handful and compares the
    exact JSON.

## Consequences

* Changing the model means changing the generator, never the generated file. Every generated
  file says so in its header, and the generator deletes exactly the files carrying that header
  before writing.
* Whoever regenerates needs the specifications. Without them the generator stops with that
  message instead of writing half a model.
* The comparison against spine-go covers **names**, not types: it found nothing when 119
  properties of anonymous inline types were wrongly typed as `String`. What found that was the
  roundtrip over the recorded datagrams of spine-go, which is therefore not a nice-to-have but
  the second half of the check. Both run in CI.
* `SPINEJSON` has to be used for reading and writing SPINE. The library defaults are wrong in
  two ways which are quiet rather than loud: `DateParseHandling` turns a timestamp into a
  `DateTime` before any converter is asked (and back into whatever the machine's locale
  prefers — this really did turn `2022-11-19T15:21:50.003Z` into `19.11.2022 15:21:50` on a
  German Windows), and an empty list sent as `{}` would be refused.
* Three findings against spine-go and the XSDs came out of this work package; they are in
  `docs/spec-deviations.md`.
* Regenerating after a specification update is one command:

  ```bash
  dotnet run --project Apps/EEBUSModelGen
  ```

  `--list` reports what was found without writing anything.
