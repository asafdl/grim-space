# CharacterTurnTimeline

A horizontal combat turn timeline inspired by Dodoveloper's SlotWheel component.

Reference:
https://github.com/Dodoveloper/godot-reusable-ui-components/blob/main/demos/slot_wheel/slot_wheel_demo.tscn

## Install

Copy:
- `character_turn_timeline.gd` -> `res://ui/character_turn_timeline.gd`
- `character_turn_timeline.tscn` -> `res://ui/character_turn_timeline.tscn`

Then instance the `.tscn` wherever the combat HUD belongs.

## API

```gdscript
$CharacterTurnTimeline.characters = ["Mage", "Goblin", "Archer", "Knight"]
$CharacterTurnTimeline.set_turn(2)
$CharacterTurnTimeline.advance_turn()
```

Signals:
- `turn_changed(index)`
- `scroll_finished`

## Design

The center of the control is fixed. The repeated character strip moves underneath it.
Multiple copies of the turn sequence are kept in the strip so the animation never
has to expose the beginning or end of the sequence.

The implementation intentionally does not depend on the original addon or
`WheelItemData`, so it can be dropped into another Godot project.
