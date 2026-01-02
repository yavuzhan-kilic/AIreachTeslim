namespace AIreach_Mobile;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        // Hocam, uygulama ilk açıldığında hangi sayfayı göstereceğimizi burada belirliyoruz.
        // Modern bir yapı olduğu için 'AppShell' yapısını ana sayfa olarak atadım.
        // Böylece alt menü (Tabbar) ve navigasyon otomatik kurulmuş oluyor.
        MainPage = new AppShell();
    }
}