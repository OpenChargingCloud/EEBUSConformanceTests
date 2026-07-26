# ADR 0008: a partner may play more than one actor of the same use case

**Status:** accepted (2026-07-26, WP09/6)

## Context

WP09/6 added the last two e-mobility use cases: the **optimisation of self-consumption during EV
charging** (OSCEV) and the **coordinated EV charging** (CEVC).

**OSCEV** turned out to be the overload protection with two words changed — a `recommendation`
instead of an `obligation`, with the scope `selfConsumption` instead of `overloadProtection`. Same
three scenarios with the same numbers, same four-second heartbeat, same one-limit-per-phase joined
through a measurement identifier. So the same treatment as LPC/LPP and MPC/MGCP applied: a
`ChargingCurrentProfile`, with `UseCases/ChargingCurrent/` holding the shared behaviour and `OPEV/`
and `OSCEV/` holding the vocabulary.

One thing in that pair is behaviour rather than vocabulary and it follows from obligation versus
recommendation: **what an EV does when it stops trusting the other side.** Under the overload
protection it falls back to a safe current, because the fuse does not go away with the energy
guard. Under the self-consumption optimisation it simply stops applying the advice and charges as
it otherwise would — advice from a source which has gone quiet is not advice. Falling back to a low
safe current there would slow a charging session down because a photovoltaic forecast stopped
arriving, which is the opposite of what the use case is for.

**CEVC** is a different animal: 105 pages, three actors, and the two most demanding data models in
SPINE. It is also the use case which broke an assumption the framework had been making since WP08.

## Decision

1. **A remote entity's support for a use case is accumulated across the actors it announces, not
   decided by whichever entry is read last.**

   `AUseCase.Evaluate` used to call `Remember` once per matching use case information entry. The
   node management use case data is grouped by *actor*, so an entity playing two actors of one use
   case produces two entries — and the second overwrote the first. The car saw scenarios 3, 6 and 8
   and not 2, 5 and 7, and which half it lost depended on the order its partner happened to list
   its actors in.

   That is not exotic. A home energy manager with a tariff subscription is the **energy guard** and
   the **energy broker** of the coordinated EV charging at the same time, on one entity, hosting one
   heartbeat. Scenarios are now unioned per entity, and an entity counts as unavailable only when
   every actor of it says so.

2. **A time series which the specification marks `timeSeriesWriteable: false` is enforced, not just
   declared.** The demand is what the car wants and the plan is what the car intends; neither
   becomes truer for an energy guard having written it. The write approval refuses those two by
   identifier and lets the constraints curve through.

3. **`updateRequired` is implemented on both sides, because it is the only way a SPINE server can
   ask for anything.** The car is the server of all four CEVC data scenarios, and a server answers
   rather than requests. When it needs a fresh power limit curve or a fresh incentive table it
   raises the flag on the description its clients are subscribed to, and lowers it when the write
   arrives ([CEVC-015], [CEVC-030]). `AUseCase.SetScenarioSupported` is the same idea one level up:
   withdrawing one scenario while keeping the use case, which is what [OSCEV-009] asks of a car with
   a full battery.

4. **A list whose entries have no primary key is written whole.** SPINE 1.3.0, 5.3.4.1 allows only
   the exchange of a complete list for entries which cannot be addressed, and `incentiveTable` is
   one — its identity is inside `tariff.tariffId` rather than on the entry. The update engine
   refused our partial write and was right to; the fix was in the caller.

## Consequences

* Point 1 was a **defect in the use case framework itself**, present since WP08 and unreachable
  until a use case had two client actors. It is the third time a "one X per Y" assumption has cost
  something: WP09/4 found it for features on an entity, WP09/5 for identifiers in a shared list,
  and this is the same shape for actors at a partner. Held by
  `CEVCTests.AllThreeActorsFindEachOther`.
* The `ChargingCurrent` layer gave `OPEVElectricVehicle` a second correctness fix on the way past:
  when it reuses a phase description another use case already wrote, the limit description has to
  quote that parameter's **measurement** identifier rather than its parameter identifier. Those two
  numbers are equal in the parameters we write ourselves and unequal in one written by the
  electricity measurement use case, so the bug was invisible until a car ran both.
  `EMobilityCoexistenceTests.BothChargingCurrentUseCasesCanRunOnOneCar` also pins the thing that
  makes it worth doing at all: a car with three phases describes three of them, not six, however
  many use cases point limits at them.
* `OPEVTrust` became `ChargingCurrentTrust` and `ChargingCurrents` moved to the OPEV subclass, where
  it can stay non-nullable — an obligation nobody is currently stating is still an obligation. The
  OSCEV equivalent, `RecommendedCurrents`, is nullable, because "no advice" is a real answer.
* CEVC is implemented on all three actors but only as far as the use case specification goes. The
  incentive table is the simple one-tier, one-boundary, one-incentive shape the `simpleIncentiveTable`
  scope names; a broker which wants tiered power boundaries has the model available and no helper.
  That is deliberate — the shape the document specifies is the shape a conformance test asks about.
