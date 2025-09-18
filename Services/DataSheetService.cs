using System.Text.Json;



namespace SKF.Services
{
    
    public class DatasheetService
    {
        private readonly List<JsonDocument> _datasheets;

        public DatasheetService(IEnumerable<string> datasheetPaths)
        {
            _datasheets = datasheetPaths.Select(path => JsonDocument.Parse(File.ReadAllText(path))).ToList();
        }

        
        public string GetAttribute(string product, string attribute)
        {

            var attributeMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "height", "Pack height" },
        { "width", "Width" },
        { "outside diameter", "Outside diameter" },
        { "bore diameter", "Bore diameter" }
        // Add more as needed
    };

            string mappedAttribute = attributeMap.TryGetValue(attribute, out var mapped) ? mapped : attribute;
            foreach (var doc in _datasheets)
            {
                var root = doc.RootElement;
                string designation = root.TryGetProperty("designation", out var d) ? d.GetString() : null;
                if (designation == null || !designation.Equals(product, StringComparison.OrdinalIgnoreCase))
                    continue;

                // Check nested arrays
                string[] arrayProps = { "dimensions", "logistics", "properties", "performance", "specifications" };
                foreach (var arrayProp in arrayProps)
                {
                    if (root.TryGetProperty(arrayProp, out var arr) && arr.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in arr.EnumerateArray())
                        {
                            if (item.TryGetProperty("name", out var nameProp) &&
                                nameProp.GetString().Equals(mappedAttribute, StringComparison.OrdinalIgnoreCase))
                            {
                                if (item.TryGetProperty("value", out var valueProp))
                                {
                                    string value = valueProp.ValueKind == JsonValueKind.String
                                        ? valueProp.GetString()
                                        : valueProp.GetRawText();

                                    string unit = item.TryGetProperty("unit", out var unitProp)
                                        ? unitProp.GetString()
                                        : "";

                                    return string.IsNullOrEmpty(unit) ? value : $"{value} {unit}";
                                }
                            }
                        }
                    }
                }
            }
            return null;
        }

    }

}
