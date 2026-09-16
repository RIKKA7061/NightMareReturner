using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using UnityEngine;

public class ShopManager : MonoBehaviour
{
    Player player;
    ItemManager itemManager;    // 아이템 매니저
    public int ItemID;          // 아이템 번호
    public int ItemMoney;       // 재화
    public Text ItemMoneyTxt;
    public Text ItemNameTxt;
    public GameObject soldout;

    private void Awake()
    {
        player = FindObjectOfType<Player>(); // 무조건 해줘야됨 (초기화)
		itemManager = FindAnyObjectByType<ItemManager>();
	}

    private void Update()
    {
        if (ItemMoneyTxt != null) ItemMoneyTxt.text = ItemMoney.ToString();
    }

    // [NR] 새 회차/계층마다 상점 재입고
    public void Restock()
    {
        var btn = GetComponent<Button>();
        if (btn != null) btn.interactable = true;
        if (soldout != null) soldout.SetActive(false);
    }

    public void Buy()
    {
        if (Player.Money >= ItemMoney)
        {
            Player.Money = Player.Money - ItemMoney;
            GetComponent<Button>().interactable = false;
			string ItemNametext = ItemNameTxt.text;

            itemManager.Signal(ItemNametext, ItemID);

            soldout.SetActive(true);
            NRUIRoot.ToastMsg(ItemNametext + " 구매", NRPalette.Gold, 1.6f); // [NR]
            // Debug.Log($"{ItemNametext}를 구매하셨습니다.");
        }

		else if (Player.Money < ItemMoney)
        {
            Debug.Log("돈 없다.");
            NRUIRoot.ToastMsg("재화가 부족합니다 (필요 " + ItemMoney + ")", NRPalette.Crimson, 1.6f); // [NR]
        }
    }
}
