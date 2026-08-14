using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
///存储运行时的数据，只有数据没有逻辑
/// </summary>
public class CharacterRunTimeData
{
    public float horizontalVelocity;
    public float verticalVelocity;
    public float currentHealth;
    public bool isground;
    public bool iswall;
    public int facingDir = 1;

    /// <summary>
    /// 是否已死亡：死亡后阻断受击/攻击等所有功能。
    /// 由 DamageReceiver 写入（单一事实源），仲裁器 / 各管理器 / 表现层读取
    /// </summary>
    public bool isDead;


    /// <summary>
    /// 是否激活攻击盒子
    /// </summary>
    public bool attackboxActive;


    public AttackBox curAttackBox;
    /// <summary>
    /// 当前激活攻击段的配置数据（类型+数值），由 AttackManager 随 curAttackBox 一起写入
    /// </summary>
    public AttackData curAttack;

    //------------  受击运行时上下文（DamageReceiver 写 / Hitstate 读）  ---------------------
    /// <summary>
    /// 归一化受击方向（本次被谁从哪个方向打中）
    /// </summary>
    public Vector2 hitDirection;
    /// <summary>
    /// 本次伤害的攻击者
    /// </summary>
    public GameObject hitAttacker;
    /// <summary>
    /// 本次伤害类型
    /// </summary>
    public DamageType hitDamageType;
    //------------  statemachine唯一写入  ---------------------
    public StateType currentstateType;
    public BaseCharacterstate currentstate;
    public StateData curStateData;
    public StateProgress stateProgress;


    /// <summary>
    /// 角色自身的游戏物体的缓存引用
    /// </summary>
    public GameObject self;
    public bool useAttackManager;

    /// <summary>
    /// 当前索敌目标（AI / 自定义状态共用；自定义状态方式A在此写入索敌结果）
    /// </summary>
    public Transform currentTarget;

    /// <summary>
    /// 自定义状态通用计时器（如到位停留计时）
    /// </summary>
    public float customStateTimer;

    //动画归一化时间：百分比动画播放进度
    public float AnimNormalizedTime;

    public CharacterRunTimeData()
    {
        facingDir = 1;
    }
}