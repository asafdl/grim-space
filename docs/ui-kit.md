# Grim Space UI Kit

The UI kit provides reusable, asset-backed Godot controls for the game's sci-fi presentation layer. It is intentionally presentation-only: components display supplied state and emit standard Godot interaction signals, but never own gameplay state or legality.

## Component catalog

| Component | Purpose | Variants |
|-----------|---------|----------|
| [`UiContainer`](../src/ui/components/UiContainer.cs) | Modal and large-surface borders | `Modal`, `Framed`, `Technical` |
| [`UiButton`](../src/ui/components/UiButton.cs) | Actions, navigation, and integrated container controls | `Action`, `Compact`, `Back`, `Technical`, `Framed`, `WideBeveled` |
| [`UiListItem`](../src/ui/components/UiListItem.cs) | Short, wide selectable rows for shops and other collections | `Tag`, `Bracket`, `Technical` |
| [`UiCard`](../src/ui/components/UiCard.cs) | Rich content requiring title, subtitle, description, trailing value, and optional image | `Angled`, `Framed`, `Technical` |

Each component type has its own file. Artwork variants share their component's behavior and are selected through `UiButtonVisual`, `UiContainerVisual`, `UiListItemVisual`, and `UiCardVisual`. These C# Godot controls do not require `.tscn` wrappers.

The development gallery is [`ui_components_preview.tscn`](../scenes/dev/ui_components_preview.tscn); run it with F6 to inspect every approved variant and palette.

## Palettes

[`HudPalettes`](../src/components/HudPalettes.cs) defines the shared palettes:

- `BlueCyan`
- `Gold`
- `PurpleRed`

`BlueCyan` is the default for production kit components.

Call `SetPalette` on every kit component when the containing HUD changes palette. Components remap their runtime textures through [`palette_remap.gdshader`](../assets/ui/components/runtime/palette_remap.gdshader) or [`button_palette.gdshader`](../assets/ui/components/runtime/button_palette.gdshader); do not tint source textures directly.

## Interaction states

Interactive components derive hover, focus, and pressed presentation from `BaseButton`. Additional semantic states are:

- `SetSelected(bool)`: emphasizes the currently selected list item or card.
- `SetAffordable(bool)`: uses danger coloring when an offer cannot be afforded.
- `SetEnabled(bool)`: disables interaction and reduces visual emphasis.
- `HudActionKind`: distinguishes secondary, primary, and destructive buttons.

Disabled or hidden UI is explanatory presentation only. Domain actions and execution agents remain authoritative for legality.

## Layout rules

- List items are short and wide: `560 x 92` in the current kit.
- Keep text inside the artwork's usable interior. List-item content uses 60 px left padding and is vertically centered.
- Container variants own their content margins, header side slots, dismiss-button pocket, and body start offset. Screens must not reposition modal titles independently.
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
