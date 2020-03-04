using System;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace WhatsBroken.Worker.Model
{
    public abstract class AzDoEntity
    {
        public Guid ProjectId { get; set; }
        public int AzDoId { get; set; }

        protected static void ConfigureModel<T>(EntityTypeBuilder<T> model) where T: AzDoEntity
        {
            model.Property(x => x.ProjectId).IsRequired();
            model.HasIndex(x => new { x.ProjectId, x.AzDoId }).IsUnique();
        }
    }
}
