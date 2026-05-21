namespace CashFlow.Consolidation.Domain.Notifications;

public class Notificator : INotificator
{
    private readonly List<Notification> _notifications = new();

    public bool HasNotification()
    {
        return _notifications.Any();
    }

    public List<Notification> GetNotifications()
    {
        return _notifications;
    }

    public void Handle(Notification notification)
    {
        _notifications.Add(notification);
    }
}