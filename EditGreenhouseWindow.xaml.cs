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
    /// <summary>
    /// Логика взаимодействия для EditGreenhouseWindow.xaml
    /// </summary>
    public partial class EditGreenhouseWindow : Window
    {
        private GreenhouseFacade _facade;
        private int _greenhouseId;
        private List<PlantingZones> _zones;

        public EditGreenhouseWindow(int id)
        {
            InitializeComponent();
            _facade = new GreenhouseFacade();
            _greenhouseId = id;
            _zones = new List<PlantingZones>();
            ZonesListView.ItemsSource = _zones;
            LoadCrops();
            LoadGreenhouseData();
        }

        private void LoadGreenhouseData()
        {
            try
            {
                var greenhouse = _facade.GetGreenhouseWithDetails(_greenhouseId);
                if (greenhouse != null)
                {
                    NameTextBox.Text = greenhouse.Name;
                    DescriptionTextBox.Text = greenhouse.Description;

                    var existingZones = greenhouse.PlantingZones.ToList();

                    _zones.AddRange(existingZones);
                    ZonesListView.Items.Refresh();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}");
            }
        }

        private void LoadCrops()
        {
            try
            {
                using (var context = new Greenhouse_AtenaEntities())
                {
                    var crops = context.Crops.ToList();
                    CropComboBox.ItemsSource = crops;
                    CropComboBox.DisplayMemberPath = "Name";
                    CropComboBox.SelectedValuePath = "Id";
                }
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

            var selectedStatus = StatusComboBox.SelectedValue?.ToString();
            if (string.IsNullOrEmpty(selectedStatus))
            {
                MessageBox.Show("Выберите статус зоны");
                return;
            }

            var zone = new PlantingZones
            {
                ZoneName = ZoneNameTextBox.Text,
                CropId = (int)CropComboBox.SelectedValue,
                Area = area,
                Status = selectedStatus,
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
                var greenhouse = new Greenhouses
                {
                    Id = _greenhouseId,
                    Name = NameTextBox.Text,
                    Description = DescriptionTextBox.Text
                };

                _facade.UpdateGreenhouse(greenhouse);

                bool hasTemperature = !string.IsNullOrWhiteSpace(TemperatureTextBox.Text);
                bool hasHumidity = !string.IsNullOrWhiteSpace(HumidityTextBox.Text);
                bool hasLight = !string.IsNullOrWhiteSpace(LightTextBox.Text);

                if (hasTemperature && hasHumidity && hasLight)
                {
                    if (int.TryParse(TemperatureTextBox.Text, out int temp) &&
                        decimal.TryParse(HumidityTextBox.Text, out decimal humidity) &&
                        decimal.TryParse(LightTextBox.Text, out decimal light))
                    {
                        var climateData = new ClimateData
                        {
                            GreenhouseId = _greenhouseId,
                            Temperature = temp,
                            Humidity = humidity,
                            Light = light,
                            Timestamp = DateTime.Now
                        };
                        _facade.AddClimateData(climateData);
                    }
                }

                var existingZonesInDb = _facade.GetPlantingZones(_greenhouseId);

                foreach (var zone in _zones)
                {
                    if (zone.Id == 0)
                    {
                        zone.GreenhouseId = _greenhouseId;
                        _facade.AddPlantingZone(zone);
                    }
                    else
                    {
                        _facade.UpdatePlantingZone(zone);
                    }
                }

                foreach (var zoneToDelete in existingZonesInDb)
                {
                    if (!_zones.Any(z => z.Id == zoneToDelete.Id))
                    {
                        _facade.DeletePlantingZone(zoneToDelete.Id);
                    }
                }

                MessageBox.Show("Данные теплицы обновлены");
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

        private void DeleteZoneButton_Click(object sender, RoutedEventArgs e)
        {
            if (ZonesListView.SelectedItem is PlantingZones selectedZone)
            {
                _zones.Remove(selectedZone);
                ZonesListView.Items.Refresh();
            }
            else
            {
                MessageBox.Show("Выберите зону для удаления");
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _facade?.Dispose();
            base.OnClosed(e);
        }
    }
}