﻿#nullable enable 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;


namespace Windows.ApplicationModel.Appointments
{
	public partial class AppointmentManager
	{

		public static IAsyncAction ShowTimeFrameAsync(DateTimeOffset timeToShow, TimeSpan duration)
			=> ShowTimeFrameAsyncTask(timeToShow, duration).AsAsyncAction();
		private static async Task ShowTimeFrameAsyncTask(DateTimeOffset timeToShow, TimeSpan duration)
		{
			var builder = Android.Provider.CalendarContract.ContentUri?.BuildUpon();
			if (builder == null)
			{
				throw new NullReferenceException("Windows.ApplicationModel.Appointments.AppointmentStore.FindAppointmentsAsyncTask, builder is null (impossible)");
			}

			builder.AppendPath("time");
			Android.Content.ContentUris.AppendId(builder, timeToShow.ToUniversalTime().ToUnixTimeMilliseconds());
			var intent = new Android.Content.Intent(Android.Content.Intent.ActionView).SetData(builder.Build());
			Android.App.Application.Context.StartActivity(intent);
		}


		public static IAsyncOperation<AppointmentStore> RequestStoreAsync(AppointmentStoreAccessType options)
			=> RequestStoreAsyncTask(options).AsAsyncOperation<AppointmentStore>();

		public static async Task<AppointmentStore?> RequestStoreAsyncTask(AppointmentStoreAccessType options)
		{
			// UWP: AppCalendarsReadWrite, AppCalendarsReadOnly,AppCalendarsReadWrite (cannot be used without special provisioning by Microsoft)
			// Android: Manifest has READ_CALENDAR and WRITE_CALENDAR, no difference between app/limited/full
			// using only AllCalendarsReadOnly, other: throw NotImplementedException

			if (options != AppointmentStoreAccessType.AllCalendarsReadOnly)
			{
				throw new NotSupportedException("AppointmentManager:RequestStoreAsync - only AllCalendarsReadOnly is implemented");
			}

			string requiredPermission = Android.Manifest.Permission.ReadCalendar;

			if (!Windows.Extensions.PermissionsHelper.IsDeclaredInManifest(requiredPermission))
			{
				throw new UnauthorizedAccessException("AppointmentManager:RequestStoreAsync - no ReadCalendar permission declared in Manifest");
			}

			if (!await Windows.Extensions.PermissionsHelper.CheckPermission(CancellationToken.None, requiredPermission))
			{
				if (!await Windows.Extensions.PermissionsHelper.TryGetPermission(CancellationToken.None, requiredPermission))
				{
					// it returns null, so it is not AppointmentStore; and UWP defined this as AppointmentStore.
					return null;
				}
			}

			return new AppointmentStore();

		}
	}
}
