extends Node2D

@onready var dialogue_box: Node2D = $DialogueBox
@onready var role_label: RichTextLabel = $DialogueBox/Role
@onready var dialogue_label: RichTextLabel = $DialogueBox/Dialogue
@onready var player = $Actors/Player
@onready var scene_transition = get_node_or_null("/root/SceneTransition")

func _ready() -> void:
	player.SetInputEnabled(false)
	set_enemies_enabled(false)

	print("Level 1 loaded")

	await scene_transition.FadeFromBlack()

	print("Fade in completed")

	await start_dialogue()

	player.SetInputEnabled(true)
	set_enemies_enabled(true)

func start_dialogue() -> void:
	dialogue_box.visible = true
	dialogue_label.position = Vector2(118.444, 750.00)

	role_label.change_text("")
	await dialogue_label.change_text("You're approaching the research facility now. This is where everything went wrong.")

	await get_tree().create_timer(2.0).timeout

	role_label.change_text("")
	await dialogue_label.change_text("Your primary objective is to eliminate the hostile entities inside and recover the research crystals.")

	await get_tree().create_timer(2.0).timeout

	role_label.change_text("")
	await dialogue_label.change_text("Stay alert. We don't know how many of them remain, or what else might be hiding inside.")

	await get_tree().create_timer(2.0).timeout

	role_label.change_text("")
	await dialogue_label.change_text("Use your weapon to defend yourself. Keep moving, and don't let them surround you.")

	await get_tree().create_timer(2.0).timeout

	role_label.change_text("")
	await dialogue_label.change_text("Dr. Thomson will remain in contact with you. Good luck.")

	await get_tree().create_timer(2.0).timeout

	dialogue_box.visible = false

func set_enemies_enabled(enabled: bool) -> void:
	var enemies = get_tree().get_nodes_in_group("enemies")
	for enemy in enemies:
		enemy.SetAIEnabled(enabled)
