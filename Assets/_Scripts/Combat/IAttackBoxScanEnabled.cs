/// <summary>
/// 攻击盒扫描标记接口（Boss 冲刺等 Custom 态专用）：
/// 实现它的自定义状态（BaseCustomStateData 子类）允许 EnvironmentSensor 在 Custom 态下
/// 持续扫描攻击盒（AttackCheck 的放宽门，原门限只允许 StateType.attack）。
/// 框架唯一改动点（EnvironmentSensor.AttackCheck）即依赖本接口识别"可扫描的自定义状态"。
/// </summary>
public interface IAttackBoxScanEnabled
{
    /// <summary>当前是否允许扫描攻击盒（冲刺全程 true；可配资产开关）</summary>
    bool attackScanEnabled { get; }
}
