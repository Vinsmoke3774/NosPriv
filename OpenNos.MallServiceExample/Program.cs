using System;
using System.Linq;
using System.Text;
using OpenNos.Master.Library.Client;
using System.Configuration;
using System.Security.Cryptography;
using OpenNos.Master.Library.Data;
using OpenNos.Data;
using OpenNos.Domain;
using System.Collections.Generic;

namespace OpenNos.MallServiceExample
{
    public class Program
    {
        protected static void Main(string[] args)
        {
            MallServiceClient.Instance.Authenticate(ConfigurationManager.AppSettings["MasterAuthKey"]);
            
            Console.WriteLine("UserName:");
            string user = "";
            int userArgIndex = Array.FindIndex(args, s => s == "--user");
            if (userArgIndex != -1
                && args.Length >= userArgIndex + 1
                && args[userArgIndex + 1] is string User)
            {
                user = User;
            }
            Console.WriteLine("Password:");
            string pass = "";
            int passArgIndex = Array.FindIndex(args, s => s == "--pass");
            if (passArgIndex != -1
                && args.Length >= passArgIndex + 1
                && args[passArgIndex + 1] is string Pass)
            {
                pass = Pass;
            }

            //pass = Sha512(pass);
            AccountDTO account = MallServiceClient.Instance.ValidateAccount(user, pass);
            if (account != null && account.Authority > AuthorityType.Unconfirmed)
            {
                IEnumerable<CharacterDTO> characters = MallServiceClient.Instance.GetCharacters(account.AccountId);

                foreach (CharacterDTO character in characters)
                {
                    Console.WriteLine($"ID: {character.CharacterId} Name: {character.Name} Level: {character.Level} Class: {character.Class}");
                }

                Console.WriteLine("CharacterID:");
                long charId = 0;
                int charIdArgIndex = Array.FindIndex(args, s => s == "--charid");
                if (charIdArgIndex != -1
                    && args.Length >= charIdArgIndex + 1
                    && long.Parse(args[charIdArgIndex + 1]) is long CharId)
                {
                    charId = CharId;
                }

                if (characters.Any(s => s.CharacterId == charId))
                {
                    Console.WriteLine("ItemVNum:");
                    short vnum = 0;
                    int vnumArgIndex = Array.FindIndex(args, s => s == "--vnum");
                    if (vnumArgIndex != -1
                        && args.Length >= vnumArgIndex + 1
                        && short.Parse(args[vnumArgIndex + 1]) is short Vnum)
                    {
                        vnum = Vnum;
                    }
                    Console.WriteLine("Amount:");
                    short amount = 0;
                    int amountArgIndex = Array.FindIndex(args, s => s == "--amount");
                    if (amountArgIndex != -1
                        && args.Length >= amountArgIndex + 1
                        && short.Parse(args[amountArgIndex + 1]) is short Amount)
                    {
                        amount = Amount;
                    }
                    Console.WriteLine("Rare:");
                    byte rare = 0;
                    int rareArgIndex = Array.FindIndex(args, s => s == "--rare");
                    if (rareArgIndex != -1
                        && args.Length >= rareArgIndex + 1
                        && byte.Parse(args[rareArgIndex + 1]) is byte Rare)
                    {
                        rare = Rare;
                    }
                    Console.WriteLine("Upgrade:");
                    byte upgrade = 0;
                    int upgradeArgIndex = Array.FindIndex(args, s => s == "--upgrade");
                    if (upgradeArgIndex != -1
                        && args.Length >= upgradeArgIndex + 1
                        && byte.Parse(args[upgradeArgIndex + 1]) is byte Upgrade)
                    {
                        upgrade = Upgrade;
                    }
                    Console.WriteLine("Upgrade:");
                    short design = 0;
                    int designArgIndex = Array.FindIndex(args, s => s == "--design");
                    if (designArgIndex != -1
                        && args.Length >= designArgIndex + 1
                        && byte.Parse(args[designArgIndex + 1]) is byte Design)
                    {
                        design = Design;
                    }
                    Console.WriteLine("Count:");
                    byte count = 0;
                    int countArgIndex = Array.FindIndex(args, s => s == "--count");
                    if (countArgIndex != -1
                        && args.Length >= countArgIndex + 1
                        && byte.Parse(args[countArgIndex + 1]) is byte Count)
                    {
                        count = Count;
                    }
                    while (count != 0)
                    {
                        MallServiceClient.Instance.SendItem(charId, new MallItem
                        {
                            ItemVNum = vnum,
                            Amount = amount,
                            Rare = rare,
                            Upgrade = upgrade,
                            Design = design
                        });
                        count--;
                    }
                }
            }
        }

        private static string Sha512(string inputString)
        {
            using (SHA512 hash = SHA512.Create())
            {
                return string.Concat(hash.ComputeHash(Encoding.UTF8.GetBytes(inputString)).Select(item => item.ToString("x2")));
            }
        }
    }
}
