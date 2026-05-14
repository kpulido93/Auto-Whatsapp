using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using FormsClipboard = System.Windows.Forms.Clipboard;
using FormsSendKeys = System.Windows.Forms.SendKeys;

namespace Automate_Whatsapp.Logic
{
    public class WhatsAppSender : IWhatsAppSender
    {
        private static readonly TimeSpan DefaultWaitTimeout = TimeSpan.FromSeconds(15);
        private const int MaxChromeStartupAttempts = 3;
        private static readonly string[] ChromeStartupTransientMarkers =
        {
            "session not created",
            "DevToolsActivePort file doesn't exist",
            "Chrome failed to start",
            "user data directory is already in use"
        };
        private static readonly string[] AudioInputAcceptMarkers =
        {
            "audio",
            ".ogg",
            ".opus",
            ".mp3",
            ".m4a",
            ".wav"
        };
        private const int NativeWindowTextMaxLength = 256;
        private const int WmKeyDown = 0x0100;
        private const int WmKeyUp = 0x0101;
        private const int WmClose = 0x0010;
        private const int VkEscape = 0x1B;

        private static class AudioAttachStage
        {
            public const string OpenMenu = "AudioAttach.OpenMenu";
            public const string FindInputBeforeClick = "AudioAttach.FindInputBeforeClick";
            public const string ClickAudioOption = "AudioAttach.ClickAudioOption";
            public const string FindInputAfterClick = "AudioAttach.FindInputAfterClick";
            public const string NativeDialogUpload = "AudioAttach.NativeDialogUpload";
            public const string WaitPreview = "AudioAttach.WaitPreview";
            public const string ClickPreviewSend = "AudioAttach.ClickPreviewSend";
            public const string WaitPreviewClose = "AudioAttach.WaitPreviewClose";
            public const string Done = "AudioAttach.Done";
        }

        private readonly IWebDriver driver;
        private readonly Action<string> log;
        private IWebElement? activeAttachmentPreview;

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool PostMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        public WhatsAppLine Line { get; }

        public WhatsAppSender(WhatsAppLine line, Action<string>? log = null)
        {
            Line = line ?? throw new ArgumentNullException(nameof(line));
            this.log = log ?? (_ => { });

            driver = CreateChromeDriverWithRetry(
                Line,
                this.log,
                (service, options) => new ChromeDriver(service, options),
                Thread.Sleep);
            driver.Navigate().GoToUrl("https://web.whatsapp.com");
        }

        public static TDriver CreateChromeDriverWithRetry<TDriver>(
            WhatsAppLine line,
            Action<string> log,
            Func<ChromeDriverService, ChromeOptions, TDriver> driverFactory,
            Action<TimeSpan>? delay = null)
        {
            ArgumentNullException.ThrowIfNull(line);
            ArgumentNullException.ThrowIfNull(log);
            ArgumentNullException.ThrowIfNull(driverFactory);

            delay ??= Thread.Sleep;

            string userDataDir = Path.GetFullPath(line.SessionPath);
            string profileDirectory = string.IsNullOrWhiteSpace(line.ProfileDirectory)
                ? "Default"
                : line.ProfileDirectory.Trim();
            Directory.CreateDirectory(userDataDir);

            Exception? lastTransientException = null;

            for (int attempt = 1; attempt <= MaxChromeStartupAttempts; attempt++)
            {
                log($"Abriendo Chrome para {line.DisplayName}. Intento {attempt}/{MaxChromeStartupAttempts}. user-data-dir: {userDataDir}. profile-directory: {profileDirectory}.");

                var chromeService = ChromeDriverService.CreateDefaultService();
                chromeService.HideCommandPromptWindow = true;

                try
                {
                    return driverFactory(chromeService, BuildChromeOptions(userDataDir, profileDirectory));
                }
                catch (WebDriverException ex) when (IsTransientChromeStartupException(ex))
                {
                    chromeService.Dispose();
                    lastTransientException = ex;
                    string errorSummary = SummarizeExceptionMessage(ex);
                    log($"Fallo transitorio al abrir Chrome. Intento {attempt}/{MaxChromeStartupAttempts}. user-data-dir: {userDataDir}. profile-directory: {profileDirectory}. Error: {errorSummary}");

                    if (attempt == MaxChromeStartupAttempts)
                    {
                        break;
                    }

                    TimeSpan backoff = TimeSpan.FromMilliseconds(500 * attempt);
                    log($"Reintentando apertura de Chrome para {line.DisplayName} en {backoff.TotalSeconds:0.0}s.");
                    delay(backoff);
                }
                catch
                {
                    chromeService.Dispose();
                    throw;
                }
            }

            string finalMessage =
                $"No se pudo abrir Chrome despues de {MaxChromeStartupAttempts} intentos. " +
                "Cierra ventanas de Chrome abiertas con esta línea o cambia la ruta de sesión. " +
                $"user-data-dir: {userDataDir}. profile-directory: {profileDirectory}. " +
                $"Ultimo error: {SummarizeExceptionMessage(lastTransientException)}";

            log(finalMessage);
            throw new WebDriverException(finalMessage, lastTransientException);
        }

        private static ChromeOptions BuildChromeOptions(string userDataDir, string profileDirectory)
        {
            ChromeOptions options = new ChromeOptions();
            options.AddArgument("--user-data-dir=" + userDataDir);
            options.AddArgument("--profile-directory=" + profileDirectory);
            options.AddArgument("--start-maximized");
            options.AddArgument("--no-first-run");
            options.AddArgument("--no-default-browser-check");
            options.AddArgument("--disable-popup-blocking");
            options.AddArgument("--remote-allow-origins=*");
            return options;
        }

        private static class Selectors
        {
            private const string XPathUppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZÁÉÍÓÚÜÑ";
            private const string XPathLowercase = "abcdefghijklmnopqrstuvwxyzáéíóúüñ";
            private const string MessageBoxAriaText = "normalize-space(translate(concat(@aria-label, ' ', @aria-placeholder), '" + XPathUppercase + "', '" + XPathLowercase + "'))";
            public const string InvalidPhoneDialogXPath = "//div[@role='dialog']";
            public const string DialogConfirmButtonXPath = ".//*[self::button or @role='button' or @tabindex='0'][normalize-space()='OK' or normalize-space()='Aceptar' or .//*[normalize-space()='OK'] or .//*[normalize-space()='Aceptar']]";
            public const string LoginRequiredCss = "canvas[aria-label*='Scan'], div[data-ref]";
            public const string ReadySidePaneCss = "#pane-side";
            public const string ChatHeaderCss = "header";
            public const string AttachButtonCss = "span[data-icon='plus-rounded'], span[data-icon='clip'], span[data-icon='attach-menu-plus']";
            public const string AttachmentMenuIndicatorCss = "[role='menu']";
            public const string FileInputCss = "input[type='file']";
            public const string AttachmentPreviewDialogXPath = "//div[@role='dialog']";
            public const string AudioVisibleTextXPath = "//*[normalize-space(translate(., '" + XPathUppercase + "', '" + XPathLowercase + "'))='audio']";
            public const string AudioHeadphonesIconXPath = "//*[local-name()='title' and normalize-space()='ic-headphones-filled']";
            private const string ClickableAudioOptionXPath = "self::li or self::div or self::button or @role='button' or @tabindex";
            private const string EnabledClickableAudioOptionXPath = "(" + ClickableAudioOptionXPath + ") and not(@aria-disabled='true')";
            private const string AudioTextConditionXPath = "normalize-space(translate(., '" + XPathUppercase + "', '" + XPathLowercase + "'))='audio'";
            private const string PreviewElementText = "normalize-space(translate(concat(@aria-label, ' ', @title, ' ', @data-icon), '" + XPathUppercase + "', '" + XPathLowercase + "'))";
            public const string AttachmentPreviewPanelXPath = "//*[self::div or @role='dialog'][not(self::footer) and not(ancestor::footer) and not(.//footer) and .//*[@data-icon='wds-ic-send-filled' or @data-icon='send']]";
            public const string AttachmentPreviewControlsXPath = ".//*[self::audio or self::video or self::canvas or self::img or @contenteditable='true' or @role='textbox' or contains(" + PreviewElementText + ", 'caption') or contains(" + PreviewElementText + ", 'pie de foto') or contains(" + PreviewElementText + ", 'adjunto') or contains(" + PreviewElementText + ", 'archivo') or contains(" + PreviewElementText + ", 'document') or contains(" + PreviewElementText + ", 'audio')]";
            public const string PreviewSendButtonXPath = ".//*[self::button or @role='button' or @tabindex='0'][not(ancestor::footer) and (.//*[@data-icon='wds-ic-send-filled' or @data-icon='send'] or @data-icon='wds-ic-send-filled' or @data-icon='send' or contains(" + PreviewElementText + ", 'send') or contains(" + PreviewElementText + ", 'enviar')) and not(@aria-disabled='true')]";

            public static readonly By[] MessageBoxLocators =
            {
                By.CssSelector("footer div[contenteditable='true']"),
                By.CssSelector("footer [role='textbox'][contenteditable='true']"),
                By.XPath("//footer//div[@contenteditable='true' and not(@aria-disabled='true')]"),
                By.XPath("//footer//*[@role='textbox' and @contenteditable='true' and not(@aria-disabled='true')]"),
                By.XPath("//*[@contenteditable='true' and (@role='textbox' or self::div) and (" +
                         "contains(" + MessageBoxAriaText + ", 'escribe un mensaje') or " +
                         "contains(" + MessageBoxAriaText + ", 'type a message') or " +
                         MessageBoxAriaText + "='mensaje' or " +
                         MessageBoxAriaText + "='message')]"),
                By.XPath("//footer//*[@contenteditable='true' and (" +
                         "contains(" + MessageBoxAriaText + ", 'mensaje') or " +
                         "contains(" + MessageBoxAriaText + ", 'message'))]"),
                By.XPath("//footer//div[@contenteditable='true' and @data-tab]")
            };

            public static readonly By[] AudioOptionLocators =
            {
                By.XPath(AudioVisibleTextXPath + "/ancestor-or-self::*[" + EnabledClickableAudioOptionXPath + "][1]"),
                By.XPath("//*[" + EnabledClickableAudioOptionXPath + " and (" + AudioTextConditionXPath + " or .//*[" + AudioTextConditionXPath + "])]"),
                By.XPath(AudioHeadphonesIconXPath + "/ancestor::*[" + EnabledClickableAudioOptionXPath + "][1]"),
                By.XPath("//*[" + EnabledClickableAudioOptionXPath + " and .//*[local-name()='title' and normalize-space()='ic-headphones-filled']]"),
                By.XPath("//li[@role='button' and @data-animate-dropdown-item='true' and .//span[normalize-space()='Audio']]")
            };
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
            WebDriverWait? attachmentWait = null;

            try
            {
                var health = GetHealthIssue();
                if (!health.IsReady)
                {
                    return WhatsAppSendResult.FromIssue(health);
                }

                if (string.IsNullOrWhiteSpace(audioPath))
                {
                    return AudioFileFailure("La ruta del archivo de audio esta vacia. Revisa la generacion TTS antes de adjuntar.");
                }

                string fullAudioPath;
                try
                {
                    fullAudioPath = Path.GetFullPath(audioPath);
                }
                catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
                {
                    return AudioFileFailure($"La ruta del archivo de audio no es valida: {audioPath}.", ex);
                }
                if (!File.Exists(fullAudioPath))
                {
                    return AudioFileFailure($"No existe el archivo de audio para adjuntar: {fullAudioPath}. Revisa la generacion TTS antes de enviar.");
                }

                var audioFile = new FileInfo(fullAudioPath);
                if (audioFile.Length <= 0)
                {
                    return AudioFileFailure($"El archivo de audio para adjuntar esta vacio: {fullAudioPath}. Genera el audio nuevamente antes de enviar.");
                }

                log($"Adjuntando audio en WhatsApp Web: {fullAudioPath} ({audioFile.Length} bytes).");

                WebDriverWait wait = new WebDriverWait(driver, DefaultWaitTimeout);
                attachmentWait = wait;

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
                    return AudioUiFailure(BuildAudioStageFailureMessage(AudioAttachStage.OpenMenu, "WhatsApp Web: no se encontro el boton de adjuntar audio."), ex);
                }

                LogAudioStage(AudioAttachStage.OpenMenu, "abriendo menu de adjuntos");
                try
                {
                    ClickElementSafely(attachBtn);
                }
                catch (WebDriverException ex) when (!IsBrowserUnavailableException(ex))
                {
                    return AudioUiFailure(BuildAudioStageFailureMessage(AudioAttachStage.OpenMenu, "no se pudo hacer click en el boton de adjuntar audio"), ex);
                }

                bool attachmentMenuOpened = false;
                IWebElement? audioInput = null;
                bool uploadedThroughNativeDialog = false;
                string audioFileName = Path.GetFileName(fullAudioPath);
                if (EnsureWhatsAppInteractableAfterAttachmentAttempt(wait, stage: AudioAttachStage.OpenMenu) is { } blockedAfterAttach)
                {
                    return blockedAfterAttach;
                }

                IWebElement? audioOption = null;
                LogAudioStage(AudioAttachStage.FindInputBeforeClick, "buscando input de audio antes del click y opcion Audio visible");
                try
                {
                    wait.Until(d =>
                    {
                        attachmentMenuOpened = attachmentMenuOpened || HasAttachmentMenuOpened(d);
                        audioInput = FindAudioFileInput(d);
                        if (audioInput != null)
                        {
                            return true;
                        }

                        audioOption = FindAudioOption(d);
                        return audioOption != null;
                    });
                }
                catch (WebDriverTimeoutException ex)
                {
                    if (EnsureWhatsAppInteractableAfterAttachmentAttempt(wait, ex, AudioAttachStage.FindInputBeforeClick) is { } blockedWhileWaitingAudioEntryPoint)
                    {
                        return blockedWhileWaitingAudioEntryPoint;
                    }

                    return AudioUiFailure(BuildAudioStageFailureMessage(AudioAttachStage.FindInputBeforeClick, "no se encontro input[type=file] compatible con audio ni opcion Audio visible en el menu de adjuntos"), ex);
                }

                string inputsBeforeAudioClick = GetFileInputsOperationalSummary(driver);
                if (audioInput == null)
                {
                    if (audioOption == null)
                    {
                        return AudioUiFailure(BuildAudioStageFailureMessage(AudioAttachStage.FindInputBeforeClick, $"no se encontro input[type=file] compatible con audio ni opcion Audio visible. Inputs antes del click: {inputsBeforeAudioClick}"));
                    }

                    LogAudioStage(AudioAttachStage.ClickAudioOption, $"No se encontró input de audio antes del click; se intentará activar la opción Audio. {inputsBeforeAudioClick}");

                    try
                    {
                        ClickElementSafely(audioOption);
                    }
                    catch (WebDriverException ex) when (!IsBrowserUnavailableException(ex))
                    {
                        if (EnsureWhatsAppInteractableAfterAttachmentAttempt(wait, ex, AudioAttachStage.ClickAudioOption) is { } blockedAfterAudioClickFailure)
                        {
                            return blockedAfterAudioClickFailure;
                        }

                        return AudioUiFailure(BuildAudioStageFailureMessage(AudioAttachStage.ClickAudioOption, $"no se pudo hacer click en la opcion Audio. Inputs antes del click: {inputsBeforeAudioClick}"), ex);
                    }

                    string nativeUploadError = "";
                    LogAudioStage(AudioAttachStage.FindInputAfterClick, "buscando input de audio despues del click en Audio o dialogo nativo esperado");
                    try
                    {
                        wait.Until(d =>
                        {
                            audioInput = FindAudioFileInput(d);
                            if (audioInput != null)
                            {
                                return true;
                            }

                            if (TryFindNativeFileDialog(out _, out _))
                            {
                                LogAudioStage(AudioAttachStage.NativeDialogUpload, "dialogo nativo detectado; cargando archivo por fallback controlado");
                                if (TryUploadFileThroughNativeDialog(fullAudioPath, out nativeUploadError))
                                {
                                    uploadedThroughNativeDialog = true;
                                }

                                return true;
                            }

                            return false;
                        });
                    }
                    catch (WebDriverTimeoutException ex)
                    {
                        if (TryFindNativeFileDialog(out _, out _))
                        {
                            LogAudioStage(AudioAttachStage.NativeDialogUpload, "dialogo nativo detectado tras timeout; cargando archivo por fallback controlado");
                            if (TryUploadFileThroughNativeDialog(fullAudioPath, out nativeUploadError))
                            {
                                uploadedThroughNativeDialog = true;
                            }
                            else
                            {
                                return NativeDialogUploadFailure(wait, nativeUploadError, ex);
                            }
                        }
                        else
                        {
                            string inputsAfterAudioClick = GetFileInputsOperationalSummary(driver);
                            return AudioUiFailure(BuildAudioStageFailureMessage(AudioAttachStage.FindInputAfterClick,
                                "despues de hacer click en Audio no aparecio input[type=file] compatible con audio ni dialogo nativo esperado. " +
                                $"Inputs antes del click: {inputsBeforeAudioClick}. " +
                                $"Inputs despues del click: {inputsAfterAudioClick}. " +
                                $"Sospecha dialogo nativo abierto: {GetNativeFileDialogDiagnostic()}"),
                                ex);
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(nativeUploadError))
                    {
                        return NativeDialogUploadFailure(wait, nativeUploadError);
                    }
                }

                if (audioInput == null && !uploadedThroughNativeDialog)
                {
                    string inputsAfterAudioClick = GetFileInputsOperationalSummary(driver);
                    return AudioUiFailure(BuildAudioStageFailureMessage(AudioAttachStage.FindInputAfterClick,
                        "no se encontro input[type=file] compatible con audio despues de la estrategia en cascada. " +
                        $"Inputs antes del click: {inputsBeforeAudioClick}. " +
                        $"Inputs despues del click: {inputsAfterAudioClick}. " +
                        $"Sospecha dialogo nativo abierto: {GetNativeFileDialogDiagnostic()}"));
                }

                if (!uploadedThroughNativeDialog)
                {
                    LogAudioStage(AudioAttachStage.FindInputAfterClick, GetFileInputsOperationalSummary(driver));
                    log($"Input file de audio seleccionado: {GetSelectedFileInputDiagnostics(audioInput!)}");
                    if (!TrySendFileToInput(audioInput!, fullAudioPath, out string inputError))
                    {
                        if (EnsureWhatsAppInteractableAfterAttachmentAttempt(wait, stage: AudioAttachStage.FindInputAfterClick) is { } blockedAfterInputFailure)
                        {
                            return blockedAfterInputFailure;
                        }

                        return AudioUiFailure(BuildAudioStageFailureMessage(AudioAttachStage.FindInputAfterClick, $"no se pudo enviar el archivo al input file de audio. {inputError}"));
                    }
                }
                else
                {
                    LogAudioStage(AudioAttachStage.NativeDialogUpload, "archivo cargado mediante dialogo nativo controlado");
                }

                if (!uploadedThroughNativeDialog)
                {
                    LogAudioStage(AudioAttachStage.FindInputAfterClick, "archivo enviado al input mediante input.SendKeys");
                }
                string attachmentCleanupStage = uploadedThroughNativeDialog ? AudioAttachStage.NativeDialogUpload : AudioAttachStage.FindInputAfterClick;
                if (TryFindNativeFileDialog(out _, out _))
                {
                    LogAudioStage(attachmentCleanupStage, "dialogo nativo pendiente despues de adjuntar; intentando cerrar antes de esperar preview");
                    if (!TryCleanupNativeFileDialogAfterAttachmentAttempt(out string attachmentCleanupDiagnostic))
                    {
                        return AudioUiFailure(BuildAudioStageFailureMessage(attachmentCleanupStage, $"no se pudo cerrar el dialogo nativo pendiente despues de adjuntar. {attachmentCleanupDiagnostic}"));
                    }

                    log(attachmentCleanupDiagnostic);
                }

                IWebElement attachmentPreview;
                LogAudioStage(AudioAttachStage.WaitPreview, "esperando preview de adjunto");
                try
                {
                    attachmentPreview = WaitForAttachmentPreview(wait, audioFileName);
                }
                catch (WebDriverTimeoutException ex)
                {
                    if (EnsureWhatsAppInteractableAfterAttachmentAttempt(wait, ex, AudioAttachStage.WaitPreview) is { } blockedWithoutPreview)
                    {
                        return blockedWithoutPreview;
                    }

                    return AudioUiFailure(BuildAudioStageFailureMessage(AudioAttachStage.WaitPreview, "El archivo fue enviado al input, pero WhatsApp no mostró preview de adjunto."), ex);
                }

                LogAudioStage(AudioAttachStage.WaitPreview, $"preview detectado: {GetAttachmentPreviewDiagnostics(attachmentPreview, audioFileName)}");

                IWebElement previewSendButton;
                LogAudioStage(AudioAttachStage.ClickPreviewSend, "buscando boton enviar dentro del preview");
                try
                {
                    previewSendButton = wait.Until(d =>
                    {
                        var currentPreview = FindAttachmentPreview(d, audioFileName);
                        return currentPreview == null
                            ? null
                            : FindPreviewSendButton(currentPreview);
                    }) ?? throw new WebDriverTimeoutException("No se encontro el boton enviar dentro del preview de adjunto.");
                }
                catch (WebDriverTimeoutException ex)
                {
                    if (EnsureWhatsAppInteractableAfterAttachmentAttempt(wait, ex, AudioAttachStage.ClickPreviewSend) is { } blockedWithoutPreviewSend)
                    {
                        return blockedWithoutPreviewSend;
                    }

                    return AudioUiFailure(BuildAudioStageFailureMessage(AudioAttachStage.ClickPreviewSend, "WhatsApp mostró preview de adjunto, pero no se encontró un botón enviar dentro del preview. No se hará click en botones globales."), ex);
                }

                try
                {
                    ClickElementSafely(previewSendButton);
                }
                catch (WebDriverException ex) when (!IsBrowserUnavailableException(ex))
                {
                    return AudioUiFailure(BuildAudioStageFailureMessage(AudioAttachStage.ClickPreviewSend, "no se pudo hacer click en el boton enviar del preview"), ex);
                }

                LogAudioStage(AudioAttachStage.ClickPreviewSend, "boton enviar del preview clickeado");

                LogAudioStage(AudioAttachStage.WaitPreviewClose, "esperando cierre del preview");
                try
                {
                    if (!VerifyAudioSendCompleted(wait, audioFileName))
                    {
                        return AudioUiFailure(BuildAudioStageFailureMessage(AudioAttachStage.WaitPreviewClose, "WhatsApp no confirmó el cierre del preview de adjunto despues de hacer click en enviar; no se marcara el audio como enviado."));
                    }
                }
                catch (WebDriverTimeoutException ex)
                {
                    if (EnsureWhatsAppInteractableAfterAttachmentAttempt(wait, ex, AudioAttachStage.WaitPreviewClose) is { } blockedAfterPreviewSend)
                    {
                        return blockedAfterPreviewSend;
                    }

                    return AudioUiFailure(BuildAudioStageFailureMessage(AudioAttachStage.WaitPreviewClose, "WhatsApp no cerró el preview de adjunto despues de hacer click en enviar; no se marcara el audio como enviado."), ex);
                }

                log("Audio enviado; verificando limpieza de diálogo nativo de archivo.");
                if (TryCleanupNativeFileDialogAfterAudioSend(out string finalCleanupDiagnostic))
                {
                    string finalCleanupLog = finalCleanupDiagnostic == "No se detectó diálogo nativo pendiente."
                        ? "No se detectó diálogo nativo pendiente después del envío."
                        : finalCleanupDiagnostic;
                    log(finalCleanupLog);
                }
                else
                {
                    log($"Advertencia: el audio fue confirmado como enviado, pero no se pudo cerrar el diálogo nativo. {finalCleanupDiagnostic}");
                }

                LogAudioStage(AudioAttachStage.Done, "audio adjuntado y enviado correctamente en WhatsApp Web");
                return WhatsAppSendResult.Ok("Audio enviado correctamente.");
            }
            catch (WebDriverException ex) when (IsBrowserUnavailableException(ex))
            {
                return Failure(
                    WhatsAppHealthStatus.BrowserUnavailable,
                    BuildAudioStageFailureMessage(AudioAttachStage.Done, "El navegador o la sesion de WebDriver no esta disponible."),
                    ex);
            }
            catch (WebDriverException ex)
            {
                if (attachmentWait != null && EnsureWhatsAppInteractableAfterAttachmentAttempt(attachmentWait, ex, AudioAttachStage.Done) is { } blockedByNativeDialog)
                {
                    return blockedByNativeDialog;
                }

                var issue = GetHealthIssue();
                if (!issue.IsReady)
                {
                    string issueMessage = BuildAudioStageFailureMessage(
                        AudioAttachStage.Done,
                        "WhatsApp dejo de estar listo durante el adjunto o envio de audio. " +
                        $"Estado: {issue.Status}. {issue.Message}");
                    log(issueMessage);
                    return WhatsAppSendResult.Failure(issue.Status, issueMessage, issue.IsGlobalFailure, ex);
                }

                return AudioUiFailure(BuildAudioStageFailureMessage(AudioAttachStage.Done, "WhatsApp Web produjo un error de WebDriver al adjuntar o enviar el audio."), ex);
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
                    return ChatOpenFailure("No se pudo abrir el chat individual: no se encontro la caja de mensaje para abrir el chat.");
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
                    ? ChatOpenFailure(BuildOpenChatTimeoutDiagnostic(), ex)
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
            foreach (var locator in Selectors.MessageBoxLocators)
            {
                try
                {
                    var messageBox = FirstDisplayed(searchContext.FindElements(locator));
                    if (messageBox != null)
                    {
                        return messageBox;
                    }
                }
                catch (StaleElementReferenceException)
                {
                }
            }

            return null;
        }

        private static IWebElement? FindAudioOption(ISearchContext searchContext)
        {
            foreach (var locator in Selectors.AudioOptionLocators)
            {
                try
                {
                    var audioOption = FirstDisplayed(searchContext.FindElements(locator));
                    if (audioOption != null)
                    {
                        return audioOption;
                    }
                }
                catch (StaleElementReferenceException)
                {
                }
            }

            return null;
        }

        private static IWebElement? FindAudioFileInput(ISearchContext searchContext)
        {
            var inputs = searchContext.FindElements(By.CssSelector(Selectors.FileInputCss));
            foreach (var input in inputs)
            {
                try
                {
                    if (IsAudioAccept(input.GetAttribute("accept")))
                    {
                        return input;
                    }
                }
                catch (StaleElementReferenceException)
                {
                }
            }

            return null;
        }

        private static string GetFileInputsDiagnostics(ISearchContext searchContext)
        {
            IReadOnlyCollection<IWebElement> inputs;
            try
            {
                inputs = searchContext.FindElements(By.CssSelector(Selectors.FileInputCss));
            }
            catch (WebDriverException ex)
            {
                return $"inputs file detectados: no disponible; error: {SummarizeExceptionMessage(ex)}";
            }

            if (inputs.Count == 0)
            {
                return "inputs file detectados: 0; accept: (ninguno)";
            }

            var details = new List<string>();
            int index = 0;
            foreach (var input in inputs)
            {
                index++;
                try
                {
                    string accept = input.GetAttribute("accept") ?? "";
                    string name = input.GetAttribute("name") ?? "";
                    string ariaLabel = input.GetAttribute("aria-label") ?? "";
                    string displayed = FormatDiagnosticFlag(ReadElementFlag(input, element => element.Displayed));
                    string enabled = FormatDiagnosticFlag(ReadElementFlag(input, element => element.Enabled));
                    string audio = FormatDiagnosticFlag(IsAudioAccept(accept));
                    details.Add($"#{index}: accept={FormatDiagnosticValue(accept)}, audio={audio}, visible={displayed}, enabled={enabled}, name={FormatDiagnosticValue(name)}, aria-label={FormatDiagnosticValue(ariaLabel)}");
                }
                catch (StaleElementReferenceException)
                {
                    details.Add($"#{index}: stale");
                }
                catch (WebDriverException ex)
                {
                    details.Add($"#{index}: no disponible ({SummarizeExceptionMessage(ex)})");
                }
            }

            return $"inputs file detectados: {inputs.Count}; {string.Join(" | ", details)}";
        }

        private static string GetFileInputsOperationalSummary(ISearchContext searchContext)
        {
            IReadOnlyCollection<IWebElement> inputs;
            try
            {
                inputs = searchContext.FindElements(By.CssSelector(Selectors.FileInputCss));
            }
            catch (WebDriverException ex)
            {
                return $"inputs file detectados: no disponible; accepts detectados: no disponible; error: {SummarizeExceptionMessage(ex)}";
            }

            if (inputs.Count == 0)
            {
                return "inputs file detectados: 0; accepts detectados: (ninguno)";
            }

            var accepts = new List<string>();
            foreach (var input in inputs)
            {
                try
                {
                    accepts.Add(FormatDiagnosticValue(input.GetAttribute("accept")));
                }
                catch (StaleElementReferenceException)
                {
                    accepts.Add("(stale)");
                }
                catch (WebDriverException)
                {
                    accepts.Add("(no disponible)");
                }
            }

            return $"inputs file detectados: {inputs.Count}; accepts detectados: {string.Join(", ", accepts)}";
        }

        private static bool IsAudioAccept(string? accept)
        {
            return !string.IsNullOrWhiteSpace(accept)
                && ContainsAny(accept, AudioInputAcceptMarkers);
        }

        private bool TrySendFileToInput(IWebElement input, string fullAudioPath, out string error)
        {
            error = "";

            try
            {
                input.SendKeys(fullAudioPath);
                return true;
            }
            catch (WebDriverException ex) when (!IsBrowserUnavailableException(ex) && IsFileInputVisibilityException(ex))
            {
                string firstError = SummarizeExceptionMessage(ex);
                if (driver is not IJavaScriptExecutor js)
                {
                    error = $"Selenium rechazo el input posiblemente oculto y no hay JavaScriptExecutor disponible. Error: {firstError}";
                    return false;
                }

                try
                {
                    js.ExecuteScript(
                        "arguments[0].removeAttribute('hidden');" +
                        "arguments[0].style.display='block';" +
                        "arguments[0].style.visibility='visible';" +
                        "arguments[0].style.opacity='1';" +
                        "arguments[0].style.pointerEvents='auto';" +
                        "arguments[0].style.position='fixed';" +
                        "arguments[0].style.left='0px';" +
                        "arguments[0].style.top='0px';" +
                        "arguments[0].style.width='1px';" +
                        "arguments[0].style.height='1px';" +
                        "arguments[0].style.zIndex='2147483647';",
                        input);
                    input.SendKeys(fullAudioPath);
                    return true;
                }
                catch (WebDriverException retryEx) when (!IsBrowserUnavailableException(retryEx))
                {
                    error = $"Selenium rechazo el input despues de hacerlo visible con JavaScript. Primer error: {firstError}. Reintento: {SummarizeExceptionMessage(retryEx)}";
                    return false;
                }
            }
            catch (WebDriverException ex) when (!IsBrowserUnavailableException(ex))
            {
                error = $"Selenium rechazo el envio del archivo al input. Error: {SummarizeExceptionMessage(ex)}";
                return false;
            }
        }

        private static bool HasAudioTextSignal(ISearchContext searchContext)
        {
            return FirstDisplayed(searchContext.FindElements(By.XPath(Selectors.AudioVisibleTextXPath))) != null;
        }

        private static bool HasAudioHeadphonesIconSignal(ISearchContext searchContext)
        {
            return searchContext.FindElements(By.XPath(Selectors.AudioHeadphonesIconXPath)).Count > 0;
        }

        private IWebElement WaitForAttachmentPreview(WebDriverWait wait, string fileName)
        {
            activeAttachmentPreview = null;
            IWebElement? preview = wait.Until(d => FindAttachmentPreview(d, fileName));
            activeAttachmentPreview = preview;
            return preview ?? throw new WebDriverTimeoutException("No se encontro preview de adjunto.");
        }

        private static IWebElement? FindAttachmentPreview(ISearchContext searchContext, string fileName)
        {
            var candidates = new List<IWebElement>();
            AddPreviewCandidates(candidates, searchContext, By.XPath(Selectors.AttachmentPreviewDialogXPath));
            AddPreviewCandidates(candidates, searchContext, By.XPath(Selectors.AttachmentPreviewPanelXPath));

            IWebElement? bestCandidate = null;
            int bestScore = 0;
            foreach (var candidate in candidates)
            {
                int score = GetAttachmentPreviewScore(candidate, fileName);
                if (score > bestScore)
                {
                    bestCandidate = candidate;
                    bestScore = score;
                }
            }

            return bestScore >= 5 ? bestCandidate : null;
        }

        private static IWebElement? FindPreviewSendButton(ISearchContext previewContext)
        {
            try
            {
                return FirstDisplayed(previewContext.FindElements(By.XPath(Selectors.PreviewSendButtonXPath)));
            }
            catch (StaleElementReferenceException)
            {
                return null;
            }
        }

        private void WaitForPreviewToClose(WebDriverWait wait)
        {
            IWebElement? preview = activeAttachmentPreview;
            wait.Until(d =>
            {
                bool trackedPreviewClosed = preview == null || IsElementGoneOrHidden(preview);
                bool noRecognizablePreview = FindAttachmentPreview(d, "") == null;
                return trackedPreviewClosed && noRecognizablePreview;
            });
            activeAttachmentPreview = null;
        }

        private bool VerifyAudioSendCompleted(WebDriverWait wait, string fileName)
        {
            _ = fileName;
            WaitForPreviewToClose(wait);
            return true;
        }

        private WhatsAppSendResult? EnsureWhatsAppInteractableAfterAttachmentAttempt(
            WebDriverWait wait,
            Exception? attachmentException = null,
            string stage = AudioAttachStage.NativeDialogUpload)
        {
            bool nativeDialogDetected = TryFindNativeFileDialog(out _, out string dialogTitle);
            if (!nativeDialogDetected && !IsPotentialNativeFileDialogBlock(attachmentException))
            {
                return null;
            }

            if (TryDismissNativeFileDialog() && WaitForWhatsAppInteractable(wait))
            {
                var issue = GetHealthIssue();
                if (issue.IsReady)
                {
                    string recoveredMessage = BuildAudioStageFailureMessage(stage, "Se detectó posible diálogo nativo de archivo abierto. Se cerró y se marcó el envío de audio como fallido.");
                    log(recoveredMessage);
                    return WhatsAppSendResult.Failure(
                        WhatsAppHealthStatus.WhatsAppAttachmentFailed,
                        recoveredMessage,
                        false,
                        attachmentException);
                }

                string notReadyAfterDismissMessage = BuildAudioStageFailureMessage(
                    stage,
                    "Se detectó posible diálogo nativo de archivo abierto y se intentó cerrarlo, " +
                    $"pero WhatsApp no volvió a estar listo. Estado: {issue.Status}. {issue.Message}");
                log(notReadyAfterDismissMessage);
                return WhatsAppSendResult.Failure(issue.Status, notReadyAfterDismissMessage, issue.IsGlobalFailure, attachmentException ?? issue.Exception);
            }

            string stillOpenTitle = TryFindNativeFileDialog(out _, out string currentDialogTitle)
                ? currentDialogTitle
                : dialogTitle;
            string unrecoveredMessage = BuildAudioStageFailureMessage(
                stage,
                "Se detectó posible diálogo nativo de archivo abierto durante el adjunto de audio, " +
                "pero no se pudo confirmar su cierre ni recuperar WhatsApp Web. " +
                $"Título detectado: {FormatDiagnosticValue(stillOpenTitle)}. Se detiene la corrida para evitar continuar en estado bloqueado.");
            log(unrecoveredMessage);

            return WhatsAppSendResult.Failure(
                WhatsAppHealthStatus.WhatsAppNotReady,
                unrecoveredMessage,
                true,
                attachmentException);
        }

        private bool TryDismissNativeFileDialog()
        {
            if (!TryFindNativeFileDialog(out IntPtr dialogHandle, out _))
            {
                return false;
            }

            SetForegroundWindow(dialogHandle);
            PostEscapeToWindow(dialogHandle);
            Thread.Sleep(300);

            if (!TryFindNativeFileDialog(out dialogHandle, out _))
            {
                return true;
            }

            SetForegroundWindow(dialogHandle);
            PostEscapeToWindow(dialogHandle);
            Thread.Sleep(500);

            if (!TryFindNativeFileDialog(out dialogHandle, out _))
            {
                return true;
            }

            PostMessage(dialogHandle, WmClose, IntPtr.Zero, IntPtr.Zero);
            Thread.Sleep(500);
            return !TryFindNativeFileDialog(out _, out _);
        }

        private bool TryCleanupNativeFileDialogAfterAttachmentAttempt(out string diagnostic)
        {
            return TryCleanupNativeFileDialog("despues de adjuntar", out diagnostic);
        }

        private bool TryCleanupNativeFileDialogAfterAudioSend(out string diagnostic)
        {
            return TryCleanupNativeFileDialog("despues del envio", out diagnostic);
        }

        private bool TryCleanupNativeFileDialog(string context, out string diagnostic)
        {
            diagnostic = "No se detectó diálogo nativo pendiente.";

            if (!TryFindNativeFileDialog(out IntPtr dialogHandle, out string dialogTitle))
            {
                return true;
            }

            if (context == "despues del envio")
            {
                log("Diálogo nativo de archivo detectado después del envío; intentando cerrar.");
            }
            else
            {
                log("Diálogo nativo de archivo detectado después de adjuntar; intentando cerrar.");
            }

            try
            {
                SetForegroundWindow(dialogHandle);
                PostEscapeToWindow(dialogHandle);
                Thread.Sleep(1000);

                if (!TryFindNativeFileDialog(out _, out _))
                {
                    diagnostic = "Diálogo nativo cerrado con Escape.";
                    return true;
                }

                PostMessage(dialogHandle, WmClose, IntPtr.Zero, IntPtr.Zero);
                Thread.Sleep(1000);

                if (!TryFindNativeFileDialog(out _, out _))
                {
                    diagnostic = "Diálogo nativo cerrado con WM_CLOSE.";
                    return true;
                }

                diagnostic = $"No se pudo cerrar el diálogo nativo {context}. Titulo detectado: {FormatDiagnosticValue(dialogTitle)}.";
                return false;
            }
            catch (Exception ex) when (ex is InvalidOperationException or ExternalException)
            {
                diagnostic = $"No se pudo cerrar el diálogo nativo {context}. Error: {SummarizeExceptionMessage(ex)}";
                return false;
            }
        }

        private bool TryUploadFileThroughNativeDialog(string fullAudioPath, out string error)
        {
            error = "";

            if (!TryFindNativeFileDialog(out IntPtr dialogHandle, out string dialogTitle))
            {
                error = "No se detecto el dialogo nativo de archivo esperado.";
                return false;
            }

            try
            {
                SetForegroundWindow(dialogHandle);
                Thread.Sleep(250);

                if (!TryPasteFilePathAndSubmit(fullAudioPath, out string pasteError))
                {
                    error = pasteError;
                    return false;
                }

                if (!WaitForNativeFileDialogToClose(dialogHandle, TimeSpan.FromSeconds(8)))
                {
                    error = $"El dialogo nativo de archivo no se cerro despues de pegar la ruta y presionar Enter. Titulo: {FormatDiagnosticValue(dialogTitle)}.";
                    return false;
                }

                return true;
            }
            catch (Exception ex) when (ex is InvalidOperationException or ExternalException or ThreadStateException)
            {
                error = $"No se pudo cargar el archivo en el dialogo nativo. Error: {SummarizeExceptionMessage(ex)}";
                return false;
            }
        }

        private WhatsAppSendResult NativeDialogUploadFailure(WebDriverWait wait, string error, Exception? exception = null)
        {
            string message = BuildAudioStageFailureMessage(
                AudioAttachStage.NativeDialogUpload,
                "WhatsApp abrió el diálogo nativo de archivo para adjuntar audio, " +
                $"pero no se pudo cargar el archivo de forma controlada. {error}");

            bool dismissed = !TryFindNativeFileDialog(out _, out _) || TryDismissNativeFileDialog();
            if (dismissed)
            {
                if (WaitForWhatsAppInteractable(wait))
                {
                    return AudioUiFailure(message, exception);
                }

                var issue = GetHealthIssue();
                if (issue.IsReady)
                {
                    return AudioUiFailure(message, exception);
                }

                string notReadyMessage = BuildAudioStageFailureMessage(
                    AudioAttachStage.NativeDialogUpload,
                    "WhatsApp no volvió a estar listo despues del fallo del dialogo nativo. " +
                    $"Estado: {issue.Status}. {issue.Message}");
                log(notReadyMessage);
                return WhatsAppSendResult.Failure(issue.Status, notReadyMessage, issue.IsGlobalFailure, exception ?? issue.Exception);
            }

            string unrecoveredMessage = BuildAudioStageFailureMessage(
                AudioAttachStage.NativeDialogUpload,
                "WhatsApp abrió el diálogo nativo de archivo para adjuntar audio, " +
                $"pero no se pudo cargar el archivo de forma controlada. {error} " +
                "El dialogo nativo sigue abierto; se detiene la corrida para evitar continuar en estado bloqueado.");
            log(unrecoveredMessage);
            return WhatsAppSendResult.Failure(
                WhatsAppHealthStatus.WhatsAppNotReady,
                unrecoveredMessage,
                true,
                exception);
        }

        private static bool TryPasteFilePathAndSubmit(string fullAudioPath, out string error)
        {
            error = "";
            Exception? failure = null;

            var thread = new Thread(() =>
            {
                try
                {
                    SetClipboardTextWithRetry(fullAudioPath);
                    FormsSendKeys.SendWait("^v");
                    FormsSendKeys.SendWait("{ENTER}");
                }
                catch (Exception ex)
                {
                    failure = ex;
                }
            });

            thread.IsBackground = true;
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();

            if (!thread.Join(TimeSpan.FromSeconds(5)))
            {
                error = "No se pudo pegar la ruta del archivo en el dialogo nativo dentro del timeout.";
                return false;
            }

            if (failure != null)
            {
                error = $"No se pudo pegar la ruta del archivo en el dialogo nativo. Error: {SummarizeExceptionMessage(failure)}";
                return false;
            }

            return true;
        }

        private static void SetClipboardTextWithRetry(string text)
        {
            const int maxAttempts = 3;
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    FormsClipboard.SetText(text);
                    return;
                }
                catch (ExternalException) when (attempt < maxAttempts)
                {
                    Thread.Sleep(100);
                }
            }

            FormsClipboard.SetText(text);
        }

        private static bool WaitForNativeFileDialogToClose(IntPtr originalDialogHandle, TimeSpan timeout)
        {
            DateTime deadline = DateTime.UtcNow.Add(timeout);
            while (DateTime.UtcNow < deadline)
            {
                if (!TryFindNativeFileDialog(out IntPtr currentDialogHandle, out _))
                {
                    return true;
                }

                if (currentDialogHandle != originalDialogHandle)
                {
                    originalDialogHandle = currentDialogHandle;
                }

                Thread.Sleep(100);
            }

            return false;
        }

        private static void PostEscapeToWindow(IntPtr windowHandle)
        {
            PostMessage(windowHandle, WmKeyDown, new IntPtr(VkEscape), IntPtr.Zero);
            PostMessage(windowHandle, WmKeyUp, new IntPtr(VkEscape), IntPtr.Zero);
        }

        private static bool TryFindNativeFileDialog(out IntPtr dialogHandle, out string title)
        {
            dialogHandle = IntPtr.Zero;
            title = "";

            IntPtr foundHandle = IntPtr.Zero;
            string foundTitle = "";

            EnumWindows((hWnd, _) =>
            {
                if (!IsWindowVisible(hWnd))
                {
                    return true;
                }

                string className = GetNativeWindowClassName(hWnd);
                if (!string.Equals(className, "#32770", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                string windowTitle = GetNativeWindowText(hWnd);
                if (!IsNativeFileDialogTitle(windowTitle))
                {
                    return true;
                }

                if (!IsLikelyBrowserNativeDialog(hWnd))
                {
                    return true;
                }

                foundHandle = hWnd;
                foundTitle = windowTitle;
                return false;
            }, IntPtr.Zero);

            dialogHandle = foundHandle;
            title = foundTitle;
            return dialogHandle != IntPtr.Zero;
        }

        private static string GetNativeFileDialogDiagnostic()
        {
            return TryFindNativeFileDialog(out _, out string title)
                ? $"si, titulo={FormatDiagnosticValue(title)}"
                : "no";
        }

        private static bool IsLikelyBrowserNativeDialog(IntPtr windowHandle)
        {
            try
            {
                _ = GetWindowThreadProcessId(windowHandle, out uint processId);
                if (processId == 0)
                {
                    return false;
                }

                using Process process = Process.GetProcessById((int)processId);
                string processName = process.ProcessName ?? "";
                return processName.Contains("chrome", StringComparison.OrdinalIgnoreCase)
                    || processName.Contains("chromedriver", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private static bool IsNativeFileDialogTitle(string title)
        {
            string normalized = (title ?? "").Trim();
            return normalized.Equals("Abrir", StringComparison.OrdinalIgnoreCase)
                || normalized.Equals("Open", StringComparison.OrdinalIgnoreCase)
                || normalized.Contains("Abrir", StringComparison.OrdinalIgnoreCase)
                || normalized.Contains("Open", StringComparison.OrdinalIgnoreCase);
        }

        private static string GetNativeWindowText(IntPtr windowHandle)
        {
            var text = new StringBuilder(NativeWindowTextMaxLength);
            _ = GetWindowText(windowHandle, text, text.Capacity);
            return text.ToString();
        }

        private static string GetNativeWindowClassName(IntPtr windowHandle)
        {
            var className = new StringBuilder(NativeWindowTextMaxLength);
            _ = GetClassName(windowHandle, className, className.Capacity);
            return className.ToString();
        }

        private static bool IsPotentialNativeFileDialogBlock(Exception? exception)
        {
            if (exception is not WebDriverException webDriverException)
            {
                return false;
            }

            string message = webDriverException.Message ?? "";
            return message.Contains("modal dialog", StringComparison.OrdinalIgnoreCase)
                || message.Contains("file dialog", StringComparison.OrdinalIgnoreCase)
                || message.Contains("user prompt", StringComparison.OrdinalIgnoreCase)
                || message.Contains("not reachable", StringComparison.OrdinalIgnoreCase)
                || message.Contains("timed out receiving message from renderer", StringComparison.OrdinalIgnoreCase);
        }

        private static bool WaitForWhatsAppInteractable(WebDriverWait wait)
        {
            try
            {
                return wait.Until(d =>
                {
                    if (TryFindNativeFileDialog(out _, out _))
                    {
                        return false;
                    }

                    return IsWhatsAppDocumentInteractable(d);
                });
            }
            catch (WebDriverTimeoutException)
            {
                return false;
            }
            catch (WebDriverException)
            {
                return false;
            }
        }

        private static bool IsWhatsAppDocumentInteractable(IWebDriver browser)
        {
            try
            {
                if (browser is IJavaScriptExecutor js)
                {
                    _ = js.ExecuteScript("return document.readyState;");
                }

                return FindMessageBox(browser) != null
                    || browser.FindElements(By.CssSelector(Selectors.ChatHeaderCss)).Count > 0
                    || browser.FindElements(By.CssSelector(Selectors.ReadySidePaneCss)).Count > 0;
            }
            catch (WebDriverException)
            {
                return false;
            }
        }

        private static bool IsElementGoneOrHidden(IWebElement element)
        {
            try
            {
                return !element.Displayed;
            }
            catch (StaleElementReferenceException)
            {
                return true;
            }
            catch (NoSuchElementException)
            {
                return true;
            }
            catch (WebDriverException)
            {
                return false;
            }
        }

        private static void AddPreviewCandidates(List<IWebElement> candidates, ISearchContext searchContext, By by)
        {
            try
            {
                foreach (var candidate in searchContext.FindElements(by))
                {
                    if (!candidates.Any(existing => existing.Equals(candidate)))
                    {
                        candidates.Add(candidate);
                    }
                }
            }
            catch (WebDriverException)
            {
            }
        }

        private static int GetAttachmentPreviewScore(IWebElement candidate, string fileName)
        {
            try
            {
                if (!candidate.Displayed || IsInsideFooter(candidate))
                {
                    return 0;
                }

                bool hasDialogRole = string.Equals(candidate.GetAttribute("role"), "dialog", StringComparison.OrdinalIgnoreCase);
                bool hasFileName = AttachmentPreviewContainsFileName(candidate, fileName);
                bool hasPreviewSendButton = FindPreviewSendButton(candidate) != null;
                bool hasPreviewControls = HasAttachmentPreviewControls(candidate);

                int score = 0;
                score += hasDialogRole ? 3 : 0;
                score += hasFileName ? 4 : 0;
                score += hasPreviewSendButton ? 3 : 0;
                score += hasPreviewControls ? 2 : 0;

                bool isAttachmentPreview =
                    hasDialogRole && (hasFileName || hasPreviewSendButton || hasPreviewControls)
                    || hasPreviewSendButton && (hasFileName || hasPreviewControls);

                return isAttachmentPreview ? score : 0;
            }
            catch (StaleElementReferenceException)
            {
                return 0;
            }
            catch (WebDriverException)
            {
                return 0;
            }
        }

        private static bool AttachmentPreviewContainsFileName(IWebElement candidate, string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return false;
            }

            string text = ReadElementText(candidate);
            if (text.Contains(fileName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
            return fileNameWithoutExtension.Length >= 3
                && text.Contains(fileNameWithoutExtension, StringComparison.OrdinalIgnoreCase);
        }

        private static bool HasAttachmentPreviewControls(ISearchContext previewContext)
        {
            try
            {
                return AnyDisplayed(previewContext.FindElements(By.XPath(Selectors.AttachmentPreviewControlsXPath)));
            }
            catch (StaleElementReferenceException)
            {
                return false;
            }
            catch (WebDriverException)
            {
                return false;
            }
        }

        private static string GetAttachmentPreviewDiagnostics(IWebElement preview, string fileName)
        {
            try
            {
                bool hasDialogRole = string.Equals(preview.GetAttribute("role"), "dialog", StringComparison.OrdinalIgnoreCase);
                bool hasFileName = AttachmentPreviewContainsFileName(preview, fileName);
                bool hasPreviewSendButton = FindPreviewSendButton(preview) != null;
                bool hasPreviewControls = HasAttachmentPreviewControls(preview);
                string textPreview = BuildTextPreview(ReadElementText(preview), 180);

                return $"role=dialog: {FormatDiagnosticFlag(hasDialogRole)}; archivo detectado: {FormatDiagnosticFlag(hasFileName)}; boton enviar en preview: {FormatDiagnosticFlag(hasPreviewSendButton)}; controles de preview: {FormatDiagnosticFlag(hasPreviewControls)}; texto: {textPreview}";
            }
            catch (StaleElementReferenceException)
            {
                return "preview stale";
            }
            catch (WebDriverException ex)
            {
                return $"preview no disponible ({SummarizeExceptionMessage(ex)})";
            }
        }

        private static bool IsInsideFooter(IWebElement element)
        {
            try
            {
                return string.Equals(element.TagName, "footer", StringComparison.OrdinalIgnoreCase)
                    || element.FindElements(By.XPath("./ancestor::footer")).Count > 0;
            }
            catch (StaleElementReferenceException)
            {
                return true;
            }
            catch (WebDriverException)
            {
                return true;
            }
        }

        private static bool AnyDisplayed(IEnumerable<IWebElement> elements)
        {
            foreach (var element in elements)
            {
                try
                {
                    if (element.Displayed)
                    {
                        return true;
                    }
                }
                catch (StaleElementReferenceException)
                {
                }
                catch (WebDriverException)
                {
                }
            }

            return false;
        }

        private static string ReadElementText(IWebElement element)
        {
            try
            {
                return element.Text ?? "";
            }
            catch (StaleElementReferenceException)
            {
                return "";
            }
            catch (WebDriverException)
            {
                return "";
            }
        }

        private bool HasMessageBox()
        {
            return FindMessageBox(driver) != null;
        }

        private bool HasAnyElement(By by)
        {
            return driver.FindElements(by).Count > 0;
        }

        private void ClickElementSafely(IWebElement element)
        {
            WebDriverException? lastException = null;

            try
            {
                element.Click();
                return;
            }
            catch (WebDriverException ex) when (!IsBrowserUnavailableException(ex))
            {
                lastException = ex;
            }

            if (driver is IJavaScriptExecutor js)
            {
                try
                {
                    js.ExecuteScript("arguments[0].scrollIntoView({block:'center', inline:'center'});", element);
                    element.Click();
                    return;
                }
                catch (WebDriverException ex) when (!IsBrowserUnavailableException(ex))
                {
                    lastException = ex;
                }

                js.ExecuteScript("arguments[0].click();", element);
                return;
            }

            throw lastException ?? new WebDriverException("No se pudo hacer click en el elemento.");
        }

        private static bool HasAttachmentMenuOpened(ISearchContext searchContext)
        {
            try
            {
                return searchContext.FindElements(By.CssSelector(Selectors.AttachmentMenuIndicatorCss)).Count > 0
                    || FindAudioFileInput(searchContext) != null
                    || FindAudioOption(searchContext) != null;
            }
            catch (WebDriverException)
            {
                return false;
            }
        }

        private string BuildAudioOptionFailureMessage(string reason, bool attachmentMenuOpened)
        {
            bool? menuOpened = ReadDiagnosticFlag(() => attachmentMenuOpened || HasAttachmentMenuOpened(driver));
            bool? audioTextDetected = ReadDiagnosticFlag(() => HasAudioTextSignal(driver));
            bool? headphonesIconDetected = ReadDiagnosticFlag(() => HasAudioHeadphonesIconSignal(driver));

            return "WhatsApp Web: fallo al preparar adjunto de audio: " + reason + ". " +
                $"Diagnostico adjuntos: menu de adjuntos abierto: {FormatDiagnosticFlag(menuOpened)}; " +
                $"{GetFileInputsDiagnostics(driver)}; " +
                $"texto Audio detectado: {FormatDiagnosticFlag(audioTextDetected)}; " +
                $"icono ic-headphones-filled detectado: {FormatDiagnosticFlag(headphonesIconDetected)}.";
        }

        private static string GetSelectedFileInputDiagnostics(IWebElement input)
        {
            try
            {
                string accept = input.GetAttribute("accept") ?? "";
                string name = input.GetAttribute("name") ?? "";
                string ariaLabel = input.GetAttribute("aria-label") ?? "";
                string displayed = FormatDiagnosticFlag(ReadElementFlag(input, element => element.Displayed));
                string enabled = FormatDiagnosticFlag(ReadElementFlag(input, element => element.Enabled));
                return $"accept={FormatDiagnosticValue(accept)}, audio={FormatDiagnosticFlag(IsAudioAccept(accept))}, visible={displayed}, enabled={enabled}, name={FormatDiagnosticValue(name)}, aria-label={FormatDiagnosticValue(ariaLabel)}";
            }
            catch (StaleElementReferenceException)
            {
                return "stale";
            }
            catch (WebDriverException ex)
            {
                return $"no disponible ({SummarizeExceptionMessage(ex)})";
            }
        }

        private static bool IsFileInputVisibilityException(WebDriverException ex)
        {
            string message = ex.Message ?? "";

            return ex is ElementNotInteractableException
                || ex is InvalidElementStateException
                || message.Contains("not interactable", StringComparison.OrdinalIgnoreCase)
                || message.Contains("not visible", StringComparison.OrdinalIgnoreCase)
                || message.Contains("element is not currently visible", StringComparison.OrdinalIgnoreCase)
                || message.Contains("hidden", StringComparison.OrdinalIgnoreCase)
                || message.Contains("displayed", StringComparison.OrdinalIgnoreCase);
        }

        private static bool? ReadElementFlag(IWebElement element, Func<IWebElement, bool> read)
        {
            try
            {
                return read(element);
            }
            catch (StaleElementReferenceException)
            {
                return null;
            }
            catch (WebDriverException)
            {
                return null;
            }
        }

        private static string FormatDiagnosticValue(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? "(vacio)"
                : BuildTextPreview(value, 160);
        }

        private void LogAudioStage(string stage, string detail)
        {
            log($"{stage}: {detail}.");
        }

        private string BuildAudioStageFailureMessage(string stage, string reason)
        {
            return $"{stage}: {reason}. {BuildAudioAttachDiagnostics()}";
        }

        private string BuildAudioAttachDiagnostics()
        {
            string inputsSummary = GetFileInputsOperationalSummary(driver);
            string audioTextDetected = FormatDiagnosticFlag(ReadDiagnosticFlag(() => HasAudioTextSignal(driver)));
            string audioIconDetected = FormatDiagnosticFlag(ReadDiagnosticFlag(() => HasAudioHeadphonesIconSignal(driver)));
            string nativeDialogDetected = GetNativeFileDialogDiagnostic();
            string healthSummary = ReadWhatsAppHealthSummary();

            return "Diagnostico audio: " +
                $"{inputsSummary}; " +
                $"texto Audio detectado: {audioTextDetected}; " +
                $"icono Audio detectado: {audioIconDetected}; " +
                $"dialogo nativo detectado: {nativeDialogDetected}; " +
                $"health: {healthSummary}.";
        }

        private string ReadWhatsAppHealthSummary()
        {
            try
            {
                WhatsAppHealthIssue issue = GetHealthIssue();
                return $"{issue.Status} ({BuildTextPreview(issue.Message, 140)})";
            }
            catch (WebDriverException ex)
            {
                return $"no disponible ({SummarizeExceptionMessage(ex)})";
            }
            catch (Exception ex)
            {
                return $"no disponible ({SummarizeExceptionMessage(ex)})";
            }
        }

        private string BuildOpenChatTimeoutDiagnostic()
        {
            string currentUrl = ReadDiagnosticText(() => driver.Url);
            string headerExists = FormatDiagnosticFlag(ReadDiagnosticFlag(By.CssSelector(Selectors.ChatHeaderCss)));
            string sidePaneExists = FormatDiagnosticFlag(ReadDiagnosticFlag(By.CssSelector(Selectors.ReadySidePaneCss)));
            string bodyPreview = BuildTextPreview(ReadDiagnosticText(GetPageText), 300);

            return "No se pudo abrir el chat individual: no se encontro la caja de mensaje del chat. " +
                $"Diagnostico: URL actual: {currentUrl}; header: {headerExists}; side pane: {sidePaneExists}; " +
                $"body visible primeros 300 caracteres: {bodyPreview}";
        }

        private string ReadDiagnosticText(Func<string> read)
        {
            try
            {
                return read();
            }
            catch (WebDriverException)
            {
                return "no disponible";
            }
        }

        private bool? ReadDiagnosticFlag(By by)
        {
            try
            {
                return HasAnyElement(by);
            }
            catch (WebDriverException)
            {
                return null;
            }
        }

        private bool? ReadDiagnosticFlag(Func<bool> read)
        {
            try
            {
                return read();
            }
            catch (WebDriverException)
            {
                return null;
            }
        }

        private static string FormatDiagnosticFlag(bool? value)
        {
            return value switch
            {
                true => "si",
                false => "no",
                _ => "no disponible"
            };
        }

        private static string BuildTextPreview(string text, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return "(sin texto visible)";
            }

            string normalized = string.Join(" ", text.Split(Array.Empty<char>(), StringSplitOptions.RemoveEmptyEntries));
            return normalized.Length <= maxLength
                ? normalized
                : normalized[..maxLength] + "...";
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
                    if (element.Displayed && element.Enabled && !IsAriaDisabled(element))
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

        private static bool IsAriaDisabled(IWebElement element)
        {
            return string.Equals(element.GetAttribute("aria-disabled"), "true", StringComparison.OrdinalIgnoreCase);
        }

        private static bool ContainsAny(string text, IEnumerable<string> markers)
        {
            return markers.Any(marker => text.IndexOf(marker, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static bool IsTransientChromeStartupException(WebDriverException ex)
        {
            string message = ex.Message ?? "";
            string? innerMessage = ex.InnerException?.Message;

            return ContainsAny(message, ChromeStartupTransientMarkers)
                || (!string.IsNullOrWhiteSpace(innerMessage) && ContainsAny(innerMessage, ChromeStartupTransientMarkers));
        }

        private static string SummarizeExceptionMessage(Exception? ex)
        {
            if (ex == null)
            {
                return "No disponible.";
            }

            string message = (ex.Message ?? "").ReplaceLineEndings(" ").Trim();
            if (string.IsNullOrWhiteSpace(message))
            {
                message = ex.GetType().Name;
            }

            const int maxLength = 280;
            return message.Length <= maxLength
                ? message
                : message[..maxLength] + "...";
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

        private WhatsAppSendResult ChatOpenFailure(string message, Exception? exception = null)
        {
            var issue = GetHealthIssue();
            return issue.IsReady
                ? WhatsAppSendResult.Failure(WhatsAppHealthStatus.ChatOpenFailed, message, false, exception)
                : WhatsAppSendResult.FromIssue(issue, exception);
        }

        private WhatsAppSendResult AudioFileFailure(string message, Exception? exception = null)
        {
            log(message);
            return WhatsAppSendResult.Failure(WhatsAppHealthStatus.AudioFileInvalid, message, false, exception);
        }

        private WhatsAppSendResult AudioUiFailure(string message, Exception? exception = null)
        {
            var issue = GetHealthIssue();
            string resultMessage = message.StartsWith("Fallo UI al adjuntar audio.", StringComparison.OrdinalIgnoreCase)
                ? message
                : $"Fallo UI al adjuntar audio. {message}";

            if (!issue.IsReady)
            {
                string notReadyMessage = $"{resultMessage} Estado WhatsApp: {issue.Status}. {issue.Message}";
                log(notReadyMessage);
                return WhatsAppSendResult.Failure(issue.Status, notReadyMessage, issue.IsGlobalFailure, exception ?? issue.Exception);
            }

            log(resultMessage);
            return WhatsAppSendResult.Failure(WhatsAppHealthStatus.WhatsAppAttachmentFailed, resultMessage, false, exception);
        }

        private static bool IsGlobalFailure(WhatsAppHealthStatus status)
        {
            return status switch
            {
                WhatsAppHealthStatus.Ready => false,
                WhatsAppHealthStatus.InvalidDestinationNumber => false,
                WhatsAppHealthStatus.ChatOpenFailed => false,
                WhatsAppHealthStatus.TextToSpeechFailed => false,
                WhatsAppHealthStatus.AudioFileInvalid => false,
                WhatsAppHealthStatus.WhatsAppAttachmentFailed => false,
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
