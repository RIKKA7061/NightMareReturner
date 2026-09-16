using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
//using UnityEditor.Animations;

// [NR] 변경 요약
//  - 대화(E) 후 최대 5초 이동이 막히던 문제: 매 FixedUpdate마다 TicToc 코루틴이 쌓이던 구조를 시간값 방식으로 교체
//  - UI를 클릭할 때 공격/문 이동이 같이 일어나던 문제 차단 (메뉴/증강 선택 중 입력 차단)
//  - 증강/영구 강화 배율(이동 속도, 공격 속도, 재사용 대기) 적용
//  - 구르기 연출(잔상) 및 구르기 증강(충격파, 보호막) 연결
//  - 체력바/쿨타임 HUD용 공개 값 추가
public class PlayerAction : MonoBehaviour
{
    [Header("이놈이 거리에 있는 플레이어야?")]
    public bool isGeoRiPlayer;

    [Header("PlayerAction 스크립트")]
    public Player playerScript;

    public float walkSpeed;              // 걷기 속도
    public TextMeshProUGUI speed_ui;          // 이동속도 보여주기
    public TextMeshProUGUI As_ui;             // 공격속도 보여주기

    public float defaultSpeed;           // 기본 속도(걷기 속도와 동일해야 함)
    public float slideSpeed;            // 슬라이드 속도
    public float slideDuration = 0.3f;        // 슬라이드 지속 시간 (0.3초)
    public float slideCooldown = 1f;          // 슬라이드 후 쿨타임 (1초)
    private float slideTime = 0f;             // 현재 슬라이드 시간
    private float cooldownTime = 0f;          // 현재 쿨타임 시간
    public float skillAttackCooldown = 2f;    // 첫 번째 스킬 쿨타임 2초
    public float skillAttack2Cooldown = 3f;   // 두 번째 스킬 쿨타임 3초
    private float skillAttackTime = 0f;       // 첫 번째 스킬 사용 가능 시간
    private float skillAttack2Time = 0f;      // 두 번째 스킬 사용 가능 시간

    public Collider2D playerCollider2D;

    Vector2 moveInput;  // 플레이어의 입력을 저장하는 변수
    Vector3 dirVec;     // 플레이어의 방향을 저장하는 변수

    [Header("조사 레이저")]
    public float Length = 0.7f;  // 레이저 거리

    [Header("공격 범위")]
    public BoxCollider2D left;      // 왼쪽 공격 범위 콜라이더
    public BoxCollider2D right;     // 오른쪽 공격 범위 콜라이더
    public BoxCollider2D LargeLeft; // 더 넓은 범위의 왼쪽 공격 범위 콜라이더
    public BoxCollider2D LargeRight; // 더 넓은 범위의 오른쪽 공격 범위 콜라이더

	[Header("대화창")]
    public TalkManager talkManager;  // 대화 매니저 참조
    public GameObject DialogSet;     // 대화창
    AudioManager audioManager;

    [Header("잽 경직 시간")]
    public float Jab;

	[Header("스킬 경직 시간")]
	public float Skill;

	[Header("궁극기 경직 시간")]
	public float Ultimite;

	[Header("현재 공격중 여부")]
	public bool isAtking = false;

	Rigidbody2D rigid;               // Rigidbody2D 컴포넌트 참조
    Animator animator;               // Animator 컴포넌트 참조
    SpriteRenderer sp;               // SpriteRenderer 컴포넌트 참조

    private GameObject scanObject;   // 스캔한 오브젝트

    private GameObject scanTP_Object;  // 스캔한 TP 오브젝트

    private Collider2D playerCollider; // 플레이어의 콜라이더

	public Transform player;

    [Header("아이템")]
    public Collider2D[] myItemColliders; // 클릭해서 넘어가는 것

	[Header("메인 카메라")]
	public Camera mainCamera; // 메인 카메라 (화면 좌표 변환용)

    [Header("지금 현재 스킬or궁극기를 사용 중인가?")]
    public bool isUsingSkillorUltimate;

	// 애니메이터 오버라이드
	//public AnimatorController animatorController;
	public AnimatorOverrideController overrideController;
	public AnimatorOverrideController overrideController2;
	public bool isRoundUP;

	public int originalLayerID = 6; // 기본 레이어 ID (Default는 0)
    public int shiftedLayerID = 10; // Shift 키를 눌렀을 때 변경할 레이어 ID

    private SpriteRenderer spriteRenderer;
    private Coroutine revertLayerCoroutine;

    // 이동 여부 확인 변수 (애니메이터와 연동)
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
	// 슬라이딩 여부 확인 변수 (애니메이터와 연동)
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

    // [NR] 현재 공격 종류 (0 기본, 1 특수, 2 궁극기) — 증강 피해 계산에 사용
    public int CurrentAttackStyle { get; private set; }
    public int SwingId { get; private set; }

    // [NR] HUD 표시용 재사용 대기 비율 (0 = 사용 가능, 1 = 방금 사용)
    float lastSkillDuration = 1f, lastUltDuration = 1f, lastSlideCooldown = 1f;
    public float SkillCooldownRatio => skillAttackTime > 0 ? Mathf.Clamp01(skillAttackTime / Mathf.Max(0.01f, lastSkillDuration)) : 0f;
    public float UltCooldownRatio => skillAttack2Time > 0 ? Mathf.Clamp01(skillAttack2Time / Mathf.Max(0.01f, lastUltDuration)) : 0f;
    public float SlideCooldownRatio => IsSliding ? 1f : isCooldown ? Mathf.Clamp01(cooldownTime / Mathf.Max(0.01f, lastSlideCooldown)) : 0f;
    public float JabCooldownRatio
    {
        get
        {
            float interval = Mathf.Max(0.2f, jabCooldown) * NRStats.AttackIntervalMultiplier;
            float remain = (lastAttackTime + interval) - Time.time;
            return remain > 0 ? Mathf.Clamp01(remain / interval) : 0f;
        }
    }

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
			animator.SetBool(AnimationStrings.canMove, value); // 애니메이터와 연동하여 canMove 상태 설정
		}
	}

    void Awake()
    {
        // 필요한 컴포넌트들을 가져옴
        rigid = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        sp = GetComponent<SpriteRenderer>();
        playerCollider = GetComponent<Collider2D>(); // 플레이어의 콜라이더 가져오기
        var audioGo = GameObject.FindGameObjectWithTag("Audio");
        audioManager = audioGo != null ? audioGo.GetComponent<AudioManager>() : null;
    }

	private void Start()
	{
		canMove = true;
	}

	public void DefaultAnimatorController()
	{
        GetComponent<Animator>().runtimeAnimatorController = overrideController2;
    }


	void Update()
    {
		// 구슬스테이지 false 일시 && 플레이어의 구슬이 1이상 일시
		if (isRoundUP == false && Player.round >= 1)
		{
			GetComponent<Animator>().runtimeAnimatorController = overrideController;
			isRoundUP = true;
		}

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
                StopSliding();  // 슬라이드가 끝나면 슬라이드 종료
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

    // [NR] 입력을 받아도 되는지 (메뉴/증강 선택/크레딧/UI 클릭 중에는 무시)
    bool InputBlocked(bool pointerAction)
    {
        if (NRUIState.IsGameplayBlocked) return true;
        if (pointerAction && NRInput.PointerOverInteractiveUI()) return true;
        return false;
    }

    public bool RealStop = false; // 멈추는 거 Update 함수에 적용
	public void OnInteract(InputAction.CallbackContext context)// 대화창 E키
	{
        if (context.started)
        {
            if (InputBlocked(false)) return;

            // [NR] 대사가 한 글자씩 나오는 중이면 먼저 전체 문장을 보여줌
            if (NRDialogueSkin.TryCompleteTyping()) return;

            // 대화 대상 찾을 시
            if (scanObject != null)// 대상을 찾았을 경우 출력
			{
                TicTocDealyTime = 0.2f; // [NR] 5초 → 0.2초 (대화 중 이동은 isDialoging이 이미 막음)

				PleaseStopPlayer();
				talkManager.DialogAction(scanObject);
			}
        }
    }

    public bool timeStopu = false;

    public Transform tempPlayerPos;

    public float tempTime = 1.0f; // 시간 값
	IEnumerator DisableCollider(BoxCollider2D collider, int AtkStyle)// 공격판정 생성
	{
        isAtking = true;
        CurrentAttackStyle = AtkStyle;
        SwingId++; // [NR] 한 번의 공격이 같은 적에게 여러 번 맞지 않도록
		canMove = false;
		PleaseStopPlayer();
		if (AtkStyle == 0 && Time.time > tempTime && !isGeoRiPlayer) // 기본공격
        {
		    animator.SetTrigger(AnimationStrings.attackTrigger);  // 공격 애니메이션 실행
            PlaySfx(1);
            tempTime = Time.time + 0.15f;
			collider.enabled = true;                              // 왼쪽 또는 오른쪽 판정 켜기
			yield return new WaitForSeconds(0.1f);                // 콜라이더를 0.1초 동안 활성화
			collider.enabled = false;                             // 왼쪽 또는 오른쪽 판정 끄기
            canMove = true;
		}
        else if(AtkStyle == 1 && !isGeoRiPlayer) // 스킬
        {
			isUsingSkillorUltimate = true;              // 지금은 스킬 사용하고 있다.
			yield return new WaitForSeconds(0.6f);      // 애니메이션 타임이 끝나기 기다리는 중
			playerScript.Atk *= playerScript.SkillAtk;  // 공격력 두배 증가
			collider.enabled = true;                    // 공격 콜라이더 활성화
			yield return new WaitForSeconds(0.1f);      // 콜라이더를 0.1초 동안 활성화
			collider.enabled = false;                   // 공격 콜라이더 비활성화
			playerScript.Atk /= playerScript.SkillAtk;  // 공격력 초기화
			isUsingSkillorUltimate = false;             // 지금은 스킬 사용하고 있지 않다.
			canMove = true;
		}
		else if (AtkStyle == 2 && !isGeoRiPlayer) // 궁극기
		{

			isUsingSkillorUltimate = true;                  // 지금은 스킬 사용하고 있다.
			yield return new WaitForSeconds(0.9f);          // 애니메이션 타임이 끝나기 기다리는 중
			playerScript.Atk *= playerScript.UltimitAtk;    // 플레이어 공격력 증가
            itemManager.HammerBuff();                       // 해머 공격력 상승 (단, 버프중 일때)
			collider.enabled = true;                        // 공격 콜라이더 활성화
            NRCombatFX.Shake(0.15f, 0.12f);                 // [NR]
			yield return new WaitForSeconds(0.1f);          // 콜라이더를 0.1초 동안 활성화
			collider.enabled = false;                       // 공격 콜라이더 비활성화
            itemManager.HammerDeBuff();                     // 해머 공격력 하락 (단, 버프중 일때)
			playerScript.Atk /= playerScript.UltimitAtk;    // 공격력 초기화
			isUsingSkillorUltimate = false;                 // 지금은 스킬 사용하고 있지 않다.
			canMove = true;
		}
		canMove = true;
        isAtking = false;
	}

    public float TicTocDealyTime;
    float stopUntil = 0f; // [NR] 정지 종료 시각

    void PlaySfx(int index)
    {
        if (audioManager != null && audioManager.audio != null && index < audioManager.audio.Length && audioManager.audio[index] != null)
            audioManager.PlayerSFX(audioManager.audio[index]);
    }

	// 플레이어 이동
	void FixedUpdate()
	{
        // [NR] 공격/상호작용 직후 잠깐 멈추는 연출은 유지하되, 코루틴을 매 프레임 쌓지 않음
        timeStopu = Time.time < stopUntil;

        //canMove가 true,
        //isDialoging(대화창 열림 여부)가 false 일때 움직일수 있음 && 타임스톱우가 false일때만
        bool dialoging = talkManager != null && talkManager.isDialoging;
        if (canMove == true && dialoging == false && timeStopu == false)
        {
			rigid.velocity = new Vector2(moveInput.x, moveInput.y) * walkSpeed * NRStats.MoveSpeedMultiplier;  // 이동 처리
        }
        else if (!IsSliding)
        {
            rigid.velocity = Vector2.zero;
        }
		// 공격이 끝났을 경우 방향키 입력이 있으면 이동하도록 설정
		if (canMove && !IsSliding && !isUsingSkillorUltimate)
		{
			IsMoving = moveInput != Vector2.zero;  // 계속 이동할 수 있도록 상태 업데이트
			SetFacingDirection(moveInput);  // 이동 방향 설정
		}

		// 플레이어 방향을 따라 레이저 쏘기
		Debug.DrawRay(rigid.position, dirVec * Length, new Color(0, 1, 0));
        RaycastHit2D rayHit = Physics2D.Raycast(rigid.position, dirVec, Length, LayerMask.GetMask("Object"));

        // 레이 맞으면!
        if (rayHit.collider != null) scanObject = rayHit.collider.gameObject;
        else scanObject = null;

        if (speed_ui != null) speed_ui.text = (walkSpeed * NRStats.MoveSpeedMultiplier).ToString("0.#"); // 이동속도
        if (As_ui != null) As_ui.text = (3 / (Mathf.Max(0.2f, jabCooldown) * NRStats.AttackIntervalMultiplier)).ToString("F2");           // 공격속도
    }

    /// <summary>[NR] 대화 대상이 앞에 있는지 (E 안내 표시용)</summary>
    public GameObject ScannedObject => scanObject;

    public void OnMove(InputAction.CallbackContext context)// 이동 입력 처리
	{
        if (canMove)
        {
            moveInput = context.ReadValue<Vector2>();

            IsMoving = moveInput != Vector2.zero;  // 움직임 상태 확인

            SetFacingDirection(moveInput);  // 방향 설정
        }
        else if (context.canceled)
        {
            moveInput = Vector2.zero; // [NR] 경직 중 키를 뗐을 때 입력이 남아 미끄러지던 문제 방지
        }
    }



    void SetFacingDirection(Vector2 moveInput)// 이동 방향 설정
	{
        bool dialoging = talkManager != null && talkManager.isDialoging;
        //x값이 0보다 큼 && 오른쪽 안바라봄 && 대화중 아님
        if (moveInput.x > 0 && dialoging == false)
        {
            //IsFacingRight = true;
            dirVec = Vector3.right;  // 오른쪽 방향 설정
            sp.flipX = false; // 캐릭터를 오른쪽으로 바라보게 설정
        }
		//x값이 0보다 큼 && 오른쪽 바라봄 && 대화중 아님
		else if (moveInput.x < 0 && dialoging == false)
        {
            //IsFacingRight = false;
            dirVec = Vector3.left;  // 왼쪽 방향 설정
            sp.flipX = true; // 캐릭터를 왼쪽으로 바라보게 설정
        }
    }

    public void OnSlide(InputAction.CallbackContext context) // 슬라이드 입력 처리
    {
        if (context.started && !isCooldown && !IsSliding)
        {
            if (InputBlocked(false)) return;
            if (_isMoving)
            {
                StartSliding(); // 슬라이드 시작
            }
        }
    }

    private void StartSliding() // 슬라이드 시작
    {
        IsSliding = true;
        slideTime = slideDuration; // 슬라이드 지속 시간 설정
        walkSpeed = slideSpeed;    // 슬라이드 속도 설정
        gameObject.layer = shiftedLayerID;
        // 슬라이드가 끝나면 자동으로 종료 처리
        Invoke(nameof(StopSliding), slideDuration); // slideDuration만큼 대기 후 StopSliding 호출
        revertLayerCoroutine = StartCoroutine(RevertLayerAfterDelay(0.5f));

        // [NR] 구르기 연출 + 증강
        NRCombatFX.Afterimage(sp, NRStats.DashInvuln ? NRPalette.Anxiety.WithAlpha(0.55f) : NRPalette.Cyan.WithAlpha(0.45f), 4, 0.06f);
        if (NRStats.DashShield > 0f) NRStats.AddShield(NRStats.DashShield);
    }

    private void StopSliding() // 슬라이드 종료
    {
        if (!IsSliding) return; // [NR] Invoke와 Update에서 두 번 호출되던 것 방지
        IsSliding = false;
        walkSpeed = defaultSpeed; // 기본 속도로 복원
        isCooldown = true;
        lastSlideCooldown = slideCooldown * NRStats.DashCooldownMultiplier;
        cooldownTime = lastSlideCooldown; // 슬라이드 쿨타임 설정

        // [NR] 잔상 증강: 구르기 끝 지점 충격파
        if (NRStats.DashShockDmg > 0f && !isGeoRiPlayer)
        {
            Vector2 center = transform.position;
            NRCombatFX.DeathBurst(center, NRPalette.Anxiety, 14);
            foreach (var c in Physics2D.OverlapCircleAll(center, 1.8f))
            {
                var target = c.GetComponentInParent<INRDamageable>();
                if (target == null || target.IsDead) continue;
                if (c.GetComponentInParent<Player>() != null) continue;
                target.ReceiveDamage(Mathf.RoundToInt(NRStats.DashShockDmg), false, NRDamageKind.Shockwave);
            }
        }
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

    IEnumerator PlaySoundWithDelay()
    {
        yield return new WaitForSeconds(0.3f); // Wait for the specified delay
        PlaySfx(3);                     // Play the sound
    }

    public float jabCooldown = 0.3f;  // 잽 공격 쿨타임 (공속을 의미)
	private float lastAttackTime = 0f;  // 마지막 공격 시간을 기록할 변수


	public void OnAttack(InputAction.CallbackContext context)// 일반 공격 (잽)
	{
		if (jabCooldown <= 0.2f)
        {
            jabCooldown = 0.2f;
        }
		// 마우스 클릭을 시작 할때 && 스킬이나 궁극기를 사용하고 있지 않을때 공격 가능
		if (context.started && isUsingSkillorUltimate == false && isAtking == false && Time.time >= lastAttackTime + jabCooldown * NRStats.AttackIntervalMultiplier)
        {
            if (InputBlocked(true)) return;

            Vector2 mousePosition = Mouse.current.position.ReadValue();           // 마우스 위치 가져오기
            Vector3 worldPosition = mainCamera.ScreenToWorldPoint(mousePosition); // 화면 좌표 -> 월드 좌표 변환
            float playerPositionX = transform.position.x;                         // 플레이어의 현재 x 좌표
            worldPosition.z = 0f; // 카메라의 z축을 0으로 설정 (2D 게임에서의 좌표)

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
				if (item == null) continue;
				if (item.bounds.Contains(worldPosition)) // 클릭한 위치가 아이템 콜라이더 범위 안에 있을 때
				{
					teleport teleport = item.GetComponent<teleport>();
					SceneChangeOnCollision sceneChangeOnCollision = item.GetComponent<SceneChangeOnCollision>();
					if (teleport != null)
					{
                        PlaySfx(6);
                        teleport.MovePlayer(playerCollider2D); // 아이템의 OnItemClicked 함수 실행
					}
                    else if (sceneChangeOnCollision != null)
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
            if (InputBlocked(true)) return;

            Vector2 mousePosition = Mouse.current.position.ReadValue(); // 마우스 위치 가져오기
            Vector3 worldPosition = mainCamera.ScreenToWorldPoint(mousePosition); // 화면 좌표 -> 월드 좌표 변환
            float playerPositionX = transform.position.x; // 플레이어의 현재 x 좌표

            if (skillAttackTime <= 0)
            {
                // 거리 출신 플레이어가 아닐 때
                if (!isGeoRiPlayer)
                {
                    animator.SetTrigger(AnimationStrings.skillAttackTrigger);  // 스킬 애니메이션 실행
                }
                lastSkillDuration = skillAttackCooldown * NRStats.SkillCooldownMultiplier;
                skillAttackTime = lastSkillDuration;  // 쿨타임 시작


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
            if (InputBlocked(false)) return;

            if (skillAttack2Time <= 0)
            {
                // 거리 출신 플레이어가 아닐시
                if (!isGeoRiPlayer)
                {
                    animator.SetTrigger(AnimationStrings.skillAttackTrigger2);  // 스킬 애니메이션 실행
                    StartCoroutine(PlaySoundWithDelay());
                }

                lastUltDuration = skillAttack2Cooldown * NRStats.UltCooldownMultiplier;
                skillAttack2Time = lastUltDuration;  // 쿨타임 시작

				int atkStyle = 2;      // 궁극기 공격 설정
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

    // 정지 함수 Like 스톱
    void PleaseStopPlayer()
    {
		rigid.velocity = Vector2.zero;
        IsMoving = false;

		tempPlayerPos = playerPos;
        stopUntil = Mathf.Max(stopUntil, Time.time + TicTocDealyTime); // [NR]
        timeStopu = true;
	}
}
