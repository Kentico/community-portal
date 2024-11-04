using CMS.ContactManagement;
using CMS.Membership;
using Kentico.OnlineMarketing.Web.Mvc;

namespace Kentico.Community.Portal.Web.Membership;

public class CommunityMemberToContactMapper(IMemberToContactMapper memberToContactMapper) : IMemberToContactMapper
{
    private readonly IMemberToContactMapper memberToContactMapper = memberToContactMapper;

    public void Map(MemberInfo member, ContactInfo contact)
    {
        var communityMember = new CommunityMember();
        communityMember.MapFromMemberInfo(member);

        contact.ContactFirstName = communityMember.FirstName;
        contact.ContactLastName = communityMember.LastName;

        memberToContactMapper.Map(member, contact);
    }
}
