using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;

namespace Automate_Whatsapp.Logic
{
    public class WhatsAppSender
    {
        private readonly IWebDriver driver;
        private readonly string sessionPath = "ChromeUserData"; // Carpeta local para mantener sesión

        public WhatsAppSender()
        {
            ChromeOptions options = new ChromeOptions();
            string fullPath = Path.GetFullPath(sessionPath);
            Directory.CreateDirectory(fullPath); // Asegurar que exista

            options.AddArgument("--user-data-dir=" + fullPath);
            options.AddArgument("--profile-directory=Default");
            options.AddArgument("--start-maximized");
            var chromeService = ChromeDriverService.CreateDefaultService();
            chromeService.HideCommandPromptWindow = true;

            driver = new ChromeDriver(chromeService, options);
            driver.Navigate().GoToUrl("https://web.whatsapp.com");

            Console.WriteLine("Escanea el código QR si es la primera vez, luego presiona Enter...");
            Console.ReadLine(); // Esperar que el usuario confirme que ya está logueado
        }

        private static readonly string[] InvalidPhoneMarkers =
            {
                "no es válido", "no es valido", "inválido", "invalido", "not valid"
            };

        private bool TryDismissInvalidPhoneDialog(out string dialogText)
        {
            dialogText = "";

            var dialogs = driver.FindElements(By.XPath("//div[@role='dialog']"));
            foreach (var dlg in dialogs)
            {
                var text = (dlg.Text ?? "").Trim();
                if (InvalidPhoneMarkers.Any(m => text.IndexOf(m, StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    dialogText = text;

                    // Clic en OK para que no bloquee el siguiente envío
                    var okBtn = dlg.FindElements(By.XPath(".//*[self::button or @role='button' or @tabindex='0'][normalize-space()='OK' or .//*[normalize-space()='OK']]"))
                                   .FirstOrDefault();
                    okBtn?.Click();

                    return true;
                }
            }

            return false;
        }

        public bool TryOpenChat(string phone, out string error)
        {
            string localError = "";   // <- variable local (NO out)
            error = "";

            string url = $"https://web.whatsapp.com/send?phone={phone}&text=&app_absent=0";
            driver.Navigate().GoToUrl(url);

            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(12));

            try
            {
                var ok = wait.Until(d =>
                {
                    if (TryDismissInvalidPhoneDialog(out var dlgText))
                    {
                        localError = dlgText;   // <- aquí sí puedes asignar
                        return true;            // terminó, pero inválido
                    }

                    var msgBoxes = d.FindElements(By.XPath("//footer//div[@contenteditable='true' and @data-tab]"));
                    if (msgBoxes.Count > 0) return true; // terminó, válido

                    return false; // seguir esperando
                });

                error = localError;
                return ok && string.IsNullOrEmpty(localError);
            }
            catch (WebDriverTimeoutException)
            {
                error = "Timeout abriendo el chat (posible número inválido o WhatsApp no listo).";
                return false;
            }
        }


        public void SendMessage(string phone, string message, bool isAudio, string audioPath = "")
        {
            string url = $"https://web.whatsapp.com/send?phone={phone}&text=&app_absent=0";
            driver.Navigate().GoToUrl(url);

            WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(15));

            try
            {
                // Esperar a que cargue el campo de mensaje
                var messageBox = wait.Until(ExpectedConditions.ElementExists(
                    By.XPath("//footer//div[@contenteditable='true' and @data-tab]")));

                Thread.Sleep(1000); // Pausa breve por estabilidad

                if (isAudio)
                {
                    SendAudio(audioPath);
                }
                else
                {
                    messageBox.SendKeys(message + OpenQA.Selenium.Keys.Enter);
                }

                Thread.Sleep(2000); // Espera después de enviar
            }
            catch (WebDriverTimeoutException)
            {
                Console.WriteLine($"❌ No se pudo cargar la conversación con {phone}. Verifica si el número es válido o si el QR está escaneado.");
            }
        }

        public void SendAudio(string audioPath)
        {
            try
            {
                WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(15));

                // Asegúrate de que el header está visible (el chat cargó)
                wait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector("header")));

                // Paso 1: Hacer clic en el ícono del clip (adjuntar)
                var attachBtn = wait.Until(ExpectedConditions.ElementToBeClickable(By.CssSelector("span[data-icon='plus-rounded']")));
                attachBtn.Click();
                Thread.Sleep(1000); // Esperar a que se abran las opciones

                var audioOption = wait.Until(ExpectedConditions.ElementToBeClickable(
                    By.XPath("//li[@role='button' and @data-animate-dropdown-item='true' and .//span[normalize-space()='Audio']]")
                // Alternativa:
                // By.XPath("//li[@role='button' and .//svg[.//title[normalize-space()='ic-headphones-filled']]]")
                ));
                audioOption.Click();

                // 3) Esperar el input correcto (accept contiene 'audio')
                IWebElement audioInput = wait.Until(d =>
                {
                    var inputs = d.FindElements(By.CssSelector("input[type='file']"));
                    return inputs.FirstOrDefault(i => (i.GetAttribute("accept") ?? "").Contains("audio"));
                });

                if (audioInput == null)
                {
                    Console.WriteLine("❌ No se encontró input para adjuntar audio (accept*=audio).");
                    return;
                }

                // Paso 3: Asignar el path del archivo
                audioInput.SendKeys(Path.GetFullPath(audioPath));
                Thread.Sleep(2000); // Esperar a que cargue el preview

                // Paso 4: Enviar
                var sendBtn = wait.Until(ExpectedConditions.ElementToBeClickable(By.CssSelector("span[data-icon='wds-ic-send-filled']")));
                sendBtn.Click();
            }
            catch (WebDriverTimeoutException ex)
            {
                Console.WriteLine("❌ Tiempo de espera agotado al buscar elementos de envío de audio.");
                Console.WriteLine(ex.Message);
            }
            catch (NoSuchElementException ex)
            {
                Console.WriteLine("❌ No se encontró un elemento esperado durante el envío de audio.");
                Console.WriteLine(ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Error inesperado al enviar audio:");
                Console.WriteLine(ex.Message);
            }
        }



        public void Close()
        {
            driver.Quit();
        }
    }
}
