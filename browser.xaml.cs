using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace vk1
{
    /// <summary>
    /// Interaction logic for browser.xaml
    /// </summary>
    public partial class browser : Window
    {
        string url; // урла авторизации
        MainWindow parent; // родительское окно
        public browser(MainWindow p)
        {
            InitializeComponent();
            parent = p;
            //урл для авторизации
            url = "https://oauth.vk.com/authorize?client_id=2995620&scope=friends&redirect_uri=https://oauth.vk.com/blank.html&display=page&response_type=token";
        } // browser

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //сразу после загрузки окна переходим по адресу авторизации
            Browser.Navigate(url);
        } // Window_Loaded

        private void Browser_LoadCompleted(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            //если в результате авторизации был получен урл с подстрокой
            //access_token, то авторизация прошла успешно - отдаем урл в основную форму
            if (e.Uri.AbsoluteUri.IndexOf("access_token") != -1)
            {
                parent.Response = e.Uri.AbsoluteUri;
                Close();
            } // if (e.Uri.AbsoluteUri.IndexOf("access_token") != -1)
        } // Browser_LoadCompleted
    } // public partial class browser : Window
} // namespace vk1
