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
	public GameObject myData;
	public GameObject statSet;
    public GameObject itemSet;
    public GameObject floorSet;
	public GameObject storySet;
	public GameObject statButton;
	public GameObject item1;
    public GameObject item2;
    public GameObject item3;
	public GameObject setting;

    //개발자
    public GameObject Dev;

	//창
	public TextMeshProUGUI TipText;

	AudioManager audioManager;

	private bool isOpened = false;
	public string text = "디폴트";

    void Awake()
    {
        audioManager = GameObject.FindGameObjectWithTag("Audio").GetComponent<AudioManager>();
    }

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
        myData.SetActive(false);
		setting.SetActive(false);
        isOpened = false;
		Time.timeScale = 1f;
    }

	public void MyDataBtn()
	{
        audioManager.PlayerSFX(audioManager.audio[4]);
        myData.SetActive(true);
        statButton.SetActive(true);
		Time.timeScale = 0f;
    }


    public void StatBtn()
	{
        audioManager.PlayerSFX(audioManager.audio[4]);
        statSet.SetActive(true);
        itemSet.SetActive(false);
        floorSet.SetActive(false);
        storySet.SetActive(false);
    }

	public void ItemBtn()
	{
        audioManager.PlayerSFX(audioManager.audio[4]);
        itemSet.SetActive(true);
        statSet.SetActive(false);
        floorSet.SetActive(false);
        storySet.SetActive(false);
    }

	public void FloorBtn()
	{
        audioManager.PlayerSFX(audioManager.audio[4]);
        floorSet.SetActive(true);
        statSet.SetActive(false);
        itemSet.SetActive(false);
        storySet.SetActive(false);
    }

	public void StoryBtn()
	{
        audioManager.PlayerSFX(audioManager.audio[4]);
        storySet.SetActive(true);
        floorSet.SetActive(false);
        statSet.SetActive(false);
        itemSet.SetActive(false);

    }

    public void Item1()
    {
        audioManager.PlayerSFX(audioManager.audio[4]);
        item1.SetActive(true);
        item2.SetActive(false);
        item3.SetActive(false);
    }

    public void Item2()
    {
        audioManager.PlayerSFX(audioManager.audio[4]);
        item2.SetActive(true);
        item1.SetActive(false);
        item3.SetActive(false);
    }

    public void Item3()
    {
        audioManager.PlayerSFX(audioManager.audio[4]);
        item3.SetActive(true);
        item1.SetActive(false);
        item2.SetActive(false);
    }

	public void SettingBtn()
	{
        audioManager.PlayerSFX(audioManager.audio[4]);
        setting.SetActive(true);
		Time.timeScale = 0f;
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
			string text = "이동 - WASD\r\n기본 공격 - 좌클릭\r\n특수 공격 - 우클릭\r\n궁극기 - R\r\n구르기 - 스페이스바\r\n대화 - E";
			TipText.text = text;
		}
		else
		{
			Debug.LogError("TipText 오브젝트가 null 상태입니다.");
		}
	}
}
