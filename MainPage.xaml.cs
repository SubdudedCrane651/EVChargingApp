using Syncfusion.Maui.Charts;
using System.Collections.Generic;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;


namespace EVCharging
{
    public partial class MainPage : ContentPage
    {
        private List<EVData> evData;

        public MainPage()
        {
            InitializeComponent();
            FetchData();
        }

        public async Task FetchData()
        {
            try { 
            using HttpClient client = new HttpClient();
            var requestData = new { Command = "LIST" };
            var content = new StringContent(JsonSerializer.Serialize(requestData), Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PostAsync("https://rj2shop.com:8003/ev_connections?Command=LIST", content);
            response.EnsureSuccessStatusCode();

            string responseData = await response.Content.ReadAsStringAsync();

            evData = JsonSerializer.Deserialize<List<EVData>>(responseData);

                DisplayReport();
            }
            catch (Exception ex)
            {
                reportText.Text = $"Error fetching data: {ex.Message}";
            }
        }

        private void DisplayReport()
        {
            string report = "";
            double totalCost = 0, totalKm = 0, totalKwh = 0;

            foreach (var entry in evData)
            {
                report += $"Date: {entry.Date}\nHours Charged: {entry.Hours_Charged}\nCost: ${entry.Cost}\nKM Driven: {entry.Km}\nEnergy Used: {entry.Kwh} kWh\nLocation: {entry.Location}\n---\n";
                totalCost += entry.Cost;
                totalKm += entry.Km;
                totalKwh += entry.Kwh;
            }

            reportText.Text = report;
            totalLabel.Text = $"Total Cost: ${totalCost:F2} | Total KM: {totalKm} | Total kWh: {totalKwh:F2}";
        }

        private void ShowChartButton_Clicked(object sender, EventArgs e)
        {
            string selectedChart = chartPicker.SelectedItem.ToString();
            if (selectedChart == "Monthly Costs") GenerateMonthlyCostChart();
            else GenerateEVvsGasChart();
        }

        private void GenerateMonthlyCostChart()
        {
            var monthCosts = new Dictionary<string, double>();

            foreach (var entry in evData)
            {
                DateTime dateObj = DateTime.Parse(entry.Date);
                string month = dateObj.ToString("MMMM"); // Get month name

                if (monthCosts.ContainsKey(month))
                    monthCosts[month] += entry.Cost;
                else
                    monthCosts[month] = entry.Cost;
            }

            var sortedMonths = monthCosts.Keys.OrderBy(m => DateTime.ParseExact(m, "MMMM", System.Globalization.CultureInfo.InvariantCulture).Month).ToList();

            // Populate Syncfusion Chart
            var series = new ColumnSeries
            {
                ItemsSource = sortedMonths.Select(month => new ChartData(month, monthCosts[month])).ToList(),
                XBindingPath = "Month",
                YBindingPath = "Cost",
                Fill = new SolidColorBrush(Colors.Green)
            };

            syncfusionChart.Series.Clear();
            syncfusionChart.Series.Add(series);
        }

        private void GenerateEVvsGasChart()
        {
            const double gasCostPerLiter = 1.50;
            const double gasEfficiencyKmPerLiter = 10;

            var monthEvCosts = new Dictionary<string, double>();
            var monthGasCosts = new Dictionary<string, double>();

            foreach (var entry in evData)
            {
                DateTime dateObj = DateTime.Parse(entry.Date);
                string month = dateObj.ToString("MMMM");

                if (monthEvCosts.ContainsKey(month))
                    monthEvCosts[month] += entry.Cost;
                else
                    monthEvCosts[month] = entry.Cost;

                double gasCost = (entry.Km / gasEfficiencyKmPerLiter) * gasCostPerLiter;

                if (monthGasCosts.ContainsKey(month))
                    monthGasCosts[month] += gasCost;
                else
                    monthGasCosts[month] = gasCost;
            }

            var sortedMonths = monthEvCosts.Keys.OrderBy(m => DateTime.ParseExact(m, "MMMM", System.Globalization.CultureInfo.InvariantCulture).Month).ToList();

            // Create EV cost series (Green)
            var evSeries = new ColumnSeries
            {
                ItemsSource = sortedMonths.Select(month => new ChartData(month, monthEvCosts[month])).ToList(),
                XBindingPath = "Month",
                YBindingPath = "Cost",
                Fill = new SolidColorBrush(Colors.Green)
            };

            // Create Gas cost series (Red)
            var gasSeries = new ColumnSeries
            {
                ItemsSource = sortedMonths.Select(month => new ChartData(month, monthGasCosts[month])).ToList(),
                XBindingPath = "Month",
                YBindingPath = "Cost",
                Fill = new SolidColorBrush(Colors.Red)
            };

            syncfusionChart.Series.Clear();
            syncfusionChart.Series.Add(evSeries);
            syncfusionChart.Series.Add(gasSeries);
        }

        public class ChartData
        {
            public string Month { get; set; }
            public double Cost { get; set; }

            public ChartData(string month, double cost)
            {
                Month = month;
                Cost = cost;
            }
        }

        private class EVData
        {
            public string Date { get; set; }
            public double Hours_Charged { get; set; }
            public double Cost { get; set; }
            public double Km { get; set; }
            public double Kwh { get; set; }
            public string Location { get; set; }
        }
    }
}