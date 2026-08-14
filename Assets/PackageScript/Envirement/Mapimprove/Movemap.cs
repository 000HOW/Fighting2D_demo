using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
///
/// </summary>
public class Movemap : MonoBehaviour
{
    private GameObject cam;
    [SerializeField] private float mapEffect;
    private float distance;//地图长度
    private float priXposition;//初始地图x位置
    private float distanceMove;//地图偏移量
    private float distanceMoved;//无限地图移动(摄像机和地图相对移动距离)
    void GetNeed()
    {
        cam = GameObject.Find("Virtual Camera");
        priXposition = transform.position.x;
        distance = GetComponentInChildren<SpriteRenderer>().bounds.size.x;
    }
    private void Update()
    {
        if(cam==null)
        {
            GetNeed();
            return;
        }
        distanceMove = cam.transform.position.x * mapEffect;
        distanceMoved = cam.transform.position.x * (1 - mapEffect);
        transform.position = new Vector3(priXposition+distanceMove, transform.position.y);
        if (distanceMoved > priXposition + distance) priXposition += distance;
        else if (distanceMoved < priXposition - distance) priXposition -= distance;
    }
}
