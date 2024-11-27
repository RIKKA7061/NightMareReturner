using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHpShow : MonoBehaviour
{
    public GameObject prefabHP_bar;//체력바(프리펩) 끌어치기
    public GameObject canvas;//canvas끌어치기
	public Player player;//플레이어

	//RectTransform hpBar;//hp바 변환하기 위한 어쩌구
	public RectTransform hpBar;//hp바 변환하기 위한 어쩌구

	public Image nowHpBar;//현재 체력바

    public float height = 1.7f;//모름

	void Start()
	{
		//player = FindObjectOfType<Player>();//무조건 해줘야됨 (초기화)
		//hpBar = Instantiate(prefabHP_bar, canvas.transform).GetComponent<RectTransform>();//체력바 canvas에다가 생성일 듯?
		//nowHpBar = hpBar.transform.GetChild(0).GetComponent<Image>();//체력바 빨간 부분만 가져오는 거일듯?
	}

	void Update()
	{
		//if(hpBar != null)
		//{
		//	Vector3 hpBarPos =
		//	Camera.main.WorldToScreenPoint(new Vector3(transform.position.x, transform.position.y + height, 0));
		//	hpBar.position = hpBarPos;
		//	nowHpBar.fillAmount = (float)player.nowHP / (float)player.maxHP;

		//	if (player.nowHP == 0)
		//	{
		//		//Destroy(hpBar.gameObject);
		//	}
		//}

		nowHpBar.fillAmount = (float)player.nowHP / (float)player.maxHP;
	}
}
