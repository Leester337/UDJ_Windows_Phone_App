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
        public FindPlayer()
        {
            InitializeComponent();
            
            
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            loadingProgressBar.IsLoading = true;
            if (settings.Contains("hashIsValid") && !((bool)settings["hashIsValid"]))
            {
                loginToUDJ();
                return;
            }
            buildPlayerList();
            
            base.OnNavigatedTo(e);
        }

        public void loginToUDJ()
        {

            string statusCode = "";
            IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;

            var client = new RestClient("https://udjplayer.com:4897/udj/");
            var request = new RestRequest("auth", Method.POST);
            currentUser = (User)settings["currentUser"];
            request.AddParameter("username", currentUser.username);
            request.AddParameter("password", currentUser.password);

            client.ExecuteAsync<AuthResponse>(request, response =>
            {
                statusCode = response.StatusCode.ToString();  //stores the Status of the request


                if (statusCode == "OK")  //if everything went okay
                {
                    AuthResponse userInfo = response.Data;
                    currentUser.hashID = userInfo.ticket_hash;
                    currentUser.id = Convert.ToInt64(userInfo.user_id);
                    currentUser.hashCreated = DateTime.Now; //set hashCreated to now
                    // DateTime hashCreatedEcho = hashCreated; 
                    string hashIDString = currentUser.hashID;
                    settings["currentUser"] = currentUser; //save currentUser 
                    // PhoneApplicationService.Current.State["currUser"] = this; 
                    buildPlayerList();

                }
                else if (statusCode == "NotFound")
                {
                    MessageBox.Show("You don't seemed to be connected to the internet, please check your settings and try again");

                }

                else
                {

                    MessageBox.Show("There seems to be an error: " + statusCode);

                }



            });

        }

        
        IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;
        

        User currentUser;
        Player selectedPlayer = null;
        GeoCoordinateWatcher watcher = new GeoCoordinateWatcher(GeoPositionAccuracy.High);
        bool goToMain = false;

        public void buildPlayerList()
        {
            currentUser = (User)settings["currentUser"]; //set currentUser
            currentUser.isAtPlayer = false;
            findLocation(watcher);        //find geocoordinates, set accuracy to high
            
            
        }
        public void findLocation(GeoCoordinateWatcher watcher)
        {
            
            watcher.MovementThreshold = 20; //checks again if user moves 20 feet
            watcher.PositionChanged += new EventHandler<GeoPositionChangedEventArgs<GeoCoordinate>>(watcher_PositionChanged);
            watcher.Start();
        }

        void watcher_PositionChanged(object sender, GeoPositionChangedEventArgs<GeoCoordinate> e)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() => myPositionChanged(e));
            
        }

        void myPositionChanged(GeoPositionChangedEventArgs<GeoCoordinate> e)
        {
            currentUser.latitude = e.Position.Location.Latitude; //set coordinates
            currentUser.longitude = e.Position.Location.Longitude;
            watcher.Stop(); //stop watcher to conserve battery
            findNearestPlayer();
            
        }

        public void findNearestPlayer()
        {
            string statusCode = "";
            string url = "https://udjplayer.com:4897/udj/players/";

            //currentUser.latitude = 40.113523; //sample coordinates that return players
            //currentUser.longitude = -88.224006;

            url += currentUser.latitude + "/";
            var client = new RestClient(url);
            var request = new RestRequest(currentUser.longitude.ToString(), Method.GET);
            request.AddHeader("X-Udj-Ticket-Hash", currentUser.hashID);

            List<Player> players = new List<Player>(); //list to store returned events
            playerListBox.DataContext = new List<Player>(); //clear the eventListBox
            recentPlayerListBox.DataContext = new List<PlayerDatePair>(); //clear recentEventListBox
            favPlayersListBox.DataContext = new List<Player>(); //clear favEventListBox, the listbox that contains the user's favorited events

            client.ExecuteAsync<List<Player>>(request, response =>
            {
                string hashIDString = currentUser.hashID;
                players = response.Data; //store the returned events
                statusCode = response.StatusCode.ToString();
                
                    if (statusCode == "NotFound") //no internet connection
                    {
                        MessageBox.Show("You don't seemed to be connected to the internet, please check your settings and try again");
                    }

                    else if (statusCode != "OK") //general error, assuming it is due to incorrect login credentials
                    {
                        MessageBox.Show("Whoops! Something went wrong, I'll redirect you to the login screen.");
                        settings.Remove("currentUser");
                        MessageBox.Show("There seems to be an error: " + statusCode);
                         this.Dispatcher.BeginInvoke(() => this.NavigationService.Navigate(new Uri("/MainPage.xaml", UriKind.RelativeOrAbsolute)));
                    }


                    loadingTextBlock.Visibility = Visibility.Collapsed; //get rid of "Loading..." textblock when the players are ready to be retreived
                    loadingProgressBar.IsLoading = false;
                    playerListBox.DataContext = players;

                    if (!settings.Contains("recentPlayers")) //if there are no recentEvents list saved, then display noRecentTextBlock
                {
                    noRecentTextBlock.Visibility = Visibility.Visible;

                }
                else
                {
                    noRecentTextBlock.Visibility = Visibility.Collapsed; //there are recent players so hide noRecentTextBlock
                    List<PlayerDatePair> recentPlayersList = new List<PlayerDatePair>();
                    recentPlayersList = (List<PlayerDatePair>)settings["recentPlayers"];  //retrieve events saved in memory
                    foreach (PlayerDatePair t in recentPlayersList)
                    {
                        MemoryStream ms = new MemoryStream(t.playerSerialized);
                        ms.Position = 0;
                        t.playerOfPair = (Player)PlayerDatePair.Deserialize(ms, typeof(Player));
                        ms.Close();
                    }

                    List<string> checkForDupPlayers = new List<string>();
                    for (int i = 0; i < recentPlayersList.Count; i++) //for all events the user recently visited
                    {

                        if (recentPlayersList[i].dateOfSignIn.AddDays(1) < DateTime.Now) //if he signed in over a day ago
                        {
                            recentPlayersList.RemoveAt(i); //remove it
                            i--;
                            break;
                        }

                        if (checkForDupPlayers.Contains(recentPlayersList[i].playerOfPair.name)) //if the event is already on the list
                            recentPlayersList.RemoveAt(i); //remove it
                        else checkForDupPlayers.Add(recentPlayersList[i].playerOfPair.name); //otherwise add it to the checkFor list
                    }
                    recentPlayerListBox.DataContext = recentPlayersList; //display recentEventsList in the recentEventListBox
                    settings["recentPlayers"] = recentPlayersList;  //save the revised events list
                }

                if (!settings.Contains("favPlayers"))  //if there are no favorite events saved
                {
                    noFavTextBlock.Visibility = Visibility.Visible; //show noFavTextBlock

                }
                else
                {
                    noFavTextBlock.Visibility = Visibility.Collapsed;  //hide noFavTextBlock

                    List<Player> favPlayersList = new List<Player>(); //clear the favorite events list
                    
                        favPlayersList = (List<Player>)settings["favPlayers"];
                    List<string> checkForDup = new List<string>();
                    for (int i = 0; i < favPlayersList.Count; i++) //for all events the user recently visited
                    {
                        if (checkForDup.Contains(favPlayersList[i].name)) //if the event is already on the list
                            favPlayersList.RemoveAt(i); //remove it
                        else checkForDup.Add(favPlayersList[i].name); //otherwise add it to the checkFor list
                    }
                 
                        favPlayersListBox.DataContext = favPlayersList; 
                        settings["favPlayers"] = favPlayersList; //save the favorites list
                    
                }                 
            });
        }

        public void playerListBox_SelectionChanged(object sender, SelectionChangedEventArgs e) //when a user taps an event
        {
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
               
                var answer = MessageBox.Show("Do you want to join this player?", "Selected Event", MessageBoxButton.OKCancel);
                if (answer == MessageBoxResult.OK)
                {
                    settings["connectedPlayer"] = selectedPlayer; //locally store the selectedEvent
                    settings["currentUser"] = currentUser;
                    settings["minClientReqID"] = 0; 
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

        public void loginToPlayer()
        {
            string statusCode = "";
            string url = "https://udjplayer.com:4897/udj/players/" + selectedPlayer.id + "/users";
            var client = new RestClient(url);
            var request = new RestRequest(currentUser.id.ToString(), Method.PUT);
            request.AddHeader("X-Udj-Ticket-Hash", currentUser.hashID);
            if (selectedPlayer.has_password && selectedPlayer.password == null)
            {
                passwordRect.Visibility = Visibility.Visible;
                passwordTitle.Visibility = Visibility.Visible;
                passwordLabel.Visibility = Visibility.Visible;
                password.Visibility = Visibility.Visible;
                passwordButton.Visibility = Visibility.Visible;
            }

            if (selectedPlayer.has_password && selectedPlayer.password != null)
                request.AddHeader("X-Udj-Player-Password", selectedPlayer.password);

            client.ExecuteAsync<Player>(request, response =>
            {
                Player alreadyLoggedInPlayer = response.Data; //if the user is already logged into an event, the event is returned and saved here
                statusCode = response.StatusCode.ToString();
                currentUser.isAtPlayer = true; //user is at Event
                if (statusCode != "Created")
                {
                    if (statusCode == "Conflict") //User already logged into player
                    {
                        //if (alreadyLoggedInPlayer == null)
                        MessageBox.Show("It seems you're already logged into another player. Go to " + alreadyLoggedInPlayer.name + " and logout before you log in here!");
                        return;
                    }
                    else if (statusCode == "NotFound")
                    {
                        MessageBox.Show("You don't seemed to be connected to the internet, please check your settings and try again");
                    }
                    else
                    {
                        var answer = MessageBox.Show("There seems to be an error: " + statusCode);
                        if (answer == MessageBoxResult.OK)
                            return;
                    }
                }

                (Application.Current.RootVisual as PhoneApplicationFrame).Navigate(new Uri("/NowPlaying.xaml", UriKind.RelativeOrAbsolute)); //navigate to NowPlaying

            });
            
            

        }

        private void favPlayerListBox_Hold(object sender, Microsoft.Phone.Controls.GestureEventArgs e) //user hold down event in favEventListBox
        {
            Player currPlayer = (Player)(sender as StackPanel).DataContext; //get event that's being held
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

        private void logoutTextBlock_Tap(object sender, Microsoft.Phone.Controls.GestureEventArgs e) //user tap logout
        {
            var answer = MessageBox.Show("Are you sure you want to log out?", "Log out", MessageBoxButton.OKCancel);
            if (answer == MessageBoxResult.OK)
            {
                goToMain = true;
                if (settings.Contains("connectedPlayer") && settings.Contains("currentUser") && settings.Contains("minClientReqID"))
                {
                    selectedPlayer = (Player)settings["connectedPlayer"];
                    //selectedPlayer.logout(currentUser.id, currentUser.hashID, true);
                    settings.Remove("currentUser");
                    settings.Remove("connectedPlayer");
                    settings.Remove("minClientReqID");
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

        

        
    }
}