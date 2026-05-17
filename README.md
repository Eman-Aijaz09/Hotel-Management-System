Hotel Management System
This is a Hotel Management System developed in C# with an Oracle Database (XE).
The system is designed to manage core hotel operations such as room bookings, inventory tracking, payroll, and more.

->Features:
Room booking management
User registration and login
Inventory and supplier tracking
Employee attendance and payroll
Customer feedback management
Task assignments

->Technologies Used
C#
ASP.NET
HTML, CSS, JavaScript
Oracle Database

-> Project Structure:
/bin                -> build output (not uploaded to GitHub)  
/obj                -> intermediate build files (not uploaded to GitHub)  
/data              -> oracleDbservice.cs file  
/Front-end          -> *.html files/images
/Properties         -> project configuration files  
appsettings.json     -> config including connection string  
*.sln                -> solution file  
*.cs                 -> main source code  

->How to Run:
-Clone this repository
-Open the solution
-Open OracleHMS.sln in Visual Studio.
-Set up the database
-Open SQL Developer.
-Connect to your Oracle XE DB.
-Run the SQL file provided:   hotel_management.sql
Update connection string; in appsettings.json, update:
"ConnectionStrings": {
  "OracleDb": "User Id=your_user;Password=your_password;Data Source=localhost:1521/xe"
}
-Build and run
-Build the solution and start the application.
