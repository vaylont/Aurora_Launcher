using AuroraLauncher;
using Npgsql;
using System.IO;
using System.Windows;

namespace Aurora_Launcher
{
    public partial class LoginWindow : Window
    {
        private const string RememberFile = "remember_me.txt";

        private readonly bool _autoLoginEnabled;
        private string? _autoLoginUsername;

        public LoginWindow() : this(true) { }
        public LoginWindow(bool autoLoginEnabled = true)
        {
            InitializeComponent();
            _autoLoginEnabled = autoLoginEnabled;

            if (File.Exists(RememberFile))
            {
                var login = File.ReadAllText(RememberFile).Trim();
                RememberMeBox.IsChecked = true;
                LoginBox.Text = login;

                if (_autoLoginEnabled)
                    _autoLoginUsername = login;
            }

            Loaded += LoginWindow_Loaded;
        }

        private void LoginWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (_autoLoginEnabled && !string.IsNullOrEmpty(_autoLoginUsername))
                AutoLogin(_autoLoginUsername);
        }

        


        private void LoadRememberedLogin()
        {
            if (File.Exists(RememberFile))
            {
                var login = File.ReadAllText(RememberFile).Trim();
                RememberMeBox.IsChecked = true;
                LoginBox.Text = login;

                AutoLogin(login);
            }
        }
        private void AutoLogin(string login)
        {
            using var conn = DbCon.GetConnection();

            var cmd = new NpgsqlCommand("SELECT user_id FROM users WHERE login = @login", conn);
            cmd.Parameters.AddWithValue("login", login);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                var userId = reader.GetInt32(0);
                var shop = new ShopWindow(userId);
                shop.Show();
                Close();
            }
            else
            {
                MessageBox.Show("Сохранённый логин недействителен.");
                File.Delete(RememberFile);
            }
        }


        private void Login_Click(object sender, RoutedEventArgs e)
        {
            var login = LoginBox.Text.Trim();
            var pass = PasswordBox.Password.Trim();

            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(pass))
            {
                MessageBox.Show("Введите логин и пароль.");
                return;
            }

            using var conn = DbCon.GetConnection();

            var cmd = new NpgsqlCommand(@"
        SELECT user_id, nickname FROM users
        WHERE login = @login AND password_hash = @pass", conn);

            cmd.Parameters.AddWithValue("login", login);
            cmd.Parameters.AddWithValue("pass", pass);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                if (RememberMeBox.IsChecked == true)
                    File.WriteAllText(RememberFile, login);
                else if (File.Exists(RememberFile))
                    File.Delete(RememberFile);

                var userId = reader.GetInt32(0);
                var shop = new ShopWindow(userId);

                shop.Show();
                this.Close();
            }
            else
            {
                MessageBox.Show("Неверный логин или пароль.");
            }
        }


        private void OpenRegister_Click(object sender, RoutedEventArgs e)
        {
            new RegisterWindow().Show();
            Close();
        }
    }
}
