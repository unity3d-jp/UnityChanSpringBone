using System;
using System.ComponentModel;
using UnityEngine;
using UnityEditor;
using System.IO;

namespace Unity.Animations.SpringBones
{
    class SpringBoneLayerSettings : ScriptableObject
    {
        private const string k_LayerSettingDir = "Assets/UnitychanSpringBone/Editor/";
        private const string k_LayerSettingAsset = k_LayerSettingDir + "LayerSettings.asset";
        private const int kLayerSize = 32; //layer is in 32bit int

        private static string[] kDefaultLayerNames =  {
            "Head", 
            "Body", 
            "Left Arm",
            "Right Arm",
            "Left Leg",
            "Right Leg",
        };
        
        [SerializeField] private string[] m_layers;

        public string[] Layers => m_layers;

        internal static SpringBoneLayerSettings GetOrCreateSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<SpringBoneLayerSettings>(k_LayerSettingAsset);
            if (settings == null)
            {
                if (!Directory.Exists(k_LayerSettingDir)) {
                    Directory.CreateDirectory(k_LayerSettingDir);
                }
                
                settings = ScriptableObject.CreateInstance<SpringBoneLayerSettings>();
                settings.m_layers = new string[kLayerSize];
                Array.Copy(kDefaultLayerNames, settings.m_layers, kDefaultLayerNames.Length);
                AssetDatabase.CreateAsset(settings, k_LayerSettingAsset);
                AssetDatabase.SaveAssets();
            }

            return settings;
        }

        internal static SerializedObject GetSerializedSettings()
        {
            return new SerializedObject(GetOrCreateSettings());
        }

        public static string GetLayerName(int index)
        {
            return GetOrCreateSettings().m_layers[index];
        }
    }
}