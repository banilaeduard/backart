using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Linq;
using WebApi.Entities;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class AuthorizeAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        bool hasAllowAnonymous = context.ActionDescriptor.EndpointMetadata
                                 .Any(em => em.GetType() == typeof(AllowAnonymousAttribute));

        if (hasAllowAnonymous) return;

        User user = null;
        if (context.HttpContext.Items.ContainsKey("User"))
        {
            user = (User)context.HttpContext.Items["User"];
        }

        if (user == null)
        {
            // not logged in
            context.Result = new JsonResult(new { message = "Unauthorized" })
            { StatusCode = StatusCodes.Status401Unauthorized };
            return;
        }
        if (context.HttpContext.Items.ContainsKey("confirmedEmail"))
        {
            var confirmedEmail = (Boolean)context.HttpContext.Items["confirmedEmail"];

            if (!confirmedEmail)
            {
                context.Result = new JsonResult(new { message = "Confirmati userul accesand link-ul trimis pe email" })
                { StatusCode = StatusCodes.Status403Forbidden };
                return;
            }
        }
        if (context.HttpContext.Items.ContainsKey("isLockedOut"))
        {
            var isLockedOut = (Boolean)context.HttpContext.Items["isLockedOut"];
            if (isLockedOut)
            {
                context.Result = new JsonResult(new { message = "Contul este blocat, prea multe incercari de a introduce o parola gresita probabil" })
                { StatusCode = StatusCodes.Status403Forbidden };
                return;
            }
        }
    }
}