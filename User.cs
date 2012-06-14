using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace vk1
{
    //класс, представляющий одного пользователя
    class User
    {
        //айди - храним как число для экономии памяти
        uint id;
        //имя, фамилия
 //       string firstName, lastName;
        //список друзей
        List<User> friends;
        //расстояние от текущего пользователя до искомого
        int distance;
        //создание пользователя по айди, имени и фамилии
        public User(uint id/*, string first, string last*/, int dist)
        {
            this.id = id;
//            firstName = first;
//            lastName = last;
            friends = new List<User>();
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
