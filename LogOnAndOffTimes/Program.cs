using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace LogOnAndOffTimes
{
    class Program
    {
        static void Main(string[] args)
        {
            const int lockId = 4800;
            const int unlockId = 4801;

            var securityLogs = new EventLog("Security");
            Console.WriteLine("Total security log entries: {0}", securityLogs.Entries.Count);

            var endDay = DateTime.Now;
            var beginningDay = DateTime.Now;

            var dateRange = GetStartAndEndFromArgs(args);
            if (dateRange != null)
            {
                beginningDay = dateRange[0];
                endDay = dateRange[1];
            }
            else
            {
                endDay = GetEndDay(endDay);
                beginningDay = GetBeginningDay(beginningDay, endDay);
            }

            var logOnOffEvents =
                securityLogs.Entries.Cast<EventLogEntry>()
                    .Where(
                        entry =>
                            entry.InstanceId == lockId ||
                            entry.InstanceId == unlockId && entry.TimeGenerated >= beginningDay &&
                            entry.TimeGenerated < endDay)
                    .OrderBy(entry => entry.TimeGenerated)
                    .ToArray();

            Console.WriteLine("Log on and off events: {0}", logOnOffEvents.Length);

            var startOfDay = DateTime.MinValue;
            var endOfDay = DateTime.MinValue;
            foreach (var entry in logOnOffEvents)
            {
                if (startOfDay == DateTime.MinValue)
                {
                    if (entry.InstanceId == unlockId)
                    {
                        startOfDay = entry.TimeGenerated;
                    }

                    continue;
                }

                if (endOfDay != DateTime.MinValue && entry.TimeGenerated.Day != endOfDay.Day)
                {
                    Console.WriteLine("{0} - {1} =\t{2}", startOfDay, endOfDay, endOfDay - startOfDay);

                    startOfDay = entry.InstanceId == unlockId ? entry.TimeGenerated : DateTime.MinValue;
                    endOfDay = DateTime.MinValue;
                }
                else if (entry.InstanceId == lockId)
                {
                    endOfDay = entry.TimeGenerated;
                }
            }


            Console.Write("{0}Press any ENTER to close the program... ", Environment.NewLine);
            Console.ReadLine();
        }

        private static List<DateTime> GetStartAndEndFromArgs(string[] args)
        {
            if (args.Length < 2)
            {
                return null;
            }

            var dateRange = new List<DateTime>();
            for (var a = 0; a < 2; a++)
            {
                var dateSplit = args[a].Split('-');
                if (dateSplit.Length < 2)
                {
                    Console.WriteLine("Error: Could not parse date argument. Expected format is XX-XX");
                }
                else
                {
                    int startArgDay;
                    int startArgMonth;
                    if (!int.TryParse(dateSplit[0], out startArgMonth) ||
                        !int.TryParse(dateSplit[1], out startArgDay))
                    {
                        continue;
                    }

                    var date = DateTime.Today;
                    for (var i = 0; i < 366; i++)
                    {
                        if (date.Day != startArgDay || date.Month != startArgMonth)
                        {
                            if (i == 365)
                            {
                                Console.WriteLine("Error: Could not find day matching {0}-{1}", startArgMonth,
                                    startArgDay);
                                break;
                            }

                            date = date.AddDays(-1);
                        }
                        else
                        {
                            dateRange.Add(date);
                        }
                    }
                }
            }

            if (dateRange.Count < 2 || dateRange[0] > dateRange[1])
            {
                return null;
            }

            dateRange[1] = dateRange[1].AddDays(1);

            return dateRange;
        }

        private static DateTime GetBeginningDay(DateTime beginningDay, DateTime endDay)
        {
            for (;
                beginningDay.DayOfWeek != DayOfWeek.Monday || beginningDay >= endDay;
                beginningDay = beginningDay.AddDays(-1))
            {
            }
            beginningDay = beginningDay.AddHours(-beginningDay.Hour);
            beginningDay = beginningDay.AddMinutes(-beginningDay.Minute);
            beginningDay = beginningDay.AddSeconds(-beginningDay.Second);
            beginningDay = beginningDay.AddMilliseconds(-beginningDay.Millisecond);
            return beginningDay;
        }

        private static DateTime GetEndDay(DateTime endDay)
        {
            for (; endDay.DayOfWeek != DayOfWeek.Monday; endDay = endDay.AddDays(-1))
            {
            }
            endDay = endDay.AddHours(-endDay.Hour - 1);
            endDay = endDay.AddMinutes(-endDay.Minute);
            endDay = endDay.AddSeconds(-endDay.Second);
            endDay = endDay.AddMilliseconds(-endDay.Millisecond);
            return endDay;
        }
    }
}
