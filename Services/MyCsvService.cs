using CsvHelper;
using CsvHelper.Configuration;
using MySqlX.XDevAPI.Common;
using System.Data;
using System.Globalization;

namespace MyFinances.Services
{
    public class MyCsvService : IMyCsvService
    {
        public bool CsvFileHasHeader(Stream csvStream)
        {
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
            bool result = true;
            if (csv.HeaderRecord != null)
            {
                foreach (var header in csv.HeaderRecord)
                {
                    if (header.Length > 0)
                    {
                        if (DateTime.TryParse(header, out _) || int.TryParse(header, out _) || double.TryParse(header, out _)) { result = false; break; }
                    }// contains Date or Double or Int fields
                }
            }
            return result;
        }

        public bool CsvFileIsValid(Stream csvStream)
        {
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

            bool result = false;
            bool[] markers = [false, false]; //first has Date field, second has Double field
            if (csv.HeaderRecord != null)
            {
                if (csv.HeaderRecord.Length > 2 && csv.HeaderRecord.Length < 6) //minimum 3 columns and maximum 5
                {
                    csv.ReadAsync();
                    if (csv.Parser.Record != null)
                    {
                        for (int i = 0; i < csv.Parser.Record.Length; i++)
                        {
                            string value = csv.GetField(i) ?? string.Empty;
                            if (DateTime.TryParse(value, out _)) markers[0] = true;
                            if (Double.TryParse(value, out _)) markers[1] = true;
                        }
                        result = markers[0] && markers[1];
                    }
                }
            }
            return result;
        }

        public DataTable CsvToDataTable(Stream csvStream, bool hasHeader)
        {
            var dt = new DataTable();

            using var reader = new StreamReader(csvStream);
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = hasHeader,
                DetectDelimiter = true,
                IgnoreBlankLines = true,
                TrimOptions = TrimOptions.Trim,
                BadDataFound = null
            };

            using (CsvReader csv = new(reader, config))
            {
                if (hasHeader)
                {
                    csv.ReadHeader();
                    if (csv.HeaderRecord != null)
                    {
                        foreach (var header in csv.HeaderRecord)
                        {
                            if (header.Length > 0) dt.Columns.Add(header, typeof(string)); // temporary type
                        }
                    }
                }
                else
                {
                    csv.Read();
                    int y = csv.ColumnCount;
                    if (y > 0)
                    {
                        int x = (int)'A';
                        for (int i = 0; i < y; i++)
                        {
                            dt.Columns.Add("COLUMN-" + (char)x, typeof(string));
                            x++;
                        }
                    }
                }
                while (csv.Read())
                {
                    var row = dt.NewRow();
                    for (int j = 0; j < dt.Columns.Count; j++)
                    {
                        row[j] = csv.GetField(j) ?? string.Empty;

                    }
                    dt.Rows.Add(row);
                }
            } 
            return dt;
        }

        public DataTable CsvToDataTable(string csvFile)
        {
            DataTable dt = new DataTable();

            using var reader = new StringReader(csvFile);

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                DetectDelimiter = true,
                IgnoreBlankLines = true,
                TrimOptions = TrimOptions.Trim,
                BadDataFound = null
            };

            using (CsvReader csv = new(reader, config))
            {
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
                    for (int j = 0; j < dt.Columns.Count; j++)
                    {
                        row[j] = csv.GetField(j) ?? string.Empty;

                    }
                    dt.Rows.Add(row);
                }
            }
            return dt;
        }
    }
}
