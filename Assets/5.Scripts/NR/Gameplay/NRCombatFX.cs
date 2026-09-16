using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using TMPro;
using UnityEngine;

// ============================================================================
// 전투 연출: 피해 숫자, 화면 흔들림, 피격 플래시, 사망 파편, 잔상, 위험 범위 표시
// ============================================================================
public class NRCombatFX : MonoBehaviour
{
	static NRCombatFX instance;

	static NRCombatFX Inst
	{
		get
		{
			if (instance == null)
			{
				var go = new GameObject("[NR CombatFX]");
				instance = go.AddComponent<NRCombatFX>();
			}
			return instance;
		}
	}

	// ---- 피해 숫자 ----
	class Floater
	{
		public TextMeshPro text;
		public Vector3 velocity;
		public float life, maxLife, baseScale;
	}
	readonly List<Floater> floaters = new List<Floater>();
	readonly Stack<TextMeshPro> textPool = new Stack<TextMeshPro>();

	public static void Number(Vector3 worldPos, string text, Color color, float scale = 1f)
	{
		Inst.SpawnNumber(worldPos, text, color, scale);
	}

	public static void DamageNumber(Vector3 worldPos, int amount, bool crit, NRDamageKind kind)
	{
		if (!NRSettings.DamageNumbers) return;
		Color c = kind == NRDamageKind.Burn ? NRPalette.Rage : crit ? NRPalette.Gold : Color.white;
		string s = crit ? amount + "!" : amount.ToString();
		Inst.SpawnNumber(worldPos + (Vector3)(Random.insideUnitCircle * 0.25f), s, c, crit ? 1.45f : kind == NRDamageKind.Burn ? 0.8f : 1f);
	}

	void SpawnNumber(Vector3 pos, string s, Color color, float scale)
	{
		TextMeshPro t = textPool.Count > 0 ? textPool.Pop() : null;
		if (t == null)
		{
			var go = new GameObject("NR Number");
			go.transform.SetParent(transform, false);
			t = go.AddComponent<TextMeshPro>();
			go.AddComponent<NRKeepFont>();
			if (NRFont.Asset != null) NRFont.Style(t, NRTextFx.OutlineShadow);
			t.alignment = TextAlignmentOptions.Center;
			t.enableWordWrapping = false;
			t.rectTransform.sizeDelta = new Vector2(6, 2);
			t.sortingOrder = 500;
		}
		t.gameObject.SetActive(true);
		t.text = s;
		t.color = color;
		t.fontSize = 4.2f;
		t.transform.position = pos + Vector3.up * 0.3f;
		t.transform.localScale = Vector3.one * scale * 1.6f;
		floaters.Add(new Floater { text = t, velocity = new Vector3(Random.Range(-0.4f, 0.4f), 2.2f, 0), life = 0f, maxLife = 0.75f, baseScale = scale });
	}

	void Update()
	{
		float dt = Time.unscaledDeltaTime;
		for (int i = floaters.Count - 1; i >= 0; i--)
		{
			var f = floaters[i];
			if (f.text == null) { floaters.RemoveAt(i); continue; }
			f.life += dt;
			float k = f.life / f.maxLife;
			f.text.transform.position += f.velocity * dt;
			f.velocity.y -= 4.5f * dt;
			float pop = k < 0.15f ? Mathf.Lerp(1.6f, 1f, k / 0.15f) : 1f;
			f.text.transform.localScale = Vector3.one * f.baseScale * pop;
			var c = f.text.color;
			c.a = k > 0.6f ? 1f - (k - 0.6f) / 0.4f : 1f;
			f.text.color = c;
			if (k >= 1f)
			{
				f.text.gameObject.SetActive(false);
				textPool.Push(f.text);
				floaters.RemoveAt(i);
			}
		}
		UpdateParticles(Time.deltaTime);
		UpdateShake();
	}

	// ---- 화면 흔들림 (Cinemachine FramingTransposer 오프셋) ----
	CinemachineFramingTransposer transposer;
	Vector3 baseOffset;
	float shakeUntil, shakeStrength, shakeDuration;
	bool shaking;

	public static void Shake(float duration, float strength)
	{
		if (!NRSettings.ScreenShake) return;
		Inst.StartShake(duration, strength);
	}

	void StartShake(float duration, float strength)
	{
		if (transposer == null)
		{
			var vcam = FindObjectOfType<CinemachineVirtualCamera>();
			if (vcam == null) return;
			transposer = vcam.GetCinemachineComponent<CinemachineFramingTransposer>();
			if (transposer == null) return;
			baseOffset = transposer.m_TrackedObjectOffset;
		}
		if (!shaking) baseOffset = transposer.m_TrackedObjectOffset;
		shaking = true;
		shakeDuration = Mathf.Max(duration, shakeUntil - Time.unscaledTime);
		shakeUntil = Time.unscaledTime + shakeDuration;
		shakeStrength = Mathf.Max(strength, shaking ? shakeStrength : 0f);
	}

	void UpdateShake()
	{
		if (!shaking) return;
		if (transposer == null) { shaking = false; return; }
		float remain = shakeUntil - Time.unscaledTime;
		if (remain <= 0f)
		{
			transposer.m_TrackedObjectOffset = baseOffset;
			shaking = false;
			shakeStrength = 0f;
			return;
		}
		float k = remain / Mathf.Max(0.01f, shakeDuration);
		Vector2 r = Random.insideUnitCircle * shakeStrength * k;
		transposer.m_TrackedObjectOffset = baseOffset + new Vector3(r.x, r.y, 0);
	}

	// ---- 파편 ----
	class Particle
	{
		public Transform t;
		public SpriteRenderer sr;
		public Vector3 v;
		public float life, max;
	}
	readonly List<Particle> particles = new List<Particle>();
	readonly Stack<SpriteRenderer> particlePool = new Stack<SpriteRenderer>();

	public static void DeathBurst(Vector3 pos, Color color, int count)
	{
		Inst.Burst(pos, color, count, 3.2f, 0.55f);
	}

	public static void Sparks(Vector3 pos, Color color, int count = 6)
	{
		Inst.Burst(pos, color, count, 2.2f, 0.3f);
	}

	void Burst(Vector3 pos, Color color, int count, float speed, float life)
	{
		for (int i = 0; i < count; i++)
		{
			SpriteRenderer sr = particlePool.Count > 0 ? particlePool.Pop() : null;
			if (sr == null)
			{
				var go = new GameObject("NR Particle");
				go.transform.SetParent(transform, false);
				sr = go.AddComponent<SpriteRenderer>();
				sr.sprite = NRSprites.White;
				sr.sortingOrder = 450;
			}
			sr.gameObject.SetActive(true);
			sr.color = i % 3 == 0 ? Color.white : color;
			float size = Random.Range(0.05f, 0.12f);
			sr.transform.localScale = new Vector3(size, size, 1f);
			sr.transform.position = pos + (Vector3)(Random.insideUnitCircle * 0.15f);
			Vector2 dir = Random.insideUnitCircle.normalized * Random.Range(0.4f, 1f) * speed;
			particles.Add(new Particle { t = sr.transform, sr = sr, v = new Vector3(dir.x, dir.y + 1f, 0), life = 0, max = life * Random.Range(0.7f, 1.3f) });
		}
	}

	void UpdateParticles(float dt)
	{
		for (int i = particles.Count - 1; i >= 0; i--)
		{
			var p = particles[i];
			if (p.t == null) { particles.RemoveAt(i); continue; }
			p.life += dt;
			p.t.position += p.v * dt;
			p.v *= 1f - 3.5f * dt;
			var c = p.sr.color;
			c.a = 1f - p.life / p.max;
			p.sr.color = c;
			if (p.life >= p.max)
			{
				p.sr.gameObject.SetActive(false);
				particlePool.Push(p.sr);
				particles.RemoveAt(i);
			}
		}
	}

	// ---- 플레이어 피격 화면 플래시 ----
	public static void PlayerHitFlash()
	{
		NRHud.FlashDamage();
	}

	// ---- 스프라이트 번쩍임 (기존 적 / 보스) ----
	public static void Flash(SpriteRenderer sr, Color flashColor, float duration = 0.09f)
	{
		if (sr == null) return;
		Inst.StartCoroutine(Inst.FlashRoutine(sr, flashColor, duration));
	}

	readonly Dictionary<SpriteRenderer, Color> originalColors = new Dictionary<SpriteRenderer, Color>();
	readonly Dictionary<SpriteRenderer, int> flashCounts = new Dictionary<SpriteRenderer, int>();

	IEnumerator FlashRoutine(SpriteRenderer sr, Color flash, float duration)
	{
		// 겹쳐서 번쩍여도 원래 색을 잃지 않도록 참조 횟수로 관리
		if (!flashCounts.TryGetValue(sr, out int count) || count <= 0) originalColors[sr] = sr.color;
		flashCounts[sr] = count + 1;
		Color orig = originalColors[sr];
		sr.color = flash;
		yield return new WaitForSeconds(duration);
		flashCounts[sr] = flashCounts[sr] - 1;
		if (flashCounts[sr] <= 0)
		{
			if (sr != null) sr.color = orig;
			flashCounts.Remove(sr);
			originalColors.Remove(sr);
		}
	}

	// ---- 구르기 잔상 ----
	public static void Afterimage(SpriteRenderer source, Color tint, int count = 4, float interval = 0.05f)
	{
		if (source == null) return;
		Inst.StartCoroutine(Inst.AfterimageRoutine(source, tint, count, interval));
	}

	IEnumerator AfterimageRoutine(SpriteRenderer src, Color tint, int count, float interval)
	{
		for (int i = 0; i < count && src != null; i++)
		{
			var go = new GameObject("NR Ghost");
			var sr = go.AddComponent<SpriteRenderer>();
			sr.sprite = src.sprite;
			sr.flipX = src.flipX;
			sr.sortingLayerID = src.sortingLayerID;
			sr.sortingOrder = src.sortingOrder - 1;
			sr.color = tint;
			go.transform.position = src.transform.position;
			go.transform.rotation = src.transform.rotation;
			go.transform.localScale = src.transform.lossyScale;
			StartCoroutine(FadeAndDestroy(sr, 0.28f));
			yield return new WaitForSeconds(interval);
		}
	}

	IEnumerator FadeAndDestroy(SpriteRenderer sr, float duration)
	{
		float t = 0f;
		Color c = sr.color;
		while (t < duration && sr != null)
		{
			t += Time.deltaTime;
			sr.color = new Color(c.r, c.g, c.b, c.a * (1f - t / duration));
			yield return null;
		}
		if (sr != null) Destroy(sr.gameObject);
	}
}

// ---------------------------------------------------------------------------
// 공격 예고 표시 (원형 / 직선)
// ---------------------------------------------------------------------------
public class NRTelegraph : MonoBehaviour
{
	SpriteRenderer fill, edge;
	float duration, t;
	bool isLine;
	Color color;
	Transform follow;
	Vector3 followOffset;

	public static NRTelegraph Circle(Vector3 pos, float radius, float duration, Color color, Transform follow = null)
	{
		var go = new GameObject("NR Telegraph Circle");
		go.transform.position = pos;
		var tg = go.AddComponent<NRTelegraph>();
		tg.duration = duration;
		tg.color = color;
		tg.follow = follow;
		if (follow != null) tg.followOffset = pos - follow.position;

		tg.edge = go.AddComponent<SpriteRenderer>();
		tg.edge.sprite = NRSprites.Disc;
		tg.edge.color = color.WithAlpha(0.55f);
		tg.edge.sortingOrder = 30;
		go.transform.localScale = Vector3.one * radius * 2f;

		var inner = new GameObject("Fill");
		inner.transform.SetParent(go.transform, false);
		tg.fill = inner.AddComponent<SpriteRenderer>();
		tg.fill.sprite = NRSprites.Disc;
		tg.fill.color = color.WithAlpha(0.5f);
		tg.fill.sortingOrder = 31;
		inner.transform.localScale = Vector3.zero;
		return tg;
	}

	public static NRTelegraph Line(Vector3 from, Vector2 dir, float length, float width, float duration, Color color)
	{
		var go = new GameObject("NR Telegraph Line");
		go.transform.position = from;
		float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
		go.transform.rotation = Quaternion.Euler(0, 0, ang);
		var tg = go.AddComponent<NRTelegraph>();
		tg.duration = duration;
		tg.color = color;
		tg.isLine = true;

		var edgeGo = new GameObject("Edge");
		edgeGo.transform.SetParent(go.transform, false);
		edgeGo.transform.localPosition = new Vector3(length / 2f, 0, 0);
		edgeGo.transform.localScale = new Vector3(length, width, 1);
		tg.edge = edgeGo.AddComponent<SpriteRenderer>();
		tg.edge.sprite = NRSprites.White;
		tg.edge.color = color.WithAlpha(0.22f);
		tg.edge.sortingOrder = 30;

		var fillGo = new GameObject("Fill");
		fillGo.transform.SetParent(go.transform, false);
		fillGo.transform.localPosition = Vector3.zero;
		fillGo.transform.localScale = new Vector3(0, width, 1);
		tg.fill = fillGo.AddComponent<SpriteRenderer>();
		tg.fill.sprite = NRSprites.White;
		tg.fill.color = color.WithAlpha(0.45f);
		tg.fill.sortingOrder = 31;
		tg.lineLength = length;
		tg.lineWidth = width;
		return tg;
	}

	float lineLength, lineWidth;

	public void SetDirection(Vector2 dir)
	{
		float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
		transform.rotation = Quaternion.Euler(0, 0, ang);
	}

	void Update()
	{
		t += Time.deltaTime;
		float k = Mathf.Clamp01(t / Mathf.Max(0.01f, duration));
		if (follow != null) transform.position = follow.position + followOffset;
		if (isLine)
		{
			fill.transform.localScale = new Vector3(lineLength * k, lineWidth, 1);
			fill.transform.localPosition = new Vector3(lineLength * k / 2f, 0, 0);
			edge.color = color.WithAlpha(0.18f + 0.12f * Mathf.Sin(t * 30f));
		}
		else
		{
			fill.transform.localScale = Vector3.one * k;
			edge.color = color.WithAlpha(0.35f + 0.25f * Mathf.Sin(t * 25f));
		}
		if (t >= duration) Destroy(gameObject);
	}
}
