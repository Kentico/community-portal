using CMS.Membership;
using Kentico.Membership;

namespace Kentico.Community.Portal.Web.Membership;

public class CommunityMember : ApplicationUser
{
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string FullName =>
        (FirstName, LastName) switch
        {
            ("", "") => "",
            (string first, "") => first,
            ("", string last) => last,
            (string first, string last) => $"{first} {last}",
            (null, null) or _ => "",
        };
    public string LinkedInIdentifier { get; set; } = "";
    public DateTime Created { get; set; }

    public override void MapToMemberInfo(MemberInfo target)
    {
        if (target is null)
        {
            throw new ArgumentNullException(nameof(target));
        }

        /*
         * base.MapToMemberInfo will set target.MemberPassword everytime
         * however we do not want to set it if PasswordHash is null,
         * and this stores the original so we can revert it
         */
        string originalPasswordHash = target.MemberPassword;

        base.MapToMemberInfo(target);

        if (PasswordHash is null)
        {
            target.MemberPassword = originalPasswordHash;
        }

        _ = target.SetValue("MemberFirstName", FirstName);
        _ = target.SetValue("MemberLastName", LastName);
        _ = target.SetValue("MemberLinkedInIdentifier", LinkedInIdentifier);
    }

    public override void MapFromMemberInfo(MemberInfo source)
    {
        base.MapFromMemberInfo(source);

        FirstName = source.GetValue("MemberFirstName", "");
        LastName = source.GetValue("MemberLastName", "");
        LinkedInIdentifier = source.GetValue("MemberLinkedInIdentifier", "");
        Created = source.MemberCreated;
    }

    public static CommunityMember FromMemberInfo(MemberInfo memberInfo)
    {
        var communityMember = new CommunityMember();
        communityMember.MapFromMemberInfo(memberInfo);

        return communityMember;
    }
}

public static class MemberInfoExtensions
{
    public static CommunityMember AsCommunityMember(this MemberInfo member) =>
        CommunityMember.FromMemberInfo(member);
}
