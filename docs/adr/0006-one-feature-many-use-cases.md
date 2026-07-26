# ADR 0006: one feature, many use cases — profiles instead of copies

**Status:** accepted (2026-07-26, WP09/4)

## Context

WP09/4 asked for two more use cases: the **limitation of power production** (LPP) and the
**monitoring of a grid connection point** (MGCP). Reading them next to the two we already had made
the shape of the work obvious:

* LPP is LPC with three words changed — `produce` instead of `consume`,
  `failsafeProductionActivePowerLimit` instead of the consumption key, and
  `powerProductionNominalMax` instead of the consumption maximum. The rule numbers match one for
  one: [LPP-919] and [LPC-919] are the same sentence about the same state.
* MGCP is MPC pointed at the boundary between a building and the grid. Different scopes, a
  different scenario numbering, and one scenario which is not a measurement — but the same
  descriptions, the same join by measurement identifier, the same subscription.

Copying each of them would have been quicker and is what the reference implementation does
(`eebus-go` has separate `cs/lpc`, `cs/lpp`, `ma/mpc`, `ma/mgcp` packages, largely duplicated). It
would also mean that the twelve transitions of the power limitation state machine exist twice, and
that a fix to the description join is a fix in two places — in a test bench, where the state machine
*is* the thing under test, that is the wrong trade.

The second question only appeared once the first was answered. Both new use cases make it normal
for **one entity to play two use cases at once**:

* a battery is limited in both directions, so it is the controllable system of LPC *and* of LPP;
* the meter at a grid connection point is regularly the monitored unit of MPC as well as the grid
  connection point of MGCP.

SPINE allows **at most one feature of a given feature type and role per entity** (1.3.0, Table 21
and the note beneath it). So those pairs do not get a feature each. They share one.

## Decision

1. **A use case which differs from another only in its vocabulary is expressed as a profile, not as
   a copy.** `PowerLimitationProfile` carries what tells LPC from LPP (the direction, the failsafe
   key, the two nominal maxima, and the rule prefix so messages quote the right document);
   `MonitoringProfile` carries what tells MPC from MGCP (the actors, the scenarios and which
   scenario a measured scope belongs to). The shared behaviour lives once, in
   `UseCases/LimitationOfPower/` and `UseCases/Monitoring/`; `LPC/`, `LPP/`, `MPC/` and `MGCP/` are
   the vocabulary plus three-line subclasses.

2. **Everything which writes to a feature assumes it is sharing it.** Concretely:

   * `SPINELocalFeature.AddFunction` no longer replaces a function which is already offered. It
     keeps the data and **combines** the two declarations' `possibleOperations`: what one use case
     needs writable stays writable for both.
   * A use case which adds an entry to a list function picks the **lowest identifier nobody else is
     using** and **appends** rather than replacing — limit ids, configuration key ids,
     characteristic ids, measurement ids.
   * `SPINELocalFeature.WriteApproval` is **chained** rather than assigned. Every use case on the
     feature is asked, and a refusal from any of them stands.

3. **What is genuinely shared is shared, not duplicated.** The failsafe *duration minimum* is one
   key even when a battery runs both power limitation use cases: how long a device can ride through
   without an energy guard is a property of the device, not of a direction. The failsafe *limit* is
   per direction and gets a key each.

4. **A scenario which is supported without being measured is declared explicitly.** MGCP scenario 1
   publishes a configuration value rather than a measurement, so `AMonitoredDevice` takes an
   `AlsoSupports` list. The alternative — inventing a quantity nobody publishes so that the scenario
   is inferred — would have put a meaningless measurement description on the wire.

## Consequences

* The power limitation state machine and its twelve transitions exist once and are tested once,
  against both profiles.
* Point 2 was not refactoring for its own sake. All three sub-points were **defects**, each
  reachable for the first time in this work package and each now pinned by a test: `AddFunction`
  silently emptied a function the other use case had filled in and narrowed what the feature
  announced it could do; both the power limitation and the monitoring server wrote their
  descriptions over whatever was there, with identifiers starting from one either way. On a real
  battery or a real grid meter every one of them would have been an interoperability bug, and the
  device would have announced half of what it offered without erroring anywhere.
* A test which wants a feature that does **not** offer some capability can no longer arrange it by
  re-declaring the function; it has to say so outright
  (`AddFunction(...).Operations = PossibleOperationsType.ReadAndMaybeWrite(...)`). One SPINE test
  did exactly that and now says what it means.
* The cost is indirection: reading what LPP does means reading `APowerLimitationControllableSystem`
  and the production profile rather than one file. That is accepted — the alternative is two files
  which drift.
* The same shape is expected to carry the remaining monitoring use cases (MOB, MOI, MPS) and the
  remaining limitation ones without a third and fourth copy.
