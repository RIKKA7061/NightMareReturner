using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BtnManager : MonoBehaviour
{
	//����
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

    //������
    public GameObject Dev;

	//â
	public TextMeshProUGUI TipText;

	AudioManager audioManager;

	private bool isOpened = false;
	public string text = "����Ʈ";

    void Awake()
    {
        audioManager = GameObject.FindGameObjectWithTag("Audio").GetComponent<AudioManager>();
    }

    void Start()
	{
		// �Ҵ� Ȯ���� ���� ����� �޽���
		if (tip == null)
		{
			Debug.LogError("Tip ������Ʈ�� �Ҵ���� �ʾҽ��ϴ�.");
		}
		if (Dev == null)
		{
			Debug.LogError("Dev ������Ʈ�� �Ҵ���� �ʾҽ��ϴ�.");
		}
		if (TipText == null)
		{
			Debug.LogError("TipText ������Ʈ�� �Ҵ���� �ʾҽ��ϴ�.");
		}
	}

	public void OnAndOFF()
	{
		if (tip != null) // Null üũ
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
			Debug.LogError("Tip ������Ʈ�� null �����Դϴ�.");
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
		if (TipText != null) // Null üũ
		{
			string text = "��Ű - H\n�ڰ� - K";
			TipText.text = text;
		}
		else
		{
			Debug.LogError("TipText ������Ʈ�� null �����Դϴ�.");
		}
	}

	public void ControlKeyTips()
	{
		if (TipText != null) // Null üũ
		{
			string text = "�̵� - WASD\r\n�⺻ ���� - ��Ŭ��\r\nƯ�� ���� - ��Ŭ��\r\n�ñر� - R\r\n������ - �����̽���\r\n��ȭ - E";
			TipText.text = text;
		}
		else
		{
			Debug.LogError("TipText ������Ʈ�� null �����Դϴ�.");
		}
	}
}
