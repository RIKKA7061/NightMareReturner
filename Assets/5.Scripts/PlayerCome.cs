using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCome : MonoBehaviour
{
	PrefabSpawner prefabSpawner;
	private void OnTriggerEnter(Collider other)
	{
		// "Player" 태그가 붙은 오브젝트인지 확인
		if (other.CompareTag("Player"))
		{
			// 프리팹 소환
			//prefabSpawner.SpawnPrefabs();
		}
	}

}
