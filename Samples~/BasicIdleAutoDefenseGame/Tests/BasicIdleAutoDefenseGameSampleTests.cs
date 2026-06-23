using NUnit.Framework;
using Deucarian.TemplateGameIdleAutoDefense;

namespace Deucarian.TemplateGameIdleAutoDefense.Samples.Tests
{
    public sealed class BasicIdleAutoDefenseGameSampleTests
    {
        [Test]
        public void BootstrapUsesTemplateController()
        {
            Assert.IsTrue(typeof(IdleAutoDefenseTemplateController).IsAssignableFrom(typeof(BasicIdleAutoDefenseGameBootstrap)));
        }

        [Test]
        public void SaveProgressionSmokePassesFromImportedSample()
        {
            IdleAutoDefenseTemplateCompositionSmokeResult result = IdleAutoDefenseTemplateSaveProgressionComposition.RunSmoke();
            Assert.IsTrue(result.Succeeded);
        }
    }
}
