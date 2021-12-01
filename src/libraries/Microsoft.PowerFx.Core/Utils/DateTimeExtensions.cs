using System;

namespace Microsoft.PowerFx.Core.Utils
{
    internal static class DateTimeExtensions
    {
        private static readonly DateTime JavaScriptEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Convert a JavaScript date value to a DateTime object.
        public static DateTime FromJavaScriptDate(double value)
        {
            // MSDN: The value represents the number of milliseconds in Universal Coordinated Time between the
            // specified date and midnight January 1, 1970.

            const int MillisecondsPerMinute = 60 * 1000;
            const int MillisecondsPerHour = 60 * MillisecondsPerMinute;
            const int MillisecondsPerDay = 24 * MillisecondsPerHour;

            return JavaScriptEpoch +
                   new TimeSpan(
                       (int)(value / MillisecondsPerDay),
                       (int)(value % MillisecondsPerDay) / MillisecondsPerHour,
                       (int)(value % MillisecondsPerHour) / MillisecondsPerMinute,
                       (int)(value % MillisecondsPerMinute) / 1000);
        }

        // Convert a DateTime object to a JavaScript date value by calculating the number of
        // milliseconds from the JavaScriptEpoch. Since the reference date is in
        // UTC, when we calculate difference we need to ensure the input DateTime object is also UTC.
        public static double ToJavaScriptDate(this DateTime dt)
        {
            return (dt.ToUniversalTime() - JavaScriptEpoch).TotalMilliseconds;
        }

        // Convert an OLE Automation date (Excel date format) to a DateTime.
        public static bool TryFromOADate(double value, out DateTime result)
        {
            // From MSDN: double-precision floating-point number that represents a date as the number of days before or after the base date,
            // midnight, 30 December 1899. The sign and integral part of d encode the date as a positive or negative day displacement from
            // 30 December 1899, and the absolute value of the fractional part of d encodes the time of day as a fraction of a day displacement
            // from midnight. d must be a value between negative 657435.0 through positive 2958465.99999999.
            // Note that because of the way dates are encoded, there are two ways of representing any time of day on 30 December 1899.
            // For example, -0.5 and 0.5 both mean noon on 30 December 1899 because a day displacement of plus or minus zero days from the
            // base date is still the base date, and a half day displacement from midnight is noon.
            // See ToOADate and the MSDN Online Library at http://MSDN.Microsoft.com/library/default.asp for more information on OLE Automation.

            double integral = Math.Truncate(value);
            double frac = Math.Abs(value) - Math.Abs(integral);

            const int HoursPerDay = 24;
            const int MinutesPerDay = 24 * 60;
            const int SecondsPerDay = 24 * 3600;

            try
            {
                result = OleAutomationEpoch + new TimeSpan((int)integral, (int)(frac * HoursPerDay) % HoursPerDay, (int)(frac * MinutesPerDay) % 60, (int)(frac * SecondsPerDay) % 60);

                // Excel stores dates as if they were always UTC, we need to treat as local time.
                result = System.DateTime.SpecifyKind(result, DateTimeKind.Local).ToUniversalTime();
            }
            catch (ArgumentOutOfRangeException)
            {
                // Value could be invalid for TimeSpan.
                result = default(DateTime);
                return false;
            }
            return true;
        }

        internal static readonly DateTime OleAutomationEpoch = new DateTime(1899, 12, 30, 0, 0, 0);
    }
}