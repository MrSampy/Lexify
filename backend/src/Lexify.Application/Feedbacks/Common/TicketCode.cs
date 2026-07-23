using System.Globalization;

namespace Lexify.Application.Feedbacks.Common;

/// <summary>
/// Renders the DB ticket number as the code users and admins actually quote (<c>LX-1042</c>).
/// One place, so the prefix never drifts between the confirmation screen and the admin list.
/// </summary>
public static class TicketCode
{
    public const string Prefix = "LX-";

    public static string From(int ticketNumber) =>
        Prefix + ticketNumber.ToString(CultureInfo.InvariantCulture);
}
