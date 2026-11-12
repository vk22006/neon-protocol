using Godot;

public partial class DoorController : Node2D {
	[Signal]
	public delegate void DoorOpenedEventHandler();

	[Signal]
	public delegate void PlayerEnteredDoorEventHandler(PlayerController player);

	[ExportGroup("Door Audio")]
	[Export]
	public AudioStream OpenSound { get; set; }

	[Export]
	public NodePath DoorVisualsPath { get; set; } = "DoorVisuals";

	[Export]
	public NodePath CollisionShapePath { get; set; } = "DoorCollision/CollisionShape2D";

	[Export]
	public NodePath ExitAreaPath { get; set; } = "ExitArea";

	private AnimatedSprite2D _doorVisuals;
	private CollisionShape2D _collisionShape;
	private Area2D _exitArea;
	private AudioStreamPlayer2D _doorAudioPlayer;
	private bool _isOpen = false;
	private bool _isFullyOpen = false;
	private bool _hasTriggeredExit = false;

	public bool IsOpen => _isOpen;
	public bool IsFullyOpen => _isFullyOpen;
	public bool HasTriggeredExit => _hasTriggeredExit;

	public override void _Ready() {
		if (!IsInGroup("door")) {
			AddToGroup("door");
		}

		_doorVisuals = GetNodeOrNull<AnimatedSprite2D>(DoorVisualsPath);
		_collisionShape = GetNodeOrNull<CollisionShape2D>(CollisionShapePath);

		_doorAudioPlayer = GetNodeOrNull<AudioStreamPlayer2D>("DoorAudioPlayer");
		if (_doorAudioPlayer == null) {
			_doorAudioPlayer = new AudioStreamPlayer2D {
				Name = "DoorAudioPlayer",
				Bus = "SFX"
			};
			AddChild(_doorAudioPlayer);
		}

		if (_doorAudioPlayer.Stream == null) {
			_doorAudioPlayer.Stream = OpenSound ?? GD.Load<AudioStream>("res://Assets/SFX/Environment/doorOpen_002.ogg");
		}
		_doorAudioPlayer.Bus = "SFX";

		// Always ensure the door starts strictly in its closed state
		_isOpen = false;
		_isFullyOpen = false;
		_hasTriggeredExit = false;

		if (_doorVisuals != null) {
			if (_doorVisuals.SpriteFrames != null && _doorVisuals.SpriteFrames.HasAnimation("success")) {
				_doorVisuals.SpriteFrames.SetAnimationLoopMode("success", SpriteFrames.LoopMode.None);
			}
			_doorVisuals.AnimationFinished += OnAnimationFinished;
			_doorVisuals.Stop();
			_doorVisuals.Animation = "default";
			_doorVisuals.Frame = 0;
			_doorVisuals.Play("default");
		}

		if (_collisionShape != null) {
			_collisionShape.SetDeferred(CollisionShape2D.PropertyName.Disabled, false);
		}

		SetupExitArea();
	}

	private void SetupExitArea() {
		_exitArea = GetNodeOrNull<Area2D>(ExitAreaPath);
		if (_exitArea == null) {
			_exitArea = new Area2D {
				Name = "ExitArea",
				CollisionLayer = 0,
				CollisionMask = 2 // Layer 2 is Player
			};
			CollisionShape2D col = new CollisionShape2D();
			RectangleShape2D rect = new RectangleShape2D {
				Size = new Vector2(16, 32)
			};
			col.Shape = rect;
			_exitArea.AddChild(col);
			AddChild(_exitArea);
		}

		// Ensure exit area detection is strictly disabled initially
		_exitArea.SetDeferred(Area2D.PropertyName.Monitoring, false);
		_exitArea.BodyEntered += OnExitAreaBodyEntered;
	}

	private void OnExitAreaBodyEntered(Node2D body) {
		TryTriggerPlayerExit(body);
	}

	private void TryTriggerPlayerExit(Node2D body) {
		if (_hasTriggeredExit || !_isOpen || !_isFullyOpen) {
			return;
		}

		if (body is PlayerController player && !player.IsDead && (player.IsInGroup("player") || player.IsInGroup("Player"))) {
			_hasTriggeredExit = true;
			GD.Print("[DoorController] Player entered fully opened door. Emitting PlayerEnteredDoor signal.");
			EmitSignal(SignalName.PlayerEnteredDoor, player);
		}
	}

	public void Close() {
		_isOpen = false;
		_isFullyOpen = false;
		_hasTriggeredExit = false;

		if (_doorVisuals != null) {
			_doorVisuals.Stop();
			_doorVisuals.Animation = "default";
			_doorVisuals.Frame = 0;
			_doorVisuals.Play("default");
		}

		if (_collisionShape != null) {
			_collisionShape.SetDeferred(CollisionShape2D.PropertyName.Disabled, false);
		}

		if (_exitArea != null) {
			_exitArea.SetDeferred(Area2D.PropertyName.Monitoring, false);
		}
	}

	public void Open() {
		if (_isOpen) {
			return;
		}

		_isOpen = true;
		_isFullyOpen = false;

		if (_doorAudioPlayer != null && _doorAudioPlayer.Stream != null) {
			_doorAudioPlayer.Play();
		}

		// Exit area monitoring remains inactive while the door opening animation plays
		if (_exitArea != null) {
			_exitArea.SetDeferred(Area2D.PropertyName.Monitoring, false);
		}

		if (_doorVisuals != null && _doorVisuals.SpriteFrames != null && _doorVisuals.SpriteFrames.HasAnimation("success")) {
			_doorVisuals.Play("success");
		} else {
			OnDoorFullyOpened();
		}
	}

	private void OnAnimationFinished() {
		if (_doorVisuals != null && _doorVisuals.Animation == "success") {
			OnDoorFullyOpened();
		}
	}

	private void OnDoorFullyOpened() {
		if (_isFullyOpen) {
			return;
		}

		_isFullyOpen = true;

		if (_collisionShape != null) {
			_collisionShape.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
		}

		if (_exitArea != null) {
			_exitArea.SetDeferred(Area2D.PropertyName.Monitoring, true);
		}

		EmitSignal(SignalName.DoorOpened);
		GD.Print("[DoorController] Door is now fully open. Activated exit area monitoring.");

		Callable.From(CheckForOverlappingPlayer).CallDeferred();
	}

	private void CheckForOverlappingPlayer() {
		if (_hasTriggeredExit || !_isOpen || !_isFullyOpen || _exitArea == null || !_exitArea.Monitoring) {
			return;
		}

		var bodies = _exitArea.GetOverlappingBodies();
		foreach (Node2D body in bodies) {
			TryTriggerPlayerExit(body);
			if (_hasTriggeredExit) {
				break;
			}
		}
	}
}
