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

    private void Awake()
    {
        player = FindObjectOfType<Player>(); // 무조건 해줘야됨 (초기화)
		itemManager = FindAnyObjectByType<ItemManager>();
	}

    private void Update()
    {
        ItemMoneyTxt.text = ItemMoney.ToString();
    }

    public void Buy()
    {
        if (Player.Money >= ItemMoney)
        {
            Player.Money = Player.Money - ItemMoney;
            GetComponent<Button>().interactable = false;
			string ItemNametext = ItemNameTxt.text;

            itemManager.Signal(ItemNametext, ItemID);

            // Debug.Log($"{ItemNametext}를 구매하셨습니다.");
        }

		else if (Player.Money < ItemMoney)
        {
            Debug.Log("돈 없다.");
        }
    }
}
