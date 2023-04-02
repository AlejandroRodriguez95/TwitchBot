using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwitchBotTest
{
    public class User
    {
        private string userName;
        private UInt64 currentGold;
        private UInt64 currentBet;
        
        public string UserName
        {
            get { return userName; }
            set { userName = value; }
        }

        public UInt64 CurrentGold
        {
            get { return currentGold; }
            set { currentGold = value; }
        }        
        public UInt64 CurrentBet
        {
            get { return currentBet; }
            set { currentBet = value; }
        }


        public User(string userName, UInt64 gold)
        {
            this.userName = userName;
            this.currentGold = gold;
        }        
        
        public User(string userName, UInt64 gold, UInt64 currentBet)
        {
            this.userName = userName;
            this.currentGold = gold;
            this.currentBet = currentBet;
        }

    }
}
