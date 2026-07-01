using Microsoft.AspNetCore.Identity;

namespace Micon.LotterySystem
{
    public class JapaneseIdentityErrorDescriber : IdentityErrorDescriber
    {
        public override IdentityError DefaultError() =>
            new IdentityError { Code = nameof(DefaultError), Description = "不明なエラーが発生しました。" };

        public override IdentityError ConcurrencyFailure() =>
            new IdentityError { Code = nameof(ConcurrencyFailure), Description = "同時に変更が行われたため、保存できませんでした。" };

        public override IdentityError PasswordMismatch() =>
            new IdentityError { Code = nameof(PasswordMismatch), Description = "パスワードが一致しません。" };

        public override IdentityError InvalidToken() =>
            new IdentityError { Code = nameof(InvalidToken), Description = "無効なトークンです。" };

        public override IdentityError LoginAlreadyAssociated() =>
            new IdentityError { Code = nameof(LoginAlreadyAssociated), Description = "このログインは既に他のアカウントに関連付けられています。" };

        public override IdentityError InvalidUserName(string userName) =>
            new IdentityError { Code = nameof(InvalidUserName), Description = $"ユーザー名 '{userName}' は無効です。" };

        public override IdentityError InvalidEmail(string email) =>
            new IdentityError { Code = nameof(InvalidEmail), Description = $"メールアドレス '{email}' は無効です。" };

        public override IdentityError DuplicateUserName(string userName) =>
            new IdentityError { Code = nameof(DuplicateUserName), Description = $"ユーザー名 '{userName}' は既に使用されています。" };

        public override IdentityError DuplicateEmail(string email) =>
            new IdentityError { Code = nameof(DuplicateEmail), Description = $"メールアドレス '{email}' は既に使用されています。" };

        public override IdentityError InvalidRoleName(string role) =>
            new IdentityError { Code = nameof(InvalidRoleName), Description = $"ロール名 '{role}' は無効です。" };

        public override IdentityError DuplicateRoleName(string role) =>
            new IdentityError { Code = nameof(DuplicateRoleName), Description = $"ロール名 '{role}' は既に存在します。" };

        public override IdentityError UserAlreadyHasPassword() =>
            new IdentityError { Code = nameof(UserAlreadyHasPassword), Description = "ユーザーには既にパスワードが設定されています。" };

        public override IdentityError UserLockoutNotEnabled() =>
            new IdentityError { Code = nameof(UserLockoutNotEnabled), Description = "このユーザーにはロックアウトが有効になっていません。" };

        public override IdentityError UserAlreadyInRole(string role) =>
            new IdentityError { Code = nameof(UserAlreadyInRole), Description = $"ユーザーは既にロール '{role}' に属しています。" };

        public override IdentityError UserNotInRole(string role) =>
            new IdentityError { Code = nameof(UserNotInRole), Description = $"ユーザーはロール '{role}' に属していません。" };

        public override IdentityError PasswordTooShort(int length) =>
            new IdentityError { Code = nameof(PasswordTooShort), Description = $"パスワードは少なくとも {length} 文字以上である必要があります。" };

        public override IdentityError PasswordRequiresNonAlphanumeric() =>
            new IdentityError { Code = nameof(PasswordRequiresNonAlphanumeric), Description = "パスワードには記号（例: !, @, #）を含める必要があります。" };

        public override IdentityError PasswordRequiresDigit() =>
            new IdentityError { Code = nameof(PasswordRequiresDigit), Description = "パスワードには少なくとも1つの数字（0～9）を含める必要があります。" };

        public override IdentityError PasswordRequiresLower() =>
            new IdentityError { Code = nameof(PasswordRequiresLower), Description = "パスワードには少なくとも1つの小文字（a～z）を含める必要があります。" };

        public override IdentityError PasswordRequiresUpper() =>
            new IdentityError { Code = nameof(PasswordRequiresUpper), Description = "パスワードには少なくとも1つの大文字（A～Z）を含める必要があります。" };

        public override IdentityError RecoveryCodeRedemptionFailed() =>
            new IdentityError { Code = nameof(RecoveryCodeRedemptionFailed), Description = "リカバリーコードの使用に失敗しました。" };

        public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) =>
            new IdentityError { Code = nameof(PasswordRequiresUniqueChars), Description = $"パスワードには少なくとも {uniqueChars} 種類の異なる文字を含める必要があります。" };

    }
}
