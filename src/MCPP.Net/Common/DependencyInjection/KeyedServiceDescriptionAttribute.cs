namespace MCPP.Net.Common.DependencyInjection
{
    /// <summary>
    /// 标记服务描述特性（支持Keyed Service）
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class KeyedServiceDescriptionAttribute : Attribute
    {
        /// <summary>
        /// 服务类型
        /// </summary>
        public Type ServiceType { get; }

        /// <summary>
        /// 服务键
        /// </summary>
        public object ServiceKey { get; }

        /// <summary>
        /// 生命周期
        /// </summary>
        public ServiceLifetime Lifetime { get; }

        /// <summary>
        /// 初始化服务描述特性
        /// </summary>
        /// <param name="serviceType">服务类型</param>
        /// <param name="serviceKey">服务键</param>
        /// <param name="lifetime">生命周期</param>
        public KeyedServiceDescriptionAttribute(Type serviceType, object serviceKey, ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            ServiceType = serviceType ?? throw new ArgumentNullException(nameof(serviceType));
            ServiceKey = serviceKey ?? throw new ArgumentNullException(nameof(serviceKey));
            Lifetime = lifetime;
        }
    }
}
