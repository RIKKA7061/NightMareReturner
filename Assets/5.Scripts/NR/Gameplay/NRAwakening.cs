using System.Collections.Generic;
using UnityEngine;

// ============================================================================
// 각성의 방: 감정 4종 중 하나를 골라 먹는다 (시작 패시브)
//  - 기존 클래스 구슬 하나를 감정별 구슬 4개로 교체
//  - 가까이 가거나 클릭하면 획득 (충돌 판정 대신 거리 판정 → 위/아래 어긋남 없음)
// ============================================================================
public static class NRAwakening
{
	public const float AutoPickRadius = 0.9f;   // 이보다 가까워지면 자동 획득 (가장 가까운 구슬 하나만)
	public const float KeyPickRadius = 2.2f;    // E 키 인식 범위 (클릭은 화면에서 바로)

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

	/// <summary>원래 단상을 가운데로 두고 2x2로 모아 둔다 (한 줄로 늘어놓으면 방 밖으로 나가 잘린다)</summary>
	static readonly Vector3[] Slots =
	{
		new Vector3(-1.3f, 0.65f, 0f),
		new Vector3(1.3f, 0.65f, 0f),
		new Vector3(-1.3f, -0.65f, 0f),
		new Vector3(1.3f, -0.65f, 0f),
	};

	/// <summary>씬의 클래스 구슬을 감정 구슬 4개로 교체 (단상도 같이 놓는다)</summary>
	public static void Setup(TouchItems classOrb)
	{
		if (classOrb == null) return;
		Clear();
		Vector3 center = classOrb.transform.position;
		var portal = classOrb.portal;
		var orbArt = classOrb.GetComponentInChildren<SpriteRenderer>();
		Transform pedestal = FindPedestal(classOrb.transform);
		Vector3 orbLift = pedestal != null ? center - pedestal.position : Vector3.zero;

		classOrb.gameObject.SetActive(false);

		Vector3 baseFoot = pedestal != null ? pedestal.position : center;
		for (int i = 0; i < Choices.Count; i++)
		{
			Vector3 foot = baseFoot + Slots[i];
			if (pedestal != null)
			{
				if (i == 0) pedestal.position = foot;       // 원래 단상을 첫 자리로
				else spawned.Add(ClonePedestal(pedestal, foot));
			}
			spawned.Add(NRAwakeningOrb.Create(Choices[i], foot + orbLift, portal, orbArt));
		}
	}

	/// <summary>구슬이 놓여 있던 단상(기둥 프리팹)</summary>
	static Transform FindPedestal(Transform orb)
	{
		var parent = orb.parent;
		if (parent == null) return null;
		foreach (Transform c in parent)
			if (c != orb && c.name.StartsWith("prop007")) return c;
		return null;
	}

	static NRAwakeningOrb ClonePedestal(Transform pedestal, Vector3 pos)
	{
		var copy = Object.Instantiate(pedestal.gameObject, pos, pedestal.rotation, pedestal.parent);
		copy.name = "NR Awakening Pedestal";
		foreach (var col in copy.GetComponentsInChildren<Collider2D>(true)) col.enabled = false;
		var holder = copy.AddComponent<NRAwakeningOrb>();
		holder.pedestalOnly = true;
		return holder;
	}

	/// <summary>실수로 옆 구슬을 먹지 않도록, 자동 획득은 가장 가까운 하나만</summary>
	public static NRAwakeningOrb Nearest(Vector2 from)
	{
		NRAwakeningOrb best = null;
		float bestDist = float.MaxValue;
		foreach (var o in spawned)
		{
			if (o == null || o.pedestalOnly) continue;
			float d = Vector2.Distance(from, o.transform.position);
			if (d < bestDist) { bestDist = d; best = o; }
		}
		return best;
	}

	public static void Clear(bool pedestalsToo = true)
	{
		for (int i = spawned.Count - 1; i >= 0; i--)
		{
			var o = spawned[i];
			if (o != null && o.pedestalOnly && !pedestalsToo) continue; // 단상은 장식으로 남긴다
			if (o != null) Object.Destroy(o.gameObject);
			spawned.RemoveAt(i);
		}
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

		Clear(false);
		NRClassAwakening.Show(() => NRAugmentSelect.Show(null, false, "첫 번째 감정", null));
	}
}

public class NRAwakeningOrb : MonoBehaviour
{
	public NRAugmentDef def;
	public bool pedestalOnly;
	GameObject portal;
	Player player;
	Transform art;
	float born;
	bool taken;

	public static NRAwakeningOrb Create(NRAugmentDef def, Vector3 pos, GameObject portal, SpriteRenderer source)
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

		// 기존 클래스 구슬 그림 그대로 사용 (없으면 기본 구슬)
		var body = new GameObject("Orb").AddComponent<SpriteRenderer>();
		body.transform.SetParent(artGo.transform, false);
		if (source != null && source.sprite != null)
		{
			body.sprite = source.sprite;
			body.color = source.color;
			body.transform.localScale = source.transform.lossyScale;
			NRSort.Set(body, SortingLayer.IDToName(source.sortingLayerID), source.sortingOrder + 1);
		}
		else
		{
			body.sprite = NRSprites.Orb;
			body.color = color;
			body.transform.localScale = Vector3.one * 1.3f;
			NRSort.Set(body, NRSort.Top, 60);
		}

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
		if (pedestalOnly) return;
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
