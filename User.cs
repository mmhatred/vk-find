using System;
using System.Xml;

namespace vk1
{
    // Class representing a single user
    class User
    {
        /// <summary>
        /// User ID
        /// </summary>
        private uint id;

        /// <summary>
        /// Initializes a new instance of the <see cref="User"/> class
        /// </summary>
        /// <param name="id">user ID</param>
        public User(uint id)
        {
            this.id = id;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="User"/> class
        /// </summary>
        /// <param name="node">XML node contains user info</param>
        public User(XmlNode node) : this(Convert.ToUInt32(node["uid"].InnerText)) { }

        /// <summary>
        /// Gets user's ID
        /// </summary>
        public uint ID
        {
            get
            {
                return id;
            }
        }
    } // class User
} // namespace vk1