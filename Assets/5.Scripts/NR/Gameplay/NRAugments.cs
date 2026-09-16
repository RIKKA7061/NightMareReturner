using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// ============================================================================
// 증강 (하데스 은총 방식): 네 가지 감정 계열 + 이중 감정(듀오)
//  - 같은 증강을 다시 고르면 레벨업(중첩)
//  - 희귀도에 따라 기본 수치 배율 증가
// ============================================================================
public enum NRFamily { Rage, Anxiety, Sorrow, Will, Duo }
public enum NRRarity { Common, Rare, Epic, Legendary }

public class NRAugmentDef
{
	public string id;
	public string name;
	public NRFamily family;
	public NRFamily duoA, duoB;
	public string format;          // {0}: 수치
	public float baseValue;
	public float perLevel;
	public float cap = float.MaxValue;
	public int maxLevel = 5;
	public int icon;
	public bool fixedValue;        // 희귀도 영향 없음
	/// <summary>수치를 NRStats 합산값에 더함 (v: 표시 수치)</summary>
	public Action<float> aggregate;
	/// <summary>획득/레벨업 즉시 적용 (체력/방어력 등 기본 스탯 변경). (새 수치, 이전 수치)</summary>
	public Action<float, float> onGain;

	public bool IsDuo => family == NRFamily.Duo;

	public float Value(int level, NRRarity rarity)
	{
		float mul = fixedValue ? 1f : NRAugments.RarityMultiplier(rarity);
		return Mathf.Min(cap, baseValue * mul + perLevel * Mathf.Max(0, level - 1));
	}

	public string Describe(float value)
	{
		string num = Mathf.Approximately(value, Mathf.Round(value)) ? Mathf.RoundToInt(value).ToString() : value.ToString("0.#");
		return string.Format(format, "<color=#" + NRPalette.ToHex(NRPalette.Gold) + ">" + num + "</color>");
	}

	public string DescribePlain(float value)
	{
		string num = Mathf.Approximately(value, Mathf.Round(value)) ? Mathf.RoundToInt(value).ToString() : value.ToString("0.#");
		return string.Format(format, num);
	}
}

public class NRAugmentOwned
{
	public NRAugmentDef def;
	public int level;
	public NRRarity rarity;
	public float Value => def.Value(level, rarity);
}

public class NRAugmentOffer
{
	public NRAugmentDef def;
	public NRRarity rarity;
	public int newLevel;
	public NRAugmentOwned owned;   // 업그레이드면 기존 보유 정보
	public bool IsUpgrade => owned != null;
	public float NewValue => def.Value(newLevel, rarity);
	public float OldValue => owned != null ? owned.Value : 0f;
}

public static class NRAugments
{
	public static readonly List<NRAugmentOwned> Owned = new List<NRAugmentOwned>();
	public static event Action OnChanged;
	public static int RerollsLeft;

	public static string FamilyName(NRFamily f)
	{
		switch (f)
		{
			case NRFamily.Rage: return "분노";
			case NRFamily.Anxiety: return "불안";
			case NRFamily.Sorrow: return "슬픔";
			case NRFamily.Will: return "의지";
			default: return "이중 감정";
		}
	}

	public static Color FamilyColor(NRFamily f)
	{
		switch (f)
		{
			case NRFamily.Rage: return NRPalette.Rage;
			case NRFamily.Anxiety: return NRPalette.Anxiety;
			case NRFamily.Sorrow: return NRPalette.Sorrow;
			case NRFamily.Will: return NRPalette.Will;
			default: return NRPalette.Gold;
		}
	}

	public static string RarityName(NRRarity r)
	{
		switch (r)
		{
			case NRRarity.Rare: return "희귀";
			case NRRarity.Epic: return "영웅";
			case NRRarity.Legendary: return "전설";
			default: return "일반";
		}
	}

	public static Color RarityColor(NRRarity r)
	{
		switch (r)
		{
			case NRRarity.Rare: return NRPalette.RarityRare;
			case NRRarity.Epic: return NRPalette.RarityEpic;
			case NRRarity.Legendary: return NRPalette.RarityLegendary;
			default: return NRPalette.RarityCommon;
		}
	}

	public static float RarityMultiplier(NRRarity r)
	{
		switch (r)
		{
			case NRRarity.Rare: return 1.3f;
			case NRRarity.Epic: return 1.6f;
			case NRRarity.Legendary: return 2.0f;
			default: return 1f;
		}
	}

	// ---------------------------------------------------------------------
	// 정의
	// ---------------------------------------------------------------------
	static Player P => NRStats.Player;

	public static readonly List<NRAugmentDef> All = new List<NRAugmentDef>
	{
		// ===== 분노 =====
		new NRAugmentDef { id = "r_jab", name = "분노의 주먹", family = NRFamily.Rage, icon = 0,
			format = "기본 공격 피해 +{0}%", baseValue = 40, perLevel = 15, aggregate = v => NRStats.JabDmgPct += v / 100f },
		new NRAugmentDef { id = "r_skill", name = "격앙된 일격", family = NRFamily.Rage, icon = 1,
			format = "특수 공격(우클릭) 피해 +{0}%", baseValue = 50, perLevel = 20, aggregate = v => NRStats.SkillDmgPct += v / 100f },
		new NRAugmentDef { id = "r_ult", name = "폭주", family = NRFamily.Rage, icon = 2,
			format = "궁극기(R) 피해 +{0}%, 궁극기 재사용 대기 -10%", baseValue = 40, perLevel = 15,
			aggregate = v => { NRStats.UltDmgPct += v / 100f; NRStats.UltCdPct += 0.10f; } },
		new NRAugmentDef { id = "r_burn", name = "불씨", family = NRFamily.Rage, icon = 3,
			format = "적중한 적에게 화상: 초당 {0} 피해 (3초, 최대 5중첩)", baseValue = 6, perLevel = 3, aggregate = v => NRStats.BurnDps += v },
		new NRAugmentDef { id = "r_berserk", name = "역린", family = NRFamily.Rage, icon = 4,
			format = "체력이 50% 이하일 때 주는 피해 +{0}%", baseValue = 30, perLevel = 10, aggregate = v => NRStats.BerserkPct += v / 100f },

		// ===== 불안 =====
		new NRAugmentDef { id = "a_speed", name = "초조한 발걸음", family = NRFamily.Anxiety, icon = 12,
			format = "이동 속도 +{0}%", baseValue = 12, perLevel = 5, cap = 60, aggregate = v => NRStats.MoveSpeedPct += v / 100f },
		new NRAugmentDef { id = "a_dash", name = "공황 질주", family = NRFamily.Anxiety, icon = 13,
			format = "구르기 재사용 대기 -{0}%, 구르는 동안 무적", baseValue = 25, perLevel = 8, cap = 70,
			aggregate = v => { NRStats.DashCdPct += v / 100f; NRStats.DashInvuln = true; } },
		new NRAugmentDef { id = "a_crit", name = "신경과민", family = NRFamily.Anxiety, icon = 14,
			format = "치명타 확률 +{0}% (치명타 피해 2배)", baseValue = 10, perLevel = 4, cap = 60, aggregate = v => NRStats.CritChance += v / 100f },
		new NRAugmentDef { id = "a_haste", name = "떨리는 손", family = NRFamily.Anxiety, icon = 15,
			format = "기본 공격 속도 +{0}%", baseValue = 15, perLevel = 6, cap = 80, aggregate = v => NRStats.AttackSpeedPct += v / 100f },
		new NRAugmentDef { id = "a_shock", name = "잔상", family = NRFamily.Anxiety, icon = 16,
			format = "구르기가 끝나면 주변에 {0} 피해 충격파", baseValue = 30, perLevel = 12, aggregate = v => NRStats.DashShockDmg += v },

		// ===== 슬픔 =====
		new NRAugmentDef { id = "s_chill", name = "가라앉는 마음", family = NRFamily.Sorrow, icon = 18,
			format = "적중한 적의 이동 속도 -{0}% (2초)", baseValue = 30, perLevel = 8, cap = 70, aggregate = v => NRStats.ChillPct = Mathf.Max(NRStats.ChillPct, v / 100f) },
		new NRAugmentDef { id = "s_veil", name = "눈물의 장막", family = NRFamily.Sorrow, icon = 19,
			format = "받는 피해 -{0}%", baseValue = 10, perLevel = 4, cap = 50, aggregate = v => NRStats.DamageReductionPct += v / 100f },
		new NRAugmentDef { id = "s_execute", name = "깊은 우울", family = NRFamily.Sorrow, icon = 21,
			format = "체력 30% 이하인 적에게 피해 +{0}%", baseValue = 40, perLevel = 15, aggregate = v => NRStats.ExecutePct += v / 100f },
		new NRAugmentDef { id = "s_mourn", name = "애도", family = NRFamily.Sorrow, icon = 22,
			format = "적을 처치하면 체력 {0} 회복", baseValue = 6, perLevel = 3, aggregate = v => NRStats.HealOnKill += v },
		new NRAugmentDef { id = "s_calm", name = "고요", family = NRFamily.Sorrow, icon = 24,
			format = "특수 공격·궁극기 재사용 대기 -{0}%", baseValue = 15, perLevel = 5, cap = 55, aggregate = v => NRStats.SkillCdPct += v / 100f },

		// ===== 의지 =====
		new NRAugmentDef { id = "w_heart", name = "강인한 심장", family = NRFamily.Will, icon = 26,
			format = "최대 체력 +{0} (획득 시 같은 만큼 회복)", baseValue = 40, perLevel = 20,
			onGain = (nv, ov) => { var p = P; if (p == null) return; int d = Mathf.RoundToInt(nv - ov); p.maxHP += d; p.nowHP = Mathf.Min(p.maxHP, p.nowHP + d); } },
		new NRAugmentDef { id = "w_shield", name = "버팀목", family = NRFamily.Will, icon = 27,
			format = "방에 들어설 때마다 보호막 {0}", baseValue = 30, perLevel = 15, aggregate = v => NRStats.RoomShield += v },
		new NRAugmentDef { id = "w_leech", name = "재기", family = NRFamily.Will, icon = 29,
			format = "입힌 피해의 {0}%만큼 체력 회복", baseValue = 3, perLevel = 1, cap = 12, aggregate = v => NRStats.LeechPct += v / 100f },
		new NRAugmentDef { id = "w_laststand", name = "굳은 결의", family = NRFamily.Will, icon = 30,
			format = "계층마다 1회, 치명상을 입으면 체력 {0}%로 버팀", baseValue = 30, perLevel = 10, cap = 80,
			aggregate = v => NRStats.LastStandPct = Mathf.Max(NRStats.LastStandPct, v / 100f),
			onGain = (nv, ov) => { if (ov <= 0f) NRStats.LastStandLeft = Mathf.Max(NRStats.LastStandLeft, 1); } },
		new NRAugmentDef { id = "w_armor", name = "각오", family = NRFamily.Will, icon = 33,
			format = "방어력 +{0}", baseValue = 20, perLevel = 10,
			onGain = (nv, ov) => { var p = P; if (p != null) p.AR += Mathf.RoundToInt(nv - ov); } },

		// ===== 이중 감정 (전설) =====
		new NRAugmentDef { id = "d_frenzy", name = "광란", family = NRFamily.Duo, duoA = NRFamily.Rage, duoB = NRFamily.Anxiety, icon = 6, fixedValue = true, maxLevel = 1,
			format = "치명타가 화상 2중첩을 남기고 치명타 피해가 2.5배", baseValue = 0, aggregate = v => NRStats.FrenzyCrit = true },
		new NRAugmentDef { id = "d_frost", name = "얼어붙은 분노", family = NRFamily.Duo, duoA = NRFamily.Rage, duoB = NRFamily.Sorrow, icon = 7, fixedValue = true, maxLevel = 3,
			format = "둔화된 적에게 주는 피해 +{0}%", baseValue = 35, perLevel = 15, aggregate = v => NRStats.FrostDmgPct += v / 100f },
		new NRAugmentDef { id = "d_resolve", name = "흔들리지 않는 걸음", family = NRFamily.Duo, duoA = NRFamily.Anxiety, duoB = NRFamily.Will, icon = 8, fixedValue = true, maxLevel = 3,
			format = "구를 때마다 보호막 {0} 획득", baseValue = 12, perLevel = 6, aggregate = v => NRStats.DashShield += v },
		new NRAugmentDef { id = "d_requiem", name = "진혼곡", family = NRFamily.Duo, duoA = NRFamily.Sorrow, duoB = NRFamily.Will, icon = 9, fixedValue = true, maxLevel = 3,
			format = "보호막이 있는 동안 주는 피해 +{0}%", baseValue = 30, perLevel = 10, aggregate = v => NRStats.RequiemPct += v / 100f },
	};

	public static NRAugmentDef Get(string id) => All.FirstOrDefault(a => a.id == id);
	public static NRAugmentOwned GetOwned(string id) => Owned.FirstOrDefault(o => o.def.id == id);
	public static bool OwnsFamily(NRFamily f) => Owned.Any(o => o.def.family == f);
	public static int CountFamily(NRFamily f) => Owned.Count(o => o.def.family == f);

	public static void ResetRun()
	{
		Owned.Clear();
		RerollsLeft = NRMeta.Rerolls;
		Recompute();
	}

	public static void Recompute()
	{
		NRStats.ClearAggregates();
		foreach (var o in Owned) o.def.aggregate?.Invoke(o.Value);
		OnChanged?.Invoke();
	}

	/// <summary>선택지 생성. family가 null이면 무작위 계열.</summary>
	public static List<NRAugmentOffer> GenerateOffers(NRFamily? family, int count, bool bossReward)
	{
		var fam = family ?? (NRFamily)UnityEngine.Random.Range(0, 4);
		var offers = new List<NRAugmentOffer>();

		var pool = All.Where(d => d.family == fam && IsAvailable(d)).OrderBy(_ => UnityEngine.Random.value).ToList();

		// 이중 감정: 두 계열을 모두 가지고 있고 한쪽이 이번 계열일 때 확률 등장
		var duos = All.Where(d => d.IsDuo && IsAvailable(d) && (d.duoA == fam || d.duoB == fam) && OwnsFamily(d.duoA) && OwnsFamily(d.duoB)).ToList();
		float duoChance = bossReward ? 0.6f : 0.3f;
		if (duos.Count > 0 && UnityEngine.Random.value < duoChance)
			offers.Add(MakeOffer(duos[UnityEngine.Random.Range(0, duos.Count)], bossReward, true));

		foreach (var d in pool)
		{
			if (offers.Count >= count) break;
			offers.Add(MakeOffer(d, bossReward, false));
		}

		if (offers.Count < count)
		{
			var others = All.Where(d => !d.IsDuo && d.family != fam && IsAvailable(d) && offers.All(o => o.def != d)).OrderBy(_ => UnityEngine.Random.value);
			foreach (var d in others)
			{
				if (offers.Count >= count) break;
				offers.Add(MakeOffer(d, bossReward, false));
			}
		}
		return offers;
	}

	public static NRFamily LastOfferedFamily;

	static bool IsAvailable(NRAugmentDef d)
	{
		var o = GetOwned(d.id);
		return o == null || o.level < d.maxLevel;
	}

	static NRAugmentOffer MakeOffer(NRAugmentDef d, bool bossReward, bool forceLegendary)
	{
		var owned = GetOwned(d.id);
		var rarity = forceLegendary || d.IsDuo ? NRRarity.Legendary : RollRarity(bossReward);
		if (owned != null && owned.rarity > rarity) rarity = owned.rarity;
		return new NRAugmentOffer
		{
			def = d,
			rarity = rarity,
			owned = owned,
			newLevel = owned != null ? owned.level + 1 : 1
		};
	}

	static NRRarity RollRarity(bool boss)
	{
		float luck = NRMeta.Luck;
		float legendary = 0.02f + luck * 0.25f + (boss ? 0.06f : 0f);
		float epic = 0.10f + luck * 0.5f + (boss ? 0.15f : 0f);
		float rare = 0.28f + luck + (boss ? 0.2f : 0f);
		float r = UnityEngine.Random.value;
		if (r < legendary) return NRRarity.Legendary;
		if (r < legendary + epic) return NRRarity.Epic;
		if (r < legendary + epic + rare) return NRRarity.Rare;
		return NRRarity.Common;
	}

	public static void Acquire(NRAugmentOffer offer)
	{
		float oldValue = 0f;
		var owned = GetOwned(offer.def.id);
		if (owned == null)
		{
			owned = new NRAugmentOwned { def = offer.def, level = 1, rarity = offer.rarity };
			Owned.Add(owned);
		}
		else
		{
			oldValue = owned.Value;
			owned.level = offer.newLevel;
			if (offer.rarity > owned.rarity) owned.rarity = offer.rarity;
		}
		offer.def.onGain?.Invoke(owned.Value, oldValue);
		NRSave.Data.augmentsPicked++;
		NRSave.MarkDirty();
		Recompute();
	}
}
