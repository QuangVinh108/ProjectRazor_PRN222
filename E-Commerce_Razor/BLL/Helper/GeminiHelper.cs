using BLL.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BLL.Helper
{
    public class GeminiHelper
    {
        private readonly string _apiKey;
        private readonly HttpClient _httpClient;

        public GeminiHelper(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            // Lấy API Key từ appsettings.json
            _apiKey = configuration["Gemini:ApiKey"];
        }

        public async Task<GeminiProductDto> AnalyzeImageAsync(IFormFile imageFile, List<string> availableCategories)
        {
            if (string.IsNullOrEmpty(_apiKey))
            {
                throw new Exception("Gemini API Key chưa được cấu hình trong appsettings.json");
            }

            // 1. Chuyển ảnh sang Base64
            using var ms = new MemoryStream();
            await imageFile.CopyToAsync(ms);
            var imageBytes = ms.ToArray();
            var base64Image = Convert.ToBase64String(imageBytes);

            string categoriesString = string.Join(", ", availableCategories.Select(c => $"'{c}'"));

            string promptText = $@"Đóng vai chuyên gia bán hàng. Phân tích hình ảnh và trả về JSON thuần túy (không markdown) gồm: 
                        productName (tên tiếng Việt), 
                        sku (mã ngắn), 
                        price (số nguyên VNĐ), 
                        description (3 câu hấp dẫn), 
                        category.
                        
                        QUAN TRỌNG: Trường 'category' BẮT BUỘC phải chọn chính xác 1 cụm từ trong danh sách sau: [{categoriesString}]. 
                        Nếu không chắc chắn, hãy chọn cái gần đúng nhất trong danh sách đó.";

            // 2. Tạo Request Body gửi lên Gemini
            var requestBody = new
            {
                contents = new[]
                    {
                        new
                        {
                            parts = new object[]
                            {
                                new { text = promptText }, 
                                new
                                {
                                    inline_data = new
                                    {
                                        mime_type = imageFile.ContentType,
                                        data = base64Image
                                    }
                                }
                            }
                        }
                    }
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            // 3. Gọi API (Sửa lại chuỗi URL cho gọn và đúng chuẩn C#)
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash-lite:generateContent?key={_apiKey}";

            var response = await _httpClient.PostAsync(url, jsonContent);

            if (!response.IsSuccessStatusCode) 
                return null;

            var responseString = await response.Content.ReadAsStringAsync();

            // 4. Parse kết quả trả về
            try
            {
                using var doc = JsonDocument.Parse(responseString);

                // Kiểm tra xem có candidate nào không trước khi truy cập index [0]
                var candidates = doc.RootElement.GetProperty("candidates");
                if (candidates.GetArrayLength() == 0) 
                    return null;

                var text = candidates[0]
                                .GetProperty("content")
                                .GetProperty("parts")[0]
                                .GetProperty("text").GetString();

                // Clean chuỗi json
                text = text.Replace("```json", "").Replace("```", "").Trim();

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                return JsonSerializer.Deserialize<GeminiProductDto>(text, options);
            }
            catch
            {
                return null;
            }
        }

        // Thêm tham số liveFaceBase64
        public async Task<GeminiIdCardDto> AnalyzeIdCardAsync(IFormFile imageFile, string liveFaceBase64)
        {
            if (string.IsNullOrEmpty(_apiKey) || _apiKey.Contains("YOUR_ACTUAL"))
            {
                Console.WriteLine("❌ LỖI: API Key chưa cấu hình.");
                return null;
            }

            try
            {
                // 1. Chuyển ảnh CCCD sang Base64
                using var ms = new MemoryStream();
                await imageFile.CopyToAsync(ms);
                var cccdBase64 = Convert.ToBase64String(ms.ToArray());

                // 2. Chẩn hóa chuỗi base64 của LiveFace (bỏ phần data:image/jpeg;base64,)
                if (!string.IsNullOrEmpty(liveFaceBase64) && liveFaceBase64.Contains(","))
                {
                    liveFaceBase64 = liveFaceBase64.Split(',')[1];
                }

                // 3. Prompt yêu cầu cả OCR và so khớp khuôn mặt
                var prompt = @"Bạn là hệ thống eKYC (Nhận diện khuôn mặt và trích xuất CCCD).
                        Nhiệm vụ:
                        1. Trích xuất thông tin từ Hình 1 (Căn cước công dân - CCCD).
                        2. So sánh khuôn mặt người trong Hình 1 (CCCD) và Hình 2 (Ảnh chụp trực tiếp).

                        Yêu cầu output chỉ trả về RAW JSON thuần túy (Không markdown, không code block).
                        Cấu trúc JSON bắt buộc:
                        {
                            ""isValid"": true, (kiểm tra Hình 1 có phải là CCCD hợp lệ, rõ nét không)
                            ""isFaceMatch"": true, (trả về true nếu khuôn mặt ở Hình 1 và Hình 2 là của cùng CÙNG MỘT NGƯỜI, false nếu khác người)
                            ""reason"": ""null nếu hợp lệ, nếu isValid=false hoặc isFaceMatch=false hãy ghi ngắn gọn lý do tại sao"",
                            ""data"": {
                                ""idNumber"": ""Số thẻ"",
                                ""fullName"": ""Họ và tên (IN HOA)"",
                                ""dob"": ""Ngày sinh (dd/MM/yyyy)"",
                                ""address"": ""Nơi thường trú""
                            }
                        }
                        Nếu hình mờ hoặc không nhận diện được, hãy đánh dấu isValid: false hoặc isFaceMatch: false và ghi rõ lý do.";

                var requestBody = new
                {
                    contents = new[]
                    {
                new
                {
                    parts = new object[]
                    {
                        new { text = prompt },
                        // Hình 1: CCCD
                        new { inline_data = new { mime_type = imageFile.ContentType, data = cccdBase64 } },
                        // Hình 2: Live selfie Camera
                        new { inline_data = new { mime_type = "image/jpeg", data = liveFaceBase64 } }
                    }
                }
            },
                    safetySettings = new[]
                    {
                new { category = "HARM_CATEGORY_HARASSMENT", threshold = "BLOCK_NONE" },
                new { category = "HARM_CATEGORY_HATE_SPEECH", threshold = "BLOCK_NONE" },
                new { category = "HARM_CATEGORY_SEXUALLY_EXPLICIT", threshold = "BLOCK_NONE" },
                new { category = "HARM_CATEGORY_DANGEROUS_CONTENT", threshold = "BLOCK_NONE" }
            }
                };

                var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash-lite:generateContent?key={_apiKey}";

                var response = await _httpClient.PostAsync(url, jsonContent);
                var responseString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return null;

                using var doc = JsonDocument.Parse(responseString);
                if (!doc.RootElement.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
                    return null;

                var text = candidates[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();

                // Xử lý dọn chuỗi JSON
                text = text.Replace("```json", "").Replace("```", "").Trim();
                int startIndex = text.IndexOf('{');
                int endIndex = text.LastIndexOf('}');
                if (startIndex >= 0 && endIndex > startIndex)
                {
                    text = text.Substring(startIndex, endIndex - startIndex + 1);
                }

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                return JsonSerializer.Deserialize<GeminiIdCardDto>(text, options);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"EX: {ex.Message}");
                return null;
            }
        }
    }
}
