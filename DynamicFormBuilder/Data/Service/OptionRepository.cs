using DynamicFormBuilder.Data.Helper;
using DynamicFormBuilder.Models;
using DynamicFormBuilder.Models.Pagination;
using Microsoft.Data.SqlClient;
using System.Data;

public class OptionRepository : IOptionRepository
{
    private readonly SqlConnection _connection;

    public OptionRepository(string connectionString)
    {        
        _connection = new SqlConnection(connectionString);
    }

    public async Task<List<OptionSetDto>> GetOptionSetsAsync()
    {
        var list = new List<OptionSetDto>();
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT OptionId, Name FROM Options ORDER BY OptionId";

        if (_connection.State != ConnectionState.Open)
            await ((SqlConnection)_connection).OpenAsync();

        using var rdr = await ((SqlCommand)cmd).ExecuteReaderAsync();
        while (await rdr.ReadAsync())
        {
            list.Add(new OptionSetDto
            {
                OptionId = rdr.GetInt32(0),
                Name = rdr.GetString(1)
            });
        }

        _connection.Close();
        return list;
    }

    public async Task<PagedResult<OptionSetDto>> GetPagedOptionSetsAsync(DataTableRequest req)
    {
        var result = new PagedResult<OptionSetDto>();

        if (_connection.State != ConnectionState.Open)
            await ((SqlConnection)_connection).OpenAsync();

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "sp_OptionSets_Paged";
        cmd.CommandType = CommandType.StoredProcedure;

        cmd.AddParameter("@Skip", req.Skip);
        cmd.AddParameter("@PageSize", req.PageSize);
        cmd.AddParameter("@Search", string.IsNullOrWhiteSpace(req.Search) ? DBNull.Value : req.Search);
        cmd.AddParameter("@SortColumn", req.SortColumn ?? "OptionId");
        cmd.AddParameter("@SortDirection", req.SortDirection ?? "DESC");

        using var rdr = await ((SqlCommand)cmd).ExecuteReaderAsync();

        if (await rdr.ReadAsync())
            result.FilteredCount = rdr.GetInt32(0);

        result.TotalCount = result.FilteredCount;

        if (await rdr.NextResultAsync())
        {
            while (await rdr.ReadAsync())
            {
                result.Data.Add(new OptionSetDto
                {
                    OptionId = rdr.GetInt32(0),
                    Name = rdr.GetString(1)
                });
            }
        }

        _connection.Close();
        return result;
    }

    public async Task<List<OptionDto>> GetOptionValuesAsync(int optionId)
    {
        var list = new List<OptionDto>();
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "sp_GetOptionValues";
        cmd.CommandType = CommandType.StoredProcedure;
        cmd.AddParameter("@OptionId", optionId);

        if (_connection.State != ConnectionState.Open)
            await ((SqlConnection)_connection).OpenAsync();

        using var rdr = await ((SqlCommand)cmd).ExecuteReaderAsync();
        while (await rdr.ReadAsync())
        {
            list.Add(new OptionDto
            {
                OptionValueId = rdr.GetInt32(0),
                OptionId = rdr.GetInt32(1),
                Value = rdr.GetString(2)
            });
        }

        _connection.Close();
        return list;
    }

    public async Task CreateOptionSetAsync(string name)
    {
        if (_connection.State != ConnectionState.Open)
            await ((SqlConnection)_connection).OpenAsync();

        using var tran = ((SqlConnection)_connection).BeginTransaction();
        try
        {
            using var checkCmd = _connection.CreateCommand();
            checkCmd.Transaction = tran;
            checkCmd.CommandText = "SELECT COUNT(*) FROM Options WHERE Name = @n";
            checkCmd.AddParameter("@n", name);

            int count = (int)await ((SqlCommand)checkCmd).ExecuteScalarAsync();
            if (count > 0)
                throw new Exception("Option Set name already exists.");

            using var cmd = _connection.CreateCommand();
            cmd.Transaction = tran;
            cmd.CommandText = "INSERT INTO Options (Name) VALUES (@n)";
            cmd.AddParameter("@n", name);

            await ((SqlCommand)cmd).ExecuteNonQueryAsync();
            tran.Commit();
        }
        catch
        {
            tran.Rollback();
            throw;
        }
        finally
        {
            _connection.Close();
        }
    }

    public async Task<OptionSetDto?> GetOptionSetAsync(int id)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT OptionId, Name FROM Options WHERE OptionId=@i";
        cmd.AddParameter("@i", id);

        if (_connection.State != ConnectionState.Open)
            await ((SqlConnection)_connection).OpenAsync();

        using var rdr = await ((SqlCommand)cmd).ExecuteReaderAsync();
        OptionSetDto? result = null;
        if (await rdr.ReadAsync())
            result = new OptionSetDto { OptionId = rdr.GetInt32(0), Name = rdr.GetString(1) };

        _connection.Close();
        return result;
    }

    public async Task<bool> UpdateOptionSetAsync(int id, string name)
    {
        if (_connection.State != ConnectionState.Open)
            await ((SqlConnection)_connection).OpenAsync();

        using var tran = ((SqlConnection)_connection).BeginTransaction();
        try
        {
            using var checkCmd = _connection.CreateCommand();
            checkCmd.Transaction = tran;
            checkCmd.CommandText = "SELECT COUNT(*) FROM Options WHERE Name = @n AND OptionId <> @i";
            checkCmd.AddParameter("@n", name);
            checkCmd.AddParameter("@i", id);

            int count = (int)await ((SqlCommand)checkCmd).ExecuteScalarAsync();
            if (count > 0)
                throw new Exception("Option Set name already exists.");

            using var cmd = _connection.CreateCommand();
            cmd.Transaction = tran;
            cmd.CommandText = "UPDATE Options SET Name=@n WHERE OptionId=@i";
            cmd.AddParameter("@n", name);
            cmd.AddParameter("@i", id);

            int rows = await ((SqlCommand)cmd).ExecuteNonQueryAsync();
            tran.Commit();

            return rows > 0;
        }
        catch
        {
            tran.Rollback();
            throw;
        }
        finally
        {
            _connection.Close();
        }
    }

    public async Task<bool> DeleteOptionSetAsync(int id)
    {
        if (_connection.State != ConnectionState.Open)
            await ((SqlConnection)_connection).OpenAsync();

        using var tran = ((SqlConnection)_connection).BeginTransaction();
        try
        {
            using var cmdChild = _connection.CreateCommand();
            cmdChild.Transaction = tran;
            cmdChild.CommandText = "DELETE OptionValues WHERE OptionId=@i";
            cmdChild.AddParameter("@i", id);
            await ((SqlCommand)cmdChild).ExecuteNonQueryAsync();

            using var cmd = _connection.CreateCommand();
            cmd.Transaction = tran;
            cmd.CommandText = "DELETE Options WHERE OptionId=@i";
            cmd.AddParameter("@i", id);

            int rows = await ((SqlCommand)cmd).ExecuteNonQueryAsync();
            tran.Commit();

            return rows > 0;
        }
        catch
        {
            tran.Rollback();
            throw;
        }
        finally
        {
            _connection.Close();
        }
    }

    public async Task<bool> AddOptionValueAsync(int setId, string value)
    {
        if (_connection.State != ConnectionState.Open)
            await ((SqlConnection)_connection).OpenAsync();

        using var tran = ((SqlConnection)_connection).BeginTransaction();
        try
        {
            using var checkCmd = _connection.CreateCommand();
            checkCmd.Transaction = tran;
            checkCmd.CommandText = "SELECT COUNT(*) FROM OptionValues WHERE OptionId = @o AND [Value] = @v";
            checkCmd.AddParameter("@o", setId);
            checkCmd.AddParameter("@v", value);

            int count = (int)await ((SqlCommand)checkCmd).ExecuteScalarAsync();
            if (count > 0)
                throw new Exception("This value already exists for the selected option set.");

            using var cmd = _connection.CreateCommand();
            cmd.Transaction = tran;
            cmd.CommandText = "INSERT INTO OptionValues (OptionId, [Value]) VALUES (@o, @v)";
            cmd.AddParameter("@o", setId);
            cmd.AddParameter("@v", value);

            int rows = await ((SqlCommand)cmd).ExecuteNonQueryAsync();
            tran.Commit();

            return rows > 0;
        }
        catch
        {
            tran.Rollback();
            throw;
        }
        finally
        {
            _connection.Close();
        }
    }

    public async Task<bool> UpdateOptionValueAsync(int id, string value, int setId)
    {
        if (_connection.State != ConnectionState.Open)
            await ((SqlConnection)_connection).OpenAsync();

        using var tran = ((SqlConnection)_connection).BeginTransaction();
        try
        {
            using var checkCmd = _connection.CreateCommand();
            checkCmd.Transaction = tran;
            checkCmd.CommandText = "SELECT COUNT(*) FROM OptionValues WHERE OptionId = @o AND [Value] = @v AND OptionValueId <> @i";
            checkCmd.AddParameter("@o", setId);
            checkCmd.AddParameter("@v", value);
            checkCmd.AddParameter("@i", id);

            int count = (int)await ((SqlCommand)checkCmd).ExecuteScalarAsync();
            if (count > 0)
                throw new Exception("This value already exists for the selected option set.");

            using var cmd = _connection.CreateCommand();
            cmd.Transaction = tran;
            cmd.CommandText = "UPDATE OptionValues SET [Value] = @v WHERE OptionValueId = @i";
            cmd.AddParameter("@v", value);
            cmd.AddParameter("@i", id);

            int rows = await ((SqlCommand)cmd).ExecuteNonQueryAsync();
            tran.Commit();

            return rows > 0;
        }
        catch
        {
            tran.Rollback();
            throw;
        }
        finally
        {
            _connection.Close();
        }
    }

    public async Task<bool> DeleteOptionValueAsync(int id)
    {
        if (_connection.State != ConnectionState.Open)
            await ((SqlConnection)_connection).OpenAsync();

        using var tran = ((SqlConnection)_connection).BeginTransaction();
        try
        {
            using var cmd = _connection.CreateCommand();
            cmd.Transaction = tran;
            cmd.CommandText = "DELETE OptionValues WHERE OptionValueId=@i";
            cmd.AddParameter("@i", id);

            int rows = await ((SqlCommand)cmd).ExecuteNonQueryAsync();
            tran.Commit();

            return rows > 0;
        }
        catch
        {
            tran.Rollback();
            throw;
        }
        finally
        {
            _connection.Close();
        }
    }
}
