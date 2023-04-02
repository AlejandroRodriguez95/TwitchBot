using System;
using TwitchBotTest;
using TwitchBotTest.Classes;

namespace TestConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            Bot bot = null;

            DatabaseConnection databaseConnection = new DatabaseConnection();

            TimerVisuals test = new TimerVisuals(1000, 1000);


            string input = "";

            while(input != "0")
            {
                input = Console.ReadLine().ToString();
                switch (input)
                {
                    case "add":
                        Console.WriteLine("enter the username: ");
                        var newName = Console.ReadLine();
                        databaseConnection.Create(newName);
                        break;

                    case "start":
                        if (bot == null)
                            bot = new Bot();
                        break;

                    case "check gold":
                        Console.WriteLine("who you wish to check: ");
                        var nameToCheck = Console.ReadLine();
                        UInt64 gold = 0;

                        if(databaseConnection.ReturnUserGold(nameToCheck, out gold))
                        {
                            Console.WriteLine($"User {nameToCheck} has {gold}");
                        }
                        else
                        {
                            Console.WriteLine("User doesn't exist!");
                        }

                        break;

                    default:
                        break;
                }
            }
        }
    }
}

//{"access_token":"d44cbkrwklsiphscmc440xlg9fhyxd",
//"expires_in":14651,
//"refresh_token":"6wysmagnflgwlu9ke1azfg8zzxd9hdfcspytafkjgxg2k18sn5",
//"scope":["channel:read:redemptions",
//"chat:edit","chat:read"],
//"token_type":"bearer"}