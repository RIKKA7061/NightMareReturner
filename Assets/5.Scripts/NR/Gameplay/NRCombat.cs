using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ============================================================================
// 전투 공통: 피해 계산 / 상태이상 / 피해원
// ============================================================================

public enum NRDamageKind { Direct, Burn, Explosion, Shockwave }

/// <summary>플레이어가 피해를 줄 수 있는 대상 (기존 Enemy, MonsterHP, 신규 NREnemy)</summary>
public interface INRDamageable
{
	void ReceiveDamage(int amount, bool crit, NRDamageKind kind);
	float HpRatio { get; }
	bool IsDead { get; }
	bool IsBoss { get; }
	Transform DamageAnchor { get; }
}

/// <summary>플레이어에게 닿으면 피해를 주는 오브젝트 (태그 weapon과 함께 사용)</summary>
public class NRDamageSource : MonoBehaviour
{
	public float damage = 10f;
	public bool destroyOnHit = true;
	public bool ignoreArmor = false;
	public bool passThroughDash = true;
}

public static class NRStats
{
	// ---- 런 상태 ----
	public static float Shield;
	public static float InvulnUntil;
	public static int DeathDefianceLeft;
	public static int LastStandLeft;
	public static int RunKills;
	public static float LastCombatTime = -100f;

	// ---- 개발자 ----
	public static bool GodMode;
	public static bool OneHitKill;

	// ---- 증강 합산값 (0.4 = 40%) ----
	public static float JabDmgPct, SkillDmgPct, UltDmgPct, BerserkPct, ExecutePct;
	public static float BurnDps;
	public static float MoveSpeedPct, AttackSpeedPct, CritChance;
	public static float DashCdPct; public static bool DashInvuln; public static float DashShockDmg;
	public static float ChillPct, DamageReductionPct, HealOnKill, SkillCdPct, UltCdPct;
	public static float RoomShield, LeechPct, LastStandPct;
	public static bool FrenzyCrit; public static float FrostDmgPct, DashShield, RequiemPct;

	static Player cachedPlayer;
	static PlayerAction cachedAction;
	static int lastSwingId = -999;
	static readonly HashSet<int> hitThisSwing = new HashSet<int>();

	public static Player Player
	{
		get
		{
			if (cachedPlayer == null) cachedPlayer = Object.FindObjectOfType<Player>();
			return cachedPlayer;
		}
	}

	public static PlayerAction Action
	{
		get
		{
			if (cachedAction == null && Player != null) cachedAction = Player.GetComponent<PlayerAction>();
			return cachedAction;
		}
	}

	public static void ClearAggregates()
	{
		JabDmgPct = SkillDmgPct = UltDmgPct = BerserkPct = ExecutePct = 0f;
		BurnDps = 0f;
		MoveSpeedPct = AttackSpeedPct = CritChance = 0f;
		DashCdPct = 0f; DashInvuln = false; DashShockDmg = 0f;
		ChillPct = DamageReductionPct = HealOnKill = SkillCdPct = UltCdPct = 0f;
		RoomShield = LeechPct = LastStandPct = 0f;
		FrenzyCrit = false; FrostDmgPct = DashShield = RequiemPct = 0f;
	}

	// ---- 이동/공격 배율 (PlayerAction에서 사용) ----
	public static float MoveSpeedMultiplier => 1f + MoveSpeedPct + NRMeta.SpeedPct;
	public static float AttackIntervalMultiplier => 1f / (1f + AttackSpeedPct);
	public static float SkillCooldownMultiplier => Mathf.Max(0.35f, 1f - SkillCdPct);
	public static float UltCooldownMultiplier => Mathf.Max(0.35f, 1f - SkillCdPct - UltCdPct);
	public static float DashCooldownMultiplier => Mathf.Max(0.3f, 1f - DashCdPct);
	public static bool IsInvulnerable => Time.time < InvulnUntil;

	/// <summary>새 회차 시작/부활 시 호출 (Player.StatDefaultPlayer 끝에서)</summary>
	public static void ResetRun(Player p)
	{
		cachedPlayer = p;
		cachedAction = p != null ? p.GetComponent<PlayerAction>() : null;
		Shield = 0f;
		InvulnUntil = 0f;
		RunKills = 0;
		DeathDefianceLeft = NRMeta.DeathDefiance;
		LastStandLeft = 0;
		ClearAggregates();
		if (p != null)
		{
			p.maxHP += NRMeta.BonusHp;
			p.Atk += NRMeta.BonusAtk;
			p.AR += NRMeta.BonusArmor;
			p.nowHP = p.maxHP;
		}
	}

	// ---- 플레이어 → 적 ----
	public static int ComputeOutgoing(int baseAtk, int style, INRDamageable target, out bool crit)
	{
		float mult = 1f;
		if (style == 0) mult += JabDmgPct;
		else if (style == 1) mult += SkillDmgPct;
		else if (style == 2) mult += UltDmgPct;

		var p = Player;
		if (p != null && p.maxHP > 0 && (float)p.nowHP / p.maxHP <= 0.5f) mult += BerserkPct;
		if (target != null && target.HpRatio <= 0.3f) mult += ExecutePct;
		if (target is Component comp)
		{
			var st = comp.GetComponent<NREnemyStatus>();
			if (st != null && st.IsChilled) mult += FrostDmgPct;
		}
		if (Shield > 0f) mult += RequiemPct;

		var act = Action;
		if (style == 0 && act != null && !NRHero.IsRanged) mult *= act.ComboDamageMultiplier;

		crit = Random.value < CritChance;
		if (crit) mult *= FrenzyCrit ? 2.5f : 2f;

		if (OneHitKill) return 99999;
		return Mathf.Max(1, Mathf.RoundToInt(baseAtk * mult));
	}

	/// <summary>플레이어 공격 판정이 적에 닿았을 때 (기존/신규 적 공용)</summary>
	public static void PlayerHitEnemy(INRDamageable target, Component targetComponent)
	{
		if (target == null || target.IsDead) return;
		var p = Player;
		var act = Action;
		if (p == null) return;

		// 한 번의 휘두르기로 같은 대상(콜라이더가 여러 개인 보스 등)에 중복 피해 방지
		int swing = act != null ? act.SwingId : -1;
		if (swing != lastSwingId) { lastSwingId = swing; hitThisSwing.Clear(); }
		int key = targetComponent != null ? targetComponent.gameObject.GetInstanceID() : target.GetHashCode();
		if (!hitThisSwing.Add(key)) return;

		int style = act != null ? act.CurrentAttackStyle : 0;
		int dmg = ComputeOutgoing(p.Atk, style, target, out bool crit);
		target.ReceiveDamage(dmg, crit, NRDamageKind.Direct);
		LastCombatTime = Time.time;

		// 근접 패시브: 3타 마무리 적중 시 보호막
		if (style == 0 && act != null && act.ComboStep == 2 && !NRHero.IsRanged) AddShieldSilent(8f);
		AfterHit(target, targetComponent, dmg, crit);
	}

	/// <summary>원거리 탄환 등 공격 판정 없이 직접 피해 (배율 적용)</summary>
	public static void PlayerHitEnemyWith(INRDamageable target, Component targetComponent, int style, float damageMul, bool forceCrit)
	{
		if (target == null || target.IsDead) return;
		var p = Player;
		if (p == null) return;
		int dmg = ComputeOutgoing(Mathf.Max(1, Mathf.RoundToInt(p.Atk * damageMul)), style, target, out bool crit);
		if (forceCrit && !crit) { crit = true; dmg = Mathf.RoundToInt(dmg * (FrenzyCrit ? 2.5f : 2f)); }
		target.ReceiveDamage(dmg, crit, NRDamageKind.Direct);
		LastCombatTime = Time.time;
		AfterHit(target, targetComponent, dmg, crit);
	}

	static void AfterHit(INRDamageable target, Component targetComponent, int dmg, bool crit)
	{

		if (targetComponent != null)
		{
			if (BurnDps > 0f || (crit && FrenzyCrit))
			{
				var st = NREnemyStatus.Get(targetComponent.gameObject);
				if (BurnDps > 0f) st.AddBurn(BurnDps, 1);
				if (crit && FrenzyCrit) st.AddBurn(Mathf.Max(6f, BurnDps), 2);
			}
			if (ChillPct > 0f) NREnemyStatus.Get(targetComponent.gameObject).Chill(ChillPct, 2f);
		}

		if (LeechPct > 0f) HealPlayer(dmg * LeechPct, false);
		NRTime.HitStop(crit ? 0.07f : 0.035f, 0.05f);
		if (crit) NRCombatFX.Shake(0.12f, 0.08f);
	}

	public static void OnEnemyKilled(Vector3 position, bool boss)
	{
		RunKills++;
		NRSave.Data.enemyKills++;
		NRSave.MarkDirty();
		if (HealOnKill > 0f) HealPlayer(HealOnKill, true);
		if (!boss) HealPlayer(NRHero.KillHeal, false); // 캐릭터 패시브
		if (Random.value < 0.75f) NRPickup.DropCoins(position, boss ? 25 : Random.Range(1, 3));
		NRCombatFX.DeathBurst(position, boss ? NRPalette.Gold : NRPalette.Pink, boss ? 40 : 14);
	}

	public static void HealPlayer(float amount, bool showNumber)
	{
		var p = Player;
		if (p == null || p.isDead || amount <= 0f) return;
		int before = p.nowHP;
		p.nowHP = Mathf.Min(p.maxHP, p.nowHP + Mathf.Max(1, Mathf.RoundToInt(amount)));
		int healed = p.nowHP - before;
		if (showNumber && healed > 0) NRCombatFX.Number(p.transform.position + Vector3.up * 0.9f, "+" + healed, NRPalette.Green, 0.9f);
	}

	public static void AddShieldSilent(float amount)
	{
		if (amount > 0f) Shield = Mathf.Max(Shield, amount);
	}

	public static void AddShield(float amount)
	{
		if (amount <= 0f) return;
		Shield = Mathf.Max(Shield, amount);
		var p = Player;
		if (p != null) NRCombatFX.Number(p.transform.position + Vector3.up * 1.0f, "보호막 " + Mathf.RoundToInt(amount), NRPalette.Shield, 0.9f);
	}

	// ---- 적 → 플레이어 ----
	/// <summary>플레이어 피해 처리 일원화. 실제로 피해가 들어갔으면 true.</summary>
	public static bool DamagePlayer(Player p, float raw, bool useArmor = true, bool fromDashable = true)
	{
		if (p == null || p.isDead) return false;
		if (GodMode) return false;
		if (NRCredits.IsPlaying) return false;
		if (Time.time < InvulnUntil) return false;
		var act = p.GetComponent<PlayerAction>();
		if (fromDashable && act != null && act.DashInvulnerable) return false; // 구르기 중에는 모든 공격 회피

		float dmg = raw;
		if (useArmor)
		{
			if (p.AR < 0) p.AR = 0;
			dmg = dmg / (1f + p.AR * 0.01f);
		}
		dmg *= 1f - Mathf.Clamp(DamageReductionPct, 0f, 0.6f);
		int total = Mathf.Max(1, (int)dmg);

		if (Shield > 0f)
		{
			float absorbed = Mathf.Min(Shield, total);
			Shield -= absorbed;
			total -= Mathf.RoundToInt(absorbed);
			NRCombatFX.Number(p.transform.position + Vector3.up * 0.8f, "흡수 " + Mathf.RoundToInt(absorbed), NRPalette.Shield, 0.8f);
		}

		LastCombatTime = Time.time;
		InvulnUntil = Time.time + 0.4f;

		if (total > 0)
		{
			p.nowHP -= total;
			NRCombatFX.Number(p.transform.position + Vector3.up * 0.8f, total.ToString(), NRPalette.Crimson, 1.0f);
			NRCombatFX.PlayerHitFlash();
			NRCombatFX.Shake(0.18f, 0.12f);
		}

		if (p.nowHP <= 0)
		{
			if (LastStandLeft > 0)
			{
				LastStandLeft--;
				p.nowHP = Mathf.Max(1, Mathf.RoundToInt(p.maxHP * LastStandPct));
				InvulnUntil = Time.time + 1.2f;
				NRUIRoot.ToastMsg("굳은 결의! 쓰러지지 않았다", NRPalette.Will);
				return true;
			}
			if (DeathDefianceLeft > 0)
			{
				DeathDefianceLeft--;
				p.nowHP = Mathf.Max(1, Mathf.RoundToInt(p.maxHP * 0.4f));
				InvulnUntil = Time.time + 1.6f;
				NRUIRoot.Banner("끈질긴 생존", "남은 부활 " + DeathDefianceLeft + "회", NRPalette.Gold, 1.2f);
				NRCombatFX.DeathBurst(p.transform.position, NRPalette.Gold, 30);
				return true;
			}
			if (!p.isDead) p.Dead();
		}
		return true;
	}

	/// <summary>플레이어 피격 판정용: 피해원 오브젝트에서 피해량 추출</summary>
	public static bool TryGetDamage(Collider2D col, out float damage, out NRDamageSource nrSource)
	{
		damage = 0f;
		nrSource = col.GetComponent<NRDamageSource>();
		if (nrSource != null) { damage = nrSource.damage; return true; }
		var far = col.GetComponent<FarATK>();
		if (far != null) { damage = far.damage; return true; }
		var matk = col.GetComponent<MonsterATK>();
		if (matk != null) { damage = matk.damage; return true; }
		return false;
	}

	/// <summary>화면에 남은 적 투사체 제거 (보스 처치/계층 이동 시)</summary>
	public static void ClearEnemyProjectiles()
	{
		foreach (var f in Object.FindObjectsOfType<FarATK>()) f.gameObject.SetActive(false);
		foreach (var m in Object.FindObjectsOfType<MonsterATK>()) Object.Destroy(m.gameObject);
		foreach (var d in Object.FindObjectsOfType<NRDamageSource>())
		{
			if (d.GetComponentInParent<NREnemy>() == null && d.GetComponentInParent<MonsterAI>() == null) Object.Destroy(d.gameObject);
		}
	}
}

// ---------------------------------------------------------------------------
// 적 상태이상: 화상(지속 피해), 둔화
// ---------------------------------------------------------------------------
public class NREnemyStatus : MonoBehaviour
{
	struct BurnStack { public float dps; public float until; }
	readonly List<BurnStack> burns = new List<BurnStack>();
	float chillPct;
	float chillUntil;
	float tickTimer;
	INRDamageable target;
	SpriteRenderer[] renderers;
	GameObject burnFx;

	public const int MaxBurnStacks = 5;

	public bool IsChilled => Time.time < chillUntil;
	public bool IsBurning => burns.Count > 0;
	public float SpeedMultiplier => IsChilled ? 1f - Mathf.Clamp(chillPct, 0f, 0.7f) : 1f;

	public static NREnemyStatus Get(GameObject go)
	{
		var s = go.GetComponent<NREnemyStatus>();
		if (s == null) s = go.AddComponent<NREnemyStatus>();
		return s;
	}

	/// <summary>대상이 없으면 1.0 반환</summary>
	public static float SpeedMul(Component c)
	{
		if (c == null) return 1f;
		var s = c.GetComponent<NREnemyStatus>();
		return s != null ? s.SpeedMultiplier : 1f;
	}

	void Awake()
	{
		target = GetComponent<INRDamageable>();
		renderers = GetComponentsInChildren<SpriteRenderer>();
	}

	public void AddBurn(float dps, int stacks)
	{
		for (int i = 0; i < stacks; i++)
		{
			if (burns.Count >= MaxBurnStacks) burns.RemoveAt(0);
			burns.Add(new BurnStack { dps = dps, until = Time.time + 3f });
		}
		if (burnFx == null)
		{
			burnFx = new GameObject("BurnFx");
			burnFx.transform.SetParent(transform, false);
			var sr = burnFx.AddComponent<SpriteRenderer>();
			sr.sprite = NRSprites.Glow;
			sr.color = NRPalette.Rage.WithAlpha(0.45f);
			NRSort.Set(sr, NRSort.Enemy, 40);
			float s = 1f / Mathf.Max(0.01f, Mathf.Abs(transform.lossyScale.x));
			burnFx.transform.localScale = Vector3.one * s * 1.2f;
		}
	}

	public void Chill(float pct, float duration)
	{
		chillPct = Mathf.Max(chillUntil > Time.time ? chillPct : 0f, pct);
		chillUntil = Time.time + duration;
	}

	void Update()
	{
		if (target == null || target.IsDead) return;

		for (int i = burns.Count - 1; i >= 0; i--)
			if (Time.time > burns[i].until) burns.RemoveAt(i);

		if (burns.Count > 0)
		{
			tickTimer += Time.deltaTime;
			if (tickTimer >= 0.5f)
			{
				tickTimer = 0f;
				float total = 0f;
				foreach (var b in burns) total += b.dps * 0.5f;
				int dmg = Mathf.Max(1, Mathf.RoundToInt(total));
				target.ReceiveDamage(dmg, false, NRDamageKind.Burn);
			}
		}
		else if (burnFx != null)
		{
			Destroy(burnFx);
			burnFx = null;
		}

		if (burnFx != null)
		{
			var sr = burnFx.GetComponent<SpriteRenderer>();
			sr.color = NRPalette.Rage.WithAlpha(0.3f + 0.2f * Mathf.Sin(Time.time * 10f));
		}
	}
}
