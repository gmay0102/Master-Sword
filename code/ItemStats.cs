using Sandbox;
using System;

public enum itemRarity
{
	[Icon( "shuffle" )] Random,
	[Icon( "sentiment_neutral" )] Common,
	[Icon( "sentiment_satisfied" )] Uncommon,
	[Icon( "mood" )] Rare,
	[Icon( "sentiment_very_satisfied" )] Exotic
}

public enum EquipSlot : byte
{
	Head,
	Face,
	Body,
	Legs,
	Feet,
	Hand
}

public sealed class ItemStats : Component
{
	[Property] public string Name { get; set; }
	
	[Property] public itemRarity ItemRarity { get; set; }
	[Property] public string Description { get; set; }
	/// <summary>
	/// Weight in pounds
	/// </summary>
	[Property, Sync] public int Weight { get; set; }

	/// <summary>
	/// If -1, item cannot be sold
	/// </summary>
	[Property, Sync] public int Value { get; set; } = -1;





	private int _rarityPicker { get; set; }






	protected override void OnStart()
	{
		base.OnStart();

		if ( ItemRarity == itemRarity.Random )
		{
			_rarityPicker = Game.Random.Next( 1, 5 );

			switch ( _rarityPicker )
			{
				case 1:
					ItemRarity = itemRarity.Common;
					break;
				case 2:
					ItemRarity = itemRarity.Uncommon;
					break;
				case 3:
					ItemRarity = itemRarity.Rare;
					break;
				case 4:
					ItemRarity = itemRarity.Exotic;
					break;
			}
		}
	}



}
