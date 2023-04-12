# TwitchBot
Twtich chat bot similar to the one used in twitch.tv/saltyteemo


Bot features:

- Connecting to a twitch channel
- Database connection to store registered users, user balance and user bets
- OBS (software used for streaming) integration to display text dynamically
- Twitch chat commands processing (register, bet on team, display current balance, etc)


General idea:
    The streamer will be able to use a command to open bets for a set amount of seconds. During this time, registered twitch viewers can use !<team> <amount> to set 
    a bet on one of the 2 teams. If the user is not yet registered, he can use !register to receive an initial amount of coins (set by the streamer) that he can use 
    for betting.

    Once bets are closed, the bot will automatically sort all the bets by percentage, and once the match is over, coins will be distributed among the viewers who bet on
    the winning team. The streamer has to tell the bot who won.

    If the bot crashes, ends unexpectedly, or if there is any problem processing bets, the bot will refund the current match bets to the users.
    
    
    
The bot will be used in the future by a streamer that plays the game "Tibia"
