extends Node

func _ready() -> void:
	call_deferred("start_dev_battle")

func start_dev_battle() -> void:
	Session.StartDevBattle()
