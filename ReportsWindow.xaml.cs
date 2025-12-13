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
    /// Логика взаимодействия для ReportsWindow.xaml
    /// </summary>
    public partial class ReportsWindow : Window
    {
        private string _currentUserRole;

        public ReportsWindow()
        {
            InitializeComponent();
        }

        public ReportsWindow(string userRole) : this()
        {
            _currentUserRole = userRole;
            LoadReports();
        }

        private void LoadReports()
        {
            try
            {
                LoadDailyReport();
                LoadYieldForecast();
                LoadCropStatus();
                LoadOverdueTasks();
                LoadTaskCompletion();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки отчетов: {ex.Message}");
            }
        }

        private void LoadDailyReport()
        {
            using (var context = new Greenhouse_AtenaEntities())
            {
                var today = DateTime.Today;
                var tomorrow = today.AddDays(1);
                var nextWeek = today.AddDays(7);

                try
                {
                    // --- КЛИМАТ ЗА СЕГОДНЯ ---
                    var climateRaw = context.ClimateData
                        .Include(c => c.Greenhouses)
                        .Where(c => c.Timestamp >= today && c.Timestamp < tomorrow)
                        .ToList();

                    var climateData = climateRaw
                        .GroupBy(c => c.GreenhouseId)
                        .Select(g => new
                        {
                            Greenhouse = g.First().Greenhouses?.Name ?? "Не указана",
                            AvgTemp = g.Average(x => x.Temperature),
                            AvgHumidity = g.Average(x => x.Humidity)
                        })
                        .ToList();

                    DailyClimateList.ItemsSource = climateData;


                    // --- ЗАДАЧИ ---
                    var todayTasks = context.WorkTasks
                        .Where(t => t.DueDate >= today && t.DueDate < tomorrow)
                        .ToList();

                    var completed = todayTasks.Count(t => t.Status == "Выполнена");
                    TasksCompletedText.Text = $"{completed} из {todayTasks.Count}";


                    // --- ПРОБЛЕМНЫЕ ЗОНЫ ---
                    var problemZones = context.PlantingZones
                        .Include(z => z.Greenhouses)
                        .Where(z => z.Status == "Problem")
                        .ToList()
                        .Select(z => new
                        {
                            Zone = z.ZoneName ?? "Не указана",
                            Greenhouse = z.Greenhouses?.Name ?? "Не указана",
                            Problem = "Требует внимания"
                        })
                        .ToList();

                    ProblemZonesList.ItemsSource = problemZones;


                    // --- БЛИЖАЙШИЙ УРОЖАЙ ---
                    var harvestRaw = context.PlantingZones
                        .Include(z => z.Crops)
                        .Include(z => z.Greenhouses)
                        .Where(z => z.ExpectedHarvestDate >= today &&
                                    z.ExpectedHarvestDate <= nextWeek)
                        .ToList();

                    var upcoming = harvestRaw
                        .Select(z => new
                        {
                            Crop = z.Crops?.Name ?? "Не указана",
                            Zone = z.ZoneName ?? "Не указана",
                            Date = z.ExpectedHarvestDate,
                            Greenhouse = z.Greenhouses?.Name ?? "Не указана"
                        })
                        .ToList();

                    UpcomingHarvestList.ItemsSource = upcoming;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки ежедневного отчета: {ex.Message}");
                }
            }
        }

        private void LoadYieldForecast()
        {
            using (var context = new Greenhouse_AtenaEntities())
            {
                var today = DateTime.Today;

                try
                {
                    var forecast7Days = GetHarvestForecast(context, today, 7);
                    var forecast30Days = GetHarvestForecast(context, today, 30);
                    var forecast60Days = GetHarvestForecast(context, today, 60);

                    Forecast7DaysText.Text = forecast7Days.Count.ToString();
                    Forecast30DaysText.Text = forecast30Days.Count.ToString();
                    Forecast60DaysText.Text = forecast60Days.Count.ToString();

                    YieldForecastList.ItemsSource = forecast7Days.Take(10);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки прогноза урожайности: {ex.Message}");
                    Forecast7DaysText.Text = "0";
                    Forecast30DaysText.Text = "0";
                    Forecast60DaysText.Text = "0";
                    YieldForecastList.ItemsSource = new List<dynamic>();
                }
            }
        }

        private List<dynamic> GetHarvestForecast(Greenhouse_AtenaEntities context, DateTime startDate, int days)
        {
            var endDate = startDate.AddDays(days);

            var zones = context.PlantingZones
                .Include(z => z.Crops)
                .Include(z => z.Greenhouses)
                .Where(z => z.ExpectedHarvestDate >= startDate &&
                            z.ExpectedHarvestDate <= endDate)
                .ToList();

            return zones
                .Select(z => new
                {
                    Crop = z.Crops?.Name ?? "Не указана",
                    Greenhouse = z.Greenhouses?.Name ?? "Не указана",
                    HarvestDate = z.ExpectedHarvestDate,
                    DaysLeft = (z.ExpectedHarvestDate.Value - startDate).Days
                })
                .ToList<dynamic>();
        }

        private void LoadCropStatus()
        {
            using (var context = new Greenhouse_AtenaEntities())
            {
                try
                {
                    var crops = context.Crops.Include(c => c.PlantingZones).ToList();

                    var good = crops.Count(c => c.PlantingZones.All(z => z.Status != "Problem"));
                    var attention = crops.Count(c =>
                        c.PlantingZones.Any(z => z.Status == "Problem") &&
                        c.PlantingZones.Count(z => z.Status == "Problem") <= c.PlantingZones.Count * 0.5);
                    var critical = crops.Count(c =>
                        c.PlantingZones.Count(z => z.Status == "Problem") > c.PlantingZones.Count * 0.5);

                    GoodCropsText.Text = good.ToString();
                    AttentionCropsText.Text = attention.ToString();
                    CriticalCropsText.Text = critical.ToString();

                    CropStatusList.ItemsSource = crops
                        .Where(c => c.PlantingZones.Any(z => z.Status == "Problem"))
                        .Select(c => new
                        {
                            Name = c.Name,
                            ProblemZones = c.PlantingZones.Count(z => z.Status == "Problem"),
                            TotalZones = c.PlantingZones.Count
                        })
                        .Take(10)
                        .ToList();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки состояния культур: {ex.Message}");
                }
            }
        }

        private void LoadOverdueTasks()
        {
            using (var context = new Greenhouse_AtenaEntities())
            {
                try
                {
                    var now = DateTime.Now;

                    var tasks = context.WorkTasks
                        .Where(t => t.DueDate < now && t.Status != "Выполнена" && t.Status != "Отменена")
                        .ToList()
                        .Select(t => new
                        {
                            Task = t.Description,
                            DueDate = t.DueDate,
                            Status = t.Status,
                            AssignedTo = t.AssignedTo ?? "Не назначен",
                            Greenhouse = "Общая"
                        })
                        .ToList();

                    OverdueTasksCountText.Text = tasks.Count.ToString();
                    OverdueTasksList.ItemsSource = tasks.Take(10);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки просроченных задач: {ex.Message}");
                }
            }
        }

        private void LoadTaskCompletion()
        {
            using (var context = new Greenhouse_AtenaEntities())
            {
                try
                {
                    var startDate = PeriodStartDatePicker.SelectedDate ?? DateTime.Today.AddDays(-30);
                    var endDate = PeriodEndDatePicker.SelectedDate ?? DateTime.Today;

                    var tasks = context.WorkTasks
                        .Where(t => t.DueDate >= startDate && t.DueDate <= endDate)
                        .ToList();

                    var completed = tasks.Count(t => t.Status == "Выполнена");
                    var notCompleted = tasks.Count(t => t.Status != "Выполнена" && t.Status != "Отменена");

                    TotalTasksText.Text = tasks.Count.ToString();
                    CompletedTasksText.Text = completed.ToString();
                    NotCompletedTasksText.Text = notCompleted.ToString();

                    TaskCompletionList.ItemsSource = tasks
                        .OrderByDescending(t => t.DueDate)
                        .Take(15)
                        .Select(t => new
                        {
                            Date = t.DueDate.ToString("dd.MM.yyyy HH:mm"),
                            Description = t.Description,
                            Status = t.Status ?? "Не указан",
                            AssignedTo = t.AssignedTo ?? "Не назначен"
                        })
                        .ToList();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки выполнения задач: {ex.Message}");
                    TotalTasksText.Text = "0";
                    CompletedTasksText.Text = "0";
                    NotCompletedTasksText.Text = "0";
                    TaskCompletionList.ItemsSource = new List<dynamic>();
                }
            }
        }

        private void PeriodStartDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PeriodStartDatePicker.SelectedDate.HasValue)
                LoadTaskCompletion();
        }

        private void PeriodEndDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PeriodEndDatePicker.SelectedDate.HasValue)
                LoadTaskCompletion();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadReports();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = new MainWindow(_currentUserRole);
            mainWindow.Show();
            this.Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (!PeriodStartDatePicker.SelectedDate.HasValue)
                PeriodStartDatePicker.SelectedDate = DateTime.Today.AddDays(-30);

            if (!PeriodEndDatePicker.SelectedDate.HasValue)
                PeriodEndDatePicker.SelectedDate = DateTime.Today;

            LoadReports();
        }
    }
}