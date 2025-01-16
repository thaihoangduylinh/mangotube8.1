
using Microsoft.PlayerFramework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Data.Xml.Dom;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.Phone.UI.Input;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.Text;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using static MangoTube8UWP.YouTubeModal;
using System.Xml;
using System.Net;
using System.IO;
using Windows.UI.Xaml.Shapes;
using Windows.UI.Notifications;
using Windows.Storage;
using Windows.Networking.BackgroundTransfer;
using Windows.Storage.Pickers;
using Autofac.Core;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Background;

namespace MangoTube8UWP
{
    public partial class VideoPage : Page
    {

        private DispatcherTimer syncTimer;

        private HttpClient _httpClient;
        private bool isFullscreen = false;
        private bool isUsingSeparateAudio = false;
        private bool isUsingAuto = false;
        private DateTime _lastTapTime = DateTime.MinValue;
        private string continuationTokenNextComments = string.Empty;
        private const double DoubleTapThreshold = 500;
        private string localVideoId;
        private bool IsLoadedForPivotComments = false;
        private bool IsLoadedForPivotRealted = false;
        private bool IsLoadedForPivotDetails = false;
        private DateTime lastTappedTime = DateTime.MinValue;

        private static string videoURL;
        private static string audioURL;
        
        private static string Description;
        private static string Title;
        private static string Subs;
        private static string Views;
        private static string AurthorPFPURL;
        private static string ThumbnailURL;
        private static string Aurthor;
        private static string Date;

        private static string BrowserID;

        private FolderPicker picker;


        public VideoPage()
        {
            InitializeComponent();

            syncTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(250)
            };

            syncTimer.Tick += SyncAudioWithVideo;

            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/89.0.4389.82 Safari/537.36");

            StatusBar statusBar = StatusBar.GetForCurrentView();
            statusBar.HideAsync();

            var sos = Windows.Devices.Sensors.SimpleOrientationSensor.GetDefault();
            Debug.WriteLine(sos == null ? "No sensor" : sos.GetCurrentOrientation().ToString());

            AudioPlayer.IsMuted = true;

            LogOrientation();


            DisplayInformation.DisplayContentsInvalidated += DisplayContentsInvalidated;
            DisplayInformation.AutoRotationPreferences = DisplayOrientations.Portrait | DisplayOrientations.Landscape;

        }

        private void DisplayContentsInvalidated(object sender, object e)
        {

            LogOrientation();
        }

        private async void DownloadButton_Click(object sender, RoutedEventArgs e)
        {

            ShowToastNotification("Download Started", "Your download has begun, you'll get a notification when done...");

            if(isUsingSeparateAudio)
            {
                ShowMessage("Warning This Will Take A Long Time To Download (Blame YouTube)! Try Mediuim For MUCH Faster Downloads.");
            }

            await StartDownloadAsync(localVideoId, Title, videoURL, audioURL);

         
        }


        private void ShowToastNotification(string title, string message)
        {
            try
            {

                ToastTemplateType toastTemplate = ToastTemplateType.ToastText04;

                XmlDocument toastXml = ToastNotificationManager.GetTemplateContent(toastTemplate);

                XmlNodeList toastTextElements = toastXml.GetElementsByTagName("text");

                if (toastTextElements.Length >= 3)
                {

                    toastTextElements[0].AppendChild(toastXml.CreateTextNode(message));

                    toastTextElements[1].AppendChild(toastXml.CreateTextNode(""));

                }
                else
                {
                    Debug.WriteLine("Error: Not enough text elements in the template.");
                }

                IXmlNode toastNode = toastXml.SelectSingleNode("/toast");

                ((XmlElement)toastNode).SetAttribute("duration", "long");

                ToastNotification toast = new ToastNotification(toastXml);

                toast.ExpirationTime = DateTimeOffset.UtcNow.AddSeconds(3600);

                ToastNotificationManager.CreateToastNotifier().Show(toast);
            }
            catch (Exception ex)
            {

                Debug.WriteLine($"Error showing toast notification: {ex.Message}");
            }
        }

        private void RewindButton_Click(object sender, RoutedEventArgs e)
        {

            if (AudioPlayer.Position > TimeSpan.FromSeconds(10))
            {
                AudioPlayer.Position -= TimeSpan.FromSeconds(10);
            }
          
 
        }

        private void FastForwardButton_Click(object sender, RoutedEventArgs e)
        {

            if (AudioPlayer.Position + TimeSpan.FromSeconds(10) < AudioPlayer.NaturalDuration)
            {
                AudioPlayer.Position += TimeSpan.FromSeconds(10);
            }

        }


        private async Task StartDownloadAsync(string videoId, string title, string videoUrl, string audioUrl)
        {
            try
            {

                StorageFolder targetFolder = KnownFolders.VideosLibrary;

                StorageFolder mangoTubeFolder;
                try
                {
                    mangoTubeFolder = await targetFolder.GetFolderAsync("MangoTube");
                }
                catch (FileNotFoundException)
                {
                    mangoTubeFolder = await targetFolder.CreateFolderAsync("MangoTube", CreationCollisionOption.FailIfExists);
                    Debug.WriteLine("MangoTube folder created.");
                }

                StorageFile videoFile = await mangoTubeFolder.CreateFileAsync($"{videoId}_video.mp4", CreationCollisionOption.ReplaceExisting);

                Debug.WriteLine($"Starting video download for {videoId} from {videoUrl}...");
                Debug.WriteLine($"Audio URL: {audioUrl}");

                var videoDownloader = new BackgroundDownloader();
                var videoDownloadOperation = videoDownloader.CreateDownload(new Uri(videoUrl), videoFile);

                var videoProgress = new Progress<DownloadOperation>(operation =>
                {
                    var progress = operation.Progress;
                    Debug.WriteLine($"Video download progress: {progress.BytesReceived} bytes downloaded of {progress.TotalBytesToReceive} bytes.");
                });


                var videoDownloadTask = videoDownloadOperation.StartAsync().AsTask();

                StorageFile audioFile = null;
                Task audioDownloadTask = null;

                if (!string.IsNullOrEmpty(audioUrl))
                {

                    audioFile = await mangoTubeFolder.CreateFileAsync($"{videoId}_audio.mp4", CreationCollisionOption.ReplaceExisting);
                    Debug.WriteLine($"Starting audio download for {videoId} from {audioUrl}...");

                    var audioDownloader = new BackgroundDownloader();
                    var audioDownloadOperation = audioDownloader.CreateDownload(new Uri(audioUrl), audioFile);

                    var audioProgress = new Progress<DownloadOperation>(operation => LogDownloadProgress(operation, "Audio"));

                    audioDownloadTask = audioDownloadOperation.StartAsync().AsTask();
                }

                var tasks = audioDownloadTask != null
                    ? new Task[] { videoDownloadTask, audioDownloadTask }
                    : new Task[] { videoDownloadTask };

                await Task.WhenAll(tasks);

                if (videoDownloadTask.IsFaulted)
                {
                    Debug.WriteLine($"Error downloading video {videoId}: {videoDownloadTask.Exception?.InnerException?.Message}");
                }
                else
                {
                    Debug.WriteLine($"Video download completed for {videoId}. File saved to: {videoFile.Path}");
                    try
                    {
                        var properties = await videoFile.GetBasicPropertiesAsync();
                        Debug.WriteLine($"Video file size: {properties.Size / 1024.0:F2} KB");
                        await SaveDownloadMetadata(videoId, title, videoUrl, audioUrl, videoFile.Path, audioFile?.Path, Description, Subs, Views, AurthorPFPURL, ThumbnailURL, Aurthor);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error accessing video file properties: {ex.Message}");
                    }
                }

                if (audioDownloadTask != null)
                {
                    if (audioDownloadTask.IsFaulted)
                    {
                        Debug.WriteLine($"Error downloading audio {videoId}: {audioDownloadTask.Exception?.InnerException?.Message}");
                    }
                    else
                    {
                        Debug.WriteLine($"Audio download completed for {videoId}. File saved to: {audioFile.Path}");
                        try
                        {
                            var properties = await audioFile.GetBasicPropertiesAsync();
                            Debug.WriteLine($"Audio file size: {properties.Size / 1024.0:F2} KB");
                            await SaveDownloadMetadata(videoId, title, videoUrl, audioUrl, videoFile.Path, audioFile.Path, Description, Subs, Views, AurthorPFPURL, ThumbnailURL, Aurthor);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error accessing audio file properties: {ex.Message}");
                        }
                    }
                }
                else
                {

                    if (audioFile != null)
                    {
                        await audioFile.DeleteAsync();
                        Debug.WriteLine("Audio URL was null. Audio file deleted.");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during download: {ex.Message}");
            }
            finally
            {
                ShowToastNotification("MangoTube8.1", "Your Download Has Finished!");
            }
        }

        private void LogDownloadProgress(DownloadOperation downloadOperation, string fileType)
        {

            double percent = downloadOperation.Progress.BytesReceived * 100.0 / downloadOperation.Progress.TotalBytesToReceive;

            Debug.WriteLine($"{fileType} download progress: {percent}% - Bytes Received: {downloadOperation.Progress.BytesReceived} / Total Bytes: {downloadOperation.Progress.TotalBytesToReceive}");

            UpdateProgress(percent, fileType);
        }

        private void UpdateProgress(double percent, string fileType)
        {

            Debug.WriteLine($"{fileType} Progress: {percent}%");
        }

        private void UpdateProgress(BackgroundDownloadProgress progress)
        {
            if (progress.BytesReceived > 0)
            {

                double percent = (double)progress.BytesReceived / progress.TotalBytesToReceive * 100;

                if (percent >= 10 && percent < 50)
                {
                    ShowToastNotification("Stage 1: Downloading", "10% downloaded...");
                }
                else if (percent >= 50 && percent < 75)
                {
                    ShowToastNotification("Stage 2: Downloading", "50% downloaded...");
                }
                else if (percent >= 75 && percent < 100)
                {
                    ShowToastNotification("Stage 3: Almost Done", "75% downloaded...");
                }
                else if (percent == 100)
                {
                    ShowToastNotification("Download Complete", "100% download complete!");
                }
            }
        }

        private void Downloads_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            Frame.Navigate(typeof(DownloadsPage));
        }

        private void History_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            Frame.Navigate(typeof(WatchHistory));
        }

        private void Profile_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            string queryString = $"browseId={BrowserID}";
            Frame.Navigate(typeof(ChannelPage), queryString);
        }

        
        private async Task SaveDownloadMetadata(string videoId, string title, string videoUrl, string audioUrl,
                                           string videoFilePath, string audioFilePath, string description,
                                           string subs, string views, string authorPFPURL, string thumbnailURL, string aurthor)
        {
            try
            {

                StorageFile metadataFile = await ApplicationData.Current.LocalFolder.CreateFileAsync("downloads.json", CreationCollisionOption.OpenIfExists);

                string json = await FileIO.ReadTextAsync(metadataFile);

                var metadata = string.IsNullOrEmpty(json) ? new DownloadMetadata() : JsonConvert.DeserializeObject<DownloadMetadata>(json);

                var newDownload = new DownloadDetails
                {
                    VideoId = videoId,
                    Title = title,
                    AudioURL = audioUrl,
                    VideoURL = videoUrl,
                    VideoFilePath = videoFilePath,
                    AudioFilePath = audioFilePath,
                    Description = description,
                    Author = aurthor,
                    Subs = subs,
                    Views = views,
                    AuthorPFPURL = authorPFPURL,
                    Date = Date,
                    ThumbnailURL = thumbnailURL
                };

                metadata.Downloads.Add(newDownload);

                string updatedJson = JsonConvert.SerializeObject(metadata, Formatting.Indented);

                await FileIO.WriteTextAsync(metadataFile, updatedJson);

                Debug.WriteLine($"Download metadata saved successfully for Video ID: {videoId}");
                Debug.WriteLine($"Saved Metadata: {updatedJson}");
            }
            catch (Exception ex)
            {

                Debug.WriteLine($"Error saving download metadata: {ex.Message}");
            }
        }

        private async Task<List<DownloadDetails>> GetDownloadMetadataAsync()
        {
            try
            {

                StorageFile metadataFile = await ApplicationData.Current.LocalFolder.GetFileAsync("downloads.json");
                string json = await FileIO.ReadTextAsync(metadataFile);
                var metadata = JsonConvert.DeserializeObject<DownloadMetadata>(json);

                return metadata?.Downloads ?? new List<DownloadDetails>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error reading download metadata: {ex.Message}");
                return new List<DownloadDetails>();
            }
        }

        private void LogOrientation()
        {
            var currentOrientation = DisplayInformation.GetForCurrentView().CurrentOrientation;

            switch (currentOrientation)
            {
                case DisplayOrientations.Portrait:
                    SetPortraitMode();
                    DisplayInformation.AutoRotationPreferences = DisplayOrientations.Portrait | DisplayOrientations.Landscape;
                    AppBar.Visibility = Visibility.Visible;
                    Debug.WriteLine("Current Orientation: Portrait");
                    break;

                case DisplayOrientations.Landscape:        
                    SetLandscapeMode();
                    VideoPlayer.IsFullScreen = true;
                    AppBar.Visibility = Visibility.Collapsed;
                    Debug.WriteLine("Current Orientation: Landscape");
                    break;

                case DisplayOrientations.PortraitFlipped:
                    SetPortraitMode();
                    DisplayInformation.AutoRotationPreferences = DisplayOrientations.Portrait | DisplayOrientations.Landscape;
                    AppBar.Visibility = Visibility.Visible;
                    Debug.WriteLine("Current Orientation: Flipped Portrait");
                    break;

                case DisplayOrientations.LandscapeFlipped:
                    SetLandscapeMode();
                    AppBar.Visibility = Visibility.Collapsed;
                    VideoPlayer.IsFullScreen = true;
                    break;

                default:
                    Debug.WriteLine("Unknown Orientation");
                    break;
            }
        }

        private void VideoPlayer_MediaPlayerPlay(object sender, RoutedEventArgs e)
        {

            if (!AudioPlayer.CurrentState.Equals(MediaElementState.Playing))
            {
                AudioPlayer.Play();
                VideoPlayer.IsFullScreen = true;
            }
        }

        private void VideoPlayer_MediaPlayerPause(object sender, RoutedEventArgs e)
        {

            if (!AudioPlayer.CurrentState.Equals(MediaElementState.Paused))
            {
                AudioPlayer.Pause();
            }
        }

        protected override void OnNavigatedTo(Windows.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            HardwareButtons.BackPressed += HardwareButtons_BackPressed;

            BubbleLoadingAnimationPlayer.Begin();

            var queryString = e.Parameter as string;
            if (string.IsNullOrEmpty(queryString))
            {
                Debug.WriteLine("Query string is null or empty.");
                return;
            }

            var videoId = GetQueryStringParameter(queryString, "videoId");

            if (!string.IsNullOrEmpty(videoId))
            {
                Debug.WriteLine("Valid videoId received: " + videoId);
                localVideoId = videoId;

                PopulateVideoStream(Settings.VideoQuality);
            }
            else
            {
                Debug.WriteLine("videoId parameter is missing or invalid.");
            }
        }

        private string GetQueryStringParameter(string query, string key)
        {

            if (!string.IsNullOrEmpty(query) && query.StartsWith("?"))
            {
                query = query.Substring(1);
            }

            string[] queryParams = query.Split('&');

            foreach (string param in queryParams)
            {

                string[] keyValue = param.Split('=');

                if (keyValue.Length == 2 && keyValue[0] == key)
                {
                    return keyValue[1];
                }
            }

            return null;
        }


        private void SyncAudioWithVideo(object sender, object e)
        {
            if (isUsingSeparateAudio)
            {

                if (VideoPlayer.CurrentState == MediaElementState.Paused)
                {
                    AudioPlayer.IsMuted = true;
                    System.Diagnostics.Debug.WriteLine("Video is paused. Audio is muted.");
                    return;
                }
                else
                {
                    AudioPlayer.IsMuted = false;
                }

                if (VideoPlayer.CurrentState == MediaElementState.Buffering)
                {
                    AudioPlayer.IsMuted = true;
                    System.Diagnostics.Debug.WriteLine("Video is Buffering. Audio is muted.");
                    return;
                }
                else
                {
                    AudioPlayer.IsMuted = false;
                }

                double positionDifference = Math.Abs(VideoPlayer.Position.TotalSeconds - AudioPlayer.Position.TotalSeconds);

                if (positionDifference > 0.5)
                {
                    AudioPlayer.Position = VideoPlayer.Position;
                    System.Diagnostics.Debug.WriteLine($"Sync correction applied. Audio Player position adjusted to {AudioPlayer.Position} to match Video Player.");
                }

                if (AudioPlayer.CurrentState != MediaElementState.Playing)
                {
                    AudioPlayer.Play();
                    AudioPlayer.IsMuted = false;
                    System.Diagnostics.Debug.WriteLine("Audio Player started playing.");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Not using separate audio, skipping sync.");
            }
        }

        private void SyncAudioIfNeeded()
        {
            if (isUsingSeparateAudio)
            {
                AudioPlayer.Position = VideoPlayer.Position;
            }
        }

        private void LoadMedia(string videoSource, string audioSource = null)
        {
            Debug.WriteLine($"Loading media: Video Source = {videoSource}, Audio Source = {audioSource}");

            if (!string.IsNullOrEmpty(videoSource))
            {
                videoURL = videoSource; 
            }

            if (!string.IsNullOrEmpty(audioSource))
            {
                audioURL = audioSource; 
            }

            if (isUsingAuto)
            {
                Debug.WriteLine("Auto quality selected. Adding StreamingMediaPlugin...");

                Debug.WriteLine("sourcypoo " + videoSource);

                SM.Media.MediaPlayer.StreamingMediaPlugin asd = new SM.Media.MediaPlayer.StreamingMediaPlugin();
                SM.Media.MediaPlayer.StreamingMediaPlugin asdf = new SM.Media.MediaPlayer.StreamingMediaPlugin();

                Debug.WriteLine("Setting VideoPlayer Source with audioSource: " + (audioSource ?? "null"));

                VideoPlayer.Plugins.Add(asd);
                VideoPlayer.Source = new Uri(videoSource);
         
                AudioPlayer.Source = new Uri(audioSource);
     
                isUsingSeparateAudio = true;

                AppBar.Visibility = Visibility.Collapsed;

                return;
            }

            if (!isUsingAuto)
            {
                VideoPlayer.Source = new Uri(videoSource);
                Debug.WriteLine($"Video source set: {videoSource}");
            }

            if (audioSource != null && !isUsingAuto)
            {
                isUsingSeparateAudio = true;
                AudioPlayer.Source = new Uri(audioSource);
                Debug.WriteLine($"Audio source set: {audioSource}");
                VideoPlayer.IsVolumeVisible = false;
            }
            else
            {
                isUsingSeparateAudio = false;
                Debug.WriteLine("No audio source provided, using video audio.");
            }
        }



        private void MediaPlayer_IsFullScreenChanged(object sender, Windows.UI.Xaml.RoutedPropertyChangedEventArgs<bool> e)
        {
            Microsoft.PlayerFramework.MediaPlayer mp = (sender as Microsoft.PlayerFramework.MediaPlayer);
            mp.IsFullWindow = !mp.IsFullWindow;
        }

        private void SetLandscapeMode()
        {
            Windows.Graphics.Display.DisplayInformation.AutoRotationPreferences = Windows.Graphics.Display.DisplayOrientations.Landscape;

            Debug.WriteLine("Orientation: Landscape");
            
            Part2.Visibility = Visibility.Collapsed;
            Header.Visibility = Visibility.Collapsed;
      
            var visibleBounds = Windows.UI.ViewManagement.ApplicationView.GetForCurrentView().VisibleBounds;

            VideoPlayer.Width = visibleBounds.Width;
            VideoPlayer.Height = visibleBounds.Height;

            MainContent.Margin = new Windows.UI.Xaml.Thickness(0, 0, 0, 0);

            VideoPlayer.MinHeight = 0;
            VideoPlayer.MinWidth = 0;
            VideoPlayer.MaxHeight = double.PositiveInfinity;
            VideoPlayer.MaxWidth = double.PositiveInfinity;

            VideoPlayer.Stretch = Stretch.Fill;

            VideoPlayer.Visibility = Visibility.Visible;

            Debug.WriteLine("Video Player Set to Landscape Mode: Width = " + VideoPlayer.Width + ", Height = " + VideoPlayer.Height);
        }

        private void SetPortraitMode()
        {

            Windows.Graphics.Display.DisplayInformation.AutoRotationPreferences = Windows.Graphics.Display.DisplayOrientations.Portrait;

            Debug.WriteLine("Orientation: Portrait");

            Part2.Visibility = Visibility.Visible;
            Header.Visibility = Visibility.Visible;

            VideoPlayer.Width = double.NaN;
            VideoPlayer.Height = 250;

            MainContent.Margin = new Windows.UI.Xaml.Thickness(0, 70, 0, 0);

            VideoPlayer.Stretch = Stretch.UniformToFill;

            VideoPlayer.Visibility = Visibility.Visible;

            Debug.WriteLine("Video Player Set to Portrait Mode: MinHeight = " + VideoPlayer.MinHeight + ", MaxHeight = " + VideoPlayer.MaxHeight);
        }

        private void SearchTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                string searchText = SearchTextBox.Text;
                Debug.WriteLine("Search Text: " + searchText);

                Frame.Navigate(typeof(SearchPage), searchText);
            }
        }

        private void HideSearchBox_Completed(object sender, object e)
        {
            YouTubeLogo.Visibility = Windows.UI.Xaml.Visibility.Visible;
            AccountButton.Visibility = Windows.UI.Xaml.Visibility.Visible;
            SearchTextBox.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        }

        private void AccountButton_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("AccountButton clicked");
            if (DropDown.Visibility == Visibility.Collapsed)
            {
                Debug.WriteLine("DropDown is currently collapsed. Showing it now.");
                DropDown.Visibility = Visibility.Visible;
                ShowDropDown.Begin();
            }
            else
            {
                Debug.WriteLine("DropDown is currently visible. Hiding it now.");
                HideDropDown.Begin();
            }
        }

        private void YouTubeLogo_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {

            if ((DateTime.Now - lastTappedTime).TotalSeconds >= 5)
            {

                Frame.Navigate(typeof(MainPage));

                lastTappedTime = DateTime.Now;
            }
            else
            {

                Debug.WriteLine("You must wait before tapping again.");
            }
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            if (SearchTextBox.Visibility == Visibility.Collapsed)
            {
                ShowSearchBox.Begin();
            }
            else
            {
                HideSearchBox.Begin();
            }
        }

        private void HideSearchBox_Completed(object sender, EventArgs e)
        {
            YouTubeLogo.Visibility = Visibility.Visible;
            SearchTextBox.Visibility = Visibility.Collapsed;
        }

        private void ProgressSlider_ValueChanged(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {

            if (Math.Abs(VideoPlayer.Position.TotalSeconds - e.NewValue) > 0.1)
            {

                VideoPlayer.Position = TimeSpan.FromSeconds(e.NewValue);
            }
        }

        private void VideoPlayer_MediaOpened(object sender, RoutedEventArgs e)
        {
            if (isUsingSeparateAudio)
            {
                AudioPlayer.Position = VideoPlayer.Position;
                AudioPlayer.Play();
                syncTimer.Start();
            }
            else
            {
                syncTimer.Stop();
            }

        }

        private void AudioPlayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            if (isUsingSeparateAudio)
            {
                AudioPlayer.Stop();
                syncTimer.Stop();
            }
        }

        private Task<string> FetchVideoStreamsMain(string videoId)
        {
            return Task.Run(async () =>
            {
                try
                {
                    string videoUrl = $"https://www.youtube.com/youtubei/v1/player?key={Settings.InnerTubeApiKey}";
                    DateTime currentUtcDateTime = DateTime.UtcNow;
                    long signatureTimestamp = (long)(currentUtcDateTime - new DateTime(1970, 1, 1)).TotalSeconds;
                    var contextData = new
                    {
                        context = new
                        {
                            client = new
                            {
                                hl = "en",
                                gl = "US",
                                clientName = "ANDROID",
                                clientVersion = "19.44.39",
                                androidSdkVersion = 34,
                                mainAppWebInfo = new
                                {
                                    graftUrl = $"/watch?v={videoId}"
                                }
                            }
                        },
                        playbackContext = new
                        {
                            vis = 0,
                            lactMilliseconds = "1"
                        },
                        videoId = videoId,
                        racyCheckOk = true,
                        contentCheckOk = true
                    };
                    string jsonRequestBody = Newtonsoft.Json.JsonConvert.SerializeObject(contextData);
                    var content = new StringContent(jsonRequestBody, Encoding.UTF8, "application/json");
                    var requestHeaders = new Dictionary<string, string>
                    {
                        { "User-Agent", "com.google.android.youtube/19.44.39 (Linux; U; Android 14) gzip" },
                        { "Referer", "https://www.youtube.com/" },
                        { "Referrer-Policy", "strict-origin-when-cross-origin" }
                    };
                    var client = new HttpClient();
                    foreach (var header in requestHeaders)
                    {
                        client.DefaultRequestHeaders.Add(header.Key, header.Value);
                    }
                    HttpResponseMessage videoResponse = await client.PostAsync(videoUrl, content);
                    videoResponse.EnsureSuccessStatusCode();
                    string videoStreamJson = await videoResponse.Content.ReadAsStringAsync();
                    return videoStreamJson;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error fetching video stream: {ex.Message}");
                    return null;
                }
            });
        }

        private Task<string> FetchVideoStreamsBackup(string videoId)
        {
            return Task.Run(async () =>
            {
                try
                {

                    string videoUrl = $"https://www.youtube.com/youtubei/v1/player?key={Settings.InnerTubeApiKey}";

                    DateTime currentUtcDateTime = DateTime.UtcNow;
                    long signatureTimestamp = (long)(currentUtcDateTime - new DateTime(1970, 1, 1)).TotalSeconds;

                    var contextData = new
                    {
                        videoId = videoId,
                        context = new
                        {
                            client = new
                            {
                                hl = "en",
                                gl = "US",
                                clientName = "IOS",
                                clientVersion = "19.29.1",
                                deviceMake = "Apple",
                                deviceModel = "iPhone",
                                osName = "iOS",
                                userAgent = "com.google.ios.youtube/19.29.1 (iPhone16,2; U; CPU iOS 17_5_1 like Mac OS X;)",
                                osVersion = "17.5.1.21F90"
                            }
                        },
                        playbackContext = new
                        {
                            contentPlaybackContext = new
                            {
                                signatureTimestamp = signatureTimestamp
                            }
                        }
                    };

                    string jsonRequestBody = Newtonsoft.Json.JsonConvert.SerializeObject(contextData);
                    var content = new StringContent(jsonRequestBody, Encoding.UTF8, "application/json");

                    HttpResponseMessage videoResponse = await _httpClient.PostAsync(videoUrl, content);
                    videoResponse.EnsureSuccessStatusCode();
                    string videoStreamJson = await videoResponse.Content.ReadAsStringAsync();

                    return videoStreamJson;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error fetching video stream: {ex.Message}");
                    return null;
                }
            });
        }

        private async void PopulateVideoStream(string videoQuality)
        {
            try
            {

                string thumbnailUrl = $"https://img.youtube.com/vi/{localVideoId}/maxresdefault.jpg";
                var bitmapImage = new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri(thumbnailUrl, UriKind.Absolute));
                VideoPlayer.PosterSource = bitmapImage;

                string videoStreamJson = "";

                if (videoQuality == "Auto")
                {
                    videoStreamJson = await FetchVideoStreamsBackup(localVideoId);
                }
                else
                {
                    videoStreamJson = await FetchVideoStreamsMain(localVideoId);
                }

                bool isBackup = false;

                if (string.IsNullOrEmpty(videoStreamJson))
                {
                    Debug.WriteLine("Main video stream is unplayable or failed, trying backup...");
                    videoStreamJson = await FetchVideoStreamsBackup(localVideoId);
                    isBackup = true;
                }
                

                if (string.IsNullOrEmpty(videoStreamJson))
                {
                    Debug.WriteLine("Failed to retrieve video stream JSON.");
                    return;
                }

                if (videoQuality == "Auto")
                {
                    Debug.WriteLine("Auto quality selected. Fetching HLS stream...");
                    isUsingAuto = true;
                    await HandleHLSStream(videoStreamJson);
                    return;
                }

                Debug.WriteLine("Video Stream JSON: " + videoStreamJson);

                if (videoStreamJson != null)
                {

                    var selectedFormat = SelectFormatBasedOnQuality(videoStreamJson, videoQuality);
                    if (selectedFormat == null)
                    {
                        Debug.WriteLine($"No format found for {videoQuality}, defaulting to Medium quality.");
                        selectedFormat = SelectFormatBasedOnQuality(videoStreamJson, "Medium");
                    }

                    if (selectedFormat != null && selectedFormat.Itag == 18)
                    {
                        Debug.WriteLine($"Selected video format is medium quality (itag 18). Skipping audio selection.");
                        LoadMedia(selectedFormat.Url);
                    }
                    else
                    {
                        var selectedAudio = SelectFormatBasedOnQuality(videoStreamJson, "Audio");

                        if (selectedAudio != null)
                        {
                            LoadMedia(selectedFormat.Url, selectedAudio.Url);
                            Debug.WriteLine($"Selected audio: {selectedAudio.Itag} - {selectedAudio.QualityLabel} - {selectedAudio.MimeType}");
                        }
                        else
                        {
                            Debug.WriteLine("No suitable audio format found.");
                        }
                    }

                    if (selectedFormat != null)
                    {
                        Debug.WriteLine($"Selected video format: {selectedFormat.Itag} - {selectedFormat.QualityLabel} - {selectedFormat.MimeType}");
                    }
                    else
                    {
                        Debug.WriteLine("No suitable video format found.");
                    }
                }
                else
                {
                    Debug.WriteLine("Failed to deserialize video stream JSON.");
                }

                if (isBackup)
                {
                    Debug.WriteLine("This is a backup video stream.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in PopulateVideoStream for video ID {localVideoId}: {ex.Message}");
            }
            finally
            {
                BubbleLoadingAnimationPlayer.Stop();
                AdjustGridHeight();
            }
        }

        private async Task GetCaptionsAndLoadIntoPlayer(string videoStreamJson)
        {
            try
            {
                Debug.WriteLine($"Raw JSON: {videoStreamJson}");

                var json = JToken.Parse(videoStreamJson);

                Debug.WriteLine($"Parsed JSON: {json}");

                string captionsUrl = json.SelectToken("$..captions.playerCaptionsTracklistRenderer.captionTracks[0].baseUrl")?.ToString();

                if (string.IsNullOrEmpty(captionsUrl))
                {
                    Debug.WriteLine("No captions URL found.");
                    return;
                }

                Debug.WriteLine($"Captions URL found: {captionsUrl}");

                using (HttpClient client = new HttpClient())
                {
                    string captionData = await client.GetStringAsync(captionsUrl);

                    string ttmlData = ConvertToTTML(captionData, captionsUrl);

                    LoadCaptionsIntoPlayer(captionsUrl, captionsUrl);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading captions: {ex.Message}");
            }
        }

        private async Task<string> SaveTTMLToTemporaryFile(string ttmlContent)
        {
            try
            {

                var tempFolder = Windows.Storage.ApplicationData.Current.TemporaryFolder;

                var tempFile = await tempFolder.CreateFileAsync(Guid.NewGuid() + ".ttml", Windows.Storage.CreationCollisionOption.ReplaceExisting);

                await Windows.Storage.FileIO.WriteTextAsync(tempFile, ttmlContent);

                Debug.WriteLine($"TTML content saved to temporary file: {tempFile.Path}");

                return tempFile.Path;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving TTML to temporary file: {ex.Message}");
                throw;
            }
        }

        private void LoadCaptionsIntoPlayer(string ttmlSource1, string ttmlSource2)
        {
            if (!string.IsNullOrEmpty(ttmlSource1) && !string.IsNullOrEmpty(ttmlSource2))
            {
                try
                {
                    Uri ttmlUri1;
                    Uri ttmlUri2;

 
                    if (Uri.TryCreate(ttmlSource1, UriKind.Absolute, out ttmlUri1))
                    {
                        Debug.WriteLine($"Loading caption 1 from URL: {ttmlUri1}");
                    }
                    else
                    {
                        Debug.WriteLine($"Invalid TTML source for caption 1: {ttmlSource1}");
                        return;
                    }

                    if (Uri.TryCreate(ttmlSource2, UriKind.Absolute, out ttmlUri2))
                    {
                        Debug.WriteLine($"Loading caption 2 from URL: {ttmlUri2}");
                    }
                    else
                    {
                        Debug.WriteLine($"Invalid TTML source for caption 2: {ttmlSource2}");
                        return;
                    }

                    Debug.WriteLine($"TTML Source 1: {ttmlUri1}");
                    Debug.WriteLine($"TTML Source 2: {ttmlUri2}");


                    var caption1 = new Microsoft.PlayerFramework.Caption
                    {
                        Source = ttmlUri1,
                        Language = "en",
                        Description = "English Captions 1"
                    };

                    var caption2 = new Microsoft.PlayerFramework.Caption
                    {
                        Source = ttmlUri2,
                        Language = "en",
                        Description = "English Captions 2"
                    };

                    Debug.WriteLine("Caption 1 created successfully.");
                    Debug.WriteLine($"Caption 1 Language: {caption1.Language}, Description: {caption1.Description}");
                    Debug.WriteLine("Caption 2 created successfully.");
                    Debug.WriteLine($"Caption 2 Language: {caption2.Language}, Description: {caption2.Description}");

   
                    VideoPlayer.IsCaptionSelectionEnabled = true;
                    VideoPlayer.SelectedCaption = caption1;
                    VideoPlayer.IsCaptionSelectionVisible = true;

                    Debug.WriteLine("Captions loaded successfully.");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error loading captions: {ex.Message}");
                }
            }
            else
            {
                Debug.WriteLine("One or both TTML sources are empty or null. Cannot load captions.");
            }
        }




        private string SanitizeXmlInput(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            input = WebUtility.HtmlDecode(input);

            input = input.Replace(":", "_");

            input = input.Replace("&", "&amp;")
                         .Replace("<", "&lt;")
                         .Replace(">", "&gt;")
                         .Replace("\"", "&quot;")
                         .Replace("'", "&apos;");

            return input;
        }

        private string ConvertToTTML(string captionData, string captionsUrl)
        {
            try
            {
                if (string.IsNullOrEmpty(captionData))
                {
                    Debug.WriteLine("Caption data is empty or null.");
                    return string.Empty;
                }

                Debug.WriteLine($"Raw caption data: {captionData.Substring(0, Math.Min(captionData.Length, 100))}");

                if (captionData.Contains("timedtext") && captionData.Contains("format=\"3\""))
                {
                    Debug.WriteLine("Caption data is already in TTML3 format. Returning the captions URL.");
                    return captionsUrl;
                }

                Debug.WriteLine("Caption data is not in TTML3 format. Converting...");

                captionData = RemoveXmlDeclaration(captionData);
                string sanitizedData = SanitizeXmlInput(captionData);

                XDocument xmlDoc = XDocument.Parse(sanitizedData);

                XDocument ttmlDoc = new XDocument(
                    new XElement("tt",
                        new XAttribute("xmlns", "http://www.w3.org/ns/ttml"),
                        new XAttribute("xml:lang", "en"),
                        new XElement("body",
                            new XElement("div")
                        )
                    )
                );

                var captions = xmlDoc.Descendants("text");

                foreach (var caption in captions)
                {
                    double startTime = double.Parse(caption.Attribute("start").Value);
                    double duration = double.Parse(caption.Attribute("dur").Value);
                    string text = caption.Value;

                    XElement captionElement = new XElement("p",
                        new XAttribute("begin", TimeSpan.FromSeconds(startTime).ToString("c")),
                        new XAttribute("end", TimeSpan.FromSeconds(startTime + duration).ToString("c")),
                        new XCData(text)
                    );

                    ttmlDoc.Element("tt").Element("body").Element("div").Add(captionElement);
                }

                string tempFilePath = SaveTTMLToTemporaryFile(ttmlDoc.ToString()).Result;
                return tempFilePath;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error converting captions to TTML: {ex.Message}");
                return string.Empty;
            }
        }

        private string RemoveXmlDeclaration(string input)
        {

            var regex = new System.Text.RegularExpressions.Regex(@"^\s*<\?xml.*\?>");
            return regex.Replace(input, "").Trim();
        }

        private bool IsVideoUnplayable(string videoStreamJson)
        {
            try
            {
                var jsonObject = JsonConvert.DeserializeObject<JObject>(videoStreamJson);
                var playabilityStatus = jsonObject?["playabilityStatus"]?["status"]?.ToString();
                return playabilityStatus == "UNPLAYABLE";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error checking playability: {ex.Message}");
                return false;
            }
        }

        private async Task HandleHLSStream(string videoStreamJson)
        {
            try
            {
                Debug.WriteLine("Received video stream JSON: " + videoStreamJson);

                JObject streamingData = JObject.Parse(videoStreamJson);

                Debug.WriteLine("Parsed JSON: " + streamingData.ToString());

                string hlsManifestUrl = FindHlsManifestUrl(streamingData);
                Debug.WriteLine("HLSJsonThingy (Video): " + (hlsManifestUrl ?? "No video hlsManifestUrl found"));

                if (!string.IsNullOrEmpty(hlsManifestUrl))
                {
                    Debug.WriteLine($"HLS manifest URL (Video): {hlsManifestUrl}");
                }

                var selectedAudio = SelectFormatBasedOnQuality(videoStreamJson, "Audio");
                if (!string.IsNullOrEmpty(selectedAudio.Url))
                {
                    Debug.WriteLine("Selected Audio URL: " + selectedAudio);

                    LoadMedia(hlsManifestUrl, selectedAudio.Url);
                }
                else
                {
                    Debug.WriteLine("No suitable audio format found in the video stream JSON.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error handling HLS stream: {ex.Message}");
            }
        }


        private string FindHlsManifestUrl(JToken token)
        {
            if (token.Type == JTokenType.Object)
            {

                JProperty hlsProperty = ((JObject)token).Property("hlsManifestUrl");
                if (hlsProperty != null)
                {
                    return hlsProperty.Value.ToString();
                }

                foreach (JProperty property in ((JObject)token).Properties())
                {
                    string result = FindHlsManifestUrl(property.Value);
                    if (!string.IsNullOrEmpty(result))
                    {
                        return result;
                    }
                }
            }
            else if (token.Type == JTokenType.Array)
            {

                foreach (JToken item in (JArray)token)
                {
                    string result = FindHlsManifestUrl(item);
                    if (!string.IsNullOrEmpty(result))
                    {
                        return result;
                    }
                }
            }

            return null;
        }


        private VideoStreamFormat SelectFormatBasedOnQuality(string streamingDataJson, string videoQuality)
        {
            Debug.WriteLine($"SelectFormatBasedOnQuality called with videoQuality: {videoQuality}");

            JObject streamingData = JObject.Parse(streamingDataJson);
            JToken adaptiveFormats = streamingData.SelectToken("streamingData.adaptiveFormats");
            JToken formats = streamingData.SelectToken("streamingData.formats");

            if (adaptiveFormats != null && adaptiveFormats.HasValues)
            {
                Debug.WriteLine($"Available adaptive formats: {adaptiveFormats.Count()}");

                if (videoQuality == "medium (recommended)")
                {
                    Debug.WriteLine("Looking for Medium quality format...");

                    JToken mediumQualityFormat = formats.FirstOrDefault(f => f?["itag"]?.ToString() == "18");

                    if (mediumQualityFormat != null)
                    {
                        Debug.WriteLine("Medium quality format found.");
                        Debug.WriteLine($"URL: {mediumQualityFormat["url"]}");
                        return mediumQualityFormat.ToObject<VideoStreamFormat>();
                    }
                }
                else if (videoQuality == "low")
                {
                    Debug.WriteLine("Looking for Low quality format (240p)...");

                    JToken lowQualityFormat = adaptiveFormats.FirstOrDefault(f => f?["qualityLabel"]?.ToString() == "240p" || f?["qualityLabel"]?.ToString() == "240");

                    if (lowQualityFormat != null)
                    {
                        Debug.WriteLine("Low quality (240p) format found.");
                        Debug.WriteLine($"URL: {lowQualityFormat["url"]}");
                        return lowQualityFormat.ToObject<VideoStreamFormat>();
                    }
                }
                else if (videoQuality == "ultra low")
                {
                    Debug.WriteLine("Looking for Ultra Low quality format (144p)...");

                    JToken ultraLowQualityFormat = adaptiveFormats.FirstOrDefault(f => f?["qualityLabel"]?.ToString() == "144p" || f?["qualityLabel"]?.ToString() == "144");

                    if (ultraLowQualityFormat != null)
                    {
                        Debug.WriteLine("Ultra Low quality (144p) format found.");
                        Debug.WriteLine($"URL: {ultraLowQualityFormat["url"]}");
                        return ultraLowQualityFormat.ToObject<VideoStreamFormat>();
                    }
                }
                else if (videoQuality == "HD1080")
                {
                    Debug.WriteLine("Looking for HD1080 quality format (1080p)...");

                    JToken hd1080QualityFormat = adaptiveFormats.FirstOrDefault(f => f?["qualityLabel"]?.ToString() == "1080p" || f?["qualityLabel"]?.ToString() == "1080");

                    if (hd1080QualityFormat != null)
                    {
                        Debug.WriteLine("HD1080 quality (1080p) format found.");
                        Debug.WriteLine($"URL: {hd1080QualityFormat["url"]}");
                        return hd1080QualityFormat.ToObject<VideoStreamFormat>();
                    }
                }
                else if (videoQuality == "SD" || videoQuality == "480")
                {
                    Debug.WriteLine("Looking for SD quality format (480p)...");

                    JToken sdQualityFormat = adaptiveFormats.FirstOrDefault(f => f?["qualityLabel"]?.ToString() == "480p" || f?["qualityLabel"]?.ToString() == "480");

                    if (sdQualityFormat != null)
                    {
                        Debug.WriteLine("SD quality (480p) format found.");
                        Debug.WriteLine($"URL: {sdQualityFormat["url"]}");
                        return sdQualityFormat.ToObject<VideoStreamFormat>();
                    }
                }
                else if (videoQuality == "HD")
                {
                    Debug.WriteLine("Looking for HD quality format (720p)...");

                    JToken hdFormat = adaptiveFormats.FirstOrDefault(f => f?["qualityLabel"]?.ToString() == "720p" || f?["qualityLabel"]?.ToString() == "hd720");
                    if (hdFormat != null)
                    {
                        Debug.WriteLine("HD format found.");
                        Debug.WriteLine($"URL: {hdFormat["url"]}");
                        return hdFormat.ToObject<VideoStreamFormat>();
                    }
                }

                if (videoQuality == "Audio")
                {
                    Debug.WriteLine("Looking for Audio format...");

                    JToken audioFormat = adaptiveFormats.FirstOrDefault(f => f?["itag"]?.ToString() == "140");
                    if (audioFormat != null)
                    {
                        Debug.WriteLine("Audio format found.");
                        Debug.WriteLine($"URL: {audioFormat["url"]}");
                        return audioFormat.ToObject<VideoStreamFormat>();
                    }
                }
            

                var qualityLabels = new[] { "1080p", "720p", "480p", "360p", "240p", "144p", "140" };

                foreach (var label in qualityLabels)
                {
                    Debug.WriteLine($"Looking for {label} format...");

                    JToken fallbackFormat = adaptiveFormats.FirstOrDefault(f => f?["qualityLabel"]?.ToString() == label);
                    if (fallbackFormat != null)
                    {
                        Debug.WriteLine($"{label} format found.");
                        Debug.WriteLine($"URL: {fallbackFormat["url"]}");
                        return fallbackFormat.ToObject<VideoStreamFormat>();
                    }
                }

                Debug.WriteLine("No specific quality found, defaulting to Medium quality.");
                JToken defaultFormat = formats.FirstOrDefault(f => f?["itag"]?.ToString() == "18") ?? adaptiveFormats.FirstOrDefault(f => f?["itag"]?.ToString() == "134");
                if (defaultFormat != null)
                {
                    Debug.WriteLine($"URL: {defaultFormat["url"]}");
                }
                return defaultFormat.ToObject<VideoStreamFormat>();
            }

            Debug.WriteLine("No adaptive formats found, defaulting to medium format.");
            return formats.FirstOrDefault(f => f?["itag"]?.ToString() == "18")?.ToObject<VideoStreamFormat>();
        }

        private async Task PopulateRelatedVideos(string videoId)
        {
            string response = await FetchRealtedVideos(videoId);

            if (!string.IsNullOrEmpty(response))
            {
                try
                {
                    var videos = new List<RealtedVideoDetail>();
                    var jsonResponse = JObject.Parse(response);

                    var videoItems = jsonResponse.SelectTokens("$..compactVideoRenderer").Where(x => x is JObject).ToList();

                    if (videoItems != null)
                    {
                        foreach (var item in videoItems)
                        {

                            var spotlightTitle = item.SelectToken("title.simpleText")?.ToString();
                            var spotlightVideoId = item.SelectToken("videoId")?.ToString();
                            var thumbnailUrl = item.SelectToken("thumbnail.thumbnails[1].url")?.ToString();
                            var views = item.SelectToken("viewCountText.simpleText")?.ToString();
                            var date = item.SelectToken("publishedTimeText.simpleText")?.ToString();
                            var author = item.SelectToken("longBylineText.runs[0].text")?.ToString();
                            var lengthText = item.SelectToken("lengthText.simpleText")?.ToString();
                            var authorPFP = item.SelectToken("channelThumbnail.thumbnails[0].url")?.ToString();

                            if (string.IsNullOrEmpty(lengthText))
                            {
                                var lengthLabel = item.SelectToken("lengthText.accessibility.accessibilityData.label")?.ToString();

                                if (!string.IsNullOrEmpty(lengthLabel))
                                {
                                    var timeMatch = System.Text.RegularExpressions.Regex.Match(lengthLabel, @"(\d+)\s+minutes?[, ]*(\d+)?\s*seconds?");
                                    if (timeMatch.Success)
                                    {
                                        int minutes = int.Parse(timeMatch.Groups[1].Value);
                                        int seconds = timeMatch.Groups[2].Success ? int.Parse(timeMatch.Groups[2].Value) : 0;

                                        lengthText = new TimeSpan(0, minutes, seconds).ToString(@"mm\:ss");
                                    }
                                }
                            }

                            if (!string.IsNullOrEmpty(spotlightTitle) && !string.IsNullOrEmpty(spotlightVideoId))
                            {
                                var video = new RealtedVideoDetail
                                {
                                    Title = spotlightTitle,
                                    Author = author,
                                    VideoId = spotlightVideoId,
                                    Thumbnail = thumbnailUrl,
                                    Views = views,
                                    Date = date,
                                    Length = lengthText
                                };

                                AddVideoCard(video, RealtedVideoItemControl);
                                videos.Add(video);
                            }
                        }

                    
                    }
                    else
                    {
                        Debug.WriteLine("No related videos found in the response.");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error parsing response: {ex.Message}");
                }
                finally
                {
                    IsLoadedForPivotRealted = true;
                    LoadMore.Visibility = Visibility.Visible;
                    BubbleLoadingAnimationRel.Stop();
                    LoadingPanelRel.Visibility = Visibility.Collapsed;
                    AdjustGridHeight();
                }
            }
        }

        private void AddVideoCard(RealtedVideoDetail video, ItemsControl itemsControl)
        {

            Debug.WriteLine($"Entering AddVideoCard method. Video: {video?.Title}, ItemsControl: {itemsControl?.Name}");

            var videoCard = CreateVideoCard(video);


            itemsControl.Items.Add(videoCard);

        }

        private void AddCommentCard(VideoComment comment, ItemsControl itemsControl)
        {
            var commentCard = CreateCommentCard(comment);
            itemsControl.Items.Add(commentCard);
        }
        

        private async Task<string> FetchRealtedVideos(string videoId)
        {
            try
            {
                string url = $"https://www.youtube.com/youtubei/v1/next?key={Settings.InnerTubeApiKey}";

                var requestBody = new
                {
                    context = new
                    {
                        client = new
                        {
                            hl = "en",
                            gl = "US",
                            clientName = "WEB",
                            clientVersion = "2.20211122.09.00",
                            userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/89.0.4389.82 Safari/537.36",
                            osName = "Windows",
                            osVersion = "10",
                            platform = "DESKTOP"
                        }
                    },
                    videoId = videoId
                };

                string jsonRequestBody = Newtonsoft.Json.JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(jsonRequestBody, Encoding.UTF8, "application/json");

                Debug.WriteLine("Request URL (Recommended): " + url);
                Debug.WriteLine("Request Body (Recommended): " + jsonRequestBody);

                HttpResponseMessage response = await _httpClient.PostAsync(url, content);
                response.EnsureSuccessStatusCode();

                string responseBody = await response.Content.ReadAsStringAsync();
                Debug.WriteLine("API Response (Recommended): " + responseBody);

                return responseBody;
            }
            catch (Exception ex)
            {
                ShowMessage("Error fetching recommended videos: " + ex.Message);
                return string.Empty;
            }
        }

        private async Task<string> FetchCommentsPostContinued(string continuationToken)
        {
            try
            {
                string url = $"https://www.youtube.com/youtubei/v1/next?key={Settings.InnerTubeApiKey}";

                var continuationContextData = new
                {
                    context = new
                    {
                        client = new
                        {
                            hl = "en",
                            gl = "US",
                            clientName = "WEB",
                            clientVersion = "2.20211122.09.00",
                            osName = "Windows",
                            osVersion = "10",
                            platform = "DESKTOP"
                        }
                    },
                    continuation = continuationToken
                };

                string continuationRequestBody = Newtonsoft.Json.JsonConvert.SerializeObject(continuationContextData);
                var continuationContent = new StringContent(continuationRequestBody, Encoding.UTF8, "application/json");

                if (_httpClient == null)
                {
                    _httpClient = new HttpClient();
                    _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/89.0.4389.82 Safari/537.36");
                }

                HttpResponseMessage response = await _httpClient.PostAsync(url, continuationContent);
                response.EnsureSuccessStatusCode();

                string responseBody = await response.Content.ReadAsStringAsync();
                Debug.WriteLine("Continued Comments API Response: " + responseBody);

                string _continuationItemRendererToken = ExtractContinuationToken(responseBody);

                if (!string.IsNullOrEmpty(_continuationItemRendererToken))
                {
                    Debug.WriteLine("Found continuation token: " + _continuationItemRendererToken);
                    continuationTokenNextComments = _continuationItemRendererToken;
                }
                else
                {
                    Debug.WriteLine("No continuation token found.");
                }

                return responseBody;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in FetchCommentsPostContinued: {ex.Message}");
                return string.Empty;
            }
        }

        private async void LoadMoreButton_Click(object sender, RoutedEventArgs e)
        {
           await PopulateComments(localVideoId);
        }

        private async void ShowMessage(string message)
        {
            MessageDialog messageDialog = new MessageDialog(message);
            await messageDialog.ShowAsync();
        }

        private void VideoPlayer_IsFullScreenChanged(object sender, RoutedEventArgs e)
        {
            if (VideoPlayer.IsFullScreen)
            {
                Debug.WriteLine("Full-screen mode activated.");
                SetLandscapeMode();
            }
            else
            {
                SetPortraitMode();
                DisplayInformation.AutoRotationPreferences = DisplayOrientations.Portrait | DisplayOrientations.Landscape;
                Debug.WriteLine("Exited full-screen mode.");
            }
        }

        private void VideoPlayer_VolumeChanged(object sender, RoutedEventArgs e)
        {
            AudioPlayer.Volume = VideoPlayer.Volume;
        }


        private async Task PopulateVideoDetails(string videoId)
        {
            try
            {
                VideoAuthorDetails result = await FetchVideoAndAuthorDetails(videoId);

                if (string.IsNullOrEmpty(result.VideoDetailsJson))
                {
                    Debug.WriteLine("No video details found for video ID: " + videoId);
                    return;
                }

                if (string.IsNullOrEmpty(result.AuthorDetailsJson))
                {
                    Debug.WriteLine("No author details found for video ID: " + videoId);
                    return;
                }

                var videoJsonResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<JObject>(result.VideoDetailsJson);
                var authorJsonResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<JObject>(result.AuthorDetailsJson);


                Debug.WriteLine("Full  videoJsonResponse JSON: " + videoJsonResponse?.ToString());

                var videoDetailsSection = videoJsonResponse.SelectToken("videoDetails");
                Debug.WriteLine("Full videoDetails JSON section: " + videoDetailsSection?.ToString());

                var videoOwnerRendererSection = authorJsonResponse.SelectToken("..videoOwnerRenderer");
                Debug.WriteLine("Full videoOwnerRenderer JSON section: " + videoOwnerRendererSection?.ToString());

                var videoUploadDateSec = authorJsonResponse.SelectToken("..dateText");
                Debug.WriteLine("Full videoUploadDateSec JSON section: " + videoUploadDateSec?.ToString());

                string FormatedViewCountStart = videoJsonResponse.SelectToken("videoDetails.viewCount")?.ToString();

                string FormatedViewCountEnd;
                int parsedViewCount;

                if (int.TryParse(FormatedViewCountStart, out parsedViewCount))
                {

                    FormatedViewCountEnd = Utils.AddCommasManually(parsedViewCount);
                    Debug.WriteLine(FormatedViewCountEnd);
                }
                else
                {

                    FormatedViewCountEnd = FormatedViewCountStart;
                    Debug.WriteLine(FormatedViewCountEnd);
                }

                var videoDetailsTab = new VideoDetailsTab
                {
                    Title = videoJsonResponse.SelectToken("videoDetails.title")?.ToString(),
                    Description = videoJsonResponse.SelectToken("videoDetails.shortDescription")?.ToString(),
                    ViewCount = FormatedViewCountEnd + " views",
                    UploadDate = authorJsonResponse
                        .SelectTokens("..dateText.simpleText")
                        .FirstOrDefault()?.ToString(),
                    Subcribers = authorJsonResponse
                        .SelectTokens("..subscriberCountText.simpleText")
                        .FirstOrDefault()?.ToString()
                };

                var authorDetails = new AuthorDetails
                {
                    AvatarUrl = authorJsonResponse.SelectToken("..videoOwnerRenderer.thumbnail.thumbnails[1].url")?.ToString(),
                    Name = authorJsonResponse.SelectToken("..videoOwnerRenderer.title.runs[0].text")?.ToString()
                };

                Title = videoDetailsTab.Title;
                Description = videoDetailsTab.Description;
                Views = videoDetailsTab.ViewCount;
                Subs = videoDetailsTab.Subcribers;
                ThumbnailURL = $"https://i.ytimg.com/vi/{videoId}/hqdefault.jpg";
                AurthorPFPURL = authorDetails.AvatarUrl;
                Aurthor = authorDetails.Name;           

                VideoTitle.Text = videoDetailsTab.Title;

                string avatarUrl = authorDetails.AvatarUrl;

                Uri avatarUri = new Uri(avatarUrl, UriKind.Absolute);

                ProfilePicture.Source = new BitmapImage(avatarUri);

                VideoUploadDate.Text = "Published on " + videoDetailsTab.UploadDate;

                Date = "Published on " + videoDetailsTab.UploadDate;

                SetVideoDescriptionWithLinks(videoDetailsTab.Description);

                VideoViews.Text = videoDetailsTab.ViewCount;

                SubscriptionStatus.Text = videoDetailsTab.Subcribers;

                Username.Text = authorDetails.Name;

                SetVideoDescriptionWithLinks(videoDetailsTab.Description);
                Debug.WriteLine($"Video Title: {videoDetailsTab.Title}");
                Debug.WriteLine($"Vew Count: {videoDetailsTab.ViewCount}");
                Debug.WriteLine($"Description: {videoDetailsTab.Description}");
                Debug.WriteLine($"Upload Date: {videoDetailsTab.UploadDate}");

                Debug.WriteLine($"Subbed {videoDetailsTab.Subcribers}");

                Debug.WriteLine($"Name {authorDetails.Name}");

                Debug.WriteLine($"Author Avatar URL: {authorDetails.AvatarUrl}");

                var browseId = authorJsonResponse
                       .SelectToken("..videoSecondaryInfoRenderer.owner.videoOwnerRenderer.title.runs[0].navigationEndpoint.browseEndpoint.browseId")?.ToString();


                if (!string.IsNullOrEmpty(browseId))
                {
                    Debug.WriteLine("Found browseId: " + browseId);
                }
                else
                {
                    Debug.WriteLine("browseId not found.");
                }

                BrowserID = browseId;

            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in PopulateVideoDetails for video ID {videoId}: {ex.Message}");
            }
            finally
            {
                IsLoadedForPivotDetails = true;
                StuffPanel.Visibility = Visibility.Visible;
                BubbleLoadingAnimationDel.Stop();
                LoadingPanelDel.Visibility = Visibility.Collapsed;
                AdjustGridHeight();

            }
        }

        private string FindBrowseId(JObject jsonObject)
        {

            foreach (var property in jsonObject.Properties())
            {
                if (property.Value is JObject)
                {
                    JObject nestedObject = property.Value as JObject;
                    if (nestedObject != null)
                    {
                        string browseId = FindBrowseId(nestedObject);
                        if (!string.IsNullOrEmpty(browseId))
                        {
                            return browseId; 
                        }
                    }
                }

                if (property.Name.Equals("browseEndpoint", StringComparison.OrdinalIgnoreCase))
                {
                    JObject browseEndpoint = property.Value as JObject;
                    if (browseEndpoint != null)
                    {
                        var browseIdToken = browseEndpoint["browseId"];
                        if (browseIdToken != null)
                        {
                            return browseIdToken.ToString();
                        }
                    }
                }
            }

            return null;
        }

        private void SetVideoDescriptionWithLinks(string description)
        {
            VideoDescription.Blocks.Clear();

            var paragraph = new Paragraph();

            string pattern = @"(http://[^\s]+|https://[^\s]+)";
            var matches = Regex.Matches(description, pattern);

            int lastIndex = 0;
            foreach (Match match in matches)
            {

                if (match.Index > lastIndex)
                {
                    paragraph.Inlines.Add(new Run { Text = description.Substring(lastIndex, match.Index - lastIndex) });
                }

                string url = match.Value;

                if (url.StartsWith("http://") || url.StartsWith("https://"))
                {
                    var hyperlink = new Hyperlink
                    {
                        NavigateUri = new Uri(url, UriKind.Absolute),
                        Foreground = new SolidColorBrush(Color.FromArgb(255, 130, 180, 220))
                    };

                    hyperlink.Inlines.Add(new Run { Text = url });

                    hyperlink.Click += async (sender, args) =>
                    {
                        try
                        {
                            if (Uri.IsWellFormedUriString(hyperlink.NavigateUri.ToString(), UriKind.Absolute))
                            {

                                await Windows.System.Launcher.LaunchUriAsync(hyperlink.NavigateUri);
                            }
                            else
                            {
                                Debug.WriteLine("Invalid URI: " + hyperlink.NavigateUri.ToString());
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine("Error launching URI: " + ex.Message);
                        }
                    };

                    paragraph.Inlines.Add(hyperlink);
                }
                else
                {

                    paragraph.Inlines.Add(new Run { Text = url });
                }

                lastIndex = match.Index + match.Length;
            }

            if (lastIndex < description.Length)
            {
                paragraph.Inlines.Add(new Run { Text = description.Substring(lastIndex) });
            }

            VideoDescription.Blocks.Add(paragraph);
        }

        private Task<VideoAuthorDetails> FetchVideoAndAuthorDetails(string videoId)
        {
            return Task.Run(async () =>
            {
                try
                {
                    string videoUrl = $"https://www.youtube.com/youtubei/v1/player?key={Settings.InnerTubeApiKey}";

                    DateTime currentUtcDateTime = DateTime.UtcNow;
                    long signatureTimestamp = (long)(currentUtcDateTime - new DateTime(1970, 1, 1)).TotalSeconds;

                    var contextData = new
                    {
                        videoId = videoId,
                        context = new
                        {
                            client = new
                            {
                                hl = "en",
                                gl = "US",
                                clientName = "IOS",
                                clientVersion = "19.29.1",
                                deviceMake = "Apple",
                                deviceModel = "iPhone",
                                osName = "iOS",
                                userAgent = "com.google.ios.youtube/19.29.1 (iPhone16,2; U; CPU iOS 17_5_1 like Mac OS X;)",
                                osVersion = "17.5.1.21F90"
                            }
                        },
                        playbackContext = new
                        {
                            contentPlaybackContext = new
                            {
                                signatureTimestamp = signatureTimestamp
                            }
                        }
                    };

                    string jsonRequestBody = Newtonsoft.Json.JsonConvert.SerializeObject(contextData);
                    var content = new StringContent(jsonRequestBody, Encoding.UTF8, "application/json");

                    HttpResponseMessage videoResponse = await _httpClient.PostAsync(videoUrl, content);
                    videoResponse.EnsureSuccessStatusCode();
                    string videoDetailsJson = await videoResponse.Content.ReadAsStringAsync();

                    string authorUrl = $"https://www.youtube.com/youtubei/v1/next?key={Settings.InnerTubeApiKey}";
                    var authorContextData = new
                    {
                        videoId = videoId,
                        context = new
                        {
                            client = new
                            {
                                hl = "en",
                                gl = "US",
                                clientName = "WEB",
                                clientVersion = "2.20211122.09.00",
                                osName = "Windows",
                                osVersion = "10",
                                platform = "DESKTOP"
                            }
                        }
                    };

                    string authorRequestBody = Newtonsoft.Json.JsonConvert.SerializeObject(authorContextData);
                    var authorContent = new StringContent(authorRequestBody, Encoding.UTF8, "application/json");

                    HttpResponseMessage authorResponse = await _httpClient.PostAsync(authorUrl, authorContent);
                    authorResponse.EnsureSuccessStatusCode();
                    string authorDetailsJson = await authorResponse.Content.ReadAsStringAsync();

                    return new VideoAuthorDetails
                    {
                        VideoDetailsJson = videoDetailsJson,
                        AuthorDetailsJson = authorDetailsJson
                    };
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error fetching video and author details: {ex.Message}");
                    return new VideoAuthorDetails { VideoDetailsJson = string.Empty, AuthorDetailsJson = string.Empty };
                }
            });
        }

        private async Task PopulateComments(string videoId)
        {
            try
            {
                string commentsJson = string.IsNullOrEmpty(continuationTokenNextComments)
                                      ? await FetchNewestComments(videoId)
                                      : await FetchCommentsPostContinued(continuationTokenNextComments);

                if (string.IsNullOrEmpty(commentsJson))
                {
                    Debug.WriteLine($"No comments found for video ID: {videoId}");
                    return;
                }

                var commentEntities = GetCommentEntities(commentsJson);

                if (commentEntities.Any())
                {
                    var videoComments = new List<VideoComment>();

                    foreach (var entity in commentEntities)
                    {
                        var commentId = entity.SelectToken("properties.commentId")?.ToString();
                        var content = entity.SelectToken("properties.content.content")?.ToString();
                        var date = entity.SelectToken("properties.publishedTime")?.ToString();
                        var author = entity.SelectToken("properties.authorButtonA11y")?.ToString();
                        var authorChannelId = entity.SelectToken("author.channelId")?.ToString();
                        var avatar = entity.SelectToken("author.avatarThumbnailUrl")?.ToString();

                        var videoComment = new VideoComment
                        {
                            CommentId = commentId,
                            Content = content,
                            Date = date,
                            Author = author,
                            AuthorChannelId = authorChannelId,
                            Avatar = avatar
                        };

                        videoComments.Add(videoComment);
                        AddCommentCard(videoComment, CommentsItemControl);
                    }
                }
                else
                {
                    Debug.WriteLine("No comment entities found.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in PopulateComments for video ID {videoId}: {ex.Message}");
            }
            finally
            {
                IsLoadedForPivotComments = true;
                BubbleLoadingAnimationCom.Stop();
                LoadingPanelCom.Visibility = Visibility.Collapsed;
                LoadMore.Visibility = Visibility.Visible;
                AdjustGridHeight();
            }
        }
       
        private async Task<string> FetchNewestComments(string videoId)
        {
            try
            {
                string url = $"https://www.youtube.com/youtubei/v1/next?key={Settings.InnerTubeApiKey}";

                var initialContextData = new
                {
                    videoId = videoId,
                    context = new
                    {
                        client = new
                        {
                            hl = "en",
                            gl = "US",
                            clientName = "WEB",
                            clientVersion = "2.20211122.09.00",
                            osName = "Windows",
                            osVersion = "10",
                            platform = "DESKTOP"
                        }
                    }
                };

                string jsonRequestBody = Newtonsoft.Json.JsonConvert.SerializeObject(initialContextData);
                var content = new StringContent(jsonRequestBody, Encoding.UTF8, "application/json");

                if (_httpClient == null)
                {
                    _httpClient = new HttpClient();
                    _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/89.0.4389.82 Safari/537.36");
                }

                HttpResponseMessage response = await _httpClient.PostAsync(url, content);
                response.EnsureSuccessStatusCode();

                string responseBody = await response.Content.ReadAsStringAsync();

                var jsonResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<JObject>(responseBody);
                var newestToken = jsonResponse
                    ["engagementPanels"]?.FirstOrDefault()
                    ["engagementPanelSectionListRenderer"]?["header"]?["engagementPanelTitleHeaderRenderer"]
                    ["menu"]?["sortFilterSubMenuRenderer"]?["subMenuItems"]
                    ?.FirstOrDefault(item => item["title"]?.ToString() == "Top comments")
                    ?["serviceEndpoint"]?["continuationCommand"]?["token"]?.ToString();

                if (string.IsNullOrEmpty(newestToken))
                {
                    Debug.WriteLine("No continuation token found for newest comments.");
                    return string.Empty;
                }

                var continuationContextData = new
                {
                    context = initialContextData.context,
                    continuation = newestToken
                };

                string continuationRequestBody = Newtonsoft.Json.JsonConvert.SerializeObject(continuationContextData);
                var continuationContent = new StringContent(continuationRequestBody, Encoding.UTF8, "application/json");

                response = await _httpClient.PostAsync(url, continuationContent);
                response.EnsureSuccessStatusCode();

                string continuationResponseBody = await response.Content.ReadAsStringAsync();
                Debug.WriteLine("Newest Comments API Response: " + continuationResponseBody);

                string _continuationTokenNextComments = ExtractContinuationToken(continuationResponseBody);

                if (!string.IsNullOrEmpty(_continuationTokenNextComments))
                {
                    Debug.WriteLine("Found continuation token: " + _continuationTokenNextComments);
                    continuationTokenNextComments = _continuationTokenNextComments;
                }
                else
                {
                    Debug.WriteLine("No continuation token found.");
                }

                return continuationResponseBody;
            }
            catch (Exception ex)
            {
                ShowMessage("Error fetching newest comments: " + ex.Message);
                return string.Empty;
            }
        }

        private string ExtractContinuationToken(string jsonResponse)
        {
            try
            {
                var jsonObject = Newtonsoft.Json.Linq.JObject.Parse(jsonResponse);

                var tokens = jsonObject.SelectTokens("$..token")
                                       .Select(t => t.ToString())
                                       .ToList();

                if (tokens.Any())
                {
                    string longestToken = tokens.OrderByDescending(t => t.Length).First();
                    Debug.WriteLine("Found longest continuation token: " + longestToken);
                    return longestToken;
                }

                Debug.WriteLine("No continuation token found.");
                return string.Empty;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in ExtractContinuationToken: {ex.Message}");
                return string.Empty;
            }
        }

        private List<JToken> FindAllOccurrences(JToken container, string targetKey)
        {
            var matches = new List<JToken>();

            if (container.Type == JTokenType.Object)
            {
                foreach (var property in container.Children<JProperty>())
                {
                    if (property.Name == targetKey)
                    {
                        matches.Add(property.Value);
                    }
                    else
                    {
                        matches.AddRange(FindAllOccurrences(property.Value, targetKey));
                    }
                }
            }
            else if (container.Type == JTokenType.Array)
            {
                foreach (var item in container.Children())
                {
                    matches.AddRange(FindAllOccurrences(item, targetKey));
                }
            }

            return matches;
        }

        private List<JToken> GetCommentEntities(string json)
        {
            try
            {
                var jsonResponse = JObject.Parse(json);

                return FindAllOccurrences(jsonResponse, "commentEntityPayload");
            }
            catch (JsonReaderException ex)
            {
                Debug.WriteLine($"JSON parsing error: {ex.Message}");
                return new List<JToken>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetCommentEntities: {ex.Message}");
                return new List<JToken>();
            }
        }

        private Border CreateCommentCard(VideoComment comment)
        {
            var grid = new Grid
            {
                Margin = new Thickness(0, 5, 0, 5),
                HorizontalAlignment = HorizontalAlignment.Left
            };

            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });

            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(3, GridUnitType.Star) });

            var bitmapImage = new BitmapImage();
            bitmapImage.UriSource = new Uri(comment.Avatar, UriKind.Absolute);

            var thumbnailImage = new Image
            {
                Source = bitmapImage,
                Width = 40,
                Height = 40,
                Stretch = Stretch.UniformToFill,
                Margin = new Thickness(10, 0, 10, 0)
            };

            Grid.SetColumn(thumbnailImage, 0);
            Grid.SetRow(thumbnailImage, 0);
            grid.Children.Add(thumbnailImage);

            var infoPanel = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 0, 0, 0)
            };

            var nameAndDateGrid = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center
            };

            nameAndDateGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
            nameAndDateGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });

            var nameTextBlock = new TextBlock
            {
                Text = comment.Author,
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromArgb(255, 91, 141, 195)),
                Margin = new Thickness(0, 0, 5, 0)
            };
            Grid.SetColumn(nameTextBlock, 0);
            nameAndDateGrid.Children.Add(nameTextBlock);

            var dateTextBlock = new TextBlock
            {
                Text = $"| {comment.Date}",
                Foreground = new SolidColorBrush(Colors.Black),
                FontWeight = FontWeights.Normal,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 12
            };
            Grid.SetColumn(dateTextBlock, 1);
            nameAndDateGrid.Children.Add(dateTextBlock);

            infoPanel.Children.Add(nameAndDateGrid);

            Grid.SetColumn(infoPanel, 1);
            Grid.SetRow(infoPanel, 0);
            grid.Children.Add(infoPanel);

            var commentTextBlock = new TextBlock
            {
                Text = comment.Content,
                Foreground = new SolidColorBrush(Colors.Black),
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                FontWeight = FontWeights.ExtraLight,
                Margin = new Thickness(0, -17.5, 10, 0)
            };

            Grid.SetColumn(commentTextBlock, 1);
            Grid.SetRow(commentTextBlock, 1);
            grid.Children.Add(commentTextBlock);

            return new Border
            {
                Child = grid,
                BorderBrush = new SolidColorBrush(Colors.Transparent),
                BorderThickness = new Thickness(0),
                Background = new SolidColorBrush(Colors.Transparent)
            };
        }


        private void HardwareButtons_BackPressed(object sender, BackPressedEventArgs e)
        {
            if (Frame.CanGoBack)
            {
                e.Handled = true;
                Frame.GoBack();
            }
            else
            {
                e.Handled = false;
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            syncTimer.Stop();

            HardwareButtons.BackPressed -= HardwareButtons_BackPressed;
        }

        private void Border_Tap(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            try
            {
                Border border = (Border)sender;

                if (border.Tag == null)
                {
                    Debug.WriteLine("Tag is null.");
                    return;
                }

                string tagData = border.Tag.ToString();
                string[] parts = tagData.Split(',');

                if (parts.Length < 4)
                {
                    Debug.WriteLine("Tag data is in an unexpected format.");
                    return;
                }

                string videoId = parts[0];
                string title = parts[1];
                string author = parts[2];
                string thumbnailURL = parts[3];

                if (string.IsNullOrEmpty(videoId))
                {
                    Debug.WriteLine("videoId is null or empty.");
                    return;
                }

                Debug.WriteLine("Navigating with videoId: " + videoId);

                Settings.AddSeedVideoId(videoId);

                var watchHistoryItem = new WatchHistoryItem
                {
                    VideoId = videoId,
                    Title = title,
                    Author = author,
                    ThumbnailURL = thumbnailURL
                };

                Settings.AddToWatchHistory(watchHistoryItem);

                string queryString = "?videoId=" + Uri.EscapeDataString(videoId);
                Debug.WriteLine("Navigating to: /VideoPage.xaml" + queryString);

                Frame.Navigate(typeof(VideoPage), queryString);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception caught in Border_Tap: {ex.Message}");
            }
        }

        private Border CreateVideoCard(RealtedVideoDetail video)
        {
            var grid = new Grid
            {
                Margin = new Thickness(5, 5, 10, 5),
                HorizontalAlignment = HorizontalAlignment.Left
            };

            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(105) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(3, GridUnitType.Star) });

            Debug.WriteLine("Thumbnail URL: " + video.Thumbnail);

            string baseUrl = $"https://i.ytimg.com/vi/{video.VideoId}/hqdefault.jpg";

            int queryIndex = baseUrl.IndexOf('?');
            if (queryIndex > 0)
            {
                baseUrl = baseUrl.Substring(0, queryIndex); 
            }

            // For some reason you MUST remove parms from it or in this very speific case it'll not load images, I barely comment but this is important!

            Uri uri = new Uri(baseUrl);

            var thumbnailImage = new Image
            {
                Width = 105,
                Height = 60,
                Visibility = Visibility.Visible,
                Stretch = Stretch.UniformToFill,
                Margin = new Thickness(10, 0, 5, 0)
            };

            try
            {
                thumbnailImage.Source = new BitmapImage(uri);  
                Debug.WriteLine("Thumbnail loaded successfully.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error loading thumbnail: " + ex.Message);
            }

            Grid.SetColumn(thumbnailImage, 0);
            Grid.SetRow(thumbnailImage, 0);
            grid.Children.Add(thumbnailImage);

            var lengthBorder = new Border
            {
                Background = new SolidColorBrush(Colors.Black) { Opacity = 0.7 },
                Padding = new Thickness(1),
                VerticalAlignment = VerticalAlignment.Bottom,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 0, 5, 0),
                MaxWidth = 100
            };

            var lengthTextBlock = new TextBlock
            {
                Text = video.Length,
                FontSize = 11.5,
                Foreground = new SolidColorBrush(Colors.White),
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center
            };

            lengthBorder.Child = lengthTextBlock;
            Grid.SetColumn(lengthBorder, 0);
            Grid.SetRow(lengthBorder, 0);
            grid.Children.Add(lengthBorder);

            var infoPanel = new Grid
            {
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0)
            };
            Grid.SetColumn(infoPanel, 1);
            grid.Children.Add(infoPanel);

            var titleTextBlock = new TextBlock
            {
                Text = video.Title,
                FontSize = 16,
                FontWeight = FontWeights.Normal,
                Foreground = new SolidColorBrush(Color.FromArgb(255, 51, 51, 51)),
                Margin = new Thickness(0, -3.5, 0, 5),
                MaxWidth = 250,
                TextWrapping = TextWrapping.Wrap,
                MaxHeight = 42.5,
                MaxLines = 2,
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            infoPanel.Children.Add(titleTextBlock);

            var viewsAndAuthorPanel = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(0, 5, 0, 5)
            };

            var viewsAndAuthorTextBlock = new TextBlock
            {
                Text = $"{video.Views} Views - By {video.Author}",
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromArgb(255, 102, 102, 102)),
                TextWrapping = TextWrapping.NoWrap,
                HorizontalAlignment = HorizontalAlignment.Left
            };

            viewsAndAuthorPanel.Children.Add(viewsAndAuthorTextBlock);
            infoPanel.Children.Add(viewsAndAuthorPanel);

            var videoCardBorder = new Border
            {
                Child = grid,
                BorderBrush = new SolidColorBrush(Colors.Transparent),
                BorderThickness = new Thickness(0),
                Background = new SolidColorBrush(Colors.Transparent),
                Tag = $"{video.VideoId},{video.Title},{video.Author},{video.Thumbnail}"
            };

            videoCardBorder.Tapped += Border_Tap;

            return videoCardBorder;
        }

        private void MainPivot_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void AdjustGridHeight() {
           // I am too lazy to remove this
        }


        private async void MainPivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedPivotItem = VideoPivot.SelectedItem as PivotItem;

            if (selectedPivotItem != null)
            {
 
                switch (selectedPivotItem.Tag?.ToString())
                {
                    case "details":
                        if (!string.IsNullOrEmpty(localVideoId) && !IsLoadedForPivotDetails)
                        {
                            LoadingPanelDel.Visibility = Visibility.Visible;
                            BubbleLoadingAnimationDel.Begin();
                            StuffPanel.Visibility = Visibility.Collapsed;
                            await PopulateVideoDetails(localVideoId);
                        }
                        else
                        {
                            StuffPanel.Visibility = Visibility.Visible;
                        }

                        Debug.WriteLine("Selected Pivot Item: details");
                        break;

                    case "comments":
                        if (!string.IsNullOrEmpty(localVideoId) && !IsLoadedForPivotComments)
                        {
                            LoadMore.Visibility = Visibility.Collapsed;
                            LoadingPanelCom.Visibility = Visibility.Visible;
                            BubbleLoadingAnimationCom.Begin();
                            await PopulateComments(localVideoId);
                        }
                        else
                        {
                            LoadMore.Visibility = Visibility.Visible;
                        }

                        Debug.WriteLine("Selected Pivot Item: comments");
                        break;

                    case "related videos":
                        if (!string.IsNullOrEmpty(localVideoId) && !IsLoadedForPivotRealted)
                        {
                            LoadingPanelRel.Visibility = Visibility.Visible;
                            BubbleLoadingAnimationRel.Begin();
                            StuffPanel.Visibility = Visibility.Collapsed;
                            await PopulateRelatedVideos(localVideoId);
                        }

                        Debug.WriteLine("Selected Pivot Item: related videos");
                        break;

                    default:
                        Debug.WriteLine("Unknown Pivot Item");
                        break;
                }
            }
        }




    }
}