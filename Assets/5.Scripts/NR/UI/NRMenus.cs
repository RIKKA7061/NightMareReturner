using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// ============================================================================
// 모달 캔버스 (씬마다 생성)
// ============================================================================
public static class NRModalHost
{
	static Canvas canvas;

	public static Transform Root
	{
		get
		{
			if (canvas == null) canvas = NRUI.CreateCanvas("NR Modal Canvas", 300);
			return canvas.transform;
		}
	}

	/// <summary>화면 전체를 덮는 모달 루트 (어두운 배경 포함)</summary>
	public static RectTransform CreateModalRoot(string name, float dim = 0.78f)
	{
		var rt = NRUI.Rect(Root, name);
		NRUI.Stretch(rt);
		var bg = NRUI.Image(rt, "Dim", NRSprites.White, new Color(0.02f, 0.01f, 0.05f, dim), true);
		NRUI.Stretch(bg.rectTransform, -20, -20, -20, -20);
		bg.gameObject.AddComponent<NRBlocksPointer>();
		return rt;
	}

	public static TextMeshProUGUI Header(Transform parent, string title, Vector2 pos, float width, float size = 56)
	{
		var t = NRUI.Label(parent, title, size, NRPalette.Text, TextAlignmentOptions.Center, NRTextFx.OutlineShadow);
		NRUI.Place(t.rectTransform, new Vector2(0.5f, 1), pos, new Vector2(width, size + 16), new Vector2(0.5f, 1));
		var line = NRUI.Image(parent, "Header Line", NRSprites.White, NRPalette.BorderHi.WithAlpha(0.6f));
		NRUI.Place(line.rectTransform, new Vector2(0.5f, 1), pos + new Vector2(0, -(size + 22)), new Vector2(width * 0.8f, 3), new Vector2(0.5f, 1));
		return t;
	}
}

// ============================================================================
// ESC 일시정지 메뉴
// ============================================================================
public class NRPauseMenu : NRModal
{
	static NRPauseMenu instance;
	RectTransform content;
	GameObject settingsPage, controlsPage, summaryPage;
	NRButton resumeButton;

	public static void Show()
	{
		if (NRUIState.AnyOpen) return;
		if (instance == null) instance = Build();
		instance.Open();
	}

	static NRPauseMenu Build()
	{
		var root = NRModalHost.CreateModalRoot("NR Pause Menu");
		var menu = root.gameObject.AddComponent<NRPauseMenu>();

		var left = NRUI.Panel(root, "Nav");
		NRUI.Place(left.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-560, 0), new Vector2(420, 760));
		NRModalHost.Header(left.transform, "일시정지", new Vector2(0, -28), 380, 52);

		var col = NRUI.Rect(left.transform, "Buttons");
		NRUI.Place(col, new Vector2(0.5f, 1), new Vector2(0, -140), new Vector2(340, 560), new Vector2(0.5f, 1));
		var v = col.gameObject.AddComponent<VerticalLayoutGroup>();
		v.spacing = 18;
		v.childControlHeight = false;
		v.childControlWidth = false;
		v.childAlignment = TextAnchor.UpperCenter;

		menu.resumeButton = NRUI.Button(col, "이어하기", () => menu.Close(), new Vector2(340, 76), 34);
		NRUI.Button(col, "설정", () => menu.ShowPage(menu.settingsPage), new Vector2(340, 76), 34);
		NRUI.Button(col, "조작법", () => menu.ShowPage(menu.controlsPage), new Vector2(340, 76), 34);
		NRUI.Button(col, "메인 메뉴로", () => NRConfirm.Show("메인 메뉴로 돌아갈까요?",
			"진행 중인 회차(증강, 계층 진행)는 사라집니다.\n죽은 횟수, 대화 진행, 악몽 결정, 영구 강화는 저장됩니다.",
			() => NRSceneFlow.LoadScene(NRGame.SceneMainMenu)), new Vector2(340, 76), 34);
		NRUI.Button(col, "게임 종료", () => NRConfirm.Show("게임을 종료할까요?",
			"진행 중인 회차는 사라집니다. 영구 진행 상황은 저장됩니다.", NRSceneFlow.Quit), new Vector2(340, 76), 34);

		var right = NRUI.Panel(root, "Content");
		NRUI.Place(right.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(230, 0), new Vector2(1040, 760));
		menu.content = right.rectTransform;

		menu.summaryPage = NRUI.Rect(right.transform, "Summary").gameObject;
		NRUI.Stretch((RectTransform)menu.summaryPage.transform);
		menu.settingsPage = NRUI.Rect(right.transform, "Settings").gameObject;
		NRUI.Stretch((RectTransform)menu.settingsPage.transform);
		NRSettingsPanel.Build(menu.settingsPage.transform, false);
		menu.controlsPage = NRUI.Rect(right.transform, "Controls").gameObject;
		NRUI.Stretch((RectTransform)menu.controlsPage.transform);
		NRControlsPanel.Build(menu.controlsPage.transform);

		root.gameObject.SetActive(false);
		return menu;
	}

	protected override void OnOpened()
	{
		BuildSummary();
		ShowPage(summaryPage);
		NRUI.Select(resumeButton.button);
	}

	void ShowPage(GameObject page)
	{
		summaryPage.SetActive(page == summaryPage);
		settingsPage.SetActive(page == settingsPage);
		controlsPage.SetActive(page == controlsPage);
	}

	public override void RequestClose()
	{
		if (!summaryPage.activeSelf) { ShowPage(summaryPage); return; }
		Close();
	}

	void BuildSummary()
	{
		foreach (Transform c in summaryPage.transform) Destroy(c.gameObject);
		NRModalHost.Header(summaryPage.transform, "현재 상태", new Vector2(0, -28), 900, 48);

		var sb = new System.Text.StringBuilder();
		sb.AppendLine("<color=#" + NRPalette.ToHex(NRPalette.TextDim) + ">위치</color>   " + NRRun.LocationLabel());
		sb.AppendLine("<color=#" + NRPalette.ToHex(NRPalette.TextDim) + ">목표</color>   " + NRObjective.Title);
		sb.AppendLine();
		sb.AppendLine("<color=#" + NRPalette.ToHex(NRPalette.TextDim) + ">죽은 횟수</color>   " + Player.DeadCount + "     <color=#" + NRPalette.ToHex(NRPalette.TextDim) + ">클리어</color>   " + NRSave.Data.clears);
		sb.AppendLine("<color=#" + NRPalette.ToHex(NRPalette.TextDim) + ">악몽 결정</color>   <color=#" + NRPalette.ToHex(NRPalette.Cyan) + ">◈ " + NRSave.Data.crystals + "</color>     이번 회차 +" + NRRun.RunCrystals);
		sb.AppendLine("<color=#" + NRPalette.ToHex(NRPalette.TextDim) + ">보유 증강</color>   " + NRAugments.Owned.Count + "개");
		foreach (var o in NRAugments.Owned)
			sb.AppendLine("   <color=#" + NRPalette.ToHex(NRAugments.FamilyColor(o.def.family)) + ">" + o.def.name + "</color> Lv" + o.level + "  <size=24><color=#" + NRPalette.ToHex(NRPalette.TextDim) + ">" + o.def.DescribePlain(o.Value) + "</color></size>");

		var scroll = NRUI.ScrollView(summaryPage.transform, "Scroll", out var contentRt);
		NRUI.Place((RectTransform)scroll.transform, new Vector2(0.5f, 1), new Vector2(0, -120), new Vector2(960, 600), new Vector2(0.5f, 1));
		NRUI.LayoutText(contentRt, sb.ToString(), 30, NRPalette.Text);
		NRUI.LayoutText(contentRt, "\n<color=#" + NRPalette.ToHex(NRPalette.TextMute) + ">TAB 현황 창에서 증강·계층·스토리를 자세히 볼 수 있습니다.</color>", 24, NRPalette.Text);
	}
}

// ============================================================================
// 설정 패널 (일시정지 메뉴 / 메인 메뉴 공용)
// ============================================================================
public static class NRSettingsPanel
{
	public static void Build(Transform parent, bool mainMenu)
	{
		NRModalHost.Header(parent, "설정", new Vector2(0, -28), 900, 48);
		var scroll = NRUI.ScrollView(parent, "Scroll", out var content, 10, 24);
		NRUI.Place((RectTransform)scroll.transform, new Vector2(0.5f, 1), new Vector2(0, -120), new Vector2(960, 610), new Vector2(0.5f, 1));

		Section(content, "화면");
		var resolutions = NRSettings.GetResolutions();
		var resLabels = resolutions.Select(r => r.x + " × " + r.y).ToArray();
		int resIndex = Mathf.Max(0, resolutions.FindIndex(r => r.x == Screen.width && r.y == Screen.height));
		NRSelector modeSel = null;
		var resSel = Row(content, NRUI.Selector(content, "해상도", resLabels, resIndex, i =>
		{
			var r = resolutions[i];
			NRSettings.ApplyDisplay(r.x, r.y, modeSel != null ? modeSel.Index : NRSettings.CurrentModeIndex);
		}, 880));
		modeSel = Row(content, NRUI.Selector(content, "화면 모드", NRSettings.ModeNames, NRSettings.CurrentModeIndex, i =>
		{
			var r = resolutions[Mathf.Clamp(resSel.Index, 0, resolutions.Count - 1)];
			NRSettings.ApplyDisplay(r.x, r.y, i);
		}, 880));
		Row(content, NRUI.Selector(content, "수직 동기화", new[] { "끄기", "켜기" }, NRSettings.VSync ? 1 : 0, i => NRSettings.VSync = i == 1, 880));
		Row(content, NRUI.Selector(content, "최대 프레임", NRSettings.FpsOptions.Select((f, i) => NRSettings.FpsLabel(i)).ToArray(), NRSettings.FpsIndex, i => NRSettings.FpsIndex = i, 880));

		Section(content, "소리");
		Row(content, NRUI.Slider(content, "배경음악", NRAudio.MusicVolume, NRAudio.SetMusicVolume, 880));
		Row(content, NRUI.Slider(content, "효과음", NRAudio.SfxVolume, NRAudio.SetSfxVolume, 880));

		Section(content, "게임");
		Row(content, NRUI.Selector(content, "화면 흔들림", new[] { "끄기", "켜기" }, NRSettings.ScreenShake ? 1 : 0, i => NRSettings.ScreenShake = i == 1, 880));
		Row(content, NRUI.Selector(content, "피해 숫자 표시", new[] { "끄기", "켜기" }, NRSettings.DamageNumbers ? 1 : 0, i => NRSettings.DamageNumbers = i == 1, 880));
		Row(content, NRUI.Selector(content, "상호작용 안내", new[] { "끄기", "켜기" }, NRSettings.InteractHints ? 1 : 0, i => NRSettings.InteractHints = i == 1, 880));

		if (mainMenu)
		{
			Section(content, "데이터");
			var row = NRUI.HRow(content, "Reset Row", 76);
			var btn = NRUI.Button(row.transform, "진행 초기화", () => NRConfirm.Show("모든 진행을 초기화할까요?",
				"죽은 횟수, 대화 진행, 악몽 결정, 영구 강화, 기록이 모두 삭제됩니다.\n이 작업은 되돌릴 수 없습니다.",
				() => { NRSave.ResetProgress(); NRUIRoot.ToastMsg("진행을 초기화했습니다", NRPalette.Crimson); }), new Vector2(320, 70), 30);
			btn.gameObject.AddComponent<LayoutElement>().preferredWidth = 320;
		}
	}

	static T Row<T>(RectTransform content, T component) where T : Component
	{
		var le = component.gameObject.AddComponent<LayoutElement>();
		le.preferredHeight = 70;
		le.minHeight = 70;
		return component;
	}

	static void Section(RectTransform content, string title)
	{
		var t = NRUI.LayoutText(content, title, 30, NRPalette.Pink);
		t.GetComponent<LayoutElement>().preferredHeight = 52;
	}
}

// ============================================================================
// 조작법 (별도로 튀어나오던 안내를 메뉴 안으로)
// ============================================================================
public static class NRControlsPanel
{
	static readonly string[,] Keys =
	{
		{ "W A S D", "이동" },
		{ "마우스 좌클릭", "기본 공격 (마우스 방향)" },
		{ "마우스 우클릭", "특수 공격" },
		{ "R", "궁극기" },
		{ "Space", "구르기 (적의 몸을 통과)" },
		{ "E", "대화 / 다음 대사" },
		{ "좌클릭 또는 접촉", "문·침대·회오리로 이동" },
		{ "TAB", "현황 창 (증강·계층·스토리)" },
		{ "ESC", "일시정지 메뉴" },
		{ "1 / 2 / 3", "증강 선택" },
	};

	public static void Build(Transform parent)
	{
		NRModalHost.Header(parent, "조작법", new Vector2(0, -28), 900, 48);
		var list = NRUI.Rect(parent, "List");
		NRUI.Place(list, new Vector2(0.5f, 1), new Vector2(0, -130), new Vector2(900, 600), new Vector2(0.5f, 1));
		var v = list.gameObject.AddComponent<VerticalLayoutGroup>();
		v.spacing = 8;
		v.childControlHeight = true;
		v.childControlWidth = true;
		v.childForceExpandHeight = false;

		for (int i = 0; i < Keys.GetLength(0); i++)
		{
			var row = NRUI.HRow(list, "Row", 50, 24);
			var keyBg = NRUI.Image(row.transform, "Key", NRSprites.ButtonFrame, Color.white);
			var le = keyBg.gameObject.AddComponent<LayoutElement>();
			le.preferredWidth = 300;
			var k = NRUI.Label(keyBg.transform, Keys[i, 0], 26, NRPalette.Cyan, TextAlignmentOptions.Center);
			NRUI.Stretch(k.rectTransform);
			var d = NRUI.Label(row.transform, Keys[i, 1], 28, NRPalette.Text, TextAlignmentOptions.MidlineLeft);
			d.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
		}
	}
}

// ============================================================================
// 확인 대화상자
// ============================================================================
public class NRConfirm : NRModal
{
	TextMeshProUGUI title, body;
	Action onYes;
	NRButton noButton;

	public static void Show(string titleText, string bodyText, Action yes)
	{
		var root = NRModalHost.CreateModalRoot("NR Confirm", 0.6f);
		var c = root.gameObject.AddComponent<NRConfirm>();
		var panel = NRUI.Panel(root, "Panel", NRSprites.ColoredFrame(NRPalette.Pink));
		NRUI.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900, 380));
		c.title = NRUI.Label(panel.transform, titleText, 44, NRPalette.Text, TextAlignmentOptions.Center, NRTextFx.OutlineShadow);
		NRUI.Place(c.title.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -34), new Vector2(820, 60), new Vector2(0.5f, 1));
		c.body = NRUI.Label(panel.transform, bodyText, 28, NRPalette.TextDim, TextAlignmentOptions.Center);
		NRUI.Place(c.body.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -110), new Vector2(820, 140), new Vector2(0.5f, 1));
		c.onYes = yes;
		var yesBtn = NRUI.Button(panel.transform, "확인", () => { c.Close(); c.onYes?.Invoke(); }, new Vector2(260, 72), 32);
		NRUI.Place(yesBtn.Rect, new Vector2(0.5f, 0), new Vector2(-150, 34), new Vector2(260, 72), new Vector2(0.5f, 0));
		c.noButton = NRUI.Button(panel.transform, "취소", () => c.Close(), new Vector2(260, 72), 32);
		NRUI.Place(c.noButton.Rect, new Vector2(0.5f, 0), new Vector2(150, 34), new Vector2(260, 72), new Vector2(0.5f, 0));
		c.Open();
		NRUI.Select(c.noButton.button);
	}

	protected override void OnClosed()
	{
		Destroy(gameObject, 0.3f);
	}
}

// ============================================================================
// 씬 전환 / 종료
// ============================================================================
public static class NRSceneFlow
{
	static bool loading;

	public static void LoadScene(string sceneName)
	{
		if (loading) return;
		NRGame.Run(LoadRoutine(sceneName));
	}

	static IEnumerator LoadRoutine(string sceneName)
	{
		loading = true;
		NRSave.Save();
		if (NRUIRoot.Instance != null) yield return NRUIRoot.Instance.Fade(1f, 0.45f);
		NRAudio.StopMusic();
		NRTime.ResetAll();
		var op = SceneManager.LoadSceneAsync(sceneName);
		while (op != null && !op.isDone) yield return null;
		yield return new WaitForSecondsRealtime(0.15f);
		if (NRUIRoot.Instance != null) yield return NRUIRoot.Instance.Fade(0f, 0.5f);
		loading = false;
	}

	public static void Quit()
	{
		NRSave.Save();
#if UNITY_EDITOR
		UnityEditor.EditorApplication.isPlaying = false;
#else
		Application.Quit();
#endif
	}
}
