using System;
using Unity.Mathematics;
using UnityEngine;
/// <summary>
/// 角色状态基类
/// </summary>
public abstract class BaseCharacterstate
{
    public StateType stateType{get;protected set;}
    public StateData stateData;
    public float entryStartTime{get;private set;}
    public float exitStartTime{get;private set;}
    public float updateStartTime{get;private set;}
    // MainLoop 状态特效的生成计时（进入状态时清零）
    float fxAccumulator;
    // 可选钩子：进入/退出阶段开始时只调用一次，方便做一次性初始化/清理
    /// <summary>
    /// 状态进入时调用一次
    /// </summary>
    /// <param name="bb"></param>
    public virtual void OnEntryStart(Blackboard bb)
    {
        if (stateData==null)
        {
            Debug.LogError($"no stateData in {stateType} !!!");
            return;
        }
        entryStartTime = Time.time;
        updateStartTime = 0;

        if (stateData.SetDirStart)
        {
            int Setdir = bb.characterRunTimeData.facingDir;
            if (bb.inputData.cur_inputComand.inLeft)
            Setdir = -1;
            if (bb.inputData.cur_inputComand.inRight)
            Setdir = 1;
            bb.characterRunTimeData.facingDir = Setdir;
        }

        // Debug.Log("OnEntryStart: "+stateType);
        bb.characterRunTimeData.useAttackManager = stateData.UseAttackManager;

        bb.eventBus.Fire(new StateEntryStart());

        fxAccumulator = 0f;
        SpawnStateFX(bb, StateFXTiming.OnEntryOnce);
    }
    /// <summary>
    /// 状态退出时调用一次
    /// </summary>
    /// <param name="bb"></param>
    public virtual void OnExitStart(Blackboard bb)
    {
        exitStartTime = Time.time;
        bb.eventBus.Fire(new StateExitStart());
        SpawnStateFX(bb, StateFXTiming.OnExitOnce);
        // Debug.Log("OnExitStart: "+stateType);
    }
    /// <summary>
    /// 状态进入时每帧调用:默认时长为状态数据里的startMotionTime
    /// </summary>
    /// <param name="bb"></param>
    /// <returns>为true退出当前进入OnUpdate</returns>
    public virtual bool OnEntry(Blackboard bb)
    {
        if (entryStartTime + stateData.startMotionTime < Time.time)
        {
            updateStartTime = Time.time;
            bb.eventBus.Fire(new StateEntryEnd());
            return true;
        }
        else return false;
    }
    /// <summary>
    /// 状态退出时每帧调用：默认时长为状态数据里的endMotionTime
    /// </summary>
    /// <param name="bb"></param>
    /// <returns>为true退出当前进入下一个状态的OnEntry</returns>
    public virtual bool OnExit(Blackboard bb)
    {

        if (exitStartTime + stateData.endMotionTime < Time.time)
        {
            bb.eventBus.Fire(new StateExitEnd());
            return true;
        }
        else return false;
    }
    /// <summary>
    /// 状态主进程每帧调用
    /// </summary>
    /// <param name="bb"></param>
    public virtual void OnUpdate(Blackboard bb)
    {
        SpawnStateFX(bb, StateFXTiming.MainLoop);
        // Debug.Log($"{bb.playerRunTimeData.self.name} {stateType} state OnUpdate:");
    }

    /// <summary>
    /// 按状态配置驱动特效：OnEntryOnce / MainLoop / OnExitOnce 三种时机
    /// </summary>
    void SpawnStateFX(Blackboard bb, StateFXTiming timing)
    {
        if (stateData == null || stateData.fxConfigs == null) return;
        Transform self = bb.characterRunTimeData.self?.transform;
        if (self == null) return;
        int dir = bb.characterRunTimeData.facingDir;

        foreach (var cfg in stateData.fxConfigs)
        {
            if (cfg == null || cfg.fxType == FXType.None || cfg.timing != timing) continue;

            Vector3 pos = cfg.GetWorldPos(self.position, dir);

            if (timing == StateFXTiming.MainLoop)
            {
                fxAccumulator += Time.fixedDeltaTime;
                if (fxAccumulator < cfg.spawnInterval) continue;   // 不到间隔不生成
                fxAccumulator = 0f;
            }
            FXManager.Instance?.PlayFX(cfg, pos, bb.characterRunTimeData.facingDir);
        }
    }


    

    // ----- 物理计算核心（由外部在 FixedUpdate 中调用） -----
    public virtual void UpdatePhysics(Blackboard bb, float deltaTime)
    {
        if (stateData == null) return;
        
        Vector2 FinalVel = Vector2.zero;

        StateProgress progress = bb.characterRunTimeData.stateProgress;
        // Debug.Log($"progress: {progress}, isExiting: {isExiting}");
        MotionSettings motion = GetMotionForProgress(progress);
        // 1. 获取当前进度和对应的配置
        float normalizedTime = GetCurrentPhaseNormalizedTime(progress);

        // Debug.Log($"normalizetTime: {normalizedTime}");
        // 2. 采样曲线
        float XspeedMul = motion.XspeedMultiplier.Evaluate(normalizedTime);
        float YspeedMul = motion.YspeedMultiplier.Evaluate(normalizedTime);

        // 3. 读取当前速度
        Vector2 currentVel = new Vector2(bb.characterRunTimeData.horizontalVelocity, bb.characterRunTimeData.verticalVelocity);




        float newVelX = 0;
        float newVelY = 0;

        // 1. 水平速度：使用 MoveTowards 制动（仅限X轴）
        float targetVelX = 0;
        if (!motion.useXCoverageWeight)
        {
            targetVelX  = (motion.maxVelocity.x == 0) 
                ? currentVel.x 
                : motion.maxVelocity.x * bb.characterRunTimeData.facingDir * XspeedMul;
        }
        else
        targetVelX = motion.maxVelocity.x * bb.characterRunTimeData.facingDir * XspeedMul;

        if (motion.maxVelocity.x != 0)
        targetVelX *= bb.modificationManager.FinalModificationMultiplier(ModifyValueType.MoveSpeed);

        if (!motion.RespondImmediately)
        newVelX = Mathf.MoveTowards(currentVel.x, targetVelX, motion.acceleration * deltaTime);
        else newVelX = targetVelX;




        // 2. 垂直速度：完全手动累加（不受水平制动影响）
        newVelY = currentVel.y;

        // 如果你需要垂直速度也有上限（比如下落终端速度），单独用 Mathf.MoveTowards 处理
        float targetVelY = 0;
        if (!motion.useYCoverageWeight)
        {
            targetVelY = (motion.maxVelocity.y == 0) 
                ? currentVel.y   // 保持原样，让重力接管
                : motion.maxVelocity.y * YspeedMul; // 限制跳跃/上升速度

            // 3. 最后加上重力（永远最后一步）
            float gravity = bb.CharacterSO.gravity * motion.gravityScale;
            targetVelY -= gravity * deltaTime;
        }
        else
        targetVelY = motion.maxVelocity.y * YspeedMul;


        // 垂直制动（仅限Y轴），避免与重力冲突
        if (!motion.RespondImmediately)
        newVelY = Mathf.MoveTowards(newVelY, targetVelY, motion.acceleration * deltaTime);
        else newVelY = targetVelY;




        // 4. 写入黑板
        bb.readytoApply.exp_horizontalVelocity = newVelX;
        bb.readytoApply.exp_VerticalVelocity = newVelY;

    }

    // private Vector2 CurveProcessing(Blackboard bb, float deltaTime,StateProgress progress,MotionSettings motion)
    // {
        
    //     return FinalVel;
    // }

    /// <summary>
    /// 根据当前状态进度获取对应motion配置
    /// </summary>
    /// <param name="progress"></param>
    /// <returns></returns>
    protected MotionSettings GetMotionForProgress(StateProgress progress)
    {
        if (stateData.mainMotion==null)
        {
            Debug.LogError("stateData no MotionSetting in MainMotion !!!");
            return null;
        }
        return progress switch
        {
            StateProgress.Entry => stateData.startMotion ?? stateData.mainMotion,
            StateProgress.Exit => stateData.endMotion ?? stateData.mainMotion,
            _ => stateData.mainMotion
        };
    }    
    /// <summary>
    /// 获取当前状态进度的动作归一化时间
    /// </summary>
    /// <param name="progress"></param>
    /// <returns></returns>
    protected float GetCurrentPhaseNormalizedTime(StateProgress progress)
    {
        float elapsed = Time.time - entryStartTime;

        switch (progress)
        {
            case StateProgress.Entry:
                return Mathf.Clamp01(stateData.startMotionTime==0 ? 0 : elapsed / stateData.startMotionTime);

            case StateProgress.Exit:
                float exitElapsed = Time.time - exitStartTime;
                return Mathf.Clamp01(stateData.endMotionTime==0 ? 0 : exitElapsed / stateData.endMotionTime);

            case StateProgress.Main:
            default:
                // 进入 Main 阶段后经过的时间
                float mainElapsed = elapsed - stateData.startMotionTime;
                float cycleDuration = stateData.GetMainCycleDuration();
                
                if (cycleDuration <= 0) return 0;
                
                // 关键：取模运算，让 normalizedTime 在 0~1 之间循环
                float normalized = mainElapsed % cycleDuration / cycleDuration;
                return Mathf.Clamp01(normalized);
        }
    }
}

public enum StateType
{
    None,
    Idle,
    Walk,
    Run,
    Dash,
    Up,
    Fall,
    attack,
    Hit,
    Air,
    Custom,
    Death
}

/// <summary>
/// 每个状态的进程
/// </summary>
public enum StateProgress
{
    Entry,
    Main,
    Exit
}