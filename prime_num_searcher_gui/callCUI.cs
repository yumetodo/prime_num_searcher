using System;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

namespace prime_num_searcher_gui
{
    class CallCUI
    {
        private string programPath;
        private Process p;
        private bool processRunning;
        public CallCUI(string path)
        {
            var fullpath = Path.GetFullPath(path);
            if (!File.Exists(fullpath))
            {
                throw new FileNotFoundException("Fail to find CUI runtime");
            }
            programPath = fullpath;
            processRunning = false;
        }
        public async Task<string> Execute(params string[] args)
        {
            ProcessStartInfo psInfo = new ProcessStartInfo
            {
                FileName = programPath,
                CreateNoWindow = true,//コンソール・ウィンドウを開かない
                UseShellExecute = false,//シェル機能を使用しない
                RedirectStandardOutput = true,//標準出力を取り込むようにする
                Arguments = string.Join(" ", args)//コマンドライン引数を設定
            };

            p = new Process { StartInfo = psInfo };
            p.Exited += (object sender, EventArgs e) => { processRunning = false; };
            processRunning = true;
            try
            {
                p.Start();
            }
            catch(Exception)
            {
                processRunning = false;
                throw;
            }
            return await Task.Run(() => p.StandardOutput.ReadToEnd());
        }
        public void Kill()
        {
            if (processRunning)
            {
                p.Kill();
                processRunning = false;
            }
        }
    }
}
