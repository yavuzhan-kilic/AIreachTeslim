using LiteDB;
using AIreach_Mobile.Models;

namespace AIreach_Mobile.Services;

// Bu sınıf, uygulamanın yerel veri tabanı işlemlerini ve veri kalıcılığını yönetmekten sorumludur.
// Mobil cihazlarda düşük kaynak tüketimi ve yüksek performans sağlaması nedeniyle, 
// kurulum gerektirmeyen ve NoSQL tabanlı olan LiteDB kütüphanesi tercih edilmiştir.
public static class HistoryService
{
    // Veritabanı dosyasının cihaz üzerindeki fiziksel yolunu dinamik olarak belirleyen özellik.
    // Her mobil cihazın dosya sistemi farklılık gösterdiğinden, veri güvenliği ve erişilebilirlik açısından 
    // en uygun konum olan 'AppData' dizini kullanılmaktadır.
    private static string DbPath => Path.Combine(FileSystem.AppDataDirectory, "aireach_history.db");

    // Analiz edilen metinlerin ve elde edilen sonuçların veritabanına kaydedilmesini sağlayan metot.
    // Bellek yönetimi (Memory Management) açısından 'using' bloğu kullanılarak, 
    // işlem tamamlandığında veritabanı bağlantısının otomatik olarak kapatılması ve kaynakların serbest bırakılması sağlanmıştır.
    public static void Kaydet(string baslik, string yazar, string alinti, string analiz)
    {
        using (var db = new LiteDatabase(DbPath))
        {
            // Veritabanı içerisinde 'gecmis' isimli koleksiyon (tablo) üzerinde işlem yapılır.
            // Eğer bu koleksiyon mevcut değilse, LiteDB tarafından otomatik olarak oluşturulur.
            var col = db.GetCollection<AramaGecmisi>("gecmis");

            var yeniKayit = new AramaGecmisi
            {
                Baslik = baslik,
                Yazar = yazar,
                Alinti = alinti,
                Analiz = analiz,
                Tarih = DateTime.Now // Verilerin kronolojik sırayla listelenebilmesi için işlem zamanı kaydedilir.
            };

            col.Insert(yeniKayit);
        }
    }

    // Kullanıcının kamera kullanmadan, manuel olarak kitap eklemesi durumunda çalışan metot.
    // Görsel analiz yapılmadığı için eksik kalan veri alanlarına varsayılan değerler atanarak 
    // veritabanı bütünlüğünün (Data Integrity) korunması amaçlanmıştır.
    public static void ManuelEkle(string baslik, string yazar)
    {
        using (var db = new LiteDatabase(DbPath))
        {
            var col = db.GetCollection<AramaGecmisi>("gecmis");

            var yeniKayit = new AramaGecmisi
            {
                Baslik = baslik,
                Yazar = yazar,
                Alinti = "Elle Eklendi",          // OCR verisi olmadığı için bilgilendirme notu eklenir.
                Analiz = "Henüz analiz edilmedi", // Yapay zeka analizi daha sonra yapılmak üzere varsayılan değer atanır.
                Tarih = DateTime.Now
            };

            col.Insert(yeniKayit);
        }
    }

    // Geçmiş ekranında kullanıcıya sunulacak olan tüm kayıtları getiren metot.
    // Kullanıcı deneyimini (UX) iyileştirmek adına, veriler tarihe göre yeniden eskiye doğru (Descending) 
    // sıralanarak listelenmektedir.
    public static List<AramaGecmisi> Getir()
    {
        using (var db = new LiteDatabase(DbPath))
        {
            var col = db.GetCollection<AramaGecmisi>("gecmis");

            return col.Query().OrderByDescending(x => x.Tarih).ToList();
        }
    }

    // Uygulama ayarları üzerinden geçmişin temizlenmesi istendiğinde çalışan metot.
    // 'gecmis' koleksiyonunu tamamen düşürerek (Drop) veritabanı sıfırlama işlemini gerçekleştirir.
    public static void Temizle()
    {
        using (var db = new LiteDatabase(DbPath))
        {
            db.DropCollection("gecmis");
        }
    }
}