using TMPro;
using UnityEditor;
using UnityEngine;

public static class JapaneseFontSetup
{
    private const string SourceFontPath = "Assets/SourceHanSansJP-Regular.otf";
    private const string OutputFolderPath = "Assets/TextMesh Pro/Resources/Fonts & Materials";
    private const string OutputAssetPath = OutputFolderPath + "/SourceHanSansJP SDF.asset";

    [MenuItem("Tools/Font/Create Japanese TMP Font Asset")]
    public static void CreateJapaneseTmpFontAsset()
    {
        Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
        if (sourceFont == null)
        {
            Debug.LogError($"Japanese font was not found: {SourceFontPath}");
            return;
        }

        if (!AssetDatabase.IsValidFolder(OutputFolderPath))
        {
            Debug.LogError($"Output folder was not found: {OutputFolderPath}");
            return;
        }

        TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(OutputAssetPath);
        if (fontAsset == null)
        {
            fontAsset = TMP_FontAsset.CreateFontAsset(sourceFont);
            AssetDatabase.CreateAsset(fontAsset, OutputAssetPath);
        }

        fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
        fontAsset.name = "SourceHanSansJP SDF";
        EditorUtility.SetDirty(fontAsset);

        TMP_Settings settings = TMP_Settings.GetSettings();
        if (settings != null)
        {
            SerializedObject serializedSettings = new SerializedObject(settings);
            SerializedProperty fallbackFontAssets = serializedSettings.FindProperty("m_fallbackFontAssets");
            if (fallbackFontAssets != null && !ContainsObjectReference(fallbackFontAssets, fontAsset))
            {
                int index = fallbackFontAssets.arraySize;
                fallbackFontAssets.InsertArrayElementAtIndex(index);
                fallbackFontAssets.GetArrayElementAtIndex(index).objectReferenceValue = fontAsset;
                serializedSettings.ApplyModifiedProperties();
                EditorUtility.SetDirty(settings);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Created/updated Japanese TMP font asset: {OutputAssetPath}");
    }

    private static bool ContainsObjectReference(SerializedProperty arrayProperty, Object target)
    {
        for (int i = 0; i < arrayProperty.arraySize; i++)
        {
            if (arrayProperty.GetArrayElementAtIndex(i).objectReferenceValue == target)
            {
                return true;
            }
        }

        return false;
    }
}
