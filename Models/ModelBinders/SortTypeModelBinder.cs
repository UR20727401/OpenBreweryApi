
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using OpenBreweryApi.Models.Requests;

namespace OpenBreweryApi.ModelBinders
{
    public class SortTypeModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var name = bindingContext.FieldName ?? bindingContext.ModelName;
            var valueResult = bindingContext.ValueProvider.GetValue(name);

            if (valueResult == ValueProviderResult.None)
            {
                bindingContext.Result = ModelBindingResult.Success(SortType.ByName);
                return Task.CompletedTask;
            }

            var raw = valueResult.FirstValue?.Trim().ToLowerInvariant() ?? string.Empty;

            var result = raw switch
            {
                "by_dist" or "bydist" or "distance" => SortType.ByDist,
                "by_city" or "bycity" or "city" => SortType.ByCity,
                "by_name" or "byname" or "name" => SortType.ByName,
                "" => SortType.ByName,
                _ => SortType.ByName
            };

            bindingContext.Result = ModelBindingResult.Success(result);
            return Task.CompletedTask;
        }
    }
}