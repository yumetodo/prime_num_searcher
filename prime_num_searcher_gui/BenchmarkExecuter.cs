using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using OxyPlot.Series;

namespace prime_num_searcher_gui
{
    class BenchmarkExecuter
    {
        private CallCUI callCUI_;
        private static readonly Regex rx = new Regex(@"searcher_name:([a-w_]+).+,time\(ns\):(\d+)");
        public BenchmarkExecuter(string path)
        {
            callCUI_ = new CallCUI(path);
        }
        private async Task<List<KeyValuePair<string, UInt64>>> Run(UInt64 n)
            => (await callCUI_.Execute(n.ToString()))
            .Split('\n')
            .Select(s => rx.Match(s))
            .Where(m => 3 == m.Groups.Count)
            .Select(m => new KeyValuePair<string, UInt64>(m.Groups[1].Value, UInt64.Parse(m.Groups[2].Value)))
            .ToList();
        public static Dictionary<string, List<ScatterPoint>> CreateResultDictionary() => new Dictionary<string, List<ScatterPoint>> {
            {"sieve_of_eratosthenes", new List<ScatterPoint> { } },
            {"simple_algrism", new List<ScatterPoint> { } },
            {"simple_algrism_mt", new List<ScatterPoint> { } },
            {"forno_method", new List<ScatterPoint> { } }
        };
        public bool IsExecutable { get; private set; } = true;
        public async Task<Dictionary<string, List<ScatterPoint>>> Execute(UInt64 maxNum, UInt64 interval, Action<UInt64> onProgressChange)
        {
            var re = CreateResultDictionary();
            if (!IsExecutable) return re;
            IsExecutable = false;
            try
            {
                UInt64 count = 1;
                Action<UInt64, List<KeyValuePair<string, UInt64>>> convert = (UInt64 i, List<KeyValuePair<string, UInt64>> runResults) =>
                {
                    foreach (var p in runResults)
                    {
                        re[p.Key].Add(new ScatterPoint(i, p.Value) { Value = 0 });
                    }
                    onProgressChange(count);
                };
                for (UInt64 i = 2; i < maxNum; i += interval, ++count)
                {
                    convert(i, await this.Run(i));
                }
                convert(maxNum, await this.Run(maxNum));
            }
            finally
            {
                IsExecutable = true;
            }
            return re;
        }
    }
}
