# Pilot personality and quirks

Status: design direction and high-level execution plan. This document does not
implement gameplay changes. Proposed details below remain open to revision.

## Goals

- Make pilots recognizable and relatable despite sharing a class and skills.
- Give players recruitment and development goals: find promising pilots and
  invest in the individuals they want to keep.
- Connect character identity to the game's grimdark, absurd atmosphere.
- Establish a personality model that dialogue, gameplay, and future world events
  can consume consistently.

The reference is the Darkest Dungeon-style quirk loop, not a freely configurable
personality build.

## Agreed direction

### Quirks

- Positive and negative quirks, with a maximum of **four of each per pilot**.
- Pilots can start with quirks and acquire them while campaigning.
- Hiring rolls 0, 1, or 2 total quirks with equal count probabilities, then selects
  uniformly without duplicates from the quirk catalog. Any polarity mix is allowed;
  the count is capped by catalog size.
- Altering quirks costs resources.
- Quirks can provide passive gameplay effects, personality effects, or both.
- Desirable combinations include strengths worth investing in and weaknesses the
  player is willing to accommodate.

### Personality

- A backend system, not a player-facing stat sheet or configuration screen.
- Players encounter personality through quirks, dialogue, behavior, and events.
- Quirks influence personality; backgrounds may also influence it.
- Personality can affect **atmosphere, gameplay, and events**.
- Personality and quirks are related but are not the same system.

### Immediate scope

There is no general world-event system yet. Existing randomized pilot barks are
the first consumer for proving personality in play. Do not build an event
framework before that connection is useful.

## Proposed model

```text
Quirks ---------------------> passive capability grants
   |
   +----> optional personality modifier -+
                                        |
Background --> optional modifier -------+--> effective personality
                                        |
Pilot-owned baseline -------------------+
   ^
   +---- future committed world-event changes
                                                |
                         +----------------------+----------------------+
                         |                      |                      |
                  bark selection         gameplay rules        future events
                  (atmosphere)
```

Backgrounds retain their existing capability effects independently of any
personality contribution.

### Starting dispositions

Use three independent, game-specific axes rather than the Big Five. Each is an
integer from **-5 to +5**, with **0 neutral**:

| Axis | -5 | +5 |
|------|----|----|
| Threat response | Fearful | Aggressive |
| Deliberation | Impulsive | Calculated |
| Allegiance | Self-interested | Team-loyal |

Threat response describes avoidance versus confrontation, not a claim that fear
and aggression cannot coexist. These axes are tendencies, not good/bad alignments
or guaranteed behavior. Specific addictions belong in quirks, not in deliberation.

[PilotPersonality](../src/pilots/PilotPersonality.cs) implements this immutable,
Godot-free value model. Out-of-range inputs are rejected rather than clamped;
the default value is neutral on all axes. Baseline personality is stored on
[Pilot](../src/run/Pilot.cs), carried through recruit profiles and pilot copies,
and saved with the run. `EffectivePersonality` combines that baseline with the
current background and quirk modifiers. Dialogue/gameplay consumers are not
connected yet.

The absurdity comes from situations and expression, not from making the model
psychologically elaborate.

### State and queries

Current foundation and future direction:

- Every pilot starts with a neutral stored baseline.
- Backgrounds and quirks have an optional `PersonalityModifier`, defaulting to
  zero, independently of their passive capability grants.
- Contributions use [PilotPersonalityModifier](../src/pilots/PilotPersonalityModifier.cs),
  which accepts the full integer range on each axis. The stored baseline and
  effective personality remain bounded -5 through +5.
- Effective personality is baseline plus the current background and quirks.
  Sum all contributions before clamping each axis to -5 through +5. Repeated reads
  never mutate the baseline; removing a quirk removes its contribution.
- Large contributions are not individually clamped: -5 and -8 produce an
  effective -5, but adding +10 to those same sources produces -3. Summation uses
  wider integer accumulators so large positive/negative modifiers cancel safely.
- Future committed world events can change the baseline through the run boundary.
- Use the existing roster update boundary to replace immutable pilot values.
- Keep personality changes deterministic and independent of Godot, bark randomness,
  scene lifetime, and presentation timing.
- Keep disposition values separate from a consumer's response policy. Dialogue
  decides how disposition influences a reaction; an event decides how it affects
  that event's outcome.

Background modifiers are authored in the catalog, including Gear Psycho
(+3 threat response) and Bean Counter (-2 threat response, +3 deliberation).
Existing quirks currently contribute zero. No separate pilot-status registry or battle-status reuse is
needed: source definitions own the modifiers, and the pilot resolves them.

Future event definitions should query personality for disposition-based outcomes,
not reproduce the mapping from quirk IDs to personality. An event explicitly
about a named quirk may still check that quirk.

### Barks

Keep the existing separation between situation and reaction:

1. A situation supplies eligible bark flavors.
2. The chosen pilot's effective personality constrains eligible responses and
   determines their relative selection weights.
3. Randomness selects a flavor and a line.
4. Existing presentation plays the selected decision.

An anxious pilot should favor frightened reactions to danger, but not speak the
same line every time. Personality does not make every reaction valid in every
situation.

The catalog has exactly six personality-expression flavors:

| Axis | Negative-side flavor | Positive-side flavor |
|------|----------------------|----------------------|
| Threat response | Anxious | Bloodthirsty |
| Deliberation | Impatient | Analytical |
| Allegiance | Entitled | Supportive |

These replace the old situational/emotional flavors. Their lines express the
corresponding attitude; they are not additional personality stats.

For each situation's offered flavor, inspect its corresponding axis:

- Opposing side: weight 0 (ineligible).
- Neutral axis: weight 1 for either side.
- Aligned side: weight equals the squared axis strength.

Roll proportionally among positive weights. If all offered responses have zero
weight, the selected pilot stays silent; no replacement speaker is rolled.
At +5 threat response and 0 deliberation, Bloodthirsty and Analytical have weights
25 and 1 (approximately 96% and 4%). Fully neutral pilots retain uniform selection.

The rule and generic response-index selection live on
[PilotPersonality](../src/pilots/PilotPersonality.cs), with
[EPilotPersonalityAxis](../src/pilots/EPilotPersonalityAxis.cs) identifying axes.
Consumers supply their options' axis and side; the selector has no bark, event,
world, or Godot dependencies. Future events can reuse it with their own options
and explicitly controlled random source. A null selection means no compatible
response; the owning event must decide what that means, rather than assume silence.

[PilotBarkCatalog](../src/pilots/barks/PilotBarkCatalog.cs) maps flavors to these
affinities. Run-bound bark selection reads current effective personality from the
authoritative roster; standalone previews intentionally use neutral personality
unless supplied a reader. Missing run pilots are errors, not neutral fallbacks.
The same selection path serves strategic, battle-hit, and enemy-kill barks. Selected decisions
are played without rerolling, even if personality changes before presentation.

Enemy-kill barks run during turn replay, after impact/death animation cleanup and
before replay advances. A lethal `ImpactFacts` against an enemy offers
Bloodthirsty, Analytical, Entitled, and Supportive to one through three living
squad pilots (not just the killer). Each selected pilot has the existing 20%
battle speaking chance; personality filtering/weighting applies. Nonlethal
enemy hits and player deaths do not trigger this pool. Like hit reactions, kill
barks ignore strategic cooldowns and do not start them, and reuse the existing
camera/speech playback sequence. Lines still come from shared flavor pools.

Ally-destroyed barks use the same post-death replay timing. A lethal impact against
a player ship offers Anxious, Bloodthirsty, Entitled, and Supportive to one through
three surviving squad pilots at 20% chance each, with personality filtering and
no strategic cooldowns. Replay replaces the ordinary player-hit reaction with
this reaction for lethal impacts, rather than playing both. The destroyed pilot
is excluded by replay's living-ship projection; with no survivors, nobody speaks.

Initially preserve speaker selection, bark chance, cooldowns, group sizes,
scripted lines, and the selection/playback split. Change reaction selection, not
all dialogue behavior at once. Personality-weighted speaker selection can wait
until there is a demonstrated need.

### Campaign development and resource investment

The intended loop is:

```text
Recruit a promising pilot
    -> campaign with them
    -> acquire positive/negative quirks
    -> change passive effects and pilot personality
    -> spend resources to alter unwanted or desirable combinations
    -> keep developing that individual
```

Acquiring quirks does not require the future general event system. A small
acquisition policy can consume selected committed campaign outcomes first.
Experience-related acquisition is a possible direction, not a requirement that
every quirk needs a bespoke trigger.

Exact resource services, costs, replacement rules, and any locking/reinforcement
mechanic are undecided. Do not treat the Darkest Dungeon reference as approval
to implement every mechanic from that game.

## Current code and extension points

Paths below describe the current implementation; suggested new types are not
existing APIs.

| System | Current code and behavior | Planned extension |
|--------|---------------------------|-------------------|
| Quirk definitions | [PilotQuirkDefinition](../src/pilots/PilotQuirkDefinition.cs), [PilotQuirkCatalog](../src/pilots/PilotQuirkCatalog.cs), [EPilotQuirkId](../src/pilots/EPilotQuirkId.cs): optional personality modifiers and capability grants; hiring rolls from the full catalog | Expand quirk content and contributions |
| Pilot identity | [Pilot](../src/run/Pilot.cs), [PilotRecruitProfile](../src/pilots/PilotRecruitProfile.cs): read-only positive and negative collections, validated for polarity, uniqueness, and four per side | Later add acquisition and paid alteration through the authoritative boundary |
| Roster authority | [RunPilotRegistry](../src/run/RunPilotRegistry.cs): owns pilot instances, validates roster updates, aggregates capability grants | Preserve this boundary for pilot replacement and enumerate all quirks for capability queries |
| Personality | [PilotPersonality](../src/pilots/PilotPersonality.cs): immutable three-axis model and modifier composition; [Pilot](../src/run/Pilot.cs) owns the baseline and exposes effective personality | Future events change the baseline through the roster boundary; no separate personality registry |
| Backgrounds | [PilotBackgroundDefinition](../src/pilots/PilotBackgroundDefinition.cs), [PilotBackgroundCatalog](../src/pilots/PilotBackgroundCatalog.cs): identity, capabilities, optional personality modifiers | Expand contributions as content is decided |
| Recruitment and starters | [PilotRecruitment](../src/world/star-system/merchants/PilotRecruitment.cs), factory methods on [Pilot](../src/run/Pilot.cs) | Generate bounded starting collections; retain seeded recruitment streams and intentional starter identities |
| Passive effects | [PilotCapability](../src/pilots/PilotCapability.cs), roster capability queries | Reuse quirk-sourced capability grants; personality does not replace this path |
| Battle passives | [BattlePassiveStatusInstaller](../src/battle/statuses/BattlePassiveStatusInstaller.cs), [BattlePilotCapabilityPipelineFactory](../src/battle/pilot-capabilities/BattlePilotCapabilityPipelineFactory.cs) | Implement relevant battle passives through existing statuses, not a new quirk modifier pipeline |
| Strategic passives | [StarMapPilotCapabilityPipelineFactory](../src/world/star-system/pilot-capabilities/StarMapPilotCapabilityPipelineFactory.cs), [RefreshStrategicPilotCapabilitiesAction](../src/world/star-system/actions/RefreshStrategicPilotCapabilitiesAction.cs) | Extend only for selected strategic effects and refresh derived grants after quirk changes |
| Dialogue | [PilotBarkDirector](../src/pilots/barks/PilotBarkDirector.cs), [PilotBarkCatalog](../src/pilots/barks/PilotBarkCatalog.cs), [EBarkFlavor](../src/pilots/barks/EBarkFlavor.cs) | Resolve each pilot's personality from authoritative inputs and weight eligible flavors |
| Run coordination | [State](../src/run/State.cs): roster, committed-outcome listeners, capability refresh, battle snapshot construction, bark director lifetime | Coordinate committed quirk changes and dependent projections without becoming the personality rule owner |
| Identity UI | [PilotIdentityIconRail](../src/world/star-system/presentation/ui/PilotIdentityIconRail.cs), [PilotCharacterSheetOverlay](../src/world/star-system/presentation/ui/PilotCharacterSheetOverlay.cs), hiring UI | Display multiple quirks with usable layout and descriptions; no personality sliders or score display |
| Persistence | [PilotPersistenceDto](../src/run/persistence/dtos/PilotPersistenceDto.cs), [SaveGameDocument](../src/run/persistence/SaveGameDocument.cs): personality and quirk collections persisted; battle unit faction persisted in format 13; older formats rejected by existing load policy | Persist any later per-quirk state; explicitly version future schema changes |
| Encounter boundary | [PilotBattleSnapshot](../src/battle/encounter/PilotBattleSnapshot.cs), [PilotBattleSnapshotDto](../src/run/persistence/dtos/PilotBattleSnapshotDto.cs), [SaveDtoMapper](../src/run/persistence/SaveDtoMapper.cs) | Extend only when battle rules actually require personality; carry derived encounter inputs instead of reading the live run roster from battle rules |
| Future world events | No general system | Define event state/actions/outcomes in the owning domain when concrete events are selected; consume the shared personality model |

### State and dependency boundaries

- The run roster remains authoritative for pilot identity and acquired quirks.
- Baseline personality belongs to the pilot in the authoritative run roster.
  Effective personality is a disposable derivation, not separately saved state.
- Presentation can read personality but cannot apply gameplay consequences.
- Paid alterations must validate resource expenditure and identity changes through
  an explicit owning-domain operation. Do not debit in UI and update the pilot
  independently.
- Follow existing committed-fact handoffs across subsystem boundaries; simulation
  previews must not mutate the roster, spend resources, or grant quirks.
- If battle gameplay needs personality later, use encounter/runtime inputs and
  preserve fork/save behavior. Presentation-only barks do not justify adding it
  to every battle snapshot now.
- Nothing in this plan requires changing the generic simulation kernel.

### Existing limitations and deferred debt

- Hiring uses a dedicated seeded `quirks` stream per sale slot and rolls from
  [PilotQuirkCatalog](../src/pilots/PilotQuirkCatalog.cs).
- Charismatic grants fleet salary discount and a personality contribution.
  Neuro-drug Addict is personality-only for now; do not advertise unimplemented
  passive gameplay effects on it.
- Background descriptions include deferred behavior, including Synth Singer's
  addiction susceptibility. A personality contribution is not automatically an
  acquisition-probability rule; do not silently claim that promise is implemented.
- [BattleController](../src/battle/presentation/scene/BattleController.cs) has an
  existing boundary TODO about battle/agent leaks. Keep bark playback as
  presentation; do not put personality gameplay rules there.
- Other existing encounter TODOs concern grid duplication and cross-world state
  carryover. They are outside this work and are not prerequisites for barks.
- The roster currently has a temporary four-pilot cap. This plan does not add
  reserves or a forced-replacement mechanic. Refusing combat was an illustrative
  personality outcome, not an approved feature.
- Current save loading requires an exact format-version match. Format 13 rejects
  earlier saves explicitly (battle unit faction is now persisted).

## Execution order

Each phase is a separate implementation increment, with review before continuing.
Tests accompany each phase rather than being deferred to the end.

### 1. Multi-quirk foundation (implemented)

Collections now cover pilot identity, recruit profiles, roster capability
aggregation, personality contributions, persistence, creation paths, fixtures, and
identity presentation. Polarity, uniqueness, and four-per-polarity limits are
enforced at the domain boundary. Hiring rolls 0-2 total quirks from the catalog
without duplicates using an independent seeded stream. Campaign acquisition and
paid alterations are not implemented.

To author content, add IDs to [EPilotQuirkId](../src/pilots/EPilotQuirkId.cs) and
define them in [PilotQuirkCatalog](../src/pilots/PilotQuirkCatalog.cs). Definitions
accept optional `personalityModifier` just like backgrounds; every assigned quirk
automatically contributes to effective personality. New quirk icons must also be
mapped in the existing identity rail.

**Exit:** zero through four quirks of each polarity survive creation, roster
updates, copying, save/load, and UI projection; invalid input fails explicitly.
All quirk grants reach the existing capability consumers.

### 2. Shared hidden personality (foundation implemented first)

The neutral, three-axis personality foundation is implemented independently of
multi-quirk storage. Personality is preserved through recruitment, copying, and
run persistence. Background and quirk definitions support optional modifiers,
and pilots resolve effective personality from all current sources. Background
modifiers are enabled; quirk values and world-event changes remain future content.

**Foundation exit:** neutral creation, non-neutral copies, and save/load preserve
all three axes. Consumers can use the same Godot-free model; no player
configuration UI exists. Contributions sum before clamping and do not accumulate
across reads or save/load.

### 3. Personality-aware atmosphere

Use personality to weight eligible bark flavors for both strategic situations
and battle hit reactions. Preserve the existing selection/playback contract and
scripted dialogue path. Add or adjust enough bark content to make the selected
dispositions audible.

**Exit:** pilots with different dispositions produce measurably different
reaction distributions in the same context, while retaining variation and all
existing eligibility, cooldown, and replay constraints.

**First playable milestone:** the player sees different quirks and hears
recognizably different pilots. Evaluate this before building world events.

### 4. Campaign quirk acquisition and useful passives

Choose a small initial quirk set and campaign acquisition triggers. Implement
their advertised passives using existing capability/status owners. Apply
acquisition from committed campaign facts through the run boundary and refresh
dependent projections.

Decide full-slot behavior before enabling acquisition: rejection, replacement,
or a player decision are different mechanics and must not be chosen accidentally.
Choose any incompatibilities and acquisition limits at this stage.

**Exit:** campaign play can persistently change a surviving pilot's quirk set
without duplicate grants from repeated notifications, preview, or reload.
Passive effects and bark personality reflect the new identity.

### 5. Resource-based quirk alteration

Choose the initial service and allowed operations, prices, eligibility, and any
time/availability cost. Add the authoritative transaction and corresponding UI.
Locking or reinforcement is optional and needs a separate design decision.

**Exit:** accepted alterations charge exactly once and persist; invalid or
unaffordable requests change neither resources nor quirks. Personality and
capability projections update consistently.

**Character-development milestone:** recruitment, campaign changes, and resource
investment form the intended pilot attachment/customization loop.

### 6. Personality-driven world events and behavior

Design a few concrete events using the established dispositions. Then introduce
only the event state, choices, resolution, persistence, and presentation those
events require in the appropriate domain.

**Exit:** event rules consume shared personality, resolved consequences are
authoritative and saveable, and event presentation does not implement rules.
Do not begin with a universal event framework or automatic combat refusal.

## Validation strategy

- Extend [pilot tests](../grim-space.Tests/Pilots/) for identity validation,
  recruitment, capability grants, catalog data, personality state, and barks.
- Extend [run persistence tests](../grim-space.Tests/Run/) for the new quirk
  collections and the explicit save-version policy.
- Test personality-weighted selection with controlled random inputs and
  sufficiently large seeded samples with explicit tolerance; avoid flaky
  assertions and claims based on one favorable line.
- Preserve bark candidate eligibility, chance, cooldowns, scripted openings,
  select-without-speaking behavior, and presentation without rerolling.
- Add owning-domain tests for acquisition and paid alterations when those phases
  are implemented, including preview isolation, failed transactions, and repeated
  committed-fact delivery.
- Test our C# domain behavior and fixtures, not Godot framework internals.
- Run the smallest relevant test selection from
  `dotnet test grim-space.Tests/grim-space.Tests.csproj`, plus `dotnet build` for
  code changes; widen validation when shared contracts change.
- Manually review identity layout and audible pilot differences once presentation
  changes exist. This documentation-only change requires no runtime validation.

## Decisions deliberately left open

- Contribution strength, composition rules, and bark weights.
- Whether backgrounds influence personality in the first version.
- Initial quirk content (hiring count/distribution is defined above).
- Acquisition triggers, rates, incompatibilities, and full-slot behavior.
- Allowed paid alterations, resource prices, and any locking/reinforcement.
- Concrete gameplay behaviors and world events.

These should be resolved at the relevant phase, not expanded into speculative
systems before the first playable personality-to-bark connection.
