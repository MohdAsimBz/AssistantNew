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


namespace AssistanceProject3rdProcesses
{


    // 1. JSON structure ko represent karne ke liye classes
    public class Product
    {
        public string ProductName { get; set; }
        public string Category { get; set; }
        public decimal Price { get; set; }
        public decimal Mrp { get; set; }
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


    class Program
    { // Replace with your actual OpenAI API key
        private const string API_KEY = "xxxx"; // Replace with your OpenAI API Key
        private const string PHONE_NUMBER = "7876400500";
        private const string WEBSITE_URL = "https://behtarzindagi.in";

        static void Main(string[] args)
        {
            MainAsync(args).GetAwaiter().GetResult();
        }
        public static async Task MainAsync(string[] args)
        {
            var apiKey = API_KEY; // Replace with your actual key or config
            string fileIdJson = "file-XKm45X3dkxFCb5EzNSjXfJ";
            string vectorjsonNew = "vs_688cb22f82c8819185e50dd2a878caf2";
            var assistantIdJsonNew = "asst_SicuKsYxn9nOSKBGJoeGyRcp";
            string userId = "8077675133";
            Console.WriteLine("Welcome! Type your query (or 'exit' to quit):");

            string threadId = "";
            string existingThreadId = GetThreadIds(userId);

            if (string.IsNullOrEmpty(existingThreadId))
            {
                // First time user — create thread via OpenAI
                 threadId = CreateThread(apiKey).GetAwaiter().GetResult(); // API se mila thread id
                SaveThreadIds(userId, threadId);
            }
            else
            {
                // Use existing threadId
                threadId = existingThreadId;
            }
            while (true)
            { 

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write("\nYou: ");
                Console.ResetColor();
                string Query = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(Query))
                    continue;

                if (Query.ToLower() == "exit")
                    break;

                // Send to GPT
                string responce = "";
                AddMessageToThread(apiKey, threadId, Query).GetAwaiter().GetResult();
                var runId = RunAssistant(apiKey, threadId, assistantIdJsonNew, vectorjsonNew);
                string FinalResult = GetRunResult(apiKey, threadId, runId).GetAwaiter().GetResult();
                string UseridNew = "8077675133";
                if (IsValidJson(FinalResult))
                {
                    
                    FormattedResponse response = ParseJsonResponse(FinalResult);
                   
                    // 1. Send main reply
                   

                    if (!string.IsNullOrEmpty(response.Reply))
                    {
                      //  responce = await SendMessageAsync(UseridNew, "Text", "*" + response.Reply + "*", "", "");
                        await Task.Delay(300);
                    }



                    // 2. Send products one by one
                    foreach (var msg in response.ProductMessages)
                    {
                        if (!string.IsNullOrEmpty(msg.ImageUrl))
                        {
                         //   responce = await SendMessageAsync(UseridNew, "Image", msg.Text, msg.ImageUrl.Replace(" ", "%20"), msg.BuyUrl);
                            await Task.Delay(300);
                        }
                    }

                    // 3. Send footer
                    if (!string.IsNullOrEmpty(response.Footer))
                    {
                     //   responce = await SendMessageAsync(UseridNew, "Text", "*" + response.Footer + "*", "", "");
                        await Task.Delay(300);
                    }
                }
                else
                {



       string desiredFooter = GenerateDynamicFooter(FinalResult, "hindi");

                    string jsonOutput = ParseResponseToJson(FinalResult, desiredFooter);
                    FormattedResponse response = ParseJsonResponse(jsonOutput);

                    if (!string.IsNullOrEmpty(response.Reply))
                    {
                        responce =  SendMessageAsync(UseridNew, "Text", "*" + response.Reply + "*", "", "").GetAwaiter().GetResult();
                        //await Task.Delay(300);
                    }



                    // 2. Send products one by one
                    foreach (var msg in response.ProductMessages)
                    {
                        if (!string.IsNullOrEmpty(msg.ImageUrl))
                        {
                               responce =  SendMessageAsync(UseridNew, "Image", msg.Text, msg.ImageUrl.Replace(" ", "%20"), msg.BuyUrl).GetAwaiter().GetResult();
                            //await Task.Delay(300);
                        }
                    }

                    // 3. Send footer
                    if (!string.IsNullOrEmpty(response.Footer))
                    {
                          responce =  SendMessageAsync(UseridNew, "Text", "*" + response.Footer + "*", "", "").GetAwaiter().GetResult();
                        // await Task.Delay(300);
                    }




                    //string jsonOutput = ConvertMarkdownToJson(FinalResult);

                    //responce = SendMessageAsync(userId, "Text", "*" + FinalResult + "*", "", "").GetAwaiter().GetResult();
                }
                // Console.WriteLine(response.Footer);



                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Assistant: " + FinalResult);
                Console.ResetColor();
            }

            Console.WriteLine("Exited.");
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

            // Match product blocks more flexibly
            string pattern = @"(?<index>\d+)\.\s*\*\*(?<ProductName>.*?)\*\*\s*[\r\n]+(?:\s*-\s+\*\*Price:\*\*\s*(INR\s*)?(?<Price>.*?)\s*[\r\n]+)?(?:\s*-\s+\*\*MRP:\*\*\s*(INR\s*)?(?<Mrp>.*?)\s*[\r\n]+)?(?:\s*-\s+\*\*Category:\*\*\s*(?<Category>.*?)\s*[\r\n]+)?(?:\s*-\s+\*\*Brand:\*\*\s*(?<Brand>.*?)\s*[\r\n]+)?(?:\s*-\s+\*\*Image:\*\*\s*!\[.*?\]\((?<Image>.*?)\)[\r\n]+)?(?:\s*-\s+\*\*Buy Link:\*\*\s*\[.*?\]\((?<BuyLink>.*?)\))?";

            var matches = Regex.Matches(textResponse, pattern, RegexOptions.Multiline | RegexOptions.IgnoreCase);

            // Detect the heading/reply
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
                    Price = ParseNumericPrice(match.Groups["Price"].Success ? match.Groups["Price"].Value.Trim() : "0"),
                    Mrp = ParseNumericPrice(match.Groups["Mrp"].Success ? match.Groups["Mrp"].Value.Trim() : "0"),
                    Brand = match.Groups["Brand"].Success ? match.Groups["Brand"].Value.Trim() : "",
                    PackSize = "1.00 Pieces", // fallback value
                    Image = match.Groups["Image"].Success ? match.Groups["Image"].Value.Trim().Replace(" ", "%20") : "",
                    BuyLink = match.Groups["BuyLink"].Success ? match.Groups["BuyLink"].Value.Trim() : ""
                };

                botResponse.products.Add(product);
            }

            if (botResponse.products.Count == 0)
            {

        private static decimal ParseNumericPrice(string priceString)
        {
            if (string.IsNullOrEmpty(priceString))
                return 0;

            string cleanPrice = priceString.Replace("INR", "").Replace("₹", "").Replace(",", "").Trim();
            
            if (decimal.TryParse(cleanPrice, out decimal result))
                return result;
            
            return 0;
        }

        public static string DetectLanguage(string query)
        {
            if (string.IsNullOrEmpty(query))
                return "english";

            if (System.Text.RegularExpressions.Regex.IsMatch(query, @"[\u0900-\u097F]"))
                return "hindi";

            string[] hinglishPatterns = {
                @"\b(kya|hai|ki|ka|ke|ko|se|me|par|aur|bhi|toh|wala|wali|chahiye|dikhao|batao|mil|gaya|nahi|abhi)\b",
                @"\b(bhai|yaar|dost|sir|madam|ji|sahab)\b",
                @"\b(paisa|rupee|rs|inr|tak|se|above|below|under|over)\b"
            };

            foreach (string pattern in hinglishPatterns)
            {
                if (System.Text.RegularExpressions.Regex.IsMatch(query.ToLower(), pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                    return "hinglish";
            }

            return "english";
        }

        public static string GenerateDynamicFooter(string response, string language)
        {
            var suggestions = new List<string>();
            
            if (response.ToLower().Contains("brush") || response.ToLower().Contains("cutter"))
                suggestions.Add("Brush Cutter");
            if (response.ToLower().Contains("tiller") || response.ToLower().Contains("cultivator"))
                suggestions.Add("Power Tiller");
            if (response.ToLower().Contains("pump") || response.ToLower().Contains("motor"))
                suggestions.Add("Water Pump");
            if (response.ToLower().Contains("spray") || response.ToLower().Contains("pesticide"))
                suggestions.Add("Sprayer");

            string baseFooter = "";
            if (language == "hindi")
            {
                if (suggestions.Count > 0)
                    baseFooter = $"Milte-julte vikalp: {string.Join(", ", suggestions.Take(2))}.";
                else
                    baseFooter = "Aur madad chahiye to batayein.";
            }
            else if (language == "hinglish")
            {
                if (suggestions.Count > 0)
                    baseFooter = $"Similar options: {string.Join(", ", suggestions.Take(2))}.";
                else
                    baseFooter = "Aur kuch chahiye to batao.";
            }
            else
            {
                if (suggestions.Count > 0)
                    baseFooter = $"Related products: {string.Join(", ", suggestions.Take(2))}.";
                else
                    baseFooter = "Need anything else? Let us know.";
            }

            return baseFooter;
        }

        public static List<string> ExtractNegativeKeywords(string query)
        {
            var negativeKeywords = new List<string>();
            var negativePatterns = new[]
            {
                @"(\w+)\s+(nahi|nahin|mat|no|not)\s+(chahiye|want|need)",
                @"(nahi|nahin|mat|no|not)\s+(\w+)",
                @"(\w+)\s+(exclude|without|except)"
            };

            foreach (var pattern in negativePatterns)
            {
                var matches = System.Text.RegularExpressions.Regex.Matches(query.ToLower(), pattern);
                foreach (System.Text.RegularExpressions.Match match in matches)
                {
                    if (match.Groups.Count > 2)
                    {
                        var keyword = match.Groups[1].Value.Trim();
                        if (!string.IsNullOrEmpty(keyword) && keyword != "nahi" && keyword != "nahin" && keyword != "mat" && keyword != "no" && keyword != "not")
                        {
                            negativeKeywords.Add(keyword);
                        }
                    }
                }
            }

            return negativeKeywords;
        }

        public static string DetectPriceSortingIntent(string query)
        {
            var cheapKeywords = new[] { "sasti", "sasta", "cheap", "low price", "kam price", "budget", "affordable" };
            var expensiveKeywords = new[] { "expensive", "costly", "premium", "high price", "mahanga", "mehenga" };

            string lowerQuery = query.ToLower();
            
            if (cheapKeywords.Any(keyword => lowerQuery.Contains(keyword)))
                return "ascending";
            
            if (expensiveKeywords.Any(keyword => lowerQuery.Contains(keyword)))
                return "descending";
            
            return "relevance";
        }

                botResponse.reply = "Maaf kijiye, humein is query se sambandhit koi product nahi mila.";
            }

            return JsonConvert.SerializeObject(botResponse, Formatting.Indented);
        }

        private static decimal ParseNumericPrice(string priceString)
        {
            if (string.IsNullOrEmpty(priceString))
                return 0;

            // Remove currency symbols and common prefixes
            string cleanPrice = priceString.Replace("INR", "").Replace("₹", "").Replace(",", "").Trim();
            
            if (decimal.TryParse(cleanPrice, out decimal result))
                return result;
            
            return 0;
        }

        public class ProductBlock
        {
            public string ProductName { get; set; }
            public string Category { get; set; }
            public string Price { get; set; }
            public string Mrp { get; set; } = "";
            public string PackSize { get; set; }
            public string Brand { get; set; }
            public string Image { get; set; }
            public string BuyLink { get; set; }
        }
        public static string ConvertMarkdownToJson(string inputText)
        {
            var products = new List<ProductBlock>();

            // Extract reply (first line)
            string[] lines = inputText.Trim().Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            string replyText = lines.Length > 0 ? lines[0].Trim() : "Here are some products for you.";

            // Extract footer (last paragraph with 'Aur kuch')
            string footer = "";
            var footerMatch = Regex.Match(inputText, @"Aur kuch.*", RegexOptions.IgnoreCase);
            if (footerMatch.Success)
                footer = footerMatch.Value.Trim().Replace("[", "").Replace("]", "").Replace("(", "").Replace(")", "");

            // Remove reply and footer from main body
            string mainContent = inputText.Replace(replyText, "").Replace(footerMatch.Value, "").Trim();

            // Extract product blocks by splitting on numbered entries
            var productBlocks = Regex.Split(mainContent, @"\n\d+\.\s+\*\*Product Name\*\*:")
                                     .Skip(1)
                                     .Select(x => "**Product Name**:" + x.Trim())
                                     .ToList();

            foreach (var block in productBlocks)
            {
                var product = new ProductBlock
                {
                    ProductName = ExtractField(block, @"\*\*Product Name\*\*:\s*(.*?)\n"),
                    Category = ExtractField(block, @"\*\*Category\*\*:\s*(.*?)\n"),
                    Price = ExtractField(block, @"\*\*Price\*\*:\s*(.*?)\n"),
                    Brand = ExtractField(block, @"\*\*Brand\*\*:\s*(.*?)\n"),
                    PackSize = ExtractField(block, @"\*\*Pack Size\*\*:\s*(.*?)\n"),
                    Image = ExtractImageUrl(block),
                    BuyLink = ExtractBuyLink(block)
                };
                products.Add(product);
            }

            // Final JSON
            var result = new JObject
            {
                ["reply"] = replyText,
                ["products"] = JArray.FromObject(products),
                ["footer"] = footer
            };

            return result.ToString(Formatting.Indented);
        }

        static string ExtractField(string text, string pattern)
        {
            var match = Regex.Match(text, pattern);
            return match.Success ? match.Groups[1].Value.Trim() : "";
        }

        static string ExtractImageUrl(string text)
        {
            var match = Regex.Match(text, @"!\[.*?\]\((.*?)\)");
            return match.Success ? match.Groups[1].Value.Trim() : "";
        }

        static string ExtractBuyLink(string text)
        {
            var match = Regex.Match(text, @"\[Link\]\((.*?)\)");
            return match.Success ? match.Groups[1].Value.Trim() : "";
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
                { "type",Type }
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
                               $"💰 Price: ₹{p["Price"]} (MRP ₹{p["Mrp"]})\n" +
                               $"📦 Pack: {p["PackSize"]}\n" +
                               $"🏭 Brand: {p["Brand"]}"+
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


        // ✅ Named cache instance (instead of .Default)
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

        // public static void Main(string[] args)
        // {
        //     var apiKey = API_KEY; // "sk-xxxxxxxxxxxxxxxxxxxx"
        //                           //var assistantIdtxt = "asst_OZjwfJEf3t6idF1UCGmWN7kU"; 
        //     Console.WriteLine("Press any key to exit...");

        //     //string fileIdTxt = "file-DqP1vzb8Mya8FB2rnnW7A9";
        //     string fileIdJson = "file-XKm45X3dkxFCb5EzNSjXfJ";
        //     string vectortxt = "vs_6870f839cb748191ae61d76c70e87fb4";


        //     string vectorjsonOld = "vs_68724a1e4f9c8191a005ec4f03e8af7c";
        //     string vectorjsonNew = "vs_687a0d4bfb1c81918bfb498293137dba";
        //     string Query = "";
        //     var threadId = "";
        //     var assistantIdJsonOld = "asst_OoASyXYJcF0LlY6Rfbw9mjgT";// "asst_VhJgQaOTKlpSHdi24HNWxFoa";
        //     var assistantIdJsonNew = "asst_2I14a1cgAgDxgKc1T27vph8g"; 
        //     //Query = TranscribeFromUrl(API_KEY, "https://interaktprodmediastorage.blob.core.windows.net/mediaprodstoragecontainer/666e4e04-1050-4b1d-82a8-8c15506b790c/inbox_customer_to_agent/mMLYEeqeuspi/CdXcjlPBcHoE.ogg?se=2030-07-08T07%3A23%3A49Z&sp=r&sv=2022-11-02&sr=b&sig=gDyIkJS6BtdSBQQ0qZDoFSMGPIQ%2BOzQle6i73esTIgE%3D");

        //     Query = "chop katr 20k wala dikhao";


        //     if (string.IsNullOrEmpty(threadId))
        //     {
        //         threadId = CreateThread(apiKey).GetAwaiter().GetResult();
        //     }

        //     AddMessageToThread(apiKey, threadId, Query).GetAwaiter().GetResult();
        //     var runId = RunAssistant(apiKey, threadId, assistantIdJsonNew, vectorjsonNew);
        //     string FinalResult = GetRunResult(apiKey, threadId, runId).GetAwaiter().GetResult();

        //     /////////////// More Meaning ful msg /////////////


        //    // List<ProductInfo> extractedProducts = ExtractProductsFromGPT(FinalResult);
        //    // string whatsappReply = ComposeWhatsAppMessage(extractedProducts, Query);



        //     string ggg = "";
        //     //////////////// File Upload and assistance creation /////////////////
        //    // string filePathOld = @"E:\files\enhanced_products_with_keywords.json";
        //     string filePathNew = @"E:\files\products_with_keywords2.json";


        //   // var result = UploadFile(API_KEY, filePathNew);
        //     string jjj = "";
        //     // string vectorOld =   CreateVectorStore(apiKey, "file-BngBLM8Ac8YK5rPE7Lr6U3");
        // // string vectorNew = CreateVectorStore(apiKey, "file-3qeuEkkr5gcqbz985dRaPp");
        //     string ksskdk = "";
        //// string AssistId =     CreateAssistant(apiKey, "");
        //     string kskdk = "";

        //     ///////////////////////////END //////////////////
        //     // bool T = AttachVectorStoreToAssistant(apiKey, assistantIdJsonOld, vectorjsonOld).GetAwaiter().GetResult();
        //     Console.ReadKey();
        // }
        public static string ComposeWhatsAppMessage(List<ProductInfo> products, string originalQuery)
        {
            var sb = new StringBuilder();

            if (products == null || products.Count == 0 || products[0].Name== "Yeh product Behtar Zindagi par available nahi mila. Aap humein 7876400500 par call karein ya [website](https://behtarzindagi.in) check karein. Aapko aur kuch chahiye toh bataiyega!")
            {
                sb.AppendLine("🙏 Bhai, aapka product *\"" + originalQuery + "\"* abhi Behtar Zindagi par available nahi mila.");
                sb.AppendLine("👉 Shayad product ka naam thoda alag ho ya spelling galat ho gaya ho.");
                sb.AppendLine("🔍 Aap *power tiller, brush cutter, milking machine* jaise popular products bhi try kar sakte hain.");
                sb.AppendLine("\n📞 Adhik jankari ke liye, humein 7876400500 par call karein ya [website](https://behtarzindagi.in) par check karein. Aur kuch dikhau?");
                return sb.ToString();
            }

            var product = products[0]; // ✅ Top match only

            sb.AppendLine($"✅ Bhai, aapke query *\"{originalQuery}\"* ke liye yeh product mil gaya hai:");
            sb.AppendLine($"\n🧑‍🌾 *{product.Name}*");
            sb.AppendLine($"💰 Price: {product.Price}");
            sb.AppendLine($"🖼️ Image: {product.ImageUrl}");
            sb.AppendLine($"🛒 Buy: {product.BuyLink}");

            var payLine = GenerateRazorpayLink(product);
            if (!string.IsNullOrEmpty(payLine))
            {
                sb.AppendLine($"\n{payLine}");
            }

          //  sb.AppendLine($"\n➡️ View More Like *{product.Name}*");
            sb.AppendLine(GenerateSuggestionLink(product));

            sb.AppendLine("\n📞 Agar aapko iske alawa koi aur product chahiye ya confusion hai, to humein call karein: 7876400500");

            sb.AppendLine("📦 Ya fir 'aur dikhao', 'brush cutter chahiye', '3hp motor', 'bijli wali machine' jaise aur sawaal bhejein!");

            return sb.ToString();
        }


        public static string GenerateSuggestionLink(ProductInfo product)
        {
            // Replace spaces with hyphens and encode for URL safety
            string formattedCategory = product.Category?.Trim().Replace(" ", "-");
            return $"➡️ View More Like: *{product.Name}*\nhttps://behtarzindagi.in/products/categoryproductlist/"+ formattedCategory;
        }
        public static string GenerateRazorpayLink(ProductInfo product)
        {
            product.Price = "416";

            //if (!decimal.TryParse(product.Price.Replace("INR", "").Replace("₹", "").Replace(",", "").Trim(), out product.Price))
            //    return null;

            decimal advance = Math.Round(Convert.ToDecimal(product.Price) * 0.10m, 2);
            string payLink = $"https://rzp.io/l/{Uri.EscapeDataString(product.Name.Replace(" ", "-"))}-{advance}";
            return $"🔥 *Sirf ₹{advance} bhar ke reserve karein!* 🔗 {payLink}";
        }



        public static List<ProductInfo> ExtractProductsFromGPT(string gptText)
        {
            var products = new List<ProductInfo>();

            // Split based on "**Product Name:**" or "**Name:**"
            var blocks = Regex.Split(gptText, @"\*\*(Name):\*\*")
                              .Select(b => b.Trim())
                              .Where(b => !string.IsNullOrWhiteSpace(b))
                              .ToList();

            foreach (var block in blocks)
            {
                var nameMatch = Regex.Match(block, @"^(.*?)(?:\r?\n|$)");
                var priceMatch = Regex.Match(block, @"\*\*Price:\*\*\s*(INR|₹)?\s?([0-9,\.]+)");
                var imageMatch = Regex.Match(block, @"\*\*Image:\*\*\s*!\[.*?\]\((https?://.*?)\)");
                var buyMatch = Regex.Match(block, @"\*\*Buy Link:\*\*.*?\((https?://.*?)\)");
                var categoryMatch = Regex.Match(block, @"\*\*Category:\*\*\s*(.*)");

                var product = new ProductInfo
                {
                    Name = nameMatch.Success ? nameMatch.Groups[1].Value.Trim() : null,
                    Price = priceMatch.Success ? $"INR {priceMatch.Groups[2].Value.Trim()}" : null,
                    ImageUrl = imageMatch.Success ? imageMatch.Groups[1].Value.Trim() : null,
                    BuyLink = buyMatch.Success ? buyMatch.Groups[1].Value.Trim() : null,
                    Category = categoryMatch.Success ? categoryMatch.Groups[1].Value.Trim() : null
                };

                if (!string.IsNullOrEmpty(product.Name))
                    products.Add(product);
            }

            return products;
        }


        public class ProductInfo
        {
            public string Name { get; set; }
            public string Price { get; set; }
            public string ImageUrl { get; set; }
            public string BuyLink { get; set; }

            public string Category { get; set; }
        }
        public class DiscountOffer
        {
            public string Message { get; set; }
            public string RazorpayPaymentLink { get; set; }
        }

        public class SuggestedProductLink
        {
            public string Title { get; set; }
            public string Link { get; set; }
        }
        public static async Task<bool> AttachVectorStoreToAssistant(string apiKey, string assistantId, string vectorStoreId)
        {
            System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            client.DefaultRequestHeaders.Add("OpenAI-Beta", "assistants=v2");

            var body = new
            {
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

            var request = new HttpRequestMessage(new HttpMethod("PATCH"), $"https://api.openai.com/v1/assistants/{assistantId}")
            {
                Content = content
            };

            var response = await client.SendAsync(request);
            var result = await response.Content.ReadAsStringAsync();

            Console.WriteLine("🧠 Attach Vector to Assistant Response:\n" + result);
            return response.IsSuccessStatusCode;
        }

        public static string TranscribeFromUrl(string apiKey, string audioUrl)
        {
            // Temporary file to save the downloaded audio
            string tempPath = Path.Combine(Path.GetTempPath(), "converted_audio.mp3");

            using (var httpClient = new HttpClient())
            {
                var audioBytes = httpClient.GetByteArrayAsync(audioUrl).Result;
                File.WriteAllBytes(tempPath, audioBytes); // ✅ Save as file
            }

            // Transcribe from the saved local file
            return TranscribeAudio(apiKey, tempPath).GetAwaiter().GetResult();
        }
        public static async Task<string> TranscribeAudio(string apiKey, string audioFilePath)
        {

            System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            using (var client = new HttpClient())
            using (var form = new MultipartFormDataContent())
            {
                // Set authorization header
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

                // Add model
                form.Add(new StringContent("whisper-1"), "model");

                // Optional: You can force Hindi if needed
                form.Add(new StringContent("en"), "language");

                // Attach audio file
                var fileBytes = File.ReadAllBytes(audioFilePath);
                var byteContent = new ByteArrayContent(fileBytes);
                byteContent.Headers.ContentType = MediaTypeHeaderValue.Parse("audio/wav"); // or audio/mpeg for mp3
                form.Add(byteContent, "file", Path.GetFileName(audioFilePath));

                // Send POST request
                var response = await client.PostAsync("https://api.openai.com/v1/audio/transcriptions", form);
                var responseString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine("❌ Whisper API Error:\n" + responseString);
                    return null;
                }

                dynamic result = Newtonsoft.Json.JsonConvert.DeserializeObject(responseString);
                return result.text;
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
         //   Console.WriteLine("Thread: " + result);
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
           // Console.WriteLine("Message: " + result);
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
                        vector_store_ids = new[] { fileId } // ✅ Link your file here
                    }
                }
            };

            var content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
            var response = client.PostAsync($"https://api.openai.com/v1/threads/{threadId}/runs", content).Result;

            var result = response.Content.ReadAsStringAsync().Result;
           // Console.WriteLine("Run: " + result);
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
           // Console.WriteLine("Messages: " + messagesResult);

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

        public static string UploadFile(string apiKey, string filePath)
        {
            System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

                var content = new MultipartFormDataContent
            {
                { new StringContent("assistants"), "purpose" },
                { new ByteArrayContent(File.ReadAllBytes(filePath)), "file", Path.GetFileName(filePath) }
            };

                // ⚠️ Use .Result instead of await
                var response = client.PostAsync("https://api.openai.com/v1/files", content).GetAwaiter().GetResult();
                var responseBody = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine("❌ Upload failed:\n" + responseBody);
                }

                return responseBody;
            }
        }
        static string CreateVectorStore(string apiKey, string fileId)
        {
            System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            client.DefaultRequestHeaders.Add("OpenAI-Beta", "assistants=v2");

            var body = new { file_ids = new[] { fileId } };
            var content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");

            var response = client.PostAsync("https://api.openai.com/v1/vector_stores", content).Result;
            var result = response.Content.ReadAsStringAsync().Result;
            Console.WriteLine("📦 Vector Store Create Response:\n" + result);
            return JsonConvert.DeserializeObject<dynamic>(result).id;
        }

        static string CreateAssistant(string apiKey, string vectorjsonOld)
        {
            System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            try
            {
                var client = new HttpClient();
                System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                client.DefaultRequestHeaders.Add("OpenAI-Beta", "assistants=v2");

                var body = new
                {
                    name = "Behtar Zindagi Bot",
                    instructions = @"# Final Assistant Instructions for Behtar Zindagi (Multilingual, Catalog-Safe, Sales-Optimized)

##  1. Assistant Goals & Behavior

1. Help customers find accurate information about products, services, pricing, usage instructions, and support queries.
2. Respond in the language used by the customer without asking them to switch.
3. Actively support sales by offering helpful product suggestions, upselling relevant accessories, and cross-selling related solutions.
4. Be concise and structured – prioritize the user's exact question, and never overwhelm them with raw text.
5. Summarize document or catalog info clearly. Avoid quoting or repeating large chunks.
6. If no relevant answer is found, respond: ""No relevant product found.""
7. Never hallucinate product details. Only answer based on catalog content.
9. Maintain a friendly, sales-forward tone — you're a digital ""krishi saathi"".
10. Always respond in the same language as the customer's message
11. Do not translate content from documents unless the user asks.
12. Never switch to English if the customer is using Hindi or Hinglish.
13. Summarize all results in user's original language.

##  2. Query Understanding & Language Handling

- Detect and respond in the same language & dialect as the user.

##  3. Enhanced Matching & Relevance Logic

- Match against: name, category, name_hindi, price, keywords.
- **NEGATIVE KEYWORD FILTERING**: If user says ""machine nahi chahiye"" or ""no machine"", EXCLUDE all products containing ""machine"" in name, category, or keywords.
- **NEGATIVE INTENT DETECTION**: If user says ""nahi chahiye"", ""don't want"", ""mat do"", ""cancel"", ""no products needed"", respond with empty products array and helpful message.
- **RELATIVE PRICE SORTING**: For queries like ""sasti chaff cutter"" or ""cheap pump"", sort results by ASCENDING price (cheapest first). For ""expensive"" or ""premium"", sort by DESCENDING price.
- Always sort products by relevance to the user's query (based on keyword match in name), then apply price sorting if detected.
- **PRICE RANGE FILTERING**: Parse specific price ranges like ""under Rs 20000"", ""20k tak"", ""below ₹15000"", ""10 se 15 hazar"", ""16k tak"" and filter products accordingly.
- Respect ALL price constraints including ranges, maximum limits, and budget specifications.
- Always return 3–4 products. If less than 3 match, show related products.
- Never return only 1 if more options exist.
- Never assume, guess or invent products.

##  4. Follow-up & Context Behavior

- If user follow-up is vague (e.g., ""Dono milengi kya?"") and no prior product context exists:
  - Hindi: ""Kripya bataye kaunse product ke baare mein poochh rahe hain?""
  - English: ""Please specify which  product you're referring to.""
- If product category changes, ignore old category and only respond to the new query.

You are a product-recommendation assistant for Behtar Zindagi. Follow EVERY rule exactly.

1) Response Format (STRICT)
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
- For TEXT fields: if missing → """" (empty string). For numeric Price/Mrp: if missing → 0 (number). DO NOT add currency symbols or units in numeric fields.
- Keys must be spelled exactly as above. Do not add, remove, rename, or reorder keys. Do not add extra fields anywhere.

2) Language & Tone
- Detect the user's language (Hindi / Hinglish / English) from their latest message.
- ""reply"" must be short, friendly, and helpful in the user's language.
- Emphasize rural user benefits briefly when relevant.
- Be professional and non-robotic. No technical jargon, no filler.

3) Politeness & Abuse Handling
- If user is rude/abusive, set ""reply"" to:
  - Hindi: ""Aapke liye achhi seva dena hamara lakshya hai. Kripya apna prashn dobara poochhein.""
  - English: ""I'm here to help. Let's focus on your request.""
- Still return a valid JSON with empty products [] and a helpful ""footer"".

4) Enhanced Product Selection Rules
- Recommend only products that exist in the provided catalog JSON.
- **NEGATIVE FILTERING**: Extract negative keywords from queries (e.g., ""chaff cutter, machine nahi chahiye"" → exclude products with ""machine"").
- **PRICE RANGE FILTERING**: Parse and apply specific price ranges:
  - ""under Rs 20000"" → filter products where price < 20000
  - ""20k tak"" → filter products where price <= 20000
  - ""below ₹15000"" → filter products where price < 15000
  - ""10 se 15 hazar"" → filter products where price >= 10000 AND price <= 15000
  - ""16k tak"" → filter products where price <= 16000
- **PRICE SORTING**: Detect relative price terms (""sasti"", ""cheap"" → ascending price; ""expensive"", ""premium"" → descending price).
- **REJECTION HANDLING**: If user says ""nahi chahiye"", ""don't want"", ""mat do"", ""cancel"", ""no products needed"", return empty products array with helpful message.
- Relevance logic: normalize layman queries (e.g., ""than ki dawai"" → mastitis medicine) and match against product fields: name, name_hindi, category, price, keywords.
- Return up to 3 best matches (do NOT hallucinate).
- Price/brand/model filters from the user must be respected.
- Tie-breakers: stock availability (prefer in_stock:true), better rating, closer price to user's budget.

5) Product Binding Integrity (STRICT)
- All fields in a single product object MUST come from the SAME catalog product.
- Never mix metadata from different products.
- ""Image"" must match the product and ""BuyLink"" must open that same product.
- One array element = one complete product.

6) Footer Content
- ""footer"" must NOT be hard-coded.
- Build a single sentence in the user's language suggesting related categories/alternatives derived from the user's intent and/or from other close matches in the catalog (no more than 12 words, no links). Example (Hindi): ""Milte-julte vikalp: Brush Cutter Mini, 52CC, Electric Cutter.""
- If nothing relevant, keep a neutral helpful line (same language): ""Aur madad chahiye to batayein.""

7) Formatting & Validation
- Output must be valid JSON (UTF-8), with double quotes for keys/strings.
- No markdown, no backticks, no comments, no trailing commas.
- Numbers must be plain numbers (e.g., 5999, not ""₹5999"" and not ""5999.0"" unless the source is decimal).
- If the query is off-catalog (no matches), return:
  - ""reply"": a gentle clarification question in the user's language,
  - ""products"": [],
  - ""footer"": a short helpful nudge (per rule 6).

8) Field Mappings Summary
- ProductName ← product.name_hindi; fallback product.name; else ''
- Category    ← product.category or ''
- Price       ← product.price (NUMBER) or 0
- Mrp         ← product.mrp (NUMBER) or 0
- PackSize    ← product.pack_size or ''
- Brand       ← product.brand or ''
- Image       ← product.image_url or ''
- BuyLink     ← product.buy_url; fallback product.seo_url; else ''

9) Examples (DO NOT COPY LITERALLY; BUILD FROM REAL CATALOG)
Valid:
{
  ""reply"": ""Brush cutter ke saste aur majboot vikalp ye rahe:"",
  ""products"": [
    {
      ""ProductName"": ""एबीएसए 2 स्ट्रोक 2.4 एचपी 52CC साइडपैक ब्रश कटर चेन ट्रिमर और ब्लेड के साथ"",
      ""Category"": ""Brush Cutter"",
      ""Price"": 5999,
      ""Mrp"": 9000,
      ""PackSize"": ""1.00 Pieces"",
      ""Brand"": ""A B S A Trades Private Limited"",
      ""Image"": ""https://d18xt1kbffvwx1.cloudfront.net/BzImage/20250722182848_2.4%20HP%2052CC%20Sidepack%20Brush%20Cuttter.jpg?width=628&height=628"",
      ""BuyLink"": ""https://behtarzindagi.in/products/productdetails/17174""
    }
  ],
  ""footer"": ""Milte-julte vikalp: Brush Cutter 52CC, Mini Cutter.""
}

Invalid (reasons): mixed fields from multiple products; extra keys; strings for numbers; emojis; markdown; wrong key order; missing required keys.

## 🔍 Enhanced Search Logic Rules

1. **Negative Keyword Detection**: Parse queries for patterns like ""X nahi chahiye"", ""no X"", ""without X"", ""mat chahiye"" and exclude matching products.
2. **Price Range Parsing**: Extract specific price ranges and apply strict filtering:
   - ""under Rs 20000"" / ""20k tak"" → price <= 20000
   - ""below ₹15000"" → price < 15000
   - ""10 se 15 hazar"" → price >= 10000 AND price <= 15000
   - ""above 25k"" → price > 25000
3. **Price Sorting Intelligence**: Detect ""sasti"", ""cheap"", ""budget"" → sort ascending; ""expensive"", ""premium"", ""costly"" → sort descending.
4. **Rejection Intent Detection**: If user says ""nahi chahiye"", ""don't want"", ""mat do"", ""cancel"", ""no products needed"", return empty products with helpful message.
5. **Layman Term Mapping**: ""than ki dawai"" → mastitis medicine, ""kharpatwar"" → weed killer, ""beej"" → seeds.
6. **Context Preservation**: Maintain search intent across follow-up questions within same conversation.

## 🚫 Critical Rejection Scenarios
- If user says ""nahi chahiye"" without specifying product → return empty products array
- If user says ""cancel"" or ""mat do"" → return empty products array  
- If user says ""no products needed"" → return empty products array
- If user explicitly rejects suggestions → acknowledge and ask for clarification

## 🎯 Final Rule

You are not just a chatbot — you are a trusted digital krishi saathi. Help the customer make confident, fast, and accurate buying decisions. Stay helpful, contextual, and native to their language and needs.",

                    ////////// Better hai///////////////////

//                    instructions = @"You are a helpful and friendly WhatsApp assistant for Behtar Zindagi. Your ONLY job is to provide product information in a strict JSON format.
//---

//### 1. Matching & Relevance
//- Understand and match layman terms (e.g., 'than ki dawai' → 'mastitis medicine').
//- Match query keywords to 'name', 'name_hindi', 'price_in_inr', or 'keywords' fields.
//- Return ONLY the 3 most relevant products.
//- Do NOT return products with weak or generic matches.
//- If no strong match is found, follow the 'No Product Found' protocol.
//- Search the entire uploaded file and return the most relevant products in JSON.

//### 🏷️ Price Range Matching
//- If the query includes a price range (e.g., '16k tak', '10 se 15 hazar', 'below ₹12000', 'under 20k'):
//  - Extract numeric values and apply filtering on the field 'Price'.
//  - Only include products with 'price' ≤ given max value or within the min-max range.
//  - For example:
//    - '16k tak ki milking machine' → return products where 'price_in_inr' ≤ 16000.
//    - '10 se 15 hazar wali chaff cutter' → return products where 'price' is between 10000 and 15000.
//  - Ignore products outside the budget, even if there is a name or keyword match.

//---

//### 2. Language and Tone
//- Greeting: Always start with a warm, human-like greeting.
//- Language Detection:
//  - Hindi Query → Hindi Reply
//  - Hinglish Query → Hinglish Reply (Use emojis for a friendly tone)
//  - English Query → English Reply
//- Abusive Queries: Handle politely and professionally.
//- Product Not Found: If no product matches, respond clearly and kindly — but never make up fake product data.
//- Conversational Style: Use a warm, human tone. Match user language automatically.
//- Use 'name_hindi' for Hindi/Hinglish replies and 'name' for English replies.

//---

//### 3. Output Format (Strict JSON Only)
//- Respond with only a JSON object, no markdown or extra commentary.
//- Always follow this structure, including for 'No Product Found':

//{
//  'reply': '<A short, friendly reply in the user\'s language. Use emojis for Hinglish.>',
//  'products': [
//    {
//      'ProductName': '<product_name>',
//      'Category': '<product_category>',
//      'Price': '<product_price>',
//      'Mrp': '<product_mrp>',
//      'PackSize': '<pack_size>',
//      'Brand': '<brand_name>',
//      'Image': '<image_url>',
//      'BuyLink': '<buy_link>'
//    }
//  ],
//  'footer': 'Aur kisi product ki information chahiye? Adhik jankari ke liye humein 7876400500 par call karein ya https://behtarzindagi.in par check karein.'
//}

//- If no matching product is found, keep 'products': [] but still include a helpful 'reply' and 'footer'.

//---

//",


                    //instructions = @"You are a helpful and friendly WhatsApp assistant for Behtar Zindagi. Your primary task is to find products from the provided JSON data and respond to the user in a strict JSON format.

                    //        ---

                 //   **1.Language and Tone:**
                 //  -**Greeting:**Always start with a warm, human-like greeting.

                 //- **Language Detection: **Respond in the same language the user uses.
                 //             -**Hindi Query → Hindi Reply**
                 //            -**Hinglish Query → Hinglish Reply (Use emojis for a friendly tone) **
                 //            -**English Query → English Reply**
                 //         -**Abusive Queries: **Handle abusive or irrelevant queries politely and professionally.

                 //        - **Product Not Found:**If a product is not found, respond politely. Do not make up information.

                 //       - **Conversational Style: **Keep the tone warm, helpful, and conversational.
                 //           -Answer user queries in Hinglish or Hindi.Use uploaded product file to match even if terms are in Hindi or English transliteration.

                    //        ---

                //        **2. Query Understanding and Product Matching:**
                //        - **Layman Terms:** Understand and normalize layman, misspelled, or slang queries. For example, 'than ki dawai' should be recognized as 'udder' or 'mastitis' medicine.
                //        - **Strict Matching:** The assistant MUST find products with a direct and strong keyword match in the `name`, `name_hindi`, or `keywords` fields.
                //        - **High Relevance Required:** Prioritize products that are highly relevant. Do NOT return products that have only a weak or generic match (e.g., a query for 'dawai' should not return a liver tonic).
                //        - **Top 3 Only:** Return a maximum of 3 products. If fewer than 3 relevant products are found, return only those.
                //        - **Handle Irrelevance:** If the query keywords do not have a strong, direct match to any product (meaning, the relevance is low), the assistant must treat it as 'no product found' and follow that protocol.
                //        - **Inference:** If an exact keyword is not present, infer the most likely product based on category, description, and context.


                //        -- -

                //        **3.Response Format(Strict JSON):**
                //        - **JSON Only:** Your entire response must be a single, valid JSON object. Do not add any text, headings, or markdown outside of the JSON.
                //        - **Field Selection:**
                //          - If the user query is in **Hindi** or **Hinglish**, populate the `ProductName` field with the value from `name_hindi`.
                //          - If the user query is in **English**, populate the `ProductName` field with the value from `name`.
                //        - **Structure:** The JSON must follow this exact structure:

                //        ```json
                //        {
                //                            'reply': '<A short, friendly message in the user's language(Hindi, Hinglish, or English). Use emojis only for Hinglish replies.>',
                //          'products': [
                //            {
                //              'ProductName': '<product_name>',
                //              'Category':'<product_category>',
                //              'Price':'<product_price>',
                //              'Mrp': '<product_mrp>',
                //             'PackSize': '<pack_size>',
                //              'Brand': '<brand_name>',
                //              'Image': '<image_url>',
                //              'BuyLink': '<buy_link>'
                //            }
                //          ],
                //          'footer': 'Aur kuch dikhau? Adhik jankari ke liye humein 7876400500 par call karein ya [https://behtarzindagi.in](https://behtarzindagi.in) par check karein.'
                //        }
                //",
                tools = new[] { new { type = "file_search" } },
                    model = "gpt-3.5-turbo-0125",
                    tool_resources = new
                    {
                        file_search = new
                        {
                            vector_store_ids = new[] { vectorjsonOld }
                        }
                    }
                };

                var json = JsonConvert.SerializeObject(body);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = client.PostAsync("https://api.openai.com/v1/assistants", content).Result;
                var responseBody = response.Content.ReadAsStringAsync().Result;

                Console.WriteLine("📥 Assistant Create Response:\n" + responseBody);

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
        //        static string CreateAssistant(string apiKey,string vectorjsonOld)
        //        {
        //            System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        //            try
        //            {
        //                var client = new HttpClient();
        //                System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

        //                // 🔐 Authorization + Assistant v2 Header
        //                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        //                client.DefaultRequestHeaders.Add("OpenAI-Beta", "assistants=v2");

        //                // 🔧 ✅ No file_ids here
        //                var body = new
        //                {
        //                    name = "Behtar Zindagi Bot",
        //                    ///////////////////////////////// Frist//////////////////////////////////////////////////////
        //                    //instructions = @"You are a smart, friendly WhatsApp assistant for Behtar Zindagi.
        //                    // Your ONLY job is to generate warm, helpful, and short responses to product-related queries in JSON format — never in plain text or markdown.
        //                    //                    - Always greet users in their language (Hindi, Hinglish, English,Malyalam or Kannad).
        //                    //                    - Normalize layman, misspelled, or unknown queries, and map them to standard product names using examples and context. For example:
        //                    //                        - 'caf katar', 'ghaas kaatne wali machine' → 'chaff cutter'
        //                    //                        - 'than ki bimari ki dawai', 'udder spray', 'mastitis ki dawa' → 'mastitis spray'
        //                    //                    - If the user's query uses an unknown, misspelled, or new word, always try to infer the most likely product using the product description, category, and context. Use your best judgment to match the user's intent with the closest product, even if an exact keyword is not present.
        //                    //                    - Search the uploaded product file for the 4 best matching products (by name,name_hindi, keywords, category, or semantic meaning).
        //                    //                    - Always reply in the same language as the user's message (Hindi,Hinglish/English/Malyalam/Kannad).
        //                    //                    - If product(s) found:
        //                    //                        •Always Show: Product Name, Price, Category, Image URL, Buy Link in each product.
        //                    //                        • End reply with: 'Adhik jankari ke liye, humein 7876400500 par call karein ya https://behtarzindagi.in par check karein.'
        //                    //                        • Always add a positive follow-up: 'Aur kuch dikhau?' or 'Kisi aur product ki talash hai?'
        //                    //                    - If product not found:
        //                    //                        • Politely say: 'Yeh product Behtar Zindagi par abhi uplabdh nhi hai. Aap humein 7876400500 par call karein ya website https://behtarzindagi.in par check karein. Aapko aur kuch chahiye toh bataiyega!'
        //                    //                    - Never guess or make up details, and never repeat the same product in one chat.
        //                    //                    - Please handle abusive and vulger query politly aur pyar se handle kro.
        //                    //                    - Keep answers short, warm, and helpful.",


        //                    ////////////////// 3rd//////////////////////////////////
        //                    //                    instructions = @"You are a smart, friendly WhatsApp assistant for Behtar Zindagi.

        //                    //Your job is to assist customers with product-related queries using short, warm, human-style messages. You must ALWAYS respond in valid JSON format — nothing outside the JSON, no markdown, no plain text, and no explanation.

        //                    //---

        //                    //📌 LANGUAGE BEHAVIOR:
        //                    //- Detect and mirror the customer's language:
        //                    //  • Hindi → reply in Hindi
        //                    //  • Hinglish (Romanized Hindi) → reply in Hinglish
        //                    //  • English → reply in English

        //                    //Use this language consistently across:
        //                    //  • reply
        //                    //  • product description
        //                    //  • footer

        //                    //---

        //                    //🔍 QUERY NORMALIZATION:
        //                    //- Normalize layman, colloquial, or misspelled terms to standard product intent using examples and keywords.

        //                    //  Examples:
        //                    //  • 'than ki dawa', 'mastitis ke liye cream' → 'mastitis spray' / 'udder care'
        //                    //  • 'gobar ke upale banane ki machine' → 'dung cake making tool'
        //                    //  • 'ghaas kaatne wali machine', 'caf cutter' → 'chaff cutter'

        //                    //Search the product list across:
        //                    //  • 'name', 'name_hindi', 'category', and 'keywords' (partial or full match)

        //                    //---

        //                    //🔎 PRODUCT MATCHING:
        //                    //- Match up to 2 best relevant products based on user query
        //                    //- Never return more than 4
        //                    //- Never repeat the same product
        //                    //- Use semantic, keyword, and category context

        //                    //---

        //                    //📦 RESPONSE FORMAT:
        //                    //You MUST reply in this **exact** JSON structure:

        //                    //{
        //                    //  'reply': '<Short, warm, helpful message in user\'s language. Use emoji only if Hinglish>',
        //                    //  'products': [
        //                    //    {
        //                    //      'ProductName': '...',
        //                    //      'Category': '...',
        //                    //      'Price': '...',
        //                    //      'Mrp': '...',
        //                    //      'PackSize': '...',
        //                    //      'Brand': '...',
        //                    //      'Image': '...',
        //                    //      'BuyLink': '...'
        //                    //    },
        //                    //    {
        //                    //      ...
        //                    //    }
        //                    //  ],
        //                    //  'footer': '<Human-style follow-up CTA in same language. Never fixed. Keep it dynamic and friendly>'
        //                    //}

        //                    //Notes:
        //                    //- 'products' must be an array, even with 1 item
        //                    //- Do not write 'Buy here', 'Here is the product', or any markdown or explanation
        //                    //- Only return plain URLs, not hyperlink text
        //                    //- Response must be a single JSON object. Nothing before or after.

        //                    //---

        //                    //✅ IF PRODUCT(S) FOUND:
        //                    //- Write a warm greeting with product name, use-case, and price
        //                    //- Add natural emoji if Hinglish
        //                    //- Make 'footer' sound dynamic — like a human would say, not a template

        //                    //✅ IF NO MATCH:
        //                    //Return this JSON:

        //                    //{
        //                    //  'reply': 'Yeh product Behtar Zindagi par abhi nahi mila. Aap chahein to 7876400500 par call karein ya https://behtarzindagi.in par check karein.',
        //                    //  'products': [],
        //                    //  'footer': 'Aur kisi product ki talash hai? Hum madad ke liye yahan hain.'
        //                    //}

        //                    //---

        //                    //⚠️ RULES:
        //                    //- Never use markdown, plain text, or assistant explanations
        //                    //- Never fabricate any product info
        //                    //- Always respond with friendly, short JSON
        //                    //- If user uses abusive or rude language, deflect politely in same language and ask how you can help

        //                    //---

        //                    //💡 EXAMPLES:

        //                    //• Hinglish: 'Bhai koi than ki spray chahiye'
        //                    //→ reply in Hinglish with product info and emoji

        //                    //• Hindi: 'गाय के थन की दवाई बताओ'
        //                    //→ reply in Hindi

        //                    //• English: 'Need machine to make dung cakes'
        //                    //→ reply in English

        //                    //• Abusive: 'Chutiya ho kya?'
        //                    //→ reply: 'Main aapki madad ke liye yahan hoon. Kripya batayein kya chahiye?'",

        //                    instructions = @"You are a helpful and friendly WhatsApp assistant for Behtar Zindagi. Your primary task is to find products from the provided JSON data and respond to the user in a strict JSON format.

        //---

        //**1. Language and Tone:**
        //- **Greeting:** Always start with a warm, human-like greeting.
        //- **Language Detection:** Respond in the same language the user uses.
        //  - **Hindi Query → Hindi Reply**
        //  - **Hinglish Query → Hinglish Reply (Use emojis for a friendly tone)**
        //  - **English Query → English Reply**
        //- **Abusive Queries:** Handle abusive or irrelevant queries politely and professionally.
        //- **Product Not Found:** If a product is not found, respond politely. Do not make up information.
        //- **Conversational Style:** Keep the tone warm, helpful, and conversational.
        //-Answer user queries in Hinglish or Hindi. Use uploaded product file to match even if terms are in Hindi or English transliteration.

        //---

        //**2. Query Understanding and Product Matching:**
        //- **Layman Terms:** Understand and normalize layman, misspelled, or slang queries. For example, 'than ki dawai' should be recognized as 'udder' or 'mastitis' medicine.
        //- **Strict Matching:** The assistant MUST find products with a direct and strong keyword match in the `name`, `name_hindi`, or `keywords` fields.
        //- **High Relevance Required:** Prioritize products that are highly relevant. Do NOT return products that have only a weak or generic match (e.g., a query for 'dawai' should not return a liver tonic).
        //- **Top 3 Only:** Return a maximum of 3 products. If fewer than 3 relevant products are found, return only those.
        //- **Handle Irrelevance:** If the query keywords do not have a strong, direct match to any product (meaning, the relevance is low), the assistant must treat it as 'no product found' and follow that protocol.
        //- **Inference:** If an exact keyword is not present, infer the most likely product based on category, description, and context.


        //-- -

        //**3.Response Format(Strict JSON):**
        //- **JSON Only:** Your entire response must be a single, valid JSON object. Do not add any text, headings, or markdown outside of the JSON.
        //- **Field Selection:**
        //  - If the user query is in **Hindi** or **Hinglish**, populate the `ProductName` field with the value from `name_hindi`.
        //  - If the user query is in **English**, populate the `ProductName` field with the value from `name`.
        //- **Structure:** The JSON must follow this exact structure:

        //```json
        //{
        //                    'reply': '<A short, friendly message in the user's language(Hindi, Hinglish, or English). Use emojis only for Hinglish replies.>',
        //  'products': [
        //    {
        //      'ProductName': '<product_name>',
        //      'Category':'<product_category>',
        //      'Price':'<product_price>',
        //      'Mrp': '<product_mrp>',
        //     'PackSize': '<pack_size>',
        //      'Brand': '<brand_name>',
        //      'Image': '<image_url>',
        //      'BuyLink': '<buy_link>'
        //    }
        //  ],
        //  'footer': 'Aur kuch dikhau? Adhik jankari ke liye humein 7876400500 par call karein ya [https://behtarzindagi.in](https://behtarzindagi.in) par check karein.'
        //}",

        //                    ///////////////// 2nd///////////////////////////


        //                    //                    instructions = @"You are a strict JSON-generating WhatsApp assistant for Behtar Zindagi.

        //                    //Your ONLY job is to generate warm, helpful, and short responses to product-related queries in JSON format — never in plain text or markdown.

        //                    //---

        //                    //📌 LANGUAGE RULES:
        //                    //- Detect the language used in the user's message.
        //                    //  • Hindi → reply in Hindi  
        //                    //  • Hinglish (Hindi in Roman script) → reply in Hinglish  
        //                    //  • English → reply in English

        //                    ////- Use the same language consistently in:
        //                    ////  • 'reply' message  
        //                    ////  • follow-up / suggestion  
        //                    ////  • call-to-action / footer

        //                    //---

        //                    //🔍 QUERY NORMALIZATION:
        //                    //- Convert layman or misspelled terms to standard product categories. For example:
        //                    //  • 'caf katar', 'ghaas kaatne wali machine' → 'chaff cutter'  
        //                    //  • 'than ki bimari ki dawai', 'mastitis spray' → 'mastitis treatment'

        //                    //- If term is unknown, infer meaning using product name, description, category, and keywords.
        //                    //🔎 MATCHING LOGIC (Must Follow):
        //                    //- Break down user queries into individual words (tokens).
        //                    //- Match any of those words with any field in the product JSON:
        //                    //  • 'name', 'name_hindi', 'category', 'brand', 'keywords'
        //                    //- If at least one word matches any field, include that product in the response.
        //                    //- Do NOT require full sentence match. Do NOT skip if product lacks exact phrase.
        //                    //- Example:
        //                    //  User query: 'गोबर के उपले बनाने की मशीन'
        //                    //  Product:
        //                    //    name_hindi: 'गोबर का उपला बनाने का उपकरण'
        //                    //    keywords: ['गोबर', 'उपला', 'बनाने', 'tool', '(manual)']
        //                    //  → ✅ This must be returned as a match.
        //                    //-- -

        //                    //🔎 PRODUCT MATCHING:
        //                    //- Match up to 3 best products from the uploaded list based on semantic similarity, name, keywords, or category.
        //                    //- Never return more than 3.
        //                    //- Do not repeat the same product in one response.

        //                    //---

        //                    //📦 RESPONSE FORMAT:
        //                    //You must ALWAYS respond in this **exact JSON structure only**:

        //                    //{
        //                    //'reply': '<Short, warm, human-style message in user\'s language. Add emojis only if Hinglish>',
        //                    //  'products': [
        //                    //    {
        //                    //      'ProductName': '...',
        //                    //      'Category': '...',
        //                    //      'Price': '...',
        //                    //      'Mrp': '...',
        //                    //      'PackSize': '...',
        //                    //      'Brand': '...',
        //                    //      'Image': '...',
        //                    //      'BuyLink': '...'
        //                    //    }
        //                    //  ],
        //                    //  'footer': 'Aur kuch dikhau? Adhik jankari ke liye 7876400500 par call karein ya https://behtarzindagi.in visit karein.'
        //                    //}

        //                    //- The 'products' field MUST be a JSON array — even for one product.
        //                    //- The 'reply' message MUST be human-style, short, and match the user's language.
        //                    //- NEVER use markdown (**bold**, `- bullets`, `[link](url)`), HTML (`<a>`), or headings (e.g. “Here is…”).
        //                    //- NEVER add narration like: “Here are some products”, “I found”, or “Buy here”.

        //                    //---

        //                    //✅ IF PRODUCT(S) FOUND:
        //                    //- 'reply' should mention name, price, category, and plain-text BuyLink
        //                    //- 'products' should include clean structured data (as shown above)
        //                    //- End 'reply' with:
        //                    //  • 'Aur kuch dikhau?' or  
        //                    //  • 'Kisi aur product ki talash hai?'

        //                    //✅ IF NO PRODUCT MATCHED:
        //                    //- Set 'reply' as:
        //                    //  'Yeh product Behtar Zindagi par abhi nahi mila. Aap humein 7876400500 par call karein ya website https://behtarzindagi.in par check karein. Aapko aur kuch chahiye toh bataiyega!'
        //                    //- Set 'products': []
        //                    //- Use the same footer

        //                    //---

        //                    //🚫 ABSOLUTE FORMAT RULES:
        //                    //- DO NOT return markdown, bullets, headings, plain text, or explanations.
        //                    //- DO NOT wrap the JSON in backticks (```), HTML, or assistant narration.
        //                    //- DO NOT use emojis outside the 'reply' field.
        //                    //- DO NOT return anything before or after the JSON object.
        //                    //- ALWAYS return a **single valid JSON object** and nothing else.

        //                    //---

        //                    //⚠️ GENERAL:
        //                    //- Never fabricate product names, prices, or links.
        //                    //- Always keep replies short, positive, and helpful.
        //                    //- Politely handle abusive or irrelevant input.
        //                    //",


        //                    tools = new[] { new { type = "file_search" } },
        //                    model = "gpt-3.5-turbo-0125",//"gpt-3.5-turbo-0125",// "gpt-4o"// 
        //                    //model = "gpt-4o"//, // or "gpt-4-turbo"
        //                    // ✅ Attach the vector store properly here
        //                    tool_resources = new
        //                    {
        //                        file_search = new
        //                        {
        //                            vector_store_ids = new[] { vectorjsonOld }
        //                        }
        //                    }
        //                };

        //                var json = JsonConvert.SerializeObject(body);
        //                var content = new StringContent(json, Encoding.UTF8, "application/json");

        //                var response = client.PostAsync("https://api.openai.com/v1/assistants", content).Result;
        //                var responseBody = response.Content.ReadAsStringAsync().Result;

        //                Console.WriteLine("📥 Assistant Create Response:\n" + responseBody);

        //                if (!response.IsSuccessStatusCode)
        //                {
        //                    Console.WriteLine("❌ Error creating assistant (Status Code: " + response.StatusCode + ")");
        //                    return null;
        //                }

        //                var responseObject = JsonConvert.DeserializeObject<dynamic>(responseBody);
        //                return responseObject.id;
        //            }
        //            catch (Exception ex)
        //            {
        //                Console.WriteLine("⚠️ Exception: " + ex.Message);
        //                return null;
        //            }
        //        }






    }


}
