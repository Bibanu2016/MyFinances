using Newtonsoft.Json.Linq;
using System.Data;
using System.Text.Json;

namespace MyFinances.Services
{
    public class DataTableJsonService : IDataTableJsonService
    {
        public DataTable ToDataTable(string json)
        {
            var token = JToken.Parse(json);
            var dt = new DataTable();

            // Flatten JSON into key/value pairs
            var flat = new Dictionary<string, string>();
            Flatten(token, flat, "");

            // Build schema
            foreach (var key in flat.Keys)
                dt.Columns.Add(key);

            // Add row
            var row = dt.NewRow();
            foreach (var kv in flat)
                row[kv.Key] = kv.Value;

            dt.Rows.Add(row);

            return dt;

        }

        public string ToJson(DataTable table)
        {
            var rows = new List<Dictionary<string, string>>();

            List<string> keys = new List<string>();
            for(int i = 0; i < table.Columns.Count; i++)
            {
                keys.Add(table.Columns[i].ColumnName);
            }
            foreach (DataRow row in table.Rows)
            {
                var dict = new Dictionary<string, string>();
                for(int j = 0; j < keys.Count; j++)
                {
                    dict.Add(keys[j], row[j].ToString() ?? string.Empty);
                }
                /*
                foreach (DataColumn col in table.Columns)
                {
                    dict[col.ColumnName] = row[col];
                }
                */
                rows.Add(dict);
            }

            return JsonSerializer.Serialize(rows, new JsonSerializerOptions
            {
                WriteIndented = true
            });

        }

        private static void Flatten(JToken token, Dictionary<string, string> dict, string prefix)
        {
            if (token is JObject obj)
            {
                foreach (var prop in obj.Properties())
                    Flatten(prop.Value, dict, $"{prefix}{prop.Name}.");
            }
            else if (token is JArray arr)
            {
                for (int i = 0; i < arr.Count; i++)
                    Flatten(arr[i], dict, $"{prefix}.");
            }
            else
            {
                dict[prefix.TrimEnd()] = token.ToString();
            }
        }

    }
}
