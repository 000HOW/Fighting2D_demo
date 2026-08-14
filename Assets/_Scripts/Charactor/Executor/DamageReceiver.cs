using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GameFramework.Event;
using UnityEngine;
/// <summary>
/// 伤害接受器
/// 注意：需要驱动
/// </summary>
public class DamageReceiver
{
    
    Blackboard blackboard;
    StateTransitionArbiter arbiter;
    ModificationManager modificationManager;
    int maxQueueSize;
    float currentHealth;
    Queue<DamageData> damageQueue = new();
    Dictionary<DamageType,StateData> HitMap = new();
    Dictionary<DamageData,float> damageInfo = new();
    /// <summary>
    /// 跨场景恢复生命值（由 CharacterControler.RestoreRuntime 调用）。
    /// 直接覆盖内部 currentHealth，防止被 OnUpdate 回写覆盖。
    /// </summary>
    public void RestoreHealth(float hp)
    {
        currentHealth = Mathf.Clamp(hp, 0f, blackboard.CharacterSO.maxHealth);
    }

    public void Initialize()
    {
        foreach(var hit in blackboard.CharacterSO.hitInfos)
        HitMap[hit.damageType] = hit.stateData;
    }

    public DamageReceiver(Blackboard bb,StateTransitionArbiter _arbiter,ModificationManager modification)
    {
        blackboard = bb;
        arbiter = _arbiter;
        modificationManager = modification;
        maxQueueSize = bb.CharacterSO.maxQueueSize;
        currentHealth = bb.CharacterSO.maxHealth;

    }

    public bool TakeDamage(DamageData damage)
    {
        // 已死亡：不再接收任何伤害（攻击扫描最终也走这里，一并拦截），返回 false 表示未命中
        if (blackboard.characterRunTimeData.isDead) return false;

        if (damageQueue.Count>maxQueueSize)
        damageQueue.Dequeue();

        damageQueue.Enqueue(damage);
        return true;
    }
    public void OnUpdate()
    {
        CalculateDamage();
        blackboard.characterRunTimeData.currentHealth = currentHealth;
    }
    void CalculateDamage()
    {
         // 没数据就跳过，节省性能
        if (damageQueue.Count == 0) return;

        // 如果已经死了，清空队列不再处理
        if (currentHealth <= 0)
        {
            damageQueue.Clear();
            return;
        }        

        damageInfo.Clear();

        // 1. 出队并累加本帧总伤害
        float totalDamage = 0f;
        
        while (damageQueue.Count > 0)
        {
            DamageData data = damageQueue.Dequeue();

            // 二次保险：如果攻击者被销毁了，跳过这个伤害
            if (data.Attacker == null) continue;

            // 2. 开始计算当前这条伤害的最终值
            float finalDamage = data.baseValue;

            finalDamage *= modificationManager.FinalModificationMultiplier(ModifyValueType.DamageReduction);

            // 4. 防止伤害变成负数（回血），取最大值0
            finalDamage = Mathf.Max(0, finalDamage);

            damageInfo[data] = finalDamage;
            
            // 5. 累加到本帧总伤害
            totalDamage += finalDamage;
            
        }

        // 6. 本帧所有伤害计算完毕，统一扣血
        if (totalDamage > 0)
        {
            currentHealth -= totalDamage;

            // 受击：UI 总连击与攻击连段都归零
            blackboard.comboManager?.BreakCombo();

            // 触发受伤事件（供UI血条、摄像机震动等监听）
            EventBus.Global.Fire(new OnHpChange(Mathf.Clamp01(currentHealth/blackboard.CharacterSO.maxHealth),blackboard.characterRunTimeData.self.name));
            
            // 7. 检查死亡
            if (currentHealth <= 0)
            {
                currentHealth = 0;

                // 从本帧伤害里取一个有效攻击者作为"凶手"
                GameObject killer = null;
                foreach (var kvp in damageInfo)
                {
                    if (kvp.Value > 0)
                    {
                        killer = kvp.Key.Attacker;
                        break;
                    }
                }

                // 标记死亡（单一事实源：阻断受击/攻击/条件转移等所有后续功能）
                blackboard.characterRunTimeData.isDead = true;

                // 死亡信号：发在角色私有总线（自动冒泡到 Global），自带 entity+killer。
                // 角色自身系统订阅私有总线 = 天然区分角色；外部系统(UI/奖励/特效)在 Global 订阅并按引用过滤
                blackboard.eventBus.Fire(new EntityDiedEvent(blackboard.characterRunTimeData.self, killer));

                // 强制进入死亡状态（刷新式立即打断一切，无视 canceltime）
                if (blackboard.CharacterSO.deathStateData != null)
                    arbiter.Request(StateType.Death, blackboard.CharacterSO.deathStateData, SwitchStateType.Refresh, ignoreCancelTime: true);
                else
                    Debug.LogWarning("no deathStateData in CharacterSO !!!");

                // 清空队列，防止死亡后还有残留伤害
                damageQueue.Clear();
            }
            else
            {
                float minValue = float.MaxValue;
                DamageData keyWithMinValue = default;

                foreach (var kvp in damageInfo)
                {
                    if (kvp.Value < minValue)
                    {
                        minValue = kvp.Value;
                        keyWithMinValue = kvp.Key;
                    }
                }

                // 写运行时受击上下文：方向来自攻击者相对位置（外部运行时值，不进入 StateData）
                Vector2 dir = (Vector2)keyWithMinValue.hitDirection;
                if (dir.sqrMagnitude < 0.0001f)
                    dir = new Vector2(-blackboard.characterRunTimeData.facingDir, 0f); // 兜底：正面打来→推向背面
                blackboard.characterRunTimeData.hitDirection = dir.normalized;
                blackboard.characterRunTimeData.hitAttacker = keyWithMinValue.Attacker;
                blackboard.characterRunTimeData.hitDamageType = keyWithMinValue.damageType;

                // 通知表现层（sprite 变红、震屏、受击特效等），与状态机解耦
                EventBus.Global.Fire(new OnHitTaken(keyWithMinValue.Attacker,
                    blackboard.characterRunTimeData.hitDirection, minValue, keyWithMinValue.damageType,
                    blackboard.characterRunTimeData.self));

                if (HitMap.TryGetValue(keyWithMinValue.damageType,out StateData data))
                {
                    // 受击属于强制打断，无视当前状态的可取消时间
                    arbiter.Request(StateType.Hit,data,SwitchStateType.Refresh,ignoreCancelTime:true);
                }
                else
                {
                    Debug.LogWarning("no DamageType with stateData");
                }
            }
        }
        
    }

}



public struct DamageData
{
    public DamageType damageType;
    [HideInInspector]
    public GameObject Attacker;
    [HideInInspector]
    public Vector2 hitDirection;
    public float baseValue;
}

/// <summary>
/// 受击事件（由 DamageReceiver 在非致死受击结算后发出）
/// 供表现层（sprite 变红、震屏、特效）监听，与状态机解耦
/// </summary>
public struct OnHitTaken
{
    public readonly GameObject Attacker;
    public readonly Vector2 HitDirection;
    public readonly float Damage;
    public readonly DamageType DamageType;
    public readonly GameObject Target;
    public OnHitTaken(GameObject attacker, Vector2 hitDir, float damage, DamageType type, GameObject target)
    {
        Attacker = attacker;
        HitDirection = hitDir;
        Damage = damage;
        DamageType = type;
        Target = target;
    }
}


public enum DamageType
{
    /// <summary>
    /// 轻击
    /// </summary>
    Tap,
    /// <summary>
    /// 重击
    /// </summary>
    HeavyStrike,
}
