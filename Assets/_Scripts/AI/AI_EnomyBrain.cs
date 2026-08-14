using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterControler))]
public class AI_EnomyBrain : MonoBehaviour
{
    [Tooltip("类型级 AI 配置（SO 资产，每个敌人类型一个）")]
    public AIEnemyProfileSO profile;

    [Header("巡视（实例级配置）")]
    [Tooltip("领域巡视路径点：拖入场景空物体按顺序循环；为空则不巡逻，原地待机")]
    public List<Transform> patrolPoints = new();
    [Tooltip("到达路径点的判定距离")] public float patrolArriveDistance = 0.3f;
    [Tooltip("到达路径点后停留时长")] public float patrolWaitTime = 1f;

    [Header("基地（实例级配置）")]
    [Tooltip("基地中心：拖入队长/基地空物体 transform；留空则默认自己的初始创建位置。\n" +
             "队长移动时，小兵离基地超过 fieldLength 会自动回防 → 实现小兵跟随队长")]
    public Transform baseTransform;

    // ============ 运行时状态（每个个体独立，勿放入 SO） ============
    CharacterControler controler;
    Rigidbody2D rigid;
    Vector2 homePos;                   // 初始创建位置（默认基地中心，存值勿存自身引用）
    Transform target;                  // 自索敌
    float lastAttackTime;

    enum AIState { Idle, Patrol, Chase, Attack, ReturnHome }
    AIState aiState = AIState.Idle;
    int patrolIndex;
    bool patrolWaiting;
    float patrolWaitTimer;

    // 技能表运行时拷贝（冷却记录在这里）
    protected readonly List<AISkillRuntime> skills = new();
    protected AISkillRuntime lastCast;

    void Awake()
    {
        controler = GetComponent<CharacterControler>();
        rigid = GetComponent<Rigidbody2D>();
        var ei = GetComponent<EnomyInput>();
        if (ei != null) controler.InputSource = ei;
        homePos = transform.position;   // 记录出生坐标（修复：home 勿存 transform 自身引用）

        if (profile == null) Debug.LogError("[AI_EnomyBrain] 缺少 AIEnemyProfileSO 配置！", this);
        InitSkills();
    }

    void InitSkills()
    {
        skills.Clear();
        if (profile == null || profile.skills == null) return;
        foreach (var slot in profile.skills)
            if (slot != null && slot.customStateData != null)
                skills.Add(new AISkillRuntime { cfg = slot });
    }

    // ============ 感知 → 决策 → 执行（由 EnomyInput 每帧轮询） ============
    public InputCommand ScanCheck()
    {
        InputCommand cmd = default;
        if (controler == null || profile == null || rigid == null) return cmd;

        // 1. 感知：自索敌（自动发现玩家）
        if (target == null)
        {
            Collider2D c = Physics2D.OverlapCircle(rigid.position, profile.detectRadius, profile.targetLayer);
            if (c != null)
            {
                target = c.transform;
            }
        }
        // 目标死亡则清空
        if (target != null)
        {
            var tc = target.GetComponent<CharacterControler>();
            if (tc == null || tc.blackboard.characterRunTimeData.isDead) target = null;
        }

        // 2. 世界事实
        Vector2 home = GetHomePos();
        float distToHome = Vector2.Distance(rigid.position, home);
        bool outOfField = distToHome > profile.fieldLength;
        // 玩家是否进入领地：玩家离开领地则放弃追击（修复：防止「回家」与「追踪」在领地边界来回切换）
        bool playerInField = target != null
            && Vector2.Distance(target.position, home) <= profile.fieldLength;
        // 回家锁存：一旦开始回家，须回到基地近旁（returnArriveDistance）才算完成，期间不被目标打断
        bool latchedReturn = aiState == AIState.ReturnHome
            && distToHome > profile.returnArriveDistance;
        float dist = DistToTarget();
        bool hasPatrol = patrolPoints != null && patrolPoints.Count > 0;

        // 3. 决策（Boss 未来在 BeforeDecision 里插阶段逻辑）
        BeforeDecision();
        if (outOfField || latchedReturn)                                         aiState = AIState.ReturnHome; // 出领地/回家途中：回家最高
        else if (target != null && playerInField && dist <= profile.attackRange) aiState = AIState.Attack;
        else if (target != null && playerInField)                                aiState = AIState.Chase;
        else if (hasPatrol)                                                      aiState = AIState.Patrol;    // 巡逻优先级高于 home 待机
        else                                                                     aiState = AIState.Idle;

        // 4. 执行
        // 4.0 全局技能步骤：技能拥有最高优先级，与 aiState 无关——任意状态（Idle/巡逻/追敌/攻击/回家）
        //     下只要满足 CanUse 即施放，早于一切移动决策；施放后本帧不再输出移动输入（技能态 Custom 接管位移）。
        //     防重复：IsCastingSkill 判定 Custom 态 / 技能入队在途（SkillPending），在途即跳过。
        if (!IsCastingSkill())
        {
            var slot = SelectSkill();
            if (slot != null && controler.CanUseSkill(slot.cfg.customStateData))
            {
                CastSkill(slot);
                return cmd;
            }
        }

        switch (aiState)
        {
            case AIState.ReturnHome: MoveTo(GetHomePos(),ref cmd); break;
            case AIState.Patrol:     PatrolStep(ref cmd); break;
            case AIState.Chase:      MoveTo(target.position,ref cmd); UpdateCombat(ref cmd); break;
            case AIState.Attack:     FaceTo(target.position,ref cmd); UpdateCombat(ref cmd); break;
            case AIState.Idle:
            default: break;
        }

        return cmd;
    }

    // ============ 领域巡视 ============
    void PatrolStep(ref InputCommand cmd)
    {
        if (patrolPoints == null || patrolPoints.Count == 0) return;
        Transform wp = patrolPoints[patrolIndex];
        if (wp == null) { AdvancePatrol(); return; }

        if (Vector2.Distance(rigid.position, wp.position) <= patrolArriveDistance)
        {
            if (!patrolWaiting)
            {
                // Debug.Log("patrolWaiting");
                patrolWaiting = true;
                patrolWaitTimer = 0f;
            }
            patrolWaitTimer += Time.fixedDeltaTime;
            if (patrolWaitTimer >= patrolWaitTime)
            {
                // Debug.Log("patrol moveNext");
                patrolWaiting = false;
                AdvancePatrol();
            }
        }
        else
        {
            // Debug.Log("patrol move");
            patrolWaiting = false;
            MoveTo(wp.position,ref cmd);
        }
    }

    void AdvancePatrol()
    {
        if (patrolPoints == null || patrolPoints.Count == 0) return;
        patrolIndex = (patrolIndex + 1) % patrolPoints.Count;
    }

    // Boss 阶段检测的扩展点（子类 override）
    protected virtual void BeforeDecision() { }

    // ============ 战斗：近身普攻（连招 Attack1→Attack2）；技能已由 ScanCheck 的全局技能步骤处理 ============
    protected virtual void UpdateCombat(ref InputCommand cmd)
    {
        if (IsCasting()) return;        // 技能施放/不可打断中，等待（技能优先，普攻让路）

        // Debug.Log($"DistToTarget: {DistToTarget()} , stopDistance: {profile.attackStopDistance}");
        // 近身普攻（走 AttackManager / ComboManager 连招）
        // 出手条件用 attackStopDistance（贴身命中距离），避免在 attackRange 内但未贴近时就挥空
        if (target != null && DistToTarget() <= profile.attackStopDistance
            && Time.time - lastAttackTime >= profile.attackInterval)
        {
            cmd.inAattack = true;
            lastAttackTime = Time.time;
        }
    }

    // ============ 技能表 ============
    protected bool IsCasting()
    {
        var bb = controler.blackboard;
        if (bb.characterRunTimeData.isDead) return true;
        return bb.characterRunTimeData.currentstateType == StateType.Custom
            || !controler.arbiter.CanCancelCurrentState();
    }

    /// <summary>
    /// 是否正在施放技能：Custom 态 或 技能入队在途（SkillPending）。
    /// 与 IsCasting 的区别：IsCasting 把"当前状态不可打断"也算施放（普攻门）；
    /// 本方法只关心"技能是否已在执行/在途"，供全局技能步骤防重复施放——
    /// UseSkill 是异步入队（SkillManager 下一帧才真正切 Custom），入队期间须锁存。
    /// 自愈：申请成功进 Custom 态 / 申请失败清空后 SkillPending 自动 false，无需手动解锁。
    /// </summary>
    protected bool IsCastingSkill()
    {
        var bb = controler.blackboard;
        if (bb.characterRunTimeData.currentstateType == StateType.Custom) return true;
        return controler.SkillPending;
    }

    protected AISkillRuntime SelectSkill()
    {
        AISkillContext ctx = BuildContext();   // 每帧只构建一次世界事实快照
        AISkillRuntime best = null;
        foreach (var rt in skills)
        {
            if (rt.cfg.noRepeat && rt == lastCast) continue;
            if (rt.cfg.requireTarget && !ctx.hasTarget) continue;   // 需要目标但没有 → 跳过（防无目标误触发）
            if (!rt.CanUse(ctx)) continue;     // 统一判定入口：冷却 + 技能自身触发条件
            if (best == null || rt.cfg.priority > best.cfg.priority) best = rt;
        }
        return best;
    }

    protected void CastSkill(AISkillRuntime rt)
    {
        controler.UseSkill(rt.cfg.customStateData);
        rt.lastUsed = Time.time;
        lastCast = rt;
    }

    // ===== 外部只读访问器：AI 决策层是索敌/目标/施放状态的唯一事实源（自定义状态只读，不重复维护） =====
    /// <summary>当前索敌目标（AI 已维护其发现与死亡清空）</summary>
    public Transform Target => target;

    /// <summary>是否有索敌目标（技能条件可用；requireTarget 门由 SelectSkill 统一处理）</summary>
    public bool HasTarget => target != null;

    /// <summary>
    /// 当前决策是否在"追敌/攻击玩家"（aiState 为 Chase/Attack）。
    /// 供接近类技能（如 Flyeye）限定目标场景，防止巡逻/回家等状态下误触发并卡死。
    /// </summary>
    public bool IsApproachingPlayer() => aiState is AIState.Chase or AIState.Attack;

    /// <summary>
    /// 当前决策是否在"巡逻"（aiState 为 Patrol）。
    /// 供飞行接近类技能（如 Flyeye）在巡逻时飞往空中/地面巡逻路点；无目标也可触发。
    /// </summary>
    public bool IsPatrolling() => aiState == AIState.Patrol;

    /// <summary>当前施放的技能运行时（自定义状态经此读写 castTimer 等施放数据）</summary>
    public AISkillRuntime CurrentCast => lastCast;

    /// <summary>
    /// 供自定义移动技能（如 Flyeye 接近）读取：当前应飞往的位置（null=原地悬停）。
    /// 决策/优先级完全沿用 ScanCheck 算出的 aiState（回家 > 追敌 > 巡逻 > 悬停），
    /// 本决策层是唯一事实源，技能只负责朝该位置做飞行执行、不重复决策。
    /// </summary>
    public Vector2? GetMoveGoal()
    {
        switch (aiState)
        {
            case AIState.ReturnHome:
                return GetHomePos();
            case AIState.Chase:
            case AIState.Attack:
                return target != null ? (Vector2?)target.position : null;
            case AIState.Patrol:
                return patrolPoints != null && patrolPoints.Count > 0
                       && patrolIndex >= 0 && patrolIndex < patrolPoints.Count && patrolPoints[patrolIndex] != null
                    ? (Vector2?)patrolPoints[patrolIndex].position : null;
            default:
                return null;   // Idle：原地悬停
        }
    }

    // ============ 工具 ============
    /// <summary>基地中心：拖入队长/基地物体则以其当前位置为中心，否则用初始创建位置</summary>
    Vector2 GetHomePos() => baseTransform != null ? (Vector2)baseTransform.position : homePos;

    /// <summary>构建技能判定用的世界事实快照（每帧一次，供所有技能共享）</summary>
    AISkillContext BuildContext()
        => new AISkillContext(this, target != null, Time.time, DistToTarget(), HpRatio(), IsTargetInFront());

    float DistToTarget() => target != null ? Vector2.Distance(rigid.position, target.position) : float.MaxValue;

    float HpRatio()
    {
        var bb = controler.blackboard;
        return bb.characterRunTimeData.currentHealth / Mathf.Max(1f, bb.CharacterSO.maxHealth);
    }

    bool IsTargetInFront()
    {
        if (target == null) return false;
        int dir = controler.blackboard.characterRunTimeData.facingDir;
        return (target.position.x - transform.position.x) * dir > 0f;
    }

    void MoveTo(Vector3 pos,ref InputCommand cmd)
    {
        float dx = pos.x - transform.position.x;
        if (Mathf.Abs(dx) > 0.1f) { if (dx > 0f) cmd.inRight = true; else cmd.inLeft = true; }
    }

    /// <summary>
    /// 面向目标：已贴近目标（attackStopDistance 内）则站定（不再输出水平输入，保证能停下出招），
    /// 否则持续走向目标并顺带转向。
    /// 修复：原实现每帧无条件输出左右移动，导致敌人贴近玩家后左右晃动、且状态被反复重入
    /// （Flip 重入 Walk 使 updateStartTime 归零 → CanCancelCurrentState 恒 false → 攻击永远无法触发）。
    /// </summary>
    void FaceTo(Vector3 pos, ref InputCommand cmd)
    {
        float dx = pos.x - transform.position.x;
        if (Mathf.Abs(dx) <= profile.attackStopDistance) return;   // 已贴身 → 站定
        if (dx > 0f) cmd.inRight = true; else cmd.inLeft = true;
    }
}

