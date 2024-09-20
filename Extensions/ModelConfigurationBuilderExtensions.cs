using EFSoftDeletes.Conventions;
using Microsoft.EntityFrameworkCore;

namespace EFSoftDeletes.Extensions
{
    public static class ModelConfigurationBuilderExtensions
    {
        public static ModelConfigurationBuilder AddSoftDeleteConvention(this ModelConfigurationBuilder configurationBuilder)
        {
            configurationBuilder.Conventions.Add(_ => new SoftDeleteConvention<bool>());
            return configurationBuilder;
        }
    }
}
