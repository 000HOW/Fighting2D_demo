using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrawTool : MonoBehaviour
{
    public Rigidbody2D rigid;
    public CharacterSO CharacterSO;
    Blackboard blackboard;
    public AttackBox attackBox;
    public ViewBox viewBox;

    void Start()
    {
        rigid = GetComponent<Rigidbody2D>();
        blackboard = GetComponent<CharacterControler>().blackboard;
    }
    private void OnDrawGizmos()
    {
        //注意绘图的先后顺序会影响覆盖关系
        Draw_groundDistance();
        Draw_groundBoxCast();
        Draw_curAttackBox();
        Draw_AttackBox();
        Draw_ViewBox();
    }

    void Draw_ViewBox()
    {
        if (viewBox==null) return;
        if (rigid==null||CharacterSO==null) return;
        if (blackboard==null||blackboard.characterRunTimeData==null) return;

        Gizmos.color = Color.red;

        // 实际检测框的范围：
        // X轴不变： [center.x - size.x/2 , center.x + size.x/2]
        // Y轴从起点顶部扫到终点底部： [center.y - size.y/2 - castDistance , center.y + size.y/2]
        
        Vector2 center = new Vector2(rigid.position.x + blackboard.characterRunTimeData.facingDir*viewBox.boxOffset.x , rigid.position.y + viewBox.boxOffset.y);
        Vector2 size = new Vector2(viewBox.boxsizeX,viewBox.boxsizeY);

        if (size.x==0||size.y==0) return;


        Gizmos.DrawWireCube(center, size);
    }

    void Draw_AttackBox()
    {
        if (attackBox==null) return;

         // 计算实际的检测盒中心（与 OverlapBoxAll 一致）
        Vector3 center = rigid.position + attackBox.boxOffset;
        // 若 boxcenter 是 Vector2，需转为 Vector3；如果是 Vector3 直接乘

        // 尺寸：2D 盒子的宽高，Z 轴厚度可设为 0 或 1（不影响显示）
        Vector3 size = new Vector3(attackBox.boxsizeX, attackBox.boxsizeY, 0.01f);
        float angle = attackBox.angle;   // 绕 Z 轴的旋转角度（度）

        // 设置 Gizmos 颜色
        Gizmos.color = Color.yellow;

        // 保存当前矩阵，应用旋转和缩放
        Matrix4x4 oldMatrix = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(center, Quaternion.Euler(0, 0, angle), size);

        // 绘制单位立方体（被矩阵变换为实际尺寸和旋转）
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one);

        // 恢复矩阵
        Gizmos.matrix = oldMatrix;
    }

    void Draw_curAttackBox()
    {
         if (blackboard == null || blackboard.characterRunTimeData == null) return;
        AttackBox curAttackBox = blackboard.characterRunTimeData.curAttackBox;
        if (curAttackBox == null) return;
        if (blackboard.characterRunTimeData.currentstateType != StateType.attack) return;
        float t = blackboard.characterRunTimeData.AnimNormalizedTime;
        bool active = t > curAttackBox.startNormalizedTime && t < curAttackBox.endNormalizedTime;
        if (!active) return;         
         
         // 计算实际的检测盒中心（与 OverlapBoxAll 一致）
        Vector3 center = new Vector3(rigid.position.x + blackboard.characterRunTimeData.facingDir*curAttackBox.boxOffset.x , rigid.position.y + curAttackBox.boxOffset.y);
        

        // 尺寸：2D 盒子的宽高，Z 轴厚度可设为 0 或 1（不影响显示）
        Vector3 size = new Vector3(curAttackBox.boxsizeX, curAttackBox.boxsizeY, 0.01f);
        float angle = curAttackBox.angle;   // 绕 Z 轴的旋转角度（度）

        // 设置 Gizmos 颜色
        Gizmos.color = Color.yellow;

        // 保存当前矩阵，应用旋转和缩放
        Matrix4x4 oldMatrix = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(center, Quaternion.Euler(0, 0, angle), size);

        // 绘制单位立方体（被矩阵变换为实际尺寸和旋转）
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one);

        // 恢复矩阵
        Gizmos.matrix = oldMatrix;

    }
    /// <summary>
    /// 绘制地面检测盒子
    /// </summary>
    void Draw_groundBoxCast()
    {
        if (rigid==null||CharacterSO==null||CharacterSO.groundCheckBox==null) return;

        Gizmos.color = Color.green;

        // 实际检测框的范围：
        // X轴不变： [center.x - size.x/2 , center.x + size.x/2]
        // Y轴从起点顶部扫到终点底部： [center.y - size.y/2 - castDistance , center.y + size.y/2]
        
        Vector2 center = rigid.position + CharacterSO.groundCheckBox.boxOffset;
        Vector2 size = new Vector2(CharacterSO.groundCheckBox.boxsizeX,CharacterSO.groundCheckBox.boxsizeY);
        float castDist = CharacterSO.groundCheckBox.castDistance;

        if (size.x==0||size.y==0) return;

        // 实际检测矩形的中心位置（向下偏移了一半的投射距离）
        Vector2 detectCenter = center + Vector2.down * (castDist / 2f);
        // 实际检测矩形的大小（高度增加了投射距离）
        Vector2 detectSize = new Vector2(size.x, size.y + castDist);

        Gizmos.DrawWireCube(detectCenter, detectSize);
    }

    /// <summary>
    /// 绘制从地面检测点出发角色实际离地的距离范围
    /// </summary>
    void Draw_groundDistance()
    {
        if (rigid==null||CharacterSO==null||CharacterSO.groundCheckBox==null) return;

        Gizmos.color = Color.red;

        Vector2 center = rigid.position + CharacterSO.groundCheckBox.boxOffset;
        Vector2 size = new Vector2(CharacterSO.groundCheckBox.boxsizeX,CharacterSO.groundCheckBox.boxsizeY);
        float castDist = CharacterSO.groundCheckBox.feetwidth;

        Vector2 detectCenter = center + Vector2.down * (castDist / 2f);
        Vector2 detectSize = new Vector2(size.x, size.y + castDist);

        Gizmos.DrawWireCube(detectCenter, detectSize);

    }
}
