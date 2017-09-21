using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using OxyPlot.Series;
using System.Threading;

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
        public bool IsPaused { get; private set; } = false;
        private Dictionary<string, List<ScatterPoint>> tmpResult;
        private SemaphoreSlim notifyResume = new SemaphoreSlim(1,1);
        private bool notifyStop = false;
        private UInt64 currentMaxPointY = 0;
        private TimeUnitManager timeUnitManager = new TimeUnitManager { };
        public TimeUnit ResultTimeUnit { get => this.timeUnitManager.unit; }
        public async Task<Dictionary<string, List<ScatterPoint>>> Execute(UInt64 maxNum, UInt64 interval, Action<UInt64> onProgressChange)
        {
            if (!IsExecutable) return CreateResultDictionary();
            this.notifyStop = false;
            //clear previous result to activate PropatyChanged event
            this.tmpResult = CreateResultDictionary();
            IsExecutable = false;
            currentMaxPointY = 0;
            this.timeUnitManager.unit = TimeUnit.nanoseconds;
            try
            {
                UInt64 count = 1;
                Action<List<KeyValuePair<string, UInt64>>> timeUnitConvertWhenRequired = (List<KeyValuePair<string, UInt64>> runResults) =>
                {
                    this.currentMaxPointY = Math.Max(runResults.Max(p => p.Value), this.currentMaxPointY);
                    if (this.timeUnitManager.HasBiggerUnit && this.timeUnitManager.ShouldAdoptBiggerUnit(this.currentMaxPointY))
                    {
                        //convert required
                        var previousUnit = this.timeUnitManager.unit;
                        timeUnitManager.MakeUnitBigger();
                        var tmp = this.tmpResult
                            .Select(pair => new KeyValuePair<string, List<ScatterPoint>>(
                                pair.Key,
                                pair.Value
                                    .Select(p => new ScatterPoint(p.X, this.timeUnitManager.Convert(p.Y, previousUnit), value: p.Value))
                                    .ToList()
                            ))
                            .ToDictionary(x => x.Key, x => x.Value);
                        this.tmpResult = tmp;
                    }
                };
                Action<UInt64, List<KeyValuePair<string, UInt64>>> convert = (UInt64 i, List<KeyValuePair<string, UInt64>> runResults) =>
                {
                    try
                    {
                        foreach (var p in runResults)
                        {
                            this.tmpResult[p.Key].Add(new ScatterPoint(i, this.timeUnitManager.Convert(p.Value), value: 0));
                        }
                        timeUnitConvertWhenRequired(runResults);
                        onProgressChange(count);
                    }
                    finally
                    {
                        this.notifyResume.Release();
                    }
                };
                for (UInt64 i = 2; i < maxNum; i += interval, ++count)
                {
                    //When this.Resume() was called, this SemaphoreSlim will block until this.NotifyRestart() was called.
                    await notifyResume.WaitAsync().ConfigureAwait(false);
                    if (this.notifyStop)
                    {
                        this.notifyResume.Release();
                        return this.tmpResult;
                    }
                    convert(i, await this.Run(i));
                }
                //When this.Resume() was called, this SemaphoreSlim will block until this.NotifyRestart() was called.
                await notifyResume.WaitAsync().ConfigureAwait(false);
                convert(maxNum, await this.Run(maxNum));
            }
            finally
            {
                this.IsExecutable = true;
            }
            return this.tmpResult;
        }
        public async Task<Dictionary<string, List<ScatterPoint>>> Pasue()
        {
            if(IsExecutable) return CreateResultDictionary();
            this.IsPaused = true;
            await notifyResume.WaitAsync().ConfigureAwait(false);
            //to activate PropatyChanged event
            return this.tmpResult.ToList().ToDictionary(x => x.Key, x => x.Value.ToArray().ToList());
        }
        public void NotifyResume()
        {
            if (!IsExecutable && IsPaused)
            {
                notifyResume.Release();
                this.IsPaused = false;
            }
        }
        public void NotifyStop()
        {
            if (!IsExecutable)
            {
                //if(!IsPaused) await notifyResume.WaitAsync().ConfigureAwait(false);
                notifyStop = true;
                if (IsPaused) notifyResume.Release();
                this.IsPaused = false;
            }
        }
    }
}
