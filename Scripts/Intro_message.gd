extends Control

@export_range(10.0, 120.0, 1.0) var typing_speed: float = 35.0

@onready var subtitle_label: Label = $CenterContainer/VBoxContainer/SubtitleLabel
@onready var scene_transition = get_node_or_null("/root/SceneTransition")

var typing_tween: Tween


func _ready() -> void:
	subtitle_label.visible_characters = 0
	await start_typing()
	
	await get_tree().create_timer(13.0).timeout
	
	await SceneTransition.FadeToBlack()
	get_tree().change_scene_to_file("res://Scenes/Cutscenes/IntroCutScene.tscn")
	await SceneTransition.FadeFromBlack()


func start_typing() -> void:
	var character_count: int = subtitle_label.text.length()

	if character_count == 0:
		return

	var duration: float = float(character_count) / typing_speed

	typing_tween = create_tween()
	typing_tween.set_trans(Tween.TRANS_LINEAR)
	typing_tween.tween_property(
		subtitle_label,
		"visible_characters",
		character_count,
		duration
	)


func _unhandled_input(event: InputEvent) -> void:
	if event.is_action_pressed("ui_accept"):
		if typing_tween != null and typing_tween.is_running():
			typing_tween.kill()
			subtitle_label.visible_characters = -1
			get_viewport().set_input_as_handled()
