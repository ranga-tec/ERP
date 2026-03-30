using ISS.Domain.Common;

namespace ISS.Domain.Service;

public enum WorkOrderStatus
{
    Open = 0,
    InProgress = 1,
    Done = 2,
    Cancelled = 3
}

public enum WorkOrderTimeEntryStatus
{
    Draft = 0,
    Submitted = 1,
    Approved = 2,
    Rejected = 3,
    Invoiced = 4
}

public sealed class WorkOrder : AuditableEntity
{
    private WorkOrder() { }

    public WorkOrder(Guid serviceJobId, string description, Guid? assignedToUserId)
    {
        ServiceJobId = serviceJobId;
        Description = Guard.NotNullOrWhiteSpace(description, nameof(Description), maxLength: 2000);
        AssignedToUserId = assignedToUserId;
        Status = WorkOrderStatus.Open;
    }

    public Guid ServiceJobId { get; private set; }
    public string Description { get; private set; } = null!;
    public Guid? AssignedToUserId { get; private set; }
    public WorkOrderStatus Status { get; private set; }
    public List<WorkOrderTimeEntry> TimeEntries { get; private set; } = new();

    public void Assign(Guid? userId) => AssignedToUserId = userId;

    public WorkOrderTimeEntry AddTimeEntry(
        Guid? technicianUserId,
        string technicianName,
        DateTimeOffset workDate,
        string workDescription,
        decimal hoursWorked,
        decimal costRate,
        bool billableToCustomer,
        decimal billableHours,
        decimal billingRate,
        decimal taxPercent,
        string? notes)
    {
        EnsureTimeEntryChangesAllowed();

        var entry = new WorkOrderTimeEntry(
            Id,
            ServiceJobId,
            technicianUserId,
            technicianName,
            workDate,
            workDescription,
            hoursWorked,
            costRate,
            billableToCustomer,
            billableHours,
            billingRate,
            taxPercent,
            notes);

        TimeEntries.Add(entry);
        return entry;
    }

    public void UpdateTimeEntry(
        Guid timeEntryId,
        Guid? technicianUserId,
        string technicianName,
        DateTimeOffset workDate,
        string workDescription,
        decimal hoursWorked,
        decimal costRate,
        bool billableToCustomer,
        decimal billableHours,
        decimal billingRate,
        decimal taxPercent,
        string? notes)
    {
        EnsureTimeEntryChangesAllowed();

        var entry = TimeEntries.FirstOrDefault(x => x.Id == timeEntryId)
            ?? throw new DomainValidationException("Work-order labor entry not found.");

        entry.Update(
            technicianUserId,
            technicianName,
            workDate,
            workDescription,
            hoursWorked,
            costRate,
            billableToCustomer,
            billableHours,
            billingRate,
            taxPercent,
            notes);
    }

    public void RemoveTimeEntry(Guid timeEntryId)
    {
        EnsureTimeEntryChangesAllowed();

        var entry = TimeEntries.FirstOrDefault(x => x.Id == timeEntryId)
            ?? throw new DomainValidationException("Work-order labor entry not found.");

        if (entry.Status != WorkOrderTimeEntryStatus.Draft)
        {
            throw new DomainValidationException("Only draft labor entries can be deleted.");
        }

        TimeEntries.Remove(entry);
    }

    public void Start()
    {
        if (Status != WorkOrderStatus.Open)
        {
            throw new DomainValidationException("Only open work orders can be started.");
        }

        Status = WorkOrderStatus.InProgress;
    }

    public void MarkDone()
    {
        if (Status != WorkOrderStatus.InProgress)
        {
            throw new DomainValidationException("Work order must be in progress to mark done.");
        }

        Status = WorkOrderStatus.Done;
    }

    public void Cancel()
    {
        if (Status == WorkOrderStatus.Done)
        {
            throw new DomainValidationException("Done work orders cannot be cancelled.");
        }

        Status = WorkOrderStatus.Cancelled;
    }

    private void EnsureTimeEntryChangesAllowed()
    {
        if (Status == WorkOrderStatus.Cancelled)
        {
            throw new DomainValidationException("Cancelled work orders cannot be changed.");
        }
    }
}

public sealed class WorkOrderTimeEntry : Entity
{
    private WorkOrderTimeEntry() { }

    public WorkOrderTimeEntry(
        Guid workOrderId,
        Guid serviceJobId,
        Guid? technicianUserId,
        string technicianName,
        DateTimeOffset workDate,
        string workDescription,
        decimal hoursWorked,
        decimal costRate,
        bool billableToCustomer,
        decimal billableHours,
        decimal billingRate,
        decimal taxPercent,
        string? notes)
    {
        WorkOrderId = workOrderId;
        ServiceJobId = serviceJobId;
        Update(
            technicianUserId,
            technicianName,
            workDate,
            workDescription,
            hoursWorked,
            costRate,
            billableToCustomer,
            billableHours,
            billingRate,
            taxPercent,
            notes);
        Status = WorkOrderTimeEntryStatus.Draft;
    }

    public Guid WorkOrderId { get; private set; }
    public Guid ServiceJobId { get; private set; }
    public Guid? TechnicianUserId { get; private set; }
    public string TechnicianName { get; private set; } = null!;
    public DateTimeOffset WorkDate { get; private set; }
    public string WorkDescription { get; private set; } = null!;
    public decimal HoursWorked { get; private set; }
    public decimal CostRate { get; private set; }
    public bool BillableToCustomer { get; private set; }
    public decimal BillableHours { get; private set; }
    public decimal BillingRate { get; private set; }
    public decimal TaxPercent { get; private set; }
    public string? Notes { get; private set; }
    public WorkOrderTimeEntryStatus Status { get; private set; }
    public DateTimeOffset? SubmittedAt { get; private set; }
    public DateTimeOffset? ApprovedAt { get; private set; }
    public DateTimeOffset? RejectedAt { get; private set; }
    public string? RejectionReason { get; private set; }
    public Guid? SalesInvoiceId { get; private set; }
    public Guid? SalesInvoiceLineId { get; private set; }
    public DateTimeOffset? InvoicedAt { get; private set; }

    public decimal LaborCost => HoursWorked * CostRate;
    public decimal BillableSubtotal => BillableToCustomer ? BillableHours * BillingRate : 0m;
    public decimal BillableTax => BillableSubtotal * (TaxPercent / 100m);
    public decimal BillableTotal => BillableSubtotal + BillableTax;

    public void Update(
        Guid? technicianUserId,
        string technicianName,
        DateTimeOffset workDate,
        string workDescription,
        decimal hoursWorked,
        decimal costRate,
        bool billableToCustomer,
        decimal billableHours,
        decimal billingRate,
        decimal taxPercent,
        string? notes)
    {
        if (Status != WorkOrderTimeEntryStatus.Draft)
        {
            throw new DomainValidationException("Only draft labor entries can be edited.");
        }

        TechnicianUserId = technicianUserId;
        TechnicianName = Guard.NotNullOrWhiteSpace(technicianName, nameof(technicianName), maxLength: 256);
        WorkDate = workDate;
        WorkDescription = Guard.NotNullOrWhiteSpace(workDescription, nameof(workDescription), maxLength: 1000);
        HoursWorked = Guard.Positive(hoursWorked, nameof(hoursWorked));
        CostRate = Guard.NotNegative(costRate, nameof(costRate));
        BillableToCustomer = billableToCustomer;

        if (billableToCustomer)
        {
            BillableHours = Guard.Positive(billableHours, nameof(billableHours));
            if (BillableHours > HoursWorked)
            {
                throw new DomainValidationException("Billable hours cannot exceed worked hours.");
            }

            BillingRate = Guard.NotNegative(billingRate, nameof(billingRate));
            TaxPercent = Guard.NotNegative(taxPercent, nameof(taxPercent));
        }
        else
        {
            if (billableHours != 0m || billingRate != 0m || taxPercent != 0m)
            {
                throw new DomainValidationException("Non-billable labor entries must keep billable hours, billing rate, and tax at 0.");
            }

            BillableHours = 0m;
            BillingRate = 0m;
            TaxPercent = 0m;
        }

        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
    }

    public void Submit(DateTimeOffset submittedAt)
    {
        if (Status != WorkOrderTimeEntryStatus.Draft)
        {
            throw new DomainValidationException("Only draft labor entries can be submitted.");
        }

        Status = WorkOrderTimeEntryStatus.Submitted;
        SubmittedAt = submittedAt;
        ApprovedAt = null;
        RejectedAt = null;
        RejectionReason = null;
    }

    public void Approve(DateTimeOffset approvedAt)
    {
        if (Status != WorkOrderTimeEntryStatus.Submitted)
        {
            throw new DomainValidationException("Only submitted labor entries can be approved.");
        }

        Status = WorkOrderTimeEntryStatus.Approved;
        ApprovedAt = approvedAt;
        RejectedAt = null;
        RejectionReason = null;
    }

    public void Reject(DateTimeOffset rejectedAt, string? rejectionReason)
    {
        if (Status != WorkOrderTimeEntryStatus.Submitted)
        {
            throw new DomainValidationException("Only submitted labor entries can be rejected.");
        }

        Status = WorkOrderTimeEntryStatus.Rejected;
        RejectedAt = rejectedAt;
        RejectionReason = string.IsNullOrWhiteSpace(rejectionReason) ? null : rejectionReason.Trim();
    }

    public void MarkInvoiced(Guid salesInvoiceId, Guid salesInvoiceLineId, DateTimeOffset invoicedAt)
    {
        if (Status != WorkOrderTimeEntryStatus.Approved)
        {
            throw new DomainValidationException("Only approved labor entries can be invoiced.");
        }

        if (!BillableToCustomer)
        {
            throw new DomainValidationException("Only billable labor entries can be invoiced.");
        }

        SalesInvoiceId = salesInvoiceId;
        SalesInvoiceLineId = salesInvoiceLineId;
        InvoicedAt = invoicedAt;
        Status = WorkOrderTimeEntryStatus.Invoiced;
    }
}
