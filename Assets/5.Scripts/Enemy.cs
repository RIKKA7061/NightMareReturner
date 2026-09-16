using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

// [NR] 변경 요약
//  - 체력이 정확히 0일 때 죽지 않던 문제(nowHP < 0) → nowHP <= 0
//  - 사망 처리가 여러 번 호출돼 처치 수가 중복 증가하던 문제 방지
//  - 한 번 발견한 플레이어를 일정 거리까지 계속 추적(어그로 유지), 평소에는 배회
//  - 둔화/화상 상태이상, 피격 번쩍임, 계층별 체력/공격력 배율, 증강 피해 계산 연동
public class Enemy : MonoBehaviour, INRDamageable
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

	// [NR]
	bool isDeadFlag = false;
	bool aggro = false;
	Vector2 homePos;
	Vector2 wanderTarget;
	float nextWanderTime;
	float hpMultiplier = 1f;
	const float LoseAggroDistance = 9f;

	public bool IsDead => isDeadFlag;
	bool INRDamageable.IsBoss => isBoss;
	public float HpRatio => maxHP > 0 ? Mathf.Clamp01((float)nowHP / maxHP) : 0f;
	public Transform DamageAnchor => transform;

	private void SetEnemyStatus(int _maxHP) // 적 체력 설정 함수
    {
        maxHP = _maxHP;
        nowHP = _maxHP;
    }

	/// <summary>[NR] 계층별 강화 (Instantiate 직후, Start 전에 호출)</summary>
	public void NRScale(float hpMul, float atkMul)
	{
		hpMultiplier = hpMul;
		foreach (var w in GetComponentsInChildren<weapon>(true)) w.damage = Mathf.RoundToInt(w.damage * atkMul);
		foreach (var w in GetComponentsInChildren<weapon2>(true)) w.MultiplyDamage(atkMul);
	}

	void Start()
	{
		rb = GetComponent<Rigidbody2D>();
		player = FindObjectOfType<Player>();// 무조건 해줘야됨 (초기화)
		SetEnemyStatus(Mathf.RoundToInt(maxHP * hpMultiplier));				// 체력 수치 설정
		homePos = transform.position;
		wanderTarget = homePos;

		// prfHpBar 프리팹을 이용해 canvas에다가 체력바 생성.
		var canvas = GameObject.Find("Canvas");
		if (prfHpBar != null && canvas != null)
		{
			bghp_bar = Instantiate(prfHpBar, canvas.transform).GetComponent<RectTransform>(); // bghp_bar생성
			hp_bar = bghp_bar.transform.GetChild(0).GetComponent<Image>(); // bghp_bar에 자식 오브젝트 컴포넌트 가져오기
			bghp_bar.gameObject.SetActive(false); // [NR] 피격 전에는 숨김
		}
	}

	float hpBarVisibleUntil;

	void Update()
	{
		if (bghp_bar == null)
		{
			return;
		}

		bool show = Time.time < hpBarVisibleUntil || isBoss;
		if (bghp_bar.gameObject.activeSelf != show) bghp_bar.gameObject.SetActive(show);
		if (!show) return;

		// 카메라 보는 기준 체력바 좌표 위치 설정
		if (Camera.main == null) return;
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
		if (maxHP <= 0) maxHP = 100; // [NR] TableManager가 없는 씬 대비
		rigid = GetComponent<Rigidbody2D>();
        target = GetComponent<Rigidbody2D>();
		spriter = GetComponent<SpriteRenderer>();
        scanner = GetComponent<Scanner>(); //근거리 공격용 스캔
        scanner2 = GetComponent<Scanner2>(); //원거리 공격용 스캔
		var p = FindObjectOfType<Player>();
		playerAction = p != null ? p.GetComponent<PlayerAction>() : null;
		prefabSpawner = FindAnyObjectByType<PrefabSpawner>();
	}

	void FixedUpdate()
    {
		if (isDeadFlag || player == null) return;

		if(player.EnmeyDown == true)
        {
			EnemyDead();
			return;
		}

		Vector2 direction = player.transform.position - transform.position;
		float dist = direction.magnitude;

		int X = Math.Abs(Mathf.RoundToInt(direction.x));
		int Y = Math.Abs(Mathf.RoundToInt(direction.y));

		if (!aggro && X <= SenserRangeX && Y <= SenserRangeY && !player.isDead) aggro = true;
		if (aggro && (dist > LoseAggroDistance || player.isDead)) aggro = false;

		float speedMul = NREnemyStatus.SpeedMul(this);

		if (aggro)//가까이 있을때 (한 번 발견하면 멀어질 때까지 추적)
		{
			//따라간다. (너무 붙으면 살짝 거리 유지)
			Vector2 dir = direction.normalized;
			if (dist < 0.6f) dir = -dir * 0.3f;
			Vector2 nextVec = dir * speed * speedMul * Time.fixedDeltaTime;
			rigid.MovePosition(rigid.position + nextVec + Separation() * Time.fixedDeltaTime);
			rigid.velocity = Vector2.zero;
			Flip(direction.x);
		}
		else
		{
			// [NR] 배회
			if (Time.time > nextWanderTime)
			{
				nextWanderTime = Time.time + UnityEngine.Random.Range(1.5f, 3f);
				wanderTarget = homePos + UnityEngine.Random.insideUnitCircle * 1.5f;
			}
			Vector2 toTarget = wanderTarget - rigid.position;
			if (toTarget.magnitude > 0.1f)
			{
				rigid.MovePosition(rigid.position + toTarget.normalized * speed * 0.35f * speedMul * Time.fixedDeltaTime);
				Flip(toTarget.x);
			}
			rigid.velocity = Vector2.zero;
		}
	}

	void Flip(float x)
	{
		//왼쪽 오른쪽 플립해주는 것
		if(x > 0)
		{
			spriter.flipX = false;
		}
		else if(x < 0)
		{
			spriter.flipX = true;
		}
	}

	// [NR] 적끼리 한 점에 겹치지 않도록 약한 밀어내기
	Vector2 Separation()
	{
		Vector2 push = Vector2.zero;
		foreach (var c in Physics2D.OverlapCircleAll(transform.position, 0.55f, 1 << 7))
		{
			if (c.attachedRigidbody == rigid) continue;
			Vector2 d = (Vector2)transform.position - (Vector2)c.transform.position;
			if (d.sqrMagnitude < 0.0001f) d = UnityEngine.Random.insideUnitCircle * 0.01f;
			push += d.normalized * (0.55f - Mathf.Min(0.55f, d.magnitude)) * 3f;
		}
		return push;
	}

	// 플레이어한테 공격 받을시 대미지 표기
	private void OnTriggerEnter2D(Collider2D other)
	{
		if (isDeadFlag) return;
		// [NR] 플레이어 공격 판정(트리거)이 켜져 있을 때만 피해
		if (other.CompareTag("Player") && other.isTrigger)
		{
			NRStats.PlayerHitEnemy(this, this);
		}
	}

	public void ReceiveDamage(int amount, bool crit, NRDamageKind kind)
	{
		if (isDeadFlag || amount <= 0) return;
		nowHP -= amount;
		aggro = true;
		hpBarVisibleUntil = Time.time + 3f;
		NRCombatFX.DamageNumber(transform.position + Vector3.up * 0.8f, amount, crit, kind);
		if (spriter != null && kind != NRDamageKind.Burn) NRCombatFX.Flash(spriter, new Color(1f, 0.45f, 0.45f, 1f), 0.08f);

		// 일반몹 죽는 함수
        if(nowHP <= 0 && isBoss == false)			// 체력이 0 이하일시
        {
			EnemyDead();
		}
		// 적 죽는 함수
		else if(nowHP <= 0 && isBoss == true)
		{
			BossDead();
		}
	}

	void BossDead()
	{
		if (isDeadFlag) return;
		isDeadFlag = true;
		NRStats.OnEnemyKilled(transform.position, true);
		NRWaves.NotifyDeath(gameObject);
		Destroy(gameObject);
		if (prefabSpawner != null) prefabSpawner.RoomEnemyCount++;        // 적 죽은 횟수 1 늘어남
		if (bghp_bar != null) Destroy(bghp_bar.gameObject);          // 체력바 삭제
	}

	void EnemyDead()
    {
		if (isDeadFlag) return;
		isDeadFlag = true;
		nowHP = 0;
		bool killedByPlayer = player != null && !player.EnmeyDown;
		if (killedByPlayer) NRStats.OnEnemyKilled(transform.position, false);
		NRWaves.NotifyDeath(gameObject);
		if (isBoss == false)
		{
			Destroy(gameObject);
		}
		if (prefabSpawner != null && killedByPlayer) prefabSpawner.RoomEnemyCount++;        // 적 죽은 횟수 1 늘어남

		if(bghp_bar != null) // 존재할 시
		{
			Destroy(bghp_bar.gameObject);          // 체력바 삭제
		}
	}

	void OnDestroy()
	{
		if (bghp_bar != null) Destroy(bghp_bar.gameObject);
	}

	// 대미지 텍스트 생성 및 1초 후 삭제 (기존 방식, 현재는 NRCombatFX 사용)
	IEnumerator ShowDamageText(int damage)
	{
		NRCombatFX.DamageNumber(transform.position + Vector3.up * 0.8f, damage, false, NRDamageKind.Direct);
		yield break;
	}

}
