﻿/*
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
using Microsoft.Phone.Tasks;

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
            progressBar.IsLoading = true;
            currentUser.username = username.Text;
            currentUser.password = password.Password;
            loginToEvent();
            
            
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

            NetworkCalls <AuthResponse>.loginToUDJBefore(currentUser);
            progressBar.IsLoading = false;

        }

        private void makeAccountTB_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            WebBrowserTask wbt = new WebBrowserTask();
            wbt.Uri = new Uri("https://www.udjplayer.com/registration/register/");
            wbt.Show();
        }

        

    }
}