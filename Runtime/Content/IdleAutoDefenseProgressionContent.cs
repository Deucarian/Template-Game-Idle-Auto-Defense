using System;
using Deucarian.IdleProgression;
using Deucarian.Progression;
using static Deucarian.TemplateGameIdleAutoDefense.BasicIdleAutoDefenseGame;

namespace Deucarian.TemplateGameIdleAutoDefense
{
    // Owns starter account, offline production and encounter-completion rewards.
    internal static class IdleAutoDefenseProgressionContent
    {
        internal static IdleProgressionDefinition CreateOfflineProgressionDefinition()
        {
            return new IdleProgressionDefinition(
                TimeSpan.FromHours(8),
                new[] { new IdleProductionRate(Credits, 0.35d) },
                new[] { new IdleCycleReward(Parts, new ProgressionAmount(1), TimeSpan.FromMinutes(4)) });
        }

        internal static ProgressionCatalog CreateProgressionCatalog()
        {
            return new ProgressionCatalog(
                new[]
                {
                    new CurrencyDefinition(Credits, new ProgressionAmount(250_000)),
                    new CurrencyDefinition(Parts, new ProgressionAmount(25_000))
                },
                new[]
                {
                    new ProgressionTrackDefinition(AccountXp, 0, new[] { new ProgressionAmount(100), new ProgressionAmount(250), new ProgressionAmount(500), new ProgressionAmount(900) })
                },
                new[]
                {
                    new ResearchNodeDefinition(CorePlatingResearch, 3, new[] { Debit(Credits, 25), Debit(Credits, 75), Debit(Credits, 160) }),
                    new ResearchNodeDefinition(PulseCapacitorResearch, 2, new[] { Debit(Parts, 2), Debit(Parts, 5) }, requiredUnlocks: new[] { PulseCannonUnlock }),
                    new ResearchNodeDefinition(ShardLoaderResearch, 2, new[] { Debit(Parts, 2), Debit(Parts, 5) }, requiredUnlocks: new[] { ShardLauncherUnlock }),
                    new ResearchNodeDefinition(OfflineRoutingResearch, 2, new[] { Debit(Credits, 40), Debit(Credits, 120) }, new[] { new ResearchPrerequisite(CorePlatingResearch, 1) })
                });
        }

        internal static RewardBundle CreateEncounterCompletionReward()
        {
            return new RewardBundle(
                new[]
                {
                    new CurrencyLine(Credits, new ProgressionAmount(60), true),
                    new CurrencyLine(Parts, new ProgressionAmount(3), true)
                },
                new[] { new XpGrant(AccountXp, new ProgressionAmount(35)) },
                new[] { StarterUnlock, Stage2Unlock, PulseCannonUnlock, ShardLauncherUnlock });
        }

        private static CurrencyLine Debit(CurrencyId currencyId, long amount)
        {
            return new CurrencyLine(currencyId, new ProgressionAmount(amount), false);
        }
    }
}
