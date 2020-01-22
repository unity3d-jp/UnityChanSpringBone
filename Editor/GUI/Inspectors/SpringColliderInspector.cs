using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Unity.Animations.SpringBones.Jobs
{
    using Inspector;

    // https://docs.unity3d.com/ScriptReference/Editor.html

    [CustomEditor(typeof(SpringCollider))]
    [CanEditMultipleObjects]
    public class SpringColliderInspector : Editor
    {
        private static class Styles
        {
            public static readonly GUIContent layerLabel = EditorGUIUtility.TrTextContent("Spring Collision Layer", "Collision Layer");
            public static readonly GUIContent shapeLabel = EditorGUIUtility.TrTextContent("Collider Shape", "Collider Shape of this Collider");
            public static readonly GUIContent radiusLabel = EditorGUIUtility.TrTextContent("Radius", "Radius of this Collider");
            public static readonly GUIContent heightLabel = EditorGUIUtility.TrTextContent("Height", "Height of this Collider");
            public static readonly GUIContent widthLabel = EditorGUIUtility.TrTextContent("Width", "Width of this Collider");
        }

        private SerializedProperty m_propLayer;
        private SerializedProperty m_propType;
        private SerializedProperty m_propRadius;
        private SerializedProperty m_propWidth;
        private SerializedProperty m_propHeight;
        private SpringBoneLayerSettings m_layerSettings;
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var newSelectedIndex = EditorGUILayout.Popup(Styles.layerLabel, m_propLayer.intValue, m_layerSettings.Layers);
            if (m_propLayer.intValue != newSelectedIndex)
            {
                m_propLayer.intValue = newSelectedIndex;
            }

            EditorGUILayout.PropertyField(m_propType, Styles.shapeLabel);

            switch ((ColliderType)m_propType.intValue)
            {
                case ColliderType.Sphere:
                    EditorGUILayout.PropertyField(m_propRadius, Styles.radiusLabel);
                    break;
                case ColliderType.Capsule:
                    EditorGUILayout.PropertyField(m_propRadius, Styles.radiusLabel);
                    EditorGUILayout.PropertyField(m_propHeight, Styles.heightLabel);
                    break;
                case ColliderType.Panel:
                    EditorGUILayout.PropertyField(m_propWidth, Styles.widthLabel);
                    EditorGUILayout.PropertyField(m_propHeight, Styles.heightLabel);
                    break;
            }

            if (m_propRadius.floatValue < 0f)
            {
                m_propRadius.floatValue = 0f;
            }
            if (m_propHeight.floatValue < 0f)
            {
                m_propHeight.floatValue = 0f;
            }
            if (m_propWidth.floatValue < 0f)
            {
                m_propWidth.floatValue = 0f;
            }
            
            serializedObject.ApplyModifiedProperties();
        }

        private void OnEnable()
        {
            m_layerSettings = SpringBoneLayerSettings.GetOrCreateSettings();
            
            m_propLayer = serializedObject.FindProperty("layer");
            m_propType = serializedObject.FindProperty("type");
            m_propRadius = serializedObject.FindProperty("radius");
            m_propWidth = serializedObject.FindProperty("width");
            m_propHeight = serializedObject.FindProperty("height");
        }
    }
}