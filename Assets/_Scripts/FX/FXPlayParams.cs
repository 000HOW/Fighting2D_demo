using UnityEngine;

/// <summary>
/// 特效播放参数：一次播放所需的完整形态信息
/// </summary>
public struct FXPlayParams
{
    public Vector3 position;
    public float rotationZ;
    public Vector3 scale;
    public float playSpeed;
    public int sortingOrder;
}
