using CsvHelper;
using CsvHelper.Configuration;
using System.Data;
using System.Globalization;
using System.Text.Json;

namespace MyFinances.Controllers
{
    public static class MyCsvOperations
    {
        public static DataTable CsvToDataTableWithTypes(Stream csvStream)
        {
            var dt = new DataTable();

            using var reader = new StreamReader(csvStream);
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                DetectDelimiter = true,
                IgnoreBlankLines = true,
                TrimOptions = TrimOptions.Trim,
                BadDataFound = null
            };

            using var csv = new CsvReader(reader, config);

            csv.Read();
            csv.ReadHeader();

            if (csv.HeaderRecord != null)
            {
                foreach (var header in csv.HeaderRecord)
                {
                    if (header.Length > 0) dt.Columns.Add(header, typeof(string)); // temporary type
                }
            }
            while (csv.Read())
            {
                var row = dt.NewRow();
                foreach (DataColumn col in dt.Columns)
                {
                    row[col.ColumnName] = csv.GetField(col.ColumnName);
                }
                dt.Rows.Add(row);
            }

            // Optional: infer types
            /*
            foreach (DataColumn col in dt.Columns)
            {
                bool allInt = dt.AsEnumerable().All(r => int.TryParse(r[col].ToString(), out _));
                bool allDecimal = dt.AsEnumerable().All(r => decimal.TryParse(r[col].ToString(), out _));
                bool allDate = dt.AsEnumerable().All(r => DateTime.TryParse(r[col].ToString(), out _));

                if (allInt) col.DataType = typeof(int);
                else if (allDecimal) col.DataType = typeof(decimal);
                else if (allDate) col.DataType = typeof(DateTime);
                else col.DataType = typeof(string);
            }
            */
            return dt;
        }

        public static string JsonToCsv(string json)
        {
            // Deserialize JSON into a list of dictionaries
            var records = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(json);

            using var writer = new StringWriter();
            using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture));

            // Write header
            if (records != null)
            {
                foreach (var header in records.First().Keys)
                    csv.WriteField(header);
                csv.NextRecord();
                // Write rows
                foreach (var record in records)
                {
                    foreach (var value in record.Values)
                        csv.WriteField(value);
                    csv.NextRecord();
                }
            }
            return writer.ToString();
        }

    }
}
