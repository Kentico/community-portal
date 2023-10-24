using CMS.Membership;
using Kentico.Membership;

namespace Kentico.Community.Portal.Web.Membership;

public class CommunityMember : ApplicationUser
{
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string LinkedInIdentifier { get; set; } = "";
    public DateTime Created { get; set; } = DateTime.MinValue;

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
}
