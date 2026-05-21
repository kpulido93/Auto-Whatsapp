using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
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
        private static readonly string[] PossibleNativeFileDialogTitleMarkers =
        {
            "Abrir",
            "Open",
            "Seleccionar",
            "Choose",
            "File Upload",
            "Cargar"
        };
        private static readonly string[] BrowserNativeDialogProcessMarkers =
        {
            "chrome",
            "chromedriver",
            "msedge",
            "msedgedriver"
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

        private enum AudioOptionClickEffect
        {
            None,
            AudioInput,
            NativeDialog,
            PossibleNativeDialog,
            AttachmentPreview,
            FileInputsChanged
        }

        private enum NativeDialogUploadStatus
        {
            DialogClosed,
            DialogStillOpen,
            Error
        }

        private sealed class NativeDialogUploadResult
        {
            public bool Success { get; init; }
            public NativeDialogUploadStatus Status { get; init; }
            public string Diagnostic { get; init; } = "";
        }

        private sealed class AudioOptionCandidateLocator
        {
            public string Origin { get; init; } = "";
            public By Locator { get; init; } = By.XPath(".");
        }

        private sealed class AudioOptionCandidateInfo
        {
            public IWebElement Element { get; init; } = null!;
            public string Origin { get; init; } = "";
            public AudioOptionCandidateSnapshot Snapshot { get; init; } = new();
            public AudioOptionCandidateEvaluation Evaluation { get; init; } = new();
        }

        internal sealed class AudioOptionCandidateSnapshot
        {
            public string TagName { get; init; } = "";
            public string Role { get; init; } = "";
            public string TabIndex { get; init; } = "";
            public string Text { get; init; } = "";
            public bool HasExactAudioText { get; init; }
            public bool HasHeadphonesIcon { get; init; }
            public bool InAttachmentMenu { get; init; }
            public bool InSidePane { get; init; }
            public bool HasGridAncestor { get; init; }
            public bool HasGridCellAncestor { get; init; }
            public bool HasListItemAncestor { get; init; }
            public bool HasChildElements { get; init; }
            public bool IsDisplayed { get; init; }
            public bool IsEnabled { get; init; }
            public bool AriaDisabled { get; init; }
            public bool IsInsideFooter { get; init; }
            public int Width { get; init; }
            public int Height { get; init; }
        }

        internal sealed class AudioOptionCandidateEvaluation
        {
            public bool Accepted { get; init; }
            public string Reason { get; init; } = "";
        }

        private sealed class AudioOutgoingBaseline
        {
            public bool IsAvailable { get; init; }
            public int OutgoingMessageCount { get; init; }
            public int OutgoingAudioMessageCount { get; init; }
            public int OutgoingAudioControlCount { get; init; }
            public string LastOutgoingMessageSignature { get; init; } = "";
            public bool LastOutgoingMessageHasAudio { get; init; }
            public int LastOutgoingMessageAudioControlCount { get; init; }
            public string Diagnostic { get; init; } = "";
        }

        private sealed class AudioSendCompletionResult
        {
            public bool Success { get; init; }
            public string Reason { get; init; } = "";
            public bool PreviewClosed { get; init; }
            public bool OutgoingAudioDetected { get; init; }
            public bool NativeDialogDetected { get; init; }
            public bool NativeDialogCleanupAttempted { get; init; }
            public string Warning { get; init; } = "";
            public string Diagnostic { get; init; } = "";
        }

        private sealed class NativeDialogWindowInfo
        {
            public IntPtr Handle { get; init; }
            public string Title { get; init; } = "";
            public string ClassName { get; init; } = "";
            public uint ProcessId { get; init; }
            public string ProcessName { get; init; } = "";
            public bool IsBrowserProcess { get; init; }
            public bool HasStrictTitle { get; init; }
            public bool HasPossibleTitle { get; init; }
            public bool AppearedAfterReference { get; init; }
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
        private static extern IntPtr GetForegroundWindow();

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
            public const string AudioTextConditionXPath = "normalize-space(translate(., '" + XPathUppercase + "', '" + XPathLowercase + "'))='audio'";
            private const string AudioTextOrDescendantConditionXPath = AudioTextConditionXPath + " or .//*[" + AudioTextConditionXPath + "]";
            private const string EnabledElementXPath = "not(@aria-disabled='true') and not(@disabled)";
            private const string PreviewElementText = "normalize-space(translate(concat(@aria-label, ' ', @title, ' ', @data-icon), '" + XPathUppercase + "', '" + XPathLowercase + "'))";
            private const string AudioControlElementText = "normalize-space(translate(concat(@aria-label, ' ', @aria-placeholder, ' ', @title, ' ', @data-icon, ' ', @data-testid), '" + XPathUppercase + "', '" + XPathLowercase + "'))";
            public const string AttachmentPreviewPanelXPath = "//*[self::div or @role='dialog'][not(self::footer) and not(ancestor::footer) and not(.//footer) and .//*[@data-icon='wds-ic-send-filled' or @data-icon='send']]";
            public const string AttachmentPreviewControlsXPath = ".//*[self::audio or self::video or self::canvas or self::img or @contenteditable='true' or @role='textbox' or contains(" + PreviewElementText + ", 'caption') or contains(" + PreviewElementText + ", 'pie de foto') or contains(" + PreviewElementText + ", 'adjunto') or contains(" + PreviewElementText + ", 'archivo') or contains(" + PreviewElementText + ", 'document') or contains(" + PreviewElementText + ", 'audio')]";
            public const string PreviewSendButtonXPath = ".//*[self::button or @role='button' or @tabindex='0'][not(ancestor::footer) and (.//*[@data-icon='wds-ic-send-filled' or @data-icon='send'] or @data-icon='wds-ic-send-filled' or @data-icon='send' or contains(" + PreviewElementText + ", 'send') or contains(" + PreviewElementText + ", 'enviar')) and not(@aria-disabled='true')]";
            public const string OutgoingMessageXPath = "//*[not(ancestor::*[@role='dialog']) and not(ancestor::footer) and (contains(concat(' ', normalize-space(@class), ' '), ' message-out ') or starts-with(@data-id, 'true_') or contains(@data-id, 'true_'))]";
            public const string OutgoingAudioSignalXPath = ".//*[self::audio or contains(" + AudioControlElementText + ", 'play') or contains(" + AudioControlElementText + ", 'reproducir') or contains(" + AudioControlElementText + ", 'pause') or contains(" + AudioControlElementText + ", 'pausar') or contains(" + AudioControlElementText + ", 'audio') or contains(" + AudioControlElementText + ", 'voice') or contains(" + AudioControlElementText + ", 'voz') or contains(" + AudioControlElementText + ", 'ptt') or contains(" + AudioControlElementText + ", 'waveform') or contains(" + AudioControlElementText + ", 'mic') or contains(" + AudioControlElementText + ", 'microfono') or contains(" + AudioControlElementText + ", 'micrófono')]";

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

            public static readonly AudioOptionCandidateLocator[] AudioOptionCandidateLocators =
            {
                new() { Origin = "role-button-menuitem-texto-audio", Locator = By.XPath("//*[" + EnabledElementXPath + " and (@role='button' or @role='menuitem') and (" + AudioTextOrDescendantConditionXPath + ")]") },
                new() { Origin = "button-li-tabindex-texto-audio", Locator = By.XPath("//*[" + EnabledElementXPath + " and (self::button or self::li or @tabindex='0') and (" + AudioTextOrDescendantConditionXPath + ")]") },
                new() { Origin = "texto-audio-mas-icono-headphones", Locator = By.XPath("//*[" + EnabledElementXPath + " and (" + AudioTextOrDescendantConditionXPath + ") and .//*[local-name()='title' and normalize-space()='ic-headphones-filled']]") },
                new() { Origin = "fila-menu-role-menu-texto-audio", Locator = By.XPath("//*[@role='menu']//*[" + EnabledElementXPath + " and (self::li or self::button or @role='button' or @role='menuitem' or @tabindex='0' or self::div) and (" + AudioTextOrDescendantConditionXPath + ")]") },
                new() { Origin = "ancestro-clickeable-texto-audio", Locator = By.XPath(AudioVisibleTextXPath + "/ancestor::*[" + EnabledElementXPath + " and (self::li or self::button or @role='button' or @role='menuitem' or @tabindex='0')][1]") },
                new() { Origin = "ancestro-clickeable-icono-headphones", Locator = By.XPath(AudioHeadphonesIconXPath + "/ancestor::*[" + EnabledElementXPath + " and (self::li or self::button or @role='button' or @role='menuitem' or @tabindex='0' or self::div)][1]") },
                new() { Origin = "legacy-li-dropdown-audio", Locator = By.XPath("//li[@role='button' and @data-animate-dropdown-item='true' and .//*[" + AudioTextConditionXPath + "]]") }
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
                bool nativeDialogStillOpenAfterUpload = false;
                bool attachmentAlreadyInPreview = false;
                NativeDialogUploadResult? nativeDialogUploadResult = null;
                string audioFileName = Path.GetFileName(fullAudioPath);
                bool UploadAudioThroughNativeDialog(out string uploadError)
                {
                    uploadError = "";
                    NativeDialogUploadResult uploadResult = TryUploadFileThroughNativeDialog(fullAudioPath);
                    nativeDialogUploadResult = uploadResult;
                    if (uploadResult.Success)
                    {
                        uploadedThroughNativeDialog = true;
                        nativeDialogStillOpenAfterUpload = uploadResult.Status == NativeDialogUploadStatus.DialogStillOpen;
                        LogAudioStage(AudioAttachStage.NativeDialogUpload, uploadResult.Diagnostic);
                        return true;
                    }

                    uploadError = uploadResult.Diagnostic;
                    return false;
                }

                if (EnsureWhatsAppInteractableAfterAttachmentAttempt(wait, stage: AudioAttachStage.OpenMenu) is { } blockedAfterAttach)
                {
                    return blockedAfterAttach;
                }

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

                        return FindAudioOptionCandidates(d).Count > 0;
                    });
                }
                catch (WebDriverTimeoutException ex)
                {
                    if (EnsureWhatsAppInteractableAfterAttachmentAttempt(wait, ex, AudioAttachStage.FindInputBeforeClick) is { } blockedWhileWaitingAudioEntryPoint)
                    {
                        return blockedWhileWaitingAudioEntryPoint;
                    }

                    return AudioUiFailure(BuildAudioStageFailureMessage(AudioAttachStage.FindInputBeforeClick, "no se encontro input[type=file] compatible con audio ni opcion Audio dentro del menu de adjuntos"), ex);
                }

                string inputsBeforeAudioClick = GetFileInputsOperationalSummary(driver);
                if (audioInput == null)
                {
                    IWebElement? bestAudioOption = FindBestClickableAudioOption(driver, out string audioOptionDiagnostics);
                    if (bestAudioOption == null)
                    {
                        return AudioUiFailure(BuildAudioStageFailureMessage(AudioAttachStage.FindInputBeforeClick, $"no se encontro input[type=file] compatible con audio ni opcion Audio dentro del menu de adjuntos. Inputs antes del click: {inputsBeforeAudioClick}. Candidatos Audio: {audioOptionDiagnostics}"));
                    }

                    int audioOptionDiagnosticSeparator = audioOptionDiagnostics.IndexOf(';');
                    string audioOptionCountDiagnostic = audioOptionDiagnosticSeparator >= 0
                        ? audioOptionDiagnostics[..audioOptionDiagnosticSeparator]
                        : audioOptionDiagnostics;
                    LogAudioStage(AudioAttachStage.ClickAudioOption, $"No se encontró input de audio antes del click; se intentará activar la opción Audio. {inputsBeforeAudioClick}. {audioOptionCountDiagnostic}");

                    if (!TryClickAudioOptionAndWaitForEffect(wait, audioFileName, out AudioOptionClickEffect clickEffect, out string clickDiagnostics))
                    {
                        if (EnsureWhatsAppInteractableAfterAttachmentAttempt(wait, stage: AudioAttachStage.ClickAudioOption) is { } blockedAfterAudioClickFailure)
                        {
                            return blockedAfterAudioClickFailure;
                        }

                        string inputsAfterFailedAudioClick = GetFileInputsOperationalSummary(driver);
                        return AudioUiFailure(BuildAudioStageFailureMessage(
                            AudioAttachStage.ClickAudioOption,
                            "ningun candidato de la opcion Audio produjo efecto. " +
                            $"Inputs antes del click: {inputsBeforeAudioClick}. " +
                            $"Inputs despues del click: {inputsAfterFailedAudioClick}. " +
                            $"Dialogo nativo detectado: {GetNativeFileDialogDiagnostic()}. " +
                            $"{clickDiagnostics}"));
                    }

                    if (clickEffect == AudioOptionClickEffect.AttachmentPreview)
                    {
                        attachmentAlreadyInPreview = true;
                    }

                    string nativeUploadError = "";
                    LogAudioStage(AudioAttachStage.FindInputAfterClick, $"buscando input de audio despues del click en Audio, dialogo nativo esperado o preview. Efecto inicial: {FormatAudioOptionClickEffect(clickEffect)}");
                    if (clickEffect == AudioOptionClickEffect.PossibleNativeDialog)
                    {
                        string possibleDialogDiagnostic = GetNativeFileDialogDiagnostic();
                        LogAudioStage(AudioAttachStage.NativeDialogUpload, $"dialogo nativo posible detectado despues del click en Audio; validando antes de cargar ruta. {possibleDialogDiagnostic}");
                        if (TryWaitForStrictNativeFileDialog(TimeSpan.FromSeconds(2), out _, out _))
                        {
                            LogAudioStage(AudioAttachStage.NativeDialogUpload, "dialogo nativo validado; cargando archivo por fallback controlado");
                            if (!UploadAudioThroughNativeDialog(out string possibleNativeUploadError))
                            {
                                return NativeDialogUploadFailure(wait, possibleNativeUploadError);
                            }
                        }
                        else
                        {
                            return PossibleNativeDialogFailure(wait, possibleDialogDiagnostic);
                        }
                    }

                    try
                    {
                        wait.Until(d =>
                        {
                            if (uploadedThroughNativeDialog)
                            {
                                return true;
                            }

                            audioInput = FindAudioFileInput(d);
                            if (audioInput != null)
                            {
                                return true;
                            }

                            if (FindAttachmentPreview(d, audioFileName) != null || FindAttachmentPreview(d, "") != null)
                            {
                                attachmentAlreadyInPreview = true;
                                return true;
                            }

                            if (TryFindNativeFileDialog(out _, out _))
                            {
                                LogAudioStage(AudioAttachStage.NativeDialogUpload, "dialogo nativo detectado; cargando archivo por fallback controlado");
                                _ = UploadAudioThroughNativeDialog(out nativeUploadError);

                                return true;
                            }

                            if (TryFindPossibleNativeFileDialog(out _, out _, out string possibleDialogDiagnostic))
                            {
                                LogAudioStage(AudioAttachStage.NativeDialogUpload, $"dialogo nativo posible detectado durante espera post-click; validando antes de cargar ruta. {possibleDialogDiagnostic}");
                                if (TryWaitForStrictNativeFileDialog(TimeSpan.FromSeconds(2), out _, out _))
                                {
                                    LogAudioStage(AudioAttachStage.NativeDialogUpload, "dialogo nativo validado; cargando archivo por fallback controlado");
                                    _ = UploadAudioThroughNativeDialog(out nativeUploadError);

                                    return true;
                                }

                                nativeUploadError = "Se detecto un dialogo nativo posible, pero no se pudo validarlo como selector de archivo de Chrome. " + possibleDialogDiagnostic;
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
                            if (!UploadAudioThroughNativeDialog(out nativeUploadError))
                            {
                                return NativeDialogUploadFailure(wait, nativeUploadError, ex);
                            }
                        }
                        else
                        {
                            string inputsAfterAudioClick = GetFileInputsOperationalSummary(driver);
                            return AudioUiFailure(BuildAudioStageFailureMessage(AudioAttachStage.FindInputAfterClick,
                                "despues de hacer click en Audio no aparecio input[type=file] compatible con audio, dialogo nativo esperado ni preview. " +
                                $"Inputs antes del click: {inputsBeforeAudioClick}. " +
                                $"Inputs despues del click: {inputsAfterAudioClick}. " +
                                $"Sospecha dialogo nativo abierto: {GetNativeFileDialogDiagnostic()}. " +
                                $"Candidatos Audio intentados: {clickDiagnostics}"),
                                ex);
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(nativeUploadError))
                    {
                        if (nativeUploadError.Contains("dialogo nativo posible", StringComparison.OrdinalIgnoreCase))
                        {
                            return PossibleNativeDialogFailure(wait, nativeUploadError);
                        }

                        return NativeDialogUploadFailure(wait, nativeUploadError);
                    }
                }

                if (audioInput == null && !uploadedThroughNativeDialog && !attachmentAlreadyInPreview)
                {
                    string inputsAfterAudioClick = GetFileInputsOperationalSummary(driver);
                    return AudioUiFailure(BuildAudioStageFailureMessage(AudioAttachStage.FindInputAfterClick,
                        "no se encontro input[type=file] compatible con audio despues de la estrategia en cascada. " +
                        $"Inputs antes del click: {inputsBeforeAudioClick}. " +
                        $"Inputs despues del click: {inputsAfterAudioClick}. " +
                        $"Sospecha dialogo nativo abierto: {GetNativeFileDialogDiagnostic()}"));
                }

                if (!uploadedThroughNativeDialog && !attachmentAlreadyInPreview)
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
                else if (attachmentAlreadyInPreview)
                {
                    LogAudioStage(AudioAttachStage.FindInputAfterClick, "preview de adjunto ya detectado despues del click en Audio");
                }
                else
                {
                    string uploadDiagnostic = nativeDialogUploadResult?.Diagnostic ?? "archivo cargado mediante dialogo nativo controlado";
                    LogAudioStage(AudioAttachStage.NativeDialogUpload, uploadDiagnostic);
                }

                if (!uploadedThroughNativeDialog && !attachmentAlreadyInPreview)
                {
                    LogAudioStage(AudioAttachStage.FindInputAfterClick, "archivo enviado al input mediante input.SendKeys");
                }
                if (uploadedThroughNativeDialog)
                {
                    LogAudioStage(AudioAttachStage.NativeDialogUpload, "Dialogo nativo usado para cargar audio; esperando cierre o preview");
                    if (nativeDialogStillOpenAfterUpload)
                    {
                        LogAudioStage(AudioAttachStage.NativeDialogUpload, "el dialogo nativo seguia abierto tras Enter; no se cerrara antes de esperar preview");
                    }
                }
                else if (TryFindNativeFileDialog(out _, out _))
                {
                    LogAudioStage(AudioAttachStage.FindInputAfterClick, "dialogo nativo pendiente despues de input.SendKeys; intentando cerrar antes de esperar preview");
                    if (!TryCleanupNativeFileDialogAfterAttachmentAttempt(out string attachmentCleanupDiagnostic))
                    {
                        return AudioUiFailure(BuildAudioStageFailureMessage(AudioAttachStage.FindInputAfterClick, $"no se pudo cerrar el dialogo nativo pendiente despues de adjuntar. {attachmentCleanupDiagnostic}"));
                    }

                    log(attachmentCleanupDiagnostic);
                }

                IWebElement attachmentPreview;
                LogAudioStage(AudioAttachStage.WaitPreview, "esperando preview de adjunto");
                try
                {
                    attachmentPreview = WaitForAttachmentPreview(wait, audioFileName);
                    if (uploadedThroughNativeDialog && TryFindNativeFileDialog(out _, out _))
                    {
                        LogAudioStage(AudioAttachStage.WaitPreview, "Preview apareció aunque el diálogo seguía abierto; se continuará y se limpiará post-envío");
                    }
                }
                catch (WebDriverTimeoutException ex)
                {
                    if (uploadedThroughNativeDialog && nativeDialogStillOpenAfterUpload)
                    {
                        LogAudioStage(AudioAttachStage.WaitPreview, "No apareció preview; cerrando diálogo nativo pendiente y marcando fallo");
                        if (TryFindNativeFileDialog(out _, out _))
                        {
                            if (TryCleanupNativeFileDialogAfterAttachmentAttempt(out string previewTimeoutCleanupDiagnostic))
                            {
                                log(previewTimeoutCleanupDiagnostic);
                            }
                            else
                            {
                                log($"No se pudo cerrar el diálogo nativo pendiente tras timeout de preview. {previewTimeoutCleanupDiagnostic}");
                            }
                        }

                        return AudioUiFailure(BuildAudioStageFailureMessage(
                            AudioAttachStage.WaitPreview,
                            "No apareció preview despues de cargar el audio por dialogo nativo; se intento cerrar el dialogo nativo pendiente y se marca el contacto como fallido."),
                            ex);
                    }

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

                AudioOutgoingBaseline outgoingBaseline = CaptureOutgoingAudioBaseline();
                LogAudioStage(AudioAttachStage.ClickPreviewSend, $"baseline mensajes salientes antes de enviar audio: {outgoingBaseline.Diagnostic}");

                try
                {
                    ClickElementSafely(previewSendButton);
                }
                catch (WebDriverException ex) when (!IsBrowserUnavailableException(ex))
                {
                    return AudioUiFailure(BuildAudioStageFailureMessage(AudioAttachStage.ClickPreviewSend, "no se pudo hacer click en el boton enviar del preview"), ex);
                }

                LogAudioStage(AudioAttachStage.ClickPreviewSend, "boton enviar del preview clickeado");

                LogAudioStage(AudioAttachStage.WaitPreviewClose, "esperando cierre del preview o confirmación por mensaje saliente");
                AudioSendCompletionResult sendCompletion = VerifyAudioSendCompleted(wait, audioFileName, outgoingBaseline);
                if (!sendCompletion.Success)
                {
                    LogAudioStage(AudioAttachStage.WaitPreviewClose, "no se confirmó envío; preview visible y sin mensaje saliente");
                    if (!sendCompletion.NativeDialogDetected)
                    {
                        LogAudioStage(AudioAttachStage.WaitPreviewClose, "No se detectó diálogo nativo; el preview no cerró dentro del timeout");
                    }

                    return AudioUiFailure(BuildAudioStageFailureMessage(
                        AudioAttachStage.WaitPreviewClose,
                        "No se confirmó envío de audio: preview no cerró y no apareció mensaje saliente de audio. " + sendCompletion.Diagnostic));
                }

                if (sendCompletion.PreviewClosed)
                {
                    LogAudioStage(AudioAttachStage.WaitPreviewClose, "audio confirmado por preview cerrado");
                }

                if (sendCompletion.OutgoingAudioDetected)
                {
                    LogAudioStage(AudioAttachStage.WaitPreviewClose, "audio confirmado por mensaje saliente de audio");
                }

                if (sendCompletion.OutgoingAudioDetected && !sendCompletion.PreviewClosed)
                {
                    LogAudioStage(AudioAttachStage.WaitPreviewClose, "preview/input tardó en cerrar, pero el audio ya está en el chat");
                }

                if (!string.IsNullOrWhiteSpace(sendCompletion.Warning))
                {
                    log(sendCompletion.Warning);
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

        private IWebElement? FindBestClickableAudioOption(ISearchContext searchContext, out string diagnostics)
        {
            var candidates = FindAudioOptionCandidateInfos(searchContext);
            diagnostics = BuildAudioOptionCandidatesDiagnostics(candidates);
            return candidates.Count == 0 ? null : candidates[0].Element;
        }

        private static IReadOnlyList<IWebElement> FindAudioOptionCandidates(ISearchContext searchContext)
        {
            return FindAudioOptionCandidateInfos(searchContext)
                .Select(candidate => candidate.Element)
                .ToList();
        }

        private static IReadOnlyList<AudioOptionCandidateInfo> FindAudioOptionCandidateInfos(ISearchContext searchContext)
        {
            var candidates = new List<AudioOptionCandidateInfo>();
            foreach (var candidateLocator in Selectors.AudioOptionCandidateLocators)
            {
                AddAudioOptionCandidates(candidates, searchContext, candidateLocator.Locator, candidateLocator.Origin);
            }

            return candidates;
        }

        private static void AddAudioOptionCandidates(
            List<AudioOptionCandidateInfo> candidates,
            ISearchContext searchContext,
            By locator,
            string origin)
        {
            try
            {
                foreach (var candidate in searchContext.FindElements(locator))
                {
                    AudioOptionCandidateSnapshot? snapshot = TryBuildAudioOptionCandidateSnapshot(candidate);
                    if (snapshot == null)
                    {
                        continue;
                    }

                    AudioOptionCandidateEvaluation evaluation = EvaluateAudioOptionCandidate(snapshot);
                    if (evaluation.Accepted && !ContainsEquivalentElement(candidates, candidate))
                    {
                        candidates.Add(new AudioOptionCandidateInfo
                        {
                            Element = candidate,
                            Origin = origin,
                            Snapshot = snapshot,
                            Evaluation = evaluation
                        });
                    }
                }
            }
            catch (StaleElementReferenceException)
            {
            }
            catch (WebDriverException)
            {
            }
        }

        private static bool ContainsEquivalentElement(IEnumerable<AudioOptionCandidateInfo> elements, IWebElement candidate)
        {
            foreach (var element in elements.Select(item => item.Element))
            {
                try
                {
                    if (element.Equals(candidate))
                    {
                        return true;
                    }
                }
                catch (WebDriverException)
                {
                }
            }

            return false;
        }

        internal static AudioOptionCandidateEvaluation EvaluateAudioOptionCandidate(AudioOptionCandidateSnapshot snapshot)
        {
            ArgumentNullException.ThrowIfNull(snapshot);

            if (!snapshot.IsDisplayed)
            {
                return RejectAudioOptionCandidate("no visible");
            }

            if (!snapshot.IsEnabled)
            {
                return RejectAudioOptionCandidate("no habilitado");
            }

            if (snapshot.IsInsideFooter)
            {
                return RejectAudioOptionCandidate("footer del chat");
            }

            if (snapshot.AriaDisabled)
            {
                return RejectAudioOptionCandidate("aria-disabled");
            }

            if (snapshot.InSidePane || IsSidePaneListCandidate(snapshot))
            {
                return RejectAudioOptionCandidate("panel lateral");
            }

            if (!snapshot.InAttachmentMenu)
            {
                return RejectAudioOptionCandidate("fuera del menu de adjuntos");
            }

            if (!snapshot.HasExactAudioText && !snapshot.HasHeadphonesIcon)
            {
                return RejectAudioOptionCandidate("sin señal de Audio");
            }

            bool isButtonLike =
                string.Equals(snapshot.TagName, "button", StringComparison.OrdinalIgnoreCase)
                || string.Equals(snapshot.TagName, "li", StringComparison.OrdinalIgnoreCase)
                || string.Equals(snapshot.Role, "button", StringComparison.OrdinalIgnoreCase)
                || string.Equals(snapshot.Role, "menuitem", StringComparison.OrdinalIgnoreCase)
                || string.Equals(snapshot.TabIndex, "0", StringComparison.OrdinalIgnoreCase);
            bool isDiv = string.Equals(snapshot.TagName, "div", StringComparison.OrdinalIgnoreCase);
            bool hasReasonableMenuItemSize =
                snapshot.Width >= 72
                && snapshot.Height >= 28
                && snapshot.Width <= 900
                && snapshot.Height <= 180;
            bool hasMinimalClickableSize = snapshot.Width >= 40 && snapshot.Height >= 20;

            if (isButtonLike)
            {
                return (hasReasonableMenuItemSize || hasMinimalClickableSize)
                    ? AcceptAudioOptionCandidate()
                    : RejectAudioOptionCandidate("no clickeable");
            }

            if (isDiv)
            {
                return hasReasonableMenuItemSize && snapshot.HasChildElements
                    ? AcceptAudioOptionCandidate()
                    : RejectAudioOptionCandidate("no clickeable");
            }

            return hasReasonableMenuItemSize && snapshot.HasChildElements
                ? AcceptAudioOptionCandidate()
                : RejectAudioOptionCandidate("no clickeable");
        }

        private static AudioOptionCandidateEvaluation AcceptAudioOptionCandidate()
        {
            return new AudioOptionCandidateEvaluation
            {
                Accepted = true,
                Reason = "aceptado"
            };
        }

        private static AudioOptionCandidateEvaluation RejectAudioOptionCandidate(string reason)
        {
            return new AudioOptionCandidateEvaluation
            {
                Accepted = false,
                Reason = reason
            };
        }

        private static bool IsSidePaneListCandidate(AudioOptionCandidateSnapshot snapshot)
        {
            return snapshot.InSidePane
                && (snapshot.HasGridAncestor
                    || snapshot.HasGridCellAncestor
                    || snapshot.HasListItemAncestor
                    || string.Equals(snapshot.Role, "grid", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(snapshot.Role, "gridcell", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(snapshot.Role, "listitem", StringComparison.OrdinalIgnoreCase));
        }

        private static AudioOptionCandidateSnapshot? TryBuildAudioOptionCandidateSnapshot(IWebElement element)
        {
            try
            {
                string text = ReadElementText(element);
                string role = element.GetAttribute("role") ?? "";
                string tabIndex = element.GetAttribute("tabindex") ?? "";
                string ariaDisabled = element.GetAttribute("aria-disabled") ?? "";
                var size = element.Size;

                return new AudioOptionCandidateSnapshot
                {
                    TagName = element.TagName ?? "",
                    Role = role,
                    TabIndex = tabIndex,
                    Text = text,
                    HasExactAudioText = ElementContainsExactAudioText(element, text),
                    HasHeadphonesIcon = ElementContainsHeadphonesIcon(element),
                    InAttachmentMenu = IsInsideAttachmentMenuOrDropdown(element),
                    InSidePane = IsInsideSidePane(element),
                    HasGridAncestor = HasAncestorOrSelfRole(element, "grid"),
                    HasGridCellAncestor = HasAncestorOrSelfRole(element, "gridcell"),
                    HasListItemAncestor = HasAncestorOrSelfRole(element, "listitem"),
                    HasChildElements = ElementHasChildElements(element),
                    IsDisplayed = element.Displayed,
                    IsEnabled = element.Enabled,
                    AriaDisabled = string.Equals(ariaDisabled, "true", StringComparison.OrdinalIgnoreCase),
                    IsInsideFooter = IsInsideFooter(element),
                    Width = size.Width,
                    Height = size.Height
                };
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

        private static bool ElementContainsExactAudioText(IWebElement element)
        {
            return ElementContainsExactAudioText(element, ReadElementText(element));
        }

        private static bool ElementContainsExactAudioText(IWebElement element, string text)
        {
            if (ContainsExactAudioTextLine(text))
            {
                return true;
            }

            try
            {
                return AnyDisplayed(element.FindElements(By.XPath(".//*[" + Selectors.AudioTextConditionXPath + "]")));
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

        private static bool ContainsExactAudioTextLine(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            var lines = text
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim());

            return lines.Any(line => string.Equals(line, "Audio", StringComparison.OrdinalIgnoreCase));
        }

        private static bool ElementContainsHeadphonesIcon(IWebElement element)
        {
            try
            {
                return element.FindElements(By.XPath(".//*[local-name()='title' and normalize-space()='ic-headphones-filled']")).Count > 0;
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

        private static bool IsInsideAttachmentMenuOrDropdown(IWebElement element)
        {
            try
            {
                return element.FindElements(By.XPath("./ancestor-or-self::*[@role='menu' or @role='menuitem' or @data-animate-dropdown-item='true']")).Count > 0;
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

        private static bool IsInsideSidePane(IWebElement element)
        {
            try
            {
                return element.FindElements(By.XPath("./ancestor-or-self::*[@id='pane-side']")).Count > 0;
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

        private static bool HasAncestorOrSelfRole(IWebElement element, string role)
        {
            try
            {
                return element.FindElements(By.XPath($"./ancestor-or-self::*[@role='{role}']")).Count > 0;
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

        private static bool ElementHasChildElements(IWebElement element)
        {
            try
            {
                return element.FindElements(By.XPath("./*")).Count > 0;
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

        private string BuildAudioOptionCandidatesDiagnostics(IReadOnlyList<AudioOptionCandidateInfo> candidates)
        {
            if (candidates.Count == 0)
            {
                return "candidatos Audio dentro del menu de adjuntos: 0";
            }

            var details = candidates
                .Take(6)
                .Select((candidate, index) => $"#{index + 1}: {GetAudioOptionCandidateDiagnostics(candidate)}")
                .ToList();

            if (candidates.Count > details.Count)
            {
                details.Add($"+{candidates.Count - details.Count} candidatos mas");
            }

            return $"candidatos Audio dentro del menu de adjuntos: {candidates.Count}; {string.Join(" | ", details)}";
        }

        private string GetAudioOptionCandidateDiagnostics(AudioOptionCandidateInfo candidate)
        {
            return GetAudioOptionCandidateDiagnostics(candidate.Element, candidate.Origin);
        }

        private string GetAudioOptionCandidateDiagnostics(IWebElement candidate, string origin)
        {
            try
            {
                if (driver is IJavaScriptExecutor js)
                {
                    object? result = js.ExecuteScript(
                        "const e = arguments[0];" +
                        "const r = e.getBoundingClientRect();" +
                        "const s = window.getComputedStyle(e);" +
                        "const text = (e.innerText || e.textContent || '').replace(/\\s+/g, ' ').trim().slice(0, 80);" +
                        "const hasIcon = Array.from(e.querySelectorAll('title')).some(t => (t.textContent || '').trim() === 'ic-headphones-filled');" +
                        "const inMenu = !!e.closest('[role=\"menu\"]');" +
                        "return 'tag=' + e.tagName.toLowerCase()" +
                        " + '; role=' + (e.getAttribute('role') || '(none)')" +
                        " + '; tabindex=' + (e.getAttribute('tabindex') || '(none)')" +
                        " + '; aria-label=' + (e.getAttribute('aria-label') || '(none)')" +
                        " + '; texto=' + (text || '(vacio)')" +
                        " + '; size=' + Math.round(r.width) + 'x' + Math.round(r.height)" +
                        " + '; pointer-events=' + s.pointerEvents" +
                        " + '; cursor=' + s.cursor" +
                        " + '; icono=' + (hasIcon ? 'si' : 'no')" +
                        " + '; en-menu=' + (inMenu ? 'si' : 'no');",
                        candidate);

                    if (result is string jsDiagnostic && !string.IsNullOrWhiteSpace(jsDiagnostic))
                    {
                        return $"origen={FormatDiagnosticValue(origin)}; {jsDiagnostic}";
                    }
                }

                string tagName = candidate.TagName ?? "(sin tag)";
                string role = candidate.GetAttribute("role") ?? "(none)";
                string tabindex = candidate.GetAttribute("tabindex") ?? "(none)";
                string ariaLabel = candidate.GetAttribute("aria-label") ?? "(none)";
                var size = candidate.Size;
                string text = BuildTextPreview(ReadElementText(candidate), 80);
                return $"origen={FormatDiagnosticValue(origin)}; tag={tagName}; role={role}; tabindex={tabindex}; aria-label={ariaLabel}; texto={text}; size={size.Width}x{size.Height}; icono={FormatDiagnosticFlag(ElementContainsHeadphonesIcon(candidate))}; en-menu={FormatDiagnosticFlag(IsInsideAttachmentMenuOrDropdown(candidate))}";
            }
            catch (StaleElementReferenceException)
            {
                return $"origen={FormatDiagnosticValue(origin)}; stale";
            }
            catch (WebDriverException ex)
            {
                return $"origen={FormatDiagnosticValue(origin)}; no disponible ({SummarizeExceptionMessage(ex)})";
            }
        }

        private bool TryClickAudioOptionAndWaitForEffect(
            WebDriverWait wait,
            string fileName,
            out AudioOptionClickEffect effect,
            out string diagnostics)
        {
            effect = AudioOptionClickEffect.None;
            var attempts = new List<string>();
            bool reopenedMenu = false;
            var candidateBatchCounts = new List<int>();

            for (int menuAttempt = 0; menuAttempt < 2; menuAttempt++)
            {
                var candidates = FindAudioOptionCandidateInfos(driver);
                candidateBatchCounts.Add(candidates.Count);
                string candidateSummary = BuildAudioOptionCandidatesDiagnostics(candidates);
                LogAudioStage(AudioAttachStage.ClickAudioOption, candidateSummary);

                if (candidates.Count == 0)
                {
                    attempts.Add(candidateSummary);
                    if (!HasAttachmentMenuOpened(driver) && !reopenedMenu)
                    {
                        LogAudioStage(AudioAttachStage.ClickAudioOption, "el menu de adjuntos no tiene candidatos y parece cerrado; se reabrira una vez para recomputar candidatos");
                        if (!TryReopenAttachmentMenu(wait, out string reopenError))
                        {
                            attempts.Add($"reapertura del menu fallida: {reopenError}");
                            diagnostics = BuildAudioOptionClickAttemptsDiagnostics(attempts, candidateBatchCounts);
                            return false;
                        }

                        reopenedMenu = true;
                        continue;
                    }
                }

                bool recomputeCandidates = false;
                for (int index = 0; index < candidates.Count; index++)
                {
                    AudioOptionCandidateInfo candidate = candidates[index];
                    string candidateDiagnostic = GetAudioOptionCandidateDiagnostics(candidate);
                    LogAudioStage(AudioAttachStage.ClickAudioOption, $"intentando candidato Audio {index + 1}/{candidates.Count}: {candidateDiagnostic}");

                    string inputsBeforeClick = GetFileInputsSignature(driver);
                    IReadOnlySet<IntPtr> nativeDialogsBeforeClick = GetVisibleNativeDialogHandles();
                    if (!TryClickAudioOptionCandidate(candidate.Element, out string clickError))
                    {
                        string failedClickDiagnostic = $"#{index + 1}: {candidateDiagnostic} => click fallido ({clickError})";
                        attempts.Add(failedClickDiagnostic);
                        LogAudioStage(AudioAttachStage.ClickAudioOption, failedClickDiagnostic);
                        continue;
                    }

                    if (TryWaitForAudioOptionClickEffect(inputsBeforeClick, nativeDialogsBeforeClick, fileName, out effect, out string effectDiagnostic))
                    {
                        string successDiagnostic = $"#{index + 1}: {candidateDiagnostic} => {effectDiagnostic}";
                        attempts.Add(successDiagnostic);
                        LogAudioStage(AudioAttachStage.ClickAudioOption, $"click produjo efecto: {effectDiagnostic}");
                        diagnostics = BuildAudioOptionClickAttemptsDiagnostics(attempts, candidateBatchCounts);
                        return true;
                    }

                    string noOpDiagnostic = $"#{index + 1}: {candidateDiagnostic} => sin efecto ({effectDiagnostic})";
                    attempts.Add(noOpDiagnostic);
                    LogAudioStage(AudioAttachStage.ClickAudioOption, $"click sin efecto; {effectDiagnostic}");

                    if (!HasAttachmentMenuOpened(driver) && !reopenedMenu)
                    {
                        LogAudioStage(AudioAttachStage.ClickAudioOption, "el menu de adjuntos se cerro sin efecto; se reabrira una vez para recomputar candidatos");
                        if (!TryReopenAttachmentMenu(wait, out string reopenError))
                        {
                            attempts.Add($"reapertura del menu fallida: {reopenError}");
                            diagnostics = BuildAudioOptionClickAttemptsDiagnostics(attempts, candidateBatchCounts);
                            return false;
                        }

                        reopenedMenu = true;
                        recomputeCandidates = true;
                        break;
                    }
                }

                if (!recomputeCandidates)
                {
                    break;
                }
            }

            diagnostics = BuildAudioOptionClickAttemptsDiagnostics(attempts, candidateBatchCounts);
            return false;
        }

        private bool TryClickAudioOptionCandidate(IWebElement candidate, out string error)
        {
            error = "";
            WebDriverException? lastException = null;

            if (driver is IJavaScriptExecutor js)
            {
                try
                {
                    js.ExecuteScript("arguments[0].scrollIntoView({block:'center', inline:'center'});", candidate);
                }
                catch (WebDriverException ex) when (!IsBrowserUnavailableException(ex))
                {
                    lastException = ex;
                }
            }

            try
            {
                new Actions(driver).MoveToElement(candidate).Click().Perform();
                return true;
            }
            catch (WebDriverException ex) when (!IsBrowserUnavailableException(ex))
            {
                lastException = ex;
            }

            try
            {
                candidate.Click();
                return true;
            }
            catch (WebDriverException ex) when (!IsBrowserUnavailableException(ex))
            {
                lastException = ex;
            }

            if (driver is IJavaScriptExecutor fallbackJs)
            {
                try
                {
                    fallbackJs.ExecuteScript("arguments[0].click();", candidate);
                    return true;
                }
                catch (WebDriverException ex) when (!IsBrowserUnavailableException(ex))
                {
                    lastException = ex;
                }
            }

            error = SummarizeExceptionMessage(lastException) ?? "Selenium no pudo hacer click en el candidato Audio.";
            return false;
        }

        private bool TryWaitForAudioOptionClickEffect(
            string inputsBeforeClick,
            IReadOnlySet<IntPtr> nativeDialogsBeforeClick,
            string fileName,
            out AudioOptionClickEffect effect,
            out string diagnostic)
        {
            effect = AudioOptionClickEffect.None;
            diagnostic = "";
            DateTime startedAt = DateTime.UtcNow;
            DateTime deadline = DateTime.UtcNow.AddSeconds(5);
            WebDriverException? lastNonFatalException = null;
            string possibleDialogDiagnostic = "";

            while (DateTime.UtcNow < deadline)
            {
                try
                {
                    if (FindAudioFileInput(driver) != null)
                    {
                        effect = AudioOptionClickEffect.AudioInput;
                        diagnostic = $"aparecio input[type=file] compatible con audio; espera={FormatElapsedMilliseconds(startedAt)}";
                        return true;
                    }

                    if (TryFindNativeFileDialogStrict(out _, out string dialogTitle))
                    {
                        effect = AudioOptionClickEffect.NativeDialog;
                        diagnostic = $"aparecio dialogo nativo de archivo: {FormatDiagnosticValue(dialogTitle)}; espera={FormatElapsedMilliseconds(startedAt)}";
                        return true;
                    }

                    if (string.IsNullOrWhiteSpace(possibleDialogDiagnostic)
                        && TryFindPossibleNativeFileDialog(out _, out _, out string currentPossibleDialogDiagnostic, nativeDialogsBeforeClick))
                    {
                        possibleDialogDiagnostic = currentPossibleDialogDiagnostic;
                    }

                    if (FindAttachmentPreview(driver, fileName) != null || FindAttachmentPreview(driver, "") != null)
                    {
                        effect = AudioOptionClickEffect.AttachmentPreview;
                        diagnostic = $"aparecio preview de adjunto; espera={FormatElapsedMilliseconds(startedAt)}";
                        return true;
                    }

                    string inputsAfterClick = GetFileInputsSignature(driver);
                    if (!string.Equals(inputsBeforeClick, inputsAfterClick, StringComparison.Ordinal))
                    {
                        effect = AudioOptionClickEffect.FileInputsChanged;
                        diagnostic = $"cambiaron los inputs file: {inputsBeforeClick} -> {inputsAfterClick}; espera={FormatElapsedMilliseconds(startedAt)}";
                        return true;
                    }
                }
                catch (WebDriverException ex) when (!IsBrowserUnavailableException(ex))
                {
                    lastNonFatalException = ex;
                }

                Thread.Sleep(200);
            }

            if (!string.IsNullOrWhiteSpace(possibleDialogDiagnostic))
            {
                effect = AudioOptionClickEffect.PossibleNativeDialog;
                diagnostic = $"aparecio dialogo nativo posible dentro de la ventana post-click; espera={FormatElapsedMilliseconds(startedAt)}; {possibleDialogDiagnostic}";
                return true;
            }

            diagnostic = $"sin input audio, dialogo nativo estricto, dialogo posible, preview ni cambio de inputs file; timeout=5000ms; espera={FormatElapsedMilliseconds(startedAt)}; inputs actuales: {GetFileInputsOperationalSummary(driver)}; dialogo nativo: {GetNativeFileDialogDiagnostic(nativeDialogsBeforeClick)}";
            if (lastNonFatalException != null)
            {
                diagnostic += $"; ultimo error no fatal: {SummarizeExceptionMessage(lastNonFatalException)}";
            }

            return false;
        }

        private bool TryReopenAttachmentMenu(WebDriverWait wait, out string error)
        {
            error = "";

            try
            {
                IWebElement attachButton = wait.Until(ExpectedConditions.ElementToBeClickable(By.CssSelector(Selectors.AttachButtonCss)))
                    ?? throw new WebDriverTimeoutException("No se encontro el boton de adjuntar para reabrir el menu.");
                ClickElementSafely(attachButton);
                Thread.Sleep(300);
                return true;
            }
            catch (WebDriverException ex) when (!IsBrowserUnavailableException(ex))
            {
                error = SummarizeExceptionMessage(ex);
                return false;
            }
        }

        private static string GetFileInputsSignature(ISearchContext searchContext)
        {
            try
            {
                var accepts = searchContext
                    .FindElements(By.CssSelector(Selectors.FileInputCss))
                    .Select(input =>
                    {
                        try
                        {
                            return input.GetAttribute("accept") ?? "";
                        }
                        catch (StaleElementReferenceException)
                        {
                            return "(stale)";
                        }
                        catch (WebDriverException)
                        {
                            return "(no disponible)";
                        }
                    })
                    .ToList();

                return $"{accepts.Count}:{string.Join("|", accepts)}";
            }
            catch (WebDriverException ex)
            {
                return $"no disponible:{SummarizeExceptionMessage(ex)}";
            }
        }

        private static string BuildAudioOptionClickAttemptsDiagnostics(
            IReadOnlyList<string> attempts,
            IReadOnlyList<int> candidateBatchCounts)
        {
            string candidateCountSummary = candidateBatchCounts.Count == 0
                ? "candidatos encontrados por pasada: (sin busqueda)"
                : $"candidatos encontrados por pasada: {string.Join(", ", candidateBatchCounts)}";

            if (attempts.Count == 0)
            {
                return $"{candidateCountSummary}; candidatos Audio intentados: 0";
            }

            return $"{candidateCountSummary}; candidatos Audio intentados: {attempts.Count}; {string.Join(" | ", attempts.Take(8))}" +
                (attempts.Count > 8 ? $" | +{attempts.Count - 8} intentos mas" : "");
        }

        private static string FormatAudioOptionClickEffect(AudioOptionClickEffect effect)
        {
            return effect switch
            {
                AudioOptionClickEffect.AudioInput => "input audio",
                AudioOptionClickEffect.NativeDialog => "dialogo nativo",
                AudioOptionClickEffect.PossibleNativeDialog => "dialogo nativo posible",
                AudioOptionClickEffect.AttachmentPreview => "preview",
                AudioOptionClickEffect.FileInputsChanged => "cambio de inputs file",
                _ => "ninguno"
            };
        }

        private static string FormatElapsedMilliseconds(DateTime startedAt)
        {
            double elapsed = Math.Max(0, (DateTime.UtcNow - startedAt).TotalMilliseconds);
            return $"{elapsed:0}ms";
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

        private AudioSendCompletionResult VerifyAudioSendCompleted(
            WebDriverWait wait,
            string fileName,
            AudioOutgoingBaseline baseline)
        {
            _ = wait;
            DateTime startedAt = DateTime.UtcNow;
            DateTime deadline = startedAt.AddSeconds(45);
            bool nativeDialogDetected = false;
            bool nativeDialogCleanupAttempted = false;
            string nativeDialogDiagnostic = "";
            string lastOutgoingDiagnostic = "";
            WebDriverException? lastNonFatalException = null;

            while (DateTime.UtcNow < deadline)
            {
                try
                {
                    if (TryFindNativeFileDialog(out _, out _))
                    {
                        nativeDialogDetected = true;
                        nativeDialogDiagnostic = GetNativeFileDialogDiagnostic();
                    }
                    else if (TryFindValidatedPossibleNativeFileDialog(out _, out _, out string possibleDialogDiagnostic))
                    {
                        nativeDialogDetected = true;
                        nativeDialogDiagnostic = possibleDialogDiagnostic;
                    }

                    bool previewClosed = IsAttachmentPreviewClosed(fileName);
                    bool previewSendButtonGone = IsPreviewSendButtonGone(fileName);

                    if (TryDetectNewOutgoingAudioMessage(baseline, out string outgoingDiagnostic))
                    {
                        activeAttachmentPreview = previewClosed ? null : activeAttachmentPreview;
                        return new AudioSendCompletionResult
                        {
                            Success = true,
                            Reason = "mensaje saliente de audio detectado",
                            PreviewClosed = previewClosed,
                            OutgoingAudioDetected = true,
                            NativeDialogDetected = nativeDialogDetected,
                            NativeDialogCleanupAttempted = false,
                            Warning = previewClosed ? "" : "Audio confirmado por mensaje saliente; el preview/input tardó en cerrar.",
                            Diagnostic = $"espera={FormatElapsedMilliseconds(startedAt)}; {outgoingDiagnostic}; preview cerrado={FormatDiagnosticFlag(previewClosed)}; boton enviar preview ausente={FormatDiagnosticFlag(previewSendButtonGone)}"
                        };
                    }

                    if (previewClosed || previewSendButtonGone)
                    {
                        activeAttachmentPreview = null;
                        return new AudioSendCompletionResult
                        {
                            Success = true,
                            Reason = previewClosed ? "preview cerrado" : "boton enviar del preview desaparecio",
                            PreviewClosed = previewClosed,
                            OutgoingAudioDetected = false,
                            NativeDialogDetected = nativeDialogDetected,
                            NativeDialogCleanupAttempted = false,
                            Warning = "",
                            Diagnostic = $"espera={FormatElapsedMilliseconds(startedAt)}; preview cerrado={FormatDiagnosticFlag(previewClosed)}; boton enviar preview ausente={FormatDiagnosticFlag(previewSendButtonGone)}; baseline={baseline.Diagnostic}; ultimo diagnostico saliente={FormatDiagnosticValue(lastOutgoingDiagnostic)}"
                        };
                    }

                    lastOutgoingDiagnostic = CaptureOutgoingAudioBaseline().Diagnostic;
                }
                catch (WebDriverException ex) when (!IsBrowserUnavailableException(ex))
                {
                    lastNonFatalException = ex;
                }

                Thread.Sleep(300);
            }

            if (nativeDialogDetected && TryFindNativeFileDialog(out _, out _))
            {
                nativeDialogCleanupAttempted = true;
                if (TryCleanupNativeFileDialogAfterAudioSend(out string cleanupDiagnostic))
                {
                    nativeDialogDiagnostic = $"{nativeDialogDiagnostic}; limpieza post-timeout: {cleanupDiagnostic}";
                }
                else
                {
                    nativeDialogDiagnostic = $"{nativeDialogDiagnostic}; limpieza post-timeout fallida: {cleanupDiagnostic}";
                }
            }
            else if (nativeDialogDetected && TryFindValidatedPossibleNativeFileDialog(out _, out _, out _))
            {
                nativeDialogCleanupAttempted = true;
                if (TryDismissPossibleNativeFileDialog(out string cleanupDiagnostic))
                {
                    nativeDialogDiagnostic = $"{nativeDialogDiagnostic}; limpieza post-timeout: {cleanupDiagnostic}";
                }
                else
                {
                    nativeDialogDiagnostic = $"{nativeDialogDiagnostic}; limpieza post-timeout fallida: {cleanupDiagnostic}";
                }
            }

            string failureDiagnostic =
                $"timeout=45000ms; espera={FormatElapsedMilliseconds(startedAt)}; " +
                $"baseline={baseline.Diagnostic}; actual={CaptureOutgoingAudioBaseline().Diagnostic}; " +
                $"dialogo nativo detectado={FormatDiagnosticFlag(nativeDialogDetected)}; " +
                $"limpieza dialogo intentada={FormatDiagnosticFlag(nativeDialogCleanupAttempted)}; " +
                $"dialogo nativo={FormatDiagnosticValue(nativeDialogDiagnostic)}";
            if (lastNonFatalException != null)
            {
                failureDiagnostic += $"; ultimo error no fatal={SummarizeExceptionMessage(lastNonFatalException)}";
            }

            return new AudioSendCompletionResult
            {
                Success = false,
                Reason = "preview visible o no confirmado y sin mensaje saliente de audio",
                PreviewClosed = false,
                OutgoingAudioDetected = false,
                NativeDialogDetected = nativeDialogDetected,
                NativeDialogCleanupAttempted = nativeDialogCleanupAttempted,
                Warning = "",
                Diagnostic = failureDiagnostic
            };
        }

        private bool IsAttachmentPreviewClosed(string fileName)
        {
            try
            {
                bool trackedPreviewClosed = activeAttachmentPreview == null || IsElementGoneOrHidden(activeAttachmentPreview);
                bool noRecognizablePreview = FindAttachmentPreview(driver, fileName) == null && FindAttachmentPreview(driver, "") == null;
                return trackedPreviewClosed && noRecognizablePreview;
            }
            catch (WebDriverException ex) when (!IsBrowserUnavailableException(ex))
            {
                return false;
            }
        }

        private bool IsPreviewSendButtonGone(string fileName)
        {
            try
            {
                var currentPreview = FindAttachmentPreview(driver, fileName) ?? FindAttachmentPreview(driver, "");
                return currentPreview != null && FindPreviewSendButton(currentPreview) == null;
            }
            catch (StaleElementReferenceException)
            {
                return true;
            }
            catch (WebDriverException ex) when (!IsBrowserUnavailableException(ex))
            {
                return false;
            }
        }

        private AudioOutgoingBaseline CaptureOutgoingAudioBaseline()
        {
            try
            {
                var outgoingMessages = FindOutgoingMessageCandidates(driver);
                int outgoingAudioMessageCount = 0;
                int outgoingAudioControlCount = 0;
                string lastSignature = "";
                bool lastHasAudio = false;
                int lastAudioControlCount = 0;

                for (int index = 0; index < outgoingMessages.Count; index++)
                {
                    IWebElement message = outgoingMessages[index];
                    int controlCount = CountOutgoingAudioSignalElements(message);
                    bool hasAudio = controlCount > 0 || HasOutgoingAudioSignal(message);
                    if (hasAudio)
                    {
                        outgoingAudioMessageCount++;
                    }

                    outgoingAudioControlCount += controlCount;

                    if (index == outgoingMessages.Count - 1)
                    {
                        lastSignature = BuildOutgoingMessageSignature(message, index, hasAudio, controlCount);
                        lastHasAudio = hasAudio;
                        lastAudioControlCount = controlCount;
                    }
                }

                string diagnostic = BuildOutgoingBaselineDiagnostic(
                    outgoingMessages.Count,
                    outgoingAudioMessageCount,
                    outgoingAudioControlCount,
                    lastSignature,
                    lastHasAudio,
                    lastAudioControlCount);

                return new AudioOutgoingBaseline
                {
                    IsAvailable = true,
                    OutgoingMessageCount = outgoingMessages.Count,
                    OutgoingAudioMessageCount = outgoingAudioMessageCount,
                    OutgoingAudioControlCount = outgoingAudioControlCount,
                    LastOutgoingMessageSignature = lastSignature,
                    LastOutgoingMessageHasAudio = lastHasAudio,
                    LastOutgoingMessageAudioControlCount = lastAudioControlCount,
                    Diagnostic = diagnostic
                };
            }
            catch (WebDriverException ex) when (!IsBrowserUnavailableException(ex))
            {
                return new AudioOutgoingBaseline
                {
                    IsAvailable = false,
                    Diagnostic = $"no disponible ({SummarizeExceptionMessage(ex)})"
                };
            }
        }

        private bool TryDetectNewOutgoingAudioMessage(AudioOutgoingBaseline baseline, out string diagnostic)
        {
            AudioOutgoingBaseline current = CaptureOutgoingAudioBaseline();
            if (!baseline.IsAvailable || !current.IsAvailable)
            {
                diagnostic = $"baseline[{baseline.Diagnostic}]; actual[{current.Diagnostic}]; senales=no evaluables";
                return false;
            }

            var signals = new List<string>();

            if (current.OutgoingAudioMessageCount > baseline.OutgoingAudioMessageCount)
            {
                signals.Add($"mensajes salientes con audio {baseline.OutgoingAudioMessageCount}->{current.OutgoingAudioMessageCount}");
            }

            if (current.OutgoingAudioControlCount > baseline.OutgoingAudioControlCount)
            {
                signals.Add($"controles audio salientes {baseline.OutgoingAudioControlCount}->{current.OutgoingAudioControlCount}");
            }

            bool lastOutgoingChanged = !string.Equals(
                current.LastOutgoingMessageSignature,
                baseline.LastOutgoingMessageSignature,
                StringComparison.Ordinal);
            if (lastOutgoingChanged && current.LastOutgoingMessageHasAudio)
            {
                signals.Add("ultimo mensaje saliente cambio y contiene senales de audio");
            }

            if (current.OutgoingMessageCount > baseline.OutgoingMessageCount && current.LastOutgoingMessageHasAudio)
            {
                signals.Add($"mensajes salientes {baseline.OutgoingMessageCount}->{current.OutgoingMessageCount} y ultimo con audio");
            }

            diagnostic =
                $"baseline[{baseline.Diagnostic}]; actual[{current.Diagnostic}]; " +
                $"senales={FormatDiagnosticValue(signals.Count == 0 ? "ninguna" : string.Join(", ", signals))}";
            return signals.Count > 0;
        }

        private static IReadOnlyList<IWebElement> FindOutgoingMessageCandidates(ISearchContext searchContext)
        {
            var candidates = new List<IWebElement>();
            try
            {
                foreach (var candidate in searchContext.FindElements(By.XPath(Selectors.OutgoingMessageXPath)))
                {
                    if (!IsOutgoingMessageCandidateUsable(candidate) || ContainsEquivalentOutgoingElement(candidates, candidate))
                    {
                        continue;
                    }

                    candidates.Add(candidate);
                }
            }
            catch (StaleElementReferenceException)
            {
            }
            catch (WebDriverException)
            {
            }

            return candidates;
        }

        private static bool IsOutgoingMessageCandidateUsable(IWebElement candidate)
        {
            try
            {
                return candidate.Displayed && !IsInsideFooter(candidate);
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

        private static bool ContainsEquivalentOutgoingElement(IEnumerable<IWebElement> elements, IWebElement candidate)
        {
            foreach (var element in elements)
            {
                try
                {
                    if (element.Equals(candidate))
                    {
                        return true;
                    }
                }
                catch (WebDriverException)
                {
                }
            }

            return false;
        }

        private static bool HasOutgoingAudioSignal(IWebElement message)
        {
            return CountOutgoingAudioSignalElements(message) > 0;
        }

        private static int CountOutgoingAudioSignalElements(IWebElement message)
        {
            try
            {
                int count = 0;
                foreach (var signal in message.FindElements(By.XPath(Selectors.OutgoingAudioSignalXPath)))
                {
                    if (IsAudioSignalElementUsable(signal))
                    {
                        count++;
                    }
                }

                return count;
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

        private static bool IsAudioSignalElementUsable(IWebElement signal)
        {
            try
            {
                return string.Equals(signal.TagName, "audio", StringComparison.OrdinalIgnoreCase) || signal.Displayed;
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

        private static string BuildOutgoingMessageSignature(
            IWebElement message,
            int index,
            bool hasAudio,
            int audioControlCount)
        {
            try
            {
                string dataId = message.GetAttribute("data-id") ?? "";
                string dataPlainText = message.GetAttribute("data-pre-plain-text") ?? "";
                string role = message.GetAttribute("role") ?? "";
                string className = message.GetAttribute("class") ?? "";
                var size = message.Size;
                return $"{index}|{dataId}|{dataPlainText}|{role}|{className}|audio={hasAudio}|controls={audioControlCount}|size={size.Width}x{size.Height}";
            }
            catch (StaleElementReferenceException)
            {
                return $"{index}|stale|audio={hasAudio}|controls={audioControlCount}";
            }
            catch (WebDriverException)
            {
                return $"{index}|no-disponible|audio={hasAudio}|controls={audioControlCount}";
            }
        }

        private static string BuildOutgoingBaselineDiagnostic(
            int outgoingMessageCount,
            int outgoingAudioMessageCount,
            int outgoingAudioControlCount,
            string lastSignature,
            bool lastHasAudio,
            int lastAudioControlCount)
        {
            return
                $"mensajes salientes={outgoingMessageCount}; " +
                $"mensajes salientes audio={outgoingAudioMessageCount}; " +
                $"controles audio salientes={outgoingAudioControlCount}; " +
                $"ultimo hash={BuildDiagnosticFingerprint(lastSignature)}; " +
                $"ultimo audio={FormatDiagnosticFlag(lastHasAudio)}; " +
                $"ultimo controles audio={lastAudioControlCount}";
        }

        private static string BuildDiagnosticFingerprint(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "(ninguno)";
            }

            unchecked
            {
                ulong hash = 1469598103934665603UL;
                foreach (char character in value)
                {
                    hash ^= character;
                    hash *= 1099511628211UL;
                }

                return hash.ToString("X16");
            }
        }

        private WhatsAppSendResult? EnsureWhatsAppInteractableAfterAttachmentAttempt(
            WebDriverWait wait,
            Exception? attachmentException = null,
            string stage = AudioAttachStage.NativeDialogUpload)
        {
            bool strictNativeDialogDetected = TryFindNativeFileDialog(out _, out string dialogTitle);
            bool validatedPossibleNativeDialogDetected = false;
            string possibleDialogDiagnostic = "";
            if (!strictNativeDialogDetected)
            {
                validatedPossibleNativeDialogDetected = TryFindValidatedPossibleNativeFileDialog(out _, out dialogTitle, out possibleDialogDiagnostic);
            }

            if (!strictNativeDialogDetected && !validatedPossibleNativeDialogDetected)
            {
                return null;
            }

            LogAudioStage(stage, "Diálogo nativo de archivo detectado; intentando cerrar");
            bool dismissed = strictNativeDialogDetected
                ? TryDismissNativeFileDialog()
                : TryDismissPossibleNativeFileDialog(out _);

            if (dismissed && WaitForWhatsAppInteractable(wait))
            {
                var issue = GetHealthIssue();
                if (issue.IsReady)
                {
                    string recoveredMessage = BuildAudioStageFailureMessage(stage, "Diálogo nativo de archivo detectado; se cerró y se marcó el envío de audio como fallido.");
                    log(recoveredMessage);
                    return WhatsAppSendResult.Failure(
                        WhatsAppHealthStatus.WhatsAppAttachmentFailed,
                        recoveredMessage,
                        false,
                        attachmentException);
                }

                string notReadyAfterDismissMessage = BuildAudioStageFailureMessage(
                    stage,
                    "Diálogo nativo de archivo detectado y se intentó cerrarlo, " +
                    $"pero WhatsApp no volvió a estar listo. Estado: {issue.Status}. {issue.Message}");
                log(notReadyAfterDismissMessage);
                return WhatsAppSendResult.Failure(issue.Status, notReadyAfterDismissMessage, issue.IsGlobalFailure, attachmentException ?? issue.Exception);
            }

            string stillOpenTitle = TryFindNativeFileDialog(out _, out string currentDialogTitle)
                ? currentDialogTitle
                : dialogTitle;
            string unrecoveredMessage = BuildAudioStageFailureMessage(
                stage,
                "Diálogo nativo de archivo detectado durante el adjunto de audio, " +
                "pero no se pudo confirmar su cierre ni recuperar WhatsApp Web. " +
                $"Título detectado: {FormatDiagnosticValue(stillOpenTitle)}. {possibleDialogDiagnostic} Se detiene la corrida para evitar continuar en estado bloqueado.");
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

            bool possibleValidatedDialog = false;
            if (!TryFindNativeFileDialog(out IntPtr dialogHandle, out string dialogTitle))
            {
                if (!TryFindValidatedPossibleNativeFileDialog(out dialogHandle, out dialogTitle, out string possibleDiagnostic))
                {
                    return true;
                }

                possibleValidatedDialog = true;
                diagnostic = possibleDiagnostic;
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

                if (!IsNativeFileDialogStillPresent(possibleValidatedDialog))
                {
                    diagnostic = "Diálogo nativo cerrado con Escape.";
                    return true;
                }

                PostMessage(dialogHandle, WmClose, IntPtr.Zero, IntPtr.Zero);
                Thread.Sleep(1000);

                if (!IsNativeFileDialogStillPresent(possibleValidatedDialog))
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

        private static bool IsNativeFileDialogStillPresent(bool includeValidatedPossible)
        {
            if (TryFindNativeFileDialog(out _, out _))
            {
                return true;
            }

            return includeValidatedPossible
                && TryFindValidatedPossibleNativeFileDialog(out _, out _, out _);
        }

        private NativeDialogUploadResult TryUploadFileThroughNativeDialog(string fullAudioPath)
        {
            if (!TryFindNativeFileDialogStrict(out IntPtr dialogHandle, out string dialogTitle))
            {
                return new NativeDialogUploadResult
                {
                    Success = false,
                    Status = NativeDialogUploadStatus.Error,
                    Diagnostic = "No se detecto el dialogo nativo de archivo esperado."
                };
            }

            try
            {
                SetForegroundWindow(dialogHandle);
                Thread.Sleep(250);

                if (!TryPasteFilePathAndSubmit(fullAudioPath, out string pasteError))
                {
                    return new NativeDialogUploadResult
                    {
                        Success = false,
                        Status = NativeDialogUploadStatus.Error,
                        Diagnostic = pasteError
                    };
                }

                if (!WaitForNativeFileDialogToClose(dialogHandle, TimeSpan.FromSeconds(8)))
                {
                    return new NativeDialogUploadResult
                    {
                        Success = true,
                        Status = NativeDialogUploadStatus.DialogStillOpen,
                        Diagnostic = $"Dialogo nativo usado para cargar audio; se pego la ruta y se presiono Enter, pero el dialogo sigue abierto. Titulo: {FormatDiagnosticValue(dialogTitle)}."
                    };
                }

                return new NativeDialogUploadResult
                {
                    Success = true,
                    Status = NativeDialogUploadStatus.DialogClosed,
                    Diagnostic = $"Dialogo nativo usado para cargar audio; se pego la ruta, se presiono Enter y el dialogo se cerro solo. Titulo: {FormatDiagnosticValue(dialogTitle)}."
                };
            }
            catch (Exception ex) when (ex is InvalidOperationException or ExternalException or ThreadStateException)
            {
                return new NativeDialogUploadResult
                {
                    Success = false,
                    Status = NativeDialogUploadStatus.Error,
                    Diagnostic = $"No se pudo cargar el archivo en el dialogo nativo. Error: {SummarizeExceptionMessage(ex)}"
                };
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

        private WhatsAppSendResult PossibleNativeDialogFailure(WebDriverWait wait, string diagnostic, Exception? exception = null)
        {
            string message = BuildAudioStageFailureMessage(
                AudioAttachStage.NativeDialogUpload,
                "Se detecto un dialogo nativo posible despues del click en Audio, " +
                "pero no se pudo validarlo como selector de archivo de Chrome. No se pegara la ruta del audio. " +
                diagnostic);

            bool dismissed = TryDismissPossibleNativeFileDialog(out string dismissDiagnostic);
            if (!string.IsNullOrWhiteSpace(dismissDiagnostic))
            {
                log(dismissDiagnostic);
            }

            if (dismissed && WaitForWhatsAppInteractable(wait))
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
                "Se detecto un dialogo nativo posible no validado y WhatsApp no quedo interactuable despues del intento de recuperacion. " +
                $"{diagnostic}. Estado: {issue.Status}. {issue.Message}");
            log(notReadyMessage);
            return WhatsAppSendResult.Failure(issue.Status, notReadyMessage, issue.IsGlobalFailure, exception ?? issue.Exception);
        }

        private static bool TryDismissPossibleNativeFileDialog(out string diagnostic)
        {
            diagnostic = "";

            if (!TryFindPossibleNativeFileDialog(out IntPtr dialogHandle, out _, out string possibleDiagnostic))
            {
                diagnostic = "No se detectó diálogo nativo posible pendiente para cerrar.";
                return true;
            }

            NativeDialogWindowInfo? candidate = GetNativeFileDialogCandidates()
                .FirstOrDefault(window => window.Handle == dialogHandle);
            if (candidate == null || !CanSafelyDismissPossibleNativeDialog(candidate))
            {
                diagnostic = "No se cerró el diálogo nativo posible porque no está validado como selector de archivo de Chrome. " + possibleDiagnostic;
                return false;
            }

            SetForegroundWindow(dialogHandle);
            PostEscapeToWindow(dialogHandle);
            Thread.Sleep(1000);

            if (!TryFindPossibleNativeFileDialog(out _, out _, out _))
            {
                diagnostic = "Diálogo nativo posible cerrado con Escape.";
                return true;
            }

            diagnostic = "No se pudo cerrar con Escape el diálogo nativo posible. " + possibleDiagnostic;
            return false;
        }

        private static bool CanSafelyDismissPossibleNativeDialog(NativeDialogWindowInfo candidate)
        {
            return candidate.IsBrowserProcess && (candidate.HasPossibleTitle || candidate.HasStrictTitle);
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

        private static bool TryWaitForStrictNativeFileDialog(TimeSpan timeout, out IntPtr dialogHandle, out string title)
        {
            DateTime deadline = DateTime.UtcNow.Add(timeout);
            do
            {
                if (TryFindNativeFileDialogStrict(out dialogHandle, out title))
                {
                    return true;
                }

                Thread.Sleep(100);
            }
            while (DateTime.UtcNow < deadline);

            dialogHandle = IntPtr.Zero;
            title = "";
            return false;
        }

        private static bool TryFindNativeFileDialog(out IntPtr dialogHandle, out string title)
        {
            return TryFindNativeFileDialogStrict(out dialogHandle, out title);
        }

        private static bool TryFindNativeFileDialogStrict(out IntPtr dialogHandle, out string title)
        {
            var candidate = GetNativeFileDialogCandidates()
                .FirstOrDefault(window => window.HasStrictTitle && window.IsBrowserProcess);

            dialogHandle = candidate?.Handle ?? IntPtr.Zero;
            title = candidate?.Title ?? "";
            return dialogHandle != IntPtr.Zero;
        }

        private static bool TryFindPossibleNativeFileDialog(
            out IntPtr dialogHandle,
            out string title,
            out string diagnostic,
            IReadOnlySet<IntPtr>? handlesBeforeReference = null)
        {
            var allCandidates = GetNativeFileDialogCandidates(handlesBeforeReference);
            var candidates = allCandidates
                .Where(window => window.HasPossibleTitle || window.AppearedAfterReference)
                .OrderByDescending(window => window.IsBrowserProcess)
                .ThenByDescending(window => window.HasStrictTitle)
                .ThenByDescending(window => window.HasPossibleTitle)
                .ThenByDescending(window => window.AppearedAfterReference)
                .ToList();

            NativeDialogWindowInfo? candidate = candidates.FirstOrDefault();
            dialogHandle = candidate?.Handle ?? IntPtr.Zero;
            title = candidate?.Title ?? "";
            diagnostic = BuildNativeFileDialogDiagnostic(allCandidates, handlesBeforeReference);
            return dialogHandle != IntPtr.Zero;
        }

        private static bool TryFindValidatedPossibleNativeFileDialog(
            out IntPtr dialogHandle,
            out string title,
            out string diagnostic)
        {
            var allCandidates = GetNativeFileDialogCandidates();
            var candidate = allCandidates
                .Where(CanSafelyDismissPossibleNativeDialog)
                .OrderByDescending(window => window.HasStrictTitle)
                .ThenByDescending(window => window.HasPossibleTitle)
                .FirstOrDefault();

            dialogHandle = candidate?.Handle ?? IntPtr.Zero;
            title = candidate?.Title ?? "";
            diagnostic = BuildNativeFileDialogDiagnostic(allCandidates, null);
            return dialogHandle != IntPtr.Zero;
        }

        private static IReadOnlySet<IntPtr> GetVisibleNativeDialogHandles()
        {
            return GetNativeFileDialogCandidates()
                .Select(candidate => candidate.Handle)
                .ToHashSet();
        }

        private static IReadOnlyList<NativeDialogWindowInfo> GetNativeFileDialogCandidates(IReadOnlySet<IntPtr>? handlesBeforeReference = null)
        {
            var candidates = new List<NativeDialogWindowInfo>();

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
                uint processId = GetNativeWindowProcessId(hWnd);
                string processName = GetNativeWindowProcessName(processId);
                bool isBrowserProcess = IsBrowserNativeDialogProcess(processName);

                candidates.Add(new NativeDialogWindowInfo
                {
                    Handle = hWnd,
                    Title = windowTitle,
                    ClassName = className,
                    ProcessId = processId,
                    ProcessName = processName,
                    IsBrowserProcess = isBrowserProcess,
                    HasStrictTitle = IsStrictNativeFileDialogTitle(windowTitle),
                    HasPossibleTitle = IsPossibleNativeFileDialogTitle(windowTitle),
                    AppearedAfterReference = handlesBeforeReference != null && !handlesBeforeReference.Contains(hWnd)
                });

                return true;
            }, IntPtr.Zero);

            return candidates;
        }

        private static string GetNativeFileDialogDiagnostic(IReadOnlySet<IntPtr>? handlesBeforeReference = null)
        {
            var candidates = GetNativeFileDialogCandidates(handlesBeforeReference);
            return BuildNativeFileDialogDiagnostic(candidates, handlesBeforeReference);
        }

        private static string BuildNativeFileDialogDiagnostic(
            IReadOnlyList<NativeDialogWindowInfo> candidates,
            IReadOnlySet<IntPtr>? handlesBeforeReference)
        {
            string foreground = GetForegroundWindowDiagnostic();
            if (candidates.Count == 0)
            {
                return $"estricto=no; posible=no; candidatos #32770: 0; foreground: {foreground}";
            }

            bool strict = candidates.Any(candidate => candidate.HasStrictTitle && candidate.IsBrowserProcess);
            bool possible = candidates.Any(candidate => candidate.HasPossibleTitle || candidate.AppearedAfterReference);
            var details = candidates
                .Take(5)
                .Select(candidate => FormatNativeDialogCandidateDiagnostic(candidate))
                .ToList();

            if (candidates.Count > details.Count)
            {
                details.Add($"+{candidates.Count - details.Count} candidatos mas");
            }

            string reference = handlesBeforeReference == null
                ? "sin referencia post-click"
                : $"referencia post-click={handlesBeforeReference.Count} handles";

            return $"estricto={FormatDiagnosticFlag(strict)}; posible={FormatDiagnosticFlag(possible)}; {reference}; candidatos #32770: {candidates.Count}; {string.Join(" | ", details)}; foreground: {foreground}";
        }

        private static string FormatNativeDialogCandidateDiagnostic(NativeDialogWindowInfo candidate)
        {
            return
                $"handle={FormatWindowHandle(candidate.Handle)}, " +
                $"titulo={FormatDiagnosticValue(candidate.Title)}, " +
                $"clase={FormatDiagnosticValue(candidate.ClassName)}, " +
                $"pid={candidate.ProcessId}, " +
                $"proceso={FormatDiagnosticValue(candidate.ProcessName)}, " +
                $"browser={FormatDiagnosticFlag(candidate.IsBrowserProcess)}, " +
                $"tituloEstricto={FormatDiagnosticFlag(candidate.HasStrictTitle)}, " +
                $"tituloPosible={FormatDiagnosticFlag(candidate.HasPossibleTitle)}, " +
                $"aparecioPostClick={FormatDiagnosticFlag(candidate.AppearedAfterReference)}";
        }

        private static bool IsLikelyBrowserNativeDialog(IntPtr windowHandle)
        {
            uint processId = GetNativeWindowProcessId(windowHandle);
            string processName = GetNativeWindowProcessName(processId);
            return IsBrowserNativeDialogProcess(processName);
        }

        private static bool IsBrowserNativeDialogProcess(string processName)
        {
            return !string.IsNullOrWhiteSpace(processName)
                && ContainsAny(processName, BrowserNativeDialogProcessMarkers);
        }

        private static bool IsStrictNativeFileDialogTitle(string title)
        {
            string normalized = (title ?? "").Trim();
            return normalized.Equals("Abrir", StringComparison.OrdinalIgnoreCase)
                || normalized.Equals("Open", StringComparison.OrdinalIgnoreCase)
                || normalized.Contains("Abrir", StringComparison.OrdinalIgnoreCase)
                || normalized.Contains("Open", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsPossibleNativeFileDialogTitle(string title)
        {
            string normalized = (title ?? "").Trim();
            return !string.IsNullOrWhiteSpace(normalized)
                && ContainsAny(normalized, PossibleNativeFileDialogTitleMarkers);
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

        private static uint GetNativeWindowProcessId(IntPtr windowHandle)
        {
            try
            {
                _ = GetWindowThreadProcessId(windowHandle, out uint processId);
                return processId;
            }
            catch
            {
                return 0;
            }
        }

        private static string GetNativeWindowProcessName(uint processId)
        {
            if (processId == 0)
            {
                return "";
            }

            try
            {
                using Process process = Process.GetProcessById((int)processId);
                return process.ProcessName ?? "";
            }
            catch
            {
                return "";
            }
        }

        private static string GetForegroundWindowDiagnostic()
        {
            IntPtr foregroundHandle = GetForegroundWindow();
            if (foregroundHandle == IntPtr.Zero)
            {
                return "handle=(ninguno)";
            }

            uint processId = GetNativeWindowProcessId(foregroundHandle);
            string processName = GetNativeWindowProcessName(processId);
            return
                $"handle={FormatWindowHandle(foregroundHandle)}, " +
                $"titulo={FormatDiagnosticValue(GetNativeWindowText(foregroundHandle))}, " +
                $"clase={FormatDiagnosticValue(GetNativeWindowClassName(foregroundHandle))}, " +
                $"pid={processId}, " +
                $"proceso={FormatDiagnosticValue(processName)}";
        }

        private static string FormatWindowHandle(IntPtr handle)
        {
            return handle == IntPtr.Zero
                ? "0x0"
                : $"0x{handle.ToInt64():X}";
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
                    || FindAudioOptionCandidates(searchContext).Count > 0;
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
