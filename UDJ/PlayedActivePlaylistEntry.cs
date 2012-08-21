using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Collections.Generic;

namespace UDJ
{
    public class PlayedActivePlaylistEntry
    {
        public List<User> upvoters { get; set; }
        public LibraryEntry song { get; set; }
        public List<User> downvoters { get; set; }
        public string time_added { get; set; }
        public string time_played { get; set; }
        public User adder { get; set; }
    }
}
