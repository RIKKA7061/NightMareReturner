using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class TouchItems : MonoBehaviour
{
	[Header("아이템과 닿을시")]

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

	// 플레이어 스크립트
	private void Awake()
	{
		player = FindObjectOfType<Player>(); // 무조건 해줘야됨 (초기화)
	}

	private void OnTriggerEnter2D(Collider2D other)// 플레이어 충돌시
	{
		if (other.CompareTag("Player"))
		{
			player.Atk += AddAtk;		// 공격력 증가
			player.nowHP += Heal;		// 체력 회복
			player.maxHP += AddHp;		// 최대체력 증가
			Player.Money += AddMoney;	// 재화 획득
			Player.round += Round;		// 구슬 획득

			if (player.nowHP > player.maxHP) player.nowHP = player.maxHP;

			Destroy(gameObject);		// 나 자신을 아이템 삭제
		}
	}


}
