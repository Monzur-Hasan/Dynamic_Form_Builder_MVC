using System.Data;

namespace DynamicFormBuilder.Data.Helper
{
    public static class DbCommandExtensions
    {
        public static void AddParameter(this IDbCommand cmd, string name, object value)
        {
            var param = cmd.CreateParameter();
            param.ParameterName = name;
            param.Value = value ?? DBNull.Value;
            cmd.Parameters.Add(param);
        }
    }
}
