using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Newtonsoft.Json;
using System.Collections.Specialized;
using System.Net;
using System.Transactions;
using Microsoft.VisualBasic;
using System.Text.Json;
using System.Net.Mime;

namespace TelegramBotExperiments
{

    class Program
    {
        public static List<BotUser> users = new List<BotUser>();
        private static readonly HttpClient client = new HttpClient();
        static ITelegramBotClient bot = new TelegramBotClient("telegramAPIKey");
        public static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            // Некоторые действия
            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(update));
            if (update.Type == Telegram.Bot.Types.Enums.UpdateType.Message)
            {
                if (!ContainCheck(users, update.Message.From.Id.ToString()))
                    users.Add(new BotUser(update.Message.From.Id.ToString(), false, Activity.justStarted));
                BotUser ActiveUser = users[GetUserIndexInList(users, update.Message.From.Id.ToString())];
                var message = update.Message;
                switch (message.Text.ToLower())
                {
                    case "0":
                        await botClient.SendTextMessageAsync(message.Chat, "Вы можете начать работу прописав /start");
                        return;
                    case "/start":
                        ActiveUser = users.Find(user => user.id == update.Message.From.Id.ToString());
                        ActiveUser.activity = Activity.justStarted;
                        await botClient.SendTextMessageAsync(message.Chat, "Добро пожаловать! Выберите одну из функций меню.");
                        return;
                    case "/translate":
                        await botClient.SendTextMessageAsync(message.Chat, "Введите текст для перевода.");
                        ActiveUser.activity = Activity.translatingText;
                        return;
                    case "/btc":
                        WebRequest req = WebRequest.Create("https://api.apilayer.com/fixer/latest?base=USD&symbols=EUR,RUB,BTC");
                        req.Method = "GET";
                        req.Headers.Add("apikey", "APIKEY");
                        WebResponse res = await req.GetResponseAsync();
                        string ans = string.Empty;
                        using (Stream s = res.GetResponseStream())
                        {
                            using (StreamReader sr = new StreamReader(s))
                            {
                                ans = await sr.ReadToEndAsync();
                            }
                        }
                        var currencyObject = JsonConvert.DeserializeObject<Rate>(ans);
                        res.Close();
                        await botClient.SendTextMessageAsync(message.Chat, $"Курс валют относительно доллара:\nРубль - {currencyObject.rates.RUB}\nЕвро - {currencyObject.rates.EUR}\nБиткоин - {currencyObject.rates.BTC}");
                        return;
                    case "/weather":
                        Dictionary<string, string> conditions = new Dictionary<string, string>();
                        conditions.Add("clear", "ясно");
                        conditions.Add("partly-cloudy ", "малооблачно");
                        conditions.Add("cloudy", "облачно с прояснениями");
                        conditions.Add("overcast", "пасмурно");
                        conditions.Add("drizzle", "морось");
                        conditions.Add("light-rain", "небольшой дождь");
                        conditions.Add("rain", "дождь");
                        conditions.Add("moderate-rain", "умеренно сильный дождь");
                        conditions.Add("heavy-rain", "сильный дождь");
                        conditions.Add("continuous-heavy-rain", "длительный сильный дождь");
                        conditions.Add("showers", "ливень");
                        conditions.Add("wet-snow ", "дождь со снегом");
                        conditions.Add("light-snow", "небольшой снег");
                        conditions.Add("snow", "снег");
                        conditions.Add("snow-showers", "снегопад");
                        conditions.Add("hail", "град");
                        conditions.Add("thunderstorm", "гроза");
                        conditions.Add("thunderstorm-with-rain", "дождь с грозой");
                        conditions.Add("thunderstorm-with-hail", "гроза с градом");
                        WebRequest request = WebRequest.Create("https://api.weather.yandex.ru/v2/informers?lat=53.100511&lon=50.007112&lang=ru-RU");
                        request.Method = "GET";
                        request.Headers.Add("X-Yandex-API-Key", "YandexAPIKEY");
                        WebResponse resoult = await request.GetResponseAsync();
                        string answ = string.Empty;
                        using (Stream s = resoult.GetResponseStream())
                        {
                            using (StreamReader sr = new StreamReader(s))
                            {
                                answ = await sr.ReadToEndAsync();
                            }
                        }
                        var weatherObject = JsonConvert.DeserializeObject<Ress>(answ);
                        resoult.Close();
                        string condition = conditions[weatherObject.fact.condition];
                        await botClient.SendTextMessageAsync(message.Chat, $"Город: Новокуйбышевск.\nЗа окном: {weatherObject.fact.temp} градусов.\nСкорость ветра: " +
                            $"{weatherObject.fact.wind_speed} м/с. \nПогода: {condition}.\nДавление: {weatherObject.fact.pressure_mm} мм ртутного столба.");
                        return;
                    case "/mathgame":
                        await botClient.SendTextMessageAsync(message.Chat, "Тогда давайте начнём! Какое ваше любимое число?");
                        ActiveUser.activity = Activity.playingMathGame;
                        return;
                }
                switch (ActiveUser.activity)
                {
                    case Activity.justStarted:
                        await botClient.SendTextMessageAsync(message.Chat, "Введён неверный запрос!");
                        return;
                    case Activity.translatingText:
                        try
                        {
                            
                            using (var wb = new WebClient())
                            {
                                TextTranslate tx = new TextTranslate("ru", message.Text, "b1guuisq3soq37rvl8vd");
                                wb.Headers.Add("Content-Type", "application/json");
                                wb.Headers.Add("Authorization", "Bearer SomeKey");
                                /*$yandexPassportOauthToken = "YANDEX TOKEN"
$Body = @{ yandexPassportOauthToken = "$yandexPassportOauthToken" } | ConvertTo - Json - Compress
Invoke - RestMethod - Method 'POST' - Uri 'https://iam.api.cloud.yandex.net/iam/v1/tokens' - Body $Body - ContentType 'Application/json' | Select - Object - ExpandProperty iamToken*/

                                string js = JsonConvert.SerializeObject(tx);
                                var response = wb.UploadString("https://translate.api.cloud.yandex.net/translate/v2/translate", "POST", js);
                                string responseInString = response;
                                var rootObject = JsonConvert.DeserializeObject<Translation>(responseInString);
                                await botClient.SendTextMessageAsync(message.Chat, rootObject.translations[0].text);
                                return;
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                        return;
                    case Activity.playingMathGame:
                        Random rnd = new Random();
                        string[] symbs = new string[] { "+", "-", "*" };
                        char symb;
                        int firstNum = 0;
                        int secondNum = 0;
                        string sy = symbs[rnd.Next(0, 3)];
                        if (!ActiveUser.isPlaying && !Int32.TryParse(message.Text, out int x))
                        {
                            await botClient.SendTextMessageAsync(message.Chat, "Вы ввели не число!");
                            return;
                        }
                        else if (ActiveUser.isPlaying && !Int32.TryParse(message.Text, out x))
                        {
                            await botClient.SendTextMessageAsync(message.Chat, "Ваш ответ не является числом!");
                            return;
                        }
                        else if (ActiveUser.MathWinsInARow == 0 && !ActiveUser.isPlaying)
                        {
                            ActiveUser.isPlaying = true;
                            firstNum = Convert.ToInt32(message.Text) + rnd.Next(1, 20);
                            secondNum = rnd.Next(15, 50);
                            ActiveUser.mathGameAnsw = DoSomeMath(firstNum, secondNum, sy);

                            await botClient.SendTextMessageAsync(message.Chat, $"Решите пример: {firstNum} {sy} {secondNum}");
                            return;
                        }

                        if (ActiveUser.isPlaying && ActiveUser.mathGameAnsw.ToString() == message.Text)
                        {
                            ActiveUser.MathWinsInARow++;
                            await botClient.SendTextMessageAsync(message.Chat, $"Примеров решено подряд: {ActiveUser.MathWinsInARow}");
                        }
                        else if (ActiveUser.isPlaying && ActiveUser.mathGameAnsw.ToString() != message.Text)
                        {
                            await botClient.SendTextMessageAsync(message.Chat, "Вы проиграли!\n Что бы опять сыграть используйте команду!");
                            ActiveUser.MathWinsInARow = 0;
                            ActiveUser.activity = Activity.justStarted;
                            ActiveUser.isPlaying = false;
                            return;
                        }

                        if (ActiveUser.MathWinsInARow > 0)
                        {
                            firstNum = rnd.Next(2, 15) + rnd.Next(1, 20);
                            secondNum = rnd.Next(15, 50);
                            ActiveUser.mathGameAnsw = DoSomeMath(firstNum, secondNum, sy);
                            await botClient.SendTextMessageAsync(message.Chat, $"Решите пример: {firstNum} {sy} {secondNum}");
                        }

                        return;
                }
            }
        }


        public static async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            // Некоторые действия
            Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(exception));
        }
        public static bool ContainCheck(List<BotUser> arr, string newid)
        {
            foreach (var item in arr)
            {
                if (item.id == newid) { return true; }
            }
            return false;
        }
        public static int GetUserIndexInList(List<BotUser> arr, string newid)
        {
            foreach (var item in arr)
            {
                int i = 0;
                if (item.id == newid) { return i; }
                i++;
            }
            return 0;
        }

        public static int DoSomeMath(int fNum, int sNum, string sym)
        {
            switch (sym)
            {
                case "+":
                    return fNum + sNum;
                case "-":
                    return fNum - sNum;
                case "*":
                    return fNum * sNum;
            }
            return 0;
        }


        static void Main(string[] args)
        {
            Console.WriteLine("Запущен бот " + bot.GetMeAsync().Result.FirstName);

            var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = { }, // receive all update types
            };
            Dictionary<string, string> conditions = new Dictionary<string, string>();
            bot.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                receiverOptions,
                cancellationToken
            );
            Console.ReadLine();
        }
        public enum Activity
        {
            justStarted,
            translatingText,
            playingMathGame
        }

        public class BotUser
        {

            public string id { get; private set; }
            public int mathGameAnsw { get; set; }
            public bool isPlaying { get; set; }
            public int MathWinsInARow { get; set; }

            public Activity activity { get; set; }
            public BotUser(string Id, bool IsPlaying, Activity Activity)
            {
                id = Id;
                isPlaying = IsPlaying;
                activity = Activity;
            }
            public BotUser()
            {

            }

        }
        public class TextTranslate
        {
            public string targetLanguageCode { get; set; }
            public string texts { get; set; }
            public string folderId { get; set; }

            public TextTranslate(string TargetLanguageCode, string Texts, string FolderId)
            {
                targetLanguageCode = TargetLanguageCode;
                texts = Texts;
                folderId = FolderId;
            }

        }
        public class Translation
        {
            public List<Item> translations { get; set; }
        }
        public class Item
        {
            public string detectedLanguageCode { get; set; }

            public string text { get; set; }
        }
        public class Ress
        {
            public fact fact { get; set; }
        }
        public class fact
        {
            public float temp { get; set; }
            public string condition { get; set; }
            public float wind_speed { get; set; }
            public float pressure_mm { get; set; }
        }
        public class Rate
        {
            public rates rates { get; set; }
        }
        public class rates
        {
            public string EUR { get; set; }
            public string RUB { get; set; }
            public string BTC { get; set; }
        }
    }
}
