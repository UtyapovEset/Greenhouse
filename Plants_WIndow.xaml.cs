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
        private GreenhouseFacade _facade;
        private List<Crops> _allCrops;
        private string _currentUserRole;

        public Plants_WIndow(string userRole)
        {
            InitializeComponent();
            _facade = new GreenhouseFacade();
            _currentUserRole = userRole;
            LoadPlants();

            AddButton.Visibility = Visibility.Visible;
            DeleteButton.Visibility = Visibility.Visible;
        }

        private void LoadPlants()
        {
            try
            {
                _allCrops = _facade.GetCrops();
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

            UpdateHealthStatus(selectedCrop);
            LoadGreenhousesForCrop(selectedCrop.Id);
            UpdateHarvestDatesForCrop(selectedCrop);
        }

        private void LoadGreenhousesForCrop(int cropId)
        {
            try
            {
                var cropWithDetails = _facade.GetCropWithDetails(cropId);
                if (cropWithDetails != null && cropWithDetails.PlantingZones != null)
                {
                    var plantingZones = cropWithDetails.PlantingZones
                        .Select(pz => new
                        {
                            GreenhouseName = pz.Greenhouses?.Name,
                            ZoneName = pz.ZoneName,
                            Area = pz.Area,
                            ExpectedHarvestDate = pz.ExpectedHarvestDate
                        })
                        .ToList();

                    GreenhousesList.ItemsSource = plantingZones;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки теплиц: {ex.Message}");
                GreenhousesList.ItemsSource = null;
            }
        }

        private void UpdateHealthStatus(Crops crop)
        {
            try
            {
                var cropWithDetails = _facade.GetCropWithDetails(crop.Id);
                if (cropWithDetails == null || cropWithDetails.PlantingZones == null)
                {
                    HealthIndicator.Fill = new SolidColorBrush(Colors.Gray);
                    HealthStatusText.Text = "Нет данных";
                    return;
                }

                var plantingZones = cropWithDetails.PlantingZones.ToList();

                if (!plantingZones.Any())
                {
                    HealthIndicator.Fill = new SolidColorBrush(Colors.Gray);
                    HealthStatusText.Text = "Нет данных";
                    return;
                }

                int problemZones = 0;
                int totalZones = plantingZones.Count;

                foreach (var zone in plantingZones)
                {
                    var climateData = _facade.GetClimateData(zone.GreenhouseId, 1);
                    var latestClimate = climateData.FirstOrDefault();

                    if (latestClimate == null)
                    {
                        problemZones++;
                        continue;
                    }

                    double tempDiff = Math.Abs(latestClimate.Temperature - crop.OptimalTemperature);
                    bool tempProblem = tempDiff > 3;

                    double humidityDiff = Math.Abs((double)(latestClimate.Humidity - crop.OptimalHumidity));
                    bool humidityProblem = humidityDiff > 10;

                    bool statusProblem = zone.Status == "На обслуживании" || zone.Status == "Проблема";

                    if (tempProblem || humidityProblem || statusProblem)
                    {
                        problemZones++;
                    }
                }

                if (problemZones == totalZones)
                {
                    HealthIndicator.Fill = new SolidColorBrush(Colors.Red);
                    HealthStatusText.Text = "Критическое состояние";
                }
                else if (problemZones > totalZones * 0.5)
                {
                    HealthIndicator.Fill = new SolidColorBrush(Colors.OrangeRed);
                    HealthStatusText.Text = "Плохое состояние";
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

                if (problemZones > 0)
                {
                    HealthStatusText.Text += $" ({problemZones}/{totalZones} проблемных зон)";
                }
            }
            catch (Exception ex)
            {
                HealthIndicator.Fill = new SolidColorBrush(Colors.Gray);
                HealthStatusText.Text = "Ошибка данных";
                Console.WriteLine($"Ошибка в UpdateHealthStatus: {ex.Message}");
            }
        }


        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentUserRole != "Admin" && _currentUserRole != "Agronomist" && _currentUserRole != "Technologist")
            {
                MessageBox.Show("У вас нет прав для добавления культур", "Ошибка доступа",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var addWindow = new CreatePlantsWindow();
            if (addWindow.ShowDialog() == true)
            {
                LoadPlants();
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentUserRole != "Admin" && _currentUserRole != "Agronomist")
            {
                MessageBox.Show("У вас нет прав для удаления культур");
                return;
            }

            if (PlantsListBox.SelectedItem is Crops selectedCrop)
            {
                var result = MessageBox.Show($"Удалить культуру '{selectedCrop.Name}'?", "Подтверждение удаления", MessageBoxButton.YesNo);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        _facade.DeleteCrop(selectedCrop.Id);
                        LoadPlants();
                        MessageBox.Show("Культура удалена");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка удаления: {ex.Message}");
                    }
                }
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = new MainWindow(_currentUserRole);
            mainWindow.Show();
            this.Close();
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentUserRole != "Admin" && _currentUserRole != "Agronomist" && _currentUserRole != "Technologist")
            {
                MessageBox.Show("У вас нет прав для редактирования культур", "Ошибка доступа",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (PlantsListBox.SelectedItem is Crops selectedCrop)
            {
                var editWindow = new EditPlantsWindow(selectedCrop);
                if (editWindow.ShowDialog() == true)
                {
                    LoadPlants();
                }
            }
            else
            {
                MessageBox.Show("Выберите культуру для редактирования", "Внимание",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void UpdateHarvestDatesForCrop(Crops crop)
        {
            try
            {
                var cropWithDetails = _facade.GetCropWithDetails(crop.Id);
                if (cropWithDetails == null || cropWithDetails.PlantingZones == null)
                    return;

                var plantingZones = cropWithDetails.PlantingZones.ToList();

                if (!plantingZones.Any())
                    return;

                foreach (var zone in plantingZones)
                {
                    zone.ExpectedHarvestDate = DateTime.Today.AddDays(crop.GrowthDays);
                    _facade.UpdatePlantingZone(zone);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка обновления даты сбора: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _facade?.Dispose();
            base.OnClosed(e);
        }
    }
}