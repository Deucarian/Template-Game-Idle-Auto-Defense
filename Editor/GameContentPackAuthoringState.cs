using System.Collections.Generic;
using UnityEngine;

namespace Deucarian.TemplateGameIdleAutoDefense.Editor
{
    public sealed class GameContentPackAuthoringState
    {
        public string PackId = "contentpack.example.basic-idle-auto-defense";
        public string DisplayName = "Basic Idle Auto Defense Pack";
        public string Description = "Packages playable authored idle auto-defense content for one-click scene setup.";
        public string Version = "0.1.0";
        public string Author = "Deucarian";
        public Sprite Icon;
        public Texture2D Banner;
        public GameContentSetAsset SelectedContentSet;
        public readonly List<GameContentSetAsset> ContentSets = new List<GameContentSetAsset>();
        public GameContentSetAsset DefaultContentSet;
        public string RequiredPackagesCsv = "com.deucarian.template.game.idle-auto-defense, com.deucarian.attacks, com.deucarian.weapon-systems, com.deucarian.run-upgrades, com.deucarian.game-content-authoring";
        public string MinimumVersionsCsv = string.Empty;
        public string CompatibilityNotes = "Validated with the Idle Auto Defense template package set.";
        public string TagsCsv = "template, content-pack, idle-auto-defense";
        public string OutputRoot = "Assets/GameContent/ContentPacks";
    }

    public sealed class GameContentPackSceneSetupState
    {
        public IdleAutoDefenseTemplateController Controller;
        public GameContentPackAsset ContentPack;
        public GameContentSetAsset SelectedContentSet;
        public string LastMessage = string.Empty;
    }
}
