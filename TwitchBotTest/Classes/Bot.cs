using System;
using System.Collections.Generic;
using TwitchBotTest;    
using TwitchBotTest.Credentials;
using TwitchLib.Client;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Events;
using TwitchLib.Client.Extensions;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;
using System.Timers;
using Newtonsoft.Json.Linq;
using WebSocketSharp;
using Newtonsoft.Json;

class Bot
{
    TwitchClient client;
    DatabaseConnection dbConnection;
    Timer timer; // total time
    Timer timerCycle; // update every 1 second
    bool betsOpen;
    string channel;
    string timeLeftForBets;
    Dictionary<string, UInt64> activeBets;
    WebSocket ws;

    public Bot()
    {

        ConnectionCredentials credentials = new ConnectionCredentials(TwitchCredentials.UserName, TwitchCredentials.BotToken);

        // OBS websocket connection
        ConnectOBSWebSocket();
        CreateTextSourceInOBS(); // will not create if it already exists!

        // Database connection
        dbConnection = new DatabaseConnection();
        dbConnection.RefundOpenBets(); // if the system crashed or was closed during a bet, this will refund the money when the bot starts



        betsOpen = false;
        // config timer
        InitializeTimer();


        activeBets = new Dictionary<string, UInt64>();


        var clientOptions = new ClientOptions
        {
            MessagesAllowedInPeriod = 750,
            ThrottlingPeriod = TimeSpan.FromSeconds(30)
        };
        WebSocketClient customClient = new WebSocketClient(clientOptions);
        client = new TwitchClient(customClient);
        client.Initialize(credentials, "alejandrou95");


        client.OnLog += Client_OnLog;
        client.OnJoinedChannel += Client_OnJoinedChannel;
        client.OnMessageReceived += Client_OnMessageReceived;
        client.OnWhisperReceived += Client_OnWhisperReceived;
        client.OnNewSubscriber += Client_OnNewSubscriber;
        client.OnConnected += Client_OnConnected;
        client.OnChatCommandReceived += Client_OnCommand;
        client.Connect();
    }

    private void InitializeTimer()
    {
        timer = new Timer(double.Parse(TimerData.BetTimer));
        timer.AutoReset = false;
        timer.Elapsed += CloseBets;

        timerCycle = new Timer(1000);
        timerCycle.Elapsed += UpdateOBSText;
        timerCycle.AutoReset = true;
    }

    private void Client_OnCommand(object sender, OnChatCommandReceivedArgs e)
    {
        ProcessCommands(e);
    }

    private void Client_OnLog(object sender, OnLogArgs e)
    {
        Console.WriteLine($"{e.DateTime.ToString()}: {e.BotUsername} - {e.Data}");
    }

    private void Client_OnConnected(object sender, OnConnectedArgs e)
    {
        Console.WriteLine($"Connected to {e.AutoJoinChannel}");
    }

    private void Client_OnJoinedChannel(object sender, OnJoinedChannelArgs e)
    {
        Console.WriteLine("Hey guys! I am a bot connected via TwitchLib!");
        client.SendMessage(e.Channel, $"Hey guys! I am a bot connected via TwitchLib! My name is {e.BotUsername}");
        channel = e.Channel;
    }

    private void Client_OnMessageReceived(object sender, OnMessageReceivedArgs e)
    {
        if (e.ChatMessage.Message.Contains("badword"))
            client.TimeoutUser(e.ChatMessage.Channel, e.ChatMessage.Username, TimeSpan.FromMinutes(30), "Bad word! 30 minute timeout!");
    }

    private void Client_OnWhisperReceived(object sender, OnWhisperReceivedArgs e)
    {
        if (e.WhisperMessage.Username == "my_friend")
            client.SendWhisper(e.WhisperMessage.Username, "Hey! Whispers are so cool!!");
    }

    private void Client_OnNewSubscriber(object sender, OnNewSubscriberArgs e)
    {
        if (e.Subscriber.SubscriptionPlan == SubscriptionPlan.Prime)
            client.SendMessage(e.Channel, $"Welcome {e.Subscriber.DisplayName} to the substers! You just earned 500 points! So kind of you to use your Twitch Prime on this channel!");
        else
            client.SendMessage(e.Channel, $"Welcome {e.Subscriber.DisplayName} to the substers! You just earned 500 points!");
    }


    private void ProcessCommands(OnChatCommandReceivedArgs e)
    {
        string channel = e.Command.ChatMessage.Channel;
        string userName = e.Command.ChatMessage.Username;
        var argList = e.Command.ArgumentsAsList;

        switch (e.Command.CommandText)
        {
            case "hello":
                Hello(userName);
                break;

            case "register":
                Register(userName);
                break;

            case "zaga":
                PlaceBet(userName, argList, E_Team.Zaga);
                break;            
            
            case "random":
                PlaceBet(userName, argList, E_Team.Random);
                break;

            case "currentbet":
                CurrentBet(userName);
                break;

            case "openbets":
                OpenBets();
                break;

            default:
                client.SendMessage(channel, "Error, command not implemented");
                break;
        }
    }

    private void Hello(string userName)
    {
        client.SendMessage(channel, $"Hello {userName}!");
    }

    private void CurrentBet(string username)
    {
        UInt64 currentBet;
        int team;
        dbConnection.ReturnCurrentBet(username, out currentBet, out team);

        client.SendMessage(channel, $"Your current bet is: {currentBet} on team {(E_Team)team}");
    }


    private void Register(string userName)
    {
        if (dbConnection.Create(userName))
            client.SendMessage(
                channel,
                $"Successfully registered {userName}. " +
                $"Your new balance is {InitialValues.UserInitialGold}"
                );

        else
            client.SendMessage(channel, $"Error. User already registered.");
    }

    
    private void PlaceBet(string userName, List<string> args, E_Team team)
    {
        if(!betsOpen)
        {
            client.SendMessage(channel, $"Bets are closed!");
            return;
        }

        if (!dbConnection.CheckIfUserHasActiveBet(userName))
        {
            client.SendMessage(channel, $"@{userName} you already have an active bet!");
            return;
        }

        

        if (args.Count == 1)
        {
            UInt64 bet;
            if (UInt64.TryParse(args[0], out bet) && bet > 0)
            {
                UInt64 currentUserGold;
                if(dbConnection.ReturnUserGold(userName, out currentUserGold))
                {
                    if(currentUserGold >= bet) // successful bet
                    {
                        dbConnection.PlaceBet(userName, currentUserGold, bet, team);
                        activeBets.Add(userName, bet);
                        client.SendMessage(channel, $"@{userName} placed {bet} on {team}");
                    }
                    else
                    {
                        client.SendMessage(channel, $"@{userName} you dont have enough gold!");
                    }
                }
                else
                {
                    client.SendMessage(channel, $"@{userName} you are not registered! Use the command !register");
                }
            }
            else
                client.SendMessage(channel, $"Error, @{userName} bet must have the syntax: !{team} <amount higher than 0>");
        }
        else
        {
            client.SendMessage(channel, $"Error, @{userName} bet must have the syntax: !{team} <amount higher than 0>");
        }
    }

    private void OpenBets()
    {
        if (betsOpen)
        {
            client.SendMessage(channel, $"Bets are already open!");
            return;
        }
        timer.Start();
        activeBets.Clear(); // clear all active bets
        betsOpen = true;
        timeLeftForBets = ((int.Parse(TimerData.BetTimer) / 1000) - 1).ToString();
        timerCycle.Start();

        client.SendMessage(channel, $"Bets are open for {double.Parse(TimerData.BetTimer) / 1000} seconds!");
    }

    private void CloseBets(object sender, ElapsedEventArgs e)
    {
        if (!betsOpen)
            return;
        timer.Stop();
        timerCycle.Stop();
        UpdateTextSourceInOBS("Bets closed.");


        client.SendMessage(channel, $"Bets have been closed.");
        betsOpen = false;
    }

    #region OBS websocket

    private void ConnectOBSWebSocket()
    {
        ws = new WebSocket("ws://localhost:4455");

        ws.OnOpen += (sender, e) =>
        {
            AuthenticateToOBS();
        };

        ws.OnError += (sender, e) =>
        {
            Console.WriteLine(e.Message.ToString());
        };

        ws.OnMessage += (sender, e) =>
        {
            Console.WriteLine(e.Data.ToString());
        };

        ws.Connect();
    }

    private void AuthenticateToOBS()
    {
        var request = new
        {
            op = 1,
            d = new
            {
                rpcVersion = 1,
                authentication = "",
                eventSubscriptions = 33
            }
        };

        ws.OnClose += (sender, e) =>
        {
            // Handle the message
            Console.WriteLine(e.Reason);
        };

        ws.Send(JsonConvert.SerializeObject(request));
    }

    private void CreateTextSourceInOBS()
    {
        var request = new
        {
            op = 6,

            d = new
            {
                requestType = "CreateInput",
                requestId = "f819dcf0-89cc-11eb-8f0e-382c4ac93b9c",
                requestData = new 
                {
                    sceneName = "Programming",
                    inputName = "Bet timer",
                    inputKind = "text_gdiplus_v2",
                    inputSettings = new
                    {
                        text = "Bets closed",
                    },
                    sceneItemEnabled = true,
                }
                
            },
        };

        ws.Send(JsonConvert.SerializeObject(request));
    }

    private void UpdateTextSourceInOBS(string newTime)
    {
        var request = new
        {
            op = 6,

            d = new
            {
                requestType = "SetInputSettings",
                requestId = "f819dcf0-89cc-11eb-8f0e-382c4ac93b9c",
                requestData = new
                {
                    sceneName = "Programming",
                    inputName = "Bet timer",
                    inputSettings = new
                    {
                        text = newTime,
                    },
                }
            },
        };
        ws.Send(JsonConvert.SerializeObject(request));
    }

    private void UpdateOBSText(object sender, ElapsedEventArgs e)
    {
        if(!betsOpen)
        {
            UpdateTextSourceInOBS("Bets closed.");
            return;
        }

        UpdateTextSourceInOBS(timeLeftForBets);
        timeLeftForBets = (int.Parse(timeLeftForBets) - 1).ToString();

    }

    #endregion
}