using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

public static class JapaneseFontSetup
{
    private const string SourceFontPath = "Assets/SourceHanSansJP-Regular.otf";
    private const string OutputFolderPath = "Assets/TextMesh Pro/Resources/Fonts & Materials";
    private const string OutputAssetPath = OutputFolderPath + "/SourceHanSansJP SDF.asset";

    [MenuItem("Tools/Font/Create Japanese TMP Font Asset")]
    public static void CreateJapaneseTmpFontAsset()
    {
        CreateJapaneseTmpFontAsset(forceRebuild: false);
    }

    [MenuItem("Tools/Font/Rebuild Japanese TMP Font Asset")]
    public static void RebuildJapaneseTmpFontAsset()
    {
        CreateJapaneseTmpFontAsset(forceRebuild: true);
    }

    [MenuItem("Tools/Font/Apply Japanese Font To Selected TMP Texts")]
    public static void ApplyJapaneseFontToSelectedTmpTexts()
    {
        TMP_FontAsset fontAsset = CreateJapaneseTmpFontAsset(forceRebuild: false);
        if (fontAsset == null)
        {
            return;
        }

        TMP_Text[] textComponents = Selection.GetFiltered<TMP_Text>(SelectionMode.Editable | SelectionMode.Deep);
        if (textComponents.Length == 0)
        {
            Debug.LogWarning("No TMP text components were selected.");
            return;
        }

        foreach (TMP_Text textComponent in textComponents)
        {
            Undo.RecordObject(textComponent, "Apply Japanese TMP Font");
            textComponent.font = fontAsset;
            textComponent.fontSharedMaterial = fontAsset.material;
            textComponent.color = Color.black;
            EditorUtility.SetDirty(textComponent);
            EditorSceneManager.MarkSceneDirty(textComponent.gameObject.scene);
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"Applied Japanese TMP font to {textComponents.Length} selected text component(s).");
    }

    private static TMP_FontAsset CreateJapaneseTmpFontAsset(bool forceRebuild)
    {
        Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
        if (sourceFont == null)
        {
            Debug.LogError($"Japanese font was not found: {SourceFontPath}");
            return null;
        }

        if (!AssetDatabase.IsValidFolder(OutputFolderPath))
        {
            Debug.LogError($"Output folder was not found: {OutputFolderPath}");
            return null;
        }

        TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(OutputAssetPath);
        if (forceRebuild || IsBrokenFontAsset(fontAsset))
        {
            AssetDatabase.DeleteAsset(OutputAssetPath);
            fontAsset = null;
        }

        if (fontAsset == null)
        {
            fontAsset = TMP_FontAsset.CreateFontAsset(
                sourceFont,
                90,
                9,
                GlyphRenderMode.SDFAA,
                1024,
                1024,
                AtlasPopulationMode.Dynamic);
            AssetDatabase.CreateAsset(fontAsset, OutputAssetPath);
        }

        fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
        fontAsset.name = "SourceHanSansJP SDF";
        EditorUtility.SetDirty(fontAsset);

        TMP_Settings settings = TMP_Settings.GetSettings();
        if (settings != null)
        {
            SerializedObject serializedSettings = new SerializedObject(settings);
            RemoveBrokenFallbackReferences(serializedSettings);

            serializedSettings.ApplyModifiedProperties();
            EditorUtility.SetDirty(settings);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Created/updated Japanese TMP font asset: {OutputAssetPath}");
        return fontAsset;
    }

    private static bool IsBrokenFontAsset(TMP_FontAsset fontAsset)
    {
        if (fontAsset == null)
        {
            return true;
        }

        return fontAsset.material == null
            || fontAsset.atlasTextures == null
            || fontAsset.atlasTextures.Length == 0
            || fontAsset.atlasTextures[0] == null;
    }

    private static void RemoveBrokenFallbackReferences(SerializedObject serializedSettings)
    {
        SerializedProperty fallbackFontAssets = serializedSettings.FindProperty("m_fallbackFontAssets");
        if (fallbackFontAssets == null)
        {
            return;
        }

        for (int i = fallbackFontAssets.arraySize - 1; i >= 0; i--)
        {
            SerializedProperty element = fallbackFontAssets.GetArrayElementAtIndex(i);
            TMP_FontAsset fallback = element.objectReferenceValue as TMP_FontAsset;
            if (IsBrokenFontAsset(fallback))
            {
                fallbackFontAssets.DeleteArrayElementAtIndex(i);
            }
        }
    }
}
