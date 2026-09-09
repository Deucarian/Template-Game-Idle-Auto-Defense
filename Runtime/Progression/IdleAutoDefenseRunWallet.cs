using System;

namespace Deucarian.TemplateGameIdleAutoDefense
{
    /// <summary>Owns spendable run credits and passive-income residual ticks.</summary>
    internal sealed class IdleAutoDefenseRunWallet
    {
        private int _passiveIncomeTicks;
        internal long Balance { get; private set; }
        internal long Earned { get; private set; }
        internal long Spent { get; private set; }

        internal void Reset()
        {
            Balance = 0;
            Earned = 0;
            Spent = 0;
            _passiveIncomeTicks = 0;
        }

        internal void SetStartingBalance(long value) => Balance = value;

        internal long AwardKills(int kills, long authoredCredits, double rewardMultiplier)
        {
            if (kills <= 0) return 0;
            long authoredBase = authoredCredits > 0L ? authoredCredits : kills * 5L;
            long earned = Math.Max(kills, (long)Math.Ceiling(authoredBase * (1d + rewardMultiplier)));
            Balance += earned;
            Earned += earned;
            return earned;
        }

        internal void GrantPassiveIncome(int ticks, IdleAutoDefenseEconomyAsset economy)
        {
            _passiveIncomeTicks += Math.Max(1, ticks);
            int intervalTicks = economy == null ? 60 : Math.Max(1, economy.PassiveIncomeIntervalTicks);
            if (_passiveIncomeTicks < intervalTicks) return;
            int intervals = _passiveIncomeTicks / intervalTicks;
            _passiveIncomeTicks %= intervalTicks;
            long amount = economy == null ? 1L : Math.Max(0L, economy.PassiveIncomeAmount);
            long earned = intervals * amount;
            Balance += earned;
            Earned += earned;
        }

        internal bool CanSpend(int cost, bool running) => running && cost > 0 && Balance >= cost;

        internal bool TrySpend(int cost, bool running)
        {
            if (!CanSpend(cost, running)) return false;
            Balance -= cost;
            Spent += cost;
            return true;
        }
    }
}
