extends Control

@onready var new_game_btn: Button = $MenuLayout/CenterContainer/MenuColumn/MenuButtonsCenter/MenuButtons/NewGameBtn
@onready var resume_btn: Button = $MenuLayout/CenterContainer/MenuColumn/MenuButtonsCenter/MenuButtons/ResumeBtn
@onready var settings_btn: Button = $MenuLayout/CenterContainer/MenuColumn/MenuButtonsCenter/MenuButtons/SettingsBtn
@onready var exit_btn: Button = $MenuLayout/CenterContainer/MenuColumn/MenuButtonsCenter/MenuButtons/ExitBtn
@onready var scene_transition = get_node_or_null("/root/SceneTransition")

const INTRO_SCENE_PATH: String = "res://Scenes/Cutscenes/IntroMessage.tscn"
const LEVEL_1_PATH: String = "res://Scenes/Levels/Level_1.tscn"
const SAVE_FILE_PATH: String = "user://savegame.json"

var _is_transitioning: bool = false
var _confirm_dialog: ConfirmationDialog = null


func _ready() -> void:
	AudioManager.play_music(
		preload("res://Assets/music/mainMenu_Exploration/mondmatt-lost-in-space-130521.mp3"),
		2.0
	)
	AudioManager.register_ui_buttons(self)
	_setup_confirmation_dialog()
	_update_resume_button_state()


func _setup_confirmation_dialog() -> void:
	_confirm_dialog = ConfirmationDialog.new()
	_confirm_dialog.title = "Start New Game?"
	_confirm_dialog.dialog_text = "An existing save file was detected.\nStarting a new game will overwrite your progress.\n\nDo you want to proceed?"
	_confirm_dialog.ok_button_text = "Start New Game"
	_confirm_dialog.cancel_button_text = "Cancel"
	_confirm_dialog.confirmed.connect(_on_confirm_new_game)
	add_child(_confirm_dialog)


func _update_resume_button_state() -> void:
	if resume_btn == null:
		return
	
	var has_save: bool = _has_valid_save()
	resume_btn.disabled = not has_save
	if not has_save:
		resume_btn.tooltip_text = "No saved game found"
	else:
		var save_data = _load_and_validate_save()
		var level_idx = save_data.get("level_index", 1)
		var crystals = save_data.get("crystal_count", 0)
		resume_btn.tooltip_text = "Continue Level %d (%d crystals)" % [level_idx, crystals]


func _on_new_game_btn_pressed() -> void:
	if _is_transitioning:
		return
	
	if _has_valid_save():
		if _confirm_dialog != null:
			_confirm_dialog.popup_centered()
		return
	
	_start_new_game()


func _on_confirm_new_game() -> void:
	_start_new_game()


func _start_new_game() -> void:
	if _is_transitioning:
		return
	_is_transitioning = true
	_set_buttons_disabled(true)
	
	var target_scene: String = INTRO_SCENE_PATH if ResourceLoader.exists(INTRO_SCENE_PATH) else LEVEL_1_PATH
	print("[MainMenu] Starting New Game. Transitioning to introduction: '%s'" % target_scene)
	
	var transition_node = _get_scene_transition()
	if transition_node != null and transition_node.has_method("StartNewGame"):
		transition_node.StartNewGame(target_scene, LEVEL_1_PATH)
	else:
		_delete_saved_game()
		if transition_node != null and transition_node.has_method("ResetGameSession"):
			transition_node.ResetGameSession()
		if transition_node != null and transition_node.has_method("FadeToBlack"):
			await transition_node.FadeToBlack()
		var error = get_tree().change_scene_to_file(target_scene)
		if error != OK:
			push_error("[MainMenu] Failed to load '%s': %d" % [target_scene, error])
			_is_transitioning = false
			_update_resume_button_state()
			_update_resume_button_state()


func _on_resume_btn_pressed() -> void:
	if _is_transitioning:
		return
	
	var save_data = _load_and_validate_save()
	if save_data.is_empty():
		push_warning("[MainMenu] Cannot resume: No valid save data found.")
		_update_resume_button_state()
		return
	
	_resume_game(save_data)


func _resume_game(save_data: Dictionary) -> void:
	if _is_transitioning:
		return
	_is_transitioning = true
	_set_buttons_disabled(true)
	
	var target_level: String = save_data.get("current_level", LEVEL_1_PATH)
	var crystals: int = int(save_data.get("crystal_count", 0))
	
	print("[MainMenu] Resuming session: Level='%s', Crystals=%d." % [target_level, crystals])
	
	var transition_node = _get_scene_transition()
	if transition_node != null and transition_node.has_method("ResumeGame"):
		transition_node.ResumeGame(target_level, crystals)
	else:
		if transition_node != null and transition_node.has_method("SetPreservedCrystals"):
			transition_node.SetPreservedCrystals(crystals)
		if transition_node != null and transition_node.has_method("FadeToBlack"):
			await transition_node.FadeToBlack()
		var error = get_tree().change_scene_to_file(target_level)
		if error != OK:
			push_error("[MainMenu] Failed to load saved level '%s': %d" % [target_level, error])
			_is_transitioning = false
			_update_resume_button_state()
			if transition_node != null and transition_node.has_method("FadeFromBlack"):
				await transition_node.FadeFromBlack()


func _on_settings_btn_pressed() -> void:
	if _is_transitioning:
		return
	
	var settings_scene := preload("res://Scenes/UI/SettingsPanel.tscn")
	var settings_instance := settings_scene.instantiate() as Control
	if settings_instance == null:
		push_error("[MainMenu] Failed to instantiate SettingsPanel.")
		return
	
	add_child(settings_instance)
	
	var audio_mgr := get_node_or_null("/root/AudioManager")
	if audio_mgr != null and audio_mgr.has_method("register_ui_buttons"):
		audio_mgr.register_ui_buttons(settings_instance)
	
	settings_instance.closed.connect(func():
		if settings_btn != null and is_instance_valid(settings_btn):
			settings_btn.grab_focus()
	)


func _on_exit_btn_pressed() -> void:
	if _is_transitioning:
		return
	get_tree().quit()


func _has_valid_save() -> bool:
	var transition_node = _get_scene_transition()
	if transition_node != null and transition_node.has_method("HasValidSave"):
		return transition_node.HasValidSave()
	
	return not _load_and_validate_save().is_empty()


func _load_and_validate_save() -> Dictionary:
	var transition_node = _get_scene_transition()
	if transition_node != null and transition_node.has_method("GetSaveData"):
		var data: Dictionary = transition_node.GetSaveData()
		if not data.is_empty():
			return data
	
	if not FileAccess.file_exists(SAVE_FILE_PATH):
		return {}
	
	var file = FileAccess.open(SAVE_FILE_PATH, FileAccess.READ)
	if file == null:
		return {}
	
	var text = file.get_as_text()
	file.close()
	
	if text.strip_edges().is_empty():
		return {}
	
	var parsed = JSON.parse_string(text)
	if typeof(parsed) != TYPE_DICTIONARY:
		return {}
	
	var dict: Dictionary = parsed
	if not dict.has("version") or int(dict.get("version")) != 1:
		return {}
	
	var level_path = str(dict.get("current_level", ""))
	if level_path.is_empty() or not ResourceLoader.exists(level_path):
		return {}
	
	return dict


func _delete_saved_game() -> void:
	var transition_node = _get_scene_transition()
	if transition_node != null and transition_node.has_method("DeleteSaveFile"):
		transition_node.DeleteSaveFile()
		return
	
	if FileAccess.file_exists(SAVE_FILE_PATH):
		DirAccess.remove_absolute(SAVE_FILE_PATH)


func _set_buttons_disabled(disabled: bool) -> void:
	if new_game_btn != null:
		new_game_btn.disabled = disabled
	if resume_btn != null:
		resume_btn.disabled = disabled
	if settings_btn != null:
		settings_btn.disabled = disabled
	if exit_btn != null:
		exit_btn.disabled = disabled


func _get_scene_transition() -> Node:
	if scene_transition != null and is_instance_valid(scene_transition):
		return scene_transition
	scene_transition = get_node_or_null("/root/SceneTransition")
	return scene_transition
