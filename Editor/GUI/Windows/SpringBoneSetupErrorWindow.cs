using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEditor.Localization.Editor;

namespace Unity.Animations.SpringBones
{
    public class SpringBoneSetupErrorWindow : EditorWindow
    {
        private static class Styles
        {
            public static readonly string editorWindowTitle = L10n.Tr("Setup Dynamics");
            
            public static readonly GUIContent labelAskDynamicError = new GUIContent(L10n.Tr("There are errors in dynamics setup. Do you want to create only ones without error(s)?"));
            public static readonly GUIContent labelSpringBoneRoot = new GUIContent(L10n.Tr("SpringBone Root"));
            public static readonly GUIContent labelColliderRoot = new GUIContent(L10n.Tr("Collider Root"));
            public static readonly GUIContent labelPath = new GUIContent(L10n.Tr("Path"));
            public static readonly GUIContent labelCreate = new GUIContent(L10n.Tr("Create"));
            public static readonly GUIContent labelCancel = new GUIContent(L10n.Tr("Cancel"));
            public static readonly GUIContent labelError = new GUIContent(L10n.Tr("Error"));
        }

        public interface IConfirmAction
        {
            void Perform();
        }

        public static void ShowWindow
        (
            GameObject springBoneRoot,
            GameObject colliderRoot,
            string path, 
            IEnumerable<DynamicsSetup.ParseMessage> errors, 
            IConfirmAction onConfirm
        )
        {
            var window = GetWindow<SpringBoneSetupErrorWindow>(Styles.editorWindowTitle);
            window.springBoneRoot = springBoneRoot;
            window.colliderRoot = colliderRoot;
            window.filePath = path;
            window.onConfirmAction = onConfirm;
            window.errors = errors.ToArray();
        }

        // private

        private GameObject springBoneRoot;
        private GameObject colliderRoot;
        private string filePath;
        private IConfirmAction onConfirmAction;
        private DynamicsSetup.ParseMessage[] errors;
        private Vector2 scrollPosition;

        private void OnGUI()
        {
            EditorGUILayout.Space();
            GUILayout.Label(Styles.labelAskDynamicError);
            EditorGUILayout.Space();
            EditorGUILayout.ObjectField(Styles.labelSpringBoneRoot, springBoneRoot, typeof(GameObject), true);
            EditorGUILayout.ObjectField(Styles.labelColliderRoot, colliderRoot, typeof(GameObject), true);
            EditorGUILayout.TextField(Styles.labelPath, filePath);
            EditorGUILayout.Space();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(Styles.labelCreate))
            {
                onConfirmAction.Perform();
                Close();
            }
            if (GUILayout.Button(Styles.labelCancel)) { Close(); }
            GUILayout.EndHorizontal();
            EditorGUILayout.Space();

            GUILayout.Label(Styles.labelError);
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, true);
            foreach (var error in errors)
            {
                var errorString = error.Message;
                if (!string.IsNullOrEmpty(error.SourceLine))
                {
                    errorString += "\n" + error.SourceLine;
                }
                GUILayout.Label(errorString);
            }
            GUILayout.EndScrollView();
        }
    }
}