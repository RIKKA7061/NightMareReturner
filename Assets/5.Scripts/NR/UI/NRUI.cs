using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// ============================================================================
// UI 빌더 (1920x1080 기준, 세로 기준 스케일)
// ============================================================================
public static class NRUI
{
	public const float RefWidth = 1920f;
	public const float RefHeight = 1080f;

	public static Canvas CreateCanvas(string name, int sortingOrder, Transform parent = null)
	{
		var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
		if (parent != null) go.transform.SetParent(parent, false);
		var canvas = go.GetComponent<Canvas>();
		canvas.renderMode = RenderMode.ScreenSpaceOverlay;
		canvas.sortingOrder = sortingOrder;
		var scaler = go.GetComponent<CanvasScaler>();
		scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
		scaler.referenceResolution = new Vector2(RefWidth, RefHeight);
		scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
		scaler.matchWidthOrHeight = 1f;
		scaler.referencePixelsPerUnit = 100f;
		return canvas;
	}

	/// <summary>씬 로드 후 호출: 씬에 EventSystem이 없을 때만 생성 (중복 방지)</summary>
	public static void EnsureEventSystem()
	{
		if (EventSystem.current != null || UnityEngine.Object.FindObjectOfType<EventSystem>() != null) return;
		new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
	}

	public static RectTransform Rect(Transform parent, string name)
	{
		var go = new GameObject(name, typeof(RectTransform));
		go.layer = 5;
		var rt = go.GetComponent<RectTransform>();
		rt.SetParent(parent, false);
		return rt;
	}

	/// <summary>앵커 한 점 기준 배치</summary>
	public static RectTransform Place(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size, Vector2? pivot = null)
	{
		rt.anchorMin = anchor;
		rt.anchorMax = anchor;
		rt.pivot = pivot ?? anchor;
		rt.anchoredPosition = pos;
		rt.sizeDelta = size;
		return rt;
	}

	/// <summary>부모 전체를 채움 (여백 지정)</summary>
	public static RectTransform Stretch(RectTransform rt, float left = 0, float right = 0, float top = 0, float bottom = 0)
	{
		rt.anchorMin = Vector2.zero;
		rt.anchorMax = Vector2.one;
		rt.pivot = new Vector2(0.5f, 0.5f);
		rt.offsetMin = new Vector2(left, bottom);
		rt.offsetMax = new Vector2(-right, -top);
		return rt;
	}

	public static Image Image(Transform parent, string name, Sprite sprite, Color color, bool raycast = false)
	{
		var rt = Rect(parent, name);
		var img = rt.gameObject.AddComponent<Image>();
		img.sprite = sprite;
		img.color = color;
		img.raycastTarget = raycast;
		if (sprite != null && sprite.border != Vector4.zero)
		{
			img.type = UnityEngine.UI.Image.Type.Sliced;
			img.pixelsPerUnitMultiplier = 1f / 3f;
		}
		return img;
	}

	public static Image Panel(Transform parent, string name, Sprite frame = null, bool blocksPointer = true)
	{
		var img = Image(parent, name, frame ?? NRSprites.PanelFrame, Color.white, blocksPointer);
		if (blocksPointer) img.gameObject.AddComponent<NRBlocksPointer>();
		return img;
	}

	public static TextMeshProUGUI Label(Transform parent, string text, float size, Color color,
		TextAlignmentOptions align = TextAlignmentOptions.Left, NRTextFx fx = NRTextFx.Shadow)
	{
		var rt = Rect(parent, "Label");
		var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
		rt.gameObject.AddComponent<NRKeepFont>();
		if (NRFont.Asset != null) NRFont.Style(t, fx);
		t.text = text;
		t.fontSize = size;
		t.color = color;
		t.alignment = align;
		t.raycastTarget = false;
		t.enableWordWrapping = true;
		t.overflowMode = TextOverflowModes.Overflow;
		t.lineSpacing = 8f;
		return t;
	}

	public static NRButton Button(Transform parent, string label, Action onClick, Vector2 size, float fontSize = 32f)
	{
		var img = Image(parent, "Button " + label, NRSprites.ButtonFrame, Color.white, true);
		img.rectTransform.sizeDelta = size;
		var btn = img.gameObject.AddComponent<Button>();
		btn.transition = Selectable.Transition.None;
		var nav = btn.navigation;
		nav.mode = Navigation.Mode.Automatic;
		btn.navigation = nav;
		var text = Label(img.transform, label, fontSize, NRPalette.Text, TextAlignmentOptions.Center);
		Stretch(text.rectTransform, 12, 12, 4, 4);
		var nb = img.gameObject.AddComponent<NRButton>();
		nb.Init(btn, img, text, onClick);
		return nb;
	}

	public static NRSlider Slider(Transform parent, string label, float value, Action<float> onChanged, float width = 760f)
	{
		var row = Rect(parent, "Slider " + label);
		row.sizeDelta = new Vector2(width, 64);
		var lbl = Label(row, label, 30, NRPalette.Text, TextAlignmentOptions.MidlineLeft);
		Place(lbl.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0), new Vector2(260, 60), new Vector2(0, 0.5f));

		var bg = Image(row, "Track", NRSprites.DarkFrame, Color.white, true);
		Place(bg.rectTransform, new Vector2(0, 0.5f), new Vector2(280, 0), new Vector2(width - 400, 36), new Vector2(0, 0.5f));
		var fillArea = Rect(bg.transform, "Fill Area");
		Stretch(fillArea, 8, 8, 8, 8);
		var fill = Image(fillArea, "Fill", NRSprites.White, NRPalette.Pink, false);
		Stretch(fill.rectTransform);
		var handleArea = Rect(bg.transform, "Handle Area");
		Stretch(handleArea, 8, 8, 0, 0);
		var handle = Image(handleArea, "Handle", NRSprites.ButtonHoverFrame, Color.white, true);
		handle.rectTransform.sizeDelta = new Vector2(28, 48);

		var slider = bg.gameObject.AddComponent<Slider>();
		slider.fillRect = fill.rectTransform;
		slider.handleRect = handle.rectTransform;
		slider.targetGraphic = handle;
		slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
		slider.minValue = 0f;
		slider.maxValue = 1f;
		slider.transition = Selectable.Transition.ColorTint;
		var colors = slider.colors;
		colors.highlightedColor = new Color(1f, 0.85f, 1f);
		colors.selectedColor = new Color(1f, 0.8f, 1f);
		slider.colors = colors;
		slider.SetValueWithoutNotify(value);

		var valueText = Label(row, Mathf.RoundToInt(value * 100) + "%", 30, NRPalette.TextDim, TextAlignmentOptions.MidlineRight);
		Place(valueText.rectTransform, new Vector2(1, 0.5f), new Vector2(0, 0), new Vector2(100, 60), new Vector2(1, 0.5f));

		var ns = row.gameObject.AddComponent<NRSlider>();
		ns.slider = slider;
		slider.onValueChanged.AddListener(v =>
		{
			valueText.text = Mathf.RoundToInt(v * 100) + "%";
			onChanged?.Invoke(v);
		});
		return ns;
	}

	public static NRSelector Selector(Transform parent, string label, string[] options, int index, Action<int> onChanged, float width = 760f)
	{
		var row = Rect(parent, "Selector " + label);
		row.sizeDelta = new Vector2(width, 64);
		var lbl = Label(row, label, 30, NRPalette.Text, TextAlignmentOptions.MidlineLeft);
		Place(lbl.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0), new Vector2(260, 60), new Vector2(0, 0.5f));

		var sel = row.gameObject.AddComponent<NRSelector>();
		float boxW = width - 280;
		var left = Button(row, "◀", () => sel.Step(-1), new Vector2(64, 56), 28);
		Place(left.Rect, new Vector2(0, 0.5f), new Vector2(280, 0), new Vector2(64, 56), new Vector2(0, 0.5f));
		var box = Image(row, "Value", NRSprites.DarkFrame, Color.white, false);
		Place(box.rectTransform, new Vector2(0, 0.5f), new Vector2(352, 0), new Vector2(boxW - 144, 56), new Vector2(0, 0.5f));
		var valueText = Label(box.transform, "", 30, NRPalette.Cyan, TextAlignmentOptions.Center);
		Stretch(valueText.rectTransform, 6, 6, 2, 2);
		var right = Button(row, "▶", () => sel.Step(1), new Vector2(64, 56), 28);
		Place(right.Rect, new Vector2(1, 0.5f), new Vector2(0, 0), new Vector2(64, 56), new Vector2(1, 0.5f));

		sel.Init(options, index, valueText, onChanged);
		return sel;
	}

	public static ScrollRect ScrollView(Transform parent, string name, out RectTransform content, float spacing = 12f, int padding = 16)
	{
		var root = Image(parent, name, NRSprites.DarkFrame, Color.white, true);
		var scroll = root.gameObject.AddComponent<ScrollRect>();
		scroll.horizontal = false;
		scroll.movementType = ScrollRect.MovementType.Clamped;
		scroll.scrollSensitivity = 40f;

		var viewport = Rect(root.transform, "Viewport");
		Stretch(viewport, 8, 28, 8, 8);
		viewport.gameObject.AddComponent<RectMask2D>();
		var vpImg = viewport.gameObject.AddComponent<Image>();
		vpImg.color = new Color(0, 0, 0, 0.001f);
		scroll.viewport = viewport;

		content = Rect(viewport, "Content");
		content.anchorMin = new Vector2(0, 1);
		content.anchorMax = new Vector2(1, 1);
		content.pivot = new Vector2(0.5f, 1);
		content.anchoredPosition = Vector2.zero;
		content.sizeDelta = new Vector2(0, 0);
		var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
		layout.spacing = spacing;
		layout.padding = new RectOffset(padding, padding, padding, padding);
		layout.childControlWidth = true;
		layout.childControlHeight = true;
		layout.childForceExpandWidth = true;
		layout.childForceExpandHeight = false;
		var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
		fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
		scroll.content = content;

		// 스크롤바
		var barBg = Image(root.transform, "Scrollbar", NRSprites.White, NRPalette.Bg0.WithAlpha(0.8f), true);
		barBg.rectTransform.anchorMin = new Vector2(1, 0);
		barBg.rectTransform.anchorMax = new Vector2(1, 1);
		barBg.rectTransform.pivot = new Vector2(1, 0.5f);
		barBg.rectTransform.sizeDelta = new Vector2(14, -16);
		barBg.rectTransform.anchoredPosition = new Vector2(-8, 0);
		var handleArea = Rect(barBg.transform, "Sliding Area");
		Stretch(handleArea);
		var handle = Image(handleArea, "Handle", NRSprites.White, NRPalette.BorderHi, true);
		Stretch(handle.rectTransform);
		var bar = barBg.gameObject.AddComponent<Scrollbar>();
		bar.handleRect = handle.rectTransform;
		bar.targetGraphic = handle;
		bar.direction = Scrollbar.Direction.BottomToTop;
		scroll.verticalScrollbar = bar;
		scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
		return scroll;
	}

	/// <summary>레이아웃 그룹 안에서 쓸 고정 높이 텍스트 블록</summary>
	public static TextMeshProUGUI LayoutText(Transform parent, string text, float size, Color color, TextAlignmentOptions align = TextAlignmentOptions.TopLeft)
	{
		var t = Label(parent, text, size, color, align);
		var le = t.gameObject.AddComponent<LayoutElement>();
		le.flexibleWidth = 1;
		return t;
	}

	public static HorizontalLayoutGroup HRow(Transform parent, string name, float height, float spacing = 12f)
	{
		var rt = Rect(parent, name);
		var le = rt.gameObject.AddComponent<LayoutElement>();
		le.preferredHeight = height;
		le.minHeight = height;
		var h = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
		h.spacing = spacing;
		h.childControlHeight = true;
		h.childControlWidth = true;
		h.childForceExpandHeight = true;
		h.childForceExpandWidth = false;
		h.childAlignment = TextAnchor.MiddleLeft;
		return h;
	}

	public static Image Icon(Transform parent, Sprite sprite, float size, Color? color = null)
	{
		var img = Image(parent, "Icon", sprite, color ?? Color.white, false);
		img.preserveAspect = true;
		img.rectTransform.sizeDelta = new Vector2(size, size);
		return img;
	}

	public static void Select(Selectable s)
	{
		if (s == null || EventSystem.current == null) return;
		EventSystem.current.SetSelectedGameObject(null);
		EventSystem.current.SetSelectedGameObject(s.gameObject);
	}
}

// ---------------------------------------------------------------------------
// 버튼 (호버/선택 시 프레임 교체 + 살짝 커짐 + 효과음)
// ---------------------------------------------------------------------------
public class NRButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler, IPointerDownHandler, IPointerUpHandler
{
	public Button button;
	public Image frame;
	public TextMeshProUGUI label;
	public RectTransform Rect => (RectTransform)transform;
	public Color normalText = NRPalette.Text;
	public Color hoverText = Color.white;
	bool hovered, selected;
	Vector3 targetScale = Vector3.one;
	Sprite normalSprite, hoverSprite, pressSprite;

	public void Init(Button b, Image f, TextMeshProUGUI l, Action onClick)
	{
		button = b; frame = f; label = l;
		normalSprite = NRSprites.ButtonFrame;
		hoverSprite = NRSprites.ButtonHoverFrame;
		pressSprite = NRSprites.ButtonPressFrame;
		if (onClick != null) button.onClick.AddListener(() => { NRAudio.PlayUI(); onClick(); });
		Refresh();
	}

	public void SetColors(Sprite normal, Sprite hover)
	{
		normalSprite = normal; hoverSprite = hover;
		Refresh();
	}

	public void SetInteractable(bool v)
	{
		button.interactable = v;
		Refresh();
	}

	void Refresh()
	{
		bool active = (hovered || selected) && button.interactable;
		if (frame != null) frame.sprite = active ? hoverSprite : normalSprite;
		if (label != null) label.color = !button.interactable ? NRPalette.TextMute : active ? hoverText : normalText;
		targetScale = active ? new Vector3(1.04f, 1.04f, 1f) : Vector3.one;
	}

	public void OnPointerEnter(PointerEventData e) { hovered = true; Refresh(); }
	public void OnPointerExit(PointerEventData e) { hovered = false; Refresh(); }
	public void OnSelect(BaseEventData e) { selected = true; Refresh(); }
	public void OnDeselect(BaseEventData e) { selected = false; Refresh(); }
	public void OnPointerDown(PointerEventData e) { if (button.interactable && frame != null) frame.sprite = pressSprite; }
	public void OnPointerUp(PointerEventData e) { Refresh(); }

	void OnDisable() { hovered = false; selected = false; transform.localScale = Vector3.one; }

	void Update()
	{
		transform.localScale = Vector3.Lerp(transform.localScale, targetScale, 1f - Mathf.Exp(-18f * Time.unscaledDeltaTime));
	}
}

public class NRSlider : MonoBehaviour
{
	public Slider slider;
}

public class NRSelector : MonoBehaviour
{
	string[] options;
	int index;
	TextMeshProUGUI text;
	Action<int> onChanged;

	public int Index => index;

	public void Init(string[] opts, int idx, TextMeshProUGUI valueText, Action<int> changed)
	{
		options = opts;
		index = Mathf.Clamp(idx, 0, Mathf.Max(0, opts.Length - 1));
		text = valueText;
		onChanged = changed;
		Refresh();
	}

	public void SetOptions(string[] opts, int idx)
	{
		options = opts;
		index = Mathf.Clamp(idx, 0, Mathf.Max(0, opts.Length - 1));
		Refresh();
	}

	public void Step(int dir)
	{
		if (options == null || options.Length == 0) return;
		index = (index + dir + options.Length) % options.Length;
		Refresh();
		onChanged?.Invoke(index);
	}

	void Refresh()
	{
		if (text != null && options != null && options.Length > 0) text.text = options[index];
	}
}

// ---------------------------------------------------------------------------
// 모달 창 기반 클래스 + 전역 UI 상태
// ---------------------------------------------------------------------------
public abstract class NRModal : MonoBehaviour
{
	public virtual bool PausesGame => true;
	public virtual bool BlocksGameplay => true;
	public virtual bool CloseOnEsc => true;
	public bool IsOpen { get; private set; }

	protected CanvasGroup group;
	string PauseKey => "modal:" + GetInstanceID();

	protected virtual void Awake()
	{
		group = GetComponent<CanvasGroup>();
		if (group == null) group = gameObject.AddComponent<CanvasGroup>();
	}

	public void Open()
	{
		if (IsOpen) return;
		IsOpen = true;
		gameObject.SetActive(true);
		NRUIState.Push(this);
		if (PausesGame) NRTime.Pause(PauseKey);
		OnOpened();
		StopAllCoroutines();
		StartCoroutine(FadeGroup(0f, 1f, 0.12f));
	}

	public void Close()
	{
		if (!IsOpen) return;
		IsOpen = false;
		NRUIState.Remove(this);
		NRTime.Resume(PauseKey);
		OnClosed();
		if (gameObject.activeInHierarchy)
		{
			StopAllCoroutines();
			StartCoroutine(FadeOutAndHide());
		}
		else gameObject.SetActive(false);
	}

	/// <summary>ESC로 닫기 요청 (하위 화면이 있으면 뒤로가기로 처리 가능)</summary>
	public virtual void RequestClose() => Close();

	protected virtual void OnOpened() { }
	protected virtual void OnClosed() { }

	protected virtual void OnDestroy()
	{
		if (IsOpen)
		{
			NRUIState.Remove(this);
			NRTime.Resume(PauseKey);
		}
	}

	IEnumerator FadeGroup(float from, float to, float dur)
	{
		float t = 0f;
		group.alpha = from;
		transform.localScale = Vector3.one * 0.98f;
		while (t < dur)
		{
			t += Time.unscaledDeltaTime;
			float k = Mathf.Clamp01(t / dur);
			group.alpha = Mathf.Lerp(from, to, k);
			transform.localScale = Vector3.one * Mathf.Lerp(0.98f, 1f, k);
			yield return null;
		}
		group.alpha = to;
		transform.localScale = Vector3.one;
	}

	IEnumerator FadeOutAndHide()
	{
		yield return FadeGroup(group.alpha, 0f, 0.1f);
		gameObject.SetActive(false);
	}
}

public static class NRUIState
{
	static readonly List<NRModal> stack = new List<NRModal>();

	public static void Push(NRModal m) { stack.Remove(m); stack.Add(m); }
	public static void Remove(NRModal m) { stack.Remove(m); }
	public static void ResetAll() { stack.Clear(); }

	public static NRModal Top
	{
		get
		{
			for (int i = stack.Count - 1; i >= 0; i--)
			{
				if (stack[i] == null) { stack.RemoveAt(i); continue; }
				return stack[i];
			}
			return null;
		}
	}

	public static bool AnyOpen => Top != null;

	/// <summary>플레이어 조작(공격/대화/구르기)을 막아야 하는지</summary>
	public static bool IsGameplayBlocked
	{
		get
		{
			for (int i = stack.Count - 1; i >= 0; i--)
			{
				if (stack[i] != null && stack[i].BlocksGameplay) return true;
			}
			return NRCredits.IsPlaying || NRTime.IsPaused;
		}
	}

	public static bool IsOpen<T>() where T : NRModal
	{
		foreach (var m in stack) if (m is T && m != null) return true;
		return false;
	}

	public static void HandleGlobalKeys()
	{
		if (NRCredits.IsPlaying) return;
		var scene = NRGame.Instance != null ? NRGame.Instance.CurrentScene : "";
		bool inGame = scene == NRGame.SceneDungeon || scene == NRGame.SceneTown;

		if (NRInput.EscDown)
		{
			var top = Top;
			if (top != null)
			{
				if (top.CloseOnEsc) top.RequestClose();
				return;
			}
			if (!inGame) return;
			if (NRScenePatcher.TryCloseLegacyPanels()) return;
			var player = NRUtil.FindPlayer();
			if (player != null && player.isDead) return;
			NRPauseMenu.Show();
			return;
		}

		if (NRInput.TabDown && inGame)
		{
			var top = Top;
			if (top is NRStatusWindow) { top.Close(); return; }
			if (top != null) return;
			NRStatusWindow.Show();
		}
	}
}

// ---------------------------------------------------------------------------
// 전역 오버레이 (페이드, 배너, 토스트) - 씬이 바뀌어도 유지
// ---------------------------------------------------------------------------
public class NRUIRoot : MonoBehaviour
{
	public static NRUIRoot Instance { get; private set; }
	Canvas canvas;
	Image fader;
	RectTransform toastRoot;
	RectTransform bannerRoot;
	TextMeshProUGUI bannerTitle, bannerSub;
	CanvasGroup bannerGroup;
	Coroutine bannerRoutine;
	Coroutine fadeRoutine;

	public Canvas Canvas => canvas;

	void Awake()
	{
		Instance = this;
		canvas = NRUI.CreateCanvas("NR Overlay Canvas", 900, transform);

		bannerRoot = NRUI.Rect(canvas.transform, "Banner");
		NRUI.Place(bannerRoot, new Vector2(0.5f, 0.62f), Vector2.zero, new Vector2(1400, 220));
		bannerGroup = bannerRoot.gameObject.AddComponent<CanvasGroup>();
		bannerGroup.alpha = 0f;
		bannerGroup.blocksRaycasts = false;
		var bannerBg = NRUI.Image(bannerRoot, "Band", NRSprites.White, new Color(0.02f, 0.01f, 0.05f, 0.72f));
		NRUI.Place(bannerBg.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(3000, 190));
		var line1 = NRUI.Image(bannerRoot, "Line Top", NRSprites.White, NRPalette.BorderHi.WithAlpha(0.8f));
		NRUI.Place(line1.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 95), new Vector2(3000, 3));
		var line2 = NRUI.Image(bannerRoot, "Line Bottom", NRSprites.White, NRPalette.BorderHi.WithAlpha(0.8f));
		NRUI.Place(line2.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -95), new Vector2(3000, 3));
		bannerTitle = NRUI.Label(bannerRoot, "", 80, NRPalette.Text, TextAlignmentOptions.Center, NRTextFx.OutlineShadow);
		NRUI.Place(bannerTitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 22), new Vector2(1600, 110));
		bannerSub = NRUI.Label(bannerRoot, "", 36, NRPalette.Pink, TextAlignmentOptions.Center);
		NRUI.Place(bannerSub.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -50), new Vector2(1600, 60));

		toastRoot = NRUI.Rect(canvas.transform, "Toasts");
		NRUI.Place(toastRoot, new Vector2(0.5f, 0f), new Vector2(0, 250), new Vector2(900, 400), new Vector2(0.5f, 0f));
		var vl = toastRoot.gameObject.AddComponent<VerticalLayoutGroup>();
		vl.childAlignment = TextAnchor.LowerCenter;
		vl.spacing = 10;
		vl.childControlHeight = true;
		vl.childControlWidth = true;
		vl.childForceExpandHeight = false;

		fader = NRUI.Image(canvas.transform, "Fader", NRSprites.White, new Color(0, 0, 0, 0), false);
		NRUI.Stretch(fader.rectTransform, -10, -10, -10, -10);
		fader.gameObject.SetActive(false);
	}

	// ---- 페이드 ----
	public Coroutine Fade(float to, float duration)
	{
		if (fadeRoutine != null) StopCoroutine(fadeRoutine);
		fadeRoutine = StartCoroutine(FadeRoutine(to, duration));
		return fadeRoutine;
	}

	public bool IsFaded => fader.gameObject.activeSelf && fader.color.a > 0.5f;

	IEnumerator FadeRoutine(float to, float duration)
	{
		fader.gameObject.SetActive(true);
		fader.raycastTarget = to > 0.5f;
		float from = fader.color.a;
		float t = 0f;
		while (t < duration)
		{
			t += Time.unscaledDeltaTime;
			fader.color = new Color(0, 0, 0, Mathf.Lerp(from, to, Mathf.Clamp01(t / duration)));
			yield return null;
		}
		fader.color = new Color(0, 0, 0, to);
		if (to <= 0.001f) fader.gameObject.SetActive(false);
		fadeRoutine = null;
	}

	// ---- 배너 (계층 진입 등) ----
	public void ShowBanner(string title, string subtitle, Color titleColor, float hold = 2.2f)
	{
		if (bannerRoutine != null) StopCoroutine(bannerRoutine);
		bannerRoutine = StartCoroutine(BannerRoutine(title, subtitle, titleColor, hold));
	}

	IEnumerator BannerRoutine(string title, string subtitle, Color color, float hold)
	{
		bannerTitle.text = title;
		bannerTitle.color = color;
		bannerSub.text = subtitle;
		bannerSub.gameObject.SetActive(!string.IsNullOrEmpty(subtitle));
		float t = 0f;
		while (t < 0.35f)
		{
			t += Time.unscaledDeltaTime;
			float k = t / 0.35f;
			bannerGroup.alpha = k;
			bannerTitle.characterSpacing = Mathf.Lerp(40f, 6f, k);
			yield return null;
		}
		bannerGroup.alpha = 1f;
		yield return new WaitForSecondsRealtime(hold);
		t = 0f;
		while (t < 0.5f)
		{
			t += Time.unscaledDeltaTime;
			bannerGroup.alpha = 1f - t / 0.5f;
			yield return null;
		}
		bannerGroup.alpha = 0f;
		bannerRoutine = null;
	}

	// ---- 토스트 ----
	public void Toast(string message, Color? accent = null, float duration = 2.6f)
	{
		var bg = NRUI.Image(toastRoot, "Toast", NRSprites.ColoredFrame(accent ?? NRPalette.BorderHi), Color.white);
		var le = bg.gameObject.AddComponent<LayoutElement>();
		le.preferredHeight = 64;
		var text = NRUI.Label(bg.transform, message, 30, NRPalette.Text, TextAlignmentOptions.Center);
		NRUI.Stretch(text.rectTransform, 20, 20, 4, 4);
		var cg = bg.gameObject.AddComponent<CanvasGroup>();
		StartCoroutine(ToastRoutine(bg.gameObject, cg, duration));
		while (toastRoot.childCount > 4) Destroy(toastRoot.GetChild(0).gameObject);
	}

	IEnumerator ToastRoutine(GameObject go, CanvasGroup cg, float duration)
	{
		float t = 0f;
		while (t < 0.2f && go != null) { t += Time.unscaledDeltaTime; cg.alpha = t / 0.2f; yield return null; }
		yield return new WaitForSecondsRealtime(duration);
		t = 0f;
		while (t < 0.4f && go != null) { t += Time.unscaledDeltaTime; cg.alpha = 1f - t / 0.4f; yield return null; }
		if (go != null) Destroy(go);
	}

	public static void ToastMsg(string message, Color? accent = null, float duration = 2.6f)
	{
		if (Instance != null) Instance.Toast(message, accent, duration);
	}

	public static void Banner(string title, string subtitle, Color color, float hold = 2.2f)
	{
		if (Instance != null) Instance.ShowBanner(title, subtitle, color, hold);
	}
}
