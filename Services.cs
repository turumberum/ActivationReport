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
            message.From.Add(new MailboxAddress("Штрих-М", "schelkunov@shtrih-m.ru"));
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
                client.Authenticate(config.GetConnectionString("MailLogin"), config.GetConnectionString("MailPassword"));

                client.Send(message);
                client.Disconnect(true);
            }
        }

        public static async void AtlasAuth()
        {
            var config = new ConfigurationBuilder()
                        .AddJsonFile("appsettings.json")
                        .Build();

            var handler = new HttpClientHandler
            {
                CookieContainer = new CookieContainer()
            };

            handler.CookieContainer.Add(new Uri("http://support.atlas-kard.ru"), new Cookie("atlassian.xsrf.token", "B9Y4-SFXY-UDJ1-SPMI|d01a1c60a20887d6d92b29ab9868b22e1465f2da|lout", "/", "support.atlas-kard.ru"));

            using (var client = new HttpClient(handler))
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36");
                client.DefaultRequestHeaders.Accept.ParseAdd("*/*");
                client.DefaultRequestHeaders.AcceptEncoding.ParseAdd("gzip, deflate");
                client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("ru-RU,ru;q=0.9,en-US;q=0.8,en;q=0.7,sr;q=0.6");
                client.DefaultRequestHeaders.Add("DNT", "1");
                client.DefaultRequestHeaders.Add("Origin", "http://support.atlas-kard.ru");
                client.DefaultRequestHeaders.Referrer = new Uri("http://support.atlas-kard.ru/jira/servicedesk/customer/user/login?destination=user%2Frequests%3Fstatus%3Dopen");
                //client.DefaultRequestHeaders.Referrer = new Uri("http://support.atlas-kard.ru/jira/servicedesk/customer/portal/3/TA-520578");
                client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");


                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("os_username", config.GetConnectionString("os_username")),
                    new KeyValuePair<string, string>("os_password", config.GetConnectionString("os_password")),
                    new KeyValuePair<string, string>("os_captcha", ""),
                    new KeyValuePair<string, string>("os_cookie", "true")
                });

                var response = await client.PostAsync("http://support.atlas-kard.ru/jira/servicedesk/customer/user/login", content);
                var responseString = await response.Content.ReadAsStringAsync();
            }
        }

        public static async void AtlasRequest(string startDate, string endDate)
        {
            Database.LoadingStatus = 1;
            AtlasAuth();
            string fullUrl = null;

            var handler = new HttpClientHandler
            {
                CookieContainer = new CookieContainer()
            };

            handler.CookieContainer.Add(new Uri("http://support.atlas-kard.ru"), new Cookie("seraph.rememberme.cookie", "796237%3A993c597d1bdd94f3c274f43b8ae6ff9557b4373c", "/", "support.atlas-kard.ru"));
            handler.CookieContainer.Add(new Uri("http://support.atlas-kard.ru"), new Cookie("atlassian.xsrf.token", "B9Y4-SFXY-UDJ1-SPMI|74446a0a93eaa639159e6e1432b0179577c48ac3|lin", "/", "support.atlas-kard.ru"));

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
                    new KeyValuePair<string, string>("atl_token", "B9Y4-SFXY-UDJ1-SPMI|74446a0a93eaa639159e6e1432b0179577c48ac3|lin"),
                    new KeyValuePair<string, string>("projectId", "10201"),
                    new KeyValuePair<string, string>("customfield_10601", "TACHO-446"),
                    new KeyValuePair<string, string>("customfield_11502", "10100"),
                    new KeyValuePair<string, string>("customfield_11500", startDate),
                    new KeyValuePair<string, string>("customfield_11501", endDate),
                    new KeyValuePair<string, string>("sd-kb-article-viewed", "false")
                });

                var response = await client.PostAsync("http://support.atlas-kard.ru/jira/servicedesk/customer/portal/3/create/27", content);

                var responseString = await response.Content.ReadAsStringAsync();
                //Debug.WriteLine(JsonConvert.DeserializeObject(responseString));

                var jsonResponse = JObject.Parse(responseString);
                var requestDetailsBaseUrl = jsonResponse["requestDetailsBaseUrl"]?.ToString();

                if (requestDetailsBaseUrl != null)
                {
                    fullUrl = "http://support.atlas-kard.ru" + requestDetailsBaseUrl;
                    Debug.WriteLine($"Create full URL: {fullUrl}");

                    // Send GET request to the URL
                    //var detailsResponse = await client.GetAsync(fullUrl);
                    //var detailsResponseString = await detailsResponse.Content.ReadAsStringAsync();
                    //Debug.WriteLine(detailsResponseString);
                }
            }
            Debug.WriteLine("Запрос отправлен, ожидаем составление отчёта");
            
            //Thread.Sleep(420000);
            Debug.WriteLine($"Выспался");

            if (fullUrl != null){
                GetDownloadLink(fullUrl);
            }
            

        }

        //public static async void RedirectTo()
        //{
        //    AtlasAuth();

        //    var handler = new HttpClientHandler
        //    {
        //        CookieContainer = new CookieContainer()
        //    };

        //    // Add cookies to the handler
        //    //handler.CookieContainer.Add(new Uri("http://support.atlas-kard.ru"), new Cookie("JSESSIONID", "4967FAB9EA921D83BC2185F8E7BAC68E", "/", "support.atlas-kard.ru"));
        //    handler.CookieContainer.Add(new Uri("http://support.atlas-kard.ru"), new Cookie("seraph.rememberme.cookie", "796237%3A993c597d1bdd94f3c274f43b8ae6ff9557b4373c", "/", "support.atlas-kard.ru"));
        //    handler.CookieContainer.Add(new Uri("http://support.atlas-kard.ru"), new Cookie("atlassian.xsrf.token", "B9Y4-SFXY-UDJ1-SPMI|74446a0a93eaa639159e6e1432b0179577c48ac3|lin", "/", "support.atlas-kard.ru"));

        //    using (var client = new HttpClient(handler))
        //    {
        //        //var client = new HttpClient();
        //        // var jsonResponse = JObject.Parse(responseString);
        //        var requestDetailsBaseUrl = "/jira/servicedesk/customer/portal/3/TA-520578";

        //        if (requestDetailsBaseUrl != null)
        //        {
        //            var fullUrl = "http://support.atlas-kard.ru" + requestDetailsBaseUrl;
        //            //Debug.WriteLine($"Navigating to: {fullUrl}");

        //            // Send GET request to the URL
        //            var detailsResponse = await client.GetAsync(fullUrl);
        //            var detailsResponseString = await detailsResponse.Content.ReadAsStringAsync();

        //            //Debug.WriteLine(detailsResponseString);

        //            var htmlDoc = new HtmlDocument();
        //            htmlDoc.LoadHtml(detailsResponseString);
        //            var ttt = htmlDoc.DocumentNode.SelectNodes("//a[contains(@class, '\"nobr\"')]");

        //            string linkClass = "\"nobr\"";

        //            var parser = new AngleSharp.Html.Parser.HtmlParser();
        //            var doc1 = parser.ParseDocument(detailsResponseString);
        //            var rrr = doc1.QuerySelector($"a.{linkClass}");

        //            var context = BrowsingContext.New(Configuration.Default);
        //            var document = context.OpenAsync(req => req.Content(detailsResponseString)).Result;
        //            var linkElement = document.QuerySelector($"a.{linkClass}");

        //            //var file = linkElement?.Get;

        //            var response = await client.GetAsync(linkElement?.GetAttribute("href"));
        //            response.EnsureSuccessStatusCode();


        //            string fileName = "C:\\Users\\Makar\\Desktop\\TEST\\";
        //            using (var fs = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None))
        //            {
        //                await response.Content.CopyToAsync(fs);
        //                Debug.WriteLine($"File downloaded successfully as {fileName}");
        //            }
        //        }
        //    }
        //}


        public static async void GetDownloadLink(string url)
        {
            AtlasAuth();

            var handler = new HttpClientHandler
            {
                CookieContainer = new CookieContainer(),
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };

            handler.CookieContainer.Add(new Uri("http://support.atlas-kard.ru"), new Cookie("seraph.rememberme.cookie", "796237%3A993c597d1bdd94f3c274f43b8ae6ff9557b4373c", "/", "support.atlas-kard.ru"));
            handler.CookieContainer.Add(new Uri("http://support.atlas-kard.ru"), new Cookie("atlassian.xsrf.token", "B9Y4-SFXY-UDJ1-SPMI|74446a0a93eaa639159e6e1432b0179577c48ac3|lin", "/", "support.atlas-kard.ru"));

            using (var client = new HttpClient(handler))
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36");
                client.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
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
                        Debug.WriteLine($"Делаю запрос");
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
                            Thread.Sleep(60000);
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


                    string filePath = "C:\\Users\\Макар\\Desktop\\Отчёты\\" + fileName;
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

        public static void ParseAndLoad(string filePath)
        {
            Database.LoadingStatus = 1;
            string row;
            string[] separateRow;
            int count = 0;
            int totalCount = 0;
            List<Activation> list = new List<Activation>();
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            using (var csvReader = new StreamReader(filePath, Encoding.GetEncoding(1251)))
            {
                using (var db = new AppDBContext())
                {
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
                                if ((!separateRow[i].Substring(separateRow[i].Length - 1, 1).Equals("\"") && !separateRow[i + 1].Substring(0, 1).Equals("\"")) ||
                                    (!separateRow[i + 1].Substring(0, 1).Equals("\"") && !separateRow[i + 1].Substring(0, 1).Equals("=")))
                                {
                                    separateRow[i] = separateRow[i] + ";" + separateRow[i + 1];
                                    separateRow[i + 1] = "";
                                }
                            }
                            separateRow = separateRow.Where(s => !string.IsNullOrEmpty(s)).ToArray();
                        }

                        if (separateRow.Length != 19)
                        {
                            Application.Current.MainPage.DisplayAlert("Ошибка разбора строки", row, "Ок");
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
            }
            //filePath = null;
            Debug.WriteLine("Уведомляю");
            Database.LoadingStatus = 2;
            Database.filePath = null;
            //Application.Current.MainPage.DisplayAlert("Уведомление",
            //                                          "Файл обработан. В базу данных загружено записей: " + totalCount.ToString(), "Ок");
        }
    }
} 
