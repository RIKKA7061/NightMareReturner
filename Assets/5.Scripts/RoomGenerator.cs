using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomGenerator : MonoBehaviour
{
	/* 문 선언 */
	public GameObject hpUpDoor;         // 체력 증가 문
	public GameObject atkUpDoor;        // 공격력 증가 문
	public GameObject emotionRoundDoor; // 감정 구슬 획득 문
	public GameObject MoneyUpDoor;      // 재화 획득 문

	/* 문이 생성될 좌표 위치 */
	[Header("문이 생성될 좌표 위치")]
	public Transform[] Pos;

	private List<DoorType> doorType;    // 문 들의 타입형 리스트

	// 생성된 문들의 참조를 저장할 리스트
	private List<GameObject> generatedDoors = new List<GameObject>();

	void Start()
	{
		//RandomDoorGenerate(); // 랜덤 문 생성
	}

	// key 0번 : 기본 시작 방
	// key 1번 : 체력방
	// key 2번 : 공격방
	// key 3번 : 재화방
	// key 4번 : 구슬방
	public void RandomDoorGenerate(int key) // 랜덤 문 생성
	{
		// 문 타입들을 리스트에 추가
		doorType = new List<DoorType> { DoorType.hpUP, DoorType.atkUP, DoorType.MoneyUp, DoorType.EmotionRound };

		// 문 2개를 랜덤으로 뽑기을 시 2개 문들을 담을 리스트
		List<DoorType> selectedDoors = new List<DoorType>();

		// 문 2개를 랜덤으로 뽑기
		for (int i = 0; i < 2; i++)
		{
			// 아직 선택 되지 않은 문들 중에서 하나를 선택
			// [NR] 감정 구슬 문(증강)이 조금 더 자주 나오도록 가중치 (감정 2 : 나머지 1)
			int RandomNumber = WeightedPick(doorType);
			selectedDoors.Add(doorType[RandomNumber]);            // 랜덤한 문의 타입을 선택된 리스트에 추가
			doorType.RemoveAt(RandomNumber);                    // 리스트에 추가된 문은 기존 리스트에서 제거
		}

		key = key * 4;

		// 선택한 방을 생성
		foreach (var door in selectedDoors)
		{
			GameObject createdDoor = null;
			switch (door)
			{
				case DoorType.hpUP:
					createdDoor = Instantiate(hpUpDoor, Pos[key].position, Quaternion.identity);
					createdDoor.name = "NRDoor_Health";
					break;
				case DoorType.atkUP:
					createdDoor = Instantiate(atkUpDoor, Pos[key+1].position, Quaternion.identity);
					createdDoor.name = "NRDoor_Attack";
					break;
				case DoorType.MoneyUp:
					createdDoor = Instantiate(MoneyUpDoor, Pos[key+2].position, Quaternion.identity);
					createdDoor.name = "NRDoor_Money";
					break;
				case DoorType.EmotionRound:
					createdDoor = Instantiate(emotionRoundDoor, Pos[key+3].position, Quaternion.identity);
					createdDoor.name = "NRDoor_Emotion";
					break;
			}
			if (createdDoor != null)
			{
				createdDoor.SetActive(true);
				generatedDoors.Add(createdDoor); // 생성된 문 참조를 리스트에 저장
			}
		}
	}

	// [NR] 가중치 랜덤
	int WeightedPick(List<DoorType> types)
	{
		float total = 0f;
		foreach (var t in types) total += t == DoorType.EmotionRound ? 2f : 1f;
		float r = Random.value * total;
		for (int i = 0; i < types.Count; i++)
		{
			r -= types[i] == DoorType.EmotionRound ? 2f : 1f;
			if (r <= 0f) return i;
		}
		return types.Count - 1;
	}

	// 문 초기화 (삭제용)
	public void DestroyDoor()
	{
		foreach (GameObject door in generatedDoors)
		{
			Destroy(door); // 생성된 문 삭제
		}
		generatedDoors.Clear(); // 참조 리스트 초기화
	}
}
