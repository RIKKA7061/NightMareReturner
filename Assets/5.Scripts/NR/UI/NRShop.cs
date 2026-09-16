using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ============================================================================
// 상점 (기존 상점 UI를 대체) — 저렴한 소모품부터 비싼 증강까지
//  기존 아이템(결속 망치/기력 방울/과민의 눈)은 ItemManager.Signal 로 기존 효과 그대로 적용
// ============================================================================
public class NRShopItem
{
	public string id;
	public string name;
	public string desc;
	public int price;
	public int maxStock;
	public int icon;
	public Color color;
	public bool passive;          // 구매하면 HUD 패시브 줄에 표시
	public Func<bool> canBuy;     // 추가 조건
	public Action buy;
}

public class NRShop : NRModal
{
	static NRShop instance;
	static readonly Dictionary<string, int> stock = new Dictionary<string, int>();

	RectTransform grid;
	TextMeshProUGUI moneyText;

	static Player P => NRStats.Player;
	static ItemManager IM => FindObjectOfType<ItemManager>();

	public static readonly List<NRShopItem> Items = new List<NRShopItem>
	{
		new NRShopItem { id = "potion", name = "체력의 물약", price = 3, maxStock = 3, icon = 26, color = NRPalette.Hp,
			desc = "체력 80 회복",
			canBuy = () => P != null && P.nowHP < P.maxHP,
			buy = () => NRStats.HealPlayer(80, true) },
		new NRShopItem { id = "whetstone", name = "숫돌", price = 5, maxStock = 2, icon = 0, color = NRPalette.Rage,
			desc = "이번 회차 공격력 +5",
			buy = () => { if (P != null) { P.Atk += 5; P.Atk2 = P.Atk - 25; } } },
		new NRShopItem { id = "bigpotion", name = "큰 체력의 물약", price = 10, maxStock = 2, icon = 29, color = NRPalette.Crimson,
			desc = "최대 체력의 50% 회복",
			canBuy = () => P != null && P.nowHP < P.maxHP,
			buy = () => { if (P != null) NRStats.HealPlayer(P.maxHP * 0.5f, true); } },
		new NRShopItem { id = "vest", name = "가죽 조끼", price = 10, maxStock = 1, icon = 33, color = NRPalette.Will,
			desc = "이번 회차 방어력 +15",
			buy = () => { if (P != null) P.AR += 15; } },
		new NRShopItem { id = "hpdrop", name = "기력 방울", price = 25, maxStock = 1, icon = 27, color = NRPalette.Green, passive = true,
			desc = "[패시브] 방에 들어갈 때마다 체력 회복",
			canBuy = () => IM != null && !IM.isNextRoundHpUp,
			buy = () => Legacy("기력 방울", 2) },
		new NRShopItem { id = "eye", name = "과민의 눈", price = 25, maxStock = 1, icon = 14, color = NRPalette.Anxiety, passive = true,
			desc = "[패시브] 방마다 공격·이동 속도 증가,\n방어력 감소",
			canBuy = () => IM != null && !IM.isCrunchMode,
			buy = () => Legacy("과민의 눈", 3) },
		new NRShopItem { id = "hammer", name = "결속 망치", price = 50, maxStock = 1, icon = 2, color = NRPalette.Gold, passive = true,
			desc = "[패시브] 3개 방 동안 궁극기 피해\n대폭 증가 (방마다 1회)",
			canBuy = () => IM != null && !IM.isHammerBuffing,
			buy = () => Legacy("결속 망치", 1) },
		new NRShopItem { id = "scroll", name = "감정의 두루마리", price = 100, maxStock = 1, icon = 28, color = NRPalette.Pink,
			desc = "증강 1개를 선택합니다",
			buy = () => NRAugmentSelect.Show(null, false, "감정의 두루마리", null) },
	};

	static void Legacy(string name, int id)
	{
		var im = IM;
		if (im == null) return;
		try { im.Signal(name, id); }
		catch (Exception e) { Debug.LogWarning("[NRShop] 기존 아이템 적용 중 오류(효과는 적용됨): " + e.Message); }
	}

	public static void Restock()
	{
		stock.Clear();
		foreach (var it in Items) stock[it.id] = it.maxStock;
	}

	static int Stock(NRShopItem it)
	{
		if (!stock.ContainsKey(it.id)) stock[it.id] = it.maxStock;
		return stock[it.id];
	}

	public static void Show()
	{
		if (NRUIState.AnyOpen) return;
		if (instance == null) instance = Build();
		instance.Open();
	}

	static NRShop Build()
	{
		var root = NRModalHost.CreateModalRoot("NR Shop", 0.7f);
		var s = root.gameObject.AddComponent<NRShop>();
		var panel = NRUI.Panel(root, "Panel", NRSprites.ColoredFrame(NRPalette.Gold));
		NRUI.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1180, 700));

		var title = NRUI.Label(panel.transform, "악몽 상인", 50, NRPalette.Gold, TextAlignmentOptions.MidlineLeft, NRTextFx.OutlineShadow);
		NRUI.Place(title.rectTransform, new Vector2(0, 1), new Vector2(40, -22), new Vector2(500, 70), new Vector2(0, 1));
		var sub = NRUI.Label(panel.transform, "필요한 것을 골라 가세요. 재화는 적을 처치하면 떨어집니다.", 24, NRPalette.TextDim, TextAlignmentOptions.MidlineLeft);
		NRUI.Place(sub.rectTransform, new Vector2(0, 1), new Vector2(42, -86), new Vector2(900, 40), new Vector2(0, 1));

		var moneyBg = NRUI.Image(panel.transform, "Money", NRSprites.DarkFrame, Color.white);
		NRUI.Place(moneyBg.rectTransform, new Vector2(1, 1), new Vector2(-40, -28), new Vector2(230, 62), new Vector2(1, 1));
		var coin = NRUI.Icon(moneyBg.transform, NRSprites.Coin, 34, NRPalette.Gold);
		NRUI.Place(coin.rectTransform, new Vector2(0, 0.5f), new Vector2(22, 0), new Vector2(34, 34), new Vector2(0, 0.5f));
		s.moneyText = NRUI.Label(moneyBg.transform, "0", 34, NRPalette.Gold, TextAlignmentOptions.MidlineRight);
		NRUI.Stretch(s.moneyText.rectTransform, 70, 20, 0, 0);

		s.grid = NRUI.Rect(panel.transform, "Grid");
		NRUI.Place(s.grid, new Vector2(0.5f, 1), new Vector2(0, -140), new Vector2(1100, 460), new Vector2(0.5f, 1));
		var g = s.grid.gameObject.AddComponent<GridLayoutGroup>();
		g.cellSize = new Vector2(262, 222);
		g.spacing = new Vector2(14, 14);
		g.childAlignment = TextAnchor.UpperCenter;

		var close = NRUI.Button(panel.transform, "닫기 (ESC)", () => s.Close(), new Vector2(260, 62), 28);
		NRUI.Place(close.Rect, new Vector2(0.5f, 0), new Vector2(0, 24), new Vector2(260, 62), new Vector2(0.5f, 0));
		root.gameObject.SetActive(false);
		return s;
	}

	protected override void OnOpened()
	{
		NRHud.SetVisible(false);
		Refresh();
	}

	protected override void OnClosed()
	{
		NRHud.SetVisible(true);
	}

	void Refresh()
	{
		moneyText.text = Player.Money.ToString();
		foreach (Transform c in grid) Destroy(c.gameObject);
		foreach (var it in Items)
		{
			var item = it;
			int left = Stock(item);
			bool condition = item.canBuy == null || item.canBuy();
			bool affordable = Player.Money >= item.price;
			bool available = left > 0 && condition;

			var card = NRUI.Image(grid, item.id, NRSprites.ColoredFrame(available ? item.color : NRPalette.Border), Color.white, true);
			var iconBg = NRUI.Image(card.transform, "IconBg", NRSprites.DarkFrame, Color.white);
			NRUI.Place(iconBg.rectTransform, new Vector2(0, 1), new Vector2(14, -14), new Vector2(76, 76), new Vector2(0, 1));
			var icon = NRUI.Icon(iconBg.transform, NRSprites.Icon(item.icon), 56);
			NRUI.Place(icon.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(56, 56));
			if (!available) icon.color = new Color(1, 1, 1, 0.35f);

			var name = NRUI.Label(card.transform, item.name, 26, available ? item.color : NRPalette.TextMute, TextAlignmentOptions.TopLeft);
			NRUI.Place(name.rectTransform, new Vector2(0, 1), new Vector2(98, -14), new Vector2(156, 60), new Vector2(0, 1));
			var st = NRUI.Label(card.transform, left > 0 ? "남은 수량 " + left : "품절", 20, NRPalette.TextMute, TextAlignmentOptions.TopLeft);
			NRUI.Place(st.rectTransform, new Vector2(0, 1), new Vector2(98, -62), new Vector2(156, 28), new Vector2(0, 1));

			var desc = NRUI.Label(card.transform, item.desc, 21, NRPalette.Text, TextAlignmentOptions.TopLeft);
			NRUI.Place(desc.rectTransform, new Vector2(0, 1), new Vector2(16, -98), new Vector2(232, 60), new Vector2(0, 1));

			string label = left <= 0 ? "품절" : !condition ? "이미 보유" : "◎ " + item.price;
			var btn = NRUI.Button(card.transform, label, () => Buy(item), new Vector2(232, 48), 26);
			NRUI.Place(btn.Rect, new Vector2(0.5f, 0), new Vector2(0, 12), new Vector2(232, 48), new Vector2(0.5f, 0));
			btn.normalText = affordable && available ? NRPalette.Gold : NRPalette.TextMute;
			btn.SetInteractable(available);
		}
	}

	void Buy(NRShopItem item)
	{
		if (Stock(item) <= 0) return;
		if (item.canBuy != null && !item.canBuy()) return;
		if (Player.Money < item.price)
		{
			NRUIRoot.ToastMsg("재화가 부족합니다 (필요 " + item.price + ")", NRPalette.Crimson, 1.6f);
			return;
		}
		Player.Money -= item.price;
		stock[item.id] = Stock(item) - 1;
		NRAudio.PlaySfx("coin", 0.8f);
		NRUIRoot.ToastMsg(item.name + " 구매", item.color, 1.6f);
		bool opensModal = item.id == "scroll";
		if (opensModal) Close();
		item.buy?.Invoke();
		if (!opensModal) Refresh();
	}
}
