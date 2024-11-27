using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    void Update()
    {
        // 보스 처치시
        // 1. 대상 객체가 null(파괴되거나 씬에서 찾을 수 없음) 상태인지 확인
        if (targetObject == null)
        {
            isPlayerMovable = false;
            // 2. 플레이어를 특정 좌표(homePosition)로 이동
            MovePlayerToHome();
            SpawnNewBoss();
        }
    }
    public bool isRoundStart = false;

	void MovePlayerToHome()
    {
        if (player != null && homePosition != null && isPlayerMovable == false && isRoundStart == true )
        {

            // 부활
            StartCoroutine(RespawnPlayerStart());
        }
        else
        {
            //Debug.LogWarning("플레이어나 HomePosition이 설정되지 않았습니다.");
        }
    }

    IEnumerator RespawnPlayerStart()
    {
		//GameOverImg.SetActive(true);
		yield return new WaitForSeconds(0.1f);
		// 2-1. 플레이어를 바로 좌표로 이동
		//player.transform.position = homePosition.position;

		// (선택적으로) 회전도 이동할 위치의 회전에 맞춤
		//player.transform.rotation = homePosition.rotation;

		//Debug.Log("플레이어가 지정된 좌표로 이동했습니다.");

		// 플레이어 스탯 초기화 
		//playerScript.StatDefaultPlayer();

		isPlayerMovable = true;

		Debug.Log("게임이 끝났습니다.");
		playerScript.RespawnPlayer();
	}

    void SpawnNewBoss()
    {
        if (bossPrefab != null && bossSpawnPoint != null)
        {
			// 보스 생성
			GameObject newBoss = Instantiate(bossPrefab, bossSpawnPoint.position, bossSpawnPoint.rotation);
            MonsterHP bossHP = newBoss.GetComponent<MonsterHP>();

            if (bossHP != null)
            {
                // MonsterHP 컴포넌트의 특정 함수 호출
                bossHP.SetDefault(); // 특정함수()는 MonsterHP 스크립트의 함수 이름으로 교체
            }
            else
            {
                Debug.LogError("MonsterHP 컴포넌트를 찾을 수 없습니다.");
            }


            targetObject = newBoss; // 새로운 보스를 targetObject로 설정
		}
        else
        {
            Debug.LogWarning("BossPrefab이나 BossSpawnPoint가 설정되지 않았습니다.");
        }
    }
}
