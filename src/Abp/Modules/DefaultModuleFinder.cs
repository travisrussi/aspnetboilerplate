﻿using System;
using System.Collections.Generic;
using System.Linq;
using Abp.Reflection;

namespace Abp.Modules
{
    internal class DefaultModuleFinder : IModuleFinder
    {
        public IAssemblyFinder AssemblyFinder { get; set; }

        public DefaultModuleFinder()
        {
            AssemblyFinder = DefaultAssemblyFinder.Instance;            
        }

        public List<Type> FindAll()
        {
            return (
                from assembly in AssemblyFinder.GetAllAssemblies()
                from type in assembly.GetTypes()
                where AbpModule.IsAbpModule(type)
                select type
                ).ToList();
        }
    }
}