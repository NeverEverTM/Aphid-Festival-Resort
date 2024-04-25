public class AphidInstance
{
	public AphidData.Status Status = new();
	public AphidData.Genes Genes = new();
	public Aphid Entity;
	public string ID;

	public enum EntityStatus { Active, Passive }
	public EntityStatus Mode = EntityStatus.Active;
}
