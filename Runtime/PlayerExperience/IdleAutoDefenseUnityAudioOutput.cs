using Deucarian.Common;
using UnityEngine;

namespace Deucarian.TemplateGameIdleAutoDefense
{
    internal sealed class IdleAutoDefenseUnityAudioOutput : IIdleAutoDefenseAudioOutput
    {
        private AudioSource _source;

        internal IdleAutoDefenseUnityAudioOutput(GameObject host)
        {
            _source = host.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = 0f;
        }

        public void Play(AudioClip clip, float volume)
        {
            if (_source != null) _source.PlayOneShot(clip, volume);
        }

        public void Dispose()
        {
            if (_source == null) return;
            _source.Stop();
            UnityObjectUtility.DestroySafely(_source);
            _source = null;
        }
    }
}
