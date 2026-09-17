using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// ============================================================================
// 상호작용 표시: 문/침대/회오리/구슬/책장/NPC
//  - 은은한 빛(항상) + 가까이 가거나 마우스를 올리면 이름표와 화살표
//  - 방 클리어 직후 등 중요한 순간에만 강하게 깜빡임
//  - 이름표/화살표는 가장 위 정렬 레이어 → 벽에 가려지지 않음
//  - 집 안에서는 이름표를 대상 위치에 겹쳐 표시 (화면 위로 잘리지 않도록)
// ============================================================================
public class NRInteractable : MonoBehaviour
{
	public string label = "";
	public string sublabel = "";
	public string hint = "클릭";
	public Color color = NRPalette.Pink;
	public float showRadius = 4.5f;
	public float interactRadius = 1.8f;
	public Action onInteract;
	public bool requireHome;
	public bool clickOnly;            // E 키로는 반응하지 않음 (NPC: PlayerAction이 E를 처리)
	public bool useColliderBounds;    // 그림 대신 콜라이더 크기 기준 (말풍선 등 큰 자식 그림 무시)

	static readonly List<NRInteractable> active = new List<NRInteractable>();

	Transform visualRoot;
	SpriteRenderer glow;
	SpriteRenderer arrow;
	TextMeshPro text;
	Bounds bounds;
	float attentionUntil;
	float born;
	Player player;
	float show;

	public static NRInteractable Attach(GameObject go, string label, string sublabel, Color color, string hint = "클릭")
	{
		var it = go.GetComponent<NRInteractable>();
		if (it == null) it = go.AddComponent<NRInteractable>();
		it.label = label;
		it.sublabel = sublabel;
		it.color = color;
		it.hint = hint;
		return it;
	}

	void Start()
	{
		born = Time.time;
		player = FindObjectOfType<Player>();
		active.Add(this);
		ComputeBounds();

		visualRoot = new GameObject("NR Interact FX").transform;
		visualRoot.SetParent(null, false);

		glow = new GameObject("Glow").AddComponent<SpriteRenderer>();
		glow.transform.SetParent(visualRoot, false);
		glow.sprite = NRSprites.Glow;
		NRSort.Set(glow, NRSort.Floor, 50);
		float gs = Mathf.Clamp(Mathf.Max(bounds.size.x, bounds.size.y) * 1.4f, 1.2f, 4.5f);
		glow.transform.localScale = new Vector3(gs, gs * 0.8f, 1f);

		arrow = new GameObject("Arrow").AddComponent<SpriteRenderer>();
		arrow.transform.SetParent(visualRoot, false);
		arrow.sprite = NRSprites.ArrowDown;
		NRSort.Set(arrow, NRSort.Top, 401);
		arrow.transform.localScale = Vector3.one * 2.2f;

		var tgo = new GameObject("Label");
		tgo.transform.SetParent(visualRoot, false);
		tgo.AddComponent<NRKeepFont>();
		text = tgo.AddComponent<TextMeshPro>();
		if (NRFont.Asset != null) NRFont.Style(text, NRTextFx.OutlineShadow);
		text.alignment = TextAlignmentOptions.Bottom;
		text.enableWordWrapping = false;
		text.fontSize = 3.2f;
		text.rectTransform.sizeDelta = new Vector2(8, 2);
		text.rectTransform.pivot = new Vector2(0.5f, 0f);
		NRSort.Set(text.GetComponent<MeshRenderer>(), NRSort.Top, 402);

		// 방금 생긴 문/보상은 주목
		if (Time.timeSinceLevelLoad > 2f) Attention(2.4f);
	}

	void ComputeBounds()
	{
		bool has = false;
		if (!useColliderBounds)
		{
			foreach (var r in GetComponentsInChildren<SpriteRenderer>())
			{
				if (r.transform.IsChildOf(transform) && r.sprite != null && (r.enabled || r.GetComponentInParent<NRPortalVisual>() != null) && r.gameObject.name != "Glow")
				{
					if (!has) { bounds = r.bounds; has = true; }
					else bounds.Encapsulate(r.bounds);
				}
			}
		}
		if (!has)
		{
			foreach (var c in GetComponents<Collider2D>())
			{
				if (!has) { bounds = c.bounds; has = true; }
				else bounds.Encapsulate(c.bounds);
			}
		}
		if (!has)
		{
			foreach (var c in GetComponentsInChildren<Collider2D>())
			{
				if (!has) { bounds = c.bounds; has = true; }
				else bounds.Encapsulate(c.bounds);
			}
		}
		if (!has) bounds = new Bounds(transform.position, Vector3.one);
		if (bounds.size.magnitude > 12f) bounds = new Bounds(transform.position, Vector3.one * 1.5f);
	}

	public void Attention(float seconds)
	{
		attentionUntil = Time.time + seconds;
	}

	bool Contains(Vector3 mouseWorld)
	{
		var b = bounds;
		b.Expand(new Vector3(0.3f, 0.3f, 100f));
		return b.Contains(new Vector3(mouseWorld.x, mouseWorld.y, bounds.center.z));
	}

	/// <summary>마우스를 품고 있는 대상 중 마우스에 가장 가까운 하나 (거리가 같으면 ID가 작은 쪽).</summary>
	NRInteractable ClosestHovered(Vector3 mouseWorld)
	{
		var best = this;
		float bestDist = Vector2.Distance(mouseWorld, bounds.center);
		foreach (var o in active)
		{
			if (o == null || o == this || !o.isActiveAndEnabled) continue;
			if (o.visualRoot == null || !o.visualRoot.gameObject.activeSelf) continue;
			if (!o.Contains(mouseWorld)) continue;
			float d = Vector2.Distance(mouseWorld, o.bounds.center);
			if (d < bestDist || (d == bestDist && o.GetInstanceID() < best.GetInstanceID())) { bestDist = d; best = o; }
		}
		return best;
	}

	/// <summary>플레이어에게 가장 가까운 이름표 하나를 고른다 (거리가 같으면 ID가 작은 쪽).</summary>
	NRInteractable Closest(Vector3 myCenter)
	{
		if (player == null) return this;
		Vector2 p = player.transform.position;
		var best = this;
		float bestDist = Vector2.Distance(p, myCenter);
		foreach (var o in active)
		{
			if (o == null || o == this || !o.isActiveAndEnabled) continue;
			if (o.visualRoot == null || !o.visualRoot.gameObject.activeSelf) continue;
			float d = Vector2.Distance(p, o.bounds.center);
			if (d < bestDist || (d == bestDist && o.GetInstanceID() < best.GetInstanceID())) { bestDist = d; best = o; }
		}
		return best;
	}

	void OnDisable()
	{
		if (visualRoot != null) visualRoot.gameObject.SetActive(false);
	}

	void OnEnable()
	{
		if (visualRoot != null) visualRoot.gameObject.SetActive(true);
	}

	void OnDestroy()
	{
		active.Remove(this);
		if (visualRoot != null) Destroy(visualRoot.gameObject);
	}

	void LateUpdate()
	{
		if (visualRoot == null) return;
		if (player == null) player = FindObjectOfType<Player>();

		// 움직이는 오브젝트 대비 주기적으로 위치 갱신
		if (Time.frameCount % 20 == 0) ComputeBounds();

		bool allowed = !requireHome || Player.gameRound == 0;
		bool hidden = !allowed || NRUIState.AnyOpen || NRCredits.IsPlaying || (player != null && player.isDead);
		visualRoot.gameObject.SetActive(!hidden);
		if (hidden) return;

		Vector3 center = bounds.center;
		float dist = player != null ? Vector2.Distance(player.transform.position, center) : 999f;
		bool hover = false;
		var cam = Camera.main;
		if (cam != null)
		{
			Vector3 m = cam.ScreenToWorldPoint(NRInput.MousePosition);
			// 클릭 영역이 겹치면 마우스에 더 가까운 쪽만 반응한다 (집 책상 ↔ 현관문)
			hover = Contains(m) && ClosestHovered(m) == this;
		}

		bool near = dist < showRadius;
		bool attention = Time.time < attentionUntil;
		float target = hover || near ? 1f : 0f;
		show = Mathf.MoveTowards(show, target, Time.deltaTime * 5f);

		// 빛: 멀리 있는 오브젝트(씬에 배치된 문 템플릿 등)는 빛나지 않도록 거리 감쇠
		float proximity = Mathf.Clamp01(1f - (dist - 7f) / 5f);
		float basePulse = (0.22f + 0.08f * Mathf.Sin(Time.time * 2.5f + born)) * proximity;
		float att = attention ? 0.35f * Mathf.Abs(Mathf.Sin((attentionUntil - Time.time) * Mathf.PI * 1.25f)) : 0f;
		glow.transform.position = new Vector3(center.x, bounds.min.y + bounds.size.y * 0.35f, 0);
		glow.color = color.WithAlpha(basePulse + 0.25f * show + att);

		bool hints = NRSettings.InteractHints;
		bool homeInside = Player.gameRound == 0 && NRGame.Instance != null && NRGame.Instance.CurrentScene == NRGame.SceneDungeon;
		float bob = Mathf.Sin(Time.time * 4f) * 0.08f;
		arrow.transform.position = new Vector3(center.x, bounds.max.y + 0.25f + bob, 0);
		arrow.color = color.WithAlpha(hints && !homeInside ? Mathf.Max(show, attention ? 0.9f : 0f) : 0f);

		// 집 안에서는 대상 위에 겹쳐 표시 (위로 올리면 화면 밖으로 잘림)
		text.transform.position = homeInside
			? new Vector3(center.x, center.y - 0.15f, 0)
			: new Vector3(center.x, bounds.max.y + 0.55f, 0);
		// 집 안은 오브젝트가 붙어 있어 이름표가 겹치므로 한 줄·작은 글씨로 줄이고 가장 가까운 하나만 보여준다
		text.fontSize = homeInside ? 2f : 3.2f;
		if (homeInside)
		{
			text.text = label;
		}
		else
		{
			string main = string.IsNullOrEmpty(hint) ? label : "<color=#" + NRPalette.ToHex(NRPalette.Cyan) + ">[" + hint + "]</color> " + label;
			text.text = string.IsNullOrEmpty(sublabel) ? main : main + "\n<size=2.4><color=#" + NRPalette.ToHex(NRPalette.TextDim) + ">" + sublabel + "</color></size>";
		}
		text.alpha = hints && (!homeInside || hover || Closest(center) == this) ? show : 0f;

		// 직접 상호작용 (책장, NPC 클릭 등)
		if (onInteract != null && !NRUIState.IsGameplayBlocked)
		{
			bool clicked = hover && NRInput.LeftClickDown && !NRInput.PointerOverInteractiveUI() && dist < 6f;
			bool pressed = !clickOnly && dist < interactRadius && NRInput.InteractDown;
			if (clicked || pressed) onInteract();
		}
	}
}

// ============================================================================
// 씬의 상호작용 대상 자동 탐색 (나중에 생성되는 문/보상 포함)
// ============================================================================
public class NRInteractableScanner : MonoBehaviour
{
	float next;
	bool townScene;

	public static void Install(bool town)
	{
		var go = new GameObject("NR Interactable Scanner");
		var s = go.AddComponent<NRInteractableScanner>();
		s.townScene = town;
	}

	static int OverRoom
	{
		get
		{
			var sp = FindObjectOfType<PrefabSpawner>();
			return sp != null ? sp.OverRoom : 5;
		}
	}

	void Update()
	{
		if (Time.unscaledTime < next) return;
		next = Time.unscaledTime + 0.5f;

		foreach (var tp in FindObjectsOfType<teleport>())
		{
			if (tp.GetComponent<NRInteractable>() != null || tp.isCarTP) continue;
			Describe(tp, out string label, out string sub, out Color color);
			bool bed = tp.gameObject.name.ToLowerInvariant().Contains("bed");
			var it = NRInteractable.Attach(tp.gameObject, label, sub, color, bed ? "클릭 / E" : "이동");
			if (bed) MakeBedClickable(tp, it);
			else DecoratePortal(tp, it, label, color);
		}
		foreach (var sc in FindObjectsOfType<SceneChangeOnCollision>())
		{
			if (sc.GetComponent<NRInteractable>() != null) continue;
			bool toTown = sc.sceneName == NRGame.SceneTown;
			var it = NRInteractable.Attach(sc.gameObject, toTown ? "거리로 나가기" : "집으로 돌아가기",
				toTown ? "사람들과 대화" : "악몽 속으로 돌아갈 준비", toTown ? NRPalette.Cyan : NRPalette.Gold, "클릭");
			it.showRadius = 6f;
			if (!toTown)
			{
				it.Attention(4f);
				NRWaypoint.Add(sc.transform, "집", NRPalette.Gold);
			}
		}
		foreach (var item in FindObjectsOfType<TouchItems>())
		{
			if (item.GetComponent<NRInteractable>() != null) continue;
			string label, sub = "";
			Color color = NRPalette.Cyan;
			if (item.isClass) { label = "클래스 구슬"; sub = "다가가서 먹으면 각성합니다"; color = NRPalette.Will; }
			else if (item.Round > 0) { label = "감정의 구슬"; sub = "증강 선택"; color = NRPalette.Anxiety; }
			else if (item.AddAtk > 0) { label = "공격력 +" + item.AddAtk; color = NRPalette.Rage; }
			else if (item.AddHp > 0) { label = "최대 체력 +" + item.AddHp; color = NRPalette.Crimson; }
			else if (item.Heal > 0) { label = "체력 회복 +" + item.Heal; color = NRPalette.Green; }
			else if (item.AddMoney > 0) { label = "재화 +" + item.AddMoney; color = NRPalette.Gold; }
			else label = "보상";
			var it = NRInteractable.Attach(item.gameObject, label, sub, color, "획득");
			it.showRadius = 6f;
			NRPickupVisual.Attach(item, color);
			if (item.isClass)
			{
				it.showRadius = 30f; // 각성의 방에서는 항상 표시
				it.Attention(6f);
				NRWaypoint.Add(item.transform, "클래스 구슬", NRPalette.Will, () => Player.gameRound == 1);
			}
			else
			{
				NRWaypoint.Add(item.transform, "보상", color);
			}
		}
		foreach (var npc in FindObjectsOfType<_Object>())
		{
			if (npc.GetComponent<NRInteractable>() != null) continue;
			bool shop = npc.CompareTag("shop");
			string name = string.IsNullOrEmpty(npc.name) ? (shop ? "상인" : "") : npc.name;
			var it = NRInteractable.Attach(npc.gameObject, shop ? "악몽 상인" : name, shop ? "클릭하면 상점" : "클릭하면 대화", shop ? NRPalette.Gold : NRPalette.Cyan, "클릭 / " + NRControls.Keys.talk);
			it.showRadius = shop ? 5f : 3f;
			it.clickOnly = true;
			it.useColliderBounds = true;
			var target = npc.gameObject;
			it.onInteract = () => TalkTo(target);
			if (shop) NRWaypoint.Add(npc.transform, "상인", NRPalette.Gold, () => Player.gameRound == OverRoom + 1);
		}
	}

	static void TalkTo(GameObject npc)
	{
		var player = FindObjectOfType<Player>();
		if (player == null) return;
		float d = Vector2.Distance(player.transform.position, npc.transform.position);
		if (d > 3.2f)
		{
			NRUIRoot.ToastMsg("조금 더 가까이 가서 말을 거세요", NRPalette.TextDim, 1.4f);
			return;
		}
		var act = player.GetComponent<PlayerAction>();
		if (act != null) act.StartTalkWith(npc);
	}

	/// <summary>집 침대: 그림은 그대로 두고, 부딪혀서가 아니라 클릭(또는 E)으로만 악몽에 들어간다.</summary>
	static void MakeBedClickable(teleport tp, NRInteractable it)
	{
		foreach (var col in tp.GetComponents<Collider2D>()) col.enabled = false;
		it.showRadius = 5f;
		it.interactRadius = 2f;
		it.onInteract = () =>
		{
			var p = FindObjectOfType<Player>();
			var pc = p != null ? p.GetComponent<Collider2D>() : null;
			if (pc != null) tp.MovePlayer(pc);
		};
		NRWaypoint.Add(tp.transform, "침대", NRPalette.Anxiety);
	}

	static void DecoratePortal(teleport tp, NRInteractable it, string label, Color color)
	{
		string raw = tp.gameObject.name;
		string n = raw.ToLowerInvariant();
		bool bossTp = raw == "ToTheBossRoomTP" || tp.isBossTP;
		bool classPortal = raw == "portal";

		NRPortalVisual.Attach(tp.gameObject, NRPortalVisual.RewardFromName(n, tp.isBossTP), color);
		it.showRadius = 6f;
		it.Attention(3f);
		int over = OverRoom;
		bool store = n.Contains("store");
		if (bossTp) NRWaypoint.Add(tp.transform, label, color, () => Player.gameRound == over + 1, 2);
		else if (store) NRWaypoint.Add(tp.transform, label, color, null, 2);
		else if (classPortal) NRWaypoint.Add(tp.transform, label, color, () => Player.gameRound == 1 && Player.round >= 1);
		else NRWaypoint.Add(tp.transform, label, color);
	}

	static void Describe(teleport tp, out string label, out string sub, out Color color)
	{
		string n = tp.gameObject.name.ToLowerInvariant();
		sub = "";
		color = NRPalette.Pink;
		if (n.Contains("bed")) { label = "꿈속으로"; sub = "악몽 속 결투 시작"; color = NRPalette.Anxiety; }
		else if (tp.isBossTP || n.Contains("boss")) { label = "수문장의 방"; sub = "보스전"; color = NRPalette.Crimson; }
		else if (n.Contains("store")) { label = "상점으로"; sub = "상인에게서 준비"; color = NRPalette.Gold; }
		else if (n.Contains("portal")) { label = "첫 번째 방으로"; color = NRPalette.Anxiety; }
		else if (n.Contains("health") || n.Contains("hp")) { label = "체력의 방"; sub = "보상: 최대 체력 증가"; color = NRPalette.Crimson; }
		else if (n.Contains("attack") || n.Contains("atk")) { label = "힘의 방"; sub = "보상: 공격력 증가"; color = NRPalette.Rage; }
		else if (n.Contains("money")) { label = "재화의 방"; sub = "보상: 재화"; color = NRPalette.Gold; }
		else if (n.Contains("emotion") || n.Contains("round")) { label = "감정의 방"; sub = "보상: 증강 선택"; color = NRPalette.Anxiety; }
		else label = "다음 방으로";
		if (NRRun.Floor >= 2 && (n.Contains("health") || n.Contains("hp") || n.Contains("attack") || n.Contains("atk") || n.Contains("money") || n.Contains("emotion")))
			sub = "보상: 증강 선택 (" + NRRun.Floor + "계층)";
	}
}

// ============================================================================
// 보상 아이템 그림 보강: 떠오르는 움직임 + 광원 + 반짝임 (16번)
// ============================================================================
public class NRPickupVisual : MonoBehaviour
{
	Transform art;
	Vector3 baseLocal;
	SpriteRenderer glow;
	SpriteRenderer shadow;
	Color color;
	float seed;

	public static void Attach(TouchItems item, Color color)
	{
		if (item.GetComponent<NRPickupVisual>() != null) return;
		var v = item.gameObject.AddComponent<NRPickupVisual>();
		v.color = color;
	}

	void Start()
	{
		seed = UnityEngine.Random.value * 10f;
		var sr = GetComponent<SpriteRenderer>();
		if (sr == null) sr = GetComponentInChildren<SpriteRenderer>();
		if (sr == null) return;
		art = sr.transform == transform ? null : sr.transform;

		var ls = transform.lossyScale;
		Vector3 inv = new Vector3(1f / Mathf.Max(0.001f, Mathf.Abs(ls.x)), 1f / Mathf.Max(0.001f, Mathf.Abs(ls.y)), 1f);

		var g = new GameObject("NR Pickup Glow");
		g.transform.SetParent(transform, false);
		g.transform.localScale = Vector3.Scale(inv, new Vector3(1.6f, 1.6f, 1f));
		glow = g.AddComponent<SpriteRenderer>();
		glow.sprite = NRSprites.Glow;
		NRSort.Set(glow, NRSort.Floor, 45);

		var s = new GameObject("NR Pickup Shadow");
		s.transform.SetParent(transform, false);
		s.transform.localPosition = new Vector3(0, -sr.bounds.extents.y * inv.y, 0);
		s.transform.localScale = Vector3.Scale(inv, new Vector3(0.7f, 0.25f, 1f));
		shadow = s.AddComponent<SpriteRenderer>();
		shadow.sprite = NRSprites.Glow;
		shadow.color = new Color(0, 0, 0, 0.5f);
		NRSort.Set(shadow, NRSort.Floor, 44);

		NRSort.Set(sr, NRSort.Enemy, 70);
		if (art != null) baseLocal = art.localPosition;
	}

	void Update()
	{
		float t = Time.time + seed;
		if (glow != null) glow.color = color.WithAlpha(0.35f + 0.2f * Mathf.Sin(t * 3f));
		if (art != null) art.localPosition = baseLocal + Vector3.up * (0.12f + 0.08f * Mathf.Sin(t * 2.5f));
		if (Mathf.Repeat(t, 0.9f) < Time.deltaTime) NRCombatFX.Sparks(transform.position + (Vector3)(UnityEngine.Random.insideUnitCircle * 0.3f), color, 1);
	}
}

// ============================================================================
// 대화창 스킨 + 한 글자씩 출력 (대화 로직은 수정하지 않음)
// ============================================================================
public class NRDialogueSkin : MonoBehaviour
{
	static readonly List<NRDialogueSkin> active = new List<NRDialogueSkin>();

	TMP_Text dialog;
	TMP_Text nextIndicator;
	string lastText = "";
	float reveal;
	bool typing;
	const float CharsPerSecond = 42f;

	public static void Apply(Transform dialogSet)
	{
		if (dialogSet == null) return;
		var talkerBg = NRUtil.FindDeep(dialogSet, "TalkerBg_Img");
		var dialogBg = NRUtil.FindDeep(dialogSet, "DialogBg_Img");
		var talker = NRUtil.FindDeep(dialogSet, "Talker_Text");
		var dialogText = NRUtil.FindDeep(dialogSet, "Dialog_Text");

		Restyle(talkerBg, NRSprites.Frame(NRPalette.Bg2, NRPalette.Pink, NRPalette.Pink.WithAlpha(0.6f), NRPalette.Bg0));
		Restyle(dialogBg, NRSprites.Frame(NRPalette.Bg0.WithAlpha(0.94f), NRPalette.BorderHi, NRPalette.BorderHi.WithAlpha(0.45f), NRPalette.Bg0));

		if (talker != null)
		{
			var t = talker.GetComponent<TMP_Text>();
			if (t != null)
			{
				t.gameObject.AddComponent<NRKeepFont>();
				NRFont.Style(t, NRTextFx.Shadow);
				t.color = NRPalette.Gold;
			}
		}
		if (dialogText != null && dialogBg != null)
		{
			var d = dialogText.GetComponent<TMP_Text>();
			if (d != null)
			{
				d.gameObject.AddComponent<NRKeepFont>();
				NRFont.Style(d, NRTextFx.Shadow);
				d.color = NRPalette.Text;
				var skin = d.gameObject.AddComponent<NRDialogueSkin>();
				skin.dialog = d;

				var ind = new GameObject("NR Next", typeof(RectTransform)).GetComponent<RectTransform>();
				ind.SetParent(dialogBg, false);
				ind.anchorMin = ind.anchorMax = new Vector2(1, 0);
				ind.pivot = new Vector2(1, 0);
				ind.anchoredPosition = new Vector2(-10, 4);
				ind.sizeDelta = new Vector2(160, 28);
				var it = ind.gameObject.AddComponent<TextMeshProUGUI>();
				ind.gameObject.AddComponent<NRKeepFont>();
				NRFont.Style(it, NRTextFx.Shadow);
				it.text = "E ▼";
				it.fontSize = 16;
				it.alignment = TextAlignmentOptions.BottomRight;
				it.color = NRPalette.Pink;
				it.raycastTarget = false;
				skin.nextIndicator = it;
			}
		}
	}

	static void Restyle(Transform t, Sprite frame)
	{
		if (t == null) return;
		var img = t.GetComponent<UnityEngine.UI.Image>();
		if (img == null) return;
		img.sprite = frame;
		img.type = UnityEngine.UI.Image.Type.Sliced;
		img.pixelsPerUnitMultiplier = 1f;
		img.color = Color.white;
	}

	void OnEnable() { active.Add(this); lastText = ""; }
	void OnDisable() { active.Remove(this); }

	public static bool TryCompleteTyping()
	{
		foreach (var s in active)
		{
			if (s != null && s.typing && s.isActiveAndEnabled)
			{
				s.reveal = 99999f;
				return true;
			}
		}
		return false;
	}

	void LateUpdate()
	{
		if (dialog == null) return;
		string current = dialog.text;
		if (current != lastText)
		{
			lastText = current;
			reveal = 0f;
			typing = true;
		}
		if (typing)
		{
			reveal += Time.unscaledDeltaTime * CharsPerSecond;
			int total = dialog.textInfo != null ? dialog.textInfo.characterCount : current.Length;
			if (total <= 0) total = current.Length;
			dialog.maxVisibleCharacters = Mathf.FloorToInt(reveal);
			if (reveal >= total) { typing = false; dialog.maxVisibleCharacters = 99999; }
		}
		if (nextIndicator != null)
		{
			nextIndicator.alpha = typing ? 0f : 0.6f + 0.4f * Mathf.Sin(Time.unscaledTime * 5f);
			nextIndicator.rectTransform.anchoredPosition = new Vector2(-10, 4 + (typing ? 0 : Mathf.Abs(Mathf.Sin(Time.unscaledTime * 4f)) * 3f));
		}
	}
}
