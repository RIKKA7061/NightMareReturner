using System.Collections.Generic;
using UnityEngine;

// ============================================================================
// 포탈 연출: 기존 문/텔레포트 그림을 숨기고 회전하는 포탈 + 보상 아이콘 표시
// ============================================================================
public enum NRPortalReward { None, Health, Attack, Money, Emotion, Store, Boss, Next }

public class NRPortalVisual : MonoBehaviour
{
	static Sprite[] ringFrames;
	static Sprite coreSprite;

	Transform root;
	SpriteRenderer ring, core, glow, bubble, icon;
	readonly List<(Transform t, SpriteRenderer sr, float speed, float phase)> motes = new List<(Transform, SpriteRenderer, float, float)>();
	Color color;
	float frameTimer;
	int frame;
	Vector3 basePos;

	public static NRPortalVisual Attach(GameObject go, NRPortalReward reward, Color color)
	{
		var v = go.GetComponent<NRPortalVisual>();
		if (v != null) return v;
		v = go.AddComponent<NRPortalVisual>();
		v.color = color;
		v.Build(reward);
		return v;
	}

	public static NRPortalReward RewardFromName(string lowerName, bool bossTp)
	{
		if (bossTp || lowerName.Contains("boss")) return NRPortalReward.Boss;
		if (lowerName.Contains("store")) return NRPortalReward.Store;
		if (lowerName.Contains("health") || lowerName.Contains("hp")) return NRPortalReward.Health;
		if (lowerName.Contains("attack") || lowerName.Contains("atk")) return NRPortalReward.Attack;
		if (lowerName.Contains("money")) return NRPortalReward.Money;
		if (lowerName.Contains("emotion") || lowerName.Contains("round")) return NRPortalReward.Emotion;
		return NRPortalReward.Next;
	}

	void Build(NRPortalReward reward)
	{
		// 기존 그림 숨김
		Bounds b = new Bounds(transform.position, Vector3.one);
		bool has = false;
		foreach (var r in GetComponentsInChildren<SpriteRenderer>())
		{
			if (!has) { b = r.bounds; has = true; } else b.Encapsulate(r.bounds);
			r.enabled = false;
		}
		if (!has)
		{
			foreach (var c in GetComponentsInChildren<Collider2D>())
			{
				if (!has) { b = c.bounds; has = true; } else b.Encapsulate(c.bounds);
			}
		}

		EnsureSprites();
		root = new GameObject("NR Portal FX").transform;
		root.SetParent(transform, false);
		root.position = new Vector3(b.center.x, b.min.y + 0.55f, 0);
		var ls = transform.lossyScale;
		root.localScale = new Vector3(1f / Mathf.Max(0.001f, Mathf.Abs(ls.x)), 1f / Mathf.Max(0.001f, Mathf.Abs(ls.y)), 1f);
		basePos = root.localPosition;

		glow = Child("Glow", NRSprites.Glow, color.WithAlpha(0.45f), NRSort.Floor, 40, new Vector3(2.6f, 1.6f, 1f), Vector3.zero);
		core = Child("Core", coreSprite, Color.Lerp(color, Color.black, 0.55f).WithAlpha(0.95f), NRSort.Floor, 41, new Vector3(1.25f, 1.55f, 1f), new Vector3(0, 0.35f, 0));
		ring = Child("Ring", ringFrames[0], color, NRSort.Floor, 42, new Vector3(1.45f, 1.8f, 1f), new Vector3(0, 0.35f, 0));

		for (int i = 0; i < 8; i++)
		{
			var m = Child("Mote", NRSprites.White, Color.Lerp(color, Color.white, 0.5f), NRSort.Floor, 43, Vector3.one * Random.Range(0.05f, 0.1f), Vector3.zero);
			motes.Add((m.transform, m, Random.Range(0.6f, 1.2f), Random.value));
		}

		Sprite iconSprite = null;
		Color iconColor = Color.white;
		switch (reward)
		{
			case NRPortalReward.Health: iconSprite = NRSprites.Heart; iconColor = NRPalette.Hp; break;
			case NRPortalReward.Attack: iconSprite = NRSprites.Icon(0); break;
			case NRPortalReward.Money: iconSprite = NRSprites.Coin; iconColor = NRPalette.Gold; break;
			case NRPortalReward.Emotion: iconSprite = NRSprites.Orb; iconColor = NRPalette.Anxiety; break;
			case NRPortalReward.Store: iconSprite = NRSprites.Coin; iconColor = NRPalette.Gold; break;
			case NRPortalReward.Boss: iconSprite = NRSprites.Icon(5); break;
		}
		if (iconSprite != null)
		{
			bubble = Child("Bubble", NRSprites.Disc, NRPalette.Bg0.WithAlpha(0.9f), NRSort.Top, 380, Vector3.one * 0.8f, new Vector3(0, 1.75f, 0));
			var ring2 = Child("BubbleRing", NRSprites.Disc, color.WithAlpha(0.9f), NRSort.Top, 379, Vector3.one * 0.9f, new Vector3(0, 1.75f, 0));
			icon = Child("Icon", iconSprite, iconColor, NRSort.Top, 381, Vector3.one, new Vector3(0, 1.75f, 0));
			float target = 0.55f;
			var ib = iconSprite.bounds.size;
			float scale = target / Mathf.Max(0.01f, Mathf.Max(ib.x, ib.y));
			icon.transform.localScale = Vector3.one * scale;
		}
	}

	SpriteRenderer Child(string name, Sprite sprite, Color c, string layer, int order, Vector3 scale, Vector3 pos)
	{
		var go = new GameObject(name);
		go.transform.SetParent(root, false);
		go.transform.localPosition = pos;
		go.transform.localScale = scale;
		var sr = go.AddComponent<SpriteRenderer>();
		sr.sprite = sprite;
		sr.color = c;
		NRSort.Set(sr, layer, order);
		return sr;
	}

	void Update()
	{
		if (ring == null) return;
		frameTimer += Time.deltaTime;
		if (frameTimer > 0.08f)
		{
			frameTimer = 0f;
			frame = (frame + 1) % ringFrames.Length;
			ring.sprite = ringFrames[frame];
		}
		float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 3f);
		glow.color = color.WithAlpha(0.35f + 0.2f * pulse);
		core.transform.localScale = new Vector3(1.25f, 1.55f, 1f) * (0.97f + 0.05f * pulse);

		for (int i = 0; i < motes.Count; i++)
		{
			var m = motes[i];
			float t = Mathf.Repeat(Time.time * m.speed * 0.6f + m.phase, 1f);
			float ang = (m.phase * 6.28f) + Time.time * 0.8f;
			m.t.localPosition = new Vector3(Mathf.Cos(ang) * 0.55f * (1f - t * 0.5f), 0.1f + t * 1.6f, 0);
			m.sr.color = m.sr.color.WithAlpha(Mathf.Sin(t * Mathf.PI) * 0.9f);
		}

		if (icon != null)
		{
			float bob = Mathf.Sin(Time.time * 2.4f) * 0.08f;
			icon.transform.localPosition = new Vector3(0, 1.75f + bob, 0);
			bubble.transform.localPosition = new Vector3(0, 1.75f + bob, 0);
		}
	}

	void OnDestroy()
	{
		// 부모와 함께 사라짐
	}

	static void EnsureSprites()
	{
		if (ringFrames != null && ringFrames[0] != null) return;
		const int size = 48;
		ringFrames = new Sprite[8];
		for (int f = 0; f < 8; f++)
		{
			var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
			var px = new Color[size * size];
			float rot = f / 8f * Mathf.PI * 2f;
			for (int y = 0; y < size; y++)
				for (int x = 0; x < size; x++)
				{
					float dx = (x + 0.5f - size / 2f) / (size / 2f);
					float dy = (y + 0.5f - size / 2f) / (size / 2f);
					float d = Mathf.Sqrt(dx * dx + dy * dy);
					float a = Mathf.Atan2(dy, dx);
					Color c = Color.clear;
					if (d > 0.72f && d < 0.98f)
					{
						float band = 0.5f + 0.5f * Mathf.Sin(a * 3f + rot);
						float alpha = d > 0.9f ? 1f : 0.55f + 0.45f * band;
						c = new Color(1, 1, 1, alpha);
					}
					else if (d <= 0.72f)
					{
						float swirl = 0.5f + 0.5f * Mathf.Sin(a * 2f - rot * 2f + d * 9f);
						c = new Color(1, 1, 1, swirl * 0.35f * (1f - d));
					}
					px[y * size + x] = c;
				}
			tex.SetPixels(px);
			tex.Apply();
			ringFrames[f] = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
		}
		coreSprite = NRSprites.Disc;
	}
}

/// <summary>화면 가장자리 방향 표시 대상 (문/포탈/구슬 등)</summary>
public class NRWaypoint
{
	public static readonly List<NRWaypoint> All = new List<NRWaypoint>();

	public Transform target;
	public string label;
	public Color color;
	public System.Func<bool> active;

	public static NRWaypoint Add(Transform target, string label, Color color, System.Func<bool> active = null)
	{
		All.RemoveAll(w => w.target == null || w.target == target);
		var w = new NRWaypoint { target = target, label = label, color = color, active = active };
		All.Add(w);
		return w;
	}

	public bool IsActive
	{
		get
		{
			if (target == null || !target.gameObject.activeInHierarchy) return false;
			return active == null || active();
		}
	}
}
