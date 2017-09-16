using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.ComponentModel;

namespace prime_num_searcher_gui
{
    //ref:
    //https://github.com/mono/mono/blob/0bcbe39b148bb498742fc68416f8293ccd350fb6/mcs/class/referencesource/System.ComponentModel.DataAnnotations/DataAnnotations/RangeAttribute.cs
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    class NumericRangeAttribute : ValidationAttribute
    {
        /// <summary>
        /// Gets the maximum value for the range
        /// </summary>
        public object Maximum { get; private set; }
        /// <summary>
        /// Gets the minimum value for the range
        /// </summary>
        public object Minimum { get; private set; }
        /// <summary>
        /// Gets the type of the <see cref="Minimum"/> and <see cref="Maximum"/> values (e.g. Int32, Double, or some custom type)
        /// </summary>
        public Type OperandType { get; private set; }
        public NumericRangeAttribute(Type t, object min, object max) : base()
        {
            this.OperandType = t;
            this.Minimum = min;
            this.Maximum = max;
        }
        /// <summary>
        /// Constructor that takes integer minimum and maximum values
        /// </summary>
        /// <param name="min">The minimum value, inclusive</param>
        /// <param name="max">The maximum value, inclusive</param>
        public NumericRangeAttribute(UInt64 min, UInt64 max) : this(typeof(UInt64), min, max) { }
        /// <summary>
        /// Constructor that takes integer minimum and maximum values
        /// </summary>
        /// <param name="min">The minimum value, inclusive</param>
        /// <param name="max">The maximum value, inclusive</param>
        public NumericRangeAttribute(Int64 min, Int64 max)   : this(typeof(Int64), min, max)  { }
        /// <summary>
        /// Constructor that takes integer minimum and maximum values
        /// </summary>
        /// <param name="min">The minimum value, inclusive</param>
        /// <param name="max">The maximum value, inclusive</param>
        public NumericRangeAttribute(UInt32 min, UInt32 max) : this(typeof(UInt32), min, max) { }
        /// <summary>
        /// Constructor that takes integer minimum and maximum values
        /// </summary>
        /// <param name="min">The minimum value, inclusive</param>
        /// <param name="max">The maximum value, inclusive</param>
        public NumericRangeAttribute(Int32 min, Int32 max)   : this(typeof(Int32), min, max)  { }
        /// <summary>
        /// Constructor that takes integer minimum and maximum values
        /// </summary>
        /// <param name="min">The minimum value, inclusive</param>
        /// <param name="max">The maximum value, inclusive</param>
        public NumericRangeAttribute(UInt16 min, UInt16 max) : this(typeof(UInt16), min, max) { }
        /// <summary>
        /// Constructor that takes integer minimum and maximum values
        /// </summary>
        /// <param name="min">The minimum value, inclusive</param>
        /// <param name="max">The maximum value, inclusive</param>
        public NumericRangeAttribute(Int16 min, Int16 max)   : this(typeof(Int16), min, max)  { }
        /// <summary>
        /// Constructor that takes integer minimum and maximum values
        /// </summary>
        /// <param name="min">The minimum value, inclusive</param>
        /// <param name="max">The maximum value, inclusive</param>
        public NumericRangeAttribute(byte min, byte max) : this(typeof(byte), min, max) { }
        /// <summary>
        /// Constructor that takes integer minimum and maximum values
        /// </summary>
        /// <param name="min">The minimum value, inclusive</param>
        /// <param name="max">The maximum value, inclusive</param>
        public NumericRangeAttribute(sbyte min, sbyte max)   : this(typeof(sbyte), min, max)  { }
        /// <summary>
        /// Constructor that takes integer minimum and maximum values
        /// </summary>
        /// <param name="min">The minimum value, inclusive</param>
        /// <param name="max">The maximum value, inclusive</param>
        public NumericRangeAttribute(float min, float max) : this(typeof(float), min, max) { }
        /// <summary>
        /// Constructor that takes integer minimum and maximum values
        /// </summary>
        /// <param name="min">The minimum value, inclusive</param>
        /// <param name="max">The maximum value, inclusive</param>
        public NumericRangeAttribute(double min, double max) : this(typeof(double), min, max) { }
        /// <summary>
        /// Override of <see cref="ValidationAttribute.FormatErrorMessage"/>
        /// </summary>
        /// <remarks>This override exists to provide a formatted message describing the minimum and maximum values</remarks>
        /// <param name="name">The user-visible name to include in the formatted message.</param>
        /// <returns>A localized string describing the minimum and maximum values</returns>
        public override string FormatErrorMessage(string name) => String.Format(CultureInfo.CurrentCulture, ErrorMessageString, name, this.Minimum, this.Maximum);
        /// <summary>
        /// Returns true if the value falls between min and max, inclusive.
        /// </summary>
        /// <param name="value">The value to test for validity.</param>
        /// <returns><c>true</c> means the <paramref name="value"/> is valid</returns>
        public override bool IsValid(object value)
        {
            // Automatically pass if value is null or empty. RequiredAttribute should be used to assert a value is not empty.
            if (value == null) return true;
            if (this.OperandType != value.GetType()) return false;
            IComparable min = (IComparable)this.Minimum;
            IComparable max = (IComparable)this.Maximum;
            return min.CompareTo(value) <= 0 && max.CompareTo(value) >= 0;
        }
    }
}
