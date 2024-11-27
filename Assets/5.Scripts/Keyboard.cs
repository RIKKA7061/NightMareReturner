using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Keyboard : MonoBehaviour
{
	//플레이어의 좌표 변수 인듯?
	public Transform player;
	private Player playerScript;
	public GameObject Dev_Btn;

	//좌표 변수 인듯?
	public Transform[] Pos;
	//Pos[0] 집 좌표
	//Pos[1] room1 좌표
	//Pos[2] room2 좌표
	//Pos[3] room3 좌표...

	private void Start()
	{
		playerScript = FindObjectOfType<Player>();
	}
	private void Update()
	{
		if (Dev_Btn.activeSelf)
		{
			//Debug.Log("Dev_Btn은 활성화된 상태입니다.");

			if (Input.GetKeyDown(KeyCode.H))
			{
				//플레이어 좌표를 -> 집 좌표
				player.position = Pos[0].position;
			}

			if (Input.GetKeyDown(KeyCode.K))
			{
				//소천하셨습니다.
				playerScript.Dead();
			}
		}
		else
		{
			//Debug.Log("Dev_Btn은 비활성화된 상태입니다.");
		}


		////숫자키 = 방번호
		//if (Input.GetKeyDown(KeyCode.Alpha1))
		//	player.position = Pos[1].position;

		//if (Input.GetKeyDown(KeyCode.Alpha2))
		//	player.position = Pos[2].position;

		//if (Input.GetKeyDown(KeyCode.Alpha3))
		//	player.position = Pos[3].position;

		//if (Input.GetKeyDown(KeyCode.Alpha4))
		//	player.position = Pos[4].position;

		//if (Input.GetKeyDown(KeyCode.Alpha5))
		//	player.position = Pos[5].position;

		//if (Input.GetKeyDown(KeyCode.Alpha6))
		//	player.position = Pos[6].position;
	}
}
