namespace Zebrahoof_EMR.Services;

public static class SignOutChannel
{
    public static string Group(string name) => "signout:" + name.Trim().ToLowerInvariant();
}
