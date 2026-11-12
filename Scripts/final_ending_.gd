extends Control

@export_range(10.0, 120.0, 1.0) var typing_speed: float = 15.0

@onready var quote: Label = $CenterContainer/VBoxContainer/Quote
@onready var quote_person: Label = $CenterContainer/VBoxContainer/QuotePerson
@onready var ending: Label = $CenterContainer/VBoxContainer/Ending
@onready var author: Label = $CenterContainer/VBoxContainer/Author
@onready var scene_transition = get_node_or_null("/root/SceneTransition")

var typing_tween: Tween
var _is_finishing: bool = false


func _ready() -> void:
	AudioManager.play_music(
		preload("res://Assets/music/ending/syouki_takahashi-epilogue-228154.mp3"),
		1.0
	)
	quote.visible_characters = 0
	quote_person.visible_characters = 0
	ending.visible_characters = 0
	author.visible_characters = 0
	
	ending.visible = false
	author.visible = false
	
	await start_typing(quote)
	await get_tree().create_timer(7.0).timeout
	typing_speed = 5.0
	await start_typing(quote_person)
	
	await get_tree().create_timer(6.0).timeout
	
	quote.visible = false
	quote_person.visible = false
	ending.visible = true
	author.visible = true
	
	await start_typing(ending)
	await get_tree().create_timer(3.0).timeout
	await start_typing(author)
	await get_tree().create_timer(5.0).timeout

	if not _is_finishing:
		_is_finishing = true
		await _finish_campaign_and_return_to_main_menu()


func start_typing(label: Label) -> void:
	var character_count: int = label.text.length()

	if character_count == 0:
		return

	var duration: float = float(character_count) / typing_speed

	typing_tween = create_tween()
	typing_tween.set_trans(Tween.TRANS_LINEAR)
	typing_tween.tween_property(
		label,
		"visible_characters",
		character_count,
		duration
	)


func _unhandled_input(event: InputEvent) -> void:
	if event.is_action_pressed("ui_accept"):
		if typing_tween != null and typing_tween.is_running():
			typing_tween.kill()
			if ending.visible:
				ending.visible_characters = -1
			elif quote_person.visible:
				quote_person.visible_characters = -1
			elif quote.visible:
				quote.visible_characters = -1
			get_viewport().set_input_as_handled()
		elif ending.visible and not _is_finishing:
			_is_finishing = true
			get_viewport().set_input_as_handled()
			await _finish_campaign_and_return_to_main_menu()


func _finish_campaign_and_return_to_main_menu() -> void:
	print("[FinalEnding] Campaign ending sequence complete. Deleting save file...")
	_delete_campaign_save()
	
	var transition = _get_scene_transition()
	if transition != null and transition.has_method("ResetGameSession"):
		transition.ResetGameSession()
	
	if transition != null and transition.has_method("FadeToBlack"):
		await transition.FadeToBlack()
	
	get_tree().change_scene_to_file("res://Scenes/UI/main_menu.tscn")
	
	if transition != null and transition.has_method("FadeFromBlack"):
		await transition.FadeFromBlack()


func _delete_campaign_save() -> void:
	var transition = _get_scene_transition()
	if transition != null and transition.has_method("DeleteSaveFile"):
		transition.DeleteSaveFile()
		return
	
	const SAVE_PATH: String = "user://savegame.json"
	if FileAccess.file_exists(SAVE_PATH):
		DirAccess.remove_absolute(SAVE_PATH)
		print("[FinalEnding] Direct fallback deletion of '%s' completed." % SAVE_PATH)


func _get_scene_transition() -> Node:
	if scene_transition != null and is_instance_valid(scene_transition):
		return scene_transition
	scene_transition = get_node_or_null("/root/SceneTransition")
	return scene_transition
