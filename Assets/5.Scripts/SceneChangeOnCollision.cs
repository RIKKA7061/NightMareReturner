using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChangeOnCollision : MonoBehaviour
{
	// 충돌했을 때 전환할 씬의 이름
	public string sceneName;

	public bool isSideViewScene = false;

	private DirectingCameraManager directingCameraManager;

	private void Start()
	{
		directingCameraManager = FindObjectOfType<DirectingCameraManager>();

		// 사이드 뷰 씬 전용
		if (isSideViewScene)
		{
			StartCoroutine(OnlySideView());

		}
	}

	IEnumerator OnlySideView()
	{
		directingCameraManager.darkBg_On();
		yield return new WaitForSeconds(0.1f);
		directingCameraManager.SetOrthoSize(1);
		yield return new WaitForSeconds(0.1f);
		directingCameraManager.darkBg_Off();

		directingCameraManager.ZoomOut();
	}

	private void OnTriggerEnter2D(Collider2D collision)
	{
		// 충돌한 객체가 특정 조건을 만족하면 씬 전환
		if (collision.gameObject.CompareTag("Player")) // 예: "Player" 태그가 붙은 객체와 충돌 시
		{
			// 씬 전환
			SceneChangeHamSu();
		}
	}

	public void SceneChangeHamSu()
	{
		StartCoroutine(ZoomInAndSceneChange());
	}

	IEnumerator ZoomInAndSceneChange()
	{
		// Zoom-in 연출 (1초 대기)
		directingCameraManager.ZoomIn();

		yield return new WaitForSeconds(1f);

		directingCameraManager.darkBg_On();

		yield return new WaitForSeconds(0.4f);

		SceneManager.LoadScene(sceneName);
	}

	IEnumerator ZoomOut()
	{
		// Zoom-in 연출 (1초 대기)
		directingCameraManager.ZoomOut();
		yield return new WaitForSeconds(1f);

		directingCameraManager.darkBg_On();
	}
}
