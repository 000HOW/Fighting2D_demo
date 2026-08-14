using UnityEngine;

/// <summary>
/// Boss 被击晕（Custom 技能状态）：眩晕动画 + 定身。
///  - 触发：BossStunSkillSlot（HP 落在 [stunMinHp, stunMaxHp] 区间）
///  - 退出：HP 超出区间（血量恢复/变化）或超时（stunDuration 兜底）→ 回 Idle
/// 无 SO 运行时字段：施放计时走 brain.CurrentCast.castTimer。
/// </summary>
[CreateAssetMenu(fileName = "BossStunStateData", menuName = "Enemy/BossStunStateData")]
public class BossStunStateData : BaseCustomStateData
{
    [Header("眩晕区间")]
    [Tooltip("HP 比例低于该值退出眩晕")] public float stunMinHp = 0f;
    [Tooltip("HP 比例高于该值退出眩晕")] public float stunMaxHp = 0.3f;
    [Tooltip("眩晕最长时长（秒，防无限）")] public float stunDuration = 2f;

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

        var blackboard = rt.self.GetComponent<CharacterControler>().blackboard;
        float hpRatio = blackboard.characterRunTimeData.currentHealth / Mathf.Max(1f, blackboard.CharacterSO.maxHealth);

        // 超区间（HP 变化）或超时 → 退出
        if (hpRatio > stunMaxHp || hpRatio < stunMinHp || cur.castTimer >= stunDuration)
        {
            Drop(bb);
            return;
        }
        cur.castTimer += Time.fixedDeltaTime;
    }

    public override void UpdatePhysics(Blackboard bb, float deltaTime)
    {
        // 眩晕定身：水平阻尼到 0，保留重力
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
