using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyAnySec : MonoBehaviour
{
    void Start()
    {
		StartCoroutine(DestroySelf());
    }

	IEnumerator DestroySelf()
	{
		// N초 기다린 뒤 삭제
		yield return new WaitForSeconds(0.2f);
		Destroy(gameObject);
	}
}
