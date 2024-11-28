using UnityEngine;

public class CarMovement : MonoBehaviour
{
	public float speed = 5f; // �ڵ��� �ӵ� ����

	private Rigidbody2D rb;

	void Start()
	{
		// Rigidbody2D ������Ʈ�� �����ɴϴ�.
		rb = GetComponent<Rigidbody2D>();
	}

	void FixedUpdate()
	{
		// ������ �������� ���������� �����̵��� ����
		rb.velocity = new Vector2(speed, rb.velocity.y);
	}
}
