using System.Data;

namespace MyFinances.Services
{
    public interface IDataTableJsonService
    {
        string ToJson(DataTable table);

        DataTable ToDataTable(string json);
    }
}
