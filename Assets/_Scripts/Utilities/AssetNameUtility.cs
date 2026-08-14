using UnityEditor;
using UnityEngine;
using System.IO;

/// <summary>
/// 工具类：提供资产命名的辅助方法
/// </summary>
public static class AssetNameUtility
{
    /// <summary>
    /// 生成唯一的资产名称，如果存在冲突则自动添加 (1), (2), (3)...
    /// </summary>
    /// <param name="currentAssetPath">当前资产的完整路径（如 AssetDatabase.GetAssetPath 返回的路径）</param>
    /// <param name="desiredName">期望的资产名称（不含扩展名）</param>
    /// <returns>确保唯一的资产名称</returns>
    public static string GenerateUniqueAssetName(string currentAssetPath, string desiredName)
    {
        // 获取当前资产所在的文件夹路径
        string directory = Path.GetDirectoryName(currentAssetPath);
        // 获取扩展名（例如 ".asset"）
        string extension = Path.GetExtension(currentAssetPath);
        // 获取当前文件名（用于判断是否就是自身）
        string currentFileName = Path.GetFileNameWithoutExtension(currentAssetPath);

        // 如果想要的名字和现在一模一样，直接返回，不需要改动
        if (desiredName == currentFileName)
            return desiredName;

        string newName = desiredName;
        int counter = 1;

        // 标准化当前资产路径，用于后续比较（统一斜杠方向）
        string normalizedCurrentPath = Path.GetFullPath(currentAssetPath);

        // 循环检查文件是否在磁盘上存在
        while (File.Exists(Path.Combine(directory, newName + extension)))
        {
            // 重要：检查找到的同名文件是不是"我自己"
            string existingFullPath = Path.Combine(directory, newName + extension);
            if (Path.GetFullPath(existingFullPath).Equals(normalizedCurrentPath, System.StringComparison.OrdinalIgnoreCase))
            {
                // 如果就是自己，说明名字没变，直接退出循环
                break;
            }

            // 如果存在的是别的资产，则生成带编号的新名字
            newName = $"{desiredName} ({counter})";
            counter++;
        }

        return newName;
    }
    /// <summary>
    /// 给当前资产重命名不重名
    /// </summary>
    /// <param name="baseName">要添加的后缀名称</param>
    /// <param name="object">要重命名的实例</param>
    public static void UpdateAssetName(string baseName,Object @object)
    {
         // 获取当前资产的路径
        string assetPath = AssetDatabase.GetAssetPath(@object.GetInstanceID());

        if (string.IsNullOrEmpty(assetPath))
            return;

        // 获取当前文件名（不含扩展名）
        string currentName = Path.GetFileNameWithoutExtension(assetPath);
        
        // 计算去重后的最终名称，避免 OnValidate 无限循环
        string finalName = GenerateUniqueAssetName(assetPath, baseName);
        
        // 如果新名称与当前名称不同，则执行重命名
        if (currentName != finalName)
        {
                // 延迟执行，避免在 OnValidate 中调用 AssetDatabase API
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (@object == null) return;
                string latestPath = AssetDatabase.GetAssetPath(@object.GetInstanceID());
                if (string.IsNullOrEmpty(latestPath)) return;
                string uniqueName = GenerateUniqueAssetName(latestPath, baseName);
                AssetDatabase.RenameAsset(latestPath, uniqueName);
                AssetDatabase.SaveAssets();
            };

        }
    }
}
