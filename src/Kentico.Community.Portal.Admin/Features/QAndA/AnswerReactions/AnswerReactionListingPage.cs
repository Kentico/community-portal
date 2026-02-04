using CMS.Membership;
using Kentico.Community.Portal.Admin.Features.QAndA;
using Kentico.Community.Portal.Admin.Features.QAndA.AnswerReactions;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Xperience.Admin.Base;

[assembly: UIPage(
    parentType: typeof(QAndAApplicationPage),
    slug: "answer-reactions",
    uiPageType: typeof(AnswerReactionListingPage),
    name: "Answer Reactions",
    templateName: TemplateNames.LISTING,
    order: 1)]

namespace Kentico.Community.Portal.Admin.Features.QAndA.AnswerReactions;

public class AnswerReactionListingPage : ListingPage
{
    protected override string ObjectType => QAndAAnswerReactionInfo.OBJECT_TYPE;

    [PageCommand(Permission = SystemPermissions.DELETE)]
    public override Task<ICommandResponse<RowActionResult>> Delete(int id) => base.Delete(id);

    public override async Task ConfigurePage()
    {
        await base.ConfigurePage();

        PageConfiguration.AddEditRowAction<AnswerReactionEditPage>();

        PageConfiguration.QueryModifiers
            .AddModifier((query, settings) =>
            {
                return query
                    .Source(source => source
                        .Join<MemberInfo>(nameof(QAndAAnswerReactionInfo.QAndAAnswerReactionMemberID), nameof(MemberInfo.MemberID))
                        .Join<QAndAAnswerDataInfo>(
                            $"KenticoCommunity_QAndAAnswerReaction.{nameof(QAndAAnswerReactionInfo.QAndAAnswerReactionAnswerID)}",
                            nameof(QAndAAnswerDataInfo.QAndAAnswerDataID)))
                    .OrderByDescending(nameof(QAndAAnswerReactionInfo.QAndAAnswerReactionDateModified));
            });

        PageConfiguration.ColumnConfigurations
            .AddColumn(
                nameof(QAndAAnswerReactionInfo.QAndAAnswerReactionID),
                "Reaction",
                searchable: true,
                minWidth: 1)
            .AddColumn(
                nameof(QAndAAnswerDataInfo.QAndAAnswerDataCodeName),
                "Answer",
                searchable: true,
                minWidth: 7)
            .AddColumn(
                nameof(MemberInfo.MemberEmail),
                "Member",
                searchable: true)
            .AddColumn(
                nameof(QAndAAnswerReactionInfo.QAndAAnswerReactionType),
                "Type",
                searchable: true,
                sortable: true)
            .AddColumn(
                nameof(QAndAAnswerReactionInfo.QAndAAnswerReactionDateModified),
                "Modified",
                searchable: true,
                sortable: true,
                defaultSortDirection: SortTypeEnum.Desc);

        _ = PageConfiguration.TableActions.AddDeleteAction(nameof(Delete));
    }
}
