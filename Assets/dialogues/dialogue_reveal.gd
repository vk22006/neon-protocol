extends RichTextLabel
@export var typing_time: float #typing time in second(s)

	
func reveal_text():
	var tween = create_tween()
	tween.tween_property(self, "visible_ratio", 1.0, typing_time).from(0.0)
	return tween
	
func change_text(role: String, play_popup: bool = true) -> void:
	var trimmed := role.strip_edges()
	var is_same_line := (trimmed == self.text.strip_edges() and self.visible_ratio >= 1.0)

	if play_popup and not trimmed.is_empty() and not is_same_line:
		if AudioManager != null and AudioManager.has_method("play_dialogue_popup"):
			AudioManager.play_dialogue_popup()

	self.text = role
	var tween = reveal_text()
	await tween.finished
