using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackRadius : MonoBehaviour
{
    public Transform player;
    public float attackRange = 1.5f; // ���� ����
    public float attackDelay = 0.6f; // ���� �� ��� �ð�
    public int damage = 20; // ���� �� ���� ������
    public float damageDelay = 0.5f; // ������ ���� ��� �ð�
    private bool isAttacking = false; // ���� ���� ������ Ȯ��

    //private bool isAttacking = false; // ���� ���� ������ Ȯ��

    System.Collections.IEnumerator AttackPlayer()
    {
        isAttacking = true;

        // ���� �� ������
        Debug.Log("�����غ�");
        yield return new WaitForSeconds(attackDelay);

        // ���� ����

        Debug.Log("����");
        player.GetComponent<Player>()?.TakeDamage(damage);


        isAttacking = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // �÷��̾� ������ ó��
            Debug.Log("�÷��̾ ���� ���� ���ݿ� �ǰݵǾ����ϴ�!");
            other.GetComponent<Player>().TakeDamage(damage);
        }
    }
}
