using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client.Hubs;

namespace Hub.Sample
{
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

    class Program
    {
        static void Main(string[] args)
        {
            HubConnection conn = new HubConnection("http://swmsignalrsite.azurewebsites.net/");
            IHubProxy proxy = conn.CreateHubProxy("erpTicker");
            int c;

            Console.WriteLine("Connection to server HUB....");

            conn.Start().Wait();

            Console.WriteLine("Press R to Reset the Hub");
            Console.WriteLine("Press U to Update a KPI");
            Console.WriteLine("Press A to Add a new KPI");

            while (true)
            {
                string s = Console.ReadLine();
                
                if ((s == "R") || (s == "r"))
                {
                    proxy.Invoke("Reset").Wait();
                }
                else if ((s == "A") || (s == "a"))
                {
                    ErpKpi k = new ErpKpi();
                    k.Channel = "Sales";
                    k.Type = Environment.TickCount.ToString();
                    k.Total = 1123.34M;
                    k.NumberOf = 1;

                    proxy.Invoke("AddKpi", new object[] {"Sales", k }).Wait();
                }
                else if ((s == "U") || (s == "u"))
                {
                    ErpKpi k = new ErpKpi();
                    k.Channel = "Sales";
                    k.Type = "Quotes";
                    k.Total = 1123.34M;
                    k.NumberOf = 4;

                    proxy.Invoke("AddKpi", new object[] { k }).Wait();
                }
                else if (s == "")
                {
                    break;
                }
            }

        }
    }
}
