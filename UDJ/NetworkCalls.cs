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

        static void loginToUDJAfter()
        {
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
                settings["currentUser"] = currentUser; //save currentUser 
                // PhoneApplicationService.Current.State["currUser"] = this; 

                Deployment.Current.Dispatcher.BeginInvoke(() => (Application.Current.RootVisual as PhoneApplicationFrame).Navigate(new Uri("/FindPlayer.xaml", UriKind.RelativeOrAbsolute)));

            }
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
            executeNetworkCall(client, request, currentUser, findNearestPlayerAfter);
        }

        static void findNearestPlayerAfter()
        {
            IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;

            PhoneApplicationPage currentPage = ((App)Application.Current).RootFrame.Content as PhoneApplicationPage;
            FindPlayer findPlayer = currentPage as FindPlayer;
            List<Player> players = new List<Player>(); //list to store returned events
            players = data as List<Player>;
            findPlayer.progressBar = false;
            findPlayer.loadingTextBlock.Visibility = Visibility.Collapsed;  //get rid of "Loading..." textblock when the players are ready to be retreived
            findPlayer.playerListBox.DataContext = new List<Player>(); //clear the eventListBox
                    findPlayer.playerListBox.DataContext = players;
                    if (players.Count == 0)
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
        }

        internal static void loginToUDJBefore(User currentUsr)
        {

            currentUser = currentUsr;
            var client = new RestClient("https://udjplayer.com:4897/udj/0_6");
            var request = new RestRequest("auth", Method.POST);

            request.AddParameter("username", currentUser.username);
            request.AddParameter("password", currentUser.password);

           executeNetworkCall(client, request, currentUser, loginToUDJAfter);
           
        }

       
     

        static void threadWait(RestClient client, RestRequest request, EventWaitHandle Wait, Action functor) 
        {
            Wait.WaitOne();
            Deployment.Current.Dispatcher.BeginInvoke(() => functor.Invoke());


            
        }

        static void executeNetworkCall(RestSharp.RestClient client, RestSharp.RestRequest request, User currentUser, Action functor)
        {
            EventWaitHandle Wait = new AutoResetEvent(false);
            Thread execRun = new Thread(() => threadWait(client, request, Wait, functor));
            execRun.Start();
            IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;

            

            client.ExecuteAsync<TypeData>(request, response =>
            {
                statusCode = response.StatusCode.ToString();  //stores the Status of the request


                if (statusCode == "OK")  //if everything went okay
                {

                    data = response.Data;

                }
                else
                {
                    data = default(TypeData);
                    
                    if (statusCode == "0")
                    {

                    }

                    else if (statusCode == "NotFound")
                    {
                        MessageBox.Show("It would be in your best interests to try again. Check your wifis!");

                    }

                    else if (statusCode == "BadRequest")
                    {
                        MessageBox.Show("Did you forget to type something? Please try again!");

                    }

                    else if (statusCode == "Unauthorized")
                    {
                        MessageBox.Show("Either that's not your username or that's not your password. Please try again!");
                        Deployment.Current.Dispatcher.BeginInvoke(() => (Application.Current.RootVisual as PhoneApplicationFrame).Navigate(new Uri("/MainPage.xaml", UriKind.RelativeOrAbsolute)));
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
