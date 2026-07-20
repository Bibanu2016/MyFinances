using System.Data;

namespace MyFinances.Services
{
    public interface IMyCsvService
    {
        bool CsvFileHasHeader(Stream csvStream);

        DataTable CsvToDataTable(Stream csvStream, bool hasHeader);

        DataTable CsvToDataTable(string csvFile);

        bool CsvFileIsValid(Stream csvStream);
    }
}
