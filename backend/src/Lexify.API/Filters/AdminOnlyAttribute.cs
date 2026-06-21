using Microsoft.AspNetCore.Mvc;

namespace Lexify.API.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class AdminOnlyAttribute() : TypeFilterAttribute(typeof(AdminOnlyFilter));
