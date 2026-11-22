using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Ochag.Validation;

namespace RestaurantApi.Attributes;

public sealed class ValidateInputFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ModelState.IsValid)
        {
            context.Result = new BadRequestObjectResult(new { status = false, message = "Проверьте формат полей: числа, даты и обязательные значения." });
            return;
        }
        // Only body parameters; route IDs, cancellation tokens and provider webhooks are not form data.
        var path = context.HttpContext.Request.Path.Value ?? "";
        foreach (var parameter in context.ActionDescriptor.Parameters.Where(p => p.BindingInfo?.BindingSource == BindingSource.Body))
        {
            if (!context.ActionArguments.TryGetValue(parameter.Name, out var value) || value == null)
            {
                context.Result = new BadRequestObjectResult(new { status = false, message = "Заполните данные формы." });
                return;
            }
            var errors = InputRules.Validate(path, JsonSerializer.SerializeToElement(value));
            if (errors.Count == 0) continue;
            context.Result = new BadRequestObjectResult(new { status = false, message = string.Join(" ", errors.Values.Distinct()), errors });
            return;
        }
    }
    public void OnActionExecuted(ActionExecutedContext context) { }
}
