using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EFSoftDeletes.Interceptors
{
class SoftDeleteQueryExpressionInterceptor : IQueryExpressionInterceptor
{
    public Expression QueryCompilationStarting(Expression queryExpression, QueryExpressionEventData eventData)
    {
        return new SoftDeletableQueryExpressionVisitor().Visit(queryExpression);
    }

    class SoftDeletableQueryExpressionVisitor : ExpressionVisitor
    {
        private const string _isDeletedProperty = "IsDeleted";
        private static readonly MethodInfo _executeDeleteMethod = typeof(RelationalQueryableExtensions).GetMethod(nameof(RelationalQueryableExtensions.ExecuteDelete), BindingFlags.Public | BindingFlags.Static)!;
        private static readonly MethodInfo _executeUpdateMethod = typeof(RelationalQueryableExtensions).GetMethod(nameof(RelationalQueryableExtensions.ExecuteUpdate), BindingFlags.Public | BindingFlags.Static)!;
        private static readonly MethodInfo _propertyMethod = typeof(EF).GetMethod(nameof(EF.Property), BindingFlags.Static | BindingFlags.Public)!;

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.IsGenericMethod && node.Method.GetGenericMethodDefinition() == _executeDeleteMethod)
            {
                var entityType = node.Method.GetGenericArguments()[0];
                var isSoftDeletable = typeof(ISoftDeletable).IsAssignableFrom(entityType);
 
                if (isSoftDeletable)
                {
                    var setPropertyMethod = typeof(SetPropertyCalls<>).MakeGenericType(entityType).GetMethods().Single(m =>
                        m.Name == nameof(SetPropertyCalls<object>.SetProperty)
                        && m.IsGenericMethod
                        && m.GetGenericArguments().Length == 1
                        && m.GetParameters().Length == 2
                        && m.GetParameters()[1].ParameterType.IsGenericMethodParameter
                        && m.GetParameters()[1].Name == "valueExpression")
                    .MakeGenericMethod(typeof(bool));
 
                    var setterParameter = Expression.Parameter(typeof(SetPropertyCalls<>).MakeGenericType(entityType), "setters");
                    var parameter = Expression.Parameter(entityType, "p");
                    var propertyCall = Expression.Call(null, _propertyMethod.MakeGenericMethod(typeof(bool)), parameter, Expression.Constant(_isDeletedProperty));
                    var propertyCallLambda = Expression.Lambda(propertyCall, parameter);
                    var setPropertyCall = Expression.Call(setterParameter, setPropertyMethod, propertyCallLambda, Expression.Constant(true));
                    var lambda = Expression.Lambda(setPropertyCall, setterParameter);

                    return Expression.Call(node.Object, _executeUpdateMethod.MakeGenericMethod(entityType), node.Arguments[0], lambda);
                }
            }             

            return base.VisitMethodCall(node);
        }
    }
}
}
