# ADR 0005: a use case is a declaration plus a watcher, and the rules live in the base

**Status:** accepted (2026-07-26, WP08)

## Context

A SPINE device says what it *is* (entities, features, functions). A use case says what it can
*do with somebody else*: "this entity is the energy guard of the limitation of power consumption,
version 1.0.0, scenarios 1 to 4". Both devices announce that in
`nodeManagementUseCaseData`, read each other's, and then have to work out whether they can
actually play together.

Two sources define the rules, and neither of them is SPINE:

* **each use case specification, section 3.1.2** ("Use Case discovery rules") — the same seven
  SHALL/SHOULD sentences in every one of them, about the name, the actor and how to evaluate
  `useCaseVersion`;
* **`EEBus_UC_IG_GeneralGuidelines_V1.0.0`** (2026-07-16), which is the cross-cutting document
  WORKPLAN § 1.2 marks as required reading — client vs. server actors, mandatory data, when to
  use restricted function exchange, subscriptions over polling, one binding partner per writable
  data point.

SPINE itself is deliberately silent: the XSD declares `UseCaseNameEnumType` and
`UseCaseActorEnumType` as **empty** restrictions of a string, unioned with the extension type.
That is the specification saying the names come from the use case documents.

## Decision

1. **The names and actors live in the use case layer**, as plain constants
   (`UseCaseNames`, `UseCaseActors`), because that is where the specifications put them. The
   spelling is not derivable — `coordinatedEvCharging` has a small "v",
   `monitoringOfPvString` a small "v" and a capital "S" — so they are taken from the Go
   reference implementation, which is proven in certification, and cross-checked against
   Annex A of the general guideline.

   Annex A's table does **not** survive text extraction from the PDF: its columns drift, so
   rows pair the wrong use case with the wrong actor. It was used to check individual names, not
   to generate the list, and nothing here was derived from it mechanically.

2. **The version rules are in one place** (`UseCaseVersion`), because they are identical in every
   use case specification. The rule which is easy to get wrong is the last one: a partner which
   announces *only* major versions we do not implement "should try to be evaluated as a valid
   partner" anyway. A different major version is a reason to be careful, not a reason to refuse
   to talk — so `Best` answers with the highest version they offer and a flag saying it is not
   ours, rather than answering "no partner".

3. **A use case is a declaration and a watcher.** `AUseCase.Register()` puts it into the node
   management use case data; the watcher half subscribes to the device's event bus at **core**
   level and recomputes, per remote entity, which scenarios can actually be played.

4. **A scenario is playable only when the partner announces it *and* has the server features it
   needs.** A device which claims a scenario it has no feature for has claimed something it
   cannot do. Believing it means sending a read to a feature which is not there; a test bench
   wants to see that rather than trust it.

5. **Availability is not the same as support.** A charging station with no car plugged in still
   supports the use case. `UseCasePartner` keeps the scenarios and reports `Available = false`,
   so "it could do this but cannot right now" stays distinguishable from "it cannot do this".

6. **One feature helper, not nine.** `UseCaseFeature` pairs one of our client features with a
   partner's server feature. eebus-go needs a class per feature type because Go needs a typed
   method per data type; here the SPINE core reads and writes any function by name, so a helper
   per feature type would be a list of names and nothing else. What the helper does add is the
   part that is not mechanical:
   * it refuses to ask for something the partner never announced;
   * it drops the selectors of a partial read when the partner did not announce a partial read,
     because § 5.3.4.5 says the answer comes back in full anyway (see `docs/spec-deviations.md`,
     S8, for what spine-go does here);
   * it writes **partially by default**, because guideline § 3.1 says a client "SHALL use RFE for
     the write command" wherever not everything is writable — "otherwise, the full write command
     would be rejected by the recipient";
   * it answers `IsRedundantPolling`, which is guideline § 3.2.3 as a question rather than a
     prohibition — a SHOULD NOT, and a question a conformance test can ask about somebody else's
     device just as well as about ours.

7. **The guideline rules which are not code are written down here**, for WP11 to turn into
   conformance checks:
   * § 2.1.3 a secondary function whose direction is reversed (the energy guard hosting its own
     heartbeat server) does not change the client/server classification of an actor;
   * § 3.1 every primary and sub-identifier SHALL be in every message, writable or not (the WP06c
     update engine already keeps them);
   * § 3.2.1 a feature is "relevant", and therefore has to be subscribed, if it appears in the
     scenario's communication sequences;
   * § 3.2.2 polling SHALL NOT be the primary data retrieval strategy where the server supports
     subscriptions;
   * § 3.3 a server actor SHALL NOT let more than one binding partner write the data points of a
     scenario — and SPINE 1.4.0 will make that at most one binding per feature.

## Consequences

* WP09 writes use cases as small subclasses: an actor, a name, a version and a list of scenarios
  with their required features. Everything else — announcing, matching partners, versions,
  availability, events — is inherited.
* The tests play the two actors of LPC against each other over the loopback, with nothing
  arranged by hand: both sides register, discover, and then know which of the other's entities
  can play which scenario.
* Three bugs in the layer below came out of writing them, all found by the use case layer doing
  something the SPINE tests had not: reading a function which holds no data yet.
  * A reply to such a read carried an **empty command** — no function name — so it could not be
    matched to the read it answered and the caller waited forever. A function which holds nothing
    is now answered with an empty instance of it, which is what the specification's own XML
    (`<setpointListData/>`) shows.
  * A message which arrives with a `msgCounterReference` and cannot be handled now **releases the
    caller with that error** instead of stranding it.
  * Nothing ever timed out. A request now waits for the `maxResponseDelay` the feature announced,
    or `SPINELocalDevice.ResponseTimeout`, and answers with error 2 ("timeout") — on the device's
    `TimeProvider`, so the test moves a fake clock rather than waiting ten seconds.

  A partner which never answers must not be able to stop this device. That is true of any stack;
  for a bench, "no answer" is a *result* and has to be reportable.
* `SPINELoopback` moved from the test project into the SPINE project. It is a test aid and a
  bench component at the same time — the same wire which carries a unit test is what a recorded
  exchange is replayed into — and both test projects need it.
* Not here, deliberately: the nine typed feature helpers of eebus-go (decision 6), and any use
  case implementation (WP09).
