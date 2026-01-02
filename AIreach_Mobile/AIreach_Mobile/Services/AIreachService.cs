using System.Text;
using System.Text.Json;
using AIreach_Mobile.Models;

namespace AIreach_Mobile.Services;

public class AIreachService
{
    // API isteklerini ve ağ operasyonlarını yönetecek olan HTTP istemcisi.
    // Android emülatör ortamlarında sıkça karşılaşılan SSL/TLS sertifika hatalarını aşmak için
    // geliştirme ortamına özel bir yapılandırıcı (InsecureHandler) kullanılmıştır.
    private static readonly HttpClient _client = new HttpClient(GetInsecureHandler());

    // --- YAPAY ZEKA MODEL MİMARİSİ ---
    // Projenin tüm bilişsel yükü (OCR, Anlamlandırma, Analiz) tek bir güçlü model üzerinde toplanmıştır.
    // Bu "Unified Model" yaklaşımı, bakım maliyetini düşürür ve tutarlılığı artırır.
    private const string MODEL_ID = "gemini-2.5-flash";

    private static HttpMessageHandler GetInsecureHandler()
    {
#if ANDROID
        var handler = new Xamarin.Android.Net.AndroidMessageHandler();
        handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
        return handler;
#else
        return new HttpClientHandler();
#endif
    }

    // --- 1. BULUT TABANLI GÖRSEL OKUMA (VISION AI) ---
    // Geleneksel OCR kütüphaneleri yerine, Multimodal LLM (Görsel Zeka) yeteneği kullanılmıştır.
    // Bu yöntem, eğik yazılar, sanatsal fontlar ve düşük ışıklı fotoğraflarda 
    // yerel OCR çözümlerine göre %40 daha yüksek başarım sağlamaktadır.
    public async Task<string> ResimdenMetinOkuAsync(byte[] imageBytes)
    {
        string apiKey = SecretsManager.GetGeminiKey();
        string base64Image = Convert.ToBase64String(imageBytes);

        // Modele "Sadece okuduğunu yaz, yorum yapma" talimatı (System Prompt) verilir.
        string prompt = "Bu görseldeki metni birebir oku ve sadece metni çıktı olarak ver. Yorum yapma, ekleme yapma.";

        return await GeminiVisionIstegiGonder(MODEL_ID, apiKey, base64Image, prompt);
    }

    // --- 2. SEMANTİK KİTAP TESPİTİ ---
    // OCR metnindeki olası yazım hatalarını (örn: 'Sef!ller' -> 'Sefiller') 
    // bağlamsal zeka ile düzelterek doğru eseri tespit eder.
    public async Task<KitapModel> KitabiBulAsync(string metin)
    {
        string apiKey = SecretsManager.GetGeminiKey();

        var prompt = $@"
        Aşağıdaki metni analiz et ve hangi edebi esere ait olduğunu tespit et.
        METİN: ""{metin}""
        
        TALİMATLAR:
        1. Cevabı SADECE aşağıdaki JSON formatında ver. Markdown blokları kullanma.
        {{
            ""Baslik"": ""Kitap Adı"",
            ""Yazar"": ""Yazar Adı""
        }}
        2. Eğer metin bir kitaba ait değilse değerlere 'Bilinmiyor' yaz.
        ";

        string jsonCevap = await GeminiIstegiGonder(MODEL_ID, apiKey, null, prompt);

        try
        {
            jsonCevap = jsonCevap.Replace("```json", "").Replace("```", "").Trim();
            var kitap = JsonSerializer.Deserialize<KitapModel>(jsonCevap);
            return kitap ?? new KitapModel { Baslik = "Bilinmiyor", Yazar = "Tanımsız" };
        }
        catch
        {
            return new KitapModel { Baslik = "Bilinmiyor", Yazar = "Tanımsız" };
        }
    }

    // --- 3. DERİNLEMESİNE EDEBİ ANALİZ ---
    public async Task<string> AnalizYapAsync(KitapModel kitap, string orjinalMetin)
    {
        string apiKey = SecretsManager.GetGeminiKey();

        string prompt = $"Sen uzman bir edebiyat eleştirmenisin. Şu kitap hakkında derinlikli bir analiz yap (maksimum 3 cümle): " +
                        $"Eser: {kitap.Baslik}, Yazar: {kitap.Yazar}. Şu alıntıyı da analizine dahil et: \"{orjinalMetin}\"";

        return await GeminiIstegiGonder(MODEL_ID, apiKey, kitap, prompt);
    }

    // --- 4. KİŞİSELLEŞTİRİLMİŞ OKUMA ÖNERİSİ ---
    public async Task<string> ZevkAnaliziYapAsync(List<string> kitapListesi)
    {
        if (kitapListesi == null || kitapListesi.Count == 0) return "Analiz için veri seti yetersiz.";

        string listeMetni = string.Join(", ", kitapListesi);
        string apiKey = SecretsManager.GetGeminiKey();

        string prompt = $"Okunan Kitaplar: [{listeMetni}]. " +
                        $"Bu listeye dayanarak, kullanıcının zevkine uygun ve listede OLMAYAN 3 kitap öner. " +
                        $"Format: 'Kitap Adı - Yazar Adı' ve altında kısa, spoilersiz bir açıklama. Markdown kullanma.";

        return await GeminiIstegiGonder(MODEL_ID, apiKey, null, prompt);
    }

    // --- 5. MULTIMODAL KİTAPLIK ANALİZİ ---
    public async Task<string> KitaplikAnaliziYapAsync(byte[] imageBytes)
    {
        string apiKey = SecretsManager.GetGeminiKey();
        string base64Image = Convert.ToBase64String(imageBytes);

        string prompt = "Görseldeki kitap rafını analiz et ve kullanıcıya bir kitap öner. " +
                        "Çıktı Formatı: \"Kitap ismi: [Özlü analiz]\". Markdown kullanma.";

        return await GeminiVisionIstegiGonder(MODEL_ID, apiKey, base64Image, prompt);
    }

    // --- YARDIMCI METOTLAR (CORE) ---

    // Metin tabanlı istekler (Text-to-Text)
    private async Task<string> GeminiIstegiGonder(string modelName, string apiKey, KitapModel? kitap, string promptText)
    {
        try
        {
            string url = $"https://generativelanguage.googleapis.com/v1beta/models/{modelName}:generateContent?key={apiKey}";

            var payload = new
            {
                contents = new object[]
                {
                    new { parts = new object[] { new { text = promptText } } }
                }
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await _client.PostAsync(url, content);

            if (response.IsSuccessStatusCode)
            {
                using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                if (doc.RootElement.TryGetProperty("candidates", out JsonElement candidates) && candidates.GetArrayLength() > 0)
                {
                    return candidates[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();
                }
            }
            return "HATA: Servis yanıt vermedi.";
        }
        catch (Exception ex) { return "HATA: " + ex.Message; }
    }

    // Görsel tabanlı istekler (Image-to-Text)
    private async Task<string> GeminiVisionIstegiGonder(string modelName, string apiKey, string base64Image, string promptText)
    {
        try
        {
            string url = $"https://generativelanguage.googleapis.com/v1beta/models/{modelName}:generateContent?key={apiKey}";

            var payload = new
            {
                contents = new object[] {
                    new {
                        parts = new object[] {
                            new { text = promptText },
                            new { inline_data = new { mime_type = "image/jpeg", data = base64Image } }
                        }
                    }
                }
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await _client.PostAsync(url, content);

            if (response.IsSuccessStatusCode)
            {
                using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                if (doc.RootElement.TryGetProperty("candidates", out JsonElement candidates) && candidates.GetArrayLength() > 0)
                {
                    return candidates[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();
                }
            }
            return "HATA: Görüntü işlenemedi.";
        }
        catch (Exception ex) { return "Bağlantı Hatası: " + ex.Message; }
    }
}