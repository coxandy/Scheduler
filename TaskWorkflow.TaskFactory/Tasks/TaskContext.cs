using System.Collections.Concurrent;
using System.Data;

namespace TaskWorkflow.TaskFactory.Tasks;

public class TaskContext
{
    private readonly ConcurrentDictionary<string, object> _variables = new();
    private readonly ConcurrentBag<DataTable> _taskDataTables = new();

    public void SetVariable(string key, object value)
    {
        _variables[key] = value;
    }

    public object GetVariable(string key)
    {
        return _variables.TryGetValue(key, out var value) ? value : null;
    }

    public bool TryGetVariable(string key, out object value)
    {
        return _variables.TryGetValue(key, out value);
    }

    public Dictionary<string, object> GetAllVariables()
    {
        return new Dictionary<string, object>(_variables);
    }

    public void AddDataTable(DataTable dt)
    {
        if (_taskDataTables.Any(x => string.Equals(x.TableName, dt.TableName, StringComparison.OrdinalIgnoreCase)))
        {
            throw new DuplicateNameException($"Duplicate Task Datatable: '{dt.TableName}'");
        }
        _taskDataTables.Add(dt);
    }

    public void AddDataTables(IEnumerable<DataTable> dataTables)
    {
        foreach (var dt in dataTables)
        {
            if (_taskDataTables.Any(x => string.Equals(x.TableName, dt.TableName, StringComparison.OrdinalIgnoreCase)))
            {
                throw new DuplicateNameException($"Duplicate Task Datatable: '{dt.TableName}'");
            }
            _taskDataTables.Add(dt);
        }
    }

    public DataTable GetDataTable(string tableName)
    {
        return _taskDataTables.FirstOrDefault(dt =>
            string.Equals(dt.TableName, tableName, StringComparison.OrdinalIgnoreCase));
    }

    public List<DataTable> GetAllDataTables()
    {
        return _taskDataTables.ToList();
    }
}
