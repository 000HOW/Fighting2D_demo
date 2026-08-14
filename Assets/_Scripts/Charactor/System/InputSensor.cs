using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 输入检测系统：
/// 注意：需要驱动
/// </summary>
public class InputSensor
{
    Func<IInput> inputProvider;   // 替代 inputSource 字段
    IInput inputSource;
    InputData inputData;
    InputCommand lastcommand;
    //使用的元组的列表
    List<(Func<InputCommand,bool> selector,ECommandType command)> bindings;

    public InputSensor(Blackboard blackboard,Func<IInput> _inputProvider)
    {
        inputProvider = _inputProvider;

        inputData = blackboard.inputData;
        lastcommand = default;
        bindings = new List<(Func<InputCommand, bool>, ECommandType)>
        {
            (cmd => cmd.inLeft, ECommandType.Left),
            (cmd => cmd.inRight, ECommandType.Right),
            (cmd => cmd.inJump, ECommandType.Jump),
            (cmd => cmd.inDash, ECommandType.Dash),
            (cmd => cmd.inAattack, ECommandType.Attack),
            (cmd => cmd.inAttack2, ECommandType.Attack2),
            (cmd => cmd.inUp, ECommandType.Up),
            (cmd => cmd.inDown, ECommandType.Down),
        };
    }

    

    public void OnUpdate()
    {
        if (inputSource==null)
        {
            // Debug.Log("input==null");
            inputSource = inputProvider?.Invoke();
            return;
        }
        InputCommand current = inputSource.GetInput();
        current.pressedTime = Time.time;

        inputData.cur_inputComand = current;

        foreach (var binding in bindings)
        {
            // 检测按下瞬间：当前为 true，上一帧为 false
            if (binding.selector(current) && !binding.selector(lastcommand))
            {
                //只有按下的瞬间，才把"具体指令"存进缓冲区
                inputData.AddBuffer(new ECommand
                {
                    pressedTime = Time.time,
                    eCommand = binding.command
                });
            }
        }

        lastcommand = current;

    }

}
