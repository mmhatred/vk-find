using System.Net;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml;

namespace vk1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Current user ID
        /// </summary>
        private string id;

        /// <summary>
        /// Token for vk interaction
        /// </summary>
        private string token;

        /// <summary>
        /// Authorization server response
        /// </summary>
        public string Response { get; set; }

        /// <summary>
        /// Mouse coordinates
        /// </summary>
        private Point mouse;

        /// <summary>
        /// Current and searched users
        /// </summary>
        private User currentUser, searchedUser;

        /// <summary>
        /// Initializes a new instance of <see cref="MainWindow"/> class
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            mouse = new Point();
            LoginButton.Focus();
        }

        /// <summary>
        /// Processes Login button click
        /// </summary>
        /// <param name="sender">event source</param>
        /// <param name="e">event parametres</param>
        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            // Open authorization page in new window
            browser b = new browser(this);
            b.ShowDialog();
            // If authorization succeeded in Response we have redirected url
            if (Response != null)
            {
                // getting id & token
                Response = Response.Substring(Response.IndexOf('#') + 1);
                string[] parsedAns = Response.Split('&');
                foreach (var s in parsedAns)
                {
                    if (s.IndexOf("user_id") == 0)
                    {
                        id = s.Substring("user_id=".Length);
                    } // if (s.IndexOf("user_id") == 0)
                    else if (s.IndexOf("access_token") == 0)
                    {
                        token = s.Substring("access_token=".Length);
                    } // if (s.IndexOf("access_token") == 0)
                } // foreach (var s in parsedAns)
                ErrorText.Content = "Шаг 2: введите адрес и нажмите Проверить";
                ErrorText.Foreground = new SolidColorBrush(Color.FromRgb(0, 0, 0));
                
                // get user's name and take it in title
                // create request string
                string req = "https://api.vk.com/method/";
                req += "users.get.xml?";
                req += "uids=" + id + "&";
                req += "first_name,last_name&";
                req += "access_token=" + token;
                // create request
                WebRequest request = WebRequest.Create(req);
                // send request and get response
                WebResponse response = request.GetResponse();
                // save response in Xmlocument
                XmlDocument xml = new XmlDocument();
                xml.LoadXml(new System.IO.StreamReader(response.GetResponseStream()).ReadToEnd());
                // gets name from xml (response->user->first_name/last_name)
                this.TitleLabel.Content = "Find: " + xml["response"]["user"]["first_name"].InnerText +
                    " " + xml["response"]["user"]["last_name"].InnerText;
                XmlNode node = xml["response"]["user"];
                // create current user
                currentUser = new User(node);
                FindIDTextBox.Focus();
            } // if (Response != null)
            // else remember about login
            else ErrorText.Foreground = new SolidColorBrush(Color.FromRgb(223, 0, 0));
        } // LoginButton_Click

        /// <summary>
        /// Processes Close button click
        /// </summary>
        /// <param name="sender">event source</param>
        /// <param name="e">event parametres</param>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        } // CloseButton_Click

        /// <summary>
        /// When left button clicked on form save coordinates
        /// </summary>
        /// <param name="sender">event source</param>
        /// <param name="e">event parametres</param>
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            mouse = e.GetPosition(null);
        } // Window_MouseLeftButtonDown

        /// <summary>
        /// Move window if left button pressed
        /// </summary>
        /// <param name="sender">event source</param>
        /// <param name="e">event parametres</param>
        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                // do not move window if active element clicked
                if (e.Device.Target == this.CloseButton ||
                    e.Device.Target == this.LoginButton ||
                    e.Device.Target == this.FindIDTextBox ||
                    e.Device.Target == this.CheckButton ||
                    e.Device.Target == this.FindButton)
                {
                    return;
                }

                Point New = e.GetPosition(null);
                this.Left += New.X - mouse.X;
                this.Top += New.Y - mouse.Y;
            } // if (e.LeftButton == MouseButtonState.Pressed)
        } // Window_MouseMove
        
        /// <summary>
        /// On Check button click gets id and checks whether such id exists
        /// </summary>
        /// <param name="sender">event source</param>
        /// <param name="e">event parametres</param>
        private void CheckButton_Click(object sender, RoutedEventArgs e)
        {
            // If not logged in remember about it
            if (token == null || currentUser == null)
            {
                ErrorText.Foreground = new SolidColorBrush(Color.FromRgb(223, 0, 0));
                LoginButton.Focus();
                System.Media.SystemSounds.Exclamation.Play();
                return;
            } // if (token == null || currentUser == null)
            // Send request
            string req = "https://api.vk.com/method/users.get.xml?";
            req += "uids=" + FindIDTextBox.Text + "&fields=first_name,last_name&access_token=" + token;
            WebRequest request = WebRequest.Create(req);
            WebResponse response = request.GetResponse();
            XmlDocument xml = new XmlDocument();
            xml.LoadXml(new System.IO.StreamReader(response.GetResponseStream()).ReadToEnd());
            // If there is an error in response show it for user
            if (xml["error"] != null)
            {
                string errorCode = xml["error"]["error_code"].InnerText;
                ErrorText.Content = "Шаг 2: введите адрес и нажмите Проверить";
                System.Media.SystemSounds.Exclamation.Play();
                // If there was earlier finded user delete him
                searchedUser = null;
                // Show info about error
                switch (errorCode)
                {
                    case "113": MessageBox.Show("Такой страницы не существует", "Ошибка");
                        ErrorText.Foreground = new SolidColorBrush(Color.FromRgb(0, 0, 0)); return;
                    case "6": MessageBox.Show("Сервер перегружен запросами, повторите попытку через несколько секунд", "Ошибка"); return;
                    case "5": MessageBox.Show("Ошибка авторизации. Попробуйте войти еще раз", "Ошибка");
                        ErrorText.Content = "Шаг 1: войдите ВКонтакте, нажав Войти"; token = null; currentUser = null;
                        LoginButton.Focus(); return;
                    case "4": goto case "5";
                    case "2": MessageBox.Show("Приложение отключено. Попробуйте повторить попытку через несколько минут", "Ошибка"); return;
                    default: MessageBox.Show("ВКонтакте сообщил о неизвестной ошибке. Попробуйте еще раз", "Ошибка"); return;
                } // switch (errorCode)
            } // if (xml["error"] != null)
            // If user is DELETED there is no such user
            if (xml["response"]["user"]["first_name"].InnerText == "DELETED" &&
                xml["response"]["user"]["last_name"].InnerText == "")
            {
                System.Media.SystemSounds.Exclamation.Play();
                MessageBox.Show("Такой страницы не существует", "Ошибка");
                ErrorText.Content = "Шаг 2: введите адрес и нажмите Проверить";
                ErrorText.Foreground = new SolidColorBrush(Color.FromRgb(0, 0, 0));
                // If there was earlier finded user delete him
                searchedUser = null;
            } // if first_name == DELETED && last_name == ""
            // save finded user and go to step 3
            else
            {
                searchedUser = new User(xml["response"]["user"]);
                ErrorText.Content = "Шаг 3: нажмите Найти путь";
                ErrorText.Foreground = new SolidColorBrush(Color.FromRgb(0, 0, 0));
                MessageBox.Show(xml["response"]["user"]["first_name"].InnerText +
                    " " + xml["response"]["user"]["last_name"].InnerText, "Пользователь найден");
                FindButton.Focus();
            } // if !(if first_name == DELETED && last_name == "")
        } // CheckButton_Click
        
        /// <summary>
        /// If enter pressed when id input check it
        /// </summary>
        /// <param name="sender">event source</param>
        /// <param name="e">event parametres</param>
        private void FindIDTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                CheckButton_Click(sender, new RoutedEventArgs(null, e));
            } // if (e.Key == Key.Enter)
        } // FindIDTextBox_KeyDown

        /// <summary>
        /// On Find button click start search
        /// </summary>
        /// <param name="sender">event source</param>
        /// <param name="e">event parametres</param>
        private void FindButton_Click(object sender, RoutedEventArgs e)
        {
            // Check step 1 
            if (currentUser == null || token == null)
            {
                ErrorText.Content = "Шаг 1: войдите ВКонтакте, нажав Войти";
                ErrorText.Foreground = new SolidColorBrush(Color.FromRgb(223, 0, 0));
                LoginButton.Focus();
                System.Media.SystemSounds.Exclamation.Play();
                return;
            } // if (currentUser == null || token == null)
            // Check step 2
            if (searchedUser == null)
            {
                ErrorText.Content = "Шаг 2: введите адрес и нажмите Проверить";
                ErrorText.Foreground = new SolidColorBrush(Color.FromRgb(223, 0, 0));
                System.Media.SystemSounds.Exclamation.Play();
                FindIDTextBox.Focus();
                return;
            } // if (findingUser == null)
            
        }// FindButton_Click

        /// <summary>
        /// Minimize button click
        /// </summary>
        /// <param name="sender">event source</param>
        /// <param name="e">event parametres</param>
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = System.Windows.WindowState.Minimized;
        } // MinimizeButton_Click

    } // public partial class MainWindow : Window
} // namespace vk1