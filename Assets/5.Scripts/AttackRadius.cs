using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// [NR] 기존에는 생성 즉시(예고 없이) 피해를 줬음 → 예고 표시 후 범위 판정
public class AttackRadius : MonoBehaviour
{
    public Transform player;
    public float attackRange = 1.5f; // 공격 범위
    public float attackDelay = 0.6f; // 공격 전 대기 시간
    public int damage = 20; // 범위 내 공격 데미지
    public float damageDelay = 0.5f; // 데미지 적용 대기 시간
    private bool isAttacking = false; // 현재 공격 중인지 확인

    void Start()
    {
        StartCoroutine(AttackPlayer());
    }

    System.Collections.IEnumerator AttackPlayer()
    {
        isAttacking = true;
        float radius = Mathf.Max(attackRange, transform.lossyScale.x * 0.5f);
        NRTelegraph.Circle(transform.position, radius, attackDelay, NRPalette.Crimson);

        // 공격 전 딜레이
        yield return new WaitForSeconds(attackDelay);

        // 공격 실행
        var p = NRStats.Player;
        if (p != null && Vector2.Distance(p.transform.position, transform.position) <= radius)
            NRStats.DamagePlayer(p, damage, true);
        NRCombatFX.DeathBurst(transform.position, NRPalette.Crimson, 18);

        isAttacking = false;
        Destroy(gameObject, 0.05f);
    }
}
