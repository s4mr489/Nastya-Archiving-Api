using Nastya_Archiving_project.Models.DTOs;
using System.Linq.Expressions;

namespace FYP.Extentions
{
    public static class IQueryableExtensions
    {
        public static IQueryable<T> WhereBaseFilter<T>(this IQueryable<T> query, BaseFilter filter)
        {
            return query
                .WhereSoftDeleted(filter.IsDeleted)
                .WhereDateRange(filter.StartDate, filter.EndDate)
                .WhereIdMatch(filter.Id);
        }

        public static IQueryable<T> WhereSoftDeleted<T>(this IQueryable<T> query, bool? filterValue)
        {
            if (filterValue == null) return query;

            var property = typeof(T).GetProperty("IsDeleted");
            if (property == null) return query;

            var parameter = Expression.Parameter(typeof(T), "e");
            var propertyAccess = Expression.Property(parameter, property);
            var condition = Expression.Equal(propertyAccess, Expression.Constant(filterValue));
            var lambda = Expression.Lambda<Func<T, bool>>(condition, parameter);

            return query.Where(lambda);
        }

        public static IQueryable<T> WhereDateRange<T>(this IQueryable<T> query, DateTime? startDate, DateTime? endDate)
        {
            var property = typeof(T).GetProperty("CreatedAt");
            if (property == null) return query;

            var parameter = Expression.Parameter(typeof(T), "e");
            Expression? expression = null;

            if (startDate != null)
            {
                var access = Expression.Property(parameter, property);
                var compare = Expression.GreaterThanOrEqual(access, Expression.Constant(startDate));
                expression = expression == null ? compare : Expression.AndAlso(expression, compare);
            }

            if (endDate != null)
            {
                var access = Expression.Property(parameter, property);
                var compare = Expression.LessThanOrEqual(access, Expression.Constant(endDate));
                expression = expression == null ? compare : Expression.AndAlso(expression, compare);
            }

            if (expression == null) return query;

            var lambda = Expression.Lambda<Func<T, bool>>(expression, parameter);
            return query.Where(lambda);
        }

        public static IQueryable<T> WhereIdMatch<T>(this IQueryable<T> query, int? id)
        {
            if (id == null) return query;

            var property = typeof(T).GetProperty("Id");
            if (property == null) return query;

            var parameter = Expression.Parameter(typeof(T), "e");
            var propertyAccess = Expression.Property(parameter, property);
            var condition = Expression.Equal(propertyAccess, Expression.Constant(id));
            var lambda = Expression.Lambda<Func<T, bool>>(condition, parameter);

            return query.Where(lambda);
        }

        public static IQueryable<T> OrderByCreationDate<T>(this IQueryable<T> query)
        {
            var property = typeof(T).GetProperty("CreatedAt");
            if (property == null) return query;

            var parameter = Expression.Parameter(typeof(T), "e");
            var propertyAccess = Expression.Property(parameter, property);
            var lambda = Expression.Lambda(propertyAccess, parameter);

            var method = typeof(Queryable).GetMethods()
                .First(m => m.Name == "OrderByDescending" && m.GetParameters().Length == 2)
                .MakeGenericMethod(typeof(T), property.PropertyType);

            return (IQueryable<T>)method.Invoke(null, new object[] { query, lambda })!;
        }

        public static IQueryable<T> WhereFilter<T>(this IQueryable<T> query, BaseFilter filter)
        {
            if (filter == null) return query;

            Expression<Func<T, bool>> predicate = e => true;
            var filterProperties = filter.GetType().GetProperties();

            foreach (var filterProperty in filterProperties)
            {
                var filterValue = filterProperty.GetValue(filter);
                if (filterValue == null) continue;

                var entityProperty = typeof(T).GetProperty(filterProperty.Name);
                if (entityProperty == null) continue;

                var parameter = Expression.Parameter(typeof(T), "e");
                var entityPropertyAccess = Expression.Property(parameter, entityProperty);

                Expression comparison;

                if (filterValue is string stringValue)
                {
                    comparison = Expression.Call(entityPropertyAccess, "Contains", null, Expression.Constant(stringValue));
                }
                else
                {
                    // ? Ensures safe comparison for nullable types like Guid?, DateTime?, etc.
                    var constant = Expression.Constant(filterValue, entityProperty.PropertyType);
                    comparison = Expression.Equal(entityPropertyAccess, constant);
                }

                predicate = predicate.AndAlso(Expression.Lambda<Func<T, bool>>(comparison, parameter));
            }

            return query.Where(predicate);
        }


        public static Expression<Func<T, bool>> AndAlso<T>(this Expression<Func<T, bool>> expr1, Expression<Func<T, bool>> expr2)
        {
            var parameter = Expression.Parameter(typeof(T), "e");
            var combined = Expression.AndAlso(
                Expression.Invoke(expr1, parameter),
                Expression.Invoke(expr2, parameter)
            );
            return Expression.Lambda<Func<T, bool>>(combined, parameter);
        }
    }
}
