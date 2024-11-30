using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static ScriptableObjectTest;

// 아이템 정보를 관리하는 테이블
[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Game/ItemDatabase")]
public class ItemTable : ScriptableObject
{
	// 아이템 데이터 구조체
	[System.Serializable]
	public class ItemData
	{
		public string Name;	  // 아이템 이름
		public int num1;      // 변수1 
		public float num2;    // 변수2
		public float num3;	  // 변수3

		public ItemData(string name, int Num1, float Num2, float Num3)
		{
			Name = name;
			num1 = Num1;
			num2 = Num2;
			num3 = Num3;
		}
	}

	// 아이템 목록
	static public Dictionary<int, ItemData> itemDictionary;

	static ItemTable()
	{
		itemDictionary = new Dictionary<int, ItemData>();

		// 아이템 데이터 등록
		itemDictionary.Add(1, new ItemData("결속 망치", 250, 0, 0));
		itemDictionary.Add(2, new ItemData("기력 방울", 1, 0, 0)); // 예제 아이템
		itemDictionary.Add(3, new ItemData("과민의 눈", -1, 0.1f, 0.02f)); // 방어력 감소, 이속 증가, 공속 증가
	}

	// 아이템 데이터 가져오기
	//public ItemData GetItemData(int itemID)
	//{
	//	if (itemDictionary.ContainsKey(itemID))
	//	{
	//		return itemDictionary[itemID];
	//	}
	//	Debug.LogWarning($"Item ID {itemID} not found in ItemTable.");
	//	return null;
	//}
}

public class ItemManager : MonoBehaviour
{
	public ScriptableObjectTest[] itemData;  // ScriptableObjectTest 연결 필드

	public Player player;               // 플레이어 스크립트
    public PlayerAction playerAction;   // 플레이어 액션 스크립트

    public bool isHammerBuffing;        // 해머 공격력 상승 버프 적용 여부
	public bool isHammerAtkUsed;        // 1회 해머 공격 실행 됨?

	public bool isCrunchMode;           // 지금 과민의 눈 중인가?
	public GameObject CrunchEye;		// 과민의 눈 버프 이미지 

	public bool isNextRoundHpUp = false;// HpUpCheck
	public GameObject HpUPSet;

	public GameObject HammerBuffSet;	// 결속망치 버프 표시용 이미지
	public TextMeshProUGUI HammerCount; // 버프 라운드 횟수

	public void Signal(string itemText, int itemID) // 상점 아이템 신호 받아온다.
    {
        switch (itemID)
        {
            case 1:
                ItemPrint(itemText, itemID);
                Hammer();
                break;
            case 2:
                ItemPrint(itemText, itemID);
				NextRoundHpUp();
				break;
            case 3:
                ItemPrint(itemText, itemID);
				CrunchModeEyeStarter();
				CrunchModeEye();
				break;

        }
    }

    private void ItemPrint(string itemText, int itemID) // 테스트
    {
		//Debug.Log(itemID + "번 아이템 :" + itemText + "를 실행");
	}
	
    private void Hammer()// 결속 망치 아이템 함수
	{
        player.HammerBuffedRoundCount = 3;		// 3 라운드만 한해서 버프 부여 (라운드 이동시 1 소모)
		isHammerBuffing = true;
		HammerBuffSet.SetActive(true);          // 버프 세트 출력
		HammerCount.text = player.HammerBuffedRoundCount.ToString(); // 라운드 횟수 출력
	}

	public void HammerBuff()// 해머 공격력 상승 (단, 버프중 일때 && 1회에 한해서)
	{
		if (isHammerBuffing == true && isHammerAtkUsed == false)
		{
			player.Atk += itemData[0].Num1;
		}
	}

	public void HammerDeBuff()// 해머 공격력 하락 (단, 버프중 일때 && 1회에 한해서)
	{
		if (isHammerBuffing == true && isHammerAtkUsed == false)
		{
			player.Atk -= itemData[0].Num1;
			isHammerAtkUsed = true; // 1회 해머 공격 상승권 써버림

			if (player.HammerBuffedRoundCount == 0) // 해머버프 라운드 횟수가 0 일시
			{
				GoodByeHammerBuff();    // 해머버프 잘가
			}
		}
	}

	public void RoundUp()
	{
		if(isHammerBuffing == true) // 라운드 기회 1소모 (단, 버프중 일때)
		{
			player.HammerBuffedRoundCount--;
			isHammerAtkUsed = false; // 1회 해머 공격 상승권 생김
			HammerCount.text = player.HammerBuffedRoundCount.ToString(); // 라운드 횟수 출력
		}

		if (player.HammerBuffedRoundCount < 0)
		{
			GoodByeHammerBuff();	// 해머버프 잘가
		}
	}

	public void GoodByeHammerBuff()
	{
		isHammerBuffing = false;                 // 라운드 수를 초과 해버렸으므로 (전체적인 해머)버프를 박탈합니다.
		HammerBuffSet.SetActive(false);          // 버프 세트 다시 안보이게
	} // 해머 버프 해제 함수

	public void NextRoundHpUp() // 기력 방울 함수
	{
		isNextRoundHpUp = true;
		HpUPSet.SetActive(true);
	}

	public void DebuffHpUp() // 기력 방울 버프 해제 함수
	{
		HpUPSet.SetActive(false);
	}

	public void HpUP() // 기력 방울 버프 있을시 적용
	{
		if(player.nowHP == player.maxHP)
		{

		}
		else if(player.nowHP <= player.maxHP)
		{
			player.nowHP += itemData[1].Num1;// hp 올려줌
		}
	}

	public void CrunchModeEyeStarter()
	{
		isCrunchMode = true;
		CrunchEye.SetActive(true);
	}

	public void OFFCrunchMode()
	{
		isCrunchMode = false;
		CrunchEye.SetActive(false);
	}

	public void CrunchModeEye() // 과민의 눈
	{
		// 공속 증가 (부활시 초기화 해야될듯)
		playerAction.jabCooldown -= itemData[2].Num3;

		// 이속 증가 (부활시 초기화 해야될듯)
		playerAction.walkSpeed += itemData[2].Num2;

		// 방어력 감소 (부활시 초기화 해야될듯)
		player.AR += itemData[2].Num1;
	}
}
