using Unity.Animations.SpringBones.GameObjectExtensions;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEditor.Localization.Editor;

namespace Unity.Animations.SpringBones
{
    public static class SpringBoneEditorActions
    {
        private static class Styles
        {
            public static readonly string logStopPlayMode = L10n.Tr("You must stop Playmode first.");
            public static readonly string logSelectOneOrMoreObjects = L10n.Tr("Select one or more objects.");
            public static readonly string logSelectOnlyOneSpringManager = L10n.Tr("Select only one SpringManager");

            public static readonly string textDelete = L10n.Tr("Delete");
            public static readonly string textCancel = L10n.Tr("Cancel");
            public static readonly string textUpdate = L10n.Tr("Update");
            
            public static readonly string textDeleteSpringBoneAndManager = L10n.Tr("Delete SpringBone and Manager");
            public static readonly string textDeleteSelectedBones = L10n.Tr("Delete selected bones");
            
            public static readonly string textUpdateFromBoneList = L10n.Tr("Update from bone list");
            
            public static readonly string textConfirmRemoveAllBoneAndManagerFormat = L10n.Tr(
                "Do you really want to remove all\n" + 
                "SpringBones and managers under this object?\n" +
                "\n{0}");
            
            public static readonly string textConfirmUpdateBonesFromListFormat = L10n.Tr(
                "Do you want to update secondary bones from bone list?\n" +
                    "\nThis will remove all SpringBones that are not listed,\n" +
                    "and will add SpringBones missing in model.\n" +
                    "\nSpringManager: {0}\n");
        }

        public static void ShowSpringBoneWindow()
        {
            SpringBoneWindow.ShowWindow();
        }

        public static void AssignSpringBonesRecursively()
        {
            if (Application.isPlaying)
            {
                Debug.LogError(Styles.logStopPlayMode);
                return;
            }

            if (Selection.gameObjects.Length < 1)
            {
                Debug.LogError(Styles.logSelectOneOrMoreObjects);
                return;
            }

            var springManagers = new HashSet<SpringManager>();
            foreach (var gameObject in Selection.gameObjects)
            {
                SpringBoneSetup.AssignSpringBonesRecursively(gameObject.transform);
                var manager = gameObject.GetComponentInParent<SpringManager>();
                if (manager != null)
                {
                    springManagers.Add(manager);
                }
            }

            foreach (var manager in springManagers)
            {
                SpringBoneSetup.FindAndAssignSpringBones(manager, true);
            }

            AssetDatabase.Refresh();
        }

        public static void CreatePivotForSpringBones()
        {
            if (Application.isPlaying)
            {
                Debug.LogError(Styles.logStopPlayMode);
                return;
            }

            if (Selection.gameObjects.Length < 1)
            {
                Debug.LogError(Styles.logSelectOneOrMoreObjects);
                return;
            }

            var selectedSpringBones = Selection.gameObjects
                .Select(gameObject => gameObject.GetComponent<SpringBone>())
                .Where(bone => bone != null);
            foreach (var springBone in selectedSpringBones)
            {
                SpringBoneSetup.CreateSpringPivotNode(springBone);
            }
        }

        public static void AddToOrUpdateSpringManagerInSelection()
        {
            if (Application.isPlaying)
            {
                Debug.LogError(Styles.logStopPlayMode);
                return;
            }

            if (Selection.gameObjects.Length <= 0)
            {
                Debug.LogError(Styles.logSelectOneOrMoreObjects);
                return;
            }

            foreach (var gameObject in Selection.gameObjects)
            {
                var manager = gameObject.GetComponent<SpringManager>();
                if (manager == null) { manager = gameObject.AddComponent<SpringManager>(); }
                SpringBoneSetup.FindAndAssignSpringBones(manager, true);
            }
        }

        public static void SelectChildSpringBones()
        {
            var springBoneObjects = Selection.gameObjects
                .SelectMany(gameObject => gameObject.GetComponentsInChildren<SpringBone>(true))
                .Select(bone => bone.gameObject)
                .Distinct()
                .ToArray();
            Selection.objects = springBoneObjects;
        }

        public static void DeleteSpringBonesAndManagers()
        {
            if (Application.isPlaying)
            {
                Debug.LogError(Styles.logStopPlayMode);
                return;
            }

            if (Selection.gameObjects.Length != 1)
            {
                Debug.LogError(Styles.logSelectOneOrMoreObjects);
                return;
            }

            var rootObject = Selection.gameObjects.First();
            var queryMessage = string.Format(Styles.textConfirmRemoveAllBoneAndManagerFormat, rootObject.name);
            if (EditorUtility.DisplayDialog(
                Styles.textDeleteSpringBoneAndManager, queryMessage, Styles.textDelete, Styles.textCancel))
            {
                SpringBoneSetup.DestroySpringManagersAndBones(rootObject);
                AssetDatabase.Refresh();
            }
        }

        public static void DeleteSelectedBones()
        {
            var springBonesToDelete = GameObjectUtil.FindComponentsOfType<SpringBone>()
                .Where(bone => Selection.gameObjects.Contains(bone.gameObject))
                .ToArray();
            var springManagersToUpdate = GameObjectUtil.FindComponentsOfType<SpringManager>()
                .Where(manager => manager.springBones.Any(bone => springBonesToDelete.Contains(bone)))
                .ToArray();
            Undo.RecordObjects(springManagersToUpdate, Styles.textDeleteSelectedBones);
            foreach (var boneToDelete in springBonesToDelete)
            {
                Undo.DestroyObjectImmediate(boneToDelete);
            }
            foreach (var manager in springManagersToUpdate)
            {
                manager.FindSpringBones(true);
            }
        }

        public static void PromptToUpdateSpringBonesFromList()
        {
            if (Application.isPlaying)
            {
                Debug.LogError(Styles.logStopPlayMode);
                return;
            }

            var selectedSpringManagers = Selection.gameObjects
                .Select(gameObject => gameObject.GetComponent<SpringManager>())
                .Where(manager => manager != null)
                .ToArray();
            if (!selectedSpringManagers.Any())
            {
                selectedSpringManagers = GameObjectUtil.FindComponentsOfType<SpringManager>().ToArray();
            }

            if (selectedSpringManagers.Count() != 1)
            {
                Debug.LogError(Styles.logSelectOnlyOneSpringManager);
                return;
            }

            var springManager = selectedSpringManagers.First();
            var queryMessage = string.Format(Styles.textConfirmUpdateBonesFromListFormat, springManager.name);
             
            if (EditorUtility.DisplayDialog(Styles.textUpdateFromBoneList, queryMessage, Styles.textUpdate, Styles.textCancel))
            {
                AutoSpringBoneSetup.UpdateSpringManagerFromBoneList(springManager);
            }
        }
    }
}