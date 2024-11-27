using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//파씽
using UnityEngine.Networking;

public class ParsingManager : MonoBehaviour
{
	public GameObject DialogSet; // 대화창

	const string URL = "https://docs.google.com/spreadsheets/d/1tEMmdWn9jWmPBLcppi0xkukl86hoj3C0Jo3x9Y8utwU/export?format=tsv&range=I2:J1000";
	string SHEET;

	//파씽 스타트
	IEnumerator Start()
	{
		using (UnityWebRequest online_sheet = UnityWebRequest.Get(URL))
		{
			yield return online_sheet.SendWebRequest();

			if(online_sheet.isDone)
			{
				SHEET = online_sheet.downloadHandler.text;
			}
		}
		//테스트 메소드
		Add();
		//AddDialogs();
	}
	//대화번호, (대화주체:대화) 세트인 딕셔너리 생성
	Dictionary<int, string> dialog = new Dictionary<int, string>();


	//엑셀의 정보를 딕셔너리 변수에 추가
	void Add()
	{
		//엑셀에 있는 값을 행기준(줄간격)("\n")으로 분리
		string[] sheet = SHEET.Split('\n');

		//행 길이
		int rowCount = Mathf.Min(1000, sheet.Length - 1);

		//1부터 시작하는 이유: 열의 정보는 무시한다~
		for (int i = 1; i <= rowCount; i++)
		{
			//빈 행이 있는 경우를 처리 건너뛰기
			if (string.IsNullOrWhiteSpace(sheet[i]))
			{
				continue;
			}

			//엑셀에 있는 값을 탭(셀)("\t")으로 분리
			string[] hang = sheet[i].Split('\t');

			//대화 넘버와 대화 주체자가 모두 있어야 작동됨
			if (hang.Length < 2)
			{
				Debug.LogError($"올바르지 않은 데이터: {sheet[i]}");
				continue;
			}

			//대화번호 
			int talkNum;

			//대화번호가 숫자인지 판별
			if (int.TryParse(hang[0], out talkNum))
			{
				//대화:주체자
				string dialogAndTalker = hang[1];

				//중복 키 방지
				if (!dialog.ContainsKey(talkNum))
				{
					dialog.Add(talkNum, dialogAndTalker);
				}
				else
				{
					Debug.LogError($"중복된 키: {talkNum}");
				}
			}
			else
			{
				//실패시 알리미
				Debug.LogError($"대화 넘버 변환 실패: {hang[0]}");
			}
		}
		//// 던전 돌입시 독백 상황 추가
		//dialog.Add(40000, "많고 많은 사람중에~ 내가 제일 잘 나가지!:진우");
		//dialog.Add(40001, "오케이 해보자:진우");
		//dialog.Add(40002, "후..:진우");
		//dialog.Add(40003, "난 할수 있다!:진우");
		//dialog.Add(40004, "또 시작해볼까..:진우");
	}

	public TalkManager talkManager;

	//private void Start()
	//{
	//	AddDialogs();
	//}

	//void AddDialogs()
	//{
	//	//죽음 회차

	//	//1회차
	//	dialog.Add(10000 + 100 + 0, "이봐:진우");
	//	dialog.Add(10000 + 100 + 1, "야옹:고양이");
	//	dialog.Add(10000 + 100 + 2, "너 누구냐:진우");
	//	dialog.Add(10000 + 100 + 3, "야옹야옹:고양이");
	//	dialog.Add(10000 + 100 + 4, "어..:진우");
	//	dialog.Add(10000 + 100 + 5, "그렇구나:진우");

	//	dialog.Add(10000 + 200 + 0, "아저씨는 누구에요?:진우");
	//	dialog.Add(10000 + 200 + 1, "알아서 뭐해:아저씨");

	//	//2회차
	//	dialog.Add(20000 + 100 + 0, "오늘 저녁 뭐먹지?:진우");
	//	dialog.Add(20000 + 100 + 1, "야아아옹:고양이");
	//	dialog.Add(20000 + 100 + 2, "그 그래..:진우");

	//	dialog.Add(20000 + 200 + 0, "어이 자네:아저씨");
	//	dialog.Add(20000 + 200 + 1, "네:진우");
	//	dialog.Add(20000 + 200 + 2, "혹시 전설을 믿나?:아저씨");
	//	dialog.Add(20000 + 200 + 3, "아니요:진우");
	//	dialog.Add(20000 + 200 + 4, "근데 왜 물어봤어?:아저씨");
	//	dialog.Add(20000 + 200 + 5, "?:진우");
	//}

	public string[] GetDialogPlz(int DialogNum,_Object obj)
	{
		if (dialog.ContainsKey(DialogNum))
		{
			string[] parts = dialog[DialogNum].Split(':');
			if (parts.Length == 2)
			{
				string Talker = parts[1];
				string Dialog = parts[0];
			}
			return parts;
		}
		else
		{
			//Debug.Log("END");
			//talkManager.isTalking = false;
			talkManager.StopDialogSet(obj);
			talkManager.i = 0;
			string[] arr = {"...","..." };
			return arr;
		}
	}

	//독백
	public string[] GetSoloDialogPlz(int DialogNum)
    {
		if (dialog.ContainsKey(DialogNum))
        {
			string[] parts = dialog[DialogNum].Split(':');
			if (parts.Length == 2)
			{
				string Talker = parts[1];
				string Dialog = parts[0];
			}
			return parts;
		}
        else
        {
			string[] arr = { "...", "..." };
			return arr;
		}
	}
}
