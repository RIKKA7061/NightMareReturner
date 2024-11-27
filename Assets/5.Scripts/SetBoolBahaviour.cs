using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SetBoolBahaviour : StateMachineBehaviour
{
    public string boolName; // 변경할 애니메이터의 bool 변수 이름
    public bool updateOnState; // 상태 진입/종료 시 bool 값을 업데이트할지 여부
    public bool updateOnStateMachine; // 스테이트 머신 진입/종료 시 bool 값을 업데이트할지 여부
    public bool valueOnEnter, valueOnExit; // 상태나 스테이트 머신에 진입/종료할 때 설정할 bool 값

    // 상태에 진입할 때 호출되는 메서드
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (updateOnState)
        {
            // updateOnState가 true일 때 상태 진입 시 bool 값을 valueOnEnter로 설정
            animator.SetBool(boolName, valueOnEnter);
        }
    }

    // 상태를 업데이트할 때 호출되는 메서드 (현재 비활성화됨)
    //override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    
    //}

    // 상태에서 나갈 때 호출되는 메서드
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (updateOnState)
        {
            // updateOnState가 true일 때 상태 종료 시 bool 값을 valueOnExit으로 설정
            animator.SetBool(boolName, valueOnExit);
        }
    }

    // 상태에서 이동할 때 호출되는 메서드 (현재 비활성화됨)
    //override public void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    
    //}

    // IK 계산 시 호출되는 메서드 (현재 비활성화됨)
    //override public void OnStateIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    
    //}

    // 스테이트 머신에 진입할 때 호출되는 메서드
    override public void OnStateMachineEnter(Animator animator, int stateMachinePathHash)
    {
        if (updateOnStateMachine)
            // updateOnStateMachine이 true일 때 스테이트 머신 진입 시 bool 값을 valueOnEnter로 설정
            animator.SetBool(boolName, valueOnEnter);
    }

    // 스테이트 머신에서 나갈 때 호출되는 메서드
    override public void OnStateMachineExit(Animator animator, int stateMachinePathHash)
    {
        if (updateOnStateMachine)
            // updateOnStateMachine이 true일 때 스테이트 머신 종료 시 bool 값을 valueOnExit으로 설정
            animator.SetBool(boolName, valueOnExit);
    }
}
