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
//  우상단: [TAB] 현황 / [ESC] 메뉴
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

	// 체력
	Image hpFill, hpLag, shieldFill;
	TextMeshProUGUI hpText;
	float lagValue = 1f;

	// 자원
	TextMeshProUGUI moneyText, orbText, crystalText, locationText, buffText;
	RectTransform crystalRow;

	// 목표
	RectTransform objectiveCard;
	Image objectiveFrame;
	TextMeshProUGUI objectiveTitle, objectiveBody, objectiveCounter, objectiveNew;
	int objectiveVersion = -1;
	Coroutine objectivePulse;

	// 증강
	RectTransform augmentRow;
	readonly List<GameObject> augmentIcons = new List<GameObject>();

	// 대화 중에는 하단 HUD를 흐리게
	CanvasGroup bottomGroup;
	TalkManager talkManager;

	// 스킬
	class Slot { public Image cover; public TextMeshProUGUI key; public Image frame; }
	Slot jabSlot, skillSlot, ultSlot, dashSlot;

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

	// 적 방향 표시
	readonly List<Image> arrows = new List<Image>();

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
		BuildAugmentRow(bottom);
		if (dungeon) BuildSkillSlots(bottom);
		BuildBossBar(root);

		for (int i = 0; i < 8; i++)
		{
			var a = NRUI.Image(root, "EnemyArrow", NRSprites.ArrowRight, NRPalette.Crimson);
			a.rectTransform.sizeDelta = new Vector2(34, 56);
			a.gameObject.SetActive(false);
			arrows.Add(a);
		}

		NRAugments.OnChanged += RebuildAugments;
		RebuildAugments();
	}

	void OnDestroy()
	{
		NRAugments.OnChanged -= RebuildAugments;
		if (instance == this) instance = null;
	}

	// ---------------------------------------------------------------------
	void BuildPlayerPanel(RectTransform root)
	{
		var panel = NRUI.Image(root, "Player Panel", NRSprites.DarkFrame, new Color(1, 1, 1, 0.92f));
		NRUI.Place(panel.rectTransform, new Vector2(0, 1), new Vector2(24, -24), new Vector2(470, dungeon ? 150 : 112), new Vector2(0, 1));

		var heart = NRUI.Icon(panel.transform, NRSprites.Heart, 34, NRPalette.Hp);
		NRUI.Place(heart.rectTransform, new Vector2(0, 1), new Vector2(22, -22), new Vector2(34, 34), new Vector2(0, 1));

		var barBg = NRUI.Image(panel.transform, "HP Back", NRSprites.White, NRPalette.HpBack);
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

		float rowY = -70;
		moneyText = ResourceItem(panel.transform, NRSprites.Coin, NRPalette.Gold, new Vector2(22, rowY));
		orbText = ResourceItem(panel.transform, NRSprites.Orb, NRPalette.Anxiety, new Vector2(170, rowY));
		crystalText = ResourceItem(panel.transform, NRSprites.Diamond, NRPalette.Cyan, new Vector2(310, rowY));
		crystalRow = (RectTransform)crystalText.transform.parent;

		if (dungeon)
		{
			buffText = NRUI.Label(panel.transform, "", 22, NRPalette.TextDim, TextAlignmentOptions.MidlineLeft);
			NRUI.Place(buffText.rectTransform, new Vector2(0, 1), new Vector2(24, -112), new Vector2(430, 30), new Vector2(0, 1));
		}
	}

	static void SetupFill(Image img)
	{
		img.type = Image.Type.Filled;
		img.fillMethod = Image.FillMethod.Horizontal;
		img.fillOrigin = 0;
		img.fillAmount = 1f;
		NRUI.Stretch(img.rectTransform);
	}

	TextMeshProUGUI ResourceItem(Transform parent, Sprite icon, Color color, Vector2 pos)
	{
		var row = NRUI.Rect(parent, "Resource");
		NRUI.Place(row, new Vector2(0, 1), pos, new Vector2(140, 36), new Vector2(0, 1));
		var img = NRUI.Icon(row, icon, 28, color);
		NRUI.Place(img.rectTransform, new Vector2(0, 0.5f), Vector2.zero, new Vector2(28, 28), new Vector2(0, 0.5f));
		var t = NRUI.Label(row, "0", 28, NRPalette.Text, TextAlignmentOptions.MidlineLeft);
		NRUI.Place(t.rectTransform, new Vector2(0, 0.5f), new Vector2(38, 0), new Vector2(110, 36), new Vector2(0, 0.5f));
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
		var status = NRUI.Button(root, "TAB  현황", () => NRStatusWindow.Show(), new Vector2(190, 58), 26);
		NRUI.Place(status.Rect, new Vector2(1, 1), new Vector2(-230, -24), new Vector2(190, 58), new Vector2(1, 1));
		var menu = NRUI.Button(root, "ESC  메뉴", () => NRPauseMenu.Show(), new Vector2(190, 58), 26);
		NRUI.Place(menu.Rect, new Vector2(1, 1), new Vector2(-24, -24), new Vector2(190, 58), new Vector2(1, 1));
	}

	void BuildAugmentRow(RectTransform root)
	{
		augmentRow = NRUI.Rect(root, "Augments");
		NRUI.Place(augmentRow, new Vector2(0, 0), new Vector2(24, 24), new Vector2(760, 64), new Vector2(0, 0));
		var h = augmentRow.gameObject.AddComponent<HorizontalLayoutGroup>();
		h.spacing = 8;
		h.childAlignment = TextAnchor.LowerLeft;
		h.childControlHeight = false;
		h.childControlWidth = false;
		h.childForceExpandWidth = false;
	}

	void RebuildAugments()
	{
		if (augmentRow == null) return;
		foreach (var g in augmentIcons) if (g != null) Destroy(g);
		augmentIcons.Clear();
		foreach (var o in NRAugments.Owned)
		{
			var owned = o;
			var frame = NRUI.Image(augmentRow, "Aug", NRSprites.ColoredFrame(NRAugments.RarityColor(o.rarity)), Color.white, true);
			frame.rectTransform.sizeDelta = new Vector2(60, 60);
			var icon = NRUI.Icon(frame.transform, NRSprites.Icon(o.def.icon), 40);
			NRUI.Place(icon.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(40, 40));
			var lv = NRUI.Label(frame.transform, o.level > 1 ? o.level.ToString() : "", 22, NRPalette.Gold, TextAlignmentOptions.BottomRight, NRTextFx.OutlineShadow);
			NRUI.Stretch(lv.rectTransform, 2, 5, 2, 2);
			var hover = frame.gameObject.AddComponent<NRHoverTooltip>();
			hover.getText = () =>
				"<color=#" + NRPalette.ToHex(NRAugments.FamilyColor(owned.def.family)) + ">" + owned.def.name + "</color>  " +
				"<size=20><color=#" + NRPalette.ToHex(NRAugments.RarityColor(owned.rarity)) + ">" + NRAugments.RarityName(owned.rarity) + " · Lv " + owned.level + "</color></size>\n" +
				owned.def.Describe(owned.Value);
			augmentIcons.Add(frame.gameObject);
		}
	}

	void BuildSkillSlots(RectTransform root)
	{
		var bar = NRUI.Rect(root, "Skills");
		NRUI.Place(bar, new Vector2(0.5f, 0), new Vector2(0, 22), new Vector2(460, 96), new Vector2(0.5f, 0));
		jabSlot = MakeSlot(bar, 0, "좌클릭", 16, NRPalette.Text);
		skillSlot = MakeSlot(bar, 1, "우클릭", 1, NRPalette.Rage);
		ultSlot = MakeSlot(bar, 2, "R", 2, NRPalette.Gold);
		dashSlot = MakeSlot(bar, 3, "Space", 13, NRPalette.Cyan);
	}

	Slot MakeSlot(RectTransform parent, int index, string key, int iconIndex, Color color)
	{
		var frame = NRUI.Image(parent, "Slot " + key, NRSprites.ColoredFrame(color.WithAlpha(1f)), Color.white);
		NRUI.Place(frame.rectTransform, new Vector2(0, 0), new Vector2(index * 116, 20), new Vector2(100, 76), new Vector2(0, 0));
		var icon = NRUI.Icon(frame.transform, NRSprites.Icon(iconIndex), 44);
		NRUI.Place(icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 4), new Vector2(44, 44));
		var cover = NRUI.Image(frame.transform, "Cooldown", NRSprites.White, new Color(0.02f, 0.01f, 0.05f, 0.72f));
		cover.type = Image.Type.Filled;
		cover.fillMethod = Image.FillMethod.Vertical;
		cover.fillOrigin = 0;
		NRUI.Stretch(cover.rectTransform, 6, 6, 6, 6);
		var k = NRUI.Label(parent, key, 20, NRPalette.TextDim, TextAlignmentOptions.Center);
		NRUI.Place(k.rectTransform, new Vector2(0, 0), new Vector2(index * 116, -6), new Vector2(100, 26), new Vector2(0, 0));
		return new Slot { cover = cover, key = k, frame = frame };
	}

	void BuildBossBar(RectTransform root)
	{
		var bg = NRUI.Image(root, "Boss Bar", NRSprites.DarkFrame, Color.white);
		bossBar = bg.rectTransform;
		NRUI.Place(bossBar, new Vector2(0.5f, 0), new Vector2(0, 150), new Vector2(980, 86), new Vector2(0.5f, 0)); // 목표 카드와 겹치지 않도록 하단 스킬 바 위
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

		UpdatePlayer();
		UpdateObjective();
		UpdateSkills();
		UpdateBoss();
		UpdateEnemyArrows();
		UpdateEffects();
	}

	void UpdatePlayer()
	{
		if (player == null) return;
		float max = Mathf.Max(1, player.maxHP);
		float ratio = Mathf.Clamp01(player.nowHP / max);
		hpFill.fillAmount = ratio;
		lagValue = lagValue > ratio ? Mathf.MoveTowards(lagValue, ratio, Time.unscaledDeltaTime * 0.6f) : ratio;
		hpLag.fillAmount = lagValue;
		shieldFill.fillAmount = Mathf.Clamp01(NRStats.Shield / max);
		string shield = NRStats.Shield > 0 ? "  <color=#" + NRPalette.ToHex(NRPalette.Shield) + ">+" + Mathf.RoundToInt(NRStats.Shield) + "</color>" : "";
		hpText.text = Mathf.Max(0, player.nowHP) + " / " + player.maxHP + shield;

		moneyText.text = Player.Money.ToString();
		orbText.text = Player.round.ToString();
		crystalText.text = NRSave.Data.crystals.ToString();
		locationText.text = NRRun.LocationLabel();

		if (buffText != null && items != null)
		{
			var parts = new List<string>();
			if (items.isHammerBuffing) parts.Add("결속 망치 " + player.HammerBuffedRoundCount);
			if (items.isNextRoundHpUp) parts.Add("기력 방울");
			if (items.isCrunchMode) parts.Add("과민의 눈");
			if (NRStats.DeathDefianceLeft > 0) parts.Add("부활 " + NRStats.DeathDefianceLeft);
			buffText.text = parts.Count > 0 ? string.Join("  ·  ", parts) : "";
		}
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
		if (combat)
		{
			int alive = NRWaves.AliveCount;
			objectiveCounter.text = "남은 적 " + alive;
		}
		else if (NRRun.BossActive) objectiveCounter.text = "";
		else objectiveCounter.text = "";
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
		if (jabSlot == null || action == null) return;
		bool locked = action.isGeoRiPlayer || Player.gameRound == 0;
		SetSlot(jabSlot, locked ? 1f : action.JabCooldownRatio, locked);
		SetSlot(skillSlot, locked ? 1f : action.SkillCooldownRatio, locked);
		SetSlot(ultSlot, locked ? 1f : action.UltCooldownRatio, locked);
		SetSlot(dashSlot, action.SlideCooldownRatio, false);
	}

	static void SetSlot(Slot s, float ratio, bool locked)
	{
		s.cover.fillAmount = ratio;
		s.cover.color = locked ? new Color(0.02f, 0.01f, 0.05f, 0.85f) : new Color(0.02f, 0.01f, 0.05f, 0.72f);
		s.frame.color = ratio <= 0.001f && !locked ? Color.white : new Color(0.75f, 0.75f, 0.8f, 1f);
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
			bossPhase.text = bossAi.IsInvulnerable ? "<color=#" + NRPalette.ToHex(NRPalette.Gold) + ">무적</color>" : "페이즈 " + bossAi.Phase;
		}
	}

	void UpdateEnemyArrows()
	{
		var cam = Camera.main;
		int used = 0;
		if (cam != null && dungeon && player != null && !player.isDead)
		{
			var alive = NRWaves.Alive;
			bool reveal = NRWaves.RoomActive && (NRWaves.AliveCount <= 3 || Time.time - NRStats.LastCombatTime > 5f);
			var targets = new List<(Vector3 pos, bool boss)>();
			if (reveal) foreach (var go in alive) if (go != null) targets.Add((go.transform.position, false));
			if (bossHp != null && !bossHp.IsDead) targets.Add((bossHp.transform.position, true));

			var canvasRect = (RectTransform)canvas.transform;
			Vector2 size = canvasRect.rect.size;
			foreach (var t in targets)
			{
				if (used >= arrows.Count) break;
				Vector3 vp = cam.WorldToViewportPoint(t.pos);
				bool onScreen = vp.z > 0 && vp.x > 0.02f && vp.x < 0.98f && vp.y > 0.02f && vp.y < 0.98f;
				if (onScreen) continue;
				Vector2 dir = new Vector2(vp.x - 0.5f, vp.y - 0.5f);
				if (vp.z < 0) dir = -dir;
				if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;
				float scale = Mathf.Min(0.46f / Mathf.Abs(dir.x + 0.0001f), 0.44f / Mathf.Abs(dir.y + 0.0001f));
				Vector2 edge = dir * scale;
				var a = arrows[used++];
				a.gameObject.SetActive(true);
				a.rectTransform.anchorMin = a.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
				a.rectTransform.anchoredPosition = new Vector2(edge.x * size.x, edge.y * size.y);
				a.rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
				float pulse = 0.65f + 0.35f * Mathf.Sin(Time.unscaledTime * 6f);
				a.color = (t.boss ? NRPalette.Gold : NRPalette.Crimson).WithAlpha(pulse);
				a.rectTransform.sizeDelta = t.boss ? new Vector2(46, 76) : new Vector2(30, 50);
			}
		}
		for (int i = used; i < arrows.Count; i++) if (arrows[i].gameObject.activeSelf) arrows[i].gameObject.SetActive(false);
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
