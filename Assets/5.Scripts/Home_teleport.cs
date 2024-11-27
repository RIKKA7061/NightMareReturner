using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Home_teleport : MonoBehaviour
{
	//이동할 위치를 public으로 설정하여 Unity 에디터에서 지정할 수 있게 함
	public Transform Home;// 플레이어가 스폰될 게임 오브젝트 좌표

	//트리거에 충돌했을 때 호출되는 함수
	private void OnTriggerEnter2D(Collider2D other)//닿은 접촉체를 매개변수로 불러옴
	{
		//플레이어가 충돌했는지 확인 (태그로 플레이어를 구분)
		if (other.CompareTag("Player"))//닿은 접촉체의 태그가 Player일 경우
		{
			Debug.Log("집으로 모실께요~");

			other.transform.position = Home.position;
			//닿은 접촉체의 플레이어 좌표가 스폰될 게임 오브젝트 좌표로 이동
		}
	}
}
