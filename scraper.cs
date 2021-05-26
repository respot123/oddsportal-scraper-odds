using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

string userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/90.0.4430.212 Safari/537.36";
 
        async void GetOddsJson()
        {
            // get uniq id season. needed for requests
            string seasonID = "";
            //url for example
            var req = (HttpWebRequest)WebRequest.Create("https://www.oddsportal.com/soccer/norway/eliteserien/rosenborg-molde-pn7YroiC/");
            req.UserAgent = userAgent;
            req.Method = "GET";

            var resp = await req.GetResponseAsync();

            using (StreamReader readStream = new StreamReader(resp.GetResponseStream()))
            {
                string x = await readStream.ReadToEndAsync();

                //create dom object from the received html for parse
                var htmlDoc = HtmlDocumentFactory.Create();
                htmlDoc.Write(x);

                HtmlElementCollection tables = htmlDoc.GetElementsByTagName("script");

                foreach (HtmlElement child in tables)
                {
                    if (child.InnerHtml != null && child.InnerHtml.Contains("new PageTournament"))
                    {
                        seasonID = child.InnerHtml.Remove(0, 88).Substring(0, 8);
                        break;
                    }
                }
            }          

            for (int j = 1; j < 100; j++)
            {
                long unixTimestamp = (long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalMilliseconds;

                string gameID = "1";

                string lin = "https://fb.oddsportal.com/ajax-sport-country-tournament-archive/" + gameID + "/" + seasonID + "/X0X0X0X0X0X0X0X0X0X0X0X0X0X0X0X56X0X2097152X262144/0/0/" + j.ToString() + "/?_=" + unixTimestamp.ToString().Trim() + "";

                var requ = (HttpWebRequest)WebRequest.Create(lin);
                requ.UserAgent = userAgent;
                requ.Method = "GET";
                requ.Headers.Add("authority", "fb.oddsportal.com");
                requ.Referer = "https://www.oddsportal.com/";

                var respo = await requ.GetResponseAsync();

                using (StreamReader readStream = new StreamReader(respo.GetResponseStream()))
                {
                    string x = await readStream.ReadToEndAsync();

                    x = DecodeEncodedNonAsciiCharacters(x);
                    x = x.Remove(0, x.IndexOf(@"""" + "html" + @"""" + ":", StringComparison.InvariantCulture)).Remove(0, 8);
                    x = x.Replace("\\", "");
                    x = x.Replace(" /", "");
                    int resultIndex = x.IndexOf("}," + @"""" + "refresh" + @"""" + ":");
                    if (resultIndex != -1)
                    {
                        x = x.Substring(0, resultIndex);
                    }

                    var htmlDoc = HtmlDocumentFactory.Create();
                    htmlDoc.Write(x);

                    if (x.Contains("No data available") || x.Contains("Unfortunately, no matches"))
                    {
                        break; // break of pagination
                    }

                    HtmlElementCollection tables = htmlDoc.GetElementsByTagName("table");

                    foreach (HtmlElement table in tables)
                    {
                        if (table.GetAttribute("className").Contains(" table-main") && table.Id == "tournamentTable")
                        {
                            foreach (HtmlElement child in table.Children[1].Children)
                            {
                                if (child.GetAttribute("className").Contains("deactivate"))
                                {
                                    string link = "https://www.oddsportal.com/" + child.Children[1].Children[0].GetAttribute("href").Replace("about:/", "");

                                    var reqq = (HttpWebRequest)WebRequest.Create(link);
                                    reqq.UserAgent = userAgent;
                                    reqq.Headers.Add("authority", "fb.oddsportal.com");
                                    reqq.Referer = "https://www.oddsportal.com/";
                                    reqq.Method = "GET";

                                    using (var responce = (HttpWebResponse)await reqq.GetResponseAsync())
                                    {
                                        using (StreamReader rStream = new StreamReader(responce.GetResponseStream()))
                                        {
                                            x = await rStream.ReadToEndAsync();

                                            htmlDoc = HtmlDocumentFactory.Create();
                                            htmlDoc.Write(x);

                                            tables = htmlDoc.GetElementsByTagName("script");

                                            foreach (HtmlElement script in tables)
                                            {
                                                if (script.InnerHtml != null)
                                                {
                                                    if (script.InnerHtml.Contains("xhash"))
                                                    {
                                                        string pattern = @"xhash"":""(?<val>.*?)"",""xhashf";

                                                        RegexOptions options = RegexOptions.Compiled | RegexOptions.Singleline;
                                                        Regex regex = new Regex(pattern, options);
                                                        Match match = regex.Match(script.InnerHtml.ToString());
                                                        string result = "";
                                                        result += match.Groups["val"].Value;

                                                        string timeNow = UnixTimeNow().ToString();

                                                        string eventId = link.Split('/')[6].Split('-').Last().Trim();
                                                        var request = (HttpWebRequest)WebRequest.Create("https://fb.oddsportal.com/feed/match/1-1-" + eventId + "-1-2-" + unhash(result).ToString() + ".dat?_=" + timeNow);
                                                        request.UserAgent = userAgent;                                     
                                                        request.Method = "GET";
                                                        request.Headers.Add("origin", "https://www.pinnacle.com");
                                                        request.Headers.Add("authority", "fb.oddsportal.com");
                                                        request.Referer = "https://www.oddsportal.com/";

                                                        using (var response = (HttpWebResponse)await request.GetResponseAsync())
                                                        {
                                                            using (StreamReader stream = new StreamReader(response.GetResponseStream()))
                                                            {
                                                                string body = await stream.ReadToEndAsync();
                                                                string jsonBody = "";

                                                                // remove bad symbol
                                                                body = body.Remove(0, 64);
                                                                jsonBody = body.Remove(body.Length - 2, 2);

                                                                JObject varJson = JsonConvert.DeserializeObject<JObject>(jsonBody);

                                                                //HERE PARSE JSON
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        string unhash(string xhash)
        {
            string[] tmp = xhash.Split('%');
            string[] arrayCode = { tmp[1], tmp[2], tmp[3], tmp[4], tmp[5] };
            var chars = arrayCode.Select(s => ExtractChar(s, Encoding.UTF8));
            var result = new string(chars.ToArray());
            return result;
        }

        static string DecodeEncodedNonAsciiCharacters(string value)
        {
            return Regex.Replace(
                value,
                @"\\u(?<Value>[a-zA-Z0-9]{4})",
                m =>
                {
                    return ((char)int.Parse(m.Groups["Value"].Value, System.Globalization.NumberStyles.HexNumber)).ToString();
                });
        }
