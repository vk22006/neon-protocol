extends Node2D

@onready var animation_player: AnimationPlayer = $AnimationPlayer
@onready var dialogue_box: Node2D = $DialogueBox
@onready var role_label: RichTextLabel = $DialogueBox/Role
@onready var dialogue_label: RichTextLabel = $DialogueBox/Dialogue
@onready var scene_transition = get_node_or_null("/root/SceneTransition")

func _ready() -> void:
	AudioManager.play_music(
		preload("res://Assets/music/mainMenu_Exploration/briefing-ambience.wav"),
		2.0
	)
	
	print("SceneTransition: ", scene_transition)
	animation_player.play("intro_opening")
	await animation_player.animation_finished

	animation_player.play("scientist_arrival")
	await animation_player.animation_finished
	
	await start_dialogue()
	
func start_dialogue() -> void:
	dialogue_box.visible = true

	# Initial briefing =======================================================================
	await role_label.change_text("Government Official:")
	await dialogue_label.change_text(
        "Dr. Thomson, thank you for coming. We have a classified matter requiring your expertise."
	)
	await get_tree().create_timer(2.5).timeout

	await role_label.change_text("Government Official:")
	await dialogue_label.change_text(
        "Our research facility has suffered a catastrophic security breach. We believe the incident is connected to an extraterrestrial entity."
	)

	await get_tree().create_timer(2.5).timeout

	await role_label.change_text("Gary Thomson")
	await dialogue_label.change_text(
    	"An extraterrestrial entity? What exactly happened at the facility?"
	)

	# Officials explaining the incident ====================================================
	await get_tree().create_timer(2.5).timeout

	await role_label.change_text("Government Official 2 (Defense):")
	await dialogue_label.change_text(
		"Several unknown entities infiltrated the facility. Our security systems were unable to contain them."
	)
	
	await get_tree().create_timer(2.5).timeout

	await role_label.change_text("Government Official 3 (Research):")
	await dialogue_label.change_text(
		"These entities do not resemble anything we've encountered before. Their structures and behaviour suggest an origin beyond our understanding of terrestrial technology."
	)

	await get_tree().create_timer(2.5).timeout

	await role_label.change_text("Gary Thomson:")
	await dialogue_label.change_text(
		"Are you telling me that something from outside our world has entered one of our research facilities?"
	)

	await get_tree().create_timer(2.5).timeout

	await role_label.change_text("Government Official 3 (Research):")
	await dialogue_label.change_text(
		"That's our current assessment. We believe they originated from Kepler, an extraterrestrial kingdom."
	)

	await get_tree().create_timer(2.5).timeout

	await role_label.change_text("Gary Thomson:")
	await dialogue_label.change_text(
		"Kepler? I've only encountered references to it in theoretical research. Are you certain?"
	)

	await get_tree().create_timer(2.5).timeout

	await role_label.change_text("Government Official:")
	await dialogue_label.change_text(
		"We have sufficient evidence to support that conclusion. However, the full extent of their presence remains unknown."
	)

	# Government's predicament ==============================================================
	await get_tree().create_timer(2.5).timeout

	await role_label.change_text("Government Official 2 (Defense):")
	await dialogue_label.change_text(
		"Our initial response teams failed to secure the facility. The situation has escalated beyond what our conventional forces can handle."
	)

	await get_tree().create_timer(2.5).timeout

	await role_label.change_text("Gary Thomson:")
	await dialogue_label.change_text(
		"Then why haven't you evacuated the entire facility and sealed it off?"
	)

	await get_tree().create_timer(2.5).timeout

	await role_label.change_text("Government Official 2 (Defense):")
	await dialogue_label.change_text(
		"Containment is no longer a reliable option. We need someone who can enter the facility, confront these entities, and retrieve the research materials."
	)

	await get_tree().create_timer(2.5).timeout

	await role_label.change_text("Gary Thomson:")
	await dialogue_label.change_text(
		"You want someone to go in there alone? Against something we barely understand?"
	)

	await get_tree().create_timer(2.5).timeout

	await role_label.change_text("Government Official:")
	await dialogue_label.change_text(
		"We understand the risks, Doctor. That's precisely why we cannot assign this mission to an ordinary operative."
	)

	# Asking to recommend an operative
	await get_tree().create_timer(2.5).timeout

	await role_label.change_text("Government Official 3 (Research):")
	await dialogue_label.change_text(
		"Dr. Thomson, you've worked with several highly experienced personnel throughout your research career. You understand the capabilities required for a mission like this."
	)

	await get_tree().create_timer(2.5).timeout

	await role_label.change_text("Government Official")
	await dialogue_label.change_text(
		"We need someone with exceptional combat experience, adaptability, and the ability to operate under extreme conditions."
	)

	await get_tree().create_timer(2.5).timeout

	await role_label.change_text("Government Official 2 (Defense):")
	await dialogue_label.change_text(
		"Someone who can handle an unknown threat without relying entirely on conventional military support."
	)

	await get_tree().create_timer(2.5).timeout

	await role_label.change_text("Government Official:")
	await dialogue_label.change_text(
		"Dr. Thomson, is there anyone you would recommend for this mission?"
	)
	
	await get_tree().create_timer(2.5).timeout

	# Hide dialogue box before transition
	dialogue_box.visible = false

	# Fade to black
	await scene_transition.FadeToBlack()

	# Time skip
	await get_tree().create_timer(1.5).timeout

	# Fade back into the meeting room
	await scene_transition.FadeFromBlack()

	# Player entrance
	animation_player.play("player_enterance")
	await animation_player.animation_finished

	# Restore dialogue box
	dialogue_box.visible = true

	# Gary recommends the player ===========================================================
	await role_label.change_text("Government Official:")
	await dialogue_label.change_text(
		"Dr. Thomson, is there anyone you would recommend for this mission?"
	)
	await get_tree().create_timer(2.5).timeout

	await role_label.change_text("Gary Thomson:")
	await dialogue_label.change_text(
		"Yes. I believe the person standing beside me is exactly who we need."
	)
	await get_tree().create_timer(2.5).timeout

	await role_label.change_text("Government Official 2 (Defense):")
	await dialogue_label.change_text(
		"You recommend this operative for a mission of this magnitude?"
	)
	await get_tree().create_timer(2.5).timeout

	await role_label.change_text("Gary Thomson:")
	await dialogue_label.change_text(
		"I've worked with them before. Their combat experience, adaptability, and ability to remain composed under pressure make them a suitable candidate."
	)
	await get_tree().create_timer(2.5).timeout

	await role_label.change_text("Government Official 3 (Research):")
	await dialogue_label.change_text(
		"Are you confident they can handle an extraterrestrial threat?"
	)
	await get_tree().create_timer(2.5).timeout

	await role_label.change_text("Gary Thomson:")
	await dialogue_label.change_text(
		"I wouldn't recommend them if I had any doubts."
	)
	await get_tree().create_timer(2.5).timeout

	await role_label.change_text("Government Official:")
	await dialogue_label.change_text(
		"Very Well. We will consider your recommendation"
	)
	await get_tree().create_timer(2.5).timeout

	await role_label.change_text("Government Official 2 (Defense):")
	await dialogue_label.change_text(
		"However, the mission is classified. The operative must understand that everything discussed here remains strictly confidential."
	)
	await get_tree().create_timer(2.5).timeout

	await role_label.change_text("Gary Thomson:")
	await dialogue_label.change_text(
		"Understood. I'll explain the situation and make sure they're aware of the risks."
	)
	await get_tree().create_timer(2.5).timeout

	# Mission Briefing =======================================================================================================
	await role_label.change_text("Government Official:")
	await dialogue_label.change_text(
		"Before we proceed, understand that this operation is strictly classified. All information discussed here must remain confidential."
	)
	await get_tree().create_timer(2.5).timeout

	await role_label.change_text("Government Official 2 (Defense):")
	await dialogue_label.change_text(
		"Your objective is to enter the research facility, eliminate the extraterrestrial entities, and retrieve the remaining research materials."
	)
	await get_tree().create_timer(2.5).timeout

	await role_label.change_text("Government Official 3 (Research):")
	await dialogue_label.change_text(
		"The crystals recovered from the facility are particularly important. Their properties may help us understand the nature of these entities and their connection to Kepler."
	)
	await get_tree().create_timer(2.5).timeout

	await role_label.change_text("Gary Thomson:")
	await dialogue_label.change_text(
		"The facility is extremely dangerous. We still don't understand the full extent of the infestation."
	)
	await get_tree().create_timer(2.5).timeout

	await role_label.change_text("Government Official:")
	await dialogue_label.change_text(
		"We must also make one thing clear. The rewards for this mission may not be particularly attractive."
	)
	await get_tree().create_timer(2.5).timeout

	await role_label.change_text("Government Official 2 (Defense):")
	await dialogue_label.change_text(
		"Given the classified nature of this operation, public recognition and conventional compensation may be limited."
	)
	await get_tree().create_timer(2.5).timeout

	await role_label.change_text("Gary Thomson:")
	await dialogue_label.change_text(
		"I understand. But this mission is important. If those entities spread beyond the facility, the consequences could be catastrophic."
	)
	await get_tree().create_timer(2.5).timeout

	await role_label.change_text("Government Official:")
	await dialogue_label.change_text(
		"We appreciate your cooperation, Dr. Thomson. The operative will receive the necessary mission details before deployment."
	)
	await get_tree().create_timer(2.5).timeout

	await role_label.change_text("Gary Thomson:")
	await dialogue_label.change_text(
		"I'll make sure everything is understood."
	)
	await get_tree().create_timer(2.5).timeout

	print("Intro finished")

	dialogue_box.visible = false

	await scene_transition.FadeToBlack()

	print("Fade to black completed")

	var error = get_tree().change_scene_to_file("res://Scenes/Levels/Level_1.tscn")

	print("Scene change result: ", error)
