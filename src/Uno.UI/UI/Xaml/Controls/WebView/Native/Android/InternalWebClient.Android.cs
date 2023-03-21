﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Uno.UI.Xaml.Controls;

internal class InternalClient : Android.Webkit.WebViewClient
{
	private readonly WebView _webView;
	//_owner is because we go through onReceivedError() and OnPageFinished() when the call fail.
	private bool _webViewSuccess = true;
	//_owner is to not have duplicate event call
	private WebErrorStatus _webErrorStatus = WebErrorStatus.Unknown;

	internal InternalClient(WebView webView)
	{
		_webView = webView;

		if (FeatureConfiguration.WebView.ForceSoftwareRendering)
		{
			//SetLayerType disables hardware acceleration for a single view.
			//_owner is required to remove glitching issues particularly when having a keyboard pop-up with a webview present.
			//http://developer.android.com/guide/topics/graphics/hardware-accel.html
			//http://stackoverflow.com/questions/27172217/android-systemui-glitches-in-lollipop
			_webView.SetLayerType(LayerType.Software, null);
		}
	}

#pragma warning disable CS0672 // Member overrides obsolete member
	internal override bool ShouldOverrideUrlLoading(Android.Webkit.WebView view, string url)
#pragma warning restore CS0672 // Member overrides obsolete member
	{
		if (url.StartsWith(Uri.UriSchemeMailto, true, CultureInfo.InvariantCulture))
		{
			_webView.CreateAndLaunchMailtoIntent(view.Context, url);
			return true;
		}

		var args = new WebViewNavigationStartingEventArgs(new Uri(url));

		_webView.NavigationStarting?.Invoke(_webView, args);

		return args.Cancel;
	}

	internal override void OnPageStarted(Android.Webkit.WebView view, string url, Bitmap favicon)
	{
		base.OnPageStarted(view, url, favicon);
		//Reset Webview Success on page started so that if we have successful navigation we don't send an webView error if a previous error happened.
		_webViewSuccess = true;
	}

#pragma warning disable 0672, 618
	internal override void OnReceivedError(Android.Webkit.WebView view, [GeneratedEnum] ClientError errorCode, string description, string failingUrl)
	{
		_webViewSuccess = false;
		_webErrorStatus = ConvertClientError(errorCode);

		base.OnReceivedError(view, errorCode, description, failingUrl);
	}
#pragma warning restore 0672, 618

	internal override void OnPageFinished(Android.Webkit.WebView view, string url)
	{
		_webView.DocumentTitle = view.Title;

		_webView.OnNavigationHistoryChanged();

		var uri = !_webView._wasLoadedFromString && !string.IsNullOrEmpty(url) ? new Uri(url) : null;
		var args = new WebViewNavigationCompletedEventArgs(_webViewSuccess, uri, _webErrorStatus);

		_webView.NavigationCompleted?.Invoke(_webView, args);
		base.OnPageFinished(view, url);
	}

	//Matched using these two sources
	//http://developer.xamarin.com/api/type/Android.Webkit.ClientError/
	//https://msdn.microsoft.com/en-ca/library/windows/apps/windows.web.weberrorstatus
	private WebErrorStatus ConvertClientError(ClientError clientError)
	{
		switch (clientError)
		{
			case ClientError.Authentication:
				return WebErrorStatus.Unauthorized;
			case ClientError.BadUrl:
				return WebErrorStatus.BadRequest;
			case ClientError.Connect:
				return WebErrorStatus.CannotConnect;
			case ClientError.FailedSslHandshake:
				return WebErrorStatus.UnexpectedClientError;
			case ClientError.File:
				return WebErrorStatus.UnexpectedClientError;
			case ClientError.FileNotFound:
				return WebErrorStatus.NotFound;
			case ClientError.HostLookup:
				return WebErrorStatus.HostNameNotResolved;
			case ClientError.Io:
				return WebErrorStatus.InternalServerError;
			case ClientError.ProxyAuthentication:
				return WebErrorStatus.ProxyAuthenticationRequired;
			case ClientError.RedirectLoop:
				return WebErrorStatus.RedirectFailed;
			case ClientError.Timeout:
				return WebErrorStatus.Timeout;
			case ClientError.TooManyRequests:
				return WebErrorStatus.UnexpectedClientError;
			case ClientError.Unknown:
				return WebErrorStatus.Unknown;
			case ClientError.UnsupportedAuthScheme:
				return WebErrorStatus.Unauthorized;
			case ClientError.UnsupportedScheme:
				return WebErrorStatus.UnexpectedClientError;
			default:
				return WebErrorStatus.UnexpectedClientError;
		}
	}
}
