using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Kentico.Community.Portal.Core.Modules.Membership;

namespace Kentico.Community.Portal.Web.Components.PageBuilder.Widgets.CommunityProgramsActivityList;

#if DEBUG
internal static class CommunityProgramsActivityListFakeData
{
    public static CommunityProgramsActivitiesQueryResponse GenerateFakeActivitiesResponse(
        int count,
        int year,
        IReadOnlyDictionary<string, string> activityTypesMap,
        IReadOnlyDictionary<string, string> impactMap,
        IReadOnlyDictionary<string, string> effortMap,
        IReadOnlyDictionary<string, string> satisfactionMap,
        int seed)
    {
        var rng = new Random(seed);

        var memberNames = Enumerable.Range(1, 25)
            .Select(i =>
            {
                var status = i % 2 == 0
                    ? ProgramStatuses.MVP
                    : ProgramStatuses.CommunityLeader;

                return new KeyValuePair<int, CommunityProgramsMemberName>(1000 + i, new CommunityProgramsMemberName($"Fake Member {i}", status));
            })
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        var memberIds = memberNames.Keys.ToArray();
        var activityTypeKeys = activityTypesMap.Keys.DefaultIfEmpty("Social").ToArray();
        var impactKeys = impactMap.Keys.DefaultIfEmpty("3").ToArray();
        var effortKeys = effortMap.Keys.DefaultIfEmpty("2").ToArray();
        var satisfactionKeys = satisfactionMap.Keys.DefaultIfEmpty("3").ToArray();

        static DateTime RandomDateInYear(Random rng, int year)
        {
            var start = new DateTime(year, 1, 1);
            var endExclusive = start.AddYears(1);
            var rangeDays = (endExclusive - start).Days;
            return start.AddDays(rng.Next(rangeDays)).AddMinutes(rng.Next(0, 24 * 60));
        }

        var items = new List<CommunityProgramsActivityListItem>(capacity: count);

        for (int i = 1; i <= count; i++)
        {
            var memberId = memberIds[rng.Next(memberIds.Length)];

            items.Add(new(
                MemberID: memberId,
                ProgramStatus: memberNames[memberId].ProgramStatus,
                ActivityDate: RandomDateInYear(rng, year),
                ActivityType: activityTypeKeys[rng.Next(activityTypeKeys.Length)],
                URL: $"https://example.com/community-programs/activity/{i}",
                ShortDescription: $"Fake activity #{i}",
                Attendees: rng.Next(0, 200).ToString(),
                Impact: impactKeys[rng.Next(impactKeys.Length)],
                Effort: effortKeys[rng.Next(effortKeys.Length)],
                Satisfaction: satisfactionKeys[rng.Next(satisfactionKeys.Length)]));
        }

        var ordered = items.OrderByDescending(i => i.ActivityDate).ToImmutableList();

        return new(
            ordered.GroupBy(i => i.MemberID).ToDictionary(g => g.Key, g => g.ToImmutableList()),
            ordered,
            memberNames);
    }
}
#endif
