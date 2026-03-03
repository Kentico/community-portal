using CMS.Membership;
using CMS.OnlineForms;
using Kentico.Community.Portal.Core.Forms;
using Kentico.Membership;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Kentico.Community.Portal.Web.Features.Api;

/// <summary>
/// API endpoints for Community Leader Activity submissions.
/// Allows authenticated members to submit their community activities (blogs, social posts, etc.)
/// without using the web form.
/// </summary>
[ApiController]
[Route("api/community-leader-activity")]
[Authorize] // Requires authenticated member session
public class CommunityLeaderActivityController(
    ILogger<CommunityLeaderActivityController> logger,
    IBizFormInfoProvider bizFormInfoProvider,
    UserManager<ApplicationUser> userManager,
    IMemberInfoProvider memberInfoProvider)
    : ControllerBase
{
    private readonly ILogger<CommunityLeaderActivityController> logger = logger;
    private readonly IBizFormInfoProvider bizFormInfoProvider = bizFormInfoProvider;
    private readonly UserManager<ApplicationUser> userManager = userManager;
    private readonly IMemberInfoProvider memberInfoProvider = memberInfoProvider;

    /// <summary>
    /// Creates a new Community Leader Activity form submission for the authenticated member
    /// </summary>
    [HttpPost]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(CreateActivityResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateActivity([FromBody] CreateActivityRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Get authenticated member
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return Unauthorized();
        }

        var member = await memberInfoProvider.GetAsync(user.Id);
        if (member is null)
        {
            logger.LogWarning("User {UserId} has no associated member", user.Id);
            return Problem("Member profile not found", statusCode: 400);
        }

        try
        {
            var formInfo = await bizFormInfoProvider.GetAsync("CommunityLeaderActivity");
            if (formInfo is null)
            {
                logger.LogError("CommunityLeaderActivity form not found");
                return Problem("Form configuration not found", statusCode: 500);
            }

            var item = new CommunityLeaderActivityItem
            {
                MemberID = member.MemberID,
                MemberEmail = member.MemberEmail,
                ActivityDate = request.ActivityDate ?? DateTime.UtcNow,
                ActivityType = request.ActivityType ?? "Social",
                URL = request.Url,
                ShortDescription = request.ShortDescription,
                Impact = request.Impact ?? "3",
                Effort = request.Effort ?? "2",
                Satisfaction = request.Satisfaction ?? "3"
            };

            item.Insert();

            logger.LogInformation(
                "Created CommunityLeaderActivity {ItemId} for member {MemberId} ({MemberEmail})",
                item.ItemID,
                member.MemberID,
                member.MemberEmail);

            var response = new CreateActivityResponse
            {
                Id = item.ItemID,
                Message = "Activity created successfully"
            };

            return CreatedAtAction(
                nameof(GetActivity),
                new { id = item.ItemID },
                response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create CommunityLeaderActivity for member {MemberId}", member.MemberID);
            return Problem("Failed to create activity", statusCode: 500);
        }
    }

    /// <summary>
    /// Gets a Community Leader Activity by ID (only your own activities)
    /// </summary>
    [HttpGet("{id:int}")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ActivityResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetActivity(int id)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return Unauthorized();
        }

        var member = await memberInfoProvider.GetAsync(user.Id);
        if (member is null)
        {
            return Unauthorized();
        }

        var item = await BizFormItemProvider
            .GetItems<CommunityLeaderActivityItem>()
            .WhereEquals(nameof(CommunityLeaderActivityItem.ItemID), id)
            .WhereEquals(nameof(CommunityLeaderActivityItem.MemberID), member.MemberID)
            .FirstOrDefaultAsync();

        if (item is null)
        {
            return NotFound();
        }

        return Ok(new ActivityResponse
        {
            Id = item.ItemID,
            ActivityDate = item.ActivityDate,
            ActivityType = item.ActivityType,
            Url = item.URL,
            ShortDescription = item.ShortDescription,
            Impact = item.Impact,
            Effort = item.Effort,
            Satisfaction = item.Satisfaction
        });
    }

    /// <summary>
    /// Lists your Community Leader Activities
    /// </summary>
    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType(typeof(List<ActivityResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ListActivities([FromQuery] int limit = 50, [FromQuery] int offset = 0)
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return Unauthorized();
        }

        var member = await memberInfoProvider.GetAsync(user.Id);
        if (member is null)
        {
            return Unauthorized();
        }

        var items = await BizFormItemProvider
            .GetItems<CommunityLeaderActivityItem>()
            .WhereEquals(nameof(CommunityLeaderActivityItem.MemberID), member.MemberID)
            .OrderByDescending(nameof(CommunityLeaderActivityItem.ActivityDate))
            .Skip(offset)
            .Take(Math.Min(limit, 100))
            .ToListAsync();

        var response = items.Select(item => new ActivityResponse
        {
            Id = item.ItemID,
            ActivityDate = item.ActivityDate,
            ActivityType = item.ActivityType,
            Url = item.URL,
            ShortDescription = item.ShortDescription,
            Impact = item.Impact,
            Effort = item.Effort,
            Satisfaction = item.Satisfaction
        }).ToList();

        return Ok(response);
    }

    /// <summary>
    /// Creates multiple Community Leader Activities in batch for the authenticated member
    /// </summary>
    [HttpPost("batch")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(BatchCreateResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateActivitiesBatch([FromBody] List<CreateActivityRequest> requests)
    {
        if (requests is null || requests.Count == 0)
        {
            return BadRequest("No activities provided");
        }

        if (requests.Count > 100)
        {
            return BadRequest("Maximum 100 activities per batch");
        }

        // Get authenticated member
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return Unauthorized();
        }

        var member = await memberInfoProvider.GetAsync(user.Id);
        if (member is null)
        {
            return Problem("Member profile not found", statusCode: 400);
        }

        var formInfo = await bizFormInfoProvider.GetAsync("CommunityLeaderActivity");
        if (formInfo is null)
        {
            return Problem("Form configuration not found", statusCode: 500);
        }

        var createdIds = new List<int>();
        var errors = new List<string>();

        foreach (var (request, index) in requests.Select((r, i) => (r, i)))
        {
            try
            {
                var item = new CommunityLeaderActivityItem
                {
                    MemberID = member.MemberID,
                    MemberEmail = member.MemberEmail,
                    ActivityDate = request.ActivityDate ?? DateTime.UtcNow,
                    ActivityType = request.ActivityType ?? "Social",
                    URL = request.Url,
                    ShortDescription = request.ShortDescription,
                    Impact = request.Impact ?? "3",
                    Effort = request.Effort ?? "2",
                    Satisfaction = request.Satisfaction ?? "3"
                };

                item.Insert();
                createdIds.Add(item.ItemID);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to create activity at index {Index} for member {MemberId}", index, member.MemberID);
                errors.Add($"Item {index}: {ex.Message}");
            }
        }

        logger.LogInformation(
            "Batch created {Count} activities for member {MemberId}",
            createdIds.Count,
            member.MemberID);

        return Created("", new BatchCreateResponse
        {
            CreatedIds = createdIds,
            CreatedCount = createdIds.Count,
            ErrorCount = errors.Count,
            Errors = errors
        });
    }
}

/// <summary>
/// Request model for creating a community leader activity
/// </summary>
public class CreateActivityRequest
{
    /// <summary>
    /// Date of the activity (defaults to now if not provided)
    /// </summary>
    public DateTime? ActivityDate { get; set; }

    /// <summary>
    /// Type of activity: Social, Blog, Video, Presentation, Podcast, Other
    /// </summary>
    public string? ActivityType { get; set; } = "Social";

    /// <summary>
    /// URL to the content (required)
    /// </summary>
    public required string Url { get; set; }

    /// <summary>
    /// Brief description of the activity (required)
    /// </summary>
    public required string ShortDescription { get; set; }

    /// <summary>
    /// Impact rating: 1-5 (default: 3)
    /// </summary>
    public string? Impact { get; set; } = "3";

    /// <summary>
    /// Effort rating: 1-5 (default: 2)
    /// </summary>
    public string? Effort { get; set; } = "2";

    /// <summary>
    /// Satisfaction rating: 1-5 (default: 3)
    /// </summary>
    public string? Satisfaction { get; set; } = "3";
}

public class CreateActivityResponse
{
    public int Id { get; set; }
    public string Message { get; set; } = "";
}

public class ActivityResponse
{
    public int Id { get; set; }
    public DateTime ActivityDate { get; set; }
    public string ActivityType { get; set; } = "";
    public string Url { get; set; } = "";
    public string ShortDescription { get; set; } = "";
    public string Impact { get; set; } = "";
    public string Effort { get; set; } = "";
    public string Satisfaction { get; set; } = "";
}

public class BatchCreateResponse
{
    public List<int> CreatedIds { get; set; } = [];
    public int CreatedCount { get; set; }
    public int ErrorCount { get; set; }
    public List<string> Errors { get; set; } = [];
}
