using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.IO;
using MySql.Data.MySqlClient;
using System.Web;
using System.Reflection.PortableExecutable;

class Program
{
    static Dictionary<string, string> sessions = new Dictionary<string, string>();
    static readonly string connectionString = "server=localhost;database=CafeManagement;user=root;password=Pritika@2005";

    static void Main(string[] args)
    {
        HttpListener listener = new HttpListener();
        listener.Prefixes.Add("http://localhost:5000/");

        try
        {
            listener.Start();
            Console.WriteLine("Cafe Management Server running on http://localhost:5000");
            while (true)
            {
                HttpListenerContext context = listener.GetContext();
                string requestUrl = context.Request.Url.AbsolutePath;
                string userEmail = GetUserEmailFromSession(context.Request);

                LogPageView(requestUrl, userEmail);

                try
                {
                    if (requestUrl == "/signup" && context.Request.HttpMethod == "POST")
                        SendResponse(context.Response, HandleSignup(context.Request));
                    else if (requestUrl == "/login" && context.Request.HttpMethod == "POST")
                        SendResponse(context.Response, HandleLogin(context));
                    else if (requestUrl == "/logout")
                        SendResponse(context.Response, HandleLogout(context));  // Call the HandleLogout method for logout route


                    else if (requestUrl == "/placeorder" && context.Request.HttpMethod == "POST")
                        SendResponse(context.Response, HandlePlaceOrder(context));
                    else if (requestUrl == "/submitfeedback" && context.Request.HttpMethod == "POST")
                        SendResponse(context.Response, HandleSubmitFeedback(context));
                    else if (requestUrl == "/makereservation" && context.Request.HttpMethod == "POST")
                        SendResponse(context.Response, HandleMakeReservation(context));
                    else if (requestUrl == "/updatemenu" && context.Request.HttpMethod == "POST")
                        SendResponse(context.Response, HandleUpdateMenu(context));
                    else if (requestUrl == "/updateinventory" && context.Request.HttpMethod == "POST")
                        SendResponse(context.Response, HandleUpdateInventory(context));
                    else if (requestUrl == "/signup")
                        SendResponse(context.Response, GetHtmlContent("signup.html"));
                    else if (requestUrl == "/login")
                        SendResponse(context.Response, GetHtmlContent("login.html"));
                    else if (requestUrl == "/welcome")
                        SendResponse(context.Response, GetHtmlContent("welcome.html"));
                    else if (requestUrl == "/dashboard")
                        SendResponse(context.Response, HandleDashboard(context.Request), "text/html");
                    else if (requestUrl == "/updatepassword" && context.Request.HttpMethod == "POST")
                        SendResponse(context.Response, HandleUpdatePassword(context));
                    else if (requestUrl == "/updatepassword")
                        SendResponse(context.Response, string.IsNullOrEmpty(userEmail) ? "<html><body><h1>Please log in.</h1></body></html>" : GetHtmlContent("updatepassword.html"));

                    else if (requestUrl == "/" || requestUrl == "/index")
                        SendResponse(context.Response, GetHtmlContent("index.html"));
                    else if (requestUrl == "/editorder" && context.Request.HttpMethod == "POST")
                        SendResponse(context.Response, HandleEditOrder(context));
                    else if (requestUrl == "/updateorder" && context.Request.HttpMethod == "POST")
                        SendResponse(context.Response, HandleUpdateOrderWithTransaction(context));
                    else if (requestUrl == "/deleteorder" && context.Request.HttpMethod == "POST")
                        SendResponse(context.Response, HandleDeleteOrder(context));
                    else if (requestUrl == "/editreservation" && context.Request.HttpMethod == "POST")
                        SendResponse(context.Response, HandleEditReservation(context));
                    else if (requestUrl == "/updatereservation" && context.Request.HttpMethod == "POST")
                        SendResponse(context.Response, HandleUpdateReservation(context));
                    else if (requestUrl == "/deletereservation" && context.Request.HttpMethod == "POST")
                        SendResponse(context.Response, HandleDeleteReservation(context));
                    else if (requestUrl == "/editmenu" && context.Request.HttpMethod == "POST")
                        SendResponse(context.Response, HandleEditMenu(context));
                    else if (requestUrl == "/deletemenu" && context.Request.HttpMethod == "POST")
                        SendResponse(context.Response, HandleDeleteMenu(context));
                    else if (requestUrl == "/editinventory" && context.Request.HttpMethod == "POST")
                        SendResponse(context.Response, HandleEditInventory(context));
                    else if (requestUrl == "/deleteinventory" && context.Request.HttpMethod == "POST")
                        SendResponse(context.Response, HandleDeleteInventory(context));
                    else if (requestUrl == "/editschedule" && context.Request.HttpMethod == "POST")
                        SendResponse(context.Response, HandleEditSchedule(context));
                    else if (requestUrl == "/updateschedule" && context.Request.HttpMethod == "POST")
                        SendResponse(context.Response, HandleUpdateSchedule(context));
                    else if (requestUrl == "/deleteschedule" && context.Request.HttpMethod == "POST")
                        SendResponse(context.Response, HandleDeleteSchedule(context));
                    else if (requestUrl == "/editfeedback" && context.Request.HttpMethod == "POST")
                        SendResponse(context.Response, HandleEditFeedback(context));
                    else if (requestUrl == "/updatefeedback" && context.Request.HttpMethod == "POST")
                        SendResponse(context.Response, HandleUpdateFeedback(context));
                    else if (requestUrl == "/deletefeedback" && context.Request.HttpMethod == "POST")
                        SendResponse(context.Response, HandleDeleteFeedback(context));
                    else
                        SendResponse(context.Response, "<html><body><h1>404 - Page Not Found</h1></body></html>");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Request error: " + ex.Message);
                    SendResponse(context.Response, "<html><body><h1>500 - Internal Server Error</h1><p>" + ex.Message + "</p></body></html>", "text/html");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Server startup error: " + ex.Message);
        }
    }

    static string GetUserEmailFromSession(HttpListenerRequest request)
    {
        string sessionId = request.Cookies["sessionId"]?.Value;
        return (!string.IsNullOrEmpty(sessionId) && sessions.TryGetValue(sessionId, out string userEmail)) ? userEmail : null;
    }

    static string HandleLogin(HttpListenerContext context)
    {
        string postData = ReadPostData(context.Request);
        var parsedData = HttpUtility.ParseQueryString(postData);
        string email = parsedData["email"];
        string password = parsedData["password"];

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            return "Error: Email and password are required.";

        using (var connection = new MySqlConnection(connectionString))
        {
            connection.Open();
            string query = "SELECT * FROM Users WHERE Email = @Email";
            using (var cmd = new MySqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@Email", email);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        string storedPasswordHash = reader["PasswordHash"].ToString();
                        if (VerifyPassword(password, storedPasswordHash))
                        {
                            string sessionId = Guid.NewGuid().ToString();
                            sessions[sessionId] = email;
                            context.Response.AppendCookie(new Cookie("sessionId", sessionId));

                            // Return HTML with success message and redirect
                            return @"<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Login Successful - Café Management System</title>
    <link href='https://fonts.googleapis.com/css2?family=Poppins:wght@300;400;600&display=swap' rel='stylesheet'>
    <style>
        :root {
            --primary-color: #6B4226;
            --secondary-color: #8B5D33;
            --accent-color: #F1C27D;
            --background-color: #F2E1C1;
            --text-color: #4B3C2E;
            --white: #ffffff;
        }
        body {
            font-family: 'Poppins', sans-serif;
            background: linear-gradient(to bottom, var(--primary-color) 0%, var(--background-color) 100%);
            color: var(--text-color);
            margin: 0;
            padding: 20px;
            text-align: center;
        }
        h1 {
            margin-top: 40px;
        }
    </style>
</head>
<body>
    <h1>Successfully login!</h1>
    <script>
        setTimeout(() => { window.location.href = '/welcome'; }, 2000);
    </script>
</body>
</html>";
                        }
                        return "Invalid credentials!";
                    }
                    return "User not found!";
                }
            }
        }
    }
    static string HandleLogout(HttpListenerContext context)
    {
        // Get the session ID from the cookie
        string sessionId = context.Request.Cookies["sessionId"]?.Value;

        // If sessionId exists, invalidate it
        if (!string.IsNullOrEmpty(sessionId))
        {
            sessions.Remove(sessionId); // Remove the session from the dictionary

            // Remove the session cookie by setting it with an expired date
            var expiredCookie = new Cookie("sessionId", "")
            {
                Expires = DateTime.Now.AddDays(-1), // Set the expiration date to the past
                Path = "/" // Set the path to match the cookie path
            };
            context.Response.Cookies.Add(expiredCookie); // Add the expired cookie to the response
        }

        // Manually set the location header for redirection
        context.Response.StatusCode = 302; // HTTP Status Code for redirection
        context.Response.Headers.Add("Location", "/login"); // Explicit redirection header

        // Ensure the response is finished and the redirection happens
        context.Response.Close(); // Explicitly close the response

        return null; // Return null as the redirection happens via HTTP headers
    }



    static string HandleDashboard(HttpListenerRequest request)
    {
        string userEmail = GetUserEmailFromSession(request);
        if (string.IsNullOrEmpty(userEmail))
        {
            return "<html><body><h1>Please log in to view the dashboard.</h1></body></html>";
        }

        try
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                string userQuery = "SELECT UserID, Role FROM Users WHERE Email = @Email";
                int userId = 0;
                string role = "";

                // Log the user query
                Console.WriteLine($"Executing query: {userQuery}");

                using (var cmd = new MySqlCommand(userQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@Email", userEmail);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            userId = Convert.ToInt32(reader["UserID"]);
                            role = reader["Role"].ToString();
                        }
                        else
                        {
                            Console.WriteLine("No user found with the given email.");
                            return "<html><body><h1>User not found. Please try again.</h1></body></html>";
                        }
                    }
                }

                var result = new StringBuilder();
                result.Append("<!DOCTYPE html><html lang=\"en\"><head><meta charset=\"UTF-8\">");
                result.Append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
                result.Append("<title>Café Dashboard</title>");
                result.Append("<link href=\"https://fonts.googleapis.com/css2?family=Poppins:wght@300;400;600&display=swap\" rel=\"stylesheet\">");
                result.Append("<style>");
                result.Append(":root {");
                result.Append("--primary-color: #6B4226; --secondary-color: #8B5D33; --accent-color: #F1C27D; --background-color: #F2E1C1; --text-color: #4B3C2E; --white: #ffffff;");
                result.Append("} ");
                result.Append("body { font-family: 'Poppins', sans-serif; background: linear-gradient(to bottom, var(--primary-color) 0%, var(--background-color) 100%); color: var(--text-color); margin: 0; padding: 20px; }");
                result.Append("h1, h2 { text-align: center; }");
                result.Append("table { width: 90%; margin: 20px auto; border-collapse: collapse; background: var(--white); }");
                result.Append("th, td { padding: 10px; border: 1px solid #ccc; text-align: center; }");
                result.Append("th { background-color: var(--secondary-color); color: var(--white); }");
                result.Append("button { margin: 5px; padding: 6px 10px; border: none; border-radius: 4px; cursor: pointer; }");
                result.Append(".edit-btn { background-color: #3498db; color: var(--white); }");
                result.Append(".delete-btn { background-color: #e74c3c; color: var(--white); }");
                result.Append("button:hover { opacity: 0.8; }");
                result.Append("a { display: block; text-align: center; margin-top: 20px; color: var(--primary-color); text-decoration: none; font-weight: bold; }");

                // Notifications Styles
                result.Append("ul { list-style-type: none; padding: 0; margin: 0; }");
                result.Append("li { background-color: var(--white); border: 1px solid #ccc; padding: 10px; margin-bottom: 10px; border-radius: 4px; font-size: 16px; color: var(--primary-color); }");
                result.Append(".no-notification { font-style: italic; color: #666; text-align: center; }");

                result.Append("</style></head><body>");
                result.Append("<h1>Café Dashboard</h1>");

                // Display Notifications
                result.Append("<h2>Your Notifications</h2><ul>");
                string notificationQuery = "SELECT Message FROM Notifications WHERE UserID = @UserID ORDER BY NotificationID DESC";
                using (var cmd = new MySqlCommand(notificationQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@UserID", userId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                result.Append($"<li>{reader["Message"]}</li>");
                            }
                        }
                        else
                        {
                            result.Append("<li class='no-notification'>No notifications</li>");
                        }
                    }
                }
                result.Append("</ul>");

                // Customer Dashboard
                if (role == "Customer")
                {
                    // Orders
                    result.Append("<h2>Your Orders</h2><table><tr><th>Order ID</th><th>Total</th><th>Date</th><th>Status</th><th>Actions</th></tr>");
                    string orderQuery = "SELECT OrderID, TotalAmount, OrderDate, Status FROM Orders WHERE UserID = @UserID";
                    using (var cmd = new MySqlCommand(orderQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("@UserID", userId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                result.Append($"<tr><td>{reader["OrderID"]}</td><td>{reader["TotalAmount"]}</td><td>{reader["OrderDate"]}</td><td>{reader["Status"]}</td>");
                                result.Append("<td>");
                                result.Append($"<form action='/editorder' method='post' style='display:inline;'><input type='hidden' name='orderId' value='{reader["OrderID"]}'>");
                                result.Append("<button type='submit' class='edit-btn'>Edit</button></form>");
                                result.Append($"<form action='/deleteorder' method='post' style='display:inline;'><input type='hidden' name='orderId' value='{reader["OrderID"]}'>");
                                result.Append("<button type='submit' class='delete-btn' onclick='return confirm(\"Are you sure?\");'>Delete</button></form>");
                                result.Append("</td></tr>");
                            }
                        }
                    }
                    result.Append("</table>");

                    // Reservations
                    result.Append("<h2>Your Reservations</h2><table><tr><th>Reservation ID</th><th>Reservation Date</th><th>Status</th><th>Actions</th></tr>");
                    string reservationQuery = "SELECT ReservationID, ReservationDate, Status FROM Reservations WHERE UserID = @UserID";
                    using (var cmd = new MySqlCommand(reservationQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("@UserID", userId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                result.Append($"<tr><td>{reader["ReservationID"]}</td><td>{reader["ReservationDate"]}</td><td>{reader["Status"]}</td>");
                                result.Append("<td>");
                                result.Append($"<form action='/editreservation' method='post' style='display:inline;'><input type='hidden' name='reservationId' value='{reader["ReservationID"]}'>");
                                result.Append("<button type='submit' class='edit-btn'>Edit</button></form>");
                                result.Append($"<form action='/deletereservation' method='post' style='display:inline;'><input type='hidden' name='reservationId' value='{reader["ReservationID"]}'>");
                                result.Append("<button type='submit' class='delete-btn' onclick='return confirm(\"Are you sure?\");'>Delete</button></form>");
                                result.Append("</td></tr>");
                            }
                        }
                    }
                    result.Append("</table>");
                    // Feedback
                    result.Append("<h2>Your Feedback</h2><table><tr><th>Feedback ID</th><th>Rating</th><th>Comment</th><th>Date</th><th>Actions</th></tr>");
                    string feedbackQuery = "SELECT FeedbackID, Rating, Comment, FeedbackDate FROM Feedback WHERE UserID = @UserID";
                    using (var cmd = new MySqlCommand(feedbackQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("@UserID", userId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                result.Append($"<tr><td>{reader["FeedbackID"]}</td><td>{reader["Rating"]}</td><td>{reader["Comment"]}</td><td>{reader["FeedbackDate"]}</td>");
                                result.Append("<td>");
                                result.Append($"<form action='/deletefeedback' method='post' style='display:inline;'><input type='hidden' name='feedbackId' value='{reader["FeedbackID"]}'>");
                                result.Append("<button type='submit' class='delete-btn' onclick='return confirm(\"Are you sure?\");'>Delete</button></form>");
                                result.Append("</td></tr>");
                            }
                        }
                    }
                }
                // Staff Dashboard
                else if (role == "Staff")
                {
                    // Menu Management
                    result.Append("<h2>Menu Management</h2><table><tr><th>Item ID</th><th>Name</th><th>Price</th><th>Actions</th></tr>");
                    string menuQuery = "SELECT ItemID, ItemName, Price FROM Menu WHERE IsAvailable = TRUE";
                    using (var cmd = new MySqlCommand(menuQuery, connection))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                result.Append($"<tr><td>{reader["ItemID"]}</td><td>{reader["ItemName"]}</td><td>{reader["Price"]}</td>");
                                result.Append("<td>");
                                result.Append($"<form action='/editmenu' method='post' style='display:inline;'><input type='hidden' name='itemId' value='{reader["ItemID"]}'>");
                                result.Append("<button type='submit' class='edit-btn'>Edit</button></form>");
                                result.Append($"<form action='/deletemenu' method='post' style='display:inline;'><input type='hidden' name='itemId' value='{reader["ItemID"]}'>");
                                result.Append("<button type='submit' class='delete-btn' onclick='return confirm(\"Are you sure?\");'>Delete</button></form>");
                                result.Append("</td></tr>");
                            }
                        }
                    }
                    result.Append("</table>");
                }
                // Manager Dashboard
                else if (role == "Manager")
                {
                    // Staff Schedule
                    result.Append("<h2>Staff Schedule</h2><table><tr><th>Schedule ID</th><th>Staff</th><th>Date</th><th>Shift</th><th>Actions</th></tr>");
                    string schedQuery = "SELECT S.ScheduleID, U.Name, S.ShiftDate, CONCAT(S.StartTime, ' - ', S.EndTime) AS Shift FROM StaffSchedule S JOIN Users U ON S.UserID = U.UserID";
                    using (var cmd = new MySqlCommand(schedQuery, connection))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                result.Append($"<tr><td>{reader["ScheduleID"]}</td><td>{reader["Name"]}</td><td>{reader["ShiftDate"]}</td><td>{reader["Shift"]}</td>");
                                result.Append("<td>");
                                result.Append($"<form action='/editschedule' method='post' style='display:inline;'><input type='hidden' name='scheduleId' value='{reader["ScheduleID"]}'>");
                                result.Append("<button type='submit' class='edit-btn'>Edit</button></form>");
                                result.Append($"<form action='/deleteschedule' method='post' style='display:inline;'><input type='hidden' name='scheduleId' value='{reader["ScheduleID"]}'>");
                                result.Append("<button type='submit' class='delete-btn' onclick='return confirm(\"Are you sure?\");'>Delete</button></form>");
                                result.Append("</td></tr>");
                            }
                        }
                    }
                    result.Append("</table>");

                    // Feedback Section
                    result.Append("<h2>Customer Feedback</h2><table><tr><th>Feedback ID</th><th>User</th><th>Rating</th><th>Comment</th><th>Date</th><th>Actions</th></tr>");
                    string feedbackQuery = "SELECT FeedbackID, U.Name, F.Rating, F.Comment, F.FeedbackDate FROM Feedback F JOIN Users U ON F.UserID = U.UserID ORDER BY F.FeedbackDate DESC";
                    using (var cmd = new MySqlCommand(feedbackQuery, connection))
                    {
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                result.Append($"<tr><td>{reader["FeedbackID"]}</td><td>{reader["Name"]}</td><td>{reader["Rating"]}</td><td>{reader["Comment"]}</td><td>{reader["FeedbackDate"]}</td>");
                                result.Append("<td>");
                                result.Append($"<form action='/deletefeedback' method='post' style='display:inline;'><input type='hidden' name='feedbackId' value='{reader["FeedbackID"]}'>");
                                result.Append("<button type='submit' class='delete-btn' onclick='return confirm(\"Are you sure?\");'>Delete</button></form>");
                                result.Append("</td></tr>");
                            }
                        }
                    }
                    result.Append("</table>");
                }

                result.Append("<a href='/welcome'><button>Back to Welcome</button></a>");
                result.Append("</body></html>");
                return result.ToString();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return "<html><body><h1>Unable to load dashboard. Please try again later.</h1></body></html>";
        }
    }

    // Method to handle feedback submission
    static string HandleSubmitFeedback(HttpListenerContext context)
    {
        string userEmail = GetUserEmailFromSession(context.Request);
        if (string.IsNullOrEmpty(userEmail))
            return "Error: Not logged in.";

        string postData = ReadPostData(context.Request);
        var parsedData = HttpUtility.ParseQueryString(postData);
        string feedback = parsedData["feedback"];
        string rating = parsedData["rating"];

        if (string.IsNullOrEmpty(feedback) || string.IsNullOrEmpty(rating))
            return "Error: Missing feedback or rating.";

        using (var connection = new MySqlConnection(connectionString))
        {
            connection.Open();

            // Get user ID
            string userQuery = "SELECT UserID FROM Users WHERE Email = @Email";
            int userId = 0;
            using (var cmd = new MySqlCommand(userQuery, connection))
            {
                cmd.Parameters.AddWithValue("@Email", userEmail);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                        userId = Convert.ToInt32(reader["UserID"]);
                }
            }

            // Insert feedback into Feedback table
            string feedbackQuery = "INSERT INTO Feedback (UserID, Rating, Comment, FeedbackDate) VALUES (@UserID, @Rating, @Comment, NOW())";
            using (var cmd = new MySqlCommand(feedbackQuery, connection))
            {
                cmd.Parameters.AddWithValue("@UserID", userId);
                cmd.Parameters.AddWithValue("@Rating", Convert.ToInt32(rating));
                cmd.Parameters.AddWithValue("@Comment", feedback);

                cmd.ExecuteNonQuery();
            }

            // Respond with success message and redirect to the welcome page
            return "<!DOCTYPE html>" +
                "<html lang='en'>" +
                "<head>" +
                "  <meta charset='UTF-8'>" +
                "  <meta name='viewport' content='width=device-width, initial-scale=1.0'>" +
                "  <title>Feedback Submitted - Café Management System</title>" +
                "  <link href='https://fonts.googleapis.com/css2?family=Poppins:wght@300;400;600&display=swap' rel='stylesheet'>" +
                "  <style>" +
                "    :root {" +
                "      --primary-color: #6B4226;" +    // Coffee Brown  
                "      --secondary-color: #8B5D33;" +  // Medium Coffee  
                "      --accent-color: #F1C27D;" +     // Creamy Beige  
                "      --background-color: #F2E1C1;" + // Light Cream  
                "      --text-color: #4B3C2E;" +       // Dark Chocolate  
                "      --white: #ffffff;" +            // White background  
                "    }" +
                "    body {" +
                "      font-family: 'Poppins', sans-serif;" +
                "      background: linear-gradient(to bottom, var(--primary-color) 0%, var(--background-color) 100%);" +
                "      color: var(--text-color);" +
                "      margin: 0;" +
                "      padding: 20px;" +
                "      text-align: center;" +
                "    }" +
                "    h1 {" +
                "      margin-top: 40px;" +
                "    }" +
                "    a {" +
                "      display: block;" +
                "      margin-top: 20px;" +
                "      color: var(--primary-color);" +
                "      text-decoration: none;" +
                "      font-weight: bold;" +
                "    }" +
                "  </style>" +
                "</head>" +
                "<body>" +
                "  <h1>Thank You for Your Feedback!</h1>" +
                "  <script>" +
                "    setTimeout(() => { window.location.href = '/welcome'; }, 2000);" +
                "  </script>" +
                "</body>" +
                "</html>";
        }
    }


    static string HandleEditOrder(HttpListenerContext context)
    {
        string userEmail = GetUserEmailFromSession(context.Request);
        if (string.IsNullOrEmpty(userEmail)) return "Error: Not logged in.";

        string postData = ReadPostData(context.Request);
        var parsedData = HttpUtility.ParseQueryString(postData);
        string orderId = parsedData["orderId"];

        string orderDate = "";
        int? currentItemId = null;
        int quantity = 1;
        int? orderDetailId = null; // To identify which OrderDetails row to edit
        string menuOptions = "";

        using (var connection = new MySqlConnection(connectionString))
        {
            connection.Open();

            // Fetch order date from Orders
            string orderQuery = "SELECT OrderDate FROM Orders WHERE OrderID = @OrderID";
            using (var cmd = new MySqlCommand(orderQuery, connection))
            {
                cmd.Parameters.AddWithValue("@OrderID", orderId);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        orderDate = ((DateTime)reader["OrderDate"]).ToString("yyyy-MM-ddTHH:mm");
                    }
                }
            }

            // Fetch the first OrderDetails row (single item assumption)
            string detailQuery = "SELECT OrderDetailID, ItemID, Quantity FROM OrderDetails WHERE OrderID = @OrderID LIMIT 1";
            using (var cmd = new MySqlCommand(detailQuery, connection))
            {
                cmd.Parameters.AddWithValue("@OrderID", orderId);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        orderDetailId = reader.GetInt32("OrderDetailID");
                        currentItemId = reader.GetInt32("ItemID");
                        quantity = reader.GetInt32("Quantity");
                    }
                }
            }

            // Get menu items for dropdown
            string menuQuery = "SELECT ItemID, ItemName FROM Menu WHERE IsAvailable = TRUE";
            using (var cmd = new MySqlCommand(menuQuery, connection))
            {
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int itemId = reader.GetInt32("ItemID");
                        string itemName = reader.GetString("ItemName");
                        menuOptions += $"<option value='{itemId}' {(currentItemId == itemId ? "selected" : "")}>{itemName}</option>";
                    }
                }
            }
        }

        return $@"<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Edit Order - Café Management System</title>
    <link href='https://fonts.googleapis.com/css2?family=Poppins:wght@300;400;600&display=swap' rel='stylesheet'>
    <style>
        :root {{ --primary-color: #6B4226; --secondary-color: #8B5D33; --accent-color: #F1C27D; --background-color: #F2E1C1; --text-color: #4B3C2E; --white: #ffffff; }}
        body {{ font-family: 'Poppins', sans-serif; background: linear-gradient(to bottom, var(--primary-color) 0%, var(--background-color) 100%); color: var(--text-color); margin: 0; padding: 20px; text-align: center; }}
        h1 {{ margin-top: 40px; color: var(--primary-color); font-size: 2.5rem; }}
        form {{ margin: 20px auto; max-width: 400px; }}
        label {{ display: block; margin: 10px 0; font-weight: 600; }}
        input[type='datetime-local'], input[type='number'], select {{ width: 100%; padding: 10px; margin: 5px 0; border: 1px solid var(--secondary-color); border-radius: 5px; box-sizing: border-box; }}
        input:focus, select:focus {{ outline: none; border-color: var(--primary-color); }}
        button {{ padding: 10px 20px; background-color: var(--secondary-color); color: var(--white); border: none; border-radius: 5px; cursor: pointer; font-weight: 600; }}
        button:hover {{ background-color: var(--primary-color); }}
        a {{ display: block; margin-top: 20px; color: var(--primary-color); text-decoration: none; font-weight: bold; }}
        a:hover {{ text-decoration: underline; }}
    </style>
</head>
<body>
    <h1>Edit Order {orderId}</h1>
    <form action='/updateorder' method='post'>
        <input type='hidden' name='orderId' value='{orderId}'>
        <input type='hidden' name='orderDetailId' value='{orderDetailId ?? 0}'>
        <label>Order Date and Time: 
            <input type='datetime-local' name='orderDate' value='{orderDate}' required>
        </label>
        <label>Item: 
            <select name='itemId' required>
                {menuOptions}
            </select>
        </label>
        <label>Quantity: 
            <input type='number' name='quantity' value='{quantity}' min='1' required>
        </label>
        <button type='submit'>Save</button>
    </form>
    <a href='/dashboard'>Back to Dashboard</a>
</body>
</html>";
    }

    static string HandleUpdateOrder(HttpListenerContext context)
    {
        string userEmail = GetUserEmailFromSession(context.Request);
        if (string.IsNullOrEmpty(userEmail)) return "Error: Not logged in.";

        string postData = ReadPostData(context.Request);
        var parsedData = HttpUtility.ParseQueryString(postData);
        string orderId = parsedData["orderId"];
        string orderDate = parsedData["orderDate"];
        string itemId = parsedData["itemId"];
        string quantity = parsedData["quantity"];

        if (string.IsNullOrEmpty(orderId) || string.IsNullOrEmpty(orderDate) ||
            string.IsNullOrEmpty(itemId) || string.IsNullOrEmpty(quantity))
            return "Error: All fields are required.";

        using (var connection = new MySqlConnection(connectionString))
        {
            connection.Open();

            // Call the UpdateOrderDetails procedure
            using (var cmd = new MySqlCommand("UpdateOrderDetails", connection))
            {
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@p_OrderID", Convert.ToInt32(orderId));
                cmd.Parameters.AddWithValue("@p_OrderDate", DateTime.Parse(orderDate));
                cmd.Parameters.AddWithValue("@p_ItemID", Convert.ToInt32(itemId));
                cmd.Parameters.AddWithValue("@p_Quantity", Convert.ToInt32(quantity));

                try
                {
                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    return $"Error updating order: {ex.Message}";
                }
            }

            return $@"<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Order Updated - Café Management System</title>
    <link href='https://fonts.googleapis.com/css2?family=Poppins:wght@300;400;600&display=swap' rel='stylesheet'>
    <style>
        :root {{ --primary-color: #6B4226; --secondary-color: #8B5D33; --accent-color: #F1C27D; --background-color: #F2E1C1; --text-color: #4B3C2E; --white: #ffffff; }}
        body {{ font-family: 'Poppins', sans-serif; background: linear-gradient(to bottom, var(--primary-color) 0%, var(--background-color) 100%); color: var(--text-color); margin: 0; padding: 20px; text-align: center; }}
        h1 {{ margin-top: 40px; color: var(--primary-color); font-size: 2.5rem; }}
        a {{ display: block; margin-top: 20px; color: var(--primary-color); text-decoration: none; font-weight: bold; }}
        a:hover {{ text-decoration: underline; }}
    </style>
</head>
<body>
    <h1>Order Updated!</h1>
    <script>setTimeout(() => {{ window.location.href = '/dashboard'; }}, 2000);</script>
</body>
</html>";
        }
    }

    static string HandleUpdateOrderWithTransaction(HttpListenerContext context)
    {
        string userEmail = GetUserEmailFromSession(context.Request);
        if (string.IsNullOrEmpty(userEmail)) return "Error: Not logged in.";

        string postData = ReadPostData(context.Request);
        var parsedData = HttpUtility.ParseQueryString(postData);
        string orderId = parsedData["orderId"];
        string orderDetailId = parsedData["orderDetailId"];
        string orderDate = parsedData["orderDate"];
        string itemId = parsedData["itemId"];
        string quantity = parsedData["quantity"];

        if (string.IsNullOrEmpty(orderId) || string.IsNullOrEmpty(orderDate) ||
            string.IsNullOrEmpty(itemId) || string.IsNullOrEmpty(quantity))
            return "Error: All fields are required.";

        using (var connection = new MySqlConnection(connectionString))
        {
            connection.Open();
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    // Update the order date
                    string orderQuery = "UPDATE Orders SET OrderDate = @OrderDate WHERE OrderID = @OrderID";
                    using (var cmd = new MySqlCommand(orderQuery, connection, transaction))
                    {
                        cmd.Parameters.AddWithValue("@OrderDate", DateTime.Parse(orderDate));
                        cmd.Parameters.AddWithValue("@OrderID", orderId);
                        cmd.ExecuteNonQuery();
                    }

                    // Log orderDetailId for debugging
                    Console.WriteLine("Updating OrderDetailID: " + orderDetailId);

                    // If editing an existing order detail
                    if (!string.IsNullOrEmpty(orderDetailId) && int.TryParse(orderDetailId, out int detailId) && detailId > 0)
                    {
                        string updateDetailQuery = @"
                        UPDATE OrderDetails 
                        SET ItemID = @ItemID, 
                            Quantity = @Quantity, 
                            UnitPrice = (SELECT Price FROM Menu WHERE ItemID = @ItemID)
                        WHERE OrderDetailID = @OrderDetailID";

                        using (var cmd = new MySqlCommand(updateDetailQuery, connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@OrderDetailID", detailId);
                            cmd.Parameters.AddWithValue("@ItemID", Convert.ToInt32(itemId));
                            cmd.Parameters.AddWithValue("@Quantity", Convert.ToInt32(quantity));
                            cmd.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        // Optional: If only one item allowed per order, delete existing details
                        string deleteOld = "DELETE FROM OrderDetails WHERE OrderID = @OrderID";
                        using (var cmd = new MySqlCommand(deleteOld, connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@OrderID", orderId);
                            cmd.ExecuteNonQuery();
                        }

                        string insertDetailQuery = @"
                        INSERT INTO OrderDetails (OrderID, ItemID, Quantity, UnitPrice)
                        VALUES (@OrderID, @ItemID, @Quantity, 
                            (SELECT Price FROM Menu WHERE ItemID = @ItemID))";

                        using (var cmd = new MySqlCommand(insertDetailQuery, connection, transaction))
                        {
                            cmd.Parameters.AddWithValue("@OrderID", orderId);
                            cmd.Parameters.AddWithValue("@ItemID", Convert.ToInt32(itemId));
                            cmd.Parameters.AddWithValue("@Quantity", Convert.ToInt32(quantity));
                            cmd.ExecuteNonQuery();
                        }
                    }

                    // Update TotalAmount based on OrderDetails
                    string totalQuery = @"
                    UPDATE Orders 
                    SET TotalAmount = (
                        SELECT SUM(Quantity * UnitPrice) 
                        FROM OrderDetails 
                        WHERE OrderID = @OrderID
                    )
                    WHERE OrderID = @OrderID";

                    using (var cmd = new MySqlCommand(totalQuery, connection, transaction))
                    {
                        cmd.Parameters.AddWithValue("@OrderID", orderId);
                        cmd.ExecuteNonQuery();
                    }

                    transaction.Commit();

                    return $@"<!DOCTYPE html>
<html lang='en'><head><meta charset='UTF-8'><meta name='viewport' content='width=device-width, initial-scale=1.0'>
<title>Order Updated - Café Management System</title>
<link href='https://fonts.googleapis.com/css2?family=Poppins:wght@300;400;600&display=swap' rel='stylesheet'>
<style>:root {{ --primary-color: #6B4226; --secondary-color: #8B5D33; --accent-color: #F1C27D; --background-color: #F2E1C1; --text-color: #4B3C2E; --white: #ffffff; }}
body {{ font-family: 'Poppins', sans-serif; background: linear-gradient(to bottom, var(--primary-color) 0%, var(--background-color) 100%); color: var(--text-color); margin: 0; padding: 20px; text-align: center; }}
h1 {{ margin-top: 40px; color: var(--primary-color); font-size: 2.5rem; }}
a {{ display: block; margin-top: 20px; color: var(--primary-color); text-decoration: none; font-weight: bold; }}
a:hover {{ text-decoration: underline; }}</style></head>
<body>
<h1>Order Updated!</h1>
<script>setTimeout(() => {{ window.location.href = '/dashboard'; }}, 2000);</script>
</body></html>";
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return $"Error updating order: {ex.Message}";
                }
            }
        }
    }


    static string HandleDeleteOrder(HttpListenerContext context)
    {
        string userEmail = GetUserEmailFromSession(context.Request);
        if (string.IsNullOrEmpty(userEmail)) return "Error: Not logged in.";

        string postData = ReadPostData(context.Request);
        var parsedData = HttpUtility.ParseQueryString(postData);
        string orderId = parsedData["orderId"];

        using (var connection = new MySqlConnection(connectionString))
        {
            connection.Open();
            string query = "DELETE FROM Orders WHERE OrderID = @OrderID";
            using (var cmd = new MySqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@OrderID", orderId);
                cmd.ExecuteNonQuery();
            }
        }

        return $@"<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Order Deleted - Café Management System</title>
    <link href='https://fonts.googleapis.com/css2?family=Poppins:wght@300;400;600&display=swap' rel='stylesheet'>
    <style>
        :root {{
            --primary-color: #6B4226;    /* Coffee Brown */
            --secondary-color: #8B5D33;  /* Medium Coffee */
            --accent-color: #F1C27D;     /* Creamy Beige */
            --background-color: #F2E1C1; /* Light Cream */
            --text-color: #4B3C2E;       /* Dark Chocolate */
            --white: #ffffff;            /* White background */
        }}
        body {{
            font-family: 'Poppins', sans-serif;
            background: linear-gradient(to bottom, var(--primary-color) 0%, var(--background-color) 100%);
            color: var(--text-color);
            margin: 0;
            padding: 20px;
            text-align: center;
        }}
        h1 {{
            margin-top: 40px;
            color: var(--primary-color);
            font-size: 2.5rem;
        }}
        a {{
            display: block;
            margin-top: 20px;
            color: var(--primary-color);
            text-decoration: none;
            font-weight: bold;
        }}
        a:hover {{
            text-decoration: underline;
        }}
    </style>
</head>
<body>
    <h1>Order Deleted!</h1>
    <script>setTimeout(() => {{ window.location.href = '/dashboard'; }}, 2000);</script>
</body>
</html>";
    }

    static string HandleEditReservation(HttpListenerContext context)
    {
        string userEmail = GetUserEmailFromSession(context.Request);
        if (string.IsNullOrEmpty(userEmail)) return "Error: Not logged in.";

        string postData = ReadPostData(context.Request);
        var parsedData = HttpUtility.ParseQueryString(postData);
        string reservationId = parsedData["reservationId"];

        return $@"<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Edit Reservation - Café Management System</title>
    <link href='https://fonts.googleapis.com/css2?family=Poppins:wght@300;400;600&display=swap' rel='stylesheet'>
    <style>
        :root {{
            --primary-color: #6B4226;    /* Coffee Brown */
            --secondary-color: #8B5D33;  /* Medium Coffee */
            --accent-color: #F1C27D;     /* Creamy Beige */
            --background-color: #F2E1C1; /* Light Cream */
            --text-color: #4B3C2E;       /* Dark Chocolate */
            --white: #ffffff;            /* White background */
        }}
        body {{
            font-family: 'Poppins', sans-serif;
            background: linear-gradient(to bottom, var(--primary-color) 0%, var(--background-color) 100%);
            color: var(--text-color);
            margin: 0;
            padding: 20px;
            text-align: center;
        }}
        h1 {{
            margin-top: 40px;
            color: var(--primary-color);
            font-size: 2.5rem;
        }}
        form {{
            margin: 20px auto;
            max-width: 400px;
        }}
        label {{
            display: block;
            margin: 10px 0;
            font-weight: 600;
        }}
        input, textarea {{
            width: 100%;
            padding: 10px;
            margin: 5px 0;
            border: 1px solid var(--secondary-color);
            border-radius: 5px;
            box-sizing: border-box;
        }}
        input:focus, textarea:focus {{
            outline: none;
            border-color: var(--primary-color);
        }}
        button {{
            padding: 10px 20px;
            background-color: var(--secondary-color);
            color: var(--white);
            border: none;
            border-radius: 5px;
            cursor: pointer;
            font-weight: 600;
        }}
        button:hover {{
            background-color: var(--primary-color);
        }}
        a {{
            display: block;
            margin-top: 20px;
            color: var(--primary-color);
            text-decoration: none;
            font-weight: bold;
        }}
        a:hover {{
            text-decoration: underline;
        }}
    </style>
</head>
<body>
    <h1>Edit Reservation {reservationId}</h1>
    <form action='/updatereservation' method='post'>
        <input type='hidden' name='reservationId' value='{reservationId}'>
        <label>Date: <input type='datetime-local' name='reservationDate' required></label>
        <label>Party Size: <input type='number' name='partySize' min='1' required></label>
        <button type='submit'>Save</button>
    </form>
    <a href='/dashboard'>Back to Dashboard</a>
</body>
</html>";
    }
    static string HandleUpdateReservation(HttpListenerContext context)
    {
        string userEmail = GetUserEmailFromSession(context.Request);
        if (string.IsNullOrEmpty(userEmail)) return "Error: Not logged in.";

        string postData = ReadPostData(context.Request);
        var parsedData = HttpUtility.ParseQueryString(postData);
        string reservationId = parsedData["reservationId"];
        string reservationDate = parsedData["reservationDate"];
        string partySize = parsedData["partySize"];

        using (var connection = new MySqlConnection(connectionString))
        {
            connection.Open();
            string query = "UPDATE Reservations SET ReservationDate = @ReservationDate, PartySize = @PartySize WHERE ReservationID = @ReservationID";
            using (var cmd = new MySqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@ReservationDate", reservationDate);
                cmd.Parameters.AddWithValue("@PartySize", partySize);
                cmd.Parameters.AddWithValue("@ReservationID", reservationId);
                cmd.ExecuteNonQuery();
            }
        }

        return $@"<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Reservation Updated - Café Management System</title>
    <link href='https://fonts.googleapis.com/css2?family=Poppins:wght@300;400;600&display=swap' rel='stylesheet'>
    <style>
        :root {{
            --primary-color: #6B4226;    /* Coffee Brown */
            --secondary-color: #8B5D33;  /* Medium Coffee */
            --accent-color: #F1C27D;     /* Creamy Beige */
            --background-color: #F2E1C1; /* Light Cream */
            --text-color: #4B3C2E;       /* Dark Chocolate */
            --white: #ffffff;            /* White background */
        }}
        body {{
            font-family: 'Poppins', sans-serif;
            background: linear-gradient(to bottom, var(--primary-color) 0%, var(--background-color) 100%);
            color: var(--text-color);
            margin: 0;
            padding: 20px;
            text-align: center;
        }}
        h1 {{
            margin-top: 40px;
            color: var(--primary-color);
            font-size: 2.5rem;
        }}
        a {{
            display: block;
            margin-top: 20px;
            color: var(--primary-color);
            text-decoration: none;
            font-weight: bold;
        }}
        a:hover {{
            text-decoration: underline;
        }}
    </style>
</head>
<body>
    <h1>Reservation Updated!</h1>
    <script>setTimeout(() => {{ window.location.href = '/dashboard'; }}, 2000);</script>
</body>
</html>";
    }

    static string HandleDeleteReservation(HttpListenerContext context)
    {
        string userEmail = GetUserEmailFromSession(context.Request);
        if (string.IsNullOrEmpty(userEmail)) return "Error: Not logged in.";

        string postData = ReadPostData(context.Request);
        var parsedData = HttpUtility.ParseQueryString(postData);
        string reservationId = parsedData["reservationId"];

        using (var connection = new MySqlConnection(connectionString))
        {
            connection.Open();
            string query = "DELETE FROM Reservations WHERE ReservationID = @ReservationID";
            using (var cmd = new MySqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@ReservationID", reservationId);
                cmd.ExecuteNonQuery();
            }
        }

        return $@"<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Reservation Deleted - Café Management System</title>
    <link href='https://fonts.googleapis.com/css2?family=Poppins:wght@300;400;600&display=swap' rel='stylesheet'>
    <style>
        :root {{
            --primary-color: #6B4226;    /* Coffee Brown */
            --secondary-color: #8B5D33;  /* Medium Coffee */
            --accent-color: #F1C27D;     /* Creamy Beige */
            --background-color: #F2E1C1; /* Light Cream */
            --text-color: #4B3C2E;       /* Dark Chocolate */
            --white: #ffffff;            /* White background */
        }}
        body {{
            font-family: 'Poppins', sans-serif;
            background: linear-gradient(to bottom, var(--primary-color) 0%, var(--background-color) 100%);
            color: var(--text-color);
            margin: 0;
            padding: 20px;
            text-align: center;
        }}
        h1 {{
            margin-top: 40px;
            color: var(--primary-color);
            font-size: 2.5rem;
        }}
        a {{
            display: block;
            margin-top: 20px;
            color: var(--primary-color);
            text-decoration: none;
            font-weight: bold;
        }}
        a:hover {{
            text-decoration: underline;
        }}
    </style>
</head>
<body>
    <h1>Reservation Deleted!</h1>
    <script>setTimeout(() => {{ window.location.href = '/dashboard'; }}, 2000);</script>
</body>
</html>";
    }
    static string HandleEditMenu(HttpListenerContext context)
    {
        string userEmail = GetUserEmailFromSession(context.Request);
        if (string.IsNullOrEmpty(userEmail)) return "Error: Not logged in.";

        string postData = ReadPostData(context.Request);
        var parsedData = HttpUtility.ParseQueryString(postData);
        string itemId = parsedData["itemId"];

        return $@"<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Edit Menu Item - Café Management System</title>
    <link href='https://fonts.googleapis.com/css2?family=Poppins:wght@300;400;600&display=swap' rel='stylesheet'>
    <style>
        :root {{
            --primary-color: #6B4226;    /* Coffee Brown */
            --secondary-color: #8B5D33;  /* Medium Coffee */
            --accent-color: #F1C27D;     /* Creamy Beige */
            --background-color: #F2E1C1; /* Light Cream */
            --text-color: #4B3C2E;       /* Dark Chocolate */
            --white: #ffffff;            /* White background */
        }}
        body {{
            font-family: 'Poppins', sans-serif;
            background: linear-gradient(to bottom, var(--primary-color) 0%, var(--background-color) 100%);
            color: var(--text-color);
            margin: 0;
            padding: 20px;
            text-align: center;
        }}
        h1 {{
            margin-top: 40px;
            color: var(--primary-color);
            font-size: 2.5rem;
        }}
        form {{
            margin: 20px auto;
            max-width: 400px;
        }}
        label {{
            display: block;
            margin: 10px 0;
            font-weight: 600;
        }}
        input, textarea {{
            width: 100%;
            padding: 10px;
            margin: 5px 0;
            border: 1px solid var(--secondary-color);
            border-radius: 5px;
            box-sizing: border-box;
        }}
        input:focus, textarea:focus {{
            outline: none;
            border-color: var(--primary-color);
        }}
        button {{
            padding: 10px 20px;
            background-color: var(--secondary-color);
            color: var(--white);
            border: none;
            border-radius: 5px;
            cursor: pointer;
            font-weight: 600;
        }}
        button:hover {{
            background-color: var(--primary-color);
        }}
        a {{
            display: block;
            margin-top: 20px;
            color: var(--primary-color);
            text-decoration: none;
            font-weight: bold;
        }}
        a:hover {{
            text-decoration: underline;
        }}
    </style>
</head>
<body>
    <h1>Edit Menu Item {itemId}</h1>
    <form action='/updatemenu' method='post'>
        <input type='hidden' name='itemId' value='{itemId}'>
        <label>Name: <input type='text' name='itemName' required></label>
        <label>Price: <input type='text' name='newPrice' required></label>
        <button type='submit'>Save</button>
    </form>
    <a href='/dashboard'>Back to Dashboard</a>
</body>
</html>";
    }
    static string HandleDeleteMenu(HttpListenerContext context)
    {
        string userEmail = GetUserEmailFromSession(context.Request);
        if (string.IsNullOrEmpty(userEmail)) return "Error: Not logged in.";

        string postData = ReadPostData(context.Request);
        var parsedData = HttpUtility.ParseQueryString(postData);
        string itemId = parsedData["itemId"];

        using (var connection = new MySqlConnection(connectionString))
        {
            connection.Open();
            string query = "DELETE FROM Menu WHERE ItemID = @ItemID";
            using (var cmd = new MySqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@ItemID", itemId);
                cmd.ExecuteNonQuery();
            }
        }

        return $@"<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Menu Item Deleted - Café Management System</title>
    <link href='https://fonts.googleapis.com/css2?family=Poppins:wght@300;400;600&display=swap' rel='stylesheet'>
    <style>
        :root {{
            --primary-color: #6B4226;    /* Coffee Brown */
            --secondary-color: #8B5D33;  /* Medium Coffee */
            --accent-color: #F1C27D;     /* Creamy Beige */
            --background-color: #F2E1C1; /* Light Cream */
            --text-color: #4B3C2E;       /* Dark Chocolate */
            --white: #ffffff;            /* White background */
        }}
        body {{
            font-family: 'Poppins', sans-serif;
            background: linear-gradient(to bottom, var(--primary-color) 0%, var(--background-color) 100%);
            color: var(--text-color);
            margin: 0;
            padding: 20px;
            text-align: center;
        }}
        h1 {{
            margin-top: 40px;
            color: var(--primary-color);
            font-size: 2.5rem;
        }}
        a {{
            display: block;
            margin-top: 20px;
            color: var(--primary-color);
            text-decoration: none;
            font-weight: bold;
        }}
        a:hover {{
            text-decoration: underline;
        }}
    </style>
</head>
<body>
    <h1>Menu Item Deleted!</h1>
    <script>setTimeout(() => {{ window.location.href = '/dashboard'; }}, 2000);</script>
</body>
</html>";
    }
    static string HandleEditInventory(HttpListenerContext context)
    {
        string userEmail = GetUserEmailFromSession(context.Request);
        if (string.IsNullOrEmpty(userEmail)) return "Error: Not logged in.";

        string postData = ReadPostData(context.Request);
        var parsedData = HttpUtility.ParseQueryString(postData);
        string inventoryId = parsedData["inventoryId"];

        return $@"<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Edit Inventory - Café Management System</title>
    <link href='https://fonts.googleapis.com/css2?family=Poppins:wght@300;400;600&display=swap' rel='stylesheet'>
    <style>
        :root {{
            --primary-color: #6B4226;    /* Coffee Brown */
            --secondary-color: #8B5D33;  /* Medium Coffee */
            --accent-color: #F1C27D;     /* Creamy Beige */
            --background-color: #F2E1C1; /* Light Cream */
            --text-color: #4B3C2E;       /* Dark Chocolate */
            --white: #ffffff;            /* White background */
        }}
        body {{
            font-family: 'Poppins', sans-serif;
            background: linear-gradient(to bottom, var(--primary-color) 0%, var(--background-color) 100%);
            color: var(--text-color);
            margin: 0;
            padding: 20px;
            text-align: center;
        }}
        h1 {{
            margin-top: 40px;
            color: var(--primary-color);
            font-size: 2.5rem;
        }}
        form {{
            margin: 20px auto;
            max-width: 400px;
        }}
        label {{
            display: block;
            margin: 10px 0;
            font-weight: 600;
        }}
        input, textarea {{
            width: 100%;
            padding: 10px;
            margin: 5px 0;
            border: 1px solid var(--secondary-color);
            border-radius: 5px;
            box-sizing: border-box;
        }}
        input:focus, textarea:focus {{
            outline: none;
            border-color: var(--primary-color);
        }}
        button {{
            padding: 10px 20px;
            background-color: var(--secondary-color);
            color: var(--white);
            border: none;
            border-radius: 5px;
            cursor: pointer;
            font-weight: 600;
        }}
        button:hover {{
            background-color: var(--primary-color);
        }}
        a {{
            display: block;
            margin-top: 20px;
            color: var(--primary-color);
            text-decoration: none;
            font-weight: bold;
        }}
        a:hover {{
            text-decoration: underline;
        }}
    </style>
</head>
<body>
    <h1>Edit Inventory {inventoryId}</h1>
    <form action='/updateinventory' method='post'>
        <input type='hidden' name='inventoryId' value='{inventoryId}'>
        <label>Name: <input type='text' name='itemName' required></label>
        <label>Stock: <input type='number' name='newQuantity' min='0' required></label>
        <button type='submit'>Save</button>
    </form>
    <a href='/dashboard'>Back to Dashboard</a>
</body>
</html>";
    }

    static string HandleDeleteInventory(HttpListenerContext context)
    {
        string userEmail = GetUserEmailFromSession(context.Request);
        if (string.IsNullOrEmpty(userEmail)) return "Error: Not logged in.";

        string postData = ReadPostData(context.Request);
        var parsedData = HttpUtility.ParseQueryString(postData);
        string inventoryId = parsedData["inventoryId"];

        using (var connection = new MySqlConnection(connectionString))
        {
            connection.Open();
            string query = "DELETE FROM Inventory WHERE InventoryID = @InventoryID";
            using (var cmd = new MySqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@InventoryID", inventoryId);
                cmd.ExecuteNonQuery();
            }
        }

        return $@"<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Inventory Deleted - Café Management System</title>
    <link href='https://fonts.googleapis.com/css2?family=Poppins:wght@300;400;600&display=swap' rel='stylesheet'>
    <style>
        :root {{
            --primary-color: #6B4226;    /* Coffee Brown */
            --secondary-color: #8B5D33;  /* Medium Coffee */
            --accent-color: #F1C27D;     /* Creamy Beige */
            --background-color: #F2E1C1; /* Light Cream */
            --text-color: #4B3C2E;       /* Dark Chocolate */
            --white: #ffffff;            /* White background */
        }}
        body {{
            font-family: 'Poppins', sans-serif;
            background: linear-gradient(to bottom, var(--primary-color) 0%, var(--background-color) 100%);
            color: var(--text-color);
            margin: 0;
            padding: 20px;
            text-align: center;
        }}
        h1 {{
            margin-top: 40px;
            color: var(--primary-color);
            font-size: 2.5rem;
        }}
        a {{
            display: block;
            margin-top: 20px;
            color: var(--primary-color);
            text-decoration: none;
            font-weight: bold;
        }}
        a:hover {{
            text-decoration: underline;
        }}
    </style>
</head>
<body>
    <h1>Inventory Deleted!</h1>
    <script>setTimeout(() => {{ window.location.href = '/dashboard'; }}, 2000);</script>
</body>
</html>";
    }
    static string HandleEditSchedule(HttpListenerContext context)
    {
        string userEmail = GetUserEmailFromSession(context.Request);
        if (string.IsNullOrEmpty(userEmail)) return "Error: Not logged in.";

        string postData = ReadPostData(context.Request);
        var parsedData = HttpUtility.ParseQueryString(postData);
        string scheduleId = parsedData["scheduleId"];

        return $@"<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Edit Schedule - Café Management System</title>
    <link href='https://fonts.googleapis.com/css2?family=Poppins:wght@300;400;600&display=swap' rel='stylesheet'>
    <style>
        :root {{
            --primary-color: #6B4226;    /* Coffee Brown */
            --secondary-color: #8B5D33;  /* Medium Coffee */
            --accent-color: #F1C27D;     /* Creamy Beige */
            --background-color: #F2E1C1; /* Light Cream */
            --text-color: #4B3C2E;       /* Dark Chocolate */
            --white: #ffffff;            /* White background */
        }}
        body {{
            font-family: 'Poppins', sans-serif;
            background: linear-gradient(to bottom, var(--primary-color) 0%, var(--background-color) 100%);
            color: var(--text-color);
            margin: 0;
            padding: 20px;
            text-align: center;
        }}
        h1 {{
            margin-top: 40px;
            color: var(--primary-color);
            font-size: 2.5rem;
        }}
        form {{
            margin: 20px auto;
            max-width: 400px;
        }}
        label {{
            display: block;
            margin: 10px 0;
            font-weight: 600;
        }}
        input, textarea {{
            width: 100%;
            padding: 10px;
            margin: 5px 0;
            border: 1px solid var(--secondary-color);
            border-radius: 5px;
            box-sizing: border-box;
        }}
        input:focus, textarea:focus {{
            outline: none;
            border-color: var(--primary-color);
        }}
        button {{
            padding: 10px 20px;
            background-color: var(--secondary-color);
            color: var(--white);
            border: none;
            border-radius: 5px;
            cursor: pointer;
            font-weight: 600;
        }}
        button:hover {{
            background-color: var(--primary-color);
        }}
        a {{
            display: block;
            margin-top: 20px;
            color: var(--primary-color);
            text-decoration: none;
            font-weight: bold;
        }}
        a:hover {{
            text-decoration: underline;
        }}
    </style>
</head>
<body>
    <h1>Edit Schedule {scheduleId}</h1>
    <form action='/updateschedule' method='post'>
        <input type='hidden' name='scheduleId' value='{scheduleId}'>
        <label>Date: <input type='date' name='shiftDate' required></label>
        <label>Start Time: <input type='time' name='startTime' required></label>
        <label>End Time: <input type='time' name='endTime' required></label>
        <button type='submit'>Save</button>
    </form>
    <a href='/dashboard'>Back to Dashboard</a>
</body>
</html>";
    }

    static string HandleUpdateSchedule(HttpListenerContext context)
    {
        string userEmail = GetUserEmailFromSession(context.Request);
        if (string.IsNullOrEmpty(userEmail)) return "Error: Not logged in.";

        string postData = ReadPostData(context.Request);
        var parsedData = HttpUtility.ParseQueryString(postData);
        string scheduleId = parsedData["scheduleId"];
        string shiftDate = parsedData["shiftDate"];
        string startTime = parsedData["startTime"];
        string endTime = parsedData["endTime"];

        using (var connection = new MySqlConnection(connectionString))
        {
            connection.Open();
            string query = "UPDATE StaffSchedule SET ShiftDate = @ShiftDate, StartTime = @StartTime, EndTime = @EndTime WHERE ScheduleID = @ScheduleID";
            using (var cmd = new MySqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@ShiftDate", shiftDate);
                cmd.Parameters.AddWithValue("@StartTime", startTime);
                cmd.Parameters.AddWithValue("@EndTime", endTime);
                cmd.Parameters.AddWithValue("@ScheduleID", scheduleId);
                cmd.ExecuteNonQuery();
            }
        }

        return $@"<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Schedule Updated - Café Management System</title>
    <link href='https://fonts.googleapis.com/css2?family=Poppins:wght@300;400;600&display=swap' rel='stylesheet'>
    <style>
        :root {{
            --primary-color: #6B4226;    /* Coffee Brown */
            --secondary-color: #8B5D33;  /* Medium Coffee */
            --accent-color: #F1C27D;     /* Creamy Beige */
            --background-color: #F2E1C1; /* Light Cream */
            --text-color: #4B3C2E;       /* Dark Chocolate */
            --white: #ffffff;            /* White background */
        }}
        body {{
            font-family: 'Poppins', sans-serif;
            background: linear-gradient(to bottom, var(--primary-color) 0%, var(--background-color) 100%);
            color: var(--text-color);
            margin: 0;
            padding: 20px;
            text-align: center;
        }}
        h1 {{
            margin-top: 40px;
            color: var(--primary-color);
            font-size: 2.5rem;
        }}
        a {{
            display: block;
            margin-top: 20px;
            color: var(--primary-color);
            text-decoration: none;
            font-weight: bold;
        }}
        a:hover {{
            text-decoration: underline;
        }}
    </style>
</head>
<body>
    <h1>Schedule Updated!</h1>
    <script>setTimeout(() => {{ window.location.href = '/dashboard'; }}, 2000);</script>
</body>
</html>";
    }

    static string HandleDeleteSchedule(HttpListenerContext context)
    {
        string userEmail = GetUserEmailFromSession(context.Request);
        if (string.IsNullOrEmpty(userEmail)) return "Error: Not logged in.";

        string postData = ReadPostData(context.Request);
        var parsedData = HttpUtility.ParseQueryString(postData);
        string scheduleId = parsedData["scheduleId"];

        using (var connection = new MySqlConnection(connectionString))
        {
            connection.Open();
            string query = "DELETE FROM StaffSchedule WHERE ScheduleID = @ScheduleID";
            using (var cmd = new MySqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@ScheduleID", scheduleId);
                cmd.ExecuteNonQuery();
            }
        }

        return $@"<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Schedule Deleted - Café Management System</title>
    <link href='https://fonts.googleapis.com/css2?family=Poppins:wght@300;400;600&display=swap' rel='stylesheet'>
    <style>
        :root {{
            --primary-color: #6B4226;    /* Coffee Brown */
            --secondary-color: #8B5D33;  /* Medium Coffee */
            --accent-color: #F1C27D;     /* Creamy Beige */
            --background-color: #F2E1C1; /* Light Cream */
            --text-color: #4B3C2E;       /* Dark Chocolate */
            --white: #ffffff;            /* White background */
        }}
        body {{
            font-family: 'Poppins', sans-serif;
            background: linear-gradient(to bottom, var(--primary-color) 0%, var(--background-color) 100%);
            color: var(--text-color);
            margin: 0;
            padding: 20px;
            text-align: center;
        }}
        h1 {{
            margin-top: 40px;
            color: var(--primary-color);
            font-size: 2.5rem;
        }}
        a {{
            display: block;
            margin-top: 20px;
            color: var(--primary-color);
            text-decoration: none;
            font-weight: bold;
        }}
        a:hover {{
            text-decoration: underline;
        }}
    </style>
</head>
<body>
    <h1>Schedule Deleted!</h1>
    <script>setTimeout(() => {{ window.location.href = '/dashboard'; }}, 2000);</script>
</body>
</html>";
    }

    static string HandleEditFeedback(HttpListenerContext context)
    {
        string userEmail = GetUserEmailFromSession(context.Request);
        if (string.IsNullOrEmpty(userEmail)) return "Error: Not logged in.";

        string postData = ReadPostData(context.Request);
        var parsedData = HttpUtility.ParseQueryString(postData);
        string feedbackId = parsedData["feedbackId"];

        return $@"<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Edit Feedback - Café Management System</title>
    <link href='https://fonts.googleapis.com/css2?family=Poppins:wght@300;400;600&display=swap' rel='stylesheet'>
    <style>
        :root {{
            --primary-color: #6B4226;    /* Coffee Brown */
            --secondary-color: #8B5D33;  /* Medium Coffee */
            --accent-color: #F1C27D;     /* Creamy Beige */
            --background-color: #F2E1C1; /* Light Cream */
            --text-color: #4B3C2E;       /* Dark Chocolate */
            --white: #ffffff;            /* White background */
        }}
        body {{
            font-family: 'Poppins', sans-serif;
            background: linear-gradient(to bottom, var(--primary-color) 0%, var(--background-color) 100%);
            color: var(--text-color);
            margin: 0;
            padding: 20px;
            text-align: center;
        }}
        h1 {{
            margin-top: 40px;
            color: var(--primary-color);
            font-size: 2.5rem;
        }}
        form {{
            margin: 20px auto;
            max-width: 400px;
        }}
        label {{
            display: block;
            margin: 10px 0;
            font-weight: 600;
        }}
        input, textarea {{
            width: 100%;
            padding: 10px;
            margin: 5px 0;
            border: 1px solid var(--secondary-color);
            border-radius: 5px;
            box-sizing: border-box;
        }}
        input:focus, textarea:focus {{
            outline: none;
            border-color: var(--primary-color);
        }}
        button {{
            padding: 10px 20px;
            background-color: var(--secondary-color);
            color: var(--white);
            border: none;
            border-radius: 5px;
            cursor: pointer;
            font-weight: 600;
        }}
        button:hover {{
            background-color: var(--primary-color);
        }}
        a {{
            display: block;
            margin-top: 20px;
            color: var(--primary-color);
            text-decoration: none;
            font-weight: bold;
        }}
        a:hover {{
            text-decoration: underline;
        }}
    </style>
</head>
<body>
    <h1>Edit Feedback {feedbackId}</h1>
    <form action='/updatefeedback' method='post'>
        <input type='hidden' name='feedbackId' value='{feedbackId}'>
        <label>Rating: <input type='number' name='rating' min='1' max='5' required></label>
        <label>Comment: <textarea name='comment' required></textarea></label>
        <button type='submit'>Save</button>
    </form>
    <a href='/dashboard'>Back to Dashboard</a>
</body>
</html>";
    }
    static string HandleUpdateFeedback(HttpListenerContext context)
    {
        string userEmail = GetUserEmailFromSession(context.Request);
        if (string.IsNullOrEmpty(userEmail)) return "Error: Not logged in.";

        string postData = ReadPostData(context.Request);
        var parsedData = HttpUtility.ParseQueryString(postData);
        string feedbackId = parsedData["feedbackId"];
        string rating = parsedData["rating"];
        string comment = parsedData["comment"];

        using (var connection = new MySqlConnection(connectionString))
        {
            connection.Open();
            string query = "UPDATE Feedback SET Rating = @Rating, Comment = @Comment WHERE FeedbackID = @FeedbackID";
            using (var cmd = new MySqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@Rating", rating);
                cmd.Parameters.AddWithValue("@Comment", comment);
                cmd.Parameters.AddWithValue("@FeedbackID", feedbackId);
                cmd.ExecuteNonQuery();
            }
        }

        return $@"<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Feedback Updated - Café Management System</title>
    <link href='https://fonts.googleapis.com/css2?family=Poppins:wght@300;400;600&display=swap' rel='stylesheet'>
    <style>
        :root {{
            --primary-color: #6B4226;    /* Coffee Brown */
            --secondary-color: #8B5D33;  /* Medium Coffee */
            --accent-color: #F1C27D;     /* Creamy Beige */
            --background-color: #F2E1C1; /* Light Cream */
            --text-color: #4B3C2E;       /* Dark Chocolate */
            --white: #ffffff;            /* White background */
        }}
        body {{
            font-family: 'Poppins', sans-serif;
            background: linear-gradient(to bottom, var(--primary-color) 0%, var(--background-color) 100%);
            color: var(--text-color);
            margin: 0;
            padding: 20px;
            text-align: center;
        }}
        h1 {{
            margin-top: 40px;
            color: var(--primary-color);
            font-size: 2.5rem;
        }}
        a {{
            display: block;
            margin-top: 20px;
            color: var(--primary-color);
            text-decoration: none;
            font-weight: bold;
        }}
        a:hover {{
            text-decoration: underline;
        }}
    </style>
</head>
<body>
    <h1>Feedback Updated!</h1>
    <script>setTimeout(() => {{ window.location.href = '/dashboard'; }}, 2000);</script>
</body>
</html>";
    }

    static string HandleDeleteFeedback(HttpListenerContext context)
    {
        string userEmail = GetUserEmailFromSession(context.Request);
        if (string.IsNullOrEmpty(userEmail)) return "Error: Not logged in.";

        string postData = ReadPostData(context.Request);
        var parsedData = HttpUtility.ParseQueryString(postData);
        string feedbackId = parsedData["feedbackId"];

        using (var connection = new MySqlConnection(connectionString))
        {
            connection.Open();
            string query = "DELETE FROM Feedback WHERE FeedbackID = @FeedbackID";
            using (var cmd = new MySqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@FeedbackID", feedbackId);
                cmd.ExecuteNonQuery();
            }
        }

        return $@"<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Feedback Deleted - Café Management System</title>
    <link href='https://fonts.googleapis.com/css2?family=Poppins:wght@300;400;600&display=swap' rel='stylesheet'>
    <style>
        :root {{
            --primary-color: #6B4226;    /* Coffee Brown */
            --secondary-color: #8B5D33;  /* Medium Coffee */
            --accent-color: #F1C27D;     /* Creamy Beige */
            --background-color: #F2E1C1; /* Light Cream */
            --text-color: #4B3C2E;       /* Dark Chocolate */
            --white: #ffffff;            /* White background */
        }}
        body {{
            font-family: 'Poppins', sans-serif;
            background: linear-gradient(to bottom, var(--primary-color) 0%, var(--background-color) 100%);
            color: var(--text-color);
            margin: 0;
            padding: 20px;
            text-align: center;
        }}
        h1 {{
            margin-top: 40px;
            color: var(--primary-color);
            font-size: 2.5rem;
        }}
        a {{
            display: block;
            margin-top: 20px;
            color: var(--primary-color);
            text-decoration: none;
            font-weight: bold;
        }}
        a:hover {{
            text-decoration: underline;
        }}
    </style>
</head>
<body>
    <h1>Feedback Deleted!</h1>
    <script>setTimeout(() => {{ window.location.href = '/dashboard'; }}, 2000);</script>
</body>
</html>";
    }

    static string HandlePlaceOrder(HttpListenerContext context)
    {
        string userEmail = GetUserEmailFromSession(context.Request);
        if (string.IsNullOrEmpty(userEmail))
            return "Error: Not logged in.";

        string postData = ReadPostData(context.Request);
        var parsedData = HttpUtility.ParseQueryString(postData);
        string itemId = parsedData["itemId"];
        string quantity = parsedData["quantity"];

        using (var connection = new MySqlConnection(connectionString))
        {
            connection.Open();
            string userQuery = "SELECT UserID FROM Users WHERE Email = @Email";
            int userId = 0;
            using (var cmd = new MySqlCommand(userQuery, connection))
            {
                cmd.Parameters.AddWithValue("@Email", userEmail);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read()) userId = Convert.ToInt32(reader["UserID"]);
                }
            }

            decimal unitPrice = 0;
            string priceQuery = "SELECT Price FROM Menu WHERE ItemID = @ItemID";
            using (var cmd = new MySqlCommand(priceQuery, connection))
            {
                cmd.Parameters.AddWithValue("@ItemID", itemId);
                unitPrice = Convert.ToDecimal(cmd.ExecuteScalar());
            }

            string orderQuery = "INSERT INTO Orders (UserID, TotalAmount, OrderDate) VALUES (@UserID, @TotalAmount, @OrderDate); SELECT LAST_INSERT_ID();";
            int orderId;
            using (var cmd = new MySqlCommand(orderQuery, connection))
            {
                cmd.Parameters.AddWithValue("@UserID", userId);
                cmd.Parameters.AddWithValue("@TotalAmount", unitPrice * Convert.ToInt32(quantity));
                cmd.Parameters.AddWithValue("@OrderDate", DateTime.Now);
                orderId = Convert.ToInt32(cmd.ExecuteScalar());
            }

            string detailQuery = "INSERT INTO OrderDetails (OrderID, ItemID, Quantity, UnitPrice) VALUES (@OrderID, @ItemID, @Quantity, @UnitPrice)";
            using (var cmd = new MySqlCommand(detailQuery, connection))
            {
                cmd.Parameters.AddWithValue("@OrderID", orderId);
                cmd.Parameters.AddWithValue("@ItemID", itemId);
                cmd.Parameters.AddWithValue("@Quantity", quantity);
                cmd.Parameters.AddWithValue("@UnitPrice", unitPrice);
                cmd.ExecuteNonQuery();
            }

            return "<!DOCTYPE html>" +
 "<html lang='en'>" +
 "<head>" +
 "  <meta charset='UTF-8'>" +
 "  <meta name='viewport' content='width=device-width, initial-scale=1.0'>" +
 "  <title>Order Success - Café Management System</title>" +
 "  <link href='https://fonts.googleapis.com/css2?family=Poppins:wght@300;400;600&display=swap' rel='stylesheet'>" +
 "  <style>" +
 "    :root {" +
 "      --primary-color: #6B4226;" +    // Coffee Brown  
 "      --secondary-color: #8B5D33;" +  // Medium Coffee  
 "      --accent-color: #F1C27D;" +     // Creamy Beige  
 "      --background-color: #F2E1C1;" + // Light Cream  
 "      --text-color: #4B3C2E;" +       // Dark Chocolate  
 "      --white: #ffffff;" +            // White background  
 "    }" +
 "    body {" +
 "      font-family: 'Poppins', sans-serif;" +
 "      background: linear-gradient(to bottom, var(--primary-color) 0%, var(--background-color) 100%);" +
 "      color: var(--text-color);" +
 "      margin: 0;" +
 "      padding: 20px;" +
 "      text-align: center;" +
 "    }" +
 "    h1 {" +
 "      margin-top: 40px;" +
 "    }" +
 "    a {" +
 "      display: block;" +
 "      margin-top: 20px;" +
 "      color: var(--primary-color);" +
 "      text-decoration: none;" +
 "      font-weight: bold;" +
 "    }" +
 "  </style>" +
 "</head>" +
 "<body>" +
 "  <h1>Order Placed Successfully!</h1>" +
 "  <script>" +
 "    setTimeout(() => { window.location.href = '/dashboard'; }, 2000);" +
 "  </script>" +
 "</body>" +
 "</html>";

        }
    }

    


    static string HandleMakeReservation(HttpListenerContext context)
    {
        string userEmail = GetUserEmailFromSession(context.Request);
        if (string.IsNullOrEmpty(userEmail))
            return "Error: Not logged in.";

        string postData = ReadPostData(context.Request);
        var parsedData = HttpUtility.ParseQueryString(postData);
        string reservationDate = parsedData["reservationDate"];
        string partySize = parsedData["partySize"];

        using (var connection = new MySqlConnection(connectionString))
        {
            connection.Open();
            string userQuery = "SELECT UserID FROM Users WHERE Email = @Email";
            int userId = 0;
            using (var cmd = new MySqlCommand(userQuery, connection))
            {
                cmd.Parameters.AddWithValue("@Email", userEmail);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read()) userId = Convert.ToInt32(reader["UserID"]);
                }
            }

            string insertQuery = "INSERT INTO Reservations (UserID, ReservationDate, PartySize) VALUES (@UserID, @ReservationDate, @PartySize)";
            using (var cmd = new MySqlCommand(insertQuery, connection))
            {
                cmd.Parameters.AddWithValue("@UserID", userId);
                cmd.Parameters.AddWithValue("@ReservationDate", reservationDate);
                cmd.Parameters.AddWithValue("@PartySize", partySize);
                cmd.ExecuteNonQuery();
            }
            return "<!DOCTYPE html>" +
"<html lang='en'>" +
"<head>" +
"  <meta charset='UTF-8'>" +
"  <meta name='viewport' content='width=device-width, initial-scale=1.0'>" +
"  <title>Order Success - Café Management System</title>" +
"  <link href='https://fonts.googleapis.com/css2?family=Poppins:wght@300;400;600&display=swap' rel='stylesheet'>" +
"  <style>" +
"    :root {" +
"      --primary-color: #6B4226;" +    // Coffee Brown  
"      --secondary-color: #8B5D33;" +  // Medium Coffee  
"      --accent-color: #F1C27D;" +     // Creamy Beige  
"      --background-color: #F2E1C1;" + // Light Cream  
"      --text-color: #4B3C2E;" +       // Dark Chocolate  
"      --white: #ffffff;" +            // White background  
"    }" +
"    body {" +
"      font-family: 'Poppins', sans-serif;" +
"      background: linear-gradient(to bottom, var(--primary-color) 0%, var(--background-color) 100%);" +
"      color: var(--text-color);" +
"      margin: 0;" +
"      padding: 20px;" +
"      text-align: center;" +
"    }" +
"    h1 {" +
"      margin-top: 40px;" +
"    }" +
"    a {" +
"      display: block;" +
"      margin-top: 20px;" +
"      color: var(--primary-color);" +
"      text-decoration: none;" +
"      font-weight: bold;" +
"    }" +
"  </style>" +
"</head>" +
"<body>" +
"  <h1>Reservation placed successfully!</h1>" +
"  <script>" +
"    setTimeout(() => { window.location.href = '/dashboard'; }, 2000);" +
"  </script>" +
"</body>" +
"</html>";

        }
    }

    static string HandleUpdateMenu(HttpListenerContext context)
    {
        string userEmail = GetUserEmailFromSession(context.Request);
        if (string.IsNullOrEmpty(userEmail))
            return "Error: Not logged in.";

        string postData = ReadPostData(context.Request);
        var parsedData = HttpUtility.ParseQueryString(postData);
        string itemId = parsedData["itemId"];
        string newPrice = parsedData["newPrice"];

        using (var connection = new MySqlConnection(connectionString))
        {
            connection.Open();
            string updateQuery = "UPDATE Menu SET Price = @Price WHERE ItemID = @ItemID";
            using (var cmd = new MySqlCommand(updateQuery, connection))
            {
                cmd.Parameters.AddWithValue("@Price", newPrice);
                cmd.Parameters.AddWithValue("@ItemID", itemId);
                cmd.ExecuteNonQuery();
            }
            return "<!DOCTYPE html>" +
"<html lang='en'>" +
"<head>" +
"  <meta charset='UTF-8'>" +
"  <meta name='viewport' content='width=device-width, initial-scale=1.0'>" +
"  <title>Order Success - Café Management System</title>" +
"  <link href='https://fonts.googleapis.com/css2?family=Poppins:wght@300;400;600&display=swap' rel='stylesheet'>" +
"  <style>" +
"    :root {" +
"      --primary-color: #6B4226;" +    // Coffee Brown  
"      --secondary-color: #8B5D33;" +  // Medium Coffee  
"      --accent-color: #F1C27D;" +     // Creamy Beige  
"      --background-color: #F2E1C1;" + // Light Cream  
"      --text-color: #4B3C2E;" +       // Dark Chocolate  
"      --white: #ffffff;" +            // White background  
"    }" +
"    body {" +
"      font-family: 'Poppins', sans-serif;" +
"      background: linear-gradient(to bottom, var(--primary-color) 0%, var(--background-color) 100%);" +
"      color: var(--text-color);" +
"      margin: 0;" +
"      padding: 20px;" +
"      text-align: center;" +
"    }" +
"    h1 {" +
"      margin-top: 40px;" +
"    }" +
"    a {" +
"      display: block;" +
"      margin-top: 20px;" +
"      color: var(--primary-color);" +
"      text-decoration: none;" +
"      font-weight: bold;" +
"    }" +
"  </style>" +
"</head>" +
"<body>" +
"  <h1>Menu Updated successfully!</h1>" +
"  <script>" +
"    setTimeout(() => { window.location.href = '/dashboard'; }, 2000);" +
"  </script>" +
"</body>" +
"</html>";

        
    
}
    }

    static string HandleUpdateInventory(HttpListenerContext context)
    {
        string userEmail = GetUserEmailFromSession(context.Request);
        if (string.IsNullOrEmpty(userEmail))
            return "Error: Not logged in.";

        string postData = ReadPostData(context.Request);
        var parsedData = HttpUtility.ParseQueryString(postData);
        string inventoryId = parsedData["inventoryId"];
        string newQuantity = parsedData["newQuantity"];

        using (var connection = new MySqlConnection(connectionString))
        {
            connection.Open();
            string updateQuery = "UPDATE Inventory SET QuantityInStock = @Quantity, LastRestockDate = @Date WHERE InventoryID = @InventoryID";
            using (var cmd = new MySqlCommand(updateQuery, connection))
            {
                cmd.Parameters.AddWithValue("@Quantity", newQuantity);
                cmd.Parameters.AddWithValue("@Date", DateTime.Now);
                cmd.Parameters.AddWithValue("@InventoryID", inventoryId);
                cmd.ExecuteNonQuery();
            }
            return "<!DOCTYPE html>" +
"<html lang='en'>" +
"<head>" +
"  <meta charset='UTF-8'>" +
"  <meta name='viewport' content='width=device-width, initial-scale=1.0'>" +
"  <title>Order Success - Café Management System</title>" +
"  <link href='https://fonts.googleapis.com/css2?family=Poppins:wght@300;400;600&display=swap' rel='stylesheet'>" +
"  <style>" +
"    :root {" +
"      --primary-color: #6B4226;" +    // Coffee Brown  
"      --secondary-color: #8B5D33;" +  // Medium Coffee  
"      --accent-color: #F1C27D;" +     // Creamy Beige  
"      --background-color: #F2E1C1;" + // Light Cream  
"      --text-color: #4B3C2E;" +       // Dark Chocolate  
"      --white: #ffffff;" +            // White background  
"    }" +
"    body {" +
"      font-family: 'Poppins', sans-serif;" +
"      background: linear-gradient(to bottom, var(--primary-color) 0%, var(--background-color) 100%);" +
"      color: var(--text-color);" +
"      margin: 0;" +
"      padding: 20px;" +
"      text-align: center;" +
"    }" +
"    h1 {" +
"      margin-top: 40px;" +
"    }" +
"    a {" +
"      display: block;" +
"      margin-top: 20px;" +
"      color: var(--primary-color);" +
"      text-decoration: none;" +
"      font-weight: bold;" +
"    }" +
"  </style>" +
"</head>" +
"<body>" +
"  <h1>Inventory Updated successfully!</h1>" +
"  <script>" +
"    setTimeout(() => { window.location.href = '/dashboard'; }, 2000);" +
"  </script>" +
"</body>" +
"</html>";

        }
    }

    static string HandleUpdatePassword(HttpListenerContext context)
{
    string userEmail = GetUserEmailFromSession(context.Request);
    if (string.IsNullOrEmpty(userEmail))
    {
        return "Error: Not logged in.";
    }

    string postData = ReadPostData(context.Request);
    if (string.IsNullOrEmpty(postData))
    {
        return "Error: No data provided.";
    }

    var parsedData = HttpUtility.ParseQueryString(postData);
    string currentPassword = parsedData["currentPassword"];
    string newPassword = parsedData["newPassword"];
    string confirmPassword = parsedData["confirmPassword"];

    if (string.IsNullOrEmpty(currentPassword) || string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmPassword))
    {
        return "Error: All fields are required.";
    }

    if (newPassword != confirmPassword)
    {
        return "Error: New password and confirm password do not match.";
    }

    using (var connection = new MySqlConnection("server=localhost;database=CafeManagement;user=root;password=Pritika@2005"))
    {
        connection.Open();
        string query = "SELECT PasswordHash FROM Users WHERE Email = @Email";
        using (var cmd = new MySqlCommand(query, connection))
        {
            cmd.Parameters.AddWithValue("@Email", userEmail);
            using (var reader = cmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    string storedPasswordHash = reader["PasswordHash"].ToString();
                    if (!VerifyPassword(currentPassword, storedPasswordHash))
                    {
                        return "Error: Current password is incorrect.";
                    }
                }
                else
                {
                    return "Error: User not found.";
                }
            }
        }

        string newPasswordHash = HashPassword(newPassword);
        string updateQuery = "UPDATE Users SET PasswordHash = @NewPasswordHash WHERE Email = @Email";
        using (var updateCmd = new MySqlCommand(updateQuery, connection))
        {
            updateCmd.Parameters.AddWithValue("@NewPasswordHash", newPasswordHash);
            updateCmd.Parameters.AddWithValue("@Email", userEmail);
            updateCmd.ExecuteNonQuery();
        }
    }

        return "<!DOCTYPE html>" +
            "<html lang='en'>" +
            "<head>" +
            "  <meta charset='UTF-8'>" +
            "  <meta name='viewport' content='width=device-width, initial-scale=1.0'>" +
            "  <title>Password Updated - Café Management System</title>" +
            "  <link href='https://fonts.googleapis.com/css2?family=Poppins:wght@300;400;600&display=swap' rel='stylesheet'>" +
            "  <style>" +
            "    :root {" +
            "      --primary-color: #6B4226;" +    // Coffee Brown
            "      --secondary-color: #8B5D33;" +  // Medium Coffee
            "      --accent-color: #F1C27D;" +     // Creamy Beige
            "      --background-color: #F2E1C1;" + // Light Cream
            "      --text-color: #4B3C2E;" +       // Dark Chocolate
            "      --white: #ffffff;" +            // White background
            "    }" +
            "    body {" +
            "      font-family: 'Poppins', sans-serif;" +
            "      background: linear-gradient(to bottom, var(--primary-color) 0%, var(--background-color) 100%);" +
            "      color: var(--text-color);" +
            "      margin: 0;" +
            "      padding: 20px;" +
            "      text-align: center;" +
            "    }" +
            "    h1 {" +
            "      margin-top: 40px;" +
            "    }" +
            "    a {" +
            "      display: block;" +
            "      margin-top: 20px;" +
            "      color: var(--primary-color);" +
            "      text-decoration: none;" +
            "      font-weight: bold;" +
            "    }" +
            "  </style>" +
            "</head>" +
            "<body>" +
            "  <h1>Password changed successfully!</h1>" +
            "  <script>" +
            "    setTimeout(() => { window.location.href = '/welcome'; }, 2000);" +
            "  </script>" +
            "</body>" +
            "</html>";
    }



    static string HandleSignup(HttpListenerRequest request)
    {
        string postData = ReadPostData(request);
        var parsedData = HttpUtility.ParseQueryString(postData);
        string name = parsedData["name"];
        string email = parsedData["email"];
        string password = parsedData["password"];
        string role = parsedData["role"];
        string phone = parsedData["phone"];

        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(role))
            return "Error: All fields are required!";

        string passwordHash = HashPassword(password);

        using (var connection = new MySqlConnection(connectionString))
        {
            connection.Open();
            string query = "INSERT INTO Users (Name, Email, PasswordHash, Role, Phone) VALUES (@Name, @Email, @PasswordHash, @Role, @Phone)";
            using (var cmd = new MySqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@Name", name);
                cmd.Parameters.AddWithValue("@Email", email);
                cmd.Parameters.AddWithValue("@PasswordHash", passwordHash);
                cmd.Parameters.AddWithValue("@Role", role);
                cmd.Parameters.AddWithValue("@Phone", phone);
                cmd.ExecuteNonQuery();
            }
            return "<!DOCTYPE html>" +
            "<html lang='en'>" +
            "<head>" +
            "  <meta charset='UTF-8'>" +
            "  <meta name='viewport' content='width=device-width, initial-scale=1.0'>" +
            "  <title>Password Updated - Café Management System</title>" +
            "  <link href='https://fonts.googleapis.com/css2?family=Poppins:wght@300;400;600&display=swap' rel='stylesheet'>" +
            "  <style>" +
            "    :root {" +
            "      --primary-color: #6B4226;" +    // Coffee Brown
            "      --secondary-color: #8B5D33;" +  // Medium Coffee
            "      --accent-color: #F1C27D;" +     // Creamy Beige
            "      --background-color: #F2E1C1;" + // Light Cream
            "      --text-color: #4B3C2E;" +       // Dark Chocolate
            "      --white: #ffffff;" +            // White background
            "    }" +
            "    body {" +
            "      font-family: 'Poppins', sans-serif;" +
            "      background: linear-gradient(to bottom, var(--primary-color) 0%, var(--background-color) 100%);" +
            "      color: var(--text-color);" +
            "      margin: 0;" +
            "      padding: 20px;" +
            "      text-align: center;" +
            "    }" +
            "    h1 {" +
            "      margin-top: 40px;" +
            "    }" +
            "    a {" +
            "      display: block;" +
            "      margin-top: 20px;" +
            "      color: var(--primary-color);" +
            "      text-decoration: none;" +
            "      font-weight: bold;" +
            "    }" +
            "  </style>" +
            "</head>" +
            "<body>" +
            "  <h1>SignUp successfully!</h1>" +
            "  <script>" +
            "    setTimeout(() => { window.location.href = '/login'; }, 2000);" +
            "  </script>" +
            "</body>" +
            "</html>";
        }
    }

        static string GetHtmlContent(string path)
    {
        try
        {
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "html", path);
            return File.ReadAllText(filePath);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error loading HTML: " + ex.Message);
            return "<html><body><h1>Error loading page</h1></body></html>";
        }
    }

    static void LogPageView(string pageUrl, string userEmail)
    {
        using (var connection = new MySqlConnection(connectionString))
        {
            connection.Open();
            string query = "INSERT INTO Views (PageURL, UserEmail) VALUES (@PageURL, @UserEmail)";
            using (var cmd = new MySqlCommand(query, connection))
            {
                cmd.Parameters.AddWithValue("@PageURL", pageUrl);
                cmd.Parameters.AddWithValue("@UserEmail", userEmail ?? (object)DBNull.Value);
                cmd.ExecuteNonQuery();
            }
        }
    }

    static string ReadPostData(HttpListenerRequest request)
    {
        using (StreamReader reader = new StreamReader(request.InputStream, request.ContentEncoding))
            return reader.ReadToEnd();
    }

    static string HashPassword(string password) => password; // Replace with proper hashing in production
    static bool VerifyPassword(string password, string storedHash) => password == storedHash;

    static void SendResponse(HttpListenerResponse response, string message, string contentType = "text/html")
    {
        byte[] buffer = Encoding.UTF8.GetBytes(message);
        response.ContentType = contentType;
        response.ContentLength64 = buffer.Length;
        response.OutputStream.Write(buffer, 0, buffer.Length);
        response.Close();
    }
}