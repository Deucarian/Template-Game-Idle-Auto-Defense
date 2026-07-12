using Deucarian.TemplateGameIdleAutoDefense;
using UnityEngine;

namespace Deucarian.TemplateGameIdleAutoDefense.Samples
{
    public sealed class BasicIdleAutoDefenseGameBootstrap : IdleAutoDefensePlayerExperienceController
    {
        [SerializeField] private GameContentPackAsset _templateContentPack;
        [SerializeField] private GameContentSetAsset _templateContentSet;
        [SerializeField] private IdleAutoDefensePlayerExperienceAsset _templatePlayerExperience;

        protected override void ConfigurePlayerExperienceBeforeBuild()
        {
            RequireAuthoredContentOnStartup();
            ConfigureContentPack(_templateContentPack, _templateContentSet);
            ConfigurePlayerExperience(_templatePlayerExperience);
        }
    }
}
