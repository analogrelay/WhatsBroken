using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WhatsBroken.Worker.Model
{
    public class TestResultDetail
    {
        public int TestResultId { get; set; }
        public string? WebUrl { get; set; }
        public string? SkipReason { get; set; }
        public string? Message { get; set; }
        public string? StackTrace { get; set; }

        public TestResult? TestResult { get; set; }

        internal static void ConfigureModel(EntityTypeBuilder<TestResultDetail> modl)
        {
            modl.HasKey(x => x.TestResultId);

            modl.HasOne(x => x.TestResult)
                .WithOne(x => x!.Details!)
                .HasForeignKey<TestResultDetail>(x => x.TestResultId);
        }
    }
}
