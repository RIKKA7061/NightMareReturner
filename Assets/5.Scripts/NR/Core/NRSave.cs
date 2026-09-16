using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// ============================================================================
// 영구 저장: 죽은 횟수 / 대화 진행 / 악몽 결정(영구 재화) / 영구 강화 / 기록
// 설정은 PlayerPrefs(NRSettings) 사용
// ============================================================================

[Serializable]
public class NRUpgradeLevel
{
	public string id;
	public int level;
}

[Serializable]
public class NRSaveData
{
	public int version = 1;

	// 스토리 진행
	public int deadCount;
	public int[] talkSaveNums = new int[4];

	// 영구 재화 / 강화
	public int crystals;
	public int totalCrystalsEarned;
	public List<NRUpgradeLevel> upgrades = new List<NRUpgradeLevel>();

	// 기록
	public int runs;
	public int clears;
	public int bestFloor;
	public int bossKills;
	public int enemyKills;
	public int augmentsPicked;

	// 튜토리얼 안내 1회 표시용
	public bool seenMirrorTip;
	public bool seenAugmentTip;
	public bool seenDashTip;
	public bool seenTownTip;

	// 첫 실행 설정 (조작 방식 / 캐릭터)
	public bool setupDone;
	public int hero;              // 0 근접, 1 원거리

	// NPC별로 현재 대화 세트를 끝까지 본 시점의 죽은 횟수 (-1 = 아직 다 안 봄)
	public int[] talkSeenAtDeath = new int[] { -1, -1, -1, -1 };
}

public static class NRSave
{
	public static NRSaveData Data { get; private set; } = new NRSaveData();
	public static event Action OnChanged;

	static bool dirty;
	static float lastSaveTime = -10f;

	static string MainPath => Path.Combine(Application.persistentDataPath, "nr_save.json");
	static string BackupPath => Path.Combine(Application.persistentDataPath, "nr_save.bak");
	static string TempPath => Path.Combine(Application.persistentDataPath, "nr_save.tmp");

	public static bool HasProgress => Data.deadCount > 0 || Data.runs > 0 || Data.crystals > 0;

	public static void Load()
	{
		Data = TryRead(MainPath) ?? TryRead(BackupPath) ?? new NRSaveData();
		Sanitize();
		PushToGame();
		dirty = false;
	}

	static NRSaveData TryRead(string path)
	{
		try
		{
			if (!File.Exists(path)) return null;
			var json = File.ReadAllText(path);
			if (string.IsNullOrWhiteSpace(json)) return null;
			return JsonUtility.FromJson<NRSaveData>(json);
		}
		catch (Exception e)
		{
			Debug.LogWarning($"[NRSave] 저장 파일 읽기 실패({path}): {e.Message}");
			return null;
		}
	}

	static void Sanitize()
	{
		if (Data.talkSaveNums == null || Data.talkSaveNums.Length < 4)
		{
			var arr = new int[4];
			if (Data.talkSaveNums != null) Array.Copy(Data.talkSaveNums, arr, Data.talkSaveNums.Length);
			Data.talkSaveNums = arr;
		}
		if (Data.upgrades == null) Data.upgrades = new List<NRUpgradeLevel>();
		if (Data.talkSeenAtDeath == null || Data.talkSeenAtDeath.Length < 4)
		{
			var arr = new int[] { -1, -1, -1, -1 };
			if (Data.talkSeenAtDeath != null) System.Array.Copy(Data.talkSeenAtDeath, arr, Data.talkSeenAtDeath.Length);
			Data.talkSeenAtDeath = arr;
		}
		Data.deadCount = Mathf.Max(0, Data.deadCount);
		Data.crystals = Mathf.Max(0, Data.crystals);
	}

	/// <summary>저장값을 기존 게임의 정적 변수에 반영</summary>
	static void PushToGame()
	{
		Player.DeadCount = Data.deadCount;
		if (_Object.EachTalkCountSaveNum == null || _Object.EachTalkCountSaveNum.Length < Data.talkSaveNums.Length)
			_Object.EachTalkCountSaveNum = new int[Mathf.Max(4, Data.talkSaveNums.Length)];
		Array.Copy(Data.talkSaveNums, _Object.EachTalkCountSaveNum, Data.talkSaveNums.Length);
	}

	/// <summary>기존 게임 코드가 바꾸는 정적 값(죽은 횟수, 대화 진행)을 감시해 저장</summary>
	public static void PollGameState()
	{
		if (Player.DeadCount != Data.deadCount)
		{
			Data.deadCount = Player.DeadCount;
			MarkDirty();
		}

		var talk = _Object.EachTalkCountSaveNum;
		if (talk != null)
		{
			if (Data.talkSaveNums.Length != talk.Length)
			{
				Data.talkSaveNums = (int[])talk.Clone();
				MarkDirty();
			}
			else
			{
				for (int i = 0; i < talk.Length; i++)
				{
					if (Data.talkSaveNums[i] != talk[i])
					{
						Data.talkSaveNums[i] = talk[i];
						MarkDirty();
					}
				}
			}
		}

		if (dirty && Time.unscaledTime - lastSaveTime > 1.5f) Save();
	}

	public static void MarkDirty()
	{
		dirty = true;
		OnChanged?.Invoke();
	}

	public static void Save()
	{
		try
		{
			Data.deadCount = Player.DeadCount;
			var json = JsonUtility.ToJson(Data, true);
			Directory.CreateDirectory(Application.persistentDataPath);
			File.WriteAllText(TempPath, json);
			if (File.Exists(MainPath))
			{
				File.Copy(MainPath, BackupPath, true);
				File.Delete(MainPath);
			}
			File.Move(TempPath, MainPath);
			dirty = false;
			lastSaveTime = Time.unscaledTime;
		}
		catch (Exception e)
		{
			Debug.LogWarning("[NRSave] 저장 실패: " + e.Message);
		}
	}

	public static void ResetProgress()
	{
		bool setup = Data.setupDone;
		int hero = Data.hero;
		Data = new NRSaveData();
		Data.setupDone = setup;
		Data.hero = hero;
		Sanitize();
		PushToGame();
		Save();
		OnChanged?.Invoke();
	}

	// ---- 악몽 결정 ----
	public static int AddCrystals(int baseAmount, bool applyBonus = true)
	{
		if (baseAmount <= 0) return 0;
		float mul = applyBonus ? 1f + NRMeta.CrystalBonus : 1f;
		int amount = Mathf.Max(1, Mathf.RoundToInt(baseAmount * mul));
		Data.crystals += amount;
		Data.totalCrystalsEarned += amount;
		MarkDirty();
		return amount;
	}

	public static bool SpendCrystals(int amount)
	{
		if (Data.crystals < amount) return false;
		Data.crystals -= amount;
		MarkDirty();
		return true;
	}

	// ---- 영구 강화 ----
	public static int GetUpgradeLevel(string id)
	{
		foreach (var u in Data.upgrades) if (u.id == id) return u.level;
		return 0;
	}

	public static void SetUpgradeLevel(string id, int level)
	{
		foreach (var u in Data.upgrades)
		{
			if (u.id == id) { u.level = level; MarkDirty(); return; }
		}
		Data.upgrades.Add(new NRUpgradeLevel { id = id, level = level });
		MarkDirty();
	}
}
