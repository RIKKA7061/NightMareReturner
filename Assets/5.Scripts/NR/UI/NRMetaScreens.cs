using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ============================================================================
// 기억의 책장 — 악몽 결정으로 영구 강화 (집에서만)
// ============================================================================
public class NRMirror : NRModal
{
	static NRMirror instance;
	RectTransform list;
	TextMeshProUGUI crystalsText;

	public static void Show()
	{
		if (NRUIState.AnyOpen) return;
		if (instance == null) instance = Build();
		instance.Open();
	}

	static NRMirror Build()
	{
		var root = NRModalHost.CreateModalRoot("NR Mirror", 0.8f);
		var m = root.gameObject.AddComponent<NRMirror>();
		var panel = NRUI.Panel(root, "Panel", NRSprites.ColoredFrame(NRPalette.Cyan));
		NRUI.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1400, 920));

		NRModalHost.Header(panel.transform, "기억의 책장", new Vector2(0, -26), 1200, 56);
		var desc = NRUI.Label(panel.transform, "악몽에서 가져온 결정으로 기억을 다듬어 영구히 강해집니다. 강화는 다음 회차부터 적용됩니다.", 26, NRPalette.TextDim, TextAlignmentOptions.Center);
		NRUI.Place(desc.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -118), new Vector2(1300, 40), new Vector2(0.5f, 1));

		m.crystalsText = NRUI.Label(panel.transform, "", 36, NRPalette.Cyan, TextAlignmentOptions.MidlineRight, NRTextFx.OutlineShadow);
		NRUI.Place(m.crystalsText.rectTransform, new Vector2(1, 1), new Vector2(-40, -30), new Vector2(360, 50), new Vector2(1, 1));

		var scroll = NRUI.ScrollView(panel.transform, "Scroll", out var content, 12, 16);
		NRUI.Place((RectTransform)scroll.transform, new Vector2(0.5f, 1), new Vector2(0, -170), new Vector2(1320, 640), new Vector2(0.5f, 1));
		m.list = content;

		var close = NRUI.Button(panel.transform, "닫기", () => m.Close(), new Vector2(260, 66), 30);
		NRUI.Place(close.Rect, new Vector2(0.5f, 0), new Vector2(0, 26), new Vector2(260, 66), new Vector2(0.5f, 0));
		root.gameObject.SetActive(false);
		return m;
	}

	protected override void OnOpened()
	{
		Refresh();
		if (!NRSave.Data.seenMirrorTip)
		{
			NRSave.Data.seenMirrorTip = true;
			NRSave.MarkDirty();
		}
	}

	void Refresh()
	{
		crystalsText.text = "◈ " + NRSave.Data.crystals;
		foreach (Transform c in list) Destroy(c.gameObject);
		foreach (var def in NRMeta.All)
		{
			var d = def;
			var row = NRUI.HRow(list, d.id, 104, 22);
			row.padding = new RectOffset(16, 16, 10, 10);
			var bg = row.gameObject.AddComponent<Image>();
			bg.sprite = NRSprites.ColoredFrame(d.IsMaxed ? NRPalette.Gold : NRPalette.Border);
			bg.type = Image.Type.Sliced;
			bg.pixelsPerUnitMultiplier = 1f / 3f;

			var icon = NRUI.Icon(row.transform, NRSprites.Icon(d.icon), 72);
			var ile = icon.gameObject.AddComponent<LayoutElement>();
			ile.preferredWidth = 72; ile.preferredHeight = 72;

			int lv = d.Level;
			string pips = "";
			for (int i = 0; i < d.maxLevel; i++) pips += i < lv ? "■" : "□";
			string now = lv > 0 ? d.Describe(lv) : "<color=#" + NRPalette.ToHex(NRPalette.TextMute) + ">미습득</color>";
			string next = d.IsMaxed ? "<color=#" + NRPalette.ToHex(NRPalette.Gold) + ">최대 강화</color>" : "다음: <color=#" + NRPalette.ToHex(NRPalette.Green) + ">" + d.Describe(lv + 1) + "</color>";
			var text = NRUI.Label(row.transform,
				"<color=#" + NRPalette.ToHex(d.color) + ">" + d.name + "</color>  <size=24><color=#" + NRPalette.ToHex(NRPalette.Pink) + ">" + pips + "</color></size>\n" +
				"<size=26>" + now + "   <color=#" + NRPalette.ToHex(NRPalette.TextMute) + ">|</color>   " + next + "</size>",
				30, NRPalette.Text, TextAlignmentOptions.MidlineLeft);
			text.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

			string label = d.IsMaxed ? "완료" : "◈ " + d.NextCost;
			var buy = NRUI.Button(row.transform, label, () =>
			{
				if (NRMeta.TryBuy(d))
				{
					NRAudio.PlaySfx("augment_pick", 0.8f);
					NRUIRoot.ToastMsg(d.name + " 강화 완료 (Lv " + d.Level + ")", d.color);
					ApplyImmediate(d);
					Refresh();
				}
				else if (!d.IsMaxed) NRUIRoot.ToastMsg("악몽 결정이 부족합니다", NRPalette.Crimson, 1.6f);
			}, new Vector2(200, 72), 30);
			var ble = buy.gameObject.AddComponent<LayoutElement>();
			ble.preferredWidth = 200; ble.preferredHeight = 72;
			buy.SetInteractable(!d.IsMaxed && NRSave.Data.crystals >= d.NextCost);
		}
	}

	/// <summary>집에 있을 때 산 체력/공격/방어 강화는 바로 반영</summary>
	static void ApplyImmediate(NRUpgradeDef d)
	{
		var p = NRStats.Player;
		if (p == null || Player.gameRound != 0) return;
		switch (d.id)
		{
			case "m_atk": p.Atk += 3; break;
			case "m_hp": p.maxHP += 30; p.nowHP = p.maxHP; break;
			case "m_armor": p.AR += 10; break;
			case "m_defy": NRStats.DeathDefianceLeft = NRMeta.DeathDefiance; break;
			case "m_reroll": NRAugments.RerollsLeft = NRMeta.Rerolls; break;
		}
	}
}

// ============================================================================
// 사망 화면
// ============================================================================
public class NRDeathScreen : MonoBehaviour
{
	static NRDeathScreen instance;
	CanvasGroup group;

	public static void Show(Player p)
	{
		Hide();
		var root = NRModalHost.CreateModalRoot("NR Death Screen", 0f);
		instance = root.gameObject.AddComponent<NRDeathScreen>();
		instance.group = root.gameObject.AddComponent<CanvasGroup>();
		instance.group.alpha = 0f;
		instance.StartCoroutine(instance.Routine(root));
	}

	public static void Hide()
	{
		if (instance != null) Destroy(instance.gameObject);
		instance = null;
	}

	IEnumerator Routine(RectTransform root)
	{
		var dim = root.GetChild(0).GetComponent<Image>();
		var red = NRUI.Image(root, "Red", NRSprites.Vignette, new Color(0.7f, 0.02f, 0.1f, 0.9f));
		NRUI.Stretch(red.rectTransform, -60, -60, -60, -60);

		var title = NRUI.Label(root, "악몽에 삼켜졌다", 96, NRPalette.Crimson, TextAlignmentOptions.Center, NRTextFx.OutlineShadow);
		NRUI.Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 230), new Vector2(1600, 130));

		string floorInfo = NRRun.InRun ? NRRun.Floor + "계층 · " + NRRun.Current.name : "—";
		var info = NRUI.Label(root,
			"<color=#" + NRPalette.ToHex(NRPalette.TextDim) + ">도달</color>   " + floorInfo + "\n" +
			"<color=#" + NRPalette.ToHex(NRPalette.TextDim) + ">처치한 적</color>   " + NRStats.RunKills + "     <color=#" + NRPalette.ToHex(NRPalette.TextDim) + ">보유 증강</color>   " + NRAugments.Owned.Count + "\n" +
			"<color=#" + NRPalette.ToHex(NRPalette.TextDim) + ">이번 회차 악몽 결정</color>   <color=#" + NRPalette.ToHex(NRPalette.Cyan) + ">◈ +" + NRRun.RunCrystals + "</color>   (보유 " + NRSave.Data.crystals + ")\n\n" +
			"<color=#" + NRPalette.ToHex(NRPalette.Pink) + ">죽은 횟수 " + Player.DeadCount + "</color>\n" +
			"<size=26><color=#" + NRPalette.ToHex(NRPalette.TextMute) + ">죽음을 거듭할수록 거리의 사람들이 들려주는 이야기가 달라집니다.\n집의 책장에서 악몽 결정으로 영구 강화를 할 수 있습니다.</color></size>",
			34, NRPalette.Text, TextAlignmentOptions.Center);
		NRUI.Place(info.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -40), new Vector2(1500, 360));

		var prompt = NRUI.Label(root, "[Space]  집에서 깨어나기", 40, NRPalette.Text, TextAlignmentOptions.Center, NRTextFx.OutlineShadow);
		NRUI.Place(prompt.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 110), new Vector2(1000, 60), new Vector2(0.5f, 0));
		prompt.gameObject.SetActive(false);

		float t = 0f;
		while (t < 1.2f)
		{
			t += Time.unscaledDeltaTime;
			group.alpha = Mathf.Clamp01(t / 1.2f);
			dim.color = new Color(0.02f, 0.0f, 0.03f, 0.75f * group.alpha);
			yield return null;
		}
		yield return new WaitForSecondsRealtime(2.9f);
		prompt.gameObject.SetActive(true);
		while (true)
		{
			prompt.color = Color.white.WithAlpha(0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 3f));
			yield return null;
		}
	}
}

// ============================================================================
// 엔딩 크레딧
// ============================================================================
public class NRCredits : MonoBehaviour
{
	public static bool IsPlaying { get; private set; }
	Action onDone;
	bool ending;

	public static void Play(bool ending, Action done)
	{
		if (IsPlaying) return;
		IsPlaying = true;
		var canvas = NRUI.CreateCanvas("NR Credits", 800);
		var c = canvas.gameObject.AddComponent<NRCredits>();
		c.onDone = done;
		c.ending = ending;
		c.StartCoroutine(c.Routine((RectTransform)canvas.transform));
	}

	void OnDestroy()
	{
		IsPlaying = false;
	}

	IEnumerator Routine(RectTransform root)
	{
		NRHud.SetVisible(false);
		var bg = NRUI.Image(root, "Black", NRSprites.White, new Color(0, 0, 0, 0), true);
		NRUI.Stretch(bg.rectTransform, -20, -20, -20, -20);
		float t = 0f;
		while (t < 1.2f) { t += Time.unscaledDeltaTime; bg.color = new Color(0, 0, 0, t / 1.2f); yield return null; }
		NRAudio.PlayMusic("credits_bgm");

		var skip = NRUI.Label(root, "Space 길게: 빨리 감기   ·   ESC: 건너뛰기", 24, NRPalette.TextMute, TextAlignmentOptions.BottomRight);
		NRUI.Place(skip.rectTransform, new Vector2(1, 0), new Vector2(-40, 30), new Vector2(800, 40), new Vector2(1, 0));

		if (ending)
		{
			string[] epilogue =
			{
				"세 번째 계층의 끝에서, 나는 악몽의 뿌리와 마주했다.",
				"그것은 괴물이 아니었다. 인정받고 싶었던, 지쳐 버린 나 자신이었다.",
				"몇 번을 쓰러졌는지는 이제 중요하지 않다.",
				"그때마다 다시 일어났다는 것만이 남았다.",
				"창밖으로 아침이 온다. 오늘은, 조금 늦게 일어나도 괜찮다.",
			};
			var line = NRUI.Label(root, "", 44, NRPalette.Text, TextAlignmentOptions.Center, NRTextFx.Shadow);
			NRUI.Place(line.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1600, 200));
			foreach (var s in epilogue)
			{
				if (Skipped) break;
				yield return FadeText(line, s, 3.2f);
			}
			Destroy(line.gameObject);
		}

		// 스크롤 크레딧
		var textAsset = Resources.Load<TextAsset>("NR/Text/credits");
		string body = textAsset != null ? textAsset.text : "악몽 회귀자\n\nThank you for playing";
		var credits = NRUI.Label(root, FormatCredits(body), 36, NRPalette.Text, TextAlignmentOptions.Top);
		var crt = credits.rectTransform;
		crt.anchorMin = new Vector2(0.5f, 0);
		crt.anchorMax = new Vector2(0.5f, 0);
		crt.pivot = new Vector2(0.5f, 1);
		crt.sizeDelta = new Vector2(1400, 100);
		float height = credits.GetPreferredValues(credits.text, 1400, 0).y;
		crt.sizeDelta = new Vector2(1400, height);
		float y = 0f;
		float endY = height + NRUI.RefHeight;
		while (y < endY && !Skipped)
		{
			float speed = NRInput.SpaceHeld ? 420f : 90f;
			y += speed * Time.unscaledDeltaTime;
			crt.anchoredPosition = new Vector2(0, y);
			yield return null;
		}

		NRAudio.StopMusic();
		var cb = onDone;
		onDone = null;
		NRHud.SetVisible(true);
		// 마지막 암전은 호출한 쪽에서 해제
		if (NRUIRoot.Instance != null) yield return NRUIRoot.Instance.Fade(1f, 0.01f);
		IsPlaying = false;
		cb?.Invoke();
		Destroy(gameObject);
	}

	bool skipped;
	bool Skipped
	{
		get
		{
			if (NRInput.EscDown) skipped = true;
			return skipped;
		}
	}

	IEnumerator FadeText(TextMeshProUGUI t, string s, float hold)
	{
		t.text = s;
		float k = 0f;
		while (k < 1f && !Skipped) { k += Time.unscaledDeltaTime / 0.8f; t.alpha = k; yield return null; }
		float h = 0f;
		while (h < hold && !Skipped) { h += Time.unscaledDeltaTime * (NRInput.SpaceHeld ? 4f : 1f); yield return null; }
		while (k > 0f && !Skipped) { k -= Time.unscaledDeltaTime / 0.6f; t.alpha = k; yield return null; }
		t.alpha = 0f;
	}

	/// <summary># 제목, ## 소제목 서식</summary>
	static string FormatCredits(string raw)
	{
		var sb = new System.Text.StringBuilder();
		foreach (var rawLine in raw.Replace("\r", "").Split('\n'))
		{
			string line = rawLine;
			if (line.StartsWith("## ")) sb.AppendLine("<size=30><color=#" + NRPalette.ToHex(NRPalette.Pink) + ">" + line.Substring(3) + "</color></size>");
			else if (line.StartsWith("# ")) sb.AppendLine("<size=80><color=#" + NRPalette.ToHex(NRPalette.Text) + ">" + line.Substring(2) + "</color></size>");
			else sb.AppendLine(line);
		}
		return sb.ToString();
	}
}
