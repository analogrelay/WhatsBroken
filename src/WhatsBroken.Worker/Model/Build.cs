using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WhatsBroken.Worker.Model
{
    public class Build: AzDoEntity
    {
        public int Id { get; set; }
        public int PipelineId { get; set; }
        public string? BuildNumber { get; set; }
        public DateTime? FinishedDate { get; set; }

        public Pipeline? Pipeline { get; set; }
        public IList<TestRun>? TestRuns { get; set; }

        internal static void ConfigureModel(EntityTypeBuilder<Build> model)
        {
            AzDoEntity.ConfigureModel(model);

            model.HasKey(x => x.Id);
            model.Property(x => x.BuildNumber).IsRequired();

            model.HasMany(x => x.TestRuns)
                .WithOne(x => x.Build!)
                .HasForeignKey(x => x.BuildId);
        }
    }
}
