using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

// ============================================================================
// 2·3계층 전용 맵
//  씬 파일은 건드리지 않고, 계층 이동 시 런타임으로 새 테마 방을 만들고
//  기존 방 좌표(입장/적 소환/보상/문/상점/보스)를 새 방으로 옮긴다.
//   2계층: 얼음 동굴 (타원형 방, 얼음 기둥, 깨지는 빙결 수정)
//   3계층: 불타는 신전 (모서리가 깎인 방, 용암 웅덩이)
//  회차가 초기화되면(집에서 깨어남) 모든 좌표를 원래대로 되돌린다.
// ============================================================================
public static class NRFloorMap
{
	class Group
	{
		public Transform anchor;
		public readonly List<Transform> members = new List<Transform>();
		public readonly List<Vector2> extraOffsets = new List<Vector2>();   // 반드시 바닥이어야 하는 추가 지점 (상점 TP 등)
	}

	static readonly Dictionary<Transform, Vector3> original = new Dictionary<Transform, Vector3>();
	static GameObject root;
	public static int ActiveFloor { get; private set; } = 1;

	// ---------------------------------------------------------------------
	public static void Enter(int floor)
	{
		if (floor <= 1) { Restore(); return; }
		var spawner = Object.FindObjectOfType<PrefabSpawner>();
		var ground = FindTilemap("ground");
		if (spawner == null || spawner.Pos == null || spawner.Pos.Length == 0 || ground == null)
		{
			Debug.LogWarning("[NRFloorMap] 맵 생성에 필요한 오브젝트를 찾지 못해 기존 맵을 사용합니다.");
			return;
		}

		RestorePositions();
		if (root != null) Object.Destroy(root);

		var theme = NRTileTheme.Load(floor == 2 ? "ice" : "ash");
		if (theme == null) { Debug.LogWarning("[NRFloorMap] 타일 테마 없음"); return; }

		var groups = CollectGroups(spawner);
		if (groups.Count == 0) return;

		// 모든 이동 대상의 원래 위치 기록
		foreach (var g in groups)
		{
			Remember(g.anchor);
			foreach (var m in g.members) Remember(m);
		}

		root = new GameObject("NR Map Floor" + floor);
		var builder = new NRMapBuilder(root.transform, ground, FindTilemap("Wall"), theme, floor);

		var moves = new List<(Transform t, Vector3 pos)>();
		Vector3 cursor = original[groups[0].anchor] + new Vector3(700f * (floor - 1), 0f, 0f);
		var rng = new System.Random(floor * 7919 + NRSave.Data.runs);

		foreach (var g in groups)
		{
			Vector3 oldAnchor = original[g.anchor];
			var offsets = new List<Vector2>();
			var targets = new List<(Transform t, Vector2 off)>();
			foreach (var m in g.members)
			{
				Vector2 off = Remap((Vector2)(original[m] - oldAnchor), floor);
				targets.Add((m, off));
				offsets.Add(off);
			}
			foreach (var e in g.extraOffsets) offsets.Add(Remap(e, floor));
			offsets.Add(Vector2.zero);

			float minX = offsets.Min(o => o.x), maxX = offsets.Max(o => o.x);
			Vector3 anchorNew = cursor + new Vector3(-minX + 6f, 0f, 0f);
			cursor = anchorNew + new Vector3(maxX + 18f, 0f, 0f);

			moves.Add((g.anchor, anchorNew));
			foreach (var (t, off) in targets) moves.Add((t, anchorNew + (Vector3)off));

			builder.BuildRoom(anchorNew, offsets, rng);
		}
		builder.Finish();

		// 부모가 먼저 이동하도록 계층 깊이 순으로 적용
		foreach (var mv in moves.OrderBy(m => Depth(m.t)))
		{
			if (mv.t == null) continue;
			mv.t.position = new Vector3(mv.pos.x, mv.pos.y, original[mv.t].z);
		}
		ActiveFloor = floor;
	}

	/// <summary>회차 초기화: 원래 맵으로</summary>
	public static void Restore()
	{
		if (ActiveFloor <= 1 && root == null && original.Count == 0) return;
		var spawner = Object.FindObjectOfType<PrefabSpawner>();
		if (spawner != null) spawner.DestroySpawnedObjects();
		var rooms = Object.FindObjectOfType<RoomGenerator>();
		if (rooms != null) rooms.DestroyDoor();
		RestorePositions();
		if (root != null) Object.Destroy(root);
		root = null;
		ActiveFloor = 1;
	}

	public static bool IsGenerated(Tilemap tm) => root != null && tm != null && tm.transform.IsChildOf(root.transform);

	// ---------------------------------------------------------------------
	static void RestorePositions()
	{
		foreach (var kv in original.OrderBy(k => Depth(k.Key)))
			if (kv.Key != null) kv.Key.position = kv.Value;
		original.Clear();
	}

	static void Remember(Transform t)
	{
		if (t != null && !original.ContainsKey(t)) original[t] = t.position;
	}

	static int Depth(Transform t)
	{
		int d = 0;
		while (t != null) { d++; t = t.parent; }
		return d;
	}

	/// <summary>계층마다 방 배치를 뒤집고 넓힌다 (같은 구성이라도 다른 동선)</summary>
	static Vector2 Remap(Vector2 off, int floor)
	{
		return floor == 2 ? new Vector2(-off.x, off.y) * 1.15f : new Vector2(off.x, -off.y) * 1.2f;
	}

	static Tilemap FindTilemap(string name)
	{
		foreach (var tm in Object.FindObjectsOfType<Tilemap>(true))
			if (tm.gameObject.name == name && tm.gameObject.scene.IsValid()) return tm;
		return null;
	}

	static List<Group> CollectGroups(PrefabSpawner spawner)
	{
		var groups = new List<Group>();
		var used = new HashSet<Transform>();
		var roomGen = Object.FindObjectOfType<RoomGenerator>();

		Group Make(Transform anchor)
		{
			if (anchor == null || used.Contains(anchor)) return null;
			used.Add(anchor);
			var g = new Group { anchor = anchor };
			groups.Add(g);
			return g;
		}
		void Add(Group g, Transform t)
		{
			if (g == null || t == null || used.Contains(t)) return;
			used.Add(t);
			g.members.Add(t);
		}

		for (int i = 0; i < spawner.Pos.Length; i++)
		{
			var g = Make(spawner.Pos[i]);
			if (g == null) continue;
			g.extraOffsets.Add(new Vector2(-1.5f, 0f)); // 상점 TP 생성 위치
			if (spawner.EnemySpawnPos != null)
				for (int k = i * 5; k < i * 5 + 5 && k < spawner.EnemySpawnPos.Length; k++)
				{
					var t = spawner.EnemySpawnPos[k];
					Add(g, t);
					if (t != null && spawner.Pos[i] != null)
						g.extraOffsets.Add((Vector2)(t.position - spawner.Pos[i].position) + new Vector2(spawner.spawnOffset, 0));
				}
			if (spawner.RewardItem_Pos != null && i < spawner.RewardItem_Pos.Length) Add(g, spawner.RewardItem_Pos[i]);
			if (roomGen != null && roomGen.Pos != null)
				for (int k = i * 4; k < i * 4 + 4 && k < roomGen.Pos.Length; k++) Add(g, roomGen.Pos[k]);
		}

		// 상점 방: 상점 TP가 보내는 위치 + 상인 + 보스방 TP
		Transform storeEntry = null;
		if (spawner.StorePos != null)
		{
			var tp = spawner.StorePos.GetComponent<teleport>();
			if (tp != null && tp.Pos != null && tp.Pos.Length > 0) storeEntry = tp.Pos[0];
		}
		var store = storeEntry != null ? (Make(storeEntry) ?? groups.FirstOrDefault(x => x.anchor == storeEntry)) : null;
		var bossTps = Object.FindObjectsOfType<teleport>(true).Where(t => t.isBossTP || t.gameObject.name == "ToTheBossRoomTP").ToList();
		foreach (var npc in Object.FindObjectsOfType<_Object>(true))
			if (npc.CompareTag("shop") && npc.gameObject.scene.IsValid()) Add(store ?? NearestGroup(groups, npc.transform), npc.transform);
		foreach (var tp in bossTps)
			if (tp.gameObject.scene.IsValid()) Add(store ?? NearestGroup(groups, tp.transform), tp.transform);

		// 보스 방
		var home = Object.FindObjectOfType<GoHomeManager>();
		foreach (var tp in bossTps)
		{
			if (tp.Pos == null || tp.Pos.Length == 0 || tp.Pos[0] == null) continue;
			var boss = Make(tp.Pos[0]) ?? groups.FirstOrDefault(x => x.anchor == tp.Pos[0]);
			if (boss != null && home != null && home.bossSpawnPoint != null) Add(boss, home.bossSpawnPoint);
			break;
		}
		return groups;
	}

	static Group NearestGroup(List<Group> groups, Transform t)
	{
		return groups.Where(g => g.anchor != null).OrderBy(g => Vector2.Distance(g.anchor.position, t.position)).FirstOrDefault();
	}
}

// ============================================================================
// 타일 테마 (Resources/NR/Tiles/<name>.png + .txt 목록)
// ============================================================================
public class NRTileTheme
{
	public readonly Dictionary<string, List<Sprite>> tiles = new Dictionary<string, List<Sprite>>();
	public readonly Dictionary<string, List<Sprite>> props = new Dictionary<string, List<Sprite>>();
	public string name;

	static readonly Dictionary<string, NRTileTheme> cache = new Dictionary<string, NRTileTheme>();

	public static NRTileTheme Load(string name)
	{
		if (cache.TryGetValue(name, out var c)) return c;
		var tex = Resources.Load<Texture2D>("NR/Tiles/" + name);
		var list = Resources.Load<TextAsset>("NR/Tiles/" + name);
		if (tex == null || list == null) return null;
		tex.filterMode = FilterMode.Point;
		var th = new NRTileTheme { name = name };
		foreach (var raw in list.text.Split('\n'))
		{
			var line = raw.Trim();
			if (line.Length == 0) continue;
			var p = line.Split(',');
			if (p.Length < 6) continue;
			string key = p[0];
			int x = int.Parse(p[2]), y = int.Parse(p[3]), w = int.Parse(p[4]), h = int.Parse(p[5]);
			var rect = new Rect(x, y, w, h);
			// 타일: 원본과 같은 중앙 피벗 / 소품: 발밑 피벗
			var tile = Sprite.Create(tex, rect, new Vector2(0.5f, 0.5f), 64f, 0, SpriteMeshType.FullRect);
			var prop = Sprite.Create(tex, rect, new Vector2(0.5f, 0.08f), 64f, 0, SpriteMeshType.FullRect);
			if (!th.tiles.ContainsKey(key)) { th.tiles[key] = new List<Sprite>(); th.props[key] = new List<Sprite>(); }
			th.tiles[key].Add(tile);
			th.props[key].Add(prop);
		}
		cache[name] = th;
		return th;
	}

	public List<Sprite> Tiles(string key) => tiles.TryGetValue(key, out var l) ? l : new List<Sprite>();
	public List<Sprite> Props(string key) => props.TryGetValue(key, out var l) ? l : new List<Sprite>();
}

// ============================================================================
// 방 만들기
// ============================================================================
public class NRMapBuilder
{
	readonly Transform root;
	readonly Tilemap floorMap, wallMap, wallTopMap, overlayMap;
	readonly NRTileTheme theme;
	readonly int floor;
	readonly Grid grid;
	readonly Dictionary<Sprite, Tile> tileCache = new Dictionary<Sprite, Tile>();
	readonly HashSet<Vector3Int> floorCells = new HashSet<Vector3Int>();
	readonly HashSet<Vector3Int> blocked = new HashSet<Vector3Int>();
	readonly HashSet<Vector3Int> lava = new HashSet<Vector3Int>();
	static readonly Vector3 SurfaceOffset = new Vector3(0, 0.25f, 0);   // 블록 윗면 = 칸 중심 + 0.25

	public NRMapBuilder(Transform root, Tilemap groundRef, Tilemap wallRef, NRTileTheme theme, int floor)
	{
		this.root = root;
		this.theme = theme;
		this.floor = floor;
		grid = groundRef.layoutGrid;
		root.SetParent(grid.transform, false);

		floorMap = MakeLayer("NR Floor", groundRef, null, 0, Vector3.zero);
		wallMap = MakeLayer("NR Wall", wallRef != null ? wallRef : groundRef, null, 0, Vector3.zero);
		wallTopMap = MakeLayer("NR Wall Top", wallRef != null ? wallRef : groundRef, null, 1, new Vector3(0, 0.5f, 0));
		overlayMap = MakeLayer("NR Overlay", groundRef, null, 1, new Vector3(0, 0.5f, 0));

		// 벽 충돌: 칸 모양(마름모) + 윗면 높이 보정
		wallMap.gameObject.layer = 0;
		var rb = wallMap.gameObject.AddComponent<Rigidbody2D>();
		rb.bodyType = RigidbodyType2D.Static;
		var tc = wallMap.gameObject.AddComponent<TilemapCollider2D>();
		tc.usedByComposite = true;
		tc.offset = SurfaceOffset;
		var comp = wallMap.gameObject.AddComponent<CompositeCollider2D>();
		comp.geometryType = CompositeCollider2D.GeometryType.Polygons;
		comp.generationType = CompositeCollider2D.GenerationType.Synchronous;
	}

	Tilemap MakeLayer(string name, Tilemap reference, string layer, int orderAdd, Vector3 offset)
	{
		var go = new GameObject(name);
		go.transform.SetParent(root, false);
		go.transform.localPosition = reference.transform.localPosition + offset;
		go.transform.localRotation = reference.transform.localRotation;
		go.transform.localScale = reference.transform.localScale;
		var tm = go.AddComponent<Tilemap>();
		tm.tileAnchor = reference.tileAnchor;
		tm.orientation = reference.orientation;
		var r = go.AddComponent<TilemapRenderer>();
		var rr = reference.GetComponent<TilemapRenderer>();
		if (rr != null)
		{
			r.sortingLayerID = rr.sortingLayerID;
			r.sortingOrder = rr.sortingOrder + orderAdd;
			r.mode = rr.mode;
			r.sharedMaterial = rr.sharedMaterial;
		}
		return tm;
	}

	Tile TileOf(Sprite s, bool collide)
	{
		if (!tileCache.TryGetValue(s, out var t))
		{
			t = ScriptableObject.CreateInstance<Tile>();
			t.sprite = s;
			t.colliderType = collide ? Tile.ColliderType.Grid : Tile.ColliderType.None;
			tileCache[s] = t;
		}
		return t;
	}

	Vector3Int Cell(Vector3 world) => floorMap.WorldToCell(world - SurfaceOffset);
	Vector3 Surface(Vector3Int cell) => floorMap.GetCellCenterWorld(cell) + SurfaceOffset;

	static Sprite Pick(List<Sprite> list, System.Random rng) => list.Count == 0 ? null : list[rng.Next(list.Count)];

	// ---------------------------------------------------------------------
	public void BuildRoom(Vector3 anchor, List<Vector2> offsets, System.Random rng)
	{
		var roomCells = new HashSet<Vector3Int>();
		var keyCells = offsets.Select(o => Cell(anchor + (Vector3)o)).ToList();
		int minX = keyCells.Min(c => c.x), maxX = keyCells.Max(c => c.x);
		int minY = keyCells.Min(c => c.y), maxY = keyCells.Max(c => c.y);
		int margin = floor == 2 ? 5 : 6;
		minX -= margin; maxX += margin; minY -= margin; maxY += margin;
		float cx = (minX + maxX) * 0.5f, cy = (minY + maxY) * 0.5f;
		float rx = (maxX - minX) * 0.5f + 0.5f, ry = (maxY - minY) * 0.5f + 0.5f;

		for (int x = minX; x <= maxX; x++)
			for (int y = minY; y <= maxY; y++)
			{
				bool inside;
				if (floor == 2)
				{
					float nx = (x - cx) / rx, ny = (y - cy) / ry;
					inside = nx * nx + ny * ny <= 1.05f;
				}
				else
				{
					int k = Mathf.Max(2, margin - 1);
					bool nearX = x - minX < k || maxX - x < k;
					bool nearY = y - minY < k || maxY - y < k;
					inside = !(nearX && nearY);
				}
				if (inside) roomCells.Add(new Vector3Int(x, y, 0));
			}

		// 중요한 지점 주변 + 입구까지 이어지는 길은 반드시 바닥
		var anchorCell = Cell(anchor);
		foreach (var kc in keyCells)
		{
			for (int dx = -2; dx <= 2; dx++)
				for (int dy = -2; dy <= 2; dy++)
					roomCells.Add(new Vector3Int(kc.x + dx, kc.y + dy, 0));
			foreach (var c in Line(anchorCell, kc))
				for (int dx = -1; dx <= 1; dx++)
					for (int dy = -1; dy <= 1; dy++)
						roomCells.Add(new Vector3Int(c.x + dx, c.y + dy, 0));
		}

		foreach (var c in roomCells)
		{
			floorCells.Add(c);
			var list = theme.Tiles("floor");
			// 기본 타일 위주, 가끔 무늬 타일
			int idx = rng.NextDouble() < 0.8 ? rng.Next(Mathf.Min(4, list.Count)) : rng.Next(list.Count);
			floorMap.SetTile(c, TileOf(list[idx], false));
		}

		// 벽 (방 바깥 테두리)
		foreach (var c in roomCells)
			for (int dx = -1; dx <= 1; dx++)
				for (int dy = -1; dy <= 1; dy++)
				{
					var n = new Vector3Int(c.x + dx, c.y + dy, 0);
					if (roomCells.Contains(n) || floorCells.Contains(n)) continue;
					PlaceWall(n, rng);
				}

		var keyWorld = offsets.Select(o => (Vector2)(anchor + (Vector3)o)).ToList();
		bool FarFromKeys(Vector3Int cell, float dist)
		{
			Vector2 w = Surface(cell);
			foreach (var k in keyWorld)
			{
				if (Vector2.Distance(w, k) < dist) return false;
				if (DistToSegment(w, anchor, k) < dist * 0.55f) return false;
			}
			return true;
		}
		bool Interior(Vector3Int c, int r)
		{
			for (int dx = -r; dx <= r; dx++)
				for (int dy = -r; dy <= r; dy++)
					if (!roomCells.Contains(new Vector3Int(c.x + dx, c.y + dy, 0))) return false;
			return true;
		}

		var candidates = roomCells.OrderBy(_ => rng.Next()).ToList();

		// 기둥 (충돌) — 동선을 막지 않는 곳에 몇 개만
		int pillars = 0;
		foreach (var c in candidates)
		{
			if (pillars >= 3) break;
			if (!Interior(c, 2) || !FarFromKeys(c, 2.8f)) continue;
			if (blocked.Any(b => Mathf.Abs(b.x - c.x) + Mathf.Abs(b.y - c.y) < 6)) continue;
			floorMap.SetTile(c, null);
			PlaceWall(c, rng);
			pillars++;
		}

		if (floor == 3)
		{
			// 용암 웅덩이 (2x2 ~ 3x2)
			int pools = 0;
			foreach (var c in candidates)
			{
				if (pools >= 2) break;
				int w = 2 + rng.Next(2), h = 2;
				bool ok = true;
				for (int dx = 0; dx < w && ok; dx++)
					for (int dy = 0; dy < h && ok; dy++)
					{
						var cc = new Vector3Int(c.x + dx, c.y + dy, 0);
						if (!Interior(cc, 1) || !FarFromKeys(cc, 2.4f) || blocked.Contains(cc) || lava.Contains(cc)) ok = false;
					}
				if (!ok) continue;
				for (int dx = 0; dx < w; dx++)
					for (int dy = 0; dy < h; dy++)
						lava.Add(new Vector3Int(c.x + dx, c.y + dy, 0));
				pools++;
			}
		}
		else
		{
			// 빙결 수정: 부수면 재화와 약간의 체력
			int crystals = 0;
			foreach (var c in candidates)
			{
				if (crystals >= 2) break;
				if (!Interior(c, 1) || !FarFromKeys(c, 2.2f) || blocked.Contains(c)) continue;
				var crystalSprites = theme.Props("deco");
				NRBreakableCrystal.Create(Surface(c), crystalSprites.Count > 2 ? crystalSprites[2] : null, root);
				blocked.Add(c);
				crystals++;
			}
			// 얼음 판
			foreach (var c in candidates.Take(Mathf.Max(1, candidates.Count / 40)))
				if (FarFromKeys(c, 1.2f) && !blocked.Contains(c)) Prop(Surface(c), Pick(theme.Props("patch"), rng), "Attack", -5);
		}

		// 벽을 따라 장식 (충돌 없음)
		foreach (var c in candidates)
		{
			if (Interior(c, 1) || blocked.Contains(c) || lava.Contains(c)) continue;
			if (rng.NextDouble() > 0.22 || !FarFromKeys(c, 1.6f)) continue;
			Prop(Surface(c), Pick(theme.Props("deco"), rng), "Deco", 0);
		}

		// 방 중앙 상징물
		var centerCell = new Vector3Int(Mathf.RoundToInt(cx), Mathf.RoundToInt(cy), 0);
		if (roomCells.Contains(centerCell) && FarFromKeys(centerCell, 2.4f) && !blocked.Contains(centerCell) && !lava.Contains(centerCell))
			Prop(Surface(centerCell), Pick(theme.Props("center"), rng), "Deco", 0);
	}

	void PlaceWall(Vector3Int c, System.Random rng)
	{
		if (blocked.Contains(c)) return;
		blocked.Add(c);
		var s = Pick(theme.Tiles("wall"), rng);
		if (s != null) wallMap.SetTile(c, TileOf(s, true));
		var top = rng.NextDouble() < 0.35 ? Pick(theme.Tiles("top"), rng) : Pick(theme.Tiles("wall"), rng);
		if (top != null) wallTopMap.SetTile(c, TileOf(top, false));
	}

	void Prop(Vector3 pos, Sprite s, string layer, int orderBias)
	{
		if (s == null) return;
		var go = new GameObject("NR Prop");
		go.transform.SetParent(root, true);
		go.transform.position = new Vector3(pos.x, pos.y - 0.1f, 0);
		var sr = go.AddComponent<SpriteRenderer>();
		sr.sprite = s;
		NRSort.Set(sr, layer, Mathf.RoundToInt(-pos.y * 10f) + orderBias);
	}

	public void Finish()
	{
		wallMap.CompressBounds();
		var comp = wallMap.GetComponent<CompositeCollider2D>();
		if (comp != null) comp.GenerateGeometry();

		if (lava.Count > 0)
		{
			var frames = theme.Tiles("lava");
			var anim = root.gameObject.AddComponent<NRLavaField>();
			anim.Init(overlayMap, lava, frames, floorMap, SurfaceOffset);
		}
	}

	static IEnumerable<Vector3Int> Line(Vector3Int a, Vector3Int b)
	{
		int x0 = a.x, y0 = a.y, x1 = b.x, y1 = b.y;
		int dx = Mathf.Abs(x1 - x0), dy = -Mathf.Abs(y1 - y0);
		int sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1, err = dx + dy;
		for (int guard = 0; guard < 2000; guard++)
		{
			yield return new Vector3Int(x0, y0, 0);
			if (x0 == x1 && y0 == y1) yield break;
			int e2 = 2 * err;
			if (e2 >= dy) { err += dy; x0 += sx; }
			if (e2 <= dx) { err += dx; y0 += sy; }
		}
	}

	static float DistToSegment(Vector2 p, Vector2 a, Vector2 b)
	{
		Vector2 ab = b - a;
		float len = ab.sqrMagnitude;
		if (len < 0.0001f) return Vector2.Distance(p, a);
		float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / len);
		return Vector2.Distance(p, a + ab * t);
	}
}

// ============================================================================
// 용암: 애니메이션 + 밟으면 화상 (구르기로 건너면 피해 없음), 적도 피해
// ============================================================================
public class NRLavaField : MonoBehaviour
{
	Tilemap overlay, floorMap;
	HashSet<Vector3Int> cells;
	List<Sprite> frames;
	Tile tile;
	Vector3 surfaceOffset;
	int frame;
	float nextFrame, nextTick;
	static bool warned;

	public void Init(Tilemap overlay, HashSet<Vector3Int> cells, List<Sprite> frames, Tilemap floorMap, Vector3 surfaceOffset)
	{
		this.overlay = overlay;
		this.cells = new HashSet<Vector3Int>(cells);
		this.frames = frames.Take(Mathf.Max(1, frames.Count - 1)).ToList();
		this.floorMap = floorMap;
		this.surfaceOffset = surfaceOffset;
		tile = ScriptableObject.CreateInstance<Tile>();
		tile.sprite = this.frames.Count > 0 ? this.frames[0] : null;
		tile.color = new Color(1f, 0.55f, 0.35f);
		foreach (var c in cells) overlay.SetTile(c, tile);
	}

	bool OnLava(Vector3 world) => cells.Contains(floorMap.WorldToCell(world - surfaceOffset));

	void Update()
	{
		if (frames == null || frames.Count == 0) return;
		if (Time.time > nextFrame)
		{
			nextFrame = Time.time + 0.14f;
			frame = (frame + 1) % frames.Count;
			tile.sprite = frames[frame];
			overlay.RefreshAllTiles();
		}
		if (Time.time < nextTick) return;
		nextTick = Time.time + 0.4f;

		var p = NRStats.Player;
		if (p != null && !p.isDead && OnLava(p.transform.position))
		{
			if (!warned)
			{
				warned = true;
				NRUIRoot.ToastMsg("용암! 밟으면 화상을 입습니다 — 구르기로 건너면 피해가 없습니다", NRPalette.Rage, 3f);
			}
			if (NRStats.DamagePlayer(p, 9f + 3f * NRRun.Floor, false, true))
				NRCombatFX.Sparks(p.transform.position, NRPalette.Rage, 5);
		}
		foreach (var go in NRWaves.Alive.ToList())
		{
			if (go == null) continue;
			var e = go.GetComponent<NREnemy>();
			if (e != null && !e.IsDead && OnLava(go.transform.position)) e.ReceiveDamage(10, false, NRDamageKind.Burn);
		}
	}
}

// ============================================================================
// 빙결 수정: 공격하면 금이 가고, 3번 치면 부서지며 재화 + 체력 조금
// ============================================================================
public class NRBreakableCrystal : MonoBehaviour
{
	int hits;
	SpriteRenderer sr;
	float lastHit;
	PlayerAction action;

	public static void Create(Vector3 pos, Sprite sprite, Transform parent)
	{
		if (sprite == null) return;
		var go = new GameObject("NR Ice Crystal");
		go.transform.SetParent(parent, true);
		go.transform.position = new Vector3(pos.x, pos.y - 0.1f, 0);
		go.layer = 7;
		var c = go.AddComponent<NRBreakableCrystal>();
		c.sr = go.AddComponent<SpriteRenderer>();
		c.sr.sprite = sprite;
		go.transform.localScale = Vector3.one * 1.5f;
		NRSort.Set(c.sr, "Deco", Mathf.RoundToInt(-pos.y * 10f));
		var col = go.AddComponent<CircleCollider2D>();
		col.isTrigger = true;
		col.radius = 0.35f;
		col.offset = new Vector2(0, 0.3f);
		var rb = go.AddComponent<Rigidbody2D>();
		rb.bodyType = RigidbodyType2D.Kinematic;
		var it = NRInteractable.Attach(go, "빙결 수정", "공격해서 부수면 재화", NRPalette.Cyan, "");
		it.showRadius = 2.5f;
	}

	void Start()
	{
		var p = FindObjectOfType<Player>();
		action = p != null ? p.GetComponent<PlayerAction>() : null;
	}

	void OnTriggerEnter2D(Collider2D other)
	{
		if (other.CompareTag("Player") && other.isTrigger && action != null && action.isAtking) Hit();
	}

	void Update()
	{
		// 원거리 캐릭터의 탄환
		if (Time.frameCount % 4 != 0) return;
		foreach (var pr in FindObjectsOfType<NRPlayerProjectile>())
			if (Vector2.Distance(pr.transform.position, (Vector2)transform.position + new Vector2(0, 0.45f)) < 0.5f)
			{
				Destroy(pr.gameObject);
				Hit();
				break;
			}
	}

	void Hit()
	{
		if (Time.time - lastHit < 0.2f) return;
		lastHit = Time.time;
		hits++;
		NRCombatFX.Sparks((Vector2)transform.position + new Vector2(0, 0.4f), NRPalette.Cyan, 8);
		NRAudio.PlaySfx("pickup", 0.3f);
		transform.localScale = Vector3.one * (1.5f - hits * 0.12f);
		sr.color = Color.Lerp(Color.white, NRPalette.Cyan, hits / 3f);
		if (hits < 3) return;
		NRCombatFX.DeathBurst(transform.position, NRPalette.Cyan, 18);
		NRPickup.DropCoins(transform.position, 3 + NRRun.Floor);
		var p = NRStats.Player;
		if (p != null) NRStats.HealPlayer(p.maxHP * 0.04f, true);
		Destroy(gameObject);
	}
}
