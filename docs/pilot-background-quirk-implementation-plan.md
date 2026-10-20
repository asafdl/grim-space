# Pilot Background and Quirk Implementation Plan

## Audience

This is a handoff plan for an implementation agent continuing the current uncommitted pilot-capability refactor.

Read [`README.md`](../README.md#game-architecture-and-boundaries) and [`pilot-hiring-design.md`](pilot-hiring-design.md) before editing. The worktree already contains substantial capability changes; integrate with them and do not revert or recreate the previous skill-specific pipelines.

## Objective

Add the first infrastructure slice for pilot backgrounds and quirks:

- every pilot has one immutable background identity;
- the model has separate positive and negative quirk slots;
- skills and quirks remain distinct persisted sources;
- quirks may grant the same runtime capabilities as skills;
- battle and strategic runtimes remain ignorant of whether a capability came from a skill or quirk.

The first implementation pass must not invent final backgrounds or quirk effects. It should establish the authoritative state, catalogs, persistence, capability resolution, and tests needed to add real content once the designer supplies it.

## Current architecture

### Skills

`Pilot` persists learned `EPilotSkillId` values. `PilotSkillDefinition` owns progression metadata and a list of granted `EPilotCapabilityId` values.

Skills are progression sources; capability IDs name executable effects:

| Skill | Capability effect |
|---|---|
| Shields Up | `ShieldBoost` |
| Dodge This | `AdjacentDamageBonus` |
| Ionized Payload | `LightningShieldDamageBonus` |
| Defensive Maneuvers | `StationaryDamageDeflection` |
| In The Fray | `AlliedDamageReduction` |
| The Drones Yearn for the Mines | `ScrapRewardBonus` |
| I Know a Bot | `ShipUpgradeDiscount` |

Do not rename capability IDs back to skill names.

### Capability grants

`PilotCapabilityGrant` contains:

- effect-oriented capability ID;
- granting pilot ID;
- source kind;
- source ID.

`PilotCapabilityGrant.FromSkills` currently projects learned skills into grants. Quirks must extend this source projection rather than becoming skills.

### Action-tag routing

Capabilities do not carry metadata tags.

Battle and strategic subsystem factories declare:

- which capability IDs they support;
- which action tags a passive modifier requires;
- which concrete effect modifier implements the behavior.

`ActionTagEffectModifier` performs the generic match: every required tag must be present in `IAction.Tags` before the inner modifier runs. Concrete effect modifiers must not:

- name action classes;
- read action-specific nested tag constants;
- repeat action-tag applicability checks.

When a new action should receive an existing capability effect, the action publishes the shared subsystem action tags. The capability and modifier should not be edited.

### Runtime ownership

- `RunPilotRegistry` queries authoritative pilot-owned sources and produces grants.
- `State` snapshots battle grants into `PilotBattleSnapshot`.
- `ActorRuntime` stores frozen battle grants/capability IDs.
- `BattlePilotCapabilityPipelineFactory` installs battle modifiers.
- `StarMapPilotCapabilityPipelineFactory` installs active pilots' strategic modifiers.
- Active capabilities map explicitly to action definitions; action tags cannot create actions.
- Stateful target-side passives may still require explicit battle-state integration because effects receive only the acting runtime.

## Confirmed first-slice decisions

- Use **backgrounds**, not classes.
- Background is immutable.
- Background will eventually control skill availability:

  `shared skills - background restrictions + background-specific skills`

- Skill-tree/background gating is deferred.
- Backgrounds have no passive rule or stat modifier in this slice.
- Pilots eventually have exactly one positive and one negative quirk.
- Quirks are immutable in this slice.
- Every real quirk must have a gameplay effect.
- The designer has not supplied final background names or quirk effects yet.
- Do not add pilot base stats.
- Do not add fatigue, injuries, treatment, mutation, or locking.
- Do not implement pilot merchants in this slice.
- Save backwards compatibility is not required. Use one canonical DTO shape and do not add legacy fallback fields or migrations.

## Non-goals

- Background-aware skill availability.
- New skill-tree UI.
- Random candidate generation.
- Pilot merchant offers or black-market proposal stock.
- Roster/deployment-cap decoupling.
- Quirk treatment or mutation.
- Ship destruction survival rolls.
- Balance or final content.
- Generic metadata tags on capabilities.

## Required implementation sequence

Implement one phase at a time. After each phase, run its focused tests and stop for review before continuing.

## Phase 1: Harden action-tag capability routing

This phase verifies the current capability refactor before adding new sources.

### Work

1. Add focused tests for `ActionTagEffectModifier`:
   - all required tags present invokes the inner modifier;
   - one required tag missing returns the original effect;
   - unrelated extra tags do not prevent a match;
   - constructor rejects an empty tag set;
   - constructor rejects empty/whitespace tags.
2. Add a battle integration test using a non-lightning-cannon test action that publishes `Attack` and `Lightning` tags and produces `ApplyAreaDamageEffect`.
3. Verify `LightningShieldDamageBonus` applies without the capability code referencing `LightningCannonAction`.
4. Add an unmatched action test proving the same effect type is not modified without the required tags.
5. Preserve existing strategic tests for reward/salvage and merchant/fetch/catalog tag combinations.

### Acceptance criteria

- Applicability is proven to depend on tags, not concrete action types.
- Concrete modifier files contain only effect transformation and tuning constants.
- Existing skill behavior remains unchanged.

## Phase 2: Add background domain stubs

### Types

Add to `src/pilots/`:

- `EPilotBackgroundId`;
- `PilotBackgroundDefinition`;
- `PilotBackgroundCatalog`.

### Initial content

Until real backgrounds are supplied, add only an explicit `Unspecified` background. It is a temporary infrastructure sentinel, not a selectable/generated candidate background.

`PilotBackgroundDefinition` should contain only:

- ID;
- display name;
- description.

SVG paths are presentation metadata and must not be stored in this Godot-free domain definition.

Do not add:

- capability IDs;
- stat modifiers;
- pricing modifiers;
- skill inclusions/exclusions yet.

### Catalog behavior

Follow `PilotSkillCatalog` conventions:

- one authoritative dictionary;
- `Get`;
- `TryGet`;
- `All`;
- clear failure for an unknown ID.

### Tests

- `Get` returns the registered definition.
- Unknown IDs throw or fail consistently with `PilotSkillCatalog`.
- `All` includes every enum value intended as catalog content.

## Phase 3: Add quirk domain stubs

### Types

Add to `src/pilots/`:

- `EPilotQuirkId`;
- `EPilotQuirkPolarity` with `Positive` and `Negative`;
- `PilotQuirkDefinition`;
- `PilotQuirkCatalog`.

### Definition shape

`PilotQuirkDefinition` contains:

- ID;
- polarity;
- display name;
- description;
- granted capability IDs.

SVG paths are presentation metadata and must not be stored in this Godot-free domain definition.

Do not put action tags on quirks. Action tags belong to actions and subsystem capability registrations.

### Initial content

Until real quirks are supplied, add two explicit infrastructure sentinels:

- one positive unspecified quirk;
- one negative unspecified quirk.

Both grant no capabilities. They exist only to make the pilot state and persistence shape complete before content design. They must not appear in future generated candidate pools.

### Catalog invariants

- IDs are unique.
- Every definition has a non-empty name/description.
- Positive sentinel reports positive polarity.
- Negative sentinel reports negative polarity.
- Capability IDs are preserved in declared order.
- Do not require capability IDs to be non-empty while the sentinels exist.

### Tests

- Catalog lookup and unknown-ID behavior.
- Polarity correctness.
- Capability list preservation.

## Phase 4: Extend authoritative pilot state

### `Pilot` fields

Add immutable properties:

- `BackgroundId`;
- `PositiveQuirkId`;
- `NegativeQuirkId`.

### Constructor invariants

Validate:

- background exists in `PilotBackgroundCatalog`;
- positive quirk exists and has positive polarity;
- negative quirk exists and has negative polarity.

Do not allow callers to swap polarity slots.

### Creation

`Pilot.CreateForShip` continues to accept its optional RNG, but do not randomize backgrounds or quirks yet. Assign the explicit infrastructure sentinels.

Do not add a parallel pilot factory solely for this step. The future pilot-offer generator will provide real identities through a deliberate construction boundary.

### Immutable-copy audit

Pass all three new fields through every existing reconstruction path:

- `WithExperience`;
- `WithAssignment`;
- `WithActive`;
- `LearnPilotSkill` / `AppendLearnedSkill`;
- `Clone`;
- persistence restore.

Missing any one of these paths will silently reset identity and is a release-blocking bug.

### Tests

Extend `PilotRegistryTests` and progression/skill tests to prove:

- starter pilots receive all three sentinel identities;
- XP updates preserve them;
- assignment and activation changes preserve them;
- skill learning preserves them;
- cloning preserves them;
- polarity violations are rejected through the available construction/restore boundary.

## Phase 5: Persist background and quirks

### DTO

Extend `PilotPersistenceDto` with:

- background ID;
- positive quirk ID;
- negative quirk ID.

Use required canonical fields. Do not make them nullable, add defaults, retain a legacy DTO, or add a save migration.

### Mapping

Update `Pilot.ToPersistence` and `Pilot.FromPersistence`.

Restore must flow through the same catalog/polarity validation as normal pilot construction. Invalid saves should fail explicitly rather than silently substituting sentinels.

### Tests

- run snapshot round-trips all three IDs;
- save/load round-trips all three IDs;
- map regeneration preserves all three IDs;
- invalid quirk polarity in persisted data fails explicitly.

## Phase 6: Generalize capability-source resolution

### Goal

Run code should resolve every pilot-owned source into grants before handing them to battle or strategic consumers.

### Grant projection

Keep `PilotCapabilityGrant.FromSkills`, and add a quirk projection method on the same type rather than creating a one-function service class:

- input: pilot ID and quirk IDs;
- lookup: `PilotQuirkCatalog`;
- output: one grant per declared capability;
- source kind: `Quirk`;
- source ID: quirk ID string.

Backgrounds grant no capabilities in this slice.

### Registry aggregation

`RunPilotRegistry.QueryCapabilities` currently resolves learned skills. Extend it to concatenate:

1. learned-skill grants;
2. positive-quirk grants;
3. negative-quirk grants.

Then apply the supplied subsystem-support predicate. Do not make `RunPilotRegistry` switch on individual capability IDs.

Preserve source grants even when two sources grant the same capability. Stacking/deduplication is owned by the consuming subsystem:

- battle runtime currently exposes a set of capability IDs for non-stacking availability checks;
- strategic pipeline counts grants for stacking modifiers.

Do not silently deduplicate the grant list.

### Battle snapshot

`State.BuildPilotBattleSnapshots` should continue querying the registry with `BattlePilotCapabilityPipelineFactory.Supports`.

`PilotBattleSnapshot.FromSkills` is a convenience for tests and skill-only fixtures. Production encounter assembly must use registry-resolved grants so quirks are included.

### Strategic pipeline

`StarMapPilotCapabilityPipelineFactory.InstallPlayerPipeline` continues querying active pilots with `Supports`. Quirk strategic grants should therefore flow automatically once real quirks grant supported capability IDs.

### Tests

- a synthetic positive quirk definition granting a supported battle capability appears in the battle grant query;
- a synthetic negative quirk definition granting a strategic capability appears in the active strategic query;
- source kind and source ID identify the quirk;
- skill and quirk sources granting the same strategic capability produce two grants;
- inactive pilots' strategic quirk grants are excluded;
- ship-specific battle queries include only the assigned pilot's quirk grants.

If production catalogs cannot safely contain synthetic test entries, define real placeholder capability-bearing test fixtures through internal test seams rather than mutating static global dictionaries.

## Phase 7: Minimal character-sheet presentation

Do this only after domain state and persistence are complete.

Update `PilotCharacterSheetOverlay` identity rail to display:

- one icon for the pilot's background;
- one icon for the positive quirk;
- one icon for the negative quirk.

Follow the existing skill-icon pattern in `PilotCharacterSheetOverlay`: presentation owns the ID-to-resource-path mapping and loads tinted textures through `SvgIconLoader`. Despite its legacy name, the loader supports both SVG and PNG resources. Do not place Godot resource paths or texture types in pilot domain definitions.

Each icon's hover tooltip must include:

- display name;
- category (`Background`, `Positive Quirk`, or `Negative Quirk`);
- description, including the exact mechanical effect for real quirks.

Use existing HUD styling. Give positive and negative quirk slots distinct positive/negative framing or tint in addition to their unique artwork; polarity must not be communicated by color alone. Keep the category visible in the tooltip so the meaning remains accessible without color perception.

Because project guidance forbids testing Godot framework behavior, do not instantiate Godot UI in unit tests. Test catalog/formatting projection helpers if a non-Godot projection is introduced; otherwise validate by build and manual scene inspection later.

Add three explicit placeholder icons for the infrastructure sentinels: background, positive quirk, and negative quirk. Their artwork and tooltips should make unfinished content obvious in development builds. Every real background and quirk must map to its own semantic artwork; never reuse the category/polarity placeholders for real content or generated candidates.

## Phase 8: Replace sentinels with real content

This phase is blocked on designer input.

For each supplied background:

1. add ID and catalog definition;
2. add a unique icon and presentation mapping;
3. do not add skill restrictions until the skill-tree design is approved;
4. add it to future generation eligibility separately from catalog registration.

For each supplied quirk:

1. add ID, polarity, name, and exact description;
2. add a unique icon and presentation mapping;
3. identify or add an effect-oriented capability ID;
4. register that capability in exactly the subsystem(s) that implement it;
5. for passive pipeline effects, declare required shared action tags in the subsystem pipeline factory;
6. implement a concrete modifier that only transforms compatible effects;
7. add action tags to every action family that should opt into the behavior;
8. add tests proving a newly tagged action receives the effect without editing the capability;
9. add tests proving unrelated actions/effects remain unchanged.

Do not add capability metadata tags. Do not check action classes inside capability modifiers.

## File-level change map

### Expected new files

- `src/pilots/EPilotBackgroundId.cs` or a cohesive background-domain file;
- `src/pilots/PilotBackgroundDefinition.cs`;
- `src/pilots/PilotBackgroundCatalog.cs`;
- `src/pilots/EPilotQuirkId.cs` or a cohesive quirk-domain file;
- `src/pilots/PilotQuirkDefinition.cs`;
- `src/pilots/PilotQuirkCatalog.cs`;
- focused catalog tests under `grim-space.Tests/Pilots/`;
- focused generic tag-wrapper tests under the core/action test area.

Follow existing repository preference for cohesive files; do not create tiny wrapper classes that only forward one call.

### Expected modified files

- `src/run/Pilot.cs`;
- `src/run/RunPilotRegistry.cs`;
- `src/run/persistence/dtos/PilotPersistenceDto.cs`;
- run persistence tests;
- pilot registry/progression tests;
- `src/world/star-system/presentation/ui/PilotCharacterSheetOverlay.cs` only in the presentation phase;
- [`pilot-hiring-design.md`](pilot-hiring-design.md) when concrete decisions change.

### Files that should not need source-specific quirk changes

- concrete battle actions, except when adding shared action tags for a new effect family;
- concrete strategic actions, except when adding shared action tags;
- `ActorRuntime`;
- `BattlePilotCapabilityPipelineFactory` beyond registering new capability effects;
- `StarMapPilotCapabilityPipelineFactory` beyond registering new capability effects;
- generic `ActionEffectPipeline`.

## Validation commands

After each phase, run the smallest matching test filter. Before declaring the slice complete, run:

```text
dotnet test grim-space.Tests/grim-space.Tests.csproj --no-restore
```

The baseline at handoff is 1,607 passing tests. Existing nullable/analyzer warnings are unrelated; do not expand scope to fix them.

## Completion checklist

- [ ] Generic tag routing is tested independently of concrete action types.
- [ ] Background and quirk catalogs exist.
- [ ] `Pilot` owns immutable background, positive quirk, and negative quirk IDs.
- [ ] Every immutable pilot reconstruction path preserves the new fields.
- [ ] Persistence uses one canonical, required shape with no compatibility fallback.
- [ ] Positive and negative slots validate catalog polarity.
- [ ] Registry capability queries aggregate skill and quirk grants.
- [ ] Battle and strategic runtimes remain source-agnostic.
- [ ] Background and both quirks render as unique icons with explanatory hover tooltips.
- [ ] Positive and negative quirk polarity remains distinguishable without relying only on color.
- [ ] No capability metadata tags were added.
- [ ] Concrete modifiers do not name actions or check action tags.
- [ ] Background skill gating remains deferred.
- [ ] Full test suite passes.

## Stop conditions

Stop and ask the designer rather than guessing if implementation reaches any of these decisions:

- real background names or descriptions;
- real positive/negative quirk names;
- exact quirk numerical effects;
- which action families should carry new matching tags;
- whether a quirk effect stacks across pilots or duplicate sources;
- whether sentinel identities should be visible outside development;
- background skill restrictions;
- random generation weights.
