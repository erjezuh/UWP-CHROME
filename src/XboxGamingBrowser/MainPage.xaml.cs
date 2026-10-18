using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using System;
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
        public MainPage() { InitializeComponent(); Loaded += async (s,e) => { try { await Browser.EnsureCoreWebView2Async(); Browser.Source = home; } catch (Exception ex) { Address.Text = "WebView2 no disponible: " + ex.Message; } }; }
        private void Go_Click(object sender, RoutedEventArgs e) => Navigate(Address.Text);
        private void Address_KeyDown(object sender, KeyRoutedEventArgs e) { if (e.Key == VirtualKey.Enter) Navigate(Address.Text); }
        private void Navigate(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return;
            string value = raw.Trim();
            if (!value.Contains("://")) value = value.Contains(" ") ? "https://www.bing.com/search?q=" + Uri.EscapeDataString(value) : "https://" + value;
            Uri uri; if (!Uri.TryCreate(value, UriKind.Absolute, out uri) || (uri.Scheme != "https" && uri.Scheme != "http")) { Address.Text = "Dirección no válida (solo HTTP/HTTPS)"; return; }
            Browser.Source = uri;
        }
        private void Back_Click(object s,RoutedEventArgs e) { if (Browser.CanGoBack) Browser.GoBack(); }
        private void Forward_Click(object s,RoutedEventArgs e) { if (Browser.CanGoForward) Browser.GoForward(); }
        private void Home_Click(object s,RoutedEventArgs e) => Browser.Source = home;
        private void Diagnostics_Click(object s,RoutedEventArgs e) => Browser.Source = new Uri("ms-appx-web:///Web/diagnostics.html");
        private async void Memory_Click(object s, RoutedEventArgs e)
        {
            ulong used = Windows.System.MemoryManager.AppMemoryUsage;
            ulong limit = Windows.System.MemoryManager.AppMemoryUsageLimit;
            var dialog = new ContentDialog { Title = "Memoria de la aplicación", Content = string.Format("Uso reportado: {0:N0} MiB\nLímite actual: {1:N0} MiB\nNivel: {2}\n\nEsta API no indica RAM física libre ni memoria privada exacta del motor gráfico.", used / 1048576.0, limit / 1048576.0, Windows.System.MemoryManager.AppMemoryUsageLevel), CloseButtonText = "Cerrar" };
            await dialog.ShowAsync();
        }
        private void Browser_NavigationStarting(WebView2 s, CoreWebView2NavigationStartingEventArgs e) { Address.Text = e.Uri; }
        private void Browser_NavigationCompleted(WebView2 s, CoreWebView2NavigationCompletedEventArgs e) { if (!e.IsSuccess) Address.Text = "Error de navegación: " + e.WebErrorStatus; }
        private void Gaming_Click(object s,RoutedEventArgs e)
        {
            gaming = !gaming; Toolbar.Visibility = gaming ? Visibility.Collapsed : Visibility.Visible; ExitGaming.Visibility = gaming ? Visibility.Visible : Visibility.Collapsed;
            ApplicationView.GetForCurrentView().TryEnterFullScreenMode();
            if (!gaming) ApplicationView.GetForCurrentView().ExitFullScreenMode();
        }
        private async void ZoomIn_Click(object s,RoutedEventArgs e) { try { if (Browser.CoreWebView2 != null) await Browser.CoreWebView2.ExecuteScriptAsync("document.body.style.zoom=(parseFloat(document.body.style.zoom||1)+0.1).toString()"); } catch { } }
        private async void ZoomOut_Click(object s,RoutedEventArgs e) { try { if (Browser.CoreWebView2 != null) await Browser.CoreWebView2.ExecuteScriptAsync("document.body.style.zoom=(parseFloat(document.body.style.zoom||1)-0.1).toString()"); } catch { } }
    }
}
