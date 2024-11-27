using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterHP : MonoBehaviour
{
    private Player player;  //플레이어
    private Rigidbody2D rb; //중력
    private PlayerAction playerAction;
    private PrefabSpawner prefabSpawner;
    private UI_MonsterHP uI_MonsterHP; // ui에 뿌려주는 hp바
    private DamageTextShow damageTextShow; // ui 데미지 수치를 표기해주는거 관련 스크립트

	[Header("체력")]
    public int maxHP; // 최대 체력 변수
    public int nowHP; // 현재 체력 변수

    [Header("이것이 보스인가?")]
    public bool isBoss;

    [Header("이것이 중간보스인가?")]
    public bool middleBoss;

    Rigidbody2D rigid;
    SpriteRenderer spriter;

    public int defaultMaxHp;

    void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        spriter = GetComponent<SpriteRenderer>();
        playerAction = FindObjectOfType<Player>().GetComponent<PlayerAction>();
        prefabSpawner = FindAnyObjectByType<PrefabSpawner>();
		uI_MonsterHP = GetComponent<UI_MonsterHP>();
		damageTextShow = GetComponent<DamageTextShow>();// 같은 컴포넌트에 속해있다.
    }

    private void SetEnemyStatus(int _maxHP)
    {
        maxHP = _maxHP;
        nowHP = _maxHP;
    }

    IEnumerator SetDefaultEnemyStat()
    {
        yield return new WaitForSeconds(2.0f);
        SetEnemyStatus(defaultMaxHp);
    }


    public void SetDefault()
    {
        SetDefaultEnemyStat();
    }
    void Start()
    {
        defaultMaxHp = maxHP;
        rb = GetComponent<Rigidbody2D>();
        player = FindObjectOfType<Player>();// 무조건 해줘야됨 (초기화)
        SetEnemyStatus(maxHP);              // 체력 수치 설정

        // prfHpBar 프리팹을 이용해 canvas에다가 체력바 생성.
        //bghp_bar = Instantiate(prfHpBar, GameObject.Find("Canvas").transform).GetComponent<RectTransform>(); // bghp_bar생성
        //hp_bar = bghp_bar.transform.GetChild(0).GetComponent<Image>(); // bghp_bar에 자식 오브젝트 컴포넌트 가져오기
    }

    private void Update()
    {
    }

    // 접촉시
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 그 대상이 플레이어 태그일시 && 플레이어가 공격중일시
        if (other.CompareTag("Player") && playerAction.isAtking)
        {
            int damage = player.Atk;                // 플레이어 대미지
			damageTextShow.ShowDamage(damage);
			//StartCoroutine(ShowDamageText(damage)); // 대미지를 표기
			nowHP = nowHP - damage;
        }

        // 일반몹 죽는 함수
        //if (nowHP < 0 && isBoss == false)			// 체력이 0보다 적을시
        //{
        //    EnemyDead();
        //}
        //// 적 죽는 함수
        //else if (nowHP < 0 && isBoss == true)
        //{
        //    BossDead();
        //}
        if (nowHP < 0 && middleBoss) // 중간보스
        {
            MiddleBossDead();
		}
    }

    void BossDead()
    {
        Destroy(gameObject);
        //prefabSpawner.RoomEnemyCount++;        // 적 죽은 횟수 1 늘어남
        //Destroy(bghp_bar.gameObject);          // 체력바 삭제
    }

    void EnemyDead()
    {
        nowHP = 0;
        if (isBoss == false)
        {
            Destroy(gameObject);
		}
        //prefabSpawner.RoomEnemyCount++;        // 적 죽은 횟수 1 늘어남
        //Destroy(bghp_bar.gameObject);          // 체력바 삭제

    }

	void MiddleBossDead()
	{
		nowHP = 0;
		uI_MonsterHP.DestroyHP_UI();// 체력바 삭제
		Destroy(gameObject);    // 자기 자신을 삭제
	}
}
