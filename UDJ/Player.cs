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
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.IO.IsolatedStorage;
using Microsoft.Phone.Controls;

namespace UDJ
{
   
    public class Player
    {
        public string id { get; set; }
        public string name { get; set; }
        public User owner { get; set; }
        public Location location { get; set; }
        public SortingAlgorithm sorting_algo { get; set; }
        public List<User> admins { get; set; }
        public bool songset_user_permission { get; set; }
        public int num_active_users { get; set; }
        public int size_limit { get; set; }
        public bool has_password { get; set; }
        public string password { get; set; }

     /*   public override string ToString()
        {
            return name;
        }
      

        public void logout(long userID, string hashID, bool appLogout)
        {
            IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;
            string statusCode = "";
            string url = "http://udjplayers.com:4897/udj/players/" + id + "/users/";
            var client = new RestClient(url);
            var request = new RestRequest(userID.ToString(), Method.DELETE);
            request.AddHeader("X-Udj-Ticket-Hash", hashID.ToString());
            password = null;
            client.ExecuteAsync(request, response =>
            {
                statusCode = response.StatusCode.ToString();
                if (statusCode != "OK")
                {
                    MessageBox.Show("There seems to be an error: " + statusCode);

                }
                    MessageBox.Show("You've been successfully logged out from this player", "Event Logout Successful", MessageBoxButton.OK);
                    if (!appLogout)
                        (Application.Current.RootVisual as PhoneApplicationFrame).Navigate(new Uri("/FindPlayer.xaml", UriKind.RelativeOrAbsolute));

            });
            
        }

       


       public List<ActivePlaylistEntry> getActivePlaylist(string hashID)
        {
            string statusCode = "";
            List<ActivePlaylistEntry> queue = new List<ActivePlaylistEntry>();
            string url = "http://udjevents.com:4897/udj/events/" + id + "/active_playlist";
            var client = new RestClient(url);
            var request = new RestRequest(Method.GET);
            request.AddHeader("X-Udj-Api-Version", "0.2");
            request.AddHeader("X-Udj-Ticket-Hash", hashID.ToString());
            client.ExecuteAsync<List<ActivePlaylistEntry>>(request, response =>
            {
                queue = response.Data;

            });
            if (statusCode != "OK")
            {
                MessageBox.Show("There seems to be an error: " + statusCode);
                return queue = new List<ActivePlaylistEntry>();
            }
            else return queue;

        } */

     //   public bool addSongToPlaylist(long lib_id, long client_request_id, string hashID)
       // {
            
       // }


        //To Do: View Add requests
    }
}
