namespace Lexify.Infrastructure.Services;

public static class EmailTemplates
{
    public static string ReviewReminder(string username, int count, string appUrl, string unsubscribeUrl) =>
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
          <p style="font-size:12px;color:#6b7280">
            Lexify · Ти отримуєш цей лист, бо у тебе є слова до повторення.<br>
            <a href="{unsubscribeUrl}" style="color:#6b7280">Відписатися від щоденних нагадувань</a>
          </p>
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

    public static string EmailVerification(string verifyUrl) =>
        $"""
        <!DOCTYPE html>
        <html lang="uk">
        <head><meta charset="UTF-8"><title>Підтвердження пошти Lexify</title></head>
        <body style="font-family:sans-serif;max-width:560px;margin:0 auto;padding:24px;color:#1a1a1a">
          <h2 style="color:#6d28d9">Lexify</h2>
          <p>Залишився один крок — підтверди свою електронну пошту, щоб увійти в акаунт.</p>
          <a href="{verifyUrl}"
             style="display:inline-block;margin-top:16px;padding:12px 24px;background:#6d28d9;color:#fff;border-radius:6px;text-decoration:none;font-weight:bold">
            Підтвердити пошту →
          </a>
          <p style="margin-top:16px;font-size:13px;color:#6b7280">Посилання дійсне 24 години. Якщо ти не реєструвався в Lexify — просто ігноруй цей лист.</p>
          <hr style="margin-top:32px;border:none;border-top:1px solid #e5e7eb">
          <p style="font-size:12px;color:#6b7280">Lexify</p>
        </body>
        </html>
        """;

    public static string EmailChangeVerification(string verifyUrl) =>
        $"""
        <!DOCTYPE html>
        <html lang="uk">
        <head><meta charset="UTF-8"><title>Зміна пошти Lexify</title></head>
        <body style="font-family:sans-serif;max-width:560px;margin:0 auto;padding:24px;color:#1a1a1a">
          <h2 style="color:#6d28d9">Lexify</h2>
          <p>Ти отримав цей лист, бо цю адресу вказали як нову пошту для акаунта Lexify.</p>
          <p>Щоб завершити зміну, підтверди адресу — після цього вхід буде саме за нею.</p>
          <a href="{verifyUrl}"
             style="display:inline-block;margin-top:16px;padding:12px 24px;background:#6d28d9;color:#fff;border-radius:6px;text-decoration:none;font-weight:bold">
            Підтвердити нову пошту →
          </a>
          <p style="margin-top:16px;font-size:13px;color:#6b7280">Посилання дійсне 24 години. Якщо це був не ти — просто ігноруй цей лист, пошта акаунта не зміниться.</p>
          <hr style="margin-top:32px;border:none;border-top:1px solid #e5e7eb">
          <p style="font-size:12px;color:#6b7280">Lexify</p>
        </body>
        </html>
        """;

    /// <summary>
    /// Sent to the *previous* address after an email change completes, so the original owner learns of
    /// it even if the account was moved out from under them — the last chance to react before the new
    /// address controls password recovery.
    /// </summary>
    public static string EmailChangedNotice(string newEmail) =>
        $"""
        <!DOCTYPE html>
        <html lang="uk">
        <head><meta charset="UTF-8"><title>Пошту акаунта Lexify змінено</title></head>
        <body style="font-family:sans-serif;max-width:560px;margin:0 auto;padding:24px;color:#1a1a1a">
          <h2 style="color:#6d28d9">Lexify</h2>
          <p>Пошту твого акаунта Lexify було змінено на <strong>{newEmail}</strong>. Відтепер вхід — саме за цією адресою.</p>
          <p style="margin-top:16px;font-size:13px;color:#6b7280">Якщо це зробив не ти — хтось міг отримати доступ до акаунта. Негайно скинь пароль і звернися до підтримки: адреса, яка бачить цей лист, більше не може входити в акаунт.</p>
          <hr style="margin-top:32px;border:none;border-top:1px solid #e5e7eb">
          <p style="font-size:12px;color:#6b7280">Lexify</p>
        </body>
        </html>
        """;

    public static string TwoFactorCode(string code) =>
        $"""
        <!DOCTYPE html>
        <html lang="uk">
        <head><meta charset="UTF-8"><title>Код підтвердження входу Lexify</title></head>
        <body style="font-family:sans-serif;max-width:560px;margin:0 auto;padding:24px;color:#1a1a1a">
          <h2 style="color:#6d28d9">Lexify</h2>
          <p>Твій код для входу в акаунт:</p>
          <p style="margin:20px 0;font-family:monospace;font-size:32px;font-weight:bold;letter-spacing:8px;color:#1a1a1a">{code}</p>
          <p style="font-size:13px;color:#6b7280">Код дійсний 10 хвилин. Якщо це були не ти — хтось знає твій пароль, тож зміни його якнайшвидше.</p>
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
