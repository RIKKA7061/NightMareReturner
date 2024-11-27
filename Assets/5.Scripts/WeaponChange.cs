using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponChange : MonoBehaviour
{
    public List<GameObject> weapons; // 무기 리스트
    private int selectedWeapon = 0;  // 현재 선택된 무기 인덱스


    void Start()
    {
        SelectWeapon();  // 시작할 때 기본 무기 선택
    }

    void Update()
    {
        // 숫자 키로 무기 교체
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            selectedWeapon = 0;
            SelectWeapon();
        }
        if (Input.GetKeyDown(KeyCode.Alpha2) && weapons.Count >= 2)
        {
            selectedWeapon = 1;
            SelectWeapon();
        }
        if (Input.GetKeyDown(KeyCode.Alpha3) && weapons.Count >= 3)
        {
            selectedWeapon = 2;
            SelectWeapon();
        }

        // 마우스 휠로 무기 교체
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll > 0f)
        {
            selectedWeapon = (selectedWeapon + 1) % weapons.Count;
            SelectWeapon();
        }
        else if (scroll < 0f)
        {
            selectedWeapon--;
            if (selectedWeapon < 0) selectedWeapon = weapons.Count - 1;
            SelectWeapon();
        }
    }

    void SelectWeapon()
    {
        // 모든 무기를 비활성화하고, 선택된 무기만 활성화
        for (int i = 0; i < weapons.Count; i++)
        {
            weapons[i].SetActive(i == selectedWeapon);
        }
    }
}
