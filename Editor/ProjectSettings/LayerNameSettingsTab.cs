using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.IO;

namespace Unity.Animations.SpringBones {
	public class LayerNameSettingsTab {
        internal class Styles
        {
            public static readonly GUIContent collisionLayerLabel = EditorGUIUtility.TrTextContent("SpringBone Collision Layer");
        }
        
        private ReorderableList m_LayersList;
        private SerializedProperty m_Layers;

        public LayerNameSettingsTab() {
            Refresh();
        }

        private void Refresh()
        {
            var layerSettings = SpringBoneLayerSettings.GetSerializedSettings();
            m_Layers = layerSettings.FindProperty("m_layers");

            System.Diagnostics.Debug.Assert(m_Layers.arraySize ==  32);
            if (m_LayersList == null)
            {
                m_LayersList = new ReorderableList(layerSettings, m_Layers, 
                    false, false, 
                    false, false)
                {
                    drawElementCallback = DrawLayerListElement,
                    elementHeight = EditorGUIUtility.singleLineHeight + 2,
                    headerHeight = 3
                };
            }
        }

        private void DrawLayerListElement(Rect rect, int index, bool selected, bool focused)
        {
//            // nicer looking with selected list row and a text field in it
//            rect.yMin += 1;
//            rect.yMax -= 1;
//
//            // De-indent by the drag handle width, so the text field lines up with others in the inspector.
//            // Will have space in front of label for more space between it and the drag handle.
//            rect.xMin -= ReorderableList.Defaults.dragHandleWidth;

            var oldName = m_Layers.GetArrayElementAtIndex(index).stringValue;
            var newName = EditorGUI.TextField(rect, " Collision Layer " + index, oldName);

            if (newName != oldName)
            {
                m_Layers.GetArrayElementAtIndex(index).stringValue = newName;
                m_Layers.serializedObject.ApplyModifiedProperties();
            }
        }        
        
		public void OnGUI () {
            EditorGUILayout.LabelField (Styles.collisionLayerLabel);
            m_LayersList.DoLayoutList();
		}
	}
}
