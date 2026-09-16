using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// ============================================================================
// 메인 메뉴 (기존 배경 아트 유지, 버튼/정보/설정/크레딧/종료 추가)
// ============================================================================
public class NRMainMenu : MonoBehaviour
{
	RectTransform settingsRoot;
	readonly List<RectTransform> embers = new List<RectTransform>();
	readonly List<float> emberSpeed = new List<float>();
	NRButton startButton;

	public static void Build(Scene scene)
	{
		// 기존 '게임 시작' 버튼은 숨김 (배경 이미지는 그대로)
		var oldCanvas = NRUtil.FindRoot(scene, "Canvas");
		if (oldCanvas != null)
		{
			var btn = NRUtil.FindDeep(oldCanvas.transform, "Button");
			if (btn != null) btn.gameObject.SetActive(false);
			// 배경이 화면 비율에 맞게 꽉 차도록
			var scaler = oldCanvas.GetComponent<CanvasScaler>();
			if (scaler != null)
			{
				scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
				scaler.referenceResolution = new Vector2(1920, 1080);
				scaler.matchWidthOrHeight = 0.5f;
			}
			var bg = NRUtil.FindDeep(oldCanvas.transform, "Image");
			if (bg != null)
			{
				var rt = (RectTransform)bg;
				NRUI.Stretch(rt);
				var img = bg.GetComponent<Image>();
				if (img != null) img.preserveAspect = false;
			}
		}

		var canvas = NRUI.CreateCanvas("NR Main Menu", 100);
		var menu = canvas.gameObject.AddComponent<NRMainMenu>();
		menu.Construct((RectTransform)canvas.transform);
		NRAudio.PlayMusic("menu_bgm", false);
	}

	void Construct(RectTransform root)
	{
		// 하단 그라데이션 띠
		var shade = NRUI.Image(root, "Shade", NRSprites.Vignette, new Color(0.02f, 0.01f, 0.06f, 0.55f));
		NRUI.Stretch(shade.rectTransform, -200, -200, -200, -200);

		for (int i = 0; i < 26; i++)
		{
			var e = NRUI.Image(root, "Ember", NRSprites.White, (i % 3 == 0 ? NRPalette.Cyan : NRPalette.Pink).WithAlpha(0.5f));
			float s = Random.Range(4f, 9f);
			e.rectTransform.sizeDelta = new Vector2(s, s);
			e.rectTransform.anchorMin = e.rectTransform.anchorMax = new Vector2(Random.value, 0);
			e.rectTransform.anchoredPosition = new Vector2(0, Random.Range(0, 1080));
			embers.Add(e.rectTransform);
			emberSpeed.Add(Random.Range(20f, 60f));
		}

		var panel = NRUI.Rect(root, "Buttons");
		NRUI.Place(panel, new Vector2(0.5f, 0), new Vector2(0, 90), new Vector2(460, 420), new Vector2(0.5f, 0));
		var v = panel.gameObject.AddComponent<VerticalLayoutGroup>();
		v.spacing = 16;
		v.childControlHeight = false;
		v.childControlWidth = false;
		v.childAlignment = TextAnchor.LowerCenter;

		string startLabel = NRSave.HasProgress ? "악몽으로 돌아가기" : "게임 시작";
		startButton = NRUI.Button(panel, startLabel, () => NRSceneFlow.LoadScene(NRGame.SceneDungeon), new Vector2(460, 84), 38);
		NRUI.Button(panel, "설정", () => settingsRoot.gameObject.SetActive(true), new Vector2(460, 72), 32);
		NRUI.Button(panel, "크레딧", () => NRCredits.Play(false, () => NRUIRoot.Instance.Fade(0f, 0.6f)), new Vector2(460, 72), 32);
		NRUI.Button(panel, "게임 종료", NRSceneFlow.Quit, new Vector2(460, 72), 32);

		var info = NRUI.Label(root, "", 26, NRPalette.TextDim, TextAlignmentOptions.BottomRight);
		NRUI.Place(info.rectTransform, new Vector2(1, 0), new Vector2(-36, 28), new Vector2(900, 80), new Vector2(1, 0));
		var d = NRSave.Data;
		info.text = NRSave.HasProgress
			? "죽은 횟수 " + d.deadCount + "   ·   클리어 " + d.clears + "   ·   <color=#" + NRPalette.ToHex(NRPalette.Cyan) + ">◈ " + d.crystals + "</color>"
			: "";
		var ver = NRUI.Label(root, "v" + Application.version, 22, NRPalette.TextMute, TextAlignmentOptions.BottomLeft);
		NRUI.Place(ver.rectTransform, new Vector2(0, 0), new Vector2(30, 28), new Vector2(400, 40), new Vector2(0, 0));

		// 설정 창
		settingsRoot = NRUI.Rect(root, "Settings");
		NRUI.Stretch(settingsRoot);
		var dim = NRUI.Image(settingsRoot, "Dim", NRSprites.White, new Color(0.02f, 0.01f, 0.05f, 0.8f), true);
		NRUI.Stretch(dim.rectTransform, -20, -20, -20, -20);
		var sp = NRUI.Panel(settingsRoot, "Panel");
		NRUI.Place(sp.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1040, 860));
		var inner = NRUI.Rect(sp.transform, "Inner");
		NRUI.Place(inner, new Vector2(0.5f, 1), Vector2.zero, new Vector2(1040, 780), new Vector2(0.5f, 1));
		NRSettingsPanel.Build(inner, true);
		var close = NRUI.Button(sp.transform, "닫기", () => settingsRoot.gameObject.SetActive(false), new Vector2(260, 66), 30);
		NRUI.Place(close.Rect, new Vector2(0.5f, 0), new Vector2(0, 24), new Vector2(260, 66), new Vector2(0.5f, 0));
		settingsRoot.gameObject.SetActive(false);

		StartCoroutine(Intro(panel));
	}

	IEnumerator Intro(RectTransform panel)
	{
		var cg = panel.gameObject.AddComponent<CanvasGroup>();
		cg.alpha = 0f;
		yield return new WaitForSecondsRealtime(0.3f);
		float t = 0f;
		while (t < 0.8f)
		{
			t += Time.unscaledDeltaTime;
			cg.alpha = t / 0.8f;
			yield return null;
		}
		cg.alpha = 1f;
		NRUI.Select(startButton.button);
	}

	void Update()
	{
		for (int i = 0; i < embers.Count; i++)
		{
			var e = embers[i];
			var p = e.anchoredPosition;
			p.y += emberSpeed[i] * Time.unscaledDeltaTime;
			p.x = Mathf.Sin(Time.unscaledTime * 0.5f + i) * 20f;
			if (p.y > 1100) p.y = -10;
			e.anchoredPosition = p;
		}
		if (settingsRoot.gameObject.activeSelf && NRInput.EscDown) settingsRoot.gameObject.SetActive(false);
	}
}
