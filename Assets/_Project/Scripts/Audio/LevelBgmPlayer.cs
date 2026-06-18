using UnityEngine;

namespace CampusNightMarket.Audio
{
    // 关卡背景音乐播放器：读取配置并循环播放二维音乐。
    [DisallowMultipleComponent]
    public class LevelBgmPlayer : MonoBehaviour
    {
        private const string ConfigResourcePath = "Audio/LevelBgmConfig";

        [SerializeField] private LevelBgmConfig config;

        private AudioSource audioSource;

        // 开始或恢复关卡背景音乐；重复调用不会叠加播放。
        public void Play()
        {
            if (config == null)
            {
                config = Resources.Load<LevelBgmConfig>(ConfigResourcePath);
            }

            if (config == null || config.Clip == null)
            {
                Debug.LogWarning("未找到关卡BGM配置或音频资源。", this);
                return;
            }

            EnsureAudioSource();
            if (audioSource.isPlaying && audioSource.clip == config.Clip)
            {
                return;
            }

            audioSource.clip = config.Clip;
            audioSource.volume = Mathf.Clamp01(config.Volume);
            audioSource.loop = true;
            audioSource.Play();
        }

        private void EnsureAudioSource()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }

            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
            audioSource.dopplerLevel = 0f;
        }
    }
}
