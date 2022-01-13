using Nethereum.Hex.HexConvertors.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreateWallet.Model
{
    class Wallet
    {
        public string AccountAdress { get; set; }
        public string PrivateKey { get; set; }

        public static List<Wallet> CreateWallet(int numberOfAccount)
        {
            var wallets = new List<Wallet>();
            for (int i = 0; i < numberOfAccount; i++)
            {
                var ecKey = Nethereum.Signer.EthECKey.GenerateKey();
                var privateKey = ecKey.GetPrivateKeyAsBytes().ToHex();
                var account = new Nethereum.Web3.Accounts.Account(privateKey);
                var adresse = account.Address;

                var newWallet = new Wallet
                {
                    PrivateKey = privateKey,
                    AccountAdress = adresse
                };

                wallets.Add(newWallet);

            }
            Console.WriteLine(wallets.Count + " WALLETS CREATED SUCCESSFULLY");
            return wallets;
        }

        public static async Task AddAllWalletsToJsonAsync(List<Wallet> newWallets)
        {
            //var pathFile = Path.Combine(@"C:\Users\Sco\source\repos\SunFlower\SunFlower\config\", "Wallets.json");
            var pathFile = @"..\..\..\config\Wallets.json";
            var json = await File.ReadAllTextAsync(pathFile);
            var actualWallets = JsonConvert.DeserializeObject<List<Wallet>>(json);
            actualWallets.AddRange(newWallets);
            File.WriteAllText(pathFile, JsonConvert.SerializeObject(actualWallets, Formatting.Indented));

        }

        public static async Task<List<Wallet>> getAllWalletsAsync()
        {
            //var pathFile = Path.Combine(@"C:\Users\Sco\source\repos\SunFlower\SunFlower\config\", "Wallets.json");
            var pathFile = @"..\..\..\config\Wallets.json";
            var json = await File.ReadAllTextAsync(pathFile);
            var wallets = JsonConvert.DeserializeObject<List<Wallet>>(json);

            return wallets;
        }
    }
}
