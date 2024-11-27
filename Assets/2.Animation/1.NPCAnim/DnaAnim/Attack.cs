using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Attack : MonoBehaviour
{
    Animator animator;
    int hashAttackCount = Animator.StringToHash("AttackCount");
    // Start is called before the first frame update
    void Start()
    {
        TryGetComponent(out animator);
    }

    // Update is called once per frame
    public int AttackCount
    {
        get => animator.GetInteger(hashAttackCount);
        set => animator.SetInteger(hashAttackCount, value);
    }
}
