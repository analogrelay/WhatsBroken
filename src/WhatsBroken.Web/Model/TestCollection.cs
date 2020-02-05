using System;
using System.Collections.Generic;

namespace WhatsBroken.Web.Model
{
    public class TestCollection
    {
        public TestCollection() : this(Array.Empty<TestCase>(), new Dictionary<string, TestProject>())
        {
        }

        public TestCollection(IReadOnlyList<TestCase> allTests, IReadOnlyDictionary<string, TestProject> projects)
        {
            AllTests = allTests;
            Projects = projects;
        }

        public IReadOnlyList<TestCase> AllTests { get; }
        public IReadOnlyDictionary<string, TestProject> Projects { get; }

        public static TestCollection Build(IReadOnlyList<TestCase> testCases)
        {
            // We know the incoming list is sorted, so take advantage of that
            string? currentProject = null;
            string? currentType = null;
            string? currentMethod = null;

            var projects = new Dictionary<string, TestProject>();
            var types = new Dictionary<string, TestType>();
            var methods = new Dictionary<string, TestMethod>();
            var cases = new Dictionary<string, TestCase>();

            foreach (var testCase in testCases)
            {
                if (!string.Equals(currentProject, testCase.Project))
                {
                    CompleteMethod();
                    CompleteType();
                    CompleteProject();
                }
                else if (!string.Equals(currentType, testCase.Type))
                {
                    CompleteMethod();
                    CompleteType();
                }
                else if (!string.Equals(currentMethod, testCase.Method))
                {
                    CompleteMethod();
                }

                currentProject = testCase.Project;
                currentType = testCase.Type;
                currentMethod = testCase.Method;

                cases.Add(testCase.ArgumentHash, testCase);
            }

            CompleteMethod();
            CompleteType();
            CompleteProject();

            return new TestCollection(testCases, projects);

            void CompleteMethod()
            {
                if (currentMethod == null || cases.Count == 0)
                {
                    return;
                }

                var method = new TestMethod(currentMethod, cases);
                cases = new Dictionary<string, TestCase>();
                currentMethod = null;
                methods.Add(method.Name, method);
            }

            void CompleteType()
            {
                if (currentType == null || methods.Count == 0)
                {
                    return;
                }

                var type = new TestType(currentType, methods);
                methods = new Dictionary<string, TestMethod>();
                currentType = null;
                types.Add(type.Name, type);
            }

            void CompleteProject()
            {
                if (currentProject == null || types.Count == 0)
                {
                    return;
                }

                var project = new TestProject(currentProject, types);
                types = new Dictionary<string, TestType>();
                currentProject = null;
                projects.Add(project.Name, project);
            }
        }
    }

    public class TestMethod
    {
        public TestMethod(string name, IReadOnlyDictionary<string, TestCase> cases)
        {
            Name = name;
            Cases = cases;
        }

        public string Name { get; }
        public IReadOnlyDictionary<string, TestCase> Cases { get; }
    }

    public class TestType
    {
        public TestType(string name, IReadOnlyDictionary<string, TestMethod> methods)
        {
            Name = name;
            Methods = methods;
        }

        public string Name { get; }
        public IReadOnlyDictionary<string, TestMethod> Methods { get; }
    }

    public class TestProject
    {
        public TestProject(string name, IReadOnlyDictionary<string, TestType> types)
        {
            Name = name;
            Types = types;
        }

        public string Name { get; }
        public IReadOnlyDictionary<string, TestType> Types { get; }
    }
}
