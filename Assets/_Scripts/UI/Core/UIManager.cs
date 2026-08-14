using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
///
/// </summary>
public class UIManager
{
    private Dictionary<UItype,GameObject> dicUI;
    public UIManager()
    {
        dicUI = new Dictionary<UItype, GameObject>();
    }
    public GameObject GetSingleUI(UItype uItype)
    {
        GameObject parent = GameObject.Find("Canvas");
        if (parent==null)
        {
            Debug.LogWarning("no Canvas!!!");
            return null;
        }
        return GetSingleUI(uItype, parent.transform);
    }

    /// <summary>
    /// 指定父节点获取 UI（跨场景 UI 层使用，不依赖场景内 "Canvas"）。
    /// </summary>
    public GameObject GetSingleUI(UItype uItype, Transform parent)
    {
        if (parent==null)
        {
            Debug.LogWarning("no UI parent!!!");
            return null;
        }
        if (dicUI.ContainsKey(uItype)) 
        return dicUI[uItype];
        GameObject new_ui = GameObject.Instantiate(Resources.Load<GameObject>(uItype.Path),parent);
        if (new_ui==null)
        {
            Debug.LogWarning("no prefab!!!");
            return null;
        }
        new_ui.name = uItype.Name;
        dicUI[uItype] = new_ui;
        return new_ui;
    }
    public void DestroyUI(UItype uItype)
    {
        if (!dicUI.ContainsKey(uItype)) return;
        GameObject.Destroy(dicUI[uItype]);
        dicUI.Remove(uItype);
    }
}
