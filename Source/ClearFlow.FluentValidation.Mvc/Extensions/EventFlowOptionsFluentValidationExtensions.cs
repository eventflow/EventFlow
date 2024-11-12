using MicroElements.Swashbuckle.FluentValidation.AspNetCore;
using Microsoft.Extensions.DependencyInjection;
using SharpGrip.FluentValidation.AutoValidation.Mvc.Extensions;

namespace ClearFlow.FluentValidation.Mvc.Extensions;
public static class EventFlowOptionsFluentValidationMvcExtensions
{
    public static IServiceCollection UseEventFlowFluentValidationMvc(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddHttpContextAccessor();
        serviceCollection.AddFluentValidationAutoValidation();

        return serviceCollection;
    }
    public static IServiceCollection UseEventFlowFluentValidationMvcWithSwagger(this IServiceCollection serviceCollection)
    {
        serviceCollection.UseEventFlowFluentValidationMvc();
        serviceCollection.AddFluentValidationRulesToSwagger();

        return serviceCollection;
    }
}
