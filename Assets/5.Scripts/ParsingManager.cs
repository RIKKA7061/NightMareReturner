using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

//파씽
using UnityEngine.Networking;

// [NR] 실시간 대화 파싱은 그대로 유지하고, 데이터를 받는 출처에 폴백만 추가
//      1) Google 시트(온라인, 타임아웃 8초) → 성공 시 로컬 캐시에 저장
//      2) 실패 시 마지막으로 성공한 캐시 (persistentDataPath)
//      3) 캐시도 없으면 게임에 포함된 스냅샷 (Resources/NR/Text/dialogue_fallback)
//      대화 번호 규칙, Add() 파싱 방식, GetDialogPlz/GetSoloDialogPlz는 변경 없음
public class ParsingManager : MonoBehaviour
{
	public GameObject DialogSet; // 대화창

	const string URL = "https://docs.google.com/spreadsheets/d/1tEMmdWn9jWmPBLcppi0xkukl86hoj3C0Jo3x9Y8utwU/export?format=tsv&range=I2:J1000";
	string SHEET;

	[Header("[NR] 출시 빌드에서 온라인 시트를 사용할지 (끄면 캐시/내장 데이터만 사용)")]
	public bool useOnlineSheet = true;

	static string CachePath => Path.Combine(Application.persistentDataPath, "dialogue_cache.tsv");

	//파씽 스타트
	IEnumerator Start()
	{
		if (useOnlineSheet)
		{
			using (UnityWebRequest online_sheet = UnityWebRequest.Get(URL))
			{
				online_sheet.timeout = 8;
				yield return online_sheet.SendWebRequest();

				if (online_sheet.result == UnityWebRequest.Result.Success && IsValidSheet(online_sheet.downloadHandler.text))
				{
					SHEET = online_sheet.downloadHandler.text;
					TryWriteCache(SHEET);
				}
				else
				{
					Debug.LogWarning("[ParsingManager] 온라인 대화 시트를 받지 못해 로컬 데이터를 사용합니다: " + online_sheet.error);
				}
			}
		}

		if (string.IsNullOrEmpty(SHEET)) SHEET = TryReadCache();
		if (string.IsNullOrEmpty(SHEET))
		{
			var bundled = Resources.Load<TextAsset>("NR/Text/dialogue_fallback");
			if (bundled != null) SHEET = bundled.text;
		}

		//테스트 메소드
		Add();
		//AddDialogs();
	}

	static bool IsValidSheet(string text)
	{
		return !string.IsNullOrEmpty(text) && text.Contains("\t") && !text.TrimStart().StartsWith("<");
	}

	static void TryWriteCache(string text)
	{
		try { File.WriteAllText(CachePath, text); }
		catch (Exception e) { Debug.LogWarning("[ParsingManager] 캐시 저장 실패: " + e.Message); }
	}

	static string TryReadCache()
	{
		try { return File.Exists(CachePath) ? File.ReadAllText(CachePath) : null; }
		catch { return null; }
	}

	//대화번호, (대화주체:대화) 세트인 딕셔너리 생성
	Dictionary<int, string> dialog = new Dictionary<int, string>();


	//엑셀의 정보를 딕셔너리 변수에 추가
	void Add()
	{
		if (string.IsNullOrEmpty(SHEET)) // [NR] 데이터가 전혀 없을 때 예외 방지
		{
			Debug.LogError("[ParsingManager] 대화 데이터가 없습니다.");
			return;
		}

		//엑셀에 있는 값을 행기준(줄간격)("\n")으로 분리
		string[] sheet = SHEET.Split('\n');

		//행 길이
		int rowCount = Mathf.Min(1000, sheet.Length - 1);

		//1부터 시작하는 이유: 열의 정보는 무시한다~
		for (int i = 1; i <= rowCount; i++)
		{
			string line = sheet[i].TrimEnd('\r'); // [NR] 줄 끝 \r 제거 (이름 뒤에 보이지 않는 문자가 붙던 문제)

			//빈 행이 있는 경우를 처리 건너뛰기
			if (string.IsNullOrWhiteSpace(line))
			{
				continue;
			}

			//엑셀에 있는 값을 탭(셀)("\t")으로 분리
			string[] hang = line.Split('\t');

			//대화 넘버와 대화 주체자가 모두 있어야 작동됨
			if (hang.Length < 2)
			{
				Debug.LogError($"올바르지 않은 데이터: {line}");
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
			else if (!string.IsNullOrWhiteSpace(hang[0]))
			{
				//실패시 알리미
				Debug.LogError($"대화 넘버 변환 실패: {hang[0]}");
			}
		}
	}

	public TalkManager talkManager;

	public string[] GetDialogPlz(int DialogNum,_Object obj)
	{
		if (dialog.ContainsKey(DialogNum))
		{
			string[] parts = dialog[DialogNum].Split(':');
			return parts;
		}
		else
		{
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
			return parts;
		}
        else
        {
			string[] arr = { "...", "..." };
			return arr;
		}
	}
}
