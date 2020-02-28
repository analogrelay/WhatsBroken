namespace WhatsBroken.Worker.Model
{
    public class Pipeline
    {
        public int Id { get; set; }
        public int AzDoId { get; set; }
        public string? Project { get; set; }
        public string? Name { get; set; }
        public string? Path { get; set; }
    }
}
