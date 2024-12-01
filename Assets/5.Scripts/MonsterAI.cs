using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterAI : MonoBehaviour
{
    public Transform player;
    public float chaseSpeed; // 이속
    public float attackCooldown; // 공속
    public float attackPreparationTime = 0.5f; // 공격 준비 시간
                                               //public int damage = 10; // 공격 데미지
    public GameObject attackProjectile; // 공격 투사체
    public float projectileSpeed = 25f; // 투사체 속도

    private bool isAttacking = false; // 현재 공격 중인지 확인
    private bool playerInRange = false; // 플레이어가 범위 내에 있는지 확인
    private bool isPreparingAttack = false; // 공격 준비 중인지 확인

    private Vector2 lockedAttackDirection; // 공격 준비 시 고정된 공격 방향
    private bool canMove = true; // 몬스터가 움직일 수 있는지 여부

    public float pattern1Cooldown = 25f; // 패턴 1 쿨타임
    public GameObject blastProjectile; // 브레스 공격 투사체
    public float blastSpeed = 15f; // 투사체 속도

    public GameObject attackRadiusPrefab; // 공격 범위를 나타낼 프리팹
    public float pattern2PreparationTime = 0.7f; // 공격 준비 시간
    public float pattern2Cooldown = 60f; // 패턴 2 쿨타임
    public int pattern2Damage = 110; // 패턴 2 데미지

    private bool isPattern1Active = false; // 패턴 1 활성화 상태
    private bool isPattern2Active = false; // 패턴 2 활성화 상태

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
        if (player == null) return;


        if (!isPattern1Active && !isPattern2Active && !isAttacking && !isPreparingAttack)
        {
            StartCoroutine(Pattern2_CircularAttack());
        }
        else if (playerInRange && !isAttacking && !isPreparingAttack)
        {
            StartCoroutine(PrepareAndAttack());
        }
        else if (playerInRange && !isAttacking && !isPreparingAttack)
        {
            StartCoroutine(Pattern1_BlastAttack());
        }
        else if (!isAttacking && !isPreparingAttack)
        {
            ChasePlayer();
        }
        else
        {
            // Idle 상태로 전환
            animator.SetBool("IsWalking", false);
        }
    }

    void ChasePlayer()
    {
        Vector2 direction = (player.position - transform.position).normalized;
        transform.position = Vector2.MoveTowards(transform.position, player.position, chaseSpeed * Time.deltaTime);

        // 걷기 애니메이션 활성화
        animator.SetBool("IsWalking", true);
    }

    System.Collections.IEnumerator PrepareAndAttack()
    {
        isPreparingAttack = true;


        // 공격 준비 애니메이션 활성화
        animator.SetTrigger("PrepareAttack");

        // 공격 준비 동안 이동 멈춤
        yield return new WaitForSeconds(attackPreparationTime);

        isAttacking = true; // 기본 공격 중 플래그 활성화
        Debug.Log("공격 준비");

        // 공격 준비 시 플레이어의 현재 위치를 기준으로 방향 고정
        lockedAttackDirection = (player.position - transform.position).normalized;

        // 공격 애니메이션 활성화
        animator.SetTrigger("Attack");

        LaunchAttack();
        yield return new WaitForSeconds(attackCooldown);

        isAttacking = false; // 기본 공격 종료
        isPreparingAttack = false;
    }

    void LaunchAttack()
    {
        if (attackProjectile != null)
        {
            // 투사체 생성 및 고정된 방향으로 발사
            GameObject projectile = Instantiate(attackProjectile, transform.position, Quaternion.identity);
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
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
        }
    }

    System.Collections.IEnumerator Pattern1_BlastAttack()
    {
        isPattern1Active = true;
        isAttacking = true; // 스킬 실행 중 플래그 활성화

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
            Destroy(blast, 3f); // n초 후 투사체 파괴
        }

        // 쿨타임 대기
        yield return new WaitForSeconds(pattern1Cooldown);

        isPattern1Active = false; // 스킬 종료
        isAttacking = false; // 스킬 실행 중 플래그 해제
    }

    System.Collections.IEnumerator Pattern2_CircularAttack()
    {
        isPattern2Active = true;
        canMove = false; // 이동 불가 상태로 전환

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
    }

}

