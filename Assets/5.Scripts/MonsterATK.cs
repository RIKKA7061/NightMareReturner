using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterATK : MonoBehaviour
{
    public int damage = 50;

    void OnTriggerEnter2D(Collider2D other)
    {
        // 몬스터와 충돌 시
        if (other.CompareTag("Player"))
        {
            Player Player = other.GetComponent<Player>();
            if (Player != null)
            {
                Player.TakeDamage(damage);
                Debug.Log($"데미지: {damage}");
            }

            Destroy(gameObject);
        }
    }

}
