﻿using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Unity.Animations.SpringBones
{
    using SpringManagerButton = SpringManagerInspector.InspectorButton<SpringManager>;

    [CustomEditor(typeof(SpringManager))]
    [CanEditMultipleObjects]
    public class SpringManagerInspector : Editor
    {
        bool isEngLang => !EditorPrefs.GetBool("UCSB_JLM");

        public class InspectorButton<T>
        {
            public InspectorButton(string label, System.Action<T> onPress)
            {
                Label = label;
                OnPress = onPress;
            }

            public string Label { get; set; }
            public System.Action<T> OnPress { get; set; }

            public void Show(T target)
            {
                if (GUILayout.Button(Label)) { OnPress(target); }
            }
        }

        public override void OnInspectorGUI()
        {
            if (targets.Length == 1)
            {
                // Only show buttons if one component is selected
                if (actionButtons == null || actionButtons.Length == 0)
                {
                    actionButtons = new[] {
                        new SpringManagerButton(isEngLang ? "Display Spring Window" : "窓を表示", ShowSpringWindow),
                        new SpringManagerButton(isEngLang ? "Select All SpringBone" : "SpringBoneを全て選択", SelectAllBones),
                        new SpringManagerButton(isEngLang ? "Update SpringBone List" : "SpringBoneリストを更新", UpdateBoneList)
                    };
                }

                EditorGUILayout.Space();
                var manager = (SpringManager)target;
                for (int buttonIndex = 0; buttonIndex < actionButtons.Length; buttonIndex++)
                {
                    actionButtons[buttonIndex].Show(manager);
                }
                EditorGUILayout.Space();
                var boneCount = (manager.springBones != null) ? manager.springBones.Length : 0;
                GUILayout.Label("Bones: " + boneCount);
                EditorGUILayout.Space();
            }

            base.OnInspectorGUI();
        }

        // private

        private SpringManagerButton[] actionButtons;

        private static void ShowSpringWindow(SpringManager manager)
        {
            SpringBoneWindow.ShowWindow();
        }

        private static void SelectAllBones(SpringManager manager)
        {
            var bones = manager.GetComponentsInChildren<SpringBone>(true);
            Selection.objects = bones.Select(item => item.gameObject).ToArray();
        }

        private static void UpdateBoneList(SpringManager manager)
        {
            SpringBoneSetup.FindAndAssignSpringBones(manager, true);
        }
    }
}