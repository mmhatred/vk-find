using System;
using System.Collections.Generic;

namespace vk1
{
    //класс, представляющий одного пользователя
    class User
    {
        //айди - храним как число для экономии памяти
        uint id;
        //имя, фамилия
 //       string firstName, lastName;
        //ассоциативный массив друзей, доступный по id
        SortedDictionary<uint, int> friends;
        
        //расстояние от текущего пользователя до искомого
        int distance;
        //создание пользователя по айди, имени и фамилии
        public User(uint id/*, string first, string last*/, int dist)
        {
            this.id = id;
//            firstName = first;
//            lastName = last;
            friends = new SortedDictionary<uint, int>();
            distance = dist;
        } // User
        //создание пользователя из одной записи ответа сервера
        public User(System.Xml.XmlNode node, int dist = -1) : this(Convert.ToUInt32(node["uid"].InnerText)/*,
                              node["first_name"].InnerText, node["last_name"].InnerText*/, dist) { }
        public uint ID
        {
            get { return id; }
        } // ID
        public int Distance
        {
            get { return distance; }
        } // Distance

        //проверка пользователей на равенство
        public static bool operator ==(User u1, User u2)
        {
            var u1IsNull = User.Equals(u1, null);
            var u2IsNull = User.Equals(u2, null);
            if (u1IsNull || u2IsNull) return (u1IsNull && u2IsNull);
            return u1.ID == u2.ID;
        } // operator ==

        // проверка на неравенство
        public static bool operator !=(User u1, User u2)
        {
            return !(u1 == u2);
        } // operator !=

        //превращение в строку - выводим айди и дистанцию
        public override string ToString()
        {
            return "ID: " + id + ", distance: " + distance;
        } // ToString

        //добавление друга в словарь
        public User AddFriend(User friend)
        {
            if (friends.ContainsKey(friend.ID)) return new User(friend.ID, friends[friend.ID]);
            friends.Add(friend.ID, friend.Distance);
            return friend;
        } // AddFriend

        //возвращает пользователя с distance на 1 меньшим, чем у заданного
        public User GetParent()
        {
            foreach (var i in friends)
            {
                if (i.Value == distance - 1) return new User(i.Key, i.Value);
            }
            return null;
        } // GetParent
        //очистка списка друзей
        public void ClearFriends()
        {
            friends.Clear();
        } // ClearFriends
        //public string FirstName
        //{
        //    get { return firstName; }
        //} // FirstName
        //public string LastName
        //{
        //    get { return lastName; }
        //} // LastName
    } // class User
} // namespace vk1
