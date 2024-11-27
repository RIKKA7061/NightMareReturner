using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class _Object : MonoBehaviour
{
    public int id;//NPC번호

    //NPC별 횟차별 대화 횟수
    public int EachTalkCount = 0;
    public int TempEachTalkCount = 0;
    static public int[] EachTalkCountSaveNum = new int[4];

    //테스트용 이름 변수
    public new string name;

    //NPC별 죽은뒤 대화 확인용 bool형 변수
	public bool isDeadUp = false;
	public bool isUpGraded = false;

    public int PlayerDead;
    public int tempPlayerDead = 0;
    public bool isDialogged = true;

    public GameObject malPeungSeon;         // 말풍선 이미지
    private Player player;                  // 플레이어 스크립트
    private float range = 2f;               // 플레이어 감지범위

    public void EachTalkCountSave(int id) // 저장
    {
        id = id / 100;

        EachTalkCountSaveNum[id]++;
        if(EachTalkCountSaveNum[id] > EachTalkCount)
        {
            EachTalkCount = EachTalkCountSaveNum[id];
        }
    }

    public void Start()
    {
        player = FindObjectOfType<Player>();// 무조건 해줘야됨 (초기화)
    }

    public void ResetEachTalkCountSave()
    {
        // 대화 저장 초기화
        for (int i = 0; i < EachTalkCountSaveNum.Length; i++)
        {
            EachTalkCountSaveNum[i] = 0;
        }

        EachTalkCount = 0;        // 현재 대화 번호 초기화
    }


	private void FixedUpdate()
	{
        /*플레이어가 가까이 왔을시 말풍선 보이기*/
		Vector2 direction = player.transform.position - transform.position;

		float X = Mathf.Abs(Mathf.Round(direction.x));
		float Y = Mathf.Abs(Mathf.Round(direction.y));

		if (isDialogged && (X<= range && Y <= range))//선언문에서 조절하셈
        {
			malPeungSeon.SetActive(true);
		}
        else if (!isDialogged || (X >= range && Y >= range))//선언문에서 조절하셈
		{
            malPeungSeon.SetActive(false);
        }
	}
}


//레포트 APK