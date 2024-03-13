using Sandbox;
using System;

public enum ItemRarity
{
	[Icon( "sentiment_neutral" )] Common,
	[Icon( "sentiment_satisfied" )] Uncommon,
	[Icon( "mood" )] Rare,
	[Icon( "sentiment_very_satisfied" )] Exotic
}

public sealed class ItemStats : Component
{
	[Property] public ItemRarity IRarity { get; set; }




	private int _rarityPicker { get; set; }






	protected override void OnStart()
	{
		base.OnStart();

		_rarityPicker = Game.Random.Next( 1, 4 );
		

		switch(_rarityPicker)
		{
			case 1: IRarity = ItemRarity.Common;
				break;
			case 2: IRarity = ItemRarity.Uncommon;
				break;
			case 3: IRarity = ItemRarity.Rare;
				break;
			case 4: IRarity = ItemRarity.Exotic;
				break;
		}
	}




}
