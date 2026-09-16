using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ============================================================================
// [NR] 보스 AI 재작성 (기존 프리팹의 직렬화 필드 이름/값은 그대로 사용)
//
// 기존 문제
//  - 기본 공격 투사체 방향(lockedAttackDirection)이 설정되지 않아 날아가지 않음
//  - 패턴 실행 중에도 추적/기본 공격이 계속됨, 플레이어가 멀리 있어도 패턴 발동
//  - 원형 공격이 예고 없이 즉시 피해
//
// 새 구조
//  대기 → 등장(포효) → [추적 → 행동 선택 → 예고 → 공격 → 빈틈] 반복
//  체력 66%/33%에서 페이즈 전환(무적 + 충격파 + 강화), 계층(tier)이 높을수록 패턴 추가
//  패턴: 조준 연사 / 브레스 / 원형 충격파 / 돌진 / 탄막 링 / 소환
// ============================================================================
public class MonsterAI : MonoBehaviour
{
    public Transform player;
    public float chaseSpeed; // 이속

    [Header("기본공격")]
    public float basicAttackCooldown = 5f; // 기본 공격 쿨타임
    public float attackPreparationTime = 0.5f; // 공격 준비 시간
    public GameObject attackProjectile; // 공격 투사체
    public float projectileSpeed = 25f; // 투사체 속도

    [Header("패턴1")]
    public float pattern1Cooldown = 25f; // 패턴 1 쿨타임
    public GameObject blastProjectile; // 브레스 공격 투사체
    public float blastSpeed = 15f; // 투사체 속도

    [Header("패턴2")]
    public GameObject attackRadiusPrefab; // 공격 범위를 나타낼 프리팹
    public float pattern2PreparationTime = 0.7f; // 공격 준비 시간
    public float pattern2Cooldown = 60f; // 패턴 2 쿨타임
    public int pattern2Damage = 110; // 패턴 2 데미지

    [Header("[NR] 보스 설정")]
    public float activationRange = 11f;
    public int tier = 1;
    public string bossName = "악몽의 수문장";
    public float damageMultiplier = 1f;

    public bool IsInvulnerable { get; private set; }
    public int Phase { get; private set; } = 1;
    public bool Engaged { get; private set; }

    private Animator animator;
    private Rigidbody2D rb;
    private MonsterHP hp;
    private Player playerScript;
    private SpriteRenderer sr;
    private bool dead;
    private float speedMul = 1f;
    private float cooldownMul = 1f;
    private readonly Dictionary<string, float> nextTime = new Dictionary<string, float>();
    private readonly List<GameObject> minions = new List<GameObject>();

    void Awake()
    {
        var p = FindObjectOfType<Player>();
        if (p != null) { player = p.transform; playerScript = p; }
        rb = GetComponent<Rigidbody2D>();
        hp = GetComponent<MonsterHP>();
        sr = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        animator = GetComponent<Animator>();
        StartCoroutine(Brain());
    }

    /// <summary>계층 정보 적용 (NRRun.ConfigureBoss)</summary>
    public void Configure(NRFloorDef def)
    {
        tier = def.bossTier;
        bossName = def.bossName;
        damageMultiplier = def.bossAtk;
        chaseSpeed = Mathf.Max(chaseSpeed, 2.2f) * (1f + 0.08f * (tier - 1));
    }

    Vector2 Center => (Vector2)transform.position + new Vector2(0, 0.6f);
    Vector2 PlayerPos => player != null ? (Vector2)player.position + new Vector2(0, 0.2f) : Center;
    Vector2 ToPlayer => PlayerPos - Center;
    float Dist => ToPlayer.magnitude;
    bool PlayerAlive => playerScript != null && !playerScript.isDead;

    bool Ready(string key) => !nextTime.TryGetValue(key, out var t) || Time.time >= t;
    void Cooldown(string key, float seconds) => nextTime[key] = Time.time + seconds * cooldownMul;

    void Trigger(string name)
    {
        if (animator != null && animator.runtimeAnimatorController != null) animator.SetTrigger(name);
    }

    void SetWalking(bool v)
    {
        if (animator != null && animator.runtimeAnimatorController != null) animator.SetBool("IsWalking", v);
    }

    // ---------------------------------------------------------------------
    IEnumerator Brain()
    {
        // 대기: 플레이어가 가까이 올 때까지
        while (!Engaged)
        {
            if (player != null && PlayerAlive && Dist < activationRange) Engaged = true;
            else yield return new WaitForSeconds(0.2f);
        }

        yield return Intro();

        Cooldown("volley", 1.2f);
        Cooldown("breath", 4f);
        Cooldown("shock", 6f);
        Cooldown("charge", 5f);
        Cooldown("ring", 7f);
        Cooldown("summon", 10f);

        while (!dead)
        {
            if (!PlayerAlive || NRRun.Transitioning)
            {
                Stop();
                yield return new WaitForSeconds(0.3f);
                continue;
            }

            CheckPhase();
            if (IsInvulnerable) { yield return null; continue; }

            var action = ChooseAction();
            if (action != null)
            {
                SetWalking(false);
                Stop();
                yield return action;
                // 공격 후 빈틈 (플레이어가 반격할 시간)
                yield return new WaitForSeconds(Random.Range(0.55f, 0.9f) * (Phase >= 3 ? 0.75f : 1f));
                continue;
            }

            // 추적
            Face(ToPlayer.x);
            SetWalking(true);
            Vector2 dir = ToPlayer.normalized;
            if (Dist > 2.2f) rb.velocity = dir * chaseSpeed * speedMul * NREnemyStatus.SpeedMul(this);
            else rb.velocity = new Vector2(-dir.y, dir.x) * chaseSpeed * 0.4f;
            yield return new WaitForFixedUpdate();
        }
    }

    IEnumerator ChooseAction()
    {
        float d = Dist;
        var options = new List<(IEnumerator routine, float weight)>();

        if (Ready("volley") && d < 9f) options.Add((AimedVolley(), 3f));
        if (Ready("breath") && Mathf.Abs(ToPlayer.y) < 2.2f && d < 8f) options.Add((Breath(), 2f));
        if (Ready("shock") && d < 3.2f) options.Add((Shockwave(), 3.5f));
        if ((tier >= 2 || Phase >= 2) && Ready("charge") && d > 3f && d < 9f) options.Add((Charge(), 2.5f));
        if ((tier >= 2 || Phase >= 2) && Ready("ring")) options.Add((RingBarrage(), 1.8f));
        if ((tier >= 3 || Phase >= 3) && Ready("summon")) options.Add((Summon(), 1.5f));

        if (options.Count == 0) return null;
        float total = 0f;
        foreach (var o in options) total += o.weight;
        float r = Random.value * total;
        foreach (var o in options)
        {
            r -= o.weight;
            if (r <= 0f) return o.routine;
        }
        return options[0].routine;
    }

    void Stop() { if (rb != null) rb.velocity = Vector2.zero; }

    void Face(float x)
    {
        Vector3 scale = transform.localScale;
        scale.x = x < 0 ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
        transform.localScale = scale;
    }

    // ---------------------------------------------------------------------
    IEnumerator Intro()
    {
        IsInvulnerable = true;
        Stop();
        Face(ToPlayer.x);
        NRHud.ShowBoss(this, hp);
        NRAudio.PlayMusic("boss_bgm");
        Trigger("PrepareAttack");
        NRCombatFX.Shake(0.8f, 0.2f);
        var def = NRRun.Current;
        NRUIRoot.Banner(bossName, def.bossTitle, NRPalette.Crimson, 1.6f);
        NRCombatFX.DeathBurst(Center, NRPalette.Crimson, 30);
        yield return new WaitForSeconds(1.6f);
        IsInvulnerable = false;
    }

    void CheckPhase()
    {
        if (hp == null || hp.maxHP <= 0) return;
        float ratio = (float)hp.nowHP / hp.maxHP;
        int target = ratio <= 0.33f ? 3 : ratio <= 0.66f ? 2 : 1;
        if (tier == 1 && target == 3) target = 2;   // 1계층 보스는 2페이즈까지
        if (target > Phase) StartCoroutine(PhaseShift(target));
    }

    IEnumerator PhaseShift(int newPhase)
    {
        Phase = newPhase;
        IsInvulnerable = true;
        Stop();
        Trigger("PrepareAttack");
        NRCombatFX.Shake(0.7f, 0.3f);
        NRUIRoot.Banner(bossName + NRUtil.Josa(bossName, "이", "가") + " 격노한다", "페이즈 " + newPhase, NRPalette.Crimson, 1.2f);
        speedMul = 1f + 0.15f * (newPhase - 1);
        cooldownMul = 1f - 0.15f * (newPhase - 1);
        if (sr != null) NRCombatFX.Flash(sr, NRPalette.Crimson, 0.5f);

        // 밀쳐내는 충격파 (피해 없음 + 약한 피해)
        NRTelegraph.Circle(Center, 3f, 0.9f, NRPalette.Crimson);
        yield return new WaitForSeconds(0.9f);
        NRCombatFX.DeathBurst(Center, NRPalette.Crimson, 40);
        if (player != null && Dist < 3f)
        {
            NRStats.DamagePlayer(playerScript, 20f * damageMultiplier, true);
            var prb = player.GetComponent<Rigidbody2D>();
            if (prb != null) prb.velocity += ToPlayer.normalized * 12f;
        }
        yield return new WaitForSeconds(0.4f);
        IsInvulnerable = false;
    }

    // ---- 패턴: 조준 연사 ----
    IEnumerator AimedVolley()
    {
        Cooldown("volley", Mathf.Max(1.8f, basicAttackCooldown * 0.5f));
        Face(ToPlayer.x);
        Trigger("PrepareAttack");
        int count = Phase >= 3 ? 5 : Phase >= 2 || tier >= 2 ? 3 : 1;
        int bursts = tier >= 3 ? 3 : 2;
        var tg = NRTelegraph.Line(Center, ToPlayer.normalized, 7f, 0.25f, attackPreparationTime + 0.2f, NRPalette.Crimson);
        yield return new WaitForSeconds(attackPreparationTime + 0.2f);
        for (int b = 0; b < bursts && !dead; b++)
        {
            Trigger("Attack");
            Vector2 baseDir = ToPlayer.normalized;
            for (int i = 0; i < count; i++)
            {
                float angle = (i - (count - 1) / 2f) * 14f;
                FireProjectile(attackProjectile, Quaternion.Euler(0, 0, angle) * baseDir, 8f + tier, 3f, 10f, NRPalette.Crimson);
            }
            yield return new WaitForSeconds(0.35f);
        }
    }

    // ---- 패턴: 브레스 (수평) ----
    IEnumerator Breath()
    {
        Cooldown("breath", Mathf.Max(5f, pattern1Cooldown * 0.35f));
        Face(ToPlayer.x);
        Vector2 dir = new Vector2(Mathf.Sign(ToPlayer.x == 0 ? 1 : ToPlayer.x), 0);
        Trigger("Pattern1");
        NRTelegraph.Line(Center, dir, 9f, 1.4f, 0.85f, NRPalette.Crimson);
        if (Phase >= 2)
        {
            NRTelegraph.Line(Center, Quaternion.Euler(0, 0, 25f) * dir, 8f, 1.0f, 0.85f, NRPalette.Crimson);
            NRTelegraph.Line(Center, Quaternion.Euler(0, 0, -25f) * dir, 8f, 1.0f, 0.85f, NRPalette.Crimson);
        }
        yield return new WaitForSeconds(0.85f);
        if (dead) yield break;
        FireProjectile(blastProjectile, dir, blastSpeed * 0.7f, 1.4f, 40f, NRPalette.Pink, 1.4f);
        if (Phase >= 2)
        {
            FireProjectile(blastProjectile, Quaternion.Euler(0, 0, 25f) * dir, blastSpeed * 0.6f, 1.4f, 40f, NRPalette.Pink, 1.0f);
            FireProjectile(blastProjectile, Quaternion.Euler(0, 0, -25f) * dir, blastSpeed * 0.6f, 1.4f, 40f, NRPalette.Pink, 1.0f);
        }
        NRCombatFX.Shake(0.3f, 0.15f);
        yield return new WaitForSeconds(0.3f);
    }

    // ---- 패턴: 원형 충격파 ----
    IEnumerator Shockwave()
    {
        Cooldown("shock", Mathf.Max(4f, pattern2Cooldown * 0.12f));
        Trigger("Pattern2");
        float radius = 2.8f + 0.3f * (tier - 1);
        NRTelegraph.Circle(Center, radius, pattern2PreparationTime + 0.3f, NRPalette.Crimson, transform);
        yield return new WaitForSeconds(pattern2PreparationTime + 0.3f);
        if (dead) yield break;
        NRCombatFX.DeathBurst(Center, NRPalette.Crimson, 34);
        NRCombatFX.Shake(0.35f, 0.25f);
        if (player != null && Dist <= radius)
            NRStats.DamagePlayer(playerScript, pattern2Damage * 0.5f * damageMultiplier, true);

        // 3페이즈: 바깥 고리 2차 충격
        if (Phase >= 3)
        {
            NRTelegraph.Circle(Center, radius + 2.2f, 0.6f, NRPalette.Pink, transform);
            yield return new WaitForSeconds(0.6f);
            float d = Dist;
            if (player != null && d > radius && d <= radius + 2.2f)
                NRStats.DamagePlayer(playerScript, pattern2Damage * 0.4f * damageMultiplier, true);
            NRCombatFX.DeathBurst(Center, NRPalette.Pink, 30);
        }
    }

    // ---- 패턴: 돌진 ----
    IEnumerator Charge()
    {
        Cooldown("charge", 6.5f);
        Face(ToPlayer.x);
        Vector2 dir = ToPlayer.normalized;
        Trigger("PrepareAttack");
        NRTelegraph.Line(Center, dir, 9f, 1.2f, 0.75f, NRPalette.Crimson);
        yield return new WaitForSeconds(0.75f);
        if (dead) yield break;
        Trigger("Attack");
        var hitbox = new GameObject("Boss Charge Hitbox");
        hitbox.layer = 21;
        hitbox.tag = "weapon";
        var col = hitbox.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.9f;
        var hrb = hitbox.AddComponent<Rigidbody2D>();
        hrb.bodyType = RigidbodyType2D.Kinematic;
        var src = hitbox.AddComponent<NRDamageSource>();
        src.damage = 45f * damageMultiplier;
        src.destroyOnHit = false;
        float t = 0f;
        while (t < 0.5f && !dead)
        {
            t += Time.fixedDeltaTime;
            rb.velocity = dir * 15f;
            hitbox.transform.position = Center;
            if (Mathf.Repeat(t, 0.1f) < Time.fixedDeltaTime && sr != null) NRCombatFX.Afterimage(sr, NRPalette.Crimson.WithAlpha(0.4f), 1, 0f);
            yield return new WaitForFixedUpdate();
        }
        Destroy(hitbox);
        Stop();
        NRCombatFX.Shake(0.2f, 0.15f);
    }

    // ---- 패턴: 탄막 링 ----
    IEnumerator RingBarrage()
    {
        Cooldown("ring", 8f);
        Trigger("Pattern2");
        NRTelegraph.Circle(Center, 1.2f, 0.7f, NRPalette.Pink, transform);
        yield return new WaitForSeconds(0.7f);
        int rings = Phase >= 3 ? 3 : 2;
        int bullets = 12 + tier * 2;
        for (int r = 0; r < rings && !dead; r++)
        {
            float offset = r * (180f / bullets);
            for (int i = 0; i < bullets; i++)
            {
                float a = offset + i * 360f / bullets;
                Vector2 d = Quaternion.Euler(0, 0, a) * Vector2.right;
                NRProjectile.Spawn(Center + d * 0.6f, d * (4.2f + 0.4f * tier), 18f * damageMultiplier, NRPalette.Pink, 0.32f, 4f, false);
            }
            yield return new WaitForSeconds(0.45f);
        }
    }

    // ---- 패턴: 소환 ----
    IEnumerator Summon()
    {
        Cooldown("summon", 14f);
        minions.RemoveAll(m => m == null);
        if (minions.Count >= 4) yield break;
        Trigger("PrepareAttack");
        var def = NRRun.Current;
        int count = Phase >= 3 ? 3 : 2;
        for (int i = 0; i < count; i++)
        {
            Vector2 pos = Center + Random.insideUnitCircle.normalized * Random.Range(2.5f, 3.5f);
            if (!NRWaves.IsFree(pos)) pos = Center + Random.insideUnitCircle * 1.5f;
            NRTelegraph.Circle(pos, 0.7f, 0.8f, NRPalette.Pink);
            StartCoroutine(SpawnMinion(pos, i % 2 == 0 ? NREnemyKind.Bomber : NREnemyKind.Assassin, def));
        }
        yield return new WaitForSeconds(0.9f);
    }

    IEnumerator SpawnMinion(Vector2 pos, NREnemyKind kind, NRFloorDef def)
    {
        yield return new WaitForSeconds(0.8f);
        if (dead) yield break;
        var e = NREnemyFactory.Create(kind, pos, def.enemyHp * 0.7f, def.enemyAtk);
        minions.Add(e.gameObject);
    }

    void FireProjectile(GameObject prefab, Vector2 dir, float speed, float life, float fallbackDamage, Color color, float scale = 1f)
    {
        if (prefab != null)
        {
            var go = Instantiate(prefab, Center + dir * 0.6f, Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg));
            if (scale != 1f) go.transform.localScale *= scale;
            var atk = go.GetComponent<MonsterATK>();
            if (atk != null) atk.damage = Mathf.RoundToInt(atk.damage * damageMultiplier);
            var prb = go.GetComponent<Rigidbody2D>();
            if (prb != null) prb.velocity = dir * speed;
            else go.AddComponent<NRMover>().velocity = dir * speed;
            Destroy(go, life);
        }
        else
        {
            NRProjectile.Spawn(Center + dir * 0.6f, dir * speed, fallbackDamage * damageMultiplier, color, 0.45f * scale, life, false);
        }
    }

    // ---- MonsterHP에서 호출 ----
    public void OnDamaged()
    {
        if (!Engaged) Engaged = true;
    }

    public void OnDeath()
    {
        dead = true;
        StopAllCoroutines();
        Stop();
        foreach (var m in minions)
        {
            if (m == null) continue;
            var e = m.GetComponent<NREnemy>();
            if (e != null) e.Despawn();
        }
        NRHud.HideBoss();
    }

    void OnDestroy()
    {
        NRHud.HideBoss(this);
    }
}

/// <summary>Rigidbody가 없는 투사체용 단순 이동</summary>
public class NRMover : MonoBehaviour
{
    public Vector2 velocity;
    void Update() { transform.position += (Vector3)(velocity * Time.deltaTime); }
}
