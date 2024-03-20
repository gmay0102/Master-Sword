using Sandbox;

public sealed class InventoryController : Component
{

	public int Slots => 28;
	public int ActiveSlot = 0;

	[Property] public List<string> Inventory { get; set; } = new List<string>{};
	
	protected override void OnUpdate()
	{

	}
}
