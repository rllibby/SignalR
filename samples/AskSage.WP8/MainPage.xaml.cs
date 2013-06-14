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
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Client.Hubs;
using Windows.Phone.Speech.Recognition;

namespace AskSage
{
    public partial class MainPage : PhoneApplicationPage
    {
        private HubConnection _Connection;
        private IHubProxy _Hub;
        private bool _Connected = false;


        // Constructor
        public MainPage()
        {
            InitializeComponent();
            DataContext = App.ViewModel;
        }

        private void ShowProgress(bool show)
        {
            Dispatcher.BeginInvoke(() =>
            {
                Visibility state = show ? Visibility.Visible : Visibility.Collapsed;

                this.Progress.Visibility = state;

                if (state == Visibility.Visible)
                {
                    this.Request.IsEnabled = false;
                    this.Speech.IsEnabled = false;
                }
                else
                {
                    this.Request.IsEnabled = true;
                    this.Speech.IsEnabled = true;
                    this.Request.Focus();
                }
            }
            );
        }

        private void ReportChange(StateChange change)
        {
            if (change.NewState == ConnectionState.Connected)
            {
                _Connected = true;
                ShowProgress(false);
            }
            else if (change.OldState == ConnectionState.Connected)
            {
                _Connected = false;
                ShowProgress(true);
            }
        }

        private void ReportError(Exception error)
        {
            ShowProgress(true);
        }

        private void OnResponse(string data)
        {
            if (data != null)
            {
                Dispatcher.BeginInvoke(() =>
                {
                    App.ViewModel.Items.Add(new ItemsModel(string.Format("Response: {0}", data)));
                    ShowProgress(false);
                }
                );
            }
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            ShowProgress(true);

            _Connection = new HubConnection("http://swmsignalrsite.azurewebsites.net/");

            _Connection.StateChanged += change => ReportChange(change);
            _Connection.Error += ex => { ReportError(ex); };

            _Hub = _Connection.CreateHubProxy("erpTicker");
            _Hub.On<string>("addResponse", data => OnResponse(data));

            _Connection.Start();
        }

        private async void Speech_Click(object sender, RoutedEventArgs e)
        {
            SpeechRecognizerUI recoWithUI = new SpeechRecognizerUI();

            SpeechRecognitionUIResult recoResult = await recoWithUI.RecognizeWithUIAsync();

            if (recoResult.RecognitionResult != null)
            {
                this.Request.Text = recoResult.RecognitionResult.Text;
            }

            this.Request.Focus();
        }

        private void Request_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if ((_Connected) && (!string.IsNullOrEmpty(Request.Text)))
                {
                    ShowProgress(true);

                    App.ViewModel.Items.Add(new ItemsModel(string.Format("Question: {0}", Request.Text)));

                    _Hub.Invoke("sendRequest", new object[] { _Connection.ConnectionId, this.Request.Text });
                }
            }
        }
    }

}