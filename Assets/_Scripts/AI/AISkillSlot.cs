using UnityEngine;

/// <summary>
/// AI 技能槽（SO 资产）：一个敌人技能 = 一行配置（触发判定 + 释放后进入的自定义状态）。
/// 多态设计：每个具体技能子类自己实现 CanUse 触发判定并持有自有字段，
/// 决策层（AI_EnomyBrain）只做通用遍历："哪个技能 CanUse 就用哪个"。
/// 施放时机：技能拥有最高优先级——**与 aiState 无关**，任意状态（Idle/巡逻/追敌/攻击/回家）
///    下只要 CanUse(ctx) 满足（含冷却与 requireTarget 门）即施放，早于一切移动决策。
/// CanUse 纯净约束：本类是 SO（被同类型所有敌人共享），CanUse 只能读配置 + ctx，
///    禁止读写任何运行时状态；冷却/施放计时等运行时数据放 AISkillRuntime。
/// </summary>
public abstract class AISkillSlot : ScriptableObject
{
    [Header("技能行为")]
    [Tooltip("释放后进入的自定义状态（BaseCustomStateData，如盾牌/飞行接近）")]
    public BaseCustomStateData customStateData;

    [Header("选择规则")]
    [Tooltip("多个技能同时可用时，分数高的优先")] public int priority = 50;
    [Tooltip("冷却（秒）")] public float cooldown = 3f;
    [Tooltip("禁止连续两次放同一个技能")] public bool noRepeat;
    [Tooltip("需要目标才可触发（自增益/回血类可关闭；无目标时 SelectSkill 直接跳过）")] public bool requireTarget = true;

    /// <summary>
    /// 统一触发判定：AI 决策层每帧调用（经由 AISkillRuntime.CanUse 统一入口）。
    /// 子类实现各自触发逻辑并持有自有字段；只读配置 + ctx，勿写运行时状态。
    /// </summary>
    public abstract bool CanUse(in AISkillContext ctx);
}

/// <summary>
/// AI 技能判定的"世界事实快照"：由 AI_EnomyBrain 每帧构建一次（零 GC），
/// 所有技能共享，避免每个技能各自重算距离/血量/朝向。
/// brain 引用供高级触发条件读取任意事实（Target / GetMoveGoal 等）。
/// </summary>
public readonly struct AISkillContext
{
    public readonly AI_EnomyBrain brain;
    public readonly bool hasTarget;
    public readonly float now;
    public readonly float distToTarget;
    public readonly float hpRatio;
    public readonly bool isTargetInFront;

    public AISkillContext(AI_EnomyBrain brain, bool hasTarget, float now, float distToTarget, float hpRatio, bool isTargetInFront)
    {
        this.brain = brain;
        this.hasTarget = hasTarget;
        this.now = now;
        this.distToTarget = distToTarget;
        this.hpRatio = hpRatio;
        this.isTargetInFront = isTargetInFront;
    }
}

/// <summary>AI 技能槽运行时状态（放 MonoBehaviour 侧：冷却等状态不能进 SO）</summary>
public class AISkillRuntime
{
    public AISkillSlot cfg;
    public float lastUsed = -999f;
    /// <summary>施放中计时（自定义状态用：如"接近到位停留"倒计时；施放开始时清零）</summary>
    public float castTimer;
    /// <summary>施放中目标点（自定义状态用：如 Boss 冲刺锁定玩家位置；SO 无运行时字段，运行时数据统一放这里）</summary>
    public Vector2 castTarget;

    public bool IsReady(float now) => now - lastUsed >= cfg.cooldown;

    /// <summary>统一判定入口：冷却就绪 && 配置触发条件成立</summary>
    public bool CanUse(in AISkillContext ctx) => IsReady(ctx.now) && cfg.CanUse(ctx);
}
