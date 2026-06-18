using UnityEngine;

namespace CampusNightMarket.Audio
{
    // 关卡背景音乐配置：保存音频引用和播放音量。
    [CreateAssetMenu(
        fileName = "LevelBgmConfig",
        menuName = "Campus Night Market/Audio/Level BGM Config")]
    public class LevelBgmConfig : ScriptableObject
    {
        [SerializeField] private AudioClip clip;
        [SerializeField, Range(0f, 1f)] private float volume = 0.4f;

        public AudioClip Clip
        {
            get { return clip; }
        }

        public float Volume
        {
            get { return volume; }
        }
    }
}
