using System.Collections;
using UnityEngine;

// ============================================================================
// 신규 적 7종 — 각각 다른 대응을 요구하도록 설계 (거미 떼 / 파수꾼 포함)
//  신전 수호자: 느리지만 강한 예고 베기/돌진 → 공격 후 빈틈을 노려라
//  폭탄 드로이드: 달려와 자폭 → 멀리서 처치하거나 구르기로 이탈
//  오브 마법사: 거리를 벌리며 유도 구체/바닥 룬 → 계속 움직이며 접근
//  암살자: 주위를 돌다 순간 돌진 → 예고선을 보고 구르기 타이밍
//  궁수: 조준선 후 고속 화살 → 조준 고정 순간 옆으로 회피, 벽 뒤로
// ============================================================================
public static class NREnemyFactory
{
	public static string KindName(NREnemyKind k)
	{
		switch (k)
		{
			case NREnemyKind.Guardian: return "신전 수호자";
			case NREnemyKind.Bomber: return "폭탄 드로이드";
			case NREnemyKind.Mage: return "오브 마법사";
			case NREnemyKind.Assassin: return "암살자";
			case NREnemyKind.Spider: return "악몽 거미";
			case NREnemyKind.Warden: return "침묵의 파수꾼";
			default: return "궁수";
		}
	}

	public static NREnemy Create(NREnemyKind kind, Vector3 position, float hpMul, float atkMul)
	{
		var go = new GameObject("NR " + KindName(kind));
		go.transform.position = position;
		NREnemy e;
		switch (kind)
		{
			case NREnemyKind.Guardian: e = go.AddComponent<NRGuardian>(); break;
			case NREnemyKind.Bomber: e = go.AddComponent<NRBomber>(); break;
			case NREnemyKind.Mage: e = go.AddComponent<NRMage>(); break;
			case NREnemyKind.Assassin: e = go.AddComponent<NRAssassin>(); break;
			case NREnemyKind.Spider: e = go.AddComponent<NRSpider>(); break;
			case NREnemyKind.Warden: e = go.AddComponent<NRWarden>(); break;
			default: e = go.AddComponent<NRArcher>(); break;
		}
		e.displayName = KindName(kind);
		e.Setup(hpMul, atkMul);
		return e;
	}
}

// ---------------------------------------------------------------------------
public class NRGuardian : NREnemy
{
	NRClip walk, attack, special, death;
	float nextSpecial;
	protected override float BodyHeight => 1.1f;
	protected override NRClip DeathClip => death;

	protected override void Awake()
	{
		maxHP = 280; moveSpeed = 1.35f; damage = 40f; knockbackResist = 0.9f; staggerTime = 0f;
		accent = NRPalette.Cyan;
		base.Awake();
		rb.mass = 6f;
	}

	protected override void ConfigureVisual()
	{
		walk = NRSpriteSheet.Load("Guardian", "walk", 59, "walk", 9, true);
		attack = NRSpriteSheet.Load("Guardian", "attack", 59, "walk", 14, false);
		special = NRSpriteSheet.Load("Guardian", "special", 59, "walk", 14, false);
		death = NRSpriteSheet.Load("Guardian", "death", 59, "walk", 10, false);
		SetBodyScale(3.2f, new Vector2(0.55f, 0.9f), 0.45f);
		anim.Play(walk);
		nextSpecial = Time.time + Random.Range(4f, 7f);
	}

	protected override IEnumerator Brain()
	{
		yield return new WaitForSeconds(0.4f);
		while (!IsDead)
		{
			if (!PlayerAlive) { Stop(); yield return null; continue; }
			float dist = DistToPlayer;

			if (Time.time > nextSpecial && dist > 2.2f && dist < 4.8f && HasLineOfSight(Center, PlayerPos))
			{
				yield return ChargeSlash();
				nextSpecial = Time.time + Random.Range(6f, 9f);
				continue;
			}
			if (dist < 1.7f)
			{
				yield return HeavySlash();
				continue;
			}
			anim.Play(walk);
			MoveDir(ToPlayer.normalized);
			yield return new WaitForFixedUpdate();
		}
	}

	IEnumerator HeavySlash()
	{
		Stop();
		FacePlayer();
		Vector2 dir = facingRight ? Vector2.right : Vector2.left;
		Vector2 hitCenter = Center + dir * 1.0f;
		NRTelegraph.Circle(hitCenter, 1.05f, 0.65f, NRPalette.Crimson);
		anim.Play(attack, true, null, 0.35f);          // 느린 예고 동작
		yield return new WaitForSeconds(0.65f);
		if (IsDead) yield break;
		anim.Play(attack, true, null, 1.4f);
		SpawnHitbox(hitCenter, new Vector2(1.9f, 1.4f), damage, 0.15f);
		NRCombatFX.Shake(0.12f, 0.1f);
		NRCombatFX.Sparks(hitCenter, NRPalette.Cyan, 8);
		yield return new WaitForSeconds(0.9f);         // 빈틈
	}

	IEnumerator ChargeSlash()
	{
		Stop();
		FacePlayer();
		Vector2 dir = ToPlayer.normalized;
		var tg = NRTelegraph.Line(Center, dir, 4.8f, 0.9f, 0.8f, NRPalette.Crimson);
		anim.Play(special, true, null, 0.3f);
		yield return new WaitForSeconds(0.8f);
		if (IsDead) yield break;
		anim.Play(special, true, null, 1.6f);
		var hb = SpawnHitbox(Center, new Vector2(1.2f, 1.2f), damage * 1.2f, 0.45f);
		float t = 0f;
		while (t < 0.32f && !IsDead)
		{
			t += Time.fixedDeltaTime;
			rb.velocity = dir * 14f;
			if (hb != null) hb.transform.position = Center + dir * 0.4f;
			yield return new WaitForFixedUpdate();
		}
		Stop();
		yield return new WaitForSeconds(1.2f);
	}
}

// ---------------------------------------------------------------------------
public class NRBomber : NREnemy
{
	NRClip move, bomb, death;
	bool exploding;
	protected override float BodyHeight => 0.5f;
	protected override NRClip DeathClip => death;

	protected override void Awake()
	{
		maxHP = 60; moveSpeed = 3.4f; damage = 70f; knockbackResist = 0.2f; staggerTime = 0.2f;
		accent = NRPalette.Gold;
		base.Awake();
	}

	protected override void ConfigureVisual()
	{
		move = NRSpriteSheet.Load("Bomber", "move", 77, "move", 12, true);
		bomb = NRSpriteSheet.Load("Bomber", "bomb", 77, "move", 14, false);
		death = NRSpriteSheet.Load("Bomber", "death", 77, "move", 14, false);
		SetBodyScale(4.2f, new Vector2(0.5f, 0.4f), 0.2f);
		anim.Play(move);
	}

	protected override IEnumerator Brain()
	{
		yield return new WaitForSeconds(0.3f);
		while (!IsDead)
		{
			if (!PlayerAlive) { Stop(); yield return null; continue; }
			if (DistToPlayer < 1.4f)
			{
				yield return Explode();
				yield break;
			}
			anim.Play(move);
			// 약간 흔들리며 접근
			Vector2 dir = ToPlayer.normalized;
			Vector2 wobble = new Vector2(-dir.y, dir.x) * Mathf.Sin(Time.time * 6f + GetInstanceID()) * 0.35f;
			MoveDir(dir + wobble);
			yield return new WaitForFixedUpdate();
		}
	}

	IEnumerator Explode()
	{
		exploding = true;
		Stop();
		float radius = 1.75f;
		NRTelegraph.Circle(transform.position, radius, 0.85f, NRPalette.Gold, transform);
		float t = 0f;
		while (t < 0.85f && !IsDead)
		{
			t += Time.deltaTime;
			if (Mathf.Repeat(t, 0.16f) < 0.08f) anim.Flash(0.05f);
			yield return null;
		}
		if (IsDead) yield break;

		anim.Play(bomb, true);
		NRCombatFX.DeathBurst(transform.position, NRPalette.Gold, 26);
		NRCombatFX.Shake(0.25f, 0.2f);
		if (player != null && Vector2.Distance(player.transform.position, transform.position) <= radius + 0.2f)
			NRStats.DamagePlayer(player, damage, true);
		// 주변 적도 휘말림
		foreach (var c in Physics2D.OverlapCircleAll(transform.position, radius))
		{
			var other = c.GetComponentInParent<NREnemy>();
			if (other != null && other != this && !other.IsDead) other.ReceiveDamage(Mathf.RoundToInt(damage * 0.5f), false, NRDamageKind.Explosion);
		}
		yield return new WaitForSeconds(0.2f);
		ReceiveDamage(99999, false, NRDamageKind.Explosion);
	}

	protected override void OnHurt()
	{
		if (!exploding && hp > 0 && HpRatio < 0.5f) moveSpeed *= 1.05f;
	}
}

// ---------------------------------------------------------------------------
public class NRMage : NREnemy
{
	NRClip idle, move, attack, death;
	float nextCast;
	float nextBlink;
	protected override float BodyHeight => 0.8f;
	protected override NRClip DeathClip => death;

	protected override void Awake()
	{
		maxHP = 120; moveSpeed = 1.7f; damage = 26f; knockbackResist = 0.3f; staggerTime = 0.15f;
		accent = NRPalette.Anxiety;
		base.Awake();
	}

	protected override void ConfigureVisual()
	{
		idle = NRSpriteSheet.Load("Mage", "idle", 34, "idle", 6, true);
		move = NRSpriteSheet.Load("Mage", "move", 34, "idle", 10, true);
		attack = NRSpriteSheet.Load("Mage", "attack", 34, "idle", 16, false);
		death = NRSpriteSheet.Load("Mage", "death", 34, "idle", 10, false);
		SetBodyScale(3.4f, new Vector2(0.45f, 0.75f), 0.38f);
		anim.Play(idle);
		nextCast = Time.time + Random.Range(1.2f, 2.2f);
	}

	protected override IEnumerator Brain()
	{
		yield return new WaitForSeconds(0.5f);
		while (!IsDead)
		{
			if (!PlayerAlive) { Stop(); anim.Play(idle); yield return null; continue; }
			float dist = DistToPlayer;

			if (dist < 2.6f && Time.time > nextBlink)
			{
				yield return Blink();
				continue;
			}
			if (Time.time > nextCast && dist < 6.5f)
			{
				yield return Random.value < 0.55f ? CastOrbs() : CastRune();
				nextCast = Time.time + Random.Range(2.6f, 3.6f);
				continue;
			}

			// 3~5 거리 유지
			Vector2 dir = ToPlayer.normalized;
			Vector2 desire = dist < 3f ? -dir : dist > 5f ? dir : new Vector2(-dir.y, dir.x) * Mathf.Sign(Mathf.Sin(Time.time * 0.7f + GetInstanceID()));
			anim.Play(move);
			MoveDir(desire, dist < 3f ? 1.2f : 0.8f);
			FacePlayer();
			yield return new WaitForFixedUpdate();
		}
	}

	IEnumerator CastOrbs()
	{
		Stop();
		FacePlayer();
		anim.Play(attack, true, null, 0.8f);
		var glow = NRTelegraph.Circle(Center + Vector2.up * 0.5f, 0.45f, 0.6f, NRPalette.Anxiety, transform);
		yield return new WaitForSeconds(0.6f);
		if (IsDead) yield break;
		Vector2 baseDir = ToPlayer.normalized;
		for (int i = -1; i <= 1; i++)
		{
			Vector2 d = Quaternion.Euler(0, 0, i * 22f) * baseDir;
			var p = SpawnProjectile(Center + Vector2.up * 0.4f, d * 3.3f, damage, NRPalette.Anxiety, 0.4f, 2.4f);
			p.homingTarget = player != null ? player.transform : null;
			p.turnRate = 70f;
		}
		yield return new WaitForSeconds(0.5f);
	}

	IEnumerator CastRune()
	{
		Stop();
		FacePlayer();
		anim.Play(attack, true, null, 0.7f);
		Vector2 target = PlayerPos;
		NRTelegraph.Circle(target, 1.3f, 1.05f, NRPalette.Anxiety);
		yield return new WaitForSeconds(1.05f);
		if (IsDead) yield break;
		NRCombatFX.DeathBurst(target, NRPalette.Anxiety, 16);
		if (player != null && Vector2.Distance((Vector2)player.transform.position, target) < 1.4f)
			NRStats.DamagePlayer(player, damage * 1.5f, true);
		yield return new WaitForSeconds(0.4f);
	}

	IEnumerator Blink()
	{
		nextBlink = Time.time + 6f;
		Stop();
		NRCombatFX.DeathBurst(Center, NRPalette.Anxiety, 12);
		sr.enabled = false;
		yield return new WaitForSeconds(0.25f);
		Vector2 away = -ToPlayer.normalized;
		Vector2 target = (Vector2)transform.position + away * 3.5f;
		for (int i = 0; i < 6; i++)
		{
			Vector2 cand = (Vector2)transform.position + (Vector2)(Quaternion.Euler(0, 0, i * 50f) * away) * 3.5f;
			if (NRWaves.IsFree(cand)) { target = cand; break; }
		}
		if (NRWaves.IsFree(target)) rb.position = target;
		sr.enabled = true;
		NRCombatFX.DeathBurst(Center, NRPalette.Anxiety, 12);
		yield return new WaitForSeconds(0.3f);
	}
}

// ---------------------------------------------------------------------------
public class NRAssassin : NREnemy
{
	NRClip idle, run, attack, death;
	float nextDash;
	int orbitSign = 1;
	protected override float BodyHeight => 0.7f;
	protected override NRClip DeathClip => death;

	protected override void Awake()
	{
		maxHP = 90; moveSpeed = 3.6f; damage = 30f; knockbackResist = 0.1f; staggerTime = 0.18f;
		accent = NRPalette.Pink;
		base.Awake();
	}

	protected override void ConfigureVisual()
	{
		idle = NRSpriteSheet.Load("Assassin", "idle", 19, "idle", 10, true);
		run = NRSpriteSheet.Load("Assassin", "run", 19, "idle", 14, true);
		attack = NRSpriteSheet.Load("Assassin", "attack", 19, "idle", 16, false);
		death = NRSpriteSheet.Load("Assassin", "death", 19, "idle", 12, false);
		SetBodyScale(6.5f, new Vector2(0.4f, 0.65f), 0.32f);
		anim.Play(idle);
		nextDash = Time.time + Random.Range(1.5f, 2.5f);
		orbitSign = Random.value < 0.5f ? 1 : -1;
	}

	protected override IEnumerator Brain()
	{
		yield return new WaitForSeconds(0.3f);
		while (!IsDead)
		{
			if (!PlayerAlive) { Stop(); anim.Play(idle); yield return null; continue; }
			float dist = DistToPlayer;
			if (Time.time > nextDash && dist < 4.5f && HasLineOfSight(Center, PlayerPos))
			{
				yield return Dash();
				nextDash = Time.time + Random.Range(2.4f, 3.4f);
				if (Random.value < 0.4f) orbitSign = -orbitSign;
				continue;
			}
			// 3~4 거리에서 주위를 돈다
			Vector2 dir = ToPlayer.normalized;
			Vector2 tangent = new Vector2(-dir.y, dir.x) * orbitSign;
			Vector2 radial = dist > 4f ? dir : dist < 3f ? -dir : Vector2.zero;
			anim.Play(run);
			MoveDir(tangent * 0.8f + radial);
			yield return new WaitForFixedUpdate();
		}
	}

	IEnumerator Dash()
	{
		Stop();
		FacePlayer();
		Vector2 dir = ToPlayer.normalized;
		var tg = NRTelegraph.Line(Center, dir, 4.2f, 0.55f, 0.45f, NRPalette.Pink);
		float t = 0f;
		while (t < 0.45f && !IsDead)
		{
			t += Time.deltaTime;
			sr.color = sr.color.WithAlpha(Mathf.Repeat(t, 0.1f) < 0.05f ? 0.35f : 1f);
			yield return null;
		}
		sr.color = sr.color.WithAlpha(1f);
		if (IsDead) yield break;
		anim.Play(attack, true);
		var hb = SpawnHitbox(Center, new Vector2(0.9f, 0.9f), damage, 0.32f);
		NRCombatFX.Afterimage(sr, NRPalette.Pink.WithAlpha(0.5f), 5, 0.05f);
		t = 0f;
		while (t < 0.24f && !IsDead)
		{
			t += Time.fixedDeltaTime;
			rb.velocity = dir * 17f;
			if (hb != null) hb.transform.position = Center;
			yield return new WaitForFixedUpdate();
		}
		Stop();
		anim.Play(idle);
		yield return new WaitForSeconds(0.7f);
	}
}

// ---------------------------------------------------------------------------
public class NRArcher : NREnemy
{
	NRClip idle, run, attack, death;
	float nextShot;
	protected override float BodyHeight => 0.7f;
	protected override NRClip DeathClip => death;

	protected override void Awake()
	{
		maxHP = 80; moveSpeed = 2.1f; damage = 34f; knockbackResist = 0.2f; staggerTime = 0.2f;
		accent = NRPalette.Green;
		base.Awake();
	}

	protected override void ConfigureVisual()
	{
		idle = NRSpriteSheet.Load("Archer", "idle", 25, "run", 10, true);
		run = NRSpriteSheet.Load("Archer", "run", 25, "run", 12, true);
		attack = NRSpriteSheet.Load("Archer", "attack", 25, "run", 14, false);
		death = NRSpriteSheet.Load("Archer", "death", 25, "run", 10, false);
		SetBodyScale(5f, new Vector2(0.4f, 0.65f), 0.32f);
		anim.Play(idle);
		nextShot = Time.time + Random.Range(1.5f, 2.5f);
	}

	protected override IEnumerator Brain()
	{
		yield return new WaitForSeconds(0.4f);
		while (!IsDead)
		{
			if (!PlayerAlive) { Stop(); anim.Play(idle); yield return null; continue; }
			float dist = DistToPlayer;
			bool los = HasLineOfSight(Center, PlayerPos);

			if (Time.time > nextShot && dist < 7f && los)
			{
				yield return AimAndShoot();
				nextShot = Time.time + Random.Range(2.2f, 3.0f);
				continue;
			}
			Vector2 dir = ToPlayer.normalized;
			Vector2 desire = dist < 3.5f ? -dir : dist > 5.5f || !los ? dir : new Vector2(-dir.y, dir.x);
			anim.Play(run);
			MoveDir(desire, dist < 3.5f ? 1.1f : 0.8f);
			yield return new WaitForFixedUpdate();
		}
	}

	IEnumerator AimAndShoot()
	{
		Stop();
		FacePlayer();
		anim.Play(idle);
		var tg = NRTelegraph.Line(Center, ToPlayer.normalized, 7.5f, 0.08f, 1.05f, NRPalette.Green);
		float t = 0f;
		Vector2 dir = ToPlayer.normalized;
		while (t < 0.8f && !IsDead)            // 조준 추적
		{
			t += Time.deltaTime;
			dir = ToPlayer.normalized;
			if (tg != null) { tg.transform.position = Center; tg.SetDirection(dir); }
			FacePlayer();
			yield return null;
		}
		if (IsDead) yield break;
		yield return new WaitForSeconds(0.25f);  // 조준 고정 (피할 타이밍)
		if (IsDead) yield break;
		anim.Play(attack, true, null, 1.5f);
		SpawnProjectile(Center + dir * 0.4f, dir * 12f, damage, NRPalette.Green, 0.3f, 0.65f, true);
		yield return new WaitForSeconds(0.45f);
	}
}

// ---------------------------------------------------------------------------
// 악몽 거미: 약하고 빠른 떼 몬스터. 3마리씩 등장해 짧게 도약하며 문다
//  → 광역 공격(콤보 마무리/궁극기)으로 한꺼번에 정리
// ---------------------------------------------------------------------------
public class NRSpider : NREnemy
{
	const string Sheet = "NR/Sprites/Enemies/Spider/sheet";
	NRClip idle, walk, attack, death;
	float nextLeap;
	protected override float BodyHeight => 0.35f;
	protected override NRClip DeathClip => death;

	protected override void Awake()
	{
		maxHP = 38; moveSpeed = 2.9f; damage = 12f; knockbackResist = 0f; staggerTime = 0.25f;
		accent = NRPalette.Sorrow;
		base.Awake();
	}

	protected override void ConfigureVisual()
	{
		idle = NRSpriteSheet.LoadGridRow(Sheet, 43, 19, 0, 1, 6, true);
		walk = NRSpriteSheet.LoadGridRow(Sheet, 43, 19, 1, 1, 14, true);
		attack = NRSpriteSheet.LoadGridRow(Sheet, 43, 19, 2, 1, 16, false);
		death = NRSpriteSheet.LoadGridRow(Sheet, 43, 19, 3, 1, 12, false);
		SetBodyScale(3.2f, new Vector2(0.55f, 0.35f), 0.15f);
		anim.Play(walk.Length > 0 ? walk : idle);
		nextLeap = Time.time + Random.Range(0.8f, 1.8f);
	}

	protected override IEnumerator Brain()
	{
		yield return new WaitForSeconds(Random.Range(0.2f, 0.5f));
		while (!IsDead)
		{
			if (!PlayerAlive) { Stop(); anim.Play(idle); yield return null; continue; }
			float dist = DistToPlayer;
			if (Time.time > nextLeap && dist < 2.6f && HasLineOfSight(Center, PlayerPos))
			{
				yield return Leap();
				nextLeap = Time.time + Random.Range(1.4f, 2.2f);
				continue;
			}
			Vector2 dir = ToPlayer.normalized;
			// 떼가 한 줄로 겹치지 않도록 좌우로 흩어지며 접근
			Vector2 side = new Vector2(-dir.y, dir.x) * Mathf.Sin(Time.time * 4f + GetInstanceID() * 0.37f) * 0.6f;
			anim.Play(walk);
			MoveDir(dir + side);
			yield return new WaitForFixedUpdate();
		}
	}

	IEnumerator Leap()
	{
		Stop();
		FacePlayer();
		Vector2 dir = ToPlayer.normalized;
		NRTelegraph.Line(Center, dir, 2.4f, 0.35f, 0.4f, NRPalette.Sorrow);
		anim.Play(idle);
		yield return new WaitForSeconds(0.4f);
		if (IsDead) yield break;
		anim.Play(attack, true);
		var hb = SpawnHitbox(Center, new Vector2(0.55f, 0.45f), damage, 0.22f);
		float t = 0f;
		while (t < 0.2f && !IsDead)
		{
			t += Time.fixedDeltaTime;
			rb.velocity = dir * 11f;
			if (hb != null) hb.transform.position = Center;
			yield return new WaitForFixedUpdate();
		}
		Stop();
		yield return new WaitForSeconds(0.5f);
	}
}

// ---------------------------------------------------------------------------
// 침묵의 파수꾼: 체력이 매우 높고 공격력은 낮은 탱커. 밀리지 않고 느리게 압박하며
// 주변을 짓누르는 작은 충격파 → 다른 적을 처리할 동안 길을 막는 역할
// ---------------------------------------------------------------------------
public class NRWarden : NREnemy
{
	const string Sheet = "NR/Sprites/Enemies/Warden/sheet";
	NRClip walk, slam, swing, death;
	float nextSlam;
	protected override float BodyHeight => 1.1f;
	protected override NRClip DeathClip => death;

	protected override void Awake()
	{
		maxHP = 560; moveSpeed = 1.05f; damage = 14f; knockbackResist = 1f; staggerTime = 0f;
		accent = NRPalette.TextDim;
		base.Awake();
		rb.mass = 30f;
	}

	protected override void ConfigureVisual()
	{
		walk = NRSpriteSheet.LoadGridRow(Sheet, 97, 32, 0, 0, 8, true);
		slam = NRSpriteSheet.LoadGridRow(Sheet, 97, 32, 1, 0, 12, false);
		swing = NRSpriteSheet.LoadGridRow(Sheet, 97, 32, 2, 0, 14, false, 100f, 12);
		death = NRSpriteSheet.LoadGridRow(Sheet, 97, 32, 1, 0, 8, false);
		SetBodyScale(4.2f, new Vector2(0.75f, 1.05f), 0.5f);
		anim.Play(walk);
		nextSlam = Time.time + Random.Range(2.5f, 4f);
	}

	protected override IEnumerator Brain()
	{
		yield return new WaitForSeconds(0.5f);
		while (!IsDead)
		{
			if (!PlayerAlive) { Stop(); yield return null; continue; }
			float dist = DistToPlayer;
			if (dist < 1.9f && Time.time > nextSlam)
			{
				yield return Slam();
				nextSlam = Time.time + Random.Range(2.8f, 3.8f);
				continue;
			}
			if (dist < 1.5f)
			{
				yield return Swing();
				continue;
			}
			anim.Play(walk);
			MoveDir(ToPlayer.normalized);
			yield return new WaitForFixedUpdate();
		}
	}

	IEnumerator Swing()
	{
		Stop();
		FacePlayer();
		Vector2 dir = facingRight ? Vector2.right : Vector2.left;
		Vector2 hit = Center + dir * 0.9f;
		NRTelegraph.Circle(hit, 0.8f, 0.55f, NRPalette.TextDim);
		anim.Play(swing, true, null, 0.5f);
		yield return new WaitForSeconds(0.55f);
		if (IsDead) yield break;
		anim.Play(swing, true, null, 1.5f);
		SpawnHitbox(hit, new Vector2(1.5f, 1.1f), damage, 0.15f);
		yield return new WaitForSeconds(0.9f);
	}

	IEnumerator Slam()
	{
		Stop();
		const float radius = 2.1f;
		NRTelegraph.Circle(transform.position, radius, 0.9f, NRPalette.Crimson, transform);
		anim.Play(slam, true, null, 0.8f);
		yield return new WaitForSeconds(0.9f);
		if (IsDead) yield break;
		NRCombatFX.Shake(0.18f, 0.15f);
		NRCombatFX.DeathBurst(transform.position, NRPalette.TextDim, 18);
		if (player != null && Vector2.Distance(player.transform.position, transform.position) <= radius + 0.1f)
			NRStats.DamagePlayer(player, damage * 1.3f, true);
		yield return new WaitForSeconds(1.0f);
	}

	protected override void OnHurt()
	{
		// 맞아도 밀리지 않음
		rb.velocity = Vector2.zero;
	}
}
