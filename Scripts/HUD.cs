using Godot;

public partial class HUD : Control {
	[Export]
	public NodePath HealthBarPath { get; set; } = "HealthBarContainer/HealthBar";

	[Export]
	public NodePath HealthLabelPath { get; set; } = "HealthBarContainer/HealthLabel";

	[Export]
	public NodePath CrystalCountLabelPath { get; set; } = "CrystalCounter/CrystalCountLabel";

	[Export]
	public NodePath GameOverContainerPath { get; set; } = "GameOverContainer";

	private ProgressBar _healthBar;
	private Label _healthLabel;
	private Label _crystalCountLabel;
	private Control _gameOverContainer;

	public override void _Ready() {
		_healthBar = GetNodeOrNull<ProgressBar>(HealthBarPath);
		_healthLabel = GetNodeOrNull<Label>(HealthLabelPath);
		_crystalCountLabel = GetNodeOrNull<Label>(CrystalCountLabelPath);
		_gameOverContainer = GetNodeOrNull<Control>(GameOverContainerPath);

		if (_gameOverContainer != null) {
			_gameOverContainer.Visible = false;
		}

		UpdateCrystalCount(0);

		PlayerController player = GetTree().GetFirstNodeInGroup("player") as PlayerController
			?? GetTree().GetFirstNodeInGroup("Player") as PlayerController;

		if (player != null) {
			player.HealthChanged += OnPlayerHealthChanged;
			player.PlayerDied += OnPlayerDied;
			player.CrystalCountChanged += OnCrystalCountChanged;
			OnPlayerHealthChanged(player.CurrentHealth, player.MaxHealth);
			OnCrystalCountChanged(player.CrystalCount);
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
			_healthLabel.Text = $"{currentHealth} / {maxHealth}";
		}
	}

	private void OnPlayerDied() {
		if (_gameOverContainer != null) {
			_gameOverContainer.Visible = true;
		}
	}

	public void UpdateCrystalCount(int count) {
		if (_crystalCountLabel != null) {
			_crystalCountLabel.Text = count.ToString("D2");
		}
	}

	private void OnCrystalCountChanged(int count) {
		UpdateCrystalCount(count);
	}
}
