using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
///
/// </summary>
public class MainPanel : BasePanel
{
    static readonly string path = "Prefab/UI/MainPanel";//地址的最后一个是要复制的文件名称
    public MainPanel() : base(new UItype(path))
    {
    }
    public override void OnEnter()
    {
        base.OnEnter();
        var btnQuit = uItool.GetOrAddComponentInChildren<Button>("Quit");
        btnQuit?.onClick.RemoveAllListeners();
        btnQuit?.onClick.AddListener(() =>
        {
        //    Debug.Log("按钮被点击"); 
           _ = GameRoot.Instance.sceneSystem.SetSceneAsync(new StartScene());
        });
    }
}
