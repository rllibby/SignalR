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
    public static class Utility
    {
        public static Brush GetChannelBrush(string channel)
        {
            if (string.Equals(channel, "Sales"))
            {
                return new SolidColorBrush(Color.FromArgb(0xff, 0x02, 0x47, 0x31));
            }
            else if (string.Equals(channel, "CashFlow"))
            {
                return new SolidColorBrush(Color.FromArgb(0xff, 0x69, 0x92, 0x3a));
            }
            else if (string.Equals(channel, "Expense"))
            {
                return new SolidColorBrush(Color.FromArgb(0xff, 0xa8, 0xb4, 0x00));
            }

            return new SolidColorBrush(Color.FromArgb(0xff, 0x00, 0x00, 0x00));
        }
    }

    public class ErpKpi
    {
        public string Type { get; set; }
        public string Channel { get; set; }
        public decimal Total { get; set; }
        public int NumberOf { get; set; }
        public decimal Last { get; set; }
        public decimal Largest { get; set; }
        public decimal Smallest { get; set; }
        public decimal Average { get; set; }
    }

    public class ChannelViewModel : INotifyPropertyChanged
    {
        private string _Name;

        public ChannelViewModel(string name)
        {
            _Name = name;
        }

        public string Name
        {
            get
            {
                return string.IsNullOrEmpty(_Name) ? "" : _Name;
            }
            set
            {
                if (value != _Name)
                {
                    _Name = value;
                    NotifyPropertyChanged("Name");
                }
            }
        }

        public string Description
        {
            get
            {
                return string.Format("{0} Channel", Name);
            }
        }

        public Brush ChannelBrush
        {
            get
            {
                return Utility.GetChannelBrush(_Name);
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

    public class ErpKpiViewModel : INotifyPropertyChanged
    {
        private string _Type;
        private string _Channel;
        private string _Total;
        private string _NumberOf;
        private string _Last;
        private string _Largest;
        private string _Smallest;
        private string _Average;

        public ErpKpiViewModel(ErpKpi kpi)
        {
            if (kpi != null)
            {
                _Type = kpi.Type;
                _Channel = kpi.Channel;
                _Total = kpi.Total.ToString("C");
                _NumberOf = kpi.NumberOf.ToString();
                _Last = kpi.Last.ToString("C");
                _Largest = kpi.Largest.ToString("C");
                _Smallest = kpi.Smallest.ToString("C");
                _Average = kpi.Average.ToString("C");
            }
        }

        public string Type
        {
            get
            {
                return _Type;
            }
            set
            {
                if (value != _Type)
                {
                    _Type = value;
                    NotifyPropertyChanged("Type");
                }
            }
        }

        public Brush ChannelBrush
        {
            get
            {
                return Utility.GetChannelBrush(_Channel);
            }
        }

        public string Channel
        {
            get
            {
                return _Channel;
            }
            set
            {
                if (value != _Channel)
                {
                    _Channel = value;
                    NotifyPropertyChanged("Channel");
                    NotifyPropertyChanged("ChannelBrush");
                }
            }
        }

        public string Total
        {
            get
            {
                return "Total: " + _Total;
            }
            set
            {
                if (value != _Total)
                {
                    _Total = value;
                    NotifyPropertyChanged("Total");
                }
            }
        }

        public string NumberOf
        {
            get
            {
                return "Number Of: " + _NumberOf;
            }
            set
            {
                if (value != _NumberOf)
                {
                    _NumberOf = value;
                    NotifyPropertyChanged("NumberOf");
                }
            }
        }

        public string Largest
        {
            get
            {
                return "Largest: " + _Largest;
            }
            set
            {
                if (value != _Largest)
                {
                    _Largest = value;
                    NotifyPropertyChanged("Highest");
                }
            }
        }

        public string Smallest
        {
            get
            {
                return "Smallest: " + _Smallest;
            }
            set
            {
                if (value != _Smallest)
                {
                    _Smallest = value;
                    NotifyPropertyChanged("Smallest");
                }
            }
        }

        public string Last
        {
            get
            {
                return "Last: " + _Last;
            }
            set
            {
                if (value != _Last)
                {
                    _Last = value;
                    NotifyPropertyChanged("Last");
                }
            }
        }

        public string Average
        {
            get
            {
                return "Average: " + _Average;
            }
            set
            {
                if (value != _Average)
                {
                    _Average = value;
                    NotifyPropertyChanged("Average");
                }
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
}