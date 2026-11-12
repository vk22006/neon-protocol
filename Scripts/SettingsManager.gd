extends Node

## SettingsManager
## Centralized autoload for managing, applying, and persisting audio and display settings.

signal settings_changed(setting_name: String, value: Variant)
signal music_toggled(enabled: bool)
signal music_volume_changed(volume: float)
signal audio_toggled(enabled: bool)
signal audio_volume_changed(volume: float)
signal fullscreen_toggled(fullscreen: bool)
signal vsync_toggled(vsync: bool)

const SETTINGS_FILE_PATH := "user://settings.cfg"

const BUS_MUSIC := "Music"
const BUS_SFX := "SFX"
const BUS_UI := "UI"
const BUS_DIALOGUE := "Dialogue"

var music_enabled: bool = true
var music_volume: float = 1.0
var audio_enabled: bool = true
var audio_volume: float = 1.0

var fullscreen: bool = true
var vsync: bool = true


func _ready() -> void:
	process_mode = Node.PROCESS_MODE_ALWAYS
	load_settings()


func load_settings() -> void:
	var config := ConfigFile.new()
	var err := config.load(SETTINGS_FILE_PATH)
	
	if err == OK:
		music_enabled = bool(config.get_value("audio", "music_enabled", true))
		music_volume = clampf(float(config.get_value("audio", "music_volume", 1.0)), 0.0, 1.0)
		audio_enabled = bool(config.get_value("audio", "audio_enabled", true))
		audio_volume = clampf(float(config.get_value("audio", "audio_volume", 1.0)), 0.0, 1.0)
		
		fullscreen = bool(config.get_value("display", "fullscreen", true))
		vsync = bool(config.get_value("display", "vsync", true))
		print("[SettingsManager] Settings loaded successfully from %s." % SETTINGS_FILE_PATH)
	else:
		print("[SettingsManager] Settings file not found or invalid (%d). Applying defaults." % err)
		music_enabled = true
		music_volume = 1.0
		audio_enabled = true
		audio_volume = 1.0
		fullscreen = true
		vsync = true
		save_settings()
	
	apply_all_settings()


func save_settings() -> void:
	var config := ConfigFile.new()
	config.set_value("audio", "music_enabled", music_enabled)
	config.set_value("audio", "music_volume", music_volume)
	config.set_value("audio", "audio_enabled", audio_enabled)
	config.set_value("audio", "audio_volume", audio_volume)
	
	config.set_value("display", "fullscreen", fullscreen)
	config.set_value("display", "vsync", vsync)
	
	var err := config.save(SETTINGS_FILE_PATH)
	if err != OK:
		push_warning("[SettingsManager] Failed to save settings to %s: %d" % [SETTINGS_FILE_PATH, err])
	else:
		print("[SettingsManager] Settings saved to %s." % SETTINGS_FILE_PATH)


func apply_all_settings() -> void:
	apply_audio_settings()
	apply_display_settings()


func apply_audio_settings() -> void:
	_apply_music_mute()
	_apply_music_volume()
	_apply_audio_mute()
	_apply_audio_volume()


func apply_display_settings() -> void:
	_apply_fullscreen()
	_apply_vsync()


func set_music_enabled(value: bool, save: bool = true) -> void:
	if music_enabled == value and not save:
		return
	music_enabled = value
	_apply_music_mute()
	if save:
		save_settings()
	music_toggled.emit(music_enabled)
	settings_changed.emit("music_enabled", music_enabled)


func set_music_volume(value: float, save: bool = true) -> void:
	var clamped := clampf(value, 0.0, 1.0)
	music_volume = clamped
	_apply_music_volume()
	if save:
		save_settings()
	music_volume_changed.emit(music_volume)
	settings_changed.emit("music_volume", music_volume)


func set_audio_enabled(value: bool, save: bool = true) -> void:
	if audio_enabled == value and not save:
		return
	audio_enabled = value
	_apply_audio_mute()
	if save:
		save_settings()
	audio_toggled.emit(audio_enabled)
	settings_changed.emit("audio_enabled", audio_enabled)


func set_audio_volume(value: float, save: bool = true) -> void:
	var clamped := clampf(value, 0.0, 1.0)
	audio_volume = clamped
	_apply_audio_volume()
	if save:
		save_settings()
	audio_volume_changed.emit(audio_volume)
	settings_changed.emit("audio_volume", audio_volume)


func set_fullscreen(value: bool, save: bool = true) -> void:
	if fullscreen == value and not save:
		return
	fullscreen = value
	_apply_fullscreen()
	if save:
		save_settings()
	fullscreen_toggled.emit(fullscreen)
	settings_changed.emit("fullscreen", fullscreen)


func set_vsync(value: bool, save: bool = true) -> void:
	if vsync == value and not save:
		return
	vsync = value
	_apply_vsync()
	if save:
		save_settings()
	vsync_toggled.emit(vsync)
	settings_changed.emit("vsync", vsync)


func _linear_to_bus_db(linear_val: float) -> float:
	if linear_val <= 0.0001:
		return -80.0
	return linear_to_db(clampf(linear_val, 0.0001, 1.0))


func _apply_music_mute() -> void:
	var idx := AudioServer.get_bus_index(BUS_MUSIC)
	if idx != -1:
		AudioServer.set_bus_mute(idx, not music_enabled)


func _apply_music_volume() -> void:
	var idx := AudioServer.get_bus_index(BUS_MUSIC)
	if idx != -1:
		AudioServer.set_bus_volume_db(idx, _linear_to_bus_db(music_volume))


func _apply_audio_mute() -> void:
	for bus_name in [BUS_SFX, BUS_UI, BUS_DIALOGUE]:
		var idx := AudioServer.get_bus_index(bus_name)
		if idx != -1:
			AudioServer.set_bus_mute(idx, not audio_enabled)


func _apply_audio_volume() -> void:
	for bus_name in [BUS_SFX, BUS_UI, BUS_DIALOGUE]:
		var idx := AudioServer.get_bus_index(bus_name)
		if idx != -1:
			AudioServer.set_bus_volume_db(idx, _linear_to_bus_db(audio_volume))


func _apply_fullscreen() -> void:
	if fullscreen:
		DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_FULLSCREEN)
	else:
		DisplayServer.window_set_mode(DisplayServer.WINDOW_MODE_WINDOWED)


func _apply_vsync() -> void:
	if vsync:
		DisplayServer.window_set_vsync_mode(DisplayServer.VSYNC_ENABLED)
	else:
		DisplayServer.window_set_vsync_mode(DisplayServer.VSYNC_DISABLED)
