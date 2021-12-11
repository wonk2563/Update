using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


class Tools
{
    private string _message;
    /// <summary>
    /// 錯誤訊息
    /// </summary>
    public string Message
    {
        get { return _message; }
    }

    int _completeFiles = 0;
    public int completeFiles
    {
        get { return _completeFiles; }
    }

    /// <summary>
    /// 複製檔案群
    /// </summary>
    /// <param name="_source">來源資料夾</param>
    /// <param name="_target">目的資料夾</param>
    /// <param name="recursive">包含子資料夾</param>
    /// <returns>成功 = 1 , 失敗 = -1</returns>    
    public int Copy(string _source, string _target, bool recursive = true)
    {
        try
        {
            if (!Directory.Exists(_source))
            {
                _message = "來源資料夾不存在!";
                return -1;
            }
            if (!Directory.Exists(_target))
            {
                // 目的資料夾不存在就建立
                Directory.CreateDirectory(_target);
            }

            // 先取得來源目露的所有檔案
            string[] files = Directory.GetFiles(_source);
            foreach (var fi in files)
            {
                string fn = Path.GetFileName(fi);
                // 複製檔案併覆蓋目的檔案
                File.Copy(fi, Path.Combine(_target, fn), true);
                _completeFiles++;
            }
        }
        catch (Exception e)
        {
            _message = e.Message;
            return -1;
        }

        //繼續處理子目錄
        if (recursive)
        {
            // 取得子目錄
            string[] dirs = Directory.GetDirectories(_source);
            if(dirs.Length != 0)
            {
                foreach (var di in dirs)
                {
                    string dn = Path.GetFileName(di);
                    // 遞迴呼叫
                    Tools x = new Tools();
                    if (x.Copy(di, Path.Combine(_target, dn)) == -1)
                    {
                        _message = x.Message;
                        return -1;
                    }
                    _completeFiles++;
                }
            }            
        }

        return 1;
    }
}

