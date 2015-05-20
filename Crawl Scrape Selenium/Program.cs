using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CsvHelper;
using OpenQA.Selenium;
using OpenQA.Selenium.PhantomJS;
using System.Threading;

namespace Crawl_Scrape_Selenium
{
    class Program
    {
        static void Main(string[] args)
        {
            var p = new Program();
            p.run();
        }

        // Selenium Driver = Browser
        private IWebDriver driver;

        // simple CSV helper
        private CsvWriter csv;
        private StreamWriter sw;

        private readonly DateTime start = DateTime.Now;
        private Random rand;
        readonly Regex linkParser = new Regex(@"\b(?:https?://|www\.)\S+\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // config params
        private const string LOGIN_URL = "";
        private const string LOGIN_USER = "";
        private const string LOGIN_PASS = "";
        private const string OUTPUT_FILE = @"output.csv";
        private const string CRAWL_ROOT = "";
        private const int SLEEP_MS_BASE = 5000;
        private const int SLEEP_MS_RAND = 15000;
        private const string STARTS_WITH_1 = "";
        private const string NOT_CONTAINS_1 = "";
        
        void run()
        {
            rand = new Random();

            // phantomjs is headless
            driver = new PhantomJSDriver(); // ChromeDriver();

            // implicit wait for the findelement function
            //driver.Manage().Timeouts().ImplicitlyWait(TimeSpan.FromSeconds(10));

            Console.WriteLine("go to login");
            driver.Navigate().GoToUrl(LOGIN_URL);

            Console.WriteLine("try login");
            var email = driver.FindElement(By.CssSelector("#email"));
            var pass = driver.FindElement(By.CssSelector("#password"));
            var submit = driver.FindElement(By.CssSelector(".large-submit"));

            email.SendKeys(LOGIN_USER);
            pass.SendKeys(LOGIN_PASS);
            submit.Click();
            Console.WriteLine("done login");

            // output
            sw = new StreamWriter(OUTPUT_FILE);
            csv = new CsvHelper.CsvWriter(sw);

            // crawl
            var allVisited = bfsVisit(CRAWL_ROOT);
            Console.WriteLine(allVisited.Count);

            // close
            sw.Close();
            driver.Close();
        }

        // fifo BFS, lifo DFS or random will do just fine
        HashSet<string> bfsVisit(string root)
        {
            var Q = new Stack<string>();
            var visited = new HashSet<string>();

            // begin
            Q.Push(root);
            visited.Add(root);

            // loop
            int seen = 0;
            while (Q.Count > 0)
            {
                seen++;
                string p = Q.Pop();
                driver.Navigate().GoToUrl(p);
                parsePage(p);

                Console.WriteLine(driver.Url + " " + seen + "/" + visited.Count + " " + (DateTime.Now - start).TotalMinutes);
                Thread.Sleep(SLEEP_MS_BASE + rand.Next(SLEEP_MS_RAND));
                try
                {
                    driver.Navigate().GoToUrl(p);
                    var children = getChildren();

                    foreach (var child in children)
                    {
                        if (!visited.Contains(child))
                        {
                            visited.Add(child);
                            Q.Push(child);
                        }
                    }
                }
                catch (Exception e)
                {
                    // server ban, link broken
                    Console.WriteLine(e);
                }
            }

            return visited;
        }

        // faster than using selenium's find tag + get href
        IEnumerable<string> getChildren()
        {
            var rawString = driver.PageSource;

            return (from Match m in linkParser.Matches(rawString) where isValid(m.Value) select m.Value).ToList();
        }

        static bool isValid(string link)
        {
            if (link == null)
                return false;
            if (!link.StartsWith(STARTS_WITH_1))
                return false;
            if (link.Contains(NOT_CONTAINS_1))
                return false;
            return true;
        }

        void parsePage(string url)
        {
            var name = "";
            try
            {
                name = driver.FindElement(By.CssSelector(".p-name")).Text;
            }
            catch (Exception)
            {
            }

            var price = "";
            try
            {
                price = driver.FindElement(By.CssSelector(".ps-sell-price")).Text;
            }
            catch (Exception)
            {
            }

            var code = "";
            try
            {
                code = driver.FindElement(By.CssSelector(".owncode")).Text;
            }
            catch (Exception)
            {
            }

            var available = "";
            try
            {
                available = driver.FindElement(By.CssSelector(".pi-availability.instock.tip.hint")).Text;
            }
            catch (Exception)
            {
            }

            var available2 = "";
            try
            {
                available2 = driver.FindElement(By.CssSelector(".pi-availability.insupplierstock.tip.hint")).Text;
            }
            catch (Exception)
            {
            }

            csv.NextRecord();
            csv.WriteField(url);
            csv.WriteField(code);
            csv.WriteField(name);
            csv.WriteField(price);
            csv.WriteField(available + ";" + available2);
            csv.WriteField(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sw.Flush();
        }
    }
}
