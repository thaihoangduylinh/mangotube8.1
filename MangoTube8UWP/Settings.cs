using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Storage;
using System.Collections.ObjectModel;
using System.IO;

namespace MangoTube8UWP
{
    public static class Settings
    {
        public const string InnerTubeApiKey = "AIzaSyAO_FJ2SlqU8Q4STEHLGCilw_Y9_11qcW8";

        private const string SeedVideoIdsKey = "SeedVideoIds";
        private const string VideoQualityKey = "VideoQuality";

        public static List<string> Qualities { get; } = new List<string> { "Medium (recommended)", "HD", "Auto"};
        private static List<string> seedVideoIds;

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