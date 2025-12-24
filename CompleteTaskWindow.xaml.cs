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
        private GreenhouseFacade _facade;
        private int _taskId;

        public CompleteTaskWindow(int taskId)
        {
            InitializeComponent();
            _facade = new GreenhouseFacade();
            _taskId = taskId;
            LoadTaskData();
        }

        private void LoadTaskData()
        {
            try
            {
                using (var context = new Greenhouse_AtenaEntities())
                {
                    var task = context.WorkTasks.Find(_taskId);
                    if (task != null)
                    {
                        TaskDescriptionText.Text = task.Description;
                    }
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
                var task = new WorkTasks
                {
                    Id = _taskId,
                    Status = "Выполнена",
                    Comments = CompletionCommentsTextBox.Text
                };

                _facade.UpdateWorkTask(task);

                MessageBox.Show("Задача отмечена как выполненная");
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

        protected override void OnClosed(EventArgs e)
        {
            _facade?.Dispose();
            base.OnClosed(e);
        }
    }
}