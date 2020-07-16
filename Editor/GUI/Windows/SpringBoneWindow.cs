using System.Linq;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEditor.Localization.Editor;

namespace Unity.Animations.SpringBones
{
    public class SpringBoneWindow : EditorWindow
    {
        private static class Styles
        {
            public static readonly string editorWindowTitle = Localization.Tr("SpringBone");

            public static readonly string logIconLoadFailedFormat = Localization.Tr("Icon load failed:\nPath:{0}");
            public static readonly string logIconDirNotFound = Localization.Tr("SpringBoneWindow icon directory not found");
            
            public static readonly string labelDynamicCSV = Localization.Tr("Dynamics CSV");
            public static readonly string labelLoad = Localization.Tr("Load");
            public static readonly string labelSave = Localization.Tr("Save");
            public static readonly string labelSpringBone = (Localization.Tr("SpringBone"));
            public static readonly string labelSpringBoneAdd = (Localization.Tr("Add\nSpringBone"));
            public static readonly string labelCreateOrigin = (Localization.Tr("Create Origin"));
            public static readonly string labelCreateManager = (Localization.Tr("Create or Update Manager"));
            public static readonly string labelMirrorBone = (Localization.Tr("Mirror SpringBones"));
            public static readonly string labelSelectOneAndChild = (Localization.Tr("Select this and child bones"));
            public static readonly string labelDeleteBone = (Localization.Tr("Delete SpringBone"));
            public static readonly string labelDeleteBoneAndManager = (Localization.Tr("Delete Managers and Bones of Selection and Children"));
            public static readonly string labelCollision = (Localization.Tr("Collision"));
            public static readonly string labelSphere = (Localization.Tr("Sphere"));
            public static readonly string labelCapsule = (Localization.Tr("Capsule"));
            public static readonly string labelQuad = (Localization.Tr("Quad"));
            public static readonly string labelFitCapsulePos = (Localization.Tr("Fit capsule place to parent"));
            public static readonly string labelExcludeCollisionFromBone = (Localization.Tr("Exclude collision from SpringBone"));
            public static readonly string labelDeleteCollier = (Localization.Tr("Delete collider of selection and children"));
            public static readonly string labelCleanup = (Localization.Tr("Cleanup"));
            public static readonly string labelShow = (Localization.Tr("Show"));
            
            public static readonly string labelShowOnlySelectedBones = (Localization.Tr("Show only selected bones"));
            public static readonly string labelShowBoneCollision = (Localization.Tr("Show bone collisions"));
            public static readonly string labelShowOnlySelectedCollider = (Localization.Tr("Show only selected colliders"));
            public static readonly string labelShowBoneName = (Localization.Tr("Show bone names"));
        }

        private const string kIconDirectoryPath = "Packages/com.unity.springbone/Editor/GUI/Icons"; 
        

        [MenuItem("Window/Animation/SpringBone/SpringBone")]
        public static void ShowWindow()
        {
            var window = GetWindow<SpringBoneWindow>(Styles.editorWindowTitle);
            window.OnShow();
        }

        // private

        private GUIElements.Column mainUI;
        private Vector2 scrollPosition;

        private Texture headerIcon;
        private Texture newDocumentIcon;
        private Texture openDocumentIcon;
        private Texture saveDocumentIcon;
        private Texture deleteIcon;
        private Texture pivotIcon;
        private Texture sphereIcon;
        private Texture capsuleIcon;
        private Texture panelIcon;

        private SpringBoneSettings settings;

        private static Texture LoadIcon(string iconDirectory, string filename)
        {
            var iconPath = PathUtil.CombinePath(iconDirectory, filename);
            var iconTexture = AssetDatabase.LoadAssetAtPath<Texture>(iconPath);
            if (iconTexture == null)
            {
                Debug.LogFormat(LogType.Warning, LogOption.None, null, Styles.logIconDirNotFound, iconPath);
            }
            return iconTexture;
        }

        private void InitializeIcons()
        {
            if (headerIcon != null) { return; }

            headerIcon = LoadIcon(kIconDirectoryPath, "SpringIcon.tga");
            newDocumentIcon = LoadIcon(kIconDirectoryPath, "NewDocumentHS.png");
            openDocumentIcon = LoadIcon(kIconDirectoryPath, "OpenHH.bmp");
            saveDocumentIcon = LoadIcon(kIconDirectoryPath, "SaveHH.bmp");
            deleteIcon = LoadIcon(kIconDirectoryPath, "Delete.png");
            pivotIcon = LoadIcon(kIconDirectoryPath, "Pivot.png");
            sphereIcon = LoadIcon(kIconDirectoryPath, "SpringSphereIcon.tga");
            capsuleIcon = LoadIcon(kIconDirectoryPath, "SpringCapsuleIcon.tga");
            panelIcon = LoadIcon(kIconDirectoryPath, "SpringPanelIcon.tga");
        }

        private void InitializeButtonGroups()
        {
            if (mainUI != null) { return; }

            const float BigButtonHeight = 60f;

            System.Func<GUIStyle> headerLabelStyleProvider = () => SpringBoneGUIStyles.HeaderLabelStyle;
            System.Func<GUIStyle> buttonLabelStyleProvider = () => SpringBoneGUIStyles.MiddleLeftJustifiedLabelStyle;

            mainUI = new GUIElements.Column(new GUIElements.IElement[]
            {
                new GUIElements.Column(new GUIElements.IElement[]
                {
                    new GUIElements.Label(Styles.labelDynamicCSV, headerLabelStyleProvider),
                    new GUIElements.Row(new GUIElements.IElement[]
                    {
                        new GUIElements.Button(Styles.labelLoad, LoadSpringBoneSetupWindow.ShowWindow, openDocumentIcon, buttonLabelStyleProvider),
                        new GUIElements.Button(Styles.labelSave, SaveSpringBoneSetupWindow.ShowWindow, saveDocumentIcon, buttonLabelStyleProvider)
                    },
                    BigButtonHeight)
                }),

                new GUIElements.Column(new GUIElements.IElement[]
                {
                    new GUIElements.Label(Styles.labelSpringBone, headerLabelStyleProvider),
                    new GUIElements.Row(new GUIElements.IElement[]
                    {
                        new GUIElements.Button(Styles.labelSpringBoneAdd, SpringBoneEditorActions.AssignSpringBonesRecursively, headerIcon, buttonLabelStyleProvider),
                        new GUIElements.Button(Styles.labelCreateOrigin, SpringBoneEditorActions.CreatePivotForSpringBones, pivotIcon, buttonLabelStyleProvider)
                    },
                    BigButtonHeight),
                    new GUIElements.Button(Styles.labelCreateManager, SpringBoneEditorActions.AddToOrUpdateSpringManagerInSelection, newDocumentIcon, buttonLabelStyleProvider),
                    //new GUIElements.Button("初期セットアップを行う", SpringBoneAutoSetupWindow.ShowWindow, newDocumentIcon, buttonLabelStyleProvider),
                    //new GUIElements.Button("初期ボーンリストに合わせる", SpringBoneEditorActions.PromptToUpdateSpringBonesFromList, null, buttonLabelStyleProvider),
                    new GUIElements.Separator(),
                    new GUIElements.Button(Styles.labelMirrorBone, MirrorSpringBoneWindow.ShowWindow, null, buttonLabelStyleProvider),
                    new GUIElements.Button(Styles.labelSelectOneAndChild, SpringBoneEditorActions.SelectChildSpringBones, null, buttonLabelStyleProvider),
                    new GUIElements.Button(Styles.labelDeleteBone, SpringBoneEditorActions.DeleteSelectedBones, deleteIcon, buttonLabelStyleProvider),
                    new GUIElements.Button(Styles.labelDeleteBoneAndManager, SpringBoneEditorActions.DeleteSpringBonesAndManagers, deleteIcon, buttonLabelStyleProvider),
                }),

                new GUIElements.Column(new GUIElements.IElement[]
                {
                    new GUIElements.Label(Styles.labelCollision, headerLabelStyleProvider),
                    new GUIElements.Row(new GUIElements.IElement[]
                    {
                        new GUIElements.Button(Styles.labelSphere, SpringColliderEditorActions.CreateSphereColliderBeneathSelectedObjects, sphereIcon, buttonLabelStyleProvider),
                        new GUIElements.Button(Styles.labelCapsule, SpringColliderEditorActions.CreateCapsuleColliderBeneathSelectedObjects, capsuleIcon, buttonLabelStyleProvider),
                        new GUIElements.Button(Styles.labelQuad, SpringColliderEditorActions.CreatePanelColliderBeneathSelectedObjects, panelIcon, buttonLabelStyleProvider),
                    },
                    BigButtonHeight),
                    new GUIElements.Button(Styles.labelFitCapsulePos, SpringColliderEditorActions.AlignSelectedCapsulesToParents, capsuleIcon, buttonLabelStyleProvider),
                    new GUIElements.Button(Styles.labelExcludeCollisionFromBone, SpringColliderEditorActions.DeleteCollidersFromSelectedSpringBones, deleteIcon, buttonLabelStyleProvider),
                    new GUIElements.Button(Styles.labelDeleteCollier, SpringColliderEditorActions.DeleteAllChildCollidersFromSelection, deleteIcon, buttonLabelStyleProvider),
                    new GUIElements.Button(Styles.labelCleanup, SpringColliderEditorActions.CleanUpDynamics, deleteIcon, buttonLabelStyleProvider)
                })
            },
            false,
            0f);
        }

        private Rect GetScrollContentsRect()
        {
            const int ScrollbarWidth = 24;
            var width = position.width - GUIElements.Spacing - ScrollbarWidth;
            var height = mainUI.Height;
            return new Rect(0f, 0f, width, height);
        }

        private void OnGUI()
        {
            if (settings == null) { LoadSettings(); }

            SpringBoneGUIStyles.ReacquireStyles();
            InitializeIcons();
            InitializeButtonGroups();

            var xPos = GUIElements.Spacing;
            var yPos = GUIElements.Spacing;
            var scrollContentsRect = GetScrollContentsRect();
            yPos = ShowHeaderUI(xPos, yPos, scrollContentsRect.width);
            var scrollViewRect = new Rect(0f, yPos, position.width, position.height - yPos);
            scrollPosition = GUI.BeginScrollView(scrollViewRect, scrollPosition, scrollContentsRect);
            mainUI.DoUI(GUIElements.Spacing, 0f, scrollContentsRect.width);
            GUI.EndScrollView();

            ApplySettings();
        }

        private static void DrawHeaderIcon
        (
            ref Rect containerRect,
            Texture iconTexture,
            int iconDrawSize,
            int spacing = 4
        )
        {
            if (iconTexture != null
                && containerRect.width >= iconDrawSize * 3)
            {
                var iconYPosition = containerRect.y + (containerRect.height - iconDrawSize) / 2;
                var iconRect = new Rect(containerRect.x, iconYPosition, iconDrawSize, iconDrawSize);
                GUI.DrawTexture(iconRect, iconTexture);

                var xOffset = iconDrawSize + spacing;
                containerRect.x += xOffset;
                containerRect.width -= xOffset;
            }
        }

        private float ShowHeaderUI(float xPos, float yPos, float uiWidth)
        {
            var needToRepaint = false;
            System.Func<GUIStyle> headerLabelStyleProvider = () => SpringBoneGUIStyles.HeaderLabelStyle;
            System.Func<GUIStyle> toggleStyleProvider = () => SpringBoneGUIStyles.ToggleStyle;
            var headerColumn = new GUIElements.Column(
                new GUIElements.IElement[] {
                    new GUIElements.Label(Styles.labelShow, headerLabelStyleProvider),
                    new GUIElements.Row(new GUIElements.IElement[]
                        {
                            new GUIElements.Toggle(Styles.labelShowOnlySelectedBones, () => settings.onlyShowSelectedBones, newValue => { settings.onlyShowSelectedBones = newValue; needToRepaint = true; }, toggleStyleProvider),
                            new GUIElements.Toggle(Styles.labelShowBoneCollision, () => settings.showBoneSpheres, newValue => { settings.showBoneSpheres = newValue; needToRepaint = true; }, toggleStyleProvider),
                        },
                        GUIElements.RowHeight),
                    new GUIElements.Row(new GUIElements.IElement[]
                        {
                            new GUIElements.Toggle(Styles.labelShowOnlySelectedCollider, () => settings.onlyShowSelectedColliders, newValue => { settings.onlyShowSelectedColliders = newValue; needToRepaint = true; }, toggleStyleProvider),
                            new GUIElements.Toggle(Styles.labelShowBoneName, () => settings.showBoneNames, newValue => { settings.showBoneNames = newValue; needToRepaint = true; }, toggleStyleProvider)
                        },
                        GUIElements.RowHeight),
                },
                true, 4f, 0f);
            headerColumn.DoUI(xPos, yPos, uiWidth);
            if (needToRepaint)
            {
                ApplySettings();
                SaveSettings();
                SceneView.RepaintAll();
            }

            return yPos + headerColumn.Height + GUIElements.Spacing;
        }

        private void ApplySettings()
        {
            SpringManager.onlyShowSelectedBones = settings.onlyShowSelectedBones;
            SpringManager.showBoneSpheres = settings.showBoneSpheres;
            SpringManager.onlyShowSelectedColliders = settings.onlyShowSelectedColliders;
            SpringManager.showBoneNames = settings.showBoneNames;
        }

#if false
        private static string GetSettingsFilePath()
        {
            const string SettingsFileName = "SpringBoneWindow.json";
            return ProjectPaths.GetUserPreferencesPath(SettingsFileName);
        }

        private void LoadSettings()
        {
            var settingPath = GetSettingsFilePath();
            if (System.IO.File.Exists(settingPath))
            {
                var settingText = FileUtil.ReadAllText(settingPath);
                if (settingText.Length > 0)
                {
                    settings = JsonUtility.FromJson<SpringBoneSettings>(settingText);
                }
            }
            if (settings == null)
            {
                settings = SpringBoneSettings.GetDefaultSettings();
            }
        }

        private void SaveSettings()
        {
            if (settings == null) { return; }
            var settingText = JsonUtility.ToJson(settings);
            FileUtil.WriteAllText(GetSettingsFilePath(), settingText);
        }

        private void OnDestroy()
        {
            SaveSettings();
        }
#else
        // Todo: Get a good settings path
        private void LoadSettings()
        {
            if (settings == null)
            {
                settings = SpringBoneSettings.GetDefaultSettings();
            }
        }

        private void SaveSettings()
        {
            // NYI
        }
#endif

        private void OnShow()
        {
            LoadSettings();
        }

        [System.Serializable]
        private class SpringBoneSettings
        {
            public bool onlyShowSelectedBones;
            public bool onlyShowSelectedColliders;
            public bool showBoneSpheres;
            public bool showBoneNames;

            public static SpringBoneSettings GetDefaultSettings()
            {
                return new SpringBoneSettings
                {
                    onlyShowSelectedBones = true,
                    onlyShowSelectedColliders = true,
                    showBoneSpheres = true,
                    showBoneNames = false
                };
            }
        }
    }
}