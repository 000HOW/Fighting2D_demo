using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
[CreateAssetMenu(fileName ="CharacterSO",menuName ="PlayerControler/CharacterSO")]
public class CharacterSO : ScriptableObject
{
    //重力加速度
    public float gravity;
    public int maxQueueSize;
    public float maxHealth;
    public GroundCheckBox groundCheckBox;
    public StateData defaultStateData;
    public StateType defaultStateType;
    /// <summary>
    /// 输入指令缓存窗口时间:
    /// 从现在到以前的时间里
    /// </summary>
    public float InputWindowTime = 1.5f;
    //条件转移表
    public List<TransitionTable> transitionTables;
    //攻击配置表
    public List<SpecialAttack> specialAttacks;
    public List<HitInfo> hitInfos;

    [Header("死亡")]
    public StateData deathStateData;   // 死亡状态资产（倒地动画等），未配置则只阻断功能、不进死亡状态

    [Header("命中特效")]
    public FXConfigSO hitImpactFX;

    [Header("连击系统")]
    // 距上次命中超过该时间，攻击连段与 UI 连击数一起归零
    public float comboResetTime = 1.5f;
}




