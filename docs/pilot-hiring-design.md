# Pilot Hiring and Differentiation

## Status

Design direction. Exact background roster, skill restrictions, quirk catalog, candidate counts, pricing, and casualty probabilities remain to be designed and balanced.

## Purpose

Pilots are the player's run-scoped party. Ships are swappable combat equipment: they provide the chassis, weapons, armour, shields, movement profile, and installed capabilities.

Pilot hiring must create meaningful choices between candidates without adding numeric base stats merely for differentiation. A candidate's value should come from:

- immutable background and future skill access;
- current level and learned skills;
- one immutable positive quirk and one immutable negative quirk;
- the included ship and its loadout;
- hiring price and likely future treatment cost.

## Design goals

- Limit active combat complexity to at most four pilots.
- Allow a larger owned roster so losses do not immediately end a run.
- Make pilots mechanically distinct independently of their assigned ships.
- Make hiring an ROI decision rather than a linear upgrade purchase.
- Make an experienced pilot's death painful without necessarily destroying the run.
- Keep ship and pilot ownership separate after recruitment.
- Reuse black-market proposals as the source and refresh boundary for pilot offers.
- Preserve the existing authority boundaries: run state owns pilots and ships, pilot skill availability owns progression gates, and the star-system contract owns a black-market proposal.

## Non-goals

- Pilot base stats or per-level stat allocation.
- Fatigue and recuperation. Roster cycling still needs a meaningful mechanic, but fatigue will not be added as placeholder bookkeeping.
- Final background, skill, quirk, injury, pricing, or casualty balance.
- Background-specific passive rules or stat modifiers.
- Quirk mutation, treatment, removal, replacement, or locking.
- Permanent pilot-to-ship affinity or ship-type restrictions.
- Bespoke art, animation, or narrative content.

## Existing model

The current game already provides:

- persistent pilots with identity, portrait, XP, level, learned skills, ship assignment, and active status;
- freely swappable pilot-to-ship assignments;
- battle and strategic pilot skills;
- ships with chassis and tiered loadouts;
- an active pilot cap of four;
- black-market sale proposals with a merchant subtype, destination, and expiration;
- ship recruitment offers backed by the global star market.

The current implementation also couples concepts that this design must separate:

- The active cap of four is also effectively the total ship recruitment cap.
- Buying a ship silently creates a new level-one pilot.
- Destroying a player ship unconditionally removes both the ship and its pilot.
- The `Ships` merchant subtype currently represents the coupled ship-and-pilot recruitment flow.

## Core ownership model

### Pilot

A pilot owns:

- identity and portrait;
- background;
- XP and level;
- learned skills;
- quirks;
- later, injuries and recovery state;
- optional assignment to an owned ship;
- active or reserve status.

A pilot does not own ship combat statistics. Hull, shields, weapons, movement, initiative, and installed abilities remain properties of the ship.

### Ship

A ship remains an independently owned inventory item. It owns:

- chassis;
- loadout and gear tiers;
- current hull and shield state;
- installed and upgraded abilities.

After recruitment, the included ship may be assigned to any pilot and the recruited pilot may be assigned to any owned ship.

### Active party and reserves

- At most four pilots are active and therefore fielded.
- The run may own more than four pilots and more than four ships.
- Active deployment, pilot ownership, ship ownership, and pilot-to-ship assignment are separate concerns.
- A pilot must have an owned ship assignment to become active.
- Hiring while four pilots are active adds the recruit to the reserve roster rather than rejecting the purchase.

A separate total roster cap may be added if it produces useful decisions, but it is not implied by the active cap.

## Pilot backgrounds

Background is the pilot's primary immutable mechanical identity. It shapes skill progression without dictating equipment.

Each background defines:

- background-specific skills added to its learnable pool;
- shared skills unavailable to that background.

The effective learnable pool is:

> shared skill pool - background restrictions + background-specific skills

Existing level, prerequisite, and already-learned checks continue to apply after the background gate.

### Background design rules

Backgrounds must be distinct enough to affect both hiring and long-term party composition:

- Each background needs exclusive skills that enable or substantially strengthen a recognizable play pattern.
- Restrictions must create real opportunity cost, not remove skills the background would never want.
- Backgrounds should offer different tactical or strategic approaches rather than form a strict quality ladder.
- Backgrounds must remain useful across multiple chassis and loadouts.
- Backgrounds do not provide passive rules or stat changes in the first slice.

The initial background roster should be small. Three or four well-separated backgrounds are preferable to many backgrounds distinguished by minor skill-list changes.

Exact background names and exclusive skills should be designed against desired combat roles before implementation. Possible role space includes aggressive positioning, defensive control, weapon-system specialization, and strategic/economic utility, but these are not yet committed backgrounds.

Background-specific rules may be considered later when they create a clear identity that skill availability cannot provide. They are not required for every background and must use an existing authoritative rule surface rather than introduce generic pilot stats. Candidate pricing may account for background as offer-generation policy without becoming a persistent pilot modifier.

## Skills

Skills remain deliberate level-up choices and the main form of pilot power progression.

- Shared skills provide common build options.
- Background-specific skills provide identity and specialization.
- Background restrictions prevent all pilots from converging on the same optimal skill set.
- Learned battle skills continue to be frozen into the encounter snapshot.
- Learned strategic skills continue to be queried from authoritative run pilot state.

Pilot background gating belongs in the existing pilot skill availability rules. Battle, strategic, and presentation code must not reproduce background checks.

Skills and quirks are separate pilot-owned sources, but both grant pilot capabilities. Battle and strategic subsystems register the capability IDs they support. Passive capability registrations declare required action tags, and the generic action-effect pipeline performs the match before invoking effect-specific transformation logic. Runtime code therefore does not depend on whether a capability originated from a skill, quirk, or future background rule, and concrete modifiers do not name the actions they affect.

## Quirks

Quirks provide candidate-level variation and immediate hiring tradeoffs.

- Every generated candidate has exactly one positive quirk and one negative quirk.
- Both quirks are immutable in the first slice.
- Quirks should normally change how a pilot is used rather than act as generic stat quality.
- Quirks must not redefine background skill access.

Good quirk space includes conditional behavior tied to positioning, damage state, weapon use, ship condition, risk, resource recovery, or other existing rules. Broad percentage bonuses to universally valuable outcomes should be used sparingly because they create obvious best pilots.

Quirk acquisition, mutation, treatment, replacement, and locking are deferred. A future mutable-quirk system may add those mechanics, but the first slice must not require their state or UI.

## Pilot identity presentation

Backgrounds, positive quirks, and negative quirks are represented by compact icons wherever pilot identity is summarized. Hovering an icon explains its category, display name, description, and, for quirks, exact mechanical effect. This keeps roster and character-sheet views scannable without hiding the information needed to compare hires.

Each real background and quirk has its own distinct icon artwork; generic category or polarity icons are reserved for development sentinels. Positive and negative quirk slots also use consistent positive/negative framing or tint, but polarity must not depend on color alone; slot placement and tooltip category remain explicit. Icon resource mapping belongs to Godot presentation code rather than the domain catalogs, which remain engine-independent.

## No pilot base stats

Pilots will not initially have accuracy, defence, speed, initiative, or other upgradeable base stats.

Those properties are already represented by ships or discrete rules. Adding pilot stats now would:

- duplicate ship authority;
- introduce numeric variance without a demonstrated combat need;
- make candidates harder to evaluate;
- compete with backgrounds, skills, and quirks for the same design space.

A pilot stat should be introduced only when a concrete mechanic repeatedly requires a scalar that cannot be represented cleanly by a ship property, skill, or quirk.

## Pilot merchant

Pilots are acquired from a dedicated pilot merchant, separate from the ship merchant.

Add a `Pilots` merchant subtype to black-market sales. A pilot merchant sells unique pilot candidates, not repeatable chassis catalog entries.

Every pilot candidate initially includes a ship. The hire is a single atomic purchase, but the two acquired entities become independent immediately afterward.

### Pilot offer

A frozen pilot offer must contain enough information to display and execute the exact hire:

- stable offer ID;
- generated pilot identity and portrait;
- pilot background;
- starting XP, level, and learned skills;
- initial quirks;
- included ship declaration and loadout tier;
- total price.

The offer should present the pilot and ship portions clearly even though they have one combined price. The player needs to understand whether the contract is expensive because of the pilot, the ship, or both.

### Proposal-scoped stock

Black-market proposal generation is the restock boundary:

1. A new black-market proposal is generated.
2. If its subtype is `Pilots`, its candidate stock is generated and frozen.
3. The proposal retains that exact stock through acceptance, facility or space-meeting presentation, save/load, and reopening.
4. Purchasing a candidate removes that candidate from the proposal.
5. Expiration removes access to the remaining candidates.
6. A later black-market proposal generates fresh candidates.

Pilot offers belong to the black-market proposal, not the global star market. Generation should be deterministic from authoritative proposal inputs, while the frozen result should be persisted so later catalog or generation changes cannot alter an existing offer.

### Purchase transaction

Hiring must atomically:

1. Validate that the endpoint currently serves the `Pilots` subtype.
2. Resolve the stable offer from that proposal's remaining stock.
3. Validate resources and any total roster limit.
4. Deduct the price.
5. Create and register the included ship as player-owned.
6. Create and register the offered pilot assigned to that ship.
7. Keep the pilot in reserve unless activation is explicitly requested and legal.
8. Remove the offer from proposal stock.

A partial result, such as paying without receiving both entities, must not be possible.

## Ship merchant

The ship merchant remains separate and must not create pilots.

The current `Ships` recruitment flow implicitly creates a pilot and therefore conflicts with this design. It must be changed into a true ship-only purchase flow or withheld until ship-only purchasing is designed. There must be only one pilot acquisition path.

## Ship destruction and pilot casualties

Battle resolution treats the ship and pilot separately:

1. A ship reaches the destruction condition.
2. The destroyed ship is removed from player ownership.
3. The assigned pilot resolves a survival outcome.
4. A surviving pilot becomes unassigned.
5. A dead pilot is removed from the roster.

The intended eventual outcome range is:

- survives without lasting consequence;
- survives with an injury;
- dies.

Exact probabilities and modifiers are deferred. Pilot quality must not simply guarantee survival, but ship systems, skills, quirks, battle circumstances, or future rescue mechanics may eventually influence the result.

Injuries are separate from quirks:

- quirks express personality, habits, and mutable long-term tendencies;
- injuries express consequences of physical harm and may have recovery state.

## Hiring decisions

A candidate should not be reducible to a single quality score. The player evaluates:

- Does this class fill a missing progression role?
- Are the candidate's learned skills immediately useful?
- Does their positive quirk fit the player's tactics?
- Is their negative quirk tolerable?
- Is the included ship useful now or valuable as replacement equipment?
- Is the combined contract price better spent on another candidate or existing ships?
- Can the run absorb another reserve pilot?

Examples of useful offer shapes include:

- a desirable background with a poor ship and a manageable negative quirk;
- an inexperienced pilot bundled with an unusually good ship;
- an experienced pilot with strong learned skills but a difficult negative quirk;
- a modest ship attached to a candidate whose background completes the active party.

Offers should avoid straightforward good/better/best tiers where the only decision is affordability.

## Interaction with roster cycling

The design supports reserves but does not yet fully justify routine cycling.

Initially, cycling can occur because of:

- pilot death;
- future injuries and recovery;
- background or skill suitability for an anticipated encounter;
- ship availability and repair state.

Fatigue is explicitly deferred until it can create meaningful tactical or strategic decisions. It should not be added solely as an escalating number that forces a pilot to sit out.

## Implementation direction

Implementation should proceed in dependency order:

1. Decouple owned pilots and ships from the four-pilot active deployment cap.
2. Add immutable pilot background state and background-aware skill availability.
3. Add exactly one persisted positive and one persisted negative quirk per pilot, without treatment or mutation mechanics.
4. Add SVG identity presentation and explanatory hover tooltips for backgrounds and both quirks.
5. Add proposal-scoped pilot offers and the `Pilots` merchant subtype.
6. Implement atomic pilot-plus-ship hiring and remove implicit pilot creation from ship purchasing.
7. Split ship destruction from pilot casualty resolution.
8. Add injuries, treatment, quirk mutation, and roster-cycling mechanics only after their rules are designed.

Each step should preserve save/load, simulation fork behavior, action legality, and the existing authoritative ownership boundaries.

## Open design questions

- Initial background roster and exclusive skills.
- Which existing skills remain shared and which backgrounds restrict them.
- Initial positive and negative quirk catalogs.
- Final SVG artwork for each background and quirk.
- Number of candidates in one pilot proposal.
- Candidate generation ranges by market tier or run progression.
- Pilot, ship, and combined price calculation.
- Whether a total owned-pilot roster cap is useful.
- Casualty probabilities and modifiers.
- Injury duration and treatment.
- Whether a later slice should add quirk acquisition, treatment, replacement, or locking.
- Whether reserve pilots receive any progression.
- What information previews upcoming encounters sufficiently to support background-based deployment choices.
