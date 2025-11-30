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
    /// Логика взаимодействия для CreatePlantsWindow.xaml
    /// </summary>
    public partial class CreatePlantsWindow : Window
    {
        private Greenhouse_AtenaEntities _context;

        public CreatePlantsWindow()
        {
            InitializeComponent();
            _context = new Greenhouse_AtenaEntities();
            LoadSorts();
        }

        private void LoadSorts()
        {
            try
            {
                var sorts = _context.Type_Crops.ToList();
                SortComboBox.ItemsSource = sorts;
                SortComboBox.DisplayMemberPath = "name_sort";
                SortComboBox.SelectedValuePath = "id";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки сортов: {ex.Message}");
            }
        }

        private void AddSortButton_Click(object sender, RoutedEventArgs e)
        {
            var newSortWindow = new Window
            {
                Title = "Новый сорт",
                Width = 300,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this
            };

            var stackPanel = new StackPanel { Margin = new Thickness(20) };

            var textBox = new TextBox { Height = 25, Margin = new Thickness(0, 0, 0, 10) };

            var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var okButton = new Button { Content = "Добавить", Width = 80, Margin = new Thickness(0, 0, 10, 0) };
            var cancelButton = new Button { Content = "Отмена", Width = 80, IsCancel = true };

            okButton.Click += (s, args) =>
            {
                if (!string.IsNullOrWhiteSpace(textBox.Text))
                {
                    try
                    {
                        var newSort = new Type_Crops
                        {
                            name_sort = textBox.Text
                        };

                        _context.Type_Crops.Add(newSort);
                        _context.SaveChanges();

                        LoadSorts();
                        MessageBox.Show("Сорт добавлен");
                        newSortWindow.Close();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка: {ex.Message}");
                    }
                }
                else
                {
                    MessageBox.Show("Введите название сорта");
                }
            };

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);

            stackPanel.Children.Add(new TextBlock { Text = "Введите название сорта:" });
            stackPanel.Children.Add(textBox);
            stackPanel.Children.Add(buttonPanel);

            newSortWindow.Content = stackPanel;
            newSortWindow.ShowDialog();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameTextBox.Text))
            {
                MessageBox.Show("Введите название культуры");
                return;
            }

            if (SortComboBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите сорт");
                return;
            }

            if (!int.TryParse(GrowthDaysTextBox.Text, out int growthDays) || growthDays <= 0)
            {
                MessageBox.Show("Введите корректное количество дней роста");
                return;
            }

            if (!int.TryParse(TemperatureTextBox.Text, out int temperature))
            {
                MessageBox.Show("Введите корректную температуру");
                return;
            }

            if (!decimal.TryParse(HumidityTextBox.Text, out decimal humidity) || humidity < 0 || humidity > 100)
            {
                MessageBox.Show("Введите корректную влажность (0-100)");
                return;
            }

            try
            {
                var newCrop = new Crops
                {
                    Name = NameTextBox.Text,
                    Sort = (int)SortComboBox.SelectedValue,
                    GrowthDays = growthDays,
                    OptimalTemperature = temperature,
                    OptimalHumidity = humidity
                };

                _context.Crops.Add(newCrop);
                _context.SaveChanges();

                MessageBox.Show("Культура добавлена успешно");
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