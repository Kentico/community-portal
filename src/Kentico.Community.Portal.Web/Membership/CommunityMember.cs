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
        //base.MapToMemberInfo(target);

        //Base has been updated in the latest version to set the target.MemberPassword everytime, however we do not want to set it if the 
        //PasswordHash == null
        //Instead we set all the other properties and do not call the base at all
        //target.MemberPassword = PasswordHash;

        if (target == null)
        {
            throw new ArgumentNullException(nameof(target));
        }

        target.MemberName = UserName;
        target.MemberEmail = Email;
        target.MemberEnabled = Enabled;
        target.MemberSecurityStamp = SecurityStamp;
        target.MemberIsExternal = IsExternal;

        _ = target.SetValue("MemberFirstName", FirstName);
        _ = target.SetValue("MemberLastName", LastName);
        _ = target.SetValue("MemberLinkedInIdentifier", LinkedInIdentifier);

        if (PasswordHash != null)
        {
            target.MemberPassword = PasswordHash;
        }
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
