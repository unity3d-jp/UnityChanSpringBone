using UnityEngine;
using UnityEditor;
using System.Linq;

namespace Unity.Animations.SpringBones
{	
	class SpringBoneSettingsProvider : SettingsProvider
	{
		private static class Styles
		{
			internal const float kToolbarWidth = 300f;
			private static readonly GUIContent settingsTabLabel = EditorGUIUtility.TrTextContent("General Settings");
			private static readonly GUIContent layerNameTabLabel = EditorGUIUtility.TrTextContent("Layers", "Configure springBone layer settings");
			private static GUIContent[] s_tabs;
			internal static GUIContent[] Tabs
			{
				get { return s_tabs ?? (s_tabs = new[] {settingsTabLabel, layerNameTabLabel}); }
			}
		}

		private readonly SpringBoneSettingsTab m_settingsTab;
		private readonly LayerNameSettingsTab m_layerNameTab;
		private Mode m_mode;

		enum Mode : int {
			SpringBoneSettings,
			LayerNameSettings
		}
		
		public SpringBoneSettingsProvider(string path, SettingsScope scope = SettingsScope.Project)
			: base(path, scope)
		{
			m_settingsTab = new SpringBoneSettingsTab ();
			m_layerNameTab = new LayerNameSettingsTab ();
			m_mode = Mode.SpringBoneSettings;			
		}

		public override void OnGUI(string searchContext)
		{
			DrawToolBar ();

			switch (m_mode) {
				case Mode.SpringBoneSettings:
					m_settingsTab.OnGUI ();
					break;
				case Mode.LayerNameSettings:
					m_layerNameTab.OnGUI ();
					break;
			}
		}
		
		private void DrawToolBar() {
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			m_mode = (Mode)GUILayout.Toolbar((int)m_mode, Styles.Tabs, "LargeButton", GUILayout.Width(Styles.kToolbarWidth) );
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
			GUILayout.Space(8f);
		}
		

		// Register the SettingsProvider
		[SettingsProvider]
		public static SettingsProvider CreateSettingsProvider()
		{
			var provider = new SpringBoneSettingsProvider("Project/Spring Bone")
			{
				keywords = 
					GetSearchKeywordsFromGUIContentProperties<SpringBoneSettingsTab.Styles>()
					.Concat(GetSearchKeywordsFromGUIContentProperties<LayerNameSettingsTab.Styles>())
			};			

			// Automatically extract all keywords from the Styles.
			return provider;
		}
	}
}
