using AngleSharp;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Net;
using Microsoft.Extensions.Configuration;
using MimeKit;
using ActivationReport.Models;
using MailKit.Net.Smtp;
using System.Text;
using ActivationReport.Components.Pages;
using MudBlazor;
using System.Net.Http.Headers;

namespace ActivationReport
{
    internal static class Services
    {
        public static void SendEmail(Company company, string csvFilePath, DateTime? month = null)
        {
            var config = new ConfigurationBuilder()
                            .AddJsonFile("appsettings.json")
                            .Build();            

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Штрих-М", config.GetSection("Email").GetSection("Login").Value));
            message.To.Add(new MailboxAddress("Отчёт по активациям СКЗИ для " + company.Name, company.Email));

            if (month != null)
            {
                message.Subject = $"Расширенный отчёт по активациям СКЗИ за " + month?.ToString("y");
            }
            else
            {
                message.Subject = $"Расширенный отчёт по активациям СКЗИ";
            }

            var bodyBuilder = new BodyBuilder { TextBody = @"Отчёт по активациям блоков СКЗИ во вложенном файле" };

            using (var stream = System.IO.File.OpenRead(csvFilePath))
            {
                bodyBuilder.Attachments.Add(Path.GetFileName(csvFilePath), stream);
            }

            message.Body = bodyBuilder.ToMessageBody();

            using (var client = new SmtpClient())
            {
                client.Connect("mail.shtrih-m.ru", 465, true);
                client.Authenticate(config.GetSection("Email").GetSection("Login").Value, config.GetSection("Email").GetSection("Password").Value);

                try
                {
                    client.Send(message);
                    client.Disconnect(true);
                }
                catch (Exception ex) 
                {
                    Console.WriteLine($"An error occurred: {ex.Message}");
                }                
            }
        }
        
        
        public static async Task<string[]> AtlasAuth()
        {
            string? xsrfToken = null;
            string? seraphRememberme = null;

            var config = new ConfigurationBuilder()
                        .AddJsonFile("appsettings.json")
                        .Build();

            var handler = new HttpClientHandler
            {
                CookieContainer = new CookieContainer()
            };

            //handler.CookieContainer.Add(new Uri("http://support.atlas-kard.ru"), new Cookie("atlassian.xsrf.token", "B9Y4-SFXY-UDJ1-SPMI|d01a1c60a20887d6d92b29ab9868b22e1465f2da|lout", "/", "support.atlas-kard.ru"));

            using var client = new HttpClient(handler);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36");
            client.DefaultRequestHeaders.Accept.ParseAdd("*/*");
            client.DefaultRequestHeaders.AcceptEncoding.ParseAdd("gzip, deflate");
            client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("ru-RU,ru;q=0.9,en-US;q=0.8,en;q=0.7,sr;q=0.6");
            client.DefaultRequestHeaders.Add("DNT", "1");
            client.DefaultRequestHeaders.Add("Origin", "http://support.atlas-kard.ru");
            client.DefaultRequestHeaders.Referrer = new Uri("http://support.atlas-kard.ru/jira/servicedesk/customer/user/login?destination=user%2Frequests%3Fstatus%3Dopen");
            client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("os_username", config.GetSection("Portal").GetSection("os_username").Value),
                new KeyValuePair<string, string>("os_password", config.GetSection("Portal").GetSection("os_password").Value),
                new KeyValuePair<string, string>("os_captcha", ""),
                new KeyValuePair<string, string>("os_cookie", "true")
            });

            var response = await client.PostAsync("http://support.atlas-kard.ru/jira/servicedesk/customer/user/login", content);
            
            var cookies = response.Headers.GetValues("Set-Cookie");

            //var responseString = await response.Content.ReadAsStringAsync();

            foreach (var cookie in cookies)
            {
                if (cookie.Contains("atlassian.xsrf.token"))
                {
                    xsrfToken = ExtractXsrfToken(cookie);                    
                }
                if (cookie.Contains("seraph.rememberme.cookie"))
                {
                    seraphRememberme = ExtractSeraphRememberme(cookie);                   
                }
            }
            Debug.WriteLine($"xsrfToken: {xsrfToken}");
            Debug.WriteLine($"seraphRememberme: {seraphRememberme}");

            return [xsrfToken, seraphRememberme];
        }

        private static string? ExtractXsrfToken(string cookie)
        {
            // Пример обработки строки cookie для извлечения XSRF-токена
            // Ожидается, что строка cookie выглядит как-то так: "atlassian.xsrf.token=<token_value>; Path=/; HttpOnly"
            var parts = cookie.Split(';');
            foreach (var part in parts)
            {
                if (part.Trim().StartsWith("atlassian.xsrf.token"))
                {
                    return part.Split('=')[1].Trim();
                }
            }
            return null;
        }

        private static string? ExtractSeraphRememberme(string cookie)
        {
            var parts = cookie.Split(';');
            foreach (var part in parts)
            {
                if (part.Trim().StartsWith("seraph.rememberme.cookie"))
                {
                    return part.Split('=')[1].Trim();
                }
            }
            return null;
        }

        public static async Task AtlasRequest(string startDate, string endDate)
        {
            string[]? cookies = await AtlasAuth();
            string? xsrfToken = cookies[0];
            string? seraphRememberme = cookies[1];

            string? fullUrl = null;

            if (xsrfToken != null && seraphRememberme != null)
            {
                Debug.WriteLine($"Делаю запрос");
                var handler = new HttpClientHandler
                {
                    CookieContainer = new CookieContainer()
                };

                handler.CookieContainer.Add(new Uri("http://support.atlas-kard.ru"), new Cookie("seraph.rememberme.cookie", seraphRememberme, "/", "support.atlas-kard.ru"));
                handler.CookieContainer.Add(new Uri("http://support.atlas-kard.ru"), new Cookie("atlassian.xsrf.token", xsrfToken, "/", "support.atlas-kard.ru"));

                using (var client = new HttpClient(handler))
                {
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36");
                    client.DefaultRequestHeaders.Accept.ParseAdd("*/*");
                    client.DefaultRequestHeaders.AcceptEncoding.ParseAdd("gzip, deflate");
                    client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("ru-RU,ru;q=0.9,en-US;q=0.8,en;q=0.7,sr;q=0.6");
                    client.DefaultRequestHeaders.Add("DNT", "1");
                    client.DefaultRequestHeaders.Add("Origin", "http://support.atlas-kard.ru");
                    client.DefaultRequestHeaders.Referrer = new Uri("http://support.atlas-kard.ru/jira/servicedesk/customer/portal/3/create/27");
                    client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");


                    var content = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("atl_token", xsrfToken),
                        new KeyValuePair<string, string>("projectId", "10201"),
                        new KeyValuePair<string, string>("customfield_10601", "TACHO-446"),
                        new KeyValuePair<string, string>("customfield_11502", "10100"),
                        new KeyValuePair<string, string>("customfield_11500", startDate),
                        new KeyValuePair<string, string>("customfield_11501", endDate),
                        new KeyValuePair<string, string>("sd-kb-article-viewed", "false")
                    });

                    var response = await client.PostAsync("http://support.atlas-kard.ru/jira/servicedesk/customer/portal/3/create/27", content);

                    var responseString = await response.Content.ReadAsStringAsync();
               
                    var jsonResponse = JObject.Parse(responseString);
                    var requestDetailsBaseUrl = jsonResponse["requestDetailsBaseUrl"]?.ToString();

                    if (requestDetailsBaseUrl != null)
                    {
                        fullUrl = "http://support.atlas-kard.ru" + requestDetailsBaseUrl;
                        Debug.WriteLine($"Create full URL: {fullUrl}");
                    }
                }

                if (fullUrl != null){
                    await GetDownloadLink(fullUrl, xsrfToken, seraphRememberme);
                }
            }
        }

        public static async Task GetDownloadLink(string url, string xsrfToken, string seraphRememberme)
       {
            var handler = new HttpClientHandler
            {
                CookieContainer = new CookieContainer(),
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };

            handler.CookieContainer.Add(new Uri("http://support.atlas-kard.ru"), new Cookie("seraph.rememberme.cookie", seraphRememberme, "/", "support.atlas-kard.ru"));
            handler.CookieContainer.Add(new Uri("http://support.atlas-kard.ru"), new Cookie("atlassian.xsrf.token", xsrfToken, "/", "support.atlas-kard.ru"));

            using (var client = new HttpClient(handler))
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36");
                //client.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*;q=0.8,application/signed-exchange;v=b3;q=0.7");
                client.DefaultRequestHeaders.Accept.ParseAdd("*/*");
                client.DefaultRequestHeaders.AcceptEncoding.ParseAdd("gzip, deflate");
                client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("ru-RU,ru;q=0.9,en-US;q=0.8,en;q=0.7,sr;q=0.6");
                client.DefaultRequestHeaders.Add("Cache-Control", "max-age=0");
                client.DefaultRequestHeaders.Add("Connection", "keep-alive");
                client.DefaultRequestHeaders.Add("DNT", "1");
                client.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");

                try
                {
                    int status = 0;
                    JObject jsonResponse;
                    HttpResponseMessage response;

                    do
                    {
                        Debug.WriteLine($"Делаю запрос 2");
                        response = await client.GetAsync(url);
                        response.EnsureSuccessStatusCode();

                        // Читаем и возвращаем HTML-контент
                        var detailsResponseString = await response.Content.ReadAsStringAsync();

                        var parser = new AngleSharp.Html.Parser.HtmlParser();
                        var document = parser.ParseDocument(detailsResponseString);
                        var JSONBody = document.QuerySelector($"#jsonPayload").InnerHtml;

                        jsonResponse = JObject.Parse(JSONBody);
                        status = jsonResponse["reqDetails"]["issue"]["activityStream"].Count();
                        Debug.WriteLine($"Status: {status}");
                        if (status < 6)
                        {
                            await Task.Delay(60000);
                        }
                    } while (status != 6);

                    string downloadUrl = "http://support.atlas-kard.ru" + jsonResponse["reqDetails"]["issue"]["activityStream"]
                                    .Where(activity => activity["type"].ToString() == "worker-comment")
                                    .Select(activity => activity["comment"].ToString())
                                    .Where(comment => comment.Contains("href=\""))
                                    .Select(comment => comment.Split(new string[] { "href=\"" }, StringSplitOptions.None)[1].Split('"')[0])
                                    .FirstOrDefault();

                    string fileName = jsonResponse["reqDetails"]["issue"]["activityStream"]
                                    .Where(activity => activity["type"].ToString() == "worker-comment")
                                    .Select(activity => activity["rawComment"].ToString())
                                    .Select(rawComment => rawComment.Split(new string[] { "^" }, StringSplitOptions.None)[1].Split(']')[0])
                                    .FirstOrDefault();

                    string filePath = "C:\\Users\\Makar\\Desktop\\TEST\\" + fileName;
        // Сделать поле для выбора
                    using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        Debug.WriteLine($"Запрос на скачивание");

                        var responseMessage = await client.GetAsync(downloadUrl);
                        var file = await responseMessage.Content.ReadAsByteArrayAsync();
                        try
                        {
                            Debug.WriteLine($"Пишу в файл");
                            fs.Write(file, 0, file.Length);                            
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Ошибка записи файла: {ex.Message}");
                        }
                    }
                    Debug.WriteLine($"Запускаю парсер");
                    ParseAndLoad(filePath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred: {ex.Message}");
                }
            }
        }

        public static async Task ParseAndLoad(string filePath)
        {
            string row;
            string[] separateRow;
            int count = 0;
            int totalCount = 0;
            List<Activation> list = new List<Activation>();
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            using (var csvReader = new StreamReader(filePath, Encoding.GetEncoding(1251)))
            {
                await using var db = new AppDBContext();
                var listDB = db.Activations.Select(t => t.CryptoBlock).ToList();

                csvReader.ReadLine();   //skip first line
                while ((row = csvReader.ReadLine()) != null && !row.Substring(0, 1).Equals(";"))
                {
                    separateRow = row.Split(";", System.StringSplitOptions.RemoveEmptyEntries);

                    if (separateRow.Length > 19)
                    {
                        for (int i = 0; i < separateRow.Length - 1; i++)
                        {
                            if (separateRow[i].Equals(""))
                            {
                                continue;
                            }
                            if (
                                (!separateRow[i].Substring(separateRow[i].Length - 1, 1).Equals("\"") && !separateRow[i + 1].Substring(0, 1).Equals("\"")) ||
                                (!separateRow[i + 1].Substring(0, 1).Equals("\"") && !separateRow[i + 1].Substring(0, 1).Equals("=")) ||
                                !separateRow[i].Substring(separateRow[i].Length - 1, 1).Equals("\"") // если ; последний в строке
                                )
                            {
                                separateRow[i] = separateRow[i] + ";" + separateRow[i + 1];
                                separateRow[i + 1] = "";
                            }
                        }
                        separateRow = separateRow.Where(s => !string.IsNullOrEmpty(s)).ToArray();
                    }

                    if (separateRow.Length != 19)
                    {
                        await Application.Current.MainPage.DisplayAlert("Ошибка разбора строки", row, "Ок");
                        return;
                    }

                    if (!separateRow[1].Substring(1, 5).Equals("Отказ") && !separateRow[1].Substring(1, 5).Equals("Ожида"))
                    {
                        var newAct = new Activation
                        {
                            CompanyId = Convert.ToInt16(separateRow[0].Substring(4, 10)),
                            ActDate = Convert.ToDateTime(separateRow[3].Replace("\"", "")).ToUniversalTime().AddHours(3.0),
                            CardNumber = separateRow[0],
                            DateRequest = separateRow[2],
                            DateAnswer = separateRow[3],
                            CompanyName = separateRow[4],
                            OGRN = separateRow[5],
                            INN = separateRow[6],
                            Region = separateRow[7],
                            City = separateRow[8],
                            Address = separateRow[9],
                            VehicleBrand = separateRow[10],
                            VehicleModel = separateRow[11],
                            VehicleYear = separateRow[12],
                            VehicleColor = separateRow[13],
                            VRN = separateRow[14],
                            VIN = separateRow[15],
                            VehiclePassport = separateRow[16],
                            CryptoBlock = separateRow[17],
                            TachoNumber = separateRow[18]
                        };

                        //if (!listDB.Exists(x => x.Equals(newAct.CryptoBlock.ToString())))
                        if (!listDB.Contains(separateRow[17]))
                        {
                            list.Add(newAct);
                            count++;
                            totalCount++;
                        }
                    }

                    if (count >= 100)
                    {
                        db.Activations.AddRange(list);
                        db.SaveChanges();
                        list.Clear();
                        count = 0;
                    }
                }
                if (count > 0)
                {
                    db.Activations.AddRange(list);
                    db.SaveChanges();
                }
            }

            Debug.WriteLine("Уведомляю о конце операции");
            Database.filePath = null;            
            Database.LoadingStatus = false;

            //Application.Current.MainPage.DisplayAlert("Уведомление",
            //                                          "Файл обработан. В базу данных загружено записей: " + totalCount.ToString(), "Ок");
        }

        //public static async Task<NetItem> StartRequestFBU()
        //{
        //    string token = "";
        //    string captcha = "https://portal.rosavtotransport.ru";
        //    CookieContainer cookie = new();
        //    //string PHPSessionID = "";
        //    //string captcha2 = "";

        //    Debug.WriteLine($"Делаю запрос на сайт");

        //    //using (var client = new HttpClient())
        //    //{
        //    //    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
        //    //    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xhtml+xml"));
        //    //    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml", 0.9));
        //    //    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("image/avif"));
        //    //    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("image/webp"));
        //    //    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("image/apng"));
        //    //    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*", 0.8));
        //    //    //client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/signed-exchange;v=b3", 0.7));

        //    //    //client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
        //    //    //client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
        //    //    //client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("br"));
        //    //    //client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("zstd"));

        //    //    client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("ru-RU"));
        //    //    // client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("ru", 0.9));

        //    //    client.DefaultRequestHeaders.Connection.Add("keep-alive");
        //    //    client.DefaultRequestHeaders.TryAddWithoutValidation("DNT", "1");
        //    //    client.DefaultRequestHeaders.Host = "portal.rosavtotransport.ru";

        //    //    client.DefaultRequestHeaders.Add("Sec-CH-UA", "\"Google Chrome\";v=\"129\", \"Not=A?Brand\";v=\"8\", \"Chromium\";v=\"129\"");
        //    //    client.DefaultRequestHeaders.Add("Sec-CH-UA-Mobile", "?0");
        //    //    client.DefaultRequestHeaders.Add("Sec-CH-UA-Platform", "\"Windows\"");
        //    //    client.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "document");
        //    //    client.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "navigate");
        //    //    client.DefaultRequestHeaders.Add("Sec-Fetch-Site", "none");
        //    //    client.DefaultRequestHeaders.Add("Sec-Fetch-User", "?1");
        //    //    client.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");
        //    //    client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/129.0.0.0 Safari/537.36");



        //    //    var response = await client.GetAsync("https://portal.rosavtotransport.ru/check");
        //    //    var getCookieValue = response.Headers.GetValues("Set-Cookie");

        //    //    cookie = getCookieValue.FirstOrDefault().Split(" ")[0];

        //    //    var responseString = await response.Content?.ReadAsStringAsync();
        //    //    var parser = new AngleSharp.Html.Parser.HtmlParser();
        //    //    var document = parser.ParseDocument(responseString);
        //    //    captcha += document.QuerySelector($"#yiiCaptcha").GetAttribute("src");
        //    //    token = document.QuerySelector("[name=\"csrf-token\"]").GetAttribute("content");
        //    //    captcha2 = "https://portal.rosavtotransport.ru" + document.QuerySelector($"#recallform-captcha-image").GetAttribute("src");

        //    //    var img2 = await client.GetAsync(captcha2);
        //    //    var getSession2 = img2.Headers.GetValues("Set-Cookie");
        //    //    PHPSessionID = getSession2.FirstOrDefault().Split(" ")[0];
        //    //}


        //    using (var driver = new ChromeDriver())
        //    {
        //        driver.Navigate().GoToUrl("https://portal.rosavtotransport.ru/check");

        //        // Ожидание загрузки капчи
        //        var wait = new OpenQA.Selenium.Support.UI.WebDriverWait(driver, TimeSpan.FromSeconds(5));
        //        var captchaImage = wait.Until(d => d.FindElement(By.Id("yiiCaptcha")));
        //        token += wait.Until(d => d.FindElement(By.Name("csrf-token")).GetAttribute("content"));

        //        captcha = captchaImage.GetAttribute("src");
        //        //Console.WriteLine("Ссылка на капчу: " + captchaImageUrl);

        //        var cookies = driver.Manage().Cookies.AllCookies;
        //        //var cookieContainer = new CookieContainer();

        //        foreach (var cook in cookies)
        //        {
        //            cookie.Add(new System.Net.Cookie(cook.Name, cook.Value, cook.Path, cook.Domain));
        //        }

        //        Console.WriteLine("Ссылка на капчу: ");

        //        return new NetItem
        //        {
        //            cookie = cookie,
        //            token = token,
        //            captcha = captcha
        //        };
        //    }


        //    //var img = await client.GetAsync(captcha);
        //    //var getSession = img.Headers.GetValues("Set-Cookie");
        //    //PHPSessionID = getSession.FirstOrDefault().Split(" ")[0];

        //}

        public static async Task RequestFBU(string token, string enteredCaptcha, CookieContainer cookie, string data, string type)
        {

            Debug.WriteLine($"Делаю запрос в ФБУ");

            var handler = new HttpClientHandler
            {
                CookieContainer = cookie,
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };

            //handler.CookieContainer.Add(new Uri("https://portal.rosavtotransport.ru"), new System.Net.Cookie { Name = "_csrf", Value=cookie.Substring(6, cookie.Length-7) });
            //handler.CookieContainer.Add(new Uri("https://portal.rosavtotransport.ru"), new System.Net.Cookie { Name = "PHPSESSID", Value= phpsessionid.Substring(10, phpsessionid.Length-11) });

            using (var client = new HttpClient(handler))
            {
                //client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36");
                //client.DefaultRequestHeaders.Accept.ParseAdd("*/*");
                //client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/x-www-form-urlencoded"));
                //client.DefaultRequestHeaders.AcceptEncoding.ParseAdd("gzip, deflate");
                //client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("ru-RU,ru;q=0.9,en-US;q=0.8,en;q=0.7,sr;q=0.6");
                //client.DefaultRequestHeaders.Add("DNT", "1");
                //client.DefaultRequestHeaders.Add("Origin", "https://portal.rosavtotransport.ru");
                //client.DefaultRequestHeaders.Referrer = new Uri("https://portal.rosavtotransport.ru/check");
                //client.DefaultRequestHeaders.Add("X-CSRF-Token", token);
                //client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");                

                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
                client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
                client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
                client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("br"));
                client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("zstd"));
                client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("ru-RU"));
                client.DefaultRequestHeaders.Connection.Add("keep-alive");
                client.DefaultRequestHeaders.Add("DNT", "1");
                client.DefaultRequestHeaders.Host = "portal.rosavtotransport.ru";
                client.DefaultRequestHeaders.Add("Origin", "https://portal.rosavtotransport.ru");
                client.DefaultRequestHeaders.Add("Referer", "https://portal.rosavtotransport.ru/check");
                client.DefaultRequestHeaders.Add("X-CSRF-Token", token);
                client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
                client.DefaultRequestHeaders.Add("sec-ch-ua", "\"Google Chrome\";v=\"129\", \"Not=A?Brand\";v=\"8\", \"Chromium\";v=\"129\"");
                client.DefaultRequestHeaders.Add("sec-ch-ua-mobile", "?0");
                client.DefaultRequestHeaders.Add("sec-ch-ua-platform", "\"Windows\"");

                var content = new FormUrlEncodedContent(new[]
                 {
                    new KeyValuePair<string, string>("_csrf", token),
                    new KeyValuePair<string, string>("type", type),
                    new KeyValuePair<string, string>("tachoSerialNumber", data),
                    new KeyValuePair<string, string>("skziSerialNumber", data),
                    new KeyValuePair<string, string>("cardSerialNumber", ""),
                    new KeyValuePair<string, string>("cardBeginDate", ""),
                    new KeyValuePair<string, string>("dateFrom", ""),
                    new KeyValuePair<string, string>("tslinkSkziSerialNumber", ""),
                    new KeyValuePair<string, string>("tslinkTachoSerialNumber", ""),
                    new KeyValuePair<string, string>("captcha", enteredCaptcha)
                });
                //Debug.WriteLine(content);
                var response = await client.PostAsync("https://portal.rosavtotransport.ru/check", content);



                var responseString = await response.Content.ReadAsStringAsync();

                var parser = new AngleSharp.Html.Parser.HtmlParser();
                var document = parser.ParseDocument(responseString);

                var test1 = 0;
            }
        }
    }
} 