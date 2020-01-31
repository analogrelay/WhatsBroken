using System;

namespace WhatsBroken.Web
{
    static class DateHelper
    {
        public static DateTime StartOfWeek(DateTime date)
        {
            // Strip off the time
            date = date.Date;
            var dayDistance = DayOfWeek.Sunday - date.DayOfWeek;
            return date.Add(TimeSpan.FromDays(dayDistance));
        }
    }
}
