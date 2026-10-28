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
| [`dockyard_ship_support_hud_preview.tscn`](../scenes/dev/dockyard_ship_support_hud_preview.tscn) | Ship upgrades shop |
| [`dockyard_weapons_hud_preview.tscn`](../scenes/dev/dockyard_weapons_hud_preview.tscn) | Ability upgrades shop |
| [`healing_station_hud_preview.tscn`](../scenes/dev/healing_station_hud_preview.tscn) | Healing station |
| [`engagement_hud_preview.tscn`](../scenes/dev/engagement_hud_preview.tscn) | Engagement (with contract) |
| [`engagement_hud_plain_preview.tscn`](../scenes/dev/engagement_hud_plain_preview.tscn) | Engagement (no contract) |
| [`wreck_hud_preview.tscn`](../scenes/dev/wreck_hud_preview.tscn) | Wreck decision (salvage) |
| [`wreck_ambush_hud_preview.tscn`](../scenes/dev/wreck_ambush_hud_preview.tscn) | Wreck decision (ambush) |
| [`fleet_management_hud_preview.tscn`](../scenes/dev/fleet_management_hud_preview.tscn) | Fleet management |
| [`npc_dialog_hud_preview.tscn`](../scenes/dev/npc_dialog_hud_preview.tscn) | NPC dialog |
| [`narrative_hud_preview.tscn`](../scenes/dev/narrative_hud_preview.tscn) | Narrative pager |
| [`dev_menu_hud_preview.tscn`](../scenes/dev/dev_menu_hud_preview.tscn) | Dev menu |

## Palettes

[`HudPalettes`](../src/components/HudPalettes.cs) defines the shared palettes:

- `BlueCyan`
- `Gold`
- `PurpleRed`

`BlueCyan` is the default for production kit components.

Star-map world-object and HUD-control hovers use `HudTooltipCard` so they share one angled outline and blue-cyan palette.

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
