using GameFramework.Event;
using UnityEngine;

/// <summary>
/// 连击系统（2026-08-06 定稿）：
/// 攻击连段与 UI 连击数是两套独立状态：
///   - totalCombo   ：UI 总连击数，跨攻击累计，仅受击/超时归零
///   - activeAttack + chainCount ：当前激活攻击自身的连段，切招即打断（从默认段重新开始）
/// 是否使用连招 = SpecialAttack.comboStages 列表是否有数据（无需单独标志位）
/// 注意：需要初始化，需要驱动
/// </summary>
public class ComboManager
{
    Blackboard bb;

    // —— UI 总连击数（跨攻击累计）——
    public int totalCombo { get; private set; }
    float lastComboHitTime;

    // —— 当前激活攻击的连段 ——
    public SpecialAttack activeAttack { get; private set; }
    public int chainCount { get; private set; }   // 当前激活攻击自身已命中的次数

    public ComboManager(Blackboard bb)
    {
        this.bb = bb;
    }

    /// <summary>是否有连招配置（有数据即使用连招，无需标志位）</summary>
    static bool HasChain(SpecialAttack attack)
        => attack != null && attack.comboStages != null && attack.comboStages.Count > 0;

    /// <summary>每帧驱动：距上次命中超过 comboResetTime 则连段与 UI 一起归零</summary>
    public void OnUpdate()
    {
        if (totalCombo <= 0) return;
        if (Time.time - lastComboHitTime > bb.CharacterSO.comboResetTime)
            BreakCombo();
    }

    /// <summary>
    /// 按攻击键时调用：返回本次要执行的连招段（null = 用默认段）
    /// 切招：打断上一个攻击的连段（UI 不清零），新攻击从默认段开始
    /// </summary>
    public ComboStage SelectStage(SpecialAttack pressed)
    {
        if (pressed == null) return null;

        // 切招：打断上一个攻击的连段（UI 不清零）
        if (pressed != activeAttack)
        {
            activeAttack = pressed;
            chainCount = 0;               // 新攻击从默认段开始
            return null;
        }

        // 同一攻击继续连段
        if (!HasChain(pressed)) return null;   // 无连招配置 → 永远默认段
        if (chainCount <= 0) return null;
        // 上限后从头循环
        return pressed.comboStages[(chainCount - 1) % pressed.comboStages.Count];
    }

    /// <summary>
    /// 攻击命中敌人时调用：UI 总连击 +1；
    /// 命中属于当前激活攻击且有连招配置则其连段 +1
    /// </summary>
    public void RegisterHit()
    {
        totalCombo++;
        lastComboHitTime = Time.time;
        EventBus.Global.Fire(new OnComboChanged(totalCombo, bb.characterRunTimeData.self?.name));

        if (HasChain(activeAttack))
            chainCount++;
    }

    /// <summary>受击/超时调用：UI 与攻击连段都归零</summary>
    public void BreakCombo()
    {
        if (totalCombo == 0 && chainCount == 0 && activeAttack == null) return;
        totalCombo = 0;
        chainCount = 0;
        activeAttack = null;
        lastComboHitTime = 0;
        EventBus.Global.Fire(new OnComboChanged(0, bb.characterRunTimeData.self?.name));
    }
}
