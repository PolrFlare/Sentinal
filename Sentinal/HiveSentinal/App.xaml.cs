using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace HiveSentinal
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                var name = new System.Reflection.AssemblyName(args.Name).Name + ".dll";
                var path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "libs", name);
                return System.IO.File.Exists(path)
                    ? System.Reflection.Assembly.LoadFrom(path)
                    : null;
            };
        }
    }
}
