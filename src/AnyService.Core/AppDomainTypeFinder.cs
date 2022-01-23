using Microsoft.Extensions.FileProviders;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace AnyService
{
    public class AppDomainTypeFinder : ITypeFinder
    {
        public const string EXCLUDED_ASSEMBLIES_PATTERN = "^AutoMapper|^Castle|^coverlet|^EasyCaching|^LiteDb|^Microsoft|^Moq|^mscorlib|^Newtonsoft.Json|^Nuget|^RabbitMQ|^Shouldly|^System|^xunit";
        public const string TO_ONLY_LOAD = ".*";

        private readonly IFileProvider _fileProvider;
        private readonly bool _loadBinFolderAssemblies;
        private bool _assembliesLoaded;
        private ICollection<Assembly> _loadedAssemblies;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="fileProvider">IFileProvider to load bin assemblies from</param>
        /// <param name="loadBinFolderAssemblies">Specifies if bin folder assemblies should be loaded. Default is true</param>
        /// <param name="excludedAsserbliesPattern">Assemblies to exclude from instance's scans</param>
        /// <param name="onlyLoadTheseAssebliesPattern">Assemlies to be loaded (ignores all  other assemlies)</param>
        public AppDomainTypeFinder(
            IFileProvider fileProvider,
            bool loadBinFolderAssemblies = true,
            string excludedAsserbliesPattern = EXCLUDED_ASSEMBLIES_PATTERN,
            string onlyLoadTheseAssebliesPattern = TO_ONLY_LOAD)
        {
            _fileProvider = fileProvider;
            _loadBinFolderAssemblies = loadBinFolderAssemblies;
            AssembliesToIgnorePattern = excludedAsserbliesPattern;
            AssembliesToOnlyLoadPattern = onlyLoadTheseAssebliesPattern;
        }

        protected string AssembliesToIgnorePattern { get; }
        protected string AssembliesToOnlyLoadPattern { get; }

        public IEnumerable<Type> GetAllTypesOf(Type assignedfromType, bool concretesOnly)
        {
            var assemblies = GetAssemblies();

            _assembliesLoaded = true;
            return GetAllTypesOf(assignedfromType, assemblies, concretesOnly);

        }
        public IEnumerable<Type> GetAllTypesOf(Type assignableFrom, IEnumerable<Assembly> assemblies, bool concretesOnly)
        {
            var result = new List<Type>();

            try
            {
                foreach (var asm in assemblies)
                {
                    Type[] types = default;
                    try
                    {
                        types = asm.GetTypes();
                    }
                    catch (Exception ex)
                    {
                        var msg = ex.Message;
                        var e = ex;
                        do
                        {
                            msg += e.Message + Environment.NewLine;
                            e = e.InnerException;
                        }
                        while (e != default);
                        Debug.WriteLine(msg);
                    }

                    if (types == default)
                        continue;

                    foreach (var t in types)
                    {
                        if (t.IsInterface
                            || (!assignableFrom.IsAssignableFrom(t) &&
                            (!assignableFrom.IsGenericType || !t.IsOfOpenGenericType(assignableFrom)))
                            || (concretesOnly && (!t.IsClass || t.IsAbstract)))
                            continue;

                        result.Add(t);
                    }
                }
            }
            catch (ReflectionTypeLoadException ex)
            {
                var msg = string.Empty;
                foreach (var e in ex.LoaderExceptions)
                    msg += e.Message + Environment.NewLine;

                var fail = new Exception(msg, ex);
                Debug.WriteLine(fail.Message, fail);

                throw fail;
            }

            return result;
        }

        protected IEnumerable<Assembly> GetAssemblies()
        {
            if (_assembliesLoaded)
                return _loadedAssemblies;

            _loadedAssemblies = new List<Assembly>();
            var addedAssemblyNames = new List<string>();

            if (_loadBinFolderAssemblies)
                LoadMatchingAssembliesFromPath(AppContext.BaseDirectory);

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (!Matches(assembly.FullName)
                    || addedAssemblyNames.Contains(assembly.FullName))
                    continue;

                _loadedAssemblies.Add(assembly);
                addedAssemblyNames.Add(assembly.FullName);
            }
            _assembliesLoaded = true;
            return _loadedAssemblies;
        }
        protected virtual bool Matches(string assemblyFullName)
        {
            return !Matches(assemblyFullName, AssembliesToIgnorePattern) &&
                Matches(assemblyFullName, AssembliesToOnlyLoadPattern);
        }

        protected void LoadMatchingAssembliesFromPath(string directoryPath)
        {
            var loadedAssemblyNames = new List<string>();
            var content = _fileProvider.GetDirectoryContents(string.Empty);

            if (!content.Exists)
                return;
            var dllPaths = content.Where(x => x.Name.EndsWith(".dll")).Select(f => f.PhysicalPath);
            foreach (var dllPath in dllPaths)
            {
                try
                {
                    var an = AssemblyName.GetAssemblyName(dllPath);
                    if (Matches(an.FullName) && !loadedAssemblyNames.Contains(an.FullName))
                        AppDomain.CurrentDomain.Load(an);

                    //old loading stuff
                    //Assembly a = Assembly.ReflectionOnlyLoadFrom(dllPath);
                    //if (Matches(a.FullName) && !loadedAssemblyNames.Contains(a.FullName))
                    //{
                    //    App.Load(a.FullName);
                    //}
                }
                catch (BadImageFormatException ex)
                {
                    Trace.TraceError(ex.ToString());
                }
            }
        }

        protected bool Matches(string assemblyFullName, string pattern, RegexOptions options = RegexOptions.IgnoreCase | RegexOptions.Compiled) =>
            Regex.IsMatch(assemblyFullName, pattern, options);
    }
}
