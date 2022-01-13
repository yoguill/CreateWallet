using System;
using System.Configuration;
using System.IO;
using System.Threading.Tasks;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Util;
using Nethereum.Web3;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using CreateWallet.Model;
using System.Collections.Generic;
using System.Numerics;
using System.Security.Cryptography;
using System.Linq;

namespace CreateWallet
{
    internal class Program
    {
        static async Task Main(string[] args)

        {           
            //////////////////////////////////Le 1er Compte doit etre le mainaccount/////////////////////////////////////////////
            
            //Exemple pour connection réseaux Polygon
            var chainId = ConfigurationManager.AppSettings["ChainIdPolygon"];
            var adressRPC = ConfigurationManager.AppSettings["AdresseRpcPolygon"];

            //config a modifier
            var numberOfWallets = 4;
            var amountToBeSentGovernanceToken = 0.5m;
            var amountToBeSentOther= 1m;

            //Contrat du/des token secondaire
            var contractAdressSunflowerSFF = ConfigurationManager.AppSettings["ContractTokenSFF"];


            ////------------------------Création Wallets-----------------------------------------------------------
            var wallets = await Wallet.getAllWalletsAsync();

            if (numberOfWallets > wallets.Count)
            {
                Console.WriteLine("FOUND " + (wallets.Count) + " WALLETS AVAILABLE");
                Console.WriteLine("TRYING TO CREATE THE " + (numberOfWallets - wallets.Count) + " WALLETS MISSING...");
                var newWallets = Wallet.CreateWallet(numberOfWallets - wallets.Count);
                await Wallet.AddAllWalletsToJsonAsync(newWallets);
                wallets = await Wallet.getAllWalletsAsync();
                Console.WriteLine(wallets.Count + " WALLETS READY TO BE USE");
            }
            else
            {
                Console.WriteLine(wallets.Count + " WALLETS READY TO BE USE");
            }

            //défintion web3 main account + 1er account dan wallets.json= main
            var accountMain = new Nethereum.Web3.Accounts.Account(wallets.First().PrivateKey, Int32.Parse((chainId)));
            var web3RpcMain = new Web3(accountMain, adressRPC);
            var balanceGouvernanceMain = await GetBalanceMainCrypto(web3RpcMain, accountMain);
            var balanceMain = await GetBalance(web3RpcMain, accountMain, contractAdressSunflowerSFF);

            //Calcul du montant de jeton a distribué par rapport au nombre de compte, mettre en dur car le montant baisse a chaque envoie(sauf si on lance une fois le prog)
            //var amountAveragePerAccount = 
            //Console.WriteLine("Moyenne de jeton par compte " + Math.Round((decimal)amountAveragePerAccount, 3));

            foreach (var wallet in wallets.Skip(1))
            {
                var accountAdress = wallet.AccountAdress;
                var privateKey = wallet.PrivateKey;

                //définition du web3 de tous les wallets
                var account = new Nethereum.Web3.Accounts.Account(privateKey, Int32.Parse((chainId)));
                var web3Rpc = new Web3(account, adressRPC);

                ////------------------------Recuperere Main crypto balance---------------------------------------------
                var balanceGouvernance = await GetBalanceMainCrypto(web3Rpc, account);

                ////------------------------Recuperere autre crypto balance--------------------------------------------
                var balance = await GetBalance(web3Rpc, account,contractAdressSunflowerSFF);

                ////------------------------Envoie Main vers secondaire jeton gouvernance------------------------------
                //await SendMAINCryptoToWallet(accountMain, account, web3RpcMain, amountToBeSentGovernanceToken);

                ////------------------------Envoie Main vers secondaire jeton secondaire-------------------------------
                //await SendCrytpoToWallet(accountMain, account, web3RpcMain, amountToBeSentOther, contractAdressSunflowerSFF);

                ////------------------------Envoie secondaire vers Main jeton secondaire-------------------------------
                //await SendCrytpoToWallet(account, accountMain, web3Rpc, amountAveragePerAccount, contractAdressSunflowerSFF);

                ////------------------------Envoie secondaire vers Main jeton gouvernance------------------------------
                //await SendMAINCryptoToWallet(accountMain, account, web3RpcMain, amountToBeSentMatic);

            }

        }
        //Envoie Coins main reseau (matic sur polyghon, bnb sur BSC ect,avax sur avalanche)
        private static async Task SendMAINCryptoToWallet(Nethereum.Web3.Accounts.Account accountFrom, Nethereum.Web3.Accounts.Account accountTo, Web3 web3RpcAdress,Decimal amount )
        {
            //Envoie matic entre comptes
            var nonce = await accountFrom.NonceService.GetNextNonceAsync();
            Console.WriteLine("WALLET n°" + accountTo.Address + " ASKING FOR " + amount + " Coins...");
            var gasTransac2 = await GetGasSetup(web3RpcAdress, accountFrom.Address, accountTo.Address);
            var transaction = await web3RpcAdress.Eth.GetEtherTransferService()
                .TransferEtherAndWaitForReceiptAsync(accountTo.Address, amount, gas: gasTransac2.gasLimit, gasPriceGwei: Web3.Convert.FromWei(gasTransac2.gasPrice, UnitConversion.EthUnit.Gwei), nonce: nonce);
            Console.WriteLine("TRANSACTION DONE");
        }

        //Envoie la crypto (fournir contrat de la crypto)
        private static async Task SendCrytpoToWallet(Nethereum.Web3.Accounts.Account accountFrom, Nethereum.Web3.Accounts.Account accountTo, Web3 web3RpcAdress, Decimal amount,string contract)
        {
            //Recuperer les Gaz moyen de frais
            var gasTransac = await GetGasSetup(web3RpcAdress, accountFrom.Address, accountTo.Address);
            
            var transactionMessage = new TransferFunction
            {
                FromAddress = accountFrom.Address,
                To = accountTo.Address,
                TokenAmount = Nethereum.Web3.Web3.Convert.ToWei(amount),
                Gas = gasTransac.gasLimit,
                GasPrice = gasTransac.gasPrice
            };

            var transferHandler = web3RpcAdress.Eth.GetContractTransactionHandler<TransferFunction>();
            var transferReceipt = await transferHandler.SendRequestAndWaitForReceiptAsync(contract, transactionMessage);
            Console.WriteLine("TRANSACTION +" + amount + " SFF FROM WALLET TO MAIN WALLET DONE");
        }

        private static async Task<decimal> GetBalanceMainCrypto(Web3 web3RpcAdress, Nethereum.Web3.Accounts.Account walletAdress)
        {
            var balanceWei = await web3RpcAdress.Eth.GetBalance.SendRequestAsync(walletAdress.Address);
            var balance = Web3.Convert.FromWei(balanceWei.Value);
            Console.WriteLine("Balance jeton de gouvernance: " + Math.Round((decimal)balance, 3));
            return balance;
        }

        //chopper la balance des autres crypto
         private static async Task<decimal> GetBalance(Web3 web3RpcAdress, Nethereum.Web3.Accounts.Account walletAdress,string contract)
        {
            var ContractHandler = web3RpcAdress.Eth.GetContractHandler(contract);
            var balanceWei = await ContractHandler.QueryAsync<GetBalanceOfFunction, BigInteger>(new GetBalanceOfFunction() { AccountAddress = walletAdress.Address });
            var balance = Web3.Convert.FromWei(balanceWei);
            Console.WriteLine("Balance jeton secondaire: " + Math.Round((decimal)balance, 3));
            return balance;
        }

        private static async Task<(HexBigInteger gasPrice, HexBigInteger gasLimit)> GetGasSetup(Web3 web3, string From, String To)
        {

            var random = RandomNumberGenerator.GetInt32(1, 999);
            var gasPrice = await web3.Eth.GasPrice.SendRequestAsync();
            var tenPercent = (gasPrice.Value * (BigDecimal)0.1d);
            gasPrice = new HexBigInteger(gasPrice + tenPercent.Mantissa + random);
            //gasPrice = new HexBigInteger(200000000000);
            var transfer = new CallInput()
            {
                From = From,
                To = To,
                GasPrice = gasPrice
            };

            var gasLimit = await web3.Eth.Transactions.EstimateGas.SendRequestAsync(transfer);
            gasLimit = new HexBigInteger(700000 + random);

            Console.WriteLine("== Nouvelle estimation ==");
            Console.WriteLine("Limit gaz : " + gasLimit);
            Console.WriteLine("Gas price : " + gasPrice.Value.ToString()[0..3] + " Gwei");

            return (gasPrice, gasLimit);
        }

        [Function("transfer", "bool")]
        public class TransferFunction : FunctionMessage
        {
            [Parameter("address", "recipient", 1)]
            public string To { get; set; }

            [Parameter("uint256", "amount", 2)]
            public BigInteger TokenAmount { get; set; }
        }
        //obligatoire pour le format (rien compris)
        [Function("balanceOf", "uint256")]
        public class GetBalanceOfFunction : FunctionMessage
        {
            [Parameter("address", "account", 1)]
            public virtual string AccountAddress { get; set; }
        }
    }
}
