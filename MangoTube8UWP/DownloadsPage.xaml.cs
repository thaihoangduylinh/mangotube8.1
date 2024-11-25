using Newtonsoft.Json;
using System;
using System.Diagnostics;
using Windows.Graphics.Display;
using Windows.Phone.UI.Input;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using static MangoTube8UWP.YouTubeModal;

namespace MangoTube8UWP
{
    public partial class DownloadsPage : Page
    {

        public DownloadsPage()
        {
            InitializeComponent();

            LoadDownloads();

            DisplayInformation.AutoRotationPreferences = DisplayOrientations.Portrait;

        }

        private DateTime lastTappedTime = DateTime.MinValue;

     
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            HardwareButtons.BackPressed += HardwareButtons_BackPressed;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

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

                string videoId = border.Tag.ToString();

                if (string.IsNullOrEmpty(videoId))
                {
                    Debug.WriteLine("videoId is null or empty.");
                    return;
                }

                Debug.WriteLine("Navigating with videoId: " + videoId);

                Settings.AddSeedVideoId(videoId);

                string queryString = "?videoId=" + Uri.EscapeDataString(videoId);
                Debug.WriteLine("Navigating to: /VideoPage.xaml" + queryString);

                // Pass just the query string part in Frame.Navigate
                Frame.Navigate(typeof(VideoDownloadPage), queryString);  // Passing query string only
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception caught in Border_Tap: {ex.Message}");
            }
        }

        private void LoadDownloads()
        {
            try
            {
                StorageFile metadataFile = ApplicationData.Current.LocalFolder.GetFileAsync("downloads.json").AsTask().Result;

                if (metadataFile != null)
                {
                    string json = FileIO.ReadTextAsync(metadataFile).AsTask().Result;
                    if (!string.IsNullOrEmpty(json))
                    {
                        var metadata = JsonConvert.DeserializeObject<DownloadMetadata>(json);

                        foreach (var download in metadata.Downloads)
                        {
                            // Check if the ThumbnailURL is valid before using it
                            string thumbnailUrl = !string.IsNullOrEmpty(download.ThumbnailURL)
                                ? download.ThumbnailURL
                                : $"https://i.ytimg.com/vi/{download.VideoId}/hqdefault.jpg";  // Default URL using videoId

                            Debug.WriteLine(download.Author + " authros");

                            var video = new DownloadVideoDetail
                            {
                                VideoId = download.VideoId,
                                Title = download.Title,
                                Author = download.Author,
                                ThumbnailURL = thumbnailUrl
                            };

                            AddVideoCard(video, DownloadsItemsControl);
                        }
                    }
                }
                else
                {
                    Debug.WriteLine("No downloads found.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading downloads: {ex.Message}");
            }
        }


        private void Downloads_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            Frame.Navigate(typeof(DownloadsPage));
        }


        private void AddVideoCard(DownloadVideoDetail video, ItemsControl itemsControl)
        {
            var videoCard = CreateVideoCard(video);
            itemsControl.Items.Add(videoCard);
        }

        private Border CreateVideoCard(DownloadVideoDetail video)
        {
            var grid = new Grid
            {
                Margin = new Thickness(15, 5, 10, 5),
                HorizontalAlignment = HorizontalAlignment.Left
            };

            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(105) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(3, GridUnitType.Star) });

            var thumbnailImage = new Image
            {
                Source = new BitmapImage(new Uri(video.ThumbnailURL, UriKind.Absolute)),
                Width = 105,
                Height = 65,
                Stretch = Stretch.UniformToFill,
                Margin = new Thickness(10, 0, 5, 0)
            };
            Grid.SetColumn(thumbnailImage, 0);
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
                Text = $"{video.Author}",
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
                Tag = video.VideoId
            };

            videoCardBorder.Tapped += Border_Tap;

            return videoCardBorder;
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

        private void HideSearchBox_Completed(object sender, object e)
        {

            YouTubeLogo.Visibility = Visibility.Visible;
            SearchTextBox.Visibility = Visibility.Collapsed;
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

        private void SearchTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                string searchText = SearchTextBox.Text;
                Debug.WriteLine("Search Text: " + searchText);

                Frame.Navigate(typeof(SearchPage), searchText);
            }
        }

    }
}