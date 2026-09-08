using System;

namespace Deucarian.TemplateGameIdleAutoDefense
{
    /// <summary>Owns fixed-rate residual time; the run decides whether another tick is allowed.</summary>
    internal sealed class IdleAutoDefenseFrameClock
    {
        private readonly Func<float, bool> _step;
        private float _accumulator;

        internal IdleAutoDefenseFrameClock(Func<float, bool> step)
        {
            _step = step ?? throw new ArgumentNullException(nameof(step));
        }

        internal void Advance(float deltaSeconds, bool fixedRate, float secondsPerTick)
        {
            if (float.IsNaN(deltaSeconds) || float.IsInfinity(deltaSeconds))
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), "Frame time must be finite.");
            if (fixedRate && (secondsPerTick <= 0f || float.IsNaN(secondsPerTick) || float.IsInfinity(secondsPerTick)))
                throw new ArgumentOutOfRangeException(nameof(secondsPerTick), "Fixed tick duration must be finite and positive.");
            deltaSeconds = deltaSeconds <= 0f ? 1f / 60f : deltaSeconds;
            if (!fixedRate)
            {
                _step(deltaSeconds);
                return;
            }

            float accumulated = _accumulator + deltaSeconds;
            if (float.IsInfinity(accumulated) || accumulated - secondsPerTick == accumulated)
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), "Frame time exceeds fixed tick precision.");
            _accumulator = accumulated;
            float tolerance = Math.Min(0.000001f, secondsPerTick * 0.01f);
            while (_accumulator + tolerance >= secondsPerTick)
            {
                _accumulator -= secondsPerTick;
                if (!_step(secondsPerTick)) break;
            }
        }

        internal void Reset() => _accumulator = 0f;
    }
}
