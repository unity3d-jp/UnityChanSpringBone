#if UNITY_2020_1_OR_NEWER
using Localization = UnityEditor.LocalizationAttribute;
#else
using Localization = UnityEditor.Localization.Editor.LocalizationAttribute;
#endif
 
[assembly: Localization]