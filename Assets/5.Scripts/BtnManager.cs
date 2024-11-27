using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BtnManager : MonoBehaviour
{
	//툴팁
	public GameObject tip;
    public GameObject shop;

    //개발자
    public GameObject Dev;

	//창
	public TextMeshProUGUI TipText;

	private bool isOpened = false;
	public string text = "디폴트";

	void Start()
	{
		// 할당 확인을 위한 디버그 메시지
		if (tip == null)
		{
			Debug.LogError("Tip 오브젝트가 할당되지 않았습니다.");
		}
		if (Dev == null)
		{
			Debug.LogError("Dev 오브젝트가 할당되지 않았습니다.");
		}
		if (TipText == null)
		{
			Debug.LogError("TipText 오브젝트가 할당되지 않았습니다.");
		}
	}

	public void OnAndOFF()
	{
		if (tip != null) // Null 체크
		{
			if (isOpened == false)
			{
				tip.SetActive(true);
				isOpened = true;
			}
			else if (isOpened == true)
			{
				tip.SetActive(false);
				isOpened = false;
			}
		}
		else
		{
			Debug.LogError("Tip 오브젝트가 null 상태입니다.");
		}
	}

	public void OFF()
	{
		tip.SetActive(false);
        shop.SetActive(false);
        isOpened = false;
	}

	public void Developement()
	{
		if (TipText != null) // Null 체크
		{
			string text = "집키 - H\n자결 - K";
			TipText.text = text;
		}
		else
		{
			Debug.LogError("TipText 오브젝트가 null 상태입니다.");
		}
	}

	public void ControlKeyTips()
	{
		if (TipText != null) // Null 체크
		{
			string text = "이동 방향 - 화살표 방향키\r\n기본 공격 - 좌클릭\r\n스킬 공격 - 우클릭\r\n궁극기 - R\r\n슬라이딩 - 쉬프트\r\n대화 - E";
			TipText.text = text;
		}
		else
		{
			Debug.LogError("TipText 오브젝트가 null 상태입니다.");
		}
	}
}
