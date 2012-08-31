/*
  Copyright 2011 Kurtis L. Nusbaum

  This file is part of UDJ.

  UDJ is free software: you can redistribute it and/or modify
  it under the terms of the GNU General Public License as published by
  the Free Software Foundation, either version 2 of the License, or
  (at your option) any later version.

  UDJ is distributed in the hope that it will be useful,
  but WITHOUT ANY WARRANTY; without even the implied warranty of
  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
  GNU General Public License for more details.

  You should have received a copy of the GNU General Public License
  along with UDJ.  If not, see <http://www.gnu.org/licenses/>.
*/

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
using RestSharp;
using System.IO.IsolatedStorage;
using System.Device.Location;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using Microsoft.Phone.Shell;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using System.Threading;
using System.Windows.Threading;
using System.Runtime.Serialization;

namespace UDJ
{

    public class User
    {
        public string id { get; set; }
        public string first_name { get; set; }
        public string last_name { get; set; }
        public string username { get; set; }
        public string password { get; set; }
        public string hashID { get; set; }
        public bool isAtPlayer { get; set; }
        public double latitude { get; set; }
        public double longitude { get; set; }
        public bool hashIsValid { get; set; }
        public DateTime hashCreated { get; set; }
        public bool isOwnerOrAdmin { get; set; }




        
        

    }
}
