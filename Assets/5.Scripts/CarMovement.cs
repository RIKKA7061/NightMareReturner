using UnityEngine;

public class CarMovement : MonoBehaviour
{
	public float speed = 5f; // 자동차 속도 조정

	private Rigidbody2D rb;

	void Start()
	{
		// Rigidbody2D 컴포넌트를 가져옵니다.
		rb = GetComponent<Rigidbody2D>();
	}

	void FixedUpdate()
	{
		// 오른쪽 방향으로 지속적으로 움직이도록 설정
		rb.velocity = new Vector2(speed, rb.velocity.y);
	}
}
