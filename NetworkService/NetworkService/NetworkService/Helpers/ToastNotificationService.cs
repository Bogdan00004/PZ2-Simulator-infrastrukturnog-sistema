using Notification.Wpf;
using System.Windows;

namespace NetworkService.Helpers
{
    public static class ToastNotificationService
    {
        private static readonly NotificationManager _manager = new NotificationManager();

        public static void ShowSuccess(string title, string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _manager.Show(new NotificationContent
                {
                    Title = title,
                    Message = message,
                    Type = NotificationType.Success
                });
            });
        }

        public static void ShowWarning(string title, string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _manager.Show(new NotificationContent
                {
                    Title = title,
                    Message = message,
                    Type = NotificationType.Warning
                });
            });
        }

        public static void ShowError(string title, string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _manager.Show(new NotificationContent
                {
                    Title = title,
                    Message = message,
                    Type = NotificationType.Error
                });
            });
        }
    }
}