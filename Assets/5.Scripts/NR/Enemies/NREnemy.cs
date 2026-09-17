using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ============================================================================
// 신규 적 기반 클래스
//  - 코드로 생성 (프리팹/애니메이터 불필요)
//  - 플레이어 공격 판정: 기존 방식과 동일 (Player 태그 + isAtking)
//  - 공격: 태그 weapon + 레이어 EnemyAttack2 + NRDamageSource
// ============================================================================
public enum NREnemyKind { Guardian, Bomber, Mage, Assassin, Archer, Spider, Warden }

public abstract class NREnemy : MonoBehaviour, INRDamageable
{
	// ---- 설정 ----
	public string displayName = "적";
	public int maxHP = 100;
	public float moveSpeed = 2f;
	public float damage = 20f;
	public float knockbackResist = 0f;   // 0 = 잘 밀림, 1 = 안 밀림
	public float staggerTime = 0.12f;    // 피격 경직 (0이면 없음)
	public Color accent = NRPalette.Crimson;

	// ---- 상태 ----
	public int hp;
	public bool IsDead { get; private set; }
	public bool IsBoss => false;
	public float HpRatio => maxHP > 0 ? (float)hp / maxHP : 0f;
	public Transform DamageAnchor => transform;

	protected Rigidbody2D rb;
	protected SpriteRenderer sr;
	protected NRSpriteAnimator anim;
	protected CapsuleCollider2D body;
	protected Player player;
	protected PlayerAction playerAction;
	protected float stunnedUntil;
	protected float spawnedAt;
	protected bool facingRight = true;

	Transform hpBarRoot;
	Transform hpFill;
	float hpBarVisibleUntil;
	SpriteRenderer shadowGlow;

	protected static int EnemyAttackLayer => 21;

	protected Vector2 PlayerPos => player != null ? (Vector2)player.transform.position + new Vector2(0, 0.2f) : (Vector2)transform.position;
	protected Vector2 Center => (Vector2)transform.position + new Vector2(0, BodyHeight * 0.45f);
	protected Vector2 ToPlayer => PlayerPos - Center;
	protected float DistToPlayer => ToPlayer.magnitude;
	protected virtual float BodyHeight => 0.9f;
	protected bool PlayerAlive => player != null && !player.isDead;

	// ---------------------------------------------------------------------
	public virtual void Setup(float hpMul, float atkMul)
	{
		maxHP = Mathf.RoundToInt(maxHP * hpMul);
		hp = maxHP;
		damage *= atkMul;
	}

	protected virtual void Awake()
	{
		gameObject.layer = 7; // Enemy
		gameObject.tag = "Enemy";
		rb = gameObject.AddComponent<Rigidbody2D>();
		rb.gravityScale = 0f;
		rb.freezeRotation = true;
		rb.drag = 8f;
		rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
		rb.interpolation = RigidbodyInterpolation2D.Interpolate;

		var bodyGo = new GameObject("Sprite");
		bodyGo.transform.SetParent(transform, false);
		sr = bodyGo.AddComponent<SpriteRenderer>();
		anim = bodyGo.AddComponent<NRSpriteAnimator>();
		anim.Init(sr, accent, 3);

		body = gameObject.AddComponent<CapsuleCollider2D>();
		body.direction = CapsuleDirection2D.Vertical;

		// 발밑 은은한 빛 (어두운 맵에서 위치 식별)
		var glowGo = new GameObject("Glow");
		glowGo.transform.SetParent(transform, false);
		glowGo.transform.localPosition = new Vector3(0, 0.05f, 0);
		shadowGlow = glowGo.AddComponent<SpriteRenderer>();
		shadowGlow.sprite = NRSprites.Glow;
		NRSort.Set(shadowGlow, NRSort.Floor, 20);

		BuildHpBar();
	}

	protected virtual void Start()
	{
		player = FindObjectOfType<Player>();
		playerAction = player != null ? player.GetComponent<PlayerAction>() : null;
		if (hp <= 0) hp = maxHP;
		spawnedAt = Time.time;
		anim.outlineColor = accent;
		shadowGlow.color = accent.WithAlpha(0.22f);
		ConfigureVisual();
		StartCoroutine(Brain());
	}

	/// <summary>스프라이트 크기/콜라이더 설정</summary>
	protected abstract void ConfigureVisual();
	protected abstract IEnumerator Brain();

	protected void SetBodyScale(float scale, Vector2 colliderSize, float colliderOffsetY)
	{
		sr.transform.localScale = new Vector3(scale, scale, 1f);
		body.size = colliderSize;
		body.offset = new Vector2(0, colliderOffsetY);
		shadowGlow.transform.localScale = new Vector3(colliderSize.x * 2.2f, colliderSize.x * 1.1f, 1f);
		hpBarRoot.localPosition = new Vector3(0, colliderOffsetY + colliderSize.y * 0.5f + 0.35f, 0);
	}

	protected virtual void Update()
	{
		if (IsDead) return;
		if (player != null && player.EnmeyDown)
		{
			Despawn();
			return;
		}
		if (hpBarRoot != null)
		{
			bool show = Time.time < hpBarVisibleUntil;
			hpBarRoot.gameObject.SetActive(show);
			if (show) hpFill.localScale = new Vector3(Mathf.Clamp01(HpRatio), 1f, 1f);
		}
	}

	// ---- 이동 보조 ----
	protected float SpeedMul => NREnemyStatus.SpeedMul(this) * (Time.time < stunnedUntil ? 0f : 1f);

	protected void MoveDir(Vector2 dir, float speedScale = 1f)
	{
		if (Time.time < stunnedUntil) return; // 경직 중에는 넉백이 감속되도록 속도를 덮어쓰지 않음
		if (dir.sqrMagnitude > 1f) dir.Normalize();
		Vector2 moved = AvoidWalls(dir);
		rb.velocity = moved * moveSpeed * speedScale * SpeedMul;
		if (Mathf.Abs(dir.x) > 0.05f) Face(dir.x);
	}

	// ---- 벽 피하기 ----
	static readonly RaycastHit2D[] steerHits = new RaycastHit2D[4];
	int steerSide;
	float steerUntil;

	/// <summary>벽에 정면으로 밀착한 채 멈추지 않도록, 막히면 벽을 따라 비껴 간다.</summary>
	Vector2 AvoidWalls(Vector2 dir)
	{
		if (dir.sqrMagnitude < 0.0001f || body == null) return dir;
		float probe = Mathf.Max(0.5f, moveSpeed * 0.3f);
		if (IsClear(dir, probe)) return dir;

		if (steerSide == 0 || Time.time > steerUntil) steerSide = UnityEngine.Random.value < 0.5f ? 1 : -1;
		for (int i = 0; i < 2; i++)
		{
			int side = i == 0 ? steerSide : -steerSide;
			for (float angle = 30f; angle <= 120f; angle += 30f)
			{
				Vector2 candidate = Rotate(dir, angle * side);
				if (!IsClear(candidate, probe)) continue;
				steerSide = side;
				steerUntil = Time.time + 0.7f; // 같은 방향으로 돌아 좌우로 떠는 것을 막음
				return candidate;
			}
		}
		return dir;
	}

	bool IsClear(Vector2 dir, float distance)
	{
		float radius = Mathf.Max(0.05f, body.bounds.extents.x * 0.85f);
		int n = Physics2D.CircleCastNonAlloc(rb.position, radius, dir.normalized, steerHits, distance, LayerMask.GetMask("Default"));
		for (int i = 0; i < n; i++)
		{
			var c = steerHits[i].collider;
			if (c != null && !c.isTrigger) return false;
		}
		return true;
	}

	static Vector2 Rotate(Vector2 v, float degrees)
	{
		float r = degrees * Mathf.Deg2Rad, cos = Mathf.Cos(r), sin = Mathf.Sin(r);
		return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
	}

	protected void Stop() { rb.velocity = Vector2.zero; }

	protected void Face(float x)
	{
		if (Mathf.Abs(x) < 0.01f) return;
		facingRight = x > 0;
		sr.flipX = !facingRight;
	}

	protected void FacePlayer() => Face(ToPlayer.x);

	protected IEnumerator WaitStun()
	{
		while (Time.time < stunnedUntil && !IsDead) yield return null;
	}

	/// <summary>벽에 막혔는지 (레이캐스트)</summary>
	protected bool HasLineOfSight(Vector2 from, Vector2 to)
	{
		var hit = Physics2D.Linecast(from, to, LayerMask.GetMask("Default"));
		return hit.collider == null || hit.collider.isTrigger;
	}

	// ---- 공격 생성 ----
	protected GameObject SpawnHitbox(Vector2 center, Vector2 size, float dmg, float life)
	{
		var go = new GameObject(displayName + " Hitbox");
		go.layer = EnemyAttackLayer;
		go.tag = "weapon";
		go.transform.position = center;
		var col = go.AddComponent<BoxCollider2D>();
		col.isTrigger = true;
		col.size = size;
		var hrb = go.AddComponent<Rigidbody2D>();
		hrb.bodyType = RigidbodyType2D.Kinematic;
		var src = go.AddComponent<NRDamageSource>();
		src.damage = dmg;
		src.destroyOnHit = false;
		Destroy(go, life);
		return go;
	}

	protected NRProjectile SpawnProjectile(Vector2 pos, Vector2 velocity, float dmg, Color color, float size = 0.35f, float life = 4f, bool arrow = false)
	{
		return NRProjectile.Spawn(pos, velocity, dmg, color, size, life, arrow);
	}

	// ---- 피해 ----
	void OnTriggerEnter2D(Collider2D other)
	{
		if (IsDead) return;
		if (other.CompareTag("Player") && playerAction != null && playerAction.isAtking && other.isTrigger)
		{
			NRStats.PlayerHitEnemy(this, this);
		}
	}

	public void ReceiveDamage(int amount, bool crit, NRDamageKind kind)
	{
		if (IsDead || amount <= 0) return;
		hp -= amount;
		hpBarVisibleUntil = Time.time + 3f;
		anim.Flash(kind == NRDamageKind.Burn ? 0.04f : 0.09f);
		NRCombatFX.DamageNumber(Center + Vector2.up * BodyHeight * 0.6f, amount, crit, kind);

		if (kind == NRDamageKind.Direct && player != null)
		{
			Vector2 away = ((Vector2)transform.position - (Vector2)player.transform.position).normalized;
			rb.velocity += away * 4.5f * (1f - knockbackResist);
			if (staggerTime > 0f) stunnedUntil = Time.time + staggerTime * (crit ? 1.6f : 1f);
			NRCombatFX.Sparks(Center, accent, 5);
			OnHurt();
		}

		if (hp <= 0) Die();
	}

	protected virtual void OnHurt() { }

	protected virtual void Die()
	{
		if (IsDead) return;
		IsDead = true;
		StopAllCoroutines();
		rb.velocity = Vector2.zero;
		rb.simulated = false;
		body.enabled = false;
		if (hpBarRoot != null) hpBarRoot.gameObject.SetActive(false);
		NRStats.OnEnemyKilled(Center, false);
		NRWaves.NotifyDeath(gameObject);
		StartCoroutine(DeathRoutine());
	}

	protected virtual NRClip DeathClip => null;

	IEnumerator DeathRoutine()
	{
		var clip = DeathClip;
		if (clip != null && clip.Length > 0)
		{
			bool done = false;
			clip.loop = false;
			anim.Play(clip, true, () => done = true);
			float limit = Time.time + 2.5f;
			while (!done && Time.time < limit) yield return null;
		}
		float t = 0f;
		while (t < 0.4f)
		{
			t += Time.deltaTime;
			float a = 1f - t / 0.4f;
			sr.color = sr.color.WithAlpha(a);
			shadowGlow.color = shadowGlow.color.WithAlpha(0.22f * a);
			yield return null;
		}
		Destroy(gameObject);
	}

	public void Despawn()
	{
		if (IsDead) return;
		IsDead = true;
		NRWaves.NotifyDeath(gameObject);
		Destroy(gameObject);
	}

	void BuildHpBar()
	{
		var root = new GameObject("HpBar");
		root.transform.SetParent(transform, false);
		hpBarRoot = root.transform;
		var bg = new GameObject("Bg").AddComponent<SpriteRenderer>();
		bg.transform.SetParent(hpBarRoot, false);
		bg.sprite = NRSprites.White;
		bg.color = new Color(0.05f, 0.03f, 0.08f, 0.9f);
		bg.transform.localScale = new Vector3(0.9f, 0.12f, 1f);
		NRSort.Set(bg, NRSort.Top, 300);
		var fillPivot = new GameObject("FillPivot").transform;
		fillPivot.SetParent(hpBarRoot, false);
		fillPivot.localPosition = new Vector3(-0.42f, 0, 0);
		hpFill = fillPivot;
		var fill = new GameObject("Fill").AddComponent<SpriteRenderer>();
		fill.transform.SetParent(fillPivot, false);
		fill.sprite = NRSprites.White;
		fill.color = NRPalette.Hp;
		fill.transform.localPosition = new Vector3(0.42f, 0, 0);
		fill.transform.localScale = new Vector3(0.84f, 0.07f, 1f);
		NRSort.Set(fill, NRSort.Top, 301);
		root.SetActive(false);
	}
}

// ---------------------------------------------------------------------------
// 적 투사체
// ---------------------------------------------------------------------------
public class NRProjectile : MonoBehaviour
{
	Rigidbody2D rb;
	SpriteRenderer core, glow;
	public Transform homingTarget;
	public float turnRate = 0f;
	float speed;
	float bornAt;

	public static NRProjectile Spawn(Vector2 pos, Vector2 velocity, float dmg, Color color, float size, float life, bool arrow)
	{
		var go = new GameObject(arrow ? "NR Arrow" : "NR Orb");
		go.layer = 21;
		go.tag = "weapon";
		go.transform.position = pos;
		var p = go.AddComponent<NRProjectile>();
		p.rb = go.AddComponent<Rigidbody2D>();
		p.rb.bodyType = RigidbodyType2D.Kinematic;
		p.rb.velocity = velocity;
		p.speed = velocity.magnitude;
		var col = go.AddComponent<CircleCollider2D>();
		col.isTrigger = true;
		col.radius = arrow ? 0.12f : size * 0.45f;
		var src = go.AddComponent<NRDamageSource>();
		src.damage = dmg;
		src.destroyOnHit = true;

		var glowGo = new GameObject("Glow");
		glowGo.transform.SetParent(go.transform, false);
		p.glow = glowGo.AddComponent<SpriteRenderer>();
		p.glow.sprite = NRSprites.Glow;
		p.glow.color = color.WithAlpha(0.6f);
		NRSort.Set(p.glow, NRSort.Enemy, 60);
		glowGo.transform.localScale = Vector3.one * size * 2.4f;

		var coreGo = new GameObject("Core");
		coreGo.transform.SetParent(go.transform, false);
		p.core = coreGo.AddComponent<SpriteRenderer>();
		p.core.sprite = NRSprites.White;
		p.core.color = Color.Lerp(color, Color.white, 0.55f);
		NRSort.Set(p.core, NRSort.Enemy, 61);
		coreGo.transform.localScale = arrow ? new Vector3(0.55f, 0.07f, 1f) : Vector3.one * size * 0.5f;

		p.bornAt = Time.time;
		p.Align();
		Destroy(go, life);
		return p;
	}

	void Align()
	{
		if (rb.velocity.sqrMagnitude > 0.001f)
			transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(rb.velocity.y, rb.velocity.x) * Mathf.Rad2Deg);
	}

	void FixedUpdate()
	{
		if (homingTarget != null && turnRate > 0f)
		{
			Vector2 desired = ((Vector2)homingTarget.position + new Vector2(0, 0.2f) - rb.position).normalized * speed;
			rb.velocity = Vector3.RotateTowards(rb.velocity, desired, turnRate * Mathf.Deg2Rad * Time.fixedDeltaTime, 0f);
		}
		Align();
		if (core != null && glow != null) glow.transform.localScale *= 1f + 0.02f * Mathf.Sin((Time.time - bornAt) * 20f);
	}

	void OnTriggerEnter2D(Collider2D other)
	{
		if (other.CompareTag("Player") && !other.isTrigger)
		{
			var act = other.GetComponent<PlayerAction>();
			if (act != null && act.DashInvulnerable) return; // 구르기 중에는 투사체가 통과
			NRCombatFX.Sparks(transform.position, core != null ? core.color : Color.white, 6);
			Destroy(gameObject, 0.01f);
			return;
		}
		// 벽(타일맵 등 비트리거, 기본 레이어)에 닿으면 소멸
		if (!other.isTrigger && other.gameObject.layer == 0 && Time.time - bornAt > 0.05f)
		{
			NRCombatFX.Sparks(transform.position, core != null ? core.color : Color.white, 4);
			Destroy(gameObject);
		}
	}
}
