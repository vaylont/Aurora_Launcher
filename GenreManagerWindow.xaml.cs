using Aurora_Launcher.Models;
using AuroraLauncher;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using static Aurora_Launcher.AddGameWindow;

namespace Aurora_Launcher
{
    public partial class GenreManagerWindow : Window
    {
        public GenreManagerWindow()
        {
            InitializeComponent();
            LoadGenres();
        }

        private void LoadGenres()
        {
            var list = new List<Genre>();
            using var conn = DbCon.GetConnection();
            var cmd = new NpgsqlCommand("SELECT genre_id, name FROM genres ORDER BY name", conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new Genre
                {
                    GenreId = reader.GetInt32(0),
                    Name = reader.GetString(1)
                });
            }
            GenresGrid.ItemsSource = list;
        }

        private void AddGenre_Click(object sender, RoutedEventArgs e)
        {
            var name = TbGenreName.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Введите название жанра.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using var conn = DbCon.GetConnection();
            // Проверка на дубликат
            var check = new NpgsqlCommand("SELECT COUNT(*) FROM genres WHERE lower(name)=lower(@n)", conn);
            check.Parameters.AddWithValue("n", name);
            var exists = (long)check.ExecuteScalar() > 0;
            if (exists)
            {
                MessageBox.Show("Жанр с таким названием уже существует.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            // Синхронизируем sequence под максимальный существующий genre_id
            using (var sync = new NpgsqlCommand(
              "SELECT setval(pg_get_serial_sequence('genres','genre_id'), " +
                "(SELECT COALESCE(MAX(genre_id),0) FROM genres))",
              conn))
            {
                sync.ExecuteNonQuery();
            }

            // Вставка
            var insert = new NpgsqlCommand("INSERT INTO genres(name) VALUES(@n)", conn);
            insert.Parameters.AddWithValue("n", name);
            insert.ExecuteNonQuery();

            TbGenreName.Clear();
            LoadGenres();
        }

        private void DeleteGenre_Click(object sender, RoutedEventArgs e)
        {
            if (GenresGrid.SelectedItem is not Genre sel) return;

            if (MessageBox.Show($"Удалить жанр «{sel.Name}»?", "Подтвердите",
                    MessageBoxButton.YesNo, MessageBoxImage.Question)
                != MessageBoxResult.Yes)
                return;

            using var conn = DbCon.GetConnection();
            var cmd = new NpgsqlCommand("DELETE FROM genres WHERE genre_id=@id", conn);
            cmd.Parameters.AddWithValue("id", sel.GenreId);
            try
            {
                cmd.ExecuteNonQuery();
            }
            catch (PostgresException ex) when (ex.SqlState == "23503")
            {
                MessageBox.Show("Нельзя удалить: есть связанные игры.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            LoadGenres();
        }

        private void RenameGenre_Click(object sender, RoutedEventArgs e)
        {
            if (GenresGrid.SelectedItem is not Genre sel) return;

            var newName = TbGenreName.Text.Trim();
            if (string.IsNullOrEmpty(newName))
            {
                MessageBox.Show("Введите новое название.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            using var conn = DbCon.GetConnection();
            // Проверка на дубль
            var check = new NpgsqlCommand("SELECT COUNT(*) FROM genres WHERE lower(name)=lower(@n) AND genre_id<>@id", conn);
            check.Parameters.AddWithValue("n", newName);
            check.Parameters.AddWithValue("id", sel.GenreId);
            if ((long)check.ExecuteScalar() > 0)
            {
                MessageBox.Show("Жанр с таким названием уже существует.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var cmd = new NpgsqlCommand("UPDATE genres SET name=@n WHERE genre_id=@id", conn);
            cmd.Parameters.AddWithValue("n", newName);
            cmd.Parameters.AddWithValue("id", sel.GenreId);
            cmd.ExecuteNonQuery();

            TbGenreName.Clear();
            LoadGenres();
        }
    }
}
