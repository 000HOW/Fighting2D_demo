using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 每个特殊技的配置数据
/// </summary>
[CreateAssetMenu(fileName ="SpecialAttack",menuName ="PlayerControler/SpecialAttack",order =0)]
public class SpecialAttack : ScriptableObject
{
    //检查时间窗口
    public float checkWindowTime;
    //期间需要的按键
    public List<ECommandType> commandList;
    //默认段：本招连击数为0时使用的攻击数据
    public StateData attackData;
    public SwitchStateType switchType;
    public AttackBox attackBox;
    //默认段：本段攻击的类型与数值（每段独立配置，与范围盒子解耦）
    public AttackData attack;

    [Header("=== 派生连招（本攻击自身连段）===")]
    // 此列表有数据即启用连招：comboStages[i] 对应「本招连击数 = i+1」时按攻击键要播放的动作
    public List<ComboStage> comboStages;
}

/// <summary>
/// 连招派生段：命中后下一段要切换到的攻击数据
/// </summary>
[System.Serializable]
public class ComboStage
{
    public StateData attackData;
    public AttackBox attackBox;
    public SwitchStateType switchType;
    //本段攻击的类型与数值（每段独立配置，与范围盒子解耦）
    public AttackData attack;
}
