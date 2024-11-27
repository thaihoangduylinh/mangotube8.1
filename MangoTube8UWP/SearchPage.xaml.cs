using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using static MangoTube8UWP.YouTubeModal;
using System.Linq;
using Windows.UI.Xaml.Controls;
using Windows.UI.Popups;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Media;
using Windows.UI;
using Windows.UI.Text;
using Windows.Phone.UI.Input;

namespace MangoTube8UWP
{
    public partial class SearchPage : Page
    {
        private bool _isSearching = false;
        private bool _hasMoreResults = false;
        private string _continuationToken = null;
        private List<SearchVideoDetail> _searchResults = new List<SearchVideoDetail>();
        private DateTime lastTappedTime = DateTime.MinValue;

        public SearchPage()
        {
            InitializeComponent();

        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            HardwareButtons.BackPressed += HardwareButtons_BackPressed;

            if (e.Parameter != null)
            {
                string query = e.Parameter as string;
                if (!string.IsNullOrEmpty(query))
                {
                    Debug.WriteLine("Search query from parameter: " + query);
                    await SearchAsync(query);
                }
                else
                {
                    await ShowMessageAsync("Search query is required.");
                    Frame.GoBack();
                }
            }
            else
            {
                await ShowMessageAsync("Search query is required.");
                Frame.GoBack();
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

        private async Task ShowMessageAsync(string message)
        {
            MessageDialog dialog = new MessageDialog(message);
            await dialog.ShowAsync();
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

        private void HideSearchBox_Completed(object sender, object e)
        {
            YouTubeLogo.Visibility = Windows.UI.Xaml.Visibility.Visible;
            AccountButton.Visibility = Windows.UI.Xaml.Visibility.Visible;
            SearchTextBox.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {

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


        private async Task SearchAsync(string query, string continuationToken = null)
        {
            if (!_isSearching && string.IsNullOrEmpty(continuationToken))
            {
                _hasMoreResults = true;
                _continuationToken = null;
            }

            if (_isSearching || (!_hasMoreResults && continuationToken == null))
            {
                Debug.WriteLine("Skipping search: either already searching or no more results.");
                return;
            }

            LoadingPanel.Visibility = Visibility.Visible;
            LoadMore.Visibility = Visibility.Collapsed;
            BubbleLoadingAnimation.Begin();

            _isSearching = true;
            Debug.WriteLine("Starting search with query: " + query + " and continuation token: " + (continuationToken ?? "None"));

            string innerTubeUrl = $"https://www.youtube.com/youtubei/v1/search?key={Settings.InnerTubeApiKey}";
            Debug.WriteLine("Making search request to: " + innerTubeUrl);

            using (var httpClient = new HttpClient())
            {
                try
                {
                    var searchData = new
                    {
                        query = query,
                        context = new
                        {
                            client = new
                            {
                                hl = "en",
                                gl = "US",
                                clientName = "WEB",
                                clientVersion = "2.20211122.09.00"
                            }
                        },
                        @params = "",
                        continuation = continuationToken
                    };

                    var jsonContent = new StringContent(JsonConvert.SerializeObject(searchData), System.Text.Encoding.UTF8, "application/json");
                    var response = await httpClient.PostAsync(innerTubeUrl, jsonContent);

                    if (!response.IsSuccessStatusCode)
                    {
                        Debug.WriteLine($"Error: Failed to fetch data. Status code: {response.StatusCode}");
                        return;
                    }

                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine("Raw JSON Response: " + jsonResponse);

                    var jsonObject = JObject.Parse(jsonResponse);

                    var items = jsonObject.SelectTokens("$.contents.twoColumnSearchResultsRenderer.primaryContents.sectionListRenderer.contents[*].itemSectionRenderer.contents[*].videoRenderer");

                    if (items == null || !items.Any())
                    {

                        items = jsonObject.SelectTokens("..videoRenderer");

                        if (items == null || !items.Any())
                        {
                            Debug.WriteLine("No videoRenderer found in continuation response.");
                            _hasMoreResults = false;
                            return;
                        }
                    }

                    if (items != null)
                    {
                        var videos = new List<SearchVideoDetail>();

                        foreach (var item in items)
                        {
                            var videoResult = new SearchVideoDetail
                            {
                                VideoId = item.SelectToken("videoId")?.ToString(),
                                Title = item.SelectToken("title.runs[0].text")?.ToString(),
                                Thumbnail = item.SelectToken("thumbnail.thumbnails[1].url")?.ToString(),
                                Author = item.SelectToken("ownerText.runs[0].text")?.ToString(),
                                Views = item.SelectToken("viewCountText.simpleText")?.ToString(),
                                Date = item.SelectToken("publishedTimeText.simpleText")?.ToString(),
                                Length = item.SelectToken("lengthText.simpleText")?.ToString()
                            };

                            if (videoResult != null)
                            {
                                videos.Add(videoResult);
                                AddVideoCard(videoResult, SearchItemsControl);
                                _searchResults.Add(videoResult);
                            }
                        }

                        string videoSearchDetailsJson = Newtonsoft.Json.JsonConvert.SerializeObject(videos, Newtonsoft.Json.Formatting.Indented);
                        Debug.WriteLine("Video Details JSON: " + videoSearchDetailsJson);

                        const string continuationCommandKey = "\"continuationCommand\": {";
                        const string tokenKey = "\"token\": \"";

                        int startIndex = jsonResponse.IndexOf(continuationCommandKey);
                        if (startIndex != -1)
                        {
                            startIndex = jsonResponse.IndexOf(tokenKey, startIndex);
                            if (startIndex != -1)
                            {
                                startIndex += tokenKey.Length;
                                int endIndex = jsonResponse.IndexOf("\"", startIndex);
                                if (endIndex != -1)
                                {
                                    _continuationToken = jsonResponse.Substring(startIndex, endIndex - startIndex);
                                    _hasMoreResults = !string.IsNullOrEmpty(_continuationToken);
                                    Debug.WriteLine("Continuation token extracted: " + _continuationToken);
                                }
                            }
                        }
                        else
                        {
                            _hasMoreResults = false;
                            Debug.WriteLine("Continuation command not found in the response.");
                        }
                    }

                    else
                    {
                        Debug.WriteLine("No items found in the response.");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Error fetching search results: " + ex.Message);
                }
                finally
                {
                    BubbleLoadingAnimation.Stop();
                    LoadingPanel.Visibility = Visibility.Collapsed;
                    LoadMore.Visibility = _hasMoreResults ? Visibility.Visible : Visibility.Collapsed;
                    _isSearching = false;
                }
            }
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

        private async void LoadMoreButton_Click(object sender, RoutedEventArgs e)
        {

            Debug.WriteLine("Loading more results...");

            if (_hasMoreResults && !string.IsNullOrEmpty(_continuationToken))
            {
                Debug.WriteLine("Loading more results...");
                await SearchAsync(SearchTextBox.Text, _continuationToken);
            }
        }

        private void AddVideoCard(SearchVideoDetail video, ItemsControl itemsControl)
        {
            var videoCard = CreateVideoCard(video);
            itemsControl.Items.Add(videoCard);
        }

        private void Downloads_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            Frame.Navigate(typeof(DownloadsPage));
        }

        private void History_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            Frame.Navigate(typeof(WatchHistory));
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

        private Border CreateVideoCard(SearchVideoDetail video)
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
                Source = new BitmapImage(new Uri(video.Thumbnail, UriKind.Absolute)),
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



    }
}