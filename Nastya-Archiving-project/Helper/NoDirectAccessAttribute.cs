using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Nastya_Archiving_project.Helper
{
    public class NoDirectAccessAttribute : ActionFilterAttribute
    {
        //that code is used to prevent direct access to the page
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (filterContext.HttpContext.Request.Headers["Referer"].Count == 0)
            {
                filterContext.Result = new NotFoundResult();
            }
        }
    }
}
