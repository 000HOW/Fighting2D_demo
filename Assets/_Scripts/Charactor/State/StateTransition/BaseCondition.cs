/// <summary>
/// 配置转换条件
/// 通过枚举选择数据
/// </summary>
public interface BaseCondition
{
    public abstract bool IsTrue(Blackboard bb);
}

public enum IntRunData
{
    facingDir
}

public enum FloatRunData
{
    horizontalSpeed,
    verticalSpeed,
    exp_horizontalVelocity,
    exp_VerticalVelocity,
    currentHealth,
}

public enum BoolRunData
{
    isground,
    iswall,
    einleft,
    einright,
    einjump,
    eindash,
    inLeft,
    inRight,
    inJump,
    inDash
}