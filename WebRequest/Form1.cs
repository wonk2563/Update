using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;
using System.Threading;

namespace WebRequest
{
    public partial class Form1 : Form
    {
        string appPath = AppDomain.CurrentDomain.BaseDirectory;
        BackgroundWorker extractWorker = new BackgroundWorker();
        BackgroundWorker copyWorker = new BackgroundWorker();
        public Form1()
        {
            InitializeComponent();
            ReadVersionByTXT();
            WorkerInit();
        }

        private void WorkerInit()
        {
            extractWorker.WorkerSupportsCancellation = true;//是否支持異步取消
            extractWorker.WorkerReportsProgress = true;//能否報告進度更新
            extractWorker.ProgressChanged += BackgroundWorker_ProgressChanged;
            extractWorker.RunWorkerCompleted += BackgroundWorker_RunWorkerCompleted;
            extractWorker.DoWork += Worker_DoWork;

            copyWorker.WorkerSupportsCancellation = true;
            copyWorker.WorkerReportsProgress = true;
            copyWorker.ProgressChanged += CopyWorker_ProgressChanged;
            copyWorker.RunWorkerCompleted += CopyWorker_RunWorkerCompleted;
            copyWorker.DoWork += CopyWorker_DoWork;
        }

        private void Delete_All_File()
        {
            //刪除data資料夾下，除了cheakURI.txt以外的檔案
            string[] files = System.IO.Directory.GetFiles(appEXpath + "data/");
            foreach(string file in files)
            {
                if(file != "cheakURI.txt")
                    File.Delete("data/" + file);
            }
        }



        //----------------------------讀取本地檔案-------------------------------------
        string nowVersion = "", cheakURI = "";
        private void ReadVersionByTXT()
        {
            string path = appPath + "version.txt";
            string uriPath = appPath + "data/cheakURI.txt";
            if (System.IO.File.Exists(path))
            {
                nowVersion = File.ReadAllText(@path, Encoding.UTF8).Replace("\n", "").Replace(" ", "").Replace("\t", "").Replace("\r", "");
                Console.WriteLine(nowVersion);
            }
            else
            {
                MessageBox.Show("無法獲取當前版本資訊","錯誤");
            }

            if (System.IO.File.Exists(uriPath))
            {
                cheakURI = File.ReadAllText(@uriPath, Encoding.UTF8);
            }
            else
            {
                MessageBox.Show("無法獲取更新資訊", "錯誤");
            }

            if (cheakURI != "")
                DownloadStreamString(cheakURI);
            else
                MessageBox.Show("無法獲取更新資訊", "錯誤");
        }

        //----------------------------確認更新-------------------------------------
        string[] updateInfo, fixInfo;
        string lastVersion = "", updateURI = "" , appEXpath = "";        
        public void DownloadStreamString(string url)
        {
            LB_nowRunning.Text = "獲取更新資訊中...";
            try
            {
                WebClient wc = new WebClient();
                wc.DownloadProgressChanged += client_DownloadProgressChanged;
                wc.DownloadFileCompleted += update_DownloadFileCompleted;
                wc.DownloadFileAsync(new Uri(url), "data/updateinfo.txt");
            }
            catch (Exception ex)
            {
                MessageBox.Show("獲取更新資訊失敗" + "\r\n" + ex.Message, "錯誤");
                Application.Exit();
            }            
        }

        string zipPath = "data/update.zip";
        private void update_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            LB_nowRunning.Text = "獲取更新資訊完成";
            updateInfo = File.ReadAllLines("data/updateinfo.txt", Encoding.UTF8);
            string fix = "";
            lastVersion = updateInfo[0];
            appEXpath = "data/" + updateInfo[1];
            updateURI = updateInfo[updateInfo.Length - 1];
            if(updateInfo.Length-3 > 0)
            {
                fixInfo = new string[updateInfo.Length - 3];
                for (int i = 0; i < updateInfo.Length - 3; i++)                
                    fixInfo[i] = updateInfo[i + 2];                
                fix = String.Join("\n", fixInfo);
            }
            if (fix == "")
                fix = "無";

            if (int.Parse(nowVersion.Replace(".", "")) < int.Parse(lastVersion.Replace(".", "")))
            {              
                label1.Text = $"\n目前版本：{nowVersion}　更新版本：{lastVersion}\n\n" +
                    $"更新內容：\n" +
                    $"{fix}\n\n";

                if (updateURI != "")
                    try
                    {
                        DownloadFile(updateURI, zipPath);
                    }
                    catch (Exception)
                    {
                        MessageBox.Show($"無法獲取更新包下載位址", "錯誤");
                    }                    
            }
            else
            {
                label1.Text = $"\n目前版本：{nowVersion}　更新版本：{lastVersion}\n\n" +
                    $"更新內容：\n" +
                    $"{fix}\n\n";
                MessageBox.Show($"目前為最新版本", "檢查更新");
            }
        }

        //----------------------------下載更新-------------------------------------       
        public void DownloadFile(string url, string filename)
        {
            LB_nowRunning.Text = "下載更新檔案中...";
            try
            {
                WebClient wc = new WebClient();
                wc.DownloadProgressChanged += client_DownloadProgressChanged;
                wc.DownloadFileCompleted += client_DownloadFileCompleted;
                wc.DownloadFileAsync(new Uri(url), filename);
            }
            catch (Exception ex)
            {
                Delete_All_File();
                MessageBox.Show("下載更新檔案失敗" + "\r\n" + ex.Message, "錯誤");
                Application.Exit();
            }
        }

        private void client_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            LB_nowRunning.Text = "更新檔案下載完成";
            ExtractUpdaeZIP();
        }

        private void client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            this.proBarDownLoad.Minimum = 0;
            this.proBarDownLoad.Maximum = (int)e.TotalBytesToReceive;
            this.proBarDownLoad.Value = (int)e.BytesReceived;
            this.lblPercent.Text = e.ProgressPercentage + "%";
        }
        

        //----------------------------解壓縮更新------------------------------------- 
        bool isDEL = false;
        private void ExtractUpdaeZIP()
        {
            LB_nowRunning.Text = "更新檔案解壓縮中...";
            extractWorker.RunWorkerAsync();
            try
            {
                if (!Directory.Exists(appEXpath))
                {
                    isDEL = true;
                    if (File.Exists(zipPath))
                        System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, "data/");
                }
                else
                {
                    Directory.Delete(appEXpath, true);
                    isDEL = true;
                    if (File.Exists(zipPath))
                        System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, "data/");
                }                    
            }
            catch (Exception ex)
            {
                Delete_All_File();
                MessageBox.Show("更新檔案解壓縮失敗" + "\r\n" + ex.Message,"錯誤");
                Application.Exit();
            }            
        }

        int value = 0;
        int uSize = 0;
        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            Thread.Sleep(1000);
            //計算壓縮檔內的檔案數量
            using (ZipFile zFile = new ZipFile(zipPath))
            {                
                foreach (ZipEntry c in zFile)
                {
                    if (c.IsFile)
                    {                        
                        uSize += 1;
                    }
                }
            }            
            //等待資料夾刪除完成
            while (!isDEL)
            {
                if (isDEL)
                    break;
            }

            //計算進度並顯示
            while (true)
            {
                DirectoryInfo dirInfo = new DirectoryInfo(appEXpath);
                int fileLen = dirInfo.GetFiles("*", SearchOption.AllDirectories).Length;
                if(fileLen != 0)
                    value = 100 * (uSize / fileLen);
                extractWorker.ReportProgress(value);

                if (value >= 100)
                {
                    break;
                }                    
            }
        }

        private void BackgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            this.proBarDownLoad.Minimum = 0;
            this.proBarDownLoad.Maximum = 100;
            this.proBarDownLoad.Value = e.ProgressPercentage;
            this.lblPercent.Text = e.ProgressPercentage + "%";
        }

        private void BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            LB_nowRunning.Text = "更新檔案解壓縮完成";
            extractWorker.CancelAsync();
            Thread.Sleep(200);
            LB_nowRunning.Text = "更新檔案複製中...";
            copyWorker.RunWorkerAsync();            
        }


        //----------------------------複製檔案-------------------------------------
        private void CopyWorker_DoWork(object sender, DoWorkEventArgs e)
        {            
            try
            {
                if (System.IO.Directory.Exists(appEXpath))
                {
                    string[] files = System.IO.Directory.GetFiles(appEXpath);
                    int count = 0;

                    //關閉要更新的檔案
                    foreach (string file in files)
                    {
                        string[] words = file.Split('\\');
                        Console.WriteLine(words[words.Length - 1]);
                        System.Diagnostics.Process[] proc = System.Diagnostics.Process.GetProcessesByName(words[words.Length - 1].Split('.')[0]);
                        foreach (System.Diagnostics.Process pro in proc)
                        {
                            pro.Kill();
                        }
                    }

                    Thread.Sleep(500);

                    // Copy the files and overwrite destination files if they already exist.
                    foreach (string s in files)
                    {
                        // Use static Path methods to extract only the file name from the path.
                        string fileName = Path.GetFileName(s);
                        string destFile = Path.Combine("", fileName);
                        File.Copy(s, destFile, true);

                        count++;
                        value = 100 * (files.Length - count);
                        copyWorker.ReportProgress(value);
                    }
                }
            }
            catch (Exception ex)
            {
                Delete_All_File();
                MessageBox.Show("更新檔案複製失敗" + "\r\n" + ex.Message, "錯誤");
                Application.Exit();
            }            
        }
        private void CopyWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            this.proBarDownLoad.Value = e.ProgressPercentage;
            this.lblPercent.Text = e.ProgressPercentage + "%";
        }

        private void CopyWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            LB_nowRunning.Text = "更新檔案複製完成";
            try
            {
                Directory.Delete(appEXpath, true);
                File.Delete(zipPath);
                File.Delete("data/updateinfo.txt");
            }
            catch (Exception ex)
            {
                MessageBox.Show("更新檔案刪除失敗" + "\r\n" + ex.Message, "錯誤");
                Application.Exit();
            }
            this.proBarDownLoad.Value = 100;
            this.lblPercent.Text = "100%";
            LB_nowRunning.Text = "更新完成";
        }        
    }
}
