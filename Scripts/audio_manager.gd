
extends Node

const SILENT_DB := -80.0

@export_range(-40.0, 0.0, 0.5) var music_volume_db: float = -6.0
@export_group("UI Audio")
@export var ui_hover_sound: AudioStream
@export var ui_click_sound: AudioStream
@export_group("Dialogue Audio")
@export var dialogue_popup_sound: AudioStream
@export_range(-40.0, 6.0, 0.5) var dialogue_popup_volume_db: float = 0.0

var music_player: AudioStreamPlayer
@onready var ui_hover_player: AudioStreamPlayer = $UIHoverPlayer
@onready var ui_click_player: AudioStreamPlayer = $UIClickPlayer
@onready var dialogue_popup_player: AudioStreamPlayer = $DialoguePopupPlayer

var _music_tween: Tween
var _music_request_id: int = 0
var _requested_track: AudioStream


func _ready() -> void:
	process_mode = Node.PROCESS_MODE_ALWAYS

	print("AudioManager path: ", get_path())

	for child in get_children():
		print("Child: ", child.name, " | Type: ", child.get_class())

	music_player = get_node_or_null("MusicPlayer") as AudioStreamPlayer

	if music_player == null:
		push_error("AudioManager: MusicPlayer is missing or is not an AudioStreamPlayer.")
		return

	print("MusicPlayer found: ", music_player.get_path())

	_requested_track = music_player.stream


func play_music(track: AudioStream, fade_duration: float = 1.0) -> void:
	if track == null:
		push_warning("AudioManager: No music track provided.")
		return

	if track == _requested_track and music_player.playing:
		return

	_requested_track = track
	_music_request_id += 1
	var request_id := _music_request_id

	if _music_tween != null and _music_tween.is_valid():
		_music_tween.kill()

	_music_tween = create_tween()
	_music_tween.set_trans(Tween.TRANS_SINE)
	_music_tween.set_ease(Tween.EASE_IN_OUT)

	_music_tween.tween_property(
		music_player,
		"volume_db",
		SILENT_DB,
		fade_duration
	)

	_music_tween.tween_callback(
		_start_music_track.bind(track, request_id, fade_duration)
	)


func _start_music_track(
	track: AudioStream,
	request_id: int,
	fade_duration: float
) -> void:
	if request_id != _music_request_id:
		return

	music_player.stream = track
	music_player.play()

	_music_tween = create_tween()
	_music_tween.set_trans(Tween.TRANS_SINE)
	_music_tween.set_ease(Tween.EASE_IN_OUT)

	_music_tween.tween_property(
		music_player,
		"volume_db",
		music_volume_db,
		fade_duration
	)


func stop_music(fade_duration: float = 1.0) -> void:
	_requested_track = null
	_music_request_id += 1
	var request_id := _music_request_id

	if _music_tween != null and _music_tween.is_valid():
		_music_tween.kill()

	_music_tween = create_tween()
	_music_tween.set_trans(Tween.TRANS_SINE)
	_music_tween.set_ease(Tween.EASE_IN_OUT)

	_music_tween.tween_property(
		music_player,
		"volume_db",
		SILENT_DB,
		fade_duration
	)

	_music_tween.tween_callback(_finish_stop.bind(request_id))


func _finish_stop(request_id: int) -> void:
	if request_id != _music_request_id:
		return

	music_player.stop()
	
# UI Audio Controls
	
func play_ui_hover() -> void:
	if ui_hover_sound == null:
		return

	ui_hover_player.stream = ui_hover_sound
	ui_hover_player.play()


func play_ui_click() -> void:
	if ui_click_sound == null:
		return

	ui_click_player.stream = ui_click_sound
	ui_click_player.play()
	
func register_ui_buttons(root: Node) -> void:
	var buttons := root.find_children("*", "BaseButton", true, false)

	for button in buttons:
		if not button.mouse_entered.is_connected(play_ui_hover):
			button.mouse_entered.connect(play_ui_hover)

		if not button.focus_entered.is_connected(play_ui_hover):
			button.focus_entered.connect(play_ui_hover)

		if not button.pressed.is_connected(play_ui_click):
			button.pressed.connect(play_ui_click)


# Dialogue Audio Controls

func play_dialogue_popup() -> void:
	var sound: AudioStream = dialogue_popup_sound
	if sound == null:
		sound = preload("res://Assets/SFX/Dialogues/forceField_003.ogg")

	if dialogue_popup_player == null:
		dialogue_popup_player = get_node_or_null("DialoguePopupPlayer") as AudioStreamPlayer
		if dialogue_popup_player == null:
			dialogue_popup_player = AudioStreamPlayer.new()
			dialogue_popup_player.name = "DialoguePopupPlayer"
			dialogue_popup_player.bus = &"UI"
			dialogue_popup_player.process_mode = Node.PROCESS_MODE_ALWAYS
			dialogue_popup_player.max_polyphony = 2
			add_child(dialogue_popup_player)

	dialogue_popup_player.stream = sound
	dialogue_popup_player.bus = &"UI"
	dialogue_popup_player.volume_db = dialogue_popup_volume_db
	dialogue_popup_player.play()


# Spatial SFX Playback

func play_sfx_2d(stream: AudioStream, global_pos: Vector2, volume_db: float = 0.0) -> AudioStreamPlayer2D:
	if stream == null:
		return null

	var target_parent: Node = get_tree().current_scene
	if target_parent == null:
		target_parent = self

	var player := AudioStreamPlayer2D.new()
	player.stream = stream
	player.bus = &"SFX"
	player.volume_db = volume_db
	player.global_position = global_pos
	target_parent.add_child(player)
	player.finished.connect(player.queue_free)
	player.play()
	return player
