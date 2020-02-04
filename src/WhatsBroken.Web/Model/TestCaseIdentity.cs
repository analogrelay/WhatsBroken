using System;

namespace WhatsBroken.Web.Model
{
    public class TestCaseIdentity: IEquatable<TestCaseIdentity>
    {
        public TestCaseIdentity(string project, string type, string method)
        {
            Project = project;
            Type = type;
            Method = method;
        }

        public string Project { get; }
        public string Type { get; }
        public string Method { get; }

        public override bool Equals(object? obj) => obj is TestCaseIdentity other && Equals(other);
        public bool Equals(TestCaseIdentity? other) => other is TestCaseIdentity &&
                   string.Equals(Project, other.Project, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(Type, other.Type, StringComparison.Ordinal) &&
                   string.Equals(Method, other.Method, StringComparison.Ordinal);
        public override int GetHashCode() => HashCode.Combine(Project, Type, Method);
    }
}
