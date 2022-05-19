﻿using System;

namespace Marsman.AzureSearchToolkit
{
    [AttributeUsage(AttributeTargets.Property)]
    public class SearchToolkitDisplayAttribute : Attribute
    {
        public DateTimeDisplayFormat DateTimeFormat { get; set; }

        /// <summary>
        /// Accepts any string that contains "0.000...0" where the number
        /// of zeroes is the number of decimal places wanted. e.g. "$0.00 USD" would
        /// render "$3.50 USD" if we gave it 3.504398433.
        /// <para>
        /// </para>
        /// <para>
        /// ...well, it was about that time I realised the format string was
        /// actually a 50ft tall creature from the paleozoeic era so I said
        /// get outta hear ya god damn Loch Ness monster I ain't giving you 
        /// no 3.504398433</para>
        /// </summary>
        public string NumberFormat { get; set; }

        public string DisplayName { get; set; }
    }

    public enum DateTimeDisplayFormat
    {
        DateTime,
        Date,
        Time
    }
}
