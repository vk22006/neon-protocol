using Godot;

public partial class DoorController : Node2D {
	[Signal]
	public delegate void DoorOpenedEventHandler();

	[Export]
	public NodePath DoorVisualsPath { get; set; } = "DoorVisuals";

	[Export]
	public NodePath CollisionShapePath { get; set; } = "DoorCollision/CollisionShape2D";

	private AnimatedSprite2D _doorVisuals;
	private CollisionShape2D _collisionShape;
	private bool _isOpen = false;

	public bool IsOpen => _isOpen;

	public override void _Ready() {
		if (!IsInGroup("door")) {
			AddToGroup("door");
		}

		_doorVisuals = GetNodeOrNull<AnimatedSprite2D>(DoorVisualsPath);
		_collisionShape = GetNodeOrNull<CollisionShape2D>(CollisionShapePath);

		if (_doorVisuals != null) {
			if (_doorVisuals.SpriteFrames != null && _doorVisuals.SpriteFrames.HasAnimation("success")) {
				_doorVisuals.SpriteFrames.SetAnimationLoopMode("success", SpriteFrames.LoopMode.None);
			}
			_doorVisuals.AnimationFinished += OnAnimationFinished;
			_doorVisuals.Play("default");
		}

		if (_collisionShape != null) {
			_collisionShape.SetDeferred(CollisionShape2D.PropertyName.Disabled, false);
		}
	}

	public void Open() {
		if (_isOpen) {
			return;
		}

		_isOpen = true;

		if (_doorVisuals != null && _doorVisuals.SpriteFrames != null && _doorVisuals.SpriteFrames.HasAnimation("success")) {
			_doorVisuals.Play("success");
		} else {
			if (_collisionShape != null) {
				_collisionShape.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
			}
			EmitSignal(SignalName.DoorOpened);
		}
	}

	private void OnAnimationFinished() {
		if (_doorVisuals != null && _doorVisuals.Animation == "success") {
			if (_collisionShape != null) {
				_collisionShape.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
			}
			EmitSignal(SignalName.DoorOpened);
		}
	}
}
