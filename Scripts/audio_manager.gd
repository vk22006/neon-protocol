
extends Node

const SILENT_DB := -80.0

@export_range(-40.0, 0.0, 0.5) var music_volume_db: float = -6.0

var music_player: AudioStreamPlayer

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
