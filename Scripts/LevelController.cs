using Godot;

public partial class LevelController : Node {
	[Export]
	public NodePath DoorPath { get; set; }

	[Export]
	public NodePath EnemyPath { get; set; }

	private DoorController _door;
	private EnemyController _enemy;
	private bool _doorTriggered = false;

	public override void _Ready() {
		ResolveReferences();
		SubscribeEvents();
	}

	private void ResolveReferences() {
		if (DoorPath != null && !DoorPath.IsEmpty) {
			_door = GetNodeOrNull<DoorController>(DoorPath);
		}

		if (_door == null) {
			_door = GetTree().GetFirstNodeInGroup("door") as DoorController;
		}

		if (EnemyPath != null && !EnemyPath.IsEmpty) {
			_enemy = GetNodeOrNull<EnemyController>(EnemyPath);
		}

		if (_enemy == null) {
			_enemy = GetTree().GetFirstNodeInGroup("enemy") as EnemyController
				?? GetTree().GetFirstNodeInGroup("Enemy") as EnemyController;
		}
	}

	private void SubscribeEvents() {
		if (_enemy != null) {
			_enemy.EnemyDefeated += OnEnemyDefeated;
			_enemy.TreeExited += OnEnemyTreeExited;

			if (_enemy.CurrentState == EnemyController.EnemyState.Dead) {
				OpenDoor();
			}
		}
	}

	public override void _ExitTree() {
		if (_enemy != null) {
			_enemy.EnemyDefeated -= OnEnemyDefeated;
			_enemy.TreeExited -= OnEnemyTreeExited;
		}
	}

	private void OnEnemyDefeated() {
		OpenDoor();
	}

	private void OnEnemyTreeExited() {
		if (!_doorTriggered && _enemy != null && _enemy.CurrentState == EnemyController.EnemyState.Dead) {
			OpenDoor();
		}
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

