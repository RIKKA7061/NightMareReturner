using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

internal class AnimationStrings
{
    // 캐릭터가 움직이고 있는지 여부를 나타내는 플래그
    internal static string isMoving = "isMoving";

    // 캐릭터가 슬라이딩 중인지 여부를 나타내는 플래그
    internal static string isSliding = "isSliding";

    // 공격 애니메이션을 시작하는 트리거
    internal static string attackTrigger = "attack";

    // 캐릭터가 이동할 수 있는지 여부를 나타내는 플래그
    internal static string canMove = "canMove";

    // 캐릭터가 타겟을 가지고 있는지 여부를 나타내는 플래그
    internal static string hasTarget = "hasTarget";

    // 캐릭터가 살아있는지 여부를 나타내는 플래그
    internal static string isAlive = "isAlive";

    // 스킬 공격 애니메이션을 시작하는 첫 번째 트리거
    internal static string skillAttackTrigger = "skillAttack";

    // 스킬 공격 애니메이션을 시작하는 두 번째 트리거
    internal static string skillAttackTrigger2 = "skillAttack2";

    // 플레이어 죽음
    internal static string DeadTrigger = "Dead";

    // 플레이어 피격 // 아파요
    internal static string OuchTrigger = "Ouch";
}
