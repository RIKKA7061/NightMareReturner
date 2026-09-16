using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// ============================================================================
// 회차(런) / 계층 진행
//  1계층: 각성의 방 → 방 4개 → 상점 → 보스
//  2계층: 방 4개 → 상점 → 보스
//  3계층: 방 4개 → 상점 → 최종 보스 → 엔딩 크레딧 → 집에서 깨어남
// ============================================================================
public class NRFloorDef
{
	public int number;
	public string name;
	public string subtitle;
	public string bossName;
	public string bossTitle;
	public Color tint;
	public Color vignette;
	public float enemyHp;
	public float enemyAtk;
	public int bossHp;
	public float bossAtk;
	public int bossTier;
}

public static class NRRun
{
	public const int MaxFloor = 3;

	public static readonly NRFloorDef[] Floors =
	{
		new NRFloorDef { number = 1, name = "직장 내 괴롭힘", subtitle = "소외와 배제의 악몽", bossName = "침묵을 강요하는 자", bossTitle = "1계층 수문장",
			tint = new Color(1f, 1f, 1f), vignette = new Color(0.25f, 0.08f, 0.35f, 0.55f), enemyHp = 1f, enemyAtk = 1f, bossHp = 1400, bossAtk = 1f, bossTier = 1 },
		new NRFloorDef { number = 2, name = "과도한 업무 스트레스", subtitle = "끝나지 않는 밤의 악몽", bossName = "끝없는 야근의 망령", bossTitle = "2계층 수문장",
			tint = new Color(0.78f, 0.86f, 1.05f), vignette = new Color(0.05f, 0.12f, 0.35f, 0.6f), enemyHp = 1.65f, enemyAtk = 1.35f, bossHp = 2600, bossAtk = 1.35f, bossTier = 2 },
		new NRFloorDef { number = 3, name = "인정받지 못한 성과", subtitle = "악몽의 뿌리", bossName = "악몽의 뿌리", bossTitle = "최종 보스",
			tint = new Color(1.05f, 0.78f, 0.82f), vignette = new Color(0.4f, 0.04f, 0.1f, 0.65f), enemyHp = 2.35f, enemyAtk = 1.7f, bossHp = 4200, bossAtk = 1.7f, bossTier = 3 },
	};

	public static int Floor { get; private set; } = 1;
	public static bool InRun { get; private set; }
	public static int RunCrystals { get; private set; }
	public static int RoomsCleared { get; private set; }
	public static bool Transitioning { get; private set; }
	public static bool BossActive;
	public static NRFloorDef Current => Floors[Mathf.Clamp(Floor - 1, 0, Floors.Length - 1)];

	static readonly Dictionary<Tilemap, Color> originalTilemapColors = new Dictionary<Tilemap, Color>();
	static int lastAppliedTintFloor = -1;

	// ---------------------------------------------------------------------
	// 기존 코드에서 호출되는 훅
	// ---------------------------------------------------------------------

	/// <summary>Player.StatDefaultPlayer 끝 (게임 시작/부활/클리어 후 초기화)</summary>
	public static void OnRunReset(Player p)
	{
		InRun = false;
		Floor = 1;
		RunCrystals = 0;
		RoomsCleared = 0;
		BossActive = false;
		Transitioning = false;
		NRAugments.ResetRun();
		NRStats.ResetRun(p);
		NRWaves.Cancel();
		RestockShop();
		originalTilemapColors.Clear();
		lastAppliedTintFloor = -1;
		NRAudio.StopMusic();
	}

	/// <summary>Player.NowPosAnnounce 에서 호출 (gameRound 변경 시)</summary>
	public static void OnPositionAnnounced(Player p)
	{
		int round = Player.gameRound;
		if (round == 0)
		{
			ApplyFloorLook(false);
			return;
		}

		if (!InRun) BeginRun(p);
		ApplyFloorLook(true);

		// 방 입장 보상 (보호막)
		if (NRStats.RoomShield > 0f) NRStats.AddShield(NRStats.RoomShield);
	}

	static void BeginRun(Player p)
	{
		InRun = true;
		Floor = 1;
		RunCrystals = 0;
		RoomsCleared = 0;
		NRAugments.RerollsLeft = NRMeta.Rerolls;
		NRSave.Data.runs++;
		NRSave.MarkDirty();
		if (NRMeta.StartMoney > 0) Player.Money += NRMeta.StartMoney;
		ShowFloorBanner();
	}

	public static void ShowFloorBanner()
	{
		var def = Current;
		NRUIRoot.Banner(def.number + "계층 · " + def.name, def.subtitle, NRPalette.Text, 2.0f);
	}

	/// <summary>방의 적을 모두 처치했을 때 (PrefabSpawner)</summary>
	public static void OnRoomCleared()
	{
		RoomsCleared++;
		int amount = NRSave.AddCrystals(2 + Floor * 2);
		RunCrystals += amount;
		NRUIRoot.ToastMsg("방 정리 완료  ◈ 악몽 결정 +" + amount, NRPalette.Cyan);
		NRAudio.PlaySfx("crystal", 0.7f);
		NRHud.PulseCrystals();
	}

	/// <summary>보스 소환 직후 (GoHomeManager.SpawnNewBoss)</summary>
	public static void ConfigureBoss(GameObject boss)
	{
		if (boss == null) return;
		var def = Current;
		var hp = boss.GetComponent<MonsterHP>();
		if (hp != null)
		{
			hp.maxHP = def.bossHp;
			hp.nowHP = def.bossHp;
		}
		var ai = boss.GetComponent<MonsterAI>();
		if (ai != null) ai.Configure(def);
		var sr = boss.GetComponent<SpriteRenderer>();
		if (sr != null && def.number > 1) sr.color = Color.Lerp(Color.white, def.vignette, 0.35f).WithAlpha(1f);
		BossActive = true;
		QuestManager.Boss(def.bossName);
	}

	/// <summary>보스 처치 (MonsterHP)</summary>
	public static void OnBossDefeated(Vector3 position)
	{
		if (Transitioning) return;
		BossActive = false;
		NRGame.Run(BossDefeatedRoutine(position));
	}

	static IEnumerator BossDefeatedRoutine(Vector3 position)
	{
		Transitioning = true;
		var def = Current;
		NRStats.InvulnUntil = Time.time + 999f;
		NRStats.ClearEnemyProjectiles();
		NRWaves.Cancel();
		NRCombatFX.DeathBurst(position, NRPalette.Gold, 50);
		NRCombatFX.Shake(0.6f, 0.35f);
		NRTime.SlowMotion(1.4f, 0.25f);
		NRAudio.StopMusic();

		int amount = NRSave.AddCrystals(20 * Floor);
		RunCrystals += amount;
		NRSave.Data.bossKills++;
		NRSave.Data.bestFloor = Mathf.Max(NRSave.Data.bestFloor, Floor);
		NRSave.MarkDirty();
		NRSave.Save();

		NRUIRoot.Banner(def.bossName + " 격파", "◈ 악몽 결정 +" + amount, NRPalette.Gold, 2.2f);
		yield return new WaitForSecondsRealtime(3.0f);

		var player = NRStats.Player;
		if (Floor < MaxFloor)
		{
			// 보스 보상: 증강 선택 (희귀도 상승)
			QuestManager.FloorClear(Floor);
			bool picked = false;
			NRAugmentSelect.Show(null, true, "수문장의 잔향", () => picked = true);
			while (!picked) yield return null;

			yield return NRUIRoot.Instance.Fade(1f, 0.6f);
			AdvanceFloor(player);
			yield return new WaitForSecondsRealtime(0.6f);
			yield return NRUIRoot.Instance.Fade(0f, 0.6f);
			NRStats.InvulnUntil = Time.time + 1.5f;
			ShowFloorBanner();
		}
		else
		{
			NRSave.Data.clears++;
			int bonus = NRSave.AddCrystals(60);
			RunCrystals += bonus;
			NRSave.Save();
			bool done = false;
			NRCredits.Play(true, () => done = true);
			while (!done) yield return null;
			EndRunVictory(player);
		}
		Transitioning = false;
	}

	static void AdvanceFloor(Player player)
	{
		var spawner = Object.FindObjectOfType<PrefabSpawner>();
		var rooms = Object.FindObjectOfType<RoomGenerator>();
		if (spawner != null)
		{
			spawner.DestroySpawnedObjects();
			spawner.RoomEnemyCount = 0;
			spawner.isSpawnned = false;
		}
		if (rooms != null) rooms.DestroyDoor();
		RestockShop();

		Floor = Mathf.Min(MaxFloor, Floor + 1);
		if (NRStats.LastStandPct > 0f) NRStats.LastStandLeft = 1;

		if (player != null)
		{
			NRStats.HealPlayer(player.maxHP * NRMeta.FloorHealPct, true);
			if (spawner != null && spawner.Pos != null && spawner.Pos.Length > 0 && spawner.Pos[0] != null)
			{
				player.transform.position = spawner.Pos[0].position;
			}
			Player.gameRound = 2;
			player.NowPosAnnounce();
		}
	}

	static void EndRunVictory(Player player)
	{
		NRGame.Run(VictoryRoutine(player));
	}

	static IEnumerator VictoryRoutine(Player player)
	{
		if (NRUIRoot.Instance != null && !NRUIRoot.Instance.IsFaded) yield return NRUIRoot.Instance.Fade(1f, 0.5f);
		if (player != null) player.RespawnPlayer();
		yield return new WaitForSecondsRealtime(0.5f);
		if (NRUIRoot.Instance != null) yield return NRUIRoot.Instance.Fade(0f, 1.0f);
		NRUIRoot.Banner("악몽에서 깨어났다", "죽은 횟수 " + Player.DeadCount + " · 클리어 " + NRSave.Data.clears + "회", NRPalette.Cyan, 2.6f);
	}

	/// <summary>Player.Dead (DeadCount 증가 직후)</summary>
	public static void OnPlayerDied(Player p)
	{
		NRWaves.Cancel();
		BossActive = false;
		NRAudio.StopMusic();
		NRSave.Data.bestFloor = Mathf.Max(NRSave.Data.bestFloor, InRun ? Floor : 0);
		NRSave.MarkDirty();
		NRSave.Save();
		NRDeathScreen.Show(p);
	}

	// ---------------------------------------------------------------------
	// 계층 분위기 (타일맵 색조 + 비네트)
	// ---------------------------------------------------------------------
	static void ApplyFloorLook(bool inDungeon)
	{
		int key = inDungeon ? Floor : 0;
		if (key == lastAppliedTintFloor) return;
		lastAppliedTintFloor = key;

		Color tint = inDungeon ? Current.tint : Color.white;
		foreach (var tm in Object.FindObjectsOfType<Tilemap>())
		{
			if (!originalTilemapColors.ContainsKey(tm)) originalTilemapColors[tm] = tm.color;
			var o = originalTilemapColors[tm];
			tm.color = new Color(o.r * tint.r, o.g * tint.g, o.b * tint.b, o.a);
		}
		NRHud.SetVignette(inDungeon ? Current.vignette : new Color(0.1f, 0.05f, 0.2f, 0.3f));
	}

	public static void RestockShop()
	{
		foreach (var shop in Object.FindObjectsOfType<ShopManager>(true)) shop.Restock();
	}

	public static string LocationLabel()
	{
		var scene = NRGame.Instance != null ? NRGame.Instance.CurrentScene : "";
		if (scene == NRGame.SceneTown) return "거리";
		int round = Player.gameRound;
		var spawner = Object.FindObjectOfType<PrefabSpawner>();
		int over = spawner != null ? spawner.OverRoom : 5;
		if (round == 0) return "집";
		string floor = Floor + "계층";
		if (round == 1) return floor + " · 각성의 방";
		if (round <= over) return floor + " · " + (round - 1) + "번째 방";
		if (round == over + 1) return floor + " · 상점";
		return floor + " · 수문장의 방";
	}
}
