using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 状态数据：纯数据可序列化类型
/// 内嵌在宿主 SO（转移表 / CharacterSO / HitInfo / SpecialAttack / BaseCustomStateData.animData）中。
/// </summary>
[System.Serializable]
public class StateData
{
    //总的动作时间
    public AnimationClip startClip;
    public float startClipSpeed;
    public MotionSettings startMotion;
    public float startMotionTime;
    [Space(15)]
    public AnimationClip mainClip;
    public MotionSettings mainMotion;
    public bool loop = true;
    /// <summary>
    /// 主循环时长，为0则默认动画实际播放长度
    /// </summary>
    public float fixedMainCycleDuration = 0f; 
    public float mainClipSpeed = 1;
    [Space(15)]
    public AnimationClip endClip;
    public float endClipSpeed;
    public MotionSettings endMotion;
    public float endMotionTime;
    [Space(20)]
    public bool SetDirStart = true;
    public bool ignoreCancelTime = false;
    public bool UseAttackManager = true;
    [Header("=== 打断与取消 ===")]
    //取消帧
    public float canceltime;
    // 当前状态的「最低打断优先级」
    // 比如某些霸体状态设一个很高的值，只有更高优先级才能打断
    public int MinInterruptPriority;

    [Header("=== 状态特效（可选，为空则不播）===")]
    public List<FXConfigSO> fxConfigs;

    // 辅助方法：获取主循环时长（优先使用固定值，否则取动画时长）
    public float GetMainCycleDuration()
    {
        if (fixedMainCycleDuration > 0) return fixedMainCycleDuration;
        else
        {
            if (mainClipSpeed==0)
            return 0;
            else
            {
                if(mainClip==null) return 0;
                else return mainClip.length/mainClipSpeed;
            }
        }
    }

}


[System.Serializable]
public class MotionSettings
{
    [Header("基础极值")]
    public Vector2 maxVelocity;          // 该状态的理论最大速度
    /// <summary>
    /// 为false，基础极值的0分量舍去使用原速度分量
    /// </summary>
    [Header("使用覆盖式的基础极值")]
    public bool useXCoverageWeight = true;
    public bool useYCoverageWeight = true;
    public float gravityScale = 1f;      // 下落速度倍率
    //立即设置为目标速度，不过度
    public bool RespondImmediately = false;
    //速度变化加速度
    public float acceleration = 10;
    [Header("倍乘速度变化曲线")]
    public AnimationCurve XspeedMultiplier = AnimationCurve.Linear(0, 1, 1, 1);
    public AnimationCurve YspeedMultiplier = AnimationCurve.Linear(0, 1, 1, 1);

}

