using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Greenhose
{
    public partial class Plants_WIndow : Window
    {
        private Greenhouse_AtenaEntities _context;
        private List<Crops> _allCrops;

        public Plants_WIndow()
        {
            InitializeComponent();
            _context = new Greenhouse_AtenaEntities();
            LoadPlants();
        }

        private void LoadPlants()
        {
            try
            {
                _allCrops = _context.Crops.ToList();
                PlantsListBox.ItemsSource = _allCrops;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки: {ex.Message}");
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var searchText = SearchTextBox.Text.ToLower();

            if (string.IsNullOrWhiteSpace(searchText))
            {
                PlantsListBox.ItemsSource = _allCrops;
            }
            else
            {
                PlantsListBox.ItemsSource = _allCrops
                    .Where(c => c.Name.ToLower().Contains(searchText))
                    .ToList();
            }
        }

        private void PlantsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedCrop = PlantsListBox.SelectedItem as Crops;

            if (selectedCrop == null) return;

            PlantNameText.Text = selectedCrop.Name;
            SortText.Text = selectedCrop.Type_Crops?.name_sort ?? "Не указан";
            GrowthDaysText.Text = $"{selectedCrop.GrowthDays} дней";
            TemperatureText.Text = $"{selectedCrop.OptimalTemperature}°C";
            HumidityText.Text = $"{selectedCrop.OptimalHumidity}%";
            StatusText.Text = "Активная";
            HarvestDateText.Text = DateTime.Now.AddDays(selectedCrop.GrowthDays).ToString("dd.MM.yyyy");

            HealthIndicator.Fill = new SolidColorBrush(Colors.Green);
            HealthStatusText.Text = "Хорошее состояние";

            UpdateHealthStatus(selectedCrop);

            LoadGreenhousesForCrop(selectedCrop.Id);
        }

        private void LoadGreenhousesForCrop(int cropId)
        {
            try
            {
                var plantingZones = _context.PlantingZones
                    .Where(pz => pz.CropId == cropId)
                    .Include(pz => pz.Greenhouses)
                    .Select(pz => new
                    {
                        GreenhouseName = pz.Greenhouses.Name,
                        ZoneName = pz.ZoneName,
                        Area = pz.Area,
                        ExpectedHarvestDate = pz.ExpectedHarvestDate
                    })
                    .ToList();

                GreenhousesList.ItemsSource = plantingZones;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки теплиц: {ex.Message}");
                GreenhousesList.ItemsSource = null;
            }
        }

        private void UpdateHealthStatus(Crops crop)
        {
            var problemZones = crop.PlantingZones?.Count(pz => pz.Status == "Problem") ?? 0;
            var totalZones = crop.PlantingZones?.Count ?? 0;

            if (totalZones == 0)
            {
                HealthIndicator.Fill = new SolidColorBrush(Colors.Gray);
                HealthStatusText.Text = "Нет данных";
            }
            else if (problemZones > totalZones * 0.5)
            {
                HealthIndicator.Fill = new SolidColorBrush(Colors.Red);
                HealthStatusText.Text = "Критическое состояние";
            }
            else if (problemZones > 0)
            {
                HealthIndicator.Fill = new SolidColorBrush(Colors.Orange);
                HealthStatusText.Text = "Требует внимания";
            }
            else
            {
                HealthIndicator.Fill = new SolidColorBrush(Colors.Green);
                HealthStatusText.Text = "Хорошее состояние";
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close();
        }
    }
}