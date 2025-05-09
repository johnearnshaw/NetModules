﻿/*
    The MIT License (MIT)

    Copyright (c) 2025 John Earnshaw, NetModules Foundation.
    Repository Url: https://github.com/netmodules/netmodules/

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in
    all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
    THE SOFTWARE.
 */

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using NetModules.Interfaces;

namespace NetModules.Classes
{
    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public sealed class TypeManager
    {
        private TypeManager() { }


        /// <summary>
        /// Find Modules. Directory depth can be controlled and is set to maximum depth by default. If you would like to create modules that
        /// are also ModuleHosts themselves, it is a good idea nest modules one directory level deep followed by nesting submodules another
        /// level deep. This will isolate parent modules from submodules.
        /// </summary>
        /// <param name="host">The Module's ModuleHost.</param>
        /// <param name="path">Where to search for DLL's that export IModule.</param>
        /// <param name="directoryDepth">How many subdirectories deep of path be searched.</param>
        public static IList<IModuleContainer> FindModules<T>(IModuleHost host, Uri path, int directoryDepth = int.MaxValue) where T : Module
        {
            var types = DllTypeSearch(host, typeof(T), path, directoryDepth);
            var containers = new List<IModuleContainer>();

            foreach (var t in types)
            {
                var attribute = t.GetCustomAttribute<ModuleAttribute>();

                if (attribute == null)
                {
                    throw new NotImplementedException(string.Format(Constants._TypeManagerNoModuleDecoration, t.Name));
                }

                attribute.Name = string.Format("{0}.{1}", t.Namespace, t.Name);
                attribute.Version = t.GetAssemblyInfo().Version;
                var location = t.GetPathToAssembly(false);
                containers.Add(new ModuleContainer(location, t, attribute, host));
            }

            return containers;
        }


        /// <summary>
        /// Find Events. Direcotry depth can be controlled and is set to maximum depth by default. If you would like to create modules that
        /// are also ModuleHosts themselves, it is a good idea nest modules one directory level deep followed by nesting submodules another
        /// level deep. This will isolate parent module events from submodule events.
        /// </summary>
        /// <param name="host">The <see cref="IModuleHost"/> instance that is searching for <see cref="IEvent"/>s.</param>
        /// <param name="path">Where to search for DLL's that export <see cref="IEvent"/>.</param>
        /// <param name="directoryDepth">How many subdirectories deep of path be searched.</param>
        public static IList<Type> FindEvents<T>(IModuleHost host, Uri path, int directoryDepth = int.MaxValue) where T : IEvent
        {
            var types = DllTypeSearch(host, typeof(T), path, directoryDepth);
            var events = new List<Type>();

            foreach (var t in types)
            {
                if (!t.GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEvent<,>)))
                {
                    throw new NotImplementedException(string.Format(Constants._TypeManagerNoIEventImplementation, t.Name));
                }

                events.Add(t);
            }

            return events;
        }


        /// <summary>
        /// 
        /// </summary>
        private static List<Type> DllTypeSearch(IModuleHost host, Type @type, Uri path, int directoryDepth = int.MaxValue)
        {
            var startingDepth = Count(Path.DirectorySeparatorChar, path.LocalPath);

            if (!Directory.Exists(path.LocalPath))
            {
                throw new ArgumentException(string.Format(Constants._TypeManagerInvalidPath, path.LocalPath), nameof(path));
            }

            var dlls = Directory.GetFiles(path.LocalPath, "*.dll", SearchOption.AllDirectories).ToList();
            dlls.AddRange(Directory.GetFiles(path.LocalPath, "*.exe", SearchOption.AllDirectories));

            dlls = dlls.Where(d => Count(Path.DirectorySeparatorChar, d) <= startingDepth + directoryDepth).ToList();
            var useable = dlls.SelectMany((dll) => GetUseableTypes(host, new Uri(dll), @type)).ToList();
            return useable;
        }


        /// <summary>
        /// 
        /// </summary>
        private static IList<Type> GetUseableTypes(IModuleHost host, Uri assemblyPath, Type type)
        {
            var ret = new List<Type>();
            
            
            // Wrapped try/catch for instances where a dll or exe file can not be loaded with .NET reflection, in this case we
            // would presume that the assembly being loaded is not a .NET assembly and therefore can not contain any useable
            // types.
            try
            {
                var loader = new AssemblyLoader(assemblyPath);
                var assembly = loader.Load();

                if (assembly == null)
                {
                    // TODO:
                    // Check why exe file throws an Exception
                    // "Could not load file or assembly '[PATH]'. The module was expected to contain an assembly manifest."
                    // For now, ignore...
                    if (!assemblyPath.AbsolutePath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    {
                        host.Log(Events.LoggingEvent.Severity.Error
                            , Constants._TypeManagerUnableToIterateAssembly
                            , assemblyPath
                            , type.FullName);
                    }

                    return ret;
                }

                foreach (Type t in assembly.GetExportedTypes())
                {
                    if ((type == t || type.IsAssignableFrom(t)) && !t.IsAbstract && !t.IsInterface)
                    {
                        ret.Add(t);
                    }
                }

                loader.Unload();
            }
            catch (Exception ex)
            {
                host.Log(Events.LoggingEvent.Severity.Error
                    , Constants._TypeManagerUnableToIterateAssembly
                    , assemblyPath
                    , type.FullName
                    , ex);
            }            

            return ret;
        }


        /// <summary>
        /// 
        /// </summary>
        public static Module InstantiateModule(Type module, AssemblyLoader loader)
        {
            try
            {
                var type = typeof(Module);
                loader.Load();

                foreach (Type t in loader.GetTypes())
                {
                    if ((type == t || type.IsAssignableFrom(t)) && !t.IsAbstract && !t.IsInterface)
                    {
                        if (t == module)
                        {
                            var result = Activator.CreateInstance(t) as Module;
                            if (result != null)
                            {
                                return result;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(Constants._ModuleInstantiateError
                    , ex);
            }
            
            return null;
        }


        /// <summary>
        /// 
        /// </summary>
        public static IEvent InstantiateEvent(Type t)
        {
            try
            {
                AssemblyLoader loader = new AssemblyLoader(new Uri(t.Assembly.Location));
                var loaded = loader.GetTypes();

                if (loaded.Length > 0)
                {
                    t = loaded.First(l => l.FullName.Equals(t.FullName));
                }

                if (Activator.CreateInstance(t) is IEvent result)
                {
                    if (!t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEvent<,>)))
                    {
                        throw new Exception(
                            Constants._TypeManagerNoEventInputOutput
                        );
                    }

                    return result;
                }
            }
            catch (Exception ex)
            {
                throw new Exception(
                    Constants._TypeManagerNoEventConstructor
                    , ex);
            }

            return null;
        }


        /// <summary>
        /// A simple method for counting instances of the same character within a string.
        /// This is used to discover and control the directory depth of a module or event in the FindModules, FindEvents and
        /// DllTypeSearch methods above.
        /// </summary>
        private static int Count(char needle, string haystack)
        {
            // If the string is empty there are certainly no instances of needle...
            if (string.IsNullOrEmpty(haystack))
            {
                return 0;
            }

            return haystack.Count(c => c == needle);
        }
    }
}
