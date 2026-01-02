using AIreach_Mobile.Services;
using AIreach_Mobile.Models;

namespace AIreach_Mobile;

// Bu sýnýf, kullanýcýnýn geçmiþ analiz kayýtlarýný görüntülediði arayüzün (UI) 
// arkasýndaki mantýðý (Code-Behind) yönetir.
public partial class HistoryPage : ContentPage
{
    public HistoryPage()
    {
        InitializeComponent();
    }

    // Sayfa ekrana her geldiðinde (Navigasyon veya geri dönme durumlarýnda) tetiklenen yaþam döngüsü metodu.
    // Kullanýcý ana sayfada yeni bir analiz yapýp buraya döndüðünde, 
    // listenin güncel kalmasýný saðlamak amacýyla veri yükleme iþlemi burada çaðrýlýr.
    protected override void OnAppearing()
    {
        base.OnAppearing();
        VerileriYukle();
    }

    // Veritabaný katmanýndan (HistoryService) kayýtlarý çeken ve arayüze baðlayan metot.
    private void VerileriYukle()
    {
        // Servis üzerinden tüm geçmiþ kayýtlarý getirilir.
        var liste = HistoryService.Getir();

        if (liste != null)
        {
            // Kullanýcý Deneyimi (UX) iyileþtirmesi:
            // Kullanýcýnýn en son yaptýðý iþlemi en üstte görmesi (LIFO - Last In First Out mantýðý) 
            // için liste ters çevrilerek CollectionView kaynaðýna atanýr.
            liste.Reverse();
            cvHistory.ItemsSource = liste;
        }
    }

    // Kullanýcýnýn tüm geçmiþi silme talebini iþleyen olay yöneticisi (Event Handler).
    // Veri kaybýný önlemek amacýyla iþlem öncesinde kullanýcýdan onay (Confirmation Dialog) alýnýr.
    private async void OnClearClicked(object sender, EventArgs e)
    {
        bool cevap = await DisplayAlert("Temizle", "Tüm analiz geçmiþi silinsin mi?", "Evet", "Hayýr");

        if (cevap)
        {
            // Kullanýcý onaylarsa veritabaný temizlenir ve arayüz güncellenir.
            HistoryService.Temizle();
            VerileriYukle();
        }
    }
}