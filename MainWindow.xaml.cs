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
        Point mouse; //координаты мыши, нужны для перемещения окна по экрану
        User currentUser, findingUser; //текущий и искомый пользователи
        public MainWindow()
        {
            InitializeComponent();
            mouse = new Point();
            //устанавливаем фокус на кнопку входа
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
                    } // if (s.IndexOf("user_id") == 0)
                    else if (s.IndexOf("access_token") == 0)
                    {
                        token = s.Substring("access_token=".Length);
                    } // if (s.IndexOf("access_token") == 0)
                } // foreach (var s in parsedAns)
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
                //создаем текущего пользователя
                currentUser = new User(node, 0);
                //ставим фокус в строку ввода айди искомого пользователя
                FindIDTextBox.Focus();
            } // if (Response != null)
            //иначе напоминаем, что сначала надо войти
            else ErrorText.Foreground = new SolidColorBrush(Color.FromRgb(223, 0, 0)); 
        } // LoginButton_Click
        //нажатие на кнопку закрытия окна
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        } // CloseButton_Click
        //когда левая кнопка мыши нажата на форме, запоминаем координаты
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            mouse = e.GetPosition(null);
        } // Window_MouseLeftButtonDown
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
                //прибавляем к текущим координатам разность между старыми и новыми координатами мыши
                Point New = e.GetPosition(null);
                this.Left += New.X - mouse.X;
                this.Top += New.Y - mouse.Y;
            } // if (e.LeftButton == MouseButtonState.Pressed)
        } // Window_MouseMove
        //при клике по кнопке "Проверить" вытаскиваем имя или айди пользователя, и проверяем, есть ли такой
        private void CheckButton_Click(object sender, RoutedEventArgs e)
        {
            //если еще не вошли в контакт, напоминаем об этом
            if (token == null || currentUser == null)
            {
                ErrorText.Foreground = new SolidColorBrush(Color.FromRgb(223, 0, 0));
                LoginButton.Focus();
                System.Media.SystemSounds.Exclamation.Play();
                return;
            } // if (token == null || currentUser == null)
            //строим запрос, отправляем его и парсим ответ
            string req = "https://api.vk.com/method/users.get.xml?";
            req += "uids=" + FindIDTextBox.Text + "&fields=first_name,last_name&access_token=" + token;
            WebRequest request = WebRequest.Create(req);
            WebResponse response = request.GetResponse();
            XmlDocument xml = new XmlDocument();
            xml.LoadXml(new System.IO.StreamReader(response.GetResponseStream()).ReadToEnd());
            //если в ответе пришла ошибка, выводим ее пользователю
            if (xml["error"] != null)
            {
                string errorCode = xml["error"]["error_code"].InnerText;
                ErrorText.Content = "Шаг 2: введите адрес и нажмите Проверить";
                System.Media.SystemSounds.Exclamation.Play();
                //если перед этим кого-то нашли, то надо его удалить
                findingUser = null;
                //выводим подробную информацию об ошибке
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
            //если пришла информация о пользователе DELETED, то такой страницы нет
            if (xml["response"]["user"]["first_name"].InnerText == "DELETED" &&
                xml["response"]["user"]["last_name"].InnerText == "")
            {
                System.Media.SystemSounds.Exclamation.Play();
                MessageBox.Show("Такой страницы не существует", "Ошибка");
                ErrorText.Content = "Шаг 2: введите адрес и нажмите Проверить";
                ErrorText.Foreground = new SolidColorBrush(Color.FromRgb(0, 0, 0));
                //если перед этим кого-то нашли, то надо его удалить
                findingUser = null;
            } // if first_name == DELETED && last_name == ""
            //иначе запоминаем найденного пользователя и переходим к шагу 3
            else
            {
                findingUser = new User(xml["response"]["user"], 1);
                ErrorText.Content = "Шаг 3: нажмите Найти путь";
                ErrorText.Foreground = new SolidColorBrush(Color.FromRgb(0, 0, 0));
                MessageBox.Show(xml["response"]["user"]["first_name"].InnerText +
                    " " + xml["response"]["user"]["last_name"].InnerText, "Пользователь найден");
                //предлагаем начать поиск
                FindButton.Focus();
            } // if !(if first_name == DELETED && last_name == "")
        } // CheckButton_Click
        //если во время ввода текста в поле ввода айди нажат Enter, нажимаем Проверить
        private void FindIDTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                CheckButton_Click(sender, new RoutedEventArgs(null, e));
            } // if (e.Key == Key.Enter)
        } // FindIDTextBox_KeyDown
        //при клике по кнопке поиска запускаем волновой алгоритм
        private void FindButton_Click(object sender, RoutedEventArgs e)
        {
            //проверяем, пройден ли шаг 1...
            if (currentUser == null || token == null)
            {
                ErrorText.Content = "Шаг 1: войдите ВКонтакте, нажав Войти";
                ErrorText.Foreground = new SolidColorBrush(Color.FromRgb(223, 0, 0));
                LoginButton.Focus();
                System.Media.SystemSounds.Exclamation.Play();
                return;
            } // if (currentUser == null || token == null)
            // ...и шаг 2
            if (findingUser == null)
            {
                ErrorText.Content = "Шаг 2: введите адрес и нажмите Проверить";
                ErrorText.Foreground = new SolidColorBrush(Color.FromRgb(223, 0, 0));
                System.Media.SystemSounds.Exclamation.Play();
                FindIDTextBox.Focus();
                return;
            } // if (findingUser == null)
            
        } // FindButton_Click

    } // public partial class MainWindow : Window
} // namespace vk1
