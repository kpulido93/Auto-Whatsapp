using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;

namespace Automate_Whatsapp.Logic
{
    public class WhatsAppSender : IWhatsAppSender
    {
        private static readonly TimeSpan DefaultWaitTimeout = TimeSpan.FromSeconds(15);

        private readonly IWebDriver driver;

        public WhatsAppLine Line { get; }

        public WhatsAppSender(WhatsAppLine line)
        {
            Line = line ?? throw new ArgumentNullException(nameof(line));

            ChromeOptions options = new ChromeOptions();
            string fullPath = Path.GetFullPath(Line.SessionPath);
            Directory.CreateDirectory(fullPath); // Asegurar que exista

            options.AddArgument("--user-data-dir=" + fullPath);
            options.AddArgument("--profile-directory=" + Line.ProfileDirectory);
            options.AddArgument("--start-maximized");
            var chromeService = ChromeDriverService.CreateDefaultService();
            chromeService.HideCommandPromptWindow = true;

            driver = new ChromeDriver(chromeService, options);
            driver.Navigate().GoToUrl("https://web.whatsapp.com");
        }

        private static class Selectors
        {
            public const string MessageBoxXPath = "//footer//div[@contenteditable='true' and @data-tab]";
            public const string InvalidPhoneDialogXPath = "//div[@role='dialog']";
            public const string DialogConfirmButtonXPath = ".//*[self::button or @role='button' or @tabindex='0'][normalize-space()='OK' or normalize-space()='Aceptar' or .//*[normalize-space()='OK'] or .//*[normalize-space()='Aceptar']]";
            public const string LoginRequiredCss = "canvas[aria-label*='Scan'], div[data-ref]";
            public const string ReadySidePaneCss = "#pane-side";
            public const string ChatHeaderCss = "header";
            public const string AttachButtonCss = "span[data-icon='plus-rounded'], span[data-icon='clip'], span[data-icon='attach-menu-plus']";
            public const string AudioOptionXPath = "//li[@role='button' and @data-animate-dropdown-item='true' and .//span[normalize-space()='Audio']]";
            public const string FileInputCss = "input[type='file']";
            public const string SendButtonCss = "span[data-icon='wds-ic-send-filled'], span[data-icon='send']";
        }

        private static class Markers
        {
            public static readonly string[] InvalidDestinationNumber =
            {
                "no es válido",
                "no es valido",
                "inválido",
                "invalido",
                "not valid",
                "phone number shared via url is invalid",
                "numero de telefono compartido a traves de la url no es valido"
            };

            public static readonly string[] LoginRequired =
            {
                "scan the qr code",
                "escanea el codigo qr",
                "escanea el código qr",
                "log in with phone number",
                "iniciar sesion con numero de telefono",
                "iniciar sesión con número de teléfono",
                "link a device",
                "vincular un dispositivo"
            };

            public static readonly string[] PhoneDisconnected =
            {
                "phone not connected",
                "telefono sin conexion",
                "teléfono sin conexión",
                "make sure your phone has an active internet connection",
                "asegurate de que tu telefono tenga una conexion activa",
                "asegúrate de que tu teléfono tenga una conexión activa",
                "trying to reach phone",
                "intentando conectar con el telefono",
                "intentando conectar con el teléfono"
            };

            public static readonly string[] SenderAccountBlockedOrRestricted =
            {
                "this account can no longer use whatsapp",
                "esta cuenta ya no puede usar whatsapp",
                "account has been banned",
                "cuenta suspendida",
                "temporarily banned",
                "temporalmente suspendida",
                "restricted",
                "restringida",
                "blocked",
                "bloqueada"
            };

            public static readonly string[] WhatsAppNotReady =
            {
                "loading",
                "cargando",
                "connecting",
                "conectando",
                "organizing messages",
                "sincronizando mensajes",
                "descargando mensajes",
                "whatsapp is temporarily unavailable",
                "whatsapp no esta disponible temporalmente",
                "whatsapp no está disponible temporalmente"
            };
        }

        public WhatsAppHealthStatus GetHealthStatus()
        {
            return GetHealthIssue().Status;
        }

        internal WhatsAppHealthIssue EnsureReady()
        {
            return GetHealthIssue();
        }

        public WhatsAppHealthIssue WaitForReady(TimeSpan timeout)
        {
            DateTime deadline = DateTime.UtcNow.Add(timeout);
            WhatsAppHealthIssue issue = GetHealthIssue();

            while (issue.Status == WhatsAppHealthStatus.WhatsAppNotReady && DateTime.UtcNow < deadline)
            {
                Thread.Sleep(500);
                issue = GetHealthIssue();
            }

            return issue;
        }

        public WhatsAppLineOperationalState GetOperationalState(TimeSpan timeout, out WhatsAppHealthIssue issue)
        {
            issue = WaitForReady(timeout);

            return issue.Status switch
            {
                WhatsAppHealthStatus.Ready => WhatsAppLineOperationalState.Ready,
                WhatsAppHealthStatus.LoginRequired => WhatsAppLineOperationalState.RequiresManualAuth,
                _ => WhatsAppLineOperationalState.NotAvailable
            };
        }

        public bool TryOpenChat(string phone, out string error)
        {
            var result = OpenChat(phone, TimeSpan.FromSeconds(12), out _);
            error = result.Success ? "" : result.Message;
            return result.Success;
        }

        public WhatsAppSendResult OpenChat(string phone)
        {
            return OpenChat(phone, DefaultWaitTimeout, out _);
        }

        public WhatsAppSendResult SendMessage(string phone, string message, bool isAudio, string audioPath = "")
        {
            var openResult = OpenChat(phone, DefaultWaitTimeout, out var messageBox);
            if (!openResult.Success)
            {
                return openResult;
            }

            if (isAudio)
            {
                return SendAudio(audioPath);
            }

            if (messageBox == null)
            {
                return UiFailure("No se encontro la caja de mensaje para enviar texto.");
            }

            try
            {
                Thread.Sleep(1000); // Pausa breve por estabilidad
                messageBox.SendKeys(message + OpenQA.Selenium.Keys.Enter);

                Thread.Sleep(2000); // Espera despues de enviar
                return WhatsAppSendResult.Ok($"Mensaje enviado a {phone}.");
            }
            catch (WebDriverException ex) when (IsBrowserUnavailableException(ex))
            {
                return Failure(WhatsAppHealthStatus.BrowserUnavailable, "El navegador o la sesion de WebDriver no esta disponible.", ex);
            }
            catch (WebDriverException ex)
            {
                var issue = GetHealthIssue();
                return issue.IsReady
                    ? Failure(WhatsAppHealthStatus.UnknownError, "Error de WebDriver al enviar el mensaje.", ex)
                    : WhatsAppSendResult.FromIssue(issue, ex);
            }
            catch (Exception ex)
            {
                return Failure(WhatsAppHealthStatus.UnknownError, "Error inesperado al enviar el mensaje.", ex);
            }
        }

        public WhatsAppSendResult SendAudio(string audioPath)
        {
            try
            {
                var health = GetHealthIssue();
                if (!health.IsReady)
                {
                    return WhatsAppSendResult.FromIssue(health);
                }

                string fullAudioPath = Path.GetFullPath(audioPath);
                if (!File.Exists(fullAudioPath))
                {
                    return WhatsAppSendResult.Failure(
                        WhatsAppHealthStatus.UnknownError,
                        $"No existe el archivo de audio: {fullAudioPath}",
                        false);
                }

                WebDriverWait wait = new WebDriverWait(driver, DefaultWaitTimeout);

                IWebElement? messageBox;
                try
                {
                    messageBox = wait.Until(d => FindMessageBox(d));
                }
                catch (WebDriverTimeoutException ex)
                {
                    return UiFailure("No se encontro la caja de mensaje antes de adjuntar audio.", ex);
                }

                if (messageBox == null)
                {
                    return UiFailure("No se encontro la caja de mensaje antes de adjuntar audio.");
                }

                try
                {
                    wait.Until(ExpectedConditions.ElementIsVisible(By.CssSelector(Selectors.ChatHeaderCss)));
                }
                catch (WebDriverTimeoutException ex)
                {
                    return UiFailure("No se encontro el encabezado del chat antes de adjuntar audio.", ex);
                }

                IWebElement attachBtn;
                try
                {
                    attachBtn = wait.Until(ExpectedConditions.ElementToBeClickable(By.CssSelector(Selectors.AttachButtonCss)));
                }
                catch (WebDriverTimeoutException ex)
                {
                    return UiFailure("No se encontro el boton de adjuntar audio.", ex);
                }

                attachBtn.Click();
                Thread.Sleep(1000); // Esperar a que se abran las opciones

                IWebElement audioOption;
                try
                {
                    audioOption = wait.Until(ExpectedConditions.ElementToBeClickable(By.XPath(Selectors.AudioOptionXPath)));
                }
                catch (WebDriverTimeoutException ex)
                {
                    return UiFailure("No se encontro la opcion Audio en el menu de adjuntos.", ex);
                }

                audioOption.Click();

                IWebElement? audioInput;
                try
                {
                    audioInput = wait.Until(d => FindAudioInput(d));
                }
                catch (WebDriverTimeoutException ex)
                {
                    return UiFailure("No se encontro input para adjuntar audio.", ex);
                }

                if (audioInput == null)
                {
                    return UiFailure("No se encontro input para adjuntar audio.");
                }

                audioInput.SendKeys(fullAudioPath);
                Thread.Sleep(2000); // Esperar a que cargue el preview

                IWebElement sendBtn;
                try
                {
                    sendBtn = wait.Until(ExpectedConditions.ElementToBeClickable(By.CssSelector(Selectors.SendButtonCss)));
                }
                catch (WebDriverTimeoutException ex)
                {
                    return UiFailure("No se encontro el boton de enviar audio.", ex);
                }

                sendBtn.Click();

                Thread.Sleep(2000);
                return WhatsAppSendResult.Ok("Audio enviado correctamente.");
            }
            catch (WebDriverException ex) when (IsBrowserUnavailableException(ex))
            {
                return Failure(WhatsAppHealthStatus.BrowserUnavailable, "El navegador o la sesion de WebDriver no esta disponible.", ex);
            }
            catch (WebDriverException ex)
            {
                var issue = GetHealthIssue();
                return issue.IsReady
                    ? Failure(WhatsAppHealthStatus.UnknownError, "Error de WebDriver al enviar el audio.", ex)
                    : WhatsAppSendResult.FromIssue(issue, ex);
            }
            catch (Exception ex)
            {
                return Failure(WhatsAppHealthStatus.UnknownError, "Error inesperado al enviar audio.", ex);
            }
        }

        private WhatsAppSendResult OpenChat(string phone, TimeSpan timeout, out IWebElement? messageBox)
        {
            messageBox = null;

            try
            {
                driver.Navigate().GoToUrl(BuildChatUrl(phone));

                var wait = new WebDriverWait(driver, timeout);
                string invalidDialogText = "";
                IWebElement? foundMessageBox = null;

                wait.Until(d =>
                {
                    if (TryDismissInvalidPhoneDialog(d, out var dlgText))
                    {
                        invalidDialogText = dlgText;
                        return true;
                    }

                    foundMessageBox = FindMessageBox(d);
                    return foundMessageBox != null;
                });

                if (!string.IsNullOrWhiteSpace(invalidDialogText))
                {
                    return WhatsAppSendResult.Failure(
                        WhatsAppHealthStatus.InvalidDestinationNumber,
                        invalidDialogText,
                        false);
                }

                if (foundMessageBox == null)
                {
                    return UiFailure("No se encontro la caja de mensaje para abrir el chat.");
                }

                messageBox = foundMessageBox;
                return WhatsAppSendResult.Ok("Chat listo para enviar.");
            }
            catch (WebDriverTimeoutException ex)
            {
                if (TryDismissInvalidPhoneDialog(out var dialogText))
                {
                    return WhatsAppSendResult.Failure(
                        WhatsAppHealthStatus.InvalidDestinationNumber,
                        dialogText,
                        false,
                        ex);
                }

                var issue = GetHealthIssue();
                return issue.IsReady
                    ? UiFailure("No se encontro la caja de mensaje del chat.", ex)
                    : WhatsAppSendResult.FromIssue(issue, ex);
            }
            catch (WebDriverException ex) when (IsBrowserUnavailableException(ex))
            {
                return Failure(WhatsAppHealthStatus.BrowserUnavailable, "El navegador o la sesion de WebDriver no esta disponible.", ex);
            }
            catch (WebDriverException ex)
            {
                var issue = GetHealthIssue();
                return issue.IsReady
                    ? Failure(WhatsAppHealthStatus.UnknownError, "Error de WebDriver al abrir el chat.", ex)
                    : WhatsAppSendResult.FromIssue(issue, ex);
            }
            catch (Exception ex)
            {
                return Failure(WhatsAppHealthStatus.UnknownError, "Error inesperado al abrir el chat.", ex);
            }
        }

        private WhatsAppHealthIssue GetHealthIssue()
        {
            try
            {
                EnsureBrowserResponding();

                string pageText = GetPageText();

                if (ContainsAny(pageText, Markers.SenderAccountBlockedOrRestricted))
                {
                    return Issue(WhatsAppHealthStatus.SenderAccountBlockedOrRestricted, "La cuenta remitente parece bloqueada o restringida.");
                }

                if (ContainsAny(pageText, Markers.PhoneDisconnected))
                {
                    return Issue(WhatsAppHealthStatus.PhoneDisconnected, "WhatsApp Web informa que el telefono esta desconectado.");
                }

                if (ContainsAny(pageText, Markers.LoginRequired) || HasAnyElement(By.CssSelector(Selectors.LoginRequiredCss)))
                {
                    return Issue(WhatsAppHealthStatus.LoginRequired, "WhatsApp Web requiere iniciar sesion o escanear el codigo QR.");
                }

                if (HasMessageBox() || HasAnyElement(By.CssSelector(Selectors.ReadySidePaneCss)))
                {
                    return Issue(WhatsAppHealthStatus.Ready, "WhatsApp Web esta listo.");
                }

                if (ContainsAny(pageText, Markers.WhatsAppNotReady))
                {
                    return Issue(WhatsAppHealthStatus.WhatsAppNotReady, "WhatsApp Web aun no esta listo.");
                }

                return Issue(WhatsAppHealthStatus.WhatsAppNotReady, "No se detectaron controles listos de WhatsApp Web.");
            }
            catch (WebDriverException ex) when (IsBrowserUnavailableException(ex))
            {
                return Issue(WhatsAppHealthStatus.BrowserUnavailable, "El navegador o la sesion de WebDriver no esta disponible.", ex);
            }
            catch (WebDriverException ex)
            {
                return Issue(WhatsAppHealthStatus.UnknownError, "Error de WebDriver al inspeccionar WhatsApp Web.", ex);
            }
            catch (Exception ex)
            {
                return Issue(WhatsAppHealthStatus.UnknownError, "Error inesperado al inspeccionar WhatsApp Web.", ex);
            }
        }

        private bool TryDismissInvalidPhoneDialog(out string dialogText)
        {
            return TryDismissInvalidPhoneDialog(driver, out dialogText);
        }

        private static bool TryDismissInvalidPhoneDialog(ISearchContext searchContext, out string dialogText)
        {
            dialogText = "";

            var dialogs = searchContext.FindElements(By.XPath(Selectors.InvalidPhoneDialogXPath));
            foreach (var dlg in dialogs)
            {
                string text;
                try
                {
                    text = (dlg.Text ?? "").Trim();
                }
                catch (StaleElementReferenceException)
                {
                    continue;
                }

                if (ContainsAny(text, Markers.InvalidDestinationNumber))
                {
                    dialogText = text;

                    // Clic en OK para que no bloquee el siguiente envio.
                    try
                    {
                        var okBtn = dlg.FindElements(By.XPath(Selectors.DialogConfirmButtonXPath))
                                       .FirstOrDefault();
                        okBtn?.Click();
                    }
                    catch (WebDriverException)
                    {
                        // El resultado invalido ya esta clasificado; no se debe bloquear por no poder cerrar el dialogo.
                    }

                    return true;
                }
            }

            return false;
        }

        private static string BuildChatUrl(string phone)
        {
            return $"https://web.whatsapp.com/send?phone={Uri.EscapeDataString(phone)}&text=&app_absent=0";
        }

        private static IWebElement? FindMessageBox(ISearchContext searchContext)
        {
            return FirstDisplayed(searchContext.FindElements(By.XPath(Selectors.MessageBoxXPath)));
        }

        private static IWebElement? FindAudioInput(ISearchContext searchContext)
        {
            return searchContext.FindElements(By.CssSelector(Selectors.FileInputCss))
                .FirstOrDefault(input => (input.GetAttribute("accept") ?? "").Contains("audio", StringComparison.OrdinalIgnoreCase));
        }

        private bool HasMessageBox()
        {
            return FindMessageBox(driver) != null;
        }

        private bool HasAnyElement(By by)
        {
            return driver.FindElements(by).Count > 0;
        }

        private string GetPageText()
        {
            try
            {
                return driver.FindElement(By.TagName("body")).Text ?? "";
            }
            catch (NoSuchElementException)
            {
                return "";
            }
        }

        private void EnsureBrowserResponding()
        {
            _ = driver.WindowHandles;
        }

        private static IWebElement? FirstDisplayed(IEnumerable<IWebElement> elements)
        {
            foreach (var element in elements)
            {
                try
                {
                    if (element.Displayed)
                    {
                        return element;
                    }
                }
                catch (StaleElementReferenceException)
                {
                }
            }

            return null;
        }

        private static bool ContainsAny(string text, IEnumerable<string> markers)
        {
            return markers.Any(marker => text.IndexOf(marker, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static WhatsAppHealthIssue Issue(WhatsAppHealthStatus status, string message, Exception? exception = null)
        {
            return new WhatsAppHealthIssue(status, message, IsGlobalFailure(status), exception);
        }

        private static WhatsAppSendResult Failure(WhatsAppHealthStatus status, string message, Exception? exception = null)
        {
            return WhatsAppSendResult.Failure(status, message, IsGlobalFailure(status), exception);
        }

        private WhatsAppSendResult UiFailure(string message, Exception? exception = null)
        {
            var issue = GetHealthIssue();
            return issue.IsReady
                ? WhatsAppSendResult.Failure(WhatsAppHealthStatus.WhatsAppNotReady, message, true, exception)
                : WhatsAppSendResult.FromIssue(issue, exception);
        }

        private static bool IsGlobalFailure(WhatsAppHealthStatus status)
        {
            return status switch
            {
                WhatsAppHealthStatus.Ready => false,
                WhatsAppHealthStatus.InvalidDestinationNumber => false,
                _ => true
            };
        }

        private static bool IsBrowserUnavailableException(WebDriverException ex)
        {
            string message = ex.Message ?? "";

            return ex is NoSuchWindowException
                || message.Contains("invalid session id", StringComparison.OrdinalIgnoreCase)
                || message.Contains("chrome not reachable", StringComparison.OrdinalIgnoreCase)
                || message.Contains("disconnected", StringComparison.OrdinalIgnoreCase)
                || message.Contains("no such window", StringComparison.OrdinalIgnoreCase)
                || message.Contains("target window already closed", StringComparison.OrdinalIgnoreCase)
                || message.Contains("session deleted", StringComparison.OrdinalIgnoreCase);
        }

        public void Close()
        {
            try
            {
                driver.Quit();
            }
            catch (WebDriverException)
            {
            }
        }
    }
}
