using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Text;
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
using Windows.Phone.Speech;
using Windows.Phone.Speech.Recognition;
using Windows.Phone.Speech.Synthesis;
using Windows.Phone.Speech.VoiceCommands;
using System.Windows.Navigation;
using ShakeGestures;

namespace AskSage
{
    public class ActionTag
    {
        public bool Parsed { get; set; }
        public string Text { get; set; }
        public string Action { get; set; }

        public ActionTag()
        {
            Parsed = false;
            Text = string.Empty;
            Action = string.Empty;
        }
    }

    public partial class MainPage : PhoneApplicationPage
    {
        private SpeechRecognizerUI _Recognizer;
        private SpeechRecognizerUI _DemoRecognizer;
        private SpeechSynthesizer _Synthesizer;
        private HubConnection _Connection;
        private IHubProxy _Hub;
        private string _preSeed = String.Empty;
        private int _Selected = (-1);
        private bool _Connected = false;
        private bool _Welcome = false;
        private bool _Waiting = false;
        private bool _Speech = true;
        private bool _Demo = true;

        /* Constructor for page */
        public MainPage()
        {
            // Initialize the component model
            InitializeComponent();

            // Set the data context for the page bindings
            DataContext = App.ViewModel;

            // Create speech recognizer 
            _Recognizer = new SpeechRecognizerUI();

            // Bind up shake gesture
            ShakeGesturesHelper.Instance.ShakeGesture += new EventHandler<ShakeGestureEventArgs>(Instance_ShakeGesture);
            ShakeGesturesHelper.Instance.MinimumRequiredMovesForShake = 4;
            ShakeGesturesHelper.Instance.Active = true;

            // Create demo recognizer and set grammer
            _DemoRecognizer = new SpeechRecognizerUI();
            _DemoRecognizer.Recognizer.Grammars.AddGrammarFromList("Demo", App.GrammerList);

            // Create speech synthesizer
            _Synthesizer = new SpeechSynthesizer();

            // Create signalr connection
            _Connection = new HubConnection("http://sagevoice.azurewebsites.net/");
            _Connection.StateChanged += change => ReportChange(change);

            // Create hub proxy
            _Hub = _Connection.CreateHubProxy("erpTicker");
            _Hub.On<string>("addResponse", data => OnResponse(data));
        }

        // Set the data context of the TextBlock to the answer.
        void Instance_ShakeGesture(object sender, ShakeGestureEventArgs e)
        {
            try
            {
                // Cancel any speech in progress
                _Synthesizer.CancelAll();
            }
            catch
            {
                // Eat it
            }
        }

        /// <summary>
        /// Installs the voice command definition file for Ask Sage.
        /// </summary>
        private async void InstallVoiceCommands()
        {
            var uri = new Uri("ms-appx:///sagevcd.xml", UriKind.Absolute);

            try
            {
                await VoiceCommandService.InstallCommandSetsFromFileAsync(uri);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
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

                    if (_Connected && !String.IsNullOrEmpty(_preSeed))
                    {
                        _Hub.Invoke("sendRequest", new object[] { _Connection.ConnectionId, _preSeed });
                        _preSeed = String.Empty;
                    }

                    // Show or hide the progress bar
                    progress.Visibility = (_Connected && !_Waiting) ? Visibility.Collapsed : Visibility.Visible;

                    // Enable or disable the text box
                    request.IsEnabled = (_Connected && !_Waiting);
                }
            );
        }

        private ActionTag ParseTag(string text, string tag)
        {
            ActionTag result = new ActionTag();
            
            int len = tag.Length + 2;
            int start, end;

            start = text.ToLower().IndexOf(string.Format("<{0}>", tag.ToLower()));

            if (start >= 0)
            {
                end = text.ToLower().IndexOf(string.Format("</{0}>", tag.ToLower()));
                if (end > start)
                {
                    result.Action = text.Substring(start + len, (end - start) - len);
                    result.Text = text.Remove(start, (end - start) + (len + 1));
                    result.Parsed = true;
                }
            }

            return result;
        }

        /* Add text to the conversation thread */
        private void AddConversationText(bool user, string text)
        {
            ItemsModel item = null;
            ActionTag tag;
            string readText = string.Empty;

            if (user == false)
            {
                tag = ParseTag(text, "phone");
                if (tag.Parsed)
                {
                    item = new ItemsModel(user, tag.Text, ActionType.Call, tag.Action);
                }
                else
                {
                    tag = ParseTag(text, "address");
                    if (tag.Parsed)
                    {
                        item = new ItemsModel(user, tag.Text, ActionType.Map, tag.Action);
                    }
                }
            }

            if (item == null)
            {
                // Create new item model
                item = new ItemsModel(user, text, ActionType.None, string.Empty);
            }

            readText = item.Text;

            // Ensure this happens on the UI thread
            Dispatcher.BeginInvoke(() =>
                {
                    // Add to the view model
                    App.ViewModel.Items.Add(item);

                    // Make sure its scrolled into view
                    conversation.ScrollIntoView(item);

                    // Should it be read aloud?
                    if (!user && _Speech)
                    {
                        try
                        {
                            // Start the speech read back
                            _Synthesizer.SpeakTextAsync(readText);
                        }
                        catch (Exception)
                        {
                            // Eat it
                        }
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

        /// <summary>
        /// Handles the voice command name
        /// </summary>
        /// <param name="voiceCommandName"></param>
        private void HandleVoiceCommand(string voiceCommandName)
        {
            MessageBox.Show(NavigationContext.ToString());
        }

        /* Navigation to the page */
        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            // Start the connection
            _Connection.Start();

            if (e.NavigationMode == NavigationMode.New)
            {
                var voiceCommandName = String.Empty;

                if (NavigationContext.QueryString.TryGetValue("voiceCommandName", out voiceCommandName))
                {
                    _Welcome = true;

                    if (NavigationContext.QueryString.TryGetValue("reco", out voiceCommandName))
                    {
                        if (!string.IsNullOrEmpty(voiceCommandName))
                        {
                            _preSeed = voiceCommandName.TrimStart(new[] { '.', ',', ' ' });
                            
                            // Add the text
                            AddConversationText(true, _preSeed);

                            if (_Connected)
                            {
                                _Hub.Invoke("sendRequest", new object[] { _Connection.ConnectionId, _preSeed });
                                _preSeed = String.Empty;
                            }
                        }
                        // Set focus to the list box
                        conversation.Focus();
                    }
                }
                else
                {
                    Task.Run(() => InstallVoiceCommands());
                }
            }
   
            UpdateState();

            base.OnNavigatedTo(e);
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

            // Clear selected
            _Selected = (-1);
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

            // If not speech
            if (!_Speech)
            {
                try
                {
                    // Cancel any speech in progress
                    _Synthesizer.CancelAll();
                }
                catch
                {
                    // Eat it
                }
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

        /* Don't allow selection */
        private void conversation_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Save selection
            if (conversation.SelectedIndex >= 0)
            {
                _Selected = conversation.SelectedIndex;
            }

            // Clear selected item
            conversation.SelectedIndex = (-1);
        }

        /* Handle action based on item */
        private void Grid_DoubleTap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            // Check selected item
            if ((_Selected >= 0) && (_Selected < App.ViewModel.Items.Count))
            {
                // Get the item
                ItemsModel item = App.ViewModel.Items[_Selected];
                // Check the action type
                if (item.Type != ActionType.None)
                {
                    // Check for call
                    if (item.Type == ActionType.Call)
                    {
                        // Phone call
                        PhoneCallTask callTask = new PhoneCallTask();
                        callTask.PhoneNumber = item.Action;
                        callTask.Show();
                    }
                    else if (item.Type == ActionType.Map)
                    {
                        // Map the address
                        MapsTask mapTask = new MapsTask();
                        mapTask.ZoomLevel = 10;
                        mapTask.SearchTerm = item.Action;
                        mapTask.Show();
                    }
                }
            }
        }
    }
}