
using Godot;

public partial class PlayerController : CharacterBody2D
{
	[Export]
	public float MoveSpeed { get; set; } = 120.0f;

	private AnimatedSprite2D _sprite;
	private Vector2 _lastDirection = Vector2.Down;

	public override void _Ready()
	{
		_sprite = GetNode<AnimatedSprite2D>("Visuals/AnimatedSprite2D");

		PlayIdleAnimation();
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

		MoveAndSlide();

		UpdateAnimation(direction);
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
