using UnityEngine;

/// <summary>
/// 受击专用状态
/// 与普通状态的区别：击退方向来自运行时 hitDirection（黑板），而不是 facingDir。
/// 力度/时长/动画仍由注入的 StateData（HitInfo 配置）决定。
/// </summary>
public class Hitstate : BaseCharacterstate
{
    public Hitstate()
    {
        stateType = StateType.Hit;
    }

    public override void UpdatePhysics(Blackboard bb, float deltaTime)
    {
        if (stateData == null) return;

        // 受击方向：运行时上下文（DamageReceiver 在请求状态前写入）
        Vector2 dir = bb.characterRunTimeData.hitDirection;
        if (dir.sqrMagnitude < 0.0001f)
            dir = Vector2.left; // 兜底：没有方向时默认向左推

        StateProgress progress = bb.characterRunTimeData.stateProgress;
        MotionSettings motion = GetMotionForProgress(progress);
        if (motion == null) return;

        float t = GetCurrentPhaseNormalizedTime(progress);
        float mulX = motion.XspeedMultiplier.Evaluate(t);
        float mulY = motion.YspeedMultiplier.Evaluate(t);

        // 力度 = 配置极值 × 受击方向 × 曲线衰减（方向不再是 facingDir）
        float targetVelX;
        if (dir.x > 0)
        targetVelX = motion.maxVelocity.x * mulX;
        else
        targetVelX = - motion.maxVelocity.x * mulX;

        float targetVelY;
        if (motion.useYCoverageWeight)
        {
            // 覆盖式：上挑/击飞由配置 maxVelocity.y 与受击方向 y 决定
            if (dir.y >= 0)
            targetVelY = motion.maxVelocity.y * mulY;
            else
            targetVelY = - motion.maxVelocity.y * mulY;
        }
        else
        {
            // 保留原速度，让重力接管（地面水平受击的常规路径）
            targetVelY = bb.characterRunTimeData.verticalVelocity;
            targetVelY -= bb.CharacterSO.gravity * motion.gravityScale * deltaTime;
        }

        Vector2 current = new Vector2(bb.characterRunTimeData.horizontalVelocity,
                                      bb.characterRunTimeData.verticalVelocity);

        float newVelX = motion.RespondImmediately
            ? targetVelX
            : Mathf.MoveTowards(current.x, targetVelX, motion.acceleration * deltaTime);

        float newVelY = motion.RespondImmediately
            ? targetVelY
            : Mathf.MoveTowards(current.y, targetVelY, motion.acceleration * deltaTime);

        bb.readytoApply.exp_horizontalVelocity = newVelX;
        bb.readytoApply.exp_VerticalVelocity = newVelY;
    }
}
