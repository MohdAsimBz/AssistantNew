# AssistantNew - Behtar Zindagi WhatsApp Assistant

A comprehensive multilingual WhatsApp assistant for Behtar Zindagi's agricultural product recommendation system.

## Features

- **Multilingual Support**: Handles Hindi, Hinglish, and English queries with automatic language detection
- **Enhanced Search Logic**: 
  - Negative keyword filtering (e.g., "machine nahi chahiye" excludes products with "machine")
  - Relative price sorting (e.g., "sasti chaff cutter" sorts by ascending price)
- **Strict JSON Response Format**: Ensures consistent 8-field product structure
- **WhatsApp Integration**: Complete integration with WhatsApp Business API
- **OpenAI Assistant**: Uses GPT-3.5-turbo with vector search for product recommendations
- **Sales-Optimized**: Designed as a digital "krishi saathi" for rural customers

## Project Structure

- `AssistanceProject3rdProcesses/Program.cs` - Main application code
- `AssistanceProject3rdProcesses/AssistanceProject3rdProcesses.csproj` - Project file
- `AssistanceProject3rdProcesses/App.config` - Application configuration
- `AssistanceProject3rdProcesses/packages.config` - NuGet packages
- `AssistanceProject3rdProcesses/Properties/AssemblyInfo.cs` - Assembly information
- `AssistanceProject3rdProcesses.sln` - Visual Studio solution file

## Dependencies

- .NET Framework 4.5.2
- Newtonsoft.Json 13.0.3
- System.Net.Http
- System.Runtime.Caching

## Enhanced Search Features

### Negative Keyword Filtering
The system automatically excludes products when users specify what they don't want:
- "chaff cutter, machine nahi chahiye" → excludes products containing "machine"
- "pump without electric" → excludes electric pumps

### Relative Price Sorting
Detects price preference terms and sorts results accordingly:
- "sasti chaff cutter" → shows cheapest chaff cutters first
- "expensive pump" → shows most expensive pumps first
- "budget tiller" → shows affordable tillers first

### Language Detection
Automatically detects and responds in the user's language:
- Hindi queries → Hindi responses
- Hinglish queries → Hinglish responses  
- English queries → English responses

## Configuration

Update the following in Program.cs:
- OpenAI API key
- WhatsApp Business API credentials
- Database connection strings
- Vector store IDs

## Usage

1. Build the solution in Visual Studio
2. Configure API keys and credentials
3. Run the console application
4. The system will handle WhatsApp messages automatically

## Response Format

All responses follow a strict JSON format:
```json
{
  "reply": "Short, friendly message in user's language",
  "products": [
    {
      "ProductName": "Product name in Hindi/English",
      "Category": "Product category",
      "Price": 5999,
      "Mrp": 9000,
      "PackSize": "1.00 Pieces",
      "Brand": "Brand name",
      "Image": "Image URL",
      "BuyLink": "Purchase URL"
    }
  ],
  "footer": "Context-based suggestions or help message"
}
```
