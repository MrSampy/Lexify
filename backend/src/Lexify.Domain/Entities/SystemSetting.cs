namespace Lexify.Domain.Entities;

public sealed class SystemSetting
{
    public string Key { get; private set; } = default!;
    public string Value { get; private set; } = default!;
    public string ValueType { get; private set; } = default!;
    public string? Description { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public Guid? UpdatedBy { get; private set; }

    private SystemSetting() { }

    public SystemSetting(string key, string value, string valueType, string? description = null)
    {
        Key = key;
        Value = value;
        ValueType = valueType;
        Description = description;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Update(string value, Guid updatedBy)
    {
        Value = value;
        UpdatedBy = updatedBy;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Keys of the settings the application actually reads at runtime. Kept here (rather than as
    /// literals scattered across the seeder and the handlers) so a rename can't silently turn a
    /// setting into a decorative row that the admin UI edits but nothing honours.
    /// </summary>
    public static class Keys
    {
        /// <summary>When "false", public sign-up is closed and only <see cref="InviteCode"/> gets a user in.</summary>
        public const string RegistrationEnabled = "features.registration_enabled";

        /// <summary>Shared invite code required to register while registration is closed. Empty = nobody can register.</summary>
        public const string InviteCode = "features.invite_code";

        /// <summary>When "true", a new account cannot sign in until its email address is confirmed.</summary>
        public const string EmailVerificationRequired = "features.email_verification_required";

        /// <summary>
        /// Master switch for two-factor (email code) at sign-in: mandatory for admins, opt-in for other
        /// users. When "false" the feature is dormant for everyone — also the operator kill switch if the
        /// mail path breaks and admins would otherwise be locked out.
        /// </summary>
        public const string TwoFactorEnabled = "features.two_factor_enabled";

        /// <summary>Per-user cap on AI calls per UTC day. Zero or negative disables the cap.</summary>
        public const string MaxAiCallsPerUserPerDay = "ai.max_calls_per_user_per_day";

        /// <summary>When "false", server-side neural TTS (Piper) is off and clients use browser speech.</summary>
        public const string TtsEnabled = "features.tts_enabled";

        /// <summary>Max words a single block may hold. Zero or negative disables the cap.</summary>
        public const string MaxWordsPerBlock = "features.max_words_per_block";

        /// <summary>Max blocks a user may own. Zero or negative disables the cap.</summary>
        public const string MaxBlocksPerUser = "features.max_blocks_per_user";

        /// <summary>Max questions a generated test may have (upper bound for the request's QuestionCount).</summary>
        public const string TestMaxQuestions = "test.max_questions";

        /// <summary>When "true", non-admin API requests get 503 (auth and health stay open).</summary>
        public const string MaintenanceEnabled = "maintenance.enabled";
    }
}
