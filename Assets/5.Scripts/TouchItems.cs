using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// [NR] 변경 요약
//  - 클래스 구슬: 각성 연출 UI → 첫 번째 증강 선택
//  - 감정 구슬(구슬 +1 보상): 증강 선택 (하데스 은총처럼 계속 중첩)
//  - 획득 효과/숫자 표시, 중복 획득 방지
public class TouchItems : MonoBehaviour
{
	[Header("아이템과 닿을시")]
	public GameObject portal;

	[Header("이거 class야?")]
	public bool isClass;

	[Header("공격력 증가")]
	public int AddAtk = 0;  // 공격력 증가

	[Header("체력 회복")]
	public int Heal = 0;    // 체력 회복

	[Header("최대 체력 증가")]
	public int AddHp = 0;   // 최대 체력 증가

	[Header("재화 증가")]
	public int AddMoney = 0;// 재화 증가

	[Header("구슬 획득")]
	public int Round = 0;   // 구슬 획득
	private Player player;
	bool consumed = false;

	// 플레이어 스크립트
	private void Awake()
	{
		player = FindObjectOfType<Player>(); // 무조건 해줘야됨 (초기화)
	}

	void OnEnable()
	{
		consumed = false;
	}

	private void OnTriggerEnter2D(Collider2D other)// 플레이어 충돌시
	{
		if (consumed) return;
		if (!other.CompareTag("Player") || other.isTrigger) return;
		if (player == null) player = FindObjectOfType<Player>();
		if (player == null || player.isDead) return;
		consumed = true;

		player.Atk += AddAtk;       // 공격력 증가
		player.Atk2 = player.Atk - 25;
		player.nowHP += Heal;		// 체력 회복
		player.maxHP += AddHp;      // 최대체력 증가
		player.maxHP2 = player.maxHP - 500;
		Player.Money += AddMoney;	// 재화 획득
		Player.round += Round;		// 구슬 획득

		if (player.nowHP > player.maxHP) player.nowHP = player.maxHP;

		// [NR] 획득 표시
		Vector3 pos = transform.position + Vector3.up * 0.6f;
		if (AddAtk != 0) NRCombatFX.Number(pos, "공격력 +" + AddAtk, NRPalette.Rage, 1f);
		if (AddHp != 0) NRCombatFX.Number(pos + Vector3.up * 0.3f, "최대 체력 +" + AddHp, NRPalette.Crimson, 1f);
		else if (Heal != 0) NRCombatFX.Number(pos, "체력 +" + Heal, NRPalette.Green, 1f);
		if (AddMoney != 0) NRCombatFX.Number(pos, "재화 +" + AddMoney, NRPalette.Gold, 1f);
		NRCombatFX.DeathBurst(transform.position, isClass || Round > 0 ? NRPalette.Anxiety : NRPalette.Cyan, 16);
		NRAudio.PlaySfx("crystal", 0.6f);

		// 클래스 아이템인 경우
		if(isClass)
		{
			// 활성화
			if (portal != null) portal.SetActive(true);

			// [NR] 각성 연출 → 첫 번째 증강
			NRClassAwakening.Show(() => NRAugmentSelect.Show(null, false, "첫 번째 감정", null));

			// 나 자신 비활성화
			gameObject.SetActive(false);
		}
		else
		{
			if (Round > 0) NRAugmentSelect.Show(null, false, "감정의 구슬", null); // [NR] 증강 선택
			Destroy(gameObject);        // 나 자신을 아이템 삭제
		}
	}


}
