using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackRadius : MonoBehaviour
{
    public Transform player;
    public float attackRange = 1.5f; // 공격 범위
    public float attackDelay = 0.6f; // 공격 전 대기 시간
    public int damage = 20; // 범위 내 공격 데미지
    public float damageDelay = 0.5f; // 데미지 적용 대기 시간
    private bool isAttacking = false; // 현재 공격 중인지 확인

    //private bool isAttacking = false; // 현재 공격 중인지 확인

    System.Collections.IEnumerator AttackPlayer()
    {
        isAttacking = true;

        // 공격 전 딜레이
        Debug.Log("공격준비");
        yield return new WaitForSeconds(attackDelay);

        // 공격 실행

        Debug.Log("공격");
        player.GetComponent<Player>()?.TakeDamage(damage);


        isAttacking = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // 플레이어 데미지 처리
            Debug.Log("플레이어가 원형 범위 공격에 피격되었습니다!");
            other.GetComponent<Player>().TakeDamage(damage);
        }
    }
}
