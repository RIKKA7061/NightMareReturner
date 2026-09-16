using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ============================================================================
// 조작 방식
//  기본 : WASD 이동 · 좌클릭 공격 · 우클릭 특수 · Space 구르기 · R 궁극기 · E 대화
//  LoL식: 우클릭 이동 · Q 공격 · W 특수 · E 구르기(마우스 방향) · R 궁극기 · F/좌클릭 대화
//  공통 : 대화 중 좌클릭 / Space 로 다음 대사
// ============================================================================
public class NRControls : MonoBehaviour
{
	public static bool IsDefault => NRSettings.ControlScheme == 0;

	PlayerAction action;
	Player player;
	Vector2 moveTarget;
	bool moving;
	float stuckTimer;
	Vector2 lastPos;
	GameObject marker;

	public struct KeyNames { public string attack, skill, dash, ult, talk, move; }

	public static KeyNames Keys => IsDefault
		? new KeyNames { attack = "좌클릭", skill = "우클릭", dash = "Space", ult = "R", talk = "E", move = "WASD" }
		: new KeyNames { attack = "Q", skill = "W", dash = "E", ult = "R", talk = "F", move = "우클릭" };

	public static void Attach(Player p)
	{
		if (p == null || p.GetComponent<NRControls>() != null) return;
		p.gameObject.AddComponent<NRControls>();
	}

	void Awake()
	{
		action = GetComponent<PlayerAction>();
		player = GetComponent<Player>();
	}

	static UnityEngine.InputSystem.Keyboard KB => UnityEngine.InputSystem.Keyboard.current;
	static UnityEngine.InputSystem.Mouse MS => UnityEngine.InputSystem.Mouse.current;

	void Update()
	{
		if (action == null || player == null || player.isDead) return;

		bool dialoging = action.talkManager != null && action.talkManager.isDialoging;

		// 대화 넘기기 (두 방식 공통): 좌클릭 / Space
		if (dialoging)
		{
			if (!NRUIState.IsGameplayBlocked && ((MS != null && MS.leftButton.wasPressedThisFrame && !NRInput.PointerOverInteractiveUI()) || (KB != null && KB.spaceKey.wasPressedThisFrame)))
				action.TalkNext();
			if (!IsDefault) { StopMoving(); action.SetMoveInput(Vector2.zero, true); }
			return;
		}

		if (IsDefault)
		{
			StopMoving();
			return;
		}

		if (NRUIState.IsGameplayBlocked)
		{
			StopMoving();
			action.SetMoveInput(Vector2.zero, true);
			return;
		}

		// ---- LoL식 ----
		if (MS != null && MS.rightButton.isPressed && !NRInput.PointerOverInteractiveUI())
		{
			var cam = Camera.main;
			if (cam != null)
			{
				Vector3 w = cam.ScreenToWorldPoint(MS.position.ReadValue());
				moveTarget = w;
				if (MS.rightButton.wasPressedThisFrame) ShowMarker(w);
				if (!moving) { lastPos = transform.position; stuckTimer = 0f; }
				moving = true;
			}
		}

		if (KB != null)
		{
			if (KB.qKey.wasPressedThisFrame) { if (action.TryAttack()) StopMoving(); }
			if (KB.wKey.wasPressedThisFrame) { if (action.TrySkill()) StopMoving(); }
			if (KB.eKey.wasPressedThisFrame) action.TrySlide(true);
			if (KB.rKey.wasPressedThisFrame) { if (action.TryUlt()) StopMoving(); }
			if (KB.fKey.wasPressedThisFrame) action.TryInteract();
			if (KB.sKey.wasPressedThisFrame) StopMoving(); // LoL의 S(정지)
		}
		// 좌클릭: 문/침대 클릭 (공격은 하지 않음)
		if (MS != null && MS.leftButton.wasPressedThisFrame && !NRInput.PointerOverInteractiveUI() && action.isGeoRiPlayer)
			action.TryAttack();

		Vector2 input = Vector2.zero;
		if (moving)
		{
			Vector2 to = moveTarget - (Vector2)transform.position;
			if (to.magnitude < 0.15f) StopMoving();
			else
			{
				input = to.normalized;
				// 벽에 막혀 제자리면 멈춤
				if (Vector2.Distance(lastPos, transform.position) < 0.01f) stuckTimer += Time.deltaTime;
				else stuckTimer = 0f;
				lastPos = transform.position;
				if (stuckTimer > 0.35f) { StopMoving(); input = Vector2.zero; }
			}
		}
		action.SetMoveInput(input, input == Vector2.zero);
	}

	void StopMoving()
	{
		moving = false;
	}

	void ShowMarker(Vector3 pos)
	{
		if (marker != null) Destroy(marker);
		marker = new GameObject("NR Move Marker");
		marker.transform.position = new Vector3(pos.x, pos.y, 0);
		var sr = marker.AddComponent<SpriteRenderer>();
		sr.sprite = NRSprites.Disc;
		sr.color = NRPalette.Green.WithAlpha(0.8f);
		NRSort.Set(sr, NRSort.Floor, 60);
		StartCoroutine(MarkerRoutine(marker, sr));
	}

	IEnumerator MarkerRoutine(GameObject go, SpriteRenderer sr)
	{
		float t = 0f;
		while (t < 0.35f && go != null)
		{
			t += Time.deltaTime;
			float k = t / 0.35f;
			go.transform.localScale = new Vector3(0.6f, 0.3f, 1f) * (1f - k * 0.6f);
			sr.color = NRPalette.Green.WithAlpha(0.8f * (1f - k));
			yield return null;
		}
		if (go != null) Destroy(go);
	}
}

// ============================================================================
// 첫 실행 설정: 조작 방식 + 캐릭터 선택 (메인 메뉴 / 집의 책상에서 다시 열 수 있음)
// ============================================================================
public class NRSetupWizard : NRModal
{
	public override bool CloseOnEsc => allowClose;
	bool allowClose;
	int step;
	RectTransform body;
	System.Action onDone;
	TextMeshProUGUI title, subtitle;
	bool heroOnly;

	public static void Show(bool firstRun, System.Action done, bool heroOnly = false)
	{
		var root = NRModalHost.CreateModalRoot("NR Setup", 0.88f);
		var w = root.gameObject.AddComponent<NRSetupWizard>();
		w.allowClose = !firstRun;
		w.onDone = done;
		w.heroOnly = heroOnly;
		w.Build(root);
		w.Open();
		w.ShowStep(heroOnly ? 1 : 0);
	}

	void Build(RectTransform root)
	{
		title = NRUI.Label(root, "", 60, NRPalette.Text, TextAlignmentOptions.Center, NRTextFx.OutlineShadow);
		NRUI.Place(title.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -70), new Vector2(1600, 80), new Vector2(0.5f, 1));
		subtitle = NRUI.Label(root, "", 28, NRPalette.TextDim, TextAlignmentOptions.Center);
		NRUI.Place(subtitle.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -160), new Vector2(1600, 44), new Vector2(0.5f, 1));
		body = NRUI.Rect(root, "Body");
		NRUI.Place(body, new Vector2(0.5f, 0.5f), new Vector2(0, -30), new Vector2(1500, 680));
	}

	void ShowStep(int s)
	{
		step = s;
		foreach (Transform c in body) Destroy(c.gameObject);
		if (s == 0)
		{
			title.text = "조작 방식 선택";
			subtitle.text = "나중에 ESC 메뉴 → 설정에서 언제든 바꿀 수 있습니다";
			Card(new Vector2(-370, 0), "기본", "키보드 이동 + 마우스 전투",
				new[] { ("WASD", "이동"), ("좌클릭", "기본 공격"), ("우클릭", "특수 공격"), ("Space", "구르기"), ("R", "궁극기"), ("E", "대화 / 다음 대사") },
				NRPalette.Cyan, NRSettings.ControlScheme == 0, () => { NRSettings.ControlScheme = 0; ShowStep(1); });
			Card(new Vector2(370, 0), "LoL식", "마우스 우클릭 이동 + QWER 스킬",
				new[] { ("우클릭", "이동 (누르고 있으면 계속)"), ("Q", "기본 공격 (마우스 방향)"), ("W", "특수 공격"), ("E", "구르기 (마우스 방향)"), ("R", "궁극기"), ("F / 좌클릭", "대화 / 다음 대사") },
				NRPalette.Gold, NRSettings.ControlScheme == 1, () => { NRSettings.ControlScheme = 1; ShowStep(1); });
		}
		else
		{
			title.text = "캐릭터 선택";
			subtitle.text = "집의 책상에서 다시 바꿀 수 있습니다 (회차 시작 전)";
			var k = NRControls.Keys;
			Card(new Vector2(-370, 0), NRHero.Name(NRHeroType.Melee), NRHero.Summary(NRHeroType.Melee),
				new[] { ("체력", "500 (높음)"), ("사거리", "근접"), (k.attack, "3타 콤보 (마무리 ×1.8)"), (k.skill, "강타 ×2"), (k.ult, "해머 ×4"), ("패시브", "처치 시 체력 5 회복, 마무리 적중 시 보호막") },
				NRPalette.Rage, NRSave.Data.hero == 0, () => Finish(NRHeroType.Melee));
			Card(new Vector2(370, 0), NRHero.Name(NRHeroType.Ranged), NRHero.Summary(NRHeroType.Ranged),
				new[] { ("체력", "400 · 방어력 낮음"), ("사거리", NRHero.RangedRange + " (원거리)"), (k.attack, "사격 ×0.75"), (k.skill, "관통탄 ×2"), (k.ult, "난사 15발"), ("패시브", "처치 시 체력 3 회복, 4번째 사격 치명타") },
				NRPalette.Cyan, NRSave.Data.hero == 1, () => Finish(NRHeroType.Ranged));
		}
	}

	void Card(Vector2 pos, string name, string desc, (string key, string text)[] rows, Color color, bool current, System.Action pick)
	{
		var frame = NRUI.Image(body, "Card " + name, NRSprites.ColoredFrame(current ? NRPalette.Pink : color), Color.white, true);
		NRUI.Place(frame.rectTransform, new Vector2(0.5f, 0.5f), pos, new Vector2(680, 640));
		var btn = frame.gameObject.AddComponent<Button>();
		btn.transition = Selectable.Transition.None;
		btn.onClick.AddListener(() => { NRAudio.PlayUI(); pick(); });
		var glow = NRUI.Image(frame.transform, "Glow", NRSprites.Glow, color.WithAlpha(0.2f));
		NRUI.Place(glow.rectTransform, new Vector2(0.5f, 1), new Vector2(0, 40), new Vector2(700, 400), new Vector2(0.5f, 1));
		frame.gameObject.AddComponent<NRCardHover>().Init(frame.rectTransform, color, glow);

		var n = NRUI.Label(frame.transform, name + (current ? "  <size=26><color=#" + NRPalette.ToHex(NRPalette.Pink) + ">(현재)</color></size>" : ""), 50, color, TextAlignmentOptions.Center, NRTextFx.OutlineShadow);
		NRUI.Place(n.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -34), new Vector2(620, 66), new Vector2(0.5f, 1));
		var d = NRUI.Label(frame.transform, desc, 26, NRPalette.TextDim, TextAlignmentOptions.Center);
		NRUI.Place(d.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -104), new Vector2(620, 70), new Vector2(0.5f, 1));

		float y = -190;
		foreach (var r in rows)
		{
			var keyBg = NRUI.Image(frame.transform, "Key", NRSprites.ButtonFrame, Color.white);
			NRUI.Place(keyBg.rectTransform, new Vector2(0, 1), new Vector2(40, y), new Vector2(190, 56), new Vector2(0, 1));
			var kt = NRUI.Label(keyBg.transform, r.key, 26, color, TextAlignmentOptions.Center);
			NRUI.Stretch(kt.rectTransform);
			var tt = NRUI.Label(frame.transform, r.text, 28, NRPalette.Text, TextAlignmentOptions.MidlineLeft);
			NRUI.Place(tt.rectTransform, new Vector2(0, 1), new Vector2(250, y), new Vector2(400, 56), new Vector2(0, 1));
			y -= 68;
		}
		var sel = NRUI.Label(frame.transform, "클릭하여 선택", 26, NRPalette.Pink, TextAlignmentOptions.Center);
		NRUI.Place(sel.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 22), new Vector2(600, 40), new Vector2(0.5f, 0));
	}

	void Finish(NRHeroType hero)
	{
		NRSave.Data.setupDone = true;
		NRHero.Set(hero);
		Close();
	}

	protected override void OnClosed()
	{
		Destroy(gameObject, 0.3f);
		var cb = onDone;
		onDone = null;
		cb?.Invoke();
	}
}

// ============================================================================
// 줍는 아이템 (코인)
// ============================================================================
public class NRPickup : MonoBehaviour
{
	int value;
	Vector2 velocity;
	float born;
	Transform target;
	SpriteRenderer sr;

	public static void DropCoins(Vector3 pos, int amount)
	{
		if (amount <= 0) return;
		int pieces = Mathf.Clamp(amount, 1, 6);
		int per = Mathf.Max(1, amount / pieces);
		int rest = amount - per * pieces;
		for (int i = 0; i < pieces; i++)
		{
			var go = new GameObject("NR Coin");
			go.transform.position = pos;
			var p = go.AddComponent<NRPickup>();
			p.value = per + (i == 0 ? rest : 0);
			p.velocity = Random.insideUnitCircle.normalized * Random.Range(1.5f, 3f);
			p.sr = go.AddComponent<SpriteRenderer>();
			p.sr.sprite = NRSprites.Coin;
			p.sr.color = NRPalette.Gold;
			NRSort.Set(p.sr, NRSort.Enemy, 80);
			go.transform.localScale = Vector3.one * 3.2f;
		}
	}

	void Start()
	{
		born = Time.time;
		var pl = FindObjectOfType<Player>();
		target = pl != null ? pl.transform : null;
	}

	void Update()
	{
		float age = Time.time - born;
		if (target == null) { if (age > 20f) Destroy(gameObject); return; }
		Vector2 to = (Vector2)target.position + Vector2.up * 0.3f - (Vector2)transform.position;
		if (age > 0.45f && to.magnitude < 3.5f)
		{
			velocity = Vector2.Lerp(velocity, to.normalized * 9f, Time.deltaTime * 8f);
		}
		else velocity *= 1f - Time.deltaTime * 4f;
		transform.position += (Vector3)(velocity * Time.deltaTime);
		sr.transform.localScale = new Vector3(3.2f * Mathf.Abs(Mathf.Cos(age * 6f)) + 0.4f, 3.2f, 1f);
		if (age > 0.45f && to.magnitude < 0.45f)
		{
			Player.Money += value;
			NRAudio.PlaySfx("coin", 0.35f);
			NRCombatFX.Number(transform.position, "+" + value, NRPalette.Gold, 0.7f);
			Destroy(gameObject);
		}
		if (age > 25f) Destroy(gameObject);
	}
}
