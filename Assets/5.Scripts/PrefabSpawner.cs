using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
//using static UnityEditor.Progress;
//using UnityEditor;

public class PrefabSpawner : MonoBehaviour
{
    public GameObject prefab;               // 생성할 적 프리팹
    public Transform playerPos;             // 플레이어 위치

    public TalkManager talkManager;         // TalkManager 스크립트
    public RoomGenerator roomGenerator;     // RoomGenerator 스크립트

    [Header("몇 라운드 이상 상점?")]
    public int OverRoom;

    [Header("상점 텔레포트 프리펩")]
    public GameObject StorePos;

	private List<GameObject> spawnedStoreTP = new List<GameObject>();   // 생성된 상점 텔레포트 객체들을 담게될 리스트 선언

	[Header("보상 아이템 프리펩")]
    public GameObject[] reward_item_prf;
    private List<GameObject> spawnedItems = new List<GameObject>();     // 생성된 보상 아이템 객체들을 담게될 리스트 선언

    [Header("몇초 후 적을 소환할 것인가?")]
    public float sec = 3.0f;

    [Header("한 라운드당 적을 몇마리 소환할 것인가?")]
    public int spawnCount = 5;

    [Header("주인공을 소환할 위치")]
    public Transform[] Pos;

    [Header("적을 소환할 위치")]
    public Transform[] EnemySpawnPos;

    [Header("보상 아이템 소환할 위치")]
    public Transform[] RewardItem_Pos;

    [Header("소환할 위치 오브젝트들")]
    public GameObject[] PosObj;

	[Header("몇초 있다가 독백을 실행할래?")]
	public float docBack_DelayTime;

    [Header("적 소환시 적들 간의 간격")]
    public float spawnOffset = 1.0f;

    [Header("적 소환시 허용할 거리 오차")]
    public float checkDistance = 0.1f;  // 허용할 거리 오차

    [Header("이미 한번 스폰함?")]
    public bool isSpawnned = false;

    [Header("현재 처치한 적의 수를 나타냅니다.")]
    public int RoomEnemyCount = 0;

    //static public bool alreadySpawnedEnemies = false; // 몬스터 소환 조건

    int temp_i = 0;

    private void Update()
    {
		// 한 방에서 적 전부 처치시
		if (RoomEnemyCount == spawnCount)
		{
            // 현재 적 처치수 초기화
            RoomEnemyCount = 0;

            // 상점 라운드 수만큼 왔을 시
            if (Player.gameRound == OverRoom || Player.gameRound > OverRoom)
            {
                // 상점 가는 텔포 생성
                Vector3 leftPosition = Pos[temp_i].position + Vector3.left * 1.5f; // 좌표 기준 왼쪽으로 이동
                GameObject newItems = Instantiate(StorePos, leftPosition, Quaternion.identity); // 생성된 텔레포트를 변수에 저장
				spawnedStoreTP.Add(newItems);
			}
			// 일반 라운드 생성 할 때
			else
			{
                roomGenerator.RandomDoorGenerate(temp_i); // 랜덤 문 생성
            }

            Debug.Log(temp_i);
            GameObject newItem = Instantiate(reward_item_prf[temp_i], RewardItem_Pos[temp_i].position, Quaternion.identity); // 보상 아이템 생성(아이템 프리펩, 소환될 위치, rotation)
			spawnedItems.Add(newItem);                                                                            // 생성된 보상 아이템을 리스트에 추가
		}

		// 플레이어가 해당 방에 올시 && 라운드가 진행 중이 아니라 플레이어가 다음 라운드를 선택했을 때 && 이미 한번 스폰함?이 False일시
		for (int i = 0; i < Pos.Length; i++)
        {
            if (Vector3.Distance(playerPos.position, Pos[i].position) < checkDistance && !isSpawnned)
            {
                temp_i = i;         // 위치 저장용
                SpawnEnemies(i);    // 적 소환
            }
        }
    }

    public void SpawnEnemies(int room) // 적 소환
    {
        // 이미 한번 스폰함? -> False 일시
        if (isSpawnned == false)
        {
            roomGenerator.DestroyDoor();                // 생성되었던 문 파괴
            isSpawnned = true;                          // 이미 한번 스폰함? -> True로 설정
            StartCoroutine(DelayedSpawn(sec, room));    // 적 소환
            StartCoroutine(DocBack());                  // 주인공 독백 시작
        }
    }

    IEnumerator DocBack() // 기다렸다가 독백출력
    {
		yield return new WaitForSeconds(docBack_DelayTime);
		talkManager.SoloTalk(); // 던전 진입 상황
	}

    IEnumerator DelayedSpawn(float delay, int room) // 기다렸다가 적 소환해주는 시간차 함수
    {
        yield return new WaitForSeconds(delay);     // N초 시간 지연 (컴포넌트에 Sec 부분)
        SpawnPrefabs(room);                         // 프리팹 소환 함수 호출
    }

    public void SpawnPrefabs(int room) // 좌표에 적 소환
    {
        for (int i = 0; i < spawnCount; i++)
        {
            //Transform spawnTransform = Pos[room];                                               // 적을 소환할 위치
            //Vector3 position = spawnTransform.position + new Vector3(i * spawnOffset, 0, 0);    // 소환할 위치 계산
            //Instantiate(prefab, position, Quaternion.identity);                                 // 프리팹 소환

            Transform spawnTransform = EnemySpawnPos[room * 5 + i];
            Vector3 position = spawnTransform.position + new Vector3(spawnOffset, 0, 0);
            Instantiate(prefab, position, Quaternion.identity);
        }
    }

    public void DestroySpawnedObjects() // 생성된 아이템 및 상점 TP 객체 제거
    {
        foreach (var item in spawnedItems)
        {
            if (item != null)
            {
                Destroy(item);
            }
        }
        spawnedItems.Clear();

        foreach(var item in spawnedStoreTP)
        {
			if (item != null)
			{
				Destroy(item);
			}
		}
		spawnedStoreTP.Clear();
	}
}
