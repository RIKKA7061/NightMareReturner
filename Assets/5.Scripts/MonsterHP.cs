using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// [NR] 변경 요약
//  - 체력 0 판정 수정(nowHP < 0 → <= 0), 중복 사망 방지
//  - SetDefault()가 코루틴을 실행하지 않던 문제 수정
//  - 증강 피해 계산, 페이즈 전환 중 무적, 피격 번쩍임, 사망 연출 후 계층 진행(NRRun)
public class MonsterHP : MonoBehaviour, INRDamageable
{
    private Player player;  //플레이어
    private Rigidbody2D rb; //중력
    private PlayerAction playerAction;
    private PrefabSpawner prefabSpawner;
    private UI_MonsterHP uI_MonsterHP; // ui에 뿌려주는 hp바
    private DamageTextShow damageTextShow; // ui 데미지 수치를 표기해주는거 관련 스크립트
    private GoHomeManager goHomeManager;
    private MonsterAI monsterAI;

	[Header("체력")]
    public int maxHP; // 최대 체력 변수
    public int nowHP; // 현재 체력 변수

    [Header("이것이 보스인가?")]
    public bool isBoss;

    [Header("이것이 중간보스인가?")]
    public bool middleBoss;

    Rigidbody2D rigid;
    SpriteRenderer spriter;

    public int defaultMaxHp;

    private Animator animator;
    bool dead = false;

    public bool IsDead => dead;
    bool INRDamageable.IsBoss => true;
    public float HpRatio => maxHP > 0 ? Mathf.Clamp01((float)nowHP / maxHP) : 0f;
    public Transform DamageAnchor => transform;

    void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        spriter = GetComponent<SpriteRenderer>();
        var p = FindObjectOfType<Player>();
        playerAction = p != null ? p.GetComponent<PlayerAction>() : null;
        prefabSpawner = FindAnyObjectByType<PrefabSpawner>();
		uI_MonsterHP = GetComponent<UI_MonsterHP>();
		damageTextShow = GetComponent<DamageTextShow>();// 같은 컴포넌트에 속해있다.
        goHomeManager = FindAnyObjectByType<GoHomeManager>();
        monsterAI = GetComponent<MonsterAI>();
    }

    private void SetEnemyStatus(int _maxHP)
    {
        maxHP = _maxHP;
        nowHP = _maxHP;
    }

    IEnumerator SetDefaultEnemyStat()
    {
        yield return new WaitForSeconds(2.0f);
        SetEnemyStatus(defaultMaxHp);
    }


    public void SetDefault()
    {
        // [NR] 기존에는 StartCoroutine 없이 호출되어 아무 일도 하지 않았음.
        // 계층별 체력은 NRRun.ConfigureBoss에서 설정하므로 여기서는 기본값만 기록
        defaultMaxHp = maxHP;
    }

    void Start()
    {
        animator = GetComponent<Animator>();
        defaultMaxHp = maxHP;
        rb = GetComponent<Rigidbody2D>();
        player = FindObjectOfType<Player>();// 무조건 해줘야됨 (초기화)
        SetEnemyStatus(maxHP);              // 체력 수치 설정
    }

    // 접촉시
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (dead) return;
        // 그 대상이 플레이어 태그일시 && 플레이어가 공격중일시 && 공격 판정(트리거)일 때
        if (other.CompareTag("Player") && other.isTrigger && playerAction != null && playerAction.isAtking)
        {
            NRStats.PlayerHitEnemy(this, this);
        }
    }

    public void ReceiveDamage(int amount, bool crit, NRDamageKind kind)
    {
        if (dead || amount <= 0) return;
        if (monsterAI != null && monsterAI.IsInvulnerable)
        {
            NRCombatFX.Number(transform.position + Vector3.up * 1.6f, "무적", NRPalette.TextDim, 0.8f);
            return;
        }
        nowHP -= amount;
        NRCombatFX.DamageNumber(transform.position + Vector3.up * 1.4f, amount, crit, kind);
        if (spriter != null && kind != NRDamageKind.Burn) NRCombatFX.Flash(spriter, new Color(1f, 0.55f, 0.55f, 1f), 0.07f);
        if (monsterAI != null) monsterAI.OnDamaged();

        if (nowHP <= 0 && middleBoss) // 중간보스
        {
            MiddleBossDead();
		}
        else if (nowHP <= 0)
        {
            nowHP = 0;
            BossDead();
        }
    }

    void BossDead()
    {
        if (dead) return;
        dead = true;
        // 죽기 애니메이션 활성화
        if (animator != null) animator.SetTrigger("Die");
        if (uI_MonsterHP != null) uI_MonsterHP.DestroyHP_UI();
        Destroy(gameObject, 1.2f);
    }

    void EnemyDead()
    {
        nowHP = 0;
        if (isBoss == false)
        {
            Destroy(gameObject);
		}
    }

	void MiddleBossDead()
	{
		if (dead) return;
		dead = true;
		nowHP = 0;
		if (monsterAI != null) monsterAI.OnDeath();
		if (uI_MonsterHP != null) uI_MonsterHP.DestroyHP_UI();// 체력바 삭제
		NRStats.OnEnemyKilled(transform.position, true);
		StartCoroutine(DeathSequence());
	}

	// [NR] 사망 애니메이션을 보여준 뒤 계층 진행
	IEnumerator DeathSequence()
	{
		var col = GetComponents<Collider2D>();
		foreach (var c in col) c.enabled = false;
		if (rigid != null) rigid.velocity = Vector2.zero;
		if (animator != null) animator.SetTrigger("Die");
		NRRun.OnBossDefeated(transform.position);
		yield return new WaitForSecondsRealtime(1.6f);
		float t = 0f;
		while (t < 0.6f && spriter != null)
		{
			t += Time.unscaledDeltaTime;
			spriter.color = spriter.color.WithAlpha(1f - t / 0.6f);
			yield return null;
		}
		Destroy(gameObject);    // 자기 자신을 삭제
	}

	void OnDestroy()
	{
		if (uI_MonsterHP != null) uI_MonsterHP.DestroyHP_UI();
	}
}
