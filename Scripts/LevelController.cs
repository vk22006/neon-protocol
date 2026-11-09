using System;
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

	[Signal]
	public delegate void LevelTransitionStartedEventHandler(string nextLevelPath);

	[Signal]
	public delegate void RunResetEventHandler();

	[Signal]
	public delegate void GameplayStateChangedEventHandler(bool enabled);

	[ExportGroup("Gameplay State")]
	[Export]
	public bool AutoEnableGameplay { get; set; } = true;

	[ExportGroup("Level References")]
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

	[ExportGroup("Level Progression")]
	[Export]
	public string NextLevelPath { get; set; } = "";

	[Export]
	public float RestartDelaySeconds { get; set; } = 2.5f;

	public static int PreservedCrystalCount { get; set; } = 0;

	private DoorController _door;
	private PlayerController _player;
	private bool _doorConnected = false;
	private bool _playerConnected = false;

	private readonly HashSet<EnemyController> _registeredEnemies = new();
	private readonly HashSet<EnemyController> _livingEnemies = new();
	private readonly HashSet<EnemyController> _defeatedEnemies = new();

	private bool _initialRegistrationComplete = false;
	private bool _doorTriggered = false;
	private bool _levelCompleted = false;
	private bool _isTransitioning = false;
	private bool _isRestarting = false;
	private bool _isGameplayEnabled = true;
	private bool _gameplayExplicitlyControlled = false;
	private SceneTreeTimer _restartTimer;

	public int TotalEnemies => _registeredEnemies.Count;
	public int RemainingEnemies => _livingEnemies.Count;
	public int DefeatedEnemies => _defeatedEnemies.Count;
	public bool IsInitialRegistrationComplete => _initialRegistrationComplete;
	public bool AreAllEnemiesDefeated => _initialRegistrationComplete && _registeredEnemies.Count > 0 && _livingEnemies.Count == 0;
	public bool IsLevelCompleted => _levelCompleted;
	public bool IsTransitioning => _isTransitioning;
	public bool IsRestarting => _isRestarting;
	public bool IsGameplayEnabled => _isGameplayEnabled;

	public IReadOnlyCollection<EnemyController> LivingEnemies => _livingEnemies;
	public IReadOnlyCollection<EnemyController> RegisteredEnemies => _registeredEnemies;

	public override void _EnterTree() {
		if (!IsInGroup("level_controller")) {
			AddToGroup("level_controller");
		}
	}

	public override void _Ready() {
		_doorTriggered = false;
		_levelCompleted = false;
		_initialRegistrationComplete = false;
		_isTransitioning = false;
		_isRestarting = false;
		_doorConnected = false;
		_playerConnected = false;
		_gameplayExplicitlyControlled = false;
		_isGameplayEnabled = AutoEnableGameplay;

		ResolveReferences();

		// Always enforce that the door begins in its closed state
		_door?.Close();

		// Initial discovery pass
		DiscoverEnemies();

		// Finalize registration deferred once all sibling/child nodes in the scene tree are ready
		Callable.From(FinalizeInitialEnemyRegistration).CallDeferred();
	}

	public override void _UnhandledInput(InputEvent @event) {
		if (!_isRestarting || SceneTransition.IsTransitioning) {
			return;
		}

		// Allow player to restart immediately upon pressing Space/Enter or mouse click during game over
		if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo) {
			if (keyEvent.Keycode == Key.Space || keyEvent.Keycode == Key.Enter) {
				RestartRun();
			}
		} else if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed) {
			RestartRun();
		}
	}

	private void ResolveReferences() {
		if (DoorPath != null && !DoorPath.IsEmpty) {
			_door = GetNodeOrNull<DoorController>(DoorPath);
		}

		if (_door == null) {
			_door = GetTree().GetFirstNodeInGroup("door") as DoorController;
		}

		if (_door != null && !_doorConnected) {
			_door.PlayerEnteredDoor += OnPlayerEnteredDoor;
			_doorConnected = true;
		}

		ResolvePlayer();
	}

	private void ResolvePlayer() {
		if (_playerConnected && _player != null && GodotObject.IsInstanceValid(_player)) {
			return;
		}

		_player = GetTree().GetFirstNodeInGroup("player") as PlayerController
			?? GetTree().GetFirstNodeInGroup("Player") as PlayerController;

		if (_player != null && !_playerConnected) {
			_player.PlayerDied += OnPlayerDied;
			_playerConnected = true;
		}
	}

	public void DiscoverEnemies() {
		// 1. Discover via dedicated groups: "enemies", "enemy", and "boss"
		string[] groups = { "enemies", "enemy", "boss" };
		foreach (string group in groups) {
			var nodes = GetTree().GetNodesInGroup(group);
			foreach (Node node in nodes) {
				if (node is EnemyController enemy) {
					RegisterEnemy(enemy);
				}
			}
		}

		// 2. Discover under ActorsPath if provided
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

		// 3. Legacy single EnemyPath if provided
		if (EnemyPath != null && !EnemyPath.IsEmpty) {
			EnemyController explicitEnemy = GetNodeOrNull<EnemyController>(EnemyPath);
			if (explicitEnemy != null) {
				RegisterEnemy(explicitEnemy);
			}
		}
	}

	public void SetGameplayEnabled(bool enabled) {
		_isGameplayEnabled = enabled;
		_gameplayExplicitlyControlled = true;

		ResolvePlayer();
		if (_player != null && GodotObject.IsInstanceValid(_player)) {
			_player.SetInputEnabled(enabled);
		}

		foreach (EnemyController enemy in _livingEnemies) {
			if (enemy != null && GodotObject.IsInstanceValid(enemy)) {
				enemy.SetAIEnabled(enabled);
			}
		}

		EmitSignal(SignalName.GameplayStateChanged, enabled);
		GD.Print($"[LevelController] SetGameplayEnabled({enabled}). Player input: {enabled}, Living enemies updated: {_livingEnemies.Count}");
	}

	public void RegisterEnemy(EnemyController enemy) {
		if (enemy == null || !GodotObject.IsInstanceValid(enemy)) {
			return;
		}

		// Prevent duplicate registration
		if (!_registeredEnemies.Add(enemy)) {
			return;
		}

		if (enemy.IsDead || enemy.CurrentHealth <= 0) {
			_defeatedEnemies.Add(enemy);
		} else {
			_livingEnemies.Add(enemy);

			enemy.EnemyDefeated += () => HandleEnemyDefeated(enemy);
			enemy.EnemyDied += () => HandleEnemyDefeated(enemy);
			enemy.TreeExited += () => OnEnemyTreeExited(enemy);

			// Inherit the current gameplay state
			enemy.SetAIEnabled(_isGameplayEnabled);
		}

		GD.Print($"[LevelController] Registered enemy '{enemy.Name}' (Type: {enemy.GetType().Name}, Health: {enemy.CurrentHealth}, Dead: {enemy.IsDead}, AIEnabled: {_isGameplayEnabled}). Living count: {_livingEnemies.Count}");

		EmitSignal(SignalName.EnemyRegistered, enemy);
		EmitSignal(SignalName.EnemyCountChanged, _livingEnemies.Count, _registeredEnemies.Count);

		// If initial registration has completed (e.g. dynamically spawned enemy), evaluate conditions
		if (_initialRegistrationComplete) {
			CheckAllEnemiesDefeated();
		}
	}

	private void FinalizeInitialEnemyRegistration() {
		// Run discovery again to catch any late-initializing enemies
		DiscoverEnemies();
		ResolveReferences();

		_initialRegistrationComplete = true;

		GD.Print($"[LevelController] Initial enemy registration finalized: {_livingEnemies.Count} living, {_registeredEnemies.Count} total registered.");

		// Apply initial gameplay state if not explicitly controlled by a cutscene controller
		if (!_gameplayExplicitlyControlled && AutoEnableGameplay) {
			SetGameplayEnabled(true);
		}

		// Verify door is closed
		if (_door != null && !_doorTriggered) {
			_door.Close();
		}

		// Only now evaluate if all enemies are defeated (never during partial setup)
		CheckAllEnemiesDefeated();
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

		// Prevent duplicate enemy-death notifications from decrementing the count multiple times
		if (!_livingEnemies.Remove(enemy)) {
			return;
		}

		_defeatedEnemies.Add(enemy);

		GD.Print($"[LevelController] Enemy defeated: '{enemy.Name}'. Remaining living enemies: {_livingEnemies.Count}/{_registeredEnemies.Count}");

		EmitSignal(SignalName.EnemyDefeated, enemy, _livingEnemies.Count, _registeredEnemies.Count);
		EmitSignal(SignalName.EnemyCountChanged, _livingEnemies.Count, _registeredEnemies.Count);

		CheckAllEnemiesDefeated();
	}

	private void CheckAllEnemiesDefeated() {
		if (!_initialRegistrationComplete) {
			return;
		}

		if (_registeredEnemies.Count > 0 && _livingEnemies.Count == 0) {
			TriggerLevelCompletionRewards();
		}
	}

	private void TriggerLevelCompletionRewards() {
		if (_levelCompleted) {
			return;
		}

		_levelCompleted = true;

		GD.Print("[LevelController] All enemies defeated! Opening level exit door.");

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

	private void OnPlayerEnteredDoor(PlayerController player) {
		TransitionToNextLevel(player);
	}

	public void TransitionToNextLevel(PlayerController player = null) {
		if (_isTransitioning) {
			return;
		}

		if (!_levelCompleted) {
			GD.Print("[LevelController] Cannot transition: Level is not completed yet.");
			return;
		}

		if (_door != null && !_door.IsOpen) {
			GD.Print("[LevelController] Cannot transition: Door is not open.");
			return;
		}

		_isTransitioning = true;
		SetGameplayEnabled(false);

		if (player != null && GodotObject.IsInstanceValid(player)) {
			PreservedCrystalCount = player.CrystalCount;
		} else if (_player != null && GodotObject.IsInstanceValid(_player)) {
			PreservedCrystalCount = _player.CrystalCount;
		}

		string nextScene = GetNextLevelScenePath();
		GD.Print($"[LevelController] Transitioning to next level: '{nextScene}'. Preserved crystals: {PreservedCrystalCount}");

		EmitSignal(SignalName.LevelTransitionStarted, nextScene);

		// If transitioning to Level 5, Level 5 will call FadeFromBlack when initialized and ready for boss intro
		bool autoFadeIn = !nextScene.Contains("Level_5");
		SceneTransition.ChangeScene(GetTree(), nextScene, autoFadeIn);
	}

	public string GetNextLevelScenePath() {
		if (!string.IsNullOrWhiteSpace(NextLevelPath)) {
			return NextLevelPath;
		}

		string currentScenePath = GetTree().CurrentScene?.SceneFilePath ?? "";
		string currentSceneName = GetTree().CurrentScene?.Name ?? "";

		if (currentScenePath.Contains("Level_1") || currentSceneName.Contains("Level_1") || currentSceneName == "Level1") {
			return "res://Scenes/Levels/Level_2.tscn";
		}
		if (currentScenePath.Contains("Level_2") || currentSceneName.Contains("Level_2") || currentSceneName == "Level2") {
			return "res://Scenes/Levels/Level_3.tscn";
		}
		if (currentScenePath.Contains("Level_3") || currentSceneName.Contains("Level_3") || currentSceneName == "Level3") {
			return "res://Scenes/Levels/Level_4.tscn";
		}
		if (currentScenePath.Contains("Level_4") || currentSceneName.Contains("Level_4") || currentSceneName == "Level4") {
			return "res://Scenes/Levels/Level_5.tscn";
		}
		if (currentScenePath.Contains("Level_5") || currentSceneName.Contains("Level_5") || currentSceneName == "Level5") {
			return "res://Scenes/Ending.tscn";
		}

		return "res://Scenes/Levels/Level_1.tscn";
	}

	private void OnPlayerDied() {
		if (_isRestarting) {
			return;
		}

		_isRestarting = true;
		PreservedCrystalCount = 0;

		GD.Print($"[LevelController] Player died. Crystal count reset to 0. Restarting Level 1 in {RestartDelaySeconds}s...");
		EmitSignal(SignalName.RunReset);

		_restartTimer = GetTree().CreateTimer(RestartDelaySeconds);
		_restartTimer.Timeout += OnRestartTimerTimeout;
	}

	private void OnRestartTimerTimeout() {
		if (_isRestarting) {
			RestartRun();
		}
	}

	public void RestartRun() {
		_isRestarting = false;
		_isTransitioning = false;
		PreservedCrystalCount = 0;

		GD.Print("[LevelController] Executing full run reset to Level 1 with fade transition.");
		SceneTransition.ChangeScene(GetTree(), "res://Scenes/Levels/Level_1.tscn");
	}
}
