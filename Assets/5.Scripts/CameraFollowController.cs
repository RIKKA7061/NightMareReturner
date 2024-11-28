using UnityEngine;
using Cinemachine;

public class CameraFollowController : MonoBehaviour
{
	public CinemachineVirtualCamera virtualCamera; // Cinemachine Virtual Camera
	public Transform playerCameraPos; // �÷��̾� Transform
	public float minX = -9.49f; // X ��ǥ �ּҰ�
	public float maxX = 10.11f; // X ��ǥ �ִ밪

	void Update()
	{
		// �÷��̾��� X ��ǥ�� Ȯ��
		float playerX = playerCameraPos.position.x;

		// X ��ǥ�� ������ ����� ����ٴϱ� ����
		if (playerX < minX || playerX > maxX)
		{
			virtualCamera.Follow = null; // ����ٴϱ� ����
		}
		else
		{
			virtualCamera.Follow = playerCameraPos; // ����ٴϱ� ����
		}
	}
}
