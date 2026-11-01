
using Godot;

public partial class PlayerController : CharacterBody2D
{
	[Export]
	public float MoveSpeed { get; set; } = 120.0f;

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
		UpdateAim();
	}
	
	private Node2D _visuals;

	public override void _Ready()
	{
		_visuals = GetNode<Node2D>("Visuals");
	}
	
		private void UpdateAim()
	{
		_visuals.LookAt(GetGlobalMousePosition());
	}
}
