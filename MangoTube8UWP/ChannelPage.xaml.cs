using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TimedText.Formatting;
using Windows.Graphics.Display;
using Windows.Phone.UI.Input;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using static MangoTube8UWP.YouTubeModal;

namespace MangoTube8UWP
{
    public partial class ChannelPage : Page
    {

        private ObservableCollection<SearchVideoDetail> Videos { get; set; } = new ObservableCollection<SearchVideoDetail>();

        private string BrowseID;
        private string LoadMoreToken;
        private bool IsVideosLoaded = false;

        public ChannelPage()
        {
            InitializeComponent();

            DisplayInformation.AutoRotationPreferences = DisplayOrientations.Portrait;

        }

        private DateTime lastTappedTime = DateTime.MinValue;


        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            HardwareButtons.BackPressed += HardwareButtons_BackPressed;

            var queryString = e.Parameter as string;

            if (string.IsNullOrEmpty(queryString))
            {
                Debug.WriteLine("Query string is null or empty.");
                return;
            }

            var browseId = GetQueryStringParameter(queryString, "browseId");

            if (string.IsNullOrEmpty(browseId))
            {
                Debug.WriteLine("browseId is null or empty.");
                return;
            }

            BrowseID = browseId;

            Debug.WriteLine("Received browseId: " + browseId);

            PopulateBannerStuff(browseId);
        }

        private string GetQueryStringParameter(string queryString, string parameterName)
        {
            if (string.IsNullOrEmpty(queryString))
                return null;

            var queryParams = queryString.TrimStart('?').Split('&');
            foreach (var param in queryParams)
            {
                var keyValue = param.Split('=');
                if (keyValue.Length == 2 && keyValue[0] == parameterName)
                {
                    return Uri.UnescapeDataString(keyValue[1]);
                }
            }

            return null;
        }


        private void PopulateBannerStuff(string browseId)
        {

            string url = $"https://www.youtube.com/youtubei/v1/browse?key={Settings.InnerTubeApiKey}";

            var clientContext = new
            {
                hl = "en",
                gl = "US",
                clientName = "MWEB",
                clientVersion = "2.20210711.08.00",
                userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/89.0.4389.82 Safari/537.36",
            };

            var requestBody = new
            {
                browseId = browseId,
                context = new
                {
                    client = clientContext
                }
            };
            Task.Run(async () =>
            {
                using (var httpClient = new HttpClient())
                {
                    try
                    {
                        string jsonRequest = JsonConvert.SerializeObject(requestBody);
                        var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
                        var response = httpClient.PostAsync(url, content).Result;
                        if (response.IsSuccessStatusCode)
                        {
                            string jsonResponse = response.Content.ReadAsStringAsync().Result;
                            Debug.WriteLine("PopulateBannerAndData JSON Response:");
                            Debug.WriteLine(jsonResponse);
                            var jsonObject = JObject.Parse(jsonResponse);

                            var channelName = jsonObject.SelectToken("metadata.channelMetadataRenderer.title")?.ToString();

                            if (!string.IsNullOrEmpty(channelName))
                            {
                                Debug.WriteLine("Found channel name: " + channelName);
                                
                                await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                                {
                                    ChannelName.Text = channelName;
                                    ChannelNameAbout.Text = channelName;
                                });
                            }
                            else
                            {
                                Debug.WriteLine("Channel name not found.");
                            }


                            var avatarUrl = jsonObject.SelectToken("metadata.channelMetadataRenderer.avatar.thumbnails[0].url")?.ToString();
                            if (!string.IsNullOrEmpty(avatarUrl))
                            {
                                Debug.WriteLine("Avatar URL: " + avatarUrl);
                                await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                                {
                                    try
                                    {
                                        Uri avatarUri = new Uri(avatarUrl, UriKind.Absolute);
                                        BitmapImage bitmapImage = new BitmapImage(avatarUri);
                                        ProfilePicture.Source = bitmapImage;
                                        Debug.WriteLine("Profile picture set successfully.");
                                    }
                                    catch (Exception ex)
                                    {
                                        Debug.WriteLine("Error setting image source: " + ex.Message);
                                    }
                                });
                            }
                            else
                            {
                                Debug.WriteLine("Avatar URL not found.");
                            }

                            var bannerUrl = jsonObject.SelectToken("..banner.imageBannerViewModel.image.sources[0].url")?.ToString();
                            if (!string.IsNullOrEmpty(bannerUrl))
                            {
                                await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                                {
                                    BannerImage.Source = new Windows.UI.Xaml.Media.Imaging.BitmapImage(new Uri(bannerUrl, UriKind.Absolute));
                                });
                                Debug.WriteLine("Banner URL: " + bannerUrl);
                            }
                            else
                            {
                                Debug.WriteLine("Banner URL not found.");
                            }

                            var continuationToken = FindLastTokenProperty(jsonObject);
                            if (!string.IsNullOrEmpty(continuationToken))
                            {
                                Debug.WriteLine("Found continuation token: " + continuationToken);
                                FetchSubscriberAndViewCounts(continuationToken, httpClient);
                            }
                            else
                            {
                                Debug.WriteLine("No continuation token found");
                            }
                        }
                        else
                        {
                            Debug.WriteLine(string.Format("Error in PopulateBannerAndData: {0}", response.StatusCode));
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(string.Format("Exception in PopulateBannerAndData: {0}", ex.Message));
                    }
                    finally
                    {

                    }
                }
            });
        }

        private async void FetchSubscriberAndViewCounts(string continuationToken, HttpClient httpClient)
        {
            string url = $"https://www.youtube.com/youtubei/v1/browse?key={Settings.InnerTubeApiKey}";

            var continuationRequestBody = new
            {
                continuation = continuationToken,
                context = new
                {
                    client = new
                    {
                        hl = "en",
                        gl = "US",
                        clientName = "MWEB",
                        clientVersion = "2.20210711.08.00",
                        userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/89.0.4389.82 Safari/537.36",
                    }
                }
            };
            string jsonRequest = JsonConvert.SerializeObject(continuationRequestBody);
            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
            try
            {
                var response = httpClient.PostAsync(url, content).Result;
                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = response.Content.ReadAsStringAsync().Result;
                    Debug.WriteLine("FetchSubscriberAndViewCounts JSON Response:");
                    Debug.WriteLine(jsonResponse);
                    var jsonObject = JObject.Parse(jsonResponse);

                    Debug.WriteLine("Parsing subscriber count...");
                    var subscriberCountText = jsonObject.SelectToken("onResponseReceivedEndpoints[0].appendContinuationItemsAction.continuationItems[0].aboutChannelRenderer.metadata.aboutChannelViewModel.subscriberCountText")?.ToString();
                    if (!string.IsNullOrEmpty(subscriberCountText))
                    {
                        string formattedSubscriberCount = ConvertSubscriberCount(subscriberCountText);
                        await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                        {
                            ChannelSubs.Text = formattedSubscriberCount;
                            ChannelSubsAbout.Text = formattedSubscriberCount;
                        });
                    }

                    Debug.WriteLine("Parsing descrription count...");
                    var description = jsonObject.SelectToken("onResponseReceivedEndpoints[0].appendContinuationItemsAction.continuationItems[0].aboutChannelRenderer.metadata.aboutChannelViewModel.description")?.ToString();
                    if (!string.IsNullOrEmpty(description))
                    {
                        await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                        {
                            SetVideoDescriptionWithLinks(description);
                        });
                    }

                    Debug.WriteLine("Searching for viewCountText...");
                    var viewCounts = SearchForProperty(jsonObject, "viewCountText");
                    if (viewCounts.Count > 0)
                    {
                        Debug.WriteLine("Found viewCountText values:");
                        foreach (var viewCount in viewCounts)
                        {
                            Debug.WriteLine(viewCount);
                        }
                        await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                         {
                             TotalViews.Text = FormatNumber(viewCounts.FirstOrDefault()?.ToString() ?? string.Empty) + " videos views";
                             ChannelViewsAbout.Text = FormatNumber(viewCounts.FirstOrDefault()?.ToString() ?? string.Empty) + " videos views";
                         });
                    }
                }
                else
                {
                    Debug.WriteLine(string.Format("Error in FetchSubscriberAndViewCounts: {0}", response.StatusCode));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(string.Format("Exception in FetchSubscriberAndViewCounts: {0}", ex.Message));
            }
        }

        private string ConvertSubscriberCount(string subscriberCountText)
        {
            string count = subscriberCountText.Replace("subscribers", "").Trim();
            if (count.EndsWith("K"))
            {
                double value = double.Parse(count.TrimEnd('K')) * 1000;
                return string.Format("{0:n0} subscribers", value);
            }
            else if (count.EndsWith("M"))
            {
                double value = double.Parse(count.TrimEnd('M')) * 1000000;
                return string.Format("{0:n0} subscribers", value);
            }
            else if (count.EndsWith("B"))
            {
                double value = double.Parse(count.TrimEnd('B')) * 1000000000;
                return string.Format("{0:n0} subscribers", value);
            }
            else
            {
                return string.Format("{0:n0} subscribers", double.Parse(count));
            }
        }

        private List<string> SearchForProperty(JToken jsonObject, string propertyName)
        {
            var results = new List<string>();
            if (jsonObject.Type == JTokenType.Object)
            {
                foreach (var property in jsonObject.Children<JProperty>())
                {
                    if (property.Name.Contains(propertyName))
                    {
                        var value = property.Value.ToString();
                        results.Add(value);
                    }
                    results.AddRange(SearchForProperty(property.Value, propertyName));
                }
            }
            else if (jsonObject.Type == JTokenType.Array)
            {
                foreach (var item in jsonObject.Children())
                {
                    results.AddRange(SearchForProperty(item, propertyName));
                }
            }
            return results;
        }

        private string FormatNumber(string numberText)
        {
            var numericPart = new string(numberText.Where(char.IsDigit).ToArray());
            if (numericPart.Length > 0)
            {
                int number;
                if (int.TryParse(numericPart, out number))
                {
                    return number.ToString("N0");
                }
                double doubleNumber;
                if (double.TryParse(numericPart, out doubleNumber))
                {
                    return doubleNumber.ToString("N0");
                }
            }
            return numberText;
        }

        private async Task PopulateVideos(string browseId)
        {
            GridLand.Visibility = Visibility.Collapsed;
            InfoBar.Visibility = Visibility.Collapsed;
            LoadingPanelCha.Visibility = Visibility.Visible;
            BubbleLoadingAnimationChannel.Begin();
            LoadMore.Visibility = Visibility.Collapsed;

            string url = $"https://www.youtube.com/youtubei/v1/browse?key={Settings.InnerTubeApiKey}";

            var videos = new List<SearchVideoDetail>();

            var clientContext = new
            {
                hl = "en",
                gl = "US",
                clientName = "MWEB",
                clientVersion = "2.20210711.08.00",
                userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/89.0.4389.82 Safari/537.36",
            };

            var requestBody = new
            {
                browseId = browseId,
                context = new
                {
                    client = clientContext
                }
            };
            using (var httpClient = new HttpClient())
            {
                try
                {
                    string jsonRequest = JsonConvert.SerializeObject(requestBody);
                    var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
                    var response = await httpClient.PostAsync(url, content);
                    if (response.IsSuccessStatusCode)
                    {
                        string jsonResponse = await response.Content.ReadAsStringAsync();
                        Debug.WriteLine("PopulateVideos JSON Response:");
                        Debug.WriteLine(jsonResponse);
                        var jsonData = JObject.Parse(jsonResponse);
                        var videoDetails = jsonData.SelectTokens("..compactVideoRenderer");
                        var author = ExtractFirstAuthor(jsonData);

                        var continuationToken = FindFirstContinuation(jsonData)?.ToString();

                        if (!string.IsNullOrEmpty(continuationToken))
                        {
                            LoadMoreToken = continuationToken;
                        }

                        foreach (var video in videoDetails)
                        {
                            string videoId = video.SelectToken("videoId")?.ToString();

                            var videoDetail = new SearchVideoDetail
                            {
                                VideoId = videoId,
                                Title = video.SelectToken("title.runs[0].text")?.ToString(),
                                Author = author,
                                Thumbnail = video.SelectToken("thumbnail.thumbnails[0].url")?.ToString(),
                                Views = video.SelectToken("viewCountText.runs[0].text")?.ToString(),
                                Length = video.SelectToken("lengthText.runs[0].text")?.ToString(),
                                Date = video.SelectToken("publishedTimeText.runs[0].text")?.ToString(),
                            };

                            videos.Add(videoDetail);
                            AddVideoCard(videoDetail, VideosItemsControl);
                            Videos.Add(videoDetail);
                        }

                        string videoDetailsJson = Newtonsoft.Json.JsonConvert.SerializeObject(videos, Newtonsoft.Json.Formatting.Indented);
                        Debug.WriteLine("Video Details JSON: " + videoDetailsJson);

                    
                    }
                    else
                    {
                        Debug.WriteLine($"Error in PopulateVideos: {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Exception in PopulateVideos: {ex.Message}");
                }
                finally
                {
                    GridLand.Visibility = Visibility.Visible;
                    InfoBar.Visibility = Visibility.Visible;
                    LoadingPanelCha.Visibility = Visibility.Collapsed;
                    LoadMore.Visibility = Visibility.Visible;
                    BubbleLoadingAnimationChannel.Stop();
                    IsVideosLoaded = true;
                }
            }
        }

        private async Task PopulateVideosContinued(string continuationToken)
        {

            string url = $"https://www.youtube.com/youtubei/v1/browse?key={Settings.InnerTubeApiKey}";

            var videos = new List<SearchVideoDetail>();

            var clientContext = new
            {
                hl = "en",
                gl = "US",
                clientName = "MWEB",
                clientVersion = "2.20210711.08.00",
                userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/89.0.4389.82 Safari/537.36",
            };

            var requestBody = new
            {
                browseId = BrowseID,
                continuation = continuationToken,
                context = new
                {
                    client = clientContext
                }
            };

            using (var httpClient = new HttpClient())
            {
                try
                {
                    string jsonRequest = JsonConvert.SerializeObject(requestBody);
                    var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                    var response = await httpClient.PostAsync(url, content);

                    if (response.IsSuccessStatusCode)
                    {
                        string jsonResponse = await response.Content.ReadAsStringAsync();
                        Debug.WriteLine("PopulateVideosContinued JSON Response:");
                        Debug.WriteLine(jsonResponse);

                        var jsonData = JObject.Parse(jsonResponse);
                        var newContinuationToken = ExtractContinuationToken(jsonData.ToString());

                        if (!string.IsNullOrEmpty(newContinuationToken))
                        {
                            LoadMoreToken = newContinuationToken;
                            Debug.WriteLine($"New continuation token: {LoadMoreToken}");
                        }
                        else
                        {
                            Debug.WriteLine("No continuation token found.");
                        }
           
                        Debug.WriteLine("PopulateVideos JSON Response:");
                        Debug.WriteLine(jsonResponse);
                        var videoDetails = jsonData.SelectTokens("..richItemRenderer.content.videoWithContextRenderer");

                        foreach (var video in videoDetails)
                        {
                            string videoId = video.SelectToken("navigationEndpoint.watchEndpoint.videoId")?.ToString();
                            var videoDetail = new SearchVideoDetail
                            {
                                VideoId = videoId,
                                Title = video.SelectToken("headline.runs[0].text")?.ToString(),
                                Author = ChannelName.Text,
                                Thumbnail = video.SelectToken("thumbnail.thumbnails[0].url")?.ToString(),
                                Views = video.SelectToken("shortViewCountText.runs[0].text")?.ToString(),
                                Length = video.SelectToken("lengthText.runs[0].text")?.ToString(),
                                Date = video.SelectToken("publishedTimeText.runs[0].text")?.ToString()
                            };

                            videos.Add(videoDetail);
                            AddVideoCard(videoDetail, VideosItemsControl);
                            Videos.Add(videoDetail);
                        }

                        string videoDetailsJson = Newtonsoft.Json.JsonConvert.SerializeObject(videos, Newtonsoft.Json.Formatting.Indented);
                        Debug.WriteLine("Video Details JSON Con: " + videoDetailsJson);

                    }
                    else
                    {
                        Debug.WriteLine($"Error in PopulateVideosContinued: {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Exception in PopulateVideosContinued: {ex.Message}");
                }
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

        private void SetVideoDescriptionWithLinks(string description)
        {
            VideoDescription.Blocks.Clear();

            var paragraph = new Windows.UI.Xaml.Documents.Paragraph();

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


        private string ExtractFirstAuthor(JToken jsonData)
        {
            try
            {
                var channelTitleToken = jsonData.SelectToken("metadata.channelMetadataRenderer.title");
                if (channelTitleToken != null)
                {
                    var channelTitle = channelTitleToken.ToString();
                    if (!string.IsNullOrEmpty(channelTitle))
                    {
                        Debug.WriteLine($"Found Channel Title: {channelTitle}");
                        return channelTitle;
                    }
                }
                var results = jsonData.SelectTokens("..longBylineText.runs.text");
                Debug.WriteLine($"Found {results.Count()} potential authors in the JSON data.");
                foreach (var result in results)
                {
                    var author = result?.ToString();
                    if (!string.IsNullOrEmpty(author))
                    {
                        Debug.WriteLine($"Found Author: {author}");
                        return author;
                    }
                }
                Debug.WriteLine("No valid author or channel title found, returning 'Unknown'");
                return "Unknown";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception in ExtractFirstAuthorOrChannelTitle: {ex.Message}");
                return "Unknown";
            }
        }

        private string FindFirstContinuation(JToken token)
        {
            if (token.Type == JTokenType.Object)
            {
                JObject obj = (JObject)token;
                foreach (var property in obj.Properties())
                {
                    if (property.Name == "continuation")
                    {
                        return property.Value.ToString();
                    }

                    var result = FindFirstContinuation(property.Value);
                    if (!string.IsNullOrEmpty(result))
                    {
                        return result; 
                    }
                }
            }

            else if (token.Type == JTokenType.Array)
            {
                JArray array = (JArray)token;
                foreach (var item in array)
                {
                    var result = FindFirstContinuation(item);
                    if (!string.IsNullOrEmpty(result))
                    {
                        return result; 
                    }
                }
            }

            return null; 
        }

        private string FindLastTokenProperty(JToken token)
        {
            List<string> tokens = new List<string>();

            if (token.Type == JTokenType.Object)
            {
                JObject obj = (JObject)token;
                foreach (var property in obj.Properties())
                {
                    if (property.Name.Equals("token", StringComparison.OrdinalIgnoreCase) && property.Value.Type == JTokenType.String)
                    {
                        tokens.Add(property.Value.ToString());
                    }

                    var result = FindLastTokenProperty(property.Value);
                    if (result != null)
                    {
                        tokens.Add(result);
                    }
                }
            }

            else if (token.Type == JTokenType.Array)
            {
                JArray array = (JArray)token;
                foreach (var item in array)
                {
                    var result = FindLastTokenProperty(item);
                    if (result != null)
                    {
                        tokens.Add(result);
                    }
                }
            }

            return tokens.Count > 0 ? tokens[tokens.Count - 1] : null;
        }

        private string FindFirstTokenProperty(JToken token)
        {
            if (token.Type == JTokenType.Object)
            {
                JObject obj = (JObject)token;
                foreach (var property in obj.Properties())
                {
                    if (property.Name.Equals("token", StringComparison.OrdinalIgnoreCase) && property.Value.Type == JTokenType.String)
                    {
                        return property.Value.ToString();
                    }
                    var result = FindFirstTokenProperty(property.Value);
                    if (result != null)
                    {
                        return result;
                    }
                }
            }
            else if (token.Type == JTokenType.Array)
            {
                JArray array = (JArray)token;
                foreach (var item in array)
                {
                    var result = FindFirstTokenProperty(item);
                    if (result != null)
                    {
                        return result;
                    }
                }
            }
            return null;
        }


        private async void LoadMoreButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(LoadMoreToken))
            {
                await PopulateVideosContinued(LoadMoreToken);
            }
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
                Frame.Navigate(typeof(VideoPage), queryString);  // Passing query string only
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception caught in Border_Tap: {ex.Message}");
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

        private void AddVideoCard(SearchVideoDetail video, ItemsControl itemsControl)
        {
            var videoCard = CreateVideoCard(video);
            itemsControl.Items.Add(videoCard);
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

        private async void MainPivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedPivotItem = MainPivot.SelectedItem as PivotItem;

            if (selectedPivotItem != null)
            {
                Debug.WriteLine($"Selected Pivot Item: {selectedPivotItem.Tag}");

                switch (selectedPivotItem.Tag?.ToString())
                {
                    case "videos":
                        Debug.WriteLine("Loading videos...");
                        await PopulateVideos(BrowseID);
                        break;

                    case "about":
                        Debug.WriteLine("About section selected");
                        break;

                    default:
                        Debug.WriteLine("Unknown Pivot Item");
                        break;
                }
            }
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