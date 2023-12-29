using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenNos.Log.Shared
{
    public enum LogType : byte
    {
        Map = 0,
        Speaker = 1,
        Whisper = 2,
        BuddyTalk = 3,
        Group = 4,
        Family = 5,
        Trade = 10,
        BazaarSell = 11,
        PrivateShopSell = 12,
        FamilyStorage = 13,
        Drop = 14,
        ItemCreate = 20,
        UserCommand = 50,
        ModeratorCommand = 51,
        GMCommand = 52,
        MallItemBuy = 60,
        Packet = 70,
    }
}
