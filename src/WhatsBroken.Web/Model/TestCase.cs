using System;

namespace WhatsBroken.Web.Model
{
    public class TestCase: IEquatable<TestCase>
    {
        public TestCase(string project, string type, string method, string arguments, string argumentHash)
        {
            Project = project;
            Type = type;
            Method = method;
            Arguments = arguments;
            ArgumentHash = argumentHash;
        }

        public string Project { get; }
        public string Type { get; }
        public string Method { get; }
        public string Arguments { get; }
        public string ArgumentHash { get; }

        public override bool Equals(object? obj) => obj is TestCase other && Equals(other);
        public bool Equals(TestCase? other) => other is TestCase &&
                   string.Equals(Project, other.Project, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(Type, other.Type, StringComparison.Ordinal) &&
                   string.Equals(Method, other.Method, StringComparison.Ordinal) &&
                   string.Equals(ArgumentHash, other.ArgumentHash, StringComparison.Ordinal);
        public override int GetHashCode() => HashCode.Combine(Project, Type, Method, ArgumentHash);
    }
}
