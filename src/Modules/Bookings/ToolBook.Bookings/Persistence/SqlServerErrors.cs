using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ToolBook.Bookings.Persistence;

internal static class SqlServerErrors
{
    /// <summary>2627: PRIMARY KEY / UNIQUE constraint violation, 2601: duplicate key in a unique index.</summary>
    public static bool IsUniqueViolation(this DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: 2627 or 2601 };
}
