using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using EFSoftDeletes.Interceptors;

namespace EFSoftDeletes.Extensions
{
    public enum SoftDeleteInterceptorOption
    {
        SaveChanges = 1,
        ExecuteDelete = 2,
        All = SaveChanges | ExecuteDelete
    }

    public static class DbContextOptionsBuilderExtensions
    {
        public static T AddSoftDeleteQueryExpressionInterceptor<T>(this T optionsBuilder) where T : DbContextOptionsBuilder
        {
            optionsBuilder.AddInterceptors(new SoftDeleteQueryExpressionInterceptor());
            return optionsBuilder;
        }
            
        public static T Add<T>(this T optionsBuilder, SoftDeleteInterceptorOption option = SoftDeleteInterceptorOption.All) where T : DbContextOptionsBuilder
        {
            var interceptors = new List<IInterceptor>();

            if ((option & SoftDeleteInterceptorOption.SaveChanges) == SoftDeleteInterceptorOption.SaveChanges)
            {
                interceptors.Add(new SoftDeleteSaveChangesInterceptor());
            }
            if ((option & SoftDeleteInterceptorOption.ExecuteDelete) == SoftDeleteInterceptorOption.ExecuteDelete)
            {
                interceptors.Add(new SoftDeleteExecuteDeleteInterceptor());
            }

            optionsBuilder.AddInterceptors(interceptors);
            return optionsBuilder;
        }
    }
}
