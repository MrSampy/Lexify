namespace Lexify.Infrastructure.Services;

public static class EmailTemplates
{
    public static string ReviewReminder(string username, int count, string appUrl) =>
        $"""
        <!DOCTYPE html>
        <html lang="uk">
        <head><meta charset="UTF-8"><title>Lexify — час повторити слова</title></head>
        <body style="font-family:sans-serif;max-width:560px;margin:0 auto;padding:24px;color:#1a1a1a">
          <h2 style="color:#6d28d9">Lexify</h2>
          <p>Привіт, <strong>{username}</strong>!</p>
          <p>У тебе <strong>{count} {WordLabel(count)}</strong> очікують на повторення сьогодні.</p>
          <p>Регулярне повторення — найкращий спосіб запам'ятати слова назавжди.</p>
          <a href="{appUrl}/review"
             style="display:inline-block;margin-top:16px;padding:12px 24px;background:#6d28d9;color:#fff;border-radius:6px;text-decoration:none;font-weight:bold">
            Почати повторення →
          </a>
          <hr style="margin-top:32px;border:none;border-top:1px solid #e5e7eb">
          <p style="font-size:12px;color:#6b7280">Lexify · Ти отримуєш цей лист, бо у тебе є слова до повторення</p>
        </body>
        </html>
        """;

    public static string Welcome(string username) =>
        $"""
        <!DOCTYPE html>
        <html lang="uk">
        <head><meta charset="UTF-8"><title>Ласкаво просимо до Lexify</title></head>
        <body style="font-family:sans-serif;max-width:560px;margin:0 auto;padding:24px;color:#1a1a1a">
          <h2 style="color:#6d28d9">Ласкаво просимо до Lexify!</h2>
          <p>Привіт, <strong>{username}</strong>!</p>
          <p>Твій акаунт успішно створено. Починай додавати слова та вчись ефективніше з AI та інтервальним повторенням.</p>
          <hr style="margin-top:32px;border:none;border-top:1px solid #e5e7eb">
          <p style="font-size:12px;color:#6b7280">Lexify</p>
        </body>
        </html>
        """;

    public static string PasswordReset(string resetUrl) =>
        $"""
        <!DOCTYPE html>
        <html lang="uk">
        <head><meta charset="UTF-8"><title>Скидання паролю Lexify</title></head>
        <body style="font-family:sans-serif;max-width:560px;margin:0 auto;padding:24px;color:#1a1a1a">
          <h2 style="color:#6d28d9">Lexify</h2>
          <p>Ти отримав цей лист, бо запитав скидання паролю.</p>
          <a href="{resetUrl}"
             style="display:inline-block;margin-top:16px;padding:12px 24px;background:#6d28d9;color:#fff;border-radius:6px;text-decoration:none;font-weight:bold">
            Скинути пароль →
          </a>
          <p style="margin-top:16px;font-size:13px;color:#6b7280">Посилання дійсне 1 годину. Якщо ти не запитував скидання — просто ігноруй цей лист.</p>
          <hr style="margin-top:32px;border:none;border-top:1px solid #e5e7eb">
          <p style="font-size:12px;color:#6b7280">Lexify</p>
        </body>
        </html>
        """;

    private static string WordLabel(int count) => count switch
    {
        1 => "слово",
        2 or 3 or 4 => "слова",
        _ => "слів"
    };
}
