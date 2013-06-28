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
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Media.Animation;
using Windows.ApplicationModel.Resources;
using System.Threading.Tasks;

namespace AskSage.WinRT
{
    public enum ActionType
    {
        None,
        Call,
        Map
    }

    public class ItemsModel : INotifyPropertyChanged
    {
        private ActionType _Type;
        private bool _User;
        private string _Text;
        private string _InputTime;
        private string _Action;

        public float Lerp(float start, float end, float amount)
        {
            float difference = end - start;
            float adjusted = difference * amount;
            return start + adjusted;
        }

        public Color Lerp(Color colour, Color to, float amount)
        {
            // start colours as lerp-able floats
            float sr = colour.R, sg = colour.G, sb = colour.B;

            // end colours as lerp-able floats
            float er = to.R, eg = to.G, eb = to.B;

            // lerp the colours to get the difference
            byte r = (byte)Lerp(sr, er, amount),
                 g = (byte)Lerp(sg, eg, amount),
                 b = (byte)Lerp(sb, eb, amount);

            // return the new colour
            return Color.FromArgb(255, r, g, b);
        }

        public ItemsModel(bool user, string text, ActionType type, string action)
        {
            _Type = type;
            _Action = action;
            _User = user;
            _Text = text;
            _InputTime = DateTime.Now.ToString("ddd") + " " + DateTime.Now.ToString("t").ToLower();
        }

        public Visibility ActionVisible
        {
            get
            {
                return Visibility.Collapsed;
                //return (_Type != ActionType.None) ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public ActionType Type
        {
            get
            {
                return _Type;
            }
        }

        public string Action
        {
            get
            {
                return _Action;
            }
        }

        public string ActionText
        {
            get
            {
                switch (_Type)
                {
                    case ActionType.Call:
                        return "Double tap to call";

                    case ActionType.Map:
                        return "Double tap to map";
                }

                return string.Empty;
            }
        }

        public string InputTime
        {
            get
            {
                return _InputTime;
            }
        }

        public string Text
        {
            get
            {
                return _Text;
            }
        }

        public Brush BackColor
        {
            get
            {
                Color c = Color.FromArgb(0xff, 0x5F, 0x37, 0xBE);

                return _User ? new SolidColorBrush(c) : new SolidColorBrush(Lerp(c, Colors.Black, 0.25f));
            }
        }

        public HorizontalAlignment Alignment
        {
            get
            {
                return _User ? HorizontalAlignment.Right : HorizontalAlignment.Left;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (null != handler)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }

    public class MainViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<ItemsModel> _Items;

        public MainViewModel()
        {
            _Items = new ObservableCollection<ItemsModel>();
        }

        public ObservableCollection<ItemsModel> Items
        {
            get
            {
                return _Items;
            }
        }

        public bool IsDataLoaded
        {
            get;
            private set;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (null != handler)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }

    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        private static MainViewModel viewModel = null;

        public static List<string> GrammerList = new List<string>();

        /// <summary>
        /// A static ViewModel used by the views to bind against.
        /// </summary>
        /// <returns>The MainViewModel object.</returns>
        public static MainViewModel ViewModel
        {
            get
            {
                // Delay creation of the view model until necessary
                if (viewModel == null)
                {
                    viewModel = new MainViewModel();
                }

                return viewModel;
            }
        }

        private async void LoadGrammer()
        {
            string line;

            var uri = new Uri("ms-appx:///Assets/Grammer.txt");

            var file = StorageFile.GetFileFromApplicationUriAsync(uri).AsTask();
            await file;
            
            var data = file.Result.OpenAsync(FileAccessMode.Read).AsTask();
            await data;

            using (var reader = new StreamReader(data.Result.AsStreamForRead()))
            {
                // Read the lines
                using (StringReader sr = new StringReader(reader.ReadToEnd()))
                {
                    // While not eof
                    while ((line = sr.ReadLine()) != null)
                    {
                        // Check line
                        if (!string.IsNullOrEmpty(line))
                        {
                            // Add line to grammer list
                            GrammerList.Add(line);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;

            LoadGrammer();
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used when the application is launched to open a specific file, to display
        /// search results, and so forth.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                if (args.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if (rootFrame.Content == null)
            {
                // When the navigation stack isn't restored navigate to the first page,
                // configuring the new page by passing required information as a navigation
                // parameter
                if (!rootFrame.Navigate(typeof(MainPage), args.Arguments))
                {
                    throw new Exception("Failed to create initial page");
                }
            }
            // Ensure the current window is active
            Window.Current.Activate();
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Save application state and stop any background activity
            deferral.Complete();
        }
    }
}
