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
using System.Device.Location;
using Microsoft.Phone.Shell;
using System.IO.IsolatedStorage;
using RestSharp;
using System.IO;
using System.Text;
using System.Windows.Navigation;


namespace UDJ
{
    public partial class FindPlayer : PhoneApplicationPage
    {
        private object _selected;

        public FindPlayer()
        {
            InitializeComponent();
        }

        internal bool progressBar
        {   
            get { return loadingProgressBar.IsLoading; }
            set { loadingProgressBar.IsLoading = value; }
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {

            
            currentUser = (User)settings["currentUser"]; //set currentUser
            if (!currentUser.hashIsValid)
            {
                loginToUDJ();
                return;
            }
           
            
            base.OnNavigatedTo(e);
        }

        //repeated again in case user has not logged in the past day, it'll log him in here
        public void loginToUDJ()
        {
            
            NetworkCalls<AuthResponse>.loginToUDJBefore(currentUser);
            if (!settings.Contains("currentUser"))
            {
                loadingProgressBar.IsLoading = false;
                this.Dispatcher.BeginInvoke(() => this.NavigationService.Navigate(new Uri("/MainPage.xaml", UriKind.RelativeOrAbsolute)));
            }
            else
            {
                buildPlayerList();
                loadingProgressBar.IsLoading = false;
            }
                

        }

        
        IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;
        

        User currentUser;
        Player selectedPlayer = null;
        GeoCoordinateWatcher watcher = new GeoCoordinateWatcher(GeoPositionAccuracy.High);

        public void buildPlayerList()
        {
            loadingProgressBar.IsLoading = true;
            noNearbyTextBlock.Visibility = Visibility.Collapsed;
            currentUser.isAtPlayer = false;

            //find geocoordinates, set accuracy to high

            watcher.MovementThreshold = 20; //checks again if user moves 20 feet
            watcher.PositionChanged += new EventHandler<GeoPositionChangedEventArgs<GeoCoordinate>>(watcher_PositionChanged);
            watcher.Start();
        }

        void watcher_PositionChanged(object sender, GeoPositionChangedEventArgs<GeoCoordinate> e)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() => myPositionChanged(e));
            watcher.PositionChanged -= new EventHandler<GeoPositionChangedEventArgs<GeoCoordinate>>(watcher_PositionChanged);
            watcher.Stop(); //stop watcher to conserve battery

        }

        void myPositionChanged(GeoPositionChangedEventArgs<GeoCoordinate> e)
        {
            currentUser.latitude = e.Position.Location.Latitude; //set coordinates
            currentUser.longitude = e.Position.Location.Longitude;
            findNearestPlayer();
            
        }

        //TO-DO: Add support for radius and handle if radius is too small or large
        public void findNearestPlayer()
        {
            NetworkCalls<List<Player>>.findNearestPlayerBefore(currentUser);
        }

        
        public void playerListBox_SelectionChanged(object sender, SelectionChangedEventArgs e) //when a user taps an event
        {
            var list = (ListBox)sender;



            if (list.SelectedItem == _selected)
            {
                list.SelectedIndex = -1;
                _selected = null;
            }
            else
            {
                _selected = list.SelectedItem;

                if (e.AddedItems.Count == 1) //checks to make sure only 1 player is selected
                {
                    currentUser = (User)settings["currentUser"]; //load currentUser
                    if (e.AddedItems[0].GetType() == typeof(Player))
                        selectedPlayer = (Player)e.AddedItems[0];
                    else
                    {
                        PlayerDatePair selectedItem = (PlayerDatePair)(e.AddedItems[0]);
                        selectedPlayer = selectedItem.playerOfPair; //get the selected event
                    }

                    var answer = MessageBox.Show("Do you want to join this player?", "Selected Player", MessageBoxButton.OKCancel);
                    if (answer == MessageBoxResult.OK)
                    {
                        settings["connectedPlayer"] = selectedPlayer; //locally store the selectedEvent
                        settings["currentUser"] = currentUser;
                        PlayerDatePair playerDate = new PlayerDatePair(selectedPlayer, DateTime.Now);
                        List<PlayerDatePair> recentPlayers = new List<PlayerDatePair>();
                        if (settings.Contains("recentPlayers"))
                        {
                            recentPlayers = (List<PlayerDatePair>)settings["recentPlayers"];
                            foreach (PlayerDatePair t in recentPlayers)
                            {
                                MemoryStream ms = new MemoryStream(t.playerSerialized);
                                ms.Position = 0;
                                t.playerOfPair = (Player)PlayerDatePair.Deserialize(ms, typeof(Player));
                                ms.Close();
                            }
                        }
                        recentPlayers.Add(playerDate); //add the selected event to recent events
                        foreach (PlayerDatePair t in recentPlayers)
                        {
                            MemoryStream ms = new MemoryStream();
                            PlayerDatePair.Serialize(ms, t.playerOfPair);
                            ms.Position = 0;
                            t.playerSerialized = ms.ToArray();

                        }

                        settings["recentPlayers"] = recentPlayers;
                        settings.Save();
                        loginToPlayer();

                    }
                    else
                    {
                        findNearestPlayer();
                    }
                }
            }
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            if (passwordRect.Visibility == Visibility.Visible)
            {
                passwordRect.Visibility = Visibility.Collapsed;
                passwordTitle.Visibility = Visibility.Collapsed;
                passwordLabel.Visibility = Visibility.Collapsed;
                password.Visibility = Visibility.Collapsed;
                passwordButton.Visibility = Visibility.Collapsed;
                e.Cancel = true;
                return;
            }

            if (NavigationService.CanGoBack)
            {
                JournalEntry lastPage = NavigationService.BackStack.Last();
                //lastPage.
            }
        }



        public void loginToPlayer()
        {
            NetworkCalls<List<String>>.loginToPlayerBefore(currentUser, selectedPlayer);
        }

     

        private void pivotControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            /*if (((Pivot)sender).SelectedIndex == 3)
                ApplicationBar = (Microsoft.Phone.Shell.ApplicationBar)Resources["appbar0"];
             * * */
            

            if (((Pivot)sender).SelectedIndex == 0)
                buildPlayerList();



        }

        private void logoutTextBlock_Tap(object sender, Microsoft.Phone.Controls.GestureEventArgs e) //user tap logout
        {
            var answer = MessageBox.Show("Are you sure you want to log out?", "Log out", MessageBoxButton.OKCancel);
            if (answer == MessageBoxResult.OK)
            {
                if (settings.Contains("connectedPlayer") && settings.Contains("currentUser"))
                {
                    selectedPlayer = (Player)settings["connectedPlayer"];
                    //selectedPlayer.logout(currentUser.id, currentUser.hashID, true);
                    settings.Remove("currentUser");
                    settings.Remove("connectedPlayer");
                }
               
                //(Application.Current.RootVisual as PhoneApplicationFrame).Navigate(new Uri("/MainPage.xaml", UriKind.RelativeOrAbsolute));
             
                this.Dispatcher.BeginInvoke(() => this.NavigationService.Navigate(new Uri("/MainPage.xaml", UriKind.RelativeOrAbsolute)));
                

                

            }

            else findNearestPlayer();
        }

            

        private void passwordButton_Click(object sender, RoutedEventArgs e)
        {
            selectedPlayer.password = password.Password;
            passwordRect.Visibility = Visibility.Collapsed;
            passwordTitle.Visibility = Visibility.Collapsed;
            passwordLabel.Visibility = Visibility.Collapsed;
            password.Visibility = Visibility.Collapsed;
            passwordButton.Visibility = Visibility.Collapsed;
            loginToPlayer();
        }

        private void playersText_Hold(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Player currPlayer = (Player)(sender as TextBlock).DataContext; //get event that's being held
            var answer = MessageBox.Show("Would you like to remove " + currPlayer.name + " from your favorites?", "Remove from favorites?", MessageBoxButton.OKCancel);
            if (answer == MessageBoxResult.OK)
            {
                List<Player> favPlayersList = new List<Player>();
                favPlayersList = (List<Player>)settings["favPlayers"]; //load favorites
                favPlayersList.Remove(currPlayer);
                settings["favPlayers"] = favPlayersList;
            }
            buildPlayerList();
        }

        private void contextMenuRemove_Click(object sender, RoutedEventArgs e)
        {
            Player currPlayer = (Player)(((sender as MenuItem).Parent as ContextMenu).DataContext); //get event that's being held
            var answer = MessageBox.Show("Are you sure you want to remove " + currPlayer.name + " from your favorites?", "Remove from favorites?", MessageBoxButton.OKCancel);
            if (answer == MessageBoxResult.OK)
            {
                List<Player> favPlayersList = new List<Player>();
                favPlayersList = (List<Player>)settings["favPlayers"]; //load favorites
                favPlayersList.Remove(currPlayer);
                settings["favPlayers"] = favPlayersList;
            }
            buildPlayerList();
        }

        

        
    }
}