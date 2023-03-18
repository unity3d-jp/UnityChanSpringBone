using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEditor.Localization.Editor;

namespace Unity.Animations.SpringBones
{
    public class LoadSpringBoneSetupWindow : EditorWindow
    {
        private static class Styles
        {
            public static readonly string editorWindowTitle = L10n.Tr("Load spring bone setup");
            public static readonly string stopPlayModeMessage = L10n.Tr("Do not setup in Play Mode");
            public static readonly string selectObjectRootsMessage = L10n.Tr("Select parent object of the spring bone");
            public static readonly string resultFormat = L10n.Tr("Set up complete:{0}\nNumber of bones: {1} Number of colliders: {2}");
            public static readonly string csvFile = L10n.Tr("CSV File");
            public static readonly string textFile = L10n.Tr("Text File");
            public static readonly string loadSpringBoneSetup = L10n.Tr("Load spring bone setup");
            public static readonly string errorFormat = L10n.Tr(
                "SpringBone setup failed.\n"
                + "Souce data may contain errors,\n"
                + "or the data don't match the character.\n"
                + "Please refer console logs for further info.\n"
                + "\n"
                + "Character: {0}\n"
                + "\n"
                + "Path: {1}");

            public static readonly string springBoneSetup = L10n.Tr("SpringBone Setup");
            public static readonly string springBoneSetupFailedFormat = L10n.Tr("SpringBone Setup failed:{0}\nPath:{1}");
            public static readonly string labelSpringBoneRoot = L10n.Tr("SpringBone Root");
            
            public static readonly GUIContent labelLoadingConfig = new GUIContent(L10n.Tr("Loading Configuration"));
            public static readonly GUIContent labelSpringBone = new GUIContent(L10n.Tr("SpringBone"));
            public static readonly GUIContent labelCollider = new GUIContent(L10n.Tr("Collider"));
            
            public static readonly GUIContent labelSelectFromRoot = new GUIContent(L10n.Tr("Get root from selection"));
            public static readonly GUIContent labelSetupLoadCSV = new GUIContent(L10n.Tr("Set up from CSV file"));
        }

        public static void ShowWindow()
        {
            var editorWindow = GetWindow<LoadSpringBoneSetupWindow>(Styles.editorWindowTitle);
            if (editorWindow != null)
            {
                editorWindow.SelectObjectsFromSelection();
            }
        }

        public static T DoObjectPicker<T>
        (
            string label,
            T currentObject,
            int uiWidth,
            int uiHeight,
            ref int yPos
        ) where T : UnityEngine.Object
        {
            var uiRect = new Rect(UISpacing, yPos, LabelWidth, uiHeight);
            GUI.Label(uiRect, label, SpringBoneGUIStyles.LabelStyle);
            uiRect.x = LabelWidth + UISpacing;
            uiRect.width = uiWidth - uiRect.x + UISpacing;
            yPos += uiHeight + UISpacing;
            return EditorGUI.ObjectField(uiRect, currentObject, typeof(T), true) as T;
        }

        // private

        private const int UIRowHeight = 24;
        private const int UISpacing = 8;
        private const int LabelWidth = 200;

        private GameObject springBoneRoot;
        private DynamicsSetup.ImportSettings importSettings;

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

        private void ShowImportSettingsUI(ref Rect uiRect)
        {
            if (importSettings == null)
            {
                importSettings = new DynamicsSetup.ImportSettings();
            }

            GUI.Label(uiRect, Styles.labelLoadingConfig, SpringBoneGUIStyles.HeaderLabelStyle);
            uiRect.y += uiRect.height;
            importSettings.ImportSpringBones = GUI.Toggle(uiRect, importSettings.ImportSpringBones, Styles.labelSpringBone, SpringBoneGUIStyles.ToggleStyle);
            uiRect.y += uiRect.height;
            importSettings.ImportCollision = GUI.Toggle(uiRect, importSettings.ImportCollision, Styles.labelCollider, SpringBoneGUIStyles.ToggleStyle);
            uiRect.y += uiRect.height;
        }

        private void OnGUI()
        {
            SpringBoneGUIStyles.ReacquireStyles();

            const int ButtonHeight = 30;

            var uiWidth = (int)position.width - UISpacing * 2;
            var yPos = UISpacing;
            springBoneRoot = DoObjectPicker(Styles.labelSpringBoneRoot, springBoneRoot, uiWidth, UIRowHeight, ref yPos);
            var buttonRect = new Rect(UISpacing, yPos, uiWidth, ButtonHeight);
            if (GUI.Button(buttonRect, Styles.labelSelectFromRoot, SpringBoneGUIStyles.ButtonStyle))
            {
                SelectObjectsFromSelection();
            }
            yPos += ButtonHeight + UISpacing;
            buttonRect.y = yPos;

            ShowImportSettingsUI(ref buttonRect);

            string errorMessage;
            if (IsOkayToSetup(out errorMessage))
            {
                if (GUI.Button(buttonRect, Styles.labelSetupLoadCSV, SpringBoneGUIStyles.ButtonStyle))
                {
                    BrowseAndLoadSpringSetup();
                }
            }
            else
            {
                const int MessageHeight = 24;
                var uiRect = new Rect(UISpacing, buttonRect.y, uiWidth, MessageHeight);
                GUI.Label(uiRect, errorMessage, SpringBoneGUIStyles.HeaderLabelStyle);
            }
        }

        private bool IsOkayToSetup(out string errorMessage)
        {
            errorMessage = "";
            if (EditorApplication.isPlaying)
            {
                errorMessage = Styles.stopPlayModeMessage;
                return false;
            }

            if (springBoneRoot == null)
            {
                errorMessage = Styles.selectObjectRootsMessage;
                return false;
            }
            return true;
        }

        private static T FindHighestComponentInHierarchy<T>(GameObject startObject) where T : Component
        {
            T highestComponent = null;
            if (startObject != null)
            {
                var transform = startObject.transform;
                while (transform != null)
                {
                    var component = transform.GetComponent<T>();
                    if (component != null) { highestComponent = component; }
                    transform = transform.parent;
                }
            }
            return highestComponent;
        }

        private class BuildDynamicsAction : SpringBoneSetupErrorWindow.IConfirmAction
        {
            public BuildDynamicsAction
            (
                DynamicsSetup newSetup,
                string newPath,
                GameObject newSpringBoneRoot
            )
            {
                setup = newSetup;
                path = newPath;
                springBoneRoot = newSpringBoneRoot;
            }

            public void Perform()
            {
                setup.Build();
                AssetDatabase.Refresh();

                var boneCount = springBoneRoot.GetComponentsInChildren<SpringBone>(true).Length;
                var colliderCount = SpringColliderSetup.GetColliderTypes()
                    .Sum(type => springBoneRoot.GetComponentsInChildren(type, true).Length);
                var resultMessage = string.Format(Styles.resultFormat, path, boneCount, colliderCount);
                Debug.Log(resultMessage);
            }

            private DynamicsSetup setup;
            private string path;
            private GameObject springBoneRoot;
        }

        private void BrowseAndLoadSpringSetup()
        {
            if (!IsOkayToSetup(out var checkErrorMessage))
            {
                Debug.LogError(checkErrorMessage);
                return;
            }

            // var initialPath = "";
            var initialDirectory = ""; // System.IO.Path.GetDirectoryName(initialPath);
            var fileFilters = new string[] { Styles.csvFile, "csv", Styles.textFile, "txt" };
            var path = EditorUtility.OpenFilePanelWithFilters(
                Styles.loadSpringBoneSetup, initialDirectory, fileFilters);
            if (path.Length == 0) { return; }

            var sourceText = FileUtil.ReadAllText(path);
            if (string.IsNullOrEmpty(sourceText)) { return; }

            var parsedSetup = DynamicsSetup.ParseFromRecordText(springBoneRoot, springBoneRoot, sourceText, importSettings);
            if (parsedSetup.Setup != null)
            {
                var buildAction = new BuildDynamicsAction(parsedSetup.Setup, path, springBoneRoot);
                if (parsedSetup.HasErrors)
                {
                    SpringBoneSetupErrorWindow.ShowWindow(springBoneRoot, springBoneRoot, path, parsedSetup.Errors, buildAction);
                }
                else
                {
                    buildAction.Perform();
                }
            }
            else
            {
                var resultErrorMessage = string.Format(Styles.errorFormat, springBoneRoot.name, path);
                EditorUtility.DisplayDialog(Styles.springBoneSetup, resultErrorMessage, "OK");
                Debug.LogFormat(LogType.Error, LogOption.None, springBoneRoot, 
                    Styles.springBoneSetupFailedFormat, springBoneRoot.name, path);
            }
            Close();
        }
    }
}