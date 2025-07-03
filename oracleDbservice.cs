using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace OracleHMS.Data
{

    public class InventoryItem
{
    public string ITEMID { get; set; }
    public string ITEMNAME { get; set; }
    public int ITEMQUANTITY { get; set; }
    public string STATUS { get; set; }
}
public class User
{
 public string UserId { get; set; }
    public string Username { get; set; }
    public string Role { get; set; }
    public string EMAIL { get; set; }
}
public class Booking
{
    public string Username { get; set; }
    public string CheckInDate { get; set; }
    public string CheckOutDate { get; set; }
    public string BookingDate { get; set; }
    public string RoomCategory { get; set; }
    public string GuestName { get; set; }
    public string RoomId { get; set; }
}
public class BookingWithRoom
{
    public string BookingId { get; set; }
    public string CheckInDate { get; set; }
    public string CheckOutDate { get; set; }
    public string BookingDate { get; set; }
    public string RoomCategory { get; set; }
    public string GuestName { get; set; }
    public string RoomId { get; set; }
    public string RoomNumber { get; set; }
    public string RoomPrice { get; set; }
    public string RoomStatus { get; set; }
    public string ActualCategory { get; set; }
}
public class Feedback
{
    public string FeedbackId { get; set; }
    public string FeedbackType { get; set; }
    public string Description { get; set; }
    public int Stars { get; set; }
    public string UserId { get; set; }
    public string Username { get; set; }
}

public class Attendance
{
    public string StaffId { get; set; }
    public DateTime AttDate { get; set; }
    public string Status { get; set; }
}

    public class OracleDbService
    {
        private readonly string? _connectionString;

        // Inject IConfiguration and retrieve connection string
        public OracleDbService(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("OracleDb");

            if (string.IsNullOrEmpty(_connectionString))
            {
                throw new InvalidOperationException("Oracle connection string is not configured correctly.");
            }
        }

        // NEW METHOD to fetch employees and return JSON-friendly format
// public async Task<List<Dictionary<string, object>>> GetEmployeesAsync()
//         {
//             var result = new List<Dictionary<string, object>>();
//             try
//             {
//                 using OracleConnection conn = new OracleConnection(_connectionString);
//                 await conn.OpenAsync();

//                 using OracleCommand cmd = new OracleCommand("SELECT * FROM system.users", conn);
//                 using OracleDataReader reader = await cmd.ExecuteReaderAsync();

//                 while (await reader.ReadAsync())
//                 {
//                     var row = new Dictionary<string, object>();
//                     for (int i = 0; i < reader.FieldCount; i++)
//                     {
//                         row[reader.GetName(i)] = reader.GetValue(i);
//                     }
//                     result.Add(row);
//                 }
//             }
//             catch (Exception ex)
//             {
//                 throw new ApplicationException("An error occurred while fetching data from Oracle DB.", ex);
//             }
            

//             return result;
//         }
public async Task<bool> RegisterUserAsync( string username,string email, string password,string role)
{
    try
    {
        using OracleConnection conn = new OracleConnection(_connectionString);
        await conn.OpenAsync();

                string checkUsernameQuery = "SELECT COUNT(*) FROM system.users WHERE USERNAME = :username";
        using OracleCommand checkCmd = new OracleCommand(checkUsernameQuery, conn);
        checkCmd.Parameters.Add(new OracleParameter("username", username));

        object checkResult = await checkCmd.ExecuteScalarAsync();
        int existingCount = Convert.ToInt32(checkResult);

        if (existingCount > 0)
        {
            throw new InvalidOperationException("Username already exists!");
        }

         string getMaxIdQuery = "SELECT NVL(MAX(TO_NUMBER(SUBSTR(USERID, 4))), 0) + 1 FROM system.users WHERE ROLE = :role";
                using OracleCommand getIdCmd = new OracleCommand(getMaxIdQuery, conn);
                getIdCmd.Parameters.Add(new OracleParameter("role", role));

                object result = await getIdCmd.ExecuteScalarAsync();
                int nextNumber = Convert.ToInt32(result);
                string prefix = role.ToUpper().Substring(0, 1) + "ID";
                string userId = prefix + nextNumber.ToString("D3");

        string insertQuery = @"INSERT INTO system.users ( userid, username, email, password,role)
                               VALUES (:userid, :username,:email, :password, :role)";

        using OracleCommand cmd = new OracleCommand(insertQuery, conn);
        cmd.Parameters.Add(new OracleParameter("userid", userId));
        cmd.Parameters.Add(new OracleParameter("username", username));
        cmd.Parameters.Add(new OracleParameter("email", email));
        cmd.Parameters.Add(new OracleParameter("password", password)); 
        cmd.Parameters.Add(new OracleParameter("role", role));

        int rowsAffected = await cmd.ExecuteNonQueryAsync();
        return rowsAffected > 0;
    }
    catch (InvalidOperationException) // for username already used
    {
        throw;
    }
    catch (Exception ex)
    {
        throw new ApplicationException("Error inserting user data into Oracle DB.", ex);
    }
}

public async Task<User?> GetUserAsync(string usernameOrEmail, string password, string role)
{
    try
    {
        using (OracleConnection connection = new OracleConnection(_connectionString))
        {
            await connection.OpenAsync();

            string query = @"
                SELECT USERID, USERNAME, ROLE FROM system.users 
                WHERE (USERNAME = :usernameOrEmail OR EMAIL = :usernameOrEmail) 
                          AND PASSWORD = :password AND ROLE = :role";

            using (OracleCommand command = new OracleCommand(query, connection))
            {
                command.Parameters.Add(new OracleParameter("usernameOrEmail", usernameOrEmail));
                command.Parameters.Add(new OracleParameter("usernameOrEmail", usernameOrEmail));
                command.Parameters.Add(new OracleParameter("password", password));
                command.Parameters.Add(new OracleParameter("role", role));

                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return new User
{
     UserId = reader["USERID"].ToString(),
    Username = reader["USERNAME"].ToString(),
    Role = reader["ROLE"].ToString()
};
                    }
                    else
                    {
                        return null;
                    }
                }
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine("Login error: " + ex.Message);
        throw;
    }
}
public async Task<string?> InsertBookingAsync(Booking booking)
{
    using var conn = new OracleConnection(_connectionString);
    await conn.OpenAsync();

    string generateIdQuery = "SELECT NVL(MAX(TO_NUMBER(SUBSTR(BOOKINGID, 3))), 0) + 1 FROM booking";
    using var generateIdCmd = new OracleCommand(generateIdQuery, conn);
    var result = await generateIdCmd.ExecuteScalarAsync();
    int nextId = Convert.ToInt32(result);
    string newBookingId = $"BK{nextId:D6}";  // e.g., BK000123

    string query = @"INSERT INTO booking 
                    (BOOKINGID, USERNAME, CHECKINDATE, CHECKOUTDATE, BOOKINGDATE, ROOMCATEGORY, GUESTNAME, ROOMID)
                     VALUES 
                    (:bookingId, :username, TO_DATE(:checkin, 'YYYY-MM-DD'), TO_DATE(:checkout, 'YYYY-MM-DD'),
                     TO_DATE(:bookingDate, 'YYYY-MM-DD'), :category, :guestName, :roomId)";

    using var cmd = new OracleCommand(query, conn);
    cmd.Parameters.Add(new("bookingId", newBookingId));
    cmd.Parameters.Add(new("username", booking.Username));
    cmd.Parameters.Add(new("checkin", booking.CheckInDate));
    cmd.Parameters.Add(new("checkout", booking.CheckOutDate));
    cmd.Parameters.Add(new("bookingDate", booking.BookingDate));
    cmd.Parameters.Add(new("category", booking.RoomCategory));
    cmd.Parameters.Add(new("guestName", booking.GuestName));
    cmd.Parameters.Add(new("roomId", booking.RoomId));

    int rowsAffected = await cmd.ExecuteNonQueryAsync();
    return rowsAffected > 0 ? newBookingId : null;
}





public async Task<bool> ValidateUserAsync(string usernameOrEmail, string password, string role)
{
    try
    {
        using (OracleConnection connection = new OracleConnection(_connectionString))
        {
            await connection.OpenAsync();

            string query = @"
                SELECT COUNT(*) FROM system.users 
                WHERE (LOWER(USERNAME) = :uname OR LOWER(EMAIL) = :email) 
                AND PASSWORD = :pwd AND LOWER(ROLE) = :role";

            using (OracleCommand command = new OracleCommand(query, connection))
            {
                command.Parameters.Add(new OracleParameter("uname", usernameOrEmail.ToLower()));
                command.Parameters.Add(new OracleParameter("email", usernameOrEmail.ToLower()));
                command.Parameters.Add(new OracleParameter("pwd", password));
                command.Parameters.Add(new OracleParameter("role", role.ToLower()));

                object result = await command.ExecuteScalarAsync();
                int count = Convert.ToInt32(result);
                Console.WriteLine($"Login count for {usernameOrEmail} as {role}: {count}");

                return count > 0;
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine("Login error: " + ex.Message);
        Console.WriteLine("StackTrace: " + ex.StackTrace);
        throw new Exception("Login error: Error validating login.", ex);
    }
}


public async Task<List<Dictionary<string, object>>> GetRoomsAsync()
{
    var result = new List<Dictionary<string, object>>();

    try
    {
        using OracleConnection conn = new OracleConnection(_connectionString);
        await conn.OpenAsync();

        string query = "SELECT * FROM system.room";
        using OracleCommand cmd = new OracleCommand(query, conn);
        using OracleDataReader reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var row = new Dictionary<string, object>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                row[reader.GetName(i)] = reader.GetValue(i);
            }
            result.Add(row);
        }
    }
    catch (Exception ex)
    {
        throw new ApplicationException("An error occurred while fetching room data.", ex);
    }

    return result;
}

public async Task<List<Dictionary<string, string>>> GetFeedbacksAsync()
{
    var feedbackList = new List<Dictionary<string, string>>();

    try
    {
        using OracleConnection conn = new OracleConnection(_connectionString);
        await conn.OpenAsync();

        string query = @"SELECT FEEDBACKTYPE, DESCRIPTION, STARS, USERID,USERNAME FROM system.feedback";
        using OracleCommand cmd = new OracleCommand(query, conn);
        using OracleDataReader reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
{
    var feedback = new Dictionary<string, string>();
    for (int i = 0; i < reader.FieldCount; i++)
    {
        feedback[reader.GetName(i)] = reader.IsDBNull(i) ? "" : reader.GetValue(i).ToString();
    }
    feedbackList.Add(feedback);
}

    }
    catch (Exception ex)
    {
        throw new ApplicationException("Error fetching feedback data.", ex);
    }

    return feedbackList;
}

public async Task<List<InventoryItem>> GetInventoryAsync()
{
    var inventory = new List<InventoryItem>();

    using (var connection = new OracleConnection(_connectionString))
    {
        await connection.OpenAsync();
        string query = "SELECT ITEMID, ITEMNAME, ITEMQUANTITY, STATUS FROM inventory";

        using (var command = new OracleCommand(query, connection))
        using (var reader = await command.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                inventory.Add(new InventoryItem
                {
                    ITEMID = reader.GetString(0),
                    ITEMNAME = reader.GetString(1),
                    ITEMQUANTITY = reader.GetInt32(2),
                    STATUS = reader.GetString(3)
                });
            }
        }
    }

    return inventory;
}

public async Task<List<Dictionary<string, string>>> GetSuppliersAsync()
{
    var suppliers = new List<Dictionary<string, string>>();

    using (var conn = new OracleConnection(_connectionString))
    {
        await conn.OpenAsync();
        string query = "SELECT SUPPLIERID, SUPPLIERNAME, CONTACTNO FROM system.SUPPLIER";

        using var cmd = new OracleCommand(query, conn);
        using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var supplier = new Dictionary<string, string>
            {
                ["SUPPLIERID"] = reader["SUPPLIERID"]?.ToString() ?? "",
                ["SUPPLIERNAME"] = reader["SUPPLIERNAME"]?.ToString() ?? "",
                ["CONTACTNO"] = reader["CONTACTNO"]?.ToString() ?? ""
            };

            suppliers.Add(supplier);
        }
    }

    return suppliers;
}

public async Task<List<Dictionary<string, string>>> GetTasksByStaffAsync(string staffid)
{
    var tasks = new List<Dictionary<string, string>>();

    using (var connection = new OracleConnection(_connectionString))
    {
        await connection.OpenAsync();

        string query = "SELECT TASKID, TASKDESCRIPTION, STATUS FROM TASK WHERE ASSIGNEDTOSTAFF = :staffid";
        using (var command = new OracleCommand(query, connection))
        {
            command.Parameters.Add(new OracleParameter("staffid", staffid));

            using (var reader = await command.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var task = new Dictionary<string, string>
                    {
                        ["TASKID"] = reader["TASKID"].ToString(),
                        ["TASKDESCRIPTION"] = reader["TASKDESCRIPTION"].ToString(),
                        ["STATUS"] = reader["STATUS"].ToString()
                    };

                    tasks.Add(task);
                }
            }
        }
    }

    return tasks;
}

public async Task<List<User>> GetAllStaffAsync()
{
    var staffList = new List<User>();

    using (var conn = new OracleConnection(_connectionString))
    {
        await conn.OpenAsync();
        using (var cmd = new OracleCommand("SELECT * FROM system.users WHERE ROLE = 'staff'", conn))
        {
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    staffList.Add(new User
                    {
                        UserId = reader["USERID"].ToString(),
                        Username = reader["USERNAME"].ToString(),
                        EMAIL = reader["EMAIL"].ToString(),
                        Role = reader["ROLE"].ToString()
                    });
                }
            }
        }
    }

    return staffList;
}

 public async Task<bool> DeleteUserAsync(string userId)
{
    using (var conn = new OracleConnection(_connectionString))
    {
        await conn.OpenAsync();
        using (var cmd = new OracleCommand("DELETE FROM USERS WHERE USERID = :id", conn))
        {
            cmd.Parameters.Add(new OracleParameter("id", userId));
            int rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }
    }
}

public async Task<List<User>> GetAllGuestsAsync()
{
    var guestList = new List<User>();

    using (var conn = new OracleConnection(_connectionString))
    {
        await conn.OpenAsync();
        using (var cmd = new OracleCommand("SELECT * FROM system.users WHERE ROLE = 'guest'", conn))
        {
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    guestList.Add(new User
                    {
                        UserId = reader["USERID"].ToString(),
                        Username = reader["USERNAME"].ToString(),
                        EMAIL = reader["EMAIL"].ToString(),
                        Role = reader["ROLE"].ToString()
                    });
                }
            }
        }
    }

    return guestList;
}

public async Task<List<Feedback>> GetAllFeedbackAsync()
{
    var feedbackList = new List<Feedback>();

    using (var conn = new OracleConnection(_connectionString))
    {
        await conn.OpenAsync();

        using (var cmd = new OracleCommand("SELECT * FROM system.feedback", conn))
        {
            using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    feedbackList.Add(new Feedback
                    {
                        FeedbackId = reader["FEEDBACKID"].ToString(),
                        FeedbackType = reader["FEEDBACKTYPE"].ToString(),
                        Description = reader["DESCRIPTION"].ToString(),
                        Stars = Convert.ToInt32(reader["STARS"]),
                        UserId = reader["USERID"].ToString(),
                        Username = reader["USERNAME"].ToString()
                    });
                }
            }
        }
    }

    return feedbackList;
}

public async Task<bool> DeleteFeedbackAsync(string feedbackId)
{
    using (var conn = new OracleConnection(_connectionString))
    {
        await conn.OpenAsync();

        using (var cmd = new OracleCommand("DELETE FROM system.feedback WHERE FEEDBACKID = :id", conn))
        {
            cmd.Parameters.Add(new OracleParameter("id", feedbackId));
            int rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }
    }
}

public async Task<List<BookingWithRoom>> GetGuestBookingHistory(string username)
{
    var result = new List<BookingWithRoom>();
    using var conn = new OracleConnection(_connectionString);
    await conn.OpenAsync();

    var cmd = new OracleCommand(@"
        SELECT 
            b.BOOKINGID, b.CHECKINDATE, b.CHECKOUTDATE, b.BOOKINGDATE, 
            b.ROOMCATEGORY AS BOOKEDCATEGORY, b.GUESTNAME, b.ROOMID,
            r.ROOMNUMBER, r.ROOMPRICE, r.ROOMSTATUS, r.ROOMCATEGORY AS ACTUALCATEGORY
        FROM BOOKING b
        JOIN ROOM r ON b.ROOMID = r.ROOMID
        WHERE b.USERNAME = :username", conn);

    cmd.Parameters.Add(new OracleParameter("username", username));

    var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        result.Add(new BookingWithRoom
        {
            BookingId = reader.GetString(0),
            CheckInDate = reader.GetDateTime(1).ToString("dd-MMM-yyyy"),
            CheckOutDate = reader.GetDateTime(2).ToString("dd-MMM-yyyy"),
            BookingDate = reader.GetDateTime(3).ToString("dd-MMM-yyyy"),
            RoomCategory = reader.GetString(4),
            GuestName = reader.GetString(5),
            RoomId = reader.GetString(6),
            RoomNumber = reader.GetString(7),
            RoomPrice = reader.GetString(8),
            RoomStatus = reader.GetString(9),
            ActualCategory = reader.GetString(10)
        });
    }

    return result;
}
 
public async Task<string?> InsertFeedbackAsync(Feedback feedback)
{
    using var conn = new OracleConnection(_connectionString);
    await conn.OpenAsync();

    // Generate FEEDBACKID
    string feedbackId = "FBID001";
    using (var getIdCmd = new OracleCommand("SELECT MAX(FEEDBACKID) FROM feedback", conn))
    {
        var result = await getIdCmd.ExecuteScalarAsync();
        if (result != DBNull.Value && result != null && result.ToString().StartsWith("FBID"))
        {
            int num = int.Parse(result.ToString().Substring(4));
            feedbackId = "FBID" + (num + 1).ToString("D3");
        }
    }

    // Insert feedback
    using var cmd = new OracleCommand(@"
        INSERT INTO feedback (FEEDBACKID, FEEDBACKTYPE, DESCRIPTION, STARS, USERID, USERNAME)
        VALUES (:feedbackId, :feedbackType, :description, :stars, :userId, :username)", conn);

    cmd.Parameters.Add(new OracleParameter("feedbackId", feedbackId));
    cmd.Parameters.Add(new OracleParameter("feedbackType", feedback.FeedbackType));
    cmd.Parameters.Add(new OracleParameter("description", feedback.Description));
    cmd.Parameters.Add(new OracleParameter("stars", feedback.Stars));
    cmd.Parameters.Add(new OracleParameter("userId", string.IsNullOrEmpty(feedback.UserId) ? DBNull.Value : (object)feedback.UserId));
    cmd.Parameters.Add(new OracleParameter("username", string.IsNullOrEmpty(feedback.Username) ? DBNull.Value : (object)feedback.Username));

    int rows = await cmd.ExecuteNonQueryAsync();
    return rows > 0 ? feedbackId : null;
}

public async Task<List<Dictionary<string, object>>> GetPayrollByStaffIdAsync(string staffId)
{
    var result = new List<Dictionary<string, object>>();
   
    using var connection = new OracleConnection(_connectionString);
    await connection.OpenAsync();

    string query = @"
  SELECT payroll_id, staff_id, staff_name, dp, pay_month, amount_p, p_status
  FROM payroll
  WHERE UPPER(staff_id) = UPPER(:staffId)";
    using var command = new OracleCommand(query, connection);
    command.Parameters.Add(new OracleParameter("staffId", staffId));

    using var reader = await command.ExecuteReaderAsync();
        if (!reader.HasRows)
    {
        Console.WriteLine($"No rows found for staffId: {staffId}");
    }
    while (await reader.ReadAsync())
    {
        var row = new Dictionary<string, object>
        {
            ["payrollid"] = reader["payroll_id"].ToString(),
["staffid"] = reader["staff_id"].ToString(),
["staffname"] = reader["staff_name"].ToString(),
["dp"] = Convert.ToInt32(reader["dp"]),
["month"] = reader["pay_month"].ToString(),
["amountp"] = Convert.ToDecimal(reader["amount_p"]),
["pstatus"] = reader["p_status"].ToString()
        };
        result.Add(row);
    }

    return result;
}

    }
}