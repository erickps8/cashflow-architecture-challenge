namespace CashFlow.Consolidation.Domain.Notifications;

public interface INotificator
{
    bool HasNotification();

    List<Notification> GetNotifications();

    void Handle(Notification notification);
}