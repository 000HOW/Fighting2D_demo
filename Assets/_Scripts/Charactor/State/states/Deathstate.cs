using UnityEngine;

/// <summary>
/// 死亡状态：终态，进入后不主动退出。
/// 水平速度阻尼到 0（尸体不再滑动/被击退），Y 保留重力让尸体自然倒地（落地后 MotionActuator 归零 Y）。
/// 所有外部转换在仲裁器里被拦截（isDead 时拒绝一切 Request），复活由外部驱动（置 isDead=false + 请求新状态）。
/// </summary>
public class Deathstate : BaseCharacterstate
{
    public Deathstate()
    {
        stateType = StateType.Death;
    }

    public override void OnEntryStart(Blackboard bb)
    {
        base.OnEntryStart(bb);

        // 立即清零施加中的速度，防止尸体带着击退惯性滑出去
        bb.readytoApply.exp_horizontalVelocity = 0;
        bb.readytoApply.exp_VerticalVelocity = 0;

        // 关闭攻击盒 / 清空连击
        bb.characterRunTimeData.attackboxActive = false;
        bb.characterRunTimeData.curAttackBox = null;
        bb.comboManager?.BreakCombo();
    }

    public override void UpdatePhysics(Blackboard bb, float deltaTime)
    {
        if (stateData == null) return;

        // 水平：阻尼归零（MoveTowards 到 0），防止残留击退速度推着尸体滑动
        float newVelX = Mathf.MoveTowards(bb.characterRunTimeData.horizontalVelocity, 0,
                                          bb.CharacterSO.gravity * deltaTime);

        // 垂直：保留重力，让尸体自然落地
        float newVelY = bb.characterRunTimeData.verticalVelocity;
        newVelY -= bb.CharacterSO.gravity * deltaTime;

        bb.readytoApply.exp_horizontalVelocity = newVelX;
        bb.readytoApply.exp_VerticalVelocity = newVelY;
    }
}
