using Godot;

public partial class PlayerController : CharacterBody2D {
	[Export]
	public float MoveSpeed { get; set; } = 120.0f;

	[ExportGroup("Weapon Attachment Offsets")]
	[Export]
	public Vector2 WeaponOffsetUp { get; set; } = new Vector2(6, 7);

	[Export]
	public Vector2 WeaponOffsetDown { get; set; } = new Vector2(6, 7);

	[Export]
	public Vector2 WeaponOffsetLeft { get; set; } = new Vector2(-6, 7);

	[Export]
	public Vector2 WeaponOffsetRight { get; set; } = new Vector2(6, 7);

	[ExportGroup("Weapon Layering")]
	[Export(PropertyHint.Range, "0,45,1")]
	public float UpwardAimThresholdDegrees { get; set; } = 25.0f;

	private AnimatedSprite2D _sprite;
	private Vector2 _lastDirection = Vector2.Down;
	private Node2D _weaponPivot;

	private enum FacingDirection {
		Up,
		Down,
		Left,
		Right
	}

	private FacingDirection _facingDirection = FacingDirection.Down;

	public override void _Ready() {
		_sprite = GetNode<AnimatedSprite2D>("Visuals/AnimatedSprite2D");
		_weaponPivot = GetNode<Node2D>("WeaponPivot");

		PlayIdleAnimation();
		UpdateWeaponPosition();
	}

	public override void _PhysicsProcess(double delta) {
		Vector2 direction = Input.GetVector(
			"move_left",
			"move_right",
			"move_up",
			"move_down"
		);

		Velocity = direction * MoveSpeed;

		if (direction != Vector2.Zero) {
			UpdateFacingDirection(direction);
			UpdateWeaponPosition();
		}

		MoveAndSlide();

		UpdateAnimation(direction);
		UpdateAim();
	}

	private void UpdateFacingDirection(Vector2 direction) {
		if (Mathf.Abs(direction.X) > Mathf.Abs(direction.Y)) {
			_facingDirection = direction.X > 0
				? FacingDirection.Right
				: FacingDirection.Left;
		} else {
			_facingDirection = direction.Y > 0
				? FacingDirection.Down
				: FacingDirection.Up;
		}
	}

	private void UpdateWeaponPosition() {
		switch (_facingDirection) {
			case FacingDirection.Up:
				_weaponPivot.Position = WeaponOffsetUp;
				break;

			case FacingDirection.Down:
				_weaponPivot.Position = WeaponOffsetDown;
				break;

			case FacingDirection.Left:
				_weaponPivot.Position = WeaponOffsetLeft;
				break;

			case FacingDirection.Right:
				_weaponPivot.Position = WeaponOffsetRight;
				break;
		}
	}

	private void UpdateAnimation(Vector2 direction) {
		if (direction != Vector2.Zero) {
			_lastDirection = direction;

			string animation = GetDirectionName(_lastDirection);
			_sprite.Play("walk_" + animation);
		} else {
			PlayIdleAnimation();
		}
	}

	private void UpdateAim() {
		Vector2 mousePosition = GetGlobalMousePosition();
		Vector2 aimVector = mousePosition - _weaponPivot.GlobalPosition;

		if (aimVector.LengthSquared() < 0.001f) {
			return;
		}

		float angle = aimVector.Angle();
		_weaponPivot.Rotation = angle;

		// Keep the weapon upright when aiming left.
		// Since the grip origin is at (0, 0) and barrel centerline is at Y = 0,
		// flipping produces no displacement at the hand attachment point.
		bool aimingLeft = Mathf.Cos(angle) < 0.0f;

		_weaponPivot.Scale = new Vector2(
			1.0f,
			aimingLeft ? -1.0f : 1.0f
		);

		UpdateWeaponLayer(angle);
	}

	private void UpdateWeaponLayer(float aimAngle) {
		float aimAngleDeg = Mathf.RadToDeg(aimAngle);

		// Upward aim angles fall in the negative degrees range [-180, 0].
		// We define a horizontal margin so horizontal aiming (East ~0°, West ~±180°)
		// remains solidly in front of the player and never prematurely switches behind.
		bool aimingUpward = aimAngleDeg < -UpwardAimThresholdDegrees &&
			aimAngleDeg > -(180.0f - UpwardAimThresholdDegrees);

		// Render behind the character when aiming upward (NE, NW, North).
		// In NE and NW, the barrel extends past the body and remains partially visible.
		// Directly North, the weapon is positioned along the spine and naturally occluded by the character.
		// Downward and horizontal aiming render in front of the player.
		bool renderBehind = aimingUpward;

		_weaponPivot.ZIndex = renderBehind ? -1 : 1;
	}

	private void PlayIdleAnimation() {
		string animation = GetDirectionName(_lastDirection);
		_sprite.Play("idle_" + animation);
	}

	private string GetDirectionName(Vector2 direction) {
		if (Mathf.Abs(direction.X) > Mathf.Abs(direction.Y)) {
			return direction.X > 0 ? "right" : "left";
		}

		return direction.Y > 0 ? "down" : "up";
	}
}
