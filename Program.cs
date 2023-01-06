using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Newtonsoft.Json;



namespace InternetShop{
    internal class Program{

        public static void Main(string[] args){
            ILogIn guestAccount = new GuestAccount();
            AccountDb accountDb = Serializer.DeserializeAccountDb();
            ShopDb shopDb = Serializer.DeserializeShopDb();
            TransactionsDb transactionsDb = Serializer.DeserializeTransactionsDb();
            
            guestAccount.LoadActionList(shopDb);
            
            ShopMediator checker = new ShopMediator(); //pass db ? 
            //checker.logIn(db, guestAccount); //instead of using this we pass this like a parameter below 
            checker.ChangeAccountType(checker.LogIn(accountDb, guestAccount), shopDb, accountDb, transactionsDb );
        }

    }

    class ShopMediator{
        private string _answer;
        

        public void ChangeAccountType(List<AccountData> accountType, ShopDb database, AccountDb accountDb, TransactionsDb transactionsDb){
            string accountT = null;
            string uName = null;
            string email = null;
            foreach (var type in accountType){
                accountT = type.accountType;
                uName = type.userName;
                email = type.email;
            }
            if (accountT == "default"){ 
                Console.WriteLine("Welcome to your account, " + uName);
                IAccount account = new CustomerAccount();
                account.userName = uName;
                account.email = email;
                account.LoadActionList(database, accountDb, transactionsDb);
            }

            if (accountT == "vendor"){
                Console.WriteLine("Welcome to your account, " + uName);
                IAccount account = new VendorAccount();
                account.userName = uName;
                account.email = email;
                Console.WriteLine("Following categories :");
                account.LoadActionList(database, accountDb);
            }
        }
        
        public List<AccountData> LogIn(AccountDb database, ILogIn account) { //returns a list of our account data to get data easily
            Console.WriteLine("Enter your login: ");
            _answer = Console.ReadLine();
            if (database.AccountsInfo.ContainsKey(_answer)){
                Console.WriteLine("Enter your password: ");
                string pass = Console.ReadLine();
                
                List<AccountData> allItems = database.AccountsInfo.Values.SelectMany(c => c).ToList();
                foreach (var item in allItems){
                    if (!Hasher.VerifyHashedPassword(item.password, pass)){ //compares passwords 
                        Console.WriteLine("Oops! You've entered a wrong password, try again, please");
                        LogIn(database, account);
                    }
                    
                    return database.AccountsInfo[_answer]; //store value by a key 
                }
            }
            else {
                Console.WriteLine("Incorrect login. Try again. \n Enter \"log in\" if you want to log in again, \"register\" if you want to sign up");
                RequestRegistration(database, account);
                
            }

            return null;
        }

        void RequestRegistration(AccountDb database, ILogIn account){
            string choose = Console.ReadLine().Trim().ToLower();
            switch (choose){
                case "login" : LogIn(database, account);
                    break;
                case "register": account.SignUp(database);
                    Serializer.Serialize(database);
                    Environment.Exit(0);
                    break;
                default: Console.WriteLine("Choose one from the following 2 options, please ");
                    RequestRegistration(database, account);
                    break;
            }

        }
        

    }

    interface IAccount{

        string email{ get; set; }
        string userName{ get; set; }
        string accountType{ get; set; }

        List<AccountData> userData{ get; }

        void LoadActionList(ShopDb database);
        void LoadActionList(ShopDb database, AccountDb accountDb);
        void LoadActionList(ShopDb database, AccountDb accountDb, TransactionsDb db);

    }
    interface ILogIn : IAccount{
        void SignUp(AccountDb database);
    }

    abstract class Accounts : IAccount{
        public string email{ get; set; }
        public string userName{ get; set; }
        protected string password{ get; set; }
        public string accountType{ get; set; }
        protected int balance { get; set; }
        
        public List<AccountData> userData{ get; }


        protected void PrintCategories() {
            Console.WriteLine("There are following categories of shop available: ");
            for (int i = 0; i < Shop.CategoriesList.Count; i++)
            {
                Console.WriteLine(Shop.CategoriesList[i]);
            }
        }

        protected void PrintCategory( ShopDb database ){
            
            
            Console.WriteLine("Enter category name you want to look at");
            string answer = Console.ReadLine();
            if (database.ItemsData.Count == 0) {
                Console.WriteLine("This category is empty, please, enter another one");
                PrintCategory(database);
            }
            foreach (var item in database.ItemsData){
                if (item.productCategory != answer){
                    Console.WriteLine("This category doesn't exist in the shop");
                    return;
                }
                Console.Write(GetItemIndex(database, item.productCode.ToLower()) + 1 + " "); // +1 because start from 0
                Console.Write(item.productName + "\n");
            }
        }

        protected void CheckProductInformation(ShopDb database){
            Console.WriteLine("Enter the product name, number or code you want to look at or enter \"Cancel\" if you want to return to main menu ");
            string answer = Console.ReadLine()?.Trim().ToLower();
            if (answer.Trim().ToLower() == "cancel"){
                return;
            }
            PrintProductInformation(database, answer);
        }

        private int GetItemIndex(ShopDb shopDb, string prCode){
            return shopDb.ItemsData.FindIndex(x => x.productCode.ToLower() == prCode);
        }
        protected ShopData GetByCodeOrNameOrIndex(ShopDb shopDb, string key){
            int i;
            bool isDigit = int.TryParse(key, out i);
            if (isDigit){
                Console.WriteLine("This product number is: " + i);
                i--;
                return shopDb.ItemsData[i];
            }
            return shopDb.ItemsData.Find(x => x.productCode.ToLower() == key || x.productName.ToLower() == key); //iterates through list and finds and returns object occurence

        }
        
        protected void PrintProductInformation(ShopDb database, string answer){
            try{
                ShopData requiredProduct = GetByCodeOrNameOrIndex(database, answer);
                if (requiredProduct.productName.Trim().ToLower() == answer.Trim().ToLower() || requiredProduct.productCode.Trim().ToLower() == answer.Trim().ToLower() || !database.ItemsData[Convert.ToInt32(answer) - 1].Equals(null)){
                    Console.WriteLine("Following information about product " + requiredProduct.productName);
                    Console.WriteLine("Product name: "+requiredProduct.productName + "\n"
                                      + "Product code: " + requiredProduct.productCode + "\n" 
                                      + "Price: " + requiredProduct.price + " UAH" + "\n"
                                      + "Warranty: " + requiredProduct.warrantyDuration + "\n"
                                      + "Description: " + requiredProduct.productDescription);
                    foreach (var fullDescription in requiredProduct.productCharacteristics){
                        Console.WriteLine(fullDescription);
                    }
                }
            }
            catch (Exception ){
                Console.WriteLine("This product doesn't exist. Try again"); CheckProductInformation(database);
            }
            

        }

        public virtual void LoadActionList(ShopDb database){ }
        public virtual void LoadActionList(ShopDb database, AccountDb accountDb){ }
        public virtual void LoadActionList(ShopDb database, AccountDb accountDb, TransactionsDb transactionsDb){ }
    }

    class GuestAccount : Accounts, ILogIn{
        private Regex _pattern = new Regex("\\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\\.[A-Za-z]{2,6}\\b");
        public List<AccountData> userData{ get; }

        public GuestAccount(){
            accountType = "guest";
            
            userData = new List<AccountData>();
        }
        
        public void SignUp(AccountDb database){
            Console.WriteLine("Enter your email");
            email = Console.ReadLine();

            CheckEmail(database);

            Console.WriteLine("Enter your user name");
            userName = Console.ReadLine();

            DefineType();
            
            Console.WriteLine("Enter your password:");
            password = Console.ReadLine();

            userData.Add(new AccountData(email, userName, Hasher.HashPassword(password), accountType, balance));
            database.AccountsInfo.Add(email, userData);
            
            
        }

        private void CheckEmail(AccountDb database){
            
            MatchCollection matches = _pattern.Matches(email);
            if (matches.Count <= 0){
                Console.WriteLine("That's not email.Please, enter your email again");
                SignUp(database);
            }
            if (matches.Count > 1){
                Console.WriteLine(
                    "Please, don't use spaces or don't try to enter more than one email. Enter your email again");
                SignUp(database);
            }
            if (database.AccountsInfo.ContainsKey(email)){
                Console.WriteLine("Account with this email already exists. Enter another email, please");
                SignUp(database);
            }

        }

        private void DefineType(){
            Console.WriteLine("Who are you ? Vendor or Customer?");
            string destination = Console.ReadLine();
            if (destination == "Vendor"){
                accountType = "vendor";
            }
            else if (destination == "Customer"){
                accountType = "default";
            }
            else{
                Console.WriteLine("Enter the correct information, please");
                DefineType();
            }
        }
        
        public override void LoadActionList(ShopDb database){
            Console.WriteLine("\nYou should log in if you want to buy something in shop \n".ToUpper());
            Console.WriteLine("Enter the following command action number you want to do ");
            PrintCommands();
            
            void PrintCommands(){ //local function 
                Console.WriteLine("Enter 1 to check all available shop categories");
                Console.WriteLine("Enter 2 to check current shop category");
                Console.WriteLine("Enter 3 to check product from shop category");
                Console.WriteLine("Enter 4 to log in");
                Console.WriteLine("Enter 5 to print available command list");
                Console.WriteLine("Enter 6 to exit shop");
            }
            while (true){
                Console.WriteLine("\nYou are in menu now. You can print your command list if you enter 5 \n");
                string answer = Console.ReadLine();
                switch (answer){
                    case "1": PrintCategories();
                        break;
                    case "2": PrintCategory(database);
                        break;
                    case "3": CheckProductInformation(database);
                        break;
                    case "4":
                        return; //exit your method
                    case "5": PrintCommands();
                        break;
                    case "6": Environment.Exit(0);
                        break;
                    default: Console.WriteLine("Please, enter the right number");
                        break;

                }
            }
        }
        
        
    }


    class CustomerAccount : Accounts{
        private void TopUpAccountBalance(AccountDb database){
            Console.WriteLine("Enter the amount of money you want to top up");
            try{
                int fill = Convert.ToInt32(Console.ReadLine());
                if (fill <= 0){
                    Console.WriteLine("You couldn't top up your account balance for negative value");
                    TopUpAccountBalance(database);
                }

                if (database.AccountsInfo.ContainsKey(email)){
                    foreach (var variable in database.AccountsInfo[email]){
                        variable.balance += fill;
                    }
                }
            
                Console.WriteLine("Your balance topped up for " + fill + " UAH");
                
            }
            catch ( Exception ){
                Console.WriteLine("Please, don't try to input incorrect values");
                TopUpAccountBalance(database);
            }
            
            Serializer.Serialize(database);
        }

        private void PrintAccountBalance(AccountDb database){
            foreach (var item in database.AccountsInfo[email]){
                Console.WriteLine("Your balance is: " + item.balance + " UAH");
            }
        }

        private void BuyProduct(ShopDb shopDb, AccountDb accountDb, TransactionsDb transactionsDb){
            Console.WriteLine("Enter the product code or name or number: "); //? - check for nullReference exception
            Console.WriteLine("You can cancel this operation if you enter \"Cancel\"");
            string userInput = Console.ReadLine()?.Trim().ToLower();

            if (userInput == "cancel"){
                return;
            }
            
            InvokeBuyProcedure(shopDb, accountDb, transactionsDb, userInput);
            
        }

        private void InvokeBuyProcedure(ShopDb shopDb, AccountDb accountDb, TransactionsDb transactionsDb, string userInput){
            try{
                ShopData item = GetByCodeOrNameOrIndex(shopDb, userInput);
                
                foreach (var account in accountDb.AccountsInfo[email]){

                    if (item.price > account.balance){
                        Console.WriteLine("You don't have enough money to buy this. Please, top up your balance");
                        return;
                    }

                    if (item.productCount <= 0){
                        Console.WriteLine("Sorry, this product is out of stock");
                        return;
                    }
                    Console.WriteLine("Are you sure you want to buy this product ? \n");
                    PrintProductInformation(shopDb, userInput);
                    Console.WriteLine("Enter \"yes\" or press any key if you sure or not");
                    string confirmation = Console.ReadLine()?.Trim().ToLower();
                    if (confirmation != "yes") { break; }
                    string phoneNumber = RequestPhoneNumber();
                    account.balance -= item.price;
                    item.productCount--;
                    foreach (var vendor in accountDb.AccountsInfo[item.vendorEmail]){
                        vendor.balance += item.price;
                        transactionsDb.TransactionsData.Add(new TransactionData(account.userName, account.email, phoneNumber, vendor.userName, vendor.email, item.productName, item.productCode, item.productCategory, item.price ));
                        Serializer.Serialize(transactionsDb);
                        Serializer.Serialize(shopDb);
                        Serializer.Serialize(accountDb);
                    }
                    Console.WriteLine("You've successfully bought this product. Please wait while the vendor calls you for your order processing. ");
                    break; //possible null reference exception if we don't exit cycle
                }
                
            }
            catch (Exception ){
                Console.WriteLine("The product with following number, name or code doesn't exist. Please, try again");
                BuyProduct(shopDb, accountDb, transactionsDb);
            }
            
        }

        private string RequestPhoneNumber(){
            Regex pattern = new Regex("^\\+380-\\d{2}-\\d{3}-\\d{2}-\\d{2}$");
            Console.WriteLine("Enter your phone number in format: +380-XX-XXX-XX-XX");
            string phoneNumber = Console.ReadLine().Trim();
            MatchCollection matches = pattern.Matches(phoneNumber);
            if (matches.Count <= 0 || matches.Count > 1){
                Console.WriteLine("Please, enter your phone number in correctly specified format");
                RequestPhoneNumber();
            }
            return phoneNumber;
        }
        
        public override void LoadActionList(ShopDb shopDb, AccountDb accountDb, TransactionsDb transactionDb ){
            Console.WriteLine("\nYou should log in if you want to buy something in shop \n".ToUpper());
            Console.WriteLine("Enter the following command action number you want to do ");
            PrintCommands();
            
            void PrintCommands(){ //local function 
                Console.WriteLine("Enter 1 to check all available shop categories");
                Console.WriteLine("Enter 2 to check current shop category");
                Console.WriteLine("Enter 3 to check all available products in current shop category");
                Console.WriteLine("Enter 4 to top up your account balance ");
                Console.WriteLine("Enter 5 to check your account balance");
                Console.WriteLine("Enter 6 to buy your product ");
                Console.WriteLine("Enter 7 to print available command list");
                Console.WriteLine("Enter 8 to log out");
            }
            while (true){
                Console.WriteLine("\nYou are in menu now. You can print your command list if you enter 7 \n");
                string answer = Console.ReadLine();
                switch (answer){
                    case "1": PrintCategories();
                        break;
                    case "2": PrintCategory(shopDb);
                        break;
                    case "3": CheckProductInformation(shopDb);
                        break;
                    case "4": TopUpAccountBalance(accountDb);
                        break;
                    case "5": PrintAccountBalance(accountDb);
                        break;
                    case "6": BuyProduct(shopDb, accountDb, transactionDb);
                        break;
                    case "7": PrintCommands();
                        break;
                    case "8": Environment.Exit(0);
                        break;
                    default: Console.WriteLine("Please, enter the right number");
                        break;

                }
            }
        }
        
        
    }

    class VendorAccount : Accounts{
        private void AddProduct(ShopDb database) { 
            List<string> prdctCharacteristics = new List<string>(); 
            
            Console.WriteLine("Enter the product category name you want to work with: ");
            string prdctCategory = Console.ReadLine().Trim().ToLower();
            for (int i = 0; i < Shop.CategoriesList.Count; i++) {
                if (!prdctCategory.Equals(Shop.CategoriesList[i].ToString().Trim().ToLower())) {
                    Console.WriteLine("This category doesn't exist in shop. Please, try again");
                    AddProduct(database);
                }
                else break;
            }

            Console.WriteLine("Enter you product count you want to sell:");
            int prdctCount = Convert.ToInt32(Console.ReadLine()); //parse to int because read(); broke input 
            
            Console.WriteLine("Enter your product name, including models, etc. : ");
            string prdctName = Console.ReadLine();

            Console.WriteLine("Enter your product description");
            string prdctDescription = Console.ReadLine();
            
            Console.WriteLine("Enter your product warranty duration: ");
            string prdctWarranty = Console.ReadLine();
            
            Console.WriteLine("Enter your product price in UAH format: ");
            int prdctPrice = Convert.ToInt32(Console.ReadLine());
            
            Console.WriteLine("Enter your product characteristics in format: \n \"characteristic tag\": description");
            //addCharacteristics(prdctCharacteristics);

            // call  addCharacteristics() method in database.Add();
            database.ItemsData.Add(new ShopData(userName, email ,prdctCount,prdctName, prdctCategory, Shop.GenerateProductCode(), AddCharacteristics(prdctCharacteristics), prdctDescription, prdctWarranty, prdctPrice ));
            Serializer.Serialize(database);
        }
        

        private List<string> AddCharacteristics(List <string> inputArray ) {
            Console.WriteLine("Enter \"confirm\" to finish adding characteristics or enter \"restart\" if you entered a wrong value ");
            while (true){
                string characteristics = Console.ReadLine();
                if (characteristics.Trim().ToLower() == "confirm") {
                    Console.WriteLine("You confirmed your input. Thank you");
                    break;
                }
                if (characteristics.Trim().ToLower() == "restart") {
                    Console.WriteLine("You requested a restart, please, enter your information again");
                    AddCharacteristics(inputArray);
                }
                inputArray.Add(characteristics);
            }

            return inputArray;
        }

        private void PrintAccountBalance(AccountDb database){
            foreach (var item in database.AccountsInfo[email]){
                Console.WriteLine("Your balance is: " + item.balance + " UAH");
            }
        }
        
        public override void LoadActionList(ShopDb shopDB, AccountDb accountDb){
            Console.WriteLine("\nYou should log in if you want to buy something in shop \n".ToUpper());
            Console.WriteLine("Enter the following command action number you want to do ");
            PrintCommands();
            
            void PrintCommands(){ //local function 
                Console.WriteLine("Enter 1 to check all available shop categories");
                Console.WriteLine("Enter 2 to check current shop category");
                Console.WriteLine("Enter 3 to check all available products in current shop category");
                Console.WriteLine("Enter 4 to place your product on the marketplace");
                Console.WriteLine("Enter 5 to check your account balance");
                Console.WriteLine("Enter 6 to print available command list");
                Console.WriteLine("Enter 7 to log out");
            }
            while (true){
                Console.WriteLine("\nYou are in menu now. You can print your command list if you enter 6 \n");
                string answer = Console.ReadLine();
                switch (answer){
                    case "1": PrintCategories();
                        break;
                    case "2": PrintCategory(shopDB);
                        break;
                    case "3": CheckProductInformation(shopDB);
                        break;
                    case "4": AddProduct(shopDB);
                        break;
                    case "5": PrintAccountBalance(accountDb);
                        break;
                    case "6": PrintCommands();
                        break;
                    case "7": Environment.Exit(0);
                        break;
                    default: Console.WriteLine("Please, enter the right number");
                        break;

                }
            }
        }
        

    }

    class Shop{
        
        public static readonly List<ProductCategories> CategoriesList = new List<ProductCategories>() //doesn't serialize
        {
            ProductCategories.Displays, ProductCategories.Notebooks, ProductCategories.Computers, ProductCategories.Headphones, //Electronics
            ProductCategories.Fridges, ProductCategories.WashingMachines, ProductCategories.KitchenStoves, ProductCategories.Driers //HouseholdAppliances
        };
        
        internal enum ProductCategories{
            Displays, Notebooks, Computers, Headphones, Fridges, WashingMachines, KitchenStoves, Driers
        }
        
        static Random _random = new Random();
        public static string GenerateProductCode(){
            int rndNumber = _random.Next(100000, 999999);

            return CreateString(3) + rndNumber;
        }
        
        private static string CreateString(int stringLength){
            const string allowedChars = "ABCDEFGHJKLMNOPQRSTUVWXYZ";
            char[] chars = new char[stringLength];

            for (int i = 0; i < stringLength; i++)
            {
                chars[i] = allowedChars[_random.Next(0, allowedChars.Length)];
            }

            return new string(chars);
        }
    }
    
    class AccountData{
        
        public string email{ get; }
        public string userName{ get; }
        public string password{ get; }
        public string accountType{ get; }
        public int balance { get; set; }
        
        
        public AccountData(string email, string userName, string password, string accountType, int balance){
            
            this.email = email;
            this.userName = userName;
            this.password = password;
            this.accountType = accountType;
            this.balance = balance;
        }
        
        // public AccountData(int balance){
        //     this.balance = balance;
        // }

    }

    class ShopData{
       public List<string> customers{ get; set; }
       
       public string vendorName{ get; }
       public string vendorEmail{ get; }
       public int productCount{ get; set; }
       public string productName{ get; }
       public string productCategory{ get; }
       public string productCode{ get; }
       public List<string> productCharacteristics { get; }
       public string productDescription{ get; }
       public string warrantyDuration{ get; }
       public int price{ get; }

       public ShopData(string vendorName, string vendorEmail, int productCount, string productName, string productCategory, string productCode, List<string> productCharacteristics, string productDescription, string warrantyDuration, int price){
           this.vendorName = vendorName;
           this.vendorEmail = vendorEmail;
           this.productCount = productCount;
           this.productName = productName;
           this.productCategory = productCategory;
           this.productCode = productCode;
           this.productCharacteristics = productCharacteristics;
           this.productDescription = productDescription;
           this.warrantyDuration = warrantyDuration;
           this.price = price;
       }
    }

    class TransactionData {
        public string customerName {get; }
        public string customerEmail{ get; }
        public string customerPhoneNumber{ get; }
        public string vendorName{ get; }
        public string vendorEmail{ get; }
        public string orderedProduct{ get; }
        public string orderedProductCode{ get; }
        public string orderedProductCategory{ get; }
        public int transactionPrice{ get; }

        public TransactionData(string customerName, string customerEmail, string customerPhoneNumber, string vendorName, string vendorEmail, string orderedProduct, string orderedProductCode, string orderedProductCategory, int transactionPrice){
            this.customerName = customerName;
            this.customerEmail = customerEmail;
            this.customerPhoneNumber = customerPhoneNumber;
            this.vendorName = vendorName;
            this.vendorEmail = vendorEmail;
            this.orderedProduct = orderedProduct;
            this.orderedProductCode = orderedProductCode;
            this.orderedProductCategory = orderedProductCategory;
            this.transactionPrice = transactionPrice;
        }
        
    }
    
    class AccountDb{
        public Dictionary<string, List<AccountData>> AccountsInfo
            = new Dictionary<string, List<AccountData>>(); //Map analogue, string is a key
    }

    class ShopDb{
        public List<ShopData> ItemsData = new List<ShopData>();
        
    }

    class TransactionsDb{
        public List<TransactionData> TransactionsData = new List<TransactionData>();
    }
    

    static class Serializer {

        public static void Serialize(AccountDb database){ //load into db 
            File.WriteAllText("accountData.json", JsonConvert.SerializeObject(database, Formatting.Indented));
        }
        public static void Serialize(ShopDb database){ //load into db 
            File.WriteAllText("shopData.json", JsonConvert.SerializeObject(database, Formatting.Indented));
        }
        public static void Serialize(TransactionsDb database){ //load into db 
            File.WriteAllText("transactionData.json", JsonConvert.SerializeObject(database, Formatting.Indented));
        }
        public static AccountDb DeserializeAccountDb(){ //get all data from db
            return File.Exists("accountData.json")
                ? JsonConvert.DeserializeObject<AccountDb>(File.ReadAllText("accountData.json")) : new AccountDb();
        }
        public static ShopDb DeserializeShopDb(){ //get all data from db
            return File.Exists("shopData.json")
                ? JsonConvert.DeserializeObject<ShopDb>(File.ReadAllText("shopData.json")) : new ShopDb();
            
        }
        public static TransactionsDb DeserializeTransactionsDb(){
            return File.Exists("transactionData.json")
                ? JsonConvert.DeserializeObject<TransactionsDb>(File.ReadAllText("transactionData.json")) : new TransactionsDb();
        }
    
    }

    static class Hasher {
        public static string HashPassword(string password)
        {
            byte[] salt; //sequence of numbers adding to the password 
            byte[] buffer2;
            if (password == null)
            {
                throw new ArgumentNullException("password");
            }
            using (Rfc2898DeriveBytes bytes = new Rfc2898DeriveBytes(password, 0x10, 0x3e8))
            {
                salt = bytes.Salt;
                buffer2 = bytes.GetBytes(0x20);
            }
            byte[] dst = new byte[0x31];
            Buffer.BlockCopy(salt, 0, dst, 1, 0x10);
            Buffer.BlockCopy(buffer2, 0, dst, 0x11, 0x20);
            return Convert.ToBase64String(dst);
        }
        
        public static bool VerifyHashedPassword(string hashedPassword, string password)
        {
            byte[] buffer4;
            if (hashedPassword == null)
            {
                return false;
            }
            if (password == null)
            {
                throw new ArgumentNullException("password");
            }
            byte[] src = Convert.FromBase64String(hashedPassword);
            if ((src.Length != 0x31) || (src[0] != 0))
            {
                return false;
            }
            byte[] dst = new byte[0x10];
            Buffer.BlockCopy(src, 1, dst, 0, 0x10);
            byte[] buffer3 = new byte[0x20];
            Buffer.BlockCopy(src, 0x11, buffer3, 0, 0x20);
            using (Rfc2898DeriveBytes bytes = new Rfc2898DeriveBytes(password, dst, 0x3e8))
            {
                buffer4 = bytes.GetBytes(0x20);
            }
            return ByteArrayCompare(buffer3, buffer4);
        }

        [DllImport("msvcrt.dll", CallingConvention=CallingConvention.Cdecl)] //  P/invoke
        static extern int memcmp(byte[] b1, byte[] b2, long count);

        static bool ByteArrayCompare(byte[] b1, byte[] b2)
        {
            // Validate buffers are the same length.
            // This also ensures that the count does not exceed the length of either buffer.  
            return b1.Length == b2.Length && memcmp(b1, b2, b1.Length) == 0;
        }
        
        
    }

    
    
}