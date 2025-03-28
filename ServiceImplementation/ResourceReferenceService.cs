using EntityDto.ExternalReferenceGroup;
using Microsoft.Extensions.DependencyInjection;
using ServiceInterface.Storage;

namespace ServiceImplementation
{
    public class ResourceReferenceService
    {
        private readonly ServiceProvider _serviceProvider;
        public ResourceReferenceService(ServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public object ResolveExternalReferenceService(Source source)
        {
            return GetInstanceForInterface(_serviceProvider, source.SourceType, source.SourceName)!;
        }

        private static object? GetInstanceForInterface(ServiceProvider serviceProvider, string interfaceName, string keyName)
        {
            // Find the type of the interface based on the string name
            Type? interfaceType = Type.GetType(interfaceName, throwOnError: false);

            if (interfaceType == null)
            {
                throw new InvalidOperationException($"Invalid interface name: {interfaceName}");
            }
            if (string.IsNullOrEmpty(keyName))
                return serviceProvider.GetService(interfaceType);
            return serviceProvider.GetKeyedService(interfaceType, keyName);
        }
    }
}
