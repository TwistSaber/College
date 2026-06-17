using System;
using System.Data;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MySql.Data.MySqlClient;

namespace Collegeschedule
{
    public partial class ScheduleEditWindow : Window
    {
        private int? scheduleId;

        public ScheduleEditWindow(int? id = null)
        {
            InitializeComponent();
            scheduleId = id;

            LoadDropdowns();

            textLesson.Text = "1";

            startHour.Text = "08";
            startMinute.Text = "30";
            endHour.Text = "10";
            endMinute.Text = "20";

            if (scheduleId.HasValue)
                LoadData();
        }

        private void LoadDropdowns()
        {
            using (var conn = Database.GetConnection())
            {
                conn.Open();

                var da = new MySqlDataAdapter(
                    "SELECT id, CONCAT(name, ' (', course, ' курс)') AS displayName FROM student_groups",
                    conn);

                var dt = new DataTable();
                da.Fill(dt);

                comboBoxGroup.DisplayMemberPath = "displayName";
                comboBoxGroup.SelectedValuePath = "id";
                comboBoxGroup.ItemsSource = dt.DefaultView;

                da = new MySqlDataAdapter("SELECT id, name FROM subjects", conn);
                dt = new DataTable();
                da.Fill(dt);

                comboBoxSubject.DisplayMemberPath = "name";
                comboBoxSubject.SelectedValuePath = "id";
                comboBoxSubject.ItemsSource = dt.DefaultView;

                da = new MySqlDataAdapter("SELECT id, full_name FROM teachers", conn);
                dt = new DataTable();
                da.Fill(dt);

                comboBoxTeacher.DisplayMemberPath = "full_name";
                comboBoxTeacher.SelectedValuePath = "id";
                comboBoxTeacher.ItemsSource = dt.DefaultView;
            }

            comboBoxDay.Items.Add(new ComboBoxItem("Понедельник", 1));
            comboBoxDay.Items.Add(new ComboBoxItem("Вторник", 2));
            comboBoxDay.Items.Add(new ComboBoxItem("Среда", 3));
            comboBoxDay.Items.Add(new ComboBoxItem("Четверг", 4));
            comboBoxDay.Items.Add(new ComboBoxItem("Пятница", 5));
            comboBoxDay.Items.Add(new ComboBoxItem("Суббота", 6));

            comboBoxDay.SelectedIndex = 0;
        }

        private void LoadData()
        {
            using (var conn = Database.GetConnection())
            {
                conn.Open();

                var cmd = new MySqlCommand("SELECT * FROM schedule WHERE id=@id", conn);
                cmd.Parameters.AddWithValue("@id", scheduleId);

                using (var r = cmd.ExecuteReader())
                {
                    if (r.Read())
                    {
                        comboBoxGroup.SelectedValue = r.GetInt32("student_group_id");
                        comboBoxSubject.SelectedValue = r.GetInt32("subject_id");
                        comboBoxTeacher.SelectedValue = r.GetInt32("teacher_id");
                        comboBoxDay.SelectedIndex = r.GetInt32("day_of_week") - 1;

                        textLesson.Text = r.GetInt32("lesson_number").ToString();

                        var tsStart = r.GetTimeSpan("start_time");
                        startHour.Text = tsStart.Hours.ToString("D2");
                        startMinute.Text = tsStart.Minutes.ToString("D2");

                        var tsEnd = r.GetTimeSpan("end_time");
                        endHour.Text = tsEnd.Hours.ToString("D2");
                        endMinute.Text = tsEnd.Minutes.ToString("D2");

                        textAuditory.Text = r.IsDBNull(r.GetOrdinal("auditory"))
                            ? ""
                            : r.GetString("auditory");
                    }
                }
            }
        }

        private void NumberOnly(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, "[0-9]");
        }

        private void FixTime(object sender, RoutedEventArgs e)
        {
            var tb = sender as TextBox;

            if (string.IsNullOrWhiteSpace(tb.Text))
                return;

            if (int.TryParse(tb.Text, out int val))
                tb.Text = val.ToString("D2");
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int groupId = Convert.ToInt32(comboBoxGroup.SelectedValue);
                int subjectId = Convert.ToInt32(comboBoxSubject.SelectedValue);
                int teacherId = Convert.ToInt32(comboBoxTeacher.SelectedValue);
                int day = ((ComboBoxItem)comboBoxDay.SelectedItem).Value;

                int lessonNumber = int.Parse(textLesson.Text);

                int sh = int.Parse(startHour.Text);
                int sm = int.Parse(startMinute.Text);
                int eh = int.Parse(endHour.Text);
                int em = int.Parse(endMinute.Text);

                if (sh < 0 || sh > 23 || eh < 0 || eh > 23 ||
                    sm < 0 || sm > 59 || em < 0 || em > 59)
                {
                    MessageBox.Show("Неверное время");
                    return;
                }

                TimeSpan start = new TimeSpan(sh, sm, 0);
                TimeSpan end = new TimeSpan(eh, em, 0);

                if (start >= end)
                {
                    MessageBox.Show("Начало должно быть раньше конца");
                    return;
                }

                string aud = textAuditory.Text.Trim();

                using (var conn = Database.GetConnection())
                {
                    conn.Open();

                    MySqlCommand cmd;

                    if (scheduleId.HasValue)
                    {
                        cmd = new MySqlCommand(@"
UPDATE schedule SET
subject_id=@sub, teacher_id=@t, student_group_id=@g,
day_of_week=@d, lesson_number=@ln,
start_time=@s, end_time=@e, auditory=@a
WHERE id=@id", conn);

                        cmd.Parameters.AddWithValue("@id", scheduleId);
                    }
                    else
                    {
                        cmd = new MySqlCommand(@"
INSERT INTO schedule
(subject_id, teacher_id, student_group_id, day_of_week,
lesson_number, start_time, end_time, auditory)
VALUES (@sub,@t,@g,@d,@ln,@s,@e,@a)", conn);
                    }

                    cmd.Parameters.AddWithValue("@sub", subjectId);
                    cmd.Parameters.AddWithValue("@t", teacherId);
                    cmd.Parameters.AddWithValue("@g", groupId);
                    cmd.Parameters.AddWithValue("@d", day);
                    cmd.Parameters.AddWithValue("@ln", lessonNumber);
                    cmd.Parameters.AddWithValue("@s", start);
                    cmd.Parameters.AddWithValue("@e", end);
                    cmd.Parameters.AddWithValue("@a", aud);

                    cmd.ExecuteNonQuery();
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message);
            }
        }
    }

    
}