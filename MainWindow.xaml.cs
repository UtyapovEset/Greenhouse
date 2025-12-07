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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Greenhose
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string _currentUserRole;

        public MainWindow(string userRole)
        {
            InitializeComponent();
            _currentUserRole = userRole;
        }

        private void CalendarBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Calendar_Window calendar = new Calendar_Window(_currentUserRole);
            calendar.Show();
            this.Close();
        }

        private void PlantsBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Plants_WIndow plantsWindow = new Plants_WIndow(_currentUserRole);
            plantsWindow.Show();
            this.Close();
        }

        private void GreenhousesBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Greenhouse_Window greenhouseWindow = new Greenhouse_Window(_currentUserRole);
            greenhouseWindow.Show();
            this.Close();
        }

        private void AddUserButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentUserRole != "Admin")
            {
                MessageBox.Show("У вас нет прав для добавления пользователей", "Ошибка доступа",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var addUserWindow = new AddUserWindow();
            addUserWindow.ShowDialog();
        }
    }
}
