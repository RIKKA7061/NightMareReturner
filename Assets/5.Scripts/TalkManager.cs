using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;//TextMeshPro용 namespace
using UnityEngine;

public class TalkManager : MonoBehaviour
{
	/*TalkManger란 대화 관련된 것을 처리하는 스크립트입니다.*/

	[Header("NPC 스캔")]
	public GameObject ScanObject;

	[Header("Player")]
	public Player player;

	[Header("파씽")]
	public ParsingManager parsingManager;

	[Header("대화창")]
	public TextMeshProUGUI Talker;
	public TextMeshProUGUI Dialog;
	string talker = "";
	string dialog = "";
    public GameObject Shop;
    public GameObject DialogSet;
	public bool isDialoging; // 상태 저장 변수

	[Header("테스트")]
	public TextMeshProUGUI TalkCountNum;
	string text = "";
	string NPCname;//테스트용 NPC 이름 변수

	int tempId = 0;//임시 NPC 주민등록번호
	//int tempDeadCount = 0;//임시 Player죽음 횟수

	public bool isOverIndex = false;//대화 흐름수 EX. 10100~10107

	//대화 액션
	public void DialogAction(GameObject scannedObject)//진우가 한번 스캔시 작동
	{
		ScanObject = scannedObject;//진우가 스캔한 NPC

		_Object obj = scannedObject.GetComponent<_Object>();//NPC 정보

		
		Talk(obj.id, scannedObject);
		tempId = obj.id;
		NPCname = obj.name;
		text = NPCname + "의 회차별 대화 횟수 : " + obj.EachTalkCount.ToString();//test
	}

	private void Update()
	{
		Talker.text = talker.ToString();//DialogSet
		Dialog.text = dialog.ToString();//DialogSet
		TalkCountNum.text = text;//test
	}

	public int i = 0;
	public int j = 0;
	void Talk(int id, GameObject scannedObject)
	{
		_Object obj = scannedObject.GetComponent<_Object>();

		if(tempId != id)
		{
			//Debug.Log("B 나는 1 늘어나");
		}


		//죽음의 수가 바뀌었는가?
		if (obj.tempPlayerDead != Player.DeadCount)
		{
			i = 0;
			obj.isDeadUp = true;

			//대화창 오픈
			//Debug.Log("대화열기");

			isDialoging = true;// talking now
			DialogSet.SetActive(true);

		}

        if (ScanObject.CompareTag("shop"))
        {
            Shop.SetActive(true);
        }

        // 죽음 횟수 가 NPC 기준으로 바뀌었는가?
        if (Player.DeadCount > _Object.EachTalkCountSaveNum[obj.id/100] && obj.isDeadUp == true)
		{
			obj.EachTalkCount++;
			obj.EachTalkCountSave(obj.id); // 대화 넘버 저장 함수
			obj.isDeadUp = false;
			obj.isDialogged = true;
		}

		if (tempId != 0 && tempId != id)//다른사람 대화했다가 저사람 대화했다가 순번 꼬이는 일 방지
		{
			i = 0;
		}

		obj.tempPlayerDead = Player.DeadCount;//현재 죽음 횟수 임시 저장

		if (isOverIndex == false)//대화 순번 index를 넘지 않을 때
		{
			int DialogNum = obj.EachTalkCount * 10000 + id + i;//대화번호
			//Debug.Log(DialogNum);//테스트
			i++;
			string[] texts = parsingManager.GetDialogPlz(DialogNum, obj);
			talker = texts[1];
			dialog = texts[0];
		}
		else if (isOverIndex == true)//대화 순번 index를 넘을 때
		{
			i = 0;
			isOverIndex = false;
		}
	}

	public void SoloTalk()
    {
		int DialogNum =  10000 + (400) + j;//대화번호
		j++;
		string[] texts = parsingManager.GetSoloDialogPlz(DialogNum); // 대화 가져오는 함수
		talker = texts[1]; // 대화 하는 사람 이름 (현재 보여집니다.)
		dialog = texts[0]; // 대화 내용 (현재 보여집니다.22)

		if(talker == "...") // 독백 내용이 더이상 없다면 안보여주기
        {
			j = 0;
			Debug.Log("할 독백 내용 없음");
        }
        else
        {
			DialogSet.SetActive(true);//대화 창 보이게 하기
			StartCoroutine(SoloStopTalkTime());// 시간 지연후 대화창 닫기
		}
	}

	// 시간 지연후 대화 닫기
	IEnumerator SoloStopTalkTime()
    {
		yield return new WaitForSeconds(2); // 2초 정도만 보여주고
		StopSoloTalk(); // 대화 안보이게 하기
    }

	//대화창 닫는 함수
	public void StopSoloTalk()
    {
		DialogSet.SetActive(false);//대화 안 보이게 하기
	}

	public void StopDialogSet(_Object obj)
	{
		DialogSet.SetActive(false);
		isDialoging = false; // not talking now
		//Debug.Log("대화 닫기");
		obj.isDialogged = false;
	}
}
