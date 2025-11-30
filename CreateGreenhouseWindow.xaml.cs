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
    /// Логика взаимодействия для CreateGreenhouseWindow.xaml
    /// </summary>
    public partial class CreateGreenhouseWindow : Window
    {
        private Greenhouse_AtenaEntities context;
        private List<PlantingZones> _zones;

        public CreateGreenhouseWindow()
        {
            InitializeComponent();
            context = new Greenhouse_AtenaEntities();
            _zones = new List<PlantingZones>();
            ZonesListView.ItemsSource = _zones;
            LoadCrops();
        }

        private void LoadCrops()
        {
            try
            {
                var crops = context.Crops.ToList();
                CropComboBox.ItemsSource = crops;
                CropComboBox.DisplayMemberPath = "Name";
                CropComboBox.SelectedValuePath = "Id";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки культур: {ex.Message}");
            }
        }

        private void AddZoneButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ZoneNameTextBox.Text))
            {
                MessageBox.Show("Введите название зоны");
                return;
            }

            if (CropComboBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите культуру");
                return;
            }

            if (!int.TryParse(AreaTextBox.Text, out int area) || area <= 0)
            {
                MessageBox.Show("Введите корректную площадь");
                return;
            }

            var zone = new PlantingZones
            {
                ZoneName = ZoneNameTextBox.Text,
                CropId = (int)CropComboBox.SelectedValue,
                Area = area,
                Status = (StatusComboBox.SelectedItem as ComboBoxItem)?.Content.ToString(),
                ExpectedHarvestDate = DateTime.Now.AddMonths(3)
            };

            _zones.Add(zone);
            ZonesListView.Items.Refresh();

            ZoneNameTextBox.Clear();
            AreaTextBox.Clear();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                MessageBox.Show("Введите название теплицы");
                return;
            }

            try
            {
                var newGreenhouse = new Greenhouses
                {
                    Name = NameTextBox.Text,
                    Description = DescriptionTextBox.Text
                };

                context.Greenhouses.Add(newGreenhouse);
                context.SaveChanges();

                if (!string.IsNullOrWhiteSpace(TemperatureTextBox.Text) &&
                    !string.IsNullOrWhiteSpace(HumidityTextBox.Text) &&
                    !string.IsNullOrWhiteSpace(LightTextBox.Text))
                {
                    if (int.TryParse(TemperatureTextBox.Text, out int temp) &&
                        decimal.TryParse(HumidityTextBox.Text, out decimal humidity) &&
                        decimal.TryParse(LightTextBox.Text, out decimal light))
                    {
                        var climateData = new ClimateData
                        {
                            GreenhouseId = newGreenhouse.Id,
                            Temperature = temp,
                            Humidity = humidity,
                            Light = light,
                            Timestamp = DateTime.Now
                        };
                        context.ClimateData.Add(climateData);
                    }
                }

                foreach (var zone in _zones)
                {
                    zone.GreenhouseId = newGreenhouse.Id;
                    context.PlantingZones.Add(zone);
                }

                context.SaveChanges();
                MessageBox.Show("Теплица и связанные данные добавлены успешно");
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}");
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}