using Godot;

public partial class PlayerController : CharacterBody2D
{
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

	private AnimatedSprite2D _sprite;
	private Vector2 _lastDirection = Vector2.Down;
	private Node2D _weaponPivot;

	private enum FacingDirection
	{
		Up,
		Down,
		Left,
		Right
	}

	private FacingDirection _facingDirection = FacingDirection.Down;

	public override void _Ready()
	{
		_sprite = GetNode<AnimatedSprite2D>("Visuals/AnimatedSprite2D");
		_weaponPivot = GetNode<Node2D>("WeaponPivot");

		PlayIdleAnimation();
		UpdateWeaponPosition();
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector2 direction = Input.GetVector(
			"move_left",
			"move_right",
			"move_up",
			"move_down"
		);

		Velocity = direction * MoveSpeed;

		if (direction != Vector2.Zero)
		{
			UpdateFacingDirection(direction);
			UpdateWeaponPosition();
		}

		MoveAndSlide();

		UpdateAnimation(direction);
		UpdateAim();
	}

	private void UpdateFacingDirection(Vector2 direction)
	{
		if (Mathf.Abs(direction.X) > Mathf.Abs(direction.Y))
		{
			_facingDirection = direction.X > 0
				? FacingDirection.Right
				: FacingDirection.Left;
		}
		else
		{
			_facingDirection = direction.Y > 0
				? FacingDirection.Down
				: FacingDirection.Up;
		}
	}

	private void UpdateWeaponPosition()
	{
		switch (_facingDirection)
		{
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

	private void UpdateAnimation(Vector2 direction)
	{
		if (direction != Vector2.Zero)
		{
			_lastDirection = direction;

			string animation = GetDirectionName(_lastDirection);
			_sprite.Play("walk_" + animation);
		}
		else
		{
			PlayIdleAnimation();
		}
	}

	private void UpdateAim()
	{
		Vector2 mousePosition = GetGlobalMousePosition();
		Vector2 aimVector = mousePosition - _weaponPivot.GlobalPosition;

		if (aimVector.LengthSquared() < 0.001f)
			return;

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

		UpdateWeaponLayer(angle, aimingLeft);
	}

	private void UpdateWeaponLayer(float aimAngle, bool aimingLeft)
	{
		bool aimingUp = Mathf.Sin(aimAngle) < 0.0f;
		bool renderBehind = false;

		switch (_facingDirection)
		{
			case FacingDirection.Up:
				// Facing away from camera: character's back faces viewer, weapon renders behind.
				renderBehind = true;
				break;

			case FacingDirection.Down:
				// Facing towards camera: weapon is in front, unless aiming upward behind the head.
				renderBehind = aimingUp;
				break;

			case FacingDirection.Left:
				// Facing left: weapon is in front when aiming forward/downward (left/down),
				// and behind when aiming upward or backwards across the body (aiming right).
				renderBehind = aimingUp || !aimingLeft;
				break;

			case FacingDirection.Right:
				// Facing right: weapon is in front when aiming forward/downward (right/down),
				// and behind when aiming upward or backwards across the body (aiming left).
				renderBehind = aimingUp || aimingLeft;
				break;
		}

		_weaponPivot.ZIndex = renderBehind ? -1 : 1;
	}

	private void PlayIdleAnimation()
	{
		string animation = GetDirectionName(_lastDirection);
		_sprite.Play("idle_" + animation);
	}

	private string GetDirectionName(Vector2 direction)
	{
		if (Mathf.Abs(direction.X) > Mathf.Abs(direction.Y))
		{
			return direction.X > 0 ? "right" : "left";
		}

		return direction.Y > 0 ? "down" : "up";
	}
}
