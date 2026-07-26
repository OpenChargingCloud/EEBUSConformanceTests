# ADR 0007: a monitored quantity is not always a quantity on a wire

**Status:** accepted (2026-07-26, WP09/5)

## Context

WP09/5 added the four e-mobility commissioning and measurement use cases: **EVSECC**, **EVCC**,
**EVCEM** and **EVSOC**. Two of them fell straight into the shapes ADR 0006 established, and the
other two are where those shapes stop.

**EVCEM** is the monitoring profile pointed at the charging cable — measurement descriptions,
electrical connection parameter descriptions, the join by measurement identifier, a subscription.
It needed no new machinery, only a profile.

**EVSOC** looks like the same thing and is not. Its measurements are a state of charge in per
cent, a state of health in per cent and a travel range in metres, and:

* none of them is on a phase. Table 6 lists **no electrical connection parameter description at
  all** — no phase, no rms variant, no voltage type, because none of those words means anything
  about how full a battery is;
* two of the three carry **no commodity type**. Table 7 names `electricity` for the state of
  charge and leaves the element out for the other two, which is the document declining to call the
  health of a battery a measurement *of electricity*;
* its scenario 2, the nominal capacity of the battery, is **not a measurement**. It is an
  `electricalConnectionCharacteristic` with the context `entity` — what the car is, not what it is
  doing.

Our monitored-device base did all three of those things unconditionally, because until now every
use case using it measured a wire.

The second half of the work package was **EVCC**, and it is the use case which finally makes the
shared-feature problem of ADR 0006 the normal case rather than an edge one. A real electric
vehicle is one SPINE entity playing four or five server actors at once — EVCC says what it is,
EVCEM says what is going into it, EVSOC says how full it is, OPEV curtails it — and SPINE allows
at most one feature of a given feature type and role per entity. Three of those write to the same
**electrical connection** feature and two to the same **measurement** feature.

## Decision

1. **Whether a monitoring use case measures an electrical connection is a fact about the use case,
   and it is on the profile.** `MonitoringProfile.ElectricalParameters` is true for MPC, MGCP and
   EVCEM and false for EVSOC. When it is false, the monitored device publishes no parameter
   descriptions and does not claim the electrical connection feature at all; the watching side
   tolerates their absence rather than requiring them.

   The alternative — publishing a parameter description for a state of charge, with some phase in
   it — was rejected for the same reason the phantom quantity was rejected in WP09/4: it would put
   a statement on the wire which the specification does not make and which is not true.

2. **The commodity type is a fact about the quantity**, defaulting to electricity and set to null
   where the document leaves it out.

3. **A scenario which is supported without being measured is declared through `AlsoSupports`**, the
   door MGCP scenario 1 already uses. EVSOC scenario 2 goes through it and publishes an electrical
   connection *characteristic*.

4. **Where a use case has no mandatory scenario but still cannot be silent**, the profile says so.
   EVCEM asks for "at least one of Scenario 1, 2 or 3, as all 3 scenarios measure electricity and
   can be converted into each other", so `AtLeastOneScenario` makes an EV publishing none of them
   an error rather than a device which supports a use case vacuously.

5. **Identifier allocation is one helper, `UseCaseIds.NextFree`, and everything which writes to a
   shared list function uses it.** Measurement identifiers, parameter identifiers, characteristic
   identifiers, configuration key identifiers, identification identifiers, load control limit
   identifiers. The rule from ADR 0006 — read, pick what is free, append — is now the only way
   these are written.

6. **"Somebody else's entry" is decided on the identifier being absent, not on it being zero.**
   `measurementId ?? 0` was the old test and it is wrong in the one case which matters: EVCC's
   charging power limit parameter has *no* measurement identifier, and the lowest free identifier
   really can be zero.

## Consequences

* EVSOC publishes three measurement descriptions and one characteristic, and nothing which claims
  a phase. `EVSOCTests.NoneOfTheseMeasurementsIsOnAWire` and
  `OnlyTheStateOfChargeIsAMeasurementOfElectricity` hold it.
* Points 5 and 6 were **defects**, and one of them was in code written two work packages ago:
  `OPEVElectricVehicle` replaced the whole electrical connection parameter list, the whole
  permitted value set list and the whole load control limit list, with identifiers hardcoded from
  zero, and assigned `WriteApproval` rather than chaining it. On a car which also runs EVCC that
  silently deleted the charging power limits — the minimum charging power an energy manager needs
  in order not to throttle a car into stopping. It was unreachable until this work package put a
  second use case on an EV entity, and `EMobilityCoexistenceTests.OPEVCanCurtailTheSameCar` found
  it on the first run.
* The commissioning use cases got a shared layer of their own (`UseCases/Commissioning/`) for the
  two facts every one of them carries — the manufacturer data and the operating state. EVSECC is
  those two and nothing else; EVCC is those two plus what only a car has.
* The scenario record is now one type. `MonitoringScenario` is gone and `UseCaseScenario` grew a
  `Mandatory` flag, so a profile and the list an actor actually supports are the same shape.
* Expected to carry forward: OSCEV (WP09/6) is another load control use case on the EV entity and
  will land next to OPEV on the same feature. Point 5 is what makes that a three-line subclass
  rather than a fifth copy of the same bug.
