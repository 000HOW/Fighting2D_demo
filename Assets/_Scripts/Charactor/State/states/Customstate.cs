using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 自定义状态
/// stateData = 动画/运动/打断等数据（内嵌可序列化 StateData，来自技能 SkillState.animData 或转移表的纯数据）。
/// customStateData = 行为资产（BaseCustomStateData SO，可空）：由 Statemachine 在进入 Custom 时注入；
///                   为 null 表示纯数据自定义状态，降级为基类通用运动（等同 GenericState 但优先级更高）。
/// </summary>
public class Customstate : BaseCharacterstate
{
    public BaseCustomStateData customStateData;
    public Customstate ()
    {
        stateType = StateType.Custom;
    }

    public override void OnEntryStart(Blackboard bb)
    {
        base.OnEntryStart(bb);
        customStateData?.OnEntryStart(bb);
    }
    public override void OnExitStart(Blackboard bb)
    {
        base.OnExitStart(bb);
        customStateData?.OnExitStart(bb);
    }
    public override bool OnEntry(Blackboard bb)
    {
        customStateData?.OnEntry(bb);
        return base.OnEntry(bb);
    }
    public override bool OnExit(Blackboard bb)
    {
        customStateData?.OnExit(bb);
        return base.OnExit(bb);
    }
    public override void OnUpdate(Blackboard bb)
    {
        base.OnUpdate(bb);
        customStateData?.OnUpdate(bb);
    }
    public override void UpdatePhysics(Blackboard bb, float deltaTime)
    {
        if (customStateData != null)
        {
            if (customStateData.useOrigPhyics)
                base.UpdatePhysics(bb, deltaTime);
            customStateData.UpdatePhysics(bb, deltaTime);
        }
        else
        {
            // 纯数据自定义状态：走基类通用运动
            base.UpdatePhysics(bb, deltaTime);
        }
    }
}
