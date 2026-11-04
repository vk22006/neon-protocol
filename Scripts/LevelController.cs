using System.Collections.Generic;
using Godot;

public partial class LevelController : Node {
	[Signal]
	public delegate void EnemyRegisteredEventHandler(EnemyController enemy);

	[Signal]
	public delegate void EnemyDefeatedEventHandler(EnemyController enemy, int remainingEnemies, int totalEnemies);

	[Signal]
	public delegate void EnemyCountChangedEventHandler(int remainingEnemies, int totalEnemies);

	[Signal]
	public delegate void AllEnemiesDefeatedEventHandler();

	[Signal]
	public delegate void LevelCompletedEventHandler();

	[Export]
	public NodePath DoorPath { get; set; }

	[Export]
	public NodePath ActorsPath { get; set; } = "../../Actors";

	[Export]
	public NodePath EnemyPath { get; set; }

	[Export]
	public PackedScene CompletionRewardScene { get; set; }

	[Export]
	public NodePath RewardSpawnPointPath { get; set; }

	private DoorController _door;
	private readonly HashSet<EnemyController> _registeredEnemies = new();
	private readonly HashSet<EnemyController> _livingEnemies = new();
	private readonly HashSet<EnemyController> _defeatedEnemies = new();

	private bool _doorTriggered = false;
	private bool _levelCompleted = false;

	public int TotalEnemies => _registeredEnemies.Count;
	public int RemainingEnemies => _livingEnemies.Count;
	public int DefeatedEnemies => _defeatedEnemies.Count;
	public bool AreAllEnemiesDefeated => _registeredEnemies.Count > 0 && _livingEnemies.Count == 0;
	public bool IsLevelCompleted => _levelCompleted;

	public IReadOnlyCollection<EnemyController> LivingEnemies => _livingEnemies;
	public IReadOnlyCollection<EnemyController> RegisteredEnemies => _registeredEnemies;

	public override void _EnterTree() {
		if (!IsInGroup("level_controller")) {
			AddToGroup("level_controller");
		}
	}

	public override void _Ready() {
		ResolveReferences();
		DiscoverEnemies();

		// Deferred second pass to ensure dynamically or later-initialized enemies are registered
		Callable.From(DiscoverEnemies).CallDeferred();
	}

	private void ResolveReferences() {
		if (DoorPath != null && !DoorPath.IsEmpty) {
			_door = GetNodeOrNull<DoorController>(DoorPath);
		}

		if (_door == null) {
			_door = GetTree().GetFirstNodeInGroup("door") as DoorController;
		}
	}

	public void DiscoverEnemies() {
		// 1. Discover via dedicated "enemies" group
		var enemiesGroup = GetTree().GetNodesInGroup("enemies");
		foreach (Node node in enemiesGroup) {
			if (node is EnemyController enemy) {
				RegisterEnemy(enemy);
			}
		}

		// 2. Discover via singular "enemy" group
		var singleEnemyGroup = GetTree().GetNodesInGroup("enemy");
		foreach (Node node in singleEnemyGroup) {
			if (node is EnemyController enemy) {
				RegisterEnemy(enemy);
			}
		}

		// 3. Discover under ActorsPath if provided
		if (ActorsPath != null && !ActorsPath.IsEmpty) {
			Node actorsNode = GetNodeOrNull<Node>(ActorsPath);
			if (actorsNode != null) {
				foreach (Node child in actorsNode.GetChildren()) {
					if (child is EnemyController enemy) {
						RegisterEnemy(enemy);
					}
				}
			}
		}

		// 4. Legacy single EnemyPath if provided
		if (EnemyPath != null && !EnemyPath.IsEmpty) {
			EnemyController explicitEnemy = GetNodeOrNull<EnemyController>(EnemyPath);
			if (explicitEnemy != null) {
				RegisterEnemy(explicitEnemy);
			}
		}
	}

	public void RegisterEnemy(EnemyController enemy) {
		if (enemy == null || !GodotObject.IsInstanceValid(enemy)) {
			return;
		}

		// Prevent duplicate registration
		if (!_registeredEnemies.Add(enemy)) {
			return;
		}

		if (enemy.CurrentState == EnemyController.EnemyState.Dead || enemy.CurrentHealth <= 0) {
			_defeatedEnemies.Add(enemy);
		} else {
			_livingEnemies.Add(enemy);

			enemy.EnemyDefeated += () => OnEnemyDefeated(enemy);
			enemy.TreeExited += () => OnEnemyTreeExited(enemy);
		}

		EmitSignal(SignalName.EnemyRegistered, enemy);
		EmitSignal(SignalName.EnemyCountChanged, _livingEnemies.Count, _registeredEnemies.Count);

		CheckAllEnemiesDefeated();
	}

	private void OnEnemyDefeated(EnemyController enemy) {
		HandleEnemyDefeated(enemy);
	}

	private void OnEnemyTreeExited(EnemyController enemy) {
		if (_livingEnemies.Contains(enemy)) {
			HandleEnemyDefeated(enemy);
		}
	}

	private void HandleEnemyDefeated(EnemyController enemy) {
		if (enemy == null || !_registeredEnemies.Contains(enemy)) {
			return;
		}

		// Prevent duplicate enemy-death notifications
		if (!_livingEnemies.Remove(enemy)) {
			return;
		}

		_defeatedEnemies.Add(enemy);

		EmitSignal(SignalName.EnemyDefeated, enemy, _livingEnemies.Count, _registeredEnemies.Count);
		EmitSignal(SignalName.EnemyCountChanged, _livingEnemies.Count, _registeredEnemies.Count);

		CheckAllEnemiesDefeated();
	}

	private void CheckAllEnemiesDefeated() {
		if (_registeredEnemies.Count > 0 && _livingEnemies.Count == 0) {
			TriggerLevelCompletionRewards();
		}
	}

	private void TriggerLevelCompletionRewards() {
		if (_levelCompleted) {
			return;
		}

		_levelCompleted = true;

		EmitSignal(SignalName.AllEnemiesDefeated);
		EmitSignal(SignalName.LevelCompleted);

		// Spawn optional extra completion reward if configured
		if (CompletionRewardScene != null) {
			Node rewardInstance = CompletionRewardScene.Instantiate();
			if (rewardInstance is Node2D reward2D) {
				Node2D spawnPoint = RewardSpawnPointPath != null && !RewardSpawnPointPath.IsEmpty
					? GetNodeOrNull<Node2D>(RewardSpawnPointPath)
					: null;
				reward2D.GlobalPosition = spawnPoint != null
					? spawnPoint.GlobalPosition
					: (_door != null ? _door.GlobalPosition : Vector2.Zero);
			}
			Node targetParent = GetTree().CurrentScene ?? GetParent();
			targetParent?.AddChild(rewardInstance);
		}

		OpenDoor();
	}

	public void OpenDoor() {
		if (_doorTriggered) {
			return;
		}

		_doorTriggered = true;

		if (_door != null) {
			_door.Open();
		} else {
			DoorController fallbackDoor = GetTree().GetFirstNodeInGroup("door") as DoorController;
			fallbackDoor?.Open();
		}
	}
}
