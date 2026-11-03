using Godot;

public partial class HUD : Control {
	[Export]
	public NodePath HealthBarPath { get; set; } = "HealthBarContainer/HealthBar";

	[Export]
	public NodePath HealthLabelPath { get; set; } = "HealthBarContainer/HealthLabel";

	[Export]
	public NodePath EnemyHealthContainerPath { get; set; } = "EnemyHealthContainer";

	[Export]
	public NodePath EnemyHealthBarPath { get; set; } = "EnemyHealthContainer/EnemyHealthBar";

	[Export]
	public NodePath EnemyHealthLabelPath { get; set; } = "EnemyHealthContainer/EnemyHealthLabel";

	[Export]
	public NodePath CrystalCountLabelPath { get; set; } = "CrystalCounter/CrystalCountLabel";

	[Export]
	public NodePath GameOverContainerPath { get; set; } = "GameOverContainer";

	private ProgressBar _healthBar;
	private Label _healthLabel;
	private Control _enemyHealthContainer;
	private ProgressBar _enemyHealthBar;
	private Label _enemyHealthLabel;
	private Label _crystalCountLabel;
	private Control _gameOverContainer;

	private EnemyController _trackedEnemy;

	public override void _Ready() {
		_healthBar = GetNodeOrNull<ProgressBar>(HealthBarPath);
		_healthLabel = GetNodeOrNull<Label>(HealthLabelPath);
		_enemyHealthContainer = GetNodeOrNull<Control>(EnemyHealthContainerPath);
		_enemyHealthBar = GetNodeOrNull<ProgressBar>(EnemyHealthBarPath);
		_enemyHealthLabel = GetNodeOrNull<Label>(EnemyHealthLabelPath);
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

		EnemyController enemy = GetTree().GetFirstNodeInGroup("enemy") as EnemyController
			?? GetTree().GetFirstNodeInGroup("Enemy") as EnemyController;

		if (enemy != null) {
			TrackEnemy(enemy);
		} else if (_enemyHealthContainer != null) {
			_enemyHealthContainer.Visible = false;
		}
	}

	public override void _ExitTree() {
		UntrackEnemy();
	}

	public void TrackEnemy(EnemyController enemy) {
		UntrackEnemy();

		if (!GodotObject.IsInstanceValid(enemy)) {
			if (_enemyHealthContainer != null) {
				_enemyHealthContainer.Visible = false;
			}
			return;
		}

		_trackedEnemy = enemy;
		if (_enemyHealthContainer != null) {
			_enemyHealthContainer.Visible = true;
		}

		_trackedEnemy.HealthChanged += OnEnemyHealthChanged;
		_trackedEnemy.EnemyDied += OnEnemyDied;
		_trackedEnemy.TreeExiting += OnEnemyTreeExiting;

		OnEnemyHealthChanged(_trackedEnemy.CurrentHealth, _trackedEnemy.MaxHealth);
	}

	private void UntrackEnemy() {
		if (GodotObject.IsInstanceValid(_trackedEnemy)) {
			_trackedEnemy.HealthChanged -= OnEnemyHealthChanged;
			_trackedEnemy.EnemyDied -= OnEnemyDied;
			_trackedEnemy.TreeExiting -= OnEnemyTreeExiting;
		}
		_trackedEnemy = null;
	}

	private void OnEnemyHealthChanged(int currentHealth, int maxHealth) {
		if (_enemyHealthBar != null) {
			_enemyHealthBar.MaxValue = maxHealth;
			_enemyHealthBar.Value = currentHealth;
		}

		if (_enemyHealthLabel != null) {
			_enemyHealthLabel.Text = $"{currentHealth} / {maxHealth}";
		}
	}

	private void OnEnemyDied() {
		if (_enemyHealthContainer != null) {
			_enemyHealthContainer.Visible = false;
		}
		UntrackEnemy();
	}

	private void OnEnemyTreeExiting() {
		if (_enemyHealthContainer != null) {
			_enemyHealthContainer.Visible = false;
		}
		UntrackEnemy();
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
