using System.Data;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Text;
using MySql.Data.MySqlClient;

namespace urunservis
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly HttpClient _httpClient;
        private readonly string _connectionString = "Server=localhost;Database=rafetiket;Uid=root;Pwd=1234;";  // şema rafetiket aşşağıdaki sql komutunda update komutunu kullandığımız tablo ise etiket olarak belirtilmiştir.
        private string _token;

        public Worker(ILogger<Worker> logger, HttpClient httpClient)
        {
            _logger = logger;
            _httpClient = httpClient;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

                try
                {
                    // Giriş yapıp token al
                    await LoginAsync();

                    // API'den veri çek ve işle
                    await FetchAndProcessProductsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred.");
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); // 1 dk bekleme
            }
        }

        private async Task FetchAndProcessProductsAsync(CancellationToken stoppingToken)
        {
            if (string.IsNullOrEmpty(_token))
            {
                throw new InvalidOperationException("No token available.");
            }

            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _token);

            var response = await _httpClient.GetAsync("https://localhost:7205/api/urun", stoppingToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(stoppingToken);

            _logger.LogInformation($"Ham JSON içeriği: {content}");

            try
            {
                var products = JsonSerializer.Deserialize<List<Product>>(content);

                if (products != null)
                {
                    foreach (var product in products)
                    {
                        _logger.LogInformation($"Ürün barkodu: {product.UrunBarkod}, isim: {product.UrunIsim}, fiyat: {product.UrunFiyat}, indirimli fiyat: {product.UrunIndirim} ");

                        // MySQL veritabanında güncelleme yap
                        await UpdateProductInDatabaseAsync(product);
                    }
                }
                else
                {
                    _logger.LogWarning("Ürün bulunamadı.");
                }
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError($"JSON deserialization başarısız: {jsonEx.Message}");
            }
        }

        private async Task UpdateProductInDatabaseAsync(Product product)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = @"
            UPDATE etiket  
            SET urunIsim = @urunIsim, urunFiyat = @urunFiyat, urunIndirimli = @urunIndirim
            WHERE urunBarkod = @urunBarkod";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@urunBarkod", product.UrunBarkod);
            command.Parameters.AddWithValue("@urunIsim", product.UrunIsim);
            command.Parameters.AddWithValue("@urunFiyat", product.UrunFiyat);
            command.Parameters.AddWithValue("@urunIndirim", product.UrunIndirim);

            int rowsAffected = await command.ExecuteNonQueryAsync();

            if (rowsAffected > 0)
            {
                _logger.LogInformation("Ürün güncellendi Barkod numarası: {urunBarkod}", product.UrunBarkod);
            }
            else
            {
                _logger.LogWarning("Ürün güncellenemedi, eşleşen ürün barkodu bulunamadı: {urunBarkod}", product.UrunBarkod);
            }
        }

        private async Task LoginAsync()
        {
            var loginRequest = new
            {
                Username = "admin",
                Password = "1234"
            };

            var content = new StringContent(JsonSerializer.Serialize(loginRequest), Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync("https://localhost:7205/api/auth/login", content);
                response.EnsureSuccessStatusCode();

                var responseData = await response.Content.ReadAsStringAsync();
                var responseJson = JsonSerializer.Deserialize<JsonElement>(responseData);
                _token = responseJson.GetProperty("token").GetString();
            }
            catch (HttpRequestException e)
            {
                _logger.LogError($"Talep başarısız: {e.Message}");
                throw;
            }
        }

        public class Product
        {
            [JsonPropertyName("urunBarkod")]
            public string UrunBarkod { get; set; }

            [JsonPropertyName("urunIsim")]
            public string UrunIsim { get; set; }

            [JsonPropertyName("urunFiyat")]
            public decimal UrunFiyat { get; set; }

            [JsonPropertyName("urunIndirim")]
            public decimal UrunIndirim { get; set; }
        }
    }
}
