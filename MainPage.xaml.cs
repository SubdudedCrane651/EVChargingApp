using Microcharts;
using Microsoft.Maui.Controls;
using SkiaSharp;
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
            var entries = new List<ChartEntry>();

            foreach (var entry in evData)
            {
                entries.Add(new ChartEntry((float)entry.Cost)
                {
                    Label = entry.Date,
                    ValueLabel = entry.Cost.ToString(),
                    Color = SKColor.Parse("#00FF00") // Green
                });
            }

            chartView.Chart = new BarChart
            {
                Entries = entries,
                BackgroundColor = SKColors.Transparent
            };
        }

        private void GenerateEVvsGasChart()
        {
            var entries = new List<ChartEntry>();

            foreach (var entry in evData)
            {
                double gasCost = (entry.Km / 10) * 1.50;
                entries.Add(new ChartEntry((float)entry.Cost)
                {
                    Label = entry.Date,
                    ValueLabel = entry.Cost.ToString(),
                    Color = SKColor.Parse("#00FF00") // EV Green
                });
                entries.Add(new ChartEntry((float)gasCost)
                {
                    Label = entry.Date,
                    ValueLabel = gasCost.ToString(),
                    Color = SKColor.Parse("#FF0000") // Gas Red
                });
            }

            chartView.Chart = new BarChart
            {
                Entries = entries,
                BackgroundColor = SKColors.Transparent
            };
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