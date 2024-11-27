using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SocialPlatforms;
using UnityEditor;
using Unity.VisualScripting;


public class PlayerAction : MonoBehaviour
{
    public Player playerScript;

    public float walkSpeed = 5f;              // 걷기 속도
    public TextMeshProUGUI speed_ui;          // 이동속도 보여주기 
    public TextMeshProUGUI As_ui;             // 공격속도 보여주기

    public float defaultSpeed = 5f;           // 기본 속도(걷기 속도와 동일해야 함)
    public float slideSpeed = 10f;            // 슬라이드 속도
    public float slideDuration = 0.3f;        // 슬라이드 지속 시간 (0.3초)
    public float slideCooldown = 1f;          // 슬라이드 후 쿨타임 (1초)
    private float slideTime = 0f;             // 남은 슬라이드 시간
    private float cooldownTime = 0f;          // 남은 쿨타임 시간
    public float skillAttackCooldown = 2f;    // 첫 번째 스킬 쿨타임 2초
    public float skillAttack2Cooldown = 3f;   // 두 번째 스킬 쿨타임 3초
    private float skillAttackTime = 0f;       // 첫 번째 스킬 사용 가능 시간
    private float skillAttack2Time = 0f;      // 두 번째 스킬 사용 가능 시간

    public Collider2D playerCollider2D;

    Vector2 moveInput;  // 플레이어의 입력을 저장하는 변수
    Vector3 dirVec;     // 플레이어의 방향을 저장하는 변수

    [Header("실선 추적기")]
    public float Length = 0.7f;  // 감지할 거리

    [Header("공격 범위")]
    public BoxCollider2D left;      // 왼쪽 공격 범위 콜라이더
    public BoxCollider2D right;     // 오른쪽 공격 범위 콜라이더
    public BoxCollider2D LargeLeft; // 더 넓은 영역을 지닌 왼쪽 공격 범위 콜라이더
    public BoxCollider2D LargeRight; // 더 넓은 영역을 지닌 왼쪽 공격 범위 콜라이더

	[Header("대화창")]
    //public TextMeshProUGUI Dialog_UI_text;
    //private string dialog_text;
    public TalkManager talkManager;  // 대화 매니저 참조
    public GameObject DialogSet;     // 대화창

    [Header("잽 경직 시간")]
    public float Jab;

	[Header("스킬 경직 시간")]
	public float Skill;

	[Header("궁극기 경직 시간")]
	public float Ultimite;

	[Header("지금 공격중 여부")]
	public bool isAtking = false;

	Rigidbody2D rigid;               // Rigidbody2D 컴포넌트 참조
    Animator animator;               // Animator 컴포넌트 참조
    SpriteRenderer sp;               // SpriteRenderer 컴포넌트 참조

    private GameObject scanObject;   // 감지된 오브젝트

    private GameObject scanTP_Object;  // 감지된 TP 오브젝트

    private Collider2D playerCollider; // 플레이어의 콜라이더

	public Transform player;

    [Header("텔포들")]
    public Collider2D[] myItemColliders; // 클릭해서 넘어가는 용

	[Header("메인 카메라")]
	public Camera mainCamera; // 메인 카메라 (화면 좌표 변환용)

    [Header("지금 현재 스킬or궁극기를 사용 중인가?")]
    public bool isUsingSkillorUltimate;


    public int originalLayerID = 6; // 기본 레이어 ID (Default는 0)
    public int shiftedLayerID = 10; // Shift 키를 눌렀을 때 적용할 레이어 ID

    private SpriteRenderer spriteRenderer;
    private Coroutine revertLayerCoroutine;

    // 이동 상태 확인 변수 (애니메이터와 연동)
    [SerializeField]
    private bool _isMoving = false;
    public bool IsMoving
    {
        get
        {
            return _isMoving;
        }
        private set
        {
            _isMoving = value;
            animator.SetBool(AnimationStrings.isMoving, value);  // 애니메이션 상태 변경
        }
    }
	// 슬라이딩 상태 확인 변수 (애니메이터와 연동)
	[SerializeField]
    private bool _isSliding = false;
    public bool IsSliding
    {
        get
        {
            return _isSliding;
        }
        private set
        {
            _isSliding = value;
            animator.SetBool(AnimationStrings.isSliding, value);  // 애니메이션 상태 변경
        }
    }
    private bool isCooldown = false;  // 슬라이드 쿨타임 중인지 여부
    public bool _isFacingRight = true;// 캐릭터가 오른쪽을 보고 있는지 확인하는 변수

    public Transform playerPos;

    public ItemManager itemManager;     // 아이템 매니저 스크립트

	


	public bool IsFacingRight
    {
        get { return _isFacingRight; }
        private set
        {
            if (_isFacingRight != value)
            {
                transform.localScale *= new Vector2(-1, 1); // 캐릭터의 방향을 뒤집음
            }

            _isFacingRight = value;
        }
    }

    // canMove: 플레이어가 이동할 수 있는지 여부를 나타냄
    public bool canMove
    {
        get
        {
            return animator.GetBool(AnimationStrings.canMove);
        }
        set
        {
			animator.SetBool(AnimationStrings.canMove, value); // 애니메이터와 연동하여 canMove 값을 설정
		}
	}

    void Awake()
    {
        // 필요한 컴포넌트들을 가져옴
        rigid = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        sp = GetComponent<SpriteRenderer>();
        playerCollider = GetComponent<Collider2D>(); // 플레이어의 콜라이더 가져오기
    }

    void Update()
    {

        if (isCooldown)
        {
            cooldownTime -= Time.deltaTime;
            if (cooldownTime <= 0)
            {
                isCooldown = false;  // 쿨타임 종료
            }
        }

        if (IsSliding)
        {
            slideTime -= Time.deltaTime;
            if (slideTime <= 0)
            {
                StopSliding();  // 슬라이드가 끝나면 슬라이딩 종료
            }
        }

        // 스킬 쿨타임 감소
        if (skillAttackTime > 0)
        {
            skillAttackTime -= Time.deltaTime;
        }

        if (skillAttack2Time > 0)
        {
            skillAttack2Time -= Time.deltaTime;
        }
    }
    public bool RealStop = false; // 멈추는 것 Update 함수에 넣음
	public void OnInteract(InputAction.CallbackContext context)// 대화창 E키
	{
        if (context.started)
        {
            // 대화 대상 찾을 시
            if (scanObject != null)// 대상을 찾았을 때의 대사
			{
                TicTocDealyTime = 5f;

				PleaseStopPlayer();
				talkManager.DialogAction(scanObject);
			}
        }
    }

    public bool timeStopu = false;

    public Transform tempPlayerPos;

    public float tempTime = 1.0f; // 시간 텀
	IEnumerator DisableCollider(BoxCollider2D collider, int AtkStyle)// 실질적인 공격
	{
        isAtking = true;
		canMove = false;
		PleaseStopPlayer();
		if (AtkStyle == 0 && Time.time > tempTime) // 기본공격
        {
			animator.SetTrigger(AnimationStrings.attackTrigger);  // 공격 애니메이션 실행
			tempTime = Time.time + 0.15f;
			collider.enabled = true;                              // 왼쪽 또는 오른쪽 공격 켜기
			yield return new WaitForSeconds(0.1f);                // 콜라이더를 0.1초 동안 활성화
			collider.enabled = false;                             // 왼쪽 또는 오른쪽 공격 끄기
		}
        else if(AtkStyle == 1) // 스킬
        {
			isUsingSkillorUltimate = true;              // 지금은 스킬 사용하고 있다.
			yield return new WaitForSeconds(0.6f);      // 애니메이션 타격할 때까지 기다리는 중
			playerScript.Atk *= playerScript.SkillAtk;  // 공격력 두배 증가
			collider.enabled = true;                    // 공격 콜라이더 활성화
			yield return new WaitForSeconds(0.1f);      // 콜라이더를 0.1초 동안 활성화
			collider.enabled = false;                   // 공격 콜라이더 비활성화
			playerScript.Atk /= playerScript.SkillAtk;  // 공격력 초기화
			isUsingSkillorUltimate = false;             // 지금은 스킬 사용하고 있지 않다.
		}
		else if (AtkStyle == 2) // 궁극기
		{
            
			isUsingSkillorUltimate = true;                  // 지금은 스킬 사용하고 있다.
			yield return new WaitForSeconds(0.9f);          // 애니메이션 타격할 때까지 기다리는 중
			playerScript.Atk *= playerScript.UltimitAtk;    // 플레이어 공격력 증가
            itemManager.HammerBuff();                       // 해머 공격력 상승 (단, 버프중 일때)
			collider.enabled = true;                        // 공격 콜라이더 활성화
			yield return new WaitForSeconds(0.1f);          // 콜라이더를 0.1초 동안 활성화
			collider.enabled = false;                       // 공격 콜라이더 비활성화
            itemManager.HammerDeBuff();                     // 해머 공격력 하락 (단, 버프중 일때)
			playerScript.Atk /= playerScript.UltimitAtk;    // 공격력 초기화
			isUsingSkillorUltimate = false;                 // 지금은 스킬 사용하고 있지 않다.
		}
		canMove = true;
        isAtking = false;
	}

    public float TicTocDealyTime;
    IEnumerator TicToc()
    {
        IsMoving = false;                       // 움직이고 있는 여부 끄기
        //canMove = false;                        // 움직일수 있는 여부 끄기
		//moveInput.x = 0;                        // input 값에 0 
		//moveInput.y = 0;
		playerPos = tempPlayerPos;
		timeStopu = true;
		yield return new WaitForSeconds(TicTocDealyTime);
        timeStopu = false;
        //canMove = true;
	}

	// 플레이어 이동
	void FixedUpdate()
	{
        //canMove는 true,
        //isDialoging(대화창 열린 여부)가 false 일때 움직일수 있음 && 타임스토푸가 false일때만
        if (canMove == true && talkManager.isDialoging == false && timeStopu == false)
        {
			rigid.velocity = new Vector2(moveInput.x * walkSpeed, moveInput.y * walkSpeed);  // 이동 처리
        }
        else if (IsMoving == false)
        {
            StartCoroutine(TicToc());
		}
		// 공격이 끝나고 계속 방향키 입력을 받으면 이동하도록 설정
		if (canMove && !IsSliding && !isUsingSkillorUltimate)
		{
			IsMoving = moveInput != Vector2.zero;  // 계속 이동할 수 있도록 상태 업데이트
			SetFacingDirection(moveInput);  // 이동 방향 설정
		}
        if (IsMoving == true && moveInput.x == 0 && moveInput.y == 0)
        {
            TicToc();
        }

		// 플레이어 주위의 얇은 선으로 감지
		Debug.DrawRay(rigid.position, dirVec * Length, new Color(0, 1, 0));
        RaycastHit2D rayHit = Physics2D.Raycast(rigid.position, dirVec, Length, LayerMask.GetMask("Object"));

        // 무언가 감지됨!
        if (rayHit.collider != null) scanObject = rayHit.collider.gameObject;
        else scanObject = null;

        // 왼쪽 화살표 = -1, 오른쪽 화살표 = 1
        // Debug.Log(moveInput.x);

        speed_ui.text = walkSpeed.ToString(); // 이동속도
        As_ui.text = (3/jabCooldown).ToString("F2");           // 공격속도
    }

    public void OnMove(InputAction.CallbackContext context)// 이동 입력 처리
	{
        if (canMove)
        {
            moveInput = context.ReadValue<Vector2>();

            IsMoving = moveInput != Vector2.zero;  // 움직임 여부 확인

            SetFacingDirection(moveInput);  // 방향 설정
        }
    }

    

    void SetFacingDirection(Vector2 moveInput)// 이동 방향 설정
	{
        //x값이 0보다 큼 && 오른쪽 안바라봄 && 대화가 끝남
        if (moveInput.x > 0 && talkManager.isDialoging == false)
        {
            //IsFacingRight = true;
            dirVec = Vector3.right;  // 오른쪽 방향 설정
            sp.flipX = false; // 캐릭터를 오른쪽으로 바라보게 설정
        }
		//x값이 0보다 큼 && 오른쪽 바라봄 && 대화가 끝남
		else if (moveInput.x < 0 && talkManager.isDialoging == false)
        {
            //IsFacingRight = false;
            dirVec = Vector3.left;  // 왼쪽 방향 설정
            sp.flipX = true; // 캐릭터를 왼쪽으로 바라보게 설정
        }
    }

    public void OnSlide(InputAction.CallbackContext context) // 슬라이딩 입력 처리
    {
        if (context.started && !isCooldown && !IsSliding)
        {
            if (_isMoving)
            {
                StartSliding(); // 슬라이딩 시작
            }
        }
    }

    private void StartSliding() // 슬라이딩 시작
    {
        IsSliding = true;
        slideTime = slideDuration; // 슬라이드 지속 시간 설정
        walkSpeed = slideSpeed;    // 슬라이드 속도 적용
        gameObject.layer = shiftedLayerID;
        // 슬라이딩이 끝나면 자동으로 종료 처리
        Invoke(nameof(StopSliding), slideDuration); // slideDuration만큼 대기 후 StopSliding 호출
        revertLayerCoroutine = StartCoroutine(RevertLayerAfterDelay(0.5f));
    }

    private void StopSliding() // 슬라이딩 종료
    {
        IsSliding = false;
        walkSpeed = defaultSpeed; // 기본 속도로 복귀
        isCooldown = true;
        cooldownTime = slideCooldown; // 슬라이드 쿨타임 적용

        // 쿨타임 타이머를 업데이트하는 코루틴 등을 사용할 수도 있음
    }

    void SetLayer(int layerID)
    {
        gameObject.layer = layerID;
    }

    IEnumerator RevertLayerAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SetLayer(originalLayerID);
    }


    public float jabCooldown = 0.3f;  // 잽 공격 쿨타임 (공속을 의미)
	private float lastAttackTime = 0f;  // 마지막 공격 시간이 저장될 변수


	public void OnAttack(InputAction.CallbackContext context)// 일반 공격 (잽)
	{
        if (jabCooldown <= 0.2f)
        {
            jabCooldown = 0.2f;
        }
		// 마우스 클릭을 시작 할때 && 스킬이나 궁극기를 사용하고 있지 않을시 잽을 실행
		if (context.started && isUsingSkillorUltimate == false && isAtking == false && Time.time >= lastAttackTime + jabCooldown)
        {
            Vector2 mousePosition = Mouse.current.position.ReadValue();           // 마우스 위치 가져오기
            Vector3 worldPosition = mainCamera.ScreenToWorldPoint(mousePosition); // 화면 좌표 -> 월드 좌표 변환
            float playerPositionX = transform.position.x;                         // 플레이어의 현재 x 좌표
            worldPosition.z = 0f; // 카메라의 z값을 0으로 설정 (2D 공간에서의 좌표)

            int atkStyle = 0; // 기본 공격 스타일

			TicTocDealyTime = Jab;                               // 몇초동안 경직되어 있을래?

			// 화면 기준으로 왼쪽 클릭 시
			if (worldPosition.x < playerPositionX)
			{
                sp.flipX = true; // 캐릭터를 왼쪽으로 바라보게 설정
                StartCoroutine(DisableCollider(left, atkStyle)); // 왼쪽 공격 활성화
            }
			// 화면 기준으로 오른쪽 클릭 시
			else
			{
                sp.flipX = false; // 캐릭터를 오른쪽으로 바라보게 설정
                StartCoroutine(DisableCollider(right, atkStyle)); // 오른쪽 공격 활성화
            }

			lastAttackTime = Time.time;  // 마지막 공격 시간 갱신


			// 아이템이 있는 콜라이더 객체 (예시로 myItemCollider를 사용)

			foreach (var item in myItemColliders)
            {
				if (item.bounds.Contains(worldPosition)) // 클릭한 위치가 아이템 콜라이더 범위 안에 있을 때
				{
					teleport teleport = item.GetComponent<teleport>();
					SceneChangeOnCollision sceneChangeOnCollision = item.GetComponent<SceneChangeOnCollision>();
					if (teleport != null)
					{
						teleport.MovePlayer(playerCollider2D); // 아이템의 OnItemClicked 함수 실행
					}
                    else if (teleport == null)
                    {
                        sceneChangeOnCollision.SceneChangeHamSu();
					}
				}
			}
        }
    }

    public void OnSkillAttack(InputAction.CallbackContext context)// 스킬
	{
		if (context.started)  // 쿨타임이 0일 때만 스킬 발동
        {
            Vector2 mousePosition = Mouse.current.position.ReadValue(); // 마우스 위치 가져오기
            Vector3 worldPosition = mainCamera.ScreenToWorldPoint(mousePosition); // 화면 좌표 -> 월드 좌표 변환
            float playerPositionX = transform.position.x; // 플레이어의 현재 x 좌표

            if (skillAttackTime <= 0)
            {
                animator.SetTrigger(AnimationStrings.skillAttackTrigger);  // 스킬 애니메이션 실행
                skillAttackTime = skillAttackCooldown;  // 쿨타임 적용


				int atkStyle = 1; // 스킬 공격 스타일

				TicTocDealyTime = Skill;                     // 몇초동안 경직되어 있을래?

				if (worldPosition.x < playerPositionX)
                {
                    sp.flipX = true; // 캐릭터를 왼쪽으로 바라보게 설정
                    StartCoroutine(DisableCollider(LargeLeft, atkStyle)); // 왼쪽 공격 활성화

                }
                else
                {
                    sp.flipX = false; // 캐릭터를 오른쪽으로 바라보게 설정
                    StartCoroutine(DisableCollider(LargeRight, atkStyle)); // 오른쪽 공격 활성화
                }
			}
		}
    }

    public void OnSkillAttack2(InputAction.CallbackContext context)// 궁극기
	{
		if (context.started)  // 쿨타임이 0일 때만 스킬 발동
        {
            if (skillAttack2Time <= 0)
            {
                animator.SetTrigger(AnimationStrings.skillAttackTrigger2);  // 스킬 애니메이션 실행
                skillAttack2Time = skillAttack2Cooldown;  // 쿨타임 적용

				int atkStyle = 2;      // 궁극기 공격 패턴
									   // 방향에 따라 적절한 BoxCollider2D 활성화
				TicTocDealyTime = Ultimite;                      // 몇초동안 경직되어 있을래?
				if (sp.flipX)
				{
					StartCoroutine(DisableCollider(LargeLeft, atkStyle));  // 왼쪽 공격 활성화
				}
				else
				{
					StartCoroutine(DisableCollider(LargeRight, atkStyle));  // 오른쪽 공격 활성화
				}
			}
		}
    }

    // 경직 함수 Like 존야
    void PleaseStopPlayer()
    {
		rigid.velocity = Vector2.zero;

		tempPlayerPos = playerPos;
		StartCoroutine(TicToc());
	}
}
