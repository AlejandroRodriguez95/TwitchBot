using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwitchBotTest.Credentials;

namespace TwitchBotTest
{
    class DatabaseConnection
    {
        private string connectionString =
            $"server = {DatabaseCredentials.address};" +
            $" user id = {DatabaseCredentials.user};" +
            $" password = {DatabaseCredentials.password};" +
            $" database = {DatabaseCredentials.database}";

        public bool Create(string username)
        {
            MySqlConnection connection = new MySqlConnection(connectionString);
            connection.Open();

            // Check if the user already exists
            MySqlCommand checkCommand = new MySqlCommand("SELECT COUNT(*) FROM users WHERE username = @username", connection);
            checkCommand.Parameters.AddWithValue("@username", username);
            int count = Convert.ToInt32(checkCommand.ExecuteScalar());

            if (count > 0)
            {
                connection.Close();
                return false;
            }
            else
            {
                // User doesn't exist, create a new record
                MySqlCommand createCommand = new MySqlCommand("INSERT INTO users (username, gold) VALUES (@username, @gold)", connection);
                createCommand.Parameters.AddWithValue("@username", username);
                createCommand.Parameters.AddWithValue("@gold", InitialValues.UserInitialGold);
                createCommand.ExecuteNonQuery();
                connection.Close();
                return true;
            }
        }

        public List<User> ReadAll()
        {
            List<User> users = new List<User>();

            MySqlConnection connection = new MySqlConnection(connectionString);
            connection.Open();

            MySqlCommand command = new MySqlCommand("SELECT * FROM users", connection);
            MySqlDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                string username = reader.GetString(0);
                UInt64 gold = reader.GetUInt64(1);
                users.Add(new User(username, gold));
            }
            reader.Close();

            connection.Close();

            return users;
        }


        public bool ReturnUserGold(string userName, out UInt64 userGold) // true if user exists, false if not
        {

            MySqlConnection connection = new MySqlConnection(connectionString);
            connection.Open();

            MySqlCommand command = new MySqlCommand("SELECT * FROM users WHERE username = @username", connection);
            command.Parameters.AddWithValue("@username", userName);
            MySqlDataReader reader = command.ExecuteReader();
            reader.Read();

            if (reader.HasRows)
            {
                userGold = reader.GetUInt64(1);
                reader.Close();
                connection.Close();
                return true;
            }
            else
            {
                reader.Close();
                connection.Close();
                userGold = 0;
                return false;
            }

        }

        public bool CheckIfUserHasActiveBet(string username)
        {
            MySqlConnection connection = new MySqlConnection(connectionString);
            connection.Open();

            MySqlCommand command = new MySqlCommand("SELECT * FROM users WHERE username = @username", connection);
            command.Parameters.AddWithValue("@username", username);
            MySqlDataReader reader = command.ExecuteReader();
            reader.Read();

            if (reader.HasRows)
            {
                if (reader.GetUInt64(2) == 0)
                {
                    reader.Close();
                    connection.Close();
                    return true;
                }
                else
                {
                    reader.Close();
                    connection.Close();
                    return false;
                }
            }
            else
            {
                reader.Close();
                connection.Close();
                return false;
            }
        }

        public bool ReturnCurrentBet(string userName, out UInt64 currentBet, out int team) // true if user exists, false if not
        {

            MySqlConnection connection = new MySqlConnection(connectionString);
            connection.Open();

            MySqlCommand command = new MySqlCommand("SELECT * FROM users WHERE username = @username", connection);
            command.Parameters.AddWithValue("@username", userName);
            MySqlDataReader reader = command.ExecuteReader();
            reader.Read();

            if (reader.HasRows)
            {
                currentBet = reader.GetUInt64(2);
                team = reader.GetInt32(3);
                reader.Close();
                connection.Close();
                return true;
            }
            else
            {
                team = 0;
                reader.Close();
                connection.Close();
                currentBet = 0;
                return false;
            }

        }


        public void PlaceBet(string userName, UInt64 currentGold, UInt64 bet, E_Team team)
        {
            MySqlConnection connection = new MySqlConnection(connectionString);
            connection.Open();

            MySqlCommand command = new MySqlCommand(
                "UPDATE users SET gold = @newGold, currentBet = @bet, bettingOnTeam = @team WHERE username = @username",
                connection
            );
            command.Parameters.AddWithValue("@newGold", currentGold - bet);
            command.Parameters.AddWithValue("@bet", bet);
            command.Parameters.AddWithValue("@username", userName);
            command.Parameters.AddWithValue("@team", team);

            command.ExecuteNonQuery();

            connection.Close();
        }

        public void RefundOpenBets()
        {
            MySqlConnection connection = new MySqlConnection(connectionString);
            connection.Open();

            MySqlCommand command = new MySqlCommand("SELECT * FROM users WHERE currentBet > 0", connection);
            MySqlDataReader reader = command.ExecuteReader();
            List<User> users = new List<User>();
            while (reader.Read())
            {
                string username = reader.GetString(0);
                UInt64 gold = reader.GetUInt64(1);
                UInt64 currentBet = reader.GetUInt64(2);

                users.Add(new User(username, gold, currentBet));
            }
            reader.Close();

            foreach(User user in users)
            {
                MySqlCommand updateCommand = new MySqlCommand("UPDATE users SET gold = @newGold, currentBet = 0, bettingOnTeam = 0 WHERE username = @username", connection);
                UInt64 newGold = user.CurrentGold + user.CurrentBet;
                updateCommand.Parameters.AddWithValue("@newGold", newGold);
                updateCommand.Parameters.AddWithValue("@username", user.UserName);
                updateCommand.ExecuteNonQuery();
            }
            connection.Close();
        }


        public void UpdateGold(string username, UInt64 gold)
        {
            MySqlConnection connection = new MySqlConnection(connectionString);
            connection.Open();

            MySqlCommand command = new MySqlCommand($"UPDATE users SET gold = {gold} WHERE username = '{username}'", connection);
            command.ExecuteNonQuery();

            connection.Close();
        }

        public void Delete(string username)
        {
            MySqlConnection connection = new MySqlConnection(connectionString);
            connection.Open();

            MySqlCommand command = new MySqlCommand($"DELETE FROM users WHERE username = '{username}'", connection);
            command.ExecuteNonQuery();

            connection.Close();
        }
    }
}