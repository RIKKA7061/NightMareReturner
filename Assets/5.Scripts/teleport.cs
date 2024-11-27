using System.Collections;
using UnityEngine;

public class teleport : MonoBehaviour
{
    [Header("좌표")]
	public Transform [] Pos; // 이동하게될 좌표

	private PlayerAction playerAction;							// PlayerAction 스크립트
	private ItemManager itemManager;							// ItemManager 스크립트
	private GoHomeManager goHomeManager;						// GoHomeManager 스크립트
	private DirectingCameraManager directingCameraManager;      // DirectingCameraManager 스크립트
	private PrefabSpawner prefabSpawner;                        // PrefabSpawner 스크립트
	

	[Header("나 클릭했어?")]
	public bool isAlreadyClicked = false;

	private void Start()
	{
		playerAction = FindObjectOfType<Player>().GetComponent<PlayerAction>(); // PlayerAction 스크립트 가져오기
		itemManager = FindObjectOfType<ItemManager>();                          // ItemManager 스크립트 가져오기
		goHomeManager = FindObjectOfType<GoHomeManager>();                      // GoHomeManager 스크립트 가져오기	
		directingCameraManager = FindObjectOfType<DirectingCameraManager>();    // DirectingCameraManager 스크립트 가져오기
		prefabSpawner = FindObjectOfType<PrefabSpawner>();                      // PrefabSpawner 스크립트 가져오기
	}

	// 플레이어와 충돌시
	private void OnTriggerEnter2D(Collider2D collider)
	{
		Debug.Log("침대와 닿았을 시");

		// playerAction 스크립트가 비었을시
		if (playerAction == null)
		{
			return;
		}

		// Pos 끌어치기 안할시
		if (Pos == null || Pos.Length == 0)
		{
			Debug.LogError("위치가 할당되지 않았습니다.");
			return;
		}

		// Pos 끌어치기 안할시22
		foreach (var pos in Pos)
		{
			if (pos == null)
			{
				Debug.LogError("위치가 할당되지 않았습니다.");
				return;
			}
		}

		// 플레이어가 공격여부가 False 일시
		if (!playerAction.isAtking)
		{
			// 플레이어 콜라이더와 충돌시
			if (collider.CompareTag("Player"))
			{
				// 위치 좌표가 2개 이상일시
				if (Pos.Length >= 2)
				{
					// 둘중 하나 랜덤으로 넘어갑니다.
					int RANDOM_NUMBER = Random.Range(0, Pos.Length);
					collider.transform.position = Pos[RANDOM_NUMBER].position;
				}
				// 위치 좌표가 1개 일시
				else 
				{
					// 플레이어를 이동시킵니다.
					MovePlayer(collider);
				}
			}
		}
		// 플레이어 공격여부가 True 일시
		else
		{
		}
	}


	// 플레이어 이동 함수
	public void MovePlayer(Collider2D collider)
	{
        if (!isAlreadyClicked)
        {
			// 클릭 여부 True
			isAlreadyClicked = true;

			// Zoom-in 연출 이후 텔레포트 실행
			StartCoroutine(ZoomAndTeleport(collider));
		}
	}

	// Zoom-in 연출 이후 텔레포트 코루틴
	private IEnumerator ZoomAndTeleport(Collider2D collider)
	{
		// Zoom-in 연출 (1초 대기)
		directingCameraManager.ZoomIn();
		yield return new WaitForSeconds(1f);

		directingCameraManager.darkBg_On();

		yield return new WaitForSeconds(0.5f);

		// 플레이어 텔포
		collider.transform.position = Pos[0].position;  // 플레이어 해당 위치로 전송

		// 클릭 여부 False
		isAlreadyClicked = false;

		Player.gameRound++;
		prefabSpawner.isSpawnned = false;    // 다음 라운드로 문을 열었으니 몬스터 소환이 가능해집니다.
		itemManager.RoundUp();                          // 라운드 기회 1소모 (단, 버프중 일때)

		if (itemManager.isNextRoundHpUp == true)
		{
			itemManager.HpUP();
		}

		if (itemManager.isCrunchMode == true)
		{
			itemManager.CrunchModeEye();
		}

		goHomeManager.isRoundStart = true;

		directingCameraManager.darkBg_Off();

		directingCameraManager.ZoomOut();
	}

}