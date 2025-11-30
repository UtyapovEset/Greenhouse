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
    /// Логика взаимодействия для AddTaskWindow.xaml
    /// </summary>
    public partial class AddTaskWindow : Window
    {
        private Greenhouse_AtenaEntities _context;
        private DateTime _selectedDate;

        public AddTaskWindow(DateTime selectedDate)
        {
            InitializeComponent();
            _context = new Greenhouse_AtenaEntities();
            _selectedDate = selectedDate;
            TaskDatePicker.SelectedDate = selectedDate;
            LoadPlans();
            LoadZones();
        }

        private void LoadPlans()
        {
            try
            {
                var plans = _context.WorkPlans.ToList();
                PlanComboBox.ItemsSource = plans;
                PlanComboBox.DisplayMemberPath = "Id";
                PlanComboBox.SelectedValuePath = "Id";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки планов: {ex.Message}");
            }
        }

        private void LoadZones()
        {
            try
            {
                var zones = _context.PlantingZones.ToList();
                ZoneComboBox.ItemsSource = zones;
                ZoneComboBox.DisplayMemberPath = "ZoneName";
                ZoneComboBox.SelectedValuePath = "Id";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки зон: {ex.Message}");
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (PlanComboBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите план работ");
                return;
            }

            if (string.IsNullOrWhiteSpace(DescriptionTextBox.Text))
            {
                MessageBox.Show("Введите описание задачи");
                return;
            }

            if (!TaskDatePicker.SelectedDate.HasValue)
            {
                MessageBox.Show("Выберите дату выполнения");
                return;
            }

            try
            {
                var task = new WorkTasks
                {
                    WorkPlanId = (int)PlanComboBox.SelectedValue,
                    PlantingZoneId = ZoneComboBox.SelectedItem != null ? (int?)ZoneComboBox.SelectedValue : null,
                    DueDate = TaskDatePicker.SelectedDate.Value.Date + TimeSpan.Parse(TimeTextBox.Text),
                    Description = DescriptionTextBox.Text,
                    AssignedTo = AssignedToTextBox.Text,
                    Comments = CommentsTextBox.Text,
                    Status = "Запланирована"
                };

                _context.WorkTasks.Add(task);
                _context.SaveChanges();

                MessageBox.Show("Задача добавлена");
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
