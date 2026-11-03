using Godot;

public partial class CrystalArtifact : Area2D {
	[Export]
	public int Value { get; set; } = 1;

	private bool _isCollected = false;
	private AnimatedSprite2D _sprite;
	private CollisionShape2D _collisionShape;

	public override void _Ready() {
		_sprite = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
		_collisionShape = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");

		if (_sprite != null && _sprite.SpriteFrames != null && _sprite.SpriteFrames.HasAnimation("spin")) {
			_sprite.Play("spin");
		}

		BodyEntered += OnBodyEntered;
	}

	private void OnBodyEntered(Node2D body) {
		if (_isCollected) {
			return;
		}

		if (body.IsInGroup("player") || body is PlayerController) {
			_isCollected = true;

			if (_collisionShape != null) {
				_collisionShape.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
			}

			if (body is PlayerController player) {
				player.CollectCrystal(Value);
			} else if (body.HasMethod("CollectCrystal")) {
				body.Call("CollectCrystal", Value);
			}

			QueueFree();
		}
	}
}
