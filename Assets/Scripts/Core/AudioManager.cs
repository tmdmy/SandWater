using UnityEngine;

namespace SandWater.Core
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private AudioClip digClip;
        [SerializeField] private AudioClip raiseClip;
        [SerializeField] private AudioClip clearClip;
        [SerializeField] private AudioClip bgmClip;

        [SerializeField, Range(0f, 1f)] private float sfxCooldown = 0.12f;

        private float _nextSfxAllowedTime;

        private void Awake()
        {
            Instance = this;
            if (bgmSource != null && bgmClip != null)
            {
                bgmSource.clip = bgmClip;
                bgmSource.loop = true;
                bgmSource.Play();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void PlayDig() => PlayEditClip(digClip);
        public void PlayRaise() => PlayEditClip(raiseClip);

        public void PlayClear()
        {
            if (clearClip == null || sfxSource == null) return;
            sfxSource.PlayOneShot(clearClip);
        }

        private void PlayEditClip(AudioClip clip)
        {
            if (clip == null || sfxSource == null) return;
            if (Time.unscaledTime < _nextSfxAllowedTime) return;
            _nextSfxAllowedTime = Time.unscaledTime + sfxCooldown;
            sfxSource.PlayOneShot(clip);
        }
    }
}
