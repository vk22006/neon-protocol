extends Node2D

@onready var player = $Actors/Player
@onready var boss = $Actors/Boss
@onready var animation_player: AnimationPlayer = $AnimationPlayer
@onready var dialogue_box: Node2D = $DialogueBox
@onready var dialogue_background: ColorRect = $DialogueBox/ColorRect
@onready var role_label: RichTextLabel = $DialogueBox/Role
@onready var dialogue_label: RichTextLabel = $DialogueBox/Dialogue
@onready var player_camera: Camera2D = $Actors/Player/Camera2D
@onready var cinematic_camera: Camera2D = $CinematicCamera
@onready var scene_transition = get_node_or_null("/root/SceneTransition")
@onready var level_controller = $Gameplay/LevelController if has_node("Gameplay/LevelController") else get_tree().get_first_node_in_group("level_controller")

const BOSS_DIALOGUE_BG = Color("#7F0000")
const BOSS_DIALOGUE_TEXT = Color("#300000")

var original_dialogue_bg: Color
var original_dialogue_text: Color
var original_role_text: Color

func _ready() -> void:
	AudioManager.play_music(
		preload("res://Assets/music/combat_boss/magiksolo-informa-incubation-278098.mp3"),
		2.0
	)
	original_dialogue_bg = dialogue_background.color
	original_dialogue_text = dialogue_label.modulate
	original_role_text = role_label.modulate
	
	animation_player.animation_finished.connect(_on_animation_finished)

	if level_controller != null:
		level_controller.SetGameplayEnabled(false)
	else:
		player.SetInputEnabled(false)
	boss.set_physics_process(false)

	player_camera.enabled = false
	cinematic_camera.enabled = true

	dialogue_box.visible = false

	# Once Level 5 is initialized and ready to display, fade in from black
	if scene_transition != null:
		await scene_transition.FadeFromBlack()

	animation_player.play("boss_intro")
	
func start_dialogue() -> void:
	dialogue_box.visible = true

	role_label.change_text("Gary Thomson:")
	await dialogue_label.change_text("You've reached the central chamber. This is where our research team encountered the most dangerous entity.")
	await get_tree().create_timer(2.0).timeout

	role_label.change_text("Gary Thomson:")
	await dialogue_label.change_text("Wait... that's the entity responsible for the security breach.")
	await get_tree().create_timer(2.0).timeout

	role_label.change_text("Gary Thomson:")
	await dialogue_label.change_text("Be careful. Its weapon systems appear significantly more advanced than anything we've encountered.")
	await get_tree().create_timer(2.0).timeout
	
	# Boss threat dialogue
	set_boss_dialogue_style()

	role_label.change_text("Boss:")
	await dialogue_label.change_text("You shouldn't have come here.")
	await get_tree().create_timer(2.0).timeout

	role_label.change_text("Boss:")
	await dialogue_label.change_text("This facility is under my control. You will not leave alive.")
	await get_tree().create_timer(2.0).timeout
	
	# Restore the original colors immediately after the boss finishes speaking
	restore_dialogue_style()

	role_label.change_text("Gary Thomson:")
	await dialogue_label.change_text("It's targeting you! Get ready!")
	await get_tree().create_timer(2.0).timeout

	role_label.change_text("Gary Thomson:")
	await dialogue_label.change_text("I'm losing the signal. You're on your own now. Good luck.")
	await get_tree().create_timer(2.0).timeout

	dialogue_box.visible = false
	
func start_gameplay() -> void:
	player_camera.enabled = true
	cinematic_camera.enabled = false

	player.SetInputEnabled(true)
	boss.set_physics_process(true)
	
func _on_animation_finished(anim_name: StringName) -> void:
	if anim_name == "boss_intro":
		await start_dialogue()
		await transition_to_gameplay()
		
func transition_to_gameplay() -> void:
	# Hide dialogue
	dialogue_box.visible = false

	# Fade to black
	await scene_transition.FadeToBlack()
	
	AudioManager.play_music(
		preload("res://Assets/music/combat_boss/amarantamusic-robot-invasion-112138.wav"),
		2.0
	)

	# Switch cameras while screen is black
	player_camera.enabled = true
	cinematic_camera.enabled = false

	# Enable gameplay systems
	if level_controller != null:
		level_controller.SetGameplayEnabled(true)
	else:
		player.SetInputEnabled(true)
	boss.set_physics_process(true)

	# Fade back into gameplay
	await scene_transition.FadeFromBlack()
	
func set_boss_dialogue_style() -> void:
	dialogue_background.color = BOSS_DIALOGUE_BG
	dialogue_label.modulate = BOSS_DIALOGUE_TEXT
	role_label.modulate = BOSS_DIALOGUE_TEXT


func restore_dialogue_style() -> void:
	dialogue_background.color = original_dialogue_bg
	dialogue_label.modulate = original_dialogue_text
	role_label.modulate = original_dialogue_text
