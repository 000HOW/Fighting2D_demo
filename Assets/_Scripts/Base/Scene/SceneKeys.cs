/// <summary>
/// 场景地址集中管理（对齐 AssetAddresses 规范：集中定义、禁止业务层手写字符串）。
/// 值必须与 Addressables Scenes group 中的 Address 一致：
///   Start = Start.unity
///   Main  = Main.unity
///   Boss  = Boss.unity
/// </summary>
public static class SceneKeys
{
    public const string Start = "Start";
    public const string Main = "Main";
    public const string Boss = "Boss";

    /// <summary>
    /// Addressables Key → Build Settings 场景名（不带 .unity 后缀）。
    /// 用于 SceneSystem 判断"目标场景是否已是当前激活场景"，从而短路跳过 Unity 场景加载。
    /// 未知 Key 返回 null。
    /// </summary>
    public static string SceneNameOf(string key)
    {
        switch (key)
        {
            case Start: return "Start";
            case Main: return "Main";
            case Boss: return "Boss";
            default: return null;
        }
    }
}
