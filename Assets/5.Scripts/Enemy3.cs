using UnityEngine;

public class Enemy3 : MonoBehaviour
{
	public float speed;//속도
	private Rigidbody2D rb;//중력
	private Player player;//플레이어

	void Start()
	{
		rb = GetComponent<Rigidbody2D>();
		// Scene에서 Player 태그를 가진 오브젝트를 찾습니다.
		player = FindObjectOfType<Player>();
	}

	void FixedUpdate()
	{
		// 플레이어 방향으로 따라갑니다.
		Vector2 direction = player.transform.position - transform.position;
		rb.velocity = direction.normalized * speed;
	}
}