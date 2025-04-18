using MCPP.Net.Common.DependencyInjection;
using MCPP.Net.Repositories;
using MCPP.Net.Repositories.Base;
using SqlSugar;
using ServiceLifetime = MCPP.Net.Common.DependencyInjection.ServiceLifetime;

namespace MCPP.Net.Repositories
{
    [ServiceDescription(typeof(IToolRepositories), ServiceLifetime.Scoped)]
    public class tool_Repositories : Repository<tool>, IToolRepositories
    {
    }
}