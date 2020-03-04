using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WhatsBroken.Worker.Model
{
    public class TestRun: AzDoEntity
    {
        public int Id { get; set; }
        public int BuildId { get; set; }
        public string? Name { get; set; }
        public string? Type { get; set; }

        public Build? Build { get; set; }
        public IList<TestResult>? Results { get; set; }

        internal static void ConfigureModel(EntityTypeBuilder<TestRun> model)
        {
            AzDoEntity.ConfigureModel(model);

            model.HasKey(x => x.Id);
            model.Property(x => x.Name).IsRequired();
        }
    }
}
