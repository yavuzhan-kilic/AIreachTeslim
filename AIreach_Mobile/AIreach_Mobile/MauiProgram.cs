using Microsoft.Extensions.Logging;
using Plugin.Maui.OCR;
using AIreach_Mobile.Services; // Servis katmanının (Service Layer) projeye dahil edilmesi.

namespace AIreach_Mobile
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif
            // --- BAĞIMLILIK ENJEKSİYONU (DEPENDENCY INJECTION) YAPILANDIRMASI ---
            // Uygulama içerisindeki nesne yönetimini (Object Lifecycle) merkezi bir yapı üzerinden yönetmek
            // ve bellek verimliliğini artırmak amacıyla Dependency Injection desenleri uygulanmıştır.

            // AIreachService Kaydı (Singleton):
            // Kaynak yönetimini optimize etmek ve her servis çağrısında yeniden nesne türetme (Instantiation)
            // maliyetinden kaçınmak amacıyla, ana servis sınıfı 'Singleton' olarak kaydedilmiştir.
            // Bu sayede uygulama yaşam döngüsü boyunca tek bir servis örneği bellekte tutulur ve API bağlantıları korunur.
            builder.Services.AddSingleton<AIreachService>();

            // Not: HistoryService sınıfı 'static' mimaride tasarlandığı için DI konteynerine
            // kaydedilmesine gerek duyulmamıştır; uygulama genelinden doğrudan erişim sağlanmaktadır.

            // --- ARAYÜZ KATMANI (UI/VIEWS) KAYITLARI ---
            // Sayfa Yönetimi (Transient):
            // Kullanıcı navigasyon deneyiminde sayfaların her açılışta temiz bir durumla (Fresh State) gelmesi
            // ve bellekten silinme işleminin (Garbage Collection) etkin yapılması için sayfalar 'Transient' olarak ayarlanmıştır.
            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<HistoryPage>();
            builder.Services.AddTransient<LibraryPage>();

            return builder.Build();
        }
    }
}