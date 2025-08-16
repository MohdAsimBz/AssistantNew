using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Caching;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AssistantApiEnhanced
{
    public class Product
    {
        public string ProductName { get; set; }
        public string Category { get; set; }
        public string Price { get; set; }
        public string Mrp { get; set; }
        public string PackSize { get; set; }
        public string Brand { get; set; }
        public string Image { get; set; }
        public string BuyLink { get; set; }
    }

    public class BotResponse
    {
        public string reply { get; set; }
        public List<Product> products { get; set; }
        public string footer { get; set; }
    }

    public class ProductMessage
    {
        public string ImageUrl { get; set; }
        public string BuyUrl { get; set; }
        public string Text { get; set; }
    }

    public class FormattedResponse
    {
        public string Reply { get; set; }
        public List<ProductMessage> ProductMessages { get; set; }
        public string Footer { get; set; }
    }

    class Program
    {
        private const string API_KEY = "xxxx"; // Replace with your actual OpenAI API Key

        static void Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "--test")
            {
                TestEnhancedFeatures.RunTests();
                return;
            }
            
            MainAsync(args).GetAwaiter().GetResult();
        }

        public static async Task MainAsync(string[] args)
        {
            var apiKey = API_KEY;
            string fileIdJson = "file-XKm45X3dkxFCb5EzNSjXfJ";
            string vectorjsonNew = "vs_688cb22f82c8819185e50dd2a878caf2";
            var assistantIdJsonNew = "asst_SicuKsYxn9nOSKBGJoeGyRcp";
            string userId = "8077675133";
            
            Console.WriteLine("Enhanced Assistant API - Welcome! Type your query (or 'exit' to quit):");

            string threadId = "";
            string existingThreadId = GetThreadIds(userId);

            if (string.IsNullOrEmpty(existingThreadId))
            {
                threadId = CreateThread(apiKey).GetAwaiter().GetResult();
                SaveThreadIds(userId, threadId);
            }
            else
            {
                threadId = existingThreadId;
            }

            while (true)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write("\nYou: ");
                Console.ResetColor();
                string query = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(query))
                    continue;

                if (query.ToLower() == "exit")
                    break;

                string processedQuery = ProcessUserIntent(query);
                if (IsNegativeIntent(query))
                {
                    HandleNegativeIntent(query);
                    continue;
                }

                AddMessageToThread(apiKey, threadId, processedQuery).GetAwaiter().GetResult();
                var runId = RunAssistant(apiKey, threadId, assistantIdJsonNew, vectorjsonNew);
                string finalResult = GetRunResult(apiKey, threadId, runId).GetAwaiter().GetResult();
                
                string userId2 = "8077675133";
                
                if (IsValidJson(finalResult))
                {
                    FormattedResponse response = ParseJsonResponse(finalResult);
                    
                    if (!string.IsNullOrEmpty(response.Reply))
                    {
                        await Task.Delay(300);
                    }

                    foreach (var msg in response.ProductMessages)
                    {
                        if (!string.IsNullOrEmpty(msg.ImageUrl))
                        {
                            await Task.Delay(300);
                        }
                    }

                    if (!string.IsNullOrEmpty(response.Footer))
                    {
                        await Task.Delay(300);
                    }
                }
                else
                {
                    string desiredFooter = "Aur kisi product ki information chahiye? Adhik jankari ke liye humein 7876400500 par call karein.";
                    string jsonOutput = ParseResponseToJson(finalResult, desiredFooter);
                    FormattedResponse response = ParseJsonResponse(jsonOutput);

                    if (!string.IsNullOrEmpty(response.Reply))
                    {
                        string responce = SendMessageAsync(userId2, "Text", "*" + response.Reply + "*", "", "").GetAwaiter().GetResult();
                    }

                    foreach (var msg in response.ProductMessages)
                    {
                        if (!string.IsNullOrEmpty(msg.ImageUrl))
                        {
                            string responce = SendMessageAsync(userId2, "Image", msg.Text, msg.ImageUrl.Replace(" ", "%20"), msg.BuyUrl).GetAwaiter().GetResult();
                        }
                    }

                    if (!string.IsNullOrEmpty(response.Footer))
                    {
                        string responce = SendMessageAsync(userId2, "Text", "*" + response.Footer + "*", "", "").GetAwaiter().GetResult();
                    }
                }

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Assistant: " + finalResult);
                Console.ResetColor();
            }

            Console.WriteLine("Exited.");
        }

        public static bool IsNegativeIntent(string query)
        {
            string lowerQuery = query.ToLower().Trim();
            
            string[] negativePatterns = {
                "nahi chahiye", "nahi chaahiye", "nhi chahiye", "nhi chaahiye",
                "don't want", "dont want", "no thanks", "not interested",
                "cancel", "stop", "band karo", "band kar do",
                "mujhe nahi chahiye", "humein nahi chahiye",
                "i don't need", "i dont need", "not required",
                "enough", "bas", "bas karo", "sufficient",
                "no product", "koi product nahi", "product nahi chahiye"
            };

            return negativePatterns.Any(pattern => lowerQuery.Contains(pattern));
        }

        public static void HandleNegativeIntent(string query)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            
            if (ContainsHindi(query))
            {
                Console.WriteLine("Assistant: Samjh gaya! Koi baat nahi. Agar aapko kuch aur chahiye toh bataiye. Hum yahan madad ke liye hain.");
            }
            else
            {
                Console.WriteLine("Assistant: Understood! No problem. Let me know if you need anything else. I'm here to help.");
            }
            
            Console.ResetColor();
        }

        public static bool ContainsHindi(string text)
        {
            string[] hindiWords = { "nahi", "chahiye", "mujhe", "humein", "karo", "bas", "product" };
            return hindiWords.Any(word => text.ToLower().Contains(word));
        }

        public static string ProcessUserIntent(string query)
        {
            var priceInfo = ExtractPriceRange(query);
            if (priceInfo.HasPriceConstraint)
            {
                return $"{query} [PRICE_FILTER: max={priceInfo.MaxPrice}, min={priceInfo.MinPrice}]";
            }
            
            return query;
        }

        public static (bool HasPriceConstraint, decimal MaxPrice, decimal MinPrice) ExtractPriceRange(string query)
        {
            string lowerQuery = query.ToLower();
            
            var underPatterns = new[]
            {
                @"under\s*(?:rs\.?|₹)?\s*(\d+(?:,\d+)*(?:k|thousand)?)",
                @"below\s*(?:rs\.?|₹)?\s*(\d+(?:,\d+)*(?:k|thousand)?)",
                @"(\d+(?:,\d+)*(?:k|thousand)?)\s*tak",
                @"(\d+(?:,\d+)*(?:k|thousand)?)\s*se\s*kam",
                @"less\s*than\s*(?:rs\.?|₹)?\s*(\d+(?:,\d+)*(?:k|thousand)?)"
            };

            var rangePatterns = new[]
            {
                @"(\d+(?:,\d+)*(?:k|thousand)?)\s*to\s*(\d+(?:,\d+)*(?:k|thousand)?)",
                @"(\d+(?:,\d+)*(?:k|thousand)?)\s*se\s*(\d+(?:,\d+)*(?:k|thousand)?)\s*tak",
                @"between\s*(?:rs\.?|₹)?\s*(\d+(?:,\d+)*(?:k|thousand)?)\s*and\s*(?:rs\.?|₹)?\s*(\d+(?:,\d+)*(?:k|thousand)?)"
            };

            foreach (var pattern in underPatterns)
            {
                var match = Regex.Match(lowerQuery, pattern);
                if (match.Success)
                {
                    decimal maxPrice = ParsePriceValue(match.Groups[1].Value);
                    return (true, maxPrice, 0);
                }
            }

            foreach (var pattern in rangePatterns)
            {
                var match = Regex.Match(lowerQuery, pattern);
                if (match.Success)
                {
                    decimal minPrice = ParsePriceValue(match.Groups[1].Value);
                    decimal maxPrice = ParsePriceValue(match.Groups[2].Value);
                    return (true, maxPrice, minPrice);
                }
            }

            return (false, 0, 0);
        }

        public static decimal ParsePriceValue(string priceStr)
        {
            priceStr = priceStr.Replace(",", "").ToLower();
            
            if (priceStr.EndsWith("k") || priceStr.Contains("thousand"))
            {
                string numStr = priceStr.Replace("k", "").Replace("thousand", "").Trim();
                if (decimal.TryParse(numStr, out decimal value))
                {
                    return value * 1000;
                }
            }
            
            if (decimal.TryParse(priceStr, out decimal directValue))
            {
                return directValue;
            }
            
            return 0;
        }

        public static bool IsValidJson(string json)
        {
            try
            {
                var token = JToken.Parse(json);
                return token.Type == JTokenType.Object || token.Type == JTokenType.Array;
            }
            catch
            {
                return false;
            }
        }

        public static string ParseResponseToJson(string textResponse, string defaultFooter)
        {
            var botResponse = new BotResponse
            {
                products = new List<Product>(),
                footer = defaultFooter
            };

            string pattern = @"(?<index>\d+)\.\s*\*\*(?<ProductName>.*?)\*\*\s*[\r\n]+(?:\s*-\s+\*\*Price:\*\*\s*(INR\s*)?(?<Price>.*?)\s*[\r\n]+)?(?:\s*-\s+\*\*MRP:\*\*\s*(INR\s*)?(?<Mrp>.*?)\s*[\r\n]+)?(?:\s*-\s+\*\*Category:\*\*\s*(?<Category>.*?)\s*[\r\n]+)?(?:\s*-\s+\*\*Brand:\*\*\s*(?<Brand>.*?)\s*[\r\n]+)?(?:\s*-\s+\*\*Image:\*\*\s*!\[.*?\]\((?<Image>.*?)\)[\r\n]+)?(?:\s*-\s+\*\*Buy Link:\*\*\s*\[.*?\]\((?<BuyLink>.*?)\))?";

            var matches = Regex.Matches(textResponse, pattern, RegexOptions.Multiline | RegexOptions.IgnoreCase);

            var replyMatch = Regex.Match(textResponse, @"^Here are.*?:", RegexOptions.IgnoreCase);
            var replyMatch2 = Regex.Match(textResponse, @"^I found.*?:", RegexOptions.IgnoreCase);
            if (replyMatch.Success || replyMatch2.Success)
            {
                botResponse.reply = "आपके लिए चुने गए कुछ विकल्प यहाँ दिए गए हैं:";
            }

            foreach (Match match in matches)
            {
                var product = new Product
                {
                    ProductName = match.Groups["ProductName"].Value.Trim(),
                    Category = match.Groups["Category"].Success ? match.Groups["Category"].Value.Trim() : "",
                    Price = match.Groups["Price"].Success ? "INR " + match.Groups["Price"].Value.Trim() : "",
                    Mrp = match.Groups["Mrp"].Success ? "INR " + match.Groups["Mrp"].Value.Trim() : "",
                    Brand = match.Groups["Brand"].Success ? match.Groups["Brand"].Value.Trim() : "",
                    PackSize = "1.00 Pieces",
                    Image = match.Groups["Image"].Success ? match.Groups["Image"].Value.Trim().Replace(" ", "%20") : "",
                    BuyLink = match.Groups["BuyLink"].Success ? match.Groups["BuyLink"].Value.Trim() : ""
                };

                botResponse.products.Add(product);
            }

            if (botResponse.products.Count == 0)
            {
                botResponse.reply = "Maaf kijiye, humein is query se sambandhit koi product nahi mila.";
            }

            return JsonConvert.SerializeObject(botResponse, Formatting.Indented);
        }

        public static FormattedResponse ParseJsonResponse(string jsonString)
        {
            var result = new FormattedResponse
            {
                ProductMessages = new List<ProductMessage>()
            };

            var doc = JObject.Parse(jsonString);

            result.Reply = doc["reply"]?.ToString() ?? "";

            var products = doc["products"] as JArray;
            if (products != null)
            {
                foreach (var p in products)
                {
                    var message = new ProductMessage
                    {
                        ImageUrl = p["Image"]?.ToString() ?? "",
                        BuyUrl = p["BuyLink"]?.ToString() ?? "",
                        Text = $"🛒 *{p["ProductName"]}*\n" +
                               $"🏷️ Category: {p["Category"]}\n" +
                               $"💰 Price: {p["Price"]} (MRP {p["Mrp"]})\n" +
                               $"📦 Pack: {p["PackSize"]}\n" +
                               $"🏭 Brand: {p["Brand"]}" +
                               $"🔗 Buy Now: {p["BuyLink"]}"
                    };

                    result.ProductMessages.Add(message);
                }
            }

            result.Footer = doc["footer"]?.ToString() ?? "";

            return result;
        }

        public static void SaveThreadIds(string userId, string threadId)
        {
            using (SqlConnection conn = new SqlConnection("data source=13.203.9.215;initial catalog=BehtarZindgiDB_R7;user id=dev406net;password=agri@402handy"))
            {
                SqlCommand cmd = new SqlCommand("Usp_InsertUserThread", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@ThreadId", threadId);

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public static string GetThreadIds(string userId)
        {
            using (SqlConnection conn = new SqlConnection("data source=13.203.9.215;initial catalog=BehtarZindgiDB_R7;user id=dev406net;password=agri@402handy"))
            {
                SqlCommand cmd = new SqlCommand("Usp_GetThreadIdByUserId", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@UserId", userId);

                conn.Open();
                var result = cmd.ExecuteScalar();
                return result?.ToString();
            }
        }

        private static readonly MemoryCache _cache = MemoryCache.Default;
        private static readonly TimeSpan _cacheDuration = TimeSpan.FromHours(12);

        public static string GetThreadId(string userId)
        {
            return _cache.Get(userId) as string;
        }

        public static void SaveThreadId(string userId, string threadId)
        {
            _cache.Set(userId, threadId, DateTimeOffset.Now.Add(_cacheDuration));
        }

        private static readonly HttpClient client = new HttpClient();
        private const string InteraktUrl = "https://api.interakt.ai/v1/public/message/";

        public static async Task<string> SendMessageAsync(string PhoneNumber, string Type, string Message, string MediaUrl, string FileName)
        {
            if (string.IsNullOrEmpty(PhoneNumber) || string.IsNullOrEmpty(Type))
                return "❌ Phone number and message type are required.";

            var payload = new Dictionary<string, object>
            {
                { "callbackData", "some_callback_data" },
                { "type", Type }
            };

            // Clean number
            if (PhoneNumber.StartsWith("+91"))
                payload["phoneNumber"] = PhoneNumber.Substring(3);
            else if (PhoneNumber.StartsWith("91"))
                payload["phoneNumber"] = PhoneNumber.Substring(2);
            else
                payload["phoneNumber"] = PhoneNumber;

            payload["countryCode"] = "+91";

            // Base data object
            var data = new Dictionary<string, object>
            {
                { "message", Message }
            };

            if (Type == "Audio" || Type == "Video")
            {
                data["mediaUrl"] = MediaUrl;
                data["fileName"] = FileName;
            }
            else if (Type == "Image")
            {
                data["mediaUrl"] = MediaUrl;
            }

            payload["data"] = data;

            // Optional reply button (only for Image type)
            if (Type == "Image")
            {
                payload["action"] = new
                {
                    buttons = new[]
                    {
                        new {
                            type = "reply",
                            reply = new {
                                id = FileName,
                                title = "Buy"
                            }
                        }
                    }
                };
            }

            // Convert to JSON
            string json = JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, InteraktUrl);
            httpRequest.Headers.Authorization = AuthenticationHeaderValue.Parse("Basic QXM4THp5VjBEckpOelFBUTdoWDVFUnh2dXBXQ1pkYXJibFFDLWt2Z1lmZzo=");
            httpRequest.Content = content;

            var response = await client.SendAsync(httpRequest);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                string id = JObject.Parse(responseContent)["id"].ToString();
                return id;
            }
            else
            {
                return "Error!\n" + responseContent;
            }
        }

        static async Task<string> CreateThread(string apiKey)
        {
            System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            client.DefaultRequestHeaders.Add("OpenAI-Beta", "assistants=v2");
            var content = new StringContent("{}", Encoding.UTF8, "application/json");
            var response = await client.PostAsync("https://api.openai.com/v1/threads", content);
            var result = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<dynamic>(result).id;
        }

        static async Task AddMessageToThread(string apiKey, string threadId, string message)
        {
            System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            client.DefaultRequestHeaders.Add("OpenAI-Beta", "assistants=v2");
            var body = new { role = "user", content = message };
            var content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
            var response = await client.PostAsync($"https://api.openai.com/v1/threads/{threadId}/messages", content);
            var result = await response.Content.ReadAsStringAsync();
        }

        static string RunAssistant(string apiKey, string threadId, string assistantId, string fileId)
        {
            System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            client.DefaultRequestHeaders.Add("OpenAI-Beta", "assistants=v2");

            var body = new
            {
                assistant_id = assistantId,
                tool_resources = new
                {
                    file_search = new
                    {
                        vector_store_ids = new[] { fileId }
                    }
                }
            };

            var content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
            var response = client.PostAsync($"https://api.openai.com/v1/threads/{threadId}/runs", content).Result;

            var result = response.Content.ReadAsStringAsync().Result;
            return JsonConvert.DeserializeObject<dynamic>(result).id;
        }

        static async Task<string> GetRunResult(string apiKey, string threadId, string runId)
        {
            System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            client.DefaultRequestHeaders.Add("OpenAI-Beta", "assistants=v2");
            
            while (true)
            {
                var response = await client.GetAsync($"https://api.openai.com/v1/threads/{threadId}/runs/{runId}");
                var result = await response.Content.ReadAsStringAsync();
                Console.WriteLine("Run Status: " + result);
                var status = JsonConvert.DeserializeObject<dynamic>(result).status.ToString();
                if (status == "completed") break;
                await Task.Delay(2000);
            }

            var messagesResponse = await client.GetAsync($"https://api.openai.com/v1/threads/{threadId}/messages");
            var messagesResult = await messagesResponse.Content.ReadAsStringAsync();

            var root = JObject.Parse(messagesResult);

            // Loop through messages
            foreach (var message in root["data"])
            {
                var role = message["role"]?.ToString();
                if (role == "assistant")
                {
                    // Navigate to content → text → value
                    var contentArray = message["content"];
                    foreach (var content in contentArray)
                    {
                        var type = content["type"]?.ToString();
                        if (type == "text")
                        {
                            var value = content["text"]?["value"]?.ToString();
                            return value;
                        }
                    }
                }
            }

            return null;
        }

        static string CreateEnhancedAssistant(string apiKey, string vectorStoreId)
        {
            System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            try
            {
                var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                client.DefaultRequestHeaders.Add("OpenAI-Beta", "assistants=v2");

                var body = new
                {
                    name = "Enhanced Behtar Zindagi Bot",
                    instructions = GetEnhancedInstructions(),
                    tools = new[] { new { type = "file_search" } },
                    model = "gpt-3.5-turbo-0125",
                    tool_resources = new
                    {
                        file_search = new
                        {
                            vector_store_ids = new[] { vectorStoreId }
                        }
                    }
                };

                var json = JsonConvert.SerializeObject(body);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = client.PostAsync("https://api.openai.com/v1/assistants", content).Result;
                var responseBody = response.Content.ReadAsStringAsync().Result;

                Console.WriteLine("📥 Enhanced Assistant Create Response:\n" + responseBody);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine("❌ Error creating assistant (Status Code: " + response.StatusCode + ")");
                    return null;
                }

                var responseObject = JsonConvert.DeserializeObject<dynamic>(responseBody);
                return responseObject.id;
            }
            catch (Exception ex)
            {
                Console.WriteLine("⚠️ Exception: " + ex.Message);
                return null;
            }
        }

        static string GetEnhancedInstructions()
        {
            return @"You are a product-recommendation assistant for Behtar Zindagi. Follow EVERY rule exactly.

## 1. Response Format (STRICT)
- Output MUST be ONLY a single valid JSON object with these top-level keys exactly:
  {
    ""reply"": ""<string>"",
    ""products"": [ { ... up to 3 items ... } ],
    ""footer"": ""<string>""
  }
- NEVER include markdown, code fences, extra commentary, or emojis.
- ""products"" is an array of 1–3 product objects. Each product MUST use EXACTLY these 8 keys in this exact order:
  1) ""ProductName""  (map from product.name_hindi; if missing use product.name; else '')
  2) ""Category""     (map from product.category or '')
  3) ""Price""        (map from product.price as NUMBER; else 0)
  4) ""Mrp""          (map from product.mrp as NUMBER; else 0)
  5) ""PackSize""     (map from product.pack_size or '')
  6) ""Brand""        (map from product.brand or '')
  7) ""Image""        (map from product.image_url or '')
  8) ""BuyLink""      (map from product.buy_url; if missing use product.seo_url; else '')

## 2. Enhanced Price Range Filtering (CRITICAL)
- If query contains price constraints like ""under Rs 20000"", ""10k tak"", ""below ₹15000"":
  - Extract the maximum price limit
  - ONLY return products where price_in_inr ≤ maximum limit
  - If query has ""X to Y"" range, filter products where price_in_inr is between X and Y
  - NEVER return products outside the specified price range
  - Examples:
    - ""milking machine under Rs 20000"" → only products with price ≤ 20000
    - ""10k se 15k wali chaff cutter"" → only products with price between 10000-15000

## 3. Enhanced Intent Recognition
- Detect negative intent when user says they don't want products:
  - ""nahi chahiye"", ""don't want"", ""not interested"", ""cancel"", ""stop""
  - ""mujhe nahi chahiye"", ""no thanks"", ""enough"", ""bas""
- For negative intent: return empty products array with polite acknowledgment
- Example response for negative intent:
  {
    ""reply"": ""Samjh gaya! Koi baat nahi. Agar aapko kuch aur chahiye toh bataiye."",
    ""products"": [],
    ""footer"": ""Hum yahan madad ke liye hain.""
  }

## 4. Relevance & Matching Logic
- Match against: name, category, name_hindi, price, keywords
- Prioritize exact keyword matches over partial matches
- For price-constrained queries, relevance = keyword match + price compliance
- Return maximum 3 most relevant products
- If no strong matches found, return empty products array

## 5. Language & Tone
- Detect user's language (Hindi/Hinglish/English) and respond in same language
- ""reply"" must be short, friendly, and helpful
- Use ""name_hindi"" for ProductName if user query is in Hindi/Hinglish
- Use ""name"" for ProductName if user query is in English

## 6. Footer Content
- ""footer"" must be dynamic and contextual, not hard-coded
- Suggest related categories or alternatives in user's language
- Keep under 12 words
- Examples: ""Milte-julte vikalp: Brush Cutter Mini, Electric Cutter""

## 7. Critical Rules
- NEVER mix fields from different products in one product object
- NEVER return products outside specified price range
- NEVER suggest products when user shows negative intent
- NEVER fabricate product information
- ALWAYS return valid JSON with exact field structure
- Price and Mrp fields must be numbers, not strings with currency symbols";
        }
    }
}
