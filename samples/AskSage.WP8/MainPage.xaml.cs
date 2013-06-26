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
using Windows.Phone.Speech.Synthesis;

namespace AskSage
{
    public partial class MainPage : PhoneApplicationPage
    {
        private SpeechRecognizerUI _Recognizer;
        private SpeechRecognizerUI _DemoRecognizer;
        private SpeechSynthesizer _Synthesizer;
        private HubConnection _Connection;
        private IHubProxy _Hub;
        private bool _Connected = false;
        private bool _Welcome = false;
        private bool _Waiting = false;
        private bool _Speech = true;
        private bool _Demo = false;

        /* Constructor for page */
        public MainPage()
        {
            // Initialize the component model
            InitializeComponent();

            // Set the data context for the page bindings
            DataContext = App.ViewModel;

            // Create speech recognizer 
            _Recognizer = new SpeechRecognizerUI();

            // Create demo recognizer and set grammer
            _DemoRecognizer = new SpeechRecognizerUI();
            _DemoRecognizer.Recognizer.Grammars.AddGrammarFromList("Demo", App.GrammerList);

            // Create speech synthesizer
            _Synthesizer = new SpeechSynthesizer();
        }

        /* Update the UI state based on connection and question status */
        private void UpdateState()
        {
            // Perform on the UI thread
            Dispatcher.BeginInvoke(() =>
                {
                    // Handle buttons 
                    foreach (ApplicationBarIconButton button in ApplicationBar.Buttons)
                    {
                        button.IsEnabled = (_Connected && !_Waiting);
                    }

                    // Show or hide the progress bar
                    progress.Visibility = (_Connected && !_Waiting) ? Visibility.Collapsed : Visibility.Visible;

                    // Enable or disable the text box
                    request.IsEnabled = (_Connected && !_Waiting);
                }
            );
        }

        /* Add text to the conversation thread */
        private void AddConversationText(bool user, string text)
        {
            // Ensure this happens on the UI thread
            Dispatcher.BeginInvoke(() =>
                {
                    // Create new item model
                    ItemsModel item = new ItemsModel(user, text);

                    // Add to the view model
                    App.ViewModel.Items.Add(item);

                    // Make sure its scrolled into view
                    conversation.ScrollIntoView(item);

                    // Should it be read aloud?
                    if (!user && _Speech)
                    {
                        // Read it
                        _Synthesizer.SpeakTextAsync(text).AsTask().Wait();
                    }
                }
            );
        }

        /* Handle the connection state change */
        private void ReportChange(StateChange change)
        {
            // Set connected state
            _Connected = (change.NewState == ConnectionState.Connected);
    
            // If connected and not welcomed yet
            if (_Connected && !_Welcome)
            {
                // Set state
                _Welcome = true;

                // Add the welcome message
                AddConversationText(false, "How can I assist you?");
            }

            // Update the UI state
            UpdateState();
        }

        /* Handle response text from system */
        private void OnResponse(string data)
        {
            // Check data
            if (data != null)
            {
                // Add response
                AddConversationText(false, data);
            }

            // Update the UI state
            UpdateState();
        }

        /* Navigation to the page */
        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            // Base
            base.OnNavigatedTo(e);

            // Update the UI state
            UpdateState();

            // Create connection
            _Connection = new HubConnection("http://swmsignalrsite.azurewebsites.net/");
            _Connection.StateChanged += change => ReportChange(change);

            // Create hub proxy
            _Hub = _Connection.CreateHubProxy("erpTicker");
            _Hub.On<string>("addResponse", data => OnResponse(data));

            // Start the connection
            _Connection.Start();
        }

        /* Handle enter key the same as clicking the ask button */
        private void Request_KeyDown(object sender, KeyEventArgs e)
        {
            // Check for enter key
            if (e.Key == Key.Enter)
            {
                // Set the key to handled
                e.Handled = true;
                // Fire the event handler
                ask_Click(sender, e);
            }
        }

        /* Clear the conversation history */
        private void clear_Click(object sender, EventArgs e)
        {
            // Clear the items
            App.ViewModel.Items.Clear();
        }

        /* Get speech input form the user */
        private async void speak_Click(object sender, EventArgs e)
        {
            SpeechRecognitionUIResult recoResult = await (_Demo ? _DemoRecognizer.RecognizeWithUIAsync() : _Recognizer.RecognizeWithUIAsync());

            // Check result
            if (recoResult.RecognitionResult != null)
            {
                // Set text input field
                request.Text = recoResult.RecognitionResult.Text;
            }
        }
        
        /* Ask the question */
        private void ask_Click(object sender, EventArgs e)
        {
            // Check state with SignalR hub
            if (_Connected && !_Waiting)
            {
                // If text
                if (!string.IsNullOrEmpty(request.Text))
                {
                    // Add the text
                    AddConversationText(true, request.Text);

                    // Invoke the request
                    _Hub.Invoke("sendRequest", new object[] { _Connection.ConnectionId, request.Text });
                    
                    // Clear the text
                    request.Text = string.Empty;
                }
                // Set focus to the list box
                conversation.Focus();
            }
        }

        /* Toggle the speech */
        private void speech_Click(object sender, EventArgs e)
        {
            // Toggle state
            _Speech = !_Speech;

            // Check application bar
            if (ApplicationBar.MenuItems.Count > 1)
            {
                // Change the text based on current state
                (ApplicationBar.MenuItems[1] as ApplicationBarMenuItem).Text = _Speech ? "don't read aloud" : "read aloud";
            }
        }

        /* Toggle the demo mode */
        private void demo_Click(object sender, EventArgs e)
        {
            // Toggle state
            _Demo = !_Demo;

            // Check application bar
            if (ApplicationBar.MenuItems.Count > 2)
            {
                // Change the text based on current state
                (ApplicationBar.MenuItems[2] as ApplicationBarMenuItem).Text = _Demo ? "demo on" : "demo off";
            }
        }
    }
}