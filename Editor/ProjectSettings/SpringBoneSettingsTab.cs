using UnityEditor;
using System.IO;
using UnityEngine;

namespace Unity.Animations.SpringBones
{
	public class SpringBoneSettingsTab {

        internal class Styles
        {
            public static readonly GUIContent todoLabel = EditorGUIUtility.TrTextContent("TODO: Implement this");
        }
        

        public SpringBoneSettingsTab() {
            Refresh();
        }

        private void Refresh()
        {
        }

		public void OnGUI ()
		{
			GUILayout.Label(Styles.todoLabel);

			GUILayout.Label("SpringBoneManager Default Values", "BoldLabel");
			GUILayout.Label("Default simulation frame rate");
			GUILayout.Label("Default dynamic ratio");
			GUILayout.Label("Default gravity");
			GUILayout.Label("Default friction");
			
			GUILayout.Label("SpringBone Default Values", "BoldLabel");
			GUILayout.Label("Default Stiffness");
			GUILayout.Label("Default air registance");
			GUILayout.Label("Default gravity");
			GUILayout.Label("Default air influence");
			
			GUILayout.Label("Default rotation stiffness");

			GUILayout.Label("Default Y angle limit");
			GUILayout.Label("Default Y angle min");
			GUILayout.Label("Default Y angle max");

			GUILayout.Label("Default Z angle limit");
			GUILayout.Label("Default Z angle min");
			GUILayout.Label("Default Z angle max");
		}
	}
}
