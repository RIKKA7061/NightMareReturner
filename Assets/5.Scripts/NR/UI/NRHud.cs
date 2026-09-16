using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// ============================================================================
// 인게임 HUD
//  좌상단: 체력/보호막, 재화·구슬·악몽 결정, 상점 아이템 버프
//  상단 중앙: 현재 위치 + 목표 카드 (갱신 시 3회만 강조)
//  우상단: [TAB] 스테이터스 / [ESC] 메뉴
//  좌하단: 보유 증강 아이콘 (마우스를 올리면 설명)
//  하단 중앙: 공격/특수/궁극기/구르기 재사용 대기
//  상단: 보스 체력바, 화면 가장자리: 화면 밖 적 방향 표시
// ============================================================================
public static class NRObjective
{
	public static string Title { get; private set; } = "";
	public static string Body { get; private set; } = "";
	public static bool Combat { get; private set; }
	public static int Version { get; private set; }

	public static void Set(string title, string body, bool combat)
	{
		bool changed = title != Title || body != Body;
		Title = title;
		Body = body;
		Combat = combat;
		if (changed) Version++;
	}
}


public class NRHud : MonoBehaviour
{
	static NRHud instance;
	public static NRHud Instance => instance;

	Canvas canvas;
	Player player;
	PlayerAction action;
	ItemManager items;
	bool dungeon;

	// 체력 (던전에서만 표시)
	RectTransform panelRect;
	GameObject heartGo, hpBarGo;
	Image hpFill, hpLag, shieldFill;
	TextMeshProUGUI hpText;
	float lagValue = 1f;

	// 자원
	TextMeshProUGUI moneyText, orbText, crystalText, locationText;
	RectTransform moneyRow, orbRow, crystalRow;

	// 목표
	RectTransform objectiveCard;
	Image objectiveFrame;
	TextMeshProUGUI objectiveTitle, objectiveBody, objectiveCounter, objectiveNew;
	int objectiveVersion = -1;
	Coroutine objectivePulse;

	// 증강 / 패시브
	RectTransform augmentRow;
	readonly List<GameObject> augmentIcons = new List<GameObject>();
	string passiveSignature = "";

	// 대화 중에는 하단 HUD를 흐리게
	CanvasGroup bottomGroup;
	TalkManager talkManager;

	// 스킬 바 (LoL 스타일)
	class Slot
	{
		public Image frame, cover, icon;
		public TextMeshProUGUI key, name, timer;
	}
	Slot passiveSlot, attackSlot, skillSlot, dashSlot, ultSlot;
	int lastScheme = -1;
	int lastHero = -1;

	// 보스
	RectTransform bossBar;
	Image bossFill, bossLag;
	TextMeshProUGUI bossName, bossPhase;
	MonsterHP bossHp;
	MonsterAI bossAi;
	float bossLagValue = 1f;

	// 화면 효과
	Image vignette, damageFlash, lowHpVignette;
	float damageFlashAlpha;

	// 화면 밖 표시
	readonly List<Image> arrows = new List<Image>();
	class Waymark { public RectTransform root; public Image arrow; public TextMeshProUGUI label; }
	readonly List<Waymark> waymarks = new List<Waymark>();

	public static void Create(bool dungeonScene)
	{
		if (instance != null) Destroy(instance.gameObject);
		var go = new GameObject("NR HUD");
		instance = go.AddComponent<NRHud>();
		instance.dungeon = dungeonScene;
		instance.Build();
	}

	void Build()
	{
		canvas = NRUI.CreateCanvas("NR HUD Canvas", 50, transform);
		var root = (RectTransform)canvas.transform;

		// ---- 화면 효과 (맨 아래) ----
		vignette = NRUI.Image(root, "Vignette", NRSprites.Vignette, new Color(0.1f, 0.05f, 0.2f, 0.3f));
		NRUI.Stretch(vignette.rectTransform, -40, -40, -40, -40);
		lowHpVignette = NRUI.Image(root, "LowHp", NRSprites.Vignette, new Color(0.9f, 0.05f, 0.15f, 0f));
		NRUI.Stretch(lowHpVignette.rectTransform, -40, -40, -40, -40);
		damageFlash = NRUI.Image(root, "DamageFlash", NRSprites.Vignette, new Color(1f, 0.1f, 0.2f, 0f));
		NRUI.Stretch(damageFlash.rectTransform, -200, -200, -200, -200);

		BuildPlayerPanel(root);
		BuildObjective(root);
		BuildTopRight(root);
		var bottom = NRUI.Rect(root, "Bottom HUD");
		NRUI.Stretch(bottom);
		bottomGroup = bottom.gameObject.AddComponent<CanvasGroup>();
		bottomGroup.blocksRaycasts = true;
		BuildAugmentRow(bottom);
		if (dungeon) BuildSkillBar(bottom);
		BuildBossBar(root);

		for (int i = 0; i < 8; i++)
		{
			var a = NRUI.Image(root, "EnemyArrow", NRSprites.ArrowRight, NRPalette.Crimson);
			a.rectTransform.sizeDelta = new Vector2(34, 56);
			a.gameObject.SetActive(false);
			arrows.Add(a);
		}
		for (int i = 0; i < 6; i++)
		{
			var r = NRUI.Rect(root, "Waypoint");
			r.sizeDelta = new Vector2(200, 90);
			var arrow = NRUI.Image(r, "Arrow", NRSprites.ArrowRight, Color.white);
			NRUI.Place(arrow.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(40, 66));
			var lbl = NRUI.Label(r, "", 24, Color.white, TextAlignmentOptions.Center, NRTextFx.OutlineShadow);
			NRUI.Place(lbl.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -52), new Vector2(240, 34));
			r.gameObject.SetActive(false);
			waymarks.Add(new Waymark { root = r, arrow = arrow, label = lbl });
		}

		NRAugments.OnChanged += RebuildAugments;
		RebuildAugments();
	}

	void OnDestroy()
	{
		NRAugments.OnChanged -= RebuildAugments;
		if (instance == this) instance = null;
	}

	static void AddTooltip(GameObject go, System.Func<string> text)
	{
		var g = go.GetComponent<Graphic>();
		if (g == null)
		{
			var img = go.AddComponent<Image>();
			img.color = new Color(0, 0, 0, 0.001f);
			g = img;
		}
		g.raycastTarget = true;
		var t = go.GetComponent<NRHoverTooltip>();
		if (t == null) t = go.AddComponent<NRHoverTooltip>();
		t.getText = text;
	}

	static string C(Color c) => "<color=#" + NRPalette.ToHex(c) + ">";

	// ---------------------------------------------------------------------
	void BuildPlayerPanel(RectTransform root)
	{
		var panel = NRUI.Image(root, "Player Panel", NRSprites.DarkFrame, new Color(1, 1, 1, 0.92f));
		panelRect = panel.rectTransform;
		NRUI.Place(panelRect, new Vector2(0, 1), new Vector2(24, -24), new Vector2(470, 118), new Vector2(0, 1));

		var heart = NRUI.Icon(panel.transform, NRSprites.Heart, 34, NRPalette.Hp);
		heartGo = heart.gameObject;
		NRUI.Place(heart.rectTransform, new Vector2(0, 1), new Vector2(22, -22), new Vector2(34, 34), new Vector2(0, 1));

		var barBg = NRUI.Image(panel.transform, "HP Back", NRSprites.White, NRPalette.HpBack);
		hpBarGo = barBg.gameObject;
		NRUI.Place(barBg.rectTransform, new Vector2(0, 1), new Vector2(68, -24), new Vector2(380, 30), new Vector2(0, 1));
		hpLag = NRUI.Image(barBg.transform, "HP Lag", NRSprites.White, new Color(1f, 0.85f, 0.85f, 0.85f));
		SetupFill(hpLag);
		hpFill = NRUI.Image(barBg.transform, "HP Fill", NRSprites.White, NRPalette.Hp);
		SetupFill(hpFill);
		shieldFill = NRUI.Image(barBg.transform, "Shield", NRSprites.White, NRPalette.Shield.WithAlpha(0.75f));
		SetupFill(shieldFill);
		var hpBorder = NRUI.Image(barBg.transform, "Border", NRSprites.Frame(Color.clear, NRPalette.Border, NRPalette.BorderHi.WithAlpha(0.4f), NRPalette.Bg0), Color.white);
		NRUI.Stretch(hpBorder.rectTransform, -6, -6, -6, -6);
		hpText = NRUI.Label(barBg.transform, "", 24, Color.white, TextAlignmentOptions.Center, NRTextFx.OutlineShadow);
		NRUI.Stretch(hpText.rectTransform);
		AddTooltip(barBg.gameObject, () =>
			C(NRPalette.Hp) + "체력</color>\n0이 되면 쓰러져 집에서 다시 시작합니다.\n" +
			C(NRPalette.TextDim) + "회복: 적 처치(패시브 " + NRHero.KillHeal + "), 상점 물약, 보상.\n" +
			C(NRPalette.Shield) + "하늘색</color> 부분은 보호막 — 체력보다 먼저 닳습니다.</color>");

		moneyText = ResourceItem(panel.transform, NRSprites.Coin, NRPalette.Gold, new Vector2(22, -70), out moneyRow,
			() => C(NRPalette.Gold) + "재화</color>\n적을 처치하면 떨어지는 코인.\n상점에서 물약·장비·패시브를 살 수 있습니다.");
		orbText = ResourceItem(panel.transform, NRSprites.Orb, NRPalette.Anxiety, new Vector2(170, -70), out orbRow,
			() => C(NRPalette.Anxiety) + "감정 구슬</color>\n각성의 방과 감정의 방에서 얻습니다.\n1개 이상이면 각성한 클래스의 기술을 씁니다.");
		crystalText = ResourceItem(panel.transform, NRSprites.Diamond, NRPalette.Cyan, new Vector2(310, -70), out crystalRow,
			() => C(NRPalette.Cyan) + "악몽 결정 (영구)</color>\n방 정리·수문장 처치로 얻고, 죽어도 사라지지 않습니다.\n집의 '기억의 책장'에서 영구 강화에 사용합니다.");
	}

	static void SetupFill(Image img)
	{
		img.type = Image.Type.Filled;
		img.fillMethod = Image.FillMethod.Horizontal;
		img.fillOrigin = 0;
		img.fillAmount = 1f;
		NRUI.Stretch(img.rectTransform);
	}

	TextMeshProUGUI ResourceItem(Transform parent, Sprite icon, Color color, Vector2 pos, out RectTransform row, System.Func<string> tip)
	{
		row = NRUI.Rect(parent, "Resource");
		NRUI.Place(row, new Vector2(0, 1), pos, new Vector2(140, 36), new Vector2(0, 1));
		var img = NRUI.Icon(row, icon, 28, color);
		NRUI.Place(img.rectTransform, new Vector2(0, 0.5f), Vector2.zero, new Vector2(28, 28), new Vector2(0, 0.5f));
		var t = NRUI.Label(row, "0", 28, NRPalette.Text, TextAlignmentOptions.MidlineLeft);
		NRUI.Place(t.rectTransform, new Vector2(0, 0.5f), new Vector2(38, 0), new Vector2(110, 36), new Vector2(0, 0.5f));
		AddTooltip(row.gameObject, tip);
		return t;
	}

	void BuildObjective(RectTransform root)
	{
		locationText = NRUI.Label(root, "", 30, NRPalette.TextDim, TextAlignmentOptions.Center);
		NRUI.Place(locationText.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -18), new Vector2(900, 40), new Vector2(0.5f, 1));

		objectiveFrame = NRUI.Image(root, "Objective", NRSprites.DarkFrame, new Color(1, 1, 1, 0.9f));
		objectiveCard = objectiveFrame.rectTransform;
		NRUI.Place(objectiveCard, new Vector2(0.5f, 1), new Vector2(0, -62), new Vector2(760, 118), new Vector2(0.5f, 1));

		var diamond = NRUI.Icon(objectiveCard, NRSprites.Diamond, 22, NRPalette.Pink);
		NRUI.Place(diamond.rectTransform, new Vector2(0, 1), new Vector2(22, -20), new Vector2(22, 22), new Vector2(0, 1));
		objectiveTitle = NRUI.Label(objectiveCard, "", 32, NRPalette.Pink, TextAlignmentOptions.TopLeft);
		NRUI.Place(objectiveTitle.rectTransform, new Vector2(0, 1), new Vector2(56, -12), new Vector2(520, 40), new Vector2(0, 1));
		objectiveCounter = NRUI.Label(objectiveCard, "", 28, NRPalette.Gold, TextAlignmentOptions.TopRight);
		NRUI.Place(objectiveCounter.rectTransform, new Vector2(1, 1), new Vector2(-20, -14), new Vector2(240, 40), new Vector2(1, 1));
		objectiveBody = NRUI.Label(objectiveCard, "", 24, NRPalette.Text, TextAlignmentOptions.TopLeft);
		NRUI.Place(objectiveBody.rectTransform, new Vector2(0, 1), new Vector2(24, -52), new Vector2(712, 90), new Vector2(0, 1));
		objectiveNew = NRUI.Label(objectiveCard, "새 목표", 22, NRPalette.Bg0, TextAlignmentOptions.Center, NRTextFx.None);
		var newBg = NRUI.Image(objectiveCard, "NewTag", NRSprites.Frame(NRPalette.Pink, NRPalette.Pink, Color.white.WithAlpha(0.6f), NRPalette.Bg0), Color.white);
		NRUI.Place(newBg.rectTransform, new Vector2(0, 1), new Vector2(-10, 14), new Vector2(110, 38), new Vector2(0, 1));
		objectiveNew.transform.SetParent(newBg.transform, false);
		NRUI.Stretch(objectiveNew.rectTransform);
		newBg.gameObject.SetActive(false);
	}

	void BuildTopRight(RectTransform root)
	{
		var status = NRUI.Button(root, "TAB  스테이터스", () => NRStatusWindow.Show(), new Vector2(260, 58), 26);
		NRUI.Place(status.Rect, new Vector2(1, 1), new Vector2(-316, -24), new Vector2(260, 58), new Vector2(1, 1));
		var menu = NRUI.Button(root, "ESC  메뉴 · 일시정지", () => NRPauseMenu.Show(), new Vector2(280, 58), 26);
		NRUI.Place(menu.Rect, new Vector2(1, 1), new Vector2(-24, -24), new Vector2(280, 58), new Vector2(1, 1));
	}

	void BuildAugmentRow(RectTransform root)
	{
		augmentRow = NRUI.Rect(root, "Augments");
		NRUI.Place(augmentRow, new Vector2(0, 0), new Vector2(24, 24), new Vector2(620, 132), new Vector2(0, 0));
		var h = augmentRow.gameObject.AddComponent<GridLayoutGroup>();
		h.cellSize = new Vector2(60, 60);
		h.spacing = new Vector2(8, 8);
		h.startCorner = GridLayoutGroup.Corner.LowerLeft;
		h.childAlignment = TextAnchor.LowerLeft;
		h.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
		h.constraintCount = 9;
	}

	string PassiveSignature()
	{
		if (items == null) return "";
		return (items.isNextRoundHpUp ? "a" : "") + (items.isCrunchMode ? "b" : "") + (items.isHammerBuffing ? "c" + (player != null ? player.HammerBuffedRoundCount : 0) : "") + NRStats.DeathDefianceLeft;
	}

	void RebuildAugments()
	{
		if (augmentRow == null) return;
		foreach (var g in augmentIcons) if (g != null) Destroy(g);
		augmentIcons.Clear();

		// 상점 패시브 / 부활 (금색 테두리)
		if (items != null)
		{
			if (items.isNextRoundHpUp) AddPassiveIcon(27, NRPalette.Green, "", () => C(NRPalette.Green) + "기력 방울</color>  <size=20>상점 패시브</size>\n방에 들어갈 때마다 체력을 회복합니다.");
			if (items.isCrunchMode) AddPassiveIcon(14, NRPalette.Anxiety, "", () => C(NRPalette.Anxiety) + "과민의 눈</color>  <size=20>상점 패시브</size>\n방에 들어갈 때마다 공격·이동 속도가 오르고 방어력이 내려갑니다.");
			if (items.isHammerBuffing && player != null) AddPassiveIcon(2, NRPalette.Gold, player.HammerBuffedRoundCount.ToString(), () => C(NRPalette.Gold) + "결속 망치</color>  <size=20>상점 패시브</size>\n궁극기 피해가 크게 오릅니다. (방마다 1회, 남은 방 " + (player != null ? player.HammerBuffedRoundCount : 0) + ")");
		}
		if (NRStats.DeathDefianceLeft > 0) AddPassiveIcon(5, NRPalette.Gold, NRStats.DeathDefianceLeft.ToString(), () => C(NRPalette.Gold) + "끈질긴 생존</color>  <size=20>영구 강화</size>\n쓰러지면 체력 40%로 부활합니다. 남은 횟수 " + NRStats.DeathDefianceLeft);

		foreach (var o in NRAugments.Owned)
		{
			var owned = o;
			var frame = NRUI.Image(augmentRow, "Aug", NRSprites.ColoredFrame(NRAugments.RarityColor(o.rarity)), Color.white, true);
			var icon = NRUI.Icon(frame.transform, NRSprites.Icon(o.def.icon), 40);
			NRUI.Place(icon.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(40, 40));
			var lv = NRUI.Label(frame.transform, o.level > 1 ? o.level.ToString() : "", 22, NRPalette.Gold, TextAlignmentOptions.BottomRight, NRTextFx.OutlineShadow);
			NRUI.Stretch(lv.rectTransform, 2, 5, 2, 2);
			var hover = frame.gameObject.AddComponent<NRHoverTooltip>();
			hover.getText = () =>
				C(NRAugments.FamilyColor(owned.def.family)) + owned.def.name + "</color>  " +
				"<size=20>" + C(NRAugments.RarityColor(owned.rarity)) + "증강 · " + NRAugments.RarityName(owned.rarity) + " · Lv " + owned.level + "</color></size>\n" +
				owned.def.Describe(owned.Value);
			augmentIcons.Add(frame.gameObject);
		}
		passiveSignature = PassiveSignature();
	}

	void AddPassiveIcon(int iconIndex, Color color, string badge, System.Func<string> tip)
	{
		var frame = NRUI.Image(augmentRow, "Passive", NRSprites.Frame(NRPalette.Bg0.WithAlpha(0.95f), NRPalette.Gold, NRPalette.Gold.WithAlpha(0.6f), NRPalette.Bg0), Color.white, true);
		var icon = NRUI.Icon(frame.transform, NRSprites.Icon(iconIndex), 40, Color.Lerp(Color.white, color, 0.25f));
		NRUI.Place(icon.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(40, 40));
		if (!string.IsNullOrEmpty(badge))
		{
			var lv = NRUI.Label(frame.transform, badge, 22, NRPalette.Gold, TextAlignmentOptions.BottomRight, NRTextFx.OutlineShadow);
			NRUI.Stretch(lv.rectTransform, 2, 5, 2, 2);
		}
		frame.gameObject.AddComponent<NRHoverTooltip>().getText = tip;
		augmentIcons.Add(frame.gameObject);
	}

	// ---- 스킬 바 ----
	void BuildSkillBar(RectTransform root)
	{
		var bar = NRUI.Image(root, "Skill Bar", NRSprites.DarkFrame, new Color(1, 1, 1, 0.9f));
		NRUI.Place(bar.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 14), new Vector2(620, 150), new Vector2(0.5f, 0));
		passiveSlot = MakeSlot(bar.rectTransform, 0, true);
		attackSlot = MakeSlot(bar.rectTransform, 1, false);
		skillSlot = MakeSlot(bar.rectTransform, 2, false);
		dashSlot = MakeSlot(bar.rectTransform, 3, false);
		ultSlot = MakeSlot(bar.rectTransform, 4, false);

		AddTooltip(passiveSlot.frame.gameObject, () => C(NRPalette.Green) + "패시브 · " + NRHero.PassiveName + "</color>\n" + NRHero.PassiveDesc);
		AddTooltip(attackSlot.frame.gameObject, () => SkillTip(NRHero.Attack, NRControls.Keys.attack, action != null ? Mathf.Max(0.2f, action.jabCooldown) : 0f));
		AddTooltip(skillSlot.frame.gameObject, () => SkillTip(NRHero.Skill, NRControls.Keys.skill, action != null ? action.skillAttackCooldown * NRStats.SkillCooldownMultiplier : 0f));
		AddTooltip(dashSlot.frame.gameObject, () => SkillTip(NRHero.Dash, NRControls.Keys.dash, action != null ? action.slideCooldown * NRStats.DashCooldownMultiplier : 0f)
			+ "\n" + C(NRPalette.TextMute) + (NRControls.IsDefault ? "방향키(WASD)를 누른 채 Space" : "E를 누르면 마우스 방향") + "</color>");
		AddTooltip(ultSlot.frame.gameObject, () => SkillTip(NRHero.Ultimate, NRControls.Keys.ult, action != null ? action.skillAttack2Cooldown * NRStats.UltCooldownMultiplier : 0f));
	}

	static string SkillTip(NRSkillInfo info, string key, float cooldown)
	{
		return C(info.color) + info.name + "</color>  " + C(NRPalette.Cyan) + "[" + key + "]</color>\n" + info.desc +
			(cooldown > 0f ? "\n" + C(NRPalette.TextMute) + "재사용 대기 " + cooldown.ToString("0.#") + "초</color>" : "");
	}

	Slot MakeSlot(RectTransform parent, int index, bool passive)
	{
		float size = passive ? 72 : 92;
		float x = 22 + index * 118 + (passive ? 10 : 0);
		var frame = NRUI.Image(parent, passive ? "Passive" : "Slot", NRSprites.ColoredFrame(NRPalette.Border), Color.white, true);
		NRUI.Place(frame.rectTransform, new Vector2(0, 1), new Vector2(x, passive ? -24 : -14), new Vector2(size, size), new Vector2(0, 1));
		var icon = NRUI.Icon(frame.transform, NRSprites.Icon(16), size - 30);
		NRUI.Place(icon.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size - 30, size - 30));
		var cover = NRUI.Image(frame.transform, "Cooldown", NRSprites.White, new Color(0.02f, 0.01f, 0.05f, 0.72f));
		cover.type = Image.Type.Filled;
		cover.fillMethod = Image.FillMethod.Radial360;
		cover.fillOrigin = (int)Image.Origin360.Top;
		cover.fillClockwise = false;
		NRUI.Stretch(cover.rectTransform, 6, 6, 6, 6);
		var timer = NRUI.Label(frame.transform, "", 30, Color.white, TextAlignmentOptions.Center, NRTextFx.OutlineShadow);
		NRUI.Stretch(timer.rectTransform);

		var keyBg = NRUI.Image(frame.transform, "KeyBadge", NRSprites.Frame(NRPalette.Bg0, NRPalette.BorderHi, NRPalette.BorderHi, NRPalette.Bg0), Color.white);
		NRUI.Place(keyBg.rectTransform, new Vector2(0, 1), new Vector2(-8, 10), new Vector2(passive ? 34 : 72, 30), new Vector2(0, 1));
		var key = NRUI.Label(keyBg.transform, passive ? "P" : "", 18, NRPalette.Cyan, TextAlignmentOptions.Center, NRTextFx.None);
		NRUI.Stretch(key.rectTransform);

		var name = NRUI.Label(parent, "", 20, NRPalette.TextDim, TextAlignmentOptions.Center);
		NRUI.Place(name.rectTransform, new Vector2(0, 1), new Vector2(x - 14, -(passive ? 24 : 14) - size - 2), new Vector2(size + 28, 28), new Vector2(0, 1));
		return new Slot { frame = frame, cover = cover, icon = icon, key = key, name = name, timer = timer };
	}

	void RefreshSkillLabels()
	{
		var k = NRControls.Keys;
		SetSlotInfo(passiveSlot, new NRSkillInfo { name = "패시브", icon = NRHero.IsRanged ? 31 : 29, color = NRPalette.Green }, "P");
		SetSlotInfo(attackSlot, NRHero.Attack, k.attack);
		SetSlotInfo(skillSlot, NRHero.Skill, k.skill);
		SetSlotInfo(dashSlot, NRHero.Dash, k.dash);
		SetSlotInfo(ultSlot, NRHero.Ultimate, k.ult);
	}

	static void SetSlotInfo(Slot s, NRSkillInfo info, string key)
	{
		s.icon.sprite = NRSprites.Icon(info.icon);
		s.frame.sprite = NRSprites.ColoredFrame(info.color);
		s.key.text = key;
		s.name.text = info.name;
	}

	// ---- 보스 ----
	void BuildBossBar(RectTransform root)
	{
		var bg = NRUI.Image(root, "Boss Bar", NRSprites.DarkFrame, Color.white);
		bossBar = bg.rectTransform;
		NRUI.Place(bossBar, new Vector2(0.5f, 0), new Vector2(0, 180), new Vector2(980, 86), new Vector2(0.5f, 0));
		bossName = NRUI.Label(bossBar, "", 30, NRPalette.Crimson, TextAlignmentOptions.TopLeft, NRTextFx.OutlineShadow);
		NRUI.Place(bossName.rectTransform, new Vector2(0, 1), new Vector2(24, -8), new Vector2(700, 36), new Vector2(0, 1));
		bossPhase = NRUI.Label(bossBar, "", 24, NRPalette.TextDim, TextAlignmentOptions.TopRight);
		NRUI.Place(bossPhase.rectTransform, new Vector2(1, 1), new Vector2(-24, -10), new Vector2(240, 32), new Vector2(1, 1));
		var back = NRUI.Image(bossBar, "Back", NRSprites.White, NRPalette.HpBack);
		NRUI.Place(back.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 14), new Vector2(930, 22), new Vector2(0.5f, 0));
		bossLag = NRUI.Image(back.transform, "Lag", NRSprites.White, new Color(1f, 0.9f, 0.8f, 0.9f));
		SetupFill(bossLag);
		bossFill = NRUI.Image(back.transform, "Fill", NRSprites.White, NRPalette.Crimson);
		SetupFill(bossFill);
		foreach (float mark in new[] { 0.33f, 0.66f })
		{
			var tick = NRUI.Image(back.transform, "Tick", NRSprites.White, NRPalette.Bg0);
			tick.rectTransform.anchorMin = new Vector2(mark, 0);
			tick.rectTransform.anchorMax = new Vector2(mark, 1);
			tick.rectTransform.sizeDelta = new Vector2(4, 0);
		}
		bossBar.gameObject.SetActive(false);
	}

	// ---------------------------------------------------------------------
	void Update()
	{
		if (player == null)
		{
			player = FindObjectOfType<Player>();
			if (player != null)
			{
				action = player.GetComponent<PlayerAction>();
				items = FindObjectOfType<ItemManager>();
			}
		}

		if (talkManager == null) talkManager = FindObjectOfType<TalkManager>();
		bool dialogOpen = talkManager != null && talkManager.DialogSet != null && talkManager.DialogSet.activeInHierarchy;
		bottomGroup.alpha = Mathf.MoveTowards(bottomGroup.alpha, dialogOpen ? 0f : 1f, Time.unscaledDeltaTime * 6f);

		if (PassiveSignature() != passiveSignature) RebuildAugments();

		UpdatePlayer();
		UpdateObjective();
		UpdateSkills();
		UpdateBoss();
		UpdateArrows();
		UpdateEffects();
	}

	void UpdatePlayer()
	{
		if (player == null) return;
		// 체력바는 던전(꿈속)에서만
		bool showHp = dungeon && Player.gameRound > 0;
		if (heartGo.activeSelf != showHp)
		{
			heartGo.SetActive(showHp);
			hpBarGo.SetActive(showHp);
			float y = showHp ? -70 : -18;
			moneyRow.anchoredPosition = new Vector2(moneyRow.anchoredPosition.x, y);
			orbRow.anchoredPosition = new Vector2(orbRow.anchoredPosition.x, y);
			crystalRow.anchoredPosition = new Vector2(crystalRow.anchoredPosition.x, y);
			panelRect.sizeDelta = new Vector2(470, showHp ? 118 : 70);
		}

		float max = Mathf.Max(1, player.maxHP);
		float ratio = Mathf.Clamp01(player.nowHP / max);
		hpFill.fillAmount = ratio;
		lagValue = lagValue > ratio ? Mathf.MoveTowards(lagValue, ratio, Time.unscaledDeltaTime * 0.6f) : ratio;
		hpLag.fillAmount = lagValue;
		shieldFill.fillAmount = Mathf.Clamp01(NRStats.Shield / max);
		string shield = NRStats.Shield > 0 ? "  " + C(NRPalette.Shield) + "+" + Mathf.RoundToInt(NRStats.Shield) + "</color>" : "";
		hpText.text = Mathf.Max(0, player.nowHP) + " / " + player.maxHP + shield;

		moneyText.text = Player.Money.ToString();
		orbText.text = Player.round.ToString();
		crystalText.text = NRSave.Data.crystals.ToString();
		locationText.text = NRRun.LocationLabel();
	}

	void UpdateObjective()
	{
		if (objectiveVersion != NRObjective.Version)
		{
			objectiveVersion = NRObjective.Version;
			objectiveTitle.text = NRObjective.Title;
			objectiveBody.text = NRObjective.Body;
			float bodyHeight = objectiveBody.GetPreferredValues(NRObjective.Body, 712, 0).y;
			objectiveCard.sizeDelta = new Vector2(760, 64 + Mathf.Max(30, bodyHeight));
			if (objectivePulse != null) StopCoroutine(objectivePulse);
			objectivePulse = StartCoroutine(PulseObjective());
		}

		bool combat = NRObjective.Combat && NRWaves.RoomActive;
		objectiveCounter.text = combat ? "남은 적 " + NRWaves.AliveCount : "";
	}

	/// <summary>목표 갱신 시 3번만 테두리/제목 강조 후 정지 (계속 깜빡이지 않음)</summary>
	IEnumerator PulseObjective()
	{
		var tag = objectiveNew.transform.parent.gameObject;
		tag.SetActive(true);
		NRAudio.PlayUI();
		for (int i = 0; i < 3; i++)
		{
			float t = 0f;
			while (t < 0.45f)
			{
				t += Time.unscaledDeltaTime;
				float k = Mathf.Sin(t / 0.45f * Mathf.PI);
				objectiveFrame.color = Color.Lerp(new Color(1, 1, 1, 0.9f), new Color(1f, 0.75f, 0.95f, 1f), k);
				objectiveCard.localScale = Vector3.one * (1f + 0.025f * k);
				objectiveTitle.color = Color.Lerp(NRPalette.Pink, Color.white, k);
				yield return null;
			}
		}
		objectiveFrame.color = new Color(1, 1, 1, 0.9f);
		objectiveCard.localScale = Vector3.one;
		objectiveTitle.color = NRPalette.Pink;
		yield return new WaitForSecondsRealtime(2.5f);
		tag.SetActive(false);
		objectivePulse = null;
	}

	void UpdateSkills()
	{
		if (attackSlot == null || action == null) return;
		if (lastScheme != NRSettings.ControlScheme || lastHero != NRSave.Data.hero)
		{
			lastScheme = NRSettings.ControlScheme;
			lastHero = NRSave.Data.hero;
			RefreshSkillLabels();
		}
		bool locked = action.isGeoRiPlayer || Player.gameRound == 0;
		SetSlot(passiveSlot, 0f, 0f, false);
		SetSlot(attackSlot, locked ? 1f : action.JabCooldownRatio, 0f, locked);
		SetSlot(skillSlot, locked ? 1f : action.SkillCooldownRatio, action.SkillCooldownRemain, locked);
		SetSlot(dashSlot, action.SlideCooldownRatio, action.SlideCooldownRemain, false);
		SetSlot(ultSlot, locked ? 1f : action.UltCooldownRatio, action.UltCooldownRemain, locked);
	}

	static void SetSlot(Slot s, float ratio, float remain, bool locked)
	{
		s.cover.fillAmount = locked ? 1f : ratio;
		s.cover.color = locked ? new Color(0.02f, 0.01f, 0.05f, 0.8f) : new Color(0.02f, 0.01f, 0.05f, 0.7f);
		s.timer.text = !locked && remain > 0.05f ? (remain >= 1f ? Mathf.CeilToInt(remain).ToString() : remain.ToString("0.0")) : "";
		s.icon.color = locked ? new Color(1, 1, 1, 0.4f) : Color.white;
	}

	void UpdateBoss()
	{
		bool show = bossHp != null && !bossHp.IsDead;
		if (bossBar.gameObject.activeSelf != show) bossBar.gameObject.SetActive(show);
		if (!show) return;
		float ratio = Mathf.Clamp01((float)bossHp.nowHP / Mathf.Max(1, bossHp.maxHP));
		bossFill.fillAmount = ratio;
		bossLagValue = bossLagValue > ratio ? Mathf.MoveTowards(bossLagValue, ratio, Time.unscaledDeltaTime * 0.35f) : ratio;
		bossLag.fillAmount = bossLagValue;
		if (bossAi != null)
		{
			bossName.text = bossAi.bossName;
			bossPhase.text = bossAi.IsInvulnerable ? C(NRPalette.Gold) + "무적</color>" : "페이즈 " + bossAi.Phase;
		}
	}

	/// <summary>화면 가장자리 위치 계산 (화면 밖이면 true)</summary>
	bool EdgePosition(Camera cam, Vector3 world, out Vector2 anchored, out float angle)
	{
		anchored = Vector2.zero;
		angle = 0f;
		Vector3 vp = cam.WorldToViewportPoint(world);
		bool onScreen = vp.z > 0 && vp.x > 0.03f && vp.x < 0.97f && vp.y > 0.03f && vp.y < 0.97f;
		if (onScreen) return false;
		Vector2 dir = new Vector2(vp.x - 0.5f, vp.y - 0.5f);
		if (vp.z < 0) dir = -dir;
		if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;
		float scale = Mathf.Min(0.44f / Mathf.Abs(dir.x + 0.0001f), 0.4f / Mathf.Abs(dir.y + 0.0001f));
		Vector2 edge = dir * scale;
		Vector2 size = ((RectTransform)canvas.transform).rect.size;
		anchored = new Vector2(edge.x * size.x, edge.y * size.y);
		angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
		return true;
	}

	void UpdateArrows()
	{
		var cam = Camera.main;
		int used = 0;
		int usedMarks = 0;
		if (cam != null && dungeon && player != null && !player.isDead && !NRRun.Transitioning)
		{
			// 적
			var alive = NRWaves.Alive;
			bool reveal = NRWaves.RoomActive && (NRWaves.AliveCount <= 3 || Time.time - NRStats.LastCombatTime > 5f);
			var targets = new List<(Vector3 pos, bool boss)>();
			if (reveal) foreach (var go in alive) if (go != null) targets.Add((go.transform.position, false));
			if (bossHp != null && !bossHp.IsDead) targets.Add((bossHp.transform.position, true));
			foreach (var t in targets)
			{
				if (used >= arrows.Count) break;
				if (!EdgePosition(cam, t.pos, out var pos, out var ang)) continue;
				var a = arrows[used++];
				a.gameObject.SetActive(true);
				a.rectTransform.anchorMin = a.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
				a.rectTransform.anchoredPosition = pos;
				a.rectTransform.localRotation = Quaternion.Euler(0, 0, ang);
				float pulse = 0.65f + 0.35f * Mathf.Sin(Time.unscaledTime * 6f);
				a.color = (t.boss ? NRPalette.Gold : NRPalette.Crimson).WithAlpha(pulse);
				a.rectTransform.sizeDelta = t.boss ? new Vector2(46, 76) : new Vector2(30, 50);
			}

			// 목적지 (문/포탈/구슬/상인) — 전투 중이 아닐 때
			if (!NRWaves.RoomActive)
			{
				foreach (var w in NRWaypoint.All)
				{
					if (usedMarks >= waymarks.Count) break;
					if (!w.IsActive) continue;
					if (Vector2.Distance(w.target.position, player.transform.position) > 45f) continue;
					if (!EdgePosition(cam, w.target.position, out var pos, out var ang)) continue;
					var m = waymarks[usedMarks++];
					m.root.gameObject.SetActive(true);
					m.root.anchorMin = m.root.anchorMax = new Vector2(0.5f, 0.5f);
					m.root.anchoredPosition = pos;
					m.arrow.rectTransform.localRotation = Quaternion.Euler(0, 0, ang);
					float pulse = 0.7f + 0.3f * Mathf.Sin(Time.unscaledTime * 4f);
					m.arrow.color = w.color.WithAlpha(pulse);
					m.label.text = w.label;
					m.label.color = w.color;
					// 이름표는 화면 안쪽으로
					Vector2 inward = -pos.normalized * 56f;
					m.label.rectTransform.anchoredPosition = inward;
				}
			}
		}
		for (int i = used; i < arrows.Count; i++) if (arrows[i].gameObject.activeSelf) arrows[i].gameObject.SetActive(false);
		for (int i = usedMarks; i < waymarks.Count; i++) if (waymarks[i].root.gameObject.activeSelf) waymarks[i].root.gameObject.SetActive(false);
	}

	void UpdateEffects()
	{
		damageFlashAlpha = Mathf.MoveTowards(damageFlashAlpha, 0f, Time.unscaledDeltaTime * 2.5f);
		damageFlash.color = new Color(1f, 0.1f, 0.2f, damageFlashAlpha);
		float lowAlpha = 0f;
		if (player != null && !player.isDead && player.maxHP > 0 && Player.gameRound > 0)
		{
			float r = (float)player.nowHP / player.maxHP;
			if (r < 0.3f) lowAlpha = (0.3f - r) / 0.3f * (0.35f + 0.2f * Mathf.Sin(Time.unscaledTime * 4f));
		}
		lowHpVignette.color = new Color(0.9f, 0.05f, 0.15f, lowAlpha);
	}

	// ---------------------------------------------------------------------
	public static void FlashDamage()
	{
		if (instance != null) instance.damageFlashAlpha = 0.55f;
	}

	public static void SetVignette(Color c)
	{
		if (instance != null) instance.vignette.color = c;
	}

	public static void PulseCrystals()
	{
		if (instance != null && instance.crystalRow != null) instance.StartCoroutine(instance.Punch(instance.crystalRow));
	}

	IEnumerator Punch(RectTransform rt)
	{
		float t = 0f;
		while (t < 0.4f)
		{
			t += Time.unscaledDeltaTime;
			rt.localScale = Vector3.one * (1f + 0.3f * Mathf.Sin(t / 0.4f * Mathf.PI));
			yield return null;
		}
		rt.localScale = Vector3.one;
	}

	public static void ShowBoss(MonsterAI ai, MonsterHP hp)
	{
		if (instance == null) return;
		instance.bossAi = ai;
		instance.bossHp = hp;
		instance.bossLagValue = 1f;
	}

	public static void HideBoss(MonsterAI ai = null)
	{
		if (instance == null) return;
		if (ai != null && instance.bossAi != ai) return;
		instance.bossAi = null;
		instance.bossHp = null;
	}

	public static void SetVisible(bool visible)
	{
		if (instance != null && instance.canvas != null) instance.canvas.enabled = visible;
	}
}

/// <summary>마우스를 올리면 HUD 툴팁 표시</summary>
public class NRHoverTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
	public System.Func<string> getText;
	public static NRHoverTooltip Current;
	static RectTransform tip;
	static TextMeshProUGUI tipText;

	public static void Bind(RectTransform tooltip, TextMeshProUGUI text) { tip = tooltip; tipText = text; }

	public void OnPointerEnter(PointerEventData e)
	{
		Current = this;
		NRTooltip.Show(getText != null ? getText() : "", (RectTransform)transform);
	}

	public void OnPointerExit(PointerEventData e)
	{
		if (Current == this) { Current = null; NRTooltip.Hide(); }
	}

	void OnDisable()
	{
		if (Current == this) { Current = null; NRTooltip.Hide(); }
	}
}

/// <summary>전역 툴팁 (전역 오버레이 캔버스 사용)</summary>
public static class NRTooltip
{
	static RectTransform root;
	static TextMeshProUGUI text;

	static void Ensure()
	{
		if (root != null || NRUIRoot.Instance == null) return;
		var bg = NRUI.Image(NRUIRoot.Instance.Canvas.transform, "Tooltip", NRSprites.PanelFrame, Color.white);
		root = bg.rectTransform;
		root.pivot = new Vector2(0, 0);
		text = NRUI.Label(root, "", 26, NRPalette.Text, TextAlignmentOptions.TopLeft);
		NRUI.Stretch(text.rectTransform, 20, 20, 16, 16);
		root.gameObject.SetActive(false);
	}

	public static void Show(string content, RectTransform anchor)
	{
		Ensure();
		if (root == null) return;
		root.gameObject.SetActive(true);
		root.SetAsLastSibling();
		text.text = content;
		var pref = text.GetPreferredValues(content, 520, 0);
		root.sizeDelta = new Vector2(Mathf.Min(560, pref.x + 44), pref.y + 36);
		var canvasRect = (RectTransform)NRUIRoot.Instance.Canvas.transform;
		Vector3[] corners = new Vector3[4];
		anchor.GetWorldCorners(corners);
		Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, corners[1]);
		RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, null, out var local);
		root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
		Vector2 pos = local + new Vector2(0, 12);
		Vector2 half = canvasRect.rect.size * 0.5f;
		pos.x = Mathf.Clamp(pos.x, -half.x + 10, half.x - root.sizeDelta.x - 10);
		pos.y = Mathf.Clamp(pos.y, -half.y + 10, half.y - root.sizeDelta.y - 10);
		root.anchoredPosition = pos;
	}

	public static void Hide()
	{
		if (root != null) root.gameObject.SetActive(false);
	}
}
