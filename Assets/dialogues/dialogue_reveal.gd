extends RichTextLabel
@export var typing_time: float #typing time in second(s)

func _ready() -> void:
	await change_text("Hello, world!")
	
func reveal_text():
	var tween = create_tween()
	tween.tween_property(self, "visible_ratio", 1.0, typing_time).from(0.0)
	return tween
	
func change_text(role : String):
	self.text = role
	reveal_text()
	await get_tree().create_timer(2.0).timeout
