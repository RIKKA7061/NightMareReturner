using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using Unity.VisualScripting;

public class Player : MonoBehaviour
{
    PlayerHpShow PlayerHpShow;

    public List<_Object> npcObjects = new List<_Object>();

    [Header("소환 스크립트")]
    public PrefabSpawner prefabSpawner;
    public PlayerAction playerAction;

    [Header("죽은 횟수")]
    static public int DeadCount = 0;
    public TextMeshProUGUI DeadCount_UI;

    //적 모두 킬
    public bool EnmeyDown = false;

    //플레이어 위치
    public Transform player;

    //집 위치
    public Transform Home;

    //지옥 위치
    public Transform DeadPoint;

    public bool isDead = false;
    public GameObject Dead_set; // 죽음 연출

    [Header("체력")]
    public int maxHP = 1000; // 최대 체력
    public int nowHP; // 체력을 int로 변경
    public TextMeshProUGUI HP_UI;

    [Header("일반 공격 (잽)")]
    public int Atk;

	[Header("스킬 (차징 펀치) 몇배만큼 증가할것인가?")]
	public int SkillAtk;

	[Header("궁극기(망치) 몇배만큼 증가 할것인가?")]
	public int UltimitAtk;

	[Header("현재 공격력 UI 표시용")]
	public TextMeshProUGUI Atk_UI;

    [Header("방어력")]
    public int AR; // Armor Resistance 방어력 Defense -> AR로 바꿈
    public TextMeshProUGUI AR_UI;

    public int atkNum; // 콤보 번호

    [Header("체력 디버프 퍼센트")]
    public float healthDebuff1 = 0.35f;       // 체력 감소 디버프 1 (35%) 공황장애
    public float healthDebuff2 = 0.3f;        // 체력 감소 디버프 2 (30%)  긴장성 두통
    public float healthDebuff3 = 0.2f;        // 체력 감소 디버프 3 (20%) 수면장애
    public float healthDebuff4 = 0.1f;        // 체력 감소 디버프 4 (10%) 탈모증


    [Header("방어력 디버프 퍼센트")]
    public float defenseDebuff1 = 0.25f;      // 방어력 감소 디버프 1 (25%) 사회적 불안장애
    public float defenseDebuff2 = 0.14f;      // 방어력 감소 디버프 2 (14%) 자존감 저하
    public float defenseDebuff3 = 0.26f;      // 방어력 감소 디버프 3 (26%) 회피행동
    public float defenseDebuff4 = 0.35f;      // 방어력 감소 디버프 4 (35%) 심장 두근거림

    [Header("재화")]
    static public int Money = 0; // 재화
	public TextMeshProUGUI MoneyTxt;
    public TextMeshProUGUI StoreMoneyTxt;

    [Header("구슬")]
	static public int round = 0; // 구슬
	public TextMeshProUGUI RoundTxt;

	Animator animator;        // Animator 컴포넌트 참조

	[Header("랜덤 문 숨기기용")]
	public RoomGenerator roomGenerator; // 랜덤 문 숨기기용

	// 디폴트 스테이터스 (저장용)
	int DefaultMaxHP = 0;
	int DefaultAtk = 0;
	int DefaultMoney = 0;
	int DefaultRound = 0;
    float D_speed = 0;
    float D_As = 0; // 공격속도
    int D_Ar = 0; // 방어력 Armor Resistance

	[Header("게임 라운드 수")]
	public TextMeshProUGUI gameRoundTMP;
	// 매 방마다 라운드 UP, 5번시 상점 -> 6번시 보스방
	static public int gameRound = 0;

    public ShopManager shop;

    public ItemManager itemManager;

    public int HammerBuffedRoundCount = 0;

    // 상점 테스트용 재화 + 100
    public void AddMoneyTest()
	{
        Money += 100;
	}

	private void Awake()
	{
		animator = GetComponent<Animator>();
	}
	private void Start()
    {
        nowHP = Mathf.FloorToInt(maxHP); // 최대 체력을 정수로 설정
        PlayerHpShow = GetComponent<PlayerHpShow>();

        // 디버프 적용
       // ApplyDebuff();

        // 게임 시작시 라운드 초기화
        gameRound = 0;

        // 디폴트 스테이터스 저장
        DefaultMaxHP = maxHP;
        DefaultAtk = Atk;
        DefaultMoney = Money;
        DefaultRound = round;
		D_speed = playerAction.walkSpeed; // start함수 내에 있음
		D_As = playerAction.jabCooldown; // 공격속도
		D_Ar = AR; // 방어력 Armor Resistance
	}

    private void ApplyDebuff()// 디버프 적용
    {
        // 체력 디버프 적용: 95% 감소를 위해 계산
        nowHP = Mathf.FloorToInt(maxHP * 0.05f); // 최종 체력 = 1000 * 0.05

        // 방어력 디버프 적용
        float debuffedDefense = AR * (1 - defenseDebuff1);
        debuffedDefense *= (1 - defenseDebuff2);
        debuffedDefense *= (1 - defenseDebuff3);
        debuffedDefense *= (1 - defenseDebuff4);
        AR = Mathf.FloorToInt(debuffedDefense);

        Debug.Log("디버프 적용 완료: ");
        Debug.Log($"체력: {nowHP}");
        Debug.Log($"방어력: {AR}");
    }

    // 죽었을 떄 행하는 것
    public void Dead()
    {
		isDead = true; // 플레이어 죽음 여부 활성화
		DeadCount++;
		StartCoroutine(DeadShow());
    }

    // 죽었을 때 행하는 것22
    IEnumerator DeadShow()
    {
		Debug.Log("내가 몇번 실행 되게?");
		player.GetComponent<PlayerInput>().enabled = false; // 멈춰
        animator.SetTrigger(AnimationStrings.DeadTrigger);  // dead 애니메이션 실행
        yield return new WaitForSeconds(4); // N초 동안 기달려
		Dead_set.SetActive(true);
		// 지옥 위치로 이동
		player.position = DeadPoint.position;
		//prefabSpawner.HideTP();
		prefabSpawner.isSpawnned = false;
		EnmeyDown = true; // 적 모두 비활성화
		yield return null;
	}

    // 적의 투사체에 의해 피격 당할시
    void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("weapon"))
            return;

		// 피격 애니메이션 재생
        if(playerAction.canMove == true)
            animator.SetTrigger(AnimationStrings.OuchTrigger);  // dead 애니메이션 실행
		else
		{
			// ResetTrigger로 해당 트리거를 비활성화
			animator.ResetTrigger(AnimationStrings.OuchTrigger);
		}

		// 방어력을 고려한 최종 피해량 계산
		//float incomingDamage = collision.GetComponent<FarATK>().damage;
		//      float finalDamage = Mathf.Max(incomingDamage - AR, 1); // 방어력 적용 후 최소 피해 1로 제한

		//      nowHP -= Mathf.FloorToInt(finalDamage); // 피해량을 정수로 적용

		// 방어력을 고려한 최종 피해량 계산
		float incomingDamage = collision.GetComponent<FarATK>().damage;
        PlayerDamaged(incomingDamage);

        // 체력이 0보다 적을시 && 죽는 여부가 비활성화 될시
		if (nowHP <= 0 && isDead == false)
        {
            Dead();
        }
    }


    private void Update()
    {
        // 죽은 상태에서 스페이스바를 누르면 부활 처리 // #####################
        if (isDead && Input.GetKeyDown(KeyCode.Space))
        {
            RespawnPlayer();
            Debug.Log("스페이스 바 누름");
        }

        //if (isDead)
        //    player.position = DeadPoint.position;

        // 임시 체력 표기 변수
        int TempNowHP = nowHP;

        if (nowHP < 0)// 체력이 음수일 때 -> 보여주기용 체력 0 표기
		{
            TempNowHP = 0;
		}
        else if (nowHP > 0) // 양수 일 때는 현재 체력으로 표기
        {
			TempNowHP = nowHP;
		}

        HP_UI.text = TempNowHP.ToString() + "/" + maxHP.ToString(); // (현재체력/최대체력)
		Atk_UI.text = Atk.ToString();                               // 공격력
		MoneyTxt.text = Money.ToString();                           // 재화
        StoreMoneyTxt.text = Money.ToString();                      // 상점에서 보이는 재화
        RoundTxt.text = round.ToString();                           // 구슬
        AR_UI.text = AR.ToString();                                 // 방어력

        // 현재 게임 라운드 수
        string Temptext = "";
        if (gameRound == 0) // 집 일때
        {
            Temptext = "집".ToString();
        }
        else if (gameRound <= prefabSpawner.OverRoom)
        {
            Temptext = "스테이지 : " + gameRound.ToString();
        }
        else if (gameRound == prefabSpawner.OverRoom + 1)
        {
            Temptext = "상점";
        }
        else if (gameRound == prefabSpawner.OverRoom + 2)
        {
            Temptext = "보스방";
        }

        gameRoundTMP.text = Temptext.ToString();

        DeadCount_UI.text = "죽은 횟수 : " + DeadCount.ToString();
    }

    public void RespawnPlayer()
    {
        // 플레이어 스테이터스 원래대로 즉, 버프 제거
        StatDefaultPlayer();

		// 진행 중인 게임 라운드 수 0으로 초기화
		gameRound = 0;

        player.GetComponent<PlayerInput>().enabled = true; // 다시 움직일수 있게
        nowHP = Mathf.FloorToInt(maxHP); // 부활 시 체력을 최대치로 설정
        isDead = false;
        player.position = Home.position; // 집 텔
        Dead_set.SetActive(false); // 죽음 연출 끝

        // 랜덤 문 다시 안보이게 숨기기
        roomGenerator.DestroyDoor();

        foreach (var npc in npcObjects)
        {
            npc.isDialogged = true;
        }
        
        EnmeyDown = false;//적 죽음 상태 해제
        prefabSpawner.RoomEnemyCount = 0;// 적 죽인 수 0으로 초기화
        prefabSpawner.DestroySpawnedObjects();// 생성된 아이템 및 상점 TP 객체 제거
	}// 플레이어 부활

	public void StatDefaultPlayer()
    {
		maxHP = DefaultMaxHP;
		Atk = DefaultAtk;
		round = DefaultRound;
		itemManager.isNextRoundHpUp = false;    // 기력방울 버프 제거
		itemManager.DebuffHpUp();
        playerAction.walkSpeed = D_speed;
        playerAction.jabCooldown = D_As;// 공격속도
        AR = D_Ar; // 방어력 Armor Resistance
        itemManager.GoodByeHammerBuff();        // 해머 버프 제거


		itemManager.OFFCrunchMode();            // 과민의 눈 버프 제거

		if (Atk != 25)
		{
			Atk = DefaultAtk;
		}
	}// 플레이어 스테이터스 디폴트로 설정


	const float AR_FACTOR = 0.01f;       // 방어력 공식에 쓰임
	public void PlayerDamaged(float dmg) // 플레이어가 받게되는 데미지 (방어력 공식 적용)
    {
		// 방어력이 음수일 경우 0으로 보정
		if (AR < 0) AR = 0;

        // 데미지 계산 (롤하고 똑같음) (방어력 100 -> 50%) (방어력 200 -> 33.33%)
		int totaldmg = Mathf.Max(1, (int)(dmg / (1 + (AR * AR_FACTOR))));

		// 현재 HP에서 피해량 감소
		nowHP -= totaldmg;
	}
}
