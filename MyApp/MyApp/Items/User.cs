using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace MyApp.Items
{
    public class User
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Indexed]
        public string Login { get; set; }

        public string Password { get; set; }

        public string LastEntrance { get; set; }

        public string LastActivity { get; set; }
    }
}
