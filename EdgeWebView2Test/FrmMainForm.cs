﻿using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Windows.Forms;
using EdgeWebView2Test.UtilClasses;
using EdgeWebView2Test.VestelSdk;
using EdgeWebView2Test.WebMessages;
using Microsoft.Web.WebView2.Core;

namespace EdgeWebView2Test
{
  public partial class FrmMainForm : Form
  {
    private FrmRemoteControl remoteControl = new FrmRemoteControl();
    private ActivityMonitorInterface activityInterface;
    private Task<CoreWebView2Environment> _webView2Environment = CoreWebView2Environment.CreateAsync();

    public FrmMainForm()
    {
      InitializeComponent();
      webView.CoreWebView2Ready += WebView_CoreWebView2Ready;

      remoteControl.RemoteKeyPressed += RemoteControl_RemoteKeyPressed;
      remoteControl.Show();
    }

    private void RemoteControl_RemoteKeyPressed(int virtualKeyCode)
    {
      PressKey(virtualKeyCode);
      webView.Focus();
      // Temp hack for now, but need to add setting later to allow HbbTV webapp
      // override the back button using an AppObject Keyset.  This is to maintain
      // the default behaviour observed on the actual TV set
      if (virtualKeyCode == 461)
      {
        if (webView.CanGoBack)
        {
          webView.GoBack();
          webView.Focus();
        }
      }

    }

    public void PressKey(int code)
    {
      WebMessageKeyPress keyPress = new WebMessageKeyPress()
      {
        keyCode = code
      };

      PostMessageSender.SendWebMessage(keyPress, webView.CoreWebView2);

      // Activity monitor keeps track of last keypress time, so we need to tell it when a key has been pressed
      activityInterface.RegisterKeyPress();
    }

    private void CoreWebView2_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
      string mimeType = e.WebMessageAsJson;
      webView.CoreWebView2.PostWebMessageAsString("true");
    }

    private void WebView_CoreWebView2Ready(object sender, EventArgs e)
    {
      webView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;

      //webView.CoreWebView2.AddWebResourceRequestedFilter("http://cheese/", CoreWebView2WebResourceContext.All);
      //webView.CoreWebView2.WebResourceRequested += CoreWebView2_WebResourceRequested;


      // Main browser polyfill (For message and event hanldling etc)
      webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(ResourceLoader.GetJsResource("BrowserInternals.js"));

      // Vestel SDK Interfaces vA0.101 (Must be instantiated FIRST as some of the OIPF objects depend on them for data)
      // http://b2bsupport.vestel.com.tr/Contents/Documents/SDK/D0301_HTML_App_Supported_Functionalities_A0.101/doc/html/APITable.html

      // HbbTV interfaces
      webView.CoreWebView2.AddHostObjectToScript("oipfInternal", new OipfObjectFactory(webView.CoreWebView2));


      // Activity interface needs input from main app, so we do this via a local property
      activityInterface = new ActivityMonitorInterface(webView.CoreWebView2);
      webView.CoreWebView2.AddHostObjectToScript("ActivityMonitorInternal", activityInterface);

      webView.CoreWebView2.AddHostObjectToScript("AsyncDataProviderInternal", new AsyncDataProviderInterface(webView.CoreWebView2));

      webView.CoreWebView2.AddHostObjectToScript("DateTimeSettingsInternal", new DateTimeSettingsInterface(webView.CoreWebView2));

      webView.CoreWebView2.AddHostObjectToScript("DisplaySettingsInternal", new DisplaySettingsInterface(webView.CoreWebView2));


      

    //-----------------------------------------------------------------------------------------------------------------------------------------------

    // Open dev tools by default
    webView.CoreWebView2.OpenDevToolsWindow();

    }

    private void CoreWebView2_WebResourceRequested(object sender, CoreWebView2WebResourceRequestedEventArgs e)
    {
      HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
      response.Content = new ByteArrayContent(File.ReadAllBytes(@"c:\\test.png"));
      response.Content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
      //response.StatusCode = HttpStatusCode.OK;

      e.Response = response;

    }

    private void btnGo_Click(object sender, EventArgs e)
    {
      if (webView != null && webView.CoreWebView2 != null && !String.IsNullOrEmpty(txtAddressBar.Text))
      {
        webView.CoreWebView2.Navigate(txtAddressBar.Text);
        webView.Focus();
      }

      WebMessageGenericEvent jsEvent = new WebMessageGenericEvent("onFoobar", new { teflon = "bumblefart" });
      PostMessageSender.SendWebMessage(jsEvent, webView.CoreWebView2);

    }

    private void btnRefresh_Click(object sender, EventArgs e)
    {
      webView.CoreWebView2.Reload();
      webView.Focus();
    }

    private void btnBack_Click(object sender, EventArgs e)
    {
      if (webView.CanGoBack)
      {
        webView.GoBack();
        webView.Focus();
      }
    }

  }
}
