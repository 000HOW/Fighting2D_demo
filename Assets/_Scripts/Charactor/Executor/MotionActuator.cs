using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// 角色物理运动执行器
/// 注意：依赖注入，需要驱动
/// </summary>
public class MotionActuator
{
    SpriteRenderer spriteRenderer;
    Rigidbody2D rigidbody2d;
    Blackboard blackboard;

    public MotionActuator(SpriteRenderer _spriteRenderer,Rigidbody2D rigid,Blackboard data)
    {
        spriteRenderer = _spriteRenderer;
        rigidbody2d = rigid;
        blackboard = data;
    }
    public void Onupdate()
    {

        float horizontalVelocity = blackboard.readytoApply.exp_horizontalVelocity;
        float verticalVelocity = blackboard.readytoApply.exp_VerticalVelocity;

        if (blackboard.characterRunTimeData.isground && verticalVelocity<0)
        verticalVelocity = 0;

        Vector2 FinalVelocity = new Vector3(horizontalVelocity,
        verticalVelocity);

        spriteRenderer.flipX = blackboard.characterRunTimeData.facingDir < 0;

        rigidbody2d.velocity = FinalVelocity;
    }

}