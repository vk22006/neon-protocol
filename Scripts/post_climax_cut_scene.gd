extends Node2D

@onready var animation_player: AnimationPlayer = $AnimationPlayer
@onready var dialogue_box: Node2D = $DialogueBox
@onready var role_label: RichTextLabel = $DialogueBox/Role
@onready var dialogue_label: RichTextLabel = $DialogueBox/Dialogue
@onready var scene_transition = get_node_or_null("/root/SceneTransition")

func _ready() -> void:
	AudioManager.play_music(
		preload("res://Assets/music/ending/thoughtful_cinematic_1.wav"),
		2.0
	)
	animation_player.play("cutscene_opening")
	await animation_player.animation_finished
	
	await start_dialogue()
	
func start_dialogue() -> void:
	dialogue_box.visible = true
	dialogue_label.position = Vector2(118.444, 750.00)
	
	animation_player.play("player_wakes_up")
	role_label.change_text("")
	await dialogue_label.change_text("[You are waking up]")
	await animation_player.animation_finished

	dialogue_label.position = Vector2(118.444, 766.333)
	
	role_label.change_text("You:")
	await dialogue_label.change_text("Where... am I?")
	
	dialogue_box.visible = false
	await get_tree().create_timer(2.0).timeout
	
	animation_player.play("scientist_arrival")
	await animation_player.animation_finished
	
	dialogue_box.visible = true
	
	role_label.change_text("Gary Thomson:")
	await dialogue_label.change_text("Easy. You're safe. You're at my home.")
	await get_tree().create_timer(2.0).timeout

	role_label.change_text("You:")
	await dialogue_label.change_text("What happened? How did I get here?")
	await get_tree().create_timer(2.0).timeout

	role_label.change_text("Gary Thomson:")
	await dialogue_label.change_text("I found you unconscious outside the secret laboratory. You weren't responding, so I brought you here.")
	await get_tree().create_timer(2.0).timeout

	role_label.change_text("You:")
	await dialogue_label.change_text("The crystal artifacts... Are they safe?")
	await get_tree().create_timer(2.0).timeout

	role_label.change_text("Gary Thomson:")
	await dialogue_label.change_text("They're safe. I managed to secure them.")
	await get_tree().create_timer(2.0).timeout

	role_label.change_text("You:")
	await dialogue_label.change_text("Then we need to submit them to the government. They need to know what happened inside that facility.")
	await get_tree().create_timer(2.0).timeout

	role_label.change_text("Gary Thomson:")
	await dialogue_label.change_text("Agreed. We've been through too much to let the truth disappear.")
	await get_tree().create_timer(2.0).timeout

	role_label.change_text("You:")
	await dialogue_label.change_text("Let's go. It's time to put an end to this.")
	await get_tree().create_timer(2.0).timeout

	dialogue_box.visible = false

	animation_player.play("leaving_home")
	await animation_player.animation_finished
	
	await SceneTransition.FadeFromBlack()
	get_tree().change_scene_to_file("res://Scenes/Cutscenes/EndingIntermediateMessage.tscn")

	


	
