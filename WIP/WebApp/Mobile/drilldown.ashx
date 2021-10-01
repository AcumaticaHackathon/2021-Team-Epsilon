<%@ WebHandler Language="C#" Class="drilldown" %>

using System;
using System.Web;
using System.IO;
using PX.Common;
using PX.Web.UI;
using PX.Data.Api.Services;

public class drilldown : IHttpHandler, System.Web.SessionState.IRequiresSessionState
{
    private readonly ICustomFilterSerializer _customFilterSerializer;
    public drilldown()
    {
            _customFilterSerializer = new CustomFilterSerializer();
    }
    public void ProcessRequest(HttpContext context)
    {
        context.Response.ContentType = "application/json";

        var filters = PXContext.SessionTyped<PXSessionStateWebUI>().RedirectFilters[context.Request.Url.PathAndQuery];
        string filterLine = null;
        string displayName = string.Empty;

        if (filters != null && filters.Length > 0)
        {
            filterLine = _customFilterSerializer.Serialize(filters);
            displayName = filters[0].Name;
        }
        var response = new
        {
            Redirect = new
            {
                ScreenId = context.Request.QueryString["screenId"],
                Filters = filterLine,
                DisplayName = displayName
            }
        };

        using (var sw = new StreamWriter(context.Response.OutputStream))
        {
            (new Newtonsoft.Json.JsonSerializer()).Serialize(sw, response);
        }
    }

    public bool IsReusable
    {
        get
        {
            return false;
        }
    }
}