using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseScanBox : ScriptableObject
{
    public Vector2 boxOffset;
    [Min(0)]
    public float boxsizeX;
    [Min(0)]
    public float boxsizeY;
    public LayerMask layerMask;
}
