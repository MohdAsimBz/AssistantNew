using System;
using System.Collections.Generic;
using System.Linq;

namespace AssistantApiEnhanced
{
    public class TestEnhancedFeatures
    {
        public static void RunTests()
        {
            Console.WriteLine("🧪 Testing Enhanced Assistant Features");
            Console.WriteLine("=====================================\n");

            TestPriceRangeExtraction();
            TestNegativeIntentDetection();
            TestQueryProcessing();
            TestLanguageDetection();

            Console.WriteLine("\n✅ All tests completed!");
        }

        static void TestPriceRangeExtraction()
        {
            Console.WriteLine("1. Testing Price Range Extraction:");
            Console.WriteLine("----------------------------------");

            var testCases = new[]
            {
                ("milking machine under Rs 20000", true, 20000m, 0m),
                ("chaff cutter below ₹15000", true, 15000m, 0m),
                ("10k tak wali machine", true, 10000m, 0m),
                ("brush cutter 5000 se 8000 tak", true, 8000m, 5000m),
                ("tractor between Rs 50000 and Rs 80000", true, 80000m, 50000m),
                ("simple query without price", false, 0m, 0m),
                ("15k se kam wali product", true, 15000m, 0m)
            };

            foreach (var (query, expectedHasConstraint, expectedMax, expectedMin) in testCases)
            {
                var result = Program.ExtractPriceRange(query);
                bool passed = result.HasPriceConstraint == expectedHasConstraint &&
                             result.MaxPrice == expectedMax &&
                             result.MinPrice == expectedMin;

                Console.WriteLine($"  Query: '{query}'");
                Console.WriteLine($"  Expected: HasConstraint={expectedHasConstraint}, Max={expectedMax}, Min={expectedMin}");
                Console.WriteLine($"  Actual: HasConstraint={result.HasPriceConstraint}, Max={result.MaxPrice}, Min={result.MinPrice}");
                Console.WriteLine($"  Result: {(passed ? "✅ PASS" : "❌ FAIL")}\n");
            }
        }

        static void TestNegativeIntentDetection()
        {
            Console.WriteLine("2. Testing Negative Intent Detection:");
            Console.WriteLine("------------------------------------");

            var testCases = new[]
            {
                ("nahi chahiye", true),
                ("don't want any product", true),
                ("not interested", true),
                ("mujhe nahi chahiye", true),
                ("cancel karo", true),
                ("bas karo", true),
                ("I want a milking machine", false),
                ("show me chaff cutters", false),
                ("kya price hai", false),
                ("no thanks", true),
                ("enough products", true)
            };

            foreach (var (query, expectedNegative) in testCases)
            {
                bool result = Program.IsNegativeIntent(query);
                bool passed = result == expectedNegative;

                Console.WriteLine($"  Query: '{query}'");
                Console.WriteLine($"  Expected: {expectedNegative}, Actual: {result}");
                Console.WriteLine($"  Result: {(passed ? "✅ PASS" : "❌ FAIL")}\n");
            }
        }

        static void TestQueryProcessing()
        {
            Console.WriteLine("3. Testing Query Processing:");
            Console.WriteLine("---------------------------");

            var testCases = new[]
            {
                "milking machine under Rs 20000",
                "chaff cutter 10k tak",
                "simple product query",
                "brush cutter between 5000 and 8000"
            };

            foreach (var query in testCases)
            {
                string processed = Program.ProcessUserIntent(query);
                bool hasFilter = processed.Contains("[PRICE_FILTER:");

                Console.WriteLine($"  Original: '{query}'");
                Console.WriteLine($"  Processed: '{processed}'");
                Console.WriteLine($"  Has Price Filter: {hasFilter}");
                Console.WriteLine($"  Result: ✅ PROCESSED\n");
            }
        }

        static void TestLanguageDetection()
        {
            Console.WriteLine("4. Testing Language Detection:");
            Console.WriteLine("-----------------------------");

            var testCases = new[]
            {
                ("nahi chahiye mujhe", true),
                ("I don't want this", false),
                ("kya price hai", true),
                ("what is the cost", false),
                ("milking machine chahiye", true)
            };

            foreach (var (query, expectedHindi) in testCases)
            {
                bool result = Program.ContainsHindi(query);
                bool passed = result == expectedHindi;

                Console.WriteLine($"  Query: '{query}'");
                Console.WriteLine($"  Expected Hindi: {expectedHindi}, Actual: {result}");
                Console.WriteLine($"  Result: {(passed ? "✅ PASS" : "❌ FAIL")}\n");
            }
        }
    }
}
