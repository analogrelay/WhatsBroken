using Microsoft.EntityFrameworkCore;

namespace WhatsBroken.Worker.Model
{
    public class WhatsBrokenDbContext: DbContext
    {
        public DbSet<Pipeline> Pipelines { get; set; } = default!;
        public DbSet<TestCase> TestCases { get; set; } = default!;
        public DbSet<Build> Builds { get; set; } = default!;
        public DbSet<TestRun> TestRuns { get; set; } = default!;
        public DbSet<TestResult> TestResults { get; set; } = default!;
        public DbSet<TestResultDetail> TestResultDetails { get; set; } = default!;

        public WhatsBrokenDbContext(DbContextOptions options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Pipeline>(Pipeline.ConfigureModel);
            modelBuilder.Entity<TestCase>(TestCase.ConfigureModel);
            modelBuilder.Entity<Build>(Build.ConfigureModel);
            modelBuilder.Entity<TestRun>(TestRun.ConfigureModel);
            modelBuilder.Entity<TestResult>(TestResult.ConfigureModel);
            modelBuilder.Entity<TestResultDetail>(TestResultDetail.ConfigureModel);
        }
    }
}
