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
        long minClientReqID;
        Dictionary<long, long> minClientReqIDMap = new Dictionary<long, long>();

        public Artist()
        {
            
            InitializeComponent();
            
            
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

            
                PageTitle.Text = settings["artist"].ToString();
                selectedArtist = settings["artist"].ToString();


            //ApplicationBar = (Microsoft.Phone.Shell.ApplicationBar)Resources["appbar3"];
            loadingProgressBar.IsLoading = true;
            connectedPlayer = (Player)settings["connectedPlayer"]; //load connected event and user
            currentUser = (User)settings["currentUser"];
            minClientReqID = Convert.ToInt64(settings["minClientReqID"]);


            base.OnNavigatedTo(e);

            ApplicationBar = (Microsoft.Phone.Shell.ApplicationBar)Resources["appbar1"];
            getSongs();
        }

        private void getSongs()
        {
            //loadingProgressBar.IsLoading = true;
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

                

                if (statusCode != "OK")
                {
                    MessageBox.Show("There seems to be an error: " + statusCode);
                }

                else if (statusCode == "NotFound")
                {
                    MessageBox.Show("You don't seemed to be connected to the internet, please check your settings and try again");
                }
                loadingProgressBar.IsLoading = false;
                foreach (LibraryEntry t in searchResults)
                {
                    
                }
                songLB.DataContext = searchResults;

            });
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
                    this.Dispatcher.BeginInvoke(() => this.NavigationService.Navigate(new Uri("/NowPlaying.xaml", UriKind.RelativeOrAbsolute)));
                });


            }
        }

        private void addSong_Click(object sender, EventArgs e)
        {
            //minClientReqIDMap[selectedSong.song.id] = minClientReqID;
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
                    this.Dispatcher.BeginInvoke(() => this.NavigationService.Navigate(new Uri("/NowPlaying.xaml", UriKind.RelativeOrAbsolute)));
                }
            });


        }

       

    }

     
}