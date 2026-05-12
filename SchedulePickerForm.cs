namespace Automate_Whatsapp
{
    public partial class SchedulePickerForm : Form
    {
        public DateTime SelectedDateTime { get; private set; }

        public SchedulePickerForm(DateTime initialDateTime)
        {
            InitializeComponent();
            InitializeValues(initialDateTime);
        }

        private void InitializeValues(DateTime initialDateTime)
        {
            var initialValue = initialDateTime <= DateTime.Now
                ? DateTime.Now.AddMinutes(5)
                : initialDateTime;

            initialValue = new DateTime(
                initialValue.Year,
                initialValue.Month,
                initialValue.Day,
                initialValue.Hour,
                initialValue.Minute,
                0);

            monthCalendarSchedule.MinDate = DateTime.Today;
            monthCalendarSchedule.SelectionStart = initialValue.Date;
            monthCalendarSchedule.SelectionEnd = initialValue.Date;
            numScheduleHour.Value = initialValue.Hour;
            numScheduleMinute.Value = initialValue.Minute;
            SelectedDateTime = initialValue;
            UpdateSummary();
        }

        private DateTime BuildSelectedDateTime()
        {
            var selectedDate = monthCalendarSchedule.SelectionStart.Date;
            return new DateTime(
                selectedDate.Year,
                selectedDate.Month,
                selectedDate.Day,
                (int)numScheduleHour.Value,
                (int)numScheduleMinute.Value,
                0);
        }

        private void UpdateSummary()
        {
            lblSummary.Text = $"Seleccionado: {BuildSelectedDateTime():dd/MM/yyyy HH:mm}";
        }

        private void monthCalendarSchedule_DateChanged(object sender, DateRangeEventArgs e)
        {
            UpdateSummary();
        }

        private void scheduleTime_ValueChanged(object sender, EventArgs e)
        {
            UpdateSummary();
        }

        private void btnAccept_Click(object sender, EventArgs e)
        {
            var selectedValue = BuildSelectedDateTime();
            if (selectedValue <= DateTime.Now)
            {
                MessageBox.Show(
                    "Selecciona una fecha y hora futura para programar el envío.",
                    "Hora no válida",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            SelectedDateTime = selectedValue;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
