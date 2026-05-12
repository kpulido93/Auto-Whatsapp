using Automate_Whatsapp.Logic;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Automate_Whatsapp
{
    public partial class Form1 : Form
    {
        private WhatsAppSender whatsSender;

        private string excelPath = "";

        private System.Windows.Forms.Timer schedulerTimer = null!;
        private DateTime scheduledTime;
        private bool isPaused = false;
        private bool isScheduled = false;

        public Form1()
        {
            InitializeComponent();

            whatsSender = new WhatsAppSender();
        }

        private void btnSelectFile_Click(object sender, EventArgs e)
        {
            openFileDialog1.Filter = "Archivos Excel|*.xlsx;*.xls";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                excelPath = openFileDialog1.FileName;
                lblFilePath.Text = $"Archivo: {Path.GetFileName(excelPath)}";
                Log("Archivo Excel seleccionado correctamente.");
            }
        }

        private void BtnSchedule_Click(object sender, EventArgs e)
        {
            scheduledTime = dateTimePicker.Value;
            schedulerTimer.Start();
            isScheduled = true;
            btnPauseResume.Text = "Pausar";
        }

        private void BtnPauseResume_Click(object sender, EventArgs e)
        {
            if (!isScheduled)
                return;

            isPaused = !isPaused;
            btnPauseResume.Text = isPaused ? "Reanudar" : "Pausar";
            Log(isPaused ? "Temporizador pausado." : "Temporizador reanudado.");
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            schedulerTimer.Stop();
            isScheduled = false;
            isPaused = false;
            btnPauseResume.Text = "Pausar";
            Log("Programación cancelada.");
        }

        private static DateTime SinSegundos(DateTime dt)
            => new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, 0);

        private async void SchedulerTimer_Tick(object sender, EventArgs e)
        {
            var target = new DateTime(
                scheduledTime.Year, scheduledTime.Month, scheduledTime.Day,
                scheduledTime.Hour, scheduledTime.Minute, 0
            );

            if (isScheduled && !isPaused && DateTime.Now >= target)
            {
                schedulerTimer.Stop();

                isScheduled = true;

                var mensajes = ExcelReader.ReadMessages(excelPath);
                if (mensajes.Count == 0)
                {
                    Log("No se encontraron mensajes válidos en el Excel.");
                    return;
                }

                // Llama a EnviarMensajes en segundo plano
                await EnviarMensajesAsync(mensajes);
            }
        }


        private void btnSend_Click(object sender, EventArgs e)
        {
            scheduledTime = SinSegundos(dateTimePicker.Value);
            dateTimePicker.Value = scheduledTime;

            if (scheduledTime <= DateTime.Now)
            {
                MessageBox.Show("Por favor selecciona una fecha y hora futura.");
                return;
            }

            if (string.IsNullOrEmpty(excelPath))
            {
                MessageBox.Show("Seleccione un archivo Excel primero.", "Advertencia", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            isScheduled = true;
            isPaused = false;
            btnPauseResume.Text = "Pausar";
            schedulerTimer.Start();

            MessageBox.Show($"Mensaje programado para {scheduledTime}");
        }

        private void Log(string message)
        {
            txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
        }

        private async Task EnviarMensajesAsync(List<(string countrycode, string phone, string message, bool toAudio)> mensajes)
        {
            Invoke((Action)(() => Log("🚀 Iniciando envío de mensajes...")));
            Invoke((Action)(() => Log($"Mensajes recibidos: {mensajes.Count}")));
            string audioPath = Path.Combine("Resources", $"ptt_{Guid.NewGuid()}.ogg");

            Invoke((Action)(() => {
                progressBar1.Minimum = 0;
                progressBar1.Maximum = mensajes.Count;
                progressBar1.Value = 0;
            }));

            var tts = new ElevenLabsTts();

            int processed = 0;

            foreach (var (countrycode, phone, message, toAudio) in mensajes)
            {
                Invoke((Action)(() => Log($"🚀 Enviando mensaje a {phone}")));

                if (!isScheduled)
                {
                    Invoke((Action)(() => Log("⚠️ Envío cancelado antes de procesar el mensaje.")));
                    break;
                }

                while (isPaused)
                {
                    await Task.Delay(500);
                    if (!isScheduled) return;
                }

                try
                {
                    var fullPhone = string.Concat(countrycode, phone);

                    // ✅ Validar SIEMPRE
                    if (!whatsSender.TryOpenChat(fullPhone, out var reason))
                    {
                        Invoke((Action)(() => Log($"❌ Número inválido {fullPhone}: {reason}")));
                        continue; // igual contará en progreso gracias al finally
                    }

                    if (toAudio)
                    {
                        var audioPathMsg = Path.Combine(
                            Path.GetDirectoryName(audioPath)!,
                            $"ptt_{Guid.NewGuid()}.ogg"
                        );

                        string? resultPath = await tts.ConvertToOggAsync(message, audioPathMsg);

                        if (string.IsNullOrEmpty(resultPath))
                        {
                            Invoke((Action)(() => Log($"❌ No se pudo generar el audio para {fullPhone}")));
                            continue;
                        }

                        whatsSender.SendAudio(resultPath);
                        Invoke((Action)(() => Log($"✅ Mensaje de audio enviado a {fullPhone}")));
                    }
                    else
                    {
                        whatsSender.SendMessage(fullPhone, message, false);
                        Invoke((Action)(() => Log($"✅ Mensaje de texto enviado a {fullPhone}")));
                    }
                }
                catch (Exception ex)
                {
                    Invoke((Action)(() => Log($"❌ Error con {phone}: {ex.Message}")));
                }
                finally
                {
                    processed++;
                    Invoke((Action)(() =>
                    {
                        progressBar1.Value = Math.Min(processed, progressBar1.Maximum);
                    }));
                }

                await Task.Delay(2000);
            }

            if (isScheduled)
            {
                Invoke((Action)(() => {
                    MessageBox.Show("Todos los mensajes fueron procesados.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }));
            }

            isScheduled = false;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            var result = MessageBox.Show("¿Estás seguro de que deseas salir?", "Confirmar salida", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.No)
            {
                e.Cancel = true; // Cancela el cierre
            }
            else
            {
                whatsSender.Close();
            }
        }
    }
}
