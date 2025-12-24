using System;
using System.Collections.Generic;
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
    /// <summary>
    /// Логика взаимодействия для Greenhouse_Window.xaml
    /// </summary>
    public partial class Greenhouse_Window : Window
    {
        private GreenhouseFacade _facade;
        private string _currentUserRole;

        public Greenhouse_Window(string currentUserRole)
        {
            InitializeComponent();
            _facade = new GreenhouseFacade();
            _currentUserRole = currentUserRole;
            LoadGreenhouses();
        }

        private void LoadGreenhouses()
        {
            try
            {
                var greenhouses = _facade.GetGreenhouses();
                GreenhousesList.ItemsSource = greenhouses;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки теплиц: {ex.Message}");
            }
        }

        private void GreenhousesList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (GreenhousesList.SelectedItem is Greenhouses selectedGreenhouse)
            {
                var greenhouseWithDetails = _facade.GetGreenhouseWithDetails(selectedGreenhouse.Id);
                if (greenhouseWithDetails != null)
                {
                    ShowGreenhouseDetails(greenhouseWithDetails);
                }
            }
        }

        private void ShowGreenhouseDetails(Greenhouses greenhouse)
        {
            DetailsPanel.Visibility = Visibility.Visible;
            NoSelectionPanel.Visibility = Visibility.Collapsed;

            GreenhouseName.Text = greenhouse.Name;
            GreenhouseDescription.Text = greenhouse.Description ?? "Описание отсутствует";
            PurposeText.Text = GetGreenhousePurpose(greenhouse);

            var climateDataList = _facade.GetClimateData(greenhouse.Id, 1);
            var climateData = climateDataList.FirstOrDefault();

            if (climateData != null)
            {
                TemperatureText.Text = $"{climateData.Temperature}°C";
                HumidityText.Text = $"{climateData.Humidity}%";
                LightText.Text = $"{climateData.Light} лк";
                LastUpdateText.Text = $"Обновлено: {climateData.Timestamp:dd.MM.yyyy HH:mm}";
            }
            else
            {
                TemperatureText.Text = "Нет данных";
                HumidityText.Text = "Нет данных";
                LightText.Text = "Нет данных";
                LastUpdateText.Text = "Данные отсутствуют";
            }

            var zonesData = _facade.GetPlantingZones(greenhouse.Id);

            var zones = zonesData.Select(z => new
            {
                ZoneName = z.ZoneName,
                CropName = z.Crops?.Name,
                Status = z.Status,
                Area = z.Area
            }).ToList();

            TotalZonesText.Text = zones.Count.ToString();
            TotalAreaText.Text = $"{zones.Sum(z => z.Area)} м²";
            ZonesList.ItemsSource = zones;
        }

        private string GetGreenhousePurpose(Greenhouses greenhouse)
        {
            if (greenhouse.Description?.ToLower().Contains("роз") == true)
                return "Выращивание роз и декоративных цветов. Оптимальные условия для цветочных культур.";
            else if (greenhouse.Description?.ToLower().Contains("тюльпан") == true)
                return "Специализированная теплица для выращивания тюльпанов и луковичных растений.";
            else if (greenhouse.Description?.ToLower().Contains("орхиде") == true)
                return "Теплица для экзотических растений, специализация - орхидеи и тропические цветы.";
            else
                return greenhouse.Description ?? "Универсальная теплица для выращивания различных культур.";
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = new MainWindow(_currentUserRole);
            mainWindow.Show();
            this.Close();
        }

        private void DeleteGreenhouseButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentUserRole != "Admin" && _currentUserRole != "Agronomist")
            {
                MessageBox.Show("У вас нет прав для удаления теплиц");
                return;
            }

            if (GreenhousesList.SelectedItem is Greenhouses selectedGreenhouse)
            {
                var result = MessageBox.Show($"Удалить теплицу '{selectedGreenhouse.Name}'?", "Подтверждение удаления",
                    MessageBoxButton.YesNo);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        _facade.DeleteGreenhouse(selectedGreenhouse.Id);
                        LoadGreenhouses();
                        MessageBox.Show("Теплица удалена");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка удаления: {ex.Message}");
                    }
                }
            }
            else
            {
                MessageBox.Show("Выберите теплицу для удаления");
            }
        }

        private void AddGreenhouseButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentUserRole != "Admin" && _currentUserRole != "Agronomist" && _currentUserRole != "Technologist")
            {
                MessageBox.Show("У вас нет прав для добавления теплиц");
                return;
            }

            var addWindow = new CreateGreenhouseWindow();
            if (addWindow.ShowDialog() == true)
            {
                LoadGreenhouses();
            }
        }

        private void EditGreenhouseButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentUserRole != "Admin" && _currentUserRole != "Agronomist" && _currentUserRole != "Technologist")
            {
                MessageBox.Show("У вас нет прав для редактирования теплиц");
                return;
            }

            if (GreenhousesList.SelectedItem is Greenhouses selectedGreenhouse)
            {
                var editWindow = new EditGreenhouseWindow(selectedGreenhouse.Id);
                if (editWindow.ShowDialog() == true)
                {
                    LoadGreenhouses();
                    if (GreenhousesList.SelectedItem is Greenhouses updatedGreenhouse &&
                        updatedGreenhouse.Id == selectedGreenhouse.Id)
                    {
                        var greenhouseWithDetails = _facade.GetGreenhouseWithDetails(updatedGreenhouse.Id);
                        if (greenhouseWithDetails != null)
                        {
                            ShowGreenhouseDetails(greenhouseWithDetails);
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show("Выберите теплицу для редактирования");
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _facade?.Dispose();
            base.OnClosed(e);
        }
    }
}