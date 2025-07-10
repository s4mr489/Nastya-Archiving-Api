using Nastya_Archiving_project.Models;
using Nastya_Archiving_project.Models.DTOs;
using System.Linq.Expressions;

namespace Nastya_Archiving_project.Extinstion
{
    public static class IQueryableExtensions
    {
        public static IQueryable<T> WhereBaseFilter<T>(this IQueryable<T> query, BaseFilter filter) where T : BaseEntity
        {
            return query
                .Where(e => filter.Id == null || e.Id == filter.Id)
                .WhereSoftDeleted(filter.IsDeleted)
                .WhereDateRange(filter.StartDate, filter.EditDate);
        }

        public static IQueryable<T> WhereSoftDeleted<T>(this IQueryable<T> query, bool? filterValue) where T : BaseEntity
        {
            if (filterValue == null)
                return query;

            return query.Where(e => e.IsDeleted == filterValue);
        }

        public static IQueryable<T> WhereDateRange<T>(
            this IQueryable<T> query,
            DateTime? startDate,
            DateTime? endDate) where T : BaseEntity
        {
            return query
                .Where(e => startDate == null || e.CreatedAt >= startDate)
                .Where(e => endDate == null || e.CreatedAt <= endDate);
        }

        public static IQueryable<T> OrderByCreationDate<T>(this IQueryable<T> query) where T : BaseEntity
        {
            return query.OrderByDescending(e => e.CreatedAt);
        }
        public static IQueryable<T> WhereFilter<T>(this IQueryable<T> query, BaseFilter filter) where T : BaseEntity
        {
            if (filter == null) return query;

            // Start with a default expression that always evaluates to true
            Expression<Func<T, bool>> predicate = e => true;

            // Iterate through all properties of the filter and create corresponding expressions
            var filterProperties = filter.GetType().GetProperties();

            foreach (var filterProperty in filterProperties)
            {
                // Only apply the filter if the property is not null or has a valid value
                var filterValue = filterProperty.GetValue(filter);
                if (filterValue != null)
                {
                    // Try to find the corresponding property in the entity (T)
                    var entityProperty = typeof(T).GetProperty(filterProperty.Name);

                    if (entityProperty != null)
                    {
                        // Dynamically build the filter expression based on property types
                        var parameter = Expression.Parameter(typeof(T), "e");
                        var entityPropertyAccess = Expression.Property(parameter, entityProperty);

                        // Create a comparison expression based on the property type
                        Expression comparison = null;

                        if (filterValue is string stringValue)
                        {
                            // For strings, we can use `Contains`
                            comparison = Expression.Call(entityPropertyAccess, "Contains", null, Expression.Constant(stringValue));
                        }
                        else if (filterValue is bool boolValue)
                        {
                            // For booleans, we just check for equality
                            comparison = Expression.Equal(entityPropertyAccess, Expression.Constant(boolValue));
                        }
                        else if (filterValue is DateTime dateTimeValue)
                        {
                            // For DateTime, check for equality
                            comparison = Expression.Equal(entityPropertyAccess, Expression.Constant(dateTimeValue));
                        }
                        else if (filterValue is int intValue)
                        {
                            // For integers, check for equality
                            comparison = Expression.Equal(entityPropertyAccess, Expression.Constant(intValue));
                        }
                        else
                        {
                            // For other types (e.g., Enum, Nullable types), you can handle them as needed
                            comparison = Expression.Equal(entityPropertyAccess, Expression.Constant(filterValue));
                        }

                        // Combine the current predicate with the new condition
                        predicate = predicate.AndAlso(Expression.Lambda<Func<T, bool>>(comparison, parameter));
                    }
                }
            }

            // Apply the dynamically built filter to the query
            return query.Where(predicate);
        }

        // Helper function for combining expressions with AND
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
