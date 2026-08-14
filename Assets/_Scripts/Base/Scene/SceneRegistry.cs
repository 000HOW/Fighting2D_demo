using UnityEngine;

/// <summary>
/// 场景 Key → BaseScene 逻辑层实例 的统一工厂。
/// 跳转管理器只传 Key，由本类负责创建对应的逻辑场景对象，避免业务层直接 new。
/// </summary>
public static class SceneRegistry
{
    public static BaseScene Create(string key)
    {
        switch (key)
        {
            case SceneKeys.Start: return new StartScene();
            case SceneKeys.Main: return new MainScene();
            case SceneKeys.Boss: return new BossScene();
            default:
                Debug.LogError($"[SceneRegistry] 未知场景 Key: {key}");
                return null;
        }
    }
}
