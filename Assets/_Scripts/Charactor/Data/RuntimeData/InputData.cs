using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEngine;

/// <summary>
/// 运行时输入数据容器
/// 注意：需要初始化，需要从输入源获取更新数据
/// </summary>
public class InputData
{
    public InputCommand cur_inputComand;
    /// <summary>
    /// 输入缓存最大容量
    /// </summary>
    private const int BUFFER_SIZE = 30;
    //输入缓存
    private ECommand []commands = new ECommand[BUFFER_SIZE];
    private int head;                   // 队头索引（最早入队的指令）
    private int tail;                   // 队尾索引（下一个写入位置）
    private int count;                  // 当前缓冲区中的指令数量

    CharacterSO CharacterSO;

    public void Initialize(CharacterSO _CharacterSO)
    {
        head = 0;
        tail = 0;
        count = 0;
        CharacterSO = _CharacterSO;
    }

    /// <summary>
    /// 清空所有指令
    /// </summary>
    public void Clear()
    {
        head = 0;
        tail = 0;
        count = 0;
    }

    /// <summary>
    /// 当前缓冲的指令数量（已自动剔除超时的）
    /// </summary>
    public int Count
    {
        get
        {
            ClearExpiredCommands();
            return count;
        }
    }

    /// <summary>
    /// 添加输入缓存
    /// </summary>
    /// <param name="comand"></param>
    public void AddBuffer(ECommand comand)
    {
        ClearExpiredCommands();

        if (count==BUFFER_SIZE)
        {
            head = (head + 1)%BUFFER_SIZE;
            count--;
        }

        commands[tail] = comand;
        tail = (tail + 1)%BUFFER_SIZE;
        count++;
    }
    public bool UseHeadBufferCommand(out ECommand command)
    {
        ClearExpiredCommands();
        if (count==0)
        {
            command=default;
            return false;
        }
        command = commands[head];
        head = (head + 1)%BUFFER_SIZE;
        count--;

        if (count==0)
        {
            tail = head;
        }

        return true;
    }
    /// <summary>
    /// 消费缓存输入数据：只消费不读
    /// </summary>
    /// <param name="useSize">要消费的指令数量</param>
    /// <returns></returns>
    public bool UseBufferCommand(int useSize = 1)
    {
        ClearExpiredCommands();

        if (count - useSize < 0)
        {
            return false;
        }

        head = (head + useSize)%BUFFER_SIZE;
        count-=useSize;

        if (count==0)
        {
            tail = head;
        }

        return true;
    }
    /// <summary>
    /// 只读取不消费
    /// </summary>
    /// <param name="index">从0开始的索引</param>
    /// <param name="command"></param>
    /// <returns></returns>
    public bool ReadBufferComand(int index,out ECommand command)
    {
        ClearExpiredCommands();
        if (index >= count|| index < 0)
        {
            command = default;
            return false;
        }

        command = commands[(head + index)%BUFFER_SIZE];
        return true;
    }
    /// <summary>
    /// 只读缓存队头指令
    /// </summary>
    /// <returns></returns>
    public ECommand PeekBufferComand()
    {
        return count==0 ? default : commands[head];
    }
    /// <summary>
    /// 清理过期指令
    /// </summary>
    private void ClearExpiredCommands()
    {
        float now = Time.time;
        int maxsize = BUFFER_SIZE;
        while (count > 0 && maxsize-->0)
        {
            ECommand front = commands[head];
            if (now-front.pressedTime <= CharacterSO.InputWindowTime)
                break;
            
            head = (head + 1)%BUFFER_SIZE;
            count--;

        }

        if (count==0)
        {
            tail = head;
        }
    }
    // void IndexAdd(ref int index) => index = (index + 1)%BUFFER_SIZE;
    // int IndexADD(int index) => (index+1)%BUFFER_SIZE;
}

/// <summary>
/// 输入数据类型
/// </summary>
[System.Serializable]
public struct InputCommand
{
    public  bool inLeft;
    public  bool inRight;
    public  bool inJump;
    public  bool inDash;
    public bool inAattack;
    public bool inAttack2;
    public bool inUp;
    public bool inDown;
    public float pressedTime;
    public Vector3 NormalizeAxis;

}
/// <summary>
/// 输入数据的原子指令，用于缓存队列
/// </summary>
public enum ECommandType
{
    None,
    Left,
    Right,
    Jump,
    Dash,
    Attack,
    Attack2,
    Up,
    Down,
}
public struct ECommand
{
    public float pressedTime;
    public ECommandType eCommand;
}