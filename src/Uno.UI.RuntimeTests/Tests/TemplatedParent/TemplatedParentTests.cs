﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Extensions;
using Uno.UI.Helpers;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.RuntimeTests.Tests.TemplatedParent.Setup;
#if HAS_UNO
using Uno.UI.Xaml;
#endif
using WindowHelper = Private.Infrastructure.TestServices.WindowHelper;

namespace Uno.UI.RuntimeTests.Tests.TemplatedParent;

[TestClass]
[RunsOnUIThread]
public partial class TemplatedParentTests
{
	[TestMethod]
	public async Task Uno8049_Test()
	{
		var setup = new Uno8049();
		await UITestHelper.Load(setup);

		var tree = setup.TreeGraph(DebugVT_TP);
		var sut = setup.Content as TextBox ?? throw new Exception("Invalid content root");
		var cc = sut.FindFirstDescendantOrThrow<ContentControl>("HeaderTemplate_ContentControl");
		var tb = cc.FindFirstDescendantOrThrow<TextBlock>("Header_TextBlock");

		Assert.IsNotNull(sut.Header);
		Assert.AreEqual(sut.Header, tb);
	}

	[TestMethod]
	public async Task Uno9059_Test()
	{
		try
		{
			var setup = new Uno9059();
			await UITestHelper.Load(setup);

			var sut = setup.Content as Uno9059_CustomControl ?? throw new Exception("Invalid content root");
			var button = sut.FindFirstDescendantOrThrow<Button>("CustomControl_Button");

			button.Flyout.ShowAt(button);
			await UITestHelper.WaitForIdle();

			var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(button.XamlRoot);
			var presenter = popups.FirstOrDefault(x => x.Child is MenuFlyoutPresenter).Child as MenuFlyoutPresenter ??
				throw new Exception("Failed to find opened MenuFlyoutPresenter");
			var tree = presenter?.TreeGraph(DebugVT_TP);
			var mfi = presenter.FindFirstDescendantOrThrow<MenuFlyoutItem>("Button_MenuFlyoutItem");

			Assert.IsNotNull(sut.Action1);
			Assert.AreEqual(sut.Action1, mfi.Command);
		}
		finally
		{
			UITestHelper.CloseAllPopups();
		}
	}

	[TestMethod]
	public async Task Uno12624_Test()
	{
		var setup = new Uno12624();
		await UITestHelper.Load(setup);

		var tree = setup.TreeGraph(DebugVT_TP);
		var expectations = """
		0.	Uno12624_LeftRightControl#RootLeftRight // TP=<null>
		1.		Uno12624_WestEastControl#LeftRightControl_Template_Root // TP=Uno12624_LeftRightControl#RootLeftRight
		2.			StackPanel#WestEastControl_Template_RootPanel // TP=Uno12624_WestEastControl#LeftRightControl_Template_Root
		3.				ContentControl#WestEastControl_Template_WestContent // TP=Uno12624_WestEastControl#LeftRightControl_Template_Root
		4.					ContentPresenter // TP=ContentControl#WestEastControl_Template_WestContent
		5.						ContentControl#LeftRightControl_Template_LeftContent // TP=Uno12624_LeftRightControl#RootLeftRight
		6.							ContentPresenter // TP=ContentControl#LeftRightControl_Template_LeftContent
		7.								TextBlock#LeftRightControl_Left // TP=<null>
		8.				ContentControl#WestEastControl_Template_EastContent // TP=Uno12624_WestEastControl#LeftRightControl_Template_Root
		9.					ContentPresenter // TP=ContentControl#WestEastControl_Template_EastContent
		10.						ContentControl#LeftRightControl_Template_RightContent // TP=Uno12624_LeftRightControl#RootLeftRight
		11.							ContentPresenter // TP=ContentControl#LeftRightControl_Template_RightContent
		12.								TextBlock#LeftRightControl_Right // TP=<null>
		""";
#if __ANDROID__ || __IOS__
		// skip ContentControls' ContentPresenter that were bypassed
		expectations = SkipLines(expectations, 4, 6, 9, 11);
#endif

		VerifyTree(expectations, setup);
	}

	[TestMethod]
	public async Task Uno17313_Test()
	{
		var setup = new Uno17313();
		await UITestHelper.Load(setup);

		var tree = setup.TreeGraph(DebugVT_TP);
		var expectations = """
		0	Uno17313_SettingsExpander // TP=<null>
		1		Uno17313_Expander // TP=Uno17313_SettingsExpander
		2			StackPanel // TP=Uno17313_Expander
		3				ContentControl // TP=Uno17313_Expander
		4					ContentPresenter // TP=ContentControl
		5						Uno17313_SettingsCard // TP=Uno17313_SettingsExpander
		6							Grid // TP=Uno17313_SettingsCard
		7								ContentPresenter#PART_HeaderPresenter // TP=Uno17313_SettingsCard
		8									ImplicitTextBlock // TP=ContentPresenter#PART_HeaderPresenter
		9								ContentPresenter#PART_ContentPresenter // TP=Uno17313_SettingsCard
		10									TextBlock // TP=<null>
		11				ContentPresenter // TP=Uno17313_Expander
		12					Border // TP=Uno17313_SettingsExpander
		""";
#if __ANDROID__ || __IOS__
		// skip ContentControls' ContentPresenter that were bypassed
		// ContentPresenter on line#11 is from the control template, so that doesn't count
		expectations = SkipLines(expectations, 4);
#endif

		VerifyTree(expectations, setup);
	}

	[TestMethod]
	public async Task ContentControl_Content_Test()
	{
		var setup = new ContentControl_Content();
		await UITestHelper.Load(setup, x => x.IsLoaded);

		// direct content members should NOT have content-presenter as templated-parent.
		var tree = setup.SUT.TreeGraph(DebugVT_TP);
		var expectations = """
		0	ContentControl#SUT // TP=<null>
		1		ContentPresenter // TP=ContentControl#SUT
		2			StackPanel // TP=<null>
		3				TextBlock // TP=<null>
		""";
		VerifyTree(expectations, setup);
	}

	[TestMethod]
	public async Task ContentControl_ContentTemplate_Test()
	{
		var setup = new ContentControl_ContentTemplate();
		await UITestHelper.Load(setup, x => x.IsLoaded);

		// data-template members should have content-presenter as templated-parent.
		var tree = setup.SUT.TreeGraph(DebugVT_TP);
		var expectations = """
		0	ContentControl#SUT // TP=<null>
		1		ContentPresenter // TP=ContentControl#SUT
		2			StackPanel // TP=ContentPresenter
		3				TextBlock // TP=ContentPresenter
		""";
		VerifyTree(expectations, setup);
	}

	[TestMethod]
	public async Task ItemsControl_HeaderFooter_NoTP_PreCompiledXaml()
	{
		var setup = new ItemsControl_HeaderFooter();
		var sut = setup.SUT;

		await ItemsControl_HeaderFooter_NoTP(setup, sut);
	}

	[TestMethod]
	public async Task ItemsControl_HeaderFooter_NoTP_DynamicXaml()
	{
		var sut = new ItemsControl()
		{
			ItemsPanel = XamlHelper.LoadXaml<ItemsPanelTemplate>("""
				<ItemsPanelTemplate>
					<StackPanel />
				</ItemsPanelTemplate>
			"""),
			Template = XamlHelper.LoadXaml<ControlTemplate>("""
				<ControlTemplate TargetType="ItemsControl">
					<ItemsPresenter Header="asd" />
				</ControlTemplate>
			"""),
		};

		await ItemsControl_HeaderFooter_NoTP(sut, sut);
	}

	public async Task ItemsControl_HeaderFooter_NoTP(FrameworkElement setup, ItemsControl sut)
	{
		await UITestHelper.Load(setup, x => x.IsLoaded);

		var tree = sut.TreeGraph(DebugVT_TP);
		var presenter = sut.FindFirstDescendantOrThrow<ItemsPresenter>();
		var children = presenter.GetChildren().ToArray();

		Assert.IsInstanceOfType<ContentControl>(children[0], "Header ContentControl not found.");
		Assert.IsInstanceOfType<StackPanel>(children[1], "StackPanel not found.");
		Assert.IsInstanceOfType<ContentControl>(children[2], "Footer ContentControl not found.");

		Assert.IsNull(GetTemplatedParentCompat(children[0] as FrameworkElement), "Injected Header ContentControl should not have a templated-parent.");
		Assert.IsNull(GetTemplatedParentCompat(children[2] as FrameworkElement), "Injected Header ContentControl should not have a templated-parent.");
	}

	[TestMethod]
	public async Task ItemsControl_ItemTemplate_Test()
	{
		var setup = new ItemsControl_ItemTemplate();
		setup.SUT.ItemsSource = new[] { 1, 2, 3 };
		await UITestHelper.Load(setup, x => x.IsLoaded);

		// item-template members should have content-presenter as templated-parent.
		var tree = setup.SUT.TreeGraph(DebugVT_TP);
		var expectations = """
		0	ItemsControl#SUT // TP=<null>
		1		ItemsPresenter // TP=ItemsControl#SUT
		2			ContentControl // TP=<null>
		3				ContentPresenter // TP=ContentControl
		4			StackPanel // TP=ItemsPresenter
		5				ContentPresenter // TP=<null>
		6					TextBlock // TP=ContentPresenter
		7				ContentPresenter // TP=<null>
		8					TextBlock // TP=ContentPresenter
		9				ContentPresenter // TP=<null>
		10					TextBlock // TP=ContentPresenter
		11			ContentControl // TP=<null>
		12				ContentPresenter // TP=ContentControl
		""";
		VerifyTree(expectations, setup);
	}
}
public partial class TemplatedParentTests
{
	private static string SkipLines(string tree, params int[] lines)
	{
		return string.Join('\n', tree.Split('\n')
			.Where((x, i) => !lines.Contains(i))
		);
	}

	private static void VerifyTree(string expectedTree, FrameworkElement root)
	{
		var expectations = expectedTree.Split('\n', StringSplitOptions.TrimEntries);
		var descendants = root.EnumerateDescendants().ToArray();

		Assert.AreEqual(expectations.Length, descendants.Length);
		for (int i = 0; i < expectations.Length; i++)
		{
			var line = expectations[i].TrimStart("\t 0123456789.".ToArray());
			var match = line.Split(" // TP=", count: 2);

			var node = descendants[i];
			var tp = GetTemplatedParentCompat(node as FrameworkElement);

			Assert.AreEqual(2, match.Length, $"Failed to parse expectation on line {i}");
			Assert.AreEqual(match[0], DescribeObject(node), $"Invalid node on line {i}");
			Assert.AreEqual(match[1], DescribeObject(tp), $"Invalid templated-parent on line {i}");
		}
	}


	private static object GetTemplatedParentCompat(FrameworkElement fe)
	{
		if (fe is null)
		{
			return null;
		}

#if HAS_UNO
		return fe.GetTemplatedParent();
#else
		fe.SetBinding(FrameworkElement.TagProperty, new Binding
		{
			RelativeSource = new() { Mode = RelativeSourceMode.TemplatedParent }
		});
		var tp = fe.Tag;

		fe.ClearValue(FrameworkElement.TagProperty);
		return tp;
#endif
	}
}
public partial class TemplatedParentTests
{
	private static IEnumerable<string> DebugVT_TP(object x)
	{
		if (x is FrameworkElement fe)
		{
			yield return $"TP={DescribeObject(GetTemplatedParentCompat(fe))}";
		}
	}

	private static string DescribeObject(object x)
	{
		if (x == null) return "<null>";
		if (x is FrameworkElement { Name: { Length: > 0 } name }) return $"{x.GetType().Name}#{name}";
		return x.GetType().Name;
	}
}
