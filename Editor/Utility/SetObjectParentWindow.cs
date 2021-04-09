using System.Linq;
using UnityEditor;
using UnityEngine;
#if UNITY_2020_2_OR_NEWER
using Localization = UnityEditor.L10n;
#else
using Localization = UnityEditor.Localization.Editor.Localization;
#endif

namespace Unity.Animations.SpringBones
{
    public class SetObjectParentWindow : EditorWindow
    {
        private static class Styles
        {
            public static readonly string editorWindowTitle = Localization.Tr("Parenting Tool");

            public static readonly GUIContent labelNewParent = new GUIContent(Localization.Tr("New Parent"));
            public static readonly GUIContent labelSetParent = new GUIContent(Localization.Tr("Set Parent"));
        }


        [MenuItem("Window/Animation/SpringBone/Parenting Tool")]
        public static void ShowWindow()
        {
            GetWindow<SetObjectParentWindow>(Styles.editorWindowTitle);
        }

        // private

        private Transform newParent;

        private void ReparentSelectedObjects()
        {
            var newChildren = Selection.gameObjects
                .Where(item => item.transform != newParent)
                .Select(gameObject => gameObject.transform)
                .ToArray();
            foreach (var child in newChildren)
            {
                Undo.SetTransformParent(child, newParent, "Set Parent");
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.Space();
            newParent = EditorGUILayout.ObjectField(Styles.labelNewParent, newParent, typeof(Transform), true) as Transform;
            EditorGUILayout.Space();
            if (GUILayout.Button(Styles.labelSetParent))
            {
                ReparentSelectedObjects();
            }
        }
   }
}