using Microsoft.PlayerFramework.CaptionSettings.Model;
using System;
using System.Diagnostics;
using Windows.Graphics.Display;
using Windows.Phone.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace MangoTube8UWP
{
    public partial class SettingsPage : Page
    {

        public SettingsPage()
        {
            InitializeComponent();

            string savedQuality = Settings.VideoQuality;
            cmbCurrFrom.ItemsSource = Settings.Qualities;
            cmbCurrFrom.SelectedItem = savedQuality;

            ComboBoxItem selectedItem = cmbCurrFrom.SelectedItem as ComboBoxItem;
            if (selectedItem != null)
            {
                selectedItem.Foreground = new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 34, 34, 34));
            }


            Debug.WriteLine("Initial video quality set to: " + savedQuality);

            DisplayInformation.AutoRotationPreferences = DisplayOrientations.Portrait;

        }

        private DateTime lastTappedTime = DateTime.MinValue;

        private void cmbCurrFrom_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbCurrFrom.SelectedItem != null)
            {
                string selectedItem = cmbCurrFrom.SelectedItem as string;

                if (!string.IsNullOrEmpty(selectedItem) && selectedItem != Settings.VideoQuality)
                {
                    Settings.VideoQuality = selectedItem;
                    Debug.WriteLine("Selected video quality: " + selectedItem);
                }
            }
        }

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

        private void Downloads_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            Frame.Navigate(typeof(DownloadsPage));
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
            YouTubeLogo.Visibility = Windows.UI.Xaml.Visibility.Visible;
            AccountButton.Visibility = Windows.UI.Xaml.Visibility.Visible;
            SearchTextBox.Visibility = Windows.UI.Xaml.Visibility.Collapsed;
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