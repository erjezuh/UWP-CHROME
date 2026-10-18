using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;

namespace XboxGamingBrowser
{
    sealed partial class App : Application
    {
        public App() { InitializeComponent(); Suspending += OnSuspending; }
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
            var window = Window.Current;
            if (window.Content == null) window.Content = new MainPage();
            window.Activate();
        }
        private void OnSuspending(object sender, Windows.ApplicationModel.SuspendingEventArgs e) { }
    }
}
