using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Scanner2 : MonoBehaviour
{
	//원거리 공격용 스크립트
	public float scanRange;
	public LayerMask targetLayer;
	public RaycastHit2D[] targets;
	public Transform nearestTarget;

	void FixedUpdate()
	{
		targets = Physics2D.CircleCastAll(transform.position, scanRange, Vector2.zero, 0, targetLayer); //원 범위안에 타겟 스캔
		nearestTarget = GetNearest();
	}

	Transform GetNearest()
	{
		Transform result = null;
		float diff = 100;

		foreach (RaycastHit2D target in targets)
		{
			Vector3 mypos = transform.position;
			Vector3 targetpos = target.transform.position;
			float curdiff = Vector3.Distance(mypos, targetpos);

			if (curdiff < diff)
			{
				diff = curdiff;
				result = target.transform;
			}
		}

		return result;
	}
}
