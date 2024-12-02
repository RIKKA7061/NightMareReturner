using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHpUpperManager : MonoBehaviour
{
    public Player player;

    public void ADD_Player_HP()
    {
        player.nowHP = player.maxHP;
    }
}
