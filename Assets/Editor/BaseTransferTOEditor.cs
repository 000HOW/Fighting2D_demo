using UnityEditor;

[CustomEditor(typeof(BaseTransferTO), true)] // true 表示对子类也生效
public class BaseTransferTOEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // 获取当前选中的 SO 实例
        BaseTransferTO transfer = (BaseTransferTO)target;

        // 通过反射或直接访问获取 Description 值（因为是 readonly 字段）
        // 这里利用反射获取字段，当然你也可以将 Description 改为虚属性
        var field = transfer.GetType().GetField("Description");
        if (field != null)
        {
            string desc = field.GetValue(transfer) as string;
            if (!string.IsNullOrEmpty(desc))
            {
                EditorGUILayout.HelpBox($"指令意图: {desc}", MessageType.Info);
                EditorGUILayout.Space();
            }
        }

        // 绘制默认的 Inspector 内容（其他序列化字段）
        DrawDefaultInspector();
    }
}