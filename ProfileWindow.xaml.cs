using Aurora_Launcher.Models;
using AuroraLauncher;
using Microsoft.WindowsAPICodePack.Dialogs;
using Npgsql;
using SharpCompress.Archives;
using SharpCompress.Archives.Rar;
using SharpCompress.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Aurora_Launcher
{
    public partial class ProfileWindow : Window
    {
        private readonly int _userId;
        // Путь к файлу, где хранятся установленные игры
        private readonly string _installInfoFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "installed_games.txt");

        public ProfileWindow(int userId)
        {
            InitializeComponent();
            _userId = userId;
            LoadProfile();
            LoadOwnedGames();
        }

        private void LoadProfile()
        {
            using var conn = DbCon.GetConnection();
            var cmd = new NpgsqlCommand(
                "SELECT nickname, balance, avatar_url, role_id FROM users WHERE user_id = @id",
                conn);
            cmd.Parameters.AddWithValue("id", _userId);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                NicknameText.Text = reader.GetString(0);
                BalanceText.Text = $"Баланс: {reader.GetDecimal(1):0.00} ₽";

                if (!reader.IsDBNull(2))
                {
                    var avatarUrl = reader.GetString(2);
                    AvatarImage.Source = new System.Windows.Media.Imaging.BitmapImage(
                        new Uri(avatarUrl, UriKind.RelativeOrAbsolute));
                }

                int roleId = reader.GetInt32(3);
                if (roleId == 2 || roleId == 3) // admin или developer
                {
                    AddGameButton.Visibility = Visibility.Visible;
                    ManageGenresButton.Visibility = Visibility.Visible;
                    StatsButton.Visibility = Visibility.Visible;
                }
            }
        }

            
        private void ManageStats_Click(object sender, RoutedEventArgs e)
        {
            var wnd = new DeveloperStatsWindow(_userId);
            wnd.Owner = this;
            wnd.ShowDialog();
        }


        private void AddGame_Click(object sender, RoutedEventArgs e)
        {
            new AddGameWindow(_userId).ShowDialog();
            LoadOwnedGames(); // Обновляем список после добавления
        }

        private void LoadOwnedGames()
        {
            var games = new List<Game>();

            // Читаем файл installed_games.txt
            Dictionary<int, string> installedMap = new();
            if (File.Exists(_installInfoFile))
            {
                foreach (var line in File.ReadAllLines(_installInfoFile))
                {
                    var parts = line.Split('|');
                    if (parts.Length == 2
                        && int.TryParse(parts[0], out int gid)
                        && !string.IsNullOrEmpty(parts[1])
                        && Directory.Exists(parts[1]))
                    {
                        installedMap[gid] = parts[1];
                    }
                }
            }

            // купленные игры из базы
            using var conn = DbCon.GetConnection();
            var cmd = new NpgsqlCommand(@"
                SELECT g.game_id, g.title, g.cover_url, g.download_url
                FROM purchases p
                JOIN games g ON p.game_id = g.game_id
                WHERE p.user_id = @id", conn);
            cmd.Parameters.AddWithValue("id", _userId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var g = new Game
                {
                    GameId = reader.GetInt32(0),
                    Title = reader.GetString(1),
                    CoverUrl = reader.IsDBNull(2) ? null : reader.GetString(2),
                    DownloadUrl = reader.IsDBNull(3) ? null : reader.GetString(3),
                    IsInstalled = false,
                    InstallPath = ""
                };

                if (installedMap.TryGetValue(g.GameId, out string path) && Directory.Exists(path))
                {
                    g.IsInstalled = true;
                    g.InstallPath = path;
                }

                games.Add(g);
            }

            OwnedGamesList.ItemsSource = games;
        }

        private async void InstallButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || !(btn.Tag is int gameId))
                return;

            if (!(btn.DataContext is Game game))
                return;

            // Если игра уже установлена — запускаем
            if (game.IsInstalled && Directory.Exists(game.InstallPath))
            {
                try
                {
                    string[] exes = Directory.GetFiles(game.InstallPath, "*.exe", SearchOption.TopDirectoryOnly);
                    if (exes.Length > 0)
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(exes[0])
                        {
                            WorkingDirectory = game.InstallPath
                        });
                    }
                    else
                    {
                        System.Windows.MessageBox.Show("Не найден исполняемый файл.");
                    }
                }
                catch (Exception ex2)
                {
                    System.Windows.MessageBox.Show("Не удалось запустить игру: " + ex2.Message);
                }
                return;
            }

            if (string.IsNullOrEmpty(game.DownloadUrl))
            {
                System.Windows.MessageBox.Show("Ссылка на скачивание не найдена.");
                return;
            }

            btn.IsEnabled = false;

            //Выбор папки для установки через CommonOpenFileDialog
            string installRoot;
            var folderDialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Title = "Выберите папку, куда установить игру:"
            };

            if (folderDialog.ShowDialog() != CommonFileDialogResult.Ok)
            {
                btn.IsEnabled = true;
                return;
            }
            installRoot = folderDialog.FileName;

            //Скачивание архива в %TEMP%
            string tempRar = Path.Combine(Path.GetTempPath(), $"game_{gameId}.rar");
            var container = (FrameworkElement)btn.Parent;
            var progressBar = (ProgressBar)container.FindName("InstallProgressBar")!;
            progressBar.Visibility = Visibility.Visible;
            progressBar.Value = 0;

            try
            {
                using var web = new WebClient();
                web.DownloadProgressChanged += (s, args) =>
                {
                    progressBar.Value = args.ProgressPercentage / 2.0;
                };
                await web.DownloadFileTaskAsync(new Uri(game.DownloadUrl), tempRar);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Ошибка при скачивании: " + ex.Message);
                progressBar.Visibility = Visibility.Collapsed;
                btn.IsEnabled = true;
                return;
            }

            // Распаковка архива RAR
            string gameFolder = Path.Combine(installRoot, game.Title);
            try
            {
                if (Directory.Exists(gameFolder))
                    Directory.Delete(gameFolder, true);
                Directory.CreateDirectory(gameFolder);

                using (var archive = RarArchive.Open(tempRar))
                {
                    var entries = archive.Entries.Where(entry => !entry.IsDirectory).ToList();
                    int total = entries.Count;
                    int count = 0;

                    foreach (var entry in entries)
                    {
                        entry.WriteToDirectory(gameFolder, new ExtractionOptions()
                        {
                            ExtractFullPath = true,
                            Overwrite = true
                        });

                        count++;

                        progressBar.Value = 50 + (count * 50.0 / total);
                    }
                }

                File.Delete(tempRar);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Ошибка при распаковке: " + ex.Message);
                progressBar.Visibility = Visibility.Collapsed;
                btn.IsEnabled = true;
                return;
            }

            //Сохранение информации об установке в файл installed_games.txt
            try
            {
                string line = $"{game.GameId}|{gameFolder}";
                File.AppendAllLines(EnsureFileDirectory(_installInfoFile), new[] { line });
            }
            catch
            {
            }

            //теперь игра установлена
            game.IsInstalled = true;
            game.InstallPath = gameFolder;

            progressBar.Value = 100;
            btn.Content = game.ButtonText;
            btn.Background = (Brush)game.ButtonColor;
            btn.IsEnabled = true;

            if (container.FindName("InstallProgressBar") is ProgressBar bar)
                bar.Visibility = Visibility.Collapsed;

            if (container.FindName("DeleteButton") is Button deleteBtn)
                deleteBtn.Visibility = Visibility.Visible;

        }

        private static string EnsureFileDirectory(string path)
        {
            try
            {
                string dir = Path.GetDirectoryName(path)!;
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
            }
            catch { /* игнорируем */ }
            return path;
        }


        private void BackToShop_Click(object sender, RoutedEventArgs e)
        {
            new ShopWindow(_userId).Show();
            Close();
        }

        private void EditProfile_Click(object sender, RoutedEventArgs e)
        {
            new EditProfileWindow(_userId).ShowDialog();
            LoadProfile();  // перезагрузить профиль после редактирования
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            const string RememberFile = "remember_me.txt";
            if (File.Exists(RememberFile))
                File.Delete(RememberFile);

            new LoginWindow(autoLoginEnabled: false).Show();
            Close();
        }

        private void RechargeButton_Click(object sender, RoutedEventArgs e)
        {
            var wnd = new RechargeWindow(_userId)
            {
                Owner = this
            };
            if (wnd.ShowDialog() == true)
            {
                // После успешного пополнения обновляем отображение баланса
                LoadProfile();
            }
        }
        private void ManageGenres_Click(object sender, RoutedEventArgs e)
        {
            var wnd = new GenreManagerWindow
            {
                Owner = this
            };
            wnd.ShowDialog();
        }


        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var running = Process.GetProcessesByName("Сапёр v34");
            if (running.Length > 0)
            {
                MessageBox.Show(
                    "Нельзя удалить игру, пока приложение запущено. Закройте приложение и повторите попытку",
                    "Ошибка удаления",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }
            if (sender is not Button delBtn || !(delBtn.Tag is int gameId))
                return;
            if (!(delBtn.DataContext is Game game) || !game.IsInstalled)
                return;

            if (MessageBox.Show(
                $"Удалить «{game.Title}»?",
                "Подтвердите удаление",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question)
              != MessageBoxResult.Yes)
                return;

            // 1. Удаляем папку
            if (Directory.Exists(game.InstallPath))
                Directory.Delete(game.InstallPath, true);

            // 2. Удаляем из файла installed_games.txt
            if (File.Exists(_installInfoFile))
            {
                var lines = File.ReadAllLines(_installInfoFile)
                                 .Where(l => !l.StartsWith(game.GameId + "|"));
                File.WriteAllLines(EnsureFileDirectory(_installInfoFile), lines);
            }

            // 3. Сбрасываем флаг
            game.IsInstalled = false;
            game.InstallPath = "";

            // 4. Обновляем UI
            // Найдём контейнер, где лежат кнопки и прогрессбар
            if (VisualTreeHelper.GetParent(delBtn) is StackPanel panel)
            {
                // Скрываем кнопку «Удалить»
                delBtn.Visibility = Visibility.Collapsed;

                // Прячем прогрессбар
                if (panel.FindName("InstallProgressBar") is ProgressBar bar)
                    bar.Visibility = Visibility.Collapsed;

                // Меняем «InstallButton» обратно на «Установить»
                if (panel.FindName("InstallButton") is Button installBtn)
                {
                    installBtn.Content = game.ButtonText;        // обычно «Установить»
                    installBtn.Background = (Brush)game.ButtonColor;
                    installBtn.IsEnabled = true;
                }
            }
        }
    }
}