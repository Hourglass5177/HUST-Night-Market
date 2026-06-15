using System;
using System.Collections.Generic;
using CampusNightMarket.Common;
using UnityEngine;
using Random = UnityEngine.Random;

namespace CampusNightMarket.RandomSystem
{
    /// <summary>
    /// 单个天气的静态配置数据。
    /// </summary>
    [Serializable]
    public class WeatherData
    {
        /// <summary>天气唯一ID，如 WEATHER_SUNNY。</summary>
        public string weatherId;

        /// <summary>天气显示名称，如 "晴天"。</summary>
        public string weatherName;

        /// <summary>客流倍率（晴天=1.0，雨天=0.7，暴风雨=0.4）。</summary>
        public float trafficModifier = 1f;

        /// <summary>收入倍率（好天气可额外加成）。</summary>
        public float incomeModifier = 1f;

        /// <summary>出现权重，权重越高越容易出现。</summary>
        public int weight = 10;

        /// <summary>天气描述文本。</summary>
        public string description;

        public WeatherData()
        {
        }

        public WeatherData(string weatherId, string weatherName, float trafficModifier, float incomeModifier, int weight, string description)
        {
            this.weatherId = weatherId;
            this.weatherName = weatherName;
            this.trafficModifier = trafficModifier;
            this.incomeModifier = incomeModifier;
            this.weight = weight;
            this.description = description;
        }
    }

    /// <summary>
    /// 天气系统：负责每天刷新天气，以及提供天气对客流和收入的倍率影响。
    /// </summary>
    public class WeatherManager : MonoBehaviour
    {
        [Header("天气配置")]
        [SerializeField] private List<WeatherData> weatherPresets = new List<WeatherData>
        {
            new WeatherData("WEATHER_SUNNY",   "晴天",    1.0f, 1.0f, 40, "阳光明媚，客流量正常。"),
            new WeatherData("WEATHER_CLOUDY",  "多云",    0.9f, 1.0f, 25, "多云天气，客流稍减。"),
            new WeatherData("WEATHER_RAINY",   "小雨",    0.7f, 0.9f, 20, "下着小雨，出行不便。"),
            new WeatherData("WEATHER_STORM",   "暴风雨",  0.4f, 0.7f, 5,  "狂风暴雨，很少有人出门。"),
            new WeatherData("WEATHER_HOT",     "炎热",    0.8f, 1.1f, 10, "天气炎热，夜市更受欢迎。")
        };

        // 当前天气
        private WeatherData currentWeather;

        /// <summary>当天开始时触发的事件，参数为当前天气ID。</summary>
        public event Action<string> WeatherChanged;

        /// <summary>当前天气数据（只读）。</summary>
        public WeatherData CurrentWeather
        {
            get { return currentWeather; }
        }

        /// <summary>当前天气ID。</summary>
        public string CurrentWeatherId
        {
            get { return currentWeather != null ? currentWeather.weatherId : "WEATHER_SUNNY"; }
        }

        /// <summary>
        /// 每天开始时调用，刷新当日天气。
        /// 按权重随机选取一种天气。
        /// </summary>
        public WeatherData RefreshWeather(int currentDay)
        {
            if (weatherPresets == null || weatherPresets.Count == 0)
            {
                Debug.LogWarning("WeatherManager: No weather presets configured.");
                return null;
            }

            // 按权重计算总权重
            int totalWeight = 0;
            for (int i = 0; i < weatherPresets.Count; i++)
            {
                if (weatherPresets[i] != null)
                {
                    totalWeight += weatherPresets[i].weight;
                }
            }

            if (totalWeight <= 0)
            {
                currentWeather = weatherPresets[0];
                return currentWeather;
            }

            // 随机抽取
            int randomValue = Random.Range(0, totalWeight);
            int cumulativeWeight = 0;

            for (int i = 0; i < weatherPresets.Count; i++)
            {
                if (weatherPresets[i] == null)
                {
                    continue;
                }

                cumulativeWeight += weatherPresets[i].weight;
                if (randomValue < cumulativeWeight)
                {
                    currentWeather = weatherPresets[i];
                    break;
                }
            }

            // 保底
            if (currentWeather == null)
            {
                currentWeather = weatherPresets[0];
            }

            Debug.Log($"WeatherManager: Day {currentDay} weather = {currentWeather.weatherName} " +
                     $"(trafficMod={currentWeather.trafficModifier}, incomeMod={currentWeather.incomeModifier})");

            WeatherChanged?.Invoke(currentWeather.weatherId);
            return currentWeather;
        }

        /// <summary>
        /// 获取当前天气对客流的倍率。
        /// EconomicManager 结算夜晚收入时需调用此方法。
        /// </summary>
        public float GetTrafficModifier()
        {
            return currentWeather != null ? currentWeather.trafficModifier : 1f;
        }

        /// <summary>
        /// 获取当前天气对收入的倍率。
        /// EconomicManager 结算夜晚收入时需调用此方法。
        /// </summary>
        public float GetIncomeModifier()
        {
            return currentWeather != null ? currentWeather.incomeModifier : 1f;
        }

        /// <summary>
        /// 强制设置指定天气（供事件系统使用）。
        /// </summary>
        public void ForceWeather(string weatherId)
        {
            for (int i = 0; i < weatherPresets.Count; i++)
            {
                if (weatherPresets[i] != null && weatherPresets[i].weatherId == weatherId)
                {
                    currentWeather = weatherPresets[i];
                    WeatherChanged?.Invoke(currentWeather.weatherId);
                    Debug.Log($"WeatherManager: Weather forced to {currentWeather.weatherName}");
                    return;
                }
            }

            Debug.LogWarning($"WeatherManager: Weather preset '{weatherId}' not found.");
        }

        /// <summary>
        /// 添加自定义天气配置。
        /// </summary>
        public void AddWeatherPreset(WeatherData preset)
        {
            if (preset != null)
            {
                weatherPresets.Add(preset);
            }
        }
    }
}