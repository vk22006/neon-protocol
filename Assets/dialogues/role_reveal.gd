extends RichTextLabel
@export var typing_time: float #typing time in second(s)



func change_text(role: String) -> void:
	self.text = role
	self.visible_ratio = 1.0
