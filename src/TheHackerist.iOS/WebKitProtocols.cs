using Foundation;
using WebKit;

namespace TheHackerist.iOS;

[Register("THWebKitProtocols")]
[Preserve(AllMembers = true)]
internal sealed class WebKitProtocols : NSObject, IWKNavigationDelegate, IWKScriptMessageHandler
{
    public void DidReceiveScriptMessage(WKUserContentController userContentController, WKScriptMessage message)
    {
    }
}
