# ADR 0003: the restricted function exchange is one generic engine over the model metadata

**Status:** accepted (2026-07-26, WP06c)

## Context

SPINE does not only exchange whole functions. A device may write one element of one entry of a
list, delete another element of another entry, and say both in the same message; it may notify
what changed rather than what is; it may read one element of one entry and get exactly that back.
The specification calls this *restricted function exchange* and defines it in § 5.3.4 —
six tables of `cmdOptions` combinations, plus 29 example datagrams in Annex A.

Applying such a command means answering questions the XSD does not answer:

| Question | Where the answer is |
|---|---|
| which entry of a list does this one mean? | its **identifiers**, stated data type by data type in the resource specification |
| may this change be applied at all? | a boolean of the data type itself (`isLimitChangeable`, `isValueChangeable`, `isSetpointChangeable`) |
| which selectors and which elements of the filter belong to this function? | the function name, and the shape of the command frame |

WP06a put all three onto the generated model as attributes — `[EEBUSKey]`, `[EEBUSWriteCheck]`
(added in WP06c) and `[EEBUSFunction]`. So the question for WP06c was not *where the knowledge
comes from* but *how many times it has to be written down*.

The Go reference implementation answers "once per list type": `model/update.go` holds the generic
algorithm, and about forty hand-written `*_additions.go` files each implement `UpdateList` for one
`xListDataType` by unwrapping its list and calling it. That is roughly 500 lines which exist only
to name a property, and a new function of a later specification version is silently not updatable
until somebody writes the forty-first.

## Decision

1. **One engine, no per-function code.** `WWCP_EEBUS_SPINE/Update/` holds `SPINETypeInfo` (the
   metadata of a data type, read once and cached), `SPINESelectors`, `SPINEElements`,
   `SPINEUpdate` and `SPINERead`. Which functions exist, which properties identify an entry and
   which list belongs to which function are read from the model at runtime. A function nobody has
   thought of yet is updated by the same code as `loadControlLimitListData`.

   A list based function is recognised by its shape rather than by its name: a data type whose
   only property is a list of a data type is an `xListData`. That is what the XSD says, and it
   does not depend on anybody keeping to the naming convention.

2. **The specification decides, not the reference implementation.** Where the two disagree — and
   they do in three places — we follow SPINE 1.3.0 and write the difference down
   (`docs/spec-deviations.md`, S4 to S6). This is the opposite of the rule for the *wire format*,
   where the Go behaviour decides (WORKPLAN § 1.2), and deliberately so: a property name is a
   convention two implementations have to share, while the update semantics are a statement about
   what a message means. Getting that wrong the same way as somebody else is not compatibility,
   it is two devices being wrong together.

3. **A stated element replaces, it does not merge.** `<FUNCTION>` names elements; each named
   element arrives complete. The decisive case is the specification's own example
   `W-M-Y_1-1-02.xml`, which modifies a value of `105` with scale `-1` by sending nothing but
   `<number>14</number>`. Merging into the old element would keep the scale and make it 1.4;
   § 5.3.4.7.1 says an omitted child falls back to its **default value**, so the answer is 14.
   The partial-ness of a restricted exchange stops at the elements of the entry.

4. **Nothing is changed in place.** Every update works on a copy of the data it was given
   (`SPINETypeInfo.Clone`), and a device therefore always holds either the old state or the new
   one. This is what makes § 5.3.4.2 implementable at all: *"a write operation with restricted
   function exchange SHALL ONLY be executed by a server if it can execute the received operation
   completely"* — the copy is simply dropped when any part of it was refused.

   The copy is complete because every property of the model holds a data type of the model, a list
   of those, or an immutable value. That is not an assumption; it is asserted for all 2133
   properties by `EveryProperty_IsAModelTypeAListOrAnImmutableValue`.

5. **Two ways of not succeeding, told apart by the data.** A **refused** write answers with the
   data unchanged. A command which is **out of spec but unambiguous** — a partial write filter
   carrying `<ELEMENTS>` (§ 5.3.4.8), a missing `cmd.function` (§ 5.3.4.1), a partial notify of a
   list whose entries have no identifiers (§ 5.3.4.1) — is carried out, and `Problem` says what
   was wrong with it. A stack may ignore the second kind; a test bench exists for it, and quoting
   the section number in the message is what makes it usable in a report.

6. **Nothing the peer sent is dropped silently.** spine-go filters incoming list entries which
   carry nothing but their identifiers, because devices announce the structure of a list before
   they send its data and those entries would become empty rows. For a stack that is right; for a
   bench it destroys the evidence — Annex A requires an entry added by a write to be complete, so
   an entry which is not is a finding. `SPINEUpdateOptions.IgnoreEntriesWithoutData` turns the
   reference behaviour on for comparison; it is off by default.

7. **The result is ordered by the identifiers of its entries.** SPINE identifies the entries of a
   list rather than ordering them, so this changes no meaning — and it makes two runs of the same
   exchange comparable, which is most of what a bench does.

8. **The write mark is imported like the identifiers.** Which boolean of a data type permits a
   remote write cannot be read from the XSD. It comes from the `eebus:"writecheck"` tags of
   spine-go, the same way and for the same reason as the identifiers (ADR 0002, decision 9), and
   the generated fixture carries it so the tests hold it in place.

9. **The read side belongs here too.** `SPINERead` answers a partial read out of the data a device
   holds: the entries the selectors select, restricted to the elements the filter names. It always
   keeps the identifiers, because § 5.3.4.5 requires them to be complete in a reply *"even if the
   corresponding read operation made use of elements selection but did not specify the elements of
   the identifier"*.

## Consequences

* The 29 official example datagrams of Annex A are checked in as tests, and they are the reason to
  trust any of this. Every one of them is read by the model, written back unchanged, put through
  the EEBUS JSON transformation of SHIP and back, and applied to a defined state whose result is
  asserted. Two of them close a loop the specification opened itself: the answer our `SPINERead`
  gives to the read `RD-P-Y_1-2-01` is compared with the reply `RY-P-Y_1-1-01` which the
  specification ships for it, character for character.
* The examples are licensed material and are not committed. Without `docs/specs/` those tests
  report *inconclusive*; the rest of the update tests need nothing but the model.
* Reading them needs the data model, and there is no way around it: an XML element which occurs
  once cannot be told from a list of one entry without the schema. `SPINEExampleXML` therefore
  walks the model and the XML side by side — which turns "the specification uses an element our
  model does not have" into an error rather than a silently dropped value.
* Three differences to spine-go came out of this and are written down in `docs/spec-deviations.md`
  (S4 partial selectors, S5 adding an entry by remote write, S6 the atomicity of a refused write).
  Each is held by a test, so none of them can quietly become true or false again.
* Reflection is used on every update. The metadata is read once per type and cached; the property
  access itself is not, and if a device ever floods us hard enough for that to matter, the place to
  change is `SPINEPropertyInfo.Get`/`Set` and nothing else.
* WP07 builds the feature layer on this: `FeatureLocal` holds the function data, and processing a
  `write` or a `notify` is `SPINEUpdate.Apply`, processing a `read` is `SPINERead.Apply`. Whether a
  peer may write at all is a question of `possibleOperations` and of the binding, which is that
  layer's business — this one answers only what the data itself says.
