using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEditor.Localization.Editor;

namespace Unity.Animations.SpringBones
{
    public class SaveSpringBoneSetupWindow : EditorWindow
    {
        private static class Styles
        {
            public static readonly string editorWindowTitle = L10n.Tr("Save SpringBone Setup");
            public static readonly string textSpringBoneRoot = L10n.Tr("SpringBone Root");
            public static readonly string textSaveSpringBoneSetup = L10n.Tr("Save SpringBone Setup");
            public static readonly string textSaveSpringBone = L10n.Tr("Save SpringBone");
            public static readonly string textFileOverwriteFormat = L10n.Tr("File already exists. Overwrite?:{0}\n\n");
            public static readonly string textOverwrite = L10n.Tr("Overwrite");
            public static readonly string textSavedFormat = L10n.Tr("Saved.: {0}");
            public static readonly string textCancel = L10n.Tr("Cancel");
            
            public static readonly GUIContent labelExportSetting = new GUIContent(L10n.Tr("Export Setting"));
            public static readonly GUIContent labelSpringBone = new GUIContent(L10n.Tr("SpringBone"));
            public static readonly GUIContent labelCollider = new GUIContent(L10n.Tr("Collider"));
            public static readonly GUIContent labelGetRootFromSelection = new GUIContent(L10n.Tr("Get root from selection"));
            public static readonly GUIContent labelSaveToCSV = new GUIContent(L10n.Tr("Save to CSV"));
        }

        public static void ShowWindow()
        {
            var editorWindow = GetWindow<SaveSpringBoneSetupWindow>(Styles.editorWindowTitle);
            if (editorWindow != null)
            {
                editorWindow.SelectObjectsFromSelection();
            }
        }

        // private

        private GameObject springBoneRoot;
        private SpringBoneSerialization.ExportSettings exportSettings;

        private void SelectObjectsFromSelection()
        {
            springBoneRoot = null;

            if (Selection.objects.Length > 0)
            {
                springBoneRoot = Selection.objects[0] as GameObject;
            }

            if (springBoneRoot == null)
            {
                var characterRootComponentTypes = new System.Type[] {
                    typeof(SpringManager),
                    typeof(Animation),
                    typeof(Animator)
                };
                springBoneRoot = characterRootComponentTypes
                    .Select(type => FindObjectOfType(type) as Component)
                    .Where(component => component != null)
                    .Select(component => component.gameObject)
                    .FirstOrDefault();
            }
        }

        private void ShowExportSettingsUI(ref Rect uiRect)
        {
            if (exportSettings == null)
            {
                exportSettings = new SpringBoneSerialization.ExportSettings();
            }

            GUI.Label(uiRect, Styles.labelExportSetting, SpringBoneGUIStyles.HeaderLabelStyle);
            uiRect.y += uiRect.height;
            exportSettings.ExportSpringBones = GUI.Toggle(uiRect, exportSettings.ExportSpringBones, Styles.labelSpringBone, SpringBoneGUIStyles.ToggleStyle);
            uiRect.y += uiRect.height;
            exportSettings.ExportCollision = GUI.Toggle(uiRect, exportSettings.ExportCollision,Styles.labelCollider, SpringBoneGUIStyles.ToggleStyle);
            uiRect.y += uiRect.height;
        }

        private void OnGUI()
        {
            SpringBoneGUIStyles.ReacquireStyles();

            const int ButtonHeight = 30;
            const int UISpacing = 8;
            const int UIRowHeight = 24;

            var uiWidth = (int)position.width - UISpacing * 2;
            var yPos = UISpacing;

            springBoneRoot = LoadSpringBoneSetupWindow.DoObjectPicker(
                Styles.textSpringBoneRoot, springBoneRoot, uiWidth, UIRowHeight, ref yPos);
            var buttonRect = new Rect(UISpacing, yPos, uiWidth, ButtonHeight);
            if (GUI.Button(buttonRect, Styles.labelGetRootFromSelection, SpringBoneGUIStyles.ButtonStyle))
            {
                SelectObjectsFromSelection();
            }
            yPos += ButtonHeight + UISpacing;
            buttonRect.y = yPos;

            ShowExportSettingsUI(ref buttonRect);
            if (springBoneRoot != null)
            {
                if (GUI.Button(buttonRect, Styles.labelSaveToCSV, SpringBoneGUIStyles.ButtonStyle))
                {
                    BrowseAndSaveSpringSetup();
                }
            }
        }

        private void BrowseAndSaveSpringSetup()
        {
            if (springBoneRoot == null) { return; }

            var initialFileName = springBoneRoot.name + "_Dynamics.csv";

            var path = EditorUtility.SaveFilePanel(
                Styles.textSaveSpringBoneSetup, "", initialFileName, "csv");
            if (path.Length == 0) { return; }

            if (System.IO.File.Exists(path))
            {
                var overwriteMessage = string.Format(Styles.textFileOverwriteFormat, path);
                if (!EditorUtility.DisplayDialog(Styles.textSaveSpringBone, overwriteMessage, Styles.textOverwrite, 
                    Styles.textCancel))
                {
                    return;
                }
            }

            var sourceText = SpringBoneSerialization.BuildDynamicsSetupString(springBoneRoot, exportSettings);
            if (FileUtil.WriteAllText(path, sourceText))
            {
                AssetDatabase.Refresh();
                Debug.LogFormat(Styles.textSavedFormat, path);
            }
        }
    }
}