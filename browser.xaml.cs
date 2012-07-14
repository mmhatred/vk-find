using System.Windows;

namespace vk1
{
    /// <summary>
    /// Interaction logic for browser.xaml
    /// </summary>
    public partial class browser : Window
    {
        /// <summary>
        /// Authorization url
        /// </summary>
        private string url;

        /// <summary>
        /// Parent window
        /// </summary>
        private MainWindow parent;

        /// <summary>
        /// Initializes a new instance of <see cref="browser"/> class
        /// </summary>
        /// <param name="p">parent window</param>
        public browser(MainWindow p)
        {
            InitializeComponent();
            parent = p;
            url = "https://oauth.vk.com/authorize?client_id=2995620&scope=friends&redirect_uri=https://oauth.vk.com/blank.html&display=page&response_type=token";
        } // browser

        /// <summary>
        /// Navigates browser to url after loading
        /// </summary>
        /// <param name="sender">event source</param>
        /// <param name="e">event parametres</param>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Browser.Navigate(url);
        } // Window_Loaded

        /// <summary>
        /// Occurs when url loaded
        /// </summary>
        /// <param name="sender">event source</param>
        /// <param name="e">event parametres</param>
        private void Browser_LoadCompleted(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            // If there is "access_token" substring assume login succeeded
            if (e.Uri.AbsoluteUri.IndexOf("access_token") != -1)
            {
                parent.Response = e.Uri.AbsoluteUri;
                Close();
            } // if (e.Uri.AbsoluteUri.IndexOf("access_token") != -1)
        } // Browser_LoadCompleted
    } // public partial class browser : Window
} // namespace vk1