public static class TransitionPriority
{
    // 用静态方法统一决定：每个状态类型对应的优先级
    public static int Of(StateType type) => type switch
    {
        StateType.Idle  => 0,
        StateType.Walk  => 0,
        StateType.Run   => 0,
        StateType.Custom=> 3,
        StateType.Up    => 5,
        StateType.Fall  => 5,

        StateType.Dash  => 10,

        StateType.attack => 50,   // 攻击行为

        StateType.Hit    => 100,  // 受击

        StateType.Death  => int.MaxValue,   // 死亡：最高优先级，可打断一切

        _ => 0
    };
}