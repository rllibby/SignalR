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
                                    DoubleAnimation xa = new DoubleAnimation();
                                    Storyboard sb = new Storyboard();

                                    xa.Duration = new Duration(TimeSpan.FromSeconds(1.0));
                                    xa.From = 0.5;
                                    xa.To = 1;

                                    sb.Duration = new Duration(TimeSpan.FromSeconds(1.0));
                                    sb.Children.Add(xa);

                                    Storyboard.SetTarget(sb, item);
                                    Storyboard.SetTargetProperty(sb, "Opacity");

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
            }
        }

        private HubConnection _Connection;
        private IHubProxy _Hub;
        private bool _Connected = false;
        private bool _Loaded = false;

        public MainPage()
        {
            this.InitializeComponent();
            DataContext = App.ViewModel;
        }
 
        private async void ShowProgress(bool show)
        {
            UiDispatcher disp = new UiDispatcher(this);
            disp.Show = show;

            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, new DispatchedHandler(disp.OnShowDispatched));
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

            _Connection = new HubConnection("http://swmsignalrsite.azurewebsites.net/");

            _Connection.StateChanged += change => ReportChange(change);
            _Connection.Error += ex => { ReportError(ex); };
            _Connection.Closed += () => ReportClosed();

            _Hub = _Connection.CreateHubProxy("erpTicker");
            _Hub.On<ErpKpi>("updateSalesKPI", data => OnUpdateKPI(data));
            _Hub.On<ErpKpi>("updateCashFlowKPI", data => OnUpdateKPI(data));
            _Hub.On<ErpKpi>("updateExpenseKPI", data => OnUpdateKPI(data));
            _Hub.On<ErpKpi>("addSalesKPI", data => OnUpdateKPI(data));
            _Hub.On<ErpKpi>("addCashFlowKPI", data => OnUpdateKPI(data));
            _Hub.On<ErpKpi>("addExpenseKPI", data => OnUpdateKPI(data));
            _Hub.On("reset", () => OnReset());
            
            _Connection.Start();
        }
       
    }
}
