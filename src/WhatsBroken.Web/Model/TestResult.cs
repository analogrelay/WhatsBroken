using System;

namespace WhatsBroken.Web.Model
{
    public class TestResult
    {
        private string _project = default!;

        public long JobId { get; set; }
        public long WorkItemId { get; set; }
        public string AzDoProject { get; set; } = default!;
        public string BuildId { get; set; } = default!;
        public string BuildNumber { get; set; } = default!;
        public string BuildDefinition { get; set; } = default!;
        public string RunType { get; set; } = default!;
        public DateTime? Started { get; set; }
        public DateTime? Finished { get; set; }
        public string ConsoleUri { get; set; } = default!;
        public string Uri { get; set; } = default!;
        public string QueueName { get; set; } = default!;
        public string Project
        {
            get => _project;
            set => _project = SanitizeProject(value);
        }

        public string Type { get; set; } = default!;
        public string Method { get; set; } = default!;
        public string Arguments { get; set; } = default!;
        public string ArgumentHash { get; set; } = default!;
        public string Result { get; set; } = default!;
        public double? Duration { get; set; }
        public string Exception { get; set; } = default!;
        public string Message { get; set; } = default!;
        public string StackTrace { get; set; } = default!;
        public string Traits { get; set; } = default!;
        public bool IsQuarantined { get; set; }
        public string SkipReason { get; set; } = default!;

        private string SanitizeProject(string value)
        {
            // Do a little data clean-up
            if(value.Contains("-netcoreapp"))
            {
                var splat = value.Split('-');
                return string.Join("-", splat[0..^1]);
            }
            return value;
        }
    }
}
