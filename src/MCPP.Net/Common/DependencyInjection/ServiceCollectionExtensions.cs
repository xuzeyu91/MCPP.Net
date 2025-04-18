using System.Reflection;

namespace MCPP.Net.Common.DependencyInjection
{
    /// <summary>
    /// 容器扩展
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 从程序集中加载类型并添加到容器中
        /// </summary>
        /// <param name="services">容器</param>
        /// <param name="assemblies">程序集集合</param>
        /// <returns></returns>
        public static IServiceCollection AddServicesFromAssemblies(this IServiceCollection services, params string[] assemblies)
        {
            Type attributeType = typeof(ServiceDescriptionAttribute);
            Type keyedAttributeType = typeof(KeyedServiceDescriptionAttribute);

            //var refAssembyNames = Assembly.GetExecutingAssembly().GetReferencedAssemblies();
            foreach (var item in assemblies)
            {
                Assembly assembly = Assembly.Load(item);

                var types = assembly.GetTypes();

                foreach (var classType in types)
                {
                    if (!classType.IsAbstract && classType.IsClass)
                    {
                        // Handle regular service registration
                        if (classType.IsDefined(attributeType, false))
                        {
                            var serviceAttribute = classType.GetCustomAttribute<ServiceDescriptionAttribute>();
                            RegisterService(services, serviceAttribute.ServiceType, classType, serviceAttribute.Lifetime);
                        }

                        // Handle keyed service registration
                        if (classType.IsDefined(keyedAttributeType, false))
                        {
                            var keyedAttribute = classType.GetCustomAttribute<KeyedServiceDescriptionAttribute>();
                            RegisterKeyedService(services, keyedAttribute.ServiceType, classType,
                                keyedAttribute.ServiceKey, keyedAttribute.Lifetime);
                        }
                    }
                }
            }

            return services;
        }

        private static void RegisterService(IServiceCollection services, Type serviceType,
            Type implementationType, ServiceLifetime lifetime)
        {
            switch (lifetime)
            {
                case ServiceLifetime.Scoped:
                    services.AddScoped(serviceType, implementationType);
                    break;
                case ServiceLifetime.Singleton:
                    services.AddSingleton(serviceType, implementationType);
                    break;
                case ServiceLifetime.Transient:
                    services.AddTransient(serviceType, implementationType);
                    break;
            }
        }

        private static void RegisterKeyedService(IServiceCollection services, Type serviceType,
            Type implementationType, object serviceKey, ServiceLifetime lifetime)
        {
            switch (lifetime)
            {
                case ServiceLifetime.Scoped:
                    services.AddKeyedScoped(serviceType, serviceKey, implementationType);
                    break;
                case ServiceLifetime.Singleton:
                    services.AddKeyedSingleton(serviceType, serviceKey, implementationType);
                    break;
                case ServiceLifetime.Transient:
                    services.AddKeyedTransient(serviceType, serviceKey, implementationType);
                    break;
            }
        }

    }
}
