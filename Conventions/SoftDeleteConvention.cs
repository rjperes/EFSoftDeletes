using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using System.Linq.Expressions;
using System.Reflection;

namespace EFSoftDeletes.Conventions
{
    internal class SoftDeleteConvention : IModelFinalizingConvention
    {
        private const string _isDeletedProperty = Constants.IsDeletedProperty;
        private static readonly MethodInfo _propertyMethod = typeof(EF).GetMethod(nameof(EF.Property), BindingFlags.Static | BindingFlags.Public)!.MakeGenericMethod(typeof(bool));

        private static LambdaExpression GetIsDeletedRestriction(Type type)
        {
            var parm = Expression.Parameter(type, "it");
            var prop = Expression.Call(_propertyMethod, parm, Expression.Constant(_isDeletedProperty));
            var condition = Expression.MakeBinary(ExpressionType.Equal, prop, Expression.Constant(false));
            var lambda = Expression.Lambda(condition, parm);
            return lambda;
        }

        public void ProcessModelFinalizing(IConventionModelBuilder modelBuilder, IConventionContext<IConventionModelBuilder> context)
        {
            foreach (var entityType in modelBuilder.Metadata.GetEntityTypes().Where(entityType => typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType)))
            {
                entityType.AddProperty(_isDeletedProperty, typeof(bool));
                modelBuilder.Entity(entityType.ClrType)!.HasQueryFilter(GetIsDeletedRestriction(entityType.ClrType));
            }
        }
    }
}
