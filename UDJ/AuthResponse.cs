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

namespace UDJ
{
    public class AuthResponse
    {
        public string ticket_hash { get; set; }
        public string user_id { get; set; }
    }
}
