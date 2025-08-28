using Aurora_Launcher.Models;
using AuroraLauncher;
using Npgsql;
using System.Collections.Generic;
using System.Windows;

namespace Aurora_Launcher
{
    public partial class DeveloperStatsWindow : Window
    {
        private readonly int _currentUserId;

        public DeveloperStatsWindow(int userId)
        {
            InitializeComponent();
            _currentUserId = userId;

            using var conn = DbCon.GetConnection();
            var cmdRole = new NpgsqlCommand(
                "SELECT role_id FROM users WHERE user_id = @id", conn);
            cmdRole.Parameters.AddWithValue("id", userId);
            int role = (int)cmdRole.ExecuteScalar();

            bool isAdmin = (role == 2);
            bool isDev = (role == 3);

            if (isAdmin)
            {
                // админу даём выбор из всех developers
                LoadDevelopers();
                CbDevelopers.Visibility = Visibility.Visible;
            }
            else if (isDev)
            {
                // у разработчика сначала определяем его developer_id
                var cmdDev = new NpgsqlCommand(@"
            SELECT developer_id 
            FROM developers 
            WHERE name = (SELECT nickname FROM users WHERE user_id = @uid)", conn);
                cmdDev.Parameters.AddWithValue("uid", userId);
                var devIdObj = cmdDev.ExecuteScalar();
                if (devIdObj is int devId)
                {
                    CbDevelopers.Visibility = Visibility.Collapsed;
                    LoadStatsForDeveloper(devId);
                }
                else
                {
                    MessageBox.Show("Ваша учётка не привязана к разработчику.",
                                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    Close();
                }
            }
            else
            {
                MessageBox.Show("Доступ запрещён.",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                Close();
            }
        }


        private void LoadDevelopers()
        {
            var list = new List<Developer>();
            using var conn = DbCon.GetConnection();
            var cmd = new NpgsqlCommand("SELECT developer_id, name FROM developers ORDER BY name", conn);
            using var rdr = cmd.ExecuteReader();
            while (rdr.Read())
                list.Add(new Developer { DeveloperId = rdr.GetInt32(0), Name = rdr.GetString(1) });

            CbDevelopers.ItemsSource = list;
            CbDevelopers.DisplayMemberPath = "Name";
            CbDevelopers.SelectedValuePath = "DeveloperId";
            if (list.Count > 0) CbDevelopers.SelectedIndex = 0;
        }

        private void CbDevelopers_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (CbDevelopers.SelectedValue is int devId)
                LoadStatsForDeveloper(devId);
        }

        private void LoadStatsForDeveloper(int developerId)
        {
            var stats = new List<GameStat>();
            using var conn = DbCon.GetConnection();
            var cmd = new NpgsqlCommand(@"
                SELECT 
                    g.title,
                    COUNT(p.*)     AS salescount,
                    g.price
                FROM games g
                LEFT JOIN purchases p ON g.game_id = p.game_id
                WHERE g.developer_id = @dev
                GROUP BY g.title, g.price
                ORDER BY salescount DESC", conn);
            cmd.Parameters.AddWithValue("dev", developerId);

            using var rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                stats.Add(new GameStat
                {
                    Title = rdr.GetString(0),
                    SalesCount = rdr.GetInt64(1),
                    Price = rdr.GetDecimal(2)
                });
            }

            GridStats.ItemsSource = stats;
            // пересчёт общей выручки
            decimal total = 0;
            foreach (var g in stats)
                total += g.Revenue;
            TbTotalRevenue.Text = $"{total:0.00} ₽";
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
