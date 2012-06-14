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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net;
using System.Xml;

namespace vk1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    ///
    public partial class MainWindow : Window
    {
        string id; //айди пользователя
        string token; //токен для работы с контактом
        public string Response { get; set; } //ответ сервера авторизации
        Point mouse;
        User currentUser, findingUser;
        public MainWindow()
        {
            InitializeComponent();
            mouse = new Point();
            LoginButton.Focus();
        }
        //нажатие на кнопку входа
        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            //открываем страницу авторизации в новом окне -- класс browser
            browser b = new browser(this);
            b.ShowDialog();
            //в случае успешной авторизации в Response сохраняется урла, на которую на перенаправил сервер
            if (Response != null)
            {
                //вытаскиваем из ответа токен и айди
                Response = Response.Substring(Response.IndexOf('#') + 1);
                string[] parsedAns = Response.Split('&');
                foreach (var s in parsedAns)
                {
                    if (s.IndexOf("user_id") == 0)
                    {
                        id = s.Substring("user_id=".Length);
                    }
                    else if (s.IndexOf("access_token") == 0)
                    {
                        token = s.Substring("access_token=".Length);
                    }
                }
                ErrorText.Content = "Шаг 2: введите адрес и нажмите Проверить";
                ErrorText.Foreground = new SolidColorBrush(Color.FromRgb(0, 0, 0));
                //получаем имя текущего пользователя и ставим его в заголовок окна
                
                //строим строку запроса
                string req = "https://api.vk.com/method/";
                req += "users.get.xml?";
                req += "uids=" + id + "&";
                req += "first_name,last_name&";
                req += "access_token=" + token;
                //создаем запрос
                WebRequest request = WebRequest.Create(req);
                //отправляем запрос и получаем результат
                WebResponse response = request.GetResponse();
                //запоминаем ответ в XmlDocument
                XmlDocument xml = new XmlDocument();
                xml.LoadXml(new System.IO.StreamReader(response.GetResponseStream()).ReadToEnd());
                //вытаскиваем из xml имя и фамилию (response->user->first_name/last_name)
                this.TitleLabel.Content = "Find: " + xml["response"]["user"]["first_name"].InnerText +
                    " " + xml["response"]["user"]["last_name"].InnerText;
                XmlNode node = xml["response"]["user"];
                currentUser = new User(node, 0);
                FindIDTextBox.Focus();
            }
            else ErrorText.Foreground = new SolidColorBrush(Color.FromRgb(223, 0, 0)); 
        }
        //нажатие на кнопку закрытия окна
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        //когда левая кнопка мыши нажата на форме, запоминаем координаты
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            mouse = e.GetPosition(null);
        }
        //если в момент движения нажата левая кнопка, двигаем окно
        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                //не обрабатываем нажатия по активным элементам окна
                if (e.Device.Target == this.CloseButton ||
                    e.Device.Target == this.LoginButton ||
                    e.Device.Target == this.FindIDTextBox ||
                    e.Device.Target == this.CheckButton ||
                    e.Device.Target == this.FindButton) return;
                Point New = e.GetPosition(null);
                this.Left += New.X - mouse.X;
                this.Top += New.Y - mouse.Y;
            }
        }
        //при клике по кнопке "Проверить" вытаскиваем имя или айди пользователя, и проверяем, есть ли такой
        private void CheckButton_Click(object sender, RoutedEventArgs e)
        {
            if (token == null || currentUser == null)
            {
                ErrorText.Foreground = new SolidColorBrush(Color.FromRgb(223, 0, 0));
                LoginButton.Focus();
                System.Media.SystemSounds.Exclamation.Play();
                return;
            }
            string req = "https://api.vk.com/method/users.get.xml?";
            req += "uids=" + FindIDTextBox.Text + "&fields=first_name,last_name&access_token=" + token;
            WebRequest request = WebRequest.Create(req);
            WebResponse response = request.GetResponse();
            XmlDocument xml = new XmlDocument();
            xml.LoadXml(new System.IO.StreamReader(response.GetResponseStream()).ReadToEnd());
            if (xml["error"] != null)
            {
                string errorCode = xml["error"]["error_code"].InnerText;
                ErrorText.Content = "Шаг 2: введите адрес и нажмите Проверить";
                System.Media.SystemSounds.Exclamation.Play();
                findingUser = null;
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
                }
            }
            if (xml["response"]["user"]["first_name"].InnerText == "DELETED" &&
                xml["response"]["user"]["last_name"].InnerText == "")
            {
                System.Media.SystemSounds.Exclamation.Play();
                MessageBox.Show("Такой страницы не существует", "Ошибка");
                ErrorText.Content = "Шаг 2: введите адрес и нажмите Проверить";
                ErrorText.Foreground = new SolidColorBrush(Color.FromRgb(0, 0, 0));
                findingUser = null;
            }
            else
            {
                findingUser = new User(xml["response"]["user"], 1);
                ErrorText.Content = "Шаг 3: нажмите Найти путь";
                ErrorText.Foreground = new SolidColorBrush(Color.FromRgb(0, 0, 0));
                MessageBox.Show(xml["response"]["user"]["first_name"].InnerText +
                    " " + xml["response"]["user"]["last_name"].InnerText, "Пользователь найден");
                FindButton.Focus();
            }
        }

        private void FindIDTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                CheckButton_Click(sender, new RoutedEventArgs(null, e));
            }
        }
        //при клике по кнопке поиска запускаем волновой алгоритм
        private void FindButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentUser == null || token == null)
            {
                ErrorText.Content = "Шаг 1: войдите ВКонтакте, нажав Войти";
                ErrorText.Foreground = new SolidColorBrush(Color.FromRgb(223, 0, 0));
                LoginButton.Focus();
                System.Media.SystemSounds.Exclamation.Play();
                return;
            }
            if (findingUser == null)
            {
                ErrorText.Content = "Шаг 2: введите адрес и нажмите Проверить";
                ErrorText.Foreground = new SolidColorBrush(Color.FromRgb(223, 0, 0));
                System.Media.SystemSounds.Exclamation.Play();
                FindIDTextBox.Focus();
                return;
            }
            
        }

    }
}
