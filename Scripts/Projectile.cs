using Godot;

public partial class Projectile : Area2D {
	[Export]
	public float Speed { get; set; } = 400.0f;

	[Export]
	public float Lifetime { get; set; } = 2.0f;

	// The projectile artwork (bullet.png) points North (-Y).
	// In 2D, 0 radians corresponds to East (+X), so a 90° (+π/2 rad) correction
	// aligns the vertical sprite and capsule shape with the direction of travel.
	[Export]
	public float VisualRotationOffset { get; set; } = Mathf.Pi / 2.0f;

	[Export]
	public int Damage { get; set; } = 10;

	private Vector2 _direction = Vector2.Right;

	public void Initialize(Vector2 direction) {
		_direction = direction.Normalized();
		Rotation = _direction.Angle() + VisualRotationOffset;
	}

	public override void _Ready() {
		BodyEntered += OnBodyEntered;
	}

	public override void _PhysicsProcess(double delta) {
		Position += _direction * Speed * (float)delta;

		Lifetime -= (float)delta;

		if (Lifetime <= 0.0f) {
			QueueFree();
		}
	}

	private void OnBodyEntered(Node2D body) {
		if (body is EnemyController enemy) {
			enemy.TakeDamage(Damage);
			QueueFree();
		} else if (body is TileMapLayer || body is StaticBody2D) {
			QueueFree();
		}
	}
}
