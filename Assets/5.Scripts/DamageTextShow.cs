using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// [NR] 캔버스 UI 텍스트 대신 월드 공간 피해 숫자(NRCombatFX) 사용 — 팝/낙하/치명타 색상
public class DamageTextShow : MonoBehaviour
{
	[Header("예는 안 갖다 붙여도 됨")]
	public TextMeshProUGUI damage_text;

	[Header("데미지 텍스트 프리펩 (현재 미사용)")]
	public GameObject damage_text_prf;

	public void ShowDamage(int damage)
	{
		NRCombatFX.DamageNumber(transform.position + Vector3.up * 1.2f, damage, false, NRDamageKind.Direct);
	}
}
