using AIreach_Mobile.Models;
using AIreach_Mobile.Services;
using Microsoft.Maui;

namespace AIreach_Mobile;

// Bu sınıf, uygulamanın ana ekranındaki kullanıcı etkileşimlerini (kamera kullanımı, metin girişi, analiz başlatma)
// yöneten ve iş mantığı katmanı (Service Layer) ile arayüz (UI) arasındaki veri akışını sağlayan yapıdır.
public partial class MainPage : ContentPage
{
    // Yapay zeka işlemlerini yürüten servis katmanı bağımlılığı.
    private readonly AIreachService _service;

    // Constructor Injection yöntemiyle servis bağımlılığı bu sınıfa enjekte edilir.
    public MainPage(AIreachService service)
    {
        InitializeComponent();
        _service = service;
    }

    // Kullanıcının kamera ikonuna tıkladığında tetiklenen olay yöneticisi (Event Handler).
    // Cihazın donanım özelliklerini kullanarak fotoğraf çekimi ve Optik Karakter Tanıma (OCR) sürecini başlatır.
    private async void OnCameraClicked(object sender, EventArgs e)
    {
        try
        {
            // Kullanıcı Deneyimi (UX): Tıklama hissiyatını güçlendirmek için görsel geri bildirim (Animasyon) uygulanır.
            var view = sender as View;
            await view.FadeTo(0.5, 100);
            await view.FadeTo(1.0, 100);

            // İşlem sürerken kullanıcıya görsel bir indikatör gösterilir.
            loadingIndicator.IsRunning = true;

            // Cihazın varsayılan medya seçicisi kullanılarak fotoğraf çekme işlemi asenkron olarak gerçekleştirilir.
            FileResult photo = await MediaPicker.Default.CapturePhotoAsync();

            if (photo != null)
            {
                // Bellek Yönetimi: Fotoğraf verisi bir akış (Stream) olarak okunur ve bellek akışına (MemoryStream) kopyalanır.
                // 'using' blokları sayesinde işlem bitince kaynaklar otomatik olarak serbest bırakılır.
                using Stream sourceStream = await photo.OpenReadAsync();
                using MemoryStream ms = new MemoryStream();
                await sourceStream.CopyToAsync(ms);
                byte[] imageBytes = ms.ToArray();

                // Elde edilen ham görsel verisi (Byte Array), yapay zeka servisine gönderilerek metne dönüştürülür.
                string okunanMetin = await _service.ResimdenMetinOkuAsync(imageBytes);
                txtInput.Text = okunanMetin; // Sonuç, kullanıcı arayüzündeki giriş alanına yansıtılır.
            }
        }
        catch (Exception ex)
        {
            // Donanımsal veya sistemsel hatalar yakalanarak kullanıcıya anlaşılır bir dille bildirilir.
            await DisplayAlert("Hata", "Kamera açılamadı: " + ex.Message, "Tamam");
        }
        finally
        {
            // Hata olsun veya olmasın, işlem tamamlandığında yükleme indikatörü kapatılır.
            loadingIndicator.IsRunning = false;
        }
    }

    // Kullanıcının 'Analiz Et' veya 'Şimdi Başla' butonuna tıkladığında çalışan ana iş mantığı.
    // Metin doğrulama, kitap tespiti, derin analiz ve veritabanı kaydı adımlarını sırasıyla yürütür.
    private async void OnAnalizClicked(object sender, EventArgs e)
    {
        try
        {
            // Giriş Doğrulama (Input Validation): Boş veya hatalı girişleri engeller.
            string girisMetni = txtInput.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(girisMetni))
            {
                await DisplayAlert("Uyarı", "Lütfen bir metin girin veya fotoğraf çekin.", "Tamam");
                return;
            }

            // Mükerrer istekleri önlemek için işlem sırasında giriş alanı pasif hale getirilir.
            loadingIndicator.IsRunning = true;
            txtInput.IsEnabled = false;

            // Adım 1: Girilen metnin hangi esere ait olduğu yapay zeka servisi aracılığıyla tespit edilir.
            var kitap = await _service.KitabiBulAsync(girisMetni);

            // Eğer kitap bulunamazsa, sistemin akışını bozmamak için varsayılan bir model oluşturulur (Fallback).
            if (kitap == null) kitap = new KitapModel { Baslik = "Bilinmiyor", Yazar = "Tanımsız" };

            // Adım 2: Kitap ve metin bilgisi kullanılarak derinlemesine edebi analiz gerçekleştirilir.
            string analizSonucu = await _service.AnalizYapAsync(kitap, girisMetni);

            // Adım 3: Analiz sonuçları arayüzdeki ilgili alanlara (Labels) bağlanır ve sonuç paneli görünür yapılır.
            resultLayout.IsVisible = true;
            lblKitapAdi.Text = kitap.Baslik;
            lblYazar.Text = kitap.Yazar;
            lblAnaliz.Text = analizSonucu;

            // Adım 4: Elde edilen veriler, kalıcılığı sağlamak amacıyla yerel veritabanına (HistoryService) kaydedilir.
            HistoryService.Kaydet(kitap.Baslik, kitap.Yazar, girisMetni, analizSonucu);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Hata", "Analiz hatası: " + ex.Message, "Tamam");
        }
        finally
        {
            // İşlem tamamlandığında arayüz tekrar etkileşime açık hale getirilir.
            loadingIndicator.IsRunning = false;
            txtInput.IsEnabled = true;
        }
    }
}