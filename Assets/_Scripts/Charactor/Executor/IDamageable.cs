using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IDamageable
{
    /// <summary>接收伤害。返回是否真正接受了伤害（已死亡/被忽略的实体返回 false，不产生命中反馈）</summary>
    public bool TakeDamage(DamageData damage);
}
