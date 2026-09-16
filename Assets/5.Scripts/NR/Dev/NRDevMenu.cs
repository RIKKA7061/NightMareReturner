#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

// ============================================================================
// 개발자 테스트 메뉴 — 에디터 / Development Build 에서만 컴파일됨 (출시 빌드에 포함되지 않음)
//  F1: 메뉴 열기/닫기   F2: 배속 전환   F3: 방의 적 전부 처치   F4: 무적
//  F5: 다음 방 강제 클리어   F6: 증강 선택 열기   F7: 악몽 결정 +100
// ============================================================================
public class NRDevMenu : MonoBehaviour
{
	bool open;
	Rect window = new Rect(20, 20, 460, 720);
	Vector2 scroll;
	static readonly float[] speeds = { 1f, 2f, 4f, 8f, 0.5f };
	int speedIndex;
	GUIStyle label;

	void Update()
	{
		if (NRInput.KeyDown(UnityEngine.InputSystem.Key.F1)) open = !open;
		if (NRInput.KeyDown(UnityEngine.InputSystem.Key.F2)) CycleSpeed();
		if (NRInput.KeyDown(UnityEngine.InputSystem.Key.F3)) NRWaves.KillAll();
		if (NRInput.KeyDown(UnityEngine.InputSystem.Key.F4)) ToggleGod();
		if (NRInput.KeyDown(UnityEngine.InputSystem.Key.F5)) ClearRoom();
		if (NRInput.KeyDown(UnityEngine.InputSystem.Key.F6)) NRAugmentSelect.Show(null, false, "개발자 증강", null);
		if (NRInput.KeyDown(UnityEngine.InputSystem.Key.F7)) { NRSave.AddCrystals(100, false); NRUIRoot.ToastMsg("[DEV] 악몽 결정 +100"); }
	}

	void CycleSpeed()
	{
		speedIndex = (speedIndex + 1) % speeds.Length;
		NRTime.SetDevScale(speeds[speedIndex]);
		NRUIRoot.ToastMsg("[DEV] 게임 속도 x" + speeds[speedIndex], NRPalette.Gold, 1.2f);
	}

	void ToggleGod()
	{
		NRStats.GodMode = !NRStats.GodMode;
		NRUIRoot.ToastMsg("[DEV] 무적 " + (NRStats.GodMode ? "켬" : "끔"), NRPalette.Gold, 1.2f);
	}

	void ClearRoom()
	{
		NRWaves.KillAll();
	}

	void OnGUI()
	{
		// 항상 표시되는 작은 배지
		var badge = new Rect(Screen.width - 250, Screen.height - 34, 240, 26);
		GUI.color = new Color(1f, 0.8f, 0.3f, 0.8f);
		GUI.Label(badge, "DEV  F1 메뉴  x" + NRTime.DevScale + (NRStats.GodMode ? "  무적" : ""));
		GUI.color = Color.white;
		if (!open) return;
		window = GUI.Window(98765, window, DrawWindow, "악몽 회귀자 개발자 메뉴");
	}

	void DrawWindow(int id)
	{
		scroll = GUILayout.BeginScrollView(scroll);
		var player = FindObjectOfType<Player>();

		GUILayout.Label("씬: " + SceneManager.GetActiveScene().name + "   위치: " + NRRun.LocationLabel());
		GUILayout.Label("gameRound " + Player.gameRound + " · 계층 " + NRRun.Floor + " · 남은 적 " + NRWaves.AliveCount);

		GUILayout.Space(6);
		GUILayout.Label("■ 시간");
		GUILayout.BeginHorizontal();
		foreach (var s in speeds)
			if (GUILayout.Button("x" + s)) { NRTime.SetDevScale(s); speedIndex = System.Array.IndexOf(speeds, s); }
		GUILayout.EndHorizontal();

		GUILayout.Label("■ 전투");
		GUILayout.BeginHorizontal();
		if (GUILayout.Button(NRStats.GodMode ? "무적 끄기" : "무적 켜기")) ToggleGod();
		if (GUILayout.Button(NRStats.OneHitKill ? "한방 끄기" : "한방 켜기")) NRStats.OneHitKill = !NRStats.OneHitKill;
		if (GUILayout.Button("적 전부 처치")) NRWaves.KillAll();
		GUILayout.EndHorizontal();
		if (player != null)
		{
			GUILayout.BeginHorizontal();
			if (GUILayout.Button("체력 가득")) player.nowHP = player.maxHP;
			if (GUILayout.Button("공격력 +50")) player.Atk += 50;
			if (GUILayout.Button("자결")) player.Dead();
			GUILayout.EndHorizontal();
		}

		GUILayout.Label("■ 이동 (던전)");
		var spawner = FindObjectOfType<PrefabSpawner>();
		if (player != null && spawner != null)
		{
			GUILayout.BeginHorizontal();
			if (GUILayout.Button("집")) { Player.gameRound = 0; player.transform.position = player.Home.position; player.NowPosAnnounce(); }
			for (int i = 0; i < spawner.Pos.Length && i < 5; i++)
			{
				int idx = i;
				if (GUILayout.Button("방" + idx)) { spawner.isSpawnned = false; Player.gameRound = Mathf.Max(2, Player.gameRound); player.transform.position = spawner.Pos[idx].position; player.NowPosAnnounce(); }
			}
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal();
			foreach (var tp in FindObjectsOfType<teleport>().Where(t => t.name.ToLower().Contains("store") || t.isBossTP || t.name.ToLower().Contains("boss")).Take(3))
			{
				var target = tp;
				if (GUILayout.Button(target.name)) { player.transform.position = target.Pos[0].position; Player.gameRound = target.isBossTP || target.name.ToLower().Contains("boss") ? spawner.OverRoom + 1 : spawner.OverRoom; player.NowPosAnnounce(); }
			}
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal();
			for (int f = 1; f <= NRRun.MaxFloor; f++)
			{
				int floor = f;
				if (GUILayout.Button(floor + "계층으로")) DevSetFloor(floor);
			}
			GUILayout.EndHorizontal();
			if (GUILayout.Button("보스 즉시 소환")) { var gh = FindObjectOfType<GoHomeManager>(); Player.gameRound = spawner.OverRoom + 2; if (gh != null) { gh.bossSpawnPoint.position = player.transform.position + Vector3.right * 4f; gh.SpawnNewBoss(); } }
			if (GUILayout.Button("엔딩 크레딧 재생")) NRCredits.Play(true, () => NRUIRoot.Instance.Fade(0f, 0.5f));
		}

		GUILayout.Label("■ 증강");
		GUILayout.BeginHorizontal();
		if (GUILayout.Button("선택 열기")) NRAugmentSelect.Show(null, false, "개발자 증강", null);
		if (GUILayout.Button("보스 보상")) NRAugmentSelect.Show(null, true, "개발자 보스 보상", null);
		if (GUILayout.Button("무작위 +1")) GiveRandomAugment();
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();
		foreach (NRFamily fam in new[] { NRFamily.Rage, NRFamily.Anxiety, NRFamily.Sorrow, NRFamily.Will })
		{
			var f = fam;
			if (GUILayout.Button(NRAugments.FamilyName(f))) NRAugmentSelect.Show(f, false, "개발자 증강", null);
		}
		GUILayout.EndHorizontal();

		GUILayout.Label("■ 자원 / 저장");
		GUILayout.BeginHorizontal();
		if (GUILayout.Button("재화 +100")) Player.Money += 100;
		if (GUILayout.Button("구슬 +1")) Player.round += 1;
		if (GUILayout.Button("결정 +100")) NRSave.AddCrystals(100, false);
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();
		if (GUILayout.Button("죽은 횟수 +1")) Player.DeadCount++;
		if (GUILayout.Button("죽은 횟수 -1")) Player.DeadCount = Mathf.Max(0, Player.DeadCount - 1);
		if (GUILayout.Button("대화 초기화"))
		{
			for (int i = 0; i < _Object.EachTalkCountSaveNum.Length; i++) _Object.EachTalkCountSaveNum[i] = 0;
			foreach (var npc in FindObjectsOfType<_Object>()) npc.ResetEachTalkCountSave();
		}
		GUILayout.EndHorizontal();
		GUILayout.BeginHorizontal();
		if (GUILayout.Button("지금 저장")) NRSave.Save();
		if (GUILayout.Button("전체 진행 초기화")) NRSave.ResetProgress();
		GUILayout.EndHorizontal();

		GUILayout.Label("■ 씬");
		GUILayout.BeginHorizontal();
		if (GUILayout.Button("메인메뉴")) NRSceneFlow.LoadScene(NRGame.SceneMainMenu);
		if (GUILayout.Button("던전(집)")) NRSceneFlow.LoadScene(NRGame.SceneDungeon);
		if (GUILayout.Button("거리")) NRSceneFlow.LoadScene(NRGame.SceneTown);
		GUILayout.EndHorizontal();

		GUILayout.Space(6);
		GUILayout.Label("단축키: F1 메뉴 · F2 배속 · F3 적 처치 · F4 무적 · F6 증강 · F7 결정");
		GUILayout.EndScrollView();
		GUI.DragWindow(new Rect(0, 0, 10000, 24));
	}

	static void GiveRandomAugment()
	{
		var offers = NRAugments.GenerateOffers(null, 1, false);
		if (offers.Count > 0)
		{
			NRAugments.Acquire(offers[0]);
			NRUIRoot.ToastMsg("[DEV] " + offers[0].def.name, NRAugments.FamilyColor(offers[0].def.family));
		}
	}

	static void DevSetFloor(int floor)
	{
		var field = typeof(NRRun).GetProperty("Floor");
		var setter = field?.GetSetMethod(true);
		setter?.Invoke(null, new object[] { floor });
		NRRun.ShowFloorBanner();
	}
}
#endif
