using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Windows.ApplicationModel;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;
using Windows.System.Profile;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.ViewManagement;

namespace XboxGamingBrowser
{
    public sealed partial class MainPage : Page
    {
        private readonly Uri home = new Uri("ms-appx-web:///Web/home.html");
        private readonly List<string> logEntries = new List<string>();
        private bool gaming;
        private bool initializationStarted;
        private bool legacyMode;
        private bool webView2TimedOut;
        private Exception webView2StartupError;
        private string currentStartupStage = "Constructor pendiente";
        private string runtimeStartupInfo = "Inicialización aún no iniciada";

        public MainPage()
        {
            InitializeComponent();
            Browser.CoreWebView2Initialized += Browser_CoreWebView2Initialized;
            Browser.CoreProcessFailed += Browser_CoreProcessFailed;
            LoadStoredLogs();
            Log("Ventana creada; esperando evento Loaded.");
            Loaded += async (s, e) =>
            {
                Log("Evento Loaded recibido.");
                await InitializeBrowserAsync();
            };
        }

        private async System.Threading.Tasks.Task InitializeBrowserAsync()
        {
            try
            {
                await InitializeBrowserCoreAsync();
            }
            catch (Exception ex)
            {
                currentStartupStage = "Error no controlado durante la inicialización";
                runtimeStartupInfo = "Error no controlado al iniciar el navegador: " + ex.Message;
                LogException("Error no controlado durante la inicialización", ex);
                try
                {
                    SetStatus("ERROR AL INICIAR MOTOR", "#FF7777");
                    Address.Text = runtimeStartupInfo + " Abre Logs para ver el detalle.";
                }
                catch { }
            }
        }

        private async System.Threading.Tasks.Task InitializeBrowserCoreAsync()
        {
            if (initializationStarted) return;
            initializationStarted = true;
            SetStage("Preparando diagnóstico");
            Log("Arranque de la aplicación.");
            SetStage("Leyendo versión del sistema");
            string osVersion = GetOsVersion();
            Log("Xbox/Windows build: " + osVersion);
            SetStage("Leyendo versión del paquete");
            Log("Paquete: " + GetPackageVersion());

            string bundledRuntime = null;
            try
            {
                SetStage("Inspeccionando runtime fijo incluido");
                bundledRuntime = FindBundledRuntime();
                Log(string.IsNullOrEmpty(bundledRuntime)
                    ? "Runtime fijo incluido: no encontrado en el paquete."
                    : "Runtime fijo incluido detectado: " + Path.GetFileName(bundledRuntime) + ".");
            }
            catch (Exception ex)
            {
                LogException("No se pudo inspeccionar el runtime incluido", ex);
            }

            // Select the packaged Fixed Version before the first CoreWebView2 creation.
            // Changing this variable after a failed system-runtime attempt is too late for
            // reliable runtime selection, so choose one environment up front.
            if (!string.IsNullOrEmpty(bundledRuntime))
            {
                Environment.SetEnvironmentVariable("WEBVIEW2_BROWSER_EXECUTABLE_FOLDER", bundledRuntime);
                Log("Runtime fijo seleccionado ANTES del primer EnsureCoreWebView2Async: " + Path.GetFileName(bundledRuntime) + ".");
            }
            else
            {
                Environment.SetEnvironmentVariable("WEBVIEW2_BROWSER_EXECUTABLE_FOLDER", null);
                Log("No hay runtime fijo; se usará el runtime del sistema.");
            }

            SetStage("Preparando el control WebView2 visible");
            Browser.Visibility = Visibility.Visible;
            LegacyBrowser.Visibility = Visibility.Collapsed;
            StartupOverlay.Visibility = Visibility.Visible;
            Browser.UpdateLayout();
            Log("Control WebView2 visible y layout actualizado antes de EnsureCoreWebView2Async.");

            string selectedRuntime = string.IsNullOrEmpty(bundledRuntime)
                ? "runtime de Xbox/sistema"
                : "runtime fijo incluido " + Path.GetFileName(bundledRuntime);
            SetStage("Inicializando " + selectedRuntime + " · límite 20 s");
            Log("Invocando WebView2 con " + selectedRuntime + " (máximo 20 s).");
            await TryInitializeWebView2Async(selectedRuntime, 20000);

            if (Browser.CoreWebView2 != null)
            {
                legacyMode = false;
                Browser.Visibility = Visibility.Visible;
                LegacyBrowser.Visibility = Visibility.Collapsed;
                StartupOverlay.Visibility = Visibility.Collapsed;
                string version = Browser.CoreWebView2.Environment.BrowserVersionString;
                runtimeStartupInfo = "WebView2 listo; versión " + version;
                currentStartupStage = "WebView2 listo";
                Log(runtimeStartupInfo);
                SetStatus("WEBVIEW2 · " + version, "#9FE870");
                NavigateTo(home);
                return;
            }

            if (webView2TimedOut)
                runtimeStartupInfo = "WebView2 no respondió dentro del tiempo límite.";
            else if (webView2StartupError != null)
                runtimeStartupInfo = "WebView2 no disponible: " + webView2StartupError.Message;
            else
                runtimeStartupInfo = "WebView2 no disponible: no se creó CoreWebView2.";
            SetStage("Activando motor alternativo UWP WebView");
            Log("WebView2 no arrancó. Activando el control UWP WebView integrado como alternativa limitada.");

            try
            {
                legacyMode = true;
                LegacyBrowser.Visibility = Visibility.Visible;
                Browser.Visibility = Visibility.Collapsed;
                StartupOverlay.Visibility = Visibility.Collapsed;
                runtimeStartupInfo += " Se usa UWP WebView (motor heredado; no equivalente a Chromium).";
                SetStatus("MOTOR ALTERNATIVO · UWP WEBVIEW", "#F4C95D");
                LegacyBrowser.Navigate(home);
                Log("UWP WebView activado; navegación inicial solicitada.");
            }
            catch (Exception ex)
            {
                runtimeStartupInfo += " También falló el motor alternativo: " + ex.Message;
                SetStatus("ERROR AL INICIAR MOTOR", "#FF7777");
                LogException("Falló también el control UWP WebView", ex);
                Address.Text = runtimeStartupInfo;
            }
        }

        private async System.Threading.Tasks.Task<bool> TryInitializeWebView2Async(string runtimeName, int timeoutMilliseconds)
        {
            DateTime startedAt = DateTime.UtcNow;
            System.Threading.Tasks.Task initialization;
            try
            {
                Log("Invocando EnsureCoreWebView2Async: " + runtimeName + ".");
                initialization = Browser.EnsureCoreWebView2Async().AsTask();
            }
            catch (Exception ex)
            {
                webView2StartupError = ex;
                LogException("Error inmediato al invocar WebView2 (" + runtimeName + ")", ex);
                return false;
            }

            System.Threading.Tasks.Task timeout = System.Threading.Tasks.Task.Delay(timeoutMilliseconds);
            if (await System.Threading.Tasks.Task.WhenAny(initialization, timeout) != initialization)
            {
                webView2TimedOut = true;
                Log("TIMEOUT: WebView2 no respondió en " + timeoutMilliseconds + " ms (" + runtimeName + "); tiempo medido " + (DateTime.UtcNow - startedAt).TotalMilliseconds.ToString("F0") + " ms. Se cambiará al motor alternativo.");
                return false;
            }

            try
            {
                await initialization;
                if (Browser.CoreWebView2 != null)
                {
                    Log("Inicialización WebView2 completada con " + runtimeName + " en " + (DateTime.UtcNow - startedAt).TotalMilliseconds.ToString("F0") + " ms.");
                    return true;
                }
                Log("La inicialización terminó sin excepción, pero CoreWebView2 sigue siendo null (" + runtimeName + ").");
            }
            catch (Exception ex)
            {
                webView2StartupError = ex;
                LogException("Falló la inicialización WebView2 (" + runtimeName + ")", ex);
            }
            return false;
        }

        private void LoadStoredLogs()
        {
            try
            {
                string path = GetLogFilePath();
                if (!File.Exists(path)) return;
                string[] previous = File.ReadAllLines(path);
                int start = Math.Max(0, previous.Length - 199);
                for (int i = start; i < previous.Length; i++) logEntries.Add(previous[i]);
                Log("Se recuperaron " + logEntries.Count + " líneas de sesiones anteriores.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("No se pudieron recuperar logs: " + ex.Message);
            }
        }

        private string GetLogFilePath()
        {
            return Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, "browser-diagnostics.log");
        }

        private static string FindBundledRuntime()
        {
            string fixedRuntimeRoot = Path.Combine(
                Package.Current.InstalledLocation.Path,
                "FixedRuntime");
            if (!Directory.Exists(fixedRuntimeRoot)) return null;

            foreach (string runtimeDirectory in Directory.GetDirectories(fixedRuntimeRoot))
            {
                if (File.Exists(Path.Combine(runtimeDirectory, "msedgewebview2.exe")))
                    return runtimeDirectory;
            }
            return null;
        }

        private void SetStage(string stage)
        {
            currentStartupStage = stage;
            Log("ETAPA DE ARRANQUE: " + stage);
            SetStatus("INICIANDO · " + stage, "#F4C95D");
        }

        private void SetStatus(string text, string color)
        {
            StatusText.Text = text;
            StatusText.Foreground = new Windows.UI.Xaml.Media.SolidColorBrush(ParseColor(color));
        }

        private static Windows.UI.Color ParseColor(string value)
        {
            return Windows.UI.ColorHelper.FromArgb(255,
                Convert.ToByte(value.Substring(1, 2), 16),
                Convert.ToByte(value.Substring(3, 2), 16),
                Convert.ToByte(value.Substring(5, 2), 16));
        }

        private string GetPackageVersion()
        {
            try
            {
                var version = Package.Current.Id.Version;
                return string.Format("{0}.{1}.{2}.{3}", version.Major, version.Minor, version.Build, version.Revision);
            }
            catch { return "desconocida"; }
        }

        private string GetOsVersion()
        {
            try
            {
                ulong raw = ulong.Parse(AnalyticsInfo.VersionInfo.DeviceFamilyVersion);
                return string.Format("{0}.{1}.{2}.{3}",
                    (raw >> 48) & 0xffff, (raw >> 32) & 0xffff,
                    (raw >> 16) & 0xffff, raw & 0xffff);
            }
            catch { return AnalyticsInfo.VersionInfo.DeviceFamily + " (build desconocida)"; }
        }

        private void Log(string message)
        {
            string line = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  " + message;
            logEntries.Add(line);
            if (logEntries.Count > 200) logEntries.RemoveAt(0);
            System.Diagnostics.Debug.WriteLine("[XboxGamingBrowser] " + line);
            try
            {
                string path = GetLogFilePath();
                File.AppendAllText(path, line + Environment.NewLine);
                if (new FileInfo(path).Length > 262144)
                    File.WriteAllLines(path, logEntries);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("No se pudo guardar log local: " + ex.Message);
            }
        }

        private void LogException(string context, Exception ex)
        {
            Log(context + " | HRESULT 0x" + Marshal.GetHRForException(ex).ToString("X8") + "\n" + ex.ToString());
            Exception inner = ex.InnerException;
            int depth = 0;
            while (inner != null && depth < 5)
            {
                Log("Inner[" + depth + "] HRESULT 0x" + Marshal.GetHRForException(inner).ToString("X8")
                    + " | " + inner.GetType().FullName + " | " + inner.Message);
                inner = inner.InnerException;
                depth++;
            }
        }

        private void Go_Click(object sender, RoutedEventArgs e) => Navigate(Address.Text);

        private void Address_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                e.Handled = true;
                Navigate(Address.Text);
            }
        }

        private void Navigate(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return;
            string value = raw.Trim();
            Uri uri;
            if (Uri.TryCreate(value, UriKind.Absolute, out uri))
            {
                if (uri.Scheme == "https" || uri.Scheme == "http")
                {
                    NavigateTo(uri);
                    return;
                }
                if (value.Contains("://"))
                {
                    Address.Text = "Dirección no válida (solo HTTP/HTTPS)";
                    Log("Navegación bloqueada: esquema no HTTP/HTTPS.");
                    return;
                }
            }

            if (LooksLikeWebAddress(value))
            {
                if (!Uri.TryCreate("https://" + value, UriKind.Absolute, out uri))
                {
                    Address.Text = "Dirección no válida";
                    return;
                }
                NavigateTo(uri);
                return;
            }
            NavigateTo(BuildSearchUri(value));
        }

        private void NavigateTo(Uri uri)
        {
            if (uri == null) return;
            if (legacyMode) LegacyBrowser.Navigate(uri);
            else if (Browser.CoreWebView2 != null) Browser.Source = uri;
            else
            {
                Log("Navegación cancelada: no hay un motor inicializado.");
                Address.Text = "No hay un motor web inicializado. Abre Logs para ver el error.";
                return;
            }
            Log("Navegación solicitada: " + uri.Scheme + "://" + uri.Host + "/");
        }

        private bool LooksLikeWebAddress(string value)
        {
            if (value.Contains(" ") || !value.Contains(".")) return false;
            Uri candidate;
            return Uri.TryCreate("https://" + value, UriKind.Absolute, out candidate)
                && candidate.Host.Contains(".");
        }

        private Uri BuildSearchUri(string query)
        {
            ComboBoxItem selected = SearchEngine.SelectedItem as ComboBoxItem;
            string searchPrefix = selected == null ? null : selected.Tag as string;
            if (string.IsNullOrWhiteSpace(searchPrefix)) searchPrefix = "https://www.google.com/search?q=";
            string url = searchPrefix + Uri.EscapeDataString(query);
            if (SearchEngine.SelectedIndex == 0) url += "&hl=es&gl=es";
            else if (SearchEngine.SelectedIndex == 1) url += "&setlang=es-ES";
            else if (SearchEngine.SelectedIndex == 2) url += "&kl=es-es";
            return new Uri(url, UriKind.Absolute);
        }

        private void Back_Click(object s, RoutedEventArgs e)
        {
            if (legacyMode ? LegacyBrowser.CanGoBack : Browser.CanGoBack) GoBack();
        }

        private void GoBack()
        {
            if (legacyMode) LegacyBrowser.GoBack();
            else if (Browser.CanGoBack) Browser.GoBack();
        }

        private void Forward_Click(object s, RoutedEventArgs e)
        {
            if (legacyMode)
            {
                if (LegacyBrowser.CanGoForward) LegacyBrowser.GoForward();
            }
            else if (Browser.CanGoForward) Browser.GoForward();
        }

        private void Home_Click(object s, RoutedEventArgs e) => NavigateTo(home);
        private void Diagnostics_Click(object s, RoutedEventArgs e) => NavigateTo(new Uri("ms-appx-web:///Web/diagnostics.html"));

        private async void Memory_Click(object s, RoutedEventArgs e)
        {
            ulong used = MemoryManager.AppMemoryUsage;
            ulong limit = MemoryManager.AppMemoryUsageLimit;
            string content = string.Format(
                "Paquete: {0}\nXbox/Windows: {1}\nFase actual: {2}\nMotor: {3}\nUso reportado: {4:N0} MiB\nLímite actual: {5:N0} MiB\nNivel: {6}\n\nLa API no informa RAM física libre ni memoria privada exacta del motor gráfico.",
                GetPackageVersion(), GetOsVersion(), currentStartupStage, runtimeStartupInfo,
                used / 1048576.0, limit / 1048576.0, MemoryManager.AppMemoryUsageLevel);
            var dialog = new ContentDialog { Title = "Estado del navegador", Content = content, CloseButtonText = "Cerrar" };
            await dialog.ShowAsync();
        }

        private async void Logs_Click(object s, RoutedEventArgs e)
        {
            Log("Panel de logs abierto.");
            string diagnosticText = string.Format(
                "Paquete: {0}\nFase actual: {1}\nEstado del motor: {2}\nLíneas de registro: {3}\n\n{4}",
                GetPackageVersion(), currentStartupStage, runtimeStartupInfo, logEntries.Count,
                logEntries.Count == 0 ? "(No hay entradas de log en memoria.)" : string.Join("\n", logEntries));
            var logText = new TextBlock
            {
                Text = diagnosticText,
                TextWrapping = TextWrapping.Wrap,
                FontFamily = new Windows.UI.Xaml.Media.FontFamily("Consolas"),
                FontSize = 15,
                Foreground = new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Colors.White)
            };
            var logViewer = new ScrollViewer
            {
                Content = logText,
                MinWidth = 620,
                MaxWidth = 1000,
                MinHeight = 300,
                MaxHeight = 520,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            };
            var dialog = new ContentDialog
            {
                Title = "Diagnóstico · " + currentStartupStage,
                Content = logViewer,
                PrimaryButtonText = "Copiar logs",
                CloseButtonText = "Cerrar",
                DefaultButton = ContentDialogButton.Primary
            };
            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                var data = new DataPackage();
                data.SetText(diagnosticText);
                Clipboard.SetContent(data);
                Log("Logs copiados al portapapeles.");
            }
        }

        private void Browser_CoreWebView2Initialized(WebView2 sender, CoreWebView2InitializedEventArgs e)
        {
            if (e.Exception != null)
            {
                webView2StartupError = e.Exception;
                LogException("CoreWebView2Initialized informó fallo", e.Exception);
                return;
            }
            if (sender.CoreWebView2 == null)
                Log("CoreWebView2Initialized se disparó sin excepción, pero CoreWebView2 es null.");
            else
                Log("CoreWebView2Initialized correcto; versión " + sender.CoreWebView2.Environment.BrowserVersionString);
        }

        private void Browser_CoreProcessFailed(WebView2 sender, CoreWebView2ProcessFailedEventArgs e)
        {
            Log("Proceso WebView2 fallido: " + e.ProcessFailedKind + "; razón " + e.Reason);
        }

        private void Browser_PointerPressed(object sender, PointerRoutedEventArgs e) => Browser.Focus(FocusState.Pointer);
        private void LegacyBrowser_PointerPressed(object sender, PointerRoutedEventArgs e) => LegacyBrowser.Focus(FocusState.Pointer);

        private void Browser_NavigationStarting(WebView2 s, CoreWebView2NavigationStartingEventArgs e)
        {
            Address.Text = e.Uri;
            Log("WebView2 navegación iniciada: " + SafeHost(e.Uri));
        }

        private void Browser_NavigationCompleted(WebView2 s, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (!e.IsSuccess)
            {
                Address.Text = "Error de navegación: " + e.WebErrorStatus;
                Log("WebView2 navegación falló: " + e.WebErrorStatus);
            }
            else Log("WebView2 navegación completada.");
        }

        private void LegacyBrowser_NavigationStarting(WebView s, WebViewNavigationStartingEventArgs e)
        {
            Address.Text = e.Uri == null ? "" : e.Uri.ToString();
            Log("UWP WebView navegación iniciada: " + (e.Uri == null ? "URL desconocida" : SafeHost(e.Uri.ToString())));
        }

        private void LegacyBrowser_NavigationCompleted(WebView s, WebViewNavigationCompletedEventArgs e)
        {
            if (!e.IsSuccess)
            {
                Address.Text = "Error de navegación UWP: " + e.WebErrorStatus;
                Log("UWP WebView navegación falló: " + e.WebErrorStatus);
            }
            else Log("UWP WebView navegación completada.");
        }

        private string SafeHost(string value)
        {
            Uri uri;
            return Uri.TryCreate(value, UriKind.Absolute, out uri) ? uri.Host : "URL local/desconocida";
        }

        private void Gaming_Click(object s, RoutedEventArgs e)
        {
            gaming = !gaming;
            Toolbar.Visibility = gaming ? Visibility.Collapsed : Visibility.Visible;
            ExitGaming.Visibility = gaming ? Visibility.Visible : Visibility.Collapsed;
            if (gaming) ApplicationView.GetForCurrentView().TryEnterFullScreenMode();
            else ApplicationView.GetForCurrentView().ExitFullScreenMode();
        }

        private async void ZoomIn_Click(object s, RoutedEventArgs e) => await ChangeZoomAsync(0.1);
        private async void ZoomOut_Click(object s, RoutedEventArgs e) => await ChangeZoomAsync(-0.1);

        private async System.Threading.Tasks.Task ChangeZoomAsync(double amount)
        {
            const string scriptStart = "document.body.style.zoom=(parseFloat(document.body.style.zoom||1)+(";
            string script = scriptStart + amount.ToString(System.Globalization.CultureInfo.InvariantCulture) + ")).toString()";
            try
            {
                if (legacyMode) await LegacyBrowser.InvokeScriptAsync("eval", new[] { script });
                else if (Browser.CoreWebView2 != null) await Browser.CoreWebView2.ExecuteScriptAsync(script);
            }
            catch (Exception ex) { LogException("No se pudo cambiar el zoom", ex); }
        }
    }
}
