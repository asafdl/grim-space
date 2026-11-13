# Grim Space UI Kit

The UI kit provides reusable, asset-backed Godot controls for the game's sci-fi presentation layer. It is intentionally presentation-only: components display supplied state and emit standard Godot interaction signals, but never own gameplay state or legality.

## Component catalog

| Component | Purpose | Variants |
|-----------|---------|----------|
| [`UiContainer`](../src/ui/components/UiContainer.cs) | Modal and large-surface borders | `Modal`, `Framed`, `Technical`, `SidebarLandscape` |
| [`UiButton`](../src/ui/components/UiButton.cs) | Actions, navigation, and integrated container controls | `Action`, `Compact`, `Back`, `Technical`, `Framed`, `WideBeveled` |
| [`UiToggle`](../src/ui/components/UiToggle.cs) | Binary settings and preferences | On/off |
| [`UiSlider`](../src/ui/components/UiSlider.cs) | Continuous settings values | Horizontal |
| [`UiSelect`](../src/ui/components/UiSelect.cs) | Choosing one value from an option list | Option button |
| [`UiTabButton`](../src/ui/components/UiTabButton.cs) | Selecting one view from a tab group | Underline selection |
| [`UiKeycap`](../src/ui/components/UiKeycap.cs) | Displaying and activating a key binding | Normal, hover, pressed, disabled |
| [`UiListItem`](../src/ui/components/UiListItem.cs) | Short, wide selectable rows for shops and other collections | `Tag`, `Bracket`, `Technical` |
| [`UiCard`](../src/ui/components/UiCard.cs) | Rich content requiring title, subtitle, description, trailing value, and optional image | `Angled`, `Framed`, `Technical` |
| [`UiReticle`](../src/ui/components/UiReticle.cs) | Decorative status/target reticle | `Blue`, `Cyan`, `Teal`, `White` |
| [`RobotPortrait`](../src/ui/portraits/RobotPortrait.cs) | Layered SVG robot portrait compositor and parts preview | 9 eye styles × 9 mouths, 6 screens, 5 ear pairs, 4 antenna options, 5 pauldron pairs, 1 chest |
| [`PilotPortrait`](../src/ui/portraits/PilotPortrait.cs) | Seeded robot avatar rendered as a texture for pilot UI | Persisted identity, legacy-ID compatibility |
| [`HudTooltipCard`](../src/components/HudTooltipCard.cs) | Pointer-following world-object tooltip card | Blue-cyan |

Each component type has its own file. Artwork variants share their component's behavior and are selected through `UiButtonVisual`, `UiContainerVisual`, `UiListItemVisual`, and `UiCardVisual`. These C# Godot controls do not require `.tscn` wrappers.

The development gallery is [`ui_components_preview.tscn`](../scenes/dev/ui_components_preview.tscn); run it with F6 to inspect every approved variant and palette, including the setting controls.

The robot parts gallery is [`robot_portrait_preview.tscn`](../scenes/dev/robot_portrait_preview.tscn).
It provides a Randomize button, a large generated avatar, a six-avatar sample
gallery, and isolated parts beside their assembled context. Randomize refreshes
the hero and samples together; there are no part or color pickers.
Assembled portraits use [`backdrops/soot.svg`](../assets/ui/robot_portraits/backdrops/soot.svg),
a subdued olive-to-charcoal radial gradient behind all parts. Isolated part
previews remain transparent. The backdrop is fixed, untinted, and does not
consume random selections or change generated identities.
Authored SVG pools live under
[`assets/ui/robot_portraits/`](../assets/ui/robot_portraits/); the compositor loads
textures rather than constructing shapes in code. All assets use a transparent
320-square canvas with baked-in attachment positions. The chest and pauldrons
share a bottom baseline at Y=310.
Screen artwork is widened 15% around X=160 in both bezel and glass assets;
ear modules sit 15 canvas units farther outward on each side to remain visible.
Eyes and mouths are independent selections, not locked expression pairs.
Separate galleries and the `Eye + Mouth` tab expose all 81
combinations; changing either selection leaves the other unchanged.

Asset pools are `faces` (paired `_eyes` and `_mouth` files), `screens` (paired
`_bezel` and `_glass` files), `ears`, `antennas`, `shoulders`, `chest`,
`cracks`, `tape`, and `rust`, plus the fixed `backdrops/soot` asset.
File stems match the lowercase part enum names in
[`RobotPortraitParts`](../src/ui/portraits/RobotPortraitParts.cs). `None` intentionally
has no antenna asset. White/grayscale artwork allows independent layer tinting;
keep the full canvas when editing or exporting rather than trimming transparency.
Layer order is backdrop, chest, pauldrons, ears, antenna, bezel, glass, eyes, mouth,
rust, cracks, tape. Flavor overlays retain their authored colors and do not glow.
Each flavor pool has two variants plus `None` (no asset). Randomization
independently gives each effect a 50% chance of being absent, otherwise selecting
either variant equally. This allows all 27 effect combinations, including clean
portraits. Effects are selected after parts and colors, preserving those choices
for existing seeds. The preview has isolated effect tabs and a stacked example row.
Tape placement chooses the head crown or either shoulder; cracks choose the
monitor, head crown, chest, or either shoulder. Seeded placements include small
position jitter and full-circle rotations (0–359 degrees), using shape-specific attachment
points and reduced overlay scales. Existing SVG artwork is reused without runtime
shape generation. Explicit parts without placement retain the SVG's original position.
Randomized cracks are 50% larger than the corresponding tape placement scale,
with a thick dark fracture and bright edge for readability on both glass and metal.
Body crack anchors sit higher to accommodate their larger rotated footprint.
Glass and face SVGs share an 8-unit LED grid with 6-square cells and 2-unit gaps.
The glass contains dim cells within its silhouette; expressions light matching cells
instead of drawing smooth strokes. The triangular screen shifts both face layers
down 24 units (three whole rows), without horizontal compression, preserving cell
alignment. Hardware artwork is unchanged.
Cells are explicit SVG rectangles, not SVG pattern fills (unsupported by the
Godot SVG importer). This artwork is authored in the assets, not generated at runtime.
Smug, Hateful, and Strange eye/mouth assets extend the original six styles.
The preview includes a dedicated row of these matched expressions.
[`face_glow.gdshader`](../assets/ui/robot_portraits/face_glow.gdshader) adds a soft
glyph-colored halo and a brighter core to both facial textures, leaving hardware
and monitor cells unchanged. It samples the authored alpha, so mixed expressions
and eye scaling retain the same effect without separate glow artwork or scene bloom.

The Godot-free [`RobotPortraitGenerator`](../src/ui/portraits/RobotPortraitGenerator.cs)
selects SVG part IDs and one complete authored color palette from a seed.
The twelve equally weighted palettes are Naval (blue steel / amber LEDs),
Butcher (oxblood / icy cyan), Reactor (forest green / lime), Salvage
(bronze / turquoise), Occult (plum / pink), Ash (gunmetal / red),
Hazard (charcoal with industrial yellow / amber), Frost (pale steel with navy / blue),
Corrosion (oxidized teal with copper / orange), Ivory (aged ivory with brown iron / green),
Cobalt (deep blue with graphite / violet), and Ember (burnt orange with dark iron / warm white).
Each fixes the primary metal on casing/chest, secondary metal on ears/pauldrons,
antenna accent, and matched dark monitor/LED colors. Colors are never mixed
between palettes or swapped between parts. Each LED/monitor pair exceeds
4.5:1 luminance contrast before the screen artwork dims it further.
Independent part choices and weathering keep mercs varied within each palette.
This palette revision recolors existing seeds and can change their weathering
because fewer random draws are consumed; the six base part selections remain unchanged.
The preview displays the hero seed; the same seed reproduces the choices with the
current generator and pools. It does not assign or change pilot identity.
Eyes are a separate texture with an `EyeOpenness` scale from 0 to 1 reserved for
blinks. Blink timing is not implemented yet.

### Pilot avatar identity and rendering

[`PilotPortraitId`](../src/pilots/PilotPortraitId.cs) rolls IDs in the
`robot_v1:<nonnegative seed>` format. Starter pilots, other created pilots,
merchant recruits, and rescued recruits use these IDs instead of the old
ten-image pool. The existing `PortraitId` field remains the identity authority:
recruitment freezes it before hiring, and pilot copies, run saves, and battle
snapshots retain it. UI refreshes never roll a new seed.

Existing saved `pilot_01` through `pilot_10` IDs resolve to robot seeds 1 through
10 without rewriting saves. Pilots that previously shared an old portrait
therefore still share an avatar. Unknown IDs throw rather than silently selecting
a substitute. Reproducibility uses the current generator, pools, and runtime;
the seed is not a frozen serialization of every part or color across future
generator changes.

[`PilotPortrait`](../src/ui/portraits/PilotPortrait.cs) owns a 320-square,
2D-only `SubViewport` and the existing SVG compositor. It renders once after
creation or an identity change, then retains the viewport texture without
per-frame avatar rendering. Its nodes and texture live with the owning widget;
there is no global portrait registry. The texture supports the existing
`TextureRect` sizing, circular masks, frames, tooltips, and drag previews.
Generic UI components accept a supplied portrait image node without depending
on pilot identity or generation.

Robot avatars replace pilot images in the strategic roster, deployment slots
and drag previews, character sheets, hiring and rescue cards, focused battle
HUD, victory rows, and speech previews. Victory rows use the battle snapshot's
portrait when the pilot is no longer in the roster. Ship/unit icons in the
turn-flow bar remain ship/unit icons.

### HUD screen previews

Production overlays have dedicated dev scenes under [`scenes/dev/`](../scenes/dev/). Open a scene and press **F6** (Run Current Scene). Each scene uses [`UiHudDevContext`](../src/presentation/dev/hud/UiHudDevContext.cs) to build a seeded run with contracts, merchants, and resources.

| Scene | Overlay |
|-------|---------|
| [`contract_hud_preview.tscn`](../scenes/dev/contract_hud_preview.tscn) | Contract board |
| [`pilot_hiring_hud_preview.tscn`](../scenes/dev/pilot_hiring_hud_preview.tscn) | Pilot broker |
| [`ship_recruitment_hud_preview.tscn`](../scenes/dev/ship_recruitment_hud_preview.tscn) | Ship broker |
| [`black_market_sale_hud_preview.tscn`](../scenes/dev/black_market_sale_hud_preview.tscn) | Black-market sale (one modal with pilot and ship sections) |
| [`healing_station_hud_preview.tscn`](../scenes/dev/healing_station_hud_preview.tscn) | Healing station |
| [`engagement_hud_preview.tscn`](../scenes/dev/engagement_hud_preview.tscn) | Engagement (with contract) |
| [`engagement_hud_plain_preview.tscn`](../scenes/dev/engagement_hud_plain_preview.tscn) | Engagement (no contract) |
| [`wreck_hud_preview.tscn`](../scenes/dev/wreck_hud_preview.tscn) | Wreck decision (salvage) |
| [`npc_dialog_hud_preview.tscn`](../scenes/dev/npc_dialog_hud_preview.tscn) | NPC dialog |
| [`pilot_speech_hud_preview.tscn`](../scenes/dev/pilot_speech_hud_preview.tscn) | Pilot speech bubbles |
| [`dev_menu_hud_preview.tscn`](../scenes/dev/dev_menu_hud_preview.tscn) | Dev menu |
| [`battle_victory_hud_preview.tscn`](../scenes/dev/battle_victory_hud_preview.tscn) | Battle victory scorecard |
| [`battle_status_hud_preview.tscn`](../scenes/dev/battle_status_hud_preview.tscn) | Battle command HUD with temporary/passive statuses, stack counters, and inspection switching |
| [`run_over_hud_preview.tscn`](../scenes/dev/run_over_hud_preview.tscn) | Run over (empty roster) |

The engagement previews retain the normal engagement content and mount the world
`PlayerFleetHud` alongside it. Four blue beveled frames extracted from the bottom
row of `1-09.png` (`deployment_pilot_frame_blue.png`, preserved artwork and alpha)
in the footer accept
pilot drags; occupied slots can be moved/swapped, or dragged back to the roster
to remove them. Drag previews retain the grab offset and portrait size; each frame
accepts drops within a 12-pixel invisible margin, without a snapping hover preview.
The existing button palette shader maps empty frames to `BlueCyan.Border` and
occupied frames to `BlueCyan.Secondary`, updating when pilots are dropped or removed.
Filled frames show the assigned ship in a small `UiReticle`, empty
when no ship is assigned. Selection is dev-only and does not change deployment,
ship assignments, or hazard pay.

The fleet rail fits the available viewport height and scrolls overflowing portraits
with the mouse wheel or its up/down arrows, using the same `HudCycleArrow` as the
character sheet and ship picker. Fleet portrait sizing uses a 125-pixel base
(approximately 30% larger than the previous 96), with a matching wider rail and
161-pixel arrow scroll steps. Class badges and level-up icons retain their sizes.
Ship badges remain 60 pixels; deployment slots and battle portraits are unchanged.
Arrows dim at the scroll limits;
wheel input over the rail does not zoom the map. The engagement preview includes
eight pilots to exercise scrolling without changing the four deployment slots.
Each fleet portrait has a 60-pixel ship reticle centered on its bottom-right corner, outside
the portrait-only blue beveled frame shared with engagement slots, using the same right-click picker
as deployment: tier-sorted pages, compatible owned ships, and comparison tooltips.
Choosing a ship uses the run's assignment/swap rules and refreshes any open deployment
slots without changing their pilot selection. Hull and shield points appear on the
ship reticle's arcs and tooltip, not the pilot portrait; XP fills bottom-up along
the portrait's left edge as an overlay without shifting the portrait.
Unassigned pilots show an empty reticle that can still open the picker.
Roster portraits show a 28-pixel class badge at the frame's top-right, with an
opaque dark backing and class-colored outline and glyph. The level label is
removed; level remains in the portrait tooltip, while XP is shown only by the portrait bar. The level-up chevron sits
at bottom-left, clear of the bottom-right ship reticle, and still opens the
character sheet. Deployment, battle, and victory portraits are unchanged.
Class glyph colors are shared through `PilotIdentityIconRail`: Space Ranger
amber, Support green, Tank blue, and Rogue violet. The same colors apply to class
icons in hiring, rescue cards, and character sheets.
The engagement preview includes all four classes and unspent level-ups to check
badge readability and separation from ship reticles.

## Pilot speech

Battle presentation registers allied encounter pilots with the same run-owned director.
The focused ship's merc speaks beside the ability-HUD portrait when visible; other
allies (and focused allies with a hidden portrait) speak above their rendered ship,
offset two world units along +Y and projected into the screen overlay at constant UI size.
These ship-following barks include a 64-pixel generated pilot avatar on the left,
using the fleet roster's blue-cyan beveled rectangular frame and portrait insets,
with the bubble connector pointing to that avatar rather than to the ship.
The avatar inherits the bubble's fade and lifetime; the combined group is clamped
inside the viewport. The focused pilot retains the existing HUD portrait connector
without a duplicate avatar. Anchor resolution runs each frame, so
focus changes move an active bubble and show/hide its attached avatar without
restarting its typewriter. Strategic-map speech is unchanged. Hidden,
non-live, behind-camera, or offscreen ship anchors are unavailable. Battle exit
unregisters its handles.

The speech preview exercises moving projected anchors, right-edge clamping, and
HUD portrait anchors. Press Space to switch focus between lead and wing while a
line is playing; click to replay the queue.

After a damaging impact on a player unit visibly completes, replay supplies its
currently living allied ships to the bark director. It selects 1–3 mercs with
independent 20% chances and Scared/Angry/Surprised/Disdain flavors. Every qualifying
hit rolls again: battle barks neither check nor start global or per-pilot real-time
cooldowns. Replay blocks,
frames each selected speaker, then waits for the actual bubble queue to finish
before framing the next speaker or continuing. No selected speakers means no
additional wait. Dead ships are excluded using replay state, not simulation end
state. Scene exit clears pending speech and prevents replay continuations.

`PilotBarkDirector.Select` returns pilot/flavor/text decisions without displaying
them or starting cooldowns. Playback can frame each selected speaker before calling
`Present`; presentation starts cooldowns only if the registered handle accepts the
line and the decision uses cooldowns. Battle decisions do not use cooldowns.
`Present` does not reroll or recheck group cooldowns. Ordinary strategic
`Offer` calls select and present immediately through this same path.

The shared `StrategicHud` owns the pilot speech UI in both the star map and facility scenes
and registers/unregisters speech handles by pilot ID as roster cards appear/disappear.
The Godot-free `PilotBarkDirector` owns event subscriptions, bark context,
speaker/flavor selection, cooldown ticking, and the scripted opener. It subscribes
on construction and unsubscribes when the run is disposed. The run owns one director;
the persistent session ticks it, so cooldowns survive scene transitions. Replacing
the strategic world rebinds its subscription without replacing the director.
It has no HUD or control references; scenes only register and unregister presentation handles.
Its delivery subscription listens for committed `ContractStateChanged` records:
only completed player delivery contracts offer a bark, not intermediate handoffs.
Each selected pilot independently uses the director's 40% default chance, then rolls
Disdain or Relieved for delivery completion and a line from that flavor. Global and
per-pilot cooldowns apply; dialogs do not block or delay barks. Scripted opening
lines still play once per new run.

Strategic roster handles supply a live visibility predicate: only portraits fully
inside the fleet scroll area and screen are eligible for selection. Visibility is
checked again before speaking, and the same anchor resolver drops active or queued
speech when its portrait scrolls out of view. The director remains Godot-free.

Committed engagement `ReachContactAction` events also offer a bark when the player
has an authoritative pending fight/flee decision, whether the player or hostile fleet
initiated contact. Allowed flavors are Scared, Angry, Surprised, and Bored.
Other fleets' contacts and delivery/black-market meetings do not trigger this reaction.
Committed player `ReachWreckageAction` events offer a bark for the pending wreck HUD:
ambush uses Scared/Angry/Surprised, while salvage uses Relieved/Bored. Both select
1–3 eligible pilots with independent default chances.
Delivery offers select one pilot; engagement offers roll one to three, capped by
eligible pilots. The global cooldown gates the whole group once.
Selected pilots are distinct; failed chance rolls are not replaced, so zero may speak.
Each successful pilot rolls its own flavor and line, and their bubbles queue
sequentially. Per-pilot cooldowns start only for accepted lines.

A committed player move schedules one travel-chatter attempt at a random simulation
tick strictly inside its journey. It selects 1–3 eligible pilots with independent
default chances and Bored/Disdain flavors. A new move replaces the attempt; arrival,
journey replacement, pursuit/engagement, battle, or map regeneration invalidates it.
Missed attempts are not retried. Simulation timing keeps pauses and travel speed aligned.

## Palettes

[`HudPalettes`](../src/components/HudPalettes.cs) defines the shared palettes:

- `BlueCyan`
- `Gold`
- `PurpleRed`

`BlueCyan` is the default for production kit components.

Star-map world-object and HUD-control hovers use `HudTooltipCard` so they share one angled outline and blue-cyan palette.
Cards created from `HudTooltipContent` can be pinned by holding Alt; while pinned, pointer interaction with the
underlying HUD is blocked. Icons with descriptions show shared explanations on hover without a link cursor or
underline; omit the catalog description to disable it. `IconTooltips` can override or provide an explanation for
a content object. The child uses the hovered icon in place of its title and the palette's secondary border color.
The separate pointer-following plain-text map tooltip is not pinnable.

Battle status presentation is projected by [`BattleStatusDisplay`](../src/battle/presentation/ui/BattleStatusDisplay.cs)
from the focused ship's `UnitDisplayState.Statuses`, including temporary effects and installed pilot passives.
Skill-backed effects reuse the pilot skill catalog's tooltip copy. Reactive Shields uses its captured gameplay
stack count (including zero); temporary shield grants share one entry with a grant counter and remaining shields
per face. Other statuses have no stack counter. The projection is disposable and never changes gameplay state.
[`BattleStatusBar`](../src/battle/presentation/ui/BattleStatusBar.cs) renders these entries immediately above
the command console, wraps additional icons upward, and uses the existing purple-red tooltip cards.
Status icons use the thin white/gray beveled square from the `1-03.png` source sheet,
extracted as `status_square_frame_neutral.png` and recolored with the shared palette shader.
The row follows the focused ship, including inspected enemies, and hides when empty or when the HUD is hidden.

Call `SetPalette` on every kit component when the containing HUD changes palette. Components remap their runtime textures through [`palette_remap.gdshader`](../assets/ui/components/runtime/palette_remap.gdshader) or [`button_palette.gdshader`](../assets/ui/components/runtime/button_palette.gdshader); do not tint source textures directly.
`UiToggle` uses blue-cyan source artwork and applies the active palette's primary color for non-default palettes; subtle hue differences may remain until matching source variants are added.

Setting controls own their visual states and palette styling. Compose them into screens rather than adding per-screen theme overrides. `UiToggle.Value`, `UiSlider.Value`, `UiSelect` selection, `UiTabButton.SetSelected`, and `UiKeycap.Text` expose presentation-facing state; screens remain responsible for supplying authoritative values and handling user intent.

## Interaction states

Interactive components derive hover, focus, and pressed presentation from `BaseButton`. Additional semantic states are:

- `SetSelected(bool)`: emphasizes the currently selected list item or card.
- `SetAffordable(bool)`: uses danger coloring when an offer cannot be afforded.
- `SetEnabled(bool)`: disables interaction and reduces visual emphasis on controls that support it.
- `HudActionKind`: distinguishes secondary, primary, and destructive buttons.

Disabled or hidden UI is explanatory presentation only. Domain actions and execution agents remain authoritative for legality.

## Layout rules

### Shared sizing API

Every kit control implements `IUiSized`:

- `DefaultSize` is read-only and owned by the component or visual variant. Zero means content-driven on that axis, not hidden or collapsed.
- `SizeOverride` accepts a `UiSizeOverride` with optional `Width` and `Height`. Unspecified axes retain their defaults; setting the whole property to `null` restores the current variant's defaults. Changing variants preserves explicit overrides.
- Defaults and overrides are layout size floors, not forced pixel dimensions. Godot containers fit the control into their allocated space using `SizeFlags`; content minimums still win. A narrower override permits shrinking below a built-in default, but cannot clip text or remove intrinsic padding.
- Fit means layout/reflow, not scaling fonts. Wrapped text can increase height, and overflowing collections belong in scroll containers. Portraits, reticles, and toggle artwork preserve their aspect ratio.
- Supply finite, non-negative dimensions. Invalid dimensions throw rather than silently falling back.

For example, `button.SizeOverride = new UiSizeOverride(Width: 280f)` keeps the variant's default height. Use this API instead of writing `Size` or `CustomMinimumSize` on kit controls. Godot layout nodes still use their native sizing properties.

Production callers use overrides for deliberate frame sizes, portrait slots, aligned key-binding columns, and large narrative actions. Menu buttons use their variant defaults and parent fill; content cards reflow to the modal's available width rather than imposing a second fixed width. Keep spacing in layout containers instead of inventing extra component minimum-width adjustments.

`ModalShell` caps its default or requested panel size to the available viewport with a 20 px inset per edge; oversized bodies scroll instead of pushing the panel off-screen.

- Image-list rows start at 110 px high and fit their parent's width. Portrait size and text insets stay fixed as text wraps and increases row height; rendered dimensions never feed back into minimum-size margins.
- Ship offers give the name the full text-column width and place gear tier and resource cost together underneath, rather than reserving a trailing price column. This keeps the black market's side-by-side lists readable without resizing the modal or portraits.
- `UiButton` fills available horizontal space by default (`SizeFlagsHorizontal = ExpandFill`). Set `ShrinkCenter` (or another non-fill flag) when a screen needs intrinsic-width buttons, such as modal footers or component-gallery rows.
- `ModalShell` renders `HudAction` cost amounts and icons inside the button, after its label, using `UiButton.SetTrailingContent`. Cost tooltips remain hoverable when the action is disabled; their hosts pass clicks through to the button.
- Cards, list items, and nested/framed content containers derive their minimum size from content and padding. Set `SizeOverride` when a screen needs a different floor; wrapped text contributes its laid-out height instead of relying on estimated line counts.
- Keep text inside the artwork's usable interior. List-item content uses 60 px left padding and is vertically centered.
- Container variants own their content margins, header side slots, dismiss-button pocket, subtitle band height, and body start offset. `ModalShell` always reserves the subtitle band and applies `BodyMarginTop` from the active `UiContainer` visual; screens must not reposition titles or add per-screen body offsets.
- Component typography derives its colors from the active palette. Screen-specific semantic values may use an explicit override, such as gold contract difficulty stars.
- Use the component's existing texture mode. Do not nine-slice artwork containing center-line details or decorative cross-center geometry.
- Keep source sheets under [`SCI-FI Components`](../assets/unused/ui/SCI-FI%20Components) unchanged. [`assets/unused`](../assets/unused) is excluded from Godot imports and exports by `.gdignore`.
- Runtime-ready crops and shaders belong under [`runtime`](../assets/ui/components/runtime). Every file there must be referenced by a component variant.
- Add visual variants through the component's enum and `SetVisual`; do not create screen-specific copies of the component.
- Keep gameplay text and values supplied by the owning overlay or view model. Components must not query or mutate world state.

## Production adoption

Production adoption is incremental. The contract HUD uses the technical container and list-item variants plus a framed read-only detail card; other overlays still use their existing content widgets. When adopting the kit elsewhere:

1. Preserve the existing overlay flow and authoritative state paths.
2. Replace presentation created by [`ModalShell`](../src/components/ModalShell.cs) or [`HudWidgets`](../src/components/HudWidgets.cs) without moving gameplay logic into the controls.
3. Bind `Pressed` and other interaction signals to the existing intent/action boundary.
4. Verify every palette and interaction state in the component gallery before using the variant in a production screen.

`ModalShell` now uses `UiContainer` as its backing surface. Pass a `UiContainerVisual` to its constructor when a screen needs `Framed` or `Technical`; existing callers default to `Modal`.

Battle and star-map pause menus share [`PauseMenuUi`](../src/presentation/support/PauseMenuUi.cs): a viewport-capped 640 x 660 modal, Purple Red menu actions, and a Gold Technical Report Issue button with extra separation. X/Escape resumes without a duplicate Continue button. Keep shared sizing and styling there; overlays supply their own actions (including battle-only Retire/Restart).
