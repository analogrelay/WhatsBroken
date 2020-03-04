using System.Collections;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WhatsBroken.Worker.Model
{
    public class Pipeline: AzDoEntity
    {
        public int Id { get; set; }
        public string? Path { get; set; }
        public string? Name { get; set; }
        public string? Project { get; set; }

        public IList<Build>? Builds { get; set; }

        internal static void ConfigureModel(EntityTypeBuilder<Pipeline> model)
        {
            AzDoEntity.ConfigureModel(model);

            model.HasKey(x => x.Id);
            model.Property(x => x.Path).IsRequired();
            model.Property(x => x.Name).IsRequired();
            model.Property(x => x.Project).IsRequired();

            model.HasMany(x => x.Builds)
                .WithOne(x => x.Pipeline!)
                .HasForeignKey(x => x.PipelineId);
        }
    }
}
