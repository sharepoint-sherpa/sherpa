﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using log4net;
using Microsoft.SharePoint.Client;
using Sherpa.Library.API;
using Sherpa.Library.SiteHierarchy.Model;

namespace Sherpa.Library.CustomTasks
{
    public class CustomTasksManager : ICustomTasksManager
    {
        private string ConfigurationRoot { get; set; }
        private Dictionary<string,TypeInfo> Tasks { set; get; }
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public CustomTasksManager(string configurationRoot)
        {
            ConfigurationRoot = configurationRoot;
        }

        private void LoadTasks()
        {
            Tasks = new Dictionary<string, TypeInfo>();
            // Find all tasks located in assemblies unde the CustomTasks folder
            foreach (var file in Directory.EnumerateFiles(Path.Combine(ConfigurationRoot, "customtasks"), "*.dll", SearchOption.AllDirectories))
            {
                var assembly = Assembly.LoadFrom(file);

                try
                {
                    var types =
                        assembly.DefinedTypes.Where(type => type.ImplementedInterfaces.Any(i => i == typeof (ITask)));
                    foreach (var typeInfo in types)
                    {
                        Tasks.Add(typeInfo.FullName, typeInfo);
                    }
                } catch {}
            }
        }
        
        public void ExecuteTasks(ShWeb rootWeb, ClientContext context)
        {
            LoadTasks();

            foreach (var taskConfig in rootWeb.CustomTaskTypes)
            {
                TypeInfo taskTypeInfo = null;
                Tasks.TryGetValue(taskConfig.FullName, out taskTypeInfo);
                if (taskTypeInfo != null)
                {
                    var instance = (ITask) Activator.CreateInstance(taskTypeInfo.AsType());
                    instance.ExecuteOn(rootWeb, context);
                }
                else
                {
                    Log.ErrorFormat("Could not find loaded task with name {0}", taskConfig.FullName);
                }
            }
        }
    }
}
