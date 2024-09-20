using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using System.Linq.Expressions;
using System.Reflection;

namespace EFSoftDeletes.Conventions
{
    internal class SoftDeleteConvention<T> : IModelFinalizingConvention
    {
        private const string _isDeletedProperty = "IsDeleted";
        private static readonly MethodInfo _propertyMethod = typeof(EF).GetMethod(nameof(EF.Property), BindingFlags.Static | BindingFlags.Public)!.MakeGenericMethod(typeof(T));

        public T DeletedValue { get; init; } = default!;
        public string IsDeletedProperty { get; init; } = _isDeletedProperty;
    
        private LambdaExpression GetIsDeletedRestriction(Type type)
        {
            var parm = Expression.Parameter(type, "it");
            var prop = Expression.Call(_propertyMethod, parm, Expression.Constant(IsDeletedProperty));
            var condition = Expression.MakeBinary(ExpressionType.Equal, prop, Expression.Constant(DeletedValue));
            var lambda = Expression.Lambda(condition, parm);
            return lambda;
        }
    
        public void ProcessModelFinalizing(IConventionModelBuilder modelBuilder, IConventionContext<IConventionModelBuilder> context)
        {
            foreach (var entityType in modelBuilder.Metadata.GetEntityTypes().Where(entityType => typeof(ISoftDeletable<T>).IsAssignableFrom(entityType.ClrType)))
            {
                entityType.AddProperty(IsDeletedProperty, typeof(T));
                modelBuilder.Entity(entityType.ClrType)!.HasQueryFilter(GetIsDeletedRestriction(entityType.ClrType));
            }
        }
    }
}
