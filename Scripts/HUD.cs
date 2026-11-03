using Godot;

public partial class HUD : Control {
	[Export]
	public NodePath HealthBarPath { get; set; } = "HealthBarContainer/HealthBar";

	[Export]
	public NodePath HealthLabelPath { get; set; } = "HealthBarContainer/HealthLabel";

	[Export]
	public NodePath GameOverContainerPath { get; set; } = "GameOverContainer";

	private ProgressBar _healthBar;
	private Label _healthLabel;
	private Control _gameOverContainer;

	public override void _Ready() {
		_healthBar = GetNodeOrNull<ProgressBar>(HealthBarPath);
		_healthLabel = GetNodeOrNull<Label>(HealthLabelPath);
		_gameOverContainer = GetNodeOrNull<Control>(GameOverContainerPath);

		if (_gameOverContainer != null) {
			_gameOverContainer.Visible = false;
		}

		PlayerController player = GetTree().GetFirstNodeInGroup("player") as PlayerController
			?? GetTree().GetFirstNodeInGroup("Player") as PlayerController;

		if (player != null) {
			player.HealthChanged += OnPlayerHealthChanged;
			player.PlayerDied += OnPlayerDied;
			OnPlayerHealthChanged(player.CurrentHealth, player.MaxHealth);
		} else {
			GD.PushWarning("HUD could not find Player in group 'player'!");
		}
	}

	private void OnPlayerHealthChanged(int currentHealth, int maxHealth) {
		if (_healthBar != null) {
			_healthBar.MaxValue = maxHealth;
			_healthBar.Value = currentHealth;
		}

		if (_healthLabel != null) {
			_healthLabel.Text = $"HP {currentHealth}/{maxHealth}";
		}
	}

	private void OnPlayerDied() {
		if (_gameOverContainer != null) {
			_gameOverContainer.Visible = true;
		}
	}
}
