using System;
using TickTickUtils.Lang;
using ticktick_WPF.Views.AI;

namespace ticktick_WPF.UriScheme.Controller;

[UriRoute("v1")]
internal class ProviderController : UriControllerBase
{
    [UriRoute("import-provider")]
    public void ImportProvider(string name, string base_url, string api_key, string model,
        double? temperature = null, int? max_tokens = null, string note = null)
    {
        try
        {
            AIProviderStore.Load();
            AIProviderStore.Import(name, base_url, api_key, model, temperature, max_tokens, note);
            UtilLog.Info("Provider imported via deep link: " + name);
        }
        catch (Exception ex)
        {
            UtilLog.Error(ex);
        }
    }
}
