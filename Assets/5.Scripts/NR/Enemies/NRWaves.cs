using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// ============================================================================
// 방 전투 웨이브: 계층/방 번호에 따라 적 조합 결정, 스폰 예고, 방 클리어 판정
//  - 기존 기본 적(Enemy2)은 사용하지 않음 → 거미 떼 / 파수꾼 등 신규 적으로 구성
//  - 기존 스폰 위치 규칙(EnemySpawnPos[room*5+i])을 그대로 사용
// ============================================================================
public static class NRWaves
{
	class Entry
	{
		public bool legacy;
		public NREnemyKind kind;
	}

	static readonly List<GameObject> alive = new List<GameObject>();
	static readonly List<List<Entry>> waves = new List<List<Entry>>();
	static int waveIndex;
	static int pendingSpawns;
	static bool roomActive;
	static bool cleared;
	static int generation;
	static PrefabSpawner spawner;
	static int roomIndex;

	public static int AliveCount { get { Prune(); return alive.Count; } }
	public static int TotalThisRoom { get; private set; }
	public static int KilledThisRoom { get; private set; }
	public static bool RoomActive => roomActive;
	public static IReadOnlyList<GameObject> Alive { get { Prune(); return alive; } }

	static float Cost(Entry e)
	{
		if (e.legacy) return 1f;
		switch (e.kind)
		{
			case NREnemyKind.Spider: return 0.5f;
			case NREnemyKind.Bomber: return 1.25f;
			case NREnemyKind.Archer: return 1.5f;
			case NREnemyKind.Assassin: return 1.75f;
			case NREnemyKind.Mage: return 2f;
			case NREnemyKind.Warden: return 2.5f;
			default: return 2.5f;
		}
	}

	public static void BeginRoom(PrefabSpawner sp, int room)
	{
		Cancel();
		spawner = sp;
		roomIndex = room;
		roomActive = true;
		cleared = false;
		waveIndex = 0;
		KilledThisRoom = 0;
		generation++;

		int floor = NRRun.Floor;
		int roomNumber = Mathf.Clamp(Player.gameRound - 1, 1, 6);
		BuildWaves(floor, roomNumber);
		TotalThisRoom = waves.Sum(w => w.Count);
		NRGame.Run(RoomRoutine(generation));
	}

	static void BuildWaves(int floor, int roomNumber)
	{
		waves.Clear();
		float budget = 4f + floor * 1.5f + roomNumber * 0.75f;
		int waveCount = floor == 1 ? (roomNumber >= 3 ? 2 : 1) : floor == 2 ? 2 : (roomNumber >= 3 ? 3 : 2);

		// 방 번호가 오를수록 새 적이 합류 (기존 기본 적은 사용하지 않음)
		var unlocked = new List<NREnemyKind> { NREnemyKind.Archer, NREnemyKind.Spider };
		if (floor >= 2 || roomNumber >= 2) { unlocked.Add(NREnemyKind.Bomber); unlocked.Add(NREnemyKind.Warden); }
		if (floor >= 2 || roomNumber >= 3) unlocked.Add(NREnemyKind.Guardian);
		if (floor >= 2) { unlocked.Add(NREnemyKind.Assassin); unlocked.Add(NREnemyKind.Mage); }
		int maxHeavy = floor >= 3 ? 2 : 1;   // 수호자 + 파수꾼 합계

		var all = new List<Entry>();
		// 첫 방은 거미 떼 + 궁수로 가볍게 시작
		AddSpiders(all);
		float remaining = budget - Cost(new Entry { kind = NREnemyKind.Spider }) * 3;
		int heavy = 0;
		int safety = 50;
		while (remaining >= 0.75f && safety-- > 0)
		{
			var kind = unlocked[Random.Range(0, unlocked.Count)];
			bool isHeavy = kind == NREnemyKind.Guardian || kind == NREnemyKind.Warden;
			if (isHeavy && heavy >= maxHeavy) kind = NREnemyKind.Archer;
			var e = new Entry { kind = kind };
			float c = kind == NREnemyKind.Spider ? Cost(e) * 3 : Cost(e);
			if (c > remaining + 0.3f)
			{
				kind = NREnemyKind.Spider;
				e = new Entry { kind = kind };
				c = Cost(e) * 3;
			}
			if (kind == NREnemyKind.Spider) AddSpiders(all);
			else
			{
				all.Add(e);
				if (kind == NREnemyKind.Guardian || kind == NREnemyKind.Warden) heavy++;
			}
			remaining -= c;
		}

		// 섞어서 웨이브로 분배 (거미 떼는 같은 웨이브에 몰리도록 묶음 단위로 섞음)
		var groups = new List<List<Entry>>();
		for (int i = 0; i < all.Count; i++)
		{
			if (all[i].kind == NREnemyKind.Spider && i + 2 < all.Count && all[i + 1].kind == NREnemyKind.Spider && all[i + 2].kind == NREnemyKind.Spider)
			{
				groups.Add(new List<Entry> { all[i], all[i + 1], all[i + 2] });
				i += 2;
			}
			else groups.Add(new List<Entry> { all[i] });
		}
		groups = groups.OrderBy(_ => Random.value).ToList();
		for (int i = 0; i < waveCount; i++) waves.Add(new List<Entry>());
		for (int i = 0; i < groups.Count; i++) waves[i % waveCount].AddRange(groups[i]);
	}

	static void AddSpiders(List<Entry> list)
	{
		for (int i = 0; i < 3; i++) list.Add(new Entry { kind = NREnemyKind.Spider });
	}

	static IEnumerator RoomRoutine(int gen)
	{
		while (roomActive && gen == generation)
		{
			if (waveIndex < waves.Count)
			{
				yield return SpawnWave(waves[waveIndex], gen);
				waveIndex++;
			}

			// 다음 웨이브: 남은 적이 적어지면
			while (roomActive && gen == generation)
			{
				Prune();
				bool lastWave = waveIndex >= waves.Count;
				if (pendingSpawns == 0 && alive.Count == 0) break;
				if (!lastWave && pendingSpawns == 0 && alive.Count <= 1) break;
				yield return new WaitForSeconds(0.25f);
			}
			if (!roomActive || gen != generation) yield break;

			if (waveIndex >= waves.Count)
			{
				Prune();
				if (alive.Count == 0 && pendingSpawns == 0)
				{
					cleared = true;
					roomActive = false;
					yield break;
				}
			}
			else
			{
				NRUIRoot.ToastMsg("적의 증원이 몰려온다!", NRPalette.Crimson, 1.6f);
				yield return new WaitForSeconds(0.8f);
			}
		}
	}

	static IEnumerator SpawnWave(List<Entry> wave, int gen)
	{
		for (int i = 0; i < wave.Count; i++)
		{
			if (!roomActive || gen != generation) yield break;
			Vector3 pos = PickSpawnPoint(i);
			NRGame.Run(SpawnOne(wave[i], pos, gen));
			yield return new WaitForSeconds(0.12f);
		}
	}

	static Vector3 PickSpawnPoint(int i)
	{
		Vector3 basePos = spawner != null ? spawner.transform.position : Vector3.zero;
		if (spawner != null && spawner.EnemySpawnPos != null && spawner.EnemySpawnPos.Length > 0)
		{
			int baseIndex = roomIndex * 5;
			int idx = Mathf.Clamp(baseIndex + (i % 5), 0, spawner.EnemySpawnPos.Length - 1);
			if (spawner.EnemySpawnPos[idx] != null) basePos = spawner.EnemySpawnPos[idx].position + new Vector3(spawner.spawnOffset, 0, 0);
		}
		if (i < 5 && IsFree(basePos)) return basePos;
		for (int attempt = 0; attempt < 12; attempt++)
		{
			Vector3 cand = basePos + (Vector3)(Random.insideUnitCircle * 1.6f);
			if (IsFree(cand)) return cand;
		}
		return basePos;
	}

	/// <summary>벽(비트리거 콜라이더)과 겹치지 않는 위치인지</summary>
	public static bool IsFree(Vector2 pos)
	{
		foreach (var c in Physics2D.OverlapCircleAll(pos, 0.3f))
		{
			if (c.isTrigger) continue;
			if (c.gameObject.layer == 0 || c.gameObject.layer == 8) return false;
		}
		return true;
	}

	static IEnumerator SpawnOne(Entry e, Vector3 pos, int gen)
	{
		pendingSpawns++;
		NRTelegraph.Circle(pos, 0.7f, 0.75f, NRPalette.Pink);
		yield return new WaitForSeconds(0.75f);
		pendingSpawns--;
		if (!roomActive || gen != generation) yield break;
		var player = NRStats.Player;
		if (player != null && player.EnmeyDown) yield break;

		var def = NRRun.Current;
		GameObject go = null;
		if (e.legacy)
		{
			if (spawner != null && spawner.prefab != null)
			{
				go = Object.Instantiate(spawner.prefab, pos, Quaternion.identity);
				var enemy = go.GetComponent<Enemy>();
				if (enemy != null) enemy.NRScale(def.enemyHp, def.enemyAtk);
			}
		}
		else
		{
			var ne = NREnemyFactory.Create(e.kind, pos, def.enemyHp, def.enemyAtk);
			go = ne.gameObject;
		}
		if (go != null)
		{
			alive.Add(go);
			NRCombatFX.DeathBurst(pos, NRPalette.Pink, 10);
		}
	}

	public static void Register(GameObject go)
	{
		if (go != null && !alive.Contains(go)) alive.Add(go);
	}

	public static void NotifyDeath(GameObject go)
	{
		if (alive.Remove(go)) KilledThisRoom++;
	}

	static void Prune()
	{
		for (int i = alive.Count - 1; i >= 0; i--)
		{
			var go = alive[i];
			if (go == null) { alive.RemoveAt(i); KilledThisRoom++; continue; }
			var ne = go.GetComponent<NREnemy>();
			if (ne != null && ne.IsDead) { alive.RemoveAt(i); KilledThisRoom++; continue; }
			var le = go.GetComponent<Enemy>();
			if (le != null && le.IsDead) { alive.RemoveAt(i); KilledThisRoom++; }
		}
	}

	/// <summary>방 클리어 1회 확인 (PrefabSpawner.Update에서 호출)</summary>
	public static bool ConsumeRoomCleared()
	{
		if (!cleared) return false;
		cleared = false;
		return true;
	}

	public static void Cancel()
	{
		generation++;
		roomActive = false;
		cleared = false;
		pendingSpawns = 0;
		foreach (var go in alive.ToList())
		{
			if (go == null) continue;
			var ne = go.GetComponent<NREnemy>();
			if (ne != null) ne.Despawn();
		}
		alive.Clear();
	}

	/// <summary>개발자: 현재 방 적 전부 처치</summary>
	public static void KillAll()
	{
		foreach (var go in alive.ToList())
		{
			if (go == null) continue;
			var d = go.GetComponent<INRDamageable>();
			if (d != null && !d.IsDead) d.ReceiveDamage(999999, false, NRDamageKind.Direct);
		}
		foreach (var boss in Object.FindObjectsOfType<MonsterHP>())
		{
			((INRDamageable)boss).ReceiveDamage(999999, false, NRDamageKind.Direct);
		}
	}
}
