using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// [NR] 보스 처치 후 즉시 집으로 리셋하던 흐름 → NRRun(계층 진행 → 3계층 후 엔딩 크레딧)
public class GoHomeManager : MonoBehaviour
{
    // 특정 객체를 감지하기 위한 변수
    [Header("보스")]
    public GameObject targetObject; // 감지할 대상 객체

    [Header("보스 프리펩")]
    public GameObject bossPrefab;   // 새로 생성할 보스 프리팹
    public Transform bossSpawnPoint; // 보스를 생성할 위치


    public GameObject GameOverImg;  // 게임 오버 연출

    [Header("집 위치")]
    public Transform homePosition;  // 이동할 좌표

    // 플레이어를 참조
    [Header("플레이어")]
    public GameObject player;
    public Player playerScript;

    public bool isPlayerMovable = false;
    public bool isBossRoundIn = false;

    public bool isRoundStart = false;

	public void MovePlayerToHome()
    {
        // [NR] 기존 호출 경로 유지용. 실제 흐름은 NRRun.OnBossDefeated에서 처리
        if (targetObject != null)
            NRRun.OnBossDefeated(targetObject.transform.position);
    }

    public void SpawnNewBoss()
    {
        if (targetObject != null) return; // [NR] 이미 보스가 있으면 중복 소환 방지

        if (bossPrefab != null && bossSpawnPoint != null)
        {
			// 보스 생성
			GameObject newBoss = Instantiate(bossPrefab, bossSpawnPoint.position, bossSpawnPoint.rotation);
            newBoss.SetActive(true);
            MonsterHP bossHP = newBoss.GetComponent<MonsterHP>();

            if (bossHP != null)
            {
                bossHP.SetDefault();
            }
            else
            {
                Debug.LogError("MonsterHP 컴포넌트를 찾을 수 없습니다.");
            }

            NRRun.ConfigureBoss(newBoss); // [NR] 계층별 체력/패턴/이름

            targetObject = newBoss; // 새로운 보스를 targetObject로 설정
		}
        else
        {
            Debug.LogWarning("BossPrefab이나 BossSpawnPoint가 설정되지 않았습니다.");
        }
    }
}
