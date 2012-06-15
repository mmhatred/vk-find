using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Net;
using System.Xml;
using System.Threading;
using System.Windows.Threading;

namespace vk1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    ///
    public partial class MainWindow : Window
    {
        object locker = new object();//для синхронизации потоков
        string id; //айди пользователя
        string token; //токен для работы с контактом
        public string Response { get; set; } //ответ сервера авторизации
        Point mouse; //координаты мыши, нужны для перемещения окна по экрану
        User currentUser, targetUser; //текущий и искомый пользователи
        uint pagesViewed = 0; //количество просмотренных страниц
        DispatcherTimer timer; // таймер для счетчика страниц
        Thread findThread; // тред, в котором происходит поиск страниц
        private List<User> userschain; // найденная цепочка пользователей
        SortedSet<uint> viewedIDs;
        public delegate void SearchCompleteEventHandler(object sender, EventArgs e); // обработчик заврешниея поиска
        public event SearchCompleteEventHandler SearchComplete; // завершение поиска
        delegate void SetButtonTextInvoker(string text); // делегат для задания текста кнопке старта поиска
        SetButtonTextInvoker SetButtonText; // экземпляр делегата для непосредственно установки


        public MainWindow()
        {
            InitializeComponent();
            mouse = new Point();
            userschain = new List<User>();
            viewedIDs = new SortedSet<uint>();
            //настраиваем таймер
            timer = new DispatcherTimer();
            timer.Interval = new TimeSpan(0, 0, 0, 0, 500);
            timer.Tick += new EventHandler(timer_Tick);
            //добавляем к делегату установки текста на кнопку функцию установки текста
            SetButtonText = setButtonText;
            //устанавливаем фокус на кнопку входа
            LoginButton.Focus();
        } // MainWindow

        //функция, устанавливающая нужный текст на кнопку поиска
        private void setButtonText(string text)
        {
            FindButton.Content = text;
        } // setButtonText

        //обработчик тика таймера
        void timer_Tick(object sender, EventArgs e)
        {
            //показываем что мы не зависли и выводим счетчик просмотренных страниц
            Dispatcher.Invoke(SetButtonText, "Стоп (обработано " + pagesViewed + " страниц)");
        } // timer_Tick


        //обработчик заверщения поиска
        private void StopSearch(object sender, EventArgs e)
        {
            timer.Stop();
            SearchComplete -= StopSearch;
            Dispatcher.Invoke(SetButtonText, "Поиск завершен, вывожу результат...");

            //цепочка имен пользователей
            string chain;
            string req = "";
            WebRequest request;
            WebResponse response;
            List<User>.Enumerator t = userschain.GetEnumerator();
            t.MoveNext();
            req = "https://api.vk.com/method/users.get.xml?uids=" + 
                t.Current.ID + "&access_token=" + token;
            request = WebRequest.Create(req);
            response = request.GetResponse();
            XmlDocument xml = new XmlDocument();
            xml.LoadXml(new System.IO.StreamReader(response.GetResponseStream()).ReadToEnd());
            chain = xml["response"]["user"]["first_name"].InnerText 
                + " " + xml["response"]["user"]["last_name"].InnerText;
            if (userschain.Count > 1)
            {
                req = "https://api.vk.com/method/users.get.xml?uids=";
                while (t.MoveNext())
                {
                    req += t.Current.ID + ",";
                }
                req = req.Substring(0, req.Length - 1);
                req += "&access_token=" + token;
                request = WebRequest.Create(req);
                response = request.GetResponse();
                xml.LoadXml(new System.IO.StreamReader(response.GetResponseStream()).ReadToEnd());
                foreach (XmlNode node in xml["response"])
                {
                    chain += " -> " + node["first_name"].InnerText + " " + node["last_name"].InnerText;
                }
            }
            MessageBox.Show(chain, "Цепочка от вас до искомого пользователя");
            Dispatcher.Invoke(SetButtonText, "Найти путь");
            userschain.Clear();
        } //StopSearch

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
            timer.Stop();
            if (findThread != null) findThread.Abort();
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
                targetUser = null;
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
                targetUser = null;
            } // if first_name == DELETED && last_name == ""
            //иначе запоминаем найденного пользователя и переходим к шагу 3
            else
            {
                targetUser = new User(xml["response"]["user"], 1);
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
            //если поиск еще не запущен, запускаем
            if (FindButton.Content.ToString() == "Найти путь")
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
                if (targetUser == null)
                {
                    ErrorText.Content = "Шаг 2: введите адрес и нажмите Проверить";
                    ErrorText.Foreground = new SolidColorBrush(Color.FromRgb(223, 0, 0));
                    System.Media.SystemSounds.Exclamation.Play();
                    FindIDTextBox.Focus();
                    return;
                } // if (findingUser == null)
                // инициируем поиск
                // подписываемся на событие конца поиска
                SearchComplete += StopSearch;
                findThread = new Thread(Find);
                FindButton.Content = "Стоп (обработано 0 страниц)";
                findThread.Start();
                timer.Start();
            } // if (FindButton.Content == "Найти путь")
           //иначе, если поиск еще не завршен, даём возможность остановить его
            else if (FindButton.Content.ToString().IndexOf("Стоп") != -1)
            {
                //прекращаем поиск
                findThread.Abort();
                timer.Stop();
                FindButton.Content = "Найти путь";
                // TODO: сделать очистку от мусора
            } // if (FindButton.Content.ToString().IndexOf("Стоп") != -1)
        }// FindButton_Click


        //клик по кнопке скрытия окна
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = System.Windows.WindowState.Minimized;
        } // MinimizeButton_Click


        // функция поиска
        private void Find()
        {
            currentUser.ClearFriends();
            targetUser.ClearFriends();
            viewedIDs.Clear();
            pagesViewed = 0;
            userschain.Clear();
            if (currentUser == targetUser)
            {
                userschain.Add(currentUser);
                SearchComplete(this, new EventArgs());
                return;
            }
 //           User current = currentUser, target = targetUser;
            Queue<User> fromCurrent = new Queue<User>(), fromTarget = new Queue<User>();
            fromCurrent.Enqueue(currentUser);
            fromTarget.Enqueue(targetUser);
            int from = 1; // 1-выборка из очереди текущего пользователя, 2 - из очереди искомого
            while (true)
            {
                if (from == 1)
                {
                    if (fromCurrent.Count == 0)
                    {
                        // TODO: сделать выход, если поиск не дал результатов
                    }
                    User nextUser = fromCurrent.Dequeue();
                    //если нашли, заполняем список, реверсим его и отдаем на вывод, не забыв все почистить
                    if (nextUser == targetUser)
                    {
                        do
                        {
                            userschain.Add(nextUser);
                            nextUser = nextUser.GetParent();
                        } while (nextUser.Distance != 0);
                        userschain.Add(nextUser);
                        userschain.Reverse();
                        fromCurrent.Clear();
                        fromTarget.Clear();
                        SearchComplete(this, new EventArgs());
                        return;
                    } // if (nextUser == targetUser)
                    //иначе запрашиваем всех друзей, добавляем их в очередь и идем дальше
                    bool firstError = true;//если ошибка возникла впервые, пробуем еще раз, иначе выходим и сообщаем об ошибке
                    while (true)
                    {
                        string req = "https://api.vk.com/method/friends.get.xml?uid=" + 
                            nextUser.ID + "&access_token=" + token;
                        WebRequest request = WebRequest.Create(req);
                        WebResponse response = request.GetResponse();
                        XmlDocument xml = new XmlDocument();
                        xml.LoadXml(new System.IO.StreamReader(response.GetResponseStream()).ReadToEnd());
                        //обработка ошибок
                        if (xml["error"] != null)
                        {
                            string code = xml["error"]["error_code"].InnerText;
                            switch (code)
                            {
                                case "1": if (firstError) { firstError = false; Thread.Sleep(1500); continue; }
                                    else
                                    {
                                        nextUser = fromCurrent.Dequeue();
                                        break;
                                    }
                                case "2": goto case "1";
                                case "4": goto case "1";
                                case "5": goto case "1";
                                case "6": Thread.Sleep(100); firstError = true; continue;
                                case "7": goto case "1";// break;// TODO: сделать пропуск пользователя
                                default: goto case "1";
                            } // switch (code)
                        } // if (xml["error"] != null)
                        else
                        {
                            XmlNode uids = xml["response"];
                            foreach (XmlNode uid in uids.ChildNodes)
                            {
                                uint id = Convert.ToUInt32(uid.InnerText);
                                if (viewedIDs.Contains(id)) continue;
                                viewedIDs.Add(id);
                                User newUser = new User(id, nextUser.Distance + 1);
                                newUser.AddFriend(currentUser);
                                newUser = currentUser.AddFriend(newUser);
                                fromCurrent.Enqueue(newUser);
                            }
                            ++pagesViewed;
                            break;
                        } // if (xml["error"] == null)
                    } // while (true)
                } // if (from == 1)
            } // while (true)
        } // Find
    } // public partial class MainWindow : Window
} // namespace vk1
