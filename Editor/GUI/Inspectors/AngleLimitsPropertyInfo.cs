using UnityEditor;
using UnityEngine;

namespace Unity.Animations.SpringBones
{
    namespace Inspector
    {
        public class AngleLimitPropertyInfo : PropertyInfo
        {
            bool isEngLang => !EditorPrefs.GetBool("UCSB_JLM");

            public AngleLimitPropertyInfo(string newName, string labelText)
                : base(newName, labelText)
            {
                minSlider = new FloatSlider(isEngLang ? "Minimum Limit" : "下限", 0f, -180f);
                maxSlider = new FloatSlider(isEngLang ? "Maximum Limit" : "上限", 0f, 180f);
            }

            public override void Show()
            {
                GUILayout.Space(14f);

                GUILayout.BeginVertical("box");

                var propertyIterator = serializedProperty.Copy();

                if (propertyIterator.NextVisible(true))
                {
                    EditorGUILayout.PropertyField(propertyIterator, label, true, null);
                }

                SerializedProperty minProperty = null;
                SerializedProperty maxProperty = null;
                if (propertyIterator.NextVisible(true))
                {
                    minProperty = propertyIterator.Copy();
                }

                if (propertyIterator.NextVisible(true))
                {
                    maxProperty = propertyIterator.Copy();
                }

                if (minProperty != null
                    && maxProperty != null)
                {
                    const float SubSpacing = 3f;
                    GUILayout.Space(SubSpacing);
                    var minChanged = minSlider.Show(minProperty);
                    GUILayout.Space(SubSpacing);
                    var maxChanged = maxSlider.Show(maxProperty);
                    GUILayout.Space(SubSpacing);
                    GUILayout.BeginHorizontal();

                    updateValuesTogether = GUILayout.Toggle(updateValuesTogether, isEngLang ? "Sync Limit" : "同時に変更");
                    if (updateValuesTogether)
                    {
                        if (minChanged)
                        {
                            maxProperty.floatValue = -minProperty.floatValue;
                        }
                        else if (maxChanged)
                        {
                            minProperty.floatValue = -maxProperty.floatValue;
                        }
                    }

                    if (GUILayout.Button(isEngLang ? "Minimum" : "下限に統一"))
                    {
                        maxProperty.floatValue = -minProperty.floatValue;
                    }

                    if (GUILayout.Button(isEngLang ? "Maximum" : "上限に統一"))
                    {
                        minProperty.floatValue = -maxProperty.floatValue;
                    }

                    if (GUILayout.Button(isEngLang ? "Flip" : "反転"))
                    {
                        var minValue = minProperty.floatValue;
                        minProperty.floatValue = -maxProperty.floatValue;
                        maxProperty.floatValue = -minValue;
                    }

                    GUILayout.EndHorizontal();
                }

                GUILayout.EndVertical();
            }

            private FloatSlider minSlider;
            private FloatSlider maxSlider;
            private bool updateValuesTogether = false;
        }
    }
}