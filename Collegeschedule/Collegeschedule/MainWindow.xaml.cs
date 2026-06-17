using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace Collegeschedule
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            dataGridSchedule.AutoGeneratingColumn += DataGridSchedule_AutoGeneratingColumn;

            LoadCourses();
            LoadDays();

            comboBoxCourse.SelectionChanged += (s, e) => LoadGroups();
            comboBoxGroups.SelectionChanged += (s, e) => LoadSchedule();
            comboBoxDays.SelectionChanged += (s, e) => LoadSchedule();
        }



        private void LoadCourses()
        {
            comboBoxCourse.Items.Clear();
            comboBoxCourse.Items.Add(1);
            comboBoxCourse.Items.Add(2);
            comboBoxCourse.Items.Add(3);
            comboBoxCourse.Items.Add(4);

            comboBoxCourse.SelectedIndex = 0;
        }

        private void LoadGroups()
        {
            if (comboBoxCourse.SelectedItem == null) return;

            int course = Convert.ToInt32(comboBoxCourse.SelectedItem);
            DataTable dt = new DataTable();

            using (var conn = Database.GetConnection())
            {
                conn.Open();
                var cmd = new MySqlCommand("SELECT id, name FROM student_groups WHERE course=@c", conn);
                cmd.Parameters.AddWithValue("@c", course);

                new MySqlDataAdapter(cmd).Fill(dt);
            }

            comboBoxGroups.DisplayMemberPath = "name";
            comboBoxGroups.SelectedValuePath = "id";
            comboBoxGroups.ItemsSource = dt.DefaultView;

            if (dt.Rows.Count > 0)
                comboBoxGroups.SelectedIndex = 0;
        }

        private void LoadDays()
        {
            comboBoxDays.Items.Clear();
            comboBoxDays.Items.Add(new ComboBoxItem("Понедельник", 1));
            comboBoxDays.Items.Add(new ComboBoxItem("Вторник", 2));
            comboBoxDays.Items.Add(new ComboBoxItem("Среда", 3));
            comboBoxDays.Items.Add(new ComboBoxItem("Четверг", 4));
            comboBoxDays.Items.Add(new ComboBoxItem("Пятница", 5));
            comboBoxDays.Items.Add(new ComboBoxItem("Суббота", 6));

            comboBoxDays.SelectedIndex = 0;
        }

        private bool isDark = false;

        private void ToggleTheme_Click(object sender, RoutedEventArgs e)
        {
            var dict = new ResourceDictionary();

            if (isDark)
                dict.Source = new Uri("Themes/LightTheme.xaml", UriKind.Relative);
            else
                dict.Source = new Uri("Themes/DarkTheme.xaml", UriKind.Relative);

            Application.Current.Resources.MergedDictionaries[0] = dict;

            isDark = !isDark;
        }

        private void LoadSchedule()
        {
            if (comboBoxGroups.SelectedValue == null || comboBoxDays.SelectedItem == null)
                return;

            int groupId = Convert.ToInt32(comboBoxGroups.SelectedValue);
            int day = ((ComboBoxItem)comboBoxDays.SelectedItem).Value;

            DataTable table = new DataTable();

            using (var conn = Database.GetConnection())
            {
                conn.Open();

                string sql = @"SELECT s.id,
                    CONCAT(TIME_FORMAT(s.start_time, '%H:%i'),' - ',TIME_FORMAT(s.end_time, '%H:%i')) AS Время,
                    subj.name AS Предмет,
                    t.full_name AS Преподаватель,
                    s.auditory AS Кабинет,
                    s.lesson_number AS Пара
                    FROM schedule s
                    JOIN subjects subj ON s.subject_id=subj.id
                    JOIN teachers t ON s.teacher_id=t.id
                    WHERE s.student_group_id=@g AND s.day_of_week=@d
                    ORDER BY s.lesson_number";

                var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@g", groupId);
                cmd.Parameters.AddWithValue("@d", day);

                new MySqlDataAdapter(cmd).Fill(table);
            }

            dataGridSchedule.ItemsSource = table.DefaultView;
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            LoadSchedule();
        }
        private void Add_Click(object sender, RoutedEventArgs e)
        {
            var w = new ScheduleEditWindow();
            if (w.ShowDialog() == true)
                LoadSchedule();
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (dataGridSchedule.SelectedItem == null) return;

            var row = (DataRowView)dataGridSchedule.SelectedItem;
            int id = Convert.ToInt32(row["id"]);

            var w = new ScheduleEditWindow(id);
            if (w.ShowDialog() == true)
                LoadSchedule();
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (dataGridSchedule.SelectedItem == null) return;

            var row = (DataRowView)dataGridSchedule.SelectedItem;
            int id = Convert.ToInt32(row["id"]);

            var res = MessageBox.Show("Удалить запись?", "Подтверждение",
                MessageBoxButton.YesNo);

            if (res != MessageBoxResult.Yes) return;

            using (var conn = Database.GetConnection())
            {
                conn.Open();
                var cmd = new MySqlCommand("DELETE FROM schedule WHERE id=@id", conn);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }

            LoadSchedule();
        }

        private void DataGridSchedule_AutoGeneratingColumn(
            object sender,
            DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.PropertyName == "id")
            {
                e.Cancel = true;
            }
        }

    }
}