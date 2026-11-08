using Godot;

public partial class BossController : EnemyController {
	public enum BossCombatState {
		Idle,
		Chasing,
		Shooting,
		Hurt,
		Dead
	}

	[Signal]
	public delegate void LaserAttackStartedEventHandler();

	[Signal]
	public delegate void LaserAttackEndedEventHandler();

	[ExportGroup("Boss Combat Settings")]
	[Export]
	public PackedScene LaserScene { get; set; }

	[Export]
	public new float MoveSpeed { get; set; } = 70.0f;

	[Export]
	public new float AttackRange { get; set; } = 150.0f;

	[Export]
	public new float AttackCooldown { get; set; } = 2.5f;

	[Export]
	public int LaserDamage { get; set; } = 25;

	[Export]
	public float LaserDuration { get; set; } = 0.85f;

	[Export]
	public override int MaxHealth { get; set; } = 200;

	[ExportGroup("Boss Loot Settings")]
	[Export]
	public int BossLootCount { get; set; } = 5;

	[Export]
	public float BossLootSpreadRadius { get; set; } = 24.0f;

	[ExportGroup("Weapon Offsets")]
	[Export]
	public Vector2 WeaponOffsetRunWithGun { get; set; } = new Vector2(27, 1);

	[Export]
	public Vector2 WeaponOffsetShooting { get; set; } = new Vector2(20, 0);

	[Export]
	public Vector2 MuzzleLocalOffset { get; set; } = new Vector2(10.125f, 0.34f);

	private BossCombatState _bossCombatState = BossCombatState.Idle;
	private int _bossHealth;
	private bool _isBossDead = false;
	private bool _isFacingLeft = false;

	private PlayerController _targetPlayer;
	private AnimatedSprite2D _bossSprite;
	private Node2D _weaponPivot;
	private AnimatedSprite2D _laserGun;
	private CollisionShape2D _bossCollisionShape;
	private ProgressBar _bossHealthBar;
	private CanvasItem _bossHealthBarAnchor;

	private float _attackCooldownTimer = 0.0f;
	private float _shootingTimer = 0.0f;
	private BossLaser _currentLaser = null;
	private bool _hasDroppedBossReward = false;

	public BossCombatState CombatState => _bossCombatState;
	public bool IsFacingLeft => _isFacingLeft;
	public override int CurrentHealth => _bossHealth;
	public override bool IsDead => _isBossDead || _bossCombatState == BossCombatState.Dead || _bossHealth <= 0;

	public override void _Ready() {
		// Initialize health first so any registration sees the correct alive state
		_bossHealth = MaxHealth;
		_currentHealth = MaxHealth;

		// Register in enemy groups for detection by player projectiles and level controller
		if (!IsInGroup("enemy")) {
			AddToGroup("enemy");
		}
		if (!IsInGroup("enemies")) {
			AddToGroup("enemies");
		}
		if (!IsInGroup("boss")) {
			AddToGroup("boss");
		}

		_bossSprite = GetNodeOrNull<AnimatedSprite2D>("Visuals/AnimatedSprite2D");
		_weaponPivot = GetNodeOrNull<Node2D>("WeaponPivot");
		_laserGun = GetNodeOrNull<AnimatedSprite2D>("WeaponPivot/LaserGun");
		_bossCollisionShape = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
		_bossHealthBar = GetNodeOrNull<ProgressBar>(HealthBarPath);
		_bossHealthBarAnchor = GetNodeOrNull<CanvasItem>(HealthBarAnchorPath);

		if (_bossHealthBar != null) {
			_bossHealthBar.MaxValue = MaxHealth;
			_bossHealthBar.Value = _bossHealth;
			_bossHealthBar.Visible = true;
		}

		if (_bossHealthBarAnchor != null) {
			_bossHealthBarAnchor.Visible = true;
		}

		EmitSignal(SignalName.HealthChanged, _bossHealth, MaxHealth);

		LevelController levelController = GetTree().GetFirstNodeInGroup("level_controller") as LevelController;
		levelController?.RegisterEnemy(this);

		if (_bossSprite != null) {
			_bossSprite.AnimationFinished += OnBossAnimationFinished;

			if (_bossSprite.SpriteFrames != null) {
				if (_bossSprite.SpriteFrames.HasAnimation("boss_death")) {
					_bossSprite.SpriteFrames.SetAnimationLoopMode("boss_death", SpriteFrames.LoopMode.None);
				}
				if (_bossSprite.SpriteFrames.HasAnimation("boss_hurt")) {
					_bossSprite.SpriteFrames.SetAnimationLoopMode("boss_hurt", SpriteFrames.LoopMode.None);
				}
			}
		}

		if (LaserScene == null) {
			LaserScene = GD.Load<PackedScene>("res://Scenes/BossLaser.tscn");
		}

		_targetPlayer = GetTree().GetFirstNodeInGroup("player") as PlayerController
			?? GetTree().GetFirstNodeInGroup("Player") as PlayerController;

		SetState(BossCombatState.Idle);
	}

	public override void _PhysicsProcess(double delta) {
		if (_isBossDead || _bossCombatState == BossCombatState.Dead) {
			return;
		}

		if (_attackCooldownTimer > 0.0f) {
			_attackCooldownTimer -= (float)delta;
		}

		if (_bossCombatState == BossCombatState.Hurt) {
			Velocity = Vector2.Zero;
			MoveAndSlide();
			return;
		}

		if (_bossCombatState == BossCombatState.Shooting) {
			Velocity = Vector2.Zero;
			MoveAndSlide();

			_shootingTimer += (float)delta;
			if (_shootingTimer >= LaserDuration) {
				EndLaserAttack();
			}
			return;
		}

		// Re-fetch player if missing or check if dead
		if (!GodotObject.IsInstanceValid(_targetPlayer) || _targetPlayer.IsDead) {
			_targetPlayer = GetTree().GetFirstNodeInGroup("player") as PlayerController;
			if (!GodotObject.IsInstanceValid(_targetPlayer) || _targetPlayer.IsDead) {
				Velocity = Vector2.Zero;
				MoveAndSlide();
				SetState(BossCombatState.Idle);
				return;
			}
		}

		Vector2 toPlayer = _targetPlayer.GlobalPosition - GlobalPosition;
		float distance = toPlayer.Length();

		// Face the player horizontally
		if (Mathf.Abs(toPlayer.X) > 4.0f) {
			_isFacingLeft = toPlayer.X < 0.0f;
			UpdateFacing();
		}

		// Check if player is within attack range
		if (distance <= AttackRange) {
			Velocity = Vector2.Zero;
			MoveAndSlide();

			if (_attackCooldownTimer <= 0.0f) {
				StartLaserAttack();
			} else {
				SetState(BossCombatState.Idle);
			}
			return;
		}

		// Outside attack range: chase player
		Vector2 moveDir = distance > 0.001f ? toPlayer.Normalized() : Vector2.Zero;
		Velocity = moveDir * MoveSpeed;
		MoveAndSlide();
		SetState(BossCombatState.Chasing);
	}

	private void StartLaserAttack() {
		if (_isBossDead) {
			return;
		}

		SetState(BossCombatState.Shooting);
		_shootingTimer = 0.0f;
		_attackCooldownTimer = AttackCooldown;
		Velocity = Vector2.Zero;

		// Face player when initiating attack
		if (GodotObject.IsInstanceValid(_targetPlayer)) {
			Vector2 toPlayer = _targetPlayer.GlobalPosition - GlobalPosition;
			if (Mathf.Abs(toPlayer.X) > 2.0f) {
				_isFacingLeft = toPlayer.X < 0.0f;
				UpdateFacing();
			}
		}

		// Animate gun firing coils
		if (_laserGun != null && _laserGun.SpriteFrames != null && _laserGun.SpriteFrames.HasAnimation("lazer_shoot")) {
			_laserGun.Play("lazer_shoot");
		}

		SpawnLaserBeam();
		EmitSignal(SignalName.LaserAttackStarted);
	}

	private void SpawnLaserBeam() {
		if (LaserScene == null) {
			GD.PushWarning("BossController: LaserScene is null!");
			return;
		}

		// Remove any existing active laser
		CleanupActiveLaser();

		Vector2 muzzlePos = GetMuzzleGlobalPosition();
		Vector2 targetPos = GodotObject.IsInstanceValid(_targetPlayer)
			? _targetPlayer.GlobalPosition
			: muzzlePos + (_isFacingLeft ? Vector2.Left * 100 : Vector2.Right * 100);

		BossLaser laser = LaserScene.Instantiate<BossLaser>();
		if (laser == null) {
			GD.PushWarning("BossController: Failed to instantiate BossLaser!");
			return;
		}

		Node targetParent = GetTree().CurrentScene ?? GetParent();
		if (targetParent != null) {
			targetParent.AddChild(laser);
		} else {
			AddChild(laser);
		}

		laser.Activate(muzzlePos, targetPos, LaserDamage, LaserDuration);
		_currentLaser = laser;
	}

	private void EndLaserAttack() {
		CleanupActiveLaser();

		if (_laserGun != null) {
			_laserGun.Stop();
			_laserGun.Frame = 0;
		}

		EmitSignal(SignalName.LaserAttackEnded);

		// Resume chasing or idle depending on player distance
		if (GodotObject.IsInstanceValid(_targetPlayer) && !_targetPlayer.IsDead) {
			float dist = GlobalPosition.DistanceTo(_targetPlayer.GlobalPosition);
			if (dist > AttackRange) {
				SetState(BossCombatState.Chasing);
			} else {
				SetState(BossCombatState.Idle);
			}
		} else {
			SetState(BossCombatState.Idle);
		}
	}

	private void CleanupActiveLaser() {
		if (_currentLaser != null && GodotObject.IsInstanceValid(_currentLaser)) {
			_currentLaser.Deactivate();
			_currentLaser.QueueFree();
			_currentLaser = null;
		}
	}

	public Vector2 GetMuzzleGlobalPosition() {
		if (_weaponPivot != null) {
			return _weaponPivot.ToGlobal(MuzzleLocalOffset);
		}
		return GlobalPosition;
	}

	private void SetState(BossCombatState newState) {
		_bossCombatState = newState;

		switch (newState) {
			case BossCombatState.Idle:
				PlayAnimation("boss_idle");
				UpdateWeaponDisplay("boss_idle");
				break;
			case BossCombatState.Chasing:
				PlayAnimation("boss_runWithGun");
				UpdateWeaponDisplay("boss_runWithGun");
				break;
			case BossCombatState.Shooting:
				PlayAnimation("boss_shooting");
				UpdateWeaponDisplay("boss_shooting");
				break;
			case BossCombatState.Hurt:
				PlayAnimation("boss_hurt");
				UpdateWeaponDisplay("boss_hurt");
				break;
			case BossCombatState.Dead:
				PlayAnimation("boss_death");
				UpdateWeaponDisplay("boss_death");
				break;
		}
	}

	private void UpdateFacing() {
		if (_bossSprite != null) {
			_bossSprite.FlipH = _isFacingLeft;
		}
		if (_weaponPivot != null) {
			_weaponPivot.Scale = new Vector2(_isFacingLeft ? -1.0f : 1.0f, 1.0f);
			string anim = _bossSprite?.Animation.ToString() ?? "";
			UpdateWeaponDisplay(anim);
		}
	}

	private void UpdateWeaponDisplay(string animName) {
		if (_weaponPivot == null) {
			return;
		}

		// Weapon visibility rules:
		// - Visible during boss_runWithGun
		// - Visible during boss_shooting
		// - Hidden during boss_idle
		// - Hidden during boss_hurt
		// - Hidden during boss_death
		if (_isBossDead || animName == "boss_idle" || animName == "boss_hurt" || animName == "boss_death") {
			_weaponPivot.Visible = false;
		} else if (animName == "boss_runWithGun") {
			_weaponPivot.Visible = true;
			_weaponPivot.Position = new Vector2(
				_isFacingLeft ? -WeaponOffsetRunWithGun.X : WeaponOffsetRunWithGun.X,
				WeaponOffsetRunWithGun.Y
			);
		} else if (animName == "boss_shooting") {
			_weaponPivot.Visible = true;
			_weaponPivot.Position = new Vector2(
				_isFacingLeft ? -WeaponOffsetShooting.X : WeaponOffsetShooting.X,
				WeaponOffsetShooting.Y
			);
		}
	}

	private void PlayAnimation(string animName) {
		if (_bossSprite != null && _bossSprite.SpriteFrames != null && _bossSprite.SpriteFrames.HasAnimation(animName)) {
			if (_bossSprite.Animation != animName || !_bossSprite.IsPlaying()) {
				_bossSprite.Play(animName);
			}
		}
	}

	public override void TakeDamage(int damage) {
		if (_isBossDead || _bossCombatState == BossCombatState.Dead || damage <= 0) {
			return;
		}

		_bossHealth -= damage;
		_currentHealth = _bossHealth;
		if (_bossHealth <= 0) {
			_bossHealth = 0;
			_currentHealth = 0;
			UpdateHealthBarDisplay();
			EmitSignal(SignalName.HealthChanged, _bossHealth, MaxHealth);
			Die();
		} else {
			UpdateHealthBarDisplay();
			EmitSignal(SignalName.HealthChanged, _bossHealth, MaxHealth);

			// Only interrupt into Hurt state if not currently performing the laser beam attack
			if (_bossCombatState != BossCombatState.Shooting) {
				SetState(BossCombatState.Hurt);
				Velocity = Vector2.Zero;
			}
		}
	}

	private void UpdateHealthBarDisplay() {
		if (_bossHealthBar != null && !_isBossDead) {
			_bossHealthBar.Value = _bossHealth;
		}
	}

	private void Die() {
		if (_isBossDead) {
			return;
		}

		_isBossDead = true;
		_bossCombatState = BossCombatState.Dead;
		_currentState = EnemyState.Dead;
		_bossHealth = 0;
		_currentHealth = 0;
		Velocity = Vector2.Zero;

		// 1. Immediately remove and deactivate any active laser
		CleanupActiveLaser();

		// 2. Hide weapon
		if (_weaponPivot != null) {
			_weaponPivot.Visible = false;
		}

		// 3. Disable collision
		if (_bossCollisionShape != null) {
			_bossCollisionShape.SetDeferred(CollisionShape2D.PropertyName.Disabled, true);
		}

		// 4. Hide health bar
		if (_bossHealthBarAnchor != null) {
			_bossHealthBarAnchor.Visible = false;
		} else if (_bossHealthBar != null) {
			_bossHealthBar.Visible = false;
		}

		// 5. Emit death signals
		EmitSignal(SignalName.EnemyDied);

		// 6. Play death animation
		PlayAnimation("boss_death");
	}

	private void OnBossAnimationFinished() {
		if (_bossCombatState == BossCombatState.Dead || _isBossDead) {
			if (_bossSprite != null && _bossSprite.Animation == "boss_death") {
				SpawnBossReward();
				EmitSignal(SignalName.EnemyDefeated);
				QueueFree();
			}
			return;
		}

		if (_bossCombatState == BossCombatState.Hurt) {
			SetState(BossCombatState.Chasing);
		}
	}

	private void SpawnBossReward() {
		if (_hasDroppedBossReward) {
			return;
		}

		_hasDroppedBossReward = true;

		PackedScene scene = CrystalDropScene ?? GD.Load<PackedScene>("res://Scenes/collectibles/Crystal_Artifact.tscn");
		if (scene == null) {
			GD.PushWarning("BossController: CrystalDropScene is null!");
			return;
		}

		Node targetParent = GetTree().CurrentScene ?? GetParent();
		if (targetParent == null) {
			return;
		}

		// Spawn exactly BossLootCount crystals distributed radially around the boss death position
		for (int i = 0; i < BossLootCount; i++) {
			Node instance = scene.Instantiate();
			if (instance is Node2D collectible) {
				float angle = (i * Mathf.Tau) / BossLootCount;
				Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * BossLootSpreadRadius;
				collectible.GlobalPosition = GlobalPosition + offset;
			}
			targetParent.AddChild(instance);
		}

		GD.Print($"[BossController] Boss defeated: spawned {BossLootCount} crystal collectibles around {GlobalPosition}.");
	}
}
