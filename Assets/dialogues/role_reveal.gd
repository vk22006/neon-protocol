extends RichTextLabel
@export var typing_time: float #typing time in second(s)

func _ready() -> void:
	await change_text("Text:")


func change_text(role : String):
	self.text = role
	await get_tree().create_timer(2.0).timeout
