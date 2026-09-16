using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// ============================================================================
// 증강 선택 화면 (하데스 은총 선택 방식)
// ============================================================================
public class NRAugmentSelect : NRModal
{
	public override bool CloseOnEsc => false;   // 반드시 하나를 골라야 함

	List<NRAugmentOffer> offers;
	readonly List<RectTransform> cards = new List<RectTransform>();
	RectTransform cardRow;
	TextMeshProUGUI titleText, subText;
	NRButton rerollButton;
	Action onPicked;
	NRFamily? family;
	bool boss;
	bool picking;
	string sourceTitle;

	static readonly Queue<(NRFamily? family, bool boss, string title, Action done)> pending = new Queue<(NRFamily?, bool, string, Action)>();
	static NRAugmentSelect active;

	public static void Show(NRFamily? family, bool bossReward, string source, Action onDone)
	{
		if (active != null || NRUIState.IsOpen<NRClassAwakening>())
		{
			pending.Enqueue((family, bossReward, source, onDone));
			return;
		}
		var root = NRModalHost.CreateModalRoot("NR Augment Select", 0.82f);
		var s = root.gameObject.AddComponent<NRAugmentSelect>();
		active = s;
		s.family = family ?? (NRFamily)UnityEngine.Random.Range(0, 4);
		s.boss = bossReward;
		s.onPicked = onDone;
		s.sourceTitle = source;
		s.BuildFrame();
		s.Open();
		s.Roll();
		NRAudio.PlaySfx("augment_pick", 0.6f);
	}

	void BuildFrame()
	{
		var root = (RectTransform)transform;
		var fam = family ?? NRFamily.Rage;
		var glow = NRUI.Image(root, "Glow", NRSprites.Glow, NRAugments.FamilyColor(fam).WithAlpha(0.25f));
		NRUI.Place(glow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(2200, 1400));

		titleText = NRUI.Label(root, "", 64, NRPalette.Text, TextAlignmentOptions.Center, NRTextFx.OutlineShadow);
		NRUI.Place(titleText.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -60), new Vector2(1600, 90), new Vector2(0.5f, 1));
		subText = NRUI.Label(root, "", 28, NRPalette.TextDim, TextAlignmentOptions.Center);
		NRUI.Place(subText.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -150), new Vector2(1600, 50), new Vector2(0.5f, 1));

		cardRow = NRUI.Rect(root, "Cards");
		NRUI.Place(cardRow, new Vector2(0.5f, 0.5f), new Vector2(0, -20), new Vector2(1500, 640));

		var hint = NRUI.Label(root, "카드를 클릭하거나 1 · 2 · 3 키로 선택", 26, NRPalette.TextMute, TextAlignmentOptions.Center);
		NRUI.Place(hint.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 130), new Vector2(1000, 40), new Vector2(0.5f, 0));

		rerollButton = NRUI.Button(root, "다시 뽑기", () => Reroll(), new Vector2(300, 66), 28);
		NRUI.Place(rerollButton.Rect, new Vector2(0.5f, 0), new Vector2(0, 50), new Vector2(300, 66), new Vector2(0.5f, 0));
	}

	void Roll()
	{
		offers = NRAugments.GenerateOffers(family, 3, boss);
		var fam = family ?? NRFamily.Rage;
		string famName = NRAugments.FamilyName(fam);
		titleText.text = "<color=#" + NRPalette.ToHex(NRAugments.FamilyColor(fam)) + ">" + famName + "</color>의 속삭임";
		subText.text = sourceTitle + (boss ? " · 희귀 등급 확률 상승" : "") + "  —  같은 증강을 다시 고르면 레벨이 오릅니다";
		rerollButton.label.text = "다시 뽑기 (" + NRAugments.RerollsLeft + ")";
		rerollButton.gameObject.SetActive(NRAugments.RerollsLeft > 0);
		BuildCards();
	}

	void Reroll()
	{
		if (picking || NRAugments.RerollsLeft <= 0) return;
		NRAugments.RerollsLeft--;
		Roll();
	}

	void BuildCards()
	{
		foreach (var c in cards) if (c != null) Destroy(c.gameObject);
		cards.Clear();
		float spacing = 490f;
		for (int i = 0; i < offers.Count; i++)
		{
			var card = BuildCard(offers[i], i);
			NRUI.Place(card, new Vector2(0.5f, 0.5f), new Vector2((i - (offers.Count - 1) / 2f) * spacing, 0), new Vector2(450, 620));
			cards.Add(card);
			StartCoroutine(CardIntro(card, i * 0.08f));
		}
		if (cards.Count > 0) NRUI.Select(cards[0].GetComponent<Button>());
	}

	RectTransform BuildCard(NRAugmentOffer offer, int index)
	{
		var def = offer.def;
		Color famColor = NRAugments.FamilyColor(def.family);
		Color rarColor = NRAugments.RarityColor(offer.rarity);

		var frame = NRUI.Image(cardRow, "Card " + index, NRSprites.ColoredFrame(rarColor), Color.white, true);
		var rt = frame.rectTransform;
		var btn = frame.gameObject.AddComponent<Button>();
		btn.transition = Selectable.Transition.None;
		int idx = index;
		btn.onClick.AddListener(() => Pick(idx));
		var fx = frame.gameObject.AddComponent<NRCardHover>();

		// 계열 리본
		var ribbon = NRUI.Image(rt, "Ribbon", NRSprites.White, famColor.WithAlpha(0.9f));
		NRUI.Place(ribbon.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -12), new Vector2(418, 50), new Vector2(0.5f, 1));
		var famText = NRUI.Label(ribbon.transform, NRAugments.FamilyName(def.family) + (def.IsDuo ? "  (" + NRAugments.FamilyName(def.duoA) + " + " + NRAugments.FamilyName(def.duoB) + ")" : ""), 26, NRPalette.Bg0, TextAlignmentOptions.Center, NRTextFx.None);
		NRUI.Stretch(famText.rectTransform);

		var keyText = NRUI.Label(rt, (index + 1).ToString(), 30, NRPalette.TextMute, TextAlignmentOptions.TopLeft);
		NRUI.Place(keyText.rectTransform, new Vector2(0, 1), new Vector2(24, -70), new Vector2(60, 40), new Vector2(0, 1));

		// 아이콘
		var iconGlow = NRUI.Image(rt, "IconGlow", NRSprites.Glow, rarColor.WithAlpha(0.35f));
		NRUI.Place(iconGlow.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -80), new Vector2(240, 240), new Vector2(0.5f, 1));
		var iconFrame = NRUI.Image(rt, "IconFrame", NRSprites.ColoredFrame(famColor), Color.white);
		NRUI.Place(iconFrame.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -110), new Vector2(160, 160), new Vector2(0.5f, 1));
		var icon = NRUI.Icon(iconFrame.transform, NRSprites.Icon(def.icon), 120);
		NRUI.Place(icon.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(120, 120));

		// 이름 / 등급
		var name = NRUI.Label(rt, def.name, 42, Color.white, TextAlignmentOptions.Center, NRTextFx.OutlineShadow);
		NRUI.Place(name.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -290), new Vector2(420, 56), new Vector2(0.5f, 1));
		string lvl = offer.IsUpgrade ? "Lv " + offer.owned.level + " → <color=#" + NRPalette.ToHex(NRPalette.Gold) + ">" + offer.newLevel + "</color>" : "<color=#" + NRPalette.ToHex(NRPalette.Cyan) + ">새 증강</color>";
		var rarity = NRUI.Label(rt, "<color=#" + NRPalette.ToHex(rarColor) + ">" + NRAugments.RarityName(offer.rarity) + "</color>  ·  " + lvl, 26, NRPalette.TextDim, TextAlignmentOptions.Center);
		NRUI.Place(rarity.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -350), new Vector2(420, 40), new Vector2(0.5f, 1));

		var sep = NRUI.Image(rt, "Sep", NRSprites.White, rarColor.WithAlpha(0.5f));
		NRUI.Place(sep.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -398), new Vector2(360, 2), new Vector2(0.5f, 1));

		// 설명 (업그레이드면 이전 → 다음 수치)
		string desc = def.Describe(offer.NewValue);
		if (offer.IsUpgrade && !def.fixedValue)
		{
			string oldV = Format(offer.OldValue), newV = Format(offer.NewValue);
			desc += "\n\n<size=24><color=#" + NRPalette.ToHex(NRPalette.TextMute) + ">현재 " + oldV + " → </color><color=#" + NRPalette.ToHex(NRPalette.Green) + ">" + newV + "</color></size>";
		}
		var d = NRUI.Label(rt, desc, 30, NRPalette.Text, TextAlignmentOptions.Top);
		NRUI.Place(d.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -418), new Vector2(390, 190), new Vector2(0.5f, 1));

		fx.Init(rt, rarColor, iconGlow);
		return rt;
	}

	static string Format(float v) => Mathf.Approximately(v, Mathf.Round(v)) ? Mathf.RoundToInt(v).ToString() : v.ToString("0.#");

	IEnumerator CardIntro(RectTransform card, float delay)
	{
		var cg = card.gameObject.AddComponent<CanvasGroup>();
		cg.alpha = 0f;
		Vector2 target = card.anchoredPosition;
		card.anchoredPosition = target + new Vector2(0, -60);
		yield return new WaitForSecondsRealtime(delay);
		float t = 0f;
		while (t < 0.25f && card != null)
		{
			t += Time.unscaledDeltaTime;
			float k = Mathf.SmoothStep(0, 1, t / 0.25f);
			cg.alpha = k;
			card.anchoredPosition = Vector2.Lerp(target + new Vector2(0, -60), target, k);
			yield return null;
		}
		if (card != null) { cg.alpha = 1f; card.anchoredPosition = target; }
	}

	void Update()
	{
		if (!IsOpen || picking) return;
		for (int i = 0; i < 3; i++)
			if (NRInput.NumberDown(i + 1) && i < offers.Count) { Pick(i); return; }
	}

	void Pick(int index)
	{
		if (picking || offers == null || index >= offers.Count) return;
		picking = true;
		var offer = offers[index];
		NRAugments.Acquire(offer);
		NRAudio.PlaySfx("augment_pick", 1f);
		StartCoroutine(PickRoutine(index, offer));
	}

	IEnumerator PickRoutine(int index, NRAugmentOffer offer)
	{
		float t = 0f;
		while (t < 0.45f)
		{
			t += Time.unscaledDeltaTime;
			float k = t / 0.45f;
			for (int i = 0; i < cards.Count; i++)
			{
				if (cards[i] == null) continue;
				var cg = cards[i].GetComponent<CanvasGroup>();
				if (i == index)
				{
					cards[i].localScale = Vector3.one * (1f + 0.12f * Mathf.Sin(k * Mathf.PI));
				}
				else if (cg != null) cg.alpha = 1f - k;
			}
			yield return null;
		}
		string msg = offer.IsUpgrade ? offer.def.name + " Lv " + offer.newLevel : offer.def.name + " 획득";
		NRUIRoot.ToastMsg(msg, NRAugments.FamilyColor(offer.def.family));
		if (!NRSave.Data.seenAugmentTip)
		{
			NRSave.Data.seenAugmentTip = true;
			NRUIRoot.ToastMsg("보유 증강은 화면 왼쪽 아래 아이콘이나 TAB 스테이터스에서 확인할 수 있습니다", NRPalette.TextDim, 4f);
		}
		var player = NRStats.Player;
		if (player != null) NRCombatFX.DeathBurst(player.transform.position + Vector3.up * 0.5f, NRAugments.FamilyColor(offer.def.family), 24);
		Close();
	}

	protected override void OnClosed()
	{
		active = null;
		var cb = onPicked;
		onPicked = null;
		Destroy(gameObject, 0.3f);
		cb?.Invoke();
		ShowNextPending();
	}

	public static void ShowNextPending()
	{
		if (active != null || pending.Count == 0) return;
		var p = pending.Dequeue();
		Show(p.family, p.boss, p.title, p.done);
	}
}

/// <summary>증강 카드 호버: 떠오름 + 광원</summary>
public class NRCardHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
	RectTransform rt;
	Image glow;
	Color color;
	bool hover, selected;
	float lift;

	public void Init(RectTransform r, Color c, Image g) { rt = r; color = c; glow = g; }
	public void OnPointerEnter(PointerEventData e) { hover = true; }
	public void OnPointerExit(PointerEventData e) { hover = false; }
	public void OnSelect(BaseEventData e) { selected = true; }
	public void OnDeselect(BaseEventData e) { selected = false; }

	void Update()
	{
		if (rt == null) return;
		bool on = hover || selected;
		float target = on ? 1f : 0f;
		lift = Mathf.MoveTowards(lift, target, Time.unscaledDeltaTime * 6f);
		rt.localScale = Vector3.one * (1f + 0.04f * lift);
		if (glow != null) glow.color = color.WithAlpha(0.3f + 0.35f * lift + 0.08f * Mathf.Sin(Time.unscaledTime * 3f));
	}
}

// ============================================================================
// 클래스 각성 화면
// ============================================================================
public class NRClassAwakening : NRModal
{
	public override bool CloseOnEsc => false;
	Action onDone;
	bool closing;
	float openedAt;

	public static void Show(Action done)
	{
		var root = NRModalHost.CreateModalRoot("NR Class Awakening", 0.86f);
		var w = root.gameObject.AddComponent<NRClassAwakening>();
		w.onDone = done;
		w.Build(root);
		w.openedAt = Time.unscaledTime;
		w.Open();
	}

	void Build(RectTransform root)
	{
		var rays = NRUI.Image(root, "Glow", NRSprites.Glow, NRPalette.Will.WithAlpha(0.35f));
		NRUI.Place(rays.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 170), new Vector2(1100, 1100));
		rays.gameObject.AddComponent<NRUIPulse>().amount = 0.05f;

		var emblemFrame = NRUI.Image(root, "Emblem", NRSprites.ColoredFrame(NRPalette.Will), Color.white);
		NRUI.Place(emblemFrame.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 250), new Vector2(200, 200));
		var emblem = NRUI.Icon(emblemFrame.transform, NRSprites.Icon(30), 150);
		NRUI.Place(emblem.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(150, 150));

		var title = NRUI.Label(root, "각성 — 감정 「<color=#" + NRPalette.ToHex(NRPalette.Will) + ">의지</color>」", 72, NRPalette.Text, TextAlignmentOptions.Center, NRTextFx.OutlineShadow);
		NRUI.Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 90), new Vector2(1400, 100));
		var sub = NRUI.Label(root, "악몽 속에서 억눌려 있던 감정이 깨어났다. 이제 맞서 싸울 수 있다.", 32, NRPalette.TextDim, TextAlignmentOptions.Center);
		NRUI.Place(sub.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 20), new Vector2(1400, 50));

		var panel = NRUI.Panel(root, "Skills", NRSprites.DarkFrame);
		NRUI.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -170), new Vector2(1200, 290));
		var action = NRStats.Action;
		var player = NRStats.Player;
		int atk = player != null ? player.Atk : 25;
		int skillMul = player != null ? player.SkillAtk : 2;
		int ultMul = player != null ? player.UltimitAtk : 4;
		string skills =
			"<color=#" + NRPalette.ToHex(NRPalette.Cyan) + ">좌클릭</color>   기본 공격 — 피해 " + atk + ", 연타 가능\n" +
			"<color=#" + NRPalette.ToHex(NRPalette.Rage) + ">우클릭</color>   특수 공격 — 피해 ×" + skillMul + ", 넓은 범위" + (action != null ? "  (대기 " + action.skillAttackCooldown.ToString("0.#") + "초)" : "") + "\n" +
			"<color=#" + NRPalette.ToHex(NRPalette.Gold) + ">R</color>        궁극기 — 피해 ×" + ultMul + ", 강력한 내려찍기" + (action != null ? "  (대기 " + action.skillAttack2Cooldown.ToString("0.#") + "초)" : "") + "\n" +
			"<color=#" + NRPalette.ToHex(NRPalette.TextMute) + ">남은 감정 「분노」「불안」「슬픔」은 증강으로 힘을 빌릴 수 있습니다.</color>";
		var st = NRUI.Label(panel.transform, skills, 32, NRPalette.Text, TextAlignmentOptions.MidlineLeft);
		NRUI.Stretch(st.rectTransform, 50, 40, 20, 20);

		var btn = NRUI.Button(root, "받아들인다", Finish, new Vector2(360, 80), 36);
		NRUI.Place(btn.Rect, new Vector2(0.5f, 0), new Vector2(0, 70), new Vector2(360, 80), new Vector2(0.5f, 0));
		NRUI.Select(btn.button);
		NRCombatFX.Shake(0.4f, 0.12f);
		NRAudio.PlaySfx("augment_pick", 0.9f);
	}

	void Update()
	{
		if (IsOpen && Time.unscaledTime - openedAt > 0.8f && (NRInput.SpaceDown || NRInput.EnterDown || NRInput.InteractDown)) Finish();
	}

	void Finish()
	{
		if (closing) return;
		closing = true;
		Close();
	}

	protected override void OnClosed()
	{
		Destroy(gameObject, 0.3f);
		var cb = onDone;
		onDone = null;
		cb?.Invoke();
		NRAugmentSelect.ShowNextPending();
	}
}
