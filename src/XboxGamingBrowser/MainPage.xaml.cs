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
        private string runtimeStartupInfo = "Inicialización aún no iniciada";

        public MainPage()
        {
            InitializeComponent();
            Loaded += async (s, e) => await InitializeBrowserAsync();
        }

        private async System.Threading.Tasks.Task InitializeBrowserAsync()
        {
            try
            {
                await InitializeBrowserCoreAsync();
            }
            catch (Exception ex)
            {
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
            SetStatus("INICIANDO MOTOR…", "#F4C95D");
            Log("Arranque de la aplicación.");
            Log("Xbox/Windows build: " + GetOsVersion());
            Log("Paquete: " + GetPackageVersion());

            string bundledRuntime = null;
            try
            {
                bundledRuntime = FindBundledRuntime();
                Log(string.IsNullOrEmpty(bundledRuntime)
                    ? "Runtime fijo incluido: no encontrado en el paquete."
                    : "Runtime fijo incluido detectado: " + Path.GetFileName(bundledRuntime) + ".");
            }
            catch (Exception ex)
            {
                LogException("No se pudo inspeccionar el runtime incluido", ex);
            }

            Exception systemRuntimeError = null;
            Environment.SetEnvironmentVariable("WEBVIEW2_BROWSER_EXECUTABLE_FOLDER", null);
            Log("Intentando inicializar WebView2 con el runtime de Xbox/sistema.");
            try
            {
                await Browser.EnsureCoreWebView2Async();
            }
            catch (Exception ex)
            {
                systemRuntimeError = ex;
                LogException("Falló la inicialización con el runtime de Xbox/sistema", ex);
            }

            if (Browser.CoreWebView2 == null && !string.IsNullOrEmpty(bundledRuntime))
            {
                Log("WebView2 del sistema no quedó disponible; intentando runtime fijo incluido.");
                try
                {
                    Environment.SetEnvironmentVariable("WEBVIEW2_BROWSER_EXECUTABLE_FOLDER", bundledRuntime);
                    await Browser.EnsureCoreWebView2Async();
                }
                catch (Exception ex)
                {
                    LogException("Falló la inicialización con el runtime fijo incluido", ex);
                }
            }

            if (Browser.CoreWebView2 != null)
            {
                legacyMode = false;
                Browser.Visibility = Visibility.Visible;
                LegacyBrowser.Visibility = Visibility.Collapsed;
                string version = Browser.CoreWebView2.Environment.BrowserVersionString;
                runtimeStartupInfo = "WebView2 listo; versión " + version;
                Log(runtimeStartupInfo);
                SetStatus("WEBVIEW2 · " + version, "#9FE870");
                NavigateTo(home);
                return;
            }

            if (systemRuntimeError != null)
                runtimeStartupInfo = "WebView2 no disponible: " + systemRuntimeError.Message;
            else
                runtimeStartupInfo = "WebView2 no disponible: no se creó CoreWebView2.";
            Log("WebView2 no arrancó. Activando el control UWP WebView integrado como alternativa limitada.");

            try
            {
                legacyMode = true;
                LegacyBrowser.Visibility = Visibility.Visible;
                Browser.Visibility = Visibility.Collapsed;
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
        }

        private void LogException(string context, Exception ex)
        {
            Log(context + ": " + ex.GetType().FullName + " | " + ex.Message + " | HRESULT 0x" + Marshal.GetHRForException(ex).ToString("X8"));
            if (!string.IsNullOrWhiteSpace(ex.StackTrace)) Log(ex.StackTrace);
            if (ex.InnerException != null)
                Log("Inner exception: " + ex.InnerException.GetType().FullName + " | " + ex.InnerException.Message
                    + " | HRESULT 0x" + Marshal.GetHRForException(ex.InnerException).ToString("X8"));
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
                "Paquete: {0}\nXbox/Windows: {1}\nMotor: {2}\nUso reportado: {3:N0} MiB\nLímite actual: {4:N0} MiB\nNivel: {5}\n\nLa API no informa RAM física libre ni memoria privada exacta del motor gráfico.",
                GetPackageVersion(), GetOsVersion(), runtimeStartupInfo,
                used / 1048576.0, limit / 1048576.0, MemoryManager.AppMemoryUsageLevel);
            var dialog = new ContentDialog { Title = "Estado del navegador", Content = content, CloseButtonText = "Cerrar" };
            await dialog.ShowAsync();
        }

        private async void Logs_Click(object s, RoutedEventArgs e)
        {
            Log("Panel de logs abierto.");
            var logBox = new TextBox
            {
                Text = string.Join("\n", logEntries),
                IsReadOnly = true,
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                FontFamily = new Windows.UI.Xaml.Media.FontFamily("Consolas"),
                MinWidth = 620,
                MaxWidth = 1000,
                MinHeight = 300,
                MaxHeight = 520,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            var dialog = new ContentDialog
            {
                Title = "Registros de diagnóstico",
                Content = logBox,
                PrimaryButtonText = "Copiar logs",
                CloseButtonText = "Cerrar",
                DefaultButton = ContentDialogButton.Primary
            };
            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                var data = new DataPackage();
                data.SetText(string.Join("\n", logEntries));
                Clipboard.SetContent(data);
                Log("Logs copiados al portapapeles.");
            }
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
