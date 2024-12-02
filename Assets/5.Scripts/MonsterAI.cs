using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterAI : MonoBehaviour
{
    public Transform player;
    public float chaseSpeed; // �̼�

    [Header("�⺻����")]
    public float basicAttackCooldown = 5f; // �⺻ ���� ��Ÿ��
    public float attackPreparationTime = 0.5f; // ���� �غ� �ð�
                                               //public int damage = 10; // ���� ������
    public GameObject attackProjectile; // ���� ����ü
    public float projectileSpeed = 25f; // ����ü �ӵ�

    private bool isAttacking = false; // ���� ���� ������ Ȯ��
    private bool playerInRange = false; // �÷��̾ ���� ���� �ִ��� Ȯ��
    private bool isPreparingAttack = false; // ���� �غ� ������ Ȯ��
    private bool isPerformingAction = false; // ���� �ൿ(�⺻ ����/���� ��) ������ Ȯ��

    private Vector2 lockedAttackDirection; // ���� �غ� �� ������ ���� ����
    private bool canMove = true; // ���Ͱ� ������ �� �ִ��� ����

    [Header("����1")]
    public float pattern1Cooldown = 25f; // ���� 1 ��Ÿ��
    public GameObject blastProjectile; // �극�� ���� ����ü
    public float blastSpeed = 15f; // ����ü �ӵ�

    [Header("����2")]
    public GameObject attackRadiusPrefab; // ���� ������ ��Ÿ�� ������
    public float pattern2PreparationTime = 0.7f; // ���� �غ� �ð�
    public float pattern2Cooldown = 60f; // ���� 2 ��Ÿ��
    public int pattern2Damage = 110; // ���� 2 ������

    private bool isPattern1Active = false; // ���� 1 Ȱ��ȭ ����
    private bool isPattern2Active = false; // ���� 2 Ȱ��ȭ ����

    private float nextBasicAttackTime = 2f; // ���� �⺻ ���� ���� �ð�
    private float nextPattern1Time = 12f; // ���� ����1 ���� �ð�
    private float nextPattern2Time = 25f; // ���� ����2 ���� �ð�

    private enum MonsterState { Idle, BasicAttack, Pattern1, Pattern2 } // ���� ����
    private MonsterState currentState = MonsterState.Idle; // ���� ����

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
        if (isPerformingAction || player == null) return;

        // �¿� ���� ó��
        FlipTowardsPlayer();

        // ���� 1 �Ǵ� ���� 2 ���� �켱
        if (Time.time >= nextPattern2Time)
        {
            StartCoroutine(Pattern2_CircularAttack());
        }
        else if (Time.time >= nextPattern1Time)
        {
            StartCoroutine(Pattern1_BlastAttack());
        }
        // �⺻ ������ ���� ���� �ƴ� ���� ����
        else if (playerInRange && Time.time >= nextBasicAttackTime && currentState == MonsterState.Idle)
        {
            StartCoroutine(BasicAttack());
        }
        else if (playerInRange)
        {
            ChasePlayer(); // �÷��̾� ����
        }
        else
        {
            animator.SetBool("IsWalking", false); // Idle ���� ����
        }
    }

    void ChasePlayer()
    {
        // �÷��̾� �������� �̵�
        Vector2 direction = (player.position - transform.position).normalized;
        transform.position = Vector2.MoveTowards(transform.position, player.position, chaseSpeed * Time.deltaTime);


        // �ȱ� �ִϸ��̼� Ȱ��ȭ
        animator.SetBool("IsWalking", true);
    }

    System.Collections.IEnumerator BasicAttack()
    {
        isPerformingAction = true;
        currentState = MonsterState.BasicAttack;

        nextBasicAttackTime = Time.time + basicAttackCooldown; // ���� �⺻ ���� �ð� ����
        Debug.Log($"���� �⺻���� �ð�: {nextBasicAttackTime}, ���� �����ð�: {Time.time}");

        // �⺻ ���� �غ� �ִϸ��̼� ����
        animator.SetTrigger("PrepareAttack");
        yield return new WaitForSeconds(attackPreparationTime);

        // �⺻ ���� ����
        animator.SetTrigger("Attack");
        LaunchAttack();

        yield return new WaitForSeconds(0.3f); // �⺻ ���� �� ��� �ð�

        isPerformingAction = false;
        currentState = MonsterState.Idle;
    }

    void LaunchAttack()
    {
        if (attackProjectile != null)
        {
            // ����ü ���� �� ������ �������� �߻�
            GameObject projectile = Instantiate(attackProjectile, transform.position, Quaternion.identity);

            Vector2 attackDirection = (player.position - transform.position).normalized;
            float angle = Mathf.Atan2(attackDirection.y, attackDirection.x) * Mathf.Rad2Deg;
            projectile.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

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
            Debug.Log("�÷��̾ �����ȿ� ����");
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            Debug.Log("�÷��̾� ���� ����");
        }
    }

    System.Collections.IEnumerator Pattern1_BlastAttack()
    {
        isPattern1Active = true;
        isAttacking = true; // ��ų ���� �� �÷��� Ȱ��ȭ

        nextPattern1Time = Time.time + pattern1Cooldown; // ���� ����1 �ð� ����
        Debug.Log($"���� ����1 �ð�: {pattern1Cooldown}, ���� �����ð�: {Time.time}");

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
            Destroy(blast, 1f); // n�� �� ����ü �ı�
        }

        // ��Ÿ�� ���
        yield return new WaitForSeconds(pattern1Cooldown);

        isPattern1Active = false; // ��ų ����
        isAttacking = false; // ��ų ���� �� �÷��� ����
        isPerformingAction = false;
        currentState = MonsterState.Idle;
    }

    System.Collections.IEnumerator Pattern2_CircularAttack()
    {
        isPattern2Active = true;
        canMove = false; // �̵� �Ұ� ���·� ��ȯ

        nextPattern2Time = Time.time + pattern2Cooldown; // ���� ����2 �ð� ����
        Debug.Log($"���� ����2 �ð�: {pattern2Cooldown}, ���� �����ð�: {Time.time}");

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
        isPerformingAction = false;
        currentState = MonsterState.Idle;
    }

    void FlipTowardsPlayer()
    {
        // ���� ������ localScale�� ������
        Vector3 scale = transform.localScale;

        // �÷��̾ ���ʿ� ������ x�� ����
        if (player.position.x < transform.position.x)
        {
            scale.x = Mathf.Abs(scale.x) * -1; // x���� ������ ����
        }
        else
        {
            scale.x = Mathf.Abs(scale.x); // x���� ����� ����
        }

        transform.localScale = scale; // ������ Scale �� ����
    }
}

