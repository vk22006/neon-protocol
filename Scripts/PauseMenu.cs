using System;
using Godot;

public partial class PauseMenu : CanvasLayer {
	[Signal]
	public delegate void GamePausedEventHandler();

	[Signal]
	public delegate void GameResumedEventHandler();

	public static PauseMenu Instance { get; private set; }

	[Export]
	public NodePath RootPath { get; set; } = "Root";

	[Export]
	public NodePath ResumeButtonPath { get; set; } = "Root/CenterContainer/PausePanel/VBoxContainer/ResumeButton";

	[Export]
	public NodePath SettingsButtonPath { get; set; } = "Root/CenterContainer/PausePanel/VBoxContainer/SettingsButton";

	[Export]
	public NodePath BackToMenuButtonPath { get; set; } = "Root/CenterContainer/PausePanel/VBoxContainer/BackToMenuButton";

	[Export]
	public NodePath ExitGameButtonPath { get; set; } = "Root/CenterContainer/PausePanel/VBoxContainer/ExitGameButton";

	private Control _root;
	private Button _resumeButton;
	private Button _settingsButton;
	private Button _backToMenuButton;
	private Button _exitGameButton;
	private Control _settingsPanelInstance;
	private bool _isPaused = false;
	private bool _isTransitioningToMenu = false;

	public bool IsPaused => _isPaused;

	public override void _EnterTree() {
		ProcessMode = ProcessModeEnum.Always;
		if (!IsInGroup("pause_menu")) {
			AddToGroup("pause_menu");
		}
		Instance = this;
	}

	public override void _ExitTree() {
		if (Instance == this) {
			Instance = null;
		}
	}

	public override void _Ready() {
		ResolveReferences();
		
		Node audioManager = GetNodeOrNull<Node>("/root/AudioManager");

		if (audioManager != null) {
			audioManager.Call("register_ui_buttons", this);
		} else {
			GD.PushWarning("PauseMenu could not find AudioManager autoload!");
		}

		if (_root != null) {
			_root.Visible = false;
		}
		_isPaused = false;
		_isTransitioningToMenu = false;

		if (_resumeButton != null) {
			_resumeButton.Pressed += OnResumeButtonPressed;
		}

		if (_settingsButton != null) {
			_settingsButton.Pressed += OnSettingsButtonPressed;
		}

		if (_backToMenuButton != null) {
			_backToMenuButton.Pressed += OnBackToMenuButtonPressed;
		}

		if (_exitGameButton != null) {
			_exitGameButton.Pressed += OnExitGameButtonPressed;
		}
	}

	private void ResolveReferences() {
		_root = GetNodeOrNull<Control>(RootPath) ?? GetNodeOrNull<Control>("Root");

		_resumeButton = GetNodeOrNull<Button>(ResumeButtonPath)
			?? FindChild("ResumeButton", true, false) as Button;

		_settingsButton = GetNodeOrNull<Button>(SettingsButtonPath)
			?? FindChild("SettingsButton", true, false) as Button;

		_backToMenuButton = GetNodeOrNull<Button>(BackToMenuButtonPath)
			?? FindChild("BackToMenuButton", true, false) as Button
			?? FindChild("BackToHomeButton", true, false) as Button;

		_exitGameButton = GetNodeOrNull<Button>(ExitGameButtonPath)
			?? FindChild("ExitGameButton", true, false) as Button;
	}

	public override void _UnhandledInput(InputEvent @event) {
		if (_isTransitioningToMenu) {
			return;
		}

		if (_settingsPanelInstance != null && GodotObject.IsInstanceValid(_settingsPanelInstance)) {
			return;
		}

		bool isPauseAction = @event.IsActionPressed("pause");
		bool isPauseKey = @event is InputEventKey keyEvent
			&& keyEvent.Pressed
			&& !keyEvent.Echo
			&& (keyEvent.Keycode == Key.Escape || keyEvent.Keycode == Key.P);

		if (isPauseAction || isPauseKey) {
			if (_isPaused) {
				GetViewport().SetInputAsHandled();
				Resume();
			} else if (CanPauseGame(GetTree())) {
				GetViewport().SetInputAsHandled();
				Pause();
			}
		}
	}

	public void Pause() {
		if (_isPaused || _isTransitioningToMenu) {
			return;
		}

		if (!CanPauseGame(GetTree())) {
			GD.Print("[PauseMenu] Pause request rejected because gameplay conditions do not permit pausing.");
			return;
		}

		_isPaused = true;
		if (_root != null) {
			_root.Visible = true;
		}

		GetTree().Paused = true;

		if (_resumeButton != null && _resumeButton.IsInsideTree()) {
			_resumeButton.CallDeferred(Control.MethodName.GrabFocus);
		}

		EmitSignal(SignalName.GamePaused);
		GD.Print("[PauseMenu] Game paused successfully.");
	}

	public void Resume() {
		if (!_isPaused || _isTransitioningToMenu) {
			return;
		}

		if (_settingsPanelInstance != null && GodotObject.IsInstanceValid(_settingsPanelInstance)) {
			_settingsPanelInstance.QueueFree();
			_settingsPanelInstance = null;
		}

		_isPaused = false;
		if (_root != null) {
			_root.Visible = false;
		}

		GetTree().Paused = false;

		EmitSignal(SignalName.GameResumed);
		GD.Print("[PauseMenu] Game resumed successfully.");
	}

	public void TogglePause() {
		if (_isPaused) {
			Resume();
		} else {
			Pause();
		}
	}

	private void OnResumeButtonPressed() {
		Resume();
	}

	private void OnSettingsButtonPressed() {
		if (_settingsPanelInstance != null && GodotObject.IsInstanceValid(_settingsPanelInstance)) {
			return;
		}

		GD.Print("[PauseMenu] Opening settings panel from pause menu.");
		var settingsScene = GD.Load<PackedScene>("res://Scenes/UI/SettingsPanel.tscn");
		if (settingsScene == null) {
			GD.PushError("[PauseMenu] Failed to load SettingsPanel.tscn");
			return;
		}

		_settingsPanelInstance = settingsScene.Instantiate<Control>();
		if (_settingsPanelInstance == null) {
			GD.PushError("[PauseMenu] Failed to instantiate SettingsPanel.");
			return;
		}

		// Hide pause menu buttons so they are not interactive while settings is open
		if (_root != null) {
			_root.Visible = false;
		}

		AddChild(_settingsPanelInstance);

		var audioManager = GetNodeOrNull<Node>("/root/AudioManager");
		if (audioManager != null && audioManager.HasMethod("register_ui_buttons")) {
			audioManager.Call("register_ui_buttons", _settingsPanelInstance);
		}

		_settingsPanelInstance.Connect("closed", Callable.From(() => {
			if (_root != null) {
				_root.Visible = true;
			}
			if (_settingsButton != null && _settingsButton.IsInsideTree()) {
				_settingsButton.CallDeferred(Control.MethodName.GrabFocus);
			}
			_settingsPanelInstance = null;
		}));
	}

	private void OnBackToMenuButtonPressed() {
		if (_isTransitioningToMenu) {
			return;
		}
		_isTransitioningToMenu = true;

		GD.Print("[PauseMenu] Back to Menu selected. Safely leaving current gameplay session.");

		// 1. Immediately disable player input and enemy AI so gameplay cannot execute for even a single frame
		var levelController = GetTree().GetFirstNodeInGroup("level_controller") as LevelController;
		if (levelController != null && GodotObject.IsInstanceValid(levelController)) {
			levelController.SetGameplayEnabled(false);
		}

		var player = GetTree().GetFirstNodeInGroup("player") as PlayerController
			?? GetTree().GetFirstNodeInGroup("Player") as PlayerController;
		if (player != null && GodotObject.IsInstanceValid(player)) {
			player.SetInputEnabled(false);
		}

		// 2. Hide the pause menu overlay
		if (_root != null) {
			_root.Visible = false;
		}
		_isPaused = false;

		// 3. Restore the SceneTree from its paused state before scene transition
		GetTree().Paused = false;

		// 4. Smoothly transition to the Main Menu using the existing SceneTransition autoload
		const string MainMenuScenePath = "res://Scenes/UI/main_menu.tscn";
		SceneTransition.ChangeScene(GetTree(), MainMenuScenePath, true);
	}

	private void OnExitGameButtonPressed() {
		GD.Print("[PauseMenu] Exit Game selected. Quitting application.");
		GetTree().Quit();
	}

	public static bool CanPauseGame(SceneTree tree, Control gameOverContainer = null) {
		if (tree == null) {
			return false;
		}

		// 1. Scene transition active
		if (SceneTransition.IsTransitioning) {
			return false;
		}

		// 2. LevelController state checks
		var levelController = tree.GetFirstNodeInGroup("level_controller") as LevelController;
		if (levelController != null) {
			if (!levelController.IsGameplayEnabled) {
				return false;
			}
			if (levelController.IsTransitioning) {
				return false;
			}
			if (levelController.IsRestarting) {
				return false;
			}
		}

		// 3. Player state checks
		var player = tree.GetFirstNodeInGroup("player") as PlayerController
			?? tree.GetFirstNodeInGroup("Player") as PlayerController;
		if (player != null) {
			if (player.IsDead || !player.InputEnabled) {
				return false;
			}
		}

		// 4. Game-over container check
		if (gameOverContainer != null && gameOverContainer.Visible) {
			return false;
		}

		// 5. Dialogue box active check
		var dialogueBox = tree.Root.FindChild("DialogueBox", true, false) as CanvasItem;
		if (dialogueBox != null && dialogueBox.Visible) {
			return false;
		}

		return true;
	}

	public static PauseMenu GetOrCreate(SceneTree tree) {
		if (Instance != null && GodotObject.IsInstanceValid(Instance)) {
			return Instance;
		}

		var existing = tree.GetFirstNodeInGroup("pause_menu") as PauseMenu;
		if (existing != null && GodotObject.IsInstanceValid(existing)) {
			Instance = existing;
			return existing;
		}

		var pauseScene = GD.Load<PackedScene>("res://Scenes/UI/PauseMenu.tscn");
		if (pauseScene == null) {
			GD.PushError("[PauseMenu] Could not load res://Scenes/UI/PauseMenu.tscn");
			return null;
		}

		var pauseMenu = pauseScene.Instantiate<PauseMenu>();
		tree.CurrentScene?.AddChild(pauseMenu);
		Instance = pauseMenu;
		return pauseMenu;
	}

	public static void OpenPauseMenu(SceneTree tree) {
		var menu = GetOrCreate(tree);
		menu?.Pause();
	}

	public static void ResumeGame(SceneTree tree) {
		var menu = Instance ?? tree?.GetFirstNodeInGroup("pause_menu") as PauseMenu;
		menu?.Resume();
	}

	public static void TogglePauseGame(SceneTree tree) {
		var menu = GetOrCreate(tree);
		menu?.TogglePause();
	}
}
