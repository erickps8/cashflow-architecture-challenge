namespace CashFlow.Launch.Domain.Notifications;

public interface INotificator
{
    bool HasNotification();

    List<Notification> GetNotifications();

    void Handle(Notification notification);
}