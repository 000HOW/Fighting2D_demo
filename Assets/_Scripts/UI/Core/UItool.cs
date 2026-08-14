using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
///
/// </summary>
public class UItool
{
    GameObject ActivePanel;
    public UItool(GameObject gameObject)
    {
        ActivePanel = gameObject;
    }
    public T GetOrAddComponent<T>()where T:Component
    {
        if (ActivePanel.GetComponent<T>()==null)
            ActivePanel.AddComponent<T>();
        return ActivePanel.GetComponent<T>();
    }
    public GameObject FindChildGameobj(string _name)
    {
        if (ActivePanel == null) return null;
        _name = _name.Trim();

        // transform.Find 是精确匹配，子物体名若带了前后空格（如 "ProgressBar "）会匹配失败，
        // 这里先直接找，找不到再遍历所有子物体做 Trim 后比较，提升容错性。
        Transform child = ActivePanel.transform.Find(_name);
        if (child == null)
        {
            foreach (Transform t in ActivePanel.transform)
            {
                if (t.name.Trim() == _name)
                {
                    child = t;
                    break;
                }
            }
        }
        if (child != null)
            return child.gameObject;
        Debug.LogWarning($"找不到子物体：{_name}");
        return null;
    }
    public GameObject FindChildGameobj(GameObject parent, string _name)
    {
        if (parent == null) return null;
        _name = _name.Trim();

        Transform child = parent.transform.Find(_name);
        if (child == null)
        {
            foreach (Transform t in parent.transform)
            {
                if (t.name.Trim() == _name)
                {
                    child = t;
                    break;
                }
            }
        }
        if (child != null)
            return child.gameObject;
        Debug.LogWarning($"找不到子物体：{_name}");
        return null;
    }
    public T GetOrAddComponentInChildren<T>(string _name)where T:Component
    {
        GameObject child = FindChildGameobj(_name);
        if (!child) return null;
        if (child.GetComponent<T>()==null)
            child.AddComponent<T>();
        return child.GetComponent<T>();
    }
    public T GetOrAddComponentInChildren<T>(GameObject parent, string _name) where T : Component
    {
        GameObject child = FindChildGameobj(parent, _name);
        if (!child) return null;
        if (child.GetComponent<T>() == null)
            child.AddComponent<T>();
        return child.GetComponent<T>();
    }

    // public T FindGameobjAndAddComponent<T>(string _name,GameObject gameObject) where T:Component
    // {
    //     GameObject ComponentPanrent = FindChildGameobj(_name);
    //     if (!ComponentPanrent) return null;
    //     GameObject child = GameObject.Instantiate(gameObject,ComponentPanrent.transform);
    //     return child.GetComponent<T>();
        
    // }
    public void RemoveAllChildren(GameObject parent)
    {
        // 从最后一个子物体开始，循环到第一个
        for (int i = parent.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.transform.GetChild(i);
            // 在运行时使用 Destroy
            Object.Destroy(child.gameObject);
        }
    }


}
