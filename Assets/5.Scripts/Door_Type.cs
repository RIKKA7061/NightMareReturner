using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Door_Type : MonoBehaviour
{
    void Start()
    {
    }
}


public enum DoorType // 문 들의 집합 ex. 체력문, 공격증진문 등등
{
    hpUP,            // 최대 체력 증진 문
    atkUP,           // 공격력 증진 문
    EmotionRound,    // 감정 구슬 획득 문
    MoneyUp          // 재화 획득 문
}
