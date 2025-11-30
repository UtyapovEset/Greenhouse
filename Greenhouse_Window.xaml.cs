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
        private Greenhouse_AtenaEntities context;
        private string _currentUserRole;


        public Greenhouse_Window(string currentUserRole )
        {
            InitializeComponent();
            context = new Greenhouse_AtenaEntities();
            LoadGreenhouses();
            _currentUserRole = currentUserRole;
        }

        private void LoadGreenhouses()
        {
            try
            {
                var greenhouses = context.Greenhouses.ToList();
                GreenhousesList.ItemsSource = greenhouses;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки теплиц: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GreenhousesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GreenhousesList.SelectedItem is Greenhouses selectedGreenhouse)
            {
                ShowGreenhouseDetails(selectedGreenhouse);
            }
        }

        private void ShowGreenhouseDetails(Greenhouses greenhouse)
        {
            DetailsPanel.Visibility = Visibility.Visible;
            NoSelectionPanel.Visibility = Visibility.Collapsed;

            GreenhouseName.Text = greenhouse.Name;
            GreenhouseDescription.Text = greenhouse.Description ?? "Описание отсутствует";
            PurposeText.Text = GetGreenhousePurpose(greenhouse);

            var climateData = context.ClimateData
                .AsNoTracking()
                .Where(c => c.GreenhouseId == greenhouse.Id)
                .OrderByDescending(c => c.Timestamp)
                .FirstOrDefault();

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

            var zones = context.PlantingZones
                .AsNoTracking()
                .Where(z => z.GreenhouseId == greenhouse.Id)
                .Join(context.Crops,
                      zone => zone.CropId,
                      crop => crop.Id,
                      (zone, crop) => new
                      {
                          ZoneName = zone.ZoneName,
                          CropName = crop.Name,
                          Status = zone.Status,
                          Area = zone.Area
                      })
                .ToList();

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
                MessageBox.Show("У вас нет прав для удаления теплиц", "Ошибка доступа",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (GreenhousesList.SelectedItem is Greenhouses selectedGreenhouse)
            {
                var result = MessageBox.Show($"Удалить теплицу '{selectedGreenhouse.Name}'?", "Подтверждение удаления",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        context.Greenhouses.Remove(selectedGreenhouse);
                        context.SaveChanges();
                        LoadGreenhouses();
                        MessageBox.Show("Теплица удалена", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Выберите теплицу для удаления", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void AddGreenhouseButton_Click(object sender, RoutedEventArgs e)
        {
            if(_currentUserRole != "Admin" && _currentUserRole != "Agronomist" && _currentUserRole != "Technologist")
            {
                MessageBox.Show("У вас нет прав для добавления теплиц", "Ошибка доступа",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var addWindow = new CreateGreenhouseWindow();
            if (addWindow.ShowDialog() == true)
            {
                LoadGreenhouses();
            }
        }
    }
}