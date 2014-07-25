using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Microsoft.AspNet.SignalR.Client.Hubs;
using Microsoft.AspNet.SignalR.Client.WinRT.Sample.ViewModels;
using Windows.UI.Xaml.Media.Animation;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.Client.WinRT.Sample
{
    public class AlertWrapper
    {
        private List<string> _List;
        private TextBlock _Control;
        private int _Index = 0;
        private int _NewAlert = (-1);
        private bool _Running = false;

        private void FadeIn()
        {
            if (_Running)
            {
                DoubleAnimation fadeIn = new DoubleAnimation();

                fadeIn.From = 0.0;
                fadeIn.To = 1;
                fadeIn.Duration = new Duration(TimeSpan.FromSeconds(2));
                fadeIn.BeginTime = TimeSpan.FromSeconds(2);

                Storyboard sb = new Storyboard();

                Storyboard.SetTarget(fadeIn, _Control);
                Storyboard.SetTargetProperty(fadeIn, "Opacity");

                sb.Children.Add(fadeIn);

                _Control.Resources.Clear();
                _Control.Resources.Add("FaderEffect", sb);

                sb.Completed += this.OnFadeInCompleted;

                sb.Begin();
            }
        }

        private void OnFadeInCompleted(object sender, object e)
        {
            DoubleAnimation fadeOut = new DoubleAnimation();

            fadeOut.From = 1;
            fadeOut.To = 0.0;
            fadeOut.Duration = new Duration(TimeSpan.FromSeconds(2));
            fadeOut.BeginTime = TimeSpan.FromSeconds(2);

            Storyboard sb = new Storyboard();

            Storyboard.SetTarget(fadeOut, _Control);
            Storyboard.SetTargetProperty(fadeOut, "Opacity");

            sb.Children.Add(fadeOut);

            _Control.Resources.Clear();
            _Control.Resources.Add("FaderEffect", sb);

            sb.Completed += OnFadeOutCompleted;

            sb.Begin();
        }

        private void OnFadeOutCompleted(object sender, object e)
        {
            if (_NewAlert >= 0)
            {
                _Index = _NewAlert;
                _NewAlert = (-1);
            }
            else
            {
                _Index++;
            }

            if (_Index >= _List.Count)
            {
                _Index = 0;

                if (_List.Count == 0)
                {
                    _Index = (-1);
                }
            }

            _Control.Text = (_Index >= 0) ? _List[_Index] : string.Empty;

            if (_Running)
            {
                FadeIn();
            }
        }

        public AlertWrapper(TextBlock control)
        {
            _Control = control;
            _Running = false;
            _List = new List<string>();
            _NewAlert = (-1);
            _Index = 0;
        }

        public void Start()
        {
            if (_Running == false)
            {
                _Running = true;
                FadeIn();
            }
        }

        public void Stop()
        {
            _Running = false;
        }

        public void AddAlert(string textAlert)
        {
            if (_List.Count >= 10)
            {
                _List.RemoveAt(0);
            }

            _List.Add(textAlert);

            if (_NewAlert == (-1))
            {
                _NewAlert = _List.Count - 1;
            }
        }
    }

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        internal class UiDispatcher
        {
            private MainPage _Page;

            public IEnumerable<ErpKpi> List;
            public ErpKpi Item;
            public string Alert;
            public bool Show;

            public UiDispatcher(MainPage page)
            {
                _Page = page;
            }

            public void ClearList()
            {
                App.ViewModel.SalesItems.Clear();
                App.ViewModel.CashFlowItems.Clear();
                App.ViewModel.ExpenseItems.Clear();
            }

            public void OnLoadList()
            {
                if (List != null)
                {
                    foreach (ErpKpi kpi in List)
                    {
                        App.ViewModel.UpdateKPI(kpi);
                    }
                }
            }

            public void OnAddAlert()
            {
                _Page._Alerts.AddAlert(Alert);
            }

            public void OnUpdateItem()
            {
                ErpKpiViewModel viewKpi = App.ViewModel.UpdateKPI(Item);

                if (viewKpi != null)
                {
                    ListBox list = null;

                    if (viewKpi.Channel.Equals("Sales"))
                    {
                        list = _Page.SalesList;
                    }
                    else if (viewKpi.Channel.Equals("CashFlow"))
                    {
                        list = _Page.CashFlowList;
                    }
                    else if (viewKpi.Channel.Equals("Expense"))
                    {
                        list = _Page.ExpenseList;
                    }

                    if (list != null)
                    {
                        for (int i = 0; i < list.Items.Count; i++)
                        {
                            ListBoxItem item = (ListBoxItem)(list.ItemContainerGenerator.ContainerFromIndex(i));

                            if ((item.DataContext != null) && (item.DataContext is ErpKpiViewModel))
                            {
                                if (viewKpi.Type.Equals(((ErpKpiViewModel)item.DataContext).Type))
                                {
                                    Storyboard sb = new Storyboard();

                                    DoubleAnimation animation = new DoubleAnimation()
                                    {
                                        Duration = new TimeSpan(0, 0, 1)
                                    };

                                    sb.Children.Add(animation);

                                    if (item.Projection == null)
                                    {
                                        item.Projection = new PlaneProjection()
                                        {
                                            CenterOfRotationX = 0.5
                                        };
                                    }

                                    PlaneProjection projection = item.Projection as PlaneProjection;

                                    animation.From = 0;
                                    animation.To = 360;

                                    Storyboard.SetTarget(animation, projection);
                                    Storyboard.SetTargetProperty(animation, "RotationX");

                                    sb.Begin();
                                }
                            }
                        }
                    }
                }
            }

            public void OnShowDispatched()
            {
                _Page.Progress.Visibility = Show ? Visibility.Visible : Visibility.Collapsed;
                _Page.AlertText.Visibility = Show ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        private HubConnection _Connection;
        private AlertWrapper _Alerts;
        private IHubProxy _Hub;
        private bool _Connected = false;
        private bool _Loaded = false;

        public MainPage()
        {
            this.InitializeComponent();
            DataContext = App.ViewModel;

            _Alerts = new AlertWrapper(AlertText);
            _Alerts.Start();
        }
 
        private async void ShowProgress(bool show)
        {
            UiDispatcher disp = new UiDispatcher(this);
            disp.Show = show;

            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, new DispatchedHandler(disp.OnShowDispatched));
        }

        private async void OnAlertUpdate(string channel, string data)
        {
            UiDispatcher disp = new UiDispatcher(this);
            disp.Alert = data;

            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, new DispatchedHandler(disp.OnAddAlert));
        }

        private async void OnUpdateKPI(ErpKpi kpi)
        {
            UiDispatcher disp = new UiDispatcher(this);
            disp.Item = kpi;

            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, new DispatchedHandler(disp.OnUpdateItem));
        }

        private async void OnReset()
        {
            UiDispatcher disp = new UiDispatcher(this);

            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, new DispatchedHandler(disp.ClearList));

            GetKpiList("Sales");
            GetKpiList("CashFlow");
            GetKpiList("Expense");
        }

        private void GetKpiList(string channel)
        {
            if (_Connected)
            {
                ShowProgress(true);

                Task <IEnumerable<ErpKpi>> task = _Hub.Invoke<IEnumerable<ErpKpi>>("GetAllKPIs", new object[] { channel });
                task.Wait();

                UiDispatcher disp = new UiDispatcher(this);
                disp.List = task.Result;

                Dispatcher.RunAsync(CoreDispatcherPriority.Normal, new DispatchedHandler(disp.OnLoadList)).AsTask().Wait();

                ShowProgress(false);
            }
        }

        private void ReportChange(StateChange change)
        {
            if (change.NewState == ConnectionState.Connected)
            {
                _Connected = true;

                if (!_Loaded)
                {
                    _Loaded = true;

                    GetKpiList("Sales");
                    GetKpiList("CashFlow");
                    GetKpiList("Expense");
                }

                ShowProgress(false);
            }
            else if (change.OldState == ConnectionState.Connected)
            {
                _Connected = false;

                ShowProgress(true);
            }
        }

        private void ReportClosed()
        {
            _Connection.Start();
        }

        private void ReportError(Exception error)
        {
            ShowProgress(true);
        } 

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.  The Parameter
        /// property is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            _Connection = new HubConnection("http://sagevoice.azurewebsites.net/");

            _Connection.StateChanged += change => ReportChange(change);
            _Connection.Error += ex => { ReportError(ex); };
            _Connection.Closed += () => ReportClosed();

            _Hub = _Connection.CreateHubProxy("erpTicker");
            _Hub.On<string, string>("addTickerItem", (channel, data) => OnAlertUpdate(channel, data));
            _Hub.On<ErpKpi>("updateSalesKPI", data => OnUpdateKPI(data));
            _Hub.On<ErpKpi>("updateCashFlowKPI", data => OnUpdateKPI(data));
            _Hub.On<ErpKpi>("updateExpenseKPI", data => OnUpdateKPI(data));
            _Hub.On<ErpKpi>("addSalesKPI", data => OnUpdateKPI(data));
            _Hub.On<ErpKpi>("addCashFlowKPI", data => OnUpdateKPI(data));
            _Hub.On<ErpKpi>("addExpenseKPI", data => OnUpdateKPI(data));
            _Hub.On("reset", () => OnReset());
            
            _Connection.Start();
        }

        private void SalesList_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            ErpKpi kpi = new ErpKpi();
            kpi.Channel = "Sales";
            kpi.Type = "Orders";
            kpi.Total = 1234.45M;

            OnUpdateKPI(kpi);
            OnAlertUpdate("Sales", "This is a test of some long text to display in the alert banner.");
        }
    }
}
