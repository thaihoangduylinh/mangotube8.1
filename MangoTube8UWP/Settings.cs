using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Storage;
using System.Collections.ObjectModel;
using System.IO;
using static MangoTube8UWP.YouTubeModal;

namespace MangoTube8UWP
{
    public static class Settings
    {
        public const string InnerTubeApiKey = "AIzaSyAO_FJ2SlqU8Q4STEHLGCilw_Y9_11qcW8";

        private const string SeedVideoIdsKey = "SeedVideoIds";
        private const string VideoQualityKey = "VideoQuality";

        public static List<string> Qualities { get; } = new List<string> { "medium (recommended)", "SD", "low", "ultra low", "HD1080", "HD", "Audio", "auto"};
        private static List<string> seedVideoIds;

        private const string WatchHistoryKey = "WatchHistory";

        private static List<WatchHistoryItem> watchHistory;

        public static IReadOnlyList<WatchHistoryItem> WatchHistory
        {
            get
            {
                if (watchHistory == null)
                {
                    LoadWatchHistory().Wait();
                }
                return new ReadOnlyCollection<WatchHistoryItem>(watchHistory);
            }
        }

        public static async void AddToWatchHistory(WatchHistoryItem item)
        {
            if (watchHistory == null)
            {
                await LoadWatchHistory();
            }

            watchHistory.Insert(0, item);

            await SaveWatchHistory();
        }

        public static async void ClearWatchHistory()
        {
            watchHistory = new List<WatchHistoryItem>();
            await SaveWatchHistory();
        }

        private static async Task LoadWatchHistory()
        {
            watchHistory = new List<WatchHistoryItem>();

            try
            {
                var localFolder = ApplicationData.Current.LocalFolder;
                StorageFile file = null;

                try
                {
                    file = await localFolder.GetFileAsync(WatchHistoryKey);
                }
                catch (FileNotFoundException)
                {
                    file = null;
                }

                if (file != null)
                {
                    string historyData = await FileIO.ReadTextAsync(file);
                    if (!string.IsNullOrEmpty(historyData))
                    {
                        watchHistory = Newtonsoft.Json.JsonConvert.DeserializeObject<List<WatchHistoryItem>>(historyData);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error loading watch history: " + ex.Message);
            }
        }

        private static async Task SaveWatchHistory()
        {
            try
            {
                var localFolder = ApplicationData.Current.LocalFolder;
                var file = await localFolder.CreateFileAsync(WatchHistoryKey, CreationCollisionOption.ReplaceExisting);

                string historyData = Newtonsoft.Json.JsonConvert.SerializeObject(watchHistory);
                await FileIO.WriteTextAsync(file, historyData);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error saving watch history: " + ex.Message);
            }
        }

        private static readonly List<string> DefaultSeedVideoIds = new List<string>
        {
            "4NxiAMLjKWs",
            "vfbmLQmMPl4",
            "Yb8g1dtEuUA",
            "1HdibCYa9FI",
            "AUJFAx9rsVc"
        };

        public static string VideoQuality
        {
            get
            {
                var settings = ApplicationData.Current.LocalSettings.Values;
                string quality = settings.ContainsKey(VideoQualityKey)
                    ? settings[VideoQualityKey] as string
                    : Qualities[0];

                Debug.WriteLine("Retrieved video quality: " + quality);
                return quality;
            }
            set
            {
                var settings = ApplicationData.Current.LocalSettings.Values;
                Debug.WriteLine("Setting video quality: " + value);
                settings[VideoQualityKey] = value;
            }
        }

        public static IReadOnlyList<string> SeedVideoIds
        {
            get
            {
                if (seedVideoIds == null)
                {
                    LoadSeedVideoIds().Wait();
                }
                return new ReadOnlyCollection<string>(seedVideoIds);
            }
        }

        public static async void AddSeedVideoId(string videoId)
        {
            if (seedVideoIds == null)
            {
                await LoadSeedVideoIds();
            }

            if (seedVideoIds.Count >= 5)
            {
                seedVideoIds.RemoveAt(0);
            }

            seedVideoIds.Add(videoId);
            await SaveSeedVideoIds();
        }

        public static async void RemoveSeedVideoId(string videoId)
        {
            if (seedVideoIds == null)
            {
                await LoadSeedVideoIds();
            }

            if (seedVideoIds.Contains(videoId))
            {
                seedVideoIds.Remove(videoId);
                await SaveSeedVideoIds();
            }
        }

        public static async Task LoadSeedVideoIds()
        {
            seedVideoIds = new List<string>();

            try
            {
                var localFolder = ApplicationData.Current.LocalFolder;
                StorageFile file = null;

                try
                {
                    file = await localFolder.GetFileAsync(SeedVideoIdsKey);
                }
                catch (FileNotFoundException)
                {

                    file = null;
                }

                if (file != null)
                {
                    string seedData = await FileIO.ReadTextAsync(file);
                    seedVideoIds = string.IsNullOrEmpty(seedData)
                        ? new List<string>(DefaultSeedVideoIds)
                        : seedData.Split(',').ToList();
                }
                else
                {
                    seedVideoIds = new List<string>(DefaultSeedVideoIds);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error loading seed video IDs: " + ex.Message);
                seedVideoIds = new List<string>(DefaultSeedVideoIds);
            }
        }

        private static async Task SaveSeedVideoIds()
        {
            try
            {
                var localFolder = ApplicationData.Current.LocalFolder;
                var file = await localFolder.CreateFileAsync(SeedVideoIdsKey, CreationCollisionOption.ReplaceExisting);

                string seedData = string.Join(",", seedVideoIds);
                await FileIO.WriteTextAsync(file, seedData);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error saving seed video IDs: " + ex.Message);
            }
        }

        public static string GetRandomSeedVideoId()
        {
            if (seedVideoIds == null || seedVideoIds.Count == 0)
            {
                return null;
            }

            Random random = new Random();
            int randomIndex = random.Next(seedVideoIds.Count);
            return seedVideoIds[randomIndex];
        }
    }
}