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
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using RestSharp;
using Newtonsoft.Json;
using System.IO.IsolatedStorage;
using Microsoft.Phone.Shell;

namespace UDJ
{
    public partial class MainPage : PhoneApplicationPage
    {
        // Constructor
        public MainPage()
        {
            InitializeComponent();
            currentUser.isAtPlayer = false;
        }

        User currentUser = new User();
        IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;  //used to save currentUser

        private void username_GotFocus(object sender, RoutedEventArgs e) //placeholder text
        {
            if (username.Text == "Username")
            {
                username.Text = "";
                SolidColorBrush black = new SolidColorBrush();
                black.Color = Colors.Black;
                username.Foreground = black;
                
            }
        }

        private void username_LostFocus(object sender, RoutedEventArgs e)  //restores placeholder text
        {
            if (username.Text == String.Empty)
            {
                username.Text = "Username";
                SolidColorBrush gray = new SolidColorBrush();
                gray.Color = Colors.Gray;
                username.Foreground = gray;
            }
        }

        private void password_GotFocus(object sender, RoutedEventArgs e) //placeholder text for password
        {
            passwordWatermark.Opacity = 0;
            password.Opacity = 100;
        }

        private void password_LostFocus(object sender, RoutedEventArgs e)  //restores placeholder text for password
        {
            if (password.Password == String.Empty)
            {
                password.Opacity = 0;
                passwordWatermark.Opacity = 100;
            }
        }

        private void login_Click(object sender, RoutedEventArgs e)
        {
            currentUser.username = username.Text;
            currentUser.password = password.Password;
            loginToEvent();
            progressBar.IsLoading = true;
            
        }

        private void password_KeyDown(object sender, KeyEventArgs e)  //logins the user when he taps the enter key
        {
            if (e.Key == Key.Enter)
                login_Click(sender, e);
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e) //if user hits back key, log out from event
        {
            base.OnBackKeyPress(e);
            while (NavigationService.CanGoBack)
            {
                NavigationService.RemoveBackEntry();
            }

        }

        private void login_GotFocus(object sender, RoutedEventArgs e)
        {
          //  login.BorderBrush = Application.Current.Resources["PhoneAccentBrush"] as SolidColorBrush;
            //login.Foreground = Application.Current.Resources["PhoneAccentBrush"] as SolidColorBrush;
        }

        private void login_LostFocus(object sender, RoutedEventArgs e)
        {
        /*    if ((Application.Current.Resources["PhoneBackgroundBrush"] as SolidColorBrush).Color == Colors.White)
            {
                SolidColorBrush brushColor = new SolidColorBrush();
                brushColor.Color = Colors.Black;
                login.BorderBrush = brushColor;
                login.Foreground = brushColor;
            } */
        }


        
        public void loginToEvent()
        {

            string statusCode = "";
            IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;

            var client = new RestClient("https://udjplayer.com:4897/udj/0_6");
            var request = new RestRequest("auth", Method.POST);

            request.AddParameter("username", currentUser.username);
            request.AddParameter("password", currentUser.password);

            client.ExecuteAsync<AuthResponse>(request, response =>
            {
                statusCode = response.StatusCode.ToString();  //stores the Status of the request


                if (statusCode == "OK")  //if everything went okay
                {
                    AuthResponse userInfo = response.Data;
                    currentUser.hashID = userInfo.ticket_hash;
                    currentUser.id = userInfo.user_id;
                    currentUser.hashCreated = DateTime.Now; //set hashCreated to now
                    // DateTime hashCreatedEcho = hashCreated; 
                    string hashIDString = currentUser.hashID;
                    settings["currentUser"] = currentUser; //save currentUser 
                    // PhoneApplicationService.Current.State["currUser"] = this; 
                    (Application.Current.RootVisual as PhoneApplicationFrame).Navigate(new Uri("/FindPlayer.xaml", UriKind.RelativeOrAbsolute)); //go to findPlayer
                }
                else if (statusCode == "NotFound")
                {
                    MessageBox.Show("It would be in your best interests to try again. Check your wifis!");
                    progressBar.IsLoading = false;
                }

                else if (statusCode == "BadRequest")
                {
                    MessageBox.Show("Did you forget to type something? Please try again!");
                    progressBar.IsLoading = false;
                }

                else if (statusCode == "Unauthorized")
                {
                    MessageBox.Show("Either that's not your username or that's not your password. Please try again!");
                    progressBar.IsLoading = false;
                }

                else
                {

                    MessageBox.Show("There seems to be an error: " + statusCode);
                    progressBar.IsLoading = false;
                }



            });

        }

        

    }
}