using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Text;
//using System.Windows.Forms;
//using System.Windows.UI.Popups;

namespace SeleniumMultipleSending
{
    class Program
    {
        IWebDriver driver;
        static readonly string doneFolderPath = @"C:\pa\wa2u\backup";
        static readonly string errorFolderPath = @"C:\pa\wa2u\error";
        static readonly string wa2uFilePath = @"C:\pa\wa2u\wa2u.txt";
        static List<string> fullPhoneNumberList = new List<string>();
        static List<string> tempMessageList = new List<string>();
        static string filePath;
        static string fullMessage;

        static void Main(string[] args)
        {
            try
            {
                PreprocessWa2u(wa2uFilePath);
                new Program().Run();
            }
            catch (Exception e)
            {
                string tempFileName = errorFolderPath + @"\error_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt";
                FileInfo fi = new FileInfo(tempFileName);

                using (FileStream sw = fi.Create())
                {
                    Byte[] info = new UTF8Encoding().GetBytes(DateTime.Now.ToString() + "\t" + e.Message + "\t" + e.StackTrace);
                    sw.Write(info);
                }
            }
        }

        private static void PreprocessWa2u(string wa2uFilePath)
        {
            string allWa2uInfo = File.ReadAllText(wa2uFilePath).Replace("\t", string.Empty);
            string[] tempWa2uInfoList = Regex.Split(allWa2uInfo, "#\r\n");

            //Make wa2u text into separated rows
            for (int i = 0; i < tempWa2uInfoList.Length; i++)
            {
                tempWa2uInfoList[i] = tempWa2uInfoList[i].Replace("\r", string.Empty).Replace("\n", string.Empty);
            }

            ProcessPhoneNumber(tempWa2uInfoList);
            ProcessTextMessage(tempWa2uInfoList);
            ProcessPdfPath(tempWa2uInfoList);
        }

        private static void ProcessPhoneNumber(string[] wa2uList)
        {
            // Assign phoneNumber as long list to static list
            foreach (string row in wa2uList) //19
            {
                if (row.Length > 0)
                {
                    string tempFirstThreeDigit = row.Substring(0, 3);
                    // If first three string is M30, then we use it
                    if (tempFirstThreeDigit == "M30")
                    {
                        string tempRow = "6" + row.Substring(4);
                        tempRow = tempRow.TrimEnd();
                        //long tempRowToNumber = long.Parse(tempRow);
                        fullPhoneNumberList.Add(tempRow);
                    }
                }
            }
        }

        private static void ProcessTextMessage(string[] wa2uList)
        {
            //TODO
            //List<string> tempMessageList = new List<string>();
            foreach (string row in wa2uList)
            {
                if (row.Length > 0)
                {
                    string tempFirstThreeDigit = row.Substring(0, 3);

                    if (tempFirstThreeDigit == "M40")
                    {
                        string tempRow = row.Substring(4);
                        tempMessageList.Add(tempRow);
                        //tempRow += Environment.NewLine;
                    }
                    //TODO
                    fullMessage = string.Join("\r\n", tempMessageList);
                }
            }
        }
        private static void ProcessPdfPath(string[] wa2uList)
        {
            foreach (string row in wa2uList)
            {
                if (row.Length > 0)
                {
                    string tempFirstThreeDigit = row.Substring(0, 3);
                    if (tempFirstThreeDigit == "M50")
                    {
                        string tempRow = row.Substring(4).Replace(" ", string.Empty);
                        filePath = tempRow;
                    }
                }
            }
        }


        private void Run()
        {
            try
            {
                // Open Chrome and then open whatsapp window
                driver = new ChromeDriver(System.IO.Directory.GetCurrentDirectory());
                driver.Navigate().GoToUrl("https://web.whatsapp.com");

                // Do login
                while (true)
                {
                    if (CheckLoggedIn())
                        break;
                }

                // For every phone number, they must send
                foreach (string phoneNum in fullPhoneNumberList)
                {
                    SendMessage(phoneNum);
                    SendPdf(phoneNum);
                    System.Threading.Thread.Sleep(9000);

                }
                while (true)
                {
                    try
                    {
                        //Ways to create the Windows pop-up message
                        //Cannot use Alert, Alert is something like refresh the Windows, that's it
                        IJavaScriptExecutor js = driver as IJavaScriptExecutor;
                        js.ExecuteScript("alert('Messages and pdf file successfully sent!');");
                        System.Threading.Thread.Sleep(5000);
                        //IAlert alert = driver.SwitchTo().Alert();
                        //alert.Accept();
                        //Console.Read();
                        break;
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                }
                    GenerateCompletedTaskBackup();

                    driver.Dispose();
            }
            catch
            {
                throw;
            }

        }

        private void GenerateCompletedTaskBackup()
        {
            File.Copy(wa2uFilePath, doneFolderPath + @"\wa2u_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt");  // \wa2u + _ + datetime.now.tostring() .txt
        }

        private bool CheckLoggedIn()
        {
            try
            {
                bool canFindElement = driver.FindElement(By.ClassName("_2zCfw")).Displayed;
                return canFindElement;
            }
            catch (Exception e)
            {
                Debug.Print(e.Message);
                return false;
            }

        }

        private void SendPdf(string phoneNumber)
        {
            try
            {
                int retries = 100;
                while (true)
                {
                    try
                    {
                        driver.FindElement(By.XPath("//span[@data-icon='clip']")).Click();
                        break;
                    }
                    catch
                    {
                        if (--retries == 0) throw;
                        System.Threading.Thread.Sleep(1000);
                    }
                }

                while (true)
                {
                    try
                    {
                        driver.FindElement(By.XPath("//input[@type='file']")).SendKeys(filePath);
                        break;
                    }
                    catch
                    {
                        if (--retries == 0) throw;
                        System.Threading.Thread.Sleep(1000);
                    }
                }

                while (true)
                {
                    try
                    {
                        driver.FindElement(By.XPath("//span[@data-icon='send-light']")).Click();
                        break;

                    }
                    catch
                    {
                        if (--retries == 0) throw;
                        else System.Threading.Thread.Sleep(1000);
                    }
                }

            }
            catch
            {
                throw;
            }
        }

        private void SendMessage(string phoneNumber)
        {
            try
            {
                int retries = 100;
                Actions action = new Actions(driver);
                driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10); //wait for maximun of 10 seconds if any element is not found
                driver.Navigate().GoToUrl("https://api.whatsapp.com/send?phone=" + phoneNumber + "&text=" + Uri.EscapeDataString(fullMessage));

                while (true)
                {
                    try
                    {
                        driver.FindElement(By.Id("action-button")).Click(); // Click SEND Buton
                        break;
                    }
                    catch
                    {
                        if (--retries == 0) throw;
                        System.Threading.Thread.Sleep(1000);
                    }
                }

                //foreach (string message in tempMessageList)
                //{
                //    while (true)
                //    {
                //        try
                //        {
                //            driver.FindElement(By.ClassName("_3u328")).SendKeys(message);
                //            //action.SendKeys(+Keys.Shift + Keys.Enter);
                //            //action.Perform();

                //            //action.KeyDown(Keys.Shift).SendKeys(Keys.Enter).KeyUp(Keys.Shift);

                //            //action.SendKeys(Keys.Enter);
                //            //action.KeyUp(Keys.Shift);


                //            //driver.FindElement(By.ClassName("_3u328")).SendKeys(message);
                //            //action.MoveToElement(driver.FindElement(By.ClassName("_3u328")))
                //            //.KeyDown(Keys.Shift)
                //            //.KeyDown(Keys.Shift)
                //            //.KeyUp(Keys.Enter)
                //            //.KeyUp(Keys.Shift);
                //            //action.Perform();
                //            break;
                //        }
                //        catch
                //        {
                //            if (--retries == 0) throw;
                //            System.Threading.Thread.Sleep(1000);
                //        }
                //    }
                //}

                while (true)
                {
                    try
                    {
                        driver.FindElement(By.CssSelector("button._3M-N-")).Click();
                        break;
                    }
                    catch
                    {
                        if (--retries == 0) throw;
                        System.Threading.Thread.Sleep(1000);
                    }

                }
            }
            catch
            {
                throw;
            }


        }
    }
}

