using AIreach_Mobile.Services;
using AIreach_Mobile.Models;

namespace AIreach_Mobile;

// Kütüphane ve öneri iþlemlerinin yürütüldüðü sayfanýn mantýksal katmanýdýr (Code-Behind).
// Kullanýcýnýn kayýtlý kitaplarýný listeleme, manuel ekleme yapma ve yapay zeka destekli öneri alma 
// iþlevlerini koordine eder.
public partial class LibraryPage : ContentPage
{
    // Yapay zeka servislerine eriþim saðlayan baðýmlýlýk (Dependency).
    private readonly AIreachService _aiService;

    // Constructor Injection yöntemiyle AIreachService sýnýfý sayfaya enjekte edilir.
    // Bu yaklaþým, servislerin test edilebilirliðini ve modülerliðini artýrýr.
    public LibraryPage(AIreachService aiService)
    {
        InitializeComponent();
        _aiService = aiService;
    }

    // Sayfa görüntülenme döngüsü (Lifecycle) baþladýðýnda tetiklenen metot.
    // Veri tutarlýlýðýný saðlamak amacýyla, sayfa her açýldýðýnda veritabanýndaki güncel liste
    // çekilerek arayüzdeki CollectionView bileþenine baðlanýr.
    protected override void OnAppearing()
    {
        base.OnAppearing();
        cvLibrary.ItemsSource = HistoryService.Getir();
    }

    // Kullanýcýnýn manuel olarak kitap eklemesini saðlayan olay yöneticisi (Event Handler).
    // Veri bütünlüðünü korumak adýna önce boþ giriþ kontrolü (Validation) yapýlýr,
    // ardýndan geçerli veriler yerel veritabanýna iþlenir.
    private async void OnManualAddClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtKitapAdi.Text) || string.IsNullOrWhiteSpace(txtYazar.Text))
        {
            await DisplayAlert("Eksik", "Lütfen kitap adý ve yazar girin.", "Tamam");
            return;
        }

        // Veritabaný servisinin statik metodu çaðrýlarak kayýt iþlemi gerçekleþtirilir.
        HistoryService.ManuelEkle(txtKitapAdi.Text, txtYazar.Text);

        // Kullanýcý deneyimini iyileþtirmek için giriþ alanlarý temizlenir ve liste güncellenir.
        txtKitapAdi.Text = "";
        txtYazar.Text = "";
        cvLibrary.ItemsSource = HistoryService.Getir();
    }

    // Kullanýcýnýn okuma geçmiþine dayalý kiþiselleþtirilmiþ öneri algoritmasýný tetikler.
    // Yeterli veri seti (Minimum 2 kitap) oluþup oluþmadýðý kontrol edildikten sonra,
    // kitap listesi yapay zeka modeline gönderilerek analiz sonucu beklenir.
    private async void OnRecommendClicked(object sender, EventArgs e)
    {
        var kitaplar = HistoryService.Getir();
        if (kitaplar.Count < 2)
        {
            await DisplayAlert("Az Veri", "Analiz için en az 2 kitap eklemelisin.", "Tamam");
            return;
        }

        aiLoading.IsRunning = true; // Asenkron iþlem sýrasýnda kullanýcýya geri bildirim (Loading) verilir.
        try
        {
            // LINQ kullanýlarak kitap listesi, yapay zeka modelinin iþleyebileceði string formatýna dönüþtürülür.
            var isimListesi = kitaplar.Select(k => $"{k.Baslik} ({k.Yazar})").ToList();
            string oneri = await _aiService.ZevkAnaliziYapAsync(isimListesi);
            await DisplayAlert("Öneri", oneri, "Süper");
        }
        catch (Exception ex) { await DisplayAlert("Hata", ex.Message, "Tamam"); }
        finally { aiLoading.IsRunning = false; }
    }

    // Multimodal AI (Görüntü Ýþleme) yeteneðini kullanarak kitaplýk fotoðrafý analizini baþlatýr.
    // Cihazýn kamera donanýmý kullanýlarak alýnan görsel, binary veri akýþýna (Stream) dönüþtürülür
    // ve analiz edilmek üzere yapay zeka servisine iletilir.
    private async void OnBookshelfPhotoClicked(object sender, EventArgs e)
    {
        try
        {
            // Cihazýn fotoðraf çekme özelliðini destekleyip desteklemediði kontrol edilir.
            if (MediaPicker.Default.IsCaptureSupported)
            {
                FileResult photo = await MediaPicker.Default.CapturePhotoAsync();
                if (photo != null)
                {
                    aiLoading.IsRunning = true;

                    // Bellek yönetimi (Memory Management) için 'using' bloklarý ile kaynaklar optimize edilir.
                    using var stream = await photo.OpenReadAsync();
                    using var ms = new MemoryStream();
                    await stream.CopyToAsync(ms);

                    // Görsel verisi (Byte Array) servise gönderilir ve Vision modelinden gelen analiz sonucu ekrana basýlýr.
                    string analiz = await _aiService.KitaplikAnaliziYapAsync(ms.ToArray());
                    await DisplayAlert("Analiz", analiz, "Tamam");
                }
            }
        }
        catch (Exception ex) { await DisplayAlert("Hata", ex.Message, "Tamam"); }
        finally { aiLoading.IsRunning = false; }
    }
}