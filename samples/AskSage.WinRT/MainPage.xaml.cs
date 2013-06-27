using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Media.Animation;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Client.Hubs;
using Windows.ApplicationModel.Resources;
using System.Threading.Tasks;

namespace AskSage.WinRT
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        internal class UiDispatcher
        {
            private MainPage _Page;
            private string _Text = string.Empty;
            private bool _User = false;

            public UiDispatcher(MainPage page, bool user, string text)
            {
                _Page = page;
                _Text = text;
                _User = user;
            }

            public UiDispatcher(MainPage page)
            {
                _Page = page;
            }

            public void UpdateState()
            {
                // Show or hide the progress bar
                _Page.progress.Visibility = (_Page._Connected && !_Page._Waiting) ? Visibility.Collapsed : Visibility.Visible;

                // Enable or disable the text box
                _Page.request.IsEnabled = (_Page._Connected && !_Page._Waiting);
            }

            public void AddConversationText()
            {
                // Create new item model
                ItemsModel item = new ItemsModel(_User, _Text);

                // Add to the view model
                App.ViewModel.Items.Add(item);

                // Make sure its scrolled into view
                _Page.conversation.ScrollIntoView(item);
            }
                
        }

        private HubConnection _Connection;
        private IHubProxy _Hub;
        private bool _Connected = false;
        private bool _Welcome = false;
        private bool _Waiting = false;
        private bool _Speech = true;
        private bool _Demo = true;

        public MainPage()
        {
            this.InitializeComponent();

            // Set the data context for the page bindings
            DataContext = App.ViewModel;
        }

        private async void ReportChange(StateChange change)
        {
            UiDispatcher disp = new UiDispatcher(this, false, "How can I assist you?");

            // Set connected state
            _Connected = (change.NewState == ConnectionState.Connected);

            // If connected and not welcomed yet
            if (_Connected && !_Welcome)
            {
                // Set state
                _Welcome = true;
                // Add text
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, new DispatchedHandler(disp.AddConversationText));
            }

            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, new DispatchedHandler(disp.UpdateState));
        }

        /* Handle response text from system */
        private async void OnResponse(string data)
        {
            UiDispatcher state = new UiDispatcher(this);

            // Check data
            if (data != null)
            {
                UiDispatcher disp = new UiDispatcher(this, false, data);
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, new DispatchedHandler(disp.AddConversationText));
            }

            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, new DispatchedHandler(state.UpdateState));
        }

        private void ReportClosed()
        {
            _Connection.Start();
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.  The Parameter
        /// property is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            UiDispatcher state = new UiDispatcher(this);

            // Update the UI state
            state.UpdateState();

            // Create connection
            _Connection = new HubConnection("http://swmsignalrsite.azurewebsites.net/");
            _Connection.StateChanged += change => ReportChange(change);
            _Connection.Closed += () => ReportClosed();

            // Create hub proxy
            _Hub = _Connection.CreateHubProxy("erpTicker");
            _Hub.On<string>("addResponse", data => OnResponse(data));

            // Start the connection
            _Connection.Start();
        }

        private void request_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            // Check for enter key
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                // Set the key to handled
                e.Handled = true;
                // Fire the event handler
                ask_Click(sender, e);
            }
        }

        private void ask_Click(object sender, RoutedEventArgs e)
        {
            UiDispatcher disp = new UiDispatcher(this, true, request.Text);

            // Check state with SignalR hub
            if (_Connected && !_Waiting)
            {
                // If text
                if (!string.IsNullOrEmpty(request.Text))
                {
                    // Add the text
                    disp.AddConversationText();

                    // Invoke the request
                    _Hub.Invoke("sendRequest", new object[] { _Connection.ConnectionId, request.Text });

                    // Clear the text
                    request.Text = string.Empty;
                }
            }
        }

        private void clear_Click(object sender, RoutedEventArgs e)
        {
            // Clear the items
            App.ViewModel.Items.Clear();
        }

        private void conversation_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            conversation.SelectedIndex = (-1);
        }
    }
}
