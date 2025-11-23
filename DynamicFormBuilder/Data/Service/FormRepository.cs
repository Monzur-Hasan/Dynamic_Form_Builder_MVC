using DynamicFormBuilder.Data.Helper;
using DynamicFormBuilder.Models;
using DynamicFormBuilder.Models.Pagination;
using Microsoft.Data.SqlClient;
using System.Data;

namespace DynamicFormBuilder.Data.Service
{
    public class FormRepository : IFormRepository
    {
        private readonly string _connectionString;

        public FormRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        private SqlConnection GetConnection() => new SqlConnection(_connectionString);

        public async Task<bool> IsTitleExistsAsync(string title)
        {
            await using var conn = GetConnection();
            await using var cmd = new SqlCommand("sp_FormTitleExists", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.AddParameter("@Title", title);

            var outParam = new SqlParameter("@Exists", SqlDbType.Bit)
            {
                Direction = ParameterDirection.Output
            };
            cmd.Parameters.Add(outParam);

            await conn.OpenAsync();
            await cmd.ExecuteNonQueryAsync();

            return outParam.Value != DBNull.Value && (bool)outParam.Value;
        }

        public async Task<int> SaveFormAsync(string title, List<FieldDto> fields)
        {
            await using var conn = GetConnection();
            await conn.OpenAsync();

            await using var tran = await conn.BeginTransactionAsync();

            try
            {
                int formId;

                await using (var cmd = new SqlCommand("sp_SaveForm", conn, (SqlTransaction)tran)
                {
                    CommandType = CommandType.StoredProcedure
                })
                {
                    cmd.AddParameter("@Title", title);

                    var outParam = new SqlParameter("@NewFormId", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.Output
                    };
                    cmd.Parameters.Add(outParam);

                    await cmd.ExecuteNonQueryAsync();
                    formId = Convert.ToInt32(outParam.Value);
                }

                foreach (var f in fields)
                {
                    await using var cmd = new SqlCommand("sp_SaveFormField", conn, (SqlTransaction)tran)
                    {
                        CommandType = CommandType.StoredProcedure
                    };

                    cmd.AddParameter("@FormId", formId);
                    cmd.AddParameter("@Label", f.Label);
                    cmd.AddParameter("@IsRequired", f.IsRequired);
                    cmd.AddParameter("@OptionId", f.OptionId);
                    cmd.AddParameter("@SelectedOptionValueId", (object?)f.SelectedOptionValueId ?? DBNull.Value);

                    await cmd.ExecuteNonQueryAsync();
                }

                await tran.CommitAsync();
                return formId;
            }
            catch
            {
                await tran.RollbackAsync();
                throw;
            }
        }
        public async Task<(List<FormDto> Data, int RecordsTotal, int RecordsFiltered)> GetFormsPagedAsync(DataTableRequest req)
        {
            var result = new List<FormDto>();

            await using var conn = GetConnection();
            await conn.OpenAsync();

            await using var cmd = new SqlCommand("sp_GetFormsPaged", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.AddParameter("@Start", req.Skip);
            cmd.AddParameter("@Length", req.PageSize);
            cmd.AddParameter("@Search", string.IsNullOrWhiteSpace(req.Search) ? DBNull.Value : req.Search);

            var outTotal = new SqlParameter("@RecordsTotal", SqlDbType.Int)
            {
                Direction = ParameterDirection.Output
            };
            cmd.Parameters.Add(outTotal);

            var outFiltered = new SqlParameter("@RecordsFiltered", SqlDbType.Int)
            {
                Direction = ParameterDirection.Output
            };
            cmd.Parameters.Add(outFiltered);

            await using var rdr = await cmd.ExecuteReaderAsync();

            while (await rdr.ReadAsync())
            {
                result.Add(new FormDto
                {
                    FormId = rdr.GetInt32(0),
                    Title = rdr.GetString(1),
                    CreatedDate = rdr.IsDBNull(2) ? null : rdr.GetDateTime(2)
                });
            }

            await rdr.CloseAsync();

            int total = outTotal.Value == DBNull.Value ? 0 : Convert.ToInt32(outTotal.Value);
            int filtered = outFiltered.Value == DBNull.Value ? 0 : Convert.ToInt32(outFiltered.Value);

            return (result, total, filtered);
        }


        public async Task<FormDto?> GetFormWithFieldsAsync(int formId)
        {
            await using var conn = GetConnection();
            await conn.OpenAsync();

            await using var cmd = new SqlCommand("sp_GetFormWithFields", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.AddParameter("@FormId", formId);

            await using var rdr = await cmd.ExecuteReaderAsync();

            FormDto? form = null;

            if (await rdr.ReadAsync())
            {
                form = new FormDto
                {
                    FormId = rdr.GetInt32(0),
                    Title = rdr.GetString(1),
                    CreatedDate = rdr.IsDBNull(2) ? null : rdr.GetDateTime(2)
                };
            }

            if (form == null)
                return null;

            if (await rdr.NextResultAsync())
            {
                while (await rdr.ReadAsync())
                {
                    form.Fields.Add(new FieldDto
                    {
                        FieldId = rdr.GetInt32(0),
                        FormId = rdr.GetInt32(1),
                        Label = rdr.GetString(2),
                        IsRequired = rdr.GetBoolean(3),
                        OptionId = rdr.IsDBNull(4) ? 0 : rdr.GetInt32(4),
                        SelectedOptionValueId = rdr.IsDBNull(5) ? null : rdr.GetInt32(5),
                        SelectedOption = rdr.IsDBNull(6) ? null : rdr.GetString(6)
                    });
                }
            }

            return form;
        }

        public async Task<bool> UpdateFormAsync(FormDto form)
        {
            await using var conn = GetConnection();
            await conn.OpenAsync();

            await using var tran = await conn.BeginTransactionAsync();

            try
            {
                await using (var cmd = new SqlCommand(
                    "UPDATE Forms SET Title=@Title WHERE FormId=@FormId",
                    conn, (SqlTransaction)tran))
                {
                    cmd.AddParameter("@FormId", form.FormId);
                    cmd.AddParameter("@Title", form.Title);
                    await cmd.ExecuteNonQueryAsync();
                }

                await using (var cmd = new SqlCommand(
                    "DELETE FormFields WHERE FormId=@FormId",
                    conn, (SqlTransaction)tran))
                {
                    cmd.AddParameter("@FormId", form.FormId);
                    await cmd.ExecuteNonQueryAsync();
                }

                foreach (var f in form.Fields)
                {
                    await using var cmd = new SqlCommand("sp_SaveFormField", conn, (SqlTransaction)tran)
                    {
                        CommandType = CommandType.StoredProcedure
                    };

                    cmd.AddParameter("@FormId", form.FormId);
                    cmd.AddParameter("@Label", f.Label);
                    cmd.AddParameter("@IsRequired", f.IsRequired);
                    cmd.AddParameter("@OptionId", f.OptionId);
                    cmd.AddParameter("@SelectedOptionValueId", (object?)f.SelectedOptionValueId ?? DBNull.Value);

                    await cmd.ExecuteNonQueryAsync();
                }

                await tran.CommitAsync();
                return true;
            }
            catch
            {
                await tran.RollbackAsync();
                throw;
            }
        }
        public async Task<bool> DeleteFormAsync(int formId)
        {
            await using var conn = GetConnection();
            await conn.OpenAsync();

            await using var tran = await conn.BeginTransactionAsync();

            try
            {
                await using (var cmd = new SqlCommand(
                    "DELETE FormFields WHERE FormId=@FormId",
                    conn, (SqlTransaction)tran))
                {
                    cmd.AddParameter("@FormId", formId);
                    await cmd.ExecuteNonQueryAsync();
                }

                int affected;
                await using (var cmd = new SqlCommand(
                    "DELETE Forms WHERE FormId=@FormId",
                    conn, (SqlTransaction)tran))
                {
                    cmd.AddParameter("@FormId", formId);
                    affected = await cmd.ExecuteNonQueryAsync();
                }

                await tran.CommitAsync();
                return affected > 0;
            }
            catch
            {
                await tran.RollbackAsync();
                throw;
            }
        }
    }
}
