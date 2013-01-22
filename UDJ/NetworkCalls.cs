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
using System.Threading;
using System.Windows.Navigation;
using System.Windows.Threading;
using System.IO;

namespace UDJ
{
    class NetworkCalls<TypeData> where TypeData : new()
    {
        
        static string statusCode = null;
        static TypeData data = default(TypeData);
        static User currentUser;
        static Player selectedPlayer;
        static bool error;

        internal static void loginToUDJBefore(User currentUsr)
        {

            currentUser = currentUsr;
            var client = new RestClient("https://udjplayer.com:4897/udj/0_6");
            var request = new RestRequest("auth", Method.POST);

            request.AddParameter("username", currentUser.username);
            request.AddParameter("password", currentUser.password);

            executeNetworkCall(client, request, loginToUDJAfter, "loginToUDJ");

        }

        static void loginToUDJAfter()
        {
            if (error)
                return;
            AuthResponse authResp = data as AuthResponse;
            IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;


            if (authResp == default(AuthResponse))
            {
                settings.Remove("currentUser");
                return;
            }
            else
            {
                currentUser.hashID = authResp.ticket_hash;
                currentUser.id = authResp.user_id;
                currentUser.hashCreated = DateTime.Now; //set hashCreated to now
                // DateTime hashCreatedEcho = hashCreated; 
                string hashIDString = currentUser.hashID;
                currentUser.hashIsValid = true;
                settings["currentUser"] = currentUser; //save currentUser 
                // PhoneApplicationService.Current.State["currUser"] = this; 

                Deployment.Current.Dispatcher.BeginInvoke(() => (Application.Current.RootVisual as PhoneApplicationFrame).Navigate(new Uri("/FindPlayer.xaml", UriKind.RelativeOrAbsolute)));

            }
        }

        internal static void loginToPlayerBefore(User currentUsr, Player selectedPlyr)
        {
            currentUser = currentUsr;
            selectedPlayer = selectedPlyr;
            PhoneApplicationPage currentPage = ((App)Application.Current).RootFrame.Content as PhoneApplicationPage;
            FindPlayer findPlayer = currentPage as FindPlayer;

            bool userIsOwnerOrAdmin = false;
            foreach (User t in selectedPlayer.admins)
            {
                if (currentUser.username == t.username)
                {
                    userIsOwnerOrAdmin = true;
                    break;
                }
            }
            if (selectedPlayer.owner.username == currentUser.username)
            {
                userIsOwnerOrAdmin = true;
                currentUser.isOwnerOrAdmin = true;
                Deployment.Current.Dispatcher.BeginInvoke(() => (Application.Current.RootVisual as PhoneApplicationFrame).Navigate(new Uri("/NowPlaying.xaml", UriKind.RelativeOrAbsolute)));
                return;
            }
            currentUser.isOwnerOrAdmin = userIsOwnerOrAdmin;
            string url = "https://udjplayer.com:4897/udj/0_6/players/" + selectedPlayer.id + "/users/";
            var client = new RestClient(url);
            var request = new RestRequest("user", Method.PUT);
            request.AddHeader("X-Udj-Ticket-Hash", currentUser.hashID);
            if (selectedPlayer.has_password && selectedPlayer.password == null)
            {
                findPlayer.passwordRect.Visibility = Visibility.Visible;
                findPlayer.passwordTitle.Visibility = Visibility.Visible;
                findPlayer.passwordLabel.Visibility = Visibility.Visible;
                findPlayer.password.Visibility = Visibility.Visible;
                findPlayer.passwordButton.Visibility = Visibility.Visible;
                return;
            }

            if (selectedPlayer.has_password && selectedPlayer.password != null)
                request.AddHeader("X-Udj-Player-Password", selectedPlayer.password);

            executeNetworkCall(client, request, loginToPlayerAfter, "loginToPlayer");

        }

        static void loginToPlayerAfter()
        {
            if (error)
                return;
            IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;
            PhoneApplicationPage currentPage = ((App)Application.Current).RootFrame.Content as PhoneApplicationPage;
            FindPlayer findPlayer = currentPage as FindPlayer;
            if (statusCode == "Created")
            {
                currentUser.isAtPlayer = true; //user is at Event
                settings["currentUser"] = currentUser; //save currentUser 
                findPlayer.loadingProgressBar.IsLoading = false;

                (Application.Current.RootVisual as PhoneApplicationFrame).Navigate(new Uri("/NowPlaying.xaml", UriKind.RelativeOrAbsolute)); //navigate to NowPlaying
            }
            else
            {
                currentUser.isAtPlayer = false; //user is at Event
                settings["currentUser"] = currentUser; //save currentUser 
                findPlayer.loadingProgressBar.IsLoading = false;

            }
        }

        internal static void updateNowPlayingBefore(User currentUsr, Player selectedPlayr)
        {
            currentUser = currentUsr;
            selectedPlayer = selectedPlayr;
            string url = "https://udjplayer.com:4897/udj/0_6/players/" + selectedPlayer.id;
            var client = new RestClient(url);
            var request = new RestRequest("active_playlist", Method.GET);
            request.AddHeader("X-Udj-Ticket-Hash", currentUser.hashID.ToString());

            executeNetworkCall(client, request, updateNowPlayingAfter, "updateNowPlaying");

        }

        static void updateNowPlayingAfter()
        {
            if (error)
                return;
            PhoneApplicationPage currentPage = ((App)Application.Current).RootFrame.Content as PhoneApplicationPage;
            NowPlaying nowPlaying = currentPage as NowPlaying;
            nowPlaying.loadingProgressBar.IsLoading = false;
            
            IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;


            nowPlaying.queueLB.DataContext = new List<ActivePlaylistEntry>(); //clear the queue listbox
            
            
            ActivePlaylistResponse activePlaylistResponse = data as ActivePlaylistResponse;
            if (activePlaylistResponse.active_playlist.Count == 0)
                nowPlaying.playlistEmpty.Visibility = Visibility.Visible;
            else nowPlaying.playlistEmpty.Visibility = Visibility.Collapsed;
            nowPlaying.currentSong = activePlaylistResponse.current_song;
            PlayedActivePlaylistEntry currentSong = nowPlaying.currentSong;
            try
            {
                nowPlaying.loadingProgressBar.IsLoading = false;
                nowPlaying.artistTB.Text = currentSong.song.artist;
                nowPlaying.songTB.Text = currentSong.song.title; //if nothing is playing

                nowPlaying.albumTB.Text = currentSong.song.album;
                nowPlaying.upVotesTB.Text = currentSong.upvoters.Count.ToString();
                nowPlaying.downVotesTB.Text = currentSong.downvoters.Count.ToString();

                long minutes = currentSong.song.duration / 60; //parse duration into minutes and seconds
                long seconds = currentSong.song.duration % 60;
                nowPlaying.durationTB.Text = minutes.ToString() + " minutes and " + seconds.ToString() + " seconds";
                
            }
            catch (NullReferenceException)
            {

                MessageBox.Show("Try adding something to the playlist now!", "There's nothing playing :(", MessageBoxButton.OK);
            }


            List<ActivePlaylistEntry> queue = new List<ActivePlaylistEntry>();
            queue = activePlaylistResponse.active_playlist;
            foreach (ActivePlaylistEntry t in queue)
            {
                t.updatePageInfo();
            }
            nowPlaying.queueLB.DataContext = queue;
            selectedPlayer.volume = activePlaylistResponse.volume;
            settings["connectedPlayer"] = selectedPlayer;
        }

        internal static void searchButtonBefore(User currentUsr, Player selectedPlayr, string searchText)
        {
            currentUser = currentUsr;
            selectedPlayer = selectedPlayr;

            string url = "https://udjplayer.com:4897/udj/0_6/players/" + selectedPlayer.id;
            var client = new RestClient(url);

            var request = new RestRequest("available_music?query=" + searchText, Method.GET);
            request.AddHeader("X-Udj-Api-Version", "0.2");
            request.AddHeader("X-Udj-Ticket-Hash", currentUser.hashID.ToString());

            executeNetworkCall(client, request, searchButtonAfter, "searchButton");

        }

        static void searchButtonAfter()
        {
            if (error)
                return;
            PhoneApplicationPage currentPage = ((App)Application.Current).RootFrame.Content as PhoneApplicationPage;
            NowPlaying nowPlaying = currentPage as NowPlaying;
            nowPlaying.loadingProgressBar.IsLoading = false;
            
            IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;
            List<LibraryEntry> searchResults = data as List<LibraryEntry>;

            if (searchResults.Count == 0)
            { //if no results are returned
                nowPlaying.searchTitle.Visibility = Visibility.Visible;
            }
            else
            {
                nowPlaying.searchListBox.DataContext = searchResults;
            }
        }

        internal static void getArtistsBefore(User currentUsr, Player selectedPlayr)
        {
            currentUser = currentUsr;
            selectedPlayer = selectedPlayr;

            string url = "https://udjplayer.com:4897/udj/0_6/players/" + selectedPlayer.id;
            var client = new RestClient(url);
            var request = new RestRequest("available_music/artists", Method.GET);
            request.AddHeader("X-Udj-Ticket-Hash", currentUser.hashID.ToString());
            
            executeNetworkCall(client, request, getArtistsAfter, "getArtists");

        }

        static void getArtistsAfter()
        {
            if (error)
                return;
            PhoneApplicationPage currentPage = ((App)Application.Current).RootFrame.Content as PhoneApplicationPage;
            NowPlaying nowPlaying = currentPage as NowPlaying;
            nowPlaying.loadingProgressBar.IsLoading = false;
            
            IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;
            List<string> searchResults = data as List<string>;
            nowPlaying.artistLB.DataContext = searchResults;

        }

        internal static void upOrDownVoteBefore(User currentUsr, Player selectedPlayr, string upOrDown, string songID, string songName, bool fromArtist)
        {
            currentUser = currentUsr;
            selectedPlayer = selectedPlayr;

            string url = "https://udjplayer.com:4897/udj/0_6/players/" + selectedPlayer.id + "/active_playlist/songs/" + songID + "/";
            var client = new RestClient(url);
            var request = new RestRequest(upOrDown, Method.POST);
            request.AddHeader("X-Udj-Ticket-Hash", currentUser.hashID.ToString());
            if (fromArtist)
            {
                executeNetworkCall(client, request, upOrDownVoteArtistAfter, upOrDown, songName);
            }
            else
            {
                executeNetworkCall(client, request, upOrDownVoteAfter, upOrDown, songName);
            }
        }

        static void upOrDownVoteAfter()
        {
            if (error)
                return;
            PhoneApplicationPage currentPage = ((App)Application.Current).RootFrame.Content as PhoneApplicationPage;
            NowPlaying nowPlaying = currentPage as NowPlaying;
            nowPlaying.loadingProgressBar.IsLoading = false;
            

            NetworkCalls<ActivePlaylistResponse>.updateNowPlayingBefore(currentUser, selectedPlayer);

        }

        static void upOrDownVoteArtistAfter()
        {
            if (error)
                return;
            PhoneApplicationPage currentPage = ((App)Application.Current).RootFrame.Content as PhoneApplicationPage;
            Artist nowPlaying = currentPage as Artist;
            nowPlaying.loadingProgressBar.IsLoading = false;
            

            Deployment.Current.Dispatcher.BeginInvoke(() => (Application.Current.RootVisual as PhoneApplicationFrame).Navigate(new Uri("/FindPlayer.xaml", UriKind.RelativeOrAbsolute)));


        }

        internal static void randomBefore(User currentUsr, Player selectedPlayr)
        {
            currentUser = currentUsr;
            selectedPlayer = selectedPlayr;

            string url = "https://udjplayer.com:4897/udj/0_6/players/" + selectedPlayer.id + "/available_music/random_songs?number_of_randoms=25";
            var client = new RestClient(url);
            var request = new RestRequest("", Method.GET);
            request.AddHeader("X-Udj-Ticket-Hash", currentUser.hashID.ToString());
            executeNetworkCall(client, request, randomAfter, "random");

        }

        static void randomAfter()
        {
            if (error)
                return;
            PhoneApplicationPage currentPage = ((App)Application.Current).RootFrame.Content as PhoneApplicationPage;
            NowPlaying nowPlaying = currentPage as NowPlaying;
            nowPlaying.loadingProgressBar.IsLoading = false;
            
            List<LibraryEntry> searchResults = data as List<LibraryEntry>;

            nowPlaying.randomLB.DataContext = new List<LibraryEntry>();  //clear searchListBox
            nowPlaying.randomLB.DataContext = searchResults;
        }

        internal static void recentBefore(User currentUsr, Player selectedPlayr)
        {
            currentUser = currentUsr;
            selectedPlayer = selectedPlayr;

            string url = "https://udjplayer.com:4897/udj/0_6/players/" + selectedPlayer.id;
            var client = new RestClient(url);
            var request = new RestRequest("recently_played?max_songs=25", Method.GET);
            request.AddHeader("X-Udj-Ticket-Hash", currentUser.hashID.ToString());
            executeNetworkCall(client, request, recentAfter, "recent");

        }

        static void recentAfter()
        {
            if (error)
                return;
            PhoneApplicationPage currentPage = ((App)Application.Current).RootFrame.Content as PhoneApplicationPage;
            NowPlaying nowPlaying = currentPage as NowPlaying;
            nowPlaying.loadingProgressBar.IsLoading = false;
            
            List<PlayedActivePlaylistEntry> searchResults = data as List<PlayedActivePlaylistEntry>;
            nowPlaying.recentlyPlayedLB.DataContext = new List<LibraryEntry>();  //clear searchListBox
            nowPlaying.recentlyPlayedLB.DataContext = searchResults;
        }

        internal static void addSongBefore(User currentUsr, Player selectedPlayr, string songID, bool fromArtist)
        {
            currentUser = currentUsr;
            selectedPlayer = selectedPlayr;

            string url = "https://udjplayer.com:4897/udj/0_6/players/" + selectedPlayer.id + "/active_playlist/songs/";
            var client = new RestClient(url);
            var request = new RestRequest(songID, Method.PUT);
            request.AddHeader("X-Udj-Ticket-Hash", currentUser.hashID.ToString());
            if (fromArtist)
            {
                executeNetworkCall(client, request, addSongArtistsAfter, "addSong");

            }
            else
            {
                executeNetworkCall(client, request, addSongAfter, "addSong");
            }
        }

        static void addSongAfter()
        {
            if (error)
                return;
            PhoneApplicationPage currentPage = ((App)Application.Current).RootFrame.Content as PhoneApplicationPage;
            NowPlaying nowPlaying = currentPage as NowPlaying;
            nowPlaying.loadingProgressBar.IsLoading = false;
            

            var answer = MessageBox.Show("Your song was successfully added and upvoted!", "Song added", MessageBoxButton.OK);
            if (answer == MessageBoxResult.OK)
            {
                if (nowPlaying.searchTitle.Visibility == System.Windows.Visibility.Visible)
                    nowPlaying.searchTitle.Visibility = System.Windows.Visibility.Collapsed;
                nowPlaying.loadingProgressBar.IsLoading = true;
                NetworkCalls<ActivePlaylistResponse>.updateNowPlayingBefore(currentUser, selectedPlayer);
            }

        }

        internal static void playSongBefore(User currentUsr, Player selectedPlayr, string songID)
        {
            currentUser = currentUsr;
            selectedPlayer = selectedPlayr;

            string url = "https://udjplayer.com:4897/udj/0_6/players/" + selectedPlayer.id + "/";
            var client = new RestClient(url);
            var request = new RestRequest("current_song", Method.POST);
            request.AddParameter("lib_id", songID);
            request.AddHeader("X-Udj-Ticket-Hash", currentUser.hashID.ToString());
                executeNetworkCall(client, request, playSongAfter, "playSong");
        }

        static void playSongAfter()
        {
            if (error)
                return;
            PhoneApplicationPage currentPage = ((App)Application.Current).RootFrame.Content as PhoneApplicationPage;
            NowPlaying nowPlaying = currentPage as NowPlaying;
            nowPlaying.loadingProgressBar.IsLoading = false;
            

                NetworkCalls<ActivePlaylistResponse>.updateNowPlayingBefore(currentUser, selectedPlayer);
        }

        internal static void removeSongBefore(User currentUsr, Player selectedPlayr, string songID)
        {
            currentUser = currentUsr;
            selectedPlayer = selectedPlayr;

            string url = "https://udjplayer.com:4897/udj/0_6/players/" + selectedPlayer.id + "/active_playlist/songs/";
            var client = new RestClient(url);
            var request = new RestRequest(songID, Method.DELETE);
            request.AddHeader("X-Udj-Ticket-Hash", currentUser.hashID.ToString());
            executeNetworkCall(client, request, removeSongAfter, "removeSong");
        }

        static void removeSongAfter()
        {
            if (error)
                return;
            PhoneApplicationPage currentPage = ((App)Application.Current).RootFrame.Content as PhoneApplicationPage;
            NowPlaying nowPlaying = currentPage as NowPlaying;
            nowPlaying.loadingProgressBar.IsLoading = false;
            

            NetworkCalls<ActivePlaylistResponse>.updateNowPlayingBefore(currentUser, selectedPlayer);


        }
        static void addSongArtistsAfter()
        {
            if (error)
                return;
            PhoneApplicationPage currentPage = ((App)Application.Current).RootFrame.Content as PhoneApplicationPage;
            Artist nowPlaying = currentPage as Artist;
            nowPlaying.loadingProgressBar.IsLoading = false;
            

            var answer = MessageBox.Show("Your song was successfully added and upvoted!", "Song added", MessageBoxButton.OK);
            if (answer == MessageBoxResult.OK)
            {
                Deployment.Current.Dispatcher.BeginInvoke(() => (Application.Current.RootVisual as PhoneApplicationFrame).Navigate(new Uri("/NowPlaying.xaml", UriKind.RelativeOrAbsolute)));
            }

        }

        internal static void logoutBefore(User currentUsr, Player selectedPlayr)
        {
            currentUser = currentUsr;
            selectedPlayer = selectedPlayr;

            string url = "https://udjplayer.com:4897/udj/0_6/players/" + selectedPlayer.id + "/users/";
            var client = new RestClient(url);
            var request = new RestRequest("user", Method.DELETE);
            request.AddHeader("X-Udj-Ticket-Hash", currentUser.hashID.ToString());
            executeNetworkCall(client, request, logoutAfter, "logout");

        }

        static void logoutAfter()
        {
            if (error)
                return;
        }

        internal static void getSongBefore(User currentUsr, Player selectedPlayr, string artist)
        {
            currentUser = currentUsr;
            selectedPlayer = selectedPlayr;

            string url = "https://udjplayer.com:4897/udj/0_6/players/" + selectedPlayer.id;
            var client = new RestClient(url);
            var request = new RestRequest("available_music/artists/" + artist, Method.GET);
            request.AddHeader("X-Udj-Ticket-Hash", currentUser.hashID.ToString());
            executeNetworkCall(client, request, getSongAfter, "getSong");

        }

        static void getSongAfter()
        {
            if (error)
                return;
            PhoneApplicationPage currentPage = ((App)Application.Current).RootFrame.Content as PhoneApplicationPage;
            Artist artistPage = currentPage as Artist;
            artistPage.loadingProgressBar.IsLoading = false;
            

            artistPage.noSongsTB.Visibility = System.Windows.Visibility.Collapsed;
            artistPage.songLB.DataContext = new List<LibraryEntry>();  //clear searchListBox
            List<LibraryEntry> searchResults = data as List<LibraryEntry>;
            if (searchResults == null)
                return;

            if (searchResults.Count == 0)
                artistPage.noSongsTB.Visibility = System.Windows.Visibility.Visible;
            artistPage.songLB.DataContext = searchResults;

        }

        internal static void pauseBefore(User currentUsr, Player selectedPlayr, string paused)
        {
            currentUser = currentUsr;
            selectedPlayer = selectedPlayr;

            string url = "https://udjplayer.com:4897/udj/0_6/players/" + selectedPlayer.id;
            var client = new RestClient(url);
            var request = new RestRequest("state", Method.POST);
            request.AddHeader("X-Udj-Ticket-Hash", currentUser.hashID.ToString());
            request.AddParameter("state", paused);

            executeNetworkCall(client, request, pauseAfter, "pause");

        }

        static void pauseAfter()
        {
            if (error)
                return;
            PhoneApplicationPage currentPage = ((App)Application.Current).RootFrame.Content as PhoneApplicationPage;
            NowPlaying artistPage = currentPage as NowPlaying;
            artistPage.loadingProgressBar.IsLoading = false;
            

        }

        internal static void volumeBefore(User currentUsr, Player selectedPlayr, string volume)
        {
            currentUser = currentUsr;
            selectedPlayer = selectedPlayr;

            string url = "https://udjplayer.com:4897/udj/0_6/players/" + selectedPlayer.id;
            var client = new RestClient(url);
            var request = new RestRequest("volume", Method.POST);
            request.AddHeader("X-Udj-Ticket-Hash", currentUser.hashID.ToString());
            request.AddParameter("volume", volume);

            executeNetworkCall(client, request, volumeAfter, "volume");

        }

        static void volumeAfter()
        {
            if (error)
                return;
            PhoneApplicationPage currentPage = ((App)Application.Current).RootFrame.Content as PhoneApplicationPage;
            NowPlaying artistPage = currentPage as NowPlaying;
            artistPage.loadingProgressBar.IsLoading = false;
            


        }


        static private void returnToEventsNoLogOut_Click(object sender, EventArgs e)
        {

            (Application.Current.RootVisual as PhoneApplicationFrame).Navigate(new Uri("/FindPlayer.xaml", UriKind.RelativeOrAbsolute)); //navigate to NowPlaying
        }

        static private void deselectAll()
        {
            PhoneApplicationPage currentPage = ((App)Application.Current).RootFrame.Content as PhoneApplicationPage;
            FindPlayer findPlayer = currentPage as FindPlayer;

            if (findPlayer.playerListBox.SelectedItem != null)
                findPlayer.playerListBox.SelectedItem = null;
            else if (findPlayer.recentPlayerListBox.SelectedItem != null)
                findPlayer.recentPlayerListBox.SelectedItem = null;
            else findPlayer.favPlayersListBox.SelectedItem = null;
        }

        internal static void findNearestPlayerBefore(User currentUsr)
        {
            currentUser = currentUsr;
            
            string url = "https://udjplayer.com:4897/udj/0_6/players/";

            //currentUser.latitude = 40.113523; //sample coordinates that return players
            //currentUser.longitude = -88.224006;

            url += currentUser.latitude + "/";
            var client = new RestClient(url);
            var request = new RestRequest(currentUser.longitude.ToString(), Method.GET);
            request.AddHeader("X-Udj-Ticket-Hash", currentUser.hashID);
            executeNetworkCall(client, request, findNearestPlayerAfter, "findNearestPlayer");
        }

        static void findNearestPlayerAfter()
        {
            if (error)
                return;
            IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;

            PhoneApplicationPage currentPage = ((App)Application.Current).RootFrame.Content as PhoneApplicationPage;
            FindPlayer findPlayer = currentPage as FindPlayer;
            List<Player> players = new List<Player>(); //list to store returned events
            players = data as List<Player>;
            findPlayer.progressBar = false;
            findPlayer.loadingTextBlock.Visibility = Visibility.Collapsed;  //get rid of "Loading..." textblock when the players are ready to be retreived
            findPlayer.playerListBox.DataContext = new List<Player>(); //clear the eventListBox
                    findPlayer.playerListBox.DataContext = players;
                    if (players == null || players.Count == 0)
                    {
                        findPlayer.noNearbyTextBlock.Visibility = Visibility.Visible;
                    }

                    if (!settings.Contains("recentPlayers")) //if there are no recentEvents list saved, then display noRecentTextBlock
                {
                    findPlayer.noRecentTextBlock.Visibility = Visibility.Visible;

                }
                else
                {
                    findPlayer.noRecentTextBlock.Visibility = Visibility.Collapsed; //there are recent players so hide noRecentTextBlock
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
                    findPlayer.recentPlayerListBox.DataContext = new List<PlayerDatePair>(); //clear recentEventListBox
                    findPlayer.recentPlayerListBox.DataContext = recentPlayersList; //display recentEventsList in the recentEventListBox
                    settings["recentPlayers"] = recentPlayersList;  //save the revised events list
                }

                if (!settings.Contains("favPlayers"))  //if there are no favorite events saved
                {
                    findPlayer.noFavTextBlock.Visibility = Visibility.Visible; //show noFavTextBlock

                }
                else
                {
                    findPlayer.noFavTextBlock.Visibility = Visibility.Collapsed;  //hide noFavTextBlock

                    List<Player> favPlayersList = new List<Player>(); //clear the favorite events list
                    
                        favPlayersList = (List<Player>)settings["favPlayers"];
                    List<string> checkForDup = new List<string>();
                    for (int i = 0; i < favPlayersList.Count; i++) //for all events the user recently visited
                    {
                        if (checkForDup.Contains(favPlayersList[i].name)) //if the event is already on the list
                            favPlayersList.RemoveAt(i); //remove it
                        else checkForDup.Add(favPlayersList[i].name); //otherwise add it to the checkFor list
                    }
                    findPlayer.favPlayersListBox.DataContext = new List<Player>(); //clear favEventListBox, the listbox that contains the user's favorited events
                        findPlayer.favPlayersListBox.DataContext = favPlayersList; 
                        settings["favPlayers"] = favPlayersList; //save the favorites list
                }
                settings["currentUser"] = currentUser; //save currentUser 


        }
            

        static void threadWait(RestClient client, RestRequest request, EventWaitHandle Wait, Action functor) 
        {
            Wait.WaitOne();
            Deployment.Current.Dispatcher.BeginInvoke(() => functor.Invoke());


            
        }

        static void executeNetworkCall(RestSharp.RestClient client, RestSharp.RestRequest request, Action functor, string call)
        {
            executeNetworkCall(client, request, functor, call, null);

        }


        static void executeNetworkCall(RestSharp.RestClient client, RestSharp.RestRequest request, Action functor, string call, string songName)
        {
            EventWaitHandle Wait = new AutoResetEvent(false);
            Thread execRun = new Thread(() => threadWait(client, request, Wait, functor));
            execRun.Start();
            IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;

            

            client.ExecuteAsync<TypeData>(request, response =>
            {
                statusCode = response.StatusCode.ToString();  //stores the Status of the request


                if (statusCode == "OK" || statusCode == "Created")  //if everything went okay
                {
                    error = false;
                    data = response.Data;

                    switch (call)
                    {
                        case "upvote":
                        case "downvote":
                            MessageBox.Show("Congrats, you've successfully " + call + "d " + songName + "!", call[0].ToString().ToUpper() + call.Substring(1) + " was successful", MessageBoxButton.OK);

                            break;
                    }

                }
                else
                {
                    error = true;
                    data = default(TypeData);
                    
                    if (statusCode == "0" || call == "logout")
                    {

                    }

                    else if (statusCode == "NotFound")
                    {
                        switch (call)
                        {
                            
                            case "loginToPlayer":
                                deselectAll();
                                MessageBox.Show("It would be in your best interests to try again. Check your wifis!");
                                break;
                            case "updateNowPlaying":
                            case "searchButton":
                            case "getArtists":
                            case "random":
                            case "upOrDownVote":
                            case "recent":
                            case "addSong":
                            case "getSong":
                            case "playSong":
                            case "removeSong":
                            case "pause":
                            case "volume":
                                int i;
                                for (i = 0; i < response.Headers.Count; i++)
                                {
                                    if (response.Headers[i].Value.ToString().Contains("inactive"))
                                    {
                                        MessageBox.Show("Looks like this player isn't running anymore. Try a different player!");
                                        returnToEventsNoLogOut_Click(new object(), new EventArgs());
                                        break;
                                    }

                                     if (response.Headers[i].Value.ToString().Contains("song"))
                                     {
                                         MessageBox.Show("Hmm that song doesn't exist in the library anymore. Try picking another one!");
                                         break;
                                     }
                                }
                                if (i == response.Headers.Count)
                                {
                                    returnToEventsNoLogOut_Click(new object(), new EventArgs());
                                    MessageBox.Show("It would be in your best interests to try again. Check your wifis!");
                                }
                                break;
                            case "loginToUDJ":
                            default:
                                MessageBox.Show("It would be in your best interests to try again. Check your wifis!");
                                break;
                            
                            
                        }

                    }

                    else if (statusCode == "BadRequest")
                    {
                        MessageBox.Show("Did you forget to type something? Please try again!");

                    }

                    else if (statusCode == "Forbidden") //User already logged into player
                    {
                        string cause = "";
                        for (int i = 0; i < response.Headers.Count; i++)
                        {
                            if (response.Headers[i].Value.ToString().Contains("player-full"))
                            {
                                cause = "full";
                            }
                            else if (response.Headers[i].Value.ToString().Contains("banned"))
                            {
                                cause = "banned";
                            }
                            else cause = "unknown";
                        }

                        switch (cause)
                        {
                            case "full":
                                MessageBox.Show("Sorry buddy, looks like " + selectedPlayer.owner.username + ", the owner, has a limit to this room. Tell him you want in!");
                                break;
                            case "banned":
                                MessageBox.Show("Yikes, looks like the owners don't want you here. Try talking to " + selectedPlayer.owner.username + ", the owner, directly.");
                                break;
                            default:
                                MessageBox.Show("WARNING WARNING: You are forbidden to join this event. Sorry buddy.");
                                break;
                        }

                        deselectAll();
                    }

                    else if (statusCode == "Unauthorized")
                    {
                        switch (call)
                        {
                            
                            case "loginToPlayer":
                                var message = MessageBox.Show("Woah slow down cowboy, this requires a password. Tap it again and put it in when you're asked. Click okay if you want me to do that for you.", "YOU SHALL NOT PASS", MessageBoxButton.OKCancel);
                                selectedPlayer.has_password = true;
                                if (message == MessageBoxResult.OK)
                                    NetworkCalls<List<String>>.loginToPlayerBefore(currentUser, selectedPlayer);
                                else deselectAll();
                                break;
                            case "updateNowPlaying":
                            case "searchButton":
                            case "getArtists": 
                            case "random":
                            case "upOrDownVote":
                            case "recent":
                            case "addSong":
                            case "getSong":
                            case "playSong":
                            case "removeSong":
                            case "pause":
                            case "volume":
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
                                break;
                            case "loginToUDJ":
                            default:
                                MessageBox.Show("Either that's not your username or that's not your password. Please try again!");
                                Deployment.Current.Dispatcher.BeginInvoke(() => (Application.Current.RootVisual as PhoneApplicationFrame).Navigate(new Uri("/MainPage.xaml", UriKind.RelativeOrAbsolute)));
                                break;
                        }
                    }

                    else
                    {

                        MessageBox.Show("Whoops! Something went wrong, I'll redirect you to the login screen. Here's the error: " + statusCode);
                        settings.Remove("currentUser");
                        Deployment.Current.Dispatcher.BeginInvoke(() => (Application.Current.RootVisual as PhoneApplicationFrame).Navigate(new Uri("/MainPage.xaml", UriKind.RelativeOrAbsolute)));

                    }
                }

                Wait.Set();
            });
            
            
        }
    }
}
