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
    /// Логика взаимодействия для CompleteTaskWindow.xaml
    /// </summary>
    public partial class CompleteTaskWindow : Window
    {
        private int _taskId;
        private Greenhouse_AtenaEntities _context;

        public CompleteTaskWindow(int taskId)
        {
            InitializeComponent();
            _taskId = taskId;
            _context = new Greenhouse_AtenaEntities();
            LoadTaskData();
        }

        private void LoadTaskData()
        {
            try
            {
                var task = _context.WorkTasks.Find(_taskId);
                if (task != null)
                {
                    TaskDescriptionText.Text = task.Description;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}");
            }
        }

        private void CompleteButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var task = _context.WorkTasks.Find(_taskId);
                if (task != null)
                {
                    task.Status = "Выполнена";
                    if (!string.IsNullOrWhiteSpace(CompletionCommentsTextBox.Text))
                    {
                        task.Comments = CompletionCommentsTextBox.Text;
                    }
                    _context.SaveChanges();
                    MessageBox.Show("Задача отмечена как выполненная");
                    this.DialogResult = true;
                    this.Close();
                }
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