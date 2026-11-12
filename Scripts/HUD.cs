using Godot;

public partial class HUD : Control {
	[Export]
	public NodePath HealthBarPath { get; set; } = "StatusContainer/HealthBarContainer/HealthBar";

	[Export]
	public NodePath HealthLabelPath { get; set; } = "StatusContainer/HealthBarContainer/HealthLabel";

	[Export]
	public NodePath CrystalCountLabelPath { get; set; } = "StatusContainer/CrystalCounter/CrystalCountLabel";

	[Export]
	public NodePath GameOverContainerPath { get; set; } = "GameOverContainer";

	[Export]
	public NodePath PauseButtonPath { get; set; } = "PauseButton";

	private ProgressBar _healthBar;
	private Label _healthLabel;
	private Label _crystalCountLabel;
	private Control _gameOverContainer;
	private BaseButton _pauseButton;

	public override void _Ready() {
		_healthBar = GetNodeOrNull<ProgressBar>(HealthBarPath)
			?? GetNodeOrNull<ProgressBar>("StatusContainer/HealthBarContainer/HealthBar")
			?? GetNodeOrNull<ProgressBar>("HealthBarContainer/HealthBar");

		_healthLabel = GetNodeOrNull<Label>(HealthLabelPath)
			?? GetNodeOrNull<Label>("StatusContainer/HealthBarContainer/HealthLabel")
			?? GetNodeOrNull<Label>("HealthBarContainer/HealthLabel");

		_crystalCountLabel = GetNodeOrNull<Label>(CrystalCountLabelPath)
			?? GetNodeOrNull<Label>("StatusContainer/CrystalCounter/CrystalCountLabel")
			?? GetNodeOrNull<Label>("CrystalCounter/CrystalCountLabel");

		_gameOverContainer = GetNodeOrNull<Control>(GameOverContainerPath)
			?? GetNodeOrNull<Control>("GameOverContainer")
			?? GetNodeOrNull<Control>("GameOverOverlay");

		_pauseButton = GetNodeOrNull<BaseButton>(PauseButtonPath)
			?? GetNodeOrNull<BaseButton>("PauseButton");
		Node audioManager = GetNodeOrNull<Node>("/root/AudioManager");

		if (audioManager != null) {
			audioManager.Call("register_ui_buttons", this);
		} else {
			GD.PushWarning("HUD could not find AudioManager autoload!");
		}

		if (_gameOverContainer != null) {
			_gameOverContainer.Visible = false;
		}

		if (_pauseButton != null) {
			_pauseButton.Pressed += OnPauseButtonPressed;
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

	public override void _Process(double delta) {
		if (_pauseButton != null) {
			_pauseButton.Visible = PauseMenu.CanPauseGame(GetTree(), _gameOverContainer);
		}
	}

	private void OnPauseButtonPressed() {
		if (PauseMenu.CanPauseGame(GetTree(), _gameOverContainer)) {
			PauseMenu.TogglePauseGame(GetTree());
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

		if (_pauseButton != null) {
			_pauseButton.Visible = false;
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
