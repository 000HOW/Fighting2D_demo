using GameFramework.Event;
using UnityEngine;

/// <summary>
/// 龙战士 Boss 大脑（AI_EnomyBrain 子类）：
///  - Phase 阶段制：BeforeDecision 按 HP 阈值算 Phase（技能槽经 CanUse 里 ctx.brain is BossBrain 门解锁）
///  - 保持距离：重写 UpdateCombat——近距/受击后后撤、远距接近、中距站定放技能
///  - 受击后短暂后撤：订阅 EventBus.Global OnHitTaken（Target==self 过滤）
///  - 胜利：演出节点手动调 TriggerVictory（先停 AI 再 UseSkill 胜利状态）
/// </summary>
public class BossBrain : AI_EnomyBrain
{
    [Header("阶段")]
    [Tooltip("HP 比例 ≤ 该值进入 Phase 2（解锁大型火球等）")] public float phase2HpRatio = 0.5f;

    [Header("保持距离")]
    [Tooltip("与玩家距离小于该值则后撤")] public float retreatDistance = 1.5f;
    [Tooltip("与玩家距离大于该值则接近")] public float approachDistance = 3.5f;
    [Tooltip("受击后该秒数内提高后撤意愿")] public float retreatAfterHitWindow = 1.5f;

    [Header("近身普攻")]
    [Tooltip("两次近身出手最小间隔（飞踢走普通攻击通道）")] public float bossAttackInterval = 1.0f;

    [Header("胜利")]
    [Tooltip("胜利状态资产（外部 TriggerVictory 时施放）")] public BaseCustomStateData victoryState;

    /// <summary>当前阶段（1/2；技能槽在 CanUse 里读此解锁）</summary>
    public int Phase { get; private set; } = 1;

    /// <summary>当前 HP 比例（基类 HpRatio 是 private，子类自算）</summary>
    public float CurrentHpRatio
    {
        get
        {
            var bb = GetComponent<CharacterControler>().blackboard;
            return bb.characterRunTimeData.currentHealth / Mathf.Max(1f, bb.CharacterSO.maxHealth);
        }
    }

    float lastHitTime = float.MinValue;
    float bossLastAttackTime = float.MinValue;

    protected override void BeforeDecision()
    {
        Phase = CurrentHpRatio > phase2HpRatio ? 1 : 2;
    }

    void OnEnable()
    {
        EventBus.Global.Subscribe<OnHitTaken>(OnBossHit);
    }

    void OnDisable()
    {
        EventBus.Global.Unsubscribe<OnHitTaken>(OnBossHit);
    }

    void OnBossHit(OnHitTaken hit)
    {
        // 只关心自己受击（OnHitTaken 发在全局总线，含受害者引用 Target）
        if (hit.Target == gameObject) lastHitTime = Time.time;
    }

    protected override void UpdateCombat(ref InputCommand cmd)
    {
        if (IsCasting()) return;          // 技能施放/不可打断中，普攻与走位让路
        if (Target == null || profile == null) return;

        float dist = Vector2.Distance(transform.position, Target.position);
        bool wantRetreat = dist < retreatDistance
            || (Time.time - lastHitTime) < retreatAfterHitWindow;

        // 走位策略：UpdateCombat 在 MoveTo/FaceTo 之后执行，可覆盖移动输入
        cmd.inLeft = false;
        cmd.inRight = false;
        if (wantRetreat)
        {
            // 反向走位：远离玩家
            if (Target.position.x > transform.position.x) cmd.inLeft = true;
            else cmd.inRight = true;
        }
        else if (dist > approachDistance)
        {
            // 远距：接近玩家
            if (Target.position.x > transform.position.x) cmd.inRight = true;
            else cmd.inLeft = true;
        }
        // 中距：站定（技能由全局技能步骤择放：火球/大型火球/冲刺）

        // 近身出手：飞踢（inAattack）走普通攻击通道（AttackManager 识别 SpecialAttack commandList=[Attack]）
        if (dist <= profile.attackStopDistance && Time.time - bossLastAttackTime >= bossAttackInterval)
        {
            cmd.inAattack = true;
            bossLastAttackTime = Time.time;
        }
    }

    /// <summary>胜利演出：停 AI 并施放胜利状态（演出节点手动调用；先停 AI 再 UseSkill）</summary>
    public void TriggerVictory()
    {
        enabled = false;
        var controler = GetComponent<CharacterControler>();
        if (victoryState != null) controler.UseSkill(victoryState);
    }
}
