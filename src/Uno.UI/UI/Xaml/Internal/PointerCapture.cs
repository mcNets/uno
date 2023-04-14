﻿#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Devices.Input;
using Windows.UI.Xaml.Input;

using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI.Extensions;
using System.Runtime.CompilerServices;
using Windows.UI.Xaml;

namespace Uno.UI.Xaml.Core;

/*
 *	This file is intrinsically linked to the UIElement and uses some of its internal APIs:
 *		internal List<Pointer> PointerCapturesBackingField;
 *		internal partial void CapturePointerNative(Pointer pointer);
 *		internal partial void ReleasePointerNative(Pointer pointer);
 */

/// <summary>
/// This is an helper class that use to manage the captures for a given pointer.
/// </summary>
internal partial class PointerCapture
{
	private static readonly IDictionary<PointerIdentifier, PointerCapture> _actives = new Dictionary<PointerIdentifier, PointerCapture>(EqualityComparer<PointerIdentifier>.Default);

	/// <summary>
	/// Current currently active pointer capture for the given pointer, or creates a new one.
	/// </summary>
	/// <param name="pointer">The pointer to capture</param>
	public static PointerCapture GetOrCreate(Pointer pointer)
		=> _actives.TryGetValue(pointer.UniqueId, out var capture)
			? capture
			: new PointerCapture(pointer); // The capture will be added to the _actives only when a target is added to it.

	internal static bool TryGet(PointerIdentifier pointer, out PointerCapture capture)
		=> _actives.TryGetValue(pointer, out capture);

	public static bool TryGet(Pointer pointer, out PointerCapture capture)
		=> _actives.TryGetValue(pointer.UniqueId, out capture);

	public static bool Any(out List<PointerCapture>? cloneOfAllCaptures)
	{
		if (_actives.Count > 0)
		{
			cloneOfAllCaptures = _actives.Values.ToList();
			return true;
		}
		else
		{
			cloneOfAllCaptures = default;
			return false;
		}
	}

	private UIElement? _nativeCaptureElement;
	private readonly Dictionary<UIElement, PointerCaptureTarget> _targets = new(2);

	private PointerCapture(Pointer pointer)
	{
		Pointer = pointer;
	}

	/// <summary>
	/// The captured pointer
	/// </summary>
	public Pointer Pointer { get; }

	/// <summary>
	/// Gets the <see cref="PointerRoutedEventArgs.FrameId"/> of the last args that has been handled by this capture
	/// </summary>
	public long MostRecentDispatchedEventFrameId { get; private set; }

	/// <summary>
	/// Determines if this capture was made only for implicit kind
	/// (So we should not use it to filter out some event on other controls)
	/// </summary>
	public bool IsImplicitOnly { get; private set; } = true;

	public IEnumerable<PointerCaptureTarget> Targets => _targets.Values;

	internal bool IsTarget(UIElement element, PointerCaptureKind kinds)
		=> _targets.TryGetValue(element, out var target)
			&& (target.Kind & kinds) != PointerCaptureKind.None;

	internal IEnumerable<PointerCaptureTarget> GetTargets(PointerCaptureKind kinds)
		=> _targets
			.Values
			.Where(target => (target.Kind & kinds) != PointerCaptureKind.None);

	internal PointerCaptureResult TryAddTarget(UIElement element, PointerCaptureKind kind, PointerRoutedEventArgs? relatedArgs = null)
	{
		global::System.Diagnostics.Debug.Assert(
			kind is PointerCaptureKind.Explicit or PointerCaptureKind.Implicit,
			"The initial capture kind must be Explicit **OR** Implicit.");

		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"{element.GetDebugName()}: Capturing ({kind}) pointer {Pointer}");
		}

		if (_targets.TryGetValue(element, out var target))
		{
			// Validate if the requested kind is not already handled
			if (target.Kind.HasFlag(kind))
			{
				return PointerCaptureResult.AlreadyCaptured;
			}
			else
			{
				// Add the new kind to the target
				target.Kind |= kind;
			}
		}
		else
		{
			target = new PointerCaptureTarget(element, kind);
			_targets.Add(element, target);

			// If the capture is made while raising an event (usually captures are made in PointerPressed handlers)
			// we re-use the current event args (if they match) to init the target.LastDispatched property.
			// Note:  we don't check the sender as we may capture on another element but the frame ID is still correct.
			if (relatedArgs?.Pointer == Pointer)
			{
				Update(target, relatedArgs);

				// In case of an implicit capture we also override the native element used for the capture.
				// cf. remarks of the PointerCaptureTarget.NativeCaptureElement.
				if (kind == PointerCaptureKind.Implicit)
				{
					target.NativeCaptureElement = relatedArgs?.OriginalSource as UIElement ?? element;
				}
			}
		}

		// If we added an explicit capture, we update the _localExplicitCaptures of the target element
		if (kind == PointerCaptureKind.Explicit)
		{
			IsImplicitOnly = false;
			element.PointerCapturesBackingField.Add(Pointer);
		}

		// Make sure that this capture is effective
		EnsureEffectiveCaptureState();

		return PointerCaptureResult.Added;
	}

	/// <summary>
	/// Removes a UIElement from the targets of this capture.
	/// DO NOT USE directly, use instead the Release method on the UIElement in order to properly raise the PointerCaptureLost event.
	/// </summary>
	internal PointerCaptureKind RemoveTarget(UIElement element, PointerCaptureKind kinds, out PointerRoutedEventArgs? lastDispatched)
	{
		if (!_targets.TryGetValue(element, out var target)
			|| (target.Kind & kinds) == 0) // Validate if any of the requested kinds is handled
		{
			lastDispatched = default;
			return PointerCaptureKind.None;
		}

		var removed = target.Kind & kinds;
		lastDispatched = target.LastDispatched;

		RemoveCore(target, kinds);

		return removed;
	}

	private void Clear()
	{
		foreach (var target in _targets.Values.ToList())
		{
			RemoveCore(target, PointerCaptureKind.Any);
		}
	}

	private void RemoveCore(PointerCaptureTarget target, PointerCaptureKind kinds)
	{
		global::System.Diagnostics.Debug.Assert(
			kinds != PointerCaptureKind.None,
			"The capture kind must be set to release pointer captures.");

		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"{target.Element.GetDebugName()}: Releasing ({kinds}) capture of pointer {Pointer}");
		}

		// If we remove an explicit capture, we update the _localExplicitCaptures of the target element
		if (kinds.HasFlag(PointerCaptureKind.Explicit)
			&& target.Kind.HasFlag(PointerCaptureKind.Explicit))
		{
			target.Element.PointerCapturesBackingField.Remove(Pointer);
		}

		target.Kind &= ~kinds;

		// The element is no longer listening for events, remove it.
		if (target.Kind == PointerCaptureKind.None)
		{
			_targets.Remove(target.Element);
		}

		IsImplicitOnly = _targets.None(t => t.Value.Kind.HasFlag(PointerCaptureKind.Explicit));

		// Validate / update the state of this capture
		EnsureEffectiveCaptureState();
	}

	/// <summary>
	/// Validate if the provided routed event args are relevant for the given element according to the active captures
	/// </summary>
	/// <param name="element">The target element for which the args are validated</param>
	/// <param name="args">The pending pointer event args that is under test</param>
	/// <param name="autoRelease">A flag that allows to automatically release any pending out-dated capture (for PointerDown only)</param>
	/// <returns>A boolean which indicates if the args are valid or not for the given element</returns>
	public bool ValidateAndUpdate(UIElement element, PointerRoutedEventArgs args, bool autoRelease)
	{
		if ((autoRelease && MostRecentDispatchedEventFrameId < args.FrameId)
			|| _nativeCaptureElement?.GetHitTestVisibility() == HitTestability.Collapsed)
		{
			// If 'autoRelease' we want to release any previous capture that was not release properly no matter the reason.
			// BUT we don't want to release a capture that was made by a child control (so MostRecentDispatchedEventFrameId should already be equals to current FrameId).
			// We also do not allow a control that is not loaded to keep a capture (they should all have been release on unload).
			// ** This is an IMPORTANT safety catch to prevent the application to become unresponsive **
			Clear();

			return true;
		}
		else if (_targets.TryGetValue(element, out var target))
		{
			Update(target, args);

			return true;
		}
		else if (IsImplicitOnly)
		{
			// If the capture is implicit, we should not filter out events for children elements.

			return true;
		}
		else
		{
			// We should dispatch the event only if the control which has captured the pointer has already dispatched the event
			// (Which actually means that the current control is a parent of the control which has captured the pointer)
			// Remarks: This is not enough to determine parent-child relationship when we dispatch multiple events base on the same native event,
			//			(as they will all have the same FrameId), however in that case we dispatch events layer per layer
			//			instead of bubbling a single event before raising the next one, so we are safe.
			//			The only limitation would be when mixing native vs. managed bubbling, but this check only prevents
			//			the leaf of the tree to raise the event, so we cannot mix bubbling mode in that case.
			return MostRecentDispatchedEventFrameId >= args.FrameId;
		}
	}

	private void Update(PointerCaptureTarget target, PointerRoutedEventArgs args)
	{
		target.LastDispatched = args;
		if (MostRecentDispatchedEventFrameId < args.FrameId)
		{
			MostRecentDispatchedEventFrameId = args.FrameId;
		}
	}

	private void EnsureEffectiveCaptureState()
	{
		if (_targets.Count > 0)
		{
			// We have some target, self enable us

			if (_actives.TryGetValue(Pointer.UniqueId, out var capture))
			{
				if (capture != this)
				{
					throw new InvalidOperationException("There is already another active capture.");
				}
			}
			else
			{
				// This is what makes this capture active
				_actives.Add(Pointer.UniqueId, this);
			}

			if (_nativeCaptureElement == null)
			{
				_nativeCaptureElement = _targets.Single().Value.NativeCaptureElement;

				CapturePointerNative();
			}
		}
		else
		{
			// We no longer have any target, cleanup

			if (_nativeCaptureElement != null)
			{
				ReleasePointerNative();

				_nativeCaptureElement = null;
			}

			if (_actives.TryGetValue(Pointer.UniqueId, out var capture) && capture == this)
			{
				// This is what makes this capture inactive
				_actives.Remove(Pointer.UniqueId);
			}
		}
	}

	/// <remarks>
	/// This method contains or is called by a try/catch containing method and can
	/// be significantly slower than other methods as a result on WebAssembly.
	/// See https://github.com/dotnet/runtime/issues/56309
	/// </remarks>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void CapturePointerNative()
	{
		try
		{
			if (_nativeCaptureElement is not null)
			{
				CaptureNative(_nativeCaptureElement, Pointer);
			}
		}
		catch (Exception e)
		{
			this.Log().Error($"Failed to capture natively pointer {Pointer}.", e);
		}
	}

	// Implement this to get pointer capture out of the bounds of the app
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	partial void CaptureNative(UIElement target, Pointer pointer);

	/// <remarks>
	/// This method contains or is called by a try/catch containing method and
	/// can be significantly slower than other methods as a result on WebAssembly.
	/// See https://github.com/dotnet/runtime/issues/56309
	/// </remarks>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void ReleasePointerNative()
	{
		try
		{
			if (_nativeCaptureElement is not null)
			{
				ReleaseNative(_nativeCaptureElement, Pointer);
			}
		}
		catch (Exception e)
		{
			this.Log().Error($"Failed to release native capture of {Pointer}", e);
		}
	}

	// Implement this to get pointer capture out of the bounds of the app
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	partial void ReleaseNative(UIElement target, Pointer pointer);
}
