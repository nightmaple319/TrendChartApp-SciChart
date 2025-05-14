using System;
using System.Windows;
using System.Text.RegularExpressions;

namespace TrendChartApp.Views
{
    /// <summary>
    /// DatabaseConnectionWindow.xaml 的互動邏輯
    /// </summary>
    public partial class DatabaseConnectionWindow : Window
    {
        // 連線字串格式: "Server={0};Database={1};User Id={2};Password={3};"
        private string _originalConnectionString;

        public string ConnectionString { get; private set; }

        public DatabaseConnectionWindow(string currentConnectionString)
        {
            InitializeComponent();

            _originalConnectionString = currentConnectionString;

            // 解析現有的連線字串並填充欄位
            ParseConnectionString(currentConnectionString);
        }

        /// <summary>
        /// 解析連線字串並填充對應欄位
        /// </summary>
        private void ParseConnectionString(string connectionString)
        {
            try
            {
                // 提取伺服器 IP
                Match serverMatch = Regex.Match(connectionString, @"Server=([^;]+);");
                if (serverMatch.Success)
                {
                    serverIpTextBox.Text = serverMatch.Groups[1].Value;
                }

                // 提取資料庫名稱
                Match dbMatch = Regex.Match(connectionString, @"Database=([^;]+);");
                if (dbMatch.Success)
                {
                    databaseNameTextBox.Text = dbMatch.Groups[1].Value;
                }

                // 提取使用者名稱
                Match userMatch = Regex.Match(connectionString, @"User Id=([^;]+);");
                if (userMatch.Success)
                {
                    userNameTextBox.Text = userMatch.Groups[1].Value;
                }

                // 提取密碼
                Match passwordMatch = Regex.Match(connectionString, @"Password=([^;]+);");
                if (passwordMatch.Success)
                {
                    passwordBox.Password = passwordMatch.Groups[1].Value;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"解析連線字串時發生錯誤: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 建立新的連線字串
        /// </summary>
        private string BuildConnectionString()
        {
            string server = serverIpTextBox.Text.Trim();
            string database = databaseNameTextBox.Text.Trim();
            string userId = userNameTextBox.Text.Trim();
            string password = passwordBox.Password;

            return $"Server={server};Database={database};User Id={userId};Password={password};";
        }

        /// <summary>
        /// 驗證輸入欄位
        /// </summary>
        private bool ValidateInputs()
        {
            if (string.IsNullOrWhiteSpace(serverIpTextBox.Text))
            {
                MessageBox.Show("請輸入伺服器 IP。", "欄位驗證", MessageBoxButton.OK, MessageBoxImage.Warning);
                serverIpTextBox.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(databaseNameTextBox.Text))
            {
                MessageBox.Show("請輸入資料庫名稱。", "欄位驗證", MessageBoxButton.OK, MessageBoxImage.Warning);
                databaseNameTextBox.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(userNameTextBox.Text))
            {
                MessageBox.Show("請輸入使用者名稱。", "欄位驗證", MessageBoxButton.OK, MessageBoxImage.Warning);
                userNameTextBox.Focus();
                return false;
            }

            return true;
        }

        /// <summary>
        /// 儲存按鈕點擊事件
        /// </summary>
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (ValidateInputs())
            {
                ConnectionString = BuildConnectionString();
                DialogResult = true;
                Close();
            }
        }

        /// <summary>
        /// 取消按鈕點擊事件
        /// </summary>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            ConnectionString = _originalConnectionString;
            DialogResult = false;
            Close();
        }
    }
}