using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DamageTextShow : MonoBehaviour
{
	[Header("예는 안 갖다 붙여도 됨")]
	public TextMeshProUGUI damage_text;

	[Header("데미지 텍스트 프리펩")]
	public GameObject damage_text_prf;

	public void ShowDamage(int damage)
	{
		StartCoroutine(ShowDamageText(damage));
	}

	// 대미지 텍스트 생성 및 1초 후 삭제
	IEnumerator ShowDamageText(int damage)
	{
		// 텍스트 프리팹 생성
		GameObject dmgText = Instantiate(damage_text_prf, GameObject.Find("Canvas").transform);
		TextMeshProUGUI dmgTextComponent = dmgText.GetComponent<TextMeshProUGUI>();

		// 텍스트 내용과 위치 설정
		dmgTextComponent.text = damage.ToString();

		// 초기 위치 설정 (월드 -> 스크린 좌표 변환)
		Vector3 startPosition = Camera.main.WorldToScreenPoint(transform.position);
		dmgText.transform.position = startPosition;

		// 이동할 y값 설정
		float moveDistance = 50f; // y축 이동 거리 (Canvas 기준)
		float duration = 0.2f;    // 지속 시간
		float elapsedTime = 0f;   // 경과 시간

		// 코루틴으로 텍스트 위치 이동
		while (elapsedTime < duration)
		{
			elapsedTime += Time.deltaTime;

			// 현재 위치 계산
			Vector3 currentPosition = dmgText.transform.position;
			currentPosition.y = Mathf.Lerp(startPosition.y, startPosition.y + moveDistance, elapsedTime / duration);
			dmgText.transform.position = currentPosition;

			yield return null; // 다음 프레임까지 대기
		}

		// 텍스트 삭제
		Destroy(dmgText);
	}

}
