
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

namespace MangoTube8UWP
{
    public partial class VideoDownloadPage : Page
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


        private FolderPicker picker;

        public VideoDownloadPage()
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


        private void Downloads_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            Frame.Navigate(typeof(DownloadsPage));
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
                    Debug.WriteLine("Current Orientation: Portrait");
                    break;

                case DisplayOrientations.Landscape:        
                    SetLandscapeMode();
                    VideoPlayer.IsFullScreen = true;
                    Debug.WriteLine("Current Orientation: Landscape");
                    break;

                case DisplayOrientations.PortraitFlipped:
                    SetPortraitMode();
                    DisplayInformation.AutoRotationPreferences = DisplayOrientations.Portrait | DisplayOrientations.Landscape;
                    Debug.WriteLine("Current Orientation: Flipped Portrait");
                    break;

                case DisplayOrientations.LandscapeFlipped:
                    SetLandscapeMode();
                    VideoPlayer.IsFullScreen = true;
                    break;

                default:
                    SetPortraitMode();
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

                LoadMedia(videoId);
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

        private async void LoadMedia(string videoId)
        {
            try
            {

                StorageFile metadataFile = await ApplicationData.Current.LocalFolder.GetFileAsync("downloads.json");
                string json = await FileIO.ReadTextAsync(metadataFile);

                var metadata = string.IsNullOrEmpty(json) ? new DownloadMetadata() : JsonConvert.DeserializeObject<DownloadMetadata>(json);

                var videoDownload = metadata.Downloads.FirstOrDefault(download => download.VideoId == videoId);

                if (videoDownload != null)
                {

                    Debug.WriteLine($"Video file path: {videoDownload.VideoFilePath}");
                    Debug.WriteLine($"Audio file path: {videoDownload.AudioFilePath}");

                    if (!string.IsNullOrEmpty(videoDownload.VideoFilePath))
                    {

                        StorageFile videoFile = await StorageFile.GetFileFromPathAsync(videoDownload.VideoFilePath);

                        var videoProperties = await videoFile.GetBasicPropertiesAsync();
                        Debug.WriteLine($"Video file size: {videoProperties.Size} bytes");

                        var videoStream = await videoFile.OpenAsync(FileAccessMode.Read);
                        VideoPlayer.SetSource(videoStream, "video/mp4");
                        Debug.WriteLine($"Video source set to local file: {videoDownload.VideoFilePath}");
                    }
                    else
                    {
                        Debug.WriteLine("No video file path found for this video.");
                    }

                    if (!string.IsNullOrEmpty(videoDownload.AudioFilePath))
                    {

                        StorageFile audioFile = await StorageFile.GetFileFromPathAsync(videoDownload.AudioFilePath);

                        var audioProperties = await audioFile.GetBasicPropertiesAsync();
                        Debug.WriteLine($"Audio file size: {audioProperties.Size} bytes");

                        isUsingSeparateAudio = true;

                        var audioStream = await audioFile.OpenAsync(FileAccessMode.Read);
                        AudioPlayer.SetSource(audioStream, "audio/mp4");
                        Debug.WriteLine($"Audio source set to local file: {videoDownload.AudioFilePath}");

                        VideoPlayer.IsVolumeVisible = false;
                    }
                    else
                    {
                        isUsingSeparateAudio = false;
                        Debug.WriteLine("No audio file path found, using video audio.");
                    }
                }
                else
                {
                    Debug.WriteLine("Video not found in downloads.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading media for videoId {videoId}: {ex.Message}");
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

                StorageFile metadataFile = await ApplicationData.Current.LocalFolder.GetFileAsync("downloads.json");
                string json = await FileIO.ReadTextAsync(metadataFile);
                
                var metadata = string.IsNullOrEmpty(json) ? new DownloadMetadata() : JsonConvert.DeserializeObject<DownloadMetadata>(json);

                
                var videoDownload = metadata.Downloads.FirstOrDefault(download => download.VideoId == videoId);

                if (videoDownload != null)
                {

                    VideoTitle.Text = videoDownload.Title;


                    VideoViews.Text = $"Views: {videoDownload.Views}";

                    ProfilePicture.Source = new BitmapImage(new Uri(videoDownload.AuthorPFPURL ?? "/Assets/DefaultAvatar.jpg"));
                    Username.Text = videoDownload.Author; 
                    SubscriptionStatus.Text = videoDownload.Subs;

                    VideoUploadDate.Text = videoDownload.Date;

                    SetVideoDescriptionWithLinks(videoDownload.Description);

                    StuffPanel.Visibility = Visibility.Visible;

                }
                else
                {
                    Debug.WriteLine($"No video found with videoId: {videoId}");
                }
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


        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            HardwareButtons.BackPressed -= HardwareButtons_BackPressed;
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
                      break;

                    default:
                        Debug.WriteLine("Unknown Pivot Item");
                        break;
                }
            }
        }




    }
}