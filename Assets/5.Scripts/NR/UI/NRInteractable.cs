using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// ============================================================================
// 상호작용 표시: 문/침대/회오리/구슬/책장/NPC
//  - 은은한 빛(항상) + 가까이 가거나 마우스를 올리면 이름표와 화살표
//  - 방 클리어 직후 등 중요한 순간에만 강하게 3회 깜빡임
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
		ComputeBounds();

		visualRoot = new GameObject("NR Interact FX").transform;
		visualRoot.SetParent(null, false);

		glow = new GameObject("Glow").AddComponent<SpriteRenderer>();
		glow.transform.SetParent(visualRoot, false);
		glow.sprite = NRSprites.Glow;
		glow.sortingOrder = 2;
		float gs = Mathf.Clamp(Mathf.Max(bounds.size.x, bounds.size.y) * 1.4f, 1.2f, 4.5f);
		glow.transform.localScale = new Vector3(gs, gs * 0.8f, 1f);

		arrow = new GameObject("Arrow").AddComponent<SpriteRenderer>();
		arrow.transform.SetParent(visualRoot, false);
		arrow.sprite = NRSprites.ArrowDown;
		arrow.sortingOrder = 401;
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
		text.sortingOrder = 402;

		// 방금 생긴 문/보상은 주목
		if (Time.timeSinceLevelLoad > 2f) Attention(2.4f);
	}

	void ComputeBounds()
	{
		bool has = false;
		foreach (var r in GetComponentsInChildren<SpriteRenderer>())
		{
			if (r.transform.IsChildOf(transform) && r.enabled && r.sprite != null)
			{
				if (!has) { bounds = r.bounds; has = true; }
				else bounds.Encapsulate(r.bounds);
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
			var b = bounds;
			b.Expand(new Vector3(0.3f, 0.3f, 100f));
			hover = b.Contains(new Vector3(m.x, m.y, center.z));
		}

		bool near = dist < showRadius;
		bool attention = Time.time < attentionUntil;
		float target = hover || near ? 1f : 0f;
		show = Mathf.MoveTowards(show, target, Time.deltaTime * 5f);

		// 빛: 평소 은은하게, 주목 시 3회 강하게
		float basePulse = 0.22f + 0.08f * Mathf.Sin(Time.time * 2.5f + born);
		float att = attention ? 0.35f * Mathf.Abs(Mathf.Sin((attentionUntil - Time.time) * Mathf.PI * 1.25f)) : 0f;
		glow.transform.position = new Vector3(center.x, bounds.min.y + bounds.size.y * 0.35f, 0);
		glow.color = color.WithAlpha(basePulse + 0.25f * show + att);

		bool hints = NRSettings.InteractHints;
		float bob = Mathf.Sin(Time.time * 4f) * 0.08f;
		arrow.transform.position = new Vector3(center.x, bounds.max.y + 0.25f + bob, 0);
		arrow.color = color.WithAlpha(hints ? Mathf.Max(show, attention ? 0.9f : 0f) : 0f);

		text.transform.position = new Vector3(center.x, bounds.max.y + 0.55f, 0);
		string main = string.IsNullOrEmpty(hint) ? label : "<color=#" + NRPalette.ToHex(NRPalette.Cyan) + ">[" + hint + "]</color> " + label;
		text.text = string.IsNullOrEmpty(sublabel) ? main : main + "\n<size=2.4><color=#" + NRPalette.ToHex(NRPalette.TextDim) + ">" + sublabel + "</color></size>";
		text.alpha = hints ? show : 0f;

		// 직접 상호작용 (책장 등)
		if (onInteract != null && !NRUIState.IsGameplayBlocked)
		{
			bool clicked = hover && NRInput.LeftClickDown && !NRInput.PointerOverInteractiveUI() && dist < 6f;
			bool pressed = dist < interactRadius && NRInput.InteractDown;
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

	void Update()
	{
		if (Time.unscaledTime < next) return;
		next = Time.unscaledTime + 0.5f;

		foreach (var tp in FindObjectsOfType<teleport>())
		{
			if (tp.GetComponent<NRInteractable>() != null || tp.isCarTP) continue;
			Describe(tp, out string label, out string sub, out Color color);
			NRInteractable.Attach(tp.gameObject, label, sub, color, "이동");
		}
		foreach (var sc in FindObjectsOfType<SceneChangeOnCollision>())
		{
			if (sc.GetComponent<NRInteractable>() != null) continue;
			bool toTown = sc.sceneName == NRGame.SceneTown;
			var it = NRInteractable.Attach(sc.gameObject, toTown ? "거리로 나가기" : "집으로 돌아가기",
				toTown ? "사람들과 대화" : "악몽 속으로 돌아갈 준비", toTown ? NRPalette.Cyan : NRPalette.Gold, "클릭");
			it.showRadius = 6f;
			if (!toTown) it.Attention(4f);
		}
		foreach (var item in FindObjectsOfType<TouchItems>())
		{
			if (item.GetComponent<NRInteractable>() != null) continue;
			string label, sub = "";
			Color color = NRPalette.Cyan;
			if (item.isClass) { label = "클래스 구슬"; sub = "다가가서 각성"; color = NRPalette.Will; }
			else if (item.Round > 0) { label = "감정의 구슬"; sub = "증강 선택"; color = NRPalette.Anxiety; }
			else if (item.AddAtk > 0) { label = "공격력 +" + item.AddAtk; color = NRPalette.Rage; }
			else if (item.AddHp > 0) { label = "최대 체력 +" + item.AddHp; color = NRPalette.Crimson; }
			else if (item.Heal > 0) { label = "체력 회복 +" + item.Heal; color = NRPalette.Green; }
			else if (item.AddMoney > 0) { label = "재화 +" + item.AddMoney; color = NRPalette.Gold; }
			else label = "보상";
			var it = NRInteractable.Attach(item.gameObject, label, sub, color, "획득");
			it.showRadius = 6f;
		}
		foreach (var npc in FindObjectsOfType<_Object>())
		{
			if (npc.GetComponent<NRInteractable>() != null) continue;
			bool shop = npc.CompareTag("shop");
			string name = string.IsNullOrEmpty(npc.name) ? (shop ? "상인" : "") : npc.name;
			var it = NRInteractable.Attach(npc.gameObject, shop ? "상점" : name, shop ? "대화하면 아이템 구매" : "", shop ? NRPalette.Gold : NRPalette.Cyan, "E");
			it.showRadius = 2.6f;
		}
	}

	static void Describe(teleport tp, out string label, out string sub, out Color color)
	{
		string n = tp.gameObject.name.ToLowerInvariant();
		sub = "";
		color = NRPalette.Pink;
		if (n.Contains("bed")) { label = "꿈속으로"; sub = "악몽 속 결투 시작"; color = NRPalette.Anxiety; }
		else if (tp.isBossTP || n.Contains("boss")) { label = "수문장의 방"; sub = "보스전"; color = NRPalette.Crimson; }
		else if (n.Contains("store")) { label = "상점으로"; color = NRPalette.Gold; }
		else if (n.Contains("portal")) { label = "첫 번째 방으로"; color = NRPalette.Anxiety; }
		else if (n.Contains("health") || n.Contains("hp")) { label = "체력의 방"; sub = "보상: 최대 체력 증가"; color = NRPalette.Crimson; }
		else if (n.Contains("attack") || n.Contains("atk")) { label = "힘의 방"; sub = "보상: 공격력 증가"; color = NRPalette.Rage; }
		else if (n.Contains("money")) { label = "재화의 방"; sub = "보상: 재화"; color = NRPalette.Gold; }
		else if (n.Contains("emotion") || n.Contains("round")) { label = "감정의 방"; sub = "보상: 증강 선택"; color = NRPalette.Anxiety; }
		else label = "다음 방으로";
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
