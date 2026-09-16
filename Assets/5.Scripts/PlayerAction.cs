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
//  - 구르기: 멈춰 있어도 마우스 방향으로 구르기, 구르는 동안 모든 적 공격 회피, 연출/증강 연결
//  - 조작 방식 2종 (기본 / LoL식 — NRControls가 입력을 넣어줌), 근접 3타 콤보, 원거리 캐릭터 사격
//  - 대화 넘기기(E/좌클릭/Space), NPC 클릭 대화
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

    private Collider2D playerCollider; // 플레이어의 콜라이더

	public Transform player;

    [Header("아이템")]
    public Collider2D[] myItemColliders; // 클릭해서 넘어가는 것

	[Header("메인 카메라")]
	public Camera mainCamera; // 메인 카메라 (화면 좌표 변환용)

    [Header("지금 현재 스킬or궁극기를 사용 중인가?")]
    public bool isUsingSkillorUltimate;

	// 애니메이터 오버라이드
	public AnimatorOverrideController overrideController;
	public AnimatorOverrideController overrideController2;
	public bool isRoundUP;

	public int originalLayerID = 6; // 기본 레이어 ID (Default는 0)
    public int shiftedLayerID = 10; // Shift 키를 눌렀을 때 변경할 레이어 ID

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

    // [NR] 콤보 (근접)
    public const float ComboWindow = 0.75f;
    public int ComboStep { get; private set; }
    public float ComboDamageMultiplier { get; private set; } = 1f;
    int rangedShotCount;

    // [NR] 구르기
    Vector2 slideDir = Vector2.right;
    float dashIFrameUntil;
    public bool DashInvulnerable => IsSliding || Time.time < dashIFrameUntil;

    // [NR] HUD 표시용 재사용 대기
    float lastSkillDuration = 1f, lastUltDuration = 1f, lastSlideCooldown = 1f;
    public float SkillCooldownRatio => skillAttackTime > 0 ? Mathf.Clamp01(skillAttackTime / Mathf.Max(0.01f, lastSkillDuration)) : 0f;
    public float UltCooldownRatio => skillAttack2Time > 0 ? Mathf.Clamp01(skillAttack2Time / Mathf.Max(0.01f, lastUltDuration)) : 0f;
    public float SlideCooldownRatio => IsSliding ? 1f : isCooldown ? Mathf.Clamp01(cooldownTime / Mathf.Max(0.01f, lastSlideCooldown)) : 0f;
    public float SkillCooldownRemain => Mathf.Max(0f, skillAttackTime);
    public float UltCooldownRemain => Mathf.Max(0f, skillAttack2Time);
    public float SlideCooldownRemain => isCooldown ? Mathf.Max(0f, cooldownTime) : 0f;
    float AttackInterval => Mathf.Max(0.2f, jabCooldown) * NRStats.AttackIntervalMultiplier * (NRHero.IsRanged ? 1.15f : 1f);
    public float JabCooldownRatio
    {
        get
        {
            float remain = (lastAttackTime + AttackInterval) - Time.time;
            return remain > 0 ? Mathf.Clamp01(remain / AttackInterval) : 0f;
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

    bool Dialoging => talkManager != null && talkManager.isDialoging;

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

        // 콤보 시간이 지나면 초기화
        if (ComboStep > 0 && Time.time - lastAttackTime > ComboWindow + 0.1f) ComboStep = 0;
    }

    // [NR] 입력을 받아도 되는지 (메뉴/증강 선택/크레딧/UI 클릭 중에는 무시)
    bool InputBlocked(bool pointerAction)
    {
        if (NRUIState.IsGameplayBlocked) return true;
        if (pointerAction && NRInput.PointerOverInteractiveUI()) return true;
        return false;
    }

    Vector3 MouseWorld()
    {
        var cam = mainCamera != null ? mainCamera : Camera.main;
        if (cam == null) return transform.position + Vector3.right;
        Vector3 w = cam.ScreenToWorldPoint(NRInput.MousePosition);
        w.z = 0f;
        return w;
    }

    Vector2 AimDirection()
    {
        Vector2 d = (Vector2)(MouseWorld() - (transform.position + Vector3.up * 0.3f));
        if (d.sqrMagnitude < 0.0001f) d = sp.flipX ? Vector2.left : Vector2.right;
        return d.normalized;
    }

    // =====================================================================
    // Input System 콜백 (기본 조작 방식일 때만 사용. LoL식은 NRControls가 Try*를 직접 호출)
    // =====================================================================
    public bool RealStop = false; // 멈추는 거 Update 함수에 적용
	public void OnInteract(InputAction.CallbackContext context)// 대화창 E키
	{
        if (context.started && NRControls.IsDefault) TryInteract();
    }

    public void OnMove(InputAction.CallbackContext context)// 이동 입력 처리
	{
        if (!NRControls.IsDefault) return;
        SetMoveInput(context.ReadValue<Vector2>(), context.canceled);
    }

    public void OnSlide(InputAction.CallbackContext context) // 슬라이드 입력 처리
    {
        if (context.started && NRControls.IsDefault) TrySlide(false);
    }

	public void OnAttack(InputAction.CallbackContext context)// 일반 공격 (잽)
	{
        if (context.started && NRControls.IsDefault) TryAttack();
    }

    public void OnSkillAttack(InputAction.CallbackContext context)// 스킬
	{
        if (context.started && NRControls.IsDefault) TrySkill();
    }

    public void OnSkillAttack2(InputAction.CallbackContext context)// 궁극기
	{
        if (context.started && NRControls.IsDefault) TryUlt();
    }

    // =====================================================================
    // 행동
    // =====================================================================
    public void SetMoveInput(Vector2 value, bool released = false)
    {
        if (canMove)
        {
            moveInput = value;
            IsMoving = moveInput != Vector2.zero;  // 움직임 상태 확인
            SetFacingDirection(moveInput);  // 방향 설정
        }
        else if (released || value == Vector2.zero)
        {
            moveInput = Vector2.zero; // 경직 중 키를 뗐을 때 입력이 남아 미끄러지던 문제 방지
        }
    }

    /// <summary>E / F / 클릭: 대화 시작·다음 대사</summary>
    public bool TryInteract()
    {
        if (InputBlocked(false)) return false;

        // 대사가 한 글자씩 나오는 중이면 먼저 전체 문장을 보여줌
        if (NRDialogueSkin.TryCompleteTyping()) return true;

        if (Dialoging) return TalkNext();

        GameObject target = scanObject;
        if (target == null) target = FindNearbyTalkable(1.6f);
        if (target == null) return false;
        StartTalkWith(target);
        return true;
    }

    /// <summary>대화 중일 때 다음 대사 (E / 좌클릭 / Space)</summary>
    public bool TalkNext()
    {
        if (!Dialoging || talkManager == null) return false;
        if (NRDialogueSkin.TryCompleteTyping()) return true;
        if (talkManager.ScanObject == null) return false;
        talkManager.DialogAction(talkManager.ScanObject);
        return true;
    }

    /// <summary>NPC를 클릭하거나 E로 말을 걸었을 때</summary>
    public void StartTalkWith(GameObject npc)
    {
        if (npc == null || talkManager == null || InputBlocked(false)) return;
        if (Dialoging) { TalkNext(); return; }
        TicTocDealyTime = 0.2f; // [NR] 5초 → 0.2초 (대화 중 이동은 isDialoging이 이미 막음)
        PleaseStopPlayer();
        float dx = npc.transform.position.x - transform.position.x;
        if (Mathf.Abs(dx) > 0.05f) { sp.flipX = dx < 0; dirVec = dx < 0 ? Vector3.left : Vector3.right; }
        talkManager.DialogAction(npc);
    }

    GameObject FindNearbyTalkable(float radius)
    {
        _Object best = null;
        float bestDist = radius;
        foreach (var o in FindObjectsOfType<_Object>())
        {
            float d = Vector2.Distance(o.transform.position, transform.position);
            if (d < bestDist) { bestDist = d; best = o; }
        }
        return best != null ? best.gameObject : null;
    }

    public bool TryAttack()
    {
		if (jabCooldown <= 0.2f) jabCooldown = 0.2f;
        if (Dialoging) return false;
        if (isUsingSkillorUltimate || isAtking || Time.time < lastAttackTime + AttackInterval) return false;
        if (InputBlocked(true)) return false;

        Vector3 worldPosition = MouseWorld();
        bool leftSide = worldPosition.x < transform.position.x;

        // 문/침대 클릭 (기존 기능)
        ClickItems(worldPosition);

        if (isGeoRiPlayer)
        {
            lastAttackTime = Time.time;
            return false;
        }

        sp.flipX = leftSide;
        TicTocDealyTime = Jab;

        if (NRHero.IsRanged)
        {
            lastAttackTime = Time.time;
            rangedShotCount++;
            bool forceCrit = rangedShotCount % 4 == 0; // 원거리 패시브
            StartCoroutine(RangedRoutine(0, forceCrit));
            return true;
        }

        // 근접 3타 콤보
        if (Time.time - lastAttackTime <= ComboWindow && ComboStep < 2) ComboStep++;
        else ComboStep = 0;
        ComboDamageMultiplier = ComboStep == 2 ? 1.8f : ComboStep == 1 ? 1.15f : 1f;
        lastAttackTime = Time.time;

        BoxCollider2D col = ComboStep == 2 ? (leftSide ? LargeLeft : LargeRight) : (leftSide ? left : right);
        StartCoroutine(DisableCollider(col, 0));
        NRCombatFX.Slash(transform.position + Vector3.up * 0.35f, leftSide, ComboStep);
        if (ComboStep == 2)
        {
            lastAttackTime += 0.2f; // 마무리 후 짧은 후딜
            NRAudio.PlaySfx("combo_finisher", 0.8f);
            NRCombatFX.Shake(0.1f, 0.07f);
        }
        return true;
    }

    void ClickItems(Vector3 worldPosition)
    {
        if (myItemColliders == null) return;
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

    public bool TrySkill()
    {
        if (Dialoging || InputBlocked(true)) return false;
        if (skillAttackTime > 0 || isUsingSkillorUltimate) return false;
        if (isGeoRiPlayer) return false;

        bool leftSide = MouseWorld().x < transform.position.x;
        lastSkillDuration = skillAttackCooldown * NRStats.SkillCooldownMultiplier;
        skillAttackTime = lastSkillDuration;  // 쿨타임 시작
        TicTocDealyTime = Skill;
        sp.flipX = leftSide;

        if (NRHero.IsRanged)
        {
            StartCoroutine(RangedRoutine(1, false));
            return true;
        }

        animator.SetTrigger(AnimationStrings.skillAttackTrigger);  // 스킬 애니메이션 실행
        StartCoroutine(DisableCollider(leftSide ? LargeLeft : LargeRight, 1));
        return true;
    }

    public bool TryUlt()
    {
        if (Dialoging || InputBlocked(false)) return false;
        if (skillAttack2Time > 0 || isUsingSkillorUltimate) return false;
        if (isGeoRiPlayer) return false;

        lastUltDuration = skillAttack2Cooldown * NRStats.UltCooldownMultiplier;
        skillAttack2Time = lastUltDuration;  // 쿨타임 시작
        TicTocDealyTime = Ultimite;

        if (NRHero.IsRanged)
        {
            sp.flipX = MouseWorld().x < transform.position.x;
            StartCoroutine(RangedRoutine(2, false));
            return true;
        }

        animator.SetTrigger(AnimationStrings.skillAttackTrigger2);  // 스킬 애니메이션 실행
        StartCoroutine(PlaySoundWithDelay());
        StartCoroutine(DisableCollider(sp.flipX ? LargeLeft : LargeRight, 2));
        return true;
    }

    /// <param name="towardMouse">LoL식: 항상 마우스 방향</param>
    public bool TrySlide(bool towardMouse)
    {
        if (isCooldown || IsSliding || Dialoging) return false;
        if (InputBlocked(false)) return false;

        Vector2 dir = (!towardMouse && moveInput.sqrMagnitude > 0.01f) ? moveInput.normalized : AimDirection();
        if (dir.sqrMagnitude < 0.01f) dir = sp.flipX ? Vector2.left : Vector2.right;
        slideDir = dir;
        if (Mathf.Abs(dir.x) > 0.05f) sp.flipX = dir.x < 0;
        StartSliding(); // 슬라이드 시작
        return true;
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

    // [NR] 원거리 캐릭터 공격
    IEnumerator RangedRoutine(int style, bool forceCrit)
    {
        CurrentAttackStyle = style;
        Vector2 origin = (Vector2)transform.position + Vector2.up * 0.35f;
        var p = playerScript;
        if (style == 0)
        {
            PleaseStopPlayer();
            Vector2 dir = AimDirection();
            NRRangedAvatar.NotifyShoot(p);
            NRPlayerProjectile.Fire(origin + dir * 0.35f, dir, 17f, NRHero.RangedRange, 0, NRHero.RangedDamageMul, 0, NRPalette.Cyan, 1f, forceCrit);
            PlaySfx(1);
            yield break;
        }

        isUsingSkillorUltimate = true;
        canMove = false;
        PleaseStopPlayer();
        if (style == 1)
        {
            Vector2 dir = AimDirection();
            var tg = NRTelegraph.Line(origin, dir, 10f, 0.12f, 0.25f, NRPalette.Rage);
            yield return new WaitForSeconds(0.25f);
            dir = AimDirection();
            NRRangedAvatar.NotifyShoot(p);
            NRPlayerProjectile.Fire(origin + dir * 0.35f, dir, 24f, 10f, 1, 2f, 6, NRPalette.Rage, 1.8f);
            NRCombatFX.Shake(0.1f, 0.06f);
            PlaySfx(3);
        }
        else
        {
            yield return new WaitForSeconds(0.25f);
            itemManager.HammerBuff();
            for (int volley = 0; volley < 3; volley++)
            {
                Vector2 dir = AimDirection();
                NRRangedAvatar.NotifyShoot(p);
                for (int i = -2; i <= 2; i++)
                {
                    Vector2 d = Quaternion.Euler(0, 0, i * 12f + (volley % 2 == 0 ? 0 : 6f)) * dir;
                    NRPlayerProjectile.Fire(origin + d * 0.35f, d, 19f, NRHero.RangedRange + 1f, 2, 0.9f, 1, NRPalette.Gold, 1.2f);
                }
                NRCombatFX.Shake(0.08f, 0.05f);
                PlaySfx(1);
                yield return new WaitForSeconds(0.14f);
            }
            itemManager.HammerDeBuff();
        }
        isUsingSkillorUltimate = false;
        canMove = true;
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

        if (IsSliding)
        {
            // 구르기는 공격 경직보다 우선 (공격 캔슬)
            rigid.velocity = slideDir * slideSpeed * NRStats.MoveSpeedMultiplier;
        }
        else if (canMove == true && Dialoging == false && timeStopu == false)
        {
			rigid.velocity = new Vector2(moveInput.x, moveInput.y) * walkSpeed * NRStats.MoveSpeedMultiplier;  // 이동 처리
        }
        else
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
        RaycastHit2D rayHit = Physics2D.Raycast(rigid.position, dirVec, Length, LayerMask.GetMask("Object"));

        // 레이 맞으면!
        if (rayHit.collider != null) scanObject = rayHit.collider.gameObject;
        else scanObject = null;

        if (speed_ui != null) speed_ui.text = (walkSpeed * NRStats.MoveSpeedMultiplier).ToString("0.#"); // 이동속도
        if (As_ui != null) As_ui.text = (1f / AttackInterval).ToString("F2");           // 공격속도
    }

    /// <summary>[NR] 대화 대상이 앞에 있는지 (E 안내 표시용)</summary>
    public GameObject ScannedObject => scanObject;

    void SetFacingDirection(Vector2 moveInput)// 이동 방향 설정
	{
        //x값이 0보다 큼 && 오른쪽 안바라봄 && 대화중 아님
        if (moveInput.x > 0 && Dialoging == false)
        {
            dirVec = Vector3.right;  // 오른쪽 방향 설정
            sp.flipX = false; // 캐릭터를 오른쪽으로 바라보게 설정
        }
		//x값이 0보다 큼 && 오른쪽 바라봄 && 대화중 아님
		else if (moveInput.x < 0 && Dialoging == false)
        {
            dirVec = Vector3.left;  // 왼쪽 방향 설정
            sp.flipX = true; // 캐릭터를 왼쪽으로 바라보게 설정
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
        if (revertLayerCoroutine != null) StopCoroutine(revertLayerCoroutine);
        revertLayerCoroutine = StartCoroutine(RevertLayerAfterDelay(0.5f));

        // [NR] 구르기 연출 + 증강
        NRCombatFX.Afterimage(NRRangedAvatar.VisualOf(playerScript), NRStats.DashInvuln ? NRPalette.Anxiety.WithAlpha(0.55f) : NRPalette.Cyan.WithAlpha(0.45f), 4, 0.06f);
        if (NRStats.DashShield > 0f) NRStats.AddShield(NRStats.DashShield);
        if (!NRSave.Data.seenDashTip && !isGeoRiPlayer)
        {
            NRSave.Data.seenDashTip = true;
            NRSave.MarkDirty();
            NRUIRoot.ToastMsg("구르는 동안에는 적의 투사체와 공격을 피할 수 있습니다", NRPalette.Anxiety, 3f);
        }
    }

    private void StopSliding() // 슬라이드 종료
    {
        if (!IsSliding) return; // [NR] Invoke와 Update에서 두 번 호출되던 것 방지
        IsSliding = false;
        walkSpeed = defaultSpeed; // 기본 속도로 복원
        isCooldown = true;
        lastSlideCooldown = slideCooldown * NRStats.DashCooldownMultiplier;
        cooldownTime = lastSlideCooldown; // 슬라이드 쿨타임 설정
        dashIFrameUntil = Time.time + (NRStats.DashInvuln ? 0.3f : 0.08f);

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
	private float lastAttackTime = -10f;  // 마지막 공격 시간을 기록할 변수

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
