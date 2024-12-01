using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterAI : MonoBehaviour
{
    public Transform player;
    public float chaseSpeed; // �̼�
    public float attackCooldown; // ����
    public float attackPreparationTime = 0.5f; // ���� �غ� �ð�
                                               //public int damage = 10; // ���� ������
    public GameObject attackProjectile; // ���� ����ü
    public float projectileSpeed = 25f; // ����ü �ӵ�

    private bool isAttacking = false; // ���� ���� ������ Ȯ��
    private bool playerInRange = false; // �÷��̾ ���� ���� �ִ��� Ȯ��
    private bool isPreparingAttack = false; // ���� �غ� ������ Ȯ��

    private Vector2 lockedAttackDirection; // ���� �غ� �� ������ ���� ����
    private bool canMove = true; // ���Ͱ� ������ �� �ִ��� ����

    public float pattern1Cooldown = 25f; // ���� 1 ��Ÿ��
    public GameObject blastProjectile; // �극�� ���� ����ü
    public float blastSpeed = 15f; // ����ü �ӵ�

    public GameObject attackRadiusPrefab; // ���� ������ ��Ÿ�� ������
    public float pattern2PreparationTime = 0.7f; // ���� �غ� �ð�
    public float pattern2Cooldown = 60f; // ���� 2 ��Ÿ��
    public int pattern2Damage = 110; // ���� 2 ������

    private bool isPattern1Active = false; // ���� 1 Ȱ��ȭ ����
    private bool isPattern2Active = false; // ���� 2 Ȱ��ȭ ����

    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("�ִϸ����� ���� �ȵ�");
        }
    }

    private void Awake()
    {
        player = FindObjectOfType<Player>().transform;    // ������ ����ߵ� (�ʱ�ȭ)
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
            // Idle ���·� ��ȯ
            animator.SetBool("IsWalking", false);
        }
    }

    void ChasePlayer()
    {
        Vector2 direction = (player.position - transform.position).normalized;
        transform.position = Vector2.MoveTowards(transform.position, player.position, chaseSpeed * Time.deltaTime);

        // �ȱ� �ִϸ��̼� Ȱ��ȭ
        animator.SetBool("IsWalking", true);
    }

    System.Collections.IEnumerator PrepareAndAttack()
    {
        isPreparingAttack = true;


        // ���� �غ� �ִϸ��̼� Ȱ��ȭ
        animator.SetTrigger("PrepareAttack");

        // ���� �غ� ���� �̵� ����
        yield return new WaitForSeconds(attackPreparationTime);

        isAttacking = true; // �⺻ ���� �� �÷��� Ȱ��ȭ
        Debug.Log("���� �غ�");

        // ���� �غ� �� �÷��̾��� ���� ��ġ�� �������� ���� ����
        lockedAttackDirection = (player.position - transform.position).normalized;

        // ���� �ִϸ��̼� Ȱ��ȭ
        animator.SetTrigger("Attack");

        LaunchAttack();
        yield return new WaitForSeconds(attackCooldown);

        isAttacking = false; // �⺻ ���� ����
        isPreparingAttack = false;
    }

    void LaunchAttack()
    {
        if (attackProjectile != null)
        {
            // ����ü ���� �� ������ �������� �߻�
            GameObject projectile = Instantiate(attackProjectile, transform.position, Quaternion.identity);
            Rigidbody2D rb = projectile.GetComponent<Rigidbody2D>();

            if (rb != null)
            {
                rb.velocity = lockedAttackDirection * projectileSpeed;
            }

            Debug.Log("(��������)���� ����ü �߻�!");
            Destroy(projectile, 0.5f); // n�� �� ����ü �ı�
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
        isAttacking = true; // ��ų ���� �� �÷��� Ȱ��ȭ

        // ���� ���� 1
        animator.SetTrigger("Pattern1");

        // �÷��̾� ��ġ ������� ���� ���
        Vector2 playerDirection = (player.position - transform.position).normalized;

        // ��/�� �Ǵ� (x �ุ ����)
        string direction = playerDirection.x > 0 ? "������" : "����";
        Debug.Log($"�극�� ���� �غ� {direction} ����");

        // 0.5�� ��� (�غ� �ð�)
        yield return new WaitForSeconds(0.5f);

        if (blastProjectile != null)
        {
            GameObject blast = Instantiate(blastProjectile, transform.position, Quaternion.identity);
            Rigidbody2D rb = blast.GetComponent<Rigidbody2D>();

            if (rb != null)
            {
                rb.velocity = new Vector2(playerDirection.x, 0).normalized * blastSpeed; // x�����θ� �߻�
            }

            Debug.Log($"�극�� �߻�! {direction} ��������!");
            Destroy(blast, 3f); // n�� �� ����ü �ı�
        }

        // ��Ÿ�� ���
        yield return new WaitForSeconds(pattern1Cooldown);

        isPattern1Active = false; // ��ų ����
        isAttacking = false; // ��ų ���� �� �÷��� ����
    }

    System.Collections.IEnumerator Pattern2_CircularAttack()
    {
        isPattern2Active = true;
        canMove = false; // �̵� �Ұ� ���·� ��ȯ

        // ���� ���� 2
        animator.SetTrigger("Pattern2");

        // ���� ���� ����
        GameObject attackRadius = Instantiate(attackRadiusPrefab, transform.position, Quaternion.identity);
        attackRadius.transform.localScale = new Vector3(2, 2, 1); // ũ�� ���� (�ʿ信 ���� ����)

        // ���� ��ũ��Ʈ�� ������ ����
        AttackRadius attackScript = attackRadius.GetComponent<AttackRadius>();
        if (attackScript != null)
        {
            attackScript.damage = pattern2Damage; // ���� ������ ����
        }

        Debug.Log("���� ���� �غ� ��!");

        // �غ� �ð� ���
        yield return new WaitForSeconds(pattern2PreparationTime);

        // ���� ����
        Destroy(attackRadius);

        Debug.Log("���� ���� �Ϸ�!");

        // ��Ÿ�� ���
        yield return new WaitForSeconds(pattern2Cooldown);

        canMove = true; // �̵� ���� ���·� ����
        isPattern2Active = false;
    }

}

