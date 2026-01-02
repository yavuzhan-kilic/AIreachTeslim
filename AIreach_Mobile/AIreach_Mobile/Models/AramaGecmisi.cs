using LiteDB;

namespace AIreach_Mobile.Models;

public class AramaGecmisi
{
    [BsonId]
    public int Id { get; set; }
    public string Baslik { get; set; }
    public string Yazar { get; set; }
    public string Alinti { get; set; }
    public string Analiz { get; set; }
    public DateTime Tarih { get; set; }
}