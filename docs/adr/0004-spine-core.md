# ADR 0004: the SPINE core is a tree of devices, entities and features with one way in and one way out

**Status:** accepted (2026-07-26, WP07)

## Context

SPINE describes a device as a tree: entities below a device, features below an entity, functions
below a feature (SPINE 1.3.0, § 2.1). Two devices exchange datagrams which address exactly one
feature on each side, and what a device may do to another is decided by three things which live
in three different places:

| Question | Where it is answered |
|---|---|
| does this feature have this function, and may it be read or written? | `possibleOperations`, announced in the detailed discovery |
| may *this* device write it? | a **binding** (§ 7.6) — without one, error 9 |
| may *this particular entry* be changed? | the data itself (`isLimitChangeable`, WP06c) |

WP06 built the data model and the update engine. WP07 is the layer which uses them: the object
tree, the routing of an incoming datagram, and the three managers — node management,
subscriptions/bindings, heartbeat.

The Go reference implementation is the obvious template, and mostly a good one. Two things about
it did not survive the port:

* it needs a hand-written `UpdateList` per list type (about forty files) and a 321-line factory
  mapping function names to types, because Go generics cannot do what reflection over the
  generated attributes does here;
* its `FeatureLocal` answers every read in full, deliberately, even where the client asked for a
  part (its own comment says so, citing § 5.3.4.5).

## Decision

1. **One tree, two halves.** `SPINELocalDevice`/`Entity`/`Feature` is what we offer;
   `SPINERemoteDevice`/`Entity`/`Feature` is what we know about somebody else. The remote half is
   explicitly a **cache**: its data comes from replies and notifies, its shape from the detailed
   discovery, and it never answers anything on its own.

2. **Nothing is per function.** `SPINEFunctionData` holds the data, the type (from the generated
   `SPINEFunctions` registry) and the `possibleOperations`, and it is the same code for all 142
   functions. A function of a later specification version works without a line being written.

3. **One way out: `ISPINEWriter`.** The whole core knows one method for sending. In production
   that is a SHIP data message; in the tests it is a loopback which records every datagram; for a
   replay it is a file. The datagram is handed over as JSON, so the seam is also a place to watch
   without interpreting.

4. **One way in: `SPINELocalDevice.ProcessDatagram`.** Everything which can be refused is refused
   there, in the order the specification puts it, and **every refusal is sent back as a result** —
   a device which asks something it may not ask has to be told, or it waits forever. A result is
   never answered with a result.

5. **A command which says two things about itself is rejected.** If a filter is present, § 5.3.4.1
   requires `cmd.function`; if the payload or a filter names a different function than
   `cmd.function` does, the message claims two data types at once. That is rejected with
   "command rejected" before anything is applied — it is the shape in which a filter gets applied
   to the wrong type.

6. **A partial read is answered partially, where the feature announced it can.** § 5.3.4.5 lets a
   server ignore a restriction it does not support and answer with more than was asked for, and we
   do exactly that where `possibleOperations.read.partial` is absent. Where it is present, we
   answer the part — announcing something and then not doing it is the worse of the two, and
   `SPINERead` (WP06c) already does the work.

7. **Waiting for an answer is set up before the question leaves.** `SPINESender.PrepareRequest`
   hands out the message counter and the datagram separately. Over a loopback the reply is
   processed while the send is still on the stack; on a real link it is a race which is lost now
   and then. This is not a test artefact — it is the reason the two-step API exists.

8. **The same question is not asked twice while it is unanswered.** A request is hashed over its
   classifier, its destination and its commands; an identical one which is still open answers with
   the first one's message counter and sends nothing. Devices do ask twice.

9. **Node management is a feature, not a special case.** `SPINENodeManagement` derives from
   `SPINELocalFeature` and overrides the message handling for the nine functions it owns;
   everything else falls through to the ordinary handling. `SPINELocalDevice` creates it itself,
   because a device without it is not a SPINE device.

10. **Subscriptions and bindings are the same shape and different things**
    (`SPINEFeatureRelations`, twice). A subscription means "tell me when this changes", a binding
    means "I may change this". Neither implies the other, and both are given up when the entity or
    the connection behind them disappears — a binding which outlives the feature it points at is a
    write permission for an address somebody else may get next.

11. **One event bus with two levels.** The stack subscribes at `Core`, everything above at
    `Application`, and core handlers run first — so a use case which is told that an entity
    disappeared does not still find its subscriptions in place. Both levels run **synchronously**
    on the publishing thread, unlike spine-go, which starts a goroutine per application handler: a
    bench which cannot say whether an event has been handled yet cannot assert anything about it.
    A handler which throws is caught and reported through `OnHandlerFailed`; it must not be able to
    stop a datagram from being processed.

12. **Everything with a clock takes the `TimeProvider` of the device.** The heartbeat, the event
    timestamps, the missing-heartbeat detection. The tests move a `FakeTimeProvider` and never
    wait.

## Consequences

* The tests run two devices wired to each other through an in-memory `ISPINEWriter` which records
  every datagram, so they assert the **exchange** and not only the result: which datagrams there
  were, in which order, with which message counters and which addresses. The node management tests
  arrange nothing by hand — the two devices start knowing nothing but the address of entity 0,
  feature 0.
* A heartbeat goes out two seconds earlier than announced (for intervals above four seconds),
  because devices exist which read the interval as "one has to arrive within this time"; spine-go
  does the same and names the Elli Connect. This is the kind of accommodation a test bench should
  know it is making, so it is a test of its own.
* `SPINEHeartbeat.Check()` is called by whoever drives the time rather than by a timer of its own.
  A device which sends nothing gives nothing to react to, so somebody has to look — and in a test
  that somebody is the test.
* One bug in the layer below came out of building this: every incoming SHIP frame was parsed with
  the date handling of the JSON library switched on, so a SPINE timestamp lost its exact text at
  the outermost layer, before any SPINE type saw it. That is the third time this trap has been hit
  in this stack and the first on the real receive path; `SHIPFrame` now reads with
  `DateParseHandling.None`, and `SPINEJSON.Read`/`ToJObject` exist so the new seam cannot
  reintroduce it.
* What is deliberately **not** here: the feature-level helpers of eebus-go (`features/`), which are
  use case scaffolding and belong to WP08, and any use case name or actor constant, which the use
  case specifications decide.
