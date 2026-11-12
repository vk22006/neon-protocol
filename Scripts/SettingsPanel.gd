extends Control

## SettingsPanel
## Reusable UI panel for Neon Protocol settings, accessible from both Main Menu and Pause Menu.

signal closed

@onready var music_toggle: CheckBox = %MusicToggle
@onready var music_slider: HSlider = %MusicSlider
@onready var music_percent_label: Label = %MusicPercentLabel

@onready var audio_toggle: CheckBox = %AudioToggle
@onready var audio_slider: HSlider = %AudioSlider
@onready var audio_percent_label: Label = %AudioPercentLabel

@onready var window_mode_option: OptionButton = %WindowModeOption
@onready var vsync_toggle: CheckBox = %VSyncToggle
@onready var back_button: Button = %BackButton

var _settings_manager: Node


func _ready() -> void:
	process_mode = Node.PROCESS_MODE_ALWAYS
	_sync_from_manager()
	
	music_toggle.toggled.connect(_on_music_toggled)
	music_slider.value_changed.connect(_on_music_slider_value_changed)
	music_slider.drag_ended.connect(_on_music_slider_drag_ended)
	
	audio_toggle.toggled.connect(_on_audio_toggled)
	audio_slider.value_changed.connect(_on_audio_slider_value_changed)
	audio_slider.drag_ended.connect(_on_audio_slider_drag_ended)
	
	window_mode_option.item_selected.connect(_on_window_mode_selected)
	vsync_toggle.toggled.connect(_on_vsync_toggled)
	back_button.pressed.connect(close)
	
	_setup_focus_navigation()
	
	# Initial focus on the first interactive element
	music_toggle.call_deferred("grab_focus")


func _get_manager() -> Node:
	if _settings_manager != null and is_instance_valid(_settings_manager):
		return _settings_manager
	
	if Engine.has_singleton("SettingsManager"):
		_settings_manager = Engine.get_singleton("SettingsManager")
		return _settings_manager
	
	var root := get_tree().root if get_tree() != null else null
	if root != null and root.has_node("SettingsManager"):
		_settings_manager = root.get_node("SettingsManager")
		return _settings_manager
	
	return null


func _sync_from_manager() -> void:
	var sm = _get_manager()
	if sm == null:
		return
	
	music_toggle.set_pressed_no_signal(sm.music_enabled)
	music_toggle.text = "ON" if sm.music_enabled else "OFF"
	var music_pct: int = int(round(sm.music_volume * 100.0))
	music_slider.set_value_no_signal(music_pct)
	music_percent_label.text = "%d%%" % music_pct
	
	audio_toggle.set_pressed_no_signal(sm.audio_enabled)
	audio_toggle.text = "ON" if sm.audio_enabled else "OFF"
	var audio_pct: int = int(round(sm.audio_volume * 100.0))
	audio_slider.set_value_no_signal(audio_pct)
	audio_percent_label.text = "%d%%" % audio_pct
	
	window_mode_option.clear()
	window_mode_option.add_item("Windowed", 0)
	window_mode_option.add_item("Fullscreen", 1)
	window_mode_option.selected = 1 if sm.fullscreen else 0
	
	vsync_toggle.set_pressed_no_signal(sm.vsync)
	vsync_toggle.text = "ON" if sm.vsync else "OFF"


func _setup_focus_navigation() -> void:
	music_toggle.focus_neighbor_top = back_button.get_path()
	music_toggle.focus_neighbor_bottom = audio_toggle.get_path()
	music_toggle.focus_neighbor_right = music_slider.get_path()
	
	music_slider.focus_neighbor_top = music_toggle.get_path()
	music_slider.focus_neighbor_bottom = audio_slider.get_path()
	music_slider.focus_neighbor_left = music_toggle.get_path()
	
	audio_toggle.focus_neighbor_top = music_toggle.get_path()
	audio_toggle.focus_neighbor_bottom = window_mode_option.get_path()
	audio_toggle.focus_neighbor_right = audio_slider.get_path()
	
	audio_slider.focus_neighbor_top = music_slider.get_path()
	audio_slider.focus_neighbor_bottom = window_mode_option.get_path()
	audio_slider.focus_neighbor_left = audio_toggle.get_path()
	
	window_mode_option.focus_neighbor_top = audio_toggle.get_path()
	window_mode_option.focus_neighbor_bottom = vsync_toggle.get_path()
	
	vsync_toggle.focus_neighbor_top = window_mode_option.get_path()
	vsync_toggle.focus_neighbor_bottom = back_button.get_path()
	
	back_button.focus_neighbor_top = vsync_toggle.get_path()
	back_button.focus_neighbor_bottom = music_toggle.get_path()


func _on_music_toggled(button_pressed: bool) -> void:
	music_toggle.text = "ON" if button_pressed else "OFF"
	var sm = _get_manager()
	if sm != null:
		sm.set_music_enabled(button_pressed, true)


func _on_music_slider_value_changed(val: float) -> void:
	music_percent_label.text = "%d%%" % int(val)
	var sm = _get_manager()
	if sm != null:
		sm.set_music_volume(val / 100.0, false)


func _on_music_slider_drag_ended(_value_changed: bool) -> void:
	var sm = _get_manager()
	if sm != null:
		sm.save_settings()


func _on_audio_toggled(button_pressed: bool) -> void:
	audio_toggle.text = "ON" if button_pressed else "OFF"
	var sm = _get_manager()
	if sm != null:
		sm.set_audio_enabled(button_pressed, true)


func _on_audio_slider_value_changed(val: float) -> void:
	audio_percent_label.text = "%d%%" % int(val)
	var sm = _get_manager()
	if sm != null:
		sm.set_audio_volume(val / 100.0, false)


func _on_audio_slider_drag_ended(_value_changed: bool) -> void:
	var sm = _get_manager()
	if sm != null:
		sm.save_settings()


func _on_window_mode_selected(index: int) -> void:
	var is_fullscreen := (index == 1)
	var sm = _get_manager()
	if sm != null:
		sm.set_fullscreen(is_fullscreen, true)


func _on_vsync_toggled(button_pressed: bool) -> void:
	vsync_toggle.text = "ON" if button_pressed else "OFF"
	var sm = _get_manager()
	if sm != null:
		sm.set_vsync(button_pressed, true)


func _unhandled_input(event: InputEvent) -> void:
	if event.is_action_pressed("ui_cancel") or event.is_action_pressed("pause"):
		get_viewport().set_input_as_handled()
		close()
	elif event is InputEventKey and event.pressed and not event.echo:
		if event.keycode == KEY_ESCAPE:
			get_viewport().set_input_as_handled()
			close()


func close() -> void:
	var sm = _get_manager()
	if sm != null:
		sm.save_settings()
	closed.emit()
	queue_free()
