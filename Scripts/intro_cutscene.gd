extends Node2D

@onready var animation_player: AnimationPlayer = $AnimationPlayer
@onready var dialogue_box: Node2D = $DialogueBox
@onready var role_label: RichTextLabel = $DialogueBox/Role
@onready var dialogue_label: RichTextLabel = $DialogueBox/Dialogue

func _ready() -> void:
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