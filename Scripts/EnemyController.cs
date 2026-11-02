
using Godot;

public partial class EnemyController : CharacterBody2D
{
	[Export]
	public float MoveSpeed { get; set; } = 60.0f;

	[Export]
	public float StopDistance { get; set; } = 22.0f;

	private CharacterBody2D _player;
	private AnimatedSprite2D _sprite;

	private Vector2 _lastDirection = Vector2.Down;

	public override void _Ready()
	{
		_sprite = GetNode<AnimatedSprite2D>("Visuals/AnimatedSprite2D");

		_player = GetTree().GetFirstNodeInGroup("player") as CharacterBody2D
			?? GetTree().GetFirstNodeInGroup("Player") as CharacterBody2D;

		if (_player == null)
		{
			GD.PushWarning("Enemy could not find the Player!");
			return;
		}

		UpdateAnimation("idle", _lastDirection);
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_player == null)
			return;

		Vector2 direction = _player.GlobalPosition - GlobalPosition;
		float distance = direction.Length();

		if (distance > 0.001f)
			_lastDirection = direction.Normalized();

		if (distance <= StopDistance)
		{
			Velocity = Vector2.Zero;
			MoveAndSlide();

			UpdateAnimation("idle", _lastDirection);
			return;
		}

		Velocity = direction.Normalized() * MoveSpeed;

		MoveAndSlide();

		UpdateAnimation("move", _lastDirection);
	}

	private void UpdateAnimation(string state, Vector2 direction)
	{
		string animation = GetDirectionName(direction);
		StringName animationName = new StringName($"{state}_{animation}");

		if (_sprite.Animation != animationName || !_sprite.IsPlaying())
		{
			_sprite.Play(animationName);
		}
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
