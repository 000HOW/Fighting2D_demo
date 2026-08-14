using System.Collections;
using System.Collections.Generic;
using GameFramework.Event;
using GameFramework.Tool;
using UnityEngine;

/// <summary>
/// 游戏入口：单例 + 场景系统 + 异步场景管理器初始化。
/// 建议在挂载本脚本的同一物体上挂载 GlobalUILayer / SceneJumpManager / PlayerSession（见用户操作文档）。
/// </summary>
public class GameRoot : MonoBehaviour
{
    public static GameRoot Instance{get;private set;}
    public SceneSystem sceneSystem{get;private set;}
    void Awake()
    {
        if (Instance==null)
        Instance = this;
        else
        Destroy(gameObject);
        sceneSystem = new SceneSystem();
        GameFramework.Scene.SceneManager.Instance.Initialize();
        DontDestroyOnLoad(gameObject);
    }
    void Start()
    {
        _ = sceneSystem.SetSceneAsync(new StartScene());
    }
}
