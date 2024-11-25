﻿// <autogenerated />
#pragma warning disable CS0114
#pragma warning disable CS0108
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Uno.UI;
using Uno.UI.Xaml;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Shapes;
using Windows.UI.Text;
using Uno.Extensions;
using Uno;
using Uno.UI.Helpers;
using Uno.UI.Helpers.Xaml;
using MyProject;

#if __ANDROID__
using _View = Android.Views.View;
#elif __IOS__
using _View = UIKit.UIView;
#elif __MACOS__
using _View = AppKit.NSView;
#else
using _View = Microsoft.UI.Xaml.UIElement;
#endif

namespace TestRepro
{
	partial class XuidGeneratorError : global::Microsoft.UI.Xaml.Controls.ContentDialog
	{
		[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
		private const string __baseUri_prefix_ContentDialog1_4483f06b3f5899cc3b98f0345eeea8e5 = "ms-appx:///TestProject/";
		[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
		private const string __baseUri_ContentDialog1_4483f06b3f5899cc3b98f0345eeea8e5 = "ms-appx:///TestProject/";
		private global::Microsoft.UI.Xaml.NameScope __nameScope = new global::Microsoft.UI.Xaml.NameScope();
		private void InitializeComponent()
		{
			NameScope.SetNameScope(this, __nameScope);
			var __that = this;
			base.IsParsing = true;
			// Source 0\ContentDialog1.xaml (Line 1:2)
			;
			
			PrimaryButtonText = global::Uno.UI.Helpers.MarkupHelper.GetResourceStringForXUid("TestProject/Resources", "XuidGeneratorErrorUid/PrimaryButtonText");
			
			this
			.GenericApply(((c0) => 
			{
				// Source 0\ContentDialog1.xaml (Line 1:2)
				
				// WARNING Property c0.base does not exist on {http://schemas.microsoft.com/winfx/2006/xaml/presentation}ContentDialog, the namespace is http://www.w3.org/XML/1998/namespace. This error was considered irrelevant by the XamlFileGenerator
			}
			))
			.GenericApply(((c1) => 
			{
				// Class TestRepro.XuidGeneratorError
				global::Uno.UI.Helpers.MarkupHelper.SetXUid(c1, "XuidGeneratorErrorUid");
				global::Uno.UI.FrameworkElementHelper.SetBaseUri(c1, __baseUri_ContentDialog1_4483f06b3f5899cc3b98f0345eeea8e5);
				c1.CreationComplete();
			}
			))
			;
			OnInitializeCompleted();

		}
		partial void OnInitializeCompleted();
	}
}
namespace MyProject
{
	static class ContentDialog1_4483f06b3f5899cc3b98f0345eeea8e5XamlApplyExtensions
	{
	}
}
