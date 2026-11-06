using Godot;

public partial class EnemyController : CharacterBody2D {
	public enum EnemyState {
		Chasing,
		Attacking,
		Hurt,
		Dead
	}

	[Signal]
	public delegate void HealthChangedEventHandler(int currentHealth, int maxHealth);

	[Signal]
	public delegate void EnemyDiedEventHandler();

	[Signal]
	public delegate void EnemyDefeatedEventHandler();

	[Export]
	public PackedScene CrystalDropScene { get; set; }

	[Export]
	public float MoveSpeed { get; set; } = 60.0f;

	[ExportGroup("Combat")]
	[Export]
	public float AttackRange { get; set; } = 30.0f;

	[Export]
	public int AttackDamage { get; set; } = 10;

	[Export]
	public float AttackCooldown { get; set; } = 1.2f;

	[Export]
	public float AttackWindup { get; set; } = 0.3f;

	[ExportGroup("Health")]
	[Export]
	public virtual int MaxHealth { get; set; } = 30;

	[Export]
	public NodePath HealthBarPath { get; set; } = "HealthBarAnchor/EnemyHealthBar";

	[Export]
	public NodePath HealthBarAnchorPath { get; set; } = "HealthBarAnchor";

	protected EnemyState _currentState = EnemyState.Chasing;
	protected int _currentHealth;

	private PlayerController _player;
	private AnimatedSprite2D _sprite;
	private CollisionShape2D _collisionShape;
	private ProgressBar _healthBar;
	private CanvasItem _healthBarAnchor;

	private Vector2 _lastDirection = Vector2.Down;

	private bool _attackDamageApplied = false;
	private float _attackTimer = 0.0f;
	private float _attackCooldownTimer = 0.0f;
	private bool _hasDroppedReward = false;

	public virtual EnemyState CurrentState => _currentState;
	public virtual int CurrentHealth => _currentHealth;
	public virtual bool IsDead => _currentState == EnemyState.Dead || _currentHealth <= 0;

	public override void _Ready() {
		// Initialize health first so any registration sees the correct alive state
		_currentHealth = MaxHealth;

		if (!IsInGroup("enemy")) {
			AddToGroup("enemy");
		}
		if (!IsInGroup("enemies")) {
			AddToGroup("enemies");
		}

		_sprite = GetNodeOrNull<AnimatedSprite2D>("Visuals/AnimatedSprite2D");
		_collisionShape = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
		_healthBar = GetNodeOrNull<ProgressBar>(HealthBarPath);
		_healthBarAnchor = GetNodeOrNull<CanvasItem>(HealthBarAnchorPath);

		if (_healthBar != null) {
			_healthBar.MaxValue = MaxHealth;
			_healthBar.Value = _currentHealth;
			_healthBar.Visible = true;
		}

		if (_healthBarAnchor != null) {
			_healthBarAnchor.Visible = true;
		}

		EmitSignal(SignalName.HealthChanged, _currentHealth, MaxHealth);

		LevelController levelController = GetTree().GetFirstNodeInGroup("level_controller") as LevelController;
		levelController?.RegisterEnemy(this);

		_player = GetTree().GetFirstNodeInGroup("player") as PlayerController
			?? GetTree().GetFirstNodeInGroup("Player") as PlayerController;

		if (_player == null) {
			GD.PushWarning("Enemy could not find the Player!");
		}

		if (_sprite != null) {
			_sprite.AnimationFinished += OnAnimationFinished;

			// Ensure one-shot animations do not loop infinitely
			if (_sprite.SpriteFrames != null) {
				string[] nonLoopingAnimations = {
					"attack_down", "attack_left", "attack_right", "attack_up",
					"hurt_down", "hurt_left", "hurt_right", "hurt_up",
					"enemy_death"
				};
				foreach (string anim in nonLoopingAnimations) {
					if (_sprite.SpriteFrames.HasAnimation(anim)) {
						_sprite.SpriteFrames.SetAnimationLoopMode(anim, SpriteFrames.LoopMode.None);
					}
				}
			}
		}

		UpdateAnimation("idle", _lastDirection);
	}

	public override void _PhysicsProcess(double delta) {
		if (_currentState == EnemyState.Dead) {
			return;
		}

		if (_currentState == EnemyState.Hurt) {
			Velocity = Vector2.Zero;
			MoveAndSlide();
			return;
		}

		// Prevent attacking or chasing a dead or invalid player
		if (!GodotObject.IsInstanceValid(_player) || _player.IsDead) {
			Velocity = Vector2.Zero;
			MoveAndSlide();
			if (_currentState == EnemyState.Attacking) {
				_currentState = EnemyState.Chasing;
			}
			UpdateAnimation("idle", _lastDirection);
			return;
		}

		Vector2 toPlayer = _player.GlobalPosition - GlobalPosition;
		float distance = toPlayer.Length();

		if (distance > 0.001f) {
			_lastDirection = toPlayer.Normalized();
		}

		if (_attackCooldownTimer > 0.0f) {
			_attackCooldownTimer -= (float)delta;
		}

		// Dynamic tracking: If the player leaves attack range,
		// immediately cancel any active attack and resume chasing!
		if (distance > AttackRange) {
			_currentState = EnemyState.Chasing;
			Velocity = _lastDirection * MoveSpeed;
			MoveAndSlide();
			UpdateAnimation("move", _lastDirection);
			return;
		}

		// Player is within attack range: stop moving
		Velocity = Vector2.Zero;
		MoveAndSlide();

		if (_currentState == EnemyState.Attacking) {
			_attackTimer += (float)delta;

			if (!_attackDamageApplied && _attackTimer >= AttackWindup) {
				ApplyAttackDamage();
			}
			return;
		}

		// Within range and ready to attack if cooldown permits
		if (_attackCooldownTimer <= 0.0f) {
			StartAttack();
		} else {
			UpdateAnimation("idle", _lastDirection);
		}
	}

	private void StartAttack() {
		_currentState = EnemyState.Attacking;
		_attackDamageApplied = false;
		_attackTimer = 0.0f;
		_attackCooldownTimer = AttackCooldown;

		UpdateAnimation("attack", _lastDirection);
	}

	private void ApplyAttackDamage() {
		_attackDamageApplied = true;

		if (!GodotObject.IsInstanceValid(_player) || _player.IsDead) {
			return;
		}

		float distance = GlobalPosition.DistanceTo(_player.GlobalPosition);
		if (distance <= AttackRange) {
			_player.TakeDamage(AttackDamage);
		}
	}

	public virtual void TakeDamage(int damage) {
		if (_currentState == EnemyState.Dead || damage <= 0) {
			return;
		}

		_currentHealth -= damage;
		if (_currentHealth <= 0) {
			_currentHealth = 0;
			UpdateHealthBar();
			EmitSignal(SignalName.HealthChanged, _currentHealth, MaxHealth);
			Die();
		} else {
			UpdateHealthBar();
			EmitSignal(SignalName.HealthChanged, _currentHealth, MaxHealth);
			_currentState = EnemyState.Hurt;
			Velocity = Vector2.Zero;
			UpdateAnimation("hurt", _lastDirection);
		}
	}

	private void UpdateHealthBar() {
		if (_healthBar != null && _currentState != EnemyState.Dead) {
			_healthBar.Value = _currentHealth;
		}
	}

	private void Die() {
		if (_currentState == EnemyState.Dead) {
			return;
		}

		_currentState = EnemyState.Dead;
		_currentHealth = 0;
		Velocity = Vector2.Zero;
		EmitSignal(SignalName.EnemyDied);

		if (_healthBarAnchor != null) {
			_healthBarAnchor.Visible = false;
		} else if (_healthBar != null) {
			_healthBar.Visible = false;
		}

		if (_collisionShape != null) {
			_collisionShape.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
		}

		if (_sprite != null && _sprite.SpriteFrames != null && _sprite.SpriteFrames.HasAnimation("enemy_death")) {
			_sprite.Play("enemy_death");
		} else {
			SpawnDeathReward();
			QueueFree();
		}
	}

	private void SpawnDeathReward() {
		if (_hasDroppedReward) {
			return;
		}

		_hasDroppedReward = true;
		EmitSignal(SignalName.EnemyDefeated);

		PackedScene scene = CrystalDropScene ?? GD.Load<PackedScene>("res://Scenes/collectibles/Crystal_Artifact.tscn");
		if (scene != null) {
			Node instance = scene.Instantiate();
			if (instance is Node2D collectible) {
				collectible.GlobalPosition = GlobalPosition;
			}
			Node targetParent = GetTree().CurrentScene ?? GetParent();
			if (targetParent != null) {
				targetParent.AddChild(instance);
			}
		}
	}

	private void OnAnimationFinished() {
		if (_currentState == EnemyState.Dead) {
			if (_sprite != null && _sprite.Animation == "enemy_death") {
				SpawnDeathReward();
				QueueFree();
			}
			return;
		}

		if (_currentState == EnemyState.Hurt) {
			_currentState = EnemyState.Chasing;
			return;
		}

		if (_currentState == EnemyState.Attacking) {
			if (_sprite != null && _sprite.Animation.ToString().StartsWith("attack_")) {
				_currentState = EnemyState.Chasing;
			}
		}
	}

	private void UpdateAnimation(string state, Vector2 direction) {
		if (_sprite == null) {
			return;
		}

		string animation = GetDirectionName(direction);
		StringName animationName = new StringName($"{state}_{animation}");

		if (_sprite.Animation != animationName || !_sprite.IsPlaying()) {
			_sprite.Play(animationName);
		}
	}

	private string GetDirectionName(Vector2 direction) {
		if (Mathf.Abs(direction.X) > Mathf.Abs(direction.Y)) {
			return direction.X > 0 ? "right" : "left";
		}

		return direction.Y > 0 ? "down" : "up";
	}
}
