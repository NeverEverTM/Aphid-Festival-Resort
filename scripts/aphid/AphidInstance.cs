using System;

public record AphidInstance(Guid GUID)
{
    public AphidData.Status Status { get; set; } = new();
    public AphidData.Genes Genes { get; set; } = new();
    public Aphid Entity;
    public Guid GUID { get; set; } = GUID;
    public string ID { get; set; } = GUID.ToString();
}