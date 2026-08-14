using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName ="SkillData",menuName ="PlayerControler/SkillData")]
public class PlayerSkillData : ScriptableObject
{
    public Sprite icon;
    public string Description;
    //技能冷却时间
    public float ColdownTime;
    public BaseCustomStateData SkillState;
    public int Priority;
}
