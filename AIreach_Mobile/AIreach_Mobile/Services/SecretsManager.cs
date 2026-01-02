using System.Reflection;
using System.Text.Json;

namespace AIreach_Mobile.Services;

// Bu sınıf, uygulama güvenliğini artırmak amacıyla Gemini API anahtarını gizlemek amacıyla tasarlanmıştır.
// kaynak kod içerisinde açık metin (Plain Text) olarak tutulması yerine, 
// derlenmiş uygulama içine gömülü (Embedded Resource) şifreli bir JSON dosyasından okunmasını sağlar.
// Bu yaklaşım, tersine mühendislik (Reverse Engineering) risklerine karşı temel bir güvenlik katmanı oluşturur.
// Uygulama tamamen client-side çalıştığı için, %100 güvenlik sağlamak mümkün değildir.
// Ancak, bu yöntemle API anahtarının doğrudan kodda bulunması engellenmiş olur.
// Gelişmiş güvenlik önlemleri için, API anahtarlarının sunucu tarafında yönetilmesi ve
// istemci ile sunucu arasında güvenli bir iletişim protokolü kullanılması önerilir.
public static class SecretsManager
{
    // Google Gemini Yapay Zeka modelinin kimlik doğrulaması için kullanılan API anahtarını döndürür.
    public static string GetGeminiKey()
    {
        var secrets = GetSecrets();
        return secrets?.GetProperty("gemini_api_key").GetString() ?? string.Empty;
    }

    // Gömülü kaynaklara (Embedded Resources) erişimi yöneten ve dosya okuma işlemini gerçekleştiren temel yardımcı metot.
    // Reflection kütüphanesi kullanılarak çalışma zamanında (Runtime) dosya içeriğine erişilir.
    private static JsonElement? GetSecrets()
    {
        // Çalışmakta olan derlemenin (Assembly) referansı alınır.
        var assembly = Assembly.GetExecutingAssembly();

        // Kaynak dosya isminin değişmesi veya farklı bir ad alanı (Namespace) altında kalması ihtimaline karşı,
        // dosya yolu dinamik olarak taranarak sonu 'secrets.json' ile biten kaynak tespit edilir.
        var resourceName = assembly.GetManifestResourceNames()
                            .FirstOrDefault(n => n.EndsWith("secrets.json"));

        // Yapılandırma dosyasının 'Build Action' özelliği 'Embedded Resource' olarak ayarlanmamışsa dosya bulunamaz.
        // Bu durumda hata ayıklama sürecini hızlandırmak için mevcut tüm kaynaklar konsola loglanır.
        if (resourceName == null)
        {
            System.Diagnostics.Debug.WriteLine(">>> [HATA] secrets.json bulunamadı! Mevcut kaynaklar şunlar:");
            foreach (var name in assembly.GetManifestResourceNames())
            {
                System.Diagnostics.Debug.WriteLine($">>> Bulunan Kaynak: {name}");
            }
            return null;
        }

        // Dosya okuma işlemi bir veri akışı (Stream) üzerinden gerçekleştirilir.
        // 'using' bloğu kullanılarak, okuma işlemi tamamlandığında bellek akışının (Memory Stream) 
        // otomatik olarak temizlenmesi (Dispose) ve sistem kaynaklarının serbest bırakılması sağlanır.
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null) return null;

        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();

        // JSON verisi ayrıştırılır (Parse).
        // Akış kapatıldıktan sonra verinin kaybolmaması için kök elementin bir kopyası (Clone) oluşturularak döndürülür.
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }
}