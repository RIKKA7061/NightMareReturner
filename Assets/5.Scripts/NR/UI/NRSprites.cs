using System.Collections.Generic;
using UnityEngine;

// ============================================================================
// 코드로 생성하는 픽셀 아트 UI/이펙트 스프라이트 (외부 에셋 의존 없음)
// ============================================================================
public static class NRSprites
{
	static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

	static Texture2D NewTex(int w, int h, FilterMode filter)
	{
		var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
		t.filterMode = filter;
		t.wrapMode = TextureWrapMode.Clamp;
		t.hideFlags = HideFlags.DontSave;
		return t;
	}

	/// <summary>9-슬라이스 픽셀 프레임 (테두리 4px, 모서리 깎임)</summary>
	public static Sprite Frame(Color fill, Color border, Color highlight, Color outline, bool cutCorners = true)
	{
		string key = "frame" + fill + border + highlight + outline + cutCorners;
		if (cache.TryGetValue(key, out var s) && s != null) return s;

		const int size = 12;
		var tex = NewTex(size, size, FilterMode.Point);
		var px = new Color[size * size];
		for (int y = 0; y < size; y++)
		{
			for (int x = 0; x < size; x++)
			{
				int edge = Mathf.Min(Mathf.Min(x, y), Mathf.Min(size - 1 - x, size - 1 - y));
				Color c;
				if (edge == 0) c = outline;
				else if (edge == 1) c = border;
				else if (edge == 2) c = (y >= size - 3 || x <= 2) ? highlight : Color.Lerp(border, fill, 0.5f);
				else c = fill;

				bool corner = cutCorners && ((x == 0 || x == size - 1) && (y == 0 || y == size - 1));
				if (corner) c = Color.clear;
				bool corner2 = cutCorners && ((x <= 1 && y <= 1) || (x >= size - 2 && y <= 1) || (x <= 1 && y >= size - 2) || (x >= size - 2 && y >= size - 2));
				if (corner2 && !corner && edge == 1) c = outline;
				px[y * size + x] = c;
			}
		}
		tex.SetPixels(px);
		tex.Apply();
		s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(4, 4, 4, 4));
		s.name = "NR Frame";
		cache[key] = s;
		return s;
	}

	public static Sprite PanelFrame => Frame(NRPalette.Bg1.WithAlpha(0.95f), NRPalette.Border, NRPalette.BorderHi.WithAlpha(0.55f), new Color(0.02f, 0.015f, 0.04f, 1f));
	public static Sprite DarkFrame => Frame(NRPalette.Bg0.WithAlpha(0.92f), NRPalette.Border, NRPalette.Border, new Color(0.02f, 0.015f, 0.04f, 1f));
	public static Sprite ButtonFrame => Frame(NRPalette.Bg2, NRPalette.Border, NRPalette.BorderHi.WithAlpha(0.35f), new Color(0.02f, 0.015f, 0.04f, 1f));
	public static Sprite ButtonHoverFrame => Frame(NRPalette.Hex("33285A"), NRPalette.BorderHi, NRPalette.Pink.WithAlpha(0.7f), new Color(0.02f, 0.015f, 0.04f, 1f));
	public static Sprite ButtonPressFrame => Frame(NRPalette.Hex("1A1430"), NRPalette.Pink, NRPalette.Pink, new Color(0.02f, 0.015f, 0.04f, 1f));
	public static Sprite ColoredFrame(Color border) => Frame(NRPalette.Bg1.WithAlpha(0.97f), border, Color.Lerp(border, Color.white, 0.35f), new Color(0.02f, 0.015f, 0.04f, 1f));

	public static Sprite White
	{
		get
		{
			if (cache.TryGetValue("white", out var s) && s != null) return s;
			var tex = NewTex(4, 4, FilterMode.Point);
			var px = new Color[16];
			for (int i = 0; i < px.Length; i++) px[i] = Color.white;
			tex.SetPixels(px);
			tex.Apply();
			s = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f, 0, SpriteMeshType.FullRect);
			cache["white"] = s;
			return s;
		}
	}

	/// <summary>부드러운 원형 광원 (월드 1유닛 = 64px)</summary>
	public static Sprite Glow
	{
		get
		{
			if (cache.TryGetValue("glow", out var s) && s != null) return s;
			const int size = 64;
			var tex = NewTex(size, size, FilterMode.Bilinear);
			var px = new Color[size * size];
			for (int y = 0; y < size; y++)
				for (int x = 0; x < size; x++)
				{
					float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(size / 2f, size / 2f)) / (size / 2f);
					float a = Mathf.Clamp01(1f - d);
					a = a * a * (3f - 2f * a);
					px[y * size + x] = new Color(1, 1, 1, a);
				}
			tex.SetPixels(px);
			tex.Apply();
			s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
			cache["glow"] = s;
			return s;
		}
	}

	/// <summary>픽셀 원판 + 테두리 (위험 범위 표시용)</summary>
	public static Sprite Disc
	{
		get
		{
			if (cache.TryGetValue("disc", out var s) && s != null) return s;
			const int size = 48;
			var tex = NewTex(size, size, FilterMode.Point);
			var px = new Color[size * size];
			float r = size / 2f;
			for (int y = 0; y < size; y++)
				for (int x = 0; x < size; x++)
				{
					float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
					Color c = Color.clear;
					if (d <= r) c = new Color(1, 1, 1, 0.35f);
					if (d <= r && d > r - 2f) c = new Color(1, 1, 1, 1f);
					px[y * size + x] = c;
				}
			tex.SetPixels(px);
			tex.Apply();
			s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
			cache["disc"] = s;
			return s;
		}
	}

	/// <summary>화면 가장자리 어둡게 (비네트)</summary>
	public static Sprite Vignette
	{
		get
		{
			if (cache.TryGetValue("vignette", out var s) && s != null) return s;
			const int size = 128;
			var tex = NewTex(size, size, FilterMode.Bilinear);
			var px = new Color[size * size];
			for (int y = 0; y < size; y++)
				for (int x = 0; x < size; x++)
				{
					float dx = (x + 0.5f) / size * 2f - 1f;
					float dy = (y + 0.5f) / size * 2f - 1f;
					float d = Mathf.Sqrt(dx * dx * 0.8f + dy * dy);
					float a = Mathf.Clamp01((d - 0.55f) / 0.75f);
					px[y * size + x] = new Color(1, 1, 1, a * a);
				}
			tex.SetPixels(px);
			tex.Apply();
			s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
			cache["vignette"] = s;
			return s;
		}
	}

	/// <summary>아래를 가리키는 픽셀 화살표</summary>
	public static Sprite ArrowDown
	{
		get
		{
			if (cache.TryGetValue("arrowdown", out var s) && s != null) return s;
			string[] rows =
			{
				"...........",
				".#########.",
				".#+++++++#.",
				"..#+++++#..",
				"...#+++#...",
				"....#+#....",
				".....#.....",
				"...........",
			};
			s = FromPattern("arrowdown", rows, 16f, new Vector2(0.5f, 0f));
			return s;
		}
	}

	/// <summary>오른쪽을 가리키는 화살표 (화면 가장자리 적 표시)</summary>
	public static Sprite ArrowRight
	{
		get
		{
			string[] rows =
			{
				"#.......",
				"##......",
				"#+#.....",
				"#++#....",
				"#+++#...",
				"#++++#..",
				"#+++++#.",
				"#++++#..",
				"#+++#...",
				"#++#....",
				"#+#.....",
				"##......",
				"#.......",
			};
			return FromPattern("arrowright", rows, 100f, new Vector2(0.5f, 0.5f));
		}
	}

	public static Sprite Diamond
	{
		get
		{
			string[] rows =
			{
				"...#...",
				"..#+#..",
				".#+++#.",
				"#+++++#",
				".#+++#.",
				"..#+#..",
				"...#...",
			};
			return FromPattern("diamond", rows, 100f, new Vector2(0.5f, 0.5f));
		}
	}

	public static Sprite Orb
	{
		get
		{
			string[] rows =
			{
				"..###..",
				".#+++#.",
				"#++@++#",
				"#+++++#",
				"#+++++#",
				".#+++#.",
				"..###..",
			};
			return FromPattern("orb", rows, 100f, new Vector2(0.5f, 0.5f));
		}
	}

	public static Sprite Coin
	{
		get
		{
			string[] rows =
			{
				"..###..",
				".#+++#.",
				"#++#++#",
				"#+#+#+#",
				"#++#++#",
				".#+++#.",
				"..###..",
			};
			return FromPattern("coin", rows, 100f, new Vector2(0.5f, 0.5f));
		}
	}

	public static Sprite Heart
	{
		get
		{
			string[] rows =
			{
				".##.##.",
				"#++#++#",
				"#+@+++#",
				"#+++++#",
				".#+++#.",
				"..#+#..",
				"...#...",
			};
			return FromPattern("heart", rows, 100f, new Vector2(0.5f, 0.5f));
		}
	}

	public static Sprite Lock
	{
		get
		{
			string[] rows =
			{
				"..###..",
				".#...#.",
				".#...#.",
				"#######",
				"#+++++#",
				"#++#++#",
				"#++#++#",
				"#+++++#",
				"#######",
			};
			return FromPattern("lock", rows, 100f, new Vector2(0.5f, 0.5f));
		}
	}

	/// <summary>'#' 테두리(어두운색), '+' 채움(흰색), '@' 하이라이트</summary>
	static Sprite FromPattern(string key, string[] rows, float ppu, Vector2 pivot)
	{
		if (cache.TryGetValue(key, out var s) && s != null) return s;
		int h = rows.Length, w = rows[0].Length;
		var tex = NewTex(w, h, FilterMode.Point);
		for (int y = 0; y < h; y++)
		{
			string row = rows[h - 1 - y];
			for (int x = 0; x < w; x++)
			{
				char ch = x < row.Length ? row[x] : '.';
				Color c = Color.clear;
				if (ch == '#') c = new Color(0.05f, 0.04f, 0.09f, 1f);
				else if (ch == '+') c = Color.white;
				else if (ch == '@') c = new Color(1f, 1f, 1f, 1f) * 1.0f + new Color(0, 0, 0, 0);
				tex.SetPixel(x, y, c);
			}
		}
		tex.Apply();
		s = Sprite.Create(tex, new Rect(0, 0, w, h), pivot, ppu, 0, SpriteMeshType.FullRect);
		cache[key] = s;
		return s;
	}

	/// <summary>초승달 모양 베기 궤적 (오른쪽을 향함)</summary>
	public static Sprite Slash
	{
		get
		{
			if (cache.TryGetValue("slash", out var s) && s != null) return s;
			const int w = 40, h = 40;
			var tex = NewTex(w, h, FilterMode.Point);
			var px = new Color[w * h];
			Vector2 c1 = new Vector2(12, 20), c2 = new Vector2(6, 20);
			for (int y = 0; y < h; y++)
				for (int x = 0; x < w; x++)
				{
					float d1 = Vector2.Distance(new Vector2(x, y), c1);
					float d2 = Vector2.Distance(new Vector2(x, y), c2);
					Color col = Color.clear;
					if (d1 < 19f && d2 > 19f && x > 10)
					{
						float edge = Mathf.Clamp01((19f - d1) / 6f);
						float tip = 1f - Mathf.Abs(y - 20f) / 20f;
						col = new Color(1, 1, 1, Mathf.Clamp01(tip * 1.6f) * (edge > 0.5f ? 1f : 0.55f));
					}
					px[y * w + x] = col;
				}
			tex.SetPixels(px);
			tex.Apply();
			s = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.3f, 0.5f), 32f, 0, SpriteMeshType.FullRect);
			cache["slash"] = s;
			return s;
		}
	}

	// ---- DARK 팩 스킬 아이콘 시트 (24x24, 6x6) ----
	static Sprite[] icons;
	public static Sprite Icon(int index)
	{
		// 도메인 리로드 없이 플레이를 다시 시작하면 static은 남고 스프라이트만 파괴되므로 다시 만든다
		if (icons != null && icons[0] == null) icons = null;
		if (icons == null)
		{
			icons = new Sprite[36];
			var tex = Resources.Load<Texture2D>("NR/Sprites/Icons/skill_icons");
			if (tex != null)
			{
				tex.filterMode = FilterMode.Point;
				for (int i = 0; i < 36; i++)
				{
					int col = i % 6, row = i / 6;
					var rect = new Rect(col * 24, tex.height - (row + 1) * 24, 24, 24);
					icons[i] = Sprite.Create(tex, rect, new Vector2(0.5f, 0.5f), 24f, 0, SpriteMeshType.FullRect);
				}
			}
		}
		if (index < 0) return null;
		return icons[index % 36];
	}
}
