using System.Collections.Generic;
using Deucarian.GameContentAuthoring.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Deucarian.TemplateGameIdleAutoDefense.Editor
{
    public static class GameContentPackSceneSetupUtility
    {
        public static IdleAutoDefenseTemplateController FindControllerInOpenScenes()
        {
            IdleAutoDefenseTemplateController[] controllers = Resources.FindObjectsOfTypeAll<IdleAutoDefenseTemplateController>();
            for (int i = 0; i < controllers.Length; i++)
            {
                IdleAutoDefenseTemplateController controller = controllers[i];
                if (controller == null || EditorUtility.IsPersistent(controller)) continue;
                if (controller.gameObject == null || !controller.gameObject.scene.IsValid() || !controller.gameObject.scene.isLoaded) continue;
                return controller;
            }

            return null;
        }

        public static GameContentAuthoringValidationResult Validate(IdleAutoDefenseTemplateController controller, GameContentPackAsset contentPack, GameContentSetAsset selectedContentSet)
        {
            var issues = new List<GameContentAuthoringValidationIssue>();
            if (controller == null)
                issues.Add(GameContentAuthoringValidationIssue.Error("Scene.Controller", "Choose an IdleAutoDefenseTemplateController in the open scene."));
            if (contentPack == null)
            {
                issues.Add(GameContentAuthoringValidationIssue.Error("ContentPack", "Choose a Content Pack asset before applying scene setup."));
                return new GameContentAuthoringValidationResult(issues);
            }

            GameContentPackResolution resolution = GameContentPackValidator.Resolve(contentPack, selectedContentSet);
            for (int i = 0; i < resolution.PackReport.Issues.Count; i++)
            {
                GameContentPackValidationIssue issue = resolution.PackReport.Issues[i];
                issues.Add(new GameContentAuthoringValidationIssue(ToSharedSeverity(issue.Severity), issue.Path, issue.Message));
            }

            if (resolution.ContentSetResolution != null)
            {
                for (int i = 0; i < resolution.ContentSetResolution.Report.Issues.Count; i++)
                {
                    GameContentSetValidationIssue issue = resolution.ContentSetResolution.Report.Issues[i];
                    GameContentAuthoringValidationSeverity severity = issue.IsError ? GameContentAuthoringValidationSeverity.Error : GameContentAuthoringValidationSeverity.Warning;
                    issues.Add(new GameContentAuthoringValidationIssue(severity, "ContentSet." + issue.Path, issue.Message));
                }
            }

            return new GameContentAuthoringValidationResult(issues);
        }

        public static string CreatePreviewSummary(GameContentPackAsset contentPack, GameContentSetAsset selectedContentSet)
        {
            if (contentPack == null) return "No content pack selected.";
            GameContentSetAsset contentSet = selectedContentSet != null ? selectedContentSet : contentPack.DefaultContentSet;
            return "Preview setup: " + contentPack.DisplayName + " will assign " + (contentSet == null ? "the pack default content set" : contentSet.DisplayName) + ". Scene is unchanged.";
        }

        public static GameContentCreationResult Apply(IdleAutoDefenseTemplateController controller, GameContentPackAsset contentPack, GameContentSetAsset selectedContentSet)
        {
            GameContentAuthoringValidationResult validation = Validate(controller, contentPack, selectedContentSet);
            if (!validation.IsValid)
                return new GameContentCreationResult(false, "Fix scene setup validation before applying the content pack.", controller);

            GameContentSetAsset contentSet = selectedContentSet != null ? selectedContentSet : contentPack.DefaultContentSet;
            Undo.RecordObject(controller, "Apply Deucarian Content Pack");
            SerializedObject serialized = new SerializedObject(controller);
            SerializedProperty packProperty = serialized.FindProperty("_contentPack");
            SerializedProperty setProperty = serialized.FindProperty("_contentSet");
            if (packProperty == null || setProperty == null)
                return new GameContentCreationResult(false, "Template controller fields are unavailable; package scripts may be out of date.", controller);

            packProperty.objectReferenceValue = contentPack;
            setProperty.objectReferenceValue = contentSet;
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(controller);
            if (controller.gameObject.scene.IsValid() && controller.gameObject.scene.isLoaded)
                EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
            return new GameContentCreationResult(true, "Applied content pack to scene controller: " + contentPack.DisplayName, controller);
        }

        private static GameContentAuthoringValidationSeverity ToSharedSeverity(GameContentPackValidationSeverity severity)
        {
            if (severity == GameContentPackValidationSeverity.Error) return GameContentAuthoringValidationSeverity.Error;
            if (severity == GameContentPackValidationSeverity.Info) return GameContentAuthoringValidationSeverity.Info;
            return GameContentAuthoringValidationSeverity.Warning;
        }
    }
}
