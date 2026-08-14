using UnityEngine;

/// <summary>
/// 盾牌自定义状态：举盾减伤，并自管"收手"（防御姿态）逻辑。
/// 收手规则（替代原 AI 决策层 UpdateStance/DropStance）：
///   - 目标死亡/丢失、不在正前方、或脱离 engageRange → 立即收手
///   - 目标持续在正面且 engageRange 内停留超 holdDuration → 收手进入攻击窗口
/// 收手即请求回 Idle，让通用 AI 决策层接管；收手后重计冷却（再举盾间隔）。
/// 无 SO 运行时字段：施放计时/冷却重置经 brain.CurrentCast（AISkillRuntime）读写，可安全共享。
/// </summary>
[CreateAssetMenu(fileName = "ShieldStateData", menuName = "Enemy/ShieldStateData")]
public class ShieldStateData : BaseCustomStateData
{
    [Header("格挡")]
    [Tooltip("0 = 完全格挡，0.5 = 减伤 50%")] public float damageReduceMultiplier = 0f;
    [Tooltip("减伤持续时长（建议 >= 举盾停留时长）")] public float blockDuration = 4f;

    [Header("防御姿态（收手规则）")]
    [Tooltip("目标持续在正面且 engageRange 内停留超过该秒数则收手进入攻击窗口（原 AIEnemyProfile.stanceTime）")]
    public float holdDuration = 2f;
    [Tooltip("目标离开该距离（或不在正前方 / 目标死亡）则立即收手（原 AIEnemyProfile.useRange）")]
    public float engageRange = 5f;

    public override void OnEntryStart(Blackboard bb)
    {
        // 施放计时清零（记录在本技能行的 AISkillRuntime 上）
        var cur = GetBrain(bb)?.CurrentCast;
        if (cur != null) cur.castTimer = 0f;

        // DamageReceiver 已读取 DamageReduction，此处加格挡修改器
        if (bb.characterRunTimeData.self.TryGetComponent<IModifier>(out var mod))
            mod.AddModifier(new ModifierData
            {
                valueype = ModifyValueType.DamageReduction,
                multiplier = damageReduceMultiplier,
                duration = blockDuration
            });
    }

    public override void OnExitStart(Blackboard bb)
    {
        // 收手重计冷却（再举盾间隔）：替代原 AI 决策层的 DropStance
        var cur = GetBrain(bb)?.CurrentCast;
        if (cur != null) cur.lastUsed = Time.time;
    }

    public override void OnEntry(Blackboard bb) { }
    public override void OnExit(Blackboard bb) { }

    public override void OnUpdate(Blackboard bb)
    {
        var rt = bb.characterRunTimeData;
        if (rt.isDead || rt.self == null) return;

        var brain = GetBrain(bb);
        var cur = brain?.CurrentCast;
        if (brain == null || cur == null) return;

        // 收手条件：目标死亡/丢失、不在正前方、脱离 engageRange → 立即收手
        bool engaged = brain.Target != null
            && Vector2.Distance(rt.self.transform.position, brain.Target.position) <= engageRange
            && ((brain.Target.position.x - rt.self.transform.position.x) * rt.facingDir > 0f);

        if (!engaged)
        {
            Drop(bb);
            return;
        }

        // 目标持续在正面停留超 holdDuration → 收手进入攻击窗口
        cur.castTimer += Time.fixedDeltaTime;
        if (cur.castTimer >= holdDuration) Drop(bb);
    }

    public override void UpdatePhysics(Blackboard bb, float deltaTime)
    {
        // 举盾定身：水平阻尼到 0，保留重力
        bb.readytoApply.exp_horizontalVelocity = Mathf.MoveTowards(
            bb.characterRunTimeData.horizontalVelocity, 0f, 20f * deltaTime);
        bb.readytoApply.exp_VerticalVelocity = bb.characterRunTimeData.verticalVelocity;
    }

    /// <summary>收手：请求回 Idle，让通用 AI 决策层进入攻击窗口</summary>
    void Drop(Blackboard bb)
    {
        bb.characterRunTimeData.self.GetComponent<CharacterControler>()
            .arbiter.Request(StateType.Idle, bb.CharacterSO.defaultStateData, ignoreCancelTime: true);
    }

    static AI_EnomyBrain GetBrain(Blackboard bb)
        => bb.characterRunTimeData.self != null
            ? bb.characterRunTimeData.self.GetComponent<AI_EnomyBrain>() : null;
}
