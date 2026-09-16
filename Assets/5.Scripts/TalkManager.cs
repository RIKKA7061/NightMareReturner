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
		if (TalkCountNum != null) TalkCountNum.text = text;//test
	}

	public int i = 0;
	public int j = 0;

	// [NR] 대화 진행 규칙 (사용자 요청)
	//  - 죽지 않아도 대화 가능
	//  - 죽지 않았으면 같은 세트를 반복
	//  - 현재 세트를 끝까지 본 뒤 죽고 돌아오면 다음 세트
	//  대화 번호 규칙(세트×10000 + NPC id + 순번)과 시트 형식은 기존 그대로
	void Talk(int id, GameObject scannedObject)
	{
		_Object obj = scannedObject.GetComponent<_Object>();
		int idx = Mathf.Max(0, id / 100);
		if (idx >= _Object.EachTalkCountSaveNum.Length) System.Array.Resize(ref _Object.EachTalkCountSaveNum, idx + 1);
		var seenArr = NRSave.Data.talkSeenAtDeath;
		if (idx >= seenArr.Length)
		{
			int old = seenArr.Length;
			System.Array.Resize(ref seenArr, idx + 1);
			for (int k = old; k < seenArr.Length; k++) seenArr[k] = -1;
			NRSave.Data.talkSeenAtDeath = seenArr;
		}

		// 상점 NPC는 대화 대신 상점
		if (ScanObject.CompareTag("shop"))
		{
			NRShop.Show();
			return;
		}

		// ---- 대화 시작 ----
		if (!isDialoging)
		{
			int set = Mathf.Max(1, _Object.EachTalkCountSaveNum[idx]);
			int seenAt = seenArr[idx];
			bool advanced = false;
			if (_Object.EachTalkCountSaveNum[idx] >= 1 && seenAt >= 0 && Player.DeadCount > seenAt && parsingManager.HasDialog((set + 1) * 10000 + id))
			{
				set++;
				seenArr[idx] = -1;
				advanced = true;
			}
			_Object.EachTalkCountSaveNum[idx] = set;
			obj.EachTalkCount = set;
			NRSave.MarkDirty();

			if (!parsingManager.HasDialog(set * 10000 + id))
			{
				NRUIRoot.ToastMsg(parsingManager.IsLoaded ? "지금은 나눌 이야기가 없습니다" : "대화를 불러오는 중입니다...", NRPalette.TextDim, 2f);
				return;
			}

			if (!advanced && seenArr[idx] >= 0)
				NRUIRoot.ToastMsg("이미 들은 이야기입니다 — 악몽에서 쓰러진 뒤 다시 오면 다음 이야기를 들을 수 있습니다", NRPalette.TextDim, 3.2f);
			else if (advanced)
				NRUIRoot.ToastMsg("새로운 이야기", NRPalette.Pink, 1.6f);

			i = 0;
			isDialoging = true;// talking now
			DialogSet.SetActive(true);
			obj.isDialogged = true;
		}

		obj.tempPlayerDead = Player.DeadCount;//현재 죽음 횟수 임시 저장

		int DialogNum = obj.EachTalkCount * 10000 + id + i;//대화번호
		i++;
		string[] texts = parsingManager.GetDialogPlz(DialogNum, obj);
		talker = texts.Length > 1 ? texts[1] : "";
		dialog = texts[0];
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
		bool wasTalking = isDialoging;
		DialogSet.SetActive(false);
		isDialoging = false; // not talking now
		obj.isDialogged = false;

		// [NR] 세트를 끝까지 봄 → 기록, 다음 이야기 안내
		if (wasTalking && obj != null)
		{
			int idx = Mathf.Max(0, obj.id / 100);
			if (idx < NRSave.Data.talkSeenAtDeath.Length)
			{
				NRSave.Data.talkSeenAtDeath[idx] = Player.DeadCount;
				NRSave.MarkDirty();
			}
			bool hasNext = parsingManager.HasDialog((obj.EachTalkCount + 1) * 10000 + obj.id);
			NRUIRoot.ToastMsg(hasNext ? "던전(악몽)에서 쓰러지고 돌아오면 새로운 이야기를 들을 수 있습니다" : "이 사람과 나눌 이야기는 여기까지입니다", hasNext ? NRPalette.Pink : NRPalette.TextDim, 3.2f);
		}
	}
}
