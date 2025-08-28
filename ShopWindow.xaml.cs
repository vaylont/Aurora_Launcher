using Aurora_Launcher.Models;
using AuroraLauncher;
using Npgsql;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;


namespace Aurora_Launcher
{
    public partial class ShopWindow : Window
    {
        private readonly int _userId;
        public ShopWindow(int userId)
        {
            InitializeComponent();
            _userId = userId;
            LoadGames();
        }
        private void GameCard_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is int gameId)
            {
                new GameDetailWindow(_userId, gameId).Show();
                this.Close();
            }
        }

        private void LoadGames()
        {
            var games = new List<Game>();

            using var conn = DbCon.GetConnection();
            var cmd = new NpgsqlCommand("SELECT game_id, title, description, cover_url, price FROM games", conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                games.Add(new Game
                {
                    GameId = reader.GetInt32(0),
                    Title = reader.GetString(1),
                    Description = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    CoverUrl = reader.IsDBNull(3) ? null : reader.GetString(3),
                    Price = reader.IsDBNull(4) ? 0 : reader.GetDecimal(4)

                });
            }

            GameList.ItemsSource = games;
        }

        private void OpenDetails_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as FrameworkElement;
            if (button?.Tag is int gameId)
            {
                new GameDetailWindow(_userId, gameId).Show();
                Close();
            }
        }


        private void OpenProfile_Click(object sender, RoutedEventArgs e)
        {
            new ProfileWindow(_userId).Show();
            Close();
        }
    }
}
