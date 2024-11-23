using System;
using System.Diagnostics;
using Windows.Graphics.Display;
using Windows.Phone.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
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