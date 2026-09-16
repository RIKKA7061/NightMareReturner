using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// [NR] 피해는 Player.OnTriggerEnter2D → NRStats.DamagePlayer 한 곳에서 처리
//      (기존에는 여기서 방어력 무시 피해 + Player 쪽 처리 시 예외가 함께 발생)
public class MonsterATK : MonoBehaviour
{
    public int damage = 50;

    void OnTriggerEnter2D(Collider2D other)
    {
        // 몬스터와 충돌 시
        if (other.CompareTag("Player") && !other.isTrigger)
        {
            var act = other.GetComponent<PlayerAction>();
            if (act != null && act.DashInvulnerable) return; // 구르기 중에는 투사체가 통과
            NRCombatFX.Sparks(transform.position, NRPalette.Crimson, 6);
            Destroy(gameObject, 0.01f);
        }
    }

}
