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
using Microsoft.Phone.Shell;
using System.IO.IsolatedStorage;
using System.Windows.Media.Imaging;

namespace UDJ
{
    public partial class PivotPage1 : PhoneApplicationPage
    {
        static IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;

        User currentUser;
        Player connectedPlayer;
        long minClientReqID;
        PlayedActivePlaylistEntry currentSong = new PlayedActivePlaylistEntry();
        ActivePlaylistEntry selectedSong = null;
        LibraryEntry selectedSearchResult = null;
        Dictionary<long, long> minClientReqIDMap = new Dictionary<long,long>();
        private object _selected;
        private object _queueSelected;
        private object _Artistselected;

        public PivotPage1()
        {
            InitializeComponent();
            
       
            //IsolatedStorageSettings.ApplicationSettings.Save();
            //updateNowPlaying(); //otherwise, get song information
           // checkBackgroundColor();
            

        }

        public void checkBackgroundColor()
        {
            Visibility darkBackgroundVisibility =
                (Visibility)Application.Current.Resources["PhoneDarkThemeVisibility"];
            // Write the theme background value.
            BitmapImage myBitmapImage;
            if (darkBackgroundVisibility == Visibility.Visible)
            {
                 
                myBitmapImage = new BitmapImage(new Uri("icons/appbar.up.rest.png", UriKind.Relative));
                upvoteNPImage.Source = myBitmapImage;
                myBitmapImage = new BitmapImage(new Uri("icons/appbar.down.rest.png", UriKind.Relative));
                downvoteNPImage.Source = myBitmapImage;
            }
            else
            {
                
                myBitmapImage = new BitmapImage(new Uri("icons/appbar.up.dark.rest.png", UriKind.Relative));
                upvoteNPImage.Source = myBitmapImage;
                myBitmapImage = new BitmapImage(new Uri("icons/appbar.down.dark.rest.png", UriKind.Relative));
                downvoteNPImage.Source = myBitmapImage;
            } 
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            settings["connectedPlayer"] = connectedPlayer;
            settings["currentUser"] = currentUser;
            settings["minClientReqID"] = minClientReqID;
            IsolatedStorageSettings.ApplicationSettings.Save();
            base.OnNavigatedFrom(e);
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            ApplicationBar = (Microsoft.Phone.Shell.ApplicationBar)Resources["appbar3"];
            loadingProgressBar.IsLoading = true;
            connectedPlayer = (Player)settings["connectedPlayer"]; //load connected event and user
            currentUser = (User)settings["currentUser"];
            currentUser.isAtPlayer = true;
            minClientReqID = Convert.ToInt64(settings["minClientReqID"]);
            checkBackgroundColor();
            base.OnNavigatedTo(e);

            
            updateNowPlaying();
        }


        private void updateNowPlaying()
        {
            if (searchTitle.Visibility == System.Windows.Visibility.Visible)
                searchTitle.Visibility = System.Windows.Visibility.Collapsed;
            string statusCode = "";
            string url = "https://udjplayer.com:4897/udj/players/" + connectedPlayer.id;
            var client = new RestClient(url);
            var request = new RestRequest("active_playlist", Method.GET);
            request.AddHeader("X-Udj-Ticket-Hash", currentUser.hashID.ToString());

            client.ExecuteAsync<ActivePlaylistResponse>(request, responseUpdate =>
            {
                ActivePlaylistResponse activePlaylistResponse = responseUpdate.Data; //Activeplaylist response is the current song playing and active playlist
                statusCode = responseUpdate.StatusCode.ToString();
                string statuscodestring = statusCode;

                if (statusCode != "OK")
                {
                    MessageBox.Show("There seems to be an error: " + statusCode);
                    currentSong = new PlayedActivePlaylistEntry();
                }

                else if (statusCode == "NotFound")  //no internet connnection
                {
                    MessageBox.Show("You don't seemed to be connected to the internet, please check your settings and try again");

                }
                queueLB.DataContext = new List<ActivePlaylistEntry>(); //clear the queue listbox

                currentSong = activePlaylistResponse.current_song;
                try
                {
                    loadingProgressBar.IsLoading = false;
                    artistTB.Text = currentSong.song.artist;
                    songTB.Text = currentSong.song.title; //if nothing is playing

                    albumTB.Text = currentSong.song.album;
                    upVotesTB.Text = currentSong.upvoters.Count.ToString();
                    downVotesTB.Text = currentSong.downvoters.Count.ToString();

                    long minutes = currentSong.song.duration / 60; //parse duration into minutes and seconds
                    long seconds = currentSong.song.duration % 60;
                    durationTB.Text = minutes.ToString() + " minutes and " + seconds.ToString() + " seconds";
                }
                catch (NullReferenceException e)
                {
                    MessageBox.Show("Try adding something to the playlist now!", "There's nothing playing :(", MessageBoxButton.OK);
                }


                List<ActivePlaylistEntry> queue = new List<ActivePlaylistEntry>();
                queue = activePlaylistResponse.active_playlist;
                foreach (ActivePlaylistEntry t in queue)
                {
                    t.updatePageInfo();
                }
                queueLB.DataContext = queue;
                settings["connectedPlayer"] = connectedPlayer;


            });

            url = "https://udjplayer.com:4897/udj/players/" + connectedPlayer.id + "/available_music/random_songs?number_of_randoms=25";
            client = new RestClient(url);
            request = new RestRequest("", Method.GET);
            request.AddHeader("X-Udj-Ticket-Hash", currentUser.hashID.ToString());

            client.ExecuteAsync<List<LibraryEntry>>(request, response =>
            {
                List<LibraryEntry> recentlyPlayedResponse = response.Data; //Activeplaylist response is the current song playing and active playlist
                statusCode = response.StatusCode.ToString();


                if (statusCode != "OK")
                {
                    MessageBox.Show("There seems to be an error: " + statusCode);
                    currentSong = new PlayedActivePlaylistEntry();
                }

                else if (statusCode == "NotFound")  //no internet connnection
                {
                    MessageBox.Show("You don't seemed to be connected to the internet, please check your settings and try again");

                }
                recentlyPlayedLB.DataContext = new List<ActivePlaylistEntry>(); //clear the queue listbox
                recentlyPlayedLB.DataContext = recentlyPlayedResponse;

            });
        }
        

     /*   private void logout_Click(object sender, EventArgs e)
        {
            var answer = MessageBox.Show("Are you sure you want to logout from this player? NOTE: You're just leaving the event, not UDJ!", "Logout From Event", MessageBoxButton.OKCancel);
            if (answer == MessageBoxResult.OK) 
            {
                currentUser.isAtPlayer = false;
                connectedPlayer.logout(currentUser.id, currentUser.hashID, false);
                
            }
        }
      */

        private void searchButton_Click(object sender, RoutedEventArgs e) //when user clicks search button
        {
           
                if (searchTitle.Visibility == System.Windows.Visibility.Visible)
                    searchTitle.Visibility = System.Windows.Visibility.Collapsed;
                loadingProgressBar.IsLoading = true;
                string statusCode = "";
                searchTitle.Visibility = Visibility.Collapsed; //hide searchTitle textblock
                string url = "https://udjplayer.com:4897/udj/players/" + connectedPlayer.id;
                var client = new RestClient(url);

                var request = new RestRequest("available_music?query=" + searchTextBox.Text, Method.GET);
                request.AddHeader("X-Udj-Api-Version", "0.2");
                request.AddHeader("X-Udj-Ticket-Hash", currentUser.hashID.ToString());

                client.ExecuteAsync<List<LibraryEntry>>(request, response =>
                {
                    searchListBox.DataContext = new List<LibraryEntry>();  //clear searchListBox
                    List<LibraryEntry> searchResults = response.Data;
                    statusCode = response.StatusCode.ToString();
                    string statuscodestring = statusCode;

                    if (searchResults.Count == 0)
                    { //if no results are returned
                        searchTitle.Visibility = Visibility.Visible;
                    }

                    if (statusCode != "OK")
                    {
                        MessageBox.Show("There seems to be an error: " + statusCode);
                    }

                    else if (statusCode == "NotFound")
                    {
                        MessageBox.Show("You don't seemed to be connected to the internet, please check your settings and try again");
                    }
                    loadingProgressBar.IsLoading = false;
                    searchListBox.DataContext = searchResults;

                });
            
           
           
        }

        private void getArtists()
        {
            loadingProgressBar.IsLoading = true;
            string statusCode = "";
            searchTitle.Visibility = Visibility.Collapsed; //hide searchTitle textblock
            string url = "https://udjplayer.com:4897/udj/players/" + connectedPlayer.id;
            var client = new RestClient(url);

            var request = new RestRequest("available_music/artists", Method.GET);

            request.AddHeader("X-Udj-Ticket-Hash", currentUser.hashID.ToString());

            client.ExecuteAsync<List<string>>(request, response =>
            {
                artistLB.DataContext = new List<LibraryEntry>();  //clear searchListBox
                List<string> searchResults = response.Data;
                statusCode = response.StatusCode.ToString();
                string statuscodestring = statusCode;

                if (searchResults.Count == 0)
                { //if no results are returned
                    searchTitle.Visibility = Visibility.Visible;
                }

                

                if (statusCode == "NotFound")
                {
                    MessageBox.Show("You don't seemed to be connected to the internet, please check your settings and try again");
                }
                else if (statusCode != "OK")
                {
                    MessageBox.Show("There seems to be an error: " + statusCode);
                }

                loadingProgressBar.IsLoading = false;
                artistLB.DataContext = searchResults;

            });
        }

       

        private void upVote_Click(object sender, EventArgs e)
        {
            upOrDownVote_Click("upvote");
            
          
        }

        private void downVote_Click(object sender, EventArgs e)
        {
            upOrDownVote_Click("downvote");
        }

        
        public void upOrDownVote_Click(string upOrDown)
        {
            if (selectedSong == null)
            {
                MessageBox.Show("Please select a song from the playlist and then try to " + upOrDown + " it again", "Error: No song selected", MessageBoxButton.OK);
            }
            else
            {
                loadingProgressBar.IsLoading = false;
                string statusCode = "";
                string url = "https://udjplayer.com:4897/udj/players/" + connectedPlayer.id + "/active_playlist/songs/" + selectedSong.song.id.ToString() + "/users/" + currentUser.id + "/";
                var client = new RestClient(url);
                var request = new RestRequest(upOrDown, Method.POST);
                ActivePlaylistEntry selectedSongString = selectedSong;
                request.AddHeader("X-Udj-Ticket-Hash", currentUser.hashID.ToString());

                client.ExecuteAsync(request, response =>
                {
                    statusCode = response.StatusCode.ToString();


                    if (statusCode != "OK")
                    {
                        MessageBox.Show("There seems to be an error: " + statusCode);
                    }
                    else if (statusCode == "NotFound")
                    {
                        MessageBox.Show("You don't seemed to be connected to the internet, please check your settings and try again");
                    }
                    else
                    {
                        loadingProgressBar.IsLoading = false;
                        MessageBox.Show("Congrats, you've successfully " + upOrDown + "d " + selectedSong.song.title + "!", upOrDown[0].ToString().ToUpper() + upOrDown.Substring(1) + " was successful", MessageBoxButton.OK);
                    }
                    updateNowPlaying();
                });


            }
        }
 

 

        private void searchTextBox_GotFocus(object sender, RoutedEventArgs e)  //placeholder text for search textbox
        {
            if (searchTextBox.Text == "Enter your search here")
                searchTextBox.Text = "";
            SolidColorBrush black = new SolidColorBrush();
            black.Color = Colors.Black;
            searchTextBox.Foreground = black;
        }

        private void searchTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (searchTextBox.Text == "")
            {
                searchTextBox.Text = "Enter your search here";
                SolidColorBrush gray = new SolidColorBrush();
                gray.Color = Colors.Gray;
                searchTextBox.Foreground = gray;
            }
        }

     

        private void searchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                searchButton_Click(sender, e);
        }

        private void favorite_Click(object sender, EventArgs e) //click favorite button
        {
           
            List<Player> favPlayers = new List<Player>();
            if (settings.Contains("favPlayers"))
                favPlayers = (List<Player>)settings["favPlayers"];
            favPlayers.Add(connectedPlayer);
            settings["favPlayers"] = favPlayers;
            MessageBox.Show(connectedPlayer.name + " has been saved as a favorite!");
        }

        private void refresh_Click(object sender, EventArgs e)
        {
            updateNowPlaying();
        }

    

        

        private void randomList()
        {
            
            loadingProgressBar.IsLoading = true;
            string statusCode = "";
            string url = "https://udjplayer.com:4897/udj/players/" + connectedPlayer.id + "/available_music/random_songs?number_of_randoms=25";
            var client = new RestClient(url);
            var request = new RestRequest("", Method.GET);
            request.AddHeader("X-Udj-Ticket-Hash", currentUser.hashID.ToString());
            //request.AddHeader("Content-Type", "text/json");
            //string jsonObject = @"[{ ""lib_id"":" +  selectedSearchResult.id + @", ""client_request_id"":" + minClientReqID + "}]"; //create JSON object
            //request.RequestFormat = DataFormat.Json;
            //request.AddParameter("text/json", jsonObject, ParameterType.RequestBody);  //add JSON object as string


            client.ExecuteAsync<List<LibraryEntry>>(request, response =>
            {
                searchListBox.DataContext = new List<LibraryEntry>();  //clear searchListBox
                List<LibraryEntry> searchResults = response.Data;
                loadingProgressBar.IsLoading = false;
                statusCode = response.StatusCode.ToString();
                string statuscodestring = statusCode;

                if (statusCode == "NotFound")
                {
                    MessageBox.Show("You don't seemed to be connected to the internet, please check your settings and try again");
                    return;
                }

                else if (statusCode != "OK")
                {
                    MessageBox.Show("There seems to be an error: " + statusCode);

                    return;
                }

                randomLB.DataContext = searchResults;


            });
        }

        private void pivotControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            /*if (((Pivot)sender).SelectedIndex == 3)
                ApplicationBar = (Microsoft.Phone.Shell.ApplicationBar)Resources["appbar0"];
             * * */
            if (ApplicationBar != (Microsoft.Phone.Shell.ApplicationBar)Resources["appbar3"])
            ApplicationBar = (Microsoft.Phone.Shell.ApplicationBar)Resources["appbar3"];

            if (((Pivot)sender).SelectedIndex == 3)
                randomList();
            if (((Pivot)sender).SelectedIndex == 4)
                getArtists();


             
        }


        private void addSong_Click(object sender, EventArgs e)
        {
            //minClientReqIDMap[selectedSearchResult.id] = minClientReqID;
                string statusCode = "";
                string url = "https://udjplayer.com:4897/udj/players/" + connectedPlayer.id + "/active_playlist/songs/";
                var client = new RestClient(url);
                var request = new RestRequest(selectedSearchResult.id.ToString(), Method.PUT);
                request.AddHeader("X-Udj-Ticket-Hash", currentUser.hashID.ToString());
                
                
                client.ExecuteAsync(request, response =>
                {
                    statusCode = response.StatusCode.ToString();

                    if (statusCode == "NotFound")
                    {
                        MessageBox.Show("You don't seemed to be connected to the internet, please check your settings and try again");
                        return;
                    }
                    else if (statusCode == "Conflict")
                    {
                        
                            selectedSong = new ActivePlaylistEntry();
                            selectedSong.song = selectedSearchResult;
                            upOrDownVote_Click("upvote");
                            
                       
                        return;

                    }
                    else if (statusCode != "Created")
                    {
                        MessageBox.Show("There seems to be an error: " + statusCode);
                        return;
                    }
                    PhoneApplicationService.Current.State["minClientReqID"] = minClientReqID++;
                    var answer = MessageBox.Show("Your song was successfully added and upvoted!", "Song added", MessageBoxButton.OK);
                    if (answer == MessageBoxResult.OK)
                    {
                        updateNowPlaying();
                    }
                });
                
            
        }

        private void returnToEvents_Click(object sender, EventArgs e)
        {
            this.Dispatcher.BeginInvoke(() => this.NavigationService.Navigate(new Uri("/FindPlayer.xaml", UriKind.RelativeOrAbsolute)));
        }

        private void artistLB_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {

            if (artistLB.SelectedItem == _Artistselected)
            {
                artistLB.SelectedIndex = -1;
                _Artistselected = null;
               
            }
            else
            {
                _Artistselected = artistLB.SelectedItem;
                
        
            }

            if (_Artistselected != null)
            {
                settings["artist"] = _Artistselected.ToString();
                NavigationService.Navigate(new Uri("/Artist.xaml", UriKind.Relative));
            }
            

        }


        
        private void searchListBox_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var list = (ListBox)sender;

            

            if (list.SelectedItem == _selected)
            {
                list.SelectedIndex = -1;
                _selected = null;
                ApplicationBar = (Microsoft.Phone.Shell.ApplicationBar)Resources["appbar3"];
            }
            else
            {
                _selected = list.SelectedItem;
                ApplicationBar = (Microsoft.Phone.Shell.ApplicationBar)Resources["appbar2"];
                selectedSearchResult = (LibraryEntry)searchListBox.SelectedItem;
            }

            
        }

        private void randomLB_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var list = (ListBox)sender;



            if (list.SelectedItem == _selected)
            {
                list.SelectedIndex = -1;
                _selected = null;
                ApplicationBar = (Microsoft.Phone.Shell.ApplicationBar)Resources["appbar3"];
            }
            else
            {
                _selected = list.SelectedItem;
                ApplicationBar = (Microsoft.Phone.Shell.ApplicationBar)Resources["appbar2"];
                selectedSearchResult = (LibraryEntry)randomLB.SelectedItem;
            }


        }

        private void recentLB_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var list = (ListBox)sender;



            if (list.SelectedItem == _selected)
            {
                list.SelectedIndex = -1;
                _selected = null;
                ApplicationBar = (Microsoft.Phone.Shell.ApplicationBar)Resources["appbar3"];
            }
            else
            {
                _selected = list.SelectedItem;
                ApplicationBar = (Microsoft.Phone.Shell.ApplicationBar)Resources["appbar2"];
                selectedSearchResult = (LibraryEntry)recentlyPlayedLB.SelectedItem;
            }


        }

        private void queueLB_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var list = (ListBox)sender;



            if (list.SelectedItem == _queueSelected)
            {
                list.SelectedIndex = -1;
                _queueSelected = null;
                ApplicationBar = (Microsoft.Phone.Shell.ApplicationBar)Resources["appbar3"];
            }
            else
            {
                _queueSelected = list.SelectedItem;
                selectedSong = (ActivePlaylistEntry)queueLB.SelectedItem;
                ApplicationBar = (Microsoft.Phone.Shell.ApplicationBar)Resources["appbar1"];
                
            }

           
        }

        

       
        
    }
}