using System.Collections.Generic;
using UnityEngine;

// ============================================================================
// 각성의 방: 감정 4종 중 하나를 골라 먹는다 (시작 패시브)
//  - 기존 클래스 구슬 하나를 감정별 구슬 4개로 교체
//  - 가까이 가거나 클릭하면 획득 (충돌 판정 대신 거리 판정 → 위/아래 어긋남 없음)
// ============================================================================
public static class NRAwakening
{
	public const float AutoPickRadius = 1.3f;   // 이보다 가까워지면 자동 획득 (가장 가까운 구슬 하나만)
	public const float KeyPickRadius = 2.8f;    // E 키 인식 범위 (클릭은 화면에서 바로)

	public static readonly List<NRAugmentDef> Choices = new List<NRAugmentDef>
	{
		new NRAugmentDef { id = "awk_rage", name = "타오르는 각성", family = NRFamily.Rage, icon = 0, fixedValue = true, maxLevel = 1,
			format = "기본 공격 피해 +{0}%", baseValue = 25,
			aggregate = v => { NRStats.JabDmgPct += v / 100f; NRStats.AttackSpeedPct += 0.1f; } },
		new NRAugmentDef { id = "awk_anxiety", name = "떨리는 각성", family = NRFamily.Anxiety, icon = 12, fixedValue = true, maxLevel = 1,
			format = "이동 속도 +{0}%, 구르기 재사용 대기 -20%", baseValue = 15,
			aggregate = v => { NRStats.MoveSpeedPct += v / 100f; NRStats.DashCdPct += 0.20f; } },
		new NRAugmentDef { id = "awk_sorrow", name = "잠긴 각성", family = NRFamily.Sorrow, icon = 20, fixedValue = true, maxLevel = 1,
			format = "받는 피해 -{0}%, 방에 들어설 때 보호막 20", baseValue = 12,
			aggregate = v => { NRStats.DamageReductionPct += v / 100f; NRStats.RoomShield += 20f; } },
		new NRAugmentDef { id = "awk_will", name = "굳은 각성", family = NRFamily.Will, icon = 26, fixedValue = true, maxLevel = 1,
			format = "최대 체력 +{0}, 적 처치 시 체력 회복 +3", baseValue = 120,
			aggregate = v => NRStats.HealOnKill += 3f,
			onGain = (nv, ov) => { var p = NRStats.Player; if (p == null) return; int d = Mathf.RoundToInt(nv - ov); p.maxHP += d; p.nowHP = Mathf.Min(p.maxHP, p.nowHP + d); } },
	};

	static readonly List<NRAwakeningOrb> spawned = new List<NRAwakeningOrb>();

	/// <summary>씬의 클래스 구슬을 감정 구슬 4개로 교체</summary>
	public static void Setup(TouchItems classOrb)
	{
		if (classOrb == null) return;
		Clear();
		Vector3 center = classOrb.transform.position;
		var portal = classOrb.portal;
		classOrb.gameObject.SetActive(false);

		for (int i = 0; i < Choices.Count; i++)
		{
			float t = i - (Choices.Count - 1) * 0.5f;
			Vector3 pos = center + new Vector3(t * 3.2f, Mathf.Abs(t) * 0.4f, 0);
			spawned.Add(NRAwakeningOrb.Create(Choices[i], pos, portal));
		}
	}

	/// <summary>실수로 옆 구슬을 먹지 않도록, 자동 획득은 가장 가까운 하나만</summary>
	public static NRAwakeningOrb Nearest(Vector2 from)
	{
		NRAwakeningOrb best = null;
		float bestDist = float.MaxValue;
		foreach (var o in spawned)
		{
			if (o == null) continue;
			float d = Vector2.Distance(from, o.transform.position);
			if (d < bestDist) { bestDist = d; best = o; }
		}
		return best;
	}

	public static void Clear()
	{
		foreach (var o in spawned) if (o != null) Object.Destroy(o.gameObject);
		spawned.Clear();
	}

	/// <summary>하나를 고르면 나머지는 사라진다</summary>
	public static void Picked(NRAwakeningOrb chosen, GameObject portal)
	{
		NRAugments.Acquire(new NRAugmentOffer { def = chosen.def, rarity = NRRarity.Common, newLevel = 1 });
		Player.round += 1; // 기존 클래스 구슬이 주던 구슬
		if (portal != null) portal.SetActive(true);

		NRCombatFX.Number(chosen.transform.position + Vector3.up * 0.8f, chosen.def.name, NRAugments.FamilyColor(chosen.def.family), 1.2f);
		NRCombatFX.DeathBurst(chosen.transform.position, NRAugments.FamilyColor(chosen.def.family), 20);
		NRAudio.PlaySfx("crystal", 0.6f);

		Clear();
		NRClassAwakening.Show(() => NRAugmentSelect.Show(null, false, "첫 번째 감정", null));
	}
}

public class NRAwakeningOrb : MonoBehaviour
{
	public NRAugmentDef def;
	GameObject portal;
	Player player;
	Transform art;
	float born;
	bool taken;

	public static NRAwakeningOrb Create(NRAugmentDef def, Vector3 pos, GameObject portal)
	{
		var go = new GameObject("NR Awakening Orb - " + def.id);
		go.transform.position = pos;
		var orb = go.AddComponent<NRAwakeningOrb>();
		orb.def = def;
		orb.portal = portal;
		Color color = NRAugments.FamilyColor(def.family);

		var artGo = new GameObject("Art");
		artGo.transform.SetParent(go.transform, false);
		orb.art = artGo.transform;

		var glow = new GameObject("Glow").AddComponent<SpriteRenderer>();
		glow.transform.SetParent(artGo.transform, false);
		glow.sprite = NRSprites.Glow;
		glow.color = color.WithAlpha(0.5f);
		glow.transform.localScale = new Vector3(2.4f, 1.8f, 1f);
		NRSort.Set(glow, NRSort.Floor, 45);

		var body = new GameObject("Orb").AddComponent<SpriteRenderer>();
		body.transform.SetParent(artGo.transform, false);
		body.sprite = NRSprites.Orb;
		body.color = color;
		body.transform.localScale = Vector3.one * 1.3f;
		NRSort.Set(body, NRSort.Top, 60);

		var icon = new GameObject("Icon").AddComponent<SpriteRenderer>();
		icon.transform.SetParent(artGo.transform, false);
		icon.sprite = NRSprites.Icon(def.icon);
		icon.color = Color.white;
		icon.transform.localPosition = new Vector3(0, 1.5f, 0);
		icon.transform.localScale = Vector3.one * 0.9f;
		NRSort.Set(icon, NRSort.Top, 61);

		var it = NRInteractable.Attach(go, def.name, def.DescribePlain(def.Value(1, NRRarity.Common)), color, "클릭 / " + NRControls.Keys.talk);
		it.showRadius = 30f;
		it.interactRadius = NRAwakening.KeyPickRadius;
		it.onInteract = orb.Take;
		it.Attention(6f);
		NRWaypoint.Add(go.transform, def.name, color, () => Player.gameRound == 1, 1);
		return orb;
	}

	void Start()
	{
		born = Time.time;
		player = FindObjectOfType<Player>();
	}

	void Update()
	{
		if (art != null) art.localPosition = new Vector3(0, 0.35f + Mathf.Sin((Time.time - born) * 2.2f) * 0.18f, 0);
		if (taken || NRUIState.IsGameplayBlocked) return;
		if (player == null) player = FindObjectOfType<Player>();
		if (player == null || player.isDead) return;
		float d = Vector2.Distance(player.transform.position, transform.position);
		if (d <= NRAwakening.AutoPickRadius && NRAwakening.Nearest(player.transform.position) == this) Take();
	}

	public void Take()
	{
		if (taken) return;
		taken = true;
		NRAwakening.Picked(this, portal);
	}
}
