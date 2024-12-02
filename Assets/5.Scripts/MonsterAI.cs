using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterAI : MonoBehaviour
{
    public Transform player;
    public float chaseSpeed; // 이속

    [Header("기본공격")]
    public float basicAttackCooldown = 5f; // 기본 공격 쿨타임
    public float attackPreparationTime = 0.5f; // 공격 준비 시간
                                               //public int damage = 10; // 공격 데미지
    public GameObject attackProjectile; // 공격 투사체
    public float projectileSpeed = 25f; // 투사체 속도

    private bool isAttacking = false; // 현재 공격 중인지 확인
    private bool playerInRange = false; // 플레이어가 범위 내에 있는지 확인
    private bool isPreparingAttack = false; // 공격 준비 중인지 확인
    private bool isPerformingAction = false; // 현재 행동(기본 공격/패턴 등) 중인지 확인

    private Vector2 lockedAttackDirection; // 공격 준비 시 고정된 공격 방향
    private bool canMove = true; // 몬스터가 움직일 수 있는지 여부

    [Header("패턴1")]
    public float pattern1Cooldown = 25f; // 패턴 1 쿨타임
    public GameObject blastProjectile; // 브레스 공격 투사체
    public float blastSpeed = 15f; // 투사체 속도

    [Header("패턴2")]
    public GameObject attackRadiusPrefab; // 공격 범위를 나타낼 프리팹
    public float pattern2PreparationTime = 0.7f; // 공격 준비 시간
    public float pattern2Cooldown = 60f; // 패턴 2 쿨타임
    public int pattern2Damage = 110; // 패턴 2 데미지

    private bool isPattern1Active = false; // 패턴 1 활성화 상태
    private bool isPattern2Active = false; // 패턴 2 활성화 상태

    private float nextBasicAttackTime = 2f; // 다음 기본 공격 가능 시간
    private float nextPattern1Time = 12f; // 다음 패턴1 가능 시간
    private float nextPattern2Time = 25f; // 다음 패턴2 가능 시간

    private enum MonsterState { Idle, BasicAttack, Pattern1, Pattern2 } // 몬스터 상태
    private MonsterState currentState = MonsterState.Idle; // 현재 상태

    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("애니메이터 실행 안됨");
        }
    }

    private void Awake()
    {
        player = FindObjectOfType<Player>().transform;    // 무조건 해줘야됨 (초기화)
    }

    void Update()
    {
        if (isPerformingAction || player == null) return;

        // 좌우 반전 처리
        FlipTowardsPlayer();

        // 패턴 1 또는 패턴 2 실행 우선
        if (Time.time >= nextPattern2Time)
        {
            StartCoroutine(Pattern2_CircularAttack());
        }
        else if (Time.time >= nextPattern1Time)
        {
            StartCoroutine(Pattern1_BlastAttack());
        }
        // 기본 공격은 패턴 중이 아닐 때만 실행
        else if (playerInRange && Time.time >= nextBasicAttackTime && currentState == MonsterState.Idle)
        {
            StartCoroutine(BasicAttack());
        }
        else if (playerInRange)
        {
            ChasePlayer(); // 플레이어 추적
        }
        else
        {
            animator.SetBool("IsWalking", false); // Idle 상태 유지
        }
    }

    void ChasePlayer()
    {
        // 플레이어 방향으로 이동
        Vector2 direction = (player.position - transform.position).normalized;
        transform.position = Vector2.MoveTowards(transform.position, player.position, chaseSpeed * Time.deltaTime);


        // 걷기 애니메이션 활성화
        animator.SetBool("IsWalking", true);
    }

    System.Collections.IEnumerator BasicAttack()
    {
        isPerformingAction = true;
        currentState = MonsterState.BasicAttack;

        nextBasicAttackTime = Time.time + basicAttackCooldown; // 다음 기본 공격 시간 설정
        Debug.Log($"다음 기본공격 시간: {nextBasicAttackTime}, 현재 남은시간: {Time.time}");

        // 기본 공격 준비 애니메이션 실행
        animator.SetTrigger("PrepareAttack");
        yield return new WaitForSeconds(attackPreparationTime);

        // 기본 공격 실행
        animator.SetTrigger("Attack");
        LaunchAttack();

        yield return new WaitForSeconds(0.3f); // 기본 공격 후 대기 시간

        isPerformingAction = false;
        currentState = MonsterState.Idle;
    }

    void LaunchAttack()
    {
        if (attackProjectile != null)
        {
            // 투사체 생성 및 고정된 방향으로 발사
            GameObject projectile = Instantiate(attackProjectile, transform.position, Quaternion.identity);

            Vector2 attackDirection = (player.position - transform.position).normalized;
            float angle = Mathf.Atan2(attackDirection.y, attackDirection.x) * Mathf.Rad2Deg;
            projectile.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

            Rigidbody2D rb = projectile.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.velocity = lockedAttackDirection * projectileSpeed;
            }

            Debug.Log("(최종보스)히히 투사체 발사!");
            Destroy(projectile, 0.5f); // n초 후 투사체 파괴
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            Debug.Log("플레이어가 범위안에 들어옴");
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            Debug.Log("플레이어 범위 밖임");
        }
    }

    System.Collections.IEnumerator Pattern1_BlastAttack()
    {
        isPattern1Active = true;
        isAttacking = true; // 스킬 실행 중 플래그 활성화

        nextPattern1Time = Time.time + pattern1Cooldown; // 다음 패턴1 시간 설정
        Debug.Log($"다음 패턴1 시간: {pattern1Cooldown}, 현재 남은시간: {Time.time}");

        // 공격 패턴 1
        animator.SetTrigger("Pattern1");

        // 플레이어 위치 기반으로 방향 계산
        Vector2 playerDirection = (player.position - transform.position).normalized;

        // 좌/우 판단 (x 축만 기준)
        string direction = playerDirection.x > 0 ? "오른쪽" : "왼쪽";
        Debug.Log($"브레스 공격 준비 {direction} 으로");

        // 0.5초 대기 (준비 시간)
        yield return new WaitForSeconds(0.5f);

        if (blastProjectile != null)
        {
            GameObject blast = Instantiate(blastProjectile, transform.position, Quaternion.identity);
            Rigidbody2D rb = blast.GetComponent<Rigidbody2D>();

            if (rb != null)
            {
                rb.velocity = new Vector2(playerDirection.x, 0).normalized * blastSpeed; // x축으로만 발사
            }

            Debug.Log($"브레스 발사! {direction} 방향으로!");
            Destroy(blast, 1f); // n초 후 투사체 파괴
        }

        // 쿨타임 대기
        yield return new WaitForSeconds(pattern1Cooldown);

        isPattern1Active = false; // 스킬 종료
        isAttacking = false; // 스킬 실행 중 플래그 해제
        isPerformingAction = false;
        currentState = MonsterState.Idle;
    }

    System.Collections.IEnumerator Pattern2_CircularAttack()
    {
        isPattern2Active = true;
        canMove = false; // 이동 불가 상태로 전환

        nextPattern2Time = Time.time + pattern2Cooldown; // 다음 패턴2 시간 설정
        Debug.Log($"다음 패턴2 시간: {pattern2Cooldown}, 현재 남은시간: {Time.time}");

        // 공격 패턴 2
        animator.SetTrigger("Pattern2");

        // 공격 범위 생성
        GameObject attackRadius = Instantiate(attackRadiusPrefab, transform.position, Quaternion.identity);
        attackRadius.transform.localScale = new Vector3(2, 2, 1); // 크기 조정 (필요에 따라 수정)

        // 범위 스크립트에 데미지 설정
        AttackRadius attackScript = attackRadius.GetComponent<AttackRadius>();
        if (attackScript != null)
        {
            attackScript.damage = pattern2Damage; // 패턴 데미지 전달
        }

        Debug.Log("원형 공격 준비 중!");

        // 준비 시간 대기
        yield return new WaitForSeconds(pattern2PreparationTime);

        // 범위 제거
        Destroy(attackRadius);

        Debug.Log("원형 공격 완료!");

        // 쿨타임 대기
        yield return new WaitForSeconds(pattern2Cooldown);

        canMove = true; // 이동 가능 상태로 복구
        isPattern2Active = false;
        isPerformingAction = false;
        currentState = MonsterState.Idle;
    }

    void FlipTowardsPlayer()
    {
        // 현재 몬스터의 localScale을 가져옴
        Vector3 scale = transform.localScale;

        // 플레이어가 왼쪽에 있으면 x축 반전
        if (player.position.x < transform.position.x)
        {
            scale.x = Mathf.Abs(scale.x) * -1; // x축을 음수로 설정
        }
        else
        {
            scale.x = Mathf.Abs(scale.x); // x축을 양수로 설정
        }

        transform.localScale = scale; // 수정된 Scale 값 적용
    }
}

