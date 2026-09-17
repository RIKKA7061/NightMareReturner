using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ============================================================================
// 캐릭터(근접 / 원거리) · 패시브 · 스킬 설명
// ============================================================================
public enum NRHeroType { Melee = 0, Ranged = 1 }

public class NRSkillInfo
{
	public string name;
	public string desc;
	public int icon;
	public Color color;
}

public static class NRHero
{
	public static NRHeroType Current => NRSave.Data.hero == 1 ? NRHeroType.Ranged : NRHeroType.Melee;
	public static bool IsRanged => Current == NRHeroType.Ranged;

	public static string Name(NRHeroType t) => t == NRHeroType.Ranged ? "침착한 사수" : "의지의 권사";
	public static string Summary(NRHeroType t) => t == NRHeroType.Ranged
		? "멀리서 싸우는 사격수. 사거리가 길지만 체력·방어력·피해가 낮습니다."
		: "가까이 붙어 싸우는 격투가. 3타 콤보와 높은 체력으로 버팁니다.";

	// ---- 패시브 ----
	public static string PassiveName => IsRanged ? "침착한 호흡" : "반격의 의지";
	public static string PassiveDesc => IsRanged
		? "적을 처치하면 체력 " + KillHeal + " 회복.\n기본 사격 4번째마다 반드시 치명타."
		: "적을 처치하면 체력 " + KillHeal + " 회복.\n3타 콤보 마무리가 적중하면 보호막 8 획득.";
	public static int KillHeal => (IsRanged ? 3 : 5) + Mathf.Max(0, NRRun.Floor - 1);

	// ---- 능력치 보정 ----
	public const int RangedHpPenalty = 100;
	public const int RangedArmorPenalty = 25;
	public const float RangedDamageMul = 0.75f;
	public const float RangedRange = 7.5f;

	/// <summary>새 회차 초기화 시 (NRStats.ResetRun 이후)</summary>
	public static void ApplyRunStats(Player p)
	{
		if (p == null) return;
		if (IsRanged)
		{
			p.maxHP = Mathf.Max(100, p.maxHP - RangedHpPenalty);
			p.AR = Mathf.Max(0, p.AR - RangedArmorPenalty);
			p.nowHP = p.maxHP;
		}
		NRRangedAvatar.Sync(p);
	}

	public static void Set(NRHeroType t)
	{
		NRSave.Data.hero = (int)t;
		NRSave.MarkDirty();
		NRSave.Save();
		var p = NRStats.Player;
		if (p != null && Player.gameRound == 0 && !p.isDead)
		{
			// 집에서 바꾸면 즉시 능력치 재계산
			p.StatDefaultPlayer();
		}
		else if (p != null) NRRangedAvatar.Sync(p);
	}

	// ---- 스킬 설명 (HUD 스킬 바 / 툴팁) ----
	public static NRSkillInfo Attack => IsRanged
		? new NRSkillInfo { name = "사격", icon = 16, color = NRPalette.Cyan, desc = "마우스 방향으로 탄환 발사.\n사거리 " + RangedRange + " · 피해 공격력×" + RangedDamageMul }
		: new NRSkillInfo { name = "연타", icon = 16, color = NRPalette.Cyan, desc = "마우스 방향 주먹 공격. 이어서 누르면 3타 콤보.\n2타 ×1.15 · 3타 마무리 ×1.8 (넓은 범위)" };
	public static NRSkillInfo Skill => IsRanged
		? new NRSkillInfo { name = "관통탄", icon = 1, color = NRPalette.Rage, desc = "잠깐 조준 후 적을 꿰뚫는 탄환.\n피해 공격력×2 · 최대 6명 관통" }
		: new NRSkillInfo { name = "강타", icon = 1, color = NRPalette.Rage, desc = "힘을 모아 넓게 내려치기.\n피해 공격력×2" };
	public static NRSkillInfo Ultimate => IsRanged
		? new NRSkillInfo { name = "난사", icon = 2, color = NRPalette.Gold, desc = "부채꼴로 탄환을 3번 퍼붓습니다.\n탄환당 피해 공격력×0.9" }
		: new NRSkillInfo { name = "해머", icon = 2, color = NRPalette.Gold, desc = "혼신의 내려찍기.\n피해 공격력×4" };
	public static NRSkillInfo Dash => new NRSkillInfo
	{
		name = "구르기", icon = 13, color = NRPalette.Anxiety,
		desc = "이동 방향으로 빠르게 구릅니다. (멈춰 있으면 마우스 방향)\n구르는 동안 적의 몸과 투사체·공격을 통과합니다."
	};
}

// ============================================================================
// 원거리 캐릭터 외형 (DARK 팩 건슬링어). 기존 플레이어 애니메이터는 그대로 두고 그림만 교체.
// ============================================================================
public class NRRangedAvatar : MonoBehaviour
{
	const string Sheet = "NR/Sprites/Hero/gunslinger";
	const float Ppu = 23f;
	/// <summary>기준점에서 발이 닿는 높이 (기존 근접 캐릭터 그림의 발 위치와 같게)</summary>
	const float FeetOffset = -0.9f;

	Player player;
	PlayerAction action;
	SpriteRenderer baseRenderer;
	SpriteRenderer sr;
	NRSpriteAnimator anim;
	NRClip idle, run, shoot, roll, hit, death;
	float shootUntil;
	int lastHp;
	bool deadPlayed;

	public static SpriteRenderer VisualOf(Player p)
	{
		if (p == null) return null;
		var av = p.GetComponent<NRRangedAvatar>();
		if (av != null && av.enabled && av.sr != null) return av.sr;
		return p.GetComponent<SpriteRenderer>();
	}

	public static void Sync(Player p)
	{
		if (p == null) return;
		var av = p.GetComponent<NRRangedAvatar>();
		if (NRHero.IsRanged)
		{
			if (av == null) av = p.gameObject.AddComponent<NRRangedAvatar>();
			av.enabled = true;
		}
		else if (av != null)
		{
			av.enabled = false;
		}
	}

	public static void NotifyShoot(Player p)
	{
		var av = p != null ? p.GetComponent<NRRangedAvatar>() : null;
		if (av != null && av.enabled) av.shootUntil = Time.time + 0.3f;
	}

	void Awake()
	{
		player = GetComponent<Player>();
		action = GetComponent<PlayerAction>();
		baseRenderer = GetComponent<SpriteRenderer>();

		var go = new GameObject("NR Gunslinger");
		go.transform.SetParent(transform, false);
		sr = go.AddComponent<SpriteRenderer>();
		anim = go.AddComponent<NRSpriteAnimator>();
		anim.Init(sr, NRPalette.Cyan, 0);
		anim.outlineColor = new Color(0, 0, 0, 0);
		if (baseRenderer != null) NRSort.Set(sr, SortingLayer.IDToName(baseRenderer.sortingLayerID), baseRenderer.sortingOrder);

		idle = NRSpriteSheet.LoadGridRow(Sheet, 48, 48, 0, 1, 6, true, Ppu);
		run = NRSpriteSheet.LoadGridRow(Sheet, 48, 48, 1, 1, 12, true, Ppu);
		shoot = NRSpriteSheet.LoadGridRow(Sheet, 48, 48, 2, 1, 16, false, Ppu);
		roll = NRSpriteSheet.LoadGridRow(Sheet, 48, 48, 8, 1, 20, false, Ppu);
		hit = NRSpriteSheet.LoadGridRow(Sheet, 48, 48, 9, 1, 12, false, Ppu, 1);
		death = NRSpriteSheet.LoadGridRow(Sheet, 48, 48, 10, 1, 8, false, Ppu);
	}

	void OnEnable()
	{
		if (baseRenderer != null && sr != null)
		{
			// 발 위치: 그림 경계는 아래 여백이 커서(128px 중 32px) 한참 아래로 내려간다.
			// 발밑 빛과 같은 높이에 발을 둔다. 거리 씬은 플레이어가 1.69배라 크기·높이 모두 부모를 따라간다.
			sr.transform.localScale = Vector3.one;
			sr.transform.localPosition = new Vector3(0, FeetOffset, 0);
			baseRenderer.color = baseRenderer.color.WithAlpha(0f);
			sr.enabled = true;
		}
		deadPlayed = false;
	}

	void OnDisable()
	{
		if (baseRenderer != null) baseRenderer.color = baseRenderer.color.WithAlpha(1f);
		if (sr != null) sr.enabled = false;
	}


	void LateUpdate()
	{
		if (player == null || action == null || sr == null) return;
		// 기존 렌더러는 계속 숨김 (피격 번쩍임 등이 알파를 되돌릴 수 있음)
		if (baseRenderer.color.a > 0f) baseRenderer.color = baseRenderer.color.WithAlpha(0f);
		sr.flipX = baseRenderer.flipX;

		if (player.isDead)
		{
			if (!deadPlayed) { anim.Play(death, true); deadPlayed = true; }
			return;
		}
		deadPlayed = false;

		if (player.nowHP < lastHp) { anim.Play(hit, true); shootUntil = Time.time + 0.18f; }
		lastHp = player.nowHP;

		if (action.IsSliding) anim.Play(roll, false, null, 1.3f);
		else if (Time.time < shootUntil) anim.Play(shoot);
		else if (action.IsMoving) anim.Play(run);
		else anim.Play(idle);
	}
}

// ============================================================================
// 플레이어 탄환 (원거리 캐릭터)
// ============================================================================
public class NRPlayerProjectile : MonoBehaviour
{
	int style;
	float damageMul;
	int pierceLeft;
	bool forceCrit;
	Vector2 velocity;
	float maxDistance;
	Vector3 origin;
	readonly HashSet<int> hit = new HashSet<int>();
	SpriteRenderer core, glow;

	public static NRPlayerProjectile Fire(Vector2 from, Vector2 dir, float speed, float range, int style, float damageMul, int pierce, Color color, float size = 1f, bool forceCrit = false)
	{
		var go = new GameObject("NR Player Bullet");
		go.layer = 6; // Player 레이어: 적(7)과 벽(0)에 충돌
		go.transform.position = from;
		go.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
		var p = go.AddComponent<NRPlayerProjectile>();
		p.style = style;
		p.damageMul = damageMul;
		p.pierceLeft = pierce;
		p.forceCrit = forceCrit;
		p.velocity = dir.normalized * speed;
		p.maxDistance = range;
		p.origin = from;

		var rb = go.AddComponent<Rigidbody2D>();
		rb.bodyType = RigidbodyType2D.Kinematic;
		rb.useFullKinematicContacts = true;
		var col = go.AddComponent<CircleCollider2D>();
		col.isTrigger = true;
		col.radius = 0.16f * size;

		var glowGo = new GameObject("Glow");
		glowGo.transform.SetParent(go.transform, false);
		p.glow = glowGo.AddComponent<SpriteRenderer>();
		p.glow.sprite = NRSprites.Glow;
		p.glow.color = color.WithAlpha(0.55f);
		NRSort.Set(p.glow, NRSort.Player, 20);
		glowGo.transform.localScale = new Vector3(0.9f, 0.5f, 1f) * size;

		var coreGo = new GameObject("Core");
		coreGo.transform.SetParent(go.transform, false);
		p.core = coreGo.AddComponent<SpriteRenderer>();
		p.core.sprite = NRSprites.White;
		p.core.color = Color.Lerp(color, Color.white, 0.6f);
		NRSort.Set(p.core, NRSort.Player, 21);
		coreGo.transform.localScale = new Vector3(0.42f, 0.09f, 1f) * size;
		return p;
	}

	void Update()
	{
		transform.position += (Vector3)(velocity * Time.deltaTime);
		if (Vector2.Distance(origin, transform.position) > maxDistance)
		{
			Fizzle();
		}
	}

	void Fizzle()
	{
		NRCombatFX.Sparks(transform.position, core != null ? core.color : Color.white, 3);
		Destroy(gameObject);
	}

	void OnTriggerEnter2D(Collider2D other)
	{
		if (other.GetComponentInParent<Player>() != null) return;
		var target = other.GetComponentInParent<INRDamageable>();
		if (target != null)
		{
			if (target.IsDead) return;
			var comp = target as Component;
			int key = comp != null ? comp.gameObject.GetInstanceID() : target.GetHashCode();
			if (!hit.Add(key)) return;
			NRStats.PlayerHitEnemyWith(target, comp, style, damageMul, forceCrit);
			NRCombatFX.Sparks(transform.position, core != null ? core.color : Color.white, 5);
			if (comp != null)
			{
				var rb = comp.GetComponent<Rigidbody2D>();
				var ne = comp as NREnemy;
				if (rb != null && ne != null) rb.velocity += velocity.normalized * (style == 1 ? 5f : 1.5f) * (1f - ne.knockbackResist);
			}
			if (pierceLeft-- <= 0) Destroy(gameObject);
			return;
		}
		if (!other.isTrigger && other.gameObject.layer == 0)
		{
			Fizzle();
		}
	}
}
