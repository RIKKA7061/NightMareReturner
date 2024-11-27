using Cinemachine;
using System.Collections;
using UnityEngine;

public class DirectingCameraManager : MonoBehaviour
{
	public CinemachineVirtualCamera virtualCamera; // CMvcam1을 할당

	[Header("목표 줌값 설정")]
	public float zoomSize = 1f;

	[Header("연출 시간")]
	public float delayTime = 1f;

	private float originalSize; // 카메라의 초기 크기 저장


	[Header("연출 때 쓰일 까만 판때기")]
	public GameObject darkBg;

	void Start()
	{
		// 카메라 초기 크기를 저장
		originalSize = virtualCamera.m_Lens.OrthographicSize;

		// 줌아웃 연출 시작
		SetOrthoSize(1);
		ZoomOut();
	}

	// 까만 판때기로 전체 화면 가리기
	public void darkBg_On()
	{
		darkBg.SetActive(true);
	}

	// 까만 판때기로 전체 화면 가려진 거 없애기
	public void darkBg_Off()
	{
		darkBg.SetActive(false);
	}

	// 카메라 줌 사이즈 크기 조절 메서드
	public void SetOrthoSize(float size)
	{
		virtualCamera.m_Lens.OrthographicSize = size;
	}

	public void ZoomIn() // 줌인 메서드
	{
		StartCoroutine(SmoothZoomTo(zoomSize, delayTime));
	}

	public void ZoomOut() // 줌아웃 메서드
	{
		StartCoroutine(SmoothZoomToOrigin(delayTime));
	}


	// 자연스럽게 줌인 하는 연출
	public IEnumerator SmoothZoomTo(float targetSize, float duration)
	{
		Debug.Log("줌인 연출 시작");
		float startSize = virtualCamera.m_Lens.OrthographicSize; // 현재 크기
		float elapsedTime = 0f; // 경과 시간

		while (elapsedTime < duration)
		{
			elapsedTime += Time.deltaTime;

			// 현재 크기를 선형적으로 계산
			float newSize = Mathf.Lerp(startSize, targetSize, elapsedTime / duration);
			SetOrthoSize(newSize);

			yield return null; // 다음 프레임까지 대기
		}

		// 정확한 목표 값으로 설정
		SetOrthoSize(targetSize);

	}

	// 자연스럽게 줌 아웃 하는 연출
	public IEnumerator SmoothZoomToOrigin(float duration)
	{
		Debug.Log("줌아웃 연출 시작");
		float startSize = virtualCamera.m_Lens.OrthographicSize; // 현재 크기
		float elapsedTime = 0f; // 경과 시간

		while (elapsedTime < duration)
		{
			elapsedTime += Time.deltaTime;

			// 원래 크기를 선형적으로 계산
			float newSize = Mathf.Lerp(startSize, originalSize, elapsedTime / duration);
			SetOrthoSize(newSize);

			yield return null; // 다음 프레임까지 대기
		}

		// 정확한 원래 값으로 설정
		SetOrthoSize(originalSize);
	}
}