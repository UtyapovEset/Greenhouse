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
    /// Логика взаимодействия для AddPlanWindow.xaml
    /// </summary>
    public partial class AddPlanWindow : Window
    {
        private Greenhouse_AtenaEntities _context;

        public AddPlanWindow()
        {
            InitializeComponent();
            _context = new Greenhouse_AtenaEntities();
            LoadGreenhouses();
            StartDatePicker.SelectedDate = DateTime.Today;
            EndDatePicker.SelectedDate = DateTime.Today.AddDays(7);
        }

        private void LoadGreenhouses()
        {
            try
            {
                var greenhouses = _context.Greenhouses.ToList();
                GreenhouseComboBox.ItemsSource = greenhouses;
                GreenhouseComboBox.DisplayMemberPath = "Name";
                GreenhouseComboBox.SelectedValuePath = "Id";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки теплиц: {ex.Message}");
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (GreenhouseComboBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите теплицу");
                return;
            }

            if (!StartDatePicker.SelectedDate.HasValue || !EndDatePicker.SelectedDate.HasValue)
            {
                MessageBox.Show("Выберите даты начала и окончания");
                return;
            }

            if (StartDatePicker.SelectedDate > EndDatePicker.SelectedDate)
            {
                MessageBox.Show("Дата начала не может быть позже даты окончания");
                return;
            }

            try
            {
                var plan = new WorkPlans
                {
                    GreenhouseId = (int)GreenhouseComboBox.SelectedValue,
                    StartDate = StartDatePicker.SelectedDate.Value,
                    EndDate = EndDatePicker.SelectedDate.Value,
                    Status = (StatusComboBox.SelectedItem as ComboBoxItem)?.Content.ToString()
                };

                _context.WorkPlans.Add(plan);
                _context.SaveChanges();

                MessageBox.Show("План работ добавлен");
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