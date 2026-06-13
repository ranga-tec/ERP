namespace ISS.Api.Security;

public sealed record AppPermissionDefinition(string Key, string Module, string Action, string Label, string Description);

public static class AppPermissions
{
    public const string PettyCashIouView = "Finance.PettyCashIou.View";
    public const string PettyCashIouCreate = "Finance.PettyCashIou.Create";
    public const string PettyCashIouSubmit = "Finance.PettyCashIou.Submit";
    public const string PettyCashIouApprove = "Finance.PettyCashIou.Approve";
    public const string PettyCashIouReject = "Finance.PettyCashIou.Reject";
    public const string PettyCashIouRelease = "Finance.PettyCashIou.Release";
    public const string PettyCashIouSettle = "Finance.PettyCashIou.Settle";

    public static readonly AppPermissionDefinition[] All =
    [
        new(PettyCashIouView, "Finance / Petty Cash IOU", "View", "View IOUs", "Open and review petty-cash IOU requests."),
        new(PettyCashIouCreate, "Finance / Petty Cash IOU", "Create", "Create IOUs", "Create petty-cash IOU requests."),
        new(PettyCashIouSubmit, "Finance / Petty Cash IOU", "Submit", "Submit IOUs", "Submit IOUs for approval."),
        new(PettyCashIouApprove, "Finance / Petty Cash IOU", "Approve", "Approve IOUs", "Approve submitted IOU requests."),
        new(PettyCashIouReject, "Finance / Petty Cash IOU", "Reject", "Reject IOUs", "Reject submitted IOU requests."),
        new(PettyCashIouRelease, "Finance / Petty Cash IOU", "Release", "Release cash", "Release approved petty cash to the requester."),
        new(PettyCashIouSettle, "Finance / Petty Cash IOU", "Settle", "Settle IOUs", "Record IOU settlement/accounting.")
    ];

    public static readonly string[] AllKeys = All.Select(x => x.Key).ToArray();
}
