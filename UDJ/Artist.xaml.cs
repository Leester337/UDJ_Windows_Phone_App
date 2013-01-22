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
using System.IO.IsolatedStorage;
using Microsoft.Phone.Shell;

namespace UDJ
{
    public partial class Artist : PhoneApplicationPage
    {
        User currentUser;
        Player connectedPlayer;
        LibraryEntry selectedSearchResult = null;
        static IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;
        string selectedArtist = "";
        private object _selected;
        ActivePlaylistEntry selectedSong = null;

        public Artist()
        {
            
            InitializeComponent();
            
            
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e)
        {
            settings["connectedPlayer"] = connectedPlayer;
            settings["currentUser"] = currentUser;
            IsolatedStorageSettings.ApplicationSettings.Save();
            base.OnNavigatedFrom(e);
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {

            
                PageTitle.Text = settings["artist"].ToString();
                selectedArtist = settings["artist"].ToString();


            //ApplicationBar = (Microsoft.Phone.Shell.ApplicationBar)Resources["appbar3"];
            loadingProgressBar.IsLoading = true;
            connectedPlayer = (Player)settings["connectedPlayer"]; //load connected event and user
            currentUser = (User)settings["currentUser"];


            base.OnNavigatedTo(e);

            ApplicationBar = (Microsoft.Phone.Shell.ApplicationBar)Resources["appbar1"];
            getSongs();
        }

        private void getSongs()
        {
            loadingProgressBar.IsLoading = true;
            NetworkCalls<List<LibraryEntry>>.getSongBefore(currentUser, connectedPlayer, selectedArtist);
            /*
            string statusCode = "";
            string url = "https://udjplayer.com:4897/udj/0_6/players/" + connectedPlayer.id;
            var client = new RestClient(url);
            var request = new RestRequest("available_music/artists/" + selectedArtist, Method.GET);
            request.AddHeader("X-Udj-Ticket-Hash", currentUser.hashID.ToString());
            
            client.ExecuteAsync<List<LibraryEntry>>(request, response =>
            {

                noSongsTB.Visibility = System.Windows.Visibility.Collapsed;
                songLB.DataContext = new List<LibraryEntry>();  //clear searchListBox
                List<LibraryEntry> searchResults = response.Data;
                statusCode = response.StatusCode.ToString();
                string statuscodestring = statusCode;

                if (searchResults == null)
                    return;

                if (searchResults.Count == 0)
                    noSongsTB.Visibility = System.Windows.Visibility.Visible;

                if (statusCode == "0")
                {
                    return;
                }

                else if (statusCode == "NotFound")
                {
                    for (int i = 0; i < response.Headers.Count; i++)
                    {
                        if (response.Headers[i].Value.ToString().Contains("inactive"))
                        {
                            MessageBox.Show("Looks like this player isn't running anymore. Try a different player!");
                            returnToEventsNoLogOut_Click(new object(), new EventArgs());
                            return;
                        }
                    }
                    MessageBox.Show("You don't seemed to be connected to the internet, please check your settings and try again");
                    loadingProgressBar.IsLoading = false;
                    return;
                }
                else if (statusCode == "Unauthorized")
                {
                    for (int i = 0; i < response.Headers.Count; i++)
                    {
                        if (response.Headers[i].Value.ToString().Contains("kicked"))
                        {
                            MessageBox.Show("Ouch. You've been kicked. I'm going to take you back to the players, just login to this player again to participate.");
                            returnToEventsNoLogOut_Click(new object(), new EventArgs());
                            return;
                        }

                        if (response.Headers[i].Value.ToString().Contains("begin-participating"))
                        {
                            MessageBox.Show("Looks like you've timed out pretty hard. Let's go back to the players page and try again.");
                            returnToEventsNoLogOut_Click(new object(), new EventArgs());
                            return;
                        }
                    }
                    loadingProgressBar.IsLoading = false;
                    return;
                }

                else if (statusCode != "OK")
                {
                    loadingProgressBar.IsLoading = false;
                    MessageBox.Show("There seems to be an error: " + statusCode);
                    return;
                }

                loadingProgressBar.IsLoading = false;
                foreach (LibraryEntry t in searchResults)
                {
                    
                }
                songLB.DataContext = searchResults;

            });
            */
        }

        private void songLB_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var list = (ListBox)sender;



            if (list.SelectedItem == _selected)
            {
                list.SelectedIndex = -1;
                _selected = null;
                ApplicationBar = (Microsoft.Phone.Shell.ApplicationBar)Resources["appbar1"];
            }
            else
            {
                _selected = list.SelectedItem;
                selectedSearchResult = (LibraryEntry)songLB.SelectedItem;
                ApplicationBar = (Microsoft.Phone.Shell.ApplicationBar)Resources["appbar2"];

            }
        }

        private void returnToNowPlayingButton_Click(object sender, EventArgs e)
        {
            this.Dispatcher.BeginInvoke(() => this.NavigationService.Navigate(new Uri("/NowPlaying.xaml", UriKind.RelativeOrAbsolute)));
        }

        private void returnToEvents_Click(object sender, EventArgs e)
        {
            this.Dispatcher.BeginInvoke(() => this.NavigationService.Navigate(new Uri("/FindPlayer.xaml", UriKind.RelativeOrAbsolute)));
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
                NetworkCalls<List<string>>.upOrDownVoteBefore(currentUser, connectedPlayer, upOrDown, selectedSong.song.id.ToString(), selectedSong.song.title, true);
                /*
                string statusCode = "";
                string url = "https://udjplayer.com:4897/udj/0_6/players/" + connectedPlayer.id + "/active_playlist/songs/" + selectedSong.song.id.ToString() + "/";
                var client = new RestClient(url);
                var request = new RestRequest(upOrDown, Method.POST);
                ActivePlaylistEntry selectedSongString = selectedSong;
                request.AddHeader("X-Udj-Ticket-Hash", currentUser.hashID.ToString());

                client.ExecuteAsync(request, response =>
                {
                    statusCode = response.StatusCode.ToString();

                    if (statusCode == "0")
                    {
                        return;
                    }

                    else if (statusCode == "NotFound")
                    {
                        for (int i = 0; i < response.Headers.Count; i++)
                        {
                            if (response.Headers[i].Value.ToString().Contains("song"))
                            {
                                MessageBox.Show("Hmm that song doesn't exist in the library anymore. Try picking another one!");
                                return;
                            }
                            
                                if (response.Headers[i].Value.ToString().Contains("inactive"))
                                {
                                    MessageBox.Show("Looks like this player isn't running anymore. Try a different player!");
                                    returnToEventsNoLogOut_Click(new object(), new EventArgs());
                                    return;
                                }
                           
                        }
                        loadingProgressBar.IsLoading = false;
                        MessageBox.Show("You don't seemed to be connected to the internet, please check your settings and try again");
                        return;
                    }

                    else if (statusCode == "Unauthorized")
                    {
                        for (int i = 0; i < response.Headers.Count; i++)
                        {
                            if (response.Headers[i].Value.ToString().Contains("kicked"))
                            {
                                MessageBox.Show("Ouch. You've been kicked. I'm going to take you back to the players, just login to this player again to participate.");
                                returnToEventsNoLogOut_Click(new object(), new EventArgs());
                                return;
                            }

                            if (response.Headers[i].Value.ToString().Contains("begin-participating"))
                            {
                                MessageBox.Show("Looks like you've timed out pretty hard. Let's go back to the players page and try again.");
                                returnToEventsNoLogOut_Click(new object(), new EventArgs());
                                return;
                            }
                        }
                        loadingProgressBar.IsLoading = false;
                        return;
                    }

                    else if (statusCode != "OK")
                    {
                        MessageBox.Show("There seems to be an error: " + statusCode);
                        return;
                    }
                    
                    else
                    {
                        loadingProgressBar.IsLoading = false;
                        MessageBox.Show("Congrats, you've successfully " + upOrDown + "d " + selectedSong.song.title + "!", upOrDown[0].ToString().ToUpper() + upOrDown.Substring(1) + " was successful", MessageBoxButton.OK);
                    }
                    
                    this.Dispatcher.BeginInvoke(() => this.NavigationService.Navigate(new Uri("/NowPlaying.xaml", UriKind.RelativeOrAbsolute)));
                });

                */
            }
        }

        private void addSong_Click(object sender, EventArgs e)
        {
            loadingProgressBar.IsLoading = true;
            NetworkCalls<List<string>>.addSongBefore(currentUser, connectedPlayer, selectedSearchResult.id.ToString(), true);
            /*
            string statusCode = "";
            string url = "https://udjplayer.com:4897/udj/0_6/players/" + connectedPlayer.id + "/active_playlist/songs/";
                var client = new RestClient(url);
                var request = new RestRequest(selectedSearchResult.id.ToString(), Method.PUT);
                request.AddHeader("X-Udj-Ticket-Hash", currentUser.hashID.ToString());
                
                
                client.ExecuteAsync(request, response =>
                {
                    statusCode = response.StatusCode.ToString();

                    if (statusCode == "0")
                    {
                        return;
                    }

                    else if (statusCode == "NotFound")
                    {

                        for (int i = 0; i < response.Headers.Count; i++)
                        {
                            if (response.Headers[i].Value.ToString().Contains("song"))
                            {
                                MessageBox.Show("Hmm that song doesn't exist in the library anymore. Try picking another one!");
                                return;
                            }
                            
                                if (response.Headers[i].Value.ToString().Contains("inactive"))
                                {
                                    MessageBox.Show("Looks like this player isn't running anymore. Try a different player!");
                                    returnToEventsNoLogOut_Click(new object(), new EventArgs());
                                    return;
                                }
                            
                        }
                        loadingProgressBar.IsLoading = false;
                        MessageBox.Show("You don't seemed to be connected to the internet, please check your settings and try again");
                        return;
                    }

                    else if (statusCode == "Unauthorized")
                    {
                        for (int i = 0; i < response.Headers.Count; i++)
                        {
                            if (response.Headers[i].Value.ToString().Contains("kicked"))
                            {
                                MessageBox.Show("Ouch. You've been kicked. I'm going to take you back to the players, just login to this player again to participate.");
                                returnToEventsNoLogOut_Click(new object(), new EventArgs());
                                return;
                            }

                            if (response.Headers[i].Value.ToString().Contains("begin-participating"))
                            {
                                MessageBox.Show("Looks like you've timed out pretty hard. Let's go back to the players page and try again.");
                                returnToEventsNoLogOut_Click(new object(), new EventArgs());
                                return;
                            }
                        }
                        loadingProgressBar.IsLoading = false;
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
                        loadingProgressBar.IsLoading = false;
                        return;
                    }
                    
                var answer = MessageBox.Show("Your song was successfully added and upvoted!", "Song added", MessageBoxButton.OK);
                if (answer == MessageBoxResult.OK)
                {
                    this.Dispatcher.BeginInvoke(() => this.NavigationService.Navigate(new Uri("/NowPlaying.xaml", UriKind.RelativeOrAbsolute)));
                }
            });

            */
        }

        private void returnToEventsNoLogOut_Click(object sender, EventArgs e)
        {

            this.Dispatcher.BeginInvoke(() => this.NavigationService.Navigate(new Uri("/FindPlayer.xaml", UriKind.RelativeOrAbsolute)));
        }

       

    }

     
}