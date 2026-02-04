using CMS.Membership;
using CMS.Websites.Internal;
using Kentico.Community.Portal.Admin.Features.QAndA;
using Kentico.Community.Portal.Admin.Features.QAndA.QuestionReactions;
using Kentico.Community.Portal.Core.Modules;
using Kentico.Xperience.Admin.Base;

[assembly: UIPage(
    parentType: typeof(QAndAApplicationPage),
    slug: "question-reactions",
    uiPageType: typeof(QuestionReactionListingPage),
    name: "Question Reactions",
    templateName: TemplateNames.LISTING,
    order: 2)]

namespace Kentico.Community.Portal.Admin.Features.QAndA.QuestionReactions;

public class QuestionReactionListingPage : ListingPage
{
    protected override string ObjectType => QAndAQuestionReactionInfo.OBJECT_TYPE;

    [PageCommand(Permission = SystemPermissions.DELETE)]
    public override Task<ICommandResponse<RowActionResult>> Delete(int id) => base.Delete(id);

    public override async Task ConfigurePage()
    {
        await base.ConfigurePage();

        PageConfiguration.AddEditRowAction<QuestionReactionEditPage>();

        PageConfiguration.QueryModifiers
            .AddModifier((query, settings) =>
            {
                return query
                    .Source(source => source
                        .Join<MemberInfo>(nameof(QAndAQuestionReactionInfo.QAndAQuestionReactionMemberID), nameof(MemberInfo.MemberID))
                        .Join<WebPageItemInfo>(
                            $"KenticoCommunity_QAndAQuestionReaction.{nameof(QAndAQuestionReactionInfo.QAndAQuestionReactionWebPageItemID)}",
                            nameof(WebPageItemInfo.WebPageItemID)))
                    .OrderByDescending(nameof(QAndAQuestionReactionInfo.QAndAQuestionReactionDateModified));
            });

        PageConfiguration.ColumnConfigurations
            .AddColumn(
                nameof(QAndAQuestionReactionInfo.QAndAQuestionReactionID),
                "Reaction",
                searchable: true,
                minWidth: 1)
            .AddColumn(
                nameof(WebPageItemInfo.WebPageItemTreePath),
                "Question",
                searchable: true,
                minWidth: 7)
            .AddColumn(
                nameof(MemberInfo.MemberEmail),
                "Member",
                searchable: true)
            .AddColumn(
                nameof(QAndAQuestionReactionInfo.QAndAQuestionReactionType),
                "Type",
                searchable: true,
                sortable: true)
            .AddColumn(
                nameof(QAndAQuestionReactionInfo.QAndAQuestionReactionDateModified),
                "Modified",
                searchable: true,
                sortable: true,
                defaultSortDirection: SortTypeEnum.Desc);

        _ = PageConfiguration.TableActions.AddDeleteAction(nameof(Delete));
    }
}
