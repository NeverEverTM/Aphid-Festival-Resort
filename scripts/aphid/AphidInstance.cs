using System;

public class AphidInstance(Guid GUID)
{
    public AphidData.Status Status = new();
    public AphidData.Genes Genes = new();
    public Aphid Entity;
    public Guid GUID { get; private set; } = GUID;
    public string ID { get; private set; } = GUID.ToString();

    public enum EntityStatus { Active, Passive }
    public EntityStatus Mode = EntityStatus.Active;
}