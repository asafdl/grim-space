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
   +----> personality contributions ----+
                                       |
Background --> optional contributions -+--> derived personality
                                                |
                         +----------------------+----------------------+
                         |                      |                      |
                  bark selection         gameplay rules        future events
                  (atmosphere)
```

Backgrounds retain their existing capability effects independently of any
personality contribution.

### Starting dispositions

Use a small game-specific vocabulary rather than the Big Five. The initial
candidate dimensions are:

| Disposition | What it describes |
|-------------|-------------------|
| Fearfulness | Sensitivity to danger and perceived risk |
| Aggression | Inclination toward violence and confrontation |
| Compulsiveness | Susceptibility to cravings, obsessions, and temptation |
| Loyalty | Inclination to prioritize the crew over personal interests |

These are working dimensions, not finalized content or good/bad alignments.
Low fearfulness is not automatically aggression; compulsiveness is not itself an
addiction. Specific addictions belong in quirks.

The absurdity comes from situations and expression, not from making the model
psychologically elaborate.

### Derivation and queries

Proposed first implementation:

- Derive a read-only personality value from the pilot's current quirks and,
  if adopted, background contributions.
- Use one shared composition rule for all consumers.
- Do not separately persist mutable personality scores or roll hidden baseline
  personality unless a concrete design need emerges.
- Keep calculation deterministic and independent of Godot, bark randomness,
  scene lifetime, and presentation timing.
- Keep disposition values separate from a consumer's response policy. Dialogue
  decides how disposition influences a reaction; an event decides how it affects
  that event's outcome.

Choose the numeric range, neutral baseline, and combination/clamping rule before
implementation. The first version only needs simple, testable contributions, not
a general-purpose personality rule engine.

Future event definitions should query personality for disposition-based outcomes,
not reproduce the mapping from quirk IDs to personality. An event explicitly
about a named quirk may still check that quirk.

### Barks

Keep the existing separation between situation and reaction:

1. A situation supplies eligible bark flavors.
2. The chosen pilot's personality influences the relative selection weights.
3. Randomness selects a flavor and a line.
4. Existing presentation plays the selected decision.

An anxious pilot should favor frightened reactions to danger, but not speak the
same line every time. Personality does not make every reaction valid in every
situation.

`Scared`, `Angry`, `Smug`, and the other existing flavors remain dialogue
categories; they are not personality dimensions. Some dispositions may require
new or revised dialogue before their influence is perceptible.

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
    -> change passive effects and derived personality
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
| Quirk definitions | [PilotQuirkDefinition](../src/pilots/PilotQuirkDefinition.cs), [PilotQuirkCatalog](../src/pilots/PilotQuirkCatalog.cs), [EPilotQuirkId](../src/pilots/EPilotQuirkId.cs): two placeholder quirks, empty capability lists, empty recruitment pools | Add personality contributions, expand content, and enable recruitment only for ready quirks |
| Pilot identity | [Pilot](../src/run/Pilot.cs), [PilotRecruitProfile](../src/pilots/PilotRecruitProfile.cs): one nullable positive and one nullable negative quirk | Replace singular slots with validated, defensively owned collections; enforce four per polarity and no duplicate IDs |
| Roster authority | [RunPilotRegistry](../src/run/RunPilotRegistry.cs): owns pilot instances, validates roster updates, aggregates capability grants | Preserve this boundary for pilot replacement and enumerate all quirks for capability queries |
| Personality | No shared model currently exists | Add a small Godot-free value model and composition logic under `src/pilots/`; suggested name: `PilotPersonality`. Keep related calculation together rather than introducing a separate manager/service |
| Backgrounds | [PilotBackgroundDefinition](../src/pilots/PilotBackgroundDefinition.cs), [PilotBackgroundCatalog](../src/pilots/PilotBackgroundCatalog.cs): identity and capabilities | Optionally add contributions using the same personality vocabulary |
| Recruitment and starters | [PilotRecruitment](../src/world/star-system/merchants/PilotRecruitment.cs), factory methods on [Pilot](../src/run/Pilot.cs) | Generate bounded starting collections; retain seeded recruitment streams and intentional starter identities |
| Passive effects | [PilotCapability](../src/pilots/PilotCapability.cs), roster capability queries | Reuse quirk-sourced capability grants; personality does not replace this path |
| Battle passives | [BattlePassiveStatusInstaller](../src/battle/statuses/BattlePassiveStatusInstaller.cs), [BattlePilotCapabilityPipelineFactory](../src/battle/pilot-capabilities/BattlePilotCapabilityPipelineFactory.cs) | Implement relevant battle passives through existing statuses, not a new quirk modifier pipeline |
| Strategic passives | [StarMapPilotCapabilityPipelineFactory](../src/world/star-system/pilot-capabilities/StarMapPilotCapabilityPipelineFactory.cs), [RefreshStrategicPilotCapabilitiesAction](../src/world/star-system/actions/RefreshStrategicPilotCapabilitiesAction.cs) | Extend only for selected strategic effects and refresh derived grants after quirk changes |
| Dialogue | [PilotBarkDirector](../src/pilots/barks/PilotBarkDirector.cs), [PilotBarkCatalog](../src/pilots/barks/PilotBarkCatalog.cs), [EBarkFlavor](../src/pilots/barks/EBarkFlavor.cs) | Resolve each pilot's personality from authoritative inputs and weight eligible flavors |
| Run coordination | [State](../src/run/State.cs): roster, committed-outcome listeners, capability refresh, battle snapshot construction, bark director lifetime | Coordinate committed quirk changes and dependent projections without becoming the personality rule owner |
| Identity UI | [PilotIdentityIconRail](../src/world/star-system/presentation/ui/PilotIdentityIconRail.cs), [PilotCharacterSheetOverlay](../src/world/star-system/presentation/ui/PilotCharacterSheetOverlay.cs), hiring UI | Display multiple quirks with usable layout and descriptions; no personality sliders or score display |
| Persistence | [PilotPersistenceDto](../src/run/persistence/dtos/PilotPersistenceDto.cs), [SaveGameDocument](../src/run/persistence/SaveGameDocument.cs) | Persist quirk collections and any later per-quirk state; explicitly version the schema change |
| Encounter boundary | [PilotBattleSnapshot](../src/battle/encounter/PilotBattleSnapshot.cs), [PilotBattleSnapshotDto](../src/run/persistence/dtos/PilotBattleSnapshotDto.cs), [SaveDtoMapper](../src/run/persistence/SaveDtoMapper.cs) | Extend only when battle rules actually require personality; carry derived encounter inputs instead of reading the live run roster from battle rules |
| Future world events | No general system | Define event state/actions/outcomes in the owning domain when concrete events are selected; consume the shared personality model |

### State and dependency boundaries

- The run roster remains authoritative for pilot identity and acquired quirks.
- Personality is derived domain data, not a second pilot registry.
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

- Hiring currently creates candidates without quirks, whereas general pilot and
  starter factories assign the two placeholders. Update all creation paths
  deliberately rather than only enabling catalog pools.
- Both existing quirks explicitly advertise unimplemented effects. A personality
  contribution alone does not fulfill their promised passive behavior.
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
- Current save loading requires an exact format-version match. Decide explicitly
  whether to migrate old singular quirks or reject the previous format; do not
  silently deserialize old pilots as having no quirks.

## Execution order

Each phase is a separate implementation increment, with review before continuing.
Tests accompany each phase rather than being deferred to the end.

### 1. Multi-quirk foundation

Replace singular slots across pilot identity, recruit profiles, roster capability
aggregation, persistence, creation paths, fixtures, and identity presentation.
Enforce polarity, uniqueness, and four-per-polarity limits at the domain boundary.
Keep existing starter quirks and hiring behavior intentional; do not introduce
campaign acquisition yet.

**Exit:** zero through four quirks of each polarity survive creation, roster
updates, copying, save/load, and UI projection; invalid input fails explicitly.
All quirk grants reach the existing capability consumers.

### 2. Shared hidden personality

Finalize the initial dimensions and simple composition rule. Add contribution
metadata to quirks and optionally backgrounds; derive personality from current
pilot identity. Start with a small content set, not a large quirk catalog.

**Exit:** composition is deterministic, neutral inputs work, quirk changes alter
the result, and all consumers can use the same Godot-free model. No independently
persisted personality state or player configuration UI exists.

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
  recruitment, capability grants, catalog data, personality derivation, and barks.
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

- Final dispositions, numeric scale, contribution strength, and bark weights.
- Whether backgrounds influence personality in the first version.
- Initial quirk content and starting quirk counts/distribution.
- Acquisition triggers, rates, incompatibilities, and full-slot behavior.
- Allowed paid alterations, resource prices, and any locking/reinforcement.
- Concrete gameplay behaviors and world events.

These should be resolved at the relevant phase, not expanded into speculative
systems before the first playable personality-to-bark connection.
