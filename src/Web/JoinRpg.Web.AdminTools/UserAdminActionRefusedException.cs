namespace JoinRpg.Web.AdminTools;

/// <summary>
/// Штатный отказ в действии администратора над пользователем (например, недопустимый email).
/// Сообщение показывается администратору как есть.
/// </summary>
public class UserAdminActionRefusedException(string message) : Exception(message);
