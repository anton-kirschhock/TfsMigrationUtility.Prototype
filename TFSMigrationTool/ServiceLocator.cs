using Microsoft.Practices.Unity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace TFSMigrationTool
{
    /// <summary>
    /// IoC registrations are done here
    /// </summary>
    public static class ServiceLocator
    {
        /// <summary>
        /// The using Container for DI
        /// </summary>
        private static UnityContainer _container;
        /// <summary>
        /// Registers all the instances and types
        /// </summary>
        static ServiceLocator()
        {
            _container = new UnityContainer();
            _container.RegisterInstance<IUnityContainer>(_container);
            _container.RegisterType<Window, MainWindow>("MainWindow");
            _container.RegisterType<Window, MetadataChanger>("MetadataChanger");
            _container.RegisterType<Window, Migrate>("Migrate");
            _container.RegisterType<Window, MigrateHistory>("MigrateHistory");
        }
        /// <summary>
        /// Resolve an instance of the default requested type from the container.
        /// </summary>
        /// <typeparam name="E">System.Type of object to get from the container.</typeparam>
        /// <returns>The retrieved object.</returns>
        public static E Resolve<E>()
        {
            return _container.Resolve<E>();
        }
        /// <summary>
        /// Resolve an instance of the default requested type from the container.
        /// </summary>
        /// <typeparam name="E">System.Type of object to get from the container.</typeparam>
        /// <param name="key">Name/Key of the object to retrieve.</param>
        /// <returns>The retrieved object.</returns>
        public static E Resolve<E>(string key)
        {
            return _container.Resolve<E>(key);
        }
        /// <summary>
        /// Return instances of all registered types requested.
        /// </summary>
        /// <typeparam name="E">The type requested.</typeparam>
        /// <returns>Set of objects of type T.</returns>
        public static IEnumerable<E> ResolveAll<E>()
        {
            return _container.ResolveAll<E>();
        }
    }
}
