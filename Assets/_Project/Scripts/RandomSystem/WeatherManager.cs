using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace CampusNightMarket.RandomSystem
{
    [Serializable]
    public class WeatherData
    {
        public string weatherId;
        public string weatherName;
        public float trafficModifier = 1f;
        public float incomeModifier = 1f;
        public int weight = 10;
        public string description;

        public WeatherData()
        {
        }

        public WeatherData(
            string weatherId,
            string weatherName,
            float trafficModifier,
            float incomeModifier,
            int weight,
            string description)
        {
            this.weatherId = weatherId;
            this.weatherName = weatherName;
            this.trafficModifier = trafficModifier;
            this.incomeModifier = incomeModifier;
            this.weight = weight;
            this.description = description;
        }
    }

    public class WeatherManager : MonoBehaviour
    {
        [Header("天气配置")]
        [SerializeField] private List<WeatherData> weatherPresets = new List<WeatherData>
        {
            new WeatherData("WEATHER_SUNNY", "晴天", 1.00f, 1.00f, 40, "天气晴朗，客流正常。"),
            new WeatherData("WEATHER_CLOUDY", "多云", 0.90f, 1.00f, 25, "多云，客流略微下降。"),
            new WeatherData("WEATHER_RAINY", "小雨", 0.70f, 0.90f, 20, "小雨影响出行，收入也会受影响。"),
            new WeatherData("WEATHER_STORM", "暴雨", 0.40f, 0.70f, 5, "暴雨显著压低夜市客流。"),
            new WeatherData("WEATHER_HOT", "炎热", 0.85f, 1.10f, 10, "天气炎热，客流略低但消费力更强。")
        };

        private WeatherData currentWeather;

        public event Action<string> WeatherChanged;

        public WeatherData CurrentWeather
        {
            get { return currentWeather; }
        }

        public string CurrentWeatherId
        {
            get { return currentWeather != null ? currentWeather.weatherId : "WEATHER_SUNNY"; }
        }

        public WeatherData RefreshWeather(int currentDay)
        {
            if (weatherPresets == null || weatherPresets.Count == 0)
            {
                Debug.LogWarning("WeatherManager: No weather presets configured.");
                currentWeather = null;
                return null;
            }

            int totalWeight = 0;
            for (int i = 0; i < weatherPresets.Count; i++)
            {
                if (weatherPresets[i] != null && weatherPresets[i].weight > 0)
                {
                    totalWeight += weatherPresets[i].weight;
                }
            }

            if (totalWeight <= 0)
            {
                currentWeather = weatherPresets[0];
                WeatherChanged?.Invoke(CurrentWeatherId);
                return currentWeather;
            }

            int randomValue = Random.Range(0, totalWeight);
            int cumulativeWeight = 0;
            for (int i = 0; i < weatherPresets.Count; i++)
            {
                WeatherData preset = weatherPresets[i];
                if (preset == null || preset.weight <= 0)
                {
                    continue;
                }

                cumulativeWeight += preset.weight;
                if (randomValue < cumulativeWeight)
                {
                    currentWeather = preset;
                    break;
                }
            }

            if (currentWeather == null)
            {
                currentWeather = weatherPresets[0];
            }

            Debug.Log(
                $"WeatherManager: Day {currentDay} weather = {currentWeather.weatherName}, " +
                $"traffic={currentWeather.trafficModifier}, income={currentWeather.incomeModifier}");
            WeatherChanged?.Invoke(currentWeather.weatherId);
            return currentWeather;
        }

        public float GetTrafficModifier()
        {
            return currentWeather != null ? currentWeather.trafficModifier : 1f;
        }

        public float GetIncomeModifier()
        {
            return currentWeather != null ? currentWeather.incomeModifier : 1f;
        }

        public void ForceWeather(string weatherId)
        {
            if (string.IsNullOrEmpty(weatherId) || weatherPresets == null)
            {
                return;
            }

            for (int i = 0; i < weatherPresets.Count; i++)
            {
                WeatherData preset = weatherPresets[i];
                if (preset != null && preset.weatherId == weatherId)
                {
                    currentWeather = preset;
                    WeatherChanged?.Invoke(currentWeather.weatherId);
                    Debug.Log($"WeatherManager: Weather forced to {currentWeather.weatherName}");
                    return;
                }
            }

            Debug.LogWarning($"WeatherManager: Weather preset '{weatherId}' not found.");
        }

        public void AddWeatherPreset(WeatherData preset)
        {
            if (preset != null)
            {
                weatherPresets.Add(preset);
            }
        }
    }
}
