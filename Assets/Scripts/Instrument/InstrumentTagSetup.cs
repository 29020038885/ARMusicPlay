using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

/// <summary>
/// 自动设置乐器系统所需的标签
/// </summary>
[InitializeOnLoad]
public static class InstrumentTagSetup
{
    static InstrumentTagSetup()
    {
        // 在编辑器启动时自动添加所需标签
        AddTag("Fret");
        AddTag("InstrumentString");
    }

    static void AddTag(string tag)
    {
        // 打开标签管理器
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tagsProp = tagManager.FindProperty("tags");

        // 检查标签是否已存在
        bool found = false;
        for (int i = 0; i < tagsProp.arraySize; i++)
        {
            SerializedProperty t = tagsProp.GetArrayElementAtIndex(i);
            if (t.stringValue.Equals(tag))
            {
                found = true;
                break;
            }
        }

        // 如果不存在，添加标签
        if (!found)
        {
            tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
            SerializedProperty newTagProp = tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1);
            newTagProp.stringValue = tag;
            tagManager.ApplyModifiedProperties();
            Debug.Log($"✅ 已自动添加标签: {tag}");
        }
    }
}

/// <summary>
/// 手动设置标签的编辑器菜单
/// </summary>
public class InstrumentTagMenu
{
    [MenuItem("Tools/乐器系统/设置所需标签")]
    public static void SetupTags()
    {
        AddTag("Fret");
        AddTag("InstrumentString");
        Debug.Log("✅ 乐器系统标签设置完成！");
        EditorUtility.DisplayDialog("完成", "已成功添加所需标签：\n- Fret\n- InstrumentString", "确定");
    }

    static void AddTag(string tag)
    {
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tagsProp = tagManager.FindProperty("tags");

        bool found = false;
        for (int i = 0; i < tagsProp.arraySize; i++)
        {
            SerializedProperty t = tagsProp.GetArrayElementAtIndex(i);
            if (t.stringValue.Equals(tag))
            {
                found = true;
                break;
            }
        }

        if (!found)
        {
            tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
            SerializedProperty newTagProp = tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1);
            newTagProp.stringValue = tag;
            tagManager.ApplyModifiedProperties();
        }
    }
}
#endif

