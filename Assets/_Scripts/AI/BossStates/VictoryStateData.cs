using UnityEngine;

/// <summary>
/// Boss 胜利动作（Custom 技能状态）：播放胜利动画并定身。
///  - 触发：演出节点手动调 BossBrain.TriggerVictory（先停 AI 再 UseSkill）
///  - 退出：holdDuration > 0 且到时自动回 Idle；holdDuration = 0（默认）不自动退出，由演出节点接管
/// 无 SO 运行时字段：施放计时走 brain.CurrentCast.castTimer。
/// </summary>
[CreateAssetMenu(fileName = "VictoryStateData", menuName = "Enemy/VictoryStateData")]
public class VictoryStateData : BaseCustomStateData
{
    [Header("胜利")]
    [Tooltip("胜利动画停留时长（0=不自动退出，由演出节点接管）")] public float holdDuration = 0f;

    public override void OnEntryStart(Blackboard bb)
    {
        var cur = GetBrain(bb)?.CurrentCast;
        if (cur != null) cur.castTimer = 0f;
    }

    public override void OnExitStart(Blackboard bb) { }
    public override void OnEntry(Blackboard bb) { }
    public override void OnExit(Blackboard bb) { }

    public override void OnUpdate(Blackboard bb)
    {
        var rt = bb.characterRunTimeData;
        if (rt.isDead || rt.self == null) return;

        var cur = GetBrain(bb)?.CurrentCast;
        if (cur == null) return;

        cur.castTimer += Time.fixedDeltaTime;
        if (holdDuration > 0f && cur.castTimer >= holdDuration) Drop(bb);
    }

    public override void UpdatePhysics(Blackboard bb, float deltaTime)
    {
        // 胜利定身：水平阻尼到 0，保留重力（站定展示胜利动画）
        var rt = bb.characterRunTimeData;
        bb.readytoApply.exp_horizontalVelocity = Mathf.MoveTowards(rt.horizontalVelocity, 0f, 20f * deltaTime);
        bb.readytoApply.exp_VerticalVelocity = rt.verticalVelocity;
    }

    void Drop(Blackboard bb)
    {
        bb.characterRunTimeData.self.GetComponent<CharacterControler>()
            .arbiter.Request(StateType.Idle, bb.CharacterSO.defaultStateData, ignoreCancelTime: true);
    }

    static AI_EnomyBrain GetBrain(Blackboard bb)
        => bb.characterRunTimeData.self != null
            ? bb.characterRunTimeData.self.GetComponent<AI_EnomyBrain>() : null;
}
