using UnityEngine;

/// <summary>
/// 所有提供输入信息的输入源定义
/// </summary>
public interface IInput
{
    public InputCommand GetInput();
    
}
