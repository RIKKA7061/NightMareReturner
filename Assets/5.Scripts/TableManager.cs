using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TableManager : MonoBehaviour
{
	// 싱글톤 인스턴스
	public static TableManager Instance;

	[Header("일반 몹")]
	[SerializeField] private int enemy1HP; // Inspector에서 조정 가능
	[SerializeField] private int enemy1Atk;

	[Header("보스 몹")]
	[SerializeField] private int bossHP;
	[SerializeField] private int bossATK;

	// Static 변수
	public static int Enemy1HP { get; private set; }
	public static int Enemy1Atk { get; private set; }
	public static int BossHP { get; private set; }
	public static int BossATK { get; private set; }

	private void Awake()
	{
		// 싱글톤 패턴 구현
		if (Instance == null)
		{
			Instance = this;
			//DontDestroyOnLoad(gameObject); // 씬 전환 시에도 유지
		}
		else
		{
			Destroy(gameObject);
		}

		// 초기화 (게임 실행 시 static 변수 업데이트)
		UpdateStaticValues();
	}

	private void UpdateStaticValues()
	{
		// Static 변수에 Inspector 값 할당
		Enemy1HP = enemy1HP;
		Enemy1Atk = enemy1Atk;
		BossHP = bossHP;
		BossATK = bossATK;
	}

	// Inspector에서 값이 변경될 때마다 static 변수 갱신
	private void OnValidate()
	{
		if (Instance == this)
		{
			UpdateStaticValues();
		}
	}
}
