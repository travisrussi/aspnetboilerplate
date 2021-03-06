﻿using System.Linq;
using System.Reflection;
using Abp.Configuration.Startup;
using Abp.Dependency;
using Castle.Core.Logging;

namespace Abp.Modules
{
    /// <summary>
    /// This class is used to manage modules.
    /// </summary>
    internal class AbpModuleManager : IAbpModuleManager
    {
        public ILogger Logger { get; set; }
        
        private readonly AbpModuleCollection _modules;

        private readonly IIocManager _iocManager;
        private readonly IModuleFinder _moduleFinder;

        public AbpModuleManager(IIocManager iocManager, IModuleFinder moduleFinder)
        {
            _modules = new AbpModuleCollection();
            _iocManager = iocManager;
            _moduleFinder = moduleFinder;
            Logger = NullLogger.Instance;
        }

        public virtual void InitializeModules()
        {
            LoadAll();

            var sortedModules = _modules.GetSortedModuleListByDependency();

            sortedModules.ForEach(module => module.Instance.PreInitialize());
            sortedModules.ForEach(module => module.Instance.Initialize());
            sortedModules.ForEach(module => module.Instance.PostInitialize());
        }

        public virtual void ShutdownModules()
        {
            var sortedModules = _modules.GetSortedModuleListByDependency();
            sortedModules.Reverse();
            sortedModules.ForEach(sm => sm.Instance.Shutdown());
        }

        private void LoadAll()
        {
            Logger.Debug("Loading Abp modules...");

            var moduleTypes = _moduleFinder.FindAll();

            //Register to IOC container.
            foreach (var moduleType in moduleTypes)
            {
                if (!AbpModule.IsAbpModule(moduleType))
                {
                    throw new AbpInitializationException("This type is not an ABP module: " + moduleType.AssemblyQualifiedName);
                }

                if (!_iocManager.IsRegistered(moduleType))
                {
                    _iocManager.Register(moduleType);
                }
            }

            //Add to module collection
            foreach (var moduleType in moduleTypes)
            {
                var moduleObject = (AbpModule) _iocManager.Resolve(moduleType);

                moduleObject.IocManager = _iocManager;
                moduleObject.Configuration = _iocManager.Resolve<IAbpStartupConfiguration>();

                _modules.Add(new AbpModuleInfo(moduleObject));
                
                Logger.DebugFormat("Loaded module: " + moduleType.AssemblyQualifiedName);
            }

            //AbpCoreModule must be the first module
            var startupModuleIndex = _modules.FindIndex(m => m.Type == typeof(AbpCoreModule));
            if (startupModuleIndex > 0)
            {
                var startupModule = _modules[startupModuleIndex];
                _modules.RemoveAt(startupModuleIndex);
                _modules.Insert(0, startupModule);
            }

            SetDependencies();

            Logger.DebugFormat("{0} modules loaded.", _modules.Count);
        }

        private void SetDependencies()
        {
            foreach (var moduleInfo in _modules)
            {
                //Set dependencies according to assembly dependency
                foreach (var referencedAssemblyName in moduleInfo.Assembly.GetReferencedAssemblies())
                {
                    var referencedAssembly = Assembly.Load(referencedAssemblyName);
                    var dependedModuleList = _modules.Where(m => m.Assembly == referencedAssembly).ToList();
                    if (dependedModuleList.Count > 0)
                    {
                        moduleInfo.Dependencies.AddRange(dependedModuleList);
                    }
                }

                //Set dependencies according to explicit dependencies
                var dependedModuleTypes = moduleInfo.Instance.GetDependedModules();
                foreach (var dependedModuleType in dependedModuleTypes)
                {
                    AbpModuleInfo dependedModule;
                    if (((dependedModule = _modules.FirstOrDefault(m => m.Type == dependedModuleType)) != null)
                        && (moduleInfo.Dependencies.FirstOrDefault(dm => dm.Type == dependedModuleType) == null))
                    {
                        moduleInfo.Dependencies.Add(dependedModule);
                    }
                }
            }
        }
    }
}
