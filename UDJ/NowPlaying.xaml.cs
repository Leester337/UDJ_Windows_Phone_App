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
    public partial class NowPlaying : PhoneApplicationPage
    {
        static IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;

        User currentUser;
        Player connectedPlayer;
        internal PlayedActivePlaylistEntry currentSong = new PlayedActivePlaylistEntry();
        ActivePlaylistEntry selectedSong = null;
        LibraryEntry selectedSearchResult = null;
        PlayedActivePlaylistEntry selectedRecent = null;
        private object _selected;
        private object _queueSelected;
        private object _Artistselected;
        bool invalidPlayer = false;
        bool paused = false;
        bool isVolOpen = false;

        public NowPlaying()
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
            checkBackgroundColor();
            changeOwnerLayout();
            getArtists();
         
            base.OnNavigatedTo(e);
        }

        private void changeOwnerLayout()
        {
       
        }

        private void updateNowPlaying()
        {
            loadingProgressBar.IsLoading = true;
            if (searchTitle.Visibility == System.Windows.Visibility.Visible)
                searchTitle.Visibility = System.Windows.Visibility.Collapsed;

            NetworkCalls<ActivePlaylistResponse>.updateNowPlayingBefore(currentUser, connectedPlayer);
        }

        private void searchButton_Click(object sender, RoutedEventArgs e) //when user clicks search button
        {
           
                if (searchTitle.Visibility == System.Windows.Visibility.Visible)
                    searchTitle.Visibility = System.Windows.Visibility.Collapsed;
                searchListBox.DataContext = new List<LibraryEntry>();  //clear searchListBox

                if (searchTextBox.Text == "" || searchTextBox.Text == "Enter your search here")
                {
                    MessageBox.Show("Try searching again buddy. This time with actual words.");
                    return;
                }

                loadingProgressBar.IsLoading = true;

                NetworkCalls<List<LibraryEntry>>.searchButtonBefore(currentUser, connectedPlayer, searchTextBox.Text);
           
        }

        private void getArtists()
        {
            if (invalidPlayer)
                return;
            loadingProgressBar.IsLoading = true;
            artistLB.DataContext = new List<LibraryEntry>();  //clear searchListBox

            NetworkCalls<List<string>>.getArtistsBefore(currentUser, connectedPlayer);

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

                NetworkCalls<List<string>>.upOrDownVoteBefore(currentUser, connectedPlayer, upOrDown, selectedSong.song.id.ToString(), selectedSong.song.title, false);

                

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
            if (pivotControl.SelectedIndex == 0 || pivotControl.SelectedIndex == 1)
                updateNowPlaying();
            if (pivotControl.SelectedIndex == 2)
            {
                if (searchTextBox.Text != null)
                    searchButton_Click(new object(), new RoutedEventArgs());
                else MessageBox.Show("Try typing your search term in the search box. Then I'd be happy to get you some delicious refreshed results.");
            }
            if (pivotControl.SelectedIndex == 3)
                randomList();
            if (pivotControl.SelectedIndex == 4)
                getArtists();
        }

    

        

        private void randomList()
        {
            
            loadingProgressBar.IsLoading = true;
            NetworkCalls<List<LibraryEntry>>.randomBefore(currentUser, connectedPlayer);

            
        }

        private void getRecent()
        {

            loadingProgressBar.IsLoading = true;
            NetworkCalls<List<PlayedActivePlaylistEntry>>.recentBefore(currentUser, connectedPlayer);

            
        }

        private void pivotControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            /*if (((Pivot)sender).SelectedIndex == 3)
                ApplicationBar = (Microsoft.Phone.Shell.ApplicationBar)Resources["appbar0"];
             * * */
            this.Focus();
            if (((Pivot)sender).SelectedIndex == 0 || ((Pivot)sender).SelectedIndex == 1)
            {
                
                if (!invalidPlayer)
                    updateNowPlaying();
            }

            if (((Pivot)sender).SelectedIndex == 0 && currentUser.isOwnerOrAdmin)
                ApplicationBar = (Microsoft.Phone.Shell.ApplicationBar)Resources["ownerBar"];
            else if (ApplicationBar != (Microsoft.Phone.Shell.ApplicationBar)Resources["appbar3"])
            ApplicationBar = (Microsoft.Phone.Shell.ApplicationBar)Resources["appbar3"];

            
            if (((Pivot)sender).SelectedIndex == 3)
                randomList();
           // if (((Pivot)sender).SelectedIndex == 4)
                //getArtists();
            if (((Pivot)sender).SelectedIndex == 5)
                getRecent();


             
        }


        private void addSong_Click(object sender, EventArgs e)
        {
            loadingProgressBar.IsLoading = true;
            string songID = selectedSearchResult == null ? selectedRecent.song.id.ToString() : selectedSearchResult.id.ToString();
            NetworkCalls<List<string>>.addSongBefore(currentUser, connectedPlayer, songID, false);
           
        }

        private void returnToEvents_Click(object sender, EventArgs e)
        {
            
            NavigationService.Navigate(new Uri("/FindPlayer.xaml", UriKind.RelativeOrAbsolute));
            logoutUser();
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
                selectedRecent = (PlayedActivePlaylistEntry)recentlyPlayedLB.SelectedItem;
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

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            if (isVolOpen)
            {
                volFadeOut();
                isVolOpen = false;
                e.Cancel = true;
                return;
            }
            
            if (!NavigationService.CanGoBack)
            {
                var answer = MessageBox.Show("If you want to exit UDJ, tap OK. Otherwise, Cancel will take you back to the players.", "Exit?", MessageBoxButton.OKCancel);

                if (answer == MessageBoxResult.Cancel)
                {
                    NavigationService.Navigate(new Uri("/FindPlayer.xaml", UriKind.RelativeOrAbsolute));
                    logoutUser();
                }
            }
            else logoutUser();
            
        }

        private void logoutUser()
        {

            if (currentUser.isOwnerOrAdmin)
            {
                return;
            }
            else 
            {
                NetworkCalls<List<string>>.logoutBefore(currentUser, connectedPlayer);

               
            }
        }

        private void MyContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            if (!currentUser.isOwnerOrAdmin)
            {
                ContextMenu contextMenu = sender as ContextMenu;
                contextMenu.IsOpen = false;
                MessageBox.Show("Ask " + connectedPlayer.owner.username + " to make you an Admin if you want to delete songs or immediately play them!");
            }
                
        }

        private void contextMenuPlay_Click(object sender, RoutedEventArgs e)
        {
            selectedSong = (((sender as MenuItem).Parent as ContextMenu).DataContext as ActivePlaylistEntry);
            var answer = MessageBox.Show("Are you sure you want to play this now? This will stop " + currentSong.song.title + " from playing.", "Play Now?", MessageBoxButton.OKCancel);
            if (answer == MessageBoxResult.Cancel)
            {
                MenuItem contextMenuItem = sender as MenuItem;
                ((ContextMenu)(contextMenuItem.Parent)).IsOpen = false;
            }
            else
            {
                MenuItem contextMenuItem = sender as MenuItem;
                ((ContextMenu)(contextMenuItem.Parent)).IsOpen = false;
                NetworkCalls<List<string>>.playSongBefore(currentUser, connectedPlayer, selectedSong.song.id);

            }

        }

        private void contextMenuRemove_Click(object sender, RoutedEventArgs e)
        {
            selectedSong = (((sender as MenuItem).Parent as ContextMenu).DataContext as ActivePlaylistEntry);
            //ListBoxItem contextMenuListItem = queueLB.ItemContainerGenerator.ContainerFromItem((sender as ContextMenu).DataContext) as ListBoxItem;
            var answer = MessageBox.Show("Are you sure you want to remove " + selectedSong.title + " from the active playlist? You can always add it again later, but it's " + selectedSong.total_votes + " votes will be lost!", "Remove from Playlist?", MessageBoxButton.OKCancel);
            if (answer == MessageBoxResult.Cancel)
            {
                MenuItem contextMenuItem = sender as MenuItem;
                ((ContextMenu)(contextMenuItem.Parent)).IsOpen = false;
            }
            else
            {
                MenuItem contextMenuItem = sender as MenuItem;
                ((ContextMenu)(contextMenuItem.Parent)).IsOpen = false;
                loadingProgressBar.IsLoading = true;
                NetworkCalls<List<string>>.removeSongBefore(currentUser, connectedPlayer, selectedSong.song.id);

            }
        }

        private void changeVolume_Click(object sender, EventArgs e)
        {
            isVolOpen = true;
            volSlider.Value = connectedPlayer.volume;
            volNum.Text = connectedPlayer.volume.ToString();
            CreateFadeInOutAnimation(0.0, 0.75, 0.0, 1.0).Begin();


        }

        private void volFadeOut()
        {
            CreateFadeInOutAnimation(0.75, 0.0, 1.0, 0.0).Begin();
        }

        private Storyboard CreateFadeInOutAnimation(double recFrom, double recTo, double canvasFrom, double canvasTo)
        {
            Storyboard sb = new Storyboard();
            DoubleAnimation recFadeInAnimation = new DoubleAnimation();
            recFadeInAnimation.From = recFrom;
            recFadeInAnimation.To = recTo;
            recFadeInAnimation.Duration = new Duration(TimeSpan.FromSeconds(1.0));

            DoubleAnimation canvasFadeInAnimation = new DoubleAnimation();
            canvasFadeInAnimation.From = canvasFrom;
            canvasFadeInAnimation.To = canvasTo;
            canvasFadeInAnimation.Duration = new Duration(TimeSpan.FromSeconds(1.0));

            Storyboard.SetTarget(recFadeInAnimation, volumeRect);
            Storyboard.SetTargetProperty(recFadeInAnimation, new PropertyPath("Opacity"));

            Storyboard.SetTarget(canvasFadeInAnimation, volumeCanvas);
            Storyboard.SetTargetProperty(canvasFadeInAnimation, new PropertyPath("Opacity"));

            sb.Children.Add(canvasFadeInAnimation);
            sb.Children.Add(recFadeInAnimation);

            return sb;
        }

        private void pauseMusic_Click(object sender, EventArgs e)
        {
            if (paused)
            {
                var answer = MessageBox.Show("Are you sure you want to start playing " + currentSong.song.title + " ?", "Play the Playlist?", MessageBoxButton.OKCancel);
                if (answer == MessageBoxResult.Cancel)
                {
                }
                else
                {
                    ApplicationBarIconButton btn = (ApplicationBarIconButton)ApplicationBar.Buttons[1];
                    btn.IconUri = new Uri("icons/appbar.transport.pause.rest.png", UriKind.Relative);
                    loadingProgressBar.IsLoading = true;
                    NetworkCalls<List<string>>.pauseBefore(currentUser, connectedPlayer, "playing");

                    paused = false;

                }
            }
            else
            {
                var answer = MessageBox.Show("Are you sure you want to pause " + currentSong.song.title + "?", "Pause the Playlist?", MessageBoxButton.OKCancel);
                if (answer == MessageBoxResult.Cancel)
                {
                }
                else
                {
                    ApplicationBarIconButton btn = (ApplicationBarIconButton)ApplicationBar.Buttons[1];
                    btn.IconUri = new Uri("icons/appbar.transport.play.rest.png", UriKind.Relative);
                    loadingProgressBar.IsLoading = true;
                    NetworkCalls<List<string>>.pauseBefore(currentUser, connectedPlayer, "paused");

                    paused = true;

                }
            }
        }

        private void volSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            volNum.Text = Convert.ToInt16(volSlider.Value).ToString();
        }

        private void volCancel_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            volFadeOut();
            isVolOpen = false;
        }

        private void volOK_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            loadingProgressBar.IsLoading = true;
            connectedPlayer.volume = Convert.ToInt16(volNum.Text);
            NetworkCalls<List<string>>.volumeBefore(currentUser, connectedPlayer, volNum.Text);
            isVolOpen = false;
            volFadeOut();
        }

        

       
        
    }
}