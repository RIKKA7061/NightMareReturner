using UnityEngine;
using Cinemachine;

public class CameraFollowController : MonoBehaviour
{
	public CinemachineVirtualCamera virtualCamera; // Cinemachine Virtual Camera
	public Transform playerCameraPos; // 플레이어 Transform
	public float minX = -9.49f; // X 좌표 최소값
	public float maxX = 10.11f; // X 좌표 최대값

	void Update()
	{
		// 플레이어의 X 좌표를 확인
		float playerX = playerCameraPos.position.x;

		// X 좌표가 범위를 벗어나면 따라다니기 중지
		if (playerX < minX || playerX > maxX)
		{
			virtualCamera.Follow = null; // 따라다니기 중지
		}
		else
		{
			virtualCamera.Follow = playerCameraPos; // 따라다니기 시작
		}
	}
}
