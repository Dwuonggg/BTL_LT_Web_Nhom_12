
using System;
using System.Collections.Generic;

namespace BTL_LT_Web_Nhom_12.Areas.Admin.ViewModels
{
    public class DashboardViewModel
    {
        public string Period { get; set; } = "today";
        public string PeriodLabel { get; set; } = "Hôm nay";

        public decimal Revenue { get; set; }

        public int TotalOrders { get; set; }
        public int NewCustomers { get; set; }

        public int PendingOrders { get; set; }
        public int PreparingOrders { get; set; }
        public int DeliveringOrders { get; set; }
        public int DeliveredOrders { get; set; }
        public int CancelledOrders { get; set; }

        public List<string> ChartLabels { get; set; } = new();
        public List<decimal> ChartValues { get; set; } = new();
    }
}
