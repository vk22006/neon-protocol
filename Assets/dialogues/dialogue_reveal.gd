extends RichTextLabel
@export var typing_time: float #typing time in second(s)

	
func reveal_text():
	var tween = create_tween()
	tween.tween_property(self, "visible_ratio", 1.0, typing_time).from(0.0)
	return tween
	
func change_text(role: String) -> void:
	self.text = role
	var tween = reveal_text()
	await tween.finished
