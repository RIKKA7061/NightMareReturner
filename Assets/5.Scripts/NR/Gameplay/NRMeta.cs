using System.Collections.Generic;
using UnityEngine;

// ============================================================================
// 영구 강화 ("기억의 책장") - 악몽 결정으로 구매, 영구 저장
// ============================================================================
public class NRUpgradeDef
{
	public string id;
	public string name;
	public string format;     // {0} = 현재 레벨 총합 수치
	public int maxLevel;
	public int[] costs;
	public float perLevel;
	public int icon;
	public Color color;

	public int Level => NRSave.GetUpgradeLevel(id);
	public bool IsMaxed => Level >= maxLevel;
	public int NextCost => IsMaxed ? 0 : costs[Mathf.Clamp(Level, 0, costs.Length - 1)];

	public string Describe(int level)
	{
		float v = perLevel * level;
		string num = Mathf.Approximately(v, Mathf.Round(v)) ? Mathf.RoundToInt(v).ToString() : v.ToString("0.#");
		return string.Format(format, num);
	}
}

public static class NRMeta
{
	public static readonly List<NRUpgradeDef> All = new List<NRUpgradeDef>
	{
		new NRUpgradeDef { id = "m_atk",    name = "단련된 주먹",   format = "공격력 +{0}",                  maxLevel = 5, costs = new[] { 10, 20, 35, 55, 80 },  perLevel = 3,  icon = 0,  color = NRPalette.Rage },
		new NRUpgradeDef { id = "m_hp",     name = "악몽 내성",     format = "최대 체력 +{0}",               maxLevel = 5, costs = new[] { 10, 20, 35, 55, 80 },  perLevel = 30, icon = 26, color = NRPalette.Crimson },
		new NRUpgradeDef { id = "m_speed",  name = "가벼운 발걸음", format = "이동 속도 +{0}%",              maxLevel = 5, costs = new[] { 15, 25, 40, 60, 85 },  perLevel = 4,  icon = 20, color = NRPalette.Anxiety },
		new NRUpgradeDef { id = "m_armor",  name = "굳은살",        format = "방어력 +{0}",                  maxLevel = 5, costs = new[] { 12, 22, 36, 56, 80 },  perLevel = 10, icon = 33, color = NRPalette.Will },
		new NRUpgradeDef { id = "m_defy",   name = "끈질긴 생존",   format = "회차마다 {0}회, 쓰러지면 체력 40%로 부활", maxLevel = 2, costs = new[] { 60, 140 }, perLevel = 1, icon = 5, color = NRPalette.Gold },
		new NRUpgradeDef { id = "m_greed",  name = "결정 수집가",   format = "악몽 결정 획득량 +{0}%",       maxLevel = 4, costs = new[] { 20, 40, 70, 110 },     perLevel = 15, icon = 31, color = NRPalette.Cyan },
		new NRUpgradeDef { id = "m_luck",   name = "감정의 공명",   format = "희귀 이상 증강 확률 +{0}%",    maxLevel = 4, costs = new[] { 25, 45, 75, 120 },     perLevel = 6,  icon = 28, color = NRPalette.RarityEpic },
		new NRUpgradeDef { id = "m_reroll", name = "망설임",        format = "회차마다 증강 다시 뽑기 {0}회", maxLevel = 3, costs = new[] { 30, 70, 130 },        perLevel = 1,  icon = 23, color = NRPalette.Sorrow },
		new NRUpgradeDef { id = "m_rest",   name = "숨 고르기",     format = "계층 이동 시 체력 {0}% 추가 회복", maxLevel = 3, costs = new[] { 20, 45, 80 },     perLevel = 10, icon = 29, color = NRPalette.Green },
		new NRUpgradeDef { id = "m_money",  name = "비상금",        format = "회차 시작 시 재화 +{0}",        maxLevel = 4, costs = new[] { 10, 25, 45, 70 },      perLevel = 20, icon = 32, color = NRPalette.Gold },
	};

	public static NRUpgradeDef Get(string id)
	{
		foreach (var d in All) if (d.id == id) return d;
		return null;
	}

	static int L(string id) => NRSave.GetUpgradeLevel(id);

	public static int BonusAtk => L("m_atk") * 3;
	public static int BonusHp => L("m_hp") * 30;
	public static float SpeedPct => L("m_speed") * 0.04f;
	public static int BonusArmor => L("m_armor") * 10;
	public static int DeathDefiance => L("m_defy");
	public static float CrystalBonus => L("m_greed") * 0.15f;
	public static float Luck => L("m_luck") * 0.06f;
	public static int Rerolls => L("m_reroll");
	public static float FloorHealPct => 0.2f + L("m_rest") * 0.10f;
	public static int StartMoney => L("m_money") * 20;

	public static bool TryBuy(NRUpgradeDef def)
	{
		if (def == null || def.IsMaxed) return false;
		int cost = def.NextCost;
		if (!NRSave.SpendCrystals(cost)) return false;
		NRSave.SetUpgradeLevel(def.id, def.Level + 1);
		NRSave.Save();
		return true;
	}
}
