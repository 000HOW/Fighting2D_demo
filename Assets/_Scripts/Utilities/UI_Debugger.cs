using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEngine;
using System.Text;
using System.Reflection;   // 新增，用于反射
using TMPro;

public class UI_Debugger : MonoBehaviour
{
    public TextMeshProUGUI text_inputData;
    public TextMeshProUGUI text_character;
    public CharacterControler playerControler;
    InputData inputData;
    Blackboard blackboard;
    void Start()
    {
        if (playerControler==null)
        blackboard = GetComponent<CharacterControler>().blackboard;
        else 
        blackboard = playerControler.blackboard;

        inputData = blackboard?.inputData;
    }
    void Update()
    {
        DebuggerInputData();
        DebuggerCharacterData();
    }

    void DebuggerCharacterData()
    {
        if (blackboard==null||text_character==null) return;
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("========== character Data ===============");
        sb.AppendLine("playerRuntimeData: ");
        sb.AppendLine($"currentHealth: {blackboard.characterRunTimeData.currentHealth}");

        text_character.text = sb.ToString();
    }
    void DebuggerInputData()
    {
        if (text_inputData==null||blackboard==null) return;
        text_inputData.text = GetInputDebugText();
    }
    /// <summary>
    /// 生成当前输入状态的完整调试文本，可直接赋给 TextMeshPro.text
    /// </summary>
    string GetInputDebugText()
    {
        if (inputData == null)
            return "InputData is null";

        // 先调用公共 Count 属性，触发过期指令清除（保持数据一致性）
        int validCount = inputData.Count;

        // 通过反射获取私有字段（不修改原类）
        var type = typeof(InputData);
        var commandsField = type.GetField("commands", BindingFlags.NonPublic | BindingFlags.Instance);
        var headField = type.GetField("head", BindingFlags.NonPublic | BindingFlags.Instance);
        var tailField = type.GetField("tail", BindingFlags.NonPublic | BindingFlags.Instance);
        // count 也可以直接用 validCount，但为了显示原始值也可反射，但 validCount 已经是最新
        // 这里反射 head/tail 以查看指针位置
        int head = (int)headField.GetValue(inputData);
        int tail = (int)tailField.GetValue(inputData);
        var comands = (ECommand[])commandsField.GetValue(inputData);

        const int bufferSize = 30;  // 与 BUFFER_SIZE 一致

        var sb = new StringBuilder();
        sb.AppendLine("===== InputData Debug =====");

        // 当前主指令
        var cur = inputData.cur_inputComand;
        sb.AppendLine($"CurCommand: L={cur.inLeft} R={cur.inRight} J={cur.inJump} D={cur.inDash} Time={cur.pressedTime:F2}");

        // 缓冲区信息
        sb.AppendLine($"Buffer Count: {validCount} (head={head}, tail={tail})");
        sb.Append("Buffer: ");
        if (validCount == 0)
        {
            sb.Append("Empty");
        }
        else
        {
            // 从 head 开始遍历 validCount 个有效指令
            for (int i = 0; i < validCount; i++)
            {
                int idx = (head + i) % bufferSize;
                var cmd = comands[idx];
                sb.Append($"{cmd.eCommand}@{cmd.pressedTime:F2}");
                if (i < validCount - 1) sb.Append(", ");
            }
        }

        return sb.ToString();
    }
}
