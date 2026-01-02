using AIreach_Mobile.Services;
using AIreach_Mobile.Models;

namespace AIreach_Mobile;

public partial class TextEditorPage : ContentPage
{
    private readonly AIreachService _service;

    public TextEditorPage(AIreachService service)
    {
        InitializeComponent();
        _service = service;
    }

    // Kamera veya MainPage tarafından gönderilen başlangıç metnini ayarlar.
    public Task SetInitialText(string text)
    {
        txtInput.Text = text;
        return Task.CompletedTask;
    }

    // Analiz butonu: MainPage'den taşınan mantık burada çalışır.
    private async void OnAnalizClicked(object sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine(">>> [LOG - UI] Analiz butonuna tıklandı (TextEditorPage).");
        try
        {
            string girisMetni = txtInput.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(girisMetni))
            {
                await DisplayAlert("Uyarı", "Lütfen önce metin ekleyin veya fotoğraf çekin.", "Tamam");
                return;
            }

            loadingIndicator.IsRunning = true;
            txtInput.IsEnabled = false;

            var kitap = await _service.KitabiBulAsync(girisMetni);
            if (kitap == null)
            {
                kitap = new KitapModel { Baslik = "Bilinmiyor", Yazar = "Bilinmiyor" };
            }

            string analizSonucu = await _service.AnalizYapAsync(kitap, girisMetni);

            resultLayout.IsVisible = true;
            lblKitapAdi.Text = kitap.Baslik;
            lblYazar.Text = kitap.Yazar;
            lblAnaliz.Text = analizSonucu;

            try
            {
                HistoryService.Kaydet(kitap.Baslik, kitap.Yazar, girisMetni, analizSonucu);
            }
            catch (Exception exDb)
            {
                System.Diagnostics.Debug.WriteLine($">>> 🛑 [LOG - DB HATA] {exDb.Message}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($">>> 🛑 [LOG - ANALIZ HATA] {ex}");
            await DisplayAlert("Hata", "Bir hata oluştu: " + ex.Message, "Tamam");
        }
        finally
        {
            loadingIndicator.IsRunning = false;
            txtInput.IsEnabled = true;
        }
    }
}