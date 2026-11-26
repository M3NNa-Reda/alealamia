namespace alealamia.Domain.Constants;

public static class TaskRules
{
    public const int MonthlyAcceptedLimit = 10;
    public const int TitleMaxLength = 200;
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;
    public const int MaxDueSoonHours = 168;
    public const int LateGraceDays = 1;          // BR-07: one extra day after the published deadline
    public const int WithdrawalBlockDays = 7;    // BR-11
    public const int NoShowBlockDays = 30;       // BR-09
    public const decimal MaxRewardAmount = 9_999_999_999_999_999.99m;
}
