namespace WhatsBroken.Web.Model
{
    class BranchInfo
    {
        public string? RefName { get; set; }
        public string? Branch { get; set; }
        public int? PrNumber { get; set; }
        public bool IsPrimary { get; set; }
        public bool IsPr { get; set; }
    }
}
