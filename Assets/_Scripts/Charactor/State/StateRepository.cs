using System;
using System.Collections.Generic;

/// <summary>
/// 状态注册仓库
/// 负责存储状态和给外界提供状态
/// 注意：依赖注入
/// </summary>
public class StateRepository
{
    readonly Dictionary<StateType, BaseCharacterstate> states = new();

    public StateRepository()
    {
        foreach (StateType t in Enum.GetValues(typeof(StateType)))
        {
            if (t == StateType.None) continue;
            states[t] = t switch
            {
                StateType.Custom => new Customstate(),
                StateType.Hit    => new Hitstate(),   // 受击走专用方向物理
                StateType.Death  => new Deathstate(), // 死亡：终态，倒地循环
                _                => new GenericState(t),
            };
        }
    }

    public BaseCharacterstate GetState(StateType stateType) => states[stateType];
}
