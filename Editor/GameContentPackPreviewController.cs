using Deucarian.GameContentAuthoring.Editor;
using UnityEditor;
using UnityEngine;

namespace Deucarian.TemplateGameIdleAutoDefense.Editor
{
    internal sealed class GameContentPackPreviewController
    {
        private string _status = "Preview idle";

        public void Draw(GameContentAuthoringPreviewContext context, GameContentPackAuthoringState state, GameContentPackSceneSetupState setup)
        {
            if (context == null) return;
            context.SetStatus(_status);

            GameContentPackAsset preview = GameContentPackAssetCreator.BuildTransient(state);
            GameContentPackValidationReport report;
            GameContentPackDependencySummary dependencies;
            try
            {
                report = GameContentPackValidator.Validate(preview);
                dependencies = GameContentPackValidator.CollectDependencies(preview);
            }
            finally
            {
                GameContentPackAssetCreator.DestroyTransient(preview);
            }

            context.DrawCard("Content Pack Preview", () =>
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (context.DrawPrimaryButton("Preview Pack", true, GUILayout.Height(26f)))
                        SetStatus(context, GameContentPackAuthoringPreviewSummaries.PreviewStatus(state, report));
                    if (context.DrawSecondaryButton("Stop Preview", true, GUILayout.Width(104f), GUILayout.Height(26f)))
                        Stop(context);
                }

                context.DrawStatus(_status);
            });

            context.DrawCard("Ready To Install", () =>
            {
                context.DrawSummaryRows(GameContentPackAuthoringPreviewSummaries.BuildReadinessRows(state, report));
            });

            context.DrawCard("Pack Summary", () =>
            {
                context.DrawSummaryRows(GameContentPackAuthoringPreviewSummaries.BuildSummaryRows(state, dependencies));
            });

            context.DrawCard("Dependency Graph", () =>
            {
                context.DrawSummaryRows(GameContentPackAuthoringPreviewSummaries.BuildDependencyRows(state));
            });

            context.DrawCard("Scene Setup", () =>
            {
                context.DrawSummaryRows(GameContentPackAuthoringPreviewSummaries.BuildSceneSetupRows(setup));
            });

            context.DrawObjectPreview(state == null ? null : state.Banner, "Banner", "Optional banner/reference image is not assigned.");
            context.DrawWarnings(GameContentPackAuthoringPreviewSummaries.BuildWarnings(report));
        }

        public void Stop()
        {
            _status = "Preview stopped";
        }

        private void Stop(GameContentAuthoringPreviewContext context)
        {
            Stop();
            context.SetStatus(_status);
        }

        private void SetStatus(GameContentAuthoringPreviewContext context, string status)
        {
            _status = string.IsNullOrWhiteSpace(status) ? "Preview idle" : status;
            context.SetStatus(_status);
        }
    }
}
