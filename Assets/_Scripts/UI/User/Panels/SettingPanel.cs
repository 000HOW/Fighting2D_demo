using UnityEngine;
using UnityEngine.UI;
/// <summary>
///
/// </summary>
public class SettingPanel : BasePanel
{
    static readonly string path = "Prefab/UI/SettingPanel";//地址的最后一个是要复制的文件名称
    public SettingPanel() : base(new UItype(path))
    {
    }
    public override void OnEnter()
    {
        base.OnEnter();
        var btnExit = uItool.GetOrAddComponentInChildren<Button>("Setting_exit");
        btnExit?.onClick.RemoveAllListeners();
        btnExit?.onClick.AddListener(() =>
        {
        //    Debug.Log("删除按钮"); 
           panelManager.Pop();
        });
    }
    public override void Exit()
    {
        base.Exit();
    }
}
