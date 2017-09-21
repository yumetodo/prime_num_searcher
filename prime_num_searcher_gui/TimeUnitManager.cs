using System;

namespace prime_num_searcher_gui
{
    enum TimeUnit : UInt64
    {
        nanoseconds = 1000000000,//10^9
        microseconds = 1000000,//10^6
        milliseconds = 1000,//10^3
        seconds = 1
    }
    static class TimeUnitEx
    {
        public static string ToUnitString(this TimeUnit unit)
            => (TimeUnit.nanoseconds == unit) ? "nano sec."
            : (TimeUnit.microseconds == unit) ? "micro sec."
            : (TimeUnit.milliseconds == unit) ? "mili sec."
            : "sec.";
    }
    class TimeUnitManager
    {
        public TimeUnitManager(TimeUnit u = TimeUnit.nanoseconds) { this.unit = u; }
        public TimeUnit unit;
        public string UnitString { get => this.unit.ToUnitString(); }
        public bool HasBiggerUnit { get => this.unit != TimeUnit.seconds; }
        public TimeUnit BiggerUnit {
            get => (TimeUnit.nanoseconds == unit) ? TimeUnit.microseconds
                : (TimeUnit.microseconds == unit) ? TimeUnit.milliseconds
                : TimeUnit.seconds;
        }
        public double Convert(double in_time, TimeUnit in_unit = TimeUnit.nanoseconds)
            => (in_unit < unit) ? in_time * ((UInt64)unit / (UInt64)in_unit)
            : in_time / ((UInt64)in_unit / (UInt64)unit);
        public void MakeUnitBigger()
        {
            this.unit = this.BiggerUnit;
        }
        public bool ShouldAdoptBiggerUnit(UInt64 current) => (20 * ((UInt64)TimeUnit.nanoseconds / (UInt64)this.BiggerUnit)) < current;
    }
}
