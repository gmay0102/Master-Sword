using Sandbox;
using System;
using System.Data.SqlTypes;

public sealed class InventoryController : Component
{
	[Property] public PlayerController Player { get; set; }

	public const int maxSlots = 24;

	public int Weight { get; private set; }
	public IReadOnlyList<ItemStats> InventoryItems => _inventoryItems;
	public IReadOnlyList<ItemStats> EquippedItems => _equippedItems;

	private readonly List<ItemStats> _inventoryItems;
	private readonly List<ItemStats> _equippedItems;

	public InventoryController()
	{
		_inventoryItems = new List<ItemStats>( new ItemStats[maxSlots] );
		_equippedItems = new List<ItemStats>( new ItemStats[Enum.GetNames( typeof( EquipSlot ) ).Length] );
	}



	protected override void OnUpdate()
	{

	}

}

