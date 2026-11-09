extends Node2D

@onready var animation_player: AnimationPlayer = $AnimationPlayer
@onready var dialogue_box: Node2D = $DialogueBox
@onready var role_label: RichTextLabel = $DialogueBox/Role
@onready var dialogue_label: RichTextLabel = $DialogueBox/Dialogue
@onready var dialogue_background: ColorRect = $DialogueBox/ColorRect
@onready var scene_transition = get_node_or_null("/root/SceneTransition")

const AWARD_DIALOGUE_BG = Color("#FFD700")
const AWARD_DIALOGUE_TEXT = Color("#B8860B")

var original_dialogue_bg: Color
var original_dialogue_text: Color

func _ready() -> void:
	original_dialogue_bg = dialogue_background.color
	original_dialogue_text = dialogue_label.modulate
	
	await start_dialogue()

func start_dialogue() -> void:
	animation_player.play("public_announcement")
	
	dialogue_box.visible = true
	dialogue_label.position = Vector2(118.444, 735.00)
	dialogue_label.size = Vector2(1020.0, 85.0)
	dialogue_label.typing_time = float(8.0)
	
	role_label.change_text("")
	await dialogue_label.change_text("For extraordinary courage, unwavering determination, and the protection of countless lives, we recognize an individual whose actions brought the truth to light.")
	
	await wait_for_animation("public_announcement")
	
	await get_tree().create_timer(1.0).timeout
	dialogue_box.visible = false
	dialogue_label.typing_time = float(5.0)
	
	dialogue_label.position = Vector2(118.444, 735.00)
	animation_player.play("awarding_scene")
	dialogue_box.visible = true
	role_label.change_text("")
	await dialogue_label.change_text("In recognition of these remarkable contributions, we hereby award...")
	
	dialogue_label.typing_time = float(3.0)
	set_award_dialogue_style()
	role_label.change_text("")
	await dialogue_label.change_text("The Order of Gaudium")
	
	await wait_for_animation("awarding_scene")
	
	await get_tree().create_timer(2.0).timeout
	restore_dialogue_style()
	
	dialogue_label.position = Vector2(118.444, 766.333)
	dialogue_label.size = Vector2(1020.0, 57.0)
	
	role_label.change_text("Gary Thomson:")
	await dialogue_label.change_text("You earned this! Every bit of it.")
	await get_tree().create_timer(2.0).timeout
	
	role_label.change_text("You:")
	await dialogue_label.change_text("We did this together!")

	await get_tree().create_timer(3.0).timeout
	dialogue_box.visible = false
	
func wait_for_animation(animation_name: StringName) -> void:
	while animation_player.is_playing() and animation_player.current_animation == animation_name:
		await get_tree().process_frame
	
func set_award_dialogue_style() -> void:
	dialogue_background.color = AWARD_DIALOGUE_BG
	dialogue_label.modulate = AWARD_DIALOGUE_TEXT


func restore_dialogue_style() -> void:
	dialogue_background.color = original_dialogue_bg
	dialogue_label.modulate = original_dialogue_text
