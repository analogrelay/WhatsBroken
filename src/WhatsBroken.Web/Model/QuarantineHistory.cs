using System;

namespace WhatsBroken.Web.Model
{
    public class QuarantineHistory
    {
        public string Project { get; set; } = default!;
        public string Type { get; set; } = default!;
        public string Method { get; set; } = default!;
        public string Arguments { get; set; } = default!;
        public string ArgumentHash { get; set; } = default!;
        public long PassingRuns { get; set; }
        public long FailingRuns { get; set; }
        public long SkippedRuns { get; set; }
        public long TotalRuns { get; set; }
        public DateTime? FirstRun { get; set; }
        public DateTime? LastRun { get; set; }
        public DateTime? FirstFailure { get; set; }
        public DateTime? LastFailure { get; set; }
        public double PassingRate { get; set; }

        public TestCase TestCase => new TestCase(Project, Type, Method, Arguments, ArgumentHash);
    }
}
