using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using System;
using System.IO;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.ViewManagement;

namespace XboxGamingBrowser
{
    public sealed partial class MainPage : Page
    {
        private readonly Uri home = new Uri("ms-appx-web:///Web/home.html");
        private bool gaming;

        public MainPage()
        {
            InitializeComponent();
            Loaded += async (s, e) =>
            {
                try
                {
                    // Set before WebView2 creates CoreWebView2. If the bundled fixed
                    // runtime cannot start on this Xbox OS, retry using the OS runtime.
                    string bundledRuntime = ConfigureBundledRuntime();
                    try
                    {
                        await Browser.EnsureCoreWebView2Async();
                    }
                    catch (Exception) when (!string.IsNullOrEmpty(bundledRuntime))
                    {
                        Environment.SetEnvironmentVariable("WEBVIEW2_BROWSER_EXECUTABLE_FOLDER", null);
                        await Browser.EnsureCoreWebView2Async();
                    }
                    Browser.Source = home;
                }
                catch (Exception ex)
                {
                    Address.Text = "WebView2 no disponible: " + ex.Message;
                }
            };
        }

        private static string ConfigureBundledRuntime()
        {
            string fixedRuntimeRoot = Path.Combine(
                Windows.ApplicationModel.Package.Current.InstalledLocation.Path,
                "FixedRuntime");
            if (!Directory.Exists(fixedRuntimeRoot))
            {
                Environment.SetEnvironmentVariable("WEBVIEW2_BROWSER_EXECUTABLE_FOLDER", null);
                return null;
            }

            foreach (string runtimeDirectory in Directory.GetDirectories(fixedRuntimeRoot))
            {
                if (File.Exists(Path.Combine(runtimeDirectory, "msedgewebview2.exe")))
                {
                    Environment.SetEnvironmentVariable(
                        "WEBVIEW2_BROWSER_EXECUTABLE_FOLDER", runtimeDirectory);
                    return runtimeDirectory;
                }
            }

            Environment.SetEnvironmentVariable("WEBVIEW2_BROWSER_EXECUTABLE_FOLDER", null);
            return null;
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
                    Browser.Source = uri;
                    return;
                }
                if (value.Contains("://"))
                {
                    Address.Text = "Dirección no válida (solo HTTP/HTTPS)";
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
                Browser.Source = uri;
                return;
            }

            Browser.Source = BuildSearchUri(value);
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
            if (string.IsNullOrWhiteSpace(searchPrefix))
                searchPrefix = "https://www.google.com/search?q=";

            string url = searchPrefix + Uri.EscapeDataString(query);
            if (SearchEngine.SelectedIndex == 0)
                url += "&hl=es&gl=es";
            else if (SearchEngine.SelectedIndex == 1)
                url += "&setlang=es-ES";
            else if (SearchEngine.SelectedIndex == 2)
                url += "&kl=es-es";
            return new Uri(url, UriKind.Absolute);
        }

        private void Back_Click(object s, RoutedEventArgs e)
        {
            if (Browser.CanGoBack) Browser.GoBack();
        }

        private void Forward_Click(object s, RoutedEventArgs e)
        {
            if (Browser.CanGoForward) Browser.GoForward();
        }

        private void Home_Click(object s, RoutedEventArgs e) => Browser.Source = home;
        private void Diagnostics_Click(object s, RoutedEventArgs e) =>
            Browser.Source = new Uri("ms-appx-web:///Web/diagnostics.html");

        private async void Memory_Click(object s, RoutedEventArgs e)
        {
            ulong used = Windows.System.MemoryManager.AppMemoryUsage;
            ulong limit = Windows.System.MemoryManager.AppMemoryUsageLimit;
            string runtimeVersion = Browser.CoreWebView2 == null
                ? "WebView2 no inicializado"
                : Browser.CoreWebView2.Environment.BrowserVersionString;
            var dialog = new ContentDialog
            {
                Title = "Memoria y motor web",
                Content = string.Format(
                    "WebView2 Runtime: {0}\nUso reportado: {1:N0} MiB\nLímite actual: {2:N0} MiB\nNivel: {3}\n\nLa API de memoria no indica RAM física libre ni memoria privada exacta del motor gráfico.",
                    runtimeVersion,
                    used / 1048576.0,
                    limit / 1048576.0,
                    Windows.System.MemoryManager.AppMemoryUsageLevel),
                CloseButtonText = "Cerrar"
            };
            await dialog.ShowAsync();
        }

        private void Browser_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            Browser.Focus(FocusState.Pointer);
        }

        private void Browser_NavigationStarting(WebView2 s, CoreWebView2NavigationStartingEventArgs e)
        {
            Address.Text = e.Uri;
        }

        private void Browser_NavigationCompleted(WebView2 s, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (!e.IsSuccess)
                Address.Text = "Error de navegación: " + e.WebErrorStatus;
        }

        private void Gaming_Click(object s, RoutedEventArgs e)
        {
            gaming = !gaming;
            Toolbar.Visibility = gaming ? Visibility.Collapsed : Visibility.Visible;
            ExitGaming.Visibility = gaming ? Visibility.Visible : Visibility.Collapsed;
            ApplicationView.GetForCurrentView().TryEnterFullScreenMode();
            if (!gaming) ApplicationView.GetForCurrentView().ExitFullScreenMode();
        }

        private async void ZoomIn_Click(object s, RoutedEventArgs e)
        {
            try
            {
                if (Browser.CoreWebView2 != null)
                    await Browser.CoreWebView2.ExecuteScriptAsync(
                        "document.body.style.zoom=(parseFloat(document.body.style.zoom||1)+0.1).toString()");
            }
            catch { }
        }

        private async void ZoomOut_Click(object s, RoutedEventArgs e)
        {
            try
            {
                if (Browser.CoreWebView2 != null)
                    await Browser.CoreWebView2.ExecuteScriptAsync(
                        "document.body.style.zoom=(parseFloat(document.body.style.zoom||1)-0.1).toString()");
            }
            catch { }
        }
    }
}
