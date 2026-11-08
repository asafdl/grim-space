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
| [`HudTooltipCard`](../src/components/HudTooltipCard.cs) | Pointer-following world-object tooltip card | Blue-cyan |

Each component type has its own file. Artwork variants share their component's behavior and are selected through `UiButtonVisual`, `UiContainerVisual`, `UiListItemVisual`, and `UiCardVisual`. These C# Godot controls do not require `.tscn` wrappers.

The development gallery is [`ui_components_preview.tscn`](../scenes/dev/ui_components_preview.tscn); run it with F6 to inspect every approved variant and palette, including the setting controls.

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
| [`wreck_ambush_hud_preview.tscn`](../scenes/dev/wreck_ambush_hud_preview.tscn) | Wreck decision (ambush) |
| [`fleet_management_hud_preview.tscn`](../scenes/dev/fleet_management_hud_preview.tscn) | Fleet management |
| [`npc_dialog_hud_preview.tscn`](../scenes/dev/npc_dialog_hud_preview.tscn) | NPC dialog |
| [`pilot_speech_hud_preview.tscn`](../scenes/dev/pilot_speech_hud_preview.tscn) | Pilot speech bubbles |
| [`dev_menu_hud_preview.tscn`](../scenes/dev/dev_menu_hud_preview.tscn) | Dev menu |
| [`battle_victory_hud_preview.tscn`](../scenes/dev/battle_victory_hud_preview.tscn) | Battle victory scorecard |
| [`battle_status_hud_preview.tscn`](../scenes/dev/battle_status_hud_preview.tscn) | Battle command HUD with temporary/passive statuses, stack counters, and inspection switching |
| [`run_over_hud_preview.tscn`](../scenes/dev/run_over_hud_preview.tscn) | Run over (empty roster) |

## Pilot speech

Battle presentation registers allied encounter pilots with the same run-owned director.
The focused ship's merc speaks beside the ability-HUD portrait when visible; other
allies (and focused allies with a hidden portrait) speak above their rendered ship,
offset two world units along +Y and projected into the screen overlay at constant UI size.
Ship bubbles omit the portrait connector. Anchor resolution runs each frame, so
focus changes move an active bubble without restarting its typewriter. Hidden,
non-live, behind-camera, or offscreen ship anchors are unavailable. Battle exit
unregisters its handles.

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
