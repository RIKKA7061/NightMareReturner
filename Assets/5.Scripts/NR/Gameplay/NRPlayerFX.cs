using UnityEngine;

// ============================================================================
// 플레이어 연출: 피격 번쩍임, 무적 시간 깜빡임, 보호막 오라, 발밑 그림자
// ============================================================================
public class NRPlayerFX : MonoBehaviour
{
	Player player;
	SpriteRenderer sr;
	SpriteRenderer shieldAura;
	SpriteRenderer footShadow;
	int lastHp;

	public static void Attach(Player p)
	{
		if (p == null || p.GetComponent<NRPlayerFX>() != null) return;
		p.gameObject.AddComponent<NRPlayerFX>();
	}

	void Start()
	{
		player = GetComponent<Player>();
		sr = GetComponent<SpriteRenderer>();
		lastHp = player != null ? player.nowHP : 0;

		var aura = new GameObject("NR Shield Aura");
		aura.transform.SetParent(transform, false);
		aura.transform.localPosition = new Vector3(0, 0.1f, 0);
		shieldAura = aura.AddComponent<SpriteRenderer>();
		shieldAura.sprite = NRSprites.Glow;
		shieldAura.sortingOrder = (sr != null ? sr.sortingOrder : 0) + 1;
		shieldAura.color = Color.clear;
		ScaleWorld(aura.transform, 1.8f);

		var shadow = new GameObject("NR Foot Glow");
		shadow.transform.SetParent(transform, false);
		shadow.transform.localPosition = new Vector3(0, -0.45f, 0);
		footShadow = shadow.AddComponent<SpriteRenderer>();
		footShadow.sprite = NRSprites.Glow;
		footShadow.sortingOrder = (sr != null ? sr.sortingOrder : 0) - 1;
		footShadow.color = NRPalette.Cyan.WithAlpha(0.12f);
		ScaleWorld(shadow.transform, 1.2f, 0.5f);
	}

	/// <summary>부모 스케일과 상관없이 월드 크기 지정</summary>
	void ScaleWorld(Transform t, float x, float y = -1f)
	{
		if (y < 0) y = x;
		var ps = transform.lossyScale;
		t.localScale = new Vector3(x / Mathf.Max(0.001f, Mathf.Abs(ps.x)), y / Mathf.Max(0.001f, Mathf.Abs(ps.y)), 1f);
	}

	void LateUpdate()
	{
		if (player == null || sr == null) return;

		// 피격 번쩍임
		if (player.nowHP < lastHp && !player.isDead) NRCombatFX.Flash(sr, new Color(1f, 0.35f, 0.4f, 1f), 0.1f);
		lastHp = player.nowHP;

		// 무적 깜빡임 (짧은 무적만, 연출용 장시간 무적은 제외)
		float remain = NRStats.InvulnUntil - Time.time;
		bool blink = remain > 0f && remain < 5f && !player.isDead;
		var c = sr.color;
		c.a = blink ? (Mathf.Repeat(Time.time, 0.12f) < 0.06f ? 0.45f : 1f) : 1f;
		sr.color = c;

		// 보호막 오라
		float target = NRStats.Shield > 0f ? 0.35f + 0.12f * Mathf.Sin(Time.time * 4f) : 0f;
		var a = shieldAura.color;
		float alpha = Mathf.MoveTowards(a.a, target, Time.deltaTime * 2f);
		shieldAura.color = NRPalette.Shield.WithAlpha(alpha);

		footShadow.enabled = !player.isDead;
	}
}
