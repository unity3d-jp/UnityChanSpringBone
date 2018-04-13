using UTJ.GameObjectExtensions;
using UnityEditor;
using UnityEngine;

namespace UTJ
{
    [InitializeOnLoad]
    public static class SpringBoneEditorBackgroundTasks
    {
        static SpringBoneEditorBackgroundTasks()
        {
#if UNITY_2017_1_OR_NEWER
            EditorApplication.playModeStateChanged += PlaymodeStateChanged;
#else
            EditorApplication.playmodeStateChanged += PlaymodeStateChanged;
#endif
        }

        private static bool wasPreviouslyPlaying;

#if UNITY_2017_1_OR_NEWER
        private static void PlaymodeStateChanged(PlayModeStateChange state)
#else
        private static void PlaymodeStateChanged()
#endif
        {
            if (wasPreviouslyPlaying && !EditorApplication.isPlaying)
            {
                // end play complete
                AutoLoadDynamics();
            }
            wasPreviouslyPlaying = EditorApplication.isPlaying;
        }

        private static void AutoLoadDynamics()
        {
            var springManagers = GameObjectUtil.FindComponentsOfType<SpringManager>();
            foreach (var springManager in springManagers)
            {
                var name = springManager.name;
                SpringBoneSetup.AutoLoad(springManager);
                System.IO.File.Delete(SpringBoneSetup.GetAutoSavePath(name));
            }
        }
    }
}