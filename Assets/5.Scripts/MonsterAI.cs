using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterAI : MonoBehaviour
{
    private Transform player;
    public float chaseSpeed; // 이속
    public float attackCooldown; // 공속
    public float attackPreparationTime = 0.5f; // 공격 준비 시간
    //public int damage = 10; // 공격 데미지
    public GameObject attackProjectile; // 공격 투사체
    public float projectileSpeed = 12f; // 투사체 속도

    private bool isAttacking = false; // 현재 공격 중인지 확인
    private bool playerInRange = false; // 플레이어가 범위 내에 있는지 확인
    private bool isPreparingAttack = false; // 공격 준비 중인지 확인

    private Vector2 lockedAttackDirection; // 공격 준비 시 고정된 공격 방향

	private void Awake()
	{
		player = FindObjectOfType<Player>().transform;    // 무조건 해줘야됨 (초기화)
	}

	void Update()
    {
        if (player == null) return;

        if (playerInRange && !isAttacking && !isPreparingAttack)
        {
            StartCoroutine(PrepareAndAttack());
        }
        else if (!isAttacking && !isPreparingAttack)
        {
            ChasePlayer();
        }
    }

    void ChasePlayer()
    {
        Vector2 direction = (player.position - transform.position).normalized;
        transform.position = Vector2.MoveTowards(transform.position, player.position, chaseSpeed * Time.deltaTime);
    }

    System.Collections.IEnumerator PrepareAndAttack()
    {
        isPreparingAttack = true;
        //Debug.Log("공격 준비");

        // 공격 준비 시 플레이어의 현재 위치를 기준으로 방향 고정
        lockedAttackDirection = (player.position - transform.position).normalized;

        // 공격 준비 동안 이동 멈춤
        yield return new WaitForSeconds(attackPreparationTime);

        isAttacking = true;
        LaunchAttack();
        yield return new WaitForSeconds(attackCooldown);

        isAttacking = false;
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

            //Debug.Log("공격!");
            Destroy(projectile, 0.1f); // n초 후 투사체 파괴
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") )
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
}
