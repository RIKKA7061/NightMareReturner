using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// ============================================================================
// TAB 현황 창: 현황(목표/능력치/아이템/기록) · 증강 · 계층 지도 · 스토리
// ============================================================================
public class NRStatusWindow : NRModal
{
	static NRStatusWindow instance;
	RectTransform body;
	readonly List<NRButton> tabs = new List<NRButton>();
	int currentTab;
	static string[] cachedStory;

	public static void Show()
	{
		if (NRUIState.AnyOpen) return;
		if (instance == null) instance = Build();
		instance.Open();
	}

	static NRStatusWindow Build()
	{
		var root = NRModalHost.CreateModalRoot("NR Status Window", 0.72f);
		var w = root.gameObject.AddComponent<NRStatusWindow>();

		var panel = NRUI.Panel(root, "Panel");
		NRUI.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -10), new Vector2(1560, 900));

		var title = NRUI.Label(panel.transform, "현황", 48, NRPalette.Text, TextAlignmentOptions.MidlineLeft, NRTextFx.OutlineShadow);
		NRUI.Place(title.rectTransform, new Vector2(0, 1), new Vector2(40, -20), new Vector2(300, 70), new Vector2(0, 1));
		var hint = NRUI.Label(panel.transform, "TAB / ESC 닫기", 24, NRPalette.TextMute, TextAlignmentOptions.MidlineRight);
		NRUI.Place(hint.rectTransform, new Vector2(1, 1), new Vector2(-40, -36), new Vector2(300, 40), new Vector2(1, 1));

		string[] names = { "현황", "증강", "계층", "스토리" };
		for (int i = 0; i < names.Length; i++)
		{
			int idx = i;
			var b = NRUI.Button(panel.transform, names[i], () => w.SelectTab(idx), new Vector2(200, 64), 30);
			NRUI.Place(b.Rect, new Vector2(0, 1), new Vector2(360 + i * 216, -24), new Vector2(200, 64), new Vector2(0, 1));
			w.tabs.Add(b);
		}
		var line = NRUI.Image(panel.transform, "Line", NRSprites.White, NRPalette.BorderHi.WithAlpha(0.5f));
		NRUI.Place(line.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -104), new Vector2(1480, 3), new Vector2(0.5f, 1));

		w.body = NRUI.Rect(panel.transform, "Body");
		NRUI.Place(w.body, new Vector2(0.5f, 1), new Vector2(0, -122), new Vector2(1480, 750), new Vector2(0.5f, 1));
		root.gameObject.SetActive(false);
		return w;
	}

	protected override void OnOpened()
	{
		SelectTab(currentTab);
	}

	void SelectTab(int index)
	{
		currentTab = index;
		for (int i = 0; i < tabs.Count; i++)
		{
			tabs[i].SetColors(i == index ? NRSprites.ButtonHoverFrame : NRSprites.ButtonFrame, NRSprites.ButtonHoverFrame);
			tabs[i].normalText = i == index ? NRPalette.Pink : NRPalette.Text;
		}
		foreach (Transform c in body) Destroy(c.gameObject);
		switch (index)
		{
			case 0: BuildOverview(); break;
			case 1: BuildAugments(); break;
			case 2: BuildFloors(); break;
			default: BuildStory(); break;
		}
	}

	static string C(Color c) => "<color=#" + NRPalette.ToHex(c) + ">";

	// ---------------------------------------------------------------------
	void BuildOverview()
	{
		var player = FindObjectOfType<Player>();
		var action = player != null ? player.GetComponent<PlayerAction>() : null;
		var items = FindObjectOfType<ItemManager>();

		// 목표 카드
		var goal = NRUI.Image(body, "Goal", NRSprites.ColoredFrame(NRPalette.Pink), Color.white);
		NRUI.Place(goal.rectTransform, new Vector2(0, 1), Vector2.zero, new Vector2(1480, 150), new Vector2(0, 1));
		var gt = NRUI.Label(goal.transform, C(NRPalette.TextDim) + "현재 목표</color>   " + C(NRPalette.Pink) + NRObjective.Title + "</color>\n" + NRObjective.Body, 28, NRPalette.Text, TextAlignmentOptions.TopLeft);
		NRUI.Stretch(gt.rectTransform, 28, 28, 18, 12);

		// 능력치
		var stats = NRUI.Image(body, "Stats", NRSprites.DarkFrame, Color.white);
		NRUI.Place(stats.rectTransform, new Vector2(0, 1), new Vector2(0, -170), new Vector2(720, 580), new Vector2(0, 1));
		var sb = new System.Text.StringBuilder();
		sb.AppendLine(C(NRPalette.Pink) + "능력치</color>");
		sb.AppendLine();
		if (player != null)
		{
			sb.AppendLine(Line("체력", player.nowHP + " / " + player.maxHP));
			sb.AppendLine(Line("공격력", player.Atk.ToString()));
			sb.AppendLine(Line("방어력", player.AR + "  " + C(NRPalette.TextMute) + "(받는 피해 " + Mathf.RoundToInt(100f / (1f + player.AR * 0.01f)) + "%)</color>"));
			if (action != null)
			{
				sb.AppendLine(Line("이동 속도", (action.defaultSpeed * NRStats.MoveSpeedMultiplier).ToString("0.0")));
				sb.AppendLine(Line("공격 속도", (1f / (Mathf.Max(0.2f, action.jabCooldown) * NRStats.AttackIntervalMultiplier)).ToString("0.00") + "회/초"));
			}
			sb.AppendLine(Line("치명타 확률", Mathf.RoundToInt(NRStats.CritChance * 100) + "%"));
			sb.AppendLine(Line("피해 감소", Mathf.RoundToInt(Mathf.Clamp(NRStats.DamageReductionPct, 0, 0.6f) * 100) + "%"));
			sb.AppendLine(Line("보호막", Mathf.RoundToInt(NRStats.Shield).ToString()));
			sb.AppendLine(Line("남은 부활", NRStats.DeathDefianceLeft.ToString()));
		}
		sb.AppendLine();
		sb.AppendLine(C(NRPalette.Pink) + "각성한 감정</color>");
		sb.AppendLine(Player.round >= 1 ? "Class 감정 「의지」 — 기본 공격 25 · 특수 공격 ×2 · 궁극기 ×4" : C(NRPalette.TextMute) + "아직 각성하지 않았습니다. (1계층 각성의 방)</color>");
		var st = NRUI.Label(stats.transform, sb.ToString(), 28, NRPalette.Text, TextAlignmentOptions.TopLeft);
		NRUI.Stretch(st.rectTransform, 30, 30, 22, 16);

		// 자원 / 아이템 / 기록
		var right = NRUI.Image(body, "Resources", NRSprites.DarkFrame, Color.white);
		NRUI.Place(right.rectTransform, new Vector2(1, 1), new Vector2(0, -170), new Vector2(740, 580), new Vector2(1, 1));
		var rb = new System.Text.StringBuilder();
		rb.AppendLine(C(NRPalette.Pink) + "자원</color>");
		rb.AppendLine();
		rb.AppendLine(Line("재화", C(NRPalette.Gold) + Player.Money + "</color>"));
		rb.AppendLine(Line("감정 구슬", C(NRPalette.Anxiety) + Player.round + "</color>"));
		rb.AppendLine(Line("악몽 결정", C(NRPalette.Cyan) + "◈ " + NRSave.Data.crystals + "</color>  " + C(NRPalette.TextMute) + "(영구 · 이번 회차 +" + NRRun.RunCrystals + ")</color>"));
		rb.AppendLine();
		rb.AppendLine(C(NRPalette.Pink) + "상점 아이템</color>");
		bool any = false;
		if (items != null)
		{
			if (items.isHammerBuffing) { rb.AppendLine("결속 망치 — 궁극기 1회 강화 (남은 방 " + (player != null ? player.HammerBuffedRoundCount : 0) + ")"); any = true; }
			if (items.isNextRoundHpUp) { rb.AppendLine("기력 방울 — 방에 들어갈 때 체력 회복"); any = true; }
			if (items.isCrunchMode) { rb.AppendLine("과민의 눈 — 방마다 공속·이속 증가, 방어력 감소"); any = true; }
		}
		if (!any) rb.AppendLine(C(NRPalette.TextMute) + "없음</color>");
		rb.AppendLine();
		rb.AppendLine(C(NRPalette.Pink) + "기록</color>");
		rb.AppendLine(Line("죽은 횟수", Player.DeadCount.ToString()));
		rb.AppendLine(Line("클리어", NRSave.Data.clears + "회   최고 " + NRSave.Data.bestFloor + "계층"));
		rb.AppendLine(Line("처치한 적", NRSave.Data.enemyKills.ToString()));
		var rt = NRUI.Label(right.transform, rb.ToString(), 28, NRPalette.Text, TextAlignmentOptions.TopLeft);
		NRUI.Stretch(rt.rectTransform, 30, 30, 22, 16);
	}

	static string Line(string label, string value) => C(NRPalette.TextDim) + label + "</color>   " + value;

	// ---------------------------------------------------------------------
	void BuildAugments()
	{
		var scroll = NRUI.ScrollView(body, "Scroll", out var content, 14, 20);
		NRUI.Stretch((RectTransform)scroll.transform);
		if (NRAugments.Owned.Count == 0)
		{
			NRUI.LayoutText(content, "\n아직 보유한 증강이 없습니다.\n\n" + C(NRPalette.TextDim) +
				"감정의 방(보라색 구슬 보상), 클래스 각성, 수문장 처치 보상에서 증강을 선택할 수 있습니다.\n" +
				"같은 증강을 다시 고르면 레벨이 올라 효과가 중첩됩니다.\n" +
				"서로 다른 감정 두 가지를 모으면 전설 등급 '이중 감정'이 등장할 수 있습니다.</color>", 30, NRPalette.Text, TextAlignmentOptions.Top);
			return;
		}
		var groups = NRAugments.Owned.GroupBy(o => o.def.family).OrderBy(g => g.Key);
		foreach (var g in groups)
		{
			var header = NRUI.LayoutText(content, C(NRAugments.FamilyColor(g.Key)) + "■ " + NRAugments.FamilyName(g.Key) + "</color>  " + C(NRPalette.TextMute) + g.Count() + "개</color>", 32, NRPalette.Text);
			header.GetComponent<LayoutElement>().preferredHeight = 50;
			foreach (var o in g)
			{
				var row = NRUI.HRow(content, "Aug", 96, 20);
				row.padding = new RectOffset(12, 12, 8, 8);
				var bg = row.gameObject.AddComponent<Image>();
				bg.sprite = NRSprites.ColoredFrame(NRAugments.RarityColor(o.rarity));
				bg.type = Image.Type.Sliced;
				bg.pixelsPerUnitMultiplier = 1f / 3f;
				var icon = NRUI.Icon(row.transform, NRSprites.Icon(o.def.icon), 72);
				var ile = icon.gameObject.AddComponent<LayoutElement>();
				ile.preferredWidth = 72; ile.preferredHeight = 72;
				var txt = NRUI.Label(row.transform,
					C(NRAugments.FamilyColor(o.def.family)) + o.def.name + "</color>  " +
					"<size=22>" + C(NRAugments.RarityColor(o.rarity)) + NRAugments.RarityName(o.rarity) + "</color> · Lv " + o.level + "/" + o.def.maxLevel + "</size>\n" +
					o.def.Describe(o.Value), 28, NRPalette.Text, TextAlignmentOptions.MidlineLeft);
				txt.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
			}
		}
	}

	// ---------------------------------------------------------------------
	void BuildFloors()
	{
		var spawner = FindObjectOfType<PrefabSpawner>();
		int over = spawner != null ? spawner.OverRoom : 5;
		int round = Player.gameRound;
		string scene = NRGame.Instance != null ? NRGame.Instance.CurrentScene : "";
		bool inDungeon = scene == NRGame.SceneDungeon && round > 0;

		for (int f = 1; f <= NRRun.MaxFloor; f++)
		{
			var def = NRRun.Floors[f - 1];
			bool current = inDungeon && NRRun.Floor == f;
			bool done = inDungeon && NRRun.Floor > f;
			var rowBg = NRUI.Image(body, "Floor " + f, NRSprites.ColoredFrame(current ? NRPalette.Pink : NRPalette.Border), Color.white);
			NRUI.Place(rowBg.rectTransform, new Vector2(0, 1), new Vector2(0, -(f - 1) * 200), new Vector2(1480, 180), new Vector2(0, 1));
			var name = NRUI.Label(rowBg.transform,
				(current ? C(NRPalette.Pink) : done ? C(NRPalette.Green) : C(NRPalette.TextDim)) + f + "계층</color>  " + def.name +
				"\n<size=22>" + C(NRPalette.TextMute) + def.subtitle + " · 수문장: " + def.bossName + "</color></size>",
				32, NRPalette.Text, TextAlignmentOptions.TopLeft);
			NRUI.Place(name.rectTransform, new Vector2(0, 1), new Vector2(28, -18), new Vector2(520, 140), new Vector2(0, 1));

			// 방 노드
			var nodes = new List<(string label, int roundValue)>();
			if (f == 1) nodes.Add(("각성", 1));
			for (int r = 2; r <= over; r++) nodes.Add(((r - 1) + "번 방", r));
			nodes.Add(("상점", over + 1));
			nodes.Add(("수문장", over + 2));

			float x = 580;
			for (int i = 0; i < nodes.Count; i++)
			{
				var n = nodes[i];
				bool nodeCurrent = current && round == n.roundValue;
				bool nodeDone = done || (current && round > n.roundValue);
				Color col = nodeCurrent ? NRPalette.Pink : nodeDone ? NRPalette.Green : NRPalette.Border;
				var node = NRUI.Image(rowBg.transform, "Node", NRSprites.ColoredFrame(col), Color.white);
				NRUI.Place(node.rectTransform, new Vector2(0, 0.5f), new Vector2(x, -6), new Vector2(118, 76), new Vector2(0, 0.5f));
				var lbl = NRUI.Label(node.transform, n.label, 24, nodeCurrent ? Color.white : nodeDone ? NRPalette.Green : NRPalette.TextDim, TextAlignmentOptions.Center);
				NRUI.Stretch(lbl.rectTransform);
				if (nodeCurrent) node.gameObject.AddComponent<NRUIPulse>();
				if (i < nodes.Count - 1)
				{
					var link = NRUI.Image(rowBg.transform, "Link", NRSprites.White, nodeDone ? NRPalette.Green.WithAlpha(0.7f) : NRPalette.Border);
					NRUI.Place(link.rectTransform, new Vector2(0, 0.5f), new Vector2(x + 118, -6), new Vector2(24, 4), new Vector2(0, 0.5f));
				}
				x += 142;
			}
		}
		var note = NRUI.Label(body, C(NRPalette.TextMute) + "죽으면 집에서 다시 시작합니다. 악몽 결정과 영구 강화, 죽은 횟수는 유지됩니다.</color>", 24, NRPalette.Text, TextAlignmentOptions.Center);
		NRUI.Place(note.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 30), new Vector2(1400, 40), new Vector2(0.5f, 0));
	}

	// ---------------------------------------------------------------------
	void BuildStory()
	{
		var scroll = NRUI.ScrollView(body, "Scroll", out var content, 18, 36);
		NRUI.Stretch((RectTransform)scroll.transform);
		foreach (var line in GetStoryLines())
		{
			bool heading = line.StartsWith("#");
			var t = NRUI.LayoutText(content, heading ? line.Substring(1) : line, heading ? 36 : 28, heading ? NRPalette.Pink : NRPalette.Text);
		}
	}

	/// <summary>기존 씬의 스토리 텍스트(StorySet)를 읽어 재사용</summary>
	static IEnumerable<string> GetStoryLines()
	{
		if (cachedStory == null)
		{
			var list = new List<string>();
			var scene = SceneManager.GetActiveScene();
			var story = NRUtil.FindInScene(scene, "StorySet");
			if (story != null)
			{
				var texts = story.GetComponentsInChildren<TextMeshProUGUI>(true)
					.Where(t => t.gameObject.name.StartsWith("Text (TMP)"))
					.OrderByDescending(t => t.rectTransform.anchoredPosition.y)
					.ToList();
				foreach (var t in texts)
				{
					string s = t.text.Replace("\\n", "\n").Trim();
					if (string.IsNullOrEmpty(s)) continue;
					bool heading = t.fontSize <= 30 || list.Count == 0;
					list.Add(heading ? "#" + s : s);
				}
			}
			if (list.Count > 0) cachedStory = list.ToArray(); // 씬에서 찾았을 때만 캐시
			if (list.Count == 0)
			{
				list.Add("#악몽 속의 트라우마");
				list.Add("25살의 유능한 개발자인 나는 직장에서의 괴롭힘과 끝없는 업무, 인정받지 못한 성과 속에서 무너져 갔다.");
				list.Add("그리고 매일 밤, 같은 악몽 속으로 돌아간다.");
			}
			return list;
		}
		return cachedStory;
	}

	public static void ClearStoryCache() { cachedStory = null; }
}

/// <summary>UI 요소를 은은하게 깜빡임 (현재 위치 표시 등)</summary>
public class NRUIPulse : MonoBehaviour
{
	public float speed = 3f;
	public float amount = 0.06f;
	void Update()
	{
		float k = Mathf.Sin(Time.unscaledTime * speed);
		transform.localScale = Vector3.one * (1f + amount * k);
	}
	void OnDisable() { transform.localScale = Vector3.one; }
}
