
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using OpenBreweryApi.Models.Requests;

namespace OpenBreweryApi.ModelBinders
{
    public class SortDirModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            var name = bindingContext.FieldName ?? bindingContext.ModelName;
            var valueResult = bindingContext.ValueProvider.GetValue(name);

            if (valueResult == ValueProviderResult.None)
            {
                bindingContext.Result = ModelBindingResult.Success(SortDir.Asc);
                return Task.CompletedTask;
            }

            var raw = valueResult.FirstValue?.Trim().ToLowerInvariant() ?? string.Empty;

            var result = raw switch
            {
                "desc" => SortDir.Desc,
                "asc" or "" => SortDir.Asc,
                _ => SortDir.Asc
            };

            bindingContext.Result = ModelBindingResult.Success(result);
            return Task.CompletedTask;
        }
    }
}