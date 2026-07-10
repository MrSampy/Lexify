using Microsoft.AspNetCore.Mvc;

namespace Lexify.API.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class SuperAdminOnlyAttribute() : TypeFilterAttribute(typeof(SuperAdminOnlyFilter));
