using Microsoft.Maui.Controls;
using Syncfusion.Maui.Charts;
using System;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
            try
            {
                HttpClientHandler handler = new HttpClientHandler();
                handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
                using HttpClient client = new HttpClient(handler);
                //using HttpClient client = new HttpClient();
                var requestData = new { Command = "LIST" };
                var content = new StringContent(JsonSerializer.Serialize(requestData), Encoding.UTF8, "application/json");

                HttpResponseMessage response = await client.PostAsync("https://rj2shop.com:8003/ev_connections?Command=LIST", content);
                response.EnsureSuccessStatusCode();

                string responseData = await response.Content.ReadAsStringAsync();

                evData = JsonSerializer.Deserialize<List<EVData>>(responseData);

                ProcessYearlyData();

                DisplayReport();
            }
            catch (Exception ex)
            {
                //reportText.Text = $"Error fetching data: {ex.Message}";
                await DisplayAlert("Alert", ex.Message, "OK");
            }
        }

        private void ProcessYearlyData()
        {
            var years = evData.Select(entry => DateTime.Parse(entry.Date).Year)
                              .Distinct()
                              .OrderByDescending(y => y)
                              .ToList();

            // Populate the Picker with yearly options
            chartPicker.Items.Clear();
            foreach (var year in years)
            {
                chartPicker.Items.Add($"Monthly Costs {year}");
                chartPicker.Items.Add($"EV vs Gas Cost {year}");
            }

            // Set the default selection to the latest year
            chartPicker.SelectedIndex = 0;
        }

        private void DisplayReport()
        {
            // Sort data from oldest to newest
            //var sortedData = evData.OrderBy(entry => DateTime.Parse(entry.Date)).ToList();

            // Sort data from newest to oldest
            var sortedData = evData.OrderByDescending(entry => DateTime.Parse(entry.Date)).ToList();

            reportListView.ItemsSource = sortedData;

            // Calculate totals
            double totalCost = sortedData.Sum(entry => entry.Cost);
            double totalKm = sortedData.Sum(entry => entry.Km);
            double totalKwh = sortedData.Sum(entry => entry.Kwh);

            totalLabel.Text = $"Total Cost: ${totalCost:F2} | Total KM: {totalKm} | Total kWh: {totalKwh:F2}";
        }

        //private void ShowChartButton_Clicked(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        string selectedChart = chartPicker.SelectedItem.ToString();
        //        if (selectedChart == "Monthly Costs") GenerateMonthlyCostChart();
        //        else GenerateEVvsGasChart();
        //    }
        //    catch { }
        //}

        private void GenerateMonthlyCostChart(int year)
        {
            var monthlyData = evData.Where(entry => DateTime.Parse(entry.Date).Year == year)
                                    .GroupBy(entry => DateTime.Parse(entry.Date).ToString("MMMM"))
                                    .ToDictionary(g => g.Key, g => g.Sum(e => e.Cost));

            var sortedMonths = monthlyData.Keys.OrderBy(m => DateTime.ParseExact(m, "MMMM", System.Globalization.CultureInfo.InvariantCulture).Month).ToList();

            var series = new ColumnSeries
            {
                ItemsSource = sortedMonths.Select(month => new ChartData(month, monthlyData[month])).ToList(),
                XBindingPath = "Month",
                YBindingPath = "Cost",
                Fill = new SolidColorBrush(Colors.Green)
            };

            syncfusionChart.Series.Clear();
            syncfusionChart.Series.Add(series);
        }

        private void GenerateEVvsGasChart(int year)
        {
            const double gasCostPerLiter = 1.50;
            const double gasEfficiencyKmPerLiter = 10;

            var monthlyEVData = evData.Where(entry => DateTime.Parse(entry.Date).Year == year)
                                      .GroupBy(entry => DateTime.Parse(entry.Date).ToString("MMMM"))
                                      .ToDictionary(g => g.Key, g => g.Sum(e => e.Cost));

            var monthlyGasData = evData.Where(entry => DateTime.Parse(entry.Date).Year == year)
                                       .GroupBy(entry => DateTime.Parse(entry.Date).ToString("MMMM"))
                                       .ToDictionary(g => g.Key, g => g.Sum(e => (e.Km / gasEfficiencyKmPerLiter) * gasCostPerLiter));

            var sortedMonths = monthlyEVData.Keys.OrderBy(m => DateTime.ParseExact(m, "MMMM", System.Globalization.CultureInfo.InvariantCulture).Month).ToList();

            var evSeries = new ColumnSeries
            {
                ItemsSource = sortedMonths.Select(month => new ChartData(month, monthlyEVData[month])).ToList(),
                XBindingPath = "Month",
                YBindingPath = "Cost",
                Fill = new SolidColorBrush(Colors.Green)
            };

            var gasSeries = new ColumnSeries
            {
                ItemsSource = sortedMonths.Select(month => new ChartData(month, monthlyGasData[month])).ToList(),
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

        private void Picker_SelectionChanged(object sender, EventArgs e)
        {
            string selectedChart = chartPicker.SelectedItem.ToString();
            var match = System.Text.RegularExpressions.Regex.Match(selectedChart, @"\d{4}");

            if (match.Success)
            {
                int selectedYear = int.Parse(match.Value);

                // Calculate total cost, km, and kWh for the selected year
                var yearlyData = evData.Where(entry => DateTime.Parse(entry.Date).Year == selectedYear).ToList();
                double totalCost = yearlyData.Sum(entry => entry.Cost);
                double totalKm = yearlyData.Sum(entry => entry.Km);
                double totalKwh = yearlyData.Sum(entry => entry.Kwh);

                // Update the label to show totals for the selected year
                totalLabel_byyear.Text = $"Year: {selectedYear} | Total Cost: ${totalCost:F2} | Total KM: {totalKm} | Total kWh: {totalKwh:F2}";

                selectedChart = chartPicker.SelectedItem.ToString();
                match = System.Text.RegularExpressions.Regex.Match(selectedChart, @"\d{4}");

                if (match.Success)
                {
                    if (selectedChart.Contains("Monthly Costs"))
                        GenerateMonthlyCostChart(selectedYear);
                    else
                        GenerateEVvsGasChart(selectedYear);
                }
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