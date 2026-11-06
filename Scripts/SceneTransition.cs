using System;
using System.Threading.Tasks;
using Godot;

public partial class SceneTransition : CanvasLayer {
	[Signal]
	public delegate void TransitionStartedEventHandler(string targetScene);

	[Signal]
	public delegate void FadeOutCompletedEventHandler();

	[Signal]
	public delegate void NewSceneLoadedEventHandler(string targetScene);

	[Signal]
	public delegate void TransitionFinishedEventHandler();

	[Export]
	public float FadeDuration { get; set; } = 0.4f;

	[Export]
	public NodePath ColorRectPath { get; set; } = "ColorRect";

	[Export]
	public NodePath AnimationPlayerPath { get; set; } = "AnimationPlayer";

	public static SceneTransition Instance { get; private set; }
	public static bool IsTransitioning => Instance != null && Instance._isTransitioning;

	private bool _isTransitioning = false;
	private ColorRect _colorRect;
	private AnimationPlayer _animPlayer;

	public override void _EnterTree() {
		if (Instance == null) {
			Instance = this;
		} else if (Instance != this) {
			GD.Print("[SceneTransition] Duplicate instance detected. Freeing duplicate node.");
			QueueFree();
			return;
		}
	}

	public override void _ExitTree() {
		if (Instance == this) {
			Instance = null;
		}
	}

	public override void _Ready() {
		ProcessMode = ProcessModeEnum.Always;
		Layer = 128;

		_colorRect = GetNodeOrNull<ColorRect>(ColorRectPath);
		_animPlayer = GetNodeOrNull<AnimationPlayer>(AnimationPlayerPath);

		if (_colorRect != null) {
			_colorRect.Visible = false;
			_colorRect.Color = new Color(0, 0, 0, 0);
			_colorRect.MouseFilter = Control.MouseFilterEnum.Ignore;
		}

		EnsureAnimationsExist();
	}

	public override void _Input(InputEvent @event) {
		if (_isTransitioning) {
			GetViewport()?.SetInputAsHandled();
		}
	}

	public override void _UnhandledInput(InputEvent @event) {
		if (_isTransitioning) {
			GetViewport()?.SetInputAsHandled();
		}
	}

	public static void ChangeScene(SceneTree tree, string targetScenePath) {
		if (tree == null) {
			GD.PushError("[SceneTransition] SceneTree is null!");
			return;
		}

		if (Instance == null || !GodotObject.IsInstanceValid(Instance)) {
			// Look for autoload instance under root
			Instance = tree.Root.GetNodeOrNull<SceneTransition>("SceneTransition");

			// If still null, instantiate dynamically and add to root
			if (Instance == null || !GodotObject.IsInstanceValid(Instance)) {
				PackedScene scene = GD.Load<PackedScene>("res://Scenes/Cutscenes/SceneTransition.tscn");
				if (scene != null) {
					var transitionNode = scene.Instantiate<SceneTransition>();
					tree.Root.AddChild(transitionNode);
					Instance = transitionNode;
				}
			}
		}

		if (Instance != null && GodotObject.IsInstanceValid(Instance)) {
			Instance.TransitionToScene(targetScenePath);
		} else {
			GD.PushWarning("[SceneTransition] Falling back to direct ChangeSceneToFile.");
			tree.ChangeSceneToFile(targetScenePath);
		}
	}

	public void TransitionToScene(string targetScenePath) {
		if (_isTransitioning) {
			GD.Print($"[SceneTransition] Transition to '{targetScenePath}' ignored: transition already in progress.");
			return;
		}

		if (string.IsNullOrWhiteSpace(targetScenePath)) {
			GD.PushWarning("[SceneTransition] Target scene path is null or empty!");
			return;
		}

		_isTransitioning = true;
		Callable.From(() => ExecuteTransition(targetScenePath)).CallDeferred();
	}

	private async void ExecuteTransition(string targetScenePath) {
		GD.Print($"[SceneTransition] Starting transition to '{targetScenePath}'.");
		EmitSignal(SignalName.TransitionStarted, targetScenePath);

		// 1. Begin with screen fully visible and disable player input
		DisableCurrentPlayerInput();

		if (_colorRect != null) {
			_colorRect.Visible = true;
			_colorRect.MouseFilter = Control.MouseFilterEnum.Stop;
		}

		// 2. Fade the screen to black
		await PlayFadeToBlack();

		// Ensure overlay is pitch black before scene swap
		if (_colorRect != null) {
			_colorRect.Color = new Color(0, 0, 0, 1);
		}

		EmitSignal(SignalName.FadeOutCompleted);

		// 3. Once the fade-out animation finishes, perform the requested scene change
		Error err = GetTree().ChangeSceneToFile(targetScenePath);
		if (err != Error.Ok) {
			GD.PushError($"[SceneTransition] Failed to change scene to '{targetScenePath}': {err}");
		}

		// 4. Wait until the new scene has been loaded and initialized
		// Wait two frames so the engine finalizes current scene swap and runs _Ready()
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

		// Re-enforce input disable on newly instantiated player
		DisableCurrentPlayerInput();

		EmitSignal(SignalName.NewSceneLoaded, targetScenePath);

		// 5. Fade from black back to the gameplay scene
		await PlayFadeFromBlack();

		// 6. Restore normal player input and gameplay
		if (_colorRect != null) {
			_colorRect.Color = new Color(0, 0, 0, 0);
			_colorRect.Visible = false;
			_colorRect.MouseFilter = Control.MouseFilterEnum.Ignore;
		}

		EnableCurrentPlayerInput();

		_isTransitioning = false;
		EmitSignal(SignalName.TransitionFinished);
		GD.Print($"[SceneTransition] Transition to '{targetScenePath}' finished successfully.");
	}

	private async Task PlayFadeToBlack() {
		if (_animPlayer != null && _animPlayer.HasAnimation("fade_to_black")) {
			_animPlayer.Play("fade_to_black");
			await ToSignal(_animPlayer, AnimationMixer.SignalName.AnimationFinished);
		} else if (_colorRect != null) {
			Tween tween = CreateTween();
			tween.TweenProperty(_colorRect, "color", new Color(0, 0, 0, 1), FadeDuration);
			await ToSignal(tween, Tween.SignalName.Finished);
		}
	}

	private async Task PlayFadeFromBlack() {
		if (_animPlayer != null && _animPlayer.HasAnimation("fade_from_black")) {
			_animPlayer.Play("fade_from_black");
			await ToSignal(_animPlayer, AnimationMixer.SignalName.AnimationFinished);
		} else if (_colorRect != null) {
			Tween tween = CreateTween();
			tween.TweenProperty(_colorRect, "color", new Color(0, 0, 0, 0), FadeDuration);
			await ToSignal(tween, Tween.SignalName.Finished);
		}
	}

	private void DisableCurrentPlayerInput() {
		PlayerController player = GetTree().GetFirstNodeInGroup("player") as PlayerController
			?? GetTree().GetFirstNodeInGroup("Player") as PlayerController;

		if (player != null && GodotObject.IsInstanceValid(player)) {
			player.SetInputEnabled(false);
		}
	}

	private void EnableCurrentPlayerInput() {
		PlayerController player = GetTree().GetFirstNodeInGroup("player") as PlayerController
			?? GetTree().GetFirstNodeInGroup("Player") as PlayerController;

		if (player != null && GodotObject.IsInstanceValid(player)) {
			player.SetInputEnabled(true);
		}
	}

	private void EnsureAnimationsExist() {
		if (_animPlayer == null) {
			return;
		}

		AnimationLibrary library = _animPlayer.HasAnimationLibrary("")
			? _animPlayer.GetAnimationLibrary("")
			: new AnimationLibrary();

		if (!library.HasAnimation("RESET")) {
			Animation resetAnim = new Animation { Length = 0.001f };
			int track = resetAnim.AddTrack(Animation.TrackType.Value);
			resetAnim.TrackSetPath(track, "ColorRect:color");
			resetAnim.TrackInsertKey(track, 0.0f, new Color(0, 0, 0, 0));
			library.AddAnimation("RESET", resetAnim);
		}

		if (!library.HasAnimation("fade_to_black")) {
			Animation fadeOut = new Animation { Length = FadeDuration };
			int track = fadeOut.AddTrack(Animation.TrackType.Value);
			fadeOut.TrackSetPath(track, "ColorRect:color");
			fadeOut.TrackInsertKey(track, 0.0f, new Color(0, 0, 0, 0));
			fadeOut.TrackInsertKey(track, FadeDuration, new Color(0, 0, 0, 1));
			library.AddAnimation("fade_to_black", fadeOut);
		}

		if (!library.HasAnimation("fade_from_black")) {
			Animation fadeIn = new Animation { Length = FadeDuration };
			int track = fadeIn.AddTrack(Animation.TrackType.Value);
			fadeIn.TrackSetPath(track, "ColorRect:color");
			fadeIn.TrackInsertKey(track, 0.0f, new Color(0, 0, 0, 1));
			fadeIn.TrackInsertKey(track, FadeDuration, new Color(0, 0, 0, 0));
			library.AddAnimation("fade_from_black", fadeIn);
		}

		if (!_animPlayer.HasAnimationLibrary("")) {
			_animPlayer.AddAnimationLibrary("", library);
		}
	}
}

