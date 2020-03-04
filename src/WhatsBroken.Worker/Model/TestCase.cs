using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WhatsBroken.Worker.Model
{
    public class TestCase
    {
        public int Id { get; set; }
        public string? Project { get; set; }
        public string? Type { get; set; }
        public string? Method { get; set; }
        public string? Arguments { get; set; }
        public string? ArgumentHash { get; set; }
        public string? Kind { get; set; }

        public IList<TestResult>? Results { get; set; }

        internal static void ConfigureModel(EntityTypeBuilder<TestCase> model)
        {
            model.HasKey(x => x.Id);
            model.Property(x => x.Project).IsRequired();
            model.Property(x => x.Type).IsRequired();
            model.Property(x => x.Method).IsRequired();
        }
    }
}
