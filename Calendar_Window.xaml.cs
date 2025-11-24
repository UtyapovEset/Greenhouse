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
    /// Логика взаимодействия для Calendar_Window.xaml
    /// </summary>
    public partial class Calendar_Window : Window
    {
        private Greenhouse_AtenaEntities context;

        public Calendar_Window()
        {
            InitializeComponent();
            context = new Greenhouse_AtenaEntities();
            MainCalendar.SelectedDate = DateTime.Today;
            LoadTasksForDate(DateTime.Today);
        }

        private void MainCalendar_SelectedDatesChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MainCalendar.SelectedDate.HasValue)
            {
                DateTime selectedDate = MainCalendar.SelectedDate.Value;
                LoadTasksForDate(selectedDate);
            }
        }

        private void LoadTasksForDate(DateTime date)
        {
            TasksHeader.Text = (date.Date == DateTime.Today)
                ? "Задачи на сегодня"
                : $"Задачи на {date.ToShortDateString()}";

            try
            {
                DateTime startDate = date.Date;
                DateTime endDate = startDate.AddDays(1);

                var tasks = context.WorkTasks
                    .AsNoTracking()
                    .Where(t => t.DueDate >= startDate && t.DueDate < endDate)
                    .OrderBy(t => t.DueDate)
                    .ToList();

                if (tasks.Any())
                {
                    TasksList.ItemsSource = tasks.Select(t => new
                    {
                        Time = t.DueDate.ToShortTimeString(),
                        Description = t.Description,
                        Status = t.Status,
                        AssignedTo = t.AssignedTo ?? "Не назначен"
                    }).ToList();
                }
                else
                {
                    TasksList.ItemsSource = new List<string> { $"На {date.ToShortDateString()} задач не найдено" };
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных из БД: {ex.Message}", "Ошибка БД", MessageBoxButton.OK, MessageBoxImage.Error);
                TasksList.ItemsSource = new List<string> { "Ошибка загрузки данных" };
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