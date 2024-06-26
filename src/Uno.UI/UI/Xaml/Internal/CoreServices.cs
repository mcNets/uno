﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// corep.h, xpcore.cpp

#nullable enable

using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Uno.UI.Xaml.Core
{
	internal class CoreServices
	{
		private static Lazy<CoreServices> _instance = new Lazy<CoreServices>(() => new CoreServices());

		private VisualTree? _mainVisualTree;

		public CoreServices()
		{
			ContentRootCoordinator = new ContentRootCoordinator(this);
		}

		// TODO Uno: This will not be a singleton when multi-window setups are supported.
		public static CoreServices Instance => _instance.Value;

		/// <summary>
		/// Provides the content root coordinator.
		/// </summary>
		public ContentRootCoordinator ContentRootCoordinator { get; }

		/// <summary>
		/// Initialization type.
		/// </summary>
		public InitializationType InitializationType { get; internal set; } =
#if HAS_UNO_WINUI
			InitializationType.IslandsOnly;
#else
			InitializationType.MainView;
#endif

		public RootVisual? MainRootVisual => _mainVisualTree?.RootVisual;

		public PopupRoot? MainPopupRoot => _mainVisualTree?.PopupRoot;

		public Canvas? MainFocusVisualRoot => _mainVisualTree?.FocusVisualRoot;

		public FullWindowMediaRoot? MainFullWindowMediaRoot => _mainVisualTree?.FullWindowMediaRoot;

		public VisualTree? MainVisualTree => _mainVisualTree;

		public DependencyObject? VisualRoot => _mainVisualTree?.PublicRootVisual;

		internal void InitCoreWindowContentRoot()
		{
			if (_mainVisualTree is not null)
			{
				return;
			}

			var contentRoot = ContentRootCoordinator.CreateContentRoot(ContentRootType.CoreWindow, ThemingHelper.GetRootVisualBackground(), null);
			_mainVisualTree = contentRoot.VisualTree;

			//TODO Uno: Add input services
			//m_inputServices.attach(new CInputServices(this));

			//// While the tree is loading, delay async processing (such as downloads and drawing)
			//// until we're ready to raise the Loaded event and render the first frame.
			//if (m_pBrowserHost)
			//{
			//	m_isMainTreeLoading = TRUE;
			//}
		}

		internal bool IsXamlVisible()
		{
			// TODO Uno: This is currently highly simplified, adjust when all islands are rooted under main tree.
			return ContentRootCoordinator.CoreWindowContentRoot?.CompositionContent.IsSiteVisible ?? false;
		}

		[NotImplemented]
		internal void UIARaiseFocusChangedEventOnUIAWindow(DependencyObject sender)
		{
		}
	}
}
