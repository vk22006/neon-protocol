using Godot;

public partial class BossLaser : Area2D {
	[ExportGroup("Laser Settings")]
	[Export]
	public int Damage { get; set; } = 25;

	[Export]
	public float Duration { get; set; } = 0.85f;

	[Export]
	public Vector2 LaserScale { get; set; } = new Vector2(0.075f, 0.075f);

	[ExportGroup("Emitter Alignment (Unscaled)")]
	// In the unscaled 1730x270 texture, the collision shape begins at x = -774, y = -9.5 relative to center.
	[Export]
	public float EmitterOffsetUnscaledX { get; set; } = -774.0f;

	[Export]
	public float EmitterOffsetUnscaledY { get; set; } = -9.5f;

	private AnimatedSprite2D _sprite;
	private CollisionShape2D _collisionShape;
	private float _timer = 0.0f;
	private bool _isActive = false;
	private bool _hasDamaged = false;

	public bool IsActive => _isActive;

	public override void _Ready() {
		_sprite = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
		_collisionShape = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");

		BodyEntered += OnBodyEntered;

		// Initial state is hidden and inactive unless already activated
		if (!_isActive) {
			Visible = false;
			if (_collisionShape != null) {
				_collisionShape.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
			}
			if (_sprite != null) {
				_sprite.Stop();
			}
		}
	}

	public void Activate(Vector2 muzzlePosition, Vector2 targetPosition, int damage, float duration) {
		Damage = damage;
		Duration = duration;
		_timer = 0.0f;
		_isActive = true;
		_hasDamaged = false;

		// Calculate direction from muzzle to target
		Vector2 toTarget = targetPosition - muzzlePosition;
		if (toTarget.LengthSquared() < 0.001f) {
			toTarget = Vector2.Right;
		}

		float angle = toTarget.Angle();
		Rotation = angle;
		Scale = LaserScale;

		// Align the glowing emitter end of the beam precisely with the gun muzzle
		Vector2 emitterLocal = new Vector2(
			EmitterOffsetUnscaledX * Scale.X,
			EmitterOffsetUnscaledY * Scale.Y
		);
		GlobalPosition = muzzlePosition - emitterLocal.Rotated(angle);

		Visible = true;

		if (_collisionShape != null) {
			_collisionShape.SetDeferred(CollisionShape2D.PropertyName.Disabled, false);
		}

		if (_sprite != null) {
			_sprite.Visible = true;
			_sprite.Play("laser_shoot");
		}

		// Perform an immediate overlap check in case the player is already intersecting the beam
		Callable.From(CheckInitialOverlaps).CallDeferred();
	}

	private void CheckInitialOverlaps() {
		if (!_isActive || _hasDamaged) {
			return;
		}

		var overlappingBodies = GetOverlappingBodies();
		foreach (Node2D body in overlappingBodies) {
			TryDamagePlayer(body);
			if (_hasDamaged) {
				break;
			}
		}
	}

	public override void _PhysicsProcess(double delta) {
		if (!_isActive) {
			return;
		}

		_timer += (float)delta;
		if (_timer >= Duration) {
			Deactivate();
			QueueFree();
		}
	}

	private void OnBodyEntered(Node2D body) {
		TryDamagePlayer(body);
	}

	private void TryDamagePlayer(Node2D body) {
		if (!_isActive || _hasDamaged) {
			return;
		}

		// Only target the player
		if (body is PlayerController player && !player.IsDead) {
			_hasDamaged = true;
			player.TakeDamage(Damage);
		}
	}

	public void Deactivate() {
		_isActive = false;
		Visible = false;

		if (_collisionShape != null) {
			_collisionShape.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
		}

		if (_sprite != null) {
			_sprite.Stop();
		}
	}
}

