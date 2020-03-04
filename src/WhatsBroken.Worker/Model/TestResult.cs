using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace WhatsBroken.Worker.Model
{
    public class TestResult
    {
        public int Id { get; set; }
        public int RunId { get; set; }
        public int CaseId { get; set; }
        public string? Outcome { get; set; }

        public TestRun? Run { get; set; }
        public TestCase? Case { get; set; }
        public TestResultDetail? Details { get; set; }

        internal static void ConfigureModel(EntityTypeBuilder<TestResult> modl)
        {
            modl.HasKey(x => x.Id);
            modl.Property(x => x.Outcome).IsRequired();

            modl.HasOne(x => x.Run)
                .WithMany(x => x!.Results)
                .HasForeignKey(r => r.RunId);
            modl.HasOne(x => x.Case)
                .WithMany(x => x!.Results)
                .HasForeignKey(r => r.CaseId);
        }
    }
}
