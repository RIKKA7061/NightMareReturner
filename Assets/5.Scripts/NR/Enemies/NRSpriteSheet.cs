using System;
using System.Collections.Generic;
using UnityEngine;

// ============================================================================
// DARK 팩 세로 스트립 스프라이트 시트를 런타임에 잘라 애니메이션으로 사용
// (Resources/NR/Sprites/Enemies/{종류}/{동작}.png, 읽기 가능/포인트 필터)
// ============================================================================
public class NRClip
{
	public Sprite[] frames;
	public Sprite[] silhouettes;   // 흰색 실루엣 (외곽선/피격 번쩍임)
	public float fps = 10f;
	public bool loop = true;
	public int Length => frames != null ? frames.Length : 0;
}

public static class NRSpriteSheet
{
	static readonly Dictionary<string, NRClip> cache = new Dictionary<string, NRClip>();
	static readonly Dictionary<string, Vector2> pivotCache = new Dictionary<string, Vector2>();

	/// <param name="kind">폴더 이름</param>
	/// <param name="anim">파일 이름</param>
	/// <param name="frameHeight">프레임 높이(px)</param>
	/// <param name="pivotAnim">피벗 계산 기준 동작 (보통 idle/walk)</param>
	public static NRClip Load(string kind, string anim, int frameHeight, string pivotAnim, float fps, bool loop, float ppu = 100f)
	{
		string key = kind + "/" + anim;
		if (cache.TryGetValue(key, out var clip)) return clip;

		var tex = Resources.Load<Texture2D>("NR/Sprites/Enemies/" + key);
		clip = new NRClip { fps = fps, loop = loop };
		if (tex == null)
		{
			Debug.LogWarning("[NRSpriteSheet] 시트 없음: " + key);
			clip.frames = new Sprite[0];
			clip.silhouettes = new Sprite[0];
			cache[key] = clip;
			return clip;
		}
		tex.filterMode = FilterMode.Point;

		Vector2 pivot = GetPivot(kind, pivotAnim, frameHeight);
		int count = Mathf.Max(1, tex.height / frameHeight);
		clip.frames = new Sprite[count];
		clip.silhouettes = new Sprite[count];

		Texture2D sil = MakeSilhouette(tex);
		for (int i = 0; i < count; i++)
		{
			var rect = new Rect(0, tex.height - (i + 1) * frameHeight, tex.width, frameHeight);
			clip.frames[i] = Sprite.Create(tex, rect, pivot, ppu, 0, SpriteMeshType.FullRect);
			clip.silhouettes[i] = sil != null ? Sprite.Create(sil, rect, pivot, ppu, 0, SpriteMeshType.FullRect) : clip.frames[i];
		}
		cache[key] = clip;
		return clip;
	}

	/// <summary>기준 동작의 모든 프레임에서 실제 그림 영역을 찾아 발밑 중앙을 피벗으로 사용</summary>
	static Vector2 GetPivot(string kind, string pivotAnim, int frameHeight)
	{
		string key = kind + "/" + pivotAnim;
		if (pivotCache.TryGetValue(key, out var p)) return p;
		p = new Vector2(0.5f, 0f);
		var tex = Resources.Load<Texture2D>("NR/Sprites/Enemies/" + key);
		if (tex != null && tex.isReadable)
		{
			try
			{
				var px = tex.GetPixels32();
				int w = tex.width, h = tex.height;
				int count = Mathf.Max(1, h / frameHeight);
				int minX = w, maxX = -1, minY = frameHeight;
				for (int f = 0; f < count; f++)
				{
					int y0 = h - (f + 1) * frameHeight;
					for (int y = 0; y < frameHeight; y++)
						for (int x = 0; x < w; x++)
						{
							if (px[(y0 + y) * w + x].a < 20) continue;
							if (x < minX) minX = x;
							if (x > maxX) maxX = x;
							if (y < minY) minY = y;
						}
				}
				if (maxX >= minX)
					p = new Vector2(((minX + maxX + 1) * 0.5f) / w, Mathf.Clamp01((float)minY / frameHeight));
			}
			catch (Exception e) { Debug.LogWarning("[NRSpriteSheet] 피벗 계산 실패: " + e.Message); }
		}
		pivotCache[key] = p;
		return p;
	}

	static Texture2D MakeSilhouette(Texture2D src)
	{
		if (!src.isReadable) return null;
		try
		{
			var px = src.GetPixels32();
			for (int i = 0; i < px.Length; i++)
			{
				byte a = px[i].a;
				px[i] = new Color32(255, 255, 255, a);
			}
			var t = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false);
			t.filterMode = FilterMode.Point;
			t.wrapMode = TextureWrapMode.Clamp;
			t.SetPixels32(px);
			t.Apply();
			return t;
		}
		catch { return null; }
	}
}

/// <summary>코드 기반 프레임 애니메이터 + 외곽선 + 피격 번쩍임</summary>
public class NRSpriteAnimator : MonoBehaviour
{
	public SpriteRenderer body;
	SpriteRenderer flash;
	SpriteRenderer[] outline;
	NRClip clip;
	int frame;
	float timer;
	float speed = 1f;
	float flashUntil;
	Action onComplete;
	readonly Dictionary<int, Action> frameEvents = new Dictionary<int, Action>();

	public int Frame => frame;
	public NRClip Current => clip;
	public bool Finished { get; private set; }
	public Color outlineColor = NRPalette.Crimson;

	public void Init(SpriteRenderer sr, Color outlineCol, int sortingOrder)
	{
		body = sr;
		outlineColor = outlineCol;
		body.sortingOrder = sortingOrder;
		outline = new SpriteRenderer[4];
		for (int i = 0; i < 4; i++)
		{
			var go = new GameObject("Outline" + i);
			go.transform.SetParent(transform, false);
			outline[i] = go.AddComponent<SpriteRenderer>();
			outline[i].sortingOrder = sortingOrder - 1;
			outline[i].color = outlineCol.WithAlpha(0.9f);
		}
		var fgo = new GameObject("Flash");
		fgo.transform.SetParent(transform, false);
		flash = fgo.AddComponent<SpriteRenderer>();
		flash.sortingOrder = sortingOrder + 1;
		flash.enabled = false;
	}

	public void Play(NRClip c, bool restart = false, Action complete = null, float playSpeed = 1f)
	{
		if (c == null || c.Length == 0) return;
		if (clip == c && !restart) { speed = playSpeed; return; }
		clip = c;
		frame = 0;
		timer = 0f;
		speed = playSpeed;
		Finished = false;
		onComplete = complete;
		frameEvents.Clear();
		Apply();
	}

	public void OnFrame(int index, Action action) { frameEvents[index] = action; }

	public void Flash(float duration = 0.08f) { flashUntil = Time.time + duration; }

	void Update()
	{
		if (clip == null || clip.Length == 0) return;
		if (!Finished)
		{
			timer += Time.deltaTime * speed;
			float step = 1f / Mathf.Max(1f, clip.fps);
			while (timer >= step)
			{
				timer -= step;
				int next = frame + 1;
				if (next >= clip.Length)
				{
					if (clip.loop) next = 0;
					else
					{
						Finished = true;
						var cb = onComplete;
						onComplete = null;
						cb?.Invoke();
						break;
					}
				}
				frame = next;
				if (frameEvents.TryGetValue(frame, out var ev)) ev?.Invoke();
				Apply();
			}
		}
	}

	void LateUpdate()
	{
		if (body == null || clip == null || clip.Length == 0) return;
		var sil = clip.silhouettes != null && frame < clip.silhouettes.Length ? clip.silhouettes[frame] : null;
		float texel = 1f / Mathf.Max(1f, body.sprite != null ? body.sprite.pixelsPerUnit : 100f);
		Vector3[] offs = { new Vector3(texel, 0), new Vector3(-texel, 0), new Vector3(0, texel), new Vector3(0, -texel) };
		for (int i = 0; i < 4; i++)
		{
			outline[i].sprite = sil;
			outline[i].flipX = body.flipX;
			outline[i].transform.localPosition = offs[i];
			outline[i].enabled = body.enabled && sil != null;
			outline[i].color = outlineColor.WithAlpha(body.color.a * 0.85f);
		}
		bool flashing = Time.time < flashUntil;
		flash.enabled = flashing && sil != null;
		if (flashing)
		{
			flash.sprite = sil;
			flash.flipX = body.flipX;
			flash.color = new Color(1f, 1f, 1f, 0.9f);
		}
	}

	void Apply()
	{
		if (body != null && clip != null && frame < clip.Length) body.sprite = clip.frames[frame];
	}
}
