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
using Microsoft.AspNet.SignalR.Client.Hubs;
using Microsoft.AspNet.SignalR.Client.WP8.Sample.ViewModels;

namespace Microsoft.AspNet.SignalR.Client.WP8.Sample
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
                Storyboard.SetTargetProperty(fadeIn, new PropertyPath("(Opacity)"));

                sb.Children.Add(fadeIn);

                _Control.Resources.Clear();
                _Control.Resources.Add("FaderEffect", sb);

                sb.Completed += this.OnFadeInCompleted;

                sb.Begin();
            }
        }

        private void OnFadeInCompleted(object sender, EventArgs e)
        {
            DoubleAnimation fadeOut = new DoubleAnimation();

            fadeOut.From = 1;
            fadeOut.To = 0.0;
            fadeOut.Duration = new Duration(TimeSpan.FromSeconds(2));
            fadeOut.BeginTime = TimeSpan.FromSeconds(2);

            Storyboard sb = new Storyboard();

            Storyboard.SetTarget(fadeOut, _Control);
            Storyboard.SetTargetProperty(fadeOut, new PropertyPath("(Opacity)"));

            sb.Children.Add(fadeOut);

            _Control.Resources.Clear();
            _Control.Resources.Add("FaderEffect", sb);

            sb.Completed += OnFadeOutCompleted;

            sb.Begin();
        }

        private void OnFadeOutCompleted(object sender, EventArgs e)
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

             _Control.Text =  (_Index >= 0) ? _List[_Index] : string.Empty;

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

    public partial class MainPage : PhoneApplicationPage
    {
        private ListBox _ActiveControl = null;
        private HubConnection _Connection;
        private IHubProxy _Hub;
        private AlertWrapper _Alerts;
        private bool _Connected = false;

        public MainPage()
        {
            InitializeComponent();

            DataContext = App.ViewModel;

            _ActiveControl = SalesChannel;
            _Alerts = new AlertWrapper(AlertText);

            _Alerts.Start();
        }

        private void ClearKpiList()
        {
            Dispatcher.BeginInvoke(() =>
            {
                App.ViewModel.SalesItems.Clear();
            }
            );
        }

        private void ShowProgress(bool show)
        {
            Dispatcher.BeginInvoke(() =>
            {
                this.Progress.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            }
            );
        }

        private void StartAnimation(ListBoxItem element)
        {
            Storyboard sb = new Storyboard();

            DoubleAnimation animation = new DoubleAnimation()
            {
                Duration = new TimeSpan(0, 0, 1)
            };

            sb.Children.Add(animation);

            if (element.Projection == null)
            {
                element.Projection = new PlaneProjection()
                {
                    CenterOfRotationX = 0.5
                };
            }

            PlaneProjection projection = element.Projection as PlaneProjection;

            animation.From = 0;
            animation.To = 360;

            Storyboard.SetTarget(animation, projection);
            Storyboard.SetTargetProperty(animation, new PropertyPath(PlaneProjection.RotationXProperty));

            sb.Begin();
        }

        private void OnAlertUpdate(string channel, string data)
        {
            Dispatcher.BeginInvoke(() =>
            {
                this._Alerts.AddAlert(data);
            }
            );
        }

        private void OnUpdateKPI(ErpKpi kpi)
        {
            if (kpi != null)
            {
                Dispatcher.BeginInvoke(() =>
                {
                    ErpKpiViewModel viewKpi = App.ViewModel.UpdateKPI(kpi);

                    if ((viewKpi != null) && (_ActiveControl != null))
                    {
                        for (int i = 0; i < _ActiveControl.Items.Count; i++)
                        {
                            ListBoxItem item = (ListBoxItem)(_ActiveControl.ItemContainerGenerator.ContainerFromIndex(i));

                            if ((item.DataContext != null) && (item.DataContext is ErpKpiViewModel))
                            {
                                if (viewKpi.Type.Equals(((ErpKpiViewModel)item.DataContext).Type))
                                {
                                    StartAnimation(item);
                                }
                            }
                        }
                    }
                }
                );
            }
        }

        private void OnReset()
        {
            Dispatcher.BeginInvoke(() =>
            {
                App.ViewModel.SalesItems.Clear();
                App.ViewModel.CashFlowItems.Clear();
                App.ViewModel.ExpenseItems.Clear();

                ShowProgress(true);

                GetKpiList("Sales");
                GetKpiList("CashFlow");
                GetKpiList("Expense");

                ShowProgress(false);
            }
            );
        }

        private void GetKpiList(string channel)
        {
            if (_Connected)
            {
                Task<IEnumerable<ErpKpi>> task = _Hub.Invoke<IEnumerable<ErpKpi>>("GetAllKPIs", new object[] { channel });
                task.Wait();

                var list = task.Result;

                Dispatcher.BeginInvoke(() =>
                    {
                        foreach (ErpKpi kpi in list)
                        {
                            App.ViewModel.UpdateKPI(kpi);
                        }
                    }
                    );
            }
        }

        private void ReportChange(StateChange change)
        {
            if (change.NewState == ConnectionState.Connected)
            {
                _Connected = true;

                ShowProgress(true);

                GetKpiList("Sales");
                GetKpiList("CashFlow");
                GetKpiList("Expense");

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

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            _Connection = new HubConnection("http://swmsignalrsite.azurewebsites.net/");

            _Connection.StateChanged += change => ReportChange(change);
            _Connection.Error += ex => { ReportError(ex); };

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

        private void SalesChannel_DoubleTap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            ErpKpi k = new ErpKpi();
            k.Channel = "Sales";
            k.Type = "Orders";
            k.Total = 1345.23M;

            OnUpdateKPI(k);

            OnAlertUpdate("Sales", "Test to check the line output available for alerts.");
        }

        private void Panorama_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Panorama.SelectedIndex == 0)
            {
                _ActiveControl = SalesChannel;
            }
            else if (Panorama.SelectedIndex == 1)
            {
                _ActiveControl = CashFlowChannel;
            }
            else
            {
                _ActiveControl = ExpenseChannel;
            }
        }
    }
}