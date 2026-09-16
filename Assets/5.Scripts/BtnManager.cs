using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// [NR] 기존 버튼 이벤트(씬에 연결된 함수 이름)는 유지.
//      현황/설정은 새 창(TAB 현황, ESC 메뉴)으로 연결하고, timeScale은 NRTime에서 일괄 관리
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
        var go = GameObject.FindGameObjectWithTag("Audio");
        audioManager = go != null ? go.GetComponent<AudioManager>() : null;
    }

    void PlayClick()
    {
        if (audioManager != null && audioManager.audio != null && audioManager.audio.Length > 4)
            audioManager.PlayerSFX(audioManager.audio[4]);
    }

	public void OnAndOFF()
	{
		if (tip != null) // Null 체크
		{
			isOpened = !isOpened;
			tip.SetActive(isOpened);
		}
	}

	public void OFF()
	{
		if (tip != null) tip.SetActive(false);
        if (shop != null) shop.SetActive(false);
        if (myData != null) myData.SetActive(false);
		if (setting != null) setting.SetActive(false);
        isOpened = false;
        NRTime.Resume("legacy");
    }

	public void MyDataBtn()
	{
        PlayClick();
        NRStatusWindow.Show(); // [NR] 새 현황 창
    }


    public void StatBtn()
	{
        PlayClick();
        if (statSet != null) statSet.SetActive(true);
        if (itemSet != null) itemSet.SetActive(false);
        if (floorSet != null) floorSet.SetActive(false);
        if (storySet != null) storySet.SetActive(false);
    }

	public void ItemBtn()
	{
        PlayClick();
        if (itemSet != null) itemSet.SetActive(true);
        if (statSet != null) statSet.SetActive(false);
        if (floorSet != null) floorSet.SetActive(false);
        if (storySet != null) storySet.SetActive(false);
    }

	public void FloorBtn()
	{
        PlayClick();
        if (floorSet != null) floorSet.SetActive(true);
        if (statSet != null) statSet.SetActive(false);
        if (itemSet != null) itemSet.SetActive(false);
        if (storySet != null) storySet.SetActive(false);
    }

	public void StoryBtn()
	{
        PlayClick();
        if (storySet != null) storySet.SetActive(true);
        if (floorSet != null) floorSet.SetActive(false);
        if (statSet != null) statSet.SetActive(false);
        if (itemSet != null) itemSet.SetActive(false);
    }

    public void Item1()
    {
        PlayClick();
        if (item1 != null) item1.SetActive(true);
        if (item2 != null) item2.SetActive(false);
        if (item3 != null) item3.SetActive(false);
    }

    public void Item2()
    {
        PlayClick();
        if (item2 != null) item2.SetActive(true);
        if (item1 != null) item1.SetActive(false);
        if (item3 != null) item3.SetActive(false);
    }

    public void Item3()
    {
        PlayClick();
        if (item3 != null) item3.SetActive(true);
        if (item1 != null) item1.SetActive(false);
        if (item2 != null) item2.SetActive(false);
    }

	public void SettingBtn()
	{
        PlayClick();
        NRPauseMenu.Show(); // [NR] 새 일시정지/설정 메뉴 (ESC와 동일)
	}

    public void Developement()
	{
		if (TipText != null) // Null 체크
		{
			TipText.text = "집키 - H\n자결 - K";
		}
	}

	public void ControlKeyTips()
	{
		if (TipText != null) // Null 체크
		{
			TipText.text = "이동 - WASD\r\n기본 공격 - 좌클릭\r\n특수 공격 - 우클릭\r\n궁극기 - R\r\n구르기 - 스페이스바\r\n대화 - E";
		}
	}
}
