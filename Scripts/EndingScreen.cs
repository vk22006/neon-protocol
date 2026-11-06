using Godot;

public partial class EndingScreen : Control {
	[Export]
	public NodePath CrystalsLabelPath { get; set; } = "CenterContainer/VBoxContainer/CrystalsLabel";

	private Label _crystalsLabel;
	private bool _isRestarting = false;

	public override void _Ready() {
		_crystalsLabel = GetNodeOrNull<Label>(CrystalsLabelPath);
		if (_crystalsLabel != null) {
			_crystalsLabel.Text = $"CRYSTALS SECURED: {LevelController.PreservedCrystalCount:D2}";
		}
		GD.Print($"[EndingScreen] Ending screen reached. Final crystals secured: {LevelController.PreservedCrystalCount}");
	}

	public override void _UnhandledInput(InputEvent @event) {
		if (_isRestarting || SceneTransition.IsTransitioning) {
			return;
		}

		if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo) {
			RestartRun();
		} else if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed) {
			RestartRun();
		}
	}

	public void RestartRun() {
		if (_isRestarting || SceneTransition.IsTransitioning) {
			return;
		}

		_isRestarting = true;
		GD.Print("[EndingScreen] Player initiated restart from ending screen. Resetting run to Level 1 with fade transition.");
		LevelController.PreservedCrystalCount = 0;
		SceneTransition.ChangeScene(GetTree(), "res://Scenes/Levels/Level_1.tscn");
	}
}

