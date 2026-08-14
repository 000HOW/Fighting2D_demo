using System;
using System.Collections;
using System.Collections.Generic;
using GameFramework.Event;
using UnityEngine;

public class ModificationManager
{
    List<UsableModifier> modifiers = new();
    // 待添加的缓冲队列
    List<UsableModifier> pendingAdds = new();

    /// <summary>
    /// 所属角色黑板（由 CharacterControler 创建后回填，无需构造参数注入）。
    /// 角色自身物体引用已缓存在 blackboard.playerRunTimeData.self，发事件时据此区分玩家/敌人。
    /// </summary>
    public Blackboard blackboard;

    // 发事件时的所属角色引用：取角色运行时数据里缓存的 self
    GameObject Owner => blackboard?.characterRunTimeData?.self;

    public float FinalModificationMultiplier(ModifyValueType valuetype)
    {
        float multiplier = 1;
        foreach(var modifier in modifiers)
        {
            if(modifier.CanUse)
            {
                if (modifier.modifierData.valueype==valuetype)
                multiplier *= modifier.modifierData.multiplier;
            }
        }
        return multiplier;
    }

    public void AddModifier(ModifierData data)
    {
        UsableModifier modifier = new UsableModifier(data);
        // 先不直接加到 modifiers，而是放到缓冲里
        pendingAdds.Add(modifier);
    }

    public void OnUpdate()
    {
        // 1. 【关键】先把待添加的合并进来（放在开头，让新Buff本帧立即参与计算）
        if (pendingAdds.Count > 0)
        {
            foreach (var m in pendingAdds)
                EventBus.Global.Fire(new ModifierAddEvent(m, Owner));
            modifiers.AddRange(pendingAdds);
            pendingAdds.Clear();
        }

        // 2. 更新所有 modifier（此时列表已包含最新的）
        foreach (var modifier in modifiers)
        {
            modifier.Tick();
        }

        // 3. 倒序移除并触发事件（零 GC）
        for (int i = modifiers.Count - 1; i >= 0; i--)
        {
            var mod = modifiers[i];
            if (!mod.CanUse)
            {
                // 触发移除事件（传递 struct，不装箱）
                EventBus.Global.Fire(new ModifierRemoveEvent(mod, Owner));
                modifiers.RemoveAt(i);
            }
        }
    }

}
/*彻底避免冲突：AddModifier 不再直接触碰主列表，任何时刻调用都不会破坏正在进行的遍历。

逻辑清晰：所有对列表的“写操作”（增删）都集中在 OnUpdate 的有序阶段完成，FinalModificationMultiplier 读取时永远拿到的是完整、一致的数据。

性能优秀：AddRange 是一次性批量操作，比逐个 Add 效率高。

*/


public class UsableModifier
{
    public readonly ModifierData modifierData;
    public float RemainingTime{get;private set;}
    public bool CanUse{get;private set;}
    public UsableModifier(ModifierData data)
    {
        modifierData = data;
        RemainingTime = modifierData.duration;
        CanUse = true;
    }
    public void Tick()
    {
        RemainingTime -= Time.deltaTime;
        if (RemainingTime<=0)
        CanUse = false;
    }
    
}

public interface IModifier
{
    void AddModifier(ModifierData modifier);
}


[System.Serializable]
public class ModifierData
{
    public ModifyValueType valueype;
    public Sprite icon;
    public float multiplier;
    public float duration;
}

public enum ModifyValueType
{
    MoveSpeed,
    AttackPower,
    DamageReduction
}