using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Razor;

namespace XerifeTv.CMS.Shared.Extensions;

/// <summary>
/// Resolves views from the module that owns the current controller.
/// Controllers placed under <c>Modules/{Module}/Controllers</c> look for views in
/// <c>Modules/{Module}/Views/{Controller}</c> and <c>Modules/{Module}/Views/Shared</c>.
/// </summary>
public sealed class ModuleViewLocationExpander : IViewLocationExpander
{
    private const string ModuleKey = "module";
    private const string ModulesNamespacePrefix = "XerifeTv.CMS.Modules.";

    public void PopulateValues(ViewLocationExpanderContext context)
    {
        context.Values[ModuleKey] = GetModulePath(context.ActionContext) ?? string.Empty;
    }

    public IEnumerable<string> ExpandViewLocations(
        ViewLocationExpanderContext context,
        IEnumerable<string> viewLocations)
    {
        var modulePath = context.Values?.TryGetValue(ModuleKey, out var value) == true ? value : null;

        if (!string.IsNullOrEmpty(modulePath))
        {
            yield return $"/Modules/{modulePath}/Views/{context.ControllerName}/{context.ViewName}.cshtml";
            yield return $"/Modules/{modulePath}/Views/Shared/{context.ViewName}.cshtml";
        }

        foreach (var location in viewLocations)
            yield return location;
    }

    private static string? GetModulePath(ActionContext actionContext)
    {
        if (actionContext.ActionDescriptor is not ControllerActionDescriptor descriptor)
            return null;

        var controllerNamespace = descriptor.ControllerTypeInfo.Namespace;

        if (controllerNamespace is null ||
            !controllerNamespace.StartsWith(ModulesNamespacePrefix, StringComparison.Ordinal))
            return null;

        var moduleNamespace = controllerNamespace[ModulesNamespacePrefix.Length..];
        var controllersSegmentIndex = moduleNamespace.IndexOf(".Controllers", StringComparison.Ordinal);

        if (controllersSegmentIndex >= 0)
            moduleNamespace = moduleNamespace[..controllersSegmentIndex];

        return moduleNamespace.Replace('.', '/');
    }
}
