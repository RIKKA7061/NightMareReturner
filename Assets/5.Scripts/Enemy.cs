using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class Enemy : MonoBehaviour
{
    [Header("속도")]
    public float speed;
	private Rigidbody2D rb;//중력
	private Player player;//플레이어
	private PlayerAction playerAction;
	private PrefabSpawner prefabSpawner;

    private Rigidbody2D target;

	[Header("감지 범위")]
    public int SenserRangeX = 3;
    public int SenserRangeY = 3;

	[Header("체력")]
	private int maxHP = 0; // 최대 체력 변수
    public int nowHP; // 현재 체력 변수

	[Header("플레이어 감지기 인듯")]
    public Scanner scanner;
    public Scanner2 scanner2;

    bool isLive;

	[Header("이것이 보스인가?")]
	public bool isBoss;

	Rigidbody2D rigid;
    SpriteRenderer spriter;

	//체력바 프리펩 
	[SerializeField]					// private형 변수를 외부에서 조절할 수 있게 바꿔줌
	private GameObject prfHpBar;		// 프리펩 체력바

	RectTransform bghp_bar;				// bghp_bar 어두운 배경 체력바
	Image hp_bar;						// hp_bar 현재 체력바

	public float height = 1.7f;         // 체력바 Y 높이

	[Header("데미지 수치 표기")]
	public TextMeshProUGUI damage_text;
	public GameObject damage_text_prf;

	private void SetEnemyStatus(int _maxHP) // 적 체력 설정 함수
    {
        maxHP = _maxHP;
        nowHP = _maxHP;
    }

	void Start()
	{
		rb = GetComponent<Rigidbody2D>();
		player = FindObjectOfType<Player>();// 무조건 해줘야됨 (초기화)
		SetEnemyStatus(maxHP);				// 체력 수치 설정

		// prfHpBar 프리팹을 이용해 canvas에다가 체력바 생성.
		bghp_bar = Instantiate(prfHpBar, GameObject.Find("Canvas").transform).GetComponent<RectTransform>(); // bghp_bar생성
		hp_bar = bghp_bar.transform.GetChild(0).GetComponent<Image>(); // bghp_bar에 자식 오브젝트 컴포넌트 가져오기
	}

	void Update()
	{
		if (bghp_bar == null)
		{
			//Debug.LogWarning("bghp_bar가 파괴되었습니다.");
			return;
		}

		// 카메라 보는 기준 체력바 좌표 위치 설정
		Vector3 _hpBarPos = Camera.main.WorldToScreenPoint(new Vector3(transform.position.x, transform.position.y + height, 0));
		bghp_bar.position = _hpBarPos; // 해당 좌표의 위치 적용하기

		hp_bar.fillAmount = (float)nowHP / (float)maxHP; // 체력 수치 적용하기
	}

	void Awake()
    {
		if (!isBoss)// 일반몬스터
		{
			maxHP = TableManager.Enemy1HP; // 최대 체력 변수
		}
		else if (isBoss) // 보스
		{
			maxHP = TableManager.BossHP; // 최대 체력 변수
		}
		rigid = GetComponent<Rigidbody2D>();
        target = GetComponent<Rigidbody2D>();
		spriter = GetComponent<SpriteRenderer>();
        scanner = GetComponent<Scanner>(); //근거리 공격용 스캔
        scanner2 = GetComponent<Scanner2>(); //원거리 공격용 스캔
		playerAction = FindObjectOfType<Player>().GetComponent<PlayerAction>();
		prefabSpawner = FindAnyObjectByType<PrefabSpawner>();
	}

	void FixedUpdate()
    {
		if(player.EnmeyDown == true)
        {
			EnemyDead();
			//GetComponent<Image>();     // 체력바(색) 파괴
			//GetComponent<RectTransform>();   // 체력바(배경) 파괴
			//Destroy(gameObject); // 적 (자기자신) 파괴
		}

		Vector2 direction = player.transform.position - transform.position;

		int X = Math.Abs(Mathf.RoundToInt(direction.x));
		int Y = Math.Abs(Mathf.RoundToInt(direction.y));

		if (X <= SenserRangeX && Y <= SenserRangeY)//가까이 있을때
		{
			//따라간다.
			Vector2 nextVec = direction.normalized * speed * Time.fixedDeltaTime;
			rigid.MovePosition(rigid.position + nextVec);
			rigid.velocity = Vector2.zero;

			//왼쪽 오른쪽 플립해주는 것
			if(direction.x > 0)
			{
				spriter.flipX = false;
			}
			else if(direction.x < 0)
			{
				spriter.flipX = true;
			}
		}
	}

	// 플레이어한테 공격 받을시 대미지 표기
	private void OnTriggerEnter2D(Collider2D other)
	{
		if (other.CompareTag("Player"))
		{
			int damage = player.Atk;				// 플레이어 대미지


			StartCoroutine(ShowDamageText(damage)); // 대미지를 표기
			nowHP = nowHP - damage;
		}

		// 일반몹 죽는 함수
        if(nowHP < 0 && isBoss == false)			// 체력이 0보다 적을시
        {
			EnemyDead();
		}
		// 적 죽는 함수
		else if(nowHP < 0 && isBoss == true)
		{
			BossDead();
		}
	}

	void BossDead()
	{
		Destroy(gameObject);
		prefabSpawner.RoomEnemyCount++;        // 적 죽은 횟수 1 늘어남
		Destroy(bghp_bar.gameObject);          // 체력바 삭제
	}

	void EnemyDead()
    {
		nowHP = 0;
		if (isBoss == false)
		{
			Destroy(gameObject);
		}
		prefabSpawner.RoomEnemyCount++;        // 적 죽은 횟수 1 늘어남

		if(bghp_bar != null) // 존재할 시
		{
			Destroy(bghp_bar.gameObject);          // 체력바 삭제
		}
	}

	// 대미지 텍스트 생성 및 1초 후 삭제
	IEnumerator ShowDamageText(int damage)
	{
		// 텍스트 프리팹 생성
		GameObject dmgText = Instantiate(damage_text_prf, GameObject.Find("Canvas").transform);
		TextMeshProUGUI dmgTextComponent = dmgText.GetComponent<TextMeshProUGUI>();

		// 텍스트 내용과 위치 설정
		dmgTextComponent.text = damage.ToString();

		// 초기 위치 설정 (월드 -> 스크린 좌표 변환)
		Vector3 startPosition = Camera.main.WorldToScreenPoint(transform.position);
		dmgText.transform.position = startPosition;

		// 이동할 y값 설정
		float moveDistance = 50f; // y축 이동 거리 (Canvas 기준)
		float duration = 0.2f;    // 지속 시간
		float elapsedTime = 0f;   // 경과 시간

		// 코루틴으로 텍스트 위치 이동
		while (elapsedTime < duration)
		{
			elapsedTime += Time.deltaTime;

			// 현재 위치 계산
			Vector3 currentPosition = dmgText.transform.position;
			currentPosition.y = Mathf.Lerp(startPosition.y, startPosition.y + moveDistance, elapsedTime / duration);
			dmgText.transform.position = currentPosition;

			yield return null; // 다음 프레임까지 대기
		}

		// 텍스트 삭제
		Destroy(dmgText);
	}

}
