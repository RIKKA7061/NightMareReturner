using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterATK : MonoBehaviour
{
    public int damage = 50;

    void OnTriggerEnter2D(Collider2D other)
    {
        // ���Ϳ� �浹 ��
        if (other.CompareTag("Player"))
        {
            Player Player = other.GetComponent<Player>();
            if (Player != null)
            {
                Player.TakeDamage(damage);
                Debug.Log($"������: {damage}");
            }

            Destroy(gameObject);
        }
    }

}
