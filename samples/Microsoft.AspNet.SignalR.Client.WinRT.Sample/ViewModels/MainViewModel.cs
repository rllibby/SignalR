using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace Microsoft.AspNet.SignalR.Client.WinRT.Sample.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public MainViewModel()
        {
            this.SalesItems = new ObservableCollection<ErpKpiViewModel>();
            this.CashFlowItems = new ObservableCollection<ErpKpiViewModel>();
            this.ExpenseItems = new ObservableCollection<ErpKpiViewModel>();
        }

        public ObservableCollection<ErpKpiViewModel> SalesItems { get; private set; }
        public ObservableCollection<ErpKpiViewModel> CashFlowItems { get; private set; }
        public ObservableCollection<ErpKpiViewModel> ExpenseItems { get; private set; }

        public Brush SalesBrush
        {
            get
            {
                return Utility.GetChannelBrush("Sales");
            }
        }

        public Brush CashFlowBrush
        {
            get
            {
                return Utility.GetChannelBrush("CashFlow");
            }
        }

        public Brush ExpenseBrush
        {
            get
            {
                return Utility.GetChannelBrush("Expense");
            }
        }

        public ErpKpiViewModel UpdateKPI(ErpKpi kpi)
        {
            ObservableCollection<ErpKpiViewModel> list = null;
            ErpKpiViewModel viewKpi = null;

            if (kpi == null)
            {
                return null;
            }

            if (kpi.Channel.Equals("Sales"))
            {
               list = SalesItems; 
            }
            else if (kpi.Channel.Equals("CashFlow"))
            {
                list = CashFlowItems; 
            }
            else if (kpi.Channel.Equals("Expense"))
            {
                list = ExpenseItems; 
            }

            if (list != null)
            {
                foreach (ErpKpiViewModel item in list)
                {
                    if (item.Type.Equals(kpi.Type))
                    {
                        item.Total = kpi.Total.ToString("C");
                        item.NumberOf = kpi.NumberOf.ToString();
                        item.Last = kpi.Last.ToString("C");
                        item.Largest = kpi.Largest.ToString("C");
                        item.Smallest = kpi.Smallest.ToString("C");
                        item.Average = kpi.Average.ToString("C");

                        return item;
                    }
                }

                viewKpi = new ErpKpiViewModel(kpi);

                list.Add(viewKpi);
            }

            return viewKpi;
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
}